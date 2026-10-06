# PartFData thread-safe space use cache (Q4) - SAM

Branch `fix/partfdata-thread-safe-cache-q4` -> base `sow/2026-Q4` (cut from `fe1d60b5`). Record date: 2026-10-07.

## Current status

PR SAM-BIM/SAM#181 open, **not merged**. One product file and one new test file. No change to classification behaviour,
the test collections, `.gitmodules`, gitlinks, workflows, `master`, `sow/2026-Q3`, icon PRs, runtime-URL
work or release/installer files.

## Defect

`PartFData.GetPartFCategory(SpaceUse)` built its `SpaceUse -> PartFCategory` map lazily and **published it
before filling it**:

```csharp
if (dictionary_SpaceUse is null)
{
    dictionary_SpaceUse = [];              // published empty
    foreach (...) dictionary_SpaceUse[...] = ...;   // then filled in place
}
return dictionary_SpaceUse.TryGetValue(...);
```

One `PartFData` is shared by every caller of `ActiveSetting` (`Query.DefaultPartFData()` /
`DefaultPartFCalculator()`), and xUnit runs test classes in parallel. A second thread that arrived after
the assignment but before the loop ended saw a non-null map and skipped the build, then either:

- read a half-filled map, getting `null` for a space use not yet added (a wet room classified as nothing,
  so sized with no extract); or
- read it while the first thread was writing, which .NET's `Dictionary` detects as
  `InvalidOperationException: Operations that change non-concurrent collections must have exclusive access`.

Observed once in a full parallel run (1/2819, `PartODwellingStrategyMaterialisationTests.SystemsScope_ScaffoldingIsTheAddMechanicalSystemsShape`
via `PartOIterationPreparationTests.Model` -> `PartFCalculator.Classify`). That class reads the default Part F
data but is not in the `"SAM.Analytical.ActiveSetting default Part F data"` xUnit collection that serialises
the other nine readers; that collection's own remarks already record the "wet room intermittently sized no
extract" symptom, which is this race.

## Fix

`SAM/SAM.Analytical/Classes/PartF/PartFData.cs` only: double-checked locking with build-then-publish.

- The map is built in a local under a private lock and assigned to the (now `volatile`) field only once
  complete. It is never modified after publication, so concurrent reads are safe without the lock.
- At most one build per instance; a thread that waited on the lock re-reads the field and uses the
  published map.
- **First category wins is unchanged:** the loop body (skip null / `Undefined`, `ContainsKey` guard, then
  add) is byte-for-byte the same and runs single-threaded over the same `PartFCategories.Values`
  enumeration, so the first category per space use in enumeration order still wins. Only the target
  variable changed (a local instead of the field).
- Rejected: `ConcurrentDictionary` / `TryAdd` (would still expose a partially-filled map and change the
  publication semantics); `Lazy<T>` (equivalent here, but a field initialiser capturing `this` and a larger
  diff). Snapshot semantics are as before: the map reflects `PartFCategories` at first use and is not
  rebuilt if the categories are replaced later.

## Regression test

`SAM/SAM.Tests/PartFDataConcurrencyTests.cs` (new, 2 tests):

- `GetPartFCategory_FirstCategoryWins_SingleThreaded` - first of several `Bedroom` categories wins;
  `Kitchen` / `Bathroom` map; `Storage` (no category) and `Undefined` give `null`; a category with no
  SpaceUse is not reachable through this lookup.
- `GetPartFCategory_ConcurrentFirstUse_NoExceptionAndSameCategoriesAsSingleThreaded` - 20 rounds, each on a
  **fresh** `PartFData` so every round hits the lazy build. `max(4, ProcessorCount)` dedicated threads are
  released together by a `Barrier` (no sleeps) and each asserts, by reference, the single-threaded answer
  for `Bathroom, Bedroom, Kitchen, Storage, Undefined`; exceptions are collected and asserted empty. The
  rule set puts 20 000 losing duplicate `Bedroom` categories before `Bathroom`, which keeps the build in
  progress long enough that released threads reliably reach it mid-build.
- Proven against the old code: with the product change reverted locally, the concurrent test failed 3/3
  runs in round 0 (`Bathroom: expected 'Bathroom', got 'null'` from many threads). With the fix: 5/5 green.

## Validation (local, APPDATA/USERPROFILE redirected to a scratch folder, NUGET_PACKAGES pinned)

- Focused `PartFDataConcurrencyTests`: 2/2 passed, 5 consecutive runs.
- Full `dotnet test SAM/SAM.Tests/SAM.Tests.csproj` (Debug, with build): **2821/2821** passed
  (2819 existing + 2 new).
- CI-equivalent Release: `dotnet build` + `dotnet test --no-build -c Release`: **2821/2821** passed.
- `msbuild SAM.sln /t:Restore` then `/t:Rebuild /m:1 /nr:false /p:Configuration=Release
  /p:UseSharedCompilation=false`: 0 errors. 110 pre-existing warnings (101 CS8632, 2 CS8073, 2 CS0661,
  2 CS0659, 2 CS0108, 1 CS0162); none in the changed files. The Grasshopper post-build copy wrote into the
  redirected APPDATA, not the real SAM install.
- PR CI (`build`, `test`, `spdx`): reported on SAM-BIM/SAM#181.

## Unresolved issues, risks

- **Sibling lazy field, not changed (out of scope):** `GetLegacyPartFCategory` has the same
  publish-before-fill pattern on `textMap_Legacy` (assigned, then `Add`ed into). It is reached from
  `GetPartFCategory(Space, out ...)` when the SpaceUse lookup returns null. Same fix shape applies; owner to
  decide whether to take it as a follow-up PR. `spaceSemanticsResolver ??= CreateResolver()` builds into a
  local first, so at worst two equivalent resolvers are built (benign).
- The `"SAM.Analytical.ActiveSetting default Part F data"` test collection is left as is; it also
  serialises other shared state and removing it is not part of this fix.

## Next step

Review and merge into `sow/2026-Q4` (maintainer's call), then add the `PROJECT_PROGRESS.md` closeout on the
base branch with the merge SHA. Optional follow-up: the same fix for `textMap_Legacy`.
