"""Build the SAM GH icon library from manifest.json.

  python build.py            -> svg/ (canonical), png/24/ (production), review sheets, collision report
Deterministic: output depends only on manifest.json + icons.py.
"""
import collections
import hashlib
import html
import json
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import icons  # noqa: E402
import render  # noqa: E402

DESIGN = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SVG = os.path.join(DESIGN, "svg")
PNG = os.path.join(DESIGN, "png", "24")
REVIEW = os.path.join(DESIGN, "review")


def display_name(r):
    import re
    n = r["name"] or ""
    m = re.match(r"^\{typeof\((?:global::[\w.]+\.)?(\w+)\)\.Name\}$", n)
    return (m.group(1) if m else n) + (" (param)" if r["kind"] == "param" else "")


def label(r):
    n = html.escape(display_name(r))
    tail = f"<br><i>{r['object']} · {r['op'] or 'param'}{' · plural' if r['plural'] else ''}{' · library' if r['container'] else ''}</i>"
    flag = " <i>(hidden/obsolete)</i>" if r["obsolete"] or "hidden" in (r["exposure"] or "") else ""
    return f"<b>{n}</b>{flag}{tail}"


def main():
    rows = json.load(open(os.path.join(DESIGN, "manifest.json"), encoding="utf-8"))
    ids = {}
    for r in rows:
        ids.setdefault(r["icon_id"], r)
    os.makedirs(SVG, exist_ok=True)
    # remove stale sources so svg/ mirrors the manifest exactly
    for fn in os.listdir(SVG):
        if fn.endswith(".svg") and fn[:-4] not in ids:
            os.remove(os.path.join(SVG, fn))
    svgs, pngs = [], []
    for iid, r in sorted(ids.items()):
        svg = icons.icon_svg(r["object"], r["op"], plural=r["plural"], container=r["container"], comment=f"SAM GH icon {iid}")
        p = os.path.join(SVG, iid + ".svg")
        open(p, "w", encoding="utf-8", newline="\n").write(svg)
        svgs.append(p)
        pngs.append(os.path.join(PNG, iid + ".png"))
    if os.path.isdir(PNG):
        for fn in os.listdir(PNG):
            if fn[:-4] not in ids:
                os.remove(os.path.join(PNG, fn))
    render.rasterise(svgs, pngs)

    # ---- collision check: identical 24px pixels for different icon ids
    by_hash = collections.defaultdict(list)
    for iid in ids:
        by_hash[hashlib.sha1(open(os.path.join(PNG, iid + ".png"), "rb").read()).hexdigest()].append(iid)
    dup = [v for v in by_hash.values() if len(v) > 1]

    # ---- batch sheets
    os.makedirs(REVIEW, exist_ok=True)
    batches = collections.OrderedDict()
    for r in rows:
        batches.setdefault(r["batch"], []).append(r)
    for b, items in batches.items():
        groups = collections.OrderedDict()
        for r in items:
            key = "Parameters: object glyph only, no badge" if r["kind"] == "param" else r["object"]
            groups.setdefault(key, []).append((os.path.join(PNG, r["icon_id"] + ".png"), label(r)))
        fn = "batch_" + b.split()[0] + ".png"
        render.sheet(f"SAM GH icons - batch {b}", f"{len(items)} objects. Native 24 px on GH light/dark body; 3x nearest-neighbour for inspection only.",
                     list(groups.items()), os.path.join(REVIEW, fn), cols=4, big=72, width=1500)
        for r in items:
            r["status"] = "redesigned"

    # ---- master library sheet: one tile per GH object, grouped by batch
    groups = [(b, [(os.path.join(PNG, r["icon_id"] + ".png"), html.escape(display_name(r).split(".")[-1])) for r in items])
              for b, items in batches.items()]
    render.sheet("SAM Grasshopper icon library", f"{len(rows)} Grasshopper objects · {len(ids)} distinct icons · grouped by object family. "
                 "Native 24 px (left: GH light, right: GH dark) and 2x inspection.", groups,
                 os.path.join(DESIGN, "SAM_GH_ICON_LIBRARY.png"), cols=8, big=48, width=2000, mini=True)

    json.dump(rows, open(os.path.join(DESIGN, "manifest.json"), "w", encoding="utf-8"), indent=1)
    import csv
    cols = list(rows[0].keys())
    with open(os.path.join(DESIGN, "manifest.csv"), "w", newline="", encoding="utf-8") as fh:
        w = csv.DictWriter(fh, fieldnames=cols)
        w.writeheader()
        w.writerows(rows)
    print(f"{len(rows)} objects, {len(ids)} icons, identical-pixel groups: {dup}")


if __name__ == "__main__":
    main()
