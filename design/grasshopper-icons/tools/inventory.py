"""Inventory every Grasshopper document object (components + params) in SAM/Grasshopper.

Parses C# source (no build needed). Writes manifest_raw.json next to this tool's parent.
An object is inventoried when a non-abstract class declares `ComponentGuid`.
"""
import json
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
DESIGN = os.path.dirname(HERE)
REPO = os.path.dirname(os.path.dirname(DESIGN))
GH = os.path.join(REPO, "Grasshopper")

CLASS_RE = re.compile(r"^\s*(?:\[[^\]]*\]\s*)*((?:public|internal|private|protected|abstract|sealed|static|partial|\s)+)class\s+(\w+)(?:<[^>{]*>)?\s*(?::\s*([^{\n]+))?", re.M)
GUID_RE = re.compile(r"ComponentGuid\s*(?:=>|\{\s*get\s*\{\s*return)\s*new\s+Guid\(\s*\"([0-9a-fA-F-]+)\"")
ICON_RE = re.compile(r"override\s+(?:System\.Drawing\.)?Bitmap\s+Icon\b(.{0,300})", re.S)
EXPOSURE_RE = re.compile(r"override\s+GH_Exposure\s+Exposure\s*=>\s*([^;]+);")
def base_args(text, pos):
    """Split the argument list of the `base(` call starting at/after pos (string- and paren-aware)."""
    i = text.index("(", pos) + 1
    args, depth, cur, instr = [], 0, "", False
    while i < len(text):
        ch = text[i]
        if instr:
            cur += ch
            if ch == "\\":
                cur += text[i + 1]; i += 1
            elif ch == '"':
                instr = False
        elif ch == '"':
            instr = True; cur += ch
        elif ch in "([{":
            depth += 1; cur += ch
        elif ch in ")]}":
            if depth == 0:
                args.append(cur.strip()); return args
            depth -= 1; cur += ch
        elif ch == "," and depth == 0:
            args.append(cur.strip()); cur = ""
        else:
            cur += ch
        i += 1
    return args


def lit(a):
    a = a.strip()
    if re.fullmatch(r'"(?:[^"\\]|\\.)*"', a):
        return a[1:-1]
    return "{" + a + "}"


def main():
    items = []
    for proj in sorted(os.listdir(GH)):
        pdir = os.path.join(GH, proj)
        if not os.path.isdir(pdir) or ".Tests" in proj or not proj.endswith(".Grasshopper"):
            continue
        for root, dirs, files in os.walk(pdir):
            dirs[:] = [d for d in dirs if d not in ("obj", "bin")]
            for f in sorted(files):
                if not f.endswith(".cs") or f.endswith(".Designer.cs"):
                    continue
                path = os.path.join(root, f)
                src = open(path, encoding="utf-8-sig").read()
                # strip // comments to avoid template/commented code
                code = re.sub(r"(?m)(^|[\s;{}])//[^\n]*", r"\1", src)
                classes = list(CLASS_RE.finditer(code))
                for i, cm in enumerate(classes):
                    body = code[cm.end(): classes[i + 1].start() if i + 1 < len(classes) else len(code)]
                    g = GUID_RE.search(body)
                    if not g or "abstract" in cm.group(1):
                        continue
                    ic = ICON_RE.search(body)
                    ex = EXPOSURE_RE.search(body)
                    ctor = re.search(r"\b" + cm.group(2) + r"\s*\(\s*\)\s*(?=:\s*base\()", body)
                    b = base_args(body, ctor.end()) if ctor else None
                    if b and b[-1].startswith("GH_ParamAccess"):
                        b = b[:-1]
                    obsolete = bool(re.search(r"\[Obsolete", code[max(0, cm.start() - 200): cm.end()])) or "_Obsolete" in cm.group(2)
                    items.append({
                        "project": proj,
                        "class": cm.group(2),
                        "base_type": (cm.group(3) or "").strip(),
                        "guid": g.group(1).lower(),
                        "name": lit(b[0]) if b else None,
                        "nickname": lit(b[1]) if b and len(b) > 1 else None,
                        "category": lit(b[-2]) if b and len(b) >= 4 else None,
                        "subcategory": lit(b[-1]) if b and len(b) >= 4 else None,
                        "icon_expr": (re.search(r"Resources\.(\w+)", ic.group(1)) or [None, None])[1] if ic else None,
                        "icon_via_bytes": bool(ic and "ToBitmap" in ic.group(1) or ic and "MemoryStream" in ic.group(1)),
                        "exposure": ex.group(1).strip() if ex else None,
                        "obsolete": obsolete,
                        "source": os.path.relpath(path, REPO).replace("\\", "/"),
                    })
    out = os.path.join(DESIGN, "manifest_raw.json")
    json.dump(items, open(out, "w", encoding="utf-8"), indent=1)
    print(len(items), "objects ->", out)


if __name__ == "__main__":
    sys.exit(main())
