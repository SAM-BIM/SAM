# PanelsBySolid: a face pair's material sampled at its deepest point — Q4 (2026-10-25)

Status: branch `fix/panels-by-solid-ownership-sample-q4` on `sow/2026-Q4` at `81a781ab` (after the merges of
SAM-BIM/SAM#220 `626a23e5`, #222 `924fec52` and #221 `945eea51`). PR SAM-BIM/SAM#223 into `sow/2026-Q4`. Independent
review 2026-10-25 of head `c9633bea`: core correction accepted; REVISE for R3 (component version), done. Tested content
`b8afc8c7` (core change `755bcef9`; first tested as `5750cd24` on `457defbb`); later commits change this record only,
and the PR description holds the final head and hosted checks. Step 1 of the Softhouse "SAM model from solids" part 2.

## Problem

`PanelsBySolid` -> `ElementPlanes` decides which element owns material crossed by several face pairs ("ownership",
most panel-like plane first). It tested each pair's material at one point: the internal point of the largest overlap
piece (`Face2D.GetInternalPoint3D`). For a frame-shaped overlap that point can fall anywhere in the frame.

Softhouse solid #385 (x -7.625..-7.175, y -2.527..-0.683, z 0.05..3.4) is a 0.45 m wall that is mostly a 1.8 x 2.87 m
opening, leaving jambs of 13.4 mm and 30.8 mm, a 0.479 m head and a 1 mm sill. The internal point of its frame-shaped
0.45 m overlap fell in a jamb; the 13 mm jamb pair (below minThickness, a claimant) is the most panel-like plane, so it
"owned" that point and the 0.45 m plane lost its only pair before it was judged. The solid then gave no plane to the
PanelsBySolids fragment pass (SAM#221), and Dining 1 (house D) leaked at (-7.5, -2.5, z 2).

## Change

`SAM/SAM.Analytical/Create/PanelsBySolid.cs`: the material sample of a pair is now the point of its (largest) overlap
farthest from the overlap's edges: the centre of its largest inscribed circle (NetTopologySuite
`MaximumInscribedCircle`, accuracy `tolerance_Snap`), falling back to the old internal point. It is used for both the
material test (`HasMaterialBetween`) and ownership (`ElementPlane.Owns`). In a frame the deepest point is in the head,
not in a jamb; a real cross-cut (the wall under a window sill) still lies inside the element that holds it. XML docs of
`ElementPlanes` updated. No public API change.

`SAMAnalytical.CreatePanelsBySolids` calls `PanelsBySolid`, so its geometry result changes: **1.1.0 -> 1.2.0** (minor,
GH_COMPONENT_STANDARDS GH-F2/GH-H2; saved definitions at 1.1.0 now get the update indication). GUID, parameter names,
order, types, access, optionality, visibility and defaults unchanged. The version guard pins 1.2.0.

## Review (independent review of `c9633bea`)

- The bounded core correction is accepted: the reviewer's serialized #385 fixture gives the expected 0.45 m wall
  (1.01207 m2) on this branch and no wall on `81a781ab`. The material sample remains a heuristic (one deepest point per
  overlap), not a whole-volume proof.
- **R3 (P2): changed GH geometry was not versioned.** The earlier record said no GH bump was needed because no
  component code changed; that was wrong, the component's computed result changes. Fixed by the 1.2.0 bump above.
- **Inherited limits, not changed here:** in the reviewer's replay, translated, rotated and face-reversed variants of
  the #385 wall and rotated slab controls fail on `81a781ab` and on this branch alike (identical outputs in
  `.review-pr223-224-20261025/probe-base-output.txt` and `probe223-output.txt`: rotated by 90 or 137 degrees, or with
  reversed faces at 0 degrees, no wall; at 13 or 45 degrees an extra 5.166 m2 wall over the opening). Their cause is in the
  existing method chain and was not located; this PR claims no rotational or face-order robustness and no full room
  recovery. Door-notch controls pass the sampled transformations on both.

## Files changed

- `SAM/SAM.Analytical/Create/PanelsBySolid.cs`
- `Grasshopper/SAM.Analytical.Grasshopper/Component/SAMAnalyticalCreatePanelsBySolids.cs` (version 1.2.0)
- `SAM/SAM.Tests/PanelsBySolidTests.cs`: fixture `WallAroundLargeOpening` (#385 face by face, as converted from Rhino),
  `PanelsBySolid_WallAroundLargeOpening_ThinJambDoesNotRemoveTheWallPlane` (fails before: no wall panel),
  `PanelsBySolid_SillUnderWindowCrossCut_IsStillPartOfTheWall` (guard: cross-cuts still removed at ratio 100)
- `Grasshopper/SAM.Core.Grasshopper.Tests/BySolidsDiagnosticDetailTests.cs` (version pin 1.2.0)
- this record

## Validation (local, machine `michal`; tested content `b8afc8c7` on base `81a781ab`; core content `755bcef9` as before)

```powershell
# deploy-safe builds: APPDATA and USERPROFILE redirected to a scratch profile (the GH PostBuild copies DLLs to
# %APPDATA%\SAM); SolutionDir set for the PostBuild xcopy. Run the two test projects one after the other: two
# in-process Rhino starts at once fail with COMException in OneTimeSetUp.
dotnet build Grasshopper/SAM.Core.Grasshopper.Tests/SAM.Core.Grasshopper.Tests.csproj -c Release -p:SolutionDir=<repo>\
dotnet build SAM/SAM.Tests/SAM.Tests.csproj -c Release
dotnet test SAM/SAM.Tests/SAM.Tests.csproj -c Release --no-build
dotnet test Grasshopper/SAM.Core.Grasshopper.Tests/SAM.Core.Grasshopper.Tests.csproj -c Release --no-build
```

- SAM.Tests **3763 passed, 0 failed, 0 skipped**; SAM.Core.Grasshopper.Tests **87 / 0 / 0**. The #385 wall test (failed before the core
  change: no wall panel) and the sill cross-cut guard pass. TRX and logs: `SAM-BIM/.evidence-pr223-224-corrections/pr223-*`
  (machine-local). Before the review: 3763 / 3763 and 87 / 87 at `755bcef9`; 3740 / 3740 on `457defbb`.
