# PartFData thread-safe classification caches (Q4) - SAM

Branch `fix/partfdata-thread-safe-cache-q4` -> base `sow/2026-Q4` (cut from `fe1d60b5`). Record date: 2026-10-07.

## Current status

PR SAM-BIM/SAM#181 open, **not merged**. All three unsynchronised shared collections on the Part F room
classification path are fixed: the two lazy caches in `PartFData` (`dictionary_SpaceUse`, `textMap_Legacy`)
and the per-space cache of the `SpaceSemanticsResolver` that `PartFData` holds. Two product files and one
new test file. No change to classification results, the test collections, `.gitmodules`, gitlinks,
workflows, `master`, `sow/2026-Q3`, icon PRs, runtime-URL work or release/installer files.

## Shared state

One `PartFData` is shared by every caller of `ActiveSetting` (`Query.DefaultPartFData()` /
`DefaultPartFCalculator()`), and xUnit runs test classes in parallel, so `PartFCalculator.Classify` ->
`PartFData.GetPartFCategory(Space, out SpaceSemantics)` runs on several threads at once. That call touches:

1. `SpaceSemanticsResolver.Resolve(space)` on the resolver the `PartFData` holds;
2. `GetPartFCategory(SpaceUse)` -> `dictionary_SpaceUse`;
3. where that gives null, `GetLegacyPartFCategory(name)` -> `textMap_Legacy`.

## Defects

### 1. `dictionary_SpaceUse` (observed failure)

Built lazily and **published before it was filled** (`dictionary_SpaceUse = [];` then
`dictionary_SpaceUse[...] = ...` through the field). A second thread could see the non-null field, skip the
build and read a half-filled map (`null` for a wet room, so no extract), or two builders could write into the
same dictionary. The one failure seen in a full parallel run (1/2819,
`PartODwellingStrategyMaterialisationTests.SystemsScope_ScaffoldingIsTheAddMechanicalSystemsShape`) threw
`InvalidOperationException: Operations that change non-concurrent collections must have exclusive access`
from `Dictionary.TryInsert` <- `set_Item` <- `PartFData.GetPartFCategory(SpaceUse)` line 323, the field write
(confirmed from the captured test log). That class reads the default Part F data but is outside the
`"SAM.Analytical.ActiveSetting default Part F data"` xUnit collection, whose remarks already describe the
"wet room intermittently sized no extract" symptom.

### 2. `textMap_Legacy` (same pattern)

Declared `private TextMap textMap_Legacy`; created only in `GetLegacyPartFCategory`, which assigned
`textMap_Legacy = Core.Create.TextMap("PartFLegacy")` (internal dictionary still null) and then called
`textMap_Legacy.Add(...)` per legacy category **through the field**; the lookup is
`textMap_Legacy.SemanticBestTextMapKey(name)`, which enumerates `TextMap.Keys` (the internal `Dictionary`)
and reads each key's values. Concurrently that gives:

- partial results: an empty or half-filled map returns null for a legacy room that has a category;
- wrong category: the matcher picks the longest phrase and returns null on a top-rank tie, so a half-filled
  map can return a shorter, generic match, or one side of a tie instead of null;
- `InvalidOperationException: Collection was modified` (enumeration during `Add`), or corruption when two
  builders `Add` into the same TextMap after one overwrote the field.

### 3. `SpaceSemanticsResolver` cache (same path, different class)

`Resolve` reads and then writes two plain dictionaries (`cache`, `cacheKey`, keyed by space Guid) on every
call. `PartFData` holds one resolver, so concurrent classification inserts into them from many threads:
reproduced at roughly 440 exceptions per 20-round run, including the exact message above and
`IndexOutOfRangeException` from the corrupted dictionary. Outside `PartFData.cs`; included at the owner's
decision because it is the same defect on the same call path. Everything else in the resolver (`textMap`,
`keys`) is set in the constructor and only read afterwards.

## Fix

- `PartFData.cs`, both lazy caches, same shape: double-checked locking, build-then-publish. Built in a local
  under a private lock (`lock_SpaceUse`, `lock_Legacy`), assigned to the now `volatile` field only once
  complete, never modified afterwards, so reads need no lock. One build per instance.
- The build loops are unchanged apart from writing to the local, so semantics are identical:
  `dictionary_SpaceUse` - skip null/`Undefined`, `ContainsKey` guard, first category per space use in
  `PartFCategories.Values` order wins; `textMap_Legacy` - only categories with a name and `SpaceUse.Undefined`,
  synonyms or else the name, keyed by category name; lookup still longest phrase wins, tie gives null, then
  `PartFCategories.TryGetValue(key)`. Both remain snapshots taken at first use, as before.
- `SpaceSemanticsResolver.cs`: the cache read and the cache write in `Resolve` are each under a private
  `lock_Cache`; `ResolveCore` stays outside the lock (it only reads the Space and the TextMap). The cache
  pair is now always updated together. Two threads resolving the same space compute the same result, so the
  last store changes nothing; results are unchanged.
