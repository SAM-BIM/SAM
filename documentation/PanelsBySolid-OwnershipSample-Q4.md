# PanelsBySolid: a face pair's material sampled at its deepest point — Q4 (2026-10-25)

Status: implemented locally on `fix/panels-by-solid-ownership-sample-q4` (base `sow/2026-Q4` at `457defbb`). Not pushed;
no PR yet (owner go-ahead needed, and the owner decides whether this is a separate PR or joins SAM-BIM/SAM#221).
Tested content `5750cd24`; later commits change this record only. Step 1 of the Softhouse "SAM model from solids" part 2.

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
`ElementPlanes` updated. No API, parameter or component change (no GH version bump).

## Files changed

- `SAM/SAM.Analytical/Create/PanelsBySolid.cs`
- `SAM/SAM.Tests/PanelsBySolidTests.cs`: fixture `WallAroundLargeOpening` (#385 face by face, as converted from Rhino),
  `PanelsBySolid_WallAroundLargeOpening_ThinJambDoesNotRemoveTheWallPlane` (fails before: no wall panel),
  `PanelsBySolid_SillUnderWindowCrossCut_IsStillPartOfTheWall` (guard: cross-cuts still removed at ratio 100)
- this record

## Validation (local, machine `michal`; tested content `5750cd24`)

```powershell
# deploy-safe build: APPDATA/USERPROFILE redirected (the GH PostBuild copies to %APPDATA%\SAM)
dotnet build SAM/SAM.Tests/SAM.Tests.csproj -c Release
dotnet test SAM/SAM.Tests/SAM.Tests.csproj -c Release --no-build
```

- SAM.Tests on this branch: 3740 / 3740 passed (sow/2026-Q4 3738 + 2 new). The new #385 test failed before the change.
- Local integration branch `local/softhouse-integration-q4` (with SAM#220/#221/#222 and this branch, never pushed):
  SAM.Tests 3756 / 3756.
- Softhouse (headless `pipe`, inputs `solids_panels_392.3dm` + `windows_201.3dm`, minThickness 0.04, Clean3D/Extend3D):
  #385 is now kept (PanelsBySolids pieces 55 -> 56, skipped solids 26 -> 25: "Created 1 panel, 0.45 m thick. Filled 1
  hole for apertures."); the ray probe from Dining 1 no longer leaks (0.1 % -> 0 %). Space count unchanged (70; GF 27).
- SAM_OCCT (`SAM_OCCT-wt-clean3d-degenerate`, SAM DLLs of the integration build swapped into the test output):
  unit 1038 / 1038; integration 469 passed, 25 failed, 2 skipped: the same 25 failures as recorded before this change
  (`SAM-BIM/.evidence-softhouse-20261025/occt-integration-final-non-passing.txt`). Machine-local evidence:
  `SAM-BIM/.evidence-softhouse-20261025-part2/`.

## Decisions and assumptions

- Generic fix in the sampling, not a special rule ("a thin claimant may not remove a much thicker plane"): the sample
  should represent the pair's material; the deepest point does, for any outline.
- Kept separate from SAM#221 (that PR is the cross-solid fragment pass; this is the single-solid ownership step, and it
  stands alone on `sow/2026-Q4`). Owner to confirm: separate PR, or cherry-pick into SAM#221.

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

Owner: review; decide separate PR vs SAM#221, and whether to pursue (a)/(b) for the Dining 1 corner. On go-ahead:
push the branch and open the PR into `sow/2026-Q4` with this record.
