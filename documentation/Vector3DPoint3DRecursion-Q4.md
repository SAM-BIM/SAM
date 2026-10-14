# Vector3D → Point3D implicit conversion recursion — Q4 (2026-10-14)

Status: fixed on `fix/vector3d-point3d-recursion-q4`; draft PR, owner review required. Not merged.
Base: `sow/2026-Q4` at `02a51568c72704797396714e364e722c109e054c`.
PR: the live draft PR for this branch in `SAM-BIM/SAM`; its description holds the final head and hosted checks.
Found while validating SAM-BIM/SAM#198; kept out of that PR at the owner's request.

## Defect

`SAM.Geometry.Spatial.Vector3D` declares `public static implicit operator Point3D(Vector3D vector3D)`.

- The operator returned `new Vector3D(...)`.
- The C# compiler converts that return value back to `Point3D` with the same operator, so the operator calls itself until the stack overflows.
- A stack overflow cannot be caught, so the host process terminates.
- The crash is triggered by any implicit vector-to-point conversion, e.g. passing a `Vector3D` where a `Point3D` parameter is expected.
- It was observed as `Stack overflow ... Vector3D.op_Implicit` (with `op_Equality` from the null check on top of the stack) in a test harness.

## Change

- The operator now returns `new Point3D(x, y, z)`. One line.
- Every other `implicit` and `explicit` operator in `SAM.Geometry` was checked. None converts to its own type:
  - `Point3D` → `Vector3D` returns a `Vector3D`;
  - `Vector2D` → `Point2D` returns a `Point2D`;
  - the face, polygon, polyline and line operators build their target type.

## Files

- `SAM/SAM.Geometry/Geometry/Spatial/Classes/Vector3D.cs`
- `SAM/SAM.Tests/VectorPointConversionTests.cs` (new)
- This record.

## Validation (local, this machine)

```powershell
dotnet test SAM/SAM.Tests/SAM.Tests.csproj -c Debug
```

Build with `APPDATA` and `USERPROFILE` redirected (and `NUGET_PACKAGES` set to the real cache). Otherwise the Analytical Grasshopper post-build step deploys to `%APPDATA%\SAM`.

- New tests:
  - `Vector3D` → `Point3D` keeps coordinates and returns a `Point3D`;
  - null stays null;
  - `Point3D` → `Vector3D` → `Point3D` round trip;
  - `Vector2D` → `Point2D`.
- Result: 4 / 4 pass.
- Mutation check: with the previous operator the test run aborts ("Test host process crashed: Stack overflow").
- Full suite: 3495 passed, 0 failed.

## Risks / next

- Behaviour change: none for working code. Every previous call of this conversion crashed the process, so no caller could have relied on it.
- Next: owner review; merge after green hosted checks; post-merge `PROJECT_PROGRESS.md` closeout.