- GH metadata audit (not `--check`; SAM has no approved baseline): `design/grasshopper-icons/tools/param_contract.py`
  inventory mode, run with Rhino 8's Python 3.13, manifests written outside the repository for a full export of
  `81a781ab` and for this branch, compared by component GUID: 460 components each, none added or removed; only
  `SAMAnalytical.CreatePanelsBySolids` (`8b0f5f2c-...`) differs, in `latestComponentVersion` 1.1.0 -> 1.2.0. Its
  extraction status is `partial` on the base and this branch alike (constructor defaults of `GooPanelParam` are outside
  the validator's source subset). Evidence `contract-*.json`, `contract-audit.txt`. `git diff --check` clean.
- Softhouse (headless `pipe`, inputs `solids_panels_392.3dm` + `windows_201.3dm`, minThickness 0.04, Clean3D/Extend3D;
  local test-only branch `local/softhouse-integration-q4-2` = `81a781ab` + this branch + the AperturesBySolids units
  branch): #385 is kept ("Created 1 panel, 0.45 m thick. Filled 1 hole for apertures."; on `457defbb` + SAM#221 it was
  the difference between 25 and 26 skipped solids); the ray probe from Dining 1 no longer leaks (0.1 % -> 0 %). Spaces
  unchanged: 70 (GF 27 / 1F 22 / 2F 21). OCCT assemblies in that run: the SAM_OCCT#83 build before its R2 correction.
- SAM_OCCT, run before the rebase (`SAM_OCCT-wt-clean3d-degenerate` at SAM_OCCT#83's pre-R2 head, with the SAM DLLs
  of a local build of `457defbb` + SAM#220/#221 (pre-R1)/#222 + this branch + the units branch swapped into its test output):
  unit 1038 / 1038; integration 469 passed, 25 failed, 2 skipped: the same 25 failures as recorded before this change
  (`SAM-BIM/.evidence-softhouse-20261025/occt-integration-final-non-passing.txt`). Machine-local evidence:
  `SAM-BIM/.evidence-softhouse-20261025-part2/`.

## Decisions and assumptions

- Generic fix in the sampling, not a special rule ("a thin claimant may not remove a much thicker plane"): the sample
  should represent the pair's material; the deepest point does, for any outline.
- Separate PR (SAM#221 merged first; this is the single-solid ownership step and stands alone on `sow/2026-Q4`).

## Unresolved: Dining 1 is still not a space (second cause)

The fix removes the #385 leak, but Dining 1 stays open through the corner at the north end of the west bay
(x -7.4..-6.64, y about -0.7..-0.2). There the source models the 0.45 m wall turning east as three thin north-south
slices (#386 0.175 m, #384 0.076 m, #391 0.234 m, y -0.683..-0.233), and #389 (a 0.45 m wall at x -6.865..-6.415 with a
window) gives a 0.074 m panel from its jamb (y -0.2325..-0.158, above minThickness) instead of its 0.45 m wall.
Mid-surfaces of the slices leave a slot open to the outside. Proof: adding two panels that close that corner
(x -7.4..-6.982 at y -0.195, and x -7.4 from y -0.24 to -0.195) gives 71 spaces, GF 28, and 44 hosted apertures
(evidence `SAM_daily/2026-10-25-Softthouse/handover/evidence/2026-10-25-part2/corner_closed_experiment.txt`).
Generic options (owner decision): (a) in ownership, a thin pair whose material lies inside a thicker non-parallel
element, which does not lie inside it, is a cross-cut (fixes #389's jamb-as-wall); (b) in PanelsBySolids, a pair whose
two faces are glued to neighbouring solids is a cut between pieces of one element, not its sides (re-reads the
slices as one 0.45 m east-west wall). Both affect every model.

## Next step

Independent review of the corrected head and its hosted checks; owner merge decision. After merge: the
PROJECT_PROGRESS.md closeout on `sow/2026-Q4`. When both this PR and SAM#224 merge, the second one meets an
adjacent-line conflict in the version guard: keep both assertions (CreatePanelsBySolids 1.2.0, CreateAperturesBySolids
1.1.0). Separately: the owner's choice of (a) or (b) for the Dining 1 corner (a new task and branch).
