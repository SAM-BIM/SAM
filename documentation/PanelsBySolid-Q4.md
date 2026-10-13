# Solid → mid-surface panels with unit warning — Q4 (2026-10-13)

Status: implemented on `feature/panels-by-solid-q4`; draft PR, owner review required. Not merged.
Base: `sow/2026-Q4` at `69f999cf97cf424609f2c4eece6d8a214605bccf`.
PR: the live draft PR for this branch in `SAM-BIM/SAM`; its description holds the final head and hosted checks.
Related:
- SAM-BIM/SAM_OCCT#76 (under-split guard in `SAMOCCT.CreateAdjacencyCluster`). It is independent, but the house result below needs both.
- The SAM_OCCT Air-panel / attic PR, which consumes the Air panels created here.

## Why

A Rhino house of closed solids was converted by a GH C# script. The script:

- reconstructed outlines as convex hulls, which lost the stair void, notches and holes;
- picked thickness faces by largest area only;
- used NURBS control points as outline samples;
- snapped against infinite planes inside inflated bounding boxes;
- took slider "mm" values in model units.

The Rhino → SAM conversion does not scale units. SAM is in metres, so a millimetre model silently produced millimetre analytical geometry.

## Change

- `Create.PanelsBySolid(Shell, …)` (pure SAM, metres):
  - The thickness pair is the opposite parallel faces with the **largest projected overlap**, so unrelated parallel ends are ignored.
  - The mid-surface is the **true section** (`Shell.Section`), keeping notches, holes and disconnected regions. Void faces are dropped by a point-in-material test.
  - Holes and notches claimed by aperture geometry (windows, doors) are filled; aperture footprints are tested inside the thickness band. Unclaimed holes stay open and get a separate **Air** panel. Unclaimed notches stay as the real outline.
  - Thin elements (`minThickness` 0.08 m) and non-panel-like elements (`maxThicknessRatio` 0.2) are skipped with a reason.
  - Every panel is stamped with `PanelSourceParameter` (SourceName, SourceId, SourceLayer, SourceThickness, SourceRole = "Opening" on Air).
- `Query.SnapToJunctions(panels, snapDistance)`: moves outer edges in-plane onto neighbouring mid-planes.
  - A move counts only where the target's actual face is within reach (a **local junction**), not just its infinite plane.
  - Extension goes to the farthest target ahead (wall past a ceiling T-junction up to the roof, which closes an attic). With nothing ahead, it trims to the nearest target behind.
  - Holes are untouched; Air panels are not moved and are not targets. Same Guids and parameters.
- `Geometry.Rhino.Query.ModelUnitsWarning()`: detects a non-metre Rhino document and explains the fix. It never converts.
- New GH `SAMAnalytical.CreatePanelsBySolids` (GUID `8b0f5f2c-…4d10`):
  - **refuses with an Error** in non-metre documents;
  - inputs: solids, aperture geometry, minThickness, snap, airForOpenings;
  - outputs: panels, airPanels, excluded, log;
  - takes name, id and layer from referenced Rhino objects.
- `SAMAnalytical.CreatePanelBy3DGeometry`: adds a **Warning** for non-metre documents. Its behaviour is otherwise unchanged.
- The global `Rhino → SAM` conversion is **not** changed.

## Validation (local, this machine)

```powershell
dotnet test SAM/SAM.Tests/SAM.Tests.csproj -c Debug
dotnet build Grasshopper/SAM.Analytical.Grasshopper/SAM.Analytical.Grasshopper.csproj -c Debug
```

- SAM.Tests: **3471 passed / 0 failed**, including `PanelsBySolidTests` **14 / 14**: wall, slab stair void → Air, aperture-claimed hole, Air disabled, door notch filled/kept, gable, thickness pair, stair tread and balustrade skipped, L-corner snap, distant parallel plane ignored, ceiling → roof extension, zero snap.
- GH project: compiles with 0 errors. The post-build deploy step fails on `xcopy … resources / Samples` ("File not found"), which is environmental; the copy of DLLs to `%APPDATA%\SAM` does run.
- End-to-end on the real house (machine-local Rhino 8 harness reading `Untitled2.3dm` scaled to metres, not committed):
  - 23 elements → 23 panels + **1 Air panel** (3.325 m², exactly the stair void). The stairs and balustrades are skipped. All window holes and door notches are claimed and filled.
  - After snap (0.3 m), `SAMOCCT.CreateAdjacencyCluster` (rebuild, no sew) gives **11 spaces**.
  - Solve3D after the snap over-splits the attic (13 cells), so it is not part of this workflow.
- Not run: hosted CI; manual Rhino/Grasshopper test.

## Remaining / next

- Owner review.
- Air identity through `CreateAdjacencyCluster` and the unconditioned attic belong to the SAM_OCCT follow-up.
- A robust unit-conversion architecture is out of scope (owner decision).
