# AperturesBySolids: framed windows in a row stay separate apertures — Q4 (2026-10-25)

Status: implemented locally on `fix/apertures-by-solids-units-q4` (base `sow/2026-Q4` at `457defbb`). Not pushed; no
PR yet (owner go-ahead needed). Tested content `22569e21`; later commits change this record only. Step 2 of the
Softhouse "SAM model from solids" part 2 ("windows - apertures were more detail").

## Problem (Softhouse numbers)

Each Softhouse window or glazed door is two solids: the glass (24 mm deep, inset 50 mm) and a frame around it (100 or
150 mm deep). Frames of neighbouring windows share a jamb face. `AperturesBySolids` joins members within `maxGap`
(0.01 m), so a whole row became one aperture: 201 solids -> **45 apertures** (41 hosted) for **100 reference openings**
(gbXML `softhouse3.xml`, frame width 0.05 m). Per storey and facade (reference / before / after):

| storey facade | reference | before | after |
| --- | --- | --- | --- |
| GF north (y 8.52) | 16 | 8 | 16 |
| GF south (y -6.62) | 12 | 5 | 12 |
| GF east / west | 2 / 2 | 1 / 2 | 2 / 2 |
| 1F south (y -2.89) | 24 | 4 ribbons of 5.35 m (+1 internal screen) | 24 (+1 internal screen) |
| 1F north / east / west | 8 / 1 / 1 | 8 / 1 / 1 | 8 / 1 / 1 |
| 2F south | 24 | 4 | 24 |
| 2F north / east / west | 8 / 1 / 1 | 8 / 1 / 1 | 8 / 1 / 1 |

Every reference opening lies inside a SAM aperture in both runs: nothing was lost or trimmed, openings were merged.
Frame vs pane is not lost: the default aperture construction gives a frame of about 0.05 m (e.g. a 1.06 x 1.8 m window:
1.91 m2, pane 1.63 m2), the source frame width. Not hosted before: the internal glazed screen (#0, y -0.69, no reference
opening) and the 3 Dining 1 apertures plus the #385 door (no Dining 1 space).

## Change

`SAM/SAM.Analytical/Create/AperturesBySolids.cs`: before proximity joining, a member without a group key whose
bounding box holds other ungrouped members (within `maxGap`; the outermost box when nested, e.g. frame > sash > glass)
gets a group key shared with them, as if they were in a Rhino group. Units that only touch stay separate; ungrouped
members that are neither around nor inside another (stick-frame bars) still join by proximity, and explicit group keys
win. The key carries a leading NUL so it cannot clash with a user key. XML docs: new "Units" item.

`SAMAnalytical.CreateAperturesBySolids` 1.0.2 -> **1.1.0** (minor: the geometry result changes; GUID, parameters and
defaults unchanged); description says framed windows in a row stay separate. Note: SAM#221's version guard edits the
adjacent line of `BySolidsDiagnosticDetailTests.ComponentVersions_AreBumped_AndTheOutputsAreStillNamedLog`, so the two
branches conflict there (resolve to both 1.1.0, as on the local integration branch).

## Files changed

- `SAM/SAM.Analytical/Create/AperturesBySolids.cs`
- `Grasshopper/SAM.Analytical.Grasshopper/Component/SAMAnalyticalCreateAperturesBySolids.cs`
- `SAM/SAM.Tests/AperturesBySolidsTests.cs`: `AperturesBySolids_RowOfTouchingFramedWindows_StaysOneAperturePerWindow`,
  `AperturesBySolids_SashInsideFrame_IsOneWindowWithItsGlass`; the existing
  `TouchingWindowsWithoutGroupKeys_MergeIntoOneAssembly` (frames without glass) still merges
- `Grasshopper/SAM.Core.Grasshopper.Tests/BySolidsDiagnosticDetailTests.cs` (version pin 1.1.0)
- this record

## Validation (local, machine `michal`; tested content `22569e21`)

```powershell
# deploy-safe builds: APPDATA/USERPROFILE redirected; SolutionDir set for the GH PostBuild xcopy
dotnet build SAM/SAM.Tests/SAM.Tests.csproj -c Release
dotnet test SAM/SAM.Tests/SAM.Tests.csproj -c Release --no-build
dotnet build Grasshopper/SAM.Core.Grasshopper.Tests/SAM.Core.Grasshopper.Tests.csproj -c Release -p:SolutionDir=<repo>\
dotnet test Grasshopper/SAM.Core.Grasshopper.Tests/SAM.Core.Grasshopper.Tests.csproj -c Release --no-build
```

- SAM.Tests 3740 / 3740 (2 new); SAM.Core.Grasshopper.Tests 84 / 84 (failed 1 before the version pin was updated).
- Softhouse, headless `pipe` and headless GH (`ghrun`, `%APPDATA%\SAM` = local integration build): **101 apertures
  (100 + the internal screen), 96 hosted** (was 45 / 41). SAM Check unchanged (the 6 known "not enclosed (1e-6)"
  warnings). Comparison listings: `SAM_daily/2026-10-25-Softthouse/handover/evidence/2026-10-25-part2/aperture_comparison_*.txt`.
- SAM_OCCT does not use `AperturesBySolids`; its suites against the integration SAM DLLs: see the ownership-sample record.

## Decisions and assumptions

- Containment of axis-aligned bounding boxes: a pane is inside the convex hull of its frame, so its box is inside the
  frame's box also on rotated walls; two neighbouring frames never hold each other.
- A frame without its glass touching a framed unit still joins that unit by proximity (unchanged rule for members
  outside units).

## Next step

Owner: review; on go-ahead push and open the PR into `sow/2026-Q4` with this record (independent of SAM#220-#222;
resolve the test-line conflict with SAM#221 when the second of the two merges).
