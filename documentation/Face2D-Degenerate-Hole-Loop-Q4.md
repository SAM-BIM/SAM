# Face2D degenerate hole loop — Q4 (2026-10-27)

Status: draft PR into `sow/2026-Q4` (base `91f25290`), branch `fix/face2d-degenerate-hole-loop-q4`. **Tested content
`b852b687`**; later commits change this record only, and the PR description holds the final head and hosted checks.
Depends on the SAM_OCCT companion `test/coplanar-merge-guard-sam-independent-q4` (see Merge order). Not merged; no
merge is authorised.

## Problem

`Geometry.Spatial.Query.Union(IEnumerable<Face3D>)` → `Planar.Query.Union(IEnumerable<Face2D>)` → NTS union →
`Polygon.ToSAM` → `Face2D.Create(external, holes)`. With two or more holes, `Face2D.Create` unions the holes again
(`Planar.Query.Union(IEnumerable<Polygon2D>)` → `ToSAM_Polygon2Ds` → `LinearRing.ToSAM`).

- `LinearRing.ToSAM` rounds, drops consecutive points within `AlmostEquals`, checks `Count < 3`, and only then does the
  `Polygon2D` constructor drop the exact closing point. A ring collapsed to A, B, A passes the check and becomes the
  2-point loop [A, B].
- `ToSAM_Polygon2Ds` added every conversion result unchecked (a null as well).
- One 2-point (or null) hole makes the face invalid, and `Spatial.Query.Union` skips it: the whole merged face is lost,
  silently.

Observed (step-0 probe, seed-400 slab, raw `Spatial.Query.Union` on `91f25290`): the union returns only the detached
4 m2 piece. The merged 126 m2 polygon has two sliver holes; the second (NTS ring
`(-1.62500025, 2.25) (-1.6250003333, 1.749999) (0.875, 1.75) (-1.625, 1.75)`, area 1.25e-6 m2) enters `Face2D.Create`
as 4 points. The hole re-union strips its zero-width spike and leaves `(0.875, 1.75) (-1.625, 1.749999) (-1.625, 1.75)`;
`(-1.625, 1.75)` is within the tolerance of `(-1.625, 1.749999)`, so the loop converts to `[(0.875, 1.75),
(-1.625, 1.749999)]` and the 126 m2 face is dropped. SAM_OCCT#85 guards one downstream caller (Clean3D); every other
caller of the conversion still had the defect.

## Change (C1 + C2, `SAM/SAM.Geometry/Geometry/Planar/Convert/ToSAM/`)

- **C1** `Polygon2D.cs` `LinearRing.ToSAM`: count the points `Polygon2D` keeps (the exact closing point excluded, as its
  constructor does) and return null below 3. Applies to the tolerance and the tolerance 0 paths.
- **C2** `Polygon2Ds.cs` `ToSAM_Polygon2Ds`: leave null conversions out in both branches (the single-ring branch returns
  an empty list instead of `[null]`).

Unchanged: rounding, the `AlmostEquals` tolerance, public signatures, every ring with 3 or more distinct points, the
older `GetArea() < tolerance` filter in `Polygon.ToSAM`, `Face2D.Create` and `Spatial.Query.Union`. No approximate
closing-point trimming was added.

What C1 means, precisely: it rejects a loop that the existing conversion has already made degenerate. It does not prove
that the source ring enclosed no area (the seed-400 sliver had 1.25e-6 m2 before `AlmostEquals` merged two of its
points), and 3 retained points are not a general proof of loop validity. Nor does this change guarantee that every
physically genuine opening survives conversion: the existing filters still apply (an opening whose ring the conversion
collapses, or one below the `GetArea() < tolerance` filter, is still dropped). The tests show only that the tested
1 mm and thin-triangle openings are kept and that C1 + C2 remove no additional opening.

## Affected callers (behaviour changes only where the output was invalid or lost)

