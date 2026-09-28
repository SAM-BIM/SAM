"""Verify built SAM *.Grasshopper.dll files embed every SAM_GH_* icon their manifest rows need.

Format-agnostic (works for both byte[] and System.Resources.Extensions Bitmap resources):
  * every required resource name must occur in the assembly's .resources name table (UTF-16LE), and
  * the assembly must embed at least as many 24x24 PNGs as required icons (legacy odd-size PNGs are reported).
Usage: python check_assemblies.py <build dir>
"""
import io
import json
import os
import sys

from PIL import Image

DESIGN = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SIG, IEND = b"\x89PNG\r\n\x1a\n", b"IEND\xaeB`\x82"


def pngs(blob):
    i = 0
    while True:
        i = blob.find(SIG, i)
        if i < 0:
            return
        j = blob.find(IEND, i)
        yield Image.open(io.BytesIO(blob[i: j + len(IEND)]))
        i = j


def main(build):
    rows = json.load(open(os.path.join(DESIGN, "manifest.json"), encoding="utf-8"))
    need = {}
    for r in rows:
        need.setdefault(r["project"], set()).add(r["resource"])
    fail = 0
    for proj, names in sorted(need.items()):
        blob = open(os.path.join(build, proj + ".dll"), "rb").read()
        missing = [n for n in names if n.encode("utf-16-le") not in blob]
        imgs = list(pngs(blob))
        bad = [im.size for im in imgs if im.size != (24, 24)]  # legacy (pre-redesign) resources, informational
        ok = not missing and sum(im.size == (24, 24) for im in imgs) >= len(names)
        fail += not ok
        print(f"{proj}: {len(names)} required icons, names missing {len(missing)}, "
              f"embedded PNGs {len(imgs)} (legacy non-24x24: {len(bad)}) -> {'OK' if ok else 'FAIL'}")
    print("RESULT:", "OK" if not fail else f"FAIL ({fail} projects)")
    return 1 if fail else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1]))
