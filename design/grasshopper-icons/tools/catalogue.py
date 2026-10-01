"""Render the grammar catalogue: every object glyph, every badge, every modifier (review aid)."""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import icons  # noqa: E402
import render  # noqa: E402

DESIGN = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(DESIGN, "review", "_catalogue")


def main():
    os.makedirs(OUT, exist_ok=True)
    jobs = []
    for name in icons.G:
        jobs.append(("Object glyphs (subject = SAM green)", f"obj_{name}", icons.icon_svg(name), f"<b>{name}</b>"))
    fam_order = list(icons.FAMILIES)
    for op, (fam, _) in sorted(icons.BADGES.items(), key=lambda kv: (fam_order.index(kv[1][0]), kv[0])):
        jobs.append((f"Operation badges", f"op_{op}", icons.icon_svg("space", op), f"<b>{op}</b><br><i>{fam}</i>"))
    for label, kw in [("plural", dict(plural=True)), ("library", dict(container="library")), ("split", dict(op="split")),
                      ("intersect", dict(op="intersect")), ("difference", dict(op="difference")), ("context (neutral)", dict(subject=False))]:
        jobs.append(("Modifiers on shell / space", f"mod_{label.split()[0]}", icons.icon_svg("shell", **kw), f"<b>{label}</b>"))
    svgs, pngs = [], []
    for _, key, svg, _ in jobs:
        p = os.path.join(OUT, "svg", key + ".svg")
        os.makedirs(os.path.dirname(p), exist_ok=True)
        open(p, "w", encoding="utf-8").write(svg)
        svgs.append(p)
        pngs.append(os.path.join(OUT, "png", key + ".png"))
    render.rasterise(svgs, pngs)
    groups = {}
    for (g, key, _, label), png in zip(jobs, pngs):
        groups.setdefault(g, []).append((png, label))
    render.sheet("SAM GH icon grammar - catalogue", "Native 24 px on GH light/dark, plus 3x nearest-neighbour for inspection.",
                 list(groups.items()), os.path.join(OUT, "catalogue.png"), cols=6, big=72, width=1400)
    print("ok", len(jobs))


if __name__ == "__main__":
    main()