| Caller | Effect |
| --- | --- |
| `Face2D.Create` (2+ holes, via the hole re-union) | the fix: the face is valid with its other holes |
| `Spatial.Query.Union` and everything built on it (`Project`, Analytical `PanelsBySolids`/`PanelsBySolid`/`AperturesBySolids`, `AdjustRoofs`, `MergeCoplanar*`, `AdjacencyCluster`, `BuildingModel`, `Extend`; SAM_OCCT `SnapStage`; SAM_Building, SAM_SolarCalculator, SAM_gbXML) | a merged face that was dropped is now returned |
| `Polygon.ToSAM` (both overloads) | none (its area filter already rejected 2-point loops) |
| `Difference.cs:396` | was an invalid 2-point `Face2D`; now null, which it already skips |
| `Intersection.cs:378` | none (the `LineString` branch runs first) |
| `Offset.cs:373` | none (receives fewer entries, never a null) |
| `Snap(Polygon2D, Polygon2D)` (`Snap.cs:62`) | may return a null entry instead of a 2-point polygon; no callers in SAM-BIM repositories |

## Tests (`SAM/SAM.Tests`, xUnit, AAA, SPDX)

- `Face2DHoleLoopTests` (new): `LinearRing.ToSAM` collapsing rings (the observed ring and a rounding case) → null;
  tolerance 0 A, B, A → null; thin valid triangles (2 um, one step, tolerance 0) keep 3 points in ring order; a square
  from every start and direction keeps its points; `ToSAM_Polygon2Ds` omits a degenerate hole and returns empty for a
  degenerate shell; `Face2D.Create` with the observed two holes keeps the genuine hole whole; two 1 mm openings and a
  thin triangle all survive the re-union.
- `Face3DUnionTests` (extended): the seed-400 fixture (bit-exact from SAM_OCCT `SnapStageTests`) as listed, and 16
  re-presentations: s0 reversed, all reversed, loop starts 1-3, s0 last, list reversed, rotated 16.13 and 30 deg,
  translated (1000 m), stood up as a wall, tilted 30 deg, three 1e-9 m jitters, ULP z-noise on s0. Each is checked on
  whole boundaries: valid faces, loops of 3+ points, region equal to the pieces' own union outside a 2 um seam band,
  every returned loop on the seams, slab 126 m2 + detached 4 m2. Genuine openings through the union: a 1 mm opening, a
  union-formed 1 mm opening, two 1 mm openings in one face and a 2.5 m x 2 um triangle, each kept with its own vertices.
- Two frames (tilted, z-noise) leave the slab as its 45 + 81 m2 halves on base and fix alike (an unmerged seam, not a
  loss); those rows pin 3 faces.

## Validation (local, machine `michal`, 2026-10-27, tested content `b852b687`)

