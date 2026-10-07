# Project Progress - SAM (2026-Q4)

## Branch

`sow/2026-Q4` - bootstrapped 2026-10-06 from `master` `56a70592`. Frozen Q3 record: `sow/2026-Q3` @ `3fcd64bf` (not modified).

## Last updated

2026-10-07 (Java-free GenOpt PR1 closeout, SAM#182).

## Current status

Q4 branch cut from `master` `56a70592`, which is the exact commit pinned in SAM_Deploy's frozen Q3 baseline (`v20261006.1`). Bootstrap added only internal docs (this file, `AGENTS.md`). No product source changed. No Q4 product work has started.

## Q4 priorities

Not yet set by the owner. Record them here at the first Q4 planning pass. Known carry-over work is listed below.

## Known carry-over work

- **SAM Grasshopper icon redesign - PR #166** (`feature/sam-gh-icon-redesign` @ `cf4d924a`, open, base `sow/2026-Q3`, not merged). Analysed 2026-10-06: the branch carries only its own 6 icon-only commits (`f57b54a`, `a7d65b0`, `55745b8`, `7300a7d`, `be024ba`, `cf4d924`) on top of Q3 commit `bc85ba61`. Those commits are not reachable from `sow/2026-Q4` (Q4 is built on the promoted `master` line), so a plain retarget would list 495 commits. Replaying exactly those commits onto `sow/2026-Q4` @ `e29cb86e` is conflict-free (verified commit-by-commit with `git merge-tree`; identical to the net-diff merge). Planned action: rebase-onto Q4 as a new branch + PR, then close this one; owner-approved controlled task, not yet executed. **Update:** migrated; replacement Q4 PR SAM#179 (see the Q4 icon-redesign migration section); this old PR stays open for now.
- Branch `codex/part-o-cooling-control-room` - Q3 complete: all commits already in sow/2026-Q3.
- Branch `docs/parto-regression-run-2026-09-23` - owner decision: 2 commits not in Q3 (docs, last 2026-09-24).

## Repository-specific next steps

- Await Q4 planning. Open PRs for Q4 work against `sow/2026-Q4`.
- Follow the continuity convention in `AGENTS.md` for every PR and closeout.

## Decisions / assumptions

- Q4 base is `master` `56a70592`; the internal files were recovered from `sow/2026-Q3` into this branch only, never onto `master`.
- Q4 history intentionally does not contain the Q3 branch history (the maintained `master` is the promoted Q3 line, which is not a descendant of `sow/2026-Q3`); the frozen `sow/2026-Q3` branch is the permanent record.
- Historical Q2/Q3 content below is kept as evidence; its branch names, SHAs and next steps describe Q3 and are not current instructions.

## Validation

- Bootstrap verified 2026-10-06: `sow/2026-Q4` was created at exactly `56a70592` and the push was a normal (non-forced) branch creation.

## Issues / blockers

- None at bootstrap.

## Next step

- Owner to set Q4 priorities; then start the first Q4 task from this branch.

## Q4 operational cleanup (2026-10-06)

- Reviewed every active Q2/Q3 reference in this repository on `sow/2026-Q4` (workflow branch filters, dependency-branch resolution, `.gitmodules`/validation, docs). Historical Q2/Q3 mentions (feature documentation records, the frozen Q3 section below) are intentionally unchanged.
- No change needed: this repository has no active Q2/Q3 operational reference.
- Checked, no action: the `github.repository_owner == 'SAM-BIM'` build guard (intentional; its comment names HoareLea only to explain why the guard exists), CODEOWNERS (SAM-BIM owners), and workflow secrets (no HoareLea-named secret). The local `upstream` (HoareLea) remote is preserved.
- Carry-over: **SAM Grasshopper icon redesign - PR #166** (`feature/sam-gh-icon-redesign` @ `cf4d924a`, open, base `sow/2026-Q3`, not merged). Analysed 2026-10-06: the branch carries only its own 6 icon-only commits (`f57b54a`, `a7d65b0`, `55745b8`, `7300a7d`, `be024ba`, `cf4d924`) on top of Q3 commit `bc85ba61`. Those commits are not reachable from `sow/2026-Q4` (Q4 is built on the promoted `master` line), so a plain retarget would list 495 commits. Replaying exactly those commits onto `sow/2026-Q4` @ `e29cb86e` is conflict-free (verified commit-by-commit with `git merge-tree`; identical to the net-diff merge). Planned action: rebase-onto Q4 as a new branch + PR, then close this one; owner-approved controlled task, not yet executed.
- Full cross-repository record, migration table and owner decisions: `SAM_Deploy:sow/2026-Q4` `PROJECT_PROGRESS.md`.

## Q4 icon-redesign migration (2026-10-06)

- Old PR: SAM-BIM/SAM#166 (`feature/sam-gh-icon-redesign` @ `cf4d924a`, base `sow/2026-Q3`) - **preserved, open, untouched**.
- New branch `feature/sam-gh-icon-redesign-q4` cut from `sow/2026-Q4` @ `5f57055d`; new PR **SAM-BIM/SAM#179** (base `sow/2026-Q4`), replay-only tip `cbf392f8`, feature head `1646852b`. **Not merged.**
- Replayed (old -> new, `cherry-pick -x`): `f57b54a`->`3412211`, `a7d65b0`->`4041941`, `55745b8`->`966421e`, `7300a7d`->`345dd7c`, `be024ba`->`afb3aba`, `cf4d924`->`cbf392f`; plus one new docs commit `1646852` pointing `documentation/GH-IconRedesign-PR1.md` at the new PR. No Q3 history imported.
- High-scrutiny review: `Grasshopper/` is byte-identical at the Q3 base, the Q3 tip and Q4 (`fe6cdeb4`), so no file touched by the PR changed on Q3/Q4 after it was cut. The 1849 files are 521 one-token `Icon` swaps, additions-only `.resx`/`Resources.Designer.cs` (6 each), 354 resource PNGs (copies of the 337 kit PNGs), the `design/grasshopper-icons` kit and the PR record. The one non-swap line is the `Icon` override added to `A_SAMAnalytical` (documented in the PR record).
- Verified at the replay-only tip, before push: tree identical to the net-diff merge of the old feature onto Q4 (`c40b597c`); same aggregate and per-commit `git patch-id`, file set, numstat and blobs (all PNG/resx/designer) as the old PR; ComponentGuid lines 533 unchanged; no EOL/BOM churn; no workflow/solution/project/`.gitmodules`/`AGENTS.md`/`PROJECT_PROGRESS.md` change; `git diff --check` output identical to the old PR (inherited, not cleaned).
- Validation: SAM_UI kit `check_source.py origin/sow/2026-Q4` (SAM's own kit predates it): 521 swaps, ComponentGuids unchanged, only flag the documented `A_SAMAnalytical` line; Release `msbuild SAM.sln` (APPDATA/USERPROFILE redirected) 0 errors; `check_assemblies.py` OK; PR CI `build`, `test`, `spdx` green; mergeable. Not run: Rhino-hosted `SAM.Core.Grasshopper.Tests` (Rhino does not start under the redirected profile; not in CI).
- Next: owner decides whether/when to close the old PR; merge remains the maintainer's call.

## Q4 runtime-URL cleanup (2026-10-06)

- **Status:** complete. SAM-BIM/SAM#180 merged into `sow/2026-Q4` as merge commit `7d1c1c75bd67730d52faa610abd8d6721366bdc0` (PR head `dc540d5ff1c17545d54af19ed2c1881f3afbef44`, Q4 base `9b11dd26`); merge method: merge commit (repository convention). Remote and local `fix/sam-bim-runtime-urls-q4` removed.
- **Work completed:** The update check (`LatestVersion`) now reads `api.github.com/repos/SAM-BIM/SAM_Deploy/releases/latest` instead of `HoareLea/SAM_Deploy` (HoareLea latest was `v20260630.1`, so every current install `v20261006.1` was told an update was available); the model description "Delivered by SAM" text and the two "source code" menu actions now name `https://github.com/SAM-BIM/SAM`. SAM-BIM is the authoritative ecosystem; HoareLea is no longer the synchronised operational source. Record: the PR's `SAM-BIM-RuntimeUrls-Q4.md` document.
- **Decisions / owner classifications:** `Modify/ExportHydra.cs` clones the private `HoareLea/ScriptsHydra`: KEEP - intentional external/private dependency (no SAM-BIM equivalent; none to be invented). Assembly author/contact strings (`Hoare Lea`, `@hoarelea.com` in `Kernel/AssemblyInfo.cs`) are provenance/metadata, not repository ownership: KEEP unchanged. The unused `AboutInfoType.HoareLea` enum/text is KEEP (dead). These are owner decisions and not baseline blockers.
- **Files changed:** `SAM/SAM.Core/Query/LatestVersion.cs`, `SAMAnalyticalCreateAnalyticalModelByAdjacencyCluster.cs`, `SAMAnalyticalCreateAnalyticalModelBySpaces.cs`, `SAMCoreFilterByType.cs`, `SAMCoreInspect.cs`, `documentation/SAM-BIM-RuntimeUrls-Q4.md` (5 product lines, one token each).
- **Validation:** `msbuild SAM.sln -p:Configuration=Release` (APPDATA/USERPROFILE redirected): 0 errors; `SAM.Core.dll` contains the new endpoint and not the old. `SAM.Tests`: second full run 2819/2819 passed (first run: 1 intermittent failure from the PartFData race below). PR CI build, test, spdx green.
- **Unresolved issues, risks:** Pre-existing intermittent race in `PartFData.GetPartFCategory` (shared dictionary written from several threads; `PartODwellingStrategyMaterialisationTests.SystemsScope_ScaffoldingIsTheAddMechanicalSystemsShape` failed once): a separate product bug that will get its own PR, not fixed here.
- **Next step:** Separate PR for the PartF concurrency defect; the 18 deferred icon PRs remain open and untouched. **Update:** the PartFData race is fixed by SAM#181 (see the Q4 Part F concurrency section).

## Q4 Part F classification concurrency (2026-10-07)

- **Status:** complete, closed. SAM-BIM/SAM#181 merged into `sow/2026-Q4` as merge commit `3a501cfe9b7061fe7bf6e940ca72620fc0cd5b7b` (PR head `bcbd0c7c1254c9312d3eb5047ae042f5f44cbd44`, Q4 base `fe1d60b5`); merge method: merge commit, head-commit protected. Remote and local `fix/partfdata-thread-safe-cache-q4` removed. Record: `documentation/PartFData-ThreadSafeCache-Q4.md`.
- **Work completed:** Fixed all three unsynchronised shared collections on the `PartFData.GetPartFCategory(Space, ...)` path (one `PartFData` is shared via `ActiveSetting`; xUnit runs classes in parallel): (1) `PartFData.dictionary_SpaceUse` - published before filled; the original intermittent failure (`InvalidOperationException` from `Dictionary.TryInsert`, 1/2819 in the SAM#180 full run); (2) `PartFData.textMap_Legacy` - same fill-after-publish pattern (partial/wrong legacy match, `Collection was modified`); (3) `SpaceSemanticsResolver.cache`/`cacheKey` - plain dictionaries written on every `Resolve`.
- **Decisions:** (1)/(2) double-checked locking, built in a local under a private lock and published via a `volatile` field only when complete; first-match / longest-match / tie-gives-null behaviour unchanged. (3) cache read and write each under `lock_Cache`, `ResolveCore` outside the lock; the cache pair always updated together. `ConcurrentDictionary`/`Lazy<T>` rejected (see record). `spaceSemanticsResolver ??=` left as is (benign: fully built before assignment). No equivalent unsafe lazy shared collection remains in `PartFData.cs`.
- **Files changed:** `SAM/SAM.Analytical/Classes/PartF/PartFData.cs`, `SAM/SAM.Analytical/Classes/Semantics/SpaceSemanticsResolver.cs`, `SAM/SAM.Tests/PartFDataConcurrencyTests.cs` (new, 5 tests), `documentation/PartFData-ThreadSafeCache-Q4.md`.
- **Validation:** Each race reproduced against the unfixed code with the new tests (all failed in round 0). Fixed: focused tests 5/5 in 10 consecutive runs; full `SAM.Tests` Debug 2824/2824 and Release 2824/2824 (2819 + 5); `msbuild SAM.sln` Release rebuild 0 errors (110 pre-existing warnings, none in changed files). PR CI `spdx`, `build`, `test` green on the exact head.
- **Unresolved issues, risks:** None for this stream. The `"SAM.Analytical.ActiveSetting default Part F data"` xUnit collection is intentionally kept (also serialises other shared state). `TM59InternalConditionResolver` has a similar per-Guid cache but is per-call, not shared; not examined further.
- **Next step:** None for Part F concurrency. The 18 Q4 icon PRs remain deliberately deferred.

## Java-free GenOpt replacement — PR1, GenOpt 3.1.1 semantic oracle (2026-10-07)

- **Status:** complete, closed.
  - SAM-BIM/SAM#182 (`feature/genopt-oracle-traces`) merged into `sow/2026-Q4` as merge commit
    `0a67e1f5b0a4e9815b68ab549b634e2310113561` (PR head `3b3ea4a0edfcfcd4edba243368061d376abea356`, Q4 base `021c36e9`).
  - Merge method: merge commit, head-commit protected. Remote and local branch removed.
  - Companion: SAM-BIM/SAM_Tas#85 (Gate T, merged `1873e578`).
  - Records: `documentation/GenOpt-Oracle-PR1.md` (PR record), `documentation/GenOpt-3.1.1-Behaviour.md` (specification).
- **Goal of the stream:** replace the Java GenOpt 3.1.1 used by Tas Generic Optimisation with a native, Tas-agnostic
  SAM kernel (`SAM.Math`), shared by SAM_UI and Grasshopper. Phase 1 is GPSHookeJeeves, GPSCoordinateSearch and
  GoldenSection only; the first Tas evaluator calls `TasGenExecute.exe`. PR1 was the semantic gate.
- **Work completed:** no product code.
  - Behaviour specification for the subset Tas uses.
  - Test tooling `SAM/SAM.Tests.GenOptOracle` (verbs `cases`, `replay`, `probe-float`, `analyse-float`,
    `export-float-fixture`, `sim`).
  - 31 synthetic golden traces plus a Java-observed float fixture in `SAM/SAM.Tests/Golden/GenOpt/`.
  - `GenOptOracleFixtureTests` (66 tests: integrity and sanitisation).
- **Key findings** (all proven by `replay`, 31/31 bit-exact against real Java GenOpt):
  - **31 deterministic oracle cases.** Each was run twice through Java GenOpt with identical results. They cover GPS-HJ,
    coordinate search, GoldenSection, bounds, failures and retry, MaxIte, mesh settings, ties, Step 0 and negative
    Step, multiple outputs and the cache cases.
  - **Cache.** GenOpt's result cache is a red-black tree with a non-transitive 1e-12 point comparator. A simple
    dictionary or first-match lookup is **not** equivalent (`ec-gs-collapse`). The native kernel needs
    approximate sorted-tree behaviour.
  - **Coordinate rounding.** GPS rounding `parseDouble(Float.toString((float)x))` is **not** shortest-decimal under
    Java 8: Java differs on 11,440 of 240,138 probed floats. `Java8FloatTextModel` reproduces all but **42**, all in
    2^83 ≤ |x| < 2^86 (≈9.7e24–7.7e25).
  - **Command-file numbers** go through `StreamTokenizer`: `-0` → `0`, and algorithm keywords reject exponent notation.
  - A global improvement found on the last allowed simulation is never recorded.
  - `MaxEqualResults` has no effect on these algorithms.
- **Decisions / assumptions:**
  - The oracle is local only: the Tas-shipped `genopt.jar` run under a Temurin JRE 8 by explicit path. Neither is
    committed.
  - Fixtures are synthetic data rows only (no GenOpt banner or copyright header, no Tas material).
  - The tool is not in `SAM.sln` (like `SAM.Tests`).
  - Licensed and scratch Tas files are not committed.
- **Open owner decisions** (blocking PR2 merge, not PR2 start):
  - **D1:** accept 2^83 ≤ |x| < 2^86 as outside the parity domain?
  - **D2:** accept the float model's provenance? Structure informed by the JDK 8 `FloatingDecimal` design, validated
    only by black-box probing, no source copied.
  - **D3:** GenOpt attribution: none, a header line, or `NOTICE`/`THIRD_PARTY.md`? Licence: BSD-3-style with an
    "Enhancements" paragraph and a DOE notice, which appears to match `BSD-3-Clause-LBNL`. No notice was added.
  - **D4:** is storing sanitised GenOpt output data rows as fixtures acceptable?
- **Files changed:** 52 files.
  - `documentation/GenOpt-3.1.1-Behaviour.md`, `documentation/GenOpt-Oracle-PR1.md`
  - `SAM/SAM.Tests.GenOptOracle/*`
  - `SAM/SAM.Tests/Golden/GenOpt/*`
  - `SAM/SAM.Tests/GenOptOracleFixtureTests.cs`
  - `SAM/SAM.Tests/SAM.Tests.csproj`
- **Validation:**
  - Full `SAM.Tests` (Release): 2890/2890.
  - The sanitisation test fails on a planted path.
  - `cases`: 31/31 deterministic. `replay`: 31/31 MATCH.
  - `probe-float`: 42 mismatches, all in the D1 band.
  - PR CI: `build`, `test`, `spdx` green.
- **Unresolved issues, risks:**
  - D1–D4 are open.
  - The oracle tool is not built by CI. `replay` is its regression check.
- **Next step:**
  1. Owner resolves D1–D4.
  2. Then PR2 `feature/native-optimiser-kernel`: a generic kernel in `SAM.Math` porting `SpecReplay` semantics
     (float model, red-black approximate cache, GPS and GoldenSection, progress, cancellation, trace). It must replay
     all 31 traces and the float fixture from `SAM.Tests`.
  3. Then SAM_Tas PR3 (evaluator).

---

# Historical record - 2026-Q3 (frozen)

Source: last revision of the file on `sow/2026-Q3`, commit `689f75d5` (the file was removed from the Q3 tip by `8ab6a62b`; `sow/2026-Q3` tip is `3fcd64bf`). Preserved verbatim except that heading levels are shifted down one. Everything below describes Q3 and is not a current instruction.

## SAM Part O PR1 progress

Base: `sow/2026-Q3`. PR1 merged as SAM-BIM/SAM#176 at `fc2345667fb2726b2364264eda14a6a219250144` on 2026-10-04. Local base updated.

### Completed
Persisted CoolingStatSpaceGuid on dwelling strategies and cooled records; refused missing or out-of-dwelling selections; validated record against selected room. Legacy cooled strategies retain no room and require explicit confirmation. Uncooled canonical strategy text remains unchanged.

### Files changed
PartODwellingStrategy, PartOCooledDwelling, PartOMaterialisationRecord, MaterialisePartODwellingStrategies, refusal enum, focused tests; this progress file.

### Validation
Focused PartODwellingStrategy tests: 145 passed; broader PartO suite: 982 passed; PR Windows build, test and SPDX passed.

### PR2 validation (2026-10-04)
No SAM code changes were needed. Existing SimulationResultProvenance tests (23 passed) cover changed weather, changed model/scenarios, missing or rewritten results, and relative result resolution. PR2 diagnostics merged in SAM_Tas#81 and SAM_UI#192. No physics changed.

### Solver2D clustered anchors (SAM_UI#58), 2026-10-05
Merged as SAM-BIM/SAM#177 at `2eecf11ac6ae1a07384ab84d54ec8d90b2120841` (head `478cdd70`); local `sow/2026-Q3` = origin; branch deleted. Three exact changes to `Solver2D.cs`, none of which moves a label:
- same-ray blocker skip (deep axis-aligned overlap, margin MacroDistance, at most 8 box checks per candidate);
- identical-search resume cursor (`CandidateSequence`, limit area by reference);
- spatial index built at every size, with no halo, MacroDistance query expansion, median-short-side cells, and an outlier list.

`WorkBudget` and all backstops are unchanged. 400 coincident labels: 620 263 → 1 336 units, 164 → 0 fallbacks. Heterogeneous near-coincident and tight clusters went from 3.8M–11.4M to 92k–192k uncapped. Healthy placement fingerprints are identical. Tests: differential oracle `ReferenceSolver2D` over 11 inputs (mutation-checked); SAM.Tests 2 818 passed; PR CI build, test and SPDX green. The remaining heterogeneous-pile cost (up to 38 % of the budget) is accepted; a layout-changing fast path is tracked in SAM_UI#199. SAM_UI end-to-end test: SAM_UI#200.

### Next step
Solver2D #58 work is complete in SAM; do not start SAM_UI#199 unless requested. PR2 is complete. Next task, only when requested: real end-to-end acceptance through SAM_UI → Part O → Prepare & Run → Iteration 3 using the prepared Nuaire sample. Do not start PR3.
