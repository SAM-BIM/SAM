# SAM Grasshopper icon redesign — PR1 record

Branch `feature/sam-gh-icon-redesign`, based on `sow/2026-Q3` @ `bc85ba61`.

## Current status
All 500 SAM Grasshopper objects (436 components + 64 params) now use redesigned icons: **500 / 500**.
The branch is built and validated. The PR is open for review and **not merged**.

## Work completed
- **Design system frozen**: `design/grasshopper-icons/ICON_DESIGN_SYSTEM.md`. The grammar is object glyph + SAM-green subject + operation badge. The palette is taken from sambim.xyz: CSS variables plus the emblem green `#81BC44` averaged from `SAM_BIM_logo.png`. It builds on the approved 5-icon exploration, with reduced detail and brand colours applied.
- **Inventory**: parsed from the C# source (`tools/inventory.py`). It covers every non-abstract class declaring `ComponentGuid` in `Grasshopper/SAM.*.Grasshopper`, with no duplicate GUIDs.
- **Classification**: `tools/classify.py` uses ordered rules plus an explicit `OVERRIDES` table. It produces `manifest.json` / `manifest.csv`, the source of truth.
- **Generation**: `tools/build.py` writes 330 distinct SVG icons (canonical, in `svg/`), their 24×24 PNGs (`png/24/`), 10 batch review sheets (`review/`) and the master sheet `SAM_GH_ICON_LIBRARY.png`.
- **Integration**: `tools/integrate.py` uses the existing mechanism: a `ResXFileRef` in `Properties/Resources.resx`, a typed property in `Resources.Designer.cs`, and the PNG in `Resources/Icons/`.

## Decisions and assumptions
- **Colour**: colour encodes operation *family* (7 badge styles); shape encodes the verb and the object. Green always marks the subject.
- **Shared icons**: qualifier variants (`…By<X>`) share an icon on purpose. That's 99 icons shared by 269 objects. See `ICON_DESIGN_SYSTEM.md` §6–7.
- **Params**: they get the object glyph with no badge.
- **Hidden/obsolete objects** (50): they get the same grammar icon as their live equivalent.
- **Resource mechanism kept**: `byte[]` + `Core.Convert.ToBitmap` in Analytical, `Bitmap` elsewhere. There's no new loader and no csproj change.
- **Legacy resources left in place**: `SAM_Small`, `SAM3`, `SAM_Geometry`, `HL_Logo24` and the rest stay, because `AssemblyInfo` (the plugin/tab identity) still uses them and git keeps the history.

## Files changed
- New: `design/grasshopper-icons/**` (spec, tools, manifest, svg, png, review sheets) and `Grasshopper/*/Resources/Icons/*.png` (346 files).
- Modified: 6× `Properties/Resources.resx`, 6× `Properties/Resources.Designer.cs`, 499 component/param `.cs` files (one token each, inside the `Icon` getter), and `A_SAMAnalytical.cs` (+1 `Icon` line; it previously had none).

## Validation
| Check | Result |
|---|---|
| Source re-parse after integration (`integrate.py` verify) | 500/500 objects reference their `SAM_GH_*` resource, and each PNG exists |
| C# diff audit | 999 changed lines: 499 token swaps + 1 added Icon line. 0 GUID, name, category or parameter lines touched |
| Identical-pixel collision check (`build.py`) | 0 identical icons across different icon ids |
| PNG format | all 346 production PNGs are 24×24 RGBA |
| `dotnet build` (Release) of all 6 GH projects | **0 compiler errors** |
| Embedded resources (`tools/check_assemblies.py` on built DLLs) | every required icon is embedded, at 24×24, in every DLL |
| `SAM.Core.Grasshopper.Tests` (Rhino.Testing, real Rhino 8 runtime; `--no-build` on the branch binaries) | **45 / 45 passed** |

The Analytical project's post-build copy step fails (`MSB3073`, xcopy exit 4) when the build is sandboxed, and the **untouched baseline fails identically** in the same sandbox. It's environment-only and unrelated to this change. The validation builds redirected `APPDATA`/`USERPROFILE` so the locally installed SAM plugin was not overwritten. The tests were then run with `--no-build` under the real profile, because Rhino.Testing hangs under a redirected profile.

## Unresolved issues / risks
- **Not yet viewed inside Rhino/Grasshopper.** Review so far is from the sheets, which render at native 24 px on simulated GH light and dark bodies.
- **Tiny badge glyphs**: some Change-family glyphs (snap, align, extend, sort) are hard to tell apart at 24 px. Their colour family still reads correctly.
- **Accidental legacy resources**: `SAM_Filter3` and `SAM_Get.Filterpng` are pre-existing 24×25 PNGs, now unused by components. They were left untouched.

## Recommended next step
Load the built `.gha` files in Rhino 8 / Grasshopper and review the ribbon and canvas in both light and dark themes. Refine any glyph through `tools/icons.py`, then re-run `classify → build → integrate`. Never hand-edit the PNGs.