Isolated builds under `C:\Users\michal\AppData\Local\Temp\sroot` (machine-local): `SAM_red` = `91f25290` + the new
tests, `SAM_fix` = `b852b687`; built with `handover/tools-2026-10-27-combined/scripts/safebuild.sh` (APPDATA and
USERPROFILE redirected to `sroot\profile`). Evidence: `SAM_daily\2026-10-25-Softthouse\handover\evidence\2026-10-27-sam-root-face2d-loop\`
(machine-local, `README.md` has every command; copy it before switching machines).

| Check | Result |
| --- | --- |
| Step-0 gate (`probe gate`, raw `Spatial.Query.Union`, frozen SAM `A20CB3E8` vs only SAM.Geometry swapped) | base: 1 face (4 m2), 2-point loop observed in the hole re-union; C1+C2: 2 faces (126 + 4); s0-reversed control byte-identical boundaries |
| New tests on base source (`SAM_red`) | 15 / 37 fail: the 6 conversion defect tests, the listed case, jitter 1-3, starts 1-3, translated, wall; 22 preservation rows pass |
| New tests on the fix | **37 / 37** |
| Mutations (`mutation/`) | C1 reverted: 15 fail; C2 reverted: 12 fail (incl. seed-400 and 8 variants); "fill a loop with area <= tolerance": 7 fail (1 mm and thin-triangle rows) |
| SAM.Tests Release, full | base + new tests **3828 / 3843** (the 15 above, nothing else); fix **3843 / 3843** |
| SAM.Core.Grasshopper.Tests Release (built with the scratch profile, run `--no-build` with the normal profile) | base **87 / 87**, fix **87 / 87** (fix run loaded SAM.Geometry `188DA1F6`) |
| 2000-seed synthetic (exact + noisy = 4000 runs, `synth/`) | raw union: base 12 slabs lost, fix **0**; Q4 Clean3D guard: base 18 `CoplanarMergeRecovered` Warnings, fix **0**; raw-union boundaries bit-identical in the other 3988 runs; Clean3D slab areas identical in all 4000 |
| SAM_OCCT companion on both SAM versions | see the SAM_OCCT record `documentation/Coplanar-Merge-Guard-SAM-Independent-Q4.md` |
| Unchanged SAM_OCCT `sow/2026-Q4` unit tests on the fix | 1049 / 1052: exactly the three #85 rows fail, each only at the Warning assertion (the slab and sources hold): the reason for the companion |
| Softhouse chains, SAM_OCCT#84 `26ddbaf` no kink, frozen native `438089F8` | F: every stage SAME, tables identical (58 spaces, 48 good, 47 / 48). R: 59 spaces / 45 good / 44 of 48 on both (partial recovery, unchanged); current SAM needs 1 recovery Warning, the fix 0. Clean and extended panels are the same panels (see "R model differences" below); the OCCT model is **not** identical: of 347 paired panels 329 SAME, 16 differ by <= 1.3e-7 m, 5 internal two-sided panels are reversed (3 of them among the 16), and two cells report `Shell.Volume()` +/-0.006 m3; rooms, matched reference identities and good classification are identical apart from "Cell n" labels |
| Consumer builds vs the fix (frozen SAM + fixed SAM.Geometry) | SAM_SolarCalculator `277a3a4`, SAM_gbXML `43b5d01`, SAM_IFC `77046be`: build OK. SolarCalculator tests: base and fix both **564 passed / 1 skipped** (fix run loaded SAM.Geometry `188DA1F6`). SAM_Systems, SAM_Validation (SAM_Mollier sibling not provisioned), SAM_Building `0907ee2` (legacy RhinoCommon 6 packages.config) and SAM_Tas (COM references need .NET Framework MSBuild): **the same failures against the unmodified SAM**, so infrastructure, not this change |
| `%APPDATA%\SAM` | 2125 files byte-identical before and after (`provenance/appdata-sam-*.sha256`) |

## R model differences: cause and classification (review finding R1, evidence `closeout/`)

- **Same input panels, different order.** The saved corrected R extended panels pair one-to-one with the baseline's by
  complete loop correspondence: 78 / 78, every loop bit-equal from the same start vertex, panel JSON equal apart from
  Guids. Only the order differs: the 2F cap is index 3 (union order) instead of 77 (appended by the #85 guard).
- **Causal control.** The corrected panels written in the baseline order (a verified permutation, nothing else changed)
  and replayed through only the native stage (OCCT AdjacencyCluster, apertures, Rooms2, good; same frozen runtime and
  options) reproduce the **baseline model byte-for-byte** (Guid-masked JSON) and identical tables, on either SAM runtime.
  The two saved runs replay exactly as controls. So the model differences are caused by panel order alone: an existing
  OCCT/AdjacencyCluster presentation sensitivity, not by geometry from this change. No sorting was added.
- **What they are.** Every cell keeps the same panels (type, construction, area, centroid, adjacency, apertures). The 5
  reversed normals are internal panels between the same two cells, reversed symmetrically (equivalent two-sided
  interfaces). The +/-0.00625 m3 is a **volume-computation artefact**: the two cells share a 0.001447 m2 sliver triangle
  that is the same triangle with the same oriented normal in both runs, but with a different vertex winding, and
  `Shell.Volume()` integrates the triangulation, so its contribution flips sign (+/-0.003123 m3 each side). A
  winding-independent volume gives the same value in both runs (36.17269 / 36.172689 and 31.242939 m3), equal to the
  baseline's `Shell.Volume()`. Geometry and attribution are unchanged.
- Reproduce (Git Bash, E = the evidence folder): `sh $E/scripts/run.sh fix reorder $E/closeout/reorder A=<pr84-R
  panels_extended.json> B=<pr84fix-R panels_extended.json> OUT=<file>`, then `sh $E/scripts/closeout-replay.sh`, then
  probe modes `incidence`, `volumes`, `faces` (`probe/Closeout.cs`); details in `closeout/README.md`. Machine-local:
  needs `%TEMP%\sroot\occt` (frozen builds, native `438089F8`), the saved chain outputs and Rhino 8.

## Decisions and assumptions

- C1 + C2 passed the step-0 gate, so `Face2D.Create` and `Spatial.Query.Union` are unchanged (review scope).
- Tolerances, `AxisX`, the area-vs-length filter, solver, kink and extraction rules are untouched.
- The corrected SAM.Geometry was tested by swapping only that DLL into frozen builds, so every other loaded binary is
  the frozen one (hashes in the evidence `assemblies*.txt`).
- **Grasshopper versions: recommend no bump** (owner decision under GH-H2). No component code, parameter or GUID
  changes, and the results that change were invalid or lost before (a defect correction, not a new contract).
  Precedent: SAM#222 (`Union.cs`) bumped nothing. That a component version cannot gate shared library behaviour is
  context, not an exemption from GH-H2, and this recommendation is not a rule for future library changes. GH-H2 lists
  "a different geometry result" as minor or major by impact and the reviewing owner, so the owner decides; components
  whose output can change indirectly: `SAMAnalyticalPanelsIntersection`, `GeometryModifyUnion`, `CreatePanelsBySolids`,
  `CreateAperturesBySolids`, and SAM_OCCT `Clean3D`/`Extend3D`/`Solve3D`.

## Merge order

1. SAM_OCCT companion PR (`test/coplanar-merge-guard-sam-independent-q4`) merges first: SAM_OCCT CI builds against the
   newest SAM `sow/*`, and the unchanged #85 tests fail on corrected SAM.
2. Then this PR (hosted CI rerun at its final head).
3. Rerun SAM_OCCT `sow/2026-Q4` CI and check in its log that SAM's merged `sow/2026-Q4` was used.
4. Each repository's `PROJECT_PROGRESS.md` closeout after its merge. #85's guard stays as defence and diagnostic.
5. SAM_OCCT#84 takes the companion when it next merges `sow/2026-Q4`.

## Unresolved / separate

- The older `Polygon.ToSAM` hole filter compares an area with a length (`GetArea() < tolerance`): it drops genuine
  openings below 1e-6 m2 and lets long slivers through. Separate task; not changed here.
- No general invalid-union fallback in `Spatial.Query.Union` (a non-null invalid merged face is still skipped). Another
  mechanism that invalidates a merged face would still lose it outside SnapStage. Separate safeguard; testing
  difficulty is not evidence it is unneeded.
- Frame-dependent merge completeness: in two frames the union leaves a 1 um seam open (45 + 81 m2). Not a loss;
  related to the `AxisX` exact-zero frame selection noted in the checklist.
- OCCT/AdjacencyCluster output depends on panel order (shown above); belongs to the #84 presentation-independent
  free-edge measurement design task. No sorting added, no expectation rebaselined.
- SAM `Shell.Volume()` integrates each oriented face's triangulation, so a sliver whose vertex winding contradicts its
  normal gives a signed error (here +/-0.006 m3). Existing; separate defect.
- Full Softhouse acceptance is not claimed: R still retains 44 / 48 (extraction differences, #84 blockers); the gate is
  the same 48 reference identities passing on F and R.

## Next step

Independent re-review of this closeout (finding R1) and the final heads; then the PRs can be marked ready for review
(not a merge authorisation). Merge the SAM_OCCT companion first, then this PR, only on owner approval. Recommended for the merge/closeout session: Claude Opus 5.5, medium effort, this session, Build mode (the
evidence and context are here).
