# Triangle3D.Reverse no-op — Q4 (2026-10-27)

Status: draft PR into `sow/2026-Q4` (base `9d8961ec`), branch `fix/triangle3d-reverse-q4`. **Tested content `d7a00b6c`**;
the later commit changes this record and its evidence only, and the PR description holds the final head and hosted
checks. Not merged; no merge is authorised.

## Problem

`Triangle3D` keeps its vertices in a `Point3D[]`. `Reverse()` was `points.Reverse();`. With `using System.Linq;` in
scope that binds to `Enumerable.Reverse` (`netstandard2.0`, `LangVersion 12`), which returns a new sequence; the result
was discarded, so `Reverse()` never changed the triangle.

Two callers rely on it to correct a triangle's winding against its parent face normal:

| Caller | Use |
| --- | --- |
| `Query.Volume(Shell)` (`Geometry/Spatial/Query/Volume.cs:56`) | reverses a triangle whose normal is not in the face-normal half-space, then adds `SignedVolume()` |
| `Create.Mesh3D(Shell)` (`Geometry/Spatial/Create/Mesh3D.cs:173`) | same test; the triangles then feed `Mesh3D(IEnumerable<Triangle3D>)`, which indexes vertices through `this[i]` |

With the no-op, the shell volume depended on the raw winding of each triangulation, and the mesh triangles kept
whatever winding `Planar.Query.Triangulate` produced instead of the face's. No other caller of `Triangle3D.Reverse()`
exists in SAM or in the sibling repositories (SAM_OCCT, SAM_Building, SAM_Tas, SAM_Topologic, SAM_gbXML, SAM_Revit,
SAM_SolarCalculator, SAM_Systems, SAM_UI, SAM_IFC, SAM_Rhino_UI, SAM_Tas_Grasshopper; grep of `.cs`).

## Change

`SAM/SAM.Geometry/Geometry/Spatial/Classes/Triangle3D.cs`: `points.Reverse();` → `Array.Reverse(points);` (in place, the
same contract as the other `Reverse()` implementations: the stored vertex order is reversed). One line.

Unchanged: public signatures, the JSON schema (`Points` array, written in stored order), coordinates, tolerances, the
`silverSpacing` area filter in `Volume`, `OrientNormals`, the shell-normal convention, `Shell.Volume` itself, solver
behaviour, panel order, golden files.

`Triangle2D.Reverse()` has the identical defect (`Geometry/Planar/Classes/Triangle2D.cs:205`). It has **no callers** and
was left alone (scope), recorded under "Separate".

## Behaviour change (callers)

| Consumer | Effect |
| --- | --- |
| `Shell.Volume()` | a triangle wound against its face is now corrected; closed shells whose triangulation mixes windings no longer carry a signed per-triangle error. Closed convex or consistently triangulated shells are unchanged |
| `Create.Mesh3D(Shell)` | every mesh triangle now follows its parent face normal; a closed outward shell gives a positive signed mesh volume |
| `Analytical.Query.Volume(Space…)`, `SpaceVolumeFilter`, `Modify.Remove` (via `Shell.Volume()`) | results move only where the volume was wrong; see the Softhouse table |
| `Mesh3D(IEnumerable<Triangle3D>)`, `Query.Convert`, `Transform`, `Project`, `Offset`, `Face3Ds`, `TryGetSAMGeometries` | none: they never call `Reverse()` |

## Tests (`SAM/SAM.Tests/Triangle3DReverseTests.cs`, xUnit, AAA, SPDX, 132 cases)

- Contract (on 5 poses: identity, translation, rotation about Z, composed X·Y·Z rotation, rotation + translation):
  vertex order reversed; indexer reflects it; normal flips; non-zero `SignedVolume` changes sign; one reversal changes
  the triangle and two restore the sequence (`==` and `Equals`); area, length, centroid and bounds unchanged; segments
  reversed and closed; `Clone()` and JSON round-trip keep the reversed order; a copy made earlier is unaffected;
  `Triangle3DObject` reverses.
- `Shell.Volume()`: a concave L-shaped prism (analytic 6), a box (24) and a right-triangle prism (30), each on 5 poses
  with 5 face-winding masks (outward, all inward, three mixed masks).
- `Create.Mesh3D(Shell)`: for the L prism on 5 poses × 5 masks, every mesh triangle's unit normal agrees with the face
  that contains its centroid; an outward shell gives mesh signed volume +6 and a fully inward one −6.

## Validation (local, machine `michal`, 2026-10-27)

Build/test commands run from `SAM/SAM` with `APPDATA` and `USERPROFILE` redirected to a scratch profile
(`SAM` and `SAM_OCCT` Grasshopper post-build steps copy into `%APPDATA%\SAM`). The real
`%APPDATA%\SAM` was not used.

```
dotnet test SAM.Tests/SAM.Tests.csproj --filter "FullyQualifiedName~Triangle3DReverseTests"
```

