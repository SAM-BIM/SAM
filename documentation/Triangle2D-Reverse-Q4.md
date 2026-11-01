# Triangle2D.Reverse no-op — Q4 (2026-11-01)

Status: local branch `fix/triangle2d-reverse-q4` (base `sow/2026-Q4` `15d8c909`), pilot LOOP-1. No PR yet. The tested
SHA is recorded by the controller test step and added to this record in a record-only commit; nothing is merged.

## Problem

`Triangle2D` keeps its vertices in a `Point2D[]`. `Reverse()` was `points.Reverse();`. With `using System.Linq;` in
scope that binds to `Enumerable.Reverse`, which returns a new sequence; the result was discarded, so `Reverse()` never
changed the triangle. It is the same defect SAM#228 fixed in `Triangle3D`.

## Callers (correction to the earlier record)

The SAM#228 record says `Triangle2D.Reverse()` has no callers. That is incomplete. `Face.Reverse()`
(`SAM/SAM.Geometry/Geometry/Base/Classes/Face.cs:274`) calls `IReversible.Reverse()` on its external edge (`:277`) and
on each internal edge (`:283`). `Triangle2D` implements `IReversible`, so a `Face` whose edge is a `Triangle2D` reaches
this method. A text search found no such `Face` caller in this repository, but that was not semantic analysis, so the
reviewer should treat the route as live. Before the fix such a `Face.Reverse()` silently left a triangle edge unchanged.

## Change

`SAM/SAM.Geometry/Geometry/Planar/Classes/Triangle2D.cs`: `points.Reverse();` → `Array.Reverse(points);` (in place; the
stored vertex order is reversed). One line. Unchanged: public signatures, JSON schema, coordinates, tolerances.

## Tests (`SAM/SAM.Tests/Triangle2DReverseTests.cs`, xUnit, SPDX header)

- `Reverse_ReversesStoredVertexOrder`
- `Reverse_FlipsOrientation` (CounterClockwise → Clockwise)
- `Reverse_ThroughIReversible_ReversesStoredVertexOrder` (the route `Face.Reverse()` uses)
- `Reverse_Twice_RestoresOriginalOrder`
- `Reverse_KeepsAreaAndPerimeter` (6 and 12 for the 3-4-5 triangle)

## Validation (developer-local, informational; machine `michal`, 2026-11-01)

| Check | Result |
| --- | --- |
| `dotnet build SAM/SAM.Tests/SAM.Tests.csproj -m:1 -nodeReuse:false -p:UseSharedCompilation=false -p:NuGetAudit=false` | succeeded, 0 errors |
| `dotnet test ... --no-build -m:1 --filter <the two PartOBaselineReferenceTests exclusions>` (Debug) | 3978 passed, 0 failed, 0 skipped |

The run is **filtered, not the full suite**: it excludes `SAM.Tests.PartOBaselineReferenceTests.ARenamedDesign_IsFoundByIdentity`
and `SAM.Tests.PartOBaselineReferenceTests.ARuntimeHint_FindsADesignTheLocatorCannot_AndIsNotPersisted` (owner direction 29;
they fail under MXC profile G on `File.Move` access denied). The fail-first control, the controller test step, local
`samctl spdx` and the Release CI job log are not part of this developer run. `SAM.Core.Grasshopper.Tests` was not run
(needs Rhino). The developer TRX is informational; the tested SHA is set only by the controller test step.

## Compatibility

No public API, JSON, tolerance, Grasshopper component, version, `PROJECT_PROGRESS.md` or CI change. Behaviour changes
only for callers that reverse a `Triangle2D`, which now gets the reversed vertex order the method always promised.

## Next step

Lead: run the controller test step, fail-first control and `samctl spdx`; review; then the record-only commit with the
tested SHA, push and Draft PR if the owner allows. Release CI must show a `Passed` line for both excluded tests and all
five new tests before merge.
