"""Wire the generated 24x24 PNGs into the Grasshopper projects using the EXISTING resource mechanism.

Per project:
  * copies png/24/<icon_id>.png -> Grasshopper/<proj>/Resources/Icons/<Resource>.png  (only icons that project uses)
  * adds ResXFileRef entries to Properties/Resources.resx   (byte[] in SAM.Analytical.Grasshopper, Bitmap elsewhere,
    matching each project's existing convention)
  * adds matching strongly-typed properties to Properties/Resources.Designer.cs
  * replaces the resource token inside each component's `Icon` getter (Resources.OLD -> Resources.NEW); nothing else.
Idempotent: re-running replaces previously generated SAM_GH_* entries. Old resources are left untouched.
"""
import json
import os
import re
import shutil

HERE = os.path.dirname(os.path.abspath(__file__))
DESIGN = os.path.dirname(HERE)
REPO = os.path.dirname(os.path.dirname(DESIGN))
PNG = os.path.join(DESIGN, "png", "24")
BYTES_PROJECTS = {"SAM.Analytical.Grasshopper"}
PREFIX = "SAM_GH_"

TYPE_BITMAP = "System.Drawing.Bitmap, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"
TYPE_BYTES = "System.Byte[], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"


def read(p):
    raw = open(p, "rb").read()
    bom = raw.startswith(b"\xef\xbb\xbf")
    text = raw.decode("utf-8-sig")
    nl = "\r\n" if "\r\n" in text else "\n"
    return text.replace("\r\n", "\n"), bom, nl


def write(p, text, bom, nl):
    data = text.replace("\n", nl).encode("utf-8")
    open(p, "wb").write((b"\xef\xbb\xbf" if bom else b"") + data)


def update_resx(path, names, as_bytes):
    text, bom, nl = read(path)
    text = re.sub(r'  <data name="' + PREFIX + r'\w+" type="System\.Resources\.ResXFileRef, System\.Windows\.Forms">\n    <value>[^<]*</value>\n  </data>\n', "", text)
    t = TYPE_BYTES if as_bytes else TYPE_BITMAP
    entries = "".join(
        f'  <data name="{n}" type="System.Resources.ResXFileRef, System.Windows.Forms">\n'
        f'    <value>..\\Resources\\Icons\\{n}.png;{t}</value>\n  </data>\n' for n in sorted(names))
    text = text.replace("</root>", entries + "</root>")
    write(path, text, bom, nl)


def update_designer(path, names, as_bytes):
    text, bom, nl = read(path)
    text = re.sub(r"        \n        /// <summary>\n        ///   Looks up a localized resource of type [^\n]+\n        /// </summary>\n"
                  r"        internal static [^\n]+ " + PREFIX + r"\w+ \{\n(?:            [^\n]*\n)+        \}\n", "", text)
    typ, cast = ("byte[]", "byte[]") if as_bytes else ("System.Drawing.Bitmap", "System.Drawing.Bitmap")
    kind = "System.Byte[]" if as_bytes else "System.Drawing.Bitmap"
    props = "".join(
        f"        \n        /// <summary>\n        ///   Looks up a localized resource of type {kind}.\n        /// </summary>\n"
        f"        internal static {typ} {n} {{\n            get {{\n"
        f"                object obj = ResourceManager.GetObject(\"{n}\", resourceCulture);\n"
        f"                return (({cast})(obj));\n            }}\n        }}\n" for n in sorted(names))
    idx = text.rstrip().rfind("}")  # namespace close
    idx = text.rfind("    }", 0, idx)  # class close
    text = text[:idx].rstrip(" ") + props.lstrip("\n").replace("        \n", "        \n", 1) + text[idx:]
    write(path, text, bom, nl)


CLASS_DECL = r"^\s*(?:\[[^\]]*\]\s*)*(?:public|internal)[\w\s]*\bclass\s+{}\b"


def update_icon(src_path, cls, resource, analytical):
    text, bom, nl = read(src_path)
    m = re.search(CLASS_DECL.format(cls), text, re.M)
    assert m, (src_path, cls)
    nxt = re.search(r"^\s*(?:public|internal)[\w\s]*\bclass\s+\w+", text[m.end():], re.M)
    end = m.end() + nxt.start() if nxt else len(text)
    body = text[m.end():end]
    ic = re.search(r"override\s+(?:System\.Drawing\.)?Bitmap\s+Icon\b", body)
    if ic:
        seg_start = ic.end()
        seg = body[seg_start: seg_start + 300]
        new_seg, n = re.subn(r"\bResources\.\w+", "Resources." + resource, seg, count=1)
        assert n == 1, (src_path, cls)
        body = body[:seg_start] + new_seg + body[seg_start + 300:]
    else:
        # class with no Icon override (inherits GH default): add one line after ComponentGuid, same style as siblings
        g = re.search(r"\n(\s*)public override Guid ComponentGuid[^\n]*\n", body)
        assert g, (src_path, cls)
        ind = g.group(1)
        expr = f"Core.Convert.ToBitmap(Resources.{resource})" if analytical else f"Resources.{resource}"
        line = f"\n{ind}protected override System.Drawing.Bitmap Icon => {expr};\n"
        body = body[:g.end()] + line + body[g.end():]
    text = text[:m.end()] + body + text[end:]
    write(src_path, text, bom, nl)


def main():
    rows = json.load(open(os.path.join(DESIGN, "manifest.json"), encoding="utf-8"))
    by_proj = {}
    for r in rows:
        by_proj.setdefault(r["project"], []).append(r)
    for proj, items in sorted(by_proj.items()):
        pdir = os.path.join(REPO, "Grasshopper", proj)
        icon_dir = os.path.join(pdir, "Resources", "Icons")
        if os.path.isdir(icon_dir):
            shutil.rmtree(icon_dir)
        os.makedirs(icon_dir)
        names = {}
        for r in items:
            names[r["resource"]] = r["icon_id"]
        for res, iid in names.items():
            shutil.copyfile(os.path.join(PNG, iid + ".png"), os.path.join(icon_dir, res + ".png"))
        as_bytes = proj in BYTES_PROJECTS
        update_resx(os.path.join(pdir, "Properties", "Resources.resx"), names, as_bytes)
        update_designer(os.path.join(pdir, "Properties", "Resources.Designer.cs"), names, as_bytes)
        for r in items:
            update_icon(os.path.join(REPO, r["source"]), r["class"], r["resource"], as_bytes)
            r["status"] = "integrated"
        print(f"{proj}: {len(items)} objects, {len(names)} icon resources")
    json.dump(rows, open(os.path.join(DESIGN, "manifest.json"), "w", encoding="utf-8"), indent=1)
    import csv
    with open(os.path.join(DESIGN, "manifest.csv"), "w", newline="", encoding="utf-8") as fh:
        w = csv.DictWriter(fh, fieldnames=list(rows[0].keys()))
        w.writeheader()
        w.writerows(rows)


if __name__ == "__main__":
    main()