| Check | Result |
| --- | --- |
| New tests on unchanged source (red; `points.Reverse()`, same build) | **76 fail / 56 pass** of 132. Failing: every contract test that needs a changed sequence (vertex order, indexer, normal, signed-volume sign, segments, JSON/clone, `Triangle3DObject`, double-reversal intermediate state), `Volume` of the concave prism (25 / 25), and all 35 `Mesh3D(Shell)` cases. Passing: the invariance rows (area, centroid, bounds, copy independence) and the `Volume` box and triangular-prism rows, whose triangulations happen to be consistently wound (kept as compatibility guards) |
| New tests on the fix (green) | **132 / 132** |
| SAM.Tests, full, on the fix | **3975 / 3975**, 0 skipped (`SAM.Geometry.dll` `EF2B5D4E`) |
| SAM.Core.Grasshopper.Tests (Debug; built with the scratch profile, run `--no-build` with the normal one), on the fix | **87 / 87** (loaded `SAM.Geometry.dll` `EF2B5D4E`; this project covers `TryGetSAMGeometries`, the only Grasshopper consumer of `Triangle3D` besides `GeometryTriangulate`) |
| Changed expectations | none: no existing test, golden or fixture changed |

The test poses compose `GetRotationX/Y/Z`. `Transform3D.GetRotation(axis, angle)` was **not** used: it is marked
"Method to be revised" and builds a non-orthonormal matrix (its `[0,2]` and `[2,0]` entries carry the same sign), so a
solid rotated with it is distorted (a 24 m3 box became 23.87). Existing, unrelated, not changed.

### Saved Softhouse R1 closeout models (read only; no solver rerun, no native rebuild)

Models: `SAM_daily\2026-10-25-Softthouse\handover\evidence\2026-10-27-sam-root-face2d-loop\chains\pr84-R\model.json`
(baseline) and `...\chains\pr84fix-R\model.json` (corrected SAM), 59 spaces each. A scratch probe
(`documentation/evidence/triangle3d-reverse-q4/r1probe.Program.cs.txt`; a `net8.0` console referencing
`SAM.Core/Geometry/Analytical`) loads each model and, per space, prints `ac.Shell(space).Volume()` and an
independent winding-free volume `|Σ area × n·p / 3|` over the faces after the same `OrientNormals` call. It was run
against the pre-fix `SAM.Geometry.dll` (`558D2B86`) and the fixed one (`EF2B5D4E`), both built from this branch.

| Cell | Model | `Shell.Volume()` before fix | after fix | independent |
| --- | --- | ---: | ---: | ---: |
| (12.159, 6.705, 4.975) | baseline | 31.242936 | 31.242936 | 31.242939 |
| (12.159, 6.705, 4.975) | corrected | **31.23669** | **31.242936** | 31.242939 |
| (12.853, 4.891, 7.963) | baseline | 36.172684 | 36.17268 | 36.17269 |
| (12.853, 4.891, 7.963) | corrected | **36.178925** | **36.17268** | 36.172689 |

- Before the fix the probe reproduces the saved defect exactly (31.23669 and 36.178925, the ±0.006 m3 of the R1
  closeout). After it, the two cells agree between the models and with the independent volume to ≤ 1e-5 m3.
- Over the 52 closed cells of each model, the largest `|Shell.Volume() − independent|` is 0.000021 (baseline, both
  builds), 0.006249 → 0.000022 (corrected: before → after). No other cell changed (a per-cell diff of the two builds
  lists exactly these two cells). 7 cells per model return NaN in every build (open shells, `IsClosed` is false): an
  existing, unrelated condition.
- Tables: `documentation/evidence/triangle3d-reverse-q4/volumes-<model>-<before|after>-fix.tsv`.

**This correction therefore fully explains the saved ±0.006 m3 volume defect.** No remaining mechanism was found, so the
"stop and report" condition did not occur. The OCCT panel-order sensitivity from the R1 closeout is separate and
unchanged.

## Compatibility

- Public API, JSON schema and serialised models: no change; `Triangle3D` JSON written before or after is identical for
  the same vertex order, and a reversed triangle serialises its reversed order.
- Callers: only the two above. Mesh output of `Create.Mesh3D(Shell)` changes only in triangle vertex order (and
  therefore normal) for triangles that were wound against their face; positions and counts are unchanged.
- Saved definitions: no component, parameter or GUID is touched.

## Decisions and assumptions

- `Array.Reverse(points)` keeps the field type and the existing contract (the other `Reverse()` methods reverse in place).
- `Shell.Volume`'s design is not changed; its per-triangle correction was the intent and now works.
- **Grasshopper versions: recommend no bump in this PR** (owner decision under GH-H2). No component code, parameter or
  GUID changes; the results that change are corrected volumes and mesh winding. GH-H2 lists a different geometry result
  as minor or major by impact, so the owner decides. Components whose output can change indirectly through
  `Space.Volume`: `SAMAnalyticalModifyVentilationProfileByFunction`, `SAMAnalyticalUpdateInfiltrationGains`, the
  space-volume filter and `Remove` paths. Deployed Grasshopper needs the new `SAM.Geometry.dll` (a coherent redeploy,
  `BuildAlls_v5.bat`); the shared GH version pins in SAM_OCCT#84 are a separate decision and are not changed here.

## Separate

- `Triangle2D.Reverse()` has the same `points.Reverse()` no-op; no callers; a one-line fix when wanted.
- `Transform3D.GetRotation(axis, angle)` is non-orthonormal (see above).
- OCCT/AdjacencyCluster panel-order sensitivity and open-shell NaN volumes: unchanged.

## Next step

Independent review of the PR; then mark ready for review (not a merge authorisation). After merge: a docs-only
`[skip ci]` closeout in `PROJECT_PROGRESS.md` on `sow/2026-Q4` with the merge SHA, and a coherent GH redeploy. Recommended
for review: Claude Opus 5.5, medium effort, a new session, Plan mode (read-only review of one line and its tests).
