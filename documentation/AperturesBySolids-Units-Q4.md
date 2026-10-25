# AperturesBySolids: framed windows in a row stay separate apertures — Q4 (2026-10-25)

Status: branch `fix/apertures-by-solids-units-q4` on `sow/2026-Q4` at `81a781ab` (after the merges of SAM-BIM/SAM#220,
#222 and #221 `945eea51`). PR SAM-BIM/SAM#224 into `sow/2026-Q4`. Independent reviews 2026-10-25: of `d108e6df`
REVISE (R1, R2), of `dd125e52` REVISE (R4, R5); both corrected and retested. Tested content **`9a191d38`**
(production code `4ee951c1`; earlier `890d535f` after R1/R2, `d17d9b84` before the reviews); later commits change this record only, and the PR description
holds the final head and hosted checks. Step 2 of the Softhouse "SAM model from solids" part 2 ("windows - apertures
were more detail").

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
Frame vs pane is not lost: the default aperture construction gives a frame of about 0.05 m, the source frame width.

## Change

`SAM/SAM.Analytical/Create/AperturesBySolids.cs`, before proximity joining (explicit group keys first, as before):

1. **Strict enclosure** (`ApertureMember.Encloses`), judged in the outer member's own plane frame so that placement in
   the world does not matter. Outer J strictly encloses inner I when:
   - (candidate filter only) I's world bounding box lies within J's grown by 2 x `maxGap`; a point within `maxGap` of
     J's depth and hull lies inside that box under any rotation (sqrt 2 < 2), so the filter never decides alone;
   - both are thinnest across parallel planes (`tolerance_Angle`), with the thinnest-plane search shared with
     `AperturesByAssembly` (`ThinnestPlane`);
   - every vertex of I lies within J's depth along J's plane and, projected on it, inside J's convex hull, each within
     `maxGap`;
   - and **not** the other way round in the same frame: if J's vertices also lie within I's depth range and I's
     projected hull (each within `maxGap`), the two are *equivalent* and neither encloses the other.
2. **Units from one snapshot.** Enclosure is evaluated for every pair of ungrouped members before any key is given.
   *Outermost* members enclose something and are enclosed by nothing (members in an enclosure cycle are never
   outermost). A member enclosed by exactly one outermost member gets that member's unit key, as if both were in a
   Rhino group.
3. **Conservative fallback, deterministic.** A unit is not formed, or is dissolved, when it cannot be justified; the
   members involved then follow the unchanged proximity rule, exactly as without units:
   - a member enclosed by no outermost member (non-transitive tolerance chain) gets no key;
   - a member enclosed by **several** outermost members dissolves **all** those competing units together (not only
     its own assignment);
   - ungrouped members connected by proximity that touch two or more different keys dissolve the inferred unit keys
     among them; repeated until nothing changes. Explicit (user) group keys are never removed.
4. Units that only touch each other (a row of framed windows sharing jambs) stay separate apertures; stick-frame bars
   (neither around nor inside another member) join by proximity as before. The unit key carries a leading NUL so it
   cannot clash with a user key.

`SAMAnalytical.CreateAperturesBySolids` 1.0.2 -> **1.1.0** (minor: the geometry result changes); description says
framed windows in a row stay separate. GUID, parameter names, order, types, access, optionality, visibility and
defaults unchanged (contract audit below). Version guard pins 1.1.0.

## Review corrections

First review (of `d108e6df`):

- **R1**: two parallel 0.1 m panes 0.9 m apart became one 1.1 m deep aperture after a 45 degree rotation (world-box
  containment only). Fixed by the plane, depth and hull evidence of item 1.
- **R2**: unit keys were overwritten and partitions depended on input order (tolerance chain A/B/C). Fixed by item 2;
  all 6 orders give one 4.03492 m2 aperture, as `sow/2026-Q4` does.

Second review (of `dd125e52`); these disproved two claims of the previous record, that units were judged
independently of rotation and that the result could not depend on input order:

- **R4**: the reverse world-box test (outer box also within the inner box within `maxGap`) still decided whether a
  unit existed. Two touching slim 2 x 1 m framed windows (frames 0.024 m deep, panes inset 12 mm at the jambs and 5 mm
  at head and sill) gave two 2 m2 apertures at 0 degrees and one 4 m2 aperture at 45. Fixed: the world box is only the
  one-direction filter, and strictness is the plane-frame test of item 1; both orientations give two windows.
- **R5**: a member shared by two outermost members left both units keyed and joined one of them by input order: two
  overlapping 4 m2 apertures whose sources changed with the order (`sow/2026-Q4`: one 6 m2 aperture). Fixed by item 3:
  one 6 m2 aperture with all five sources in every order.

## Files changed

- `SAM/SAM.Analytical/Create/AperturesBySolids.cs`
- `Grasshopper/SAM.Analytical.Grasshopper/Component/SAMAnalyticalCreateAperturesBySolids.cs` (version, description)
- `SAM/SAM.Tests/AperturesBySolidsTests.cs` (tests below; the class joins the xUnit collection
  "SAM.Analytical.ActiveSetting default aperture construction library")
- `SAM/SAM.Tests/PartFTransferAirDoorTests.cs`, `SAM/SAM.Tests/QuadraticScanRegressionTests.cs` (collection comments only)
- `Grasshopper/SAM.Core.Grasshopper.Tests/BySolidsDiagnosticDetailTests.cs` (version pin 1.1.0)
- this record

## Tests (all in `SAM/SAM.Tests/AperturesBySolidsTests.cs`)

- Feature: `RowOfTouchingFramedWindows_StaysOneAperturePerWindow`, `SashInsideFrame_IsOneWindowWithItsGlass`.
- R1: `SeparateParallelPanes_StaySeparateUnderRigidMotion`, `SeparateParallelFramedWindows_StaySeparateUnderRigidMotion`
  (0 and 45 degrees with translation; areas, SourceThickness, membership, mid-planes),
  `RowOfTouchingFramedWindows_SameUnitsUnderRigidMotion`.
- R2: `ToleranceChainOfNestedMembers_SameApertureForEveryInputOrder` (6 orders),
  `NestedSashAndStickFrames_SameAperturesForEveryInputOrder` (3 orders),
  `ExplicitGroupKeyFrame_KeepsItsGlassAndStaysApartFromAFramedUnit` (24 orders).
- R4: `SlimFramedRow_StaysTwoWindowsForEveryRotationAndOrder` (0, 13, 45, 90, 137 degrees with translation, 24 orders
  each; membership, 2 m2 each, depth 0.024 m, mid-plane), `NearlyEqualMembers_AreNoUnitAndJoinByProximity` (0 and 45
  degrees; equivalence policy).
- R5: `SharedInnerOfTwoContainers_FallsBackToOneApertureForEveryOrder` (120 orders at 0 and 45 degrees; one 6 m2
  aperture, all sources, depth and mid-plane), `MullionTouchingTwoFramedWindows_FallsBackToOneApertureForEveryOrder`
  (120 orders at 0 and 45 degrees; the same ambiguity through proximity, framed).
- Unchanged and passing: touching frames without glass merge, different group keys stay separate, stick frame and
  glass, door frame with leaf, corner window not planar, rotated wall, constructions, invalid member, hosting.

Fail before / pass after (machine-local `SAM-BIM/.evidence-pr223-224-corrections/`):
on `d108e6df` exactly the 3 R1/R2 cases failed (`pr224-fail-before-d108e6df.txt`); on `890d535f` exactly the R4 cases
at 45 and 137 degrees and the R5 and mullion cases at 0 and 45 degrees failed, 32 passed
(`revision2/pr224-fail-before-890d535f.txt`); on `4ee951c1` 38 / 38 aperture tests pass.

## Validation (local, machine `michal`; tested content `9a191d38` on base `81a781ab`)

```powershell
# deploy-safe builds: APPDATA and USERPROFILE redirected to a scratch profile (the GH PostBuild copies DLLs to
# %APPDATA%\SAM); SolutionDir set for the PostBuild xcopy. Run the test projects one after the other (two in-process
# Rhino starts at once fail with COMException in OneTimeSetUp), each with a hang timeout.
dotnet build Grasshopper/SAM.Core.Grasshopper.Tests/SAM.Core.Grasshopper.Tests.csproj -c Release -p:SolutionDir=<repo>\
dotnet build SAM/SAM.Tests/SAM.Tests.csproj -c Release
dotnet test SAM/SAM.Tests/SAM.Tests.csproj -c Release --no-build --blame-hang-timeout 10m
dotnet test Grasshopper/SAM.Core.Grasshopper.Tests/SAM.Core.Grasshopper.Tests.csproj -c Release --no-build --blame-hang-timeout 5m
dotnet test SAM/SAM.Tests/SAM.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~AperturesBySolids"
```

- SAM.Tests **3783 passed, 0 failed, 0 skipped** in three consecutive full runs; SAM.Core.Grasshopper.Tests
  **87 / 0 / 0**; focused aperture suite 38 / 0 / 0. Build 0 errors. TRX and logs:
  `SAM-BIM/.evidence-pr223-224-corrections/revision2/pr224-*` (`-run1..3`, `GH-final`).
- The first full run on `4ee951c1` had 1 failure: `MullionTouchingTwoFramedWindows_...(angle: 0)` got no aperture
  (`revision2/pr224-SAM.Tests.txt`); it never failed in isolation (0 of 600 runs, `revision2/probe-mullion`). Cause:
  `PartFTransferAirDoorTests` temporarily replaces ActiveSetting's default aperture construction library with an
  empty one, and AperturesBySolids reads it when no constructions are given. `9a191d38` puts
  `AperturesBySolidsTests` in that xUnit collection, as `QuadraticScanRegressionTests` already is (test-only).
- Probe replays (copies of the reviewer's projects in `revision2/probe224`, `revision2/probe-base`, run from the
  SAM-BIM parent): this build (SAM.Analytical SHA256 `EF5ED1A5...`) gives parallel panes 2 apertures at 0 and 45
  degrees; nested chain one 4.03492 m2 aperture in both orders; ambiguous containers one 6 m2 aperture with all five
  sources in both orders; slim row two 2 m2 apertures at 0 and 45 degrees. Q4 (`507F9CA6...`, as in the review): slim
  row one 4 m2 aperture at both angles, ambiguous containers one 6 m2. Outputs `revision2/probe224-after-output.txt`,
  `revision2/probe-base-replay-output.txt`.
- Softhouse aperture-only replay (headless `pipe` aperture mode; no OCCT, no deployment): 101 apertures, the
  per-aperture list identical to the run before this revision (`revision2/softhouse-apertures-only.txt`). Hosting (96)
  was last measured at `890d535f` with OCCT and is not remeasured here.
- GH metadata audit (`param_contract.py` inventory mode with Rhino 8's Python 3.13, not `--check`: SAM has no approved
  baseline): by GUID 460 components on `81a781ab` and this branch; only `SAMAnalytical.CreateAperturesBySolids`
  differs, in `latestComponentVersion` 1.0.2 -> 1.1.0 and `description`. Extraction `partial` on base and branch
  alike, so this is not a compatibility PASS. Evidence `revision2/contract-*.json`, `revision2/contract-audit.txt`.
- `git diff --check` clean.
- Independent reviewer evidence (separate from the above): `.review-pr223-224-20261025/` and `revision2/`.

## Integration with SAM#223 (2026-10-25)

- SAM#223 merged into `sow/2026-Q4` as `0c9c5894` (parents `81a781ab`, `c46cb554`). `origin/sow/2026-Q4` was merged
  into this branch (merge commit, no rebase). The only conflict was the adjacent-line assertions in
  `Grasshopper/SAM.Core.Grasshopper.Tests/BySolidsDiagnosticDetailTests.cs`; resolved by keeping both:
  `CreatePanelsBySolids` 1.2.0 and `CreateAperturesBySolids` 1.1.0, with their comments. No other production,
  parameter, GUID, default, fixture or tolerance change.
- Local validation of the merged content (APPDATA redirected to a scratch profile; suites run one after the other):
  SAM.Tests **3785 / 0 / 0** (3783 + the two #223 tests); SAM.Core.Grasshopper.Tests **87 / 0 / 0** (a first attempt
  with USERPROFILE and LOCALAPPDATA also redirected hung at Rhino start-up and was aborted by the hang timeout; the
  rerun with only APPDATA redirected passed). `git diff --check` clean. Machine-local evidence:
  `SAM-BIM/.evidence-pr223-224-integration/` (`tests/full.trx`, `tests/gh.trx`). The hosted result is on the PR.

## Decisions and assumptions

- A unit needs geometric evidence in the outer member's plane frame; when the evidence is missing, mutual
  (equivalent members) or ambiguous (competing containers, a free member touching two keys), the members involved
  follow the unchanged proximity rule, so their result is what it was without units.
- The projected test uses the outer member's convex hull: a pane in a U-shaped door frame (open at the bottom) counts
  as enclosed; a concave outline is not required.

## Remaining limits

- Enclosure tests vertices against a convex hull within `maxGap`: a member with a re-entrant outline nested in a
  re-entrant frame could be accepted where a strict containment test would not. No such case is known.
- A free member touching two **explicit** groups joins one of them by input order; this is the behaviour of
  `sow/2026-Q4` and is not changed here (explicit keys stay authoritative and are never dissolved).
- Ambiguous sets fall back silently (no diagnostic), so the diagnostics detail behaviour is unchanged.
- Grasshopper behaviour validated by the headless GH test suite only; no interactive canvas or saved-definition
  round trip in this round.

## Next step

Owner-authorised guarded merge at the validated integration head. After merge: the
PROJECT_PROGRESS.md closeout on `sow/2026-Q4`. When both this PR and SAM#223 merge, the second one meets an
adjacent-line conflict in the version guard: keep both assertions (CreatePanelsBySolids 1.2.0, CreateAperturesBySolids
1.1.0) and test the merged content.
