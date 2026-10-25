# AperturesBySolids: framed windows in a row stay separate apertures — Q4 (2026-10-25)

Status: branch `fix/apertures-by-solids-units-q4` on `sow/2026-Q4` at `81a781ab` (after the merges of SAM-BIM/SAM#220,
#222 and #221 `945eea51`). PR SAM-BIM/SAM#224 into `sow/2026-Q4`. Independent review 2026-10-25 of head `d108e6df`:
REVISE (R1, R2 below); corrected and retested. Tested content `890d535f` (earlier `d17d9b84`, before the review);
later commits change this record only, and the PR description holds the final head and hosted checks. Step 2 of the
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
1.91 m2, pane 1.63 m2), the source frame width. Not hosted: the internal glazed screen (#0, y -0.69, no reference
opening) and the 3 Dining 1 apertures plus the #385 door (no Dining 1 space).

## Change

`SAM/SAM.Analytical/Create/AperturesBySolids.cs`, before proximity joining (explicit group keys first, as before):

1. **Enclosure** (`ApertureMember.Encloses`). Member J encloses member I when: I's world bounding box lies within J's
   grown by `maxGap`, and not the other way round (candidate prefilter only); both are thinnest across parallel planes
   (`tolerance_Angle`); every vertex of I lies within J's depth along J's plane and, projected on that plane, inside the
   convex hull of J; each within `maxGap`. The thinnest plane is the one `AperturesByAssembly` uses for an assembly,
   now shared as `ThinnestPlane`.
2. **Units, order-free.** Enclosure is evaluated for every pair of ungrouped members first. A member joins a unit only
   when exactly one *outermost* member (one that encloses something and is enclosed by nothing) encloses it; that
   member and the outermost one get the same group key, as if they were in a Rhino group. A member enclosed by no
   outermost member (a non-transitive chain: A holds B and B holds C within `maxGap`, A does not hold C) or by several
   gets no unit key and joins by proximity exactly as before units existed. Keys are written once, never overwritten.
3. Units that only touch (a row of framed windows sharing jambs) stay separate apertures. Members neither around nor
   inside another (stick-frame bars) and ungrouped members next to an explicit group behave as before. The unit key
   carries a leading NUL so it cannot clash with a user key.

`SAMAnalytical.CreateAperturesBySolids` 1.0.2 -> **1.1.0** (minor: the geometry result changes); description says
framed windows in a row stay separate. GUID, parameter names, order, types, access, optionality, visibility and
defaults unchanged (contract audit below). Version guard
`BySolidsDiagnosticDetailTests.ComponentVersions_AreBumped_AndTheOutputsAreStillNamedLog` pins 1.1.0.

## Review corrections (independent review of `d108e6df`)

