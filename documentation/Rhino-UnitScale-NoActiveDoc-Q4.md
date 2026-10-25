# Rhino converters without an active document: UnitScale NaN fallback — Q4 (2026-10-25)

Status: implemented locally on `fix/rhino-unitscale-no-doc-q4` (base `sow/2026-Q4` at `457defbb`). Not pushed; no PR yet
(owner go-ahead required). Step A of the Softhouse "SAM model from solids" task.

## Problem

`SAM.Geometry.Rhino.Query.UnitScale()` returned `NaN` when `RhinoDoc.ActiveDoc` is null (Rhino.Inside, Compute/Hops,
tests). Every Rhino converter multiplies its tolerance by it. In `Convert.ToSAM(Brep)` the face test becomes
`IsPlanar(NaN)`, which is false for `SumSurface` (extrusion) faces, so those faces go down the mesh path and return
distorted, non-closed shells. Interactive Rhino (a metre document is active) is not affected.

Measured on the Softhouse solids (`SAM_daily/2026-10-25-Softthouse/handover/inputs/solids_panels_392.3dm`, faces are
`SumSurface` + `NurbsSurface`):

| solid | no doc (before) | metre doc / no doc (after) |
| --- | --- | --- |
| #181 | open, areas 6.98, 1.17, 0.79, 1.24 | closed, vol 2.4435, areas 6.98, 1.17, 0.73 |
| #2 | open, vol NaN | closed, vol 0.1118 |
| #18 | open, vol NaN | closed, vol 0.0919 |

## Fix

`UnitScale()` (no argument) returns 1 when there is no active document: SAM geometry is metres. `UnitScale(RhinoDoc)`
is unchanged (still NaN for an explicit null). Call sites covered: `Convert/ToSAM/IGeometry3Ds.cs`,
`Convert/ToRhino/Brep.cs` (3), `Create/Shell.cs`, and the preview caches of `GooPanel`, `GooAperture`,
`GooAdjacencyCluster` (they only compare the value).

## Files changed

- `Rhino/SAM.Geometry.Rhino/Query/UnitScale.cs`
- `Grasshopper/SAM.Core.Grasshopper.Tests/UnitScaleNoActiveDocTests.cs` (new, NUnit + Rhino.Testing like the rest of
  the project)
- this record

## Validation (local, headless Rhino 8 via Rhino.Testing; not run by CI)

```powershell
dotnet build Grasshopper/SAM.Core.Grasshopper.Tests/SAM.Core.Grasshopper.Tests.csproj -c Release -p:SolutionDir=<repo>\
dotnet test  Grasshopper/SAM.Core.Grasshopper.Tests/SAM.Core.Grasshopper.Tests.csproj -c Release --no-build
```

- New tests, with the fix: 2 / 2 pass. With `UnitScale.cs` reverted: both fail (`NaN` scale; face area
  0.72940002629756862 instead of 0.7294 through the mesh path).
- Whole project: 86 / 86 pass.
- Caution: the SAM Grasshopper projects' PostBuild copies `build\*.dll` into `%APPDATA%\SAM`. Build worktrees with
  `APPDATA`/`USERPROFILE` redirected to a scratch folder so the deployed SAM is not replaced.

## Risks

- A caller that relied on NaN to detect "no document" through the no-argument overload would now get 1. None found in
  SAM-BIM repositories (`SAM_Systems` uses `RhinoMath.UnitScale` on an explicit document).

## Next step

Owner review; then push the branch and open the PR into `sow/2026-Q4`.
