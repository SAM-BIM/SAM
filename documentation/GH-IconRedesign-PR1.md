# SAM Grasshopper icon redesign — PR1 record

Branch `feature/sam-gh-icon-redesign`, based on `sow/2026-Q3` @ `bc85ba61`. PR SAM-BIM/SAM#166.

## Current status
All **522** SAM Grasshopper objects (458 components + 64 params) now use redesigned icons: **522 / 522**.
A final visual-acceptance pass was done in real Rhino 8 / Grasshopper. The branch is built and validated, ready for final PR review, and **not merged**.

## Work completed
- **Design system frozen**: `design/grasshopper-icons/ICON_DESIGN_SYSTEM.md`. The grammar is object glyph + SAM-green subject + operation badge. The palette comes from sambim.xyz: CSS variables plus the emblem green `#81BC44` from `SAM_BIM_logo.png`.
- **Inventory**: parsed from the C# source (`tools/inventory.py`). It covers every non-abstract class that declares `ComponentGuid`. It matches both `new Guid("…")` and target-typed `new("…")`.
- **Classification**: `tools/classify.py` (rules plus an `OVERRIDES` table) produces `manifest.json` / `manifest.csv`, the source of truth.
- **Generation**: `tools/build.py` writes 337 distinct SVGs (canonical), their 24×24 PNGs, 10 batch review sheets and `SAM_GH_ICON_LIBRARY.png`.
- **Integration**: `tools/integrate.py` uses the existing `ResXFileRef` + `Resources.Designer.cs` mechanism, with the PNGs in `Resources/Icons/`.
- **Live review**: `tools/live_review_rhino.py` runs in real Rhino 8. It places representative components at 1:1 and captures the canvas and ribbon in the light and dark skins, then restores the Grasshopper settings. Evidence is in `design/grasshopper-icons/review/live/`.

## Count reconciliation
- **436 + 64 (not 437 + 63)**: `GooObjectParam` derives from `GH_Param<IGH_Goo>`, not `GH_PersistentParam`. An early summary grouped it by its (mis-parsed) category, so it counted as a component. It is a param. The GUID set was unchanged.
- **500 → 522**: 22 classes declare `ComponentGuid => new("…")` (target-typed `new`). The first inventory regex only matched `new Guid("…")`, so these kept `SAM_Small`. They include the approved `SAMAnalytical.AddAperturesByAzimuths`, plus AddFeatureShade, the CreateCaseBy… set, the Part F components, FilterByAzimuth/Geometry/Points, UpdateZone, Samples and others. The regex is fixed; all 22 are classified, integrated and verified.
- **335 → 330 → 337 distinct icons**: 335 was an interim count before the Add rule (plurality not drawn for Add) and the panel filter/snap corrections. 330 was the first final count. The 22 recovered objects added 7 new icon ids.
- **337 SVGs vs 354 PNGs**: one SVG per distinct icon id. Each assembly embeds its own copy of an icon it uses: Analytical 236 + Core 73 + Geometry 31 + Weather 10 + Architectural 3 + Math 1 = 354. 17 icons are used in two assemblies, and that duplication is necessary.

## Live visual review (real Rhino 8 / Grasshopper, native 1:1)
- **Unexpected condition**: freshly placed components show Grasshopper's **orange warning body** (unconnected inputs). The review sheets had only simulated grey bodies.
- **Finding 1, fixed**: on orange bodies the Transfer badge (white disc, hairline glyph) collapsed to a grey dot, so **ToFile/FromFile and ToJson/FromJson were indistinguishable**. Transfer glyphs are now bold solid arrows (↓ import, ↑ export, ⇄ convert).
- **Finding 2, fixed**: Change-family glyphs (snap, align, extend, sort, merge, split, update, set…) were too thin to tell apart. Every badge glyph now uses a stroke of at least 1.6 px with filled arrowheads, and shapes are simplified to fill the disc. That's 21 badge glyphs in total, re-drawn in `icons.py`.
- **Checks that passed**: Get ↗ vs Set ↙; Create (single green) vs Add (grey + green); singular vs plural (SplitPanel vs SplitPanels, GetSpaceByName vs GetSpaces); the Panel, Aperture and Construction families; and all object shapes in both the light and dark skins.
- **After the fix**: re-captured in real Rhino. All the pairs above are distinct at native size. Evidence: `review/live/live_pairs_after_badge_fix.png`.

## Decisions and assumptions
- **Colour**: colour encodes the operation *family* (7 badge styles). Shape encodes the verb and the object. Green always marks the subject.
- **Shared icons**: qualifier variants (`…By<X>`) share an icon on purpose: 101 icons are shared by 286 objects.
- **Params**: the object glyph with no badge.
- **Hidden/obsolete** (50): the same icon as their live equivalent.
- **Resource mechanism kept**: `byte[]` + `Core.Convert.ToBitmap` in Analytical, `Bitmap` elsewhere. No csproj change.
- **Legacy resources kept**: they're used by `AssemblyInfo`, and git keeps the history.
- **Position-independent rasteriser**: each icon is rendered alone at a fixed position. The earlier shared sprite made antialiasing depend on sprite position. The switch was a one-time change of 186 antialiasing pixels over 69 icons; the SVG sources didn't change.

## Files changed
- New: `design/grasshopper-icons/**` and `Grasshopper/*/Resources/Icons/*.png` (354 files).
- Modified: 6× `Resources.resx`, 6× `Resources.Designer.cs`, and 521 `.cs` files. Each `.cs` change is one resource token inside the `Icon` getter, plus 1 added `Icon` line on `A_SAMAnalytical`, which had none.

## Validation (final)
| Check | Result |
|---|---|
| Source re-parse after integration | **522/522** objects reference their `SAM_GH_*` resource, and each PNG exists |
| C# diff vs base | 1043 changed lines = 521 token swaps + 1 added Icon line. 0 GUID, name, category or parameter lines |
| Identical-pixel collision check | 0 |
| Production PNGs | 354, all 24×24 RGBA |
| `dotnet build` of the 6 GH projects (Debug, installed to `%APPDATA%\SAM`) | 0 compiler errors |
| `tools/check_assemblies.py build` | every required icon embedded at 24×24 in all 6 DLLs |
| `SAM.Core.Grasshopper.Tests` (real Rhino 8 runtime) | **45 / 45 passed** |
| Live Rhino canvas, light + dark skins | 56 representative components loaded; icons render. The 2 findings above are fixed and re-verified |

Known build noise, not caused by this PR:
- Analytical's post-build `xcopy …\files\resources` / `Samples` exits with code 4 ("File not found"). The untouched baseline behaves identically.
- A post-build copy fails with a file lock if Rhino is open during the build.

## Unresolved issues / risks
- **Other SAM repos**: their plugins (SAM_UI, Acoustic, Mollier, Weather.UI…) are out of scope and keep their old icons. They also appear in the SAM tabs.
- **Unused legacy PNGs**: `SAM_Filter3` and `SAM_Get.Filterpng` are pre-existing 24×25 files, now unused by components. They were left untouched.

## Recommended next step
Final PR review of SAM-BIM/SAM#166, then merge (by the maintainer). After merge, add the `PROJECT_PROGRESS.md` closeout entry on `sow/2026-Q3` with the merge SHA.