- **R1 (P1): a world bounding box could not establish a unit.** Two separate parallel 0.1 m panes 0.9 m apart became
  one 12 m2 aperture 1.1 m deep after a 45 degree rotation (the small pane fell inside the large one's world box).
  Fixed by item 1. Regressions: `AperturesBySolids_SeparateParallelPanes_StaySeparateUnderRigidMotion` and
  `AperturesBySolids_SeparateParallelFramedWindows_StaySeparateUnderRigidMotion` (0 and 45 degrees, rotation plus
  translation; areas, SourceThickness, source membership and each aperture's mid-plane checked), plus
  `AperturesBySolids_RowOfTouchingFramedWindows_SameUnitsUnderRigidMotion`.
- **R2 (P2): unit keys were overwritten and partitions depended on input order.** The reviewer's chain A/B/C gave
  `{A}{B,C}` or `{C}{B,A}` (overlapping apertures) depending on order. Fixed by item 2: all orders now give one
  4.03492 m2 aperture `{A,B,C}`, as `sow/2026-Q4` does. Regressions:
  `AperturesBySolids_ToleranceChainOfNestedMembers_SameApertureForEveryInputOrder` (6 orders),
  `AperturesBySolids_NestedSashAndStickFrames_SameAperturesForEveryInputOrder` (3 orders),
  `AperturesBySolids_ExplicitGroupKeyFrame_KeepsItsGlassAndStaysApartFromAFramedUnit` (24 orders).
- Fail before / pass after: on `d108e6df` exactly the three R1/R2 cases failed (panes 45 degrees, framed 45 degrees,
  tolerance chain), 24 aperture tests passed; after: 27 / 27 (`.evidence-pr223-224-corrections/pr224-fail-before-d108e6df.txt`).
- The reviewer's probe (`.review-pr223-224-20261025/probe224`, replayed from a copy in the evidence folder against
  this build): parallel panes 0 and 45 degrees -> 2 apertures, 12 and 0.8 m2, depth 0.1 m each; nested chain in both
  orders -> 1 aperture 4.03492 m2 `{outer, middle, inner}`.

## Files changed

- `SAM/SAM.Analytical/Create/AperturesBySolids.cs`
- `Grasshopper/SAM.Analytical.Grasshopper/Component/SAMAnalyticalCreateAperturesBySolids.cs`
- `SAM/SAM.Tests/AperturesBySolidsTests.cs`: the two first tests (row of framed windows, sash in frame) and the review
  regressions above; the existing `TouchingWindowsWithoutGroupKeys_MergeIntoOneAssembly`, stick-frame, explicit-group
  and corner-window tests are unchanged and pass
- `Grasshopper/SAM.Core.Grasshopper.Tests/BySolidsDiagnosticDetailTests.cs` (version pin 1.1.0)
- this record

## Validation (local, machine `michal`; tested content `890d535f` on base `81a781ab`)

```powershell
# deploy-safe builds: APPDATA and USERPROFILE redirected to a scratch profile (the GH PostBuild copies DLLs to
# %APPDATA%\SAM); SolutionDir set for the PostBuild xcopy. Run the two test projects one after the other: two
# in-process Rhino starts at once fail with COMException in OneTimeSetUp.
dotnet build Grasshopper/SAM.Core.Grasshopper.Tests/SAM.Core.Grasshopper.Tests.csproj -c Release -p:SolutionDir=<repo>\
dotnet build SAM/SAM.Tests/SAM.Tests.csproj -c Release
dotnet test SAM/SAM.Tests/SAM.Tests.csproj -c Release --no-build
dotnet test Grasshopper/SAM.Core.Grasshopper.Tests/SAM.Core.Grasshopper.Tests.csproj -c Release --no-build
```

- SAM.Tests **3772 passed, 0 failed, 0 skipped** (3763 + 9 new cases); SAM.Core.Grasshopper.Tests **87 / 0 / 0**.
  Build 0 errors. TRX and logs: `SAM-BIM/.evidence-pr223-224-corrections/pr224-*` (machine-local).
- GH metadata audit (not `--check`; SAM has no approved baseline): `design/grasshopper-icons/tools/param_contract.py`
  inventory mode, run with Rhino 8's Python 3.13, manifests written outside the repository for a full export of
  `81a781ab` and for this branch, compared by component GUID: 460 components each, none added or removed; only
  `SAMAnalytical.CreateAperturesBySolids` (`b115fc0d-...`) differs, in `latestComponentVersion` 1.0.2 -> 1.1.0 and
  `description`. Its extraction status is `partial` on the base and on this branch alike (constructor defaults of
  the Goo parameter types are outside the validator's source subset). Evidence `contract-*.json`, `contract-audit.txt`.
- Softhouse (headless `pipe` with this branch's SAM build; OCCT assemblies of an earlier SAM_OCCT build): 101
  apertures, 96 hosted, 70 spaces; the per-aperture list (storey, facade, centre, size, area, pane, sources) is
  identical to the run before the review corrections.
- `git diff --check` clean.

## Decisions and assumptions

- A unit needs geometric evidence (plane, depth, projected hull), not only a world bounding box; when evidence is
  missing or ambiguous the unchanged proximity rule applies, so the result is never worse than without units.
- The projected test uses the outer member's convex hull: a pane in a U-shaped door frame (open at the bottom) still
  counts as enclosed; a concave outline is not required.
- A frame without its glass touching a framed unit still joins that unit by proximity (unchanged rule for members
  outside units).

## Remaining limits

- Enclosure is tested on vertices against a convex hull within `maxGap`; a member with a re-entrant outline nested in
  a re-entrant frame could be accepted where a strict containment test would not. No such case is known.
- Grasshopper behaviour validated by the headless GH test suite only; no interactive canvas or saved-definition
  round trip in this round.

## Next step

Independent review of the corrected head and its hosted checks; owner merge decision. After merge: the
PROJECT_PROGRESS.md closeout on `sow/2026-Q4`. When both this PR and SAM#223 merge, the second one meets an
adjacent-line conflict in the version guard: keep both assertions (CreatePanelsBySolids 1.2.0, CreateAperturesBySolids 1.1.0).
