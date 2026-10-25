# Face3D Union with a degenerate face — Q4 (2026-10-25)

Status: implemented locally on `fix/spatial-union-degenerate-face-q4` (base `sow/2026-Q4` at `457defbb`). Not pushed; no
PR yet (owner go-ahead required). Found during the Softhouse "SAM model from solids" task.

## Problem

`Geometry.Spatial.Query.Union(IEnumerable<Face3D>, tolerance)` groups faces plane by plane. A face joins the group of
the current seed only when its `InternalPoint3D(tolerance)` exists. A valid face without an internal point (a face
collapsed to ~1e-14 m wide) as the seed left its own group empty, and the method returned `null` for the whole set.

Effect downstream: `SAMOCCT.Clean3D` (SnapStage stage 3) falls back to the unmerged input when the union is null, so
one collapsed panel cancelled every coplanar merge of the model. On the Softhouse panels (BySolids, minThickness 0.04,
with the PanelsBySolids pass) Clean3D returned 391 -> 391 panels and OCCT rejected the build; with this fix 387 -> 98
and OCCT built 88 spaces.

## Fix

The seed always belongs to its own group (it is trivially coplanar with itself); the internal-point test applies to
the other faces only. A degenerate seed is returned unchanged; the other faces merge as before.

## Files changed

- `SAM/SAM.Geometry/Geometry/Spatial/Query/Union.cs`
- `SAM/SAM.Tests/Face3DUnionTests.cs` (new; the degenerate face uses the exact coordinates of a collapsed Softhouse
  wall face, on its explicit plane — rebuilt from points the plane refit makes it invalid)
- this record

## Validation (local)

```powershell
dotnet build SAM/SAM.Tests/SAM.Tests.csproj -c Release
dotnet test  SAM/SAM.Tests/SAM.Tests.csproj -c Release --no-build
```

- `Face3DUnionTests`: 2 / 2 with the fix; with `Union.cs` reverted the degenerate case fails (`null`).
- `SAM.Tests`: 3740 / 3740.
- Build worktrees with `APPDATA`/`USERPROFILE` redirected: the SAM Grasshopper projects' PostBuild deploys to
  `%APPDATA%\SAM`.

## Related

The collapsed faces came from `Query.SnapToJunctions`; that source is fixed separately on
`fix/panels-by-solids-continuation-q4` (collapsed panels are dropped with Warning `SNP002`). This fix keeps the union
robust for any other producer of such faces.

## Next step

Owner review; then push and open the PR into `sow/2026-Q4`.