- Rejected: `ConcurrentDictionary` (would still publish a partial map for 1/2, and would not keep `cache` and
  `cacheKey` in step for 3); `Lazy<T>` (equivalent for 1/2, larger diff).

## Regression tests

`SAM/SAM.Tests/PartFDataConcurrencyTests.cs` (new, 5 tests). Concurrent tests share `RunConcurrently`:
`max(4, ProcessorCount)` dedicated threads released together by a `Barrier` (no sleeps), exceptions and
mismatches collected and asserted empty, 1-minute join guard. Every round uses a **fresh** `PartFData`.

- `GetPartFCategory_FirstCategoryWins_SingleThreaded` and `..._ConcurrentFirstUse_...` - 20 000 losing
  duplicate `Bedroom` categories before `Bathroom`; asserts by reference first-wins `Bedroom`, `Kitchen`,
  `Bathroom`, null for `Storage`/`Undefined`.
- `GetPartFCategory_Legacy_SingleThreaded` and `GetPartFCategory_Legacy_ConcurrentFirstUse_...` - invented
  names (no shared vocabulary match) with 5 000 filler categories between the generic/first-tied categories
  and the deciding ones: `Quax Zorb 1` -> `Quax Zorb` (not generic `Zorb`), `Vell` -> null (tie), `Mirk 2` ->
  `Mirk` (no synonyms, matched by name), `Zorb`, `Nothing Here` -> null. The spaces are resolved once through
  the resolver before the threads start, so the legacy build is the only first-time work they race on.
- `GetPartFCategory_Space_ConcurrentResolve_SharedResolverCacheStaysCorrect` - each thread classifies 200
  shared and 200 own new spaces per round through `GetPartFCategory(Space, ...)` on a small rule set; asserts
  each space gets its own category and SpaceUse.

Evidence against the unfixed code (product file reverted locally, test unchanged):

- `dictionary_SpaceUse`: 3/3 failed in round 0 (`Bathroom: expected 'Bathroom', got 'null'`).
- `textMap_Legacy` (dictionary fix kept): 3/3 failed in round 0 with `Collection was modified`; with the
  mismatch assert ordered first, 5/5 showed `'Quax Zorb 1': expected 'Quax Zorb', got 'null'` (half-built
  map). The wrong-category variant was not observed in those runs; the test asserts it regardless.
- Resolver cache (both PartFData fixes kept): 5/5 failed in round 0 with the exact original
  `Operations that change non-concurrent collections must have exclusive access`.
- Note: a first draft of the resolver test used the 20 000-category rule set; the resolver merges every
  category's synonyms, so it was slow enough to hit the join guard on fixed and unfixed code alike. That was
  a test-design error, not a product hang; the test now uses a small rule set.

## Validation (local, APPDATA/USERPROFILE redirected to a scratch folder, NUGET_PACKAGES pinned)

- Focused `PartFDataConcurrencyTests` (Release, built immediately before): 5/5 passed in 10 consecutive runs
  (~9 s per run).
- Full `dotnet test SAM/SAM.Tests/SAM.Tests.csproj` (Debug, with build): **2824/2824** passed
  (2819 existing + 5 new).
- CI-equivalent Release: `dotnet build -c Release` then `dotnet test -c Release --no-build`: **2824/2824**.
- `msbuild SAM.sln /t:Restore` then `/t:Rebuild /m:1 /nr:false /p:Configuration=Release
  /p:UseSharedCompilation=false`: 0 errors. 110 pre-existing warnings (101 CS8632, 2 CS8073, 2 CS0661,
  2 CS0659, 2 CS0108, 1 CS0162), the same set as the PR's first revision; none in the changed files. The Grasshopper
  post-build copy wrote into the redirected APPDATA, not the real SAM install.
- PR CI (`build`, `test`, `spdx`): reported on SAM-BIM/SAM#181.

## Final concurrency audit of `PartFData.cs`

- No static mutable state (only `const`s and one pure static method).
- `PartFCategories`, `WholeDwellingRates_Lps`: public, caller-owned, never lazily populated or mutated by
  `PartFData`; only read. Mutating them while lookups run was never supported and is unchanged.
- `dictionary_SpaceUse`, `textMap_Legacy`: fixed as above.
- `spaceSemanticsResolver ??= CreateResolver()`: the resolver and its TextMap are fully built before the
  assignment, so it is not fill-after-publish. A first-use race can build two equivalent resolvers and keep
  one; each is now internally thread-safe, so this is benign and left unchanged.
- No equivalent race remains in `PartFData`. Outside this path, `TM59InternalConditionResolver` has a
  similar per-Guid cache but is created per `TM59Manager` call, not shared through `PartFData`; not examined
  further.

## Unresolved issues, risks

- The `"SAM.Analytical.ActiveSetting default Part F data"` test collection is left as is; it also
  serialises other shared state and removing it is not part of this fix.

## Next step

Review and merge into `sow/2026-Q4` (maintainer's call), then add the `PROJECT_PROGRESS.md` closeout on the
base branch with the merge SHA.
