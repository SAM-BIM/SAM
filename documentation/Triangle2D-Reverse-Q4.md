# Triangle2D.Reverse no-op — Q4 (2026-11-01)

Status: Draft PR from `fix/triangle2d-reverse-q4` into `sow/2026-Q4` (base `15d8c9097360ffd2580ef3b82239e70c3b646c94`),
pilot LOOP-1. Not merged. Tested content SHA: `914be22451da79dcc9b0aedf0f02e70496781245`. This record-only commit follows
it and changes only this file. The PR number, final head and GitHub checks are in the PR description.

This delivery does not close F2, the tamper promise, teardown, continuity, S-09 or Phase 1a.

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

## Validation at `914be224` (local, 2026-11-01; GitHub checks are in the PR description)

| Check | Where | Result |
| --- | --- | --- |
| `dotnet build SAM/SAM.Tests/SAM.Tests.csproj -m:1 -nodeReuse:false -p:UseSharedCompilation=false -p:NuGetAudit=false` | Controller test step, isolated checkout, MXC profile G | exit 0, 20.5 s |
| `dotnet test SAM/SAM.Tests/SAM.Tests.csproj --no-build -m:1 --filter <fixed filter>` (Debug) | Same | **Filtered run:** 3978 passed, 0 failed, 0 skipped, 196.6 s |
| The two excluded tests, by name (Debug) | Scratch clone, outside MXC, extra check | Both passed |
| Fail-first control: the new tests with the unfixed `Triangle2D.cs` | Scratch clone, outside MXC | `Reverse_ReversesStoredVertexOrder`, `Reverse_FlipsOrientation` and `Reverse_ThroughIReversible_ReversesStoredVertexOrder` failed. `Reverse_Twice_RestoresOriginalOrder` and `Reverse_KeepsAreaAndPerimeter` passed, as expected of a no-op |
| `samctl spdx` | Local | Pass, 2 files |
| Developer run, same commands | Developer worktree | 3978 passed, 0 failed, 0 skipped (informational) |
| Independent review, Codex `gpt-6.1-sol` High, MXC profile G, at the tested SHA | Local | ACCEPT, round 1 of 3, no findings |
| Controller acceptance gate | Local | Passed |

The controller run is **filtered, not the full suite**: it excludes exactly `SAM.Tests.PartOBaselineReferenceTests.ARenamedDesign_IsFoundByIdentity`
and `SAM.Tests.PartOBaselineReferenceTests.ARuntimeHint_FindsADesignTheLocatorCannot_AndIsNotPersisted` (owner direction 29;
they fail under MXC profile G on `File.Move` access denied and pass outside it). The tested SHA is set only by the
controller test step.

Machine-local evidence, not in this repository: `tasks/phase1a/evidence/loop-1/` on the `SAM_Workspace` branch
`phase1a/build`, which is local only.

## Unresolved

- Release CI must still show a `Passed` line for both excluded tests and all five new tests. A green build alone is
  insufficient.
- `SAM.Core.Grasshopper.Tests` was not run (needs Rhino).
- The controller test step runs Debug only. Release is covered by CI.
- The `IReversible` test covers the interface route; no test builds a `Face` with a `Triangle2D` edge.

## Compatibility

No public API, JSON, tolerance, Grasshopper component, version, `PROJECT_PROGRESS.md` or CI change. Behaviour changes
only for callers that reverse a `Triangle2D`, which now gets the reversed vertex order the method always promised.

## Next step

Lead: verify CI at the final head, including the named Release tests, then stop for the owner's exact approval line.
Marking the PR Ready and merging need that line. After merge, add the `PROJECT_PROGRESS.md` closeout with the merge SHA.
