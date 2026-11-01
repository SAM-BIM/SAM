# Triangle2D.Reverse no-op — Q4 (2026-11-01)

Status: open PR SAM#229 from `fix/triangle2d-reverse-q4` into `sow/2026-Q4` (base `15d8c9097360ffd2580ef3b82239e70c3b646c94`),
pilot LOOP-1. Marked Ready for review on GitHub on 2026-11-01 at 09:15 UTC under `michaldengusiak`. Not merged and not
approved. Tested content SHA: `5d608857004b3ad30b6d2f9e6546af7f53999f0b`, which adds one test to the reviewed content
`914be22451da79dcc9b0aedf0f02e70496781245`. This record-only commit follows it and changes only this file. The final
head and GitHub checks are in the PR description.

This delivery does not close F2, the tamper promise, teardown, continuity, S-09 or Phase 1a. Those are items of the
agent-workflow pilot, unrelated to `Triangle2D`, tracked in `tasks/phase1a/HANDOVER.md` on the `SAM_Workspace` branch
`phase1a/build`.

## Problem

`Triangle2D` keeps its vertices in a `Point2D[]`. `Reverse()` was `points.Reverse();`. With `using System.Linq;` in
scope that binds to `Enumerable.Reverse`, which returns a new sequence; the result was discarded, so `Reverse()` never
changed the triangle. It is the same defect SAM#228 fixed in `Triangle3D`.

## Callers (correction to the earlier record)

The SAM#228 record says `Triangle2D.Reverse()` has no callers. That is incomplete. `Face.Reverse()`
(`SAM/SAM.Geometry/Geometry/Base/Classes/Face.cs:274`) calls `IReversible.Reverse()` on its external edge (`:277`) and
on each internal edge (`:283`). `Triangle2D` implements `IReversible`, so a `Face` whose edge is a `Triangle2D` reaches
this method. Such a `Face` is easy to build: the public implicit `Face2D(Triangle2D)` operator
(`SAM/SAM.Geometry/Geometry/Planar/Classes/Face2D.cs:29`) makes one, and `Face(IClosed2D)` keeps a `Triangle2D` clone.
A text search of this repository found no caller of `Face.Reverse()`, and sibling repositories were not searched. Before
the fix such a `Face.Reverse()` silently left a triangle edge unchanged.

## Change

`SAM/SAM.Geometry/Geometry/Planar/Classes/Triangle2D.cs`: `points.Reverse();` → `Array.Reverse(points);` (in place; the
stored vertex order is reversed). One line. Unchanged: public signatures, JSON schema, coordinates, tolerances.

## Tests (`SAM/SAM.Tests/Triangle2DReverseTests.cs`, xUnit, SPDX header)

- `Reverse_ReversesStoredVertexOrder`
- `Reverse_FlipsOrientation` (CounterClockwise → Clockwise)
- `Reverse_ThroughIReversible_ReversesStoredVertexOrder` (the route `Face.Reverse()` uses)
- `Reverse_ThroughFace2D_ReversesTriangleExternalEdge` (a real `Face2D` built from a `Triangle2D`)
- `Reverse_Twice_RestoresOriginalOrder`
- `Reverse_KeepsAreaAndPerimeter` (6 and 12 for the 3-4-5 triangle)

## Validation at `5d608857` (local, 2026-11-01)

Developer run on Windows, Debug, outside MXC, after the `Face2D` test was added following the owner's PR review:

| Check | Result |
| --- | --- |
| `dotnet test SAM/SAM.Tests/SAM.Tests.csproj -p:NuGetAudit=false` | **Full suite, no filter:** 3981 passed, 0 failed, 0 skipped |
| Fail-first control: `dotnet test SAM/SAM.Tests/SAM.Tests.csproj -p:NuGetAudit=false --filter "FullyQualifiedName~Triangle2DReverseTests"` with `Array.Reverse(points);` temporarily put back to `points.Reverse();` | 4 of 6 failed: order, orientation, `IReversible` and `Face2D`. The twice-reversed and area/perimeter tests passed, as expected of a no-op |

The controller test step, the Codex review and the acceptance gate were not repeated at `5d608857`; their results below
apply to `914be224`, which has the same production code.

## Validation at `914be224` (local, 2026-11-01; GitHub checks are in the PR description)

| Check | Where | Result |
| --- | --- | --- |
| `dotnet build SAM/SAM.Tests/SAM.Tests.csproj -m:1 -nodeReuse:false -p:UseSharedCompilation=false -p:NuGetAudit=false` | Controller test step, isolated checkout, MXC profile G | exit 0, 20.5 s |
| `dotnet test SAM/SAM.Tests/SAM.Tests.csproj --no-build -m:1 --filter "FullyQualifiedName!=SAM.Tests.PartOBaselineReferenceTests.ARenamedDesign_IsFoundByIdentity&FullyQualifiedName!=SAM.Tests.PartOBaselineReferenceTests.ARuntimeHint_FindsADesignTheLocatorCannot_AndIsNotPersisted"` (Debug) | Same | **Filtered run:** 3978 passed, 0 failed, 0 skipped, 196.6 s |
| The two excluded tests, by name (Debug) | Scratch clone, outside MXC, extra check | Both passed |
| Fail-first control: the new tests with the unfixed `Triangle2D.cs` | Scratch clone, outside MXC | `Reverse_ReversesStoredVertexOrder`, `Reverse_FlipsOrientation` and `Reverse_ThroughIReversible_ReversesStoredVertexOrder` failed. `Reverse_Twice_RestoresOriginalOrder` and `Reverse_KeepsAreaAndPerimeter` passed, as expected of a no-op |
| `samctl spdx` | Local | Pass, 2 files |
| Developer run, same commands | Developer worktree | 3978 passed, 0 failed, 0 skipped (informational) |
| Independent review, Codex `gpt-6.1-sol` High, MXC profile G, at the tested SHA | Local | ACCEPT, round 1 of 3, no findings |
| Controller acceptance gate | Local | Passed |

The controller run is **filtered, not the full suite**: it excludes exactly `SAM.Tests.PartOBaselineReferenceTests.ARenamedDesign_IsFoundByIdentity`
and `SAM.Tests.PartOBaselineReferenceTests.ARuntimeHint_FindsADesignTheLocatorCannot_AndIsNotPersisted` (owner direction 29,
pilot LOOP-1 only: these two tests alone may be filtered from the controller run in this repository, and they must still
pass by name in Release CI; they fail under MXC profile G on `File.Move` access denied and pass outside it).

Machine-local evidence, not in this repository: `tasks/phase1a/evidence/loop-1/` on the `SAM_Workspace` branch
`phase1a/build`, which is local only.

## Unresolved

- Release CI at the final head must show a `Passed` line for both excluded tests and all six new tests; the result is
  in the PR description. A green build alone is insufficient.
- `SAM.Core.Grasshopper.Tests` was not run (needs Rhino).
- The controller test step runs Debug only. Release is covered by CI.
- The `Face2D` test covers the external edge only; the internal-edge branch (`Face.cs:283`) has no test.

## Compatibility

No public API, JSON, tolerance, Grasshopper component, version, `PROJECT_PROGRESS.md` or CI change. Behaviour changes
only for callers that reverse a `Triangle2D`, which now gets the reversed vertex order the method always promised.

## Next step

Owner: review the final head and give the exact approval line; merging needs it. After merge, add the `PROJECT_PROGRESS.md` closeout with the merge SHA.
