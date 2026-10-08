# Project Progress - SAM (2026-Q4)

## Branch

`sow/2026-Q4` - bootstrapped 2026-10-06 from `master` `56a70592`. Frozen Q3 record: `sow/2026-Q3` @ `3fcd64bf` (not modified).

## Last updated

2026-10-08 (native Optimisation PR2 energy/mass units and display formatting closeout, SAM#186; Java-free GenOpt PR6 comment-only closeout, SAM#185). Earlier: 2026-10-07 (Part O stable semantic design key closeout, SAM#184).

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
  - **Update:** D1–D4 resolved by the owner on 2026-10-07 and implemented in PR2 (see the PR2 section below).
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
  - **Update:** steps 1 and 2 are done (PR2, SAM#183, below).

## Java-free GenOpt replacement — PR2, native optimisation kernel in `SAM.Math` (2026-10-07)

- **Status:** complete, closed.
  - SAM-BIM/SAM#183 (`feature/native-optimiser-kernel`) merged into `sow/2026-Q4` as merge commit
    `0989ad819b34a7b797cc8c6ddf977081fbff1165` (PR head `647d17b5ad4a886e2a198a898d41feade5b65ea1`, Q4 base `e4a6f8e4`).
  - Merge method: merge commit, head-commit protected.
  - Remote and local branch removed.
  - Record: `documentation/GenOpt-NativeKernel-PR2.md`.
- **Work completed:** a generic kernel in `SAM/SAM.Math/Classes/Optimisation` (namespace `SAM.Math`).
  - It implements the PR1 specification (`SpecReplay` semantics), not textbook pattern search.
  - Algorithms: `HookeJeeves`, `CoordinateSearch` (shared `GeneralisedPatternSearch`), `GoldenSection`.
  - The internal `EvaluationContext` evaluation layer:
    - `Java8FloatText` rounding, with its own exact decimal→double conversion;
    - bounds;
    - `ApproximatePointCache<T>` (red-black tree, 1e-12 comparator);
    - numbering;
    - retry-once-then-stop;
    - cancellation;
    - main/sub counters, minimum report, progress.
  - API:
    - `Run(OptimisationProblem, IObjectiveEvaluator, IProgress<OptimisationProgress>, CancellationToken)` →
      `OptimisationResult`;
    - trace entries map one-to-one to OutputListingAll and OutputListingMain rows.
  - Dependencies: no Tas, EDSL, GenOpt-file-format or Java dependency. GenOpt text, `StreamTokenizer` parsing and the
    synthetic functions live only in the test adapter `SAM.Tests/Helpers/GenOptGoldenTrace.cs`.
- **Owner decisions (2026-10-07):**
  - **D1:** the 2^83 ≤ |x| < 2^86 band is accepted as outside parity and documented. A test pins the 42 values.
  - **D2:** the float-model provenance is accepted. The statement is in the class comment, the spec and the record.
  - **D3:** acknowledgment line in the `Optimiser` class comment, plus a GenOpt entry in `THIRD_PARTY.md` (SAM's
    established convention). The entry holds the licence and provenance details, with the copyright line taken
    verbatim from `legal.html` in `genopt.jar`.
  - **D4:** fixtures are accepted with the console transcript stripped from all 31 traces. The oracle tool still
    compares it for determinism, and a fixture test enforces the rule.
- **Decisions / assumptions:**
  - `SAM.Math` stays `netstandard2.0` with C# 7.3, and gains no new references.
  - Supported runtime: .NET 8. SAM no longer supports or uses .NET Framework (owner, 2026-10-07). No .NET Framework
    compatibility work or testing is in scope.
  - Additions outside GenOpt parity: cancellation (`Cancelled`, with the partial trace); evaluator-defined failure
    (exception, null or wrong output count counts as a failed attempt); validation before the first evaluation.
- **Files changed:** 66.
  - New in `SAM.Math`: `Classes/Optimisation/*` (19), `Enums/*` (3), `Interfaces/IObjectiveEvaluator.cs`.
  - New in `SAM.Tests`: `OptimisationGoldenTraceTests.cs`, `OptimisationKernelTests.cs`, `Java8FloatTextTests.cs`,
    `ApproximatePointCacheTests.cs`, `Helpers/GenOptGoldenTrace.cs`.
  - Modified: `GenOptOracleFixtureTests.cs`, `SAM.Tests.csproj`, `Golden/GenOpt/*.json` (31, deletions only),
    `SAM.Tests.GenOptOracle/CaseRunner.cs`, `THIRD_PARTY.md`, `documentation/GenOpt-3.1.1-Behaviour.md`.
  - New: `documentation/GenOpt-NativeKernel-PR2.md`.
- **Validation:**
  - All 31 golden traces replay **bit for bit** in `SAM.Tests`. They were re-run on the merged `sow/2026-Q4`, with 232
    GenOpt-related tests passing.
  - Full `SAM.Tests` Release: 3056/3056 (2890 + 166) on 5 consecutive runs.
  - Mutation check: 5 kernel mutations, each caught.
  - Kernel float model against the oracle model: 7,775,409 floats, 0 differences (local).
  - Oracle `replay` on the stripped fixtures: 31/31 MATCH.
  - PR CI on the head: `build`, `test`, `spdx` green.
- **Unresolved issues, risks:**
  - No golden trace visits a value where Java 8 and shortest-decimal rounding disagree. The fixture and two
    end-to-end tests cover this instead.
  - One unidentified intermittent `SAM.Tests` failure: once in 6 local full runs, a parameterless `[Fact]`, name not
    captured. If it recurs, capture the name and open a separate issue.
  - The `Java8FloatText` class comment still says why it avoids `double.Parse`, citing the .NET Framework parser.
    It was left unchanged on purpose (no product-code change for the runtime correction). It is not a compatibility
    requirement.
- **Next step:**
  - SAM_Tas PR3: a native GenOpt compatibility adapter plus `TasGenExecuteObjectiveEvaluator`, following
    `TASGENEXECUTE_PROTOCOL.md` §2.3. It consumes the `SAM.Math` kernel.
  - Only when the owner requests it.

---

# Historical record - 2026-Q3 (frozen)

Source: last revision of the file on `sow/2026-Q3`, commit `689f75d5` (the file was removed from the Q3 tip by `8ab6a62b`; `sow/2026-Q3` tip is `3fcd64bf`). Preserved verbatim except that heading levels are shifted down one. Everything below describes Q3 and is not a current instruction.

## Java-free GenOpt replacement — PR6, `Java8FloatText` comment (2026-10-08)

- **Status:** complete, closed. SAM-BIM/SAM#185 (`docs/java8floattext-runtime-comment`) merged into `sow/2026-Q4` as
  merge commit `bac7507c35e4d380aca11c2635d00ef4c2203057` (parents: Q4 base `f391e022` + reviewed PR head
  `eb1efdcb3166e20d0ff99d335b63253063765dd6`; merge tree `bbeef13b` identical to the head tree); merge method: merge
  commit with `--match-head-commit`. PR CI (`build`, `test`, `spdx`) green on the head; post-merge `Build (Windows)` and
  `Test` on `bac7507c` green. Branch removed locally and on origin.
- **Work completed:** comment only in `SAM.Math/Classes/Optimisation/Java8FloatText.cs`: the sentence ".NET Framework parser
  is not correctly rounded" (deferred from PR3 to PR6) now states that the class converts with its own exact arithmetic
  so its result does not depend on any runtime's `double.Parse`, without naming .NET Framework hosts. No code or binary
  behaviour change. `Java8FloatText` is not obsoleted by the Java retirement: it models GenOpt's arithmetic, which the
  native optimiser reproduces.
- **Review:** Codex P2 on the first wording (named .NET Framework Revit/Rhino hosts, contradicting the 2026-10-07 owner
  decision that SAM no longer supports .NET Framework) fixed in `eb1efdcb`, thread resolved.
- **Programme context:** independent part of the PR6 set: SAM_Tas#87 (legacy Java route retired, `NativeGenOptOutcome`;
  merge `8dffaa3d`), SAM_Tas_Grasshopper#12 (merge `c43c2cad`), SAM_UI#212 (merge `a985d7d9`). Record: SAM_Tas
  `SAM_Tas/SAM.Analytical.Tas.GenOpt/NATIVE_GENOPT_PR6.md`.
- **Files changed:** 1 (`Java8FloatText.cs`). **Validation:** `dotnet build SAM.Math.csproj -c Release` 0 errors; CI green.
- **Unresolved issues, risks:** none for SAM. SAM_Deploy follow-up (separate): ship one current `SAM.Math.dll` to every
  SAM/Rhino location.
- **Next step:** none for this PR.

## Energy and mass units, engineering display formatting — native Optimisation PR2 (2026-10-08)

- **Status:** complete, closed. SAM-BIM/SAM#186 (`feature/units-energy-mass-formatting`) merged into `sow/2026-Q4` as
  merge commit `b52beaf3d2cae1f88d568f5333eed81d677c329e` (parents: Q4 base `b081065e` + reviewed PR head
  `5273ce725d513ab982aa102d24f8064aed0ab58c`; merge tree `016a578b` identical to the head tree); merge method: merge
  commit, after the owner's acceptance. PR CI (`build`, `test`, `spdx`) green on the head; post-merge `Build (Windows)`
  and `Test` on `b52beaf3` green. Codex: no review, comment or thread. Branch removed on origin. Record:
  `documentation/Units-EnergyMass-Formatting-PR.md`. PR2 of the staged native Optimisation plan
  (`opus-5-5-robust-blum.md`).
- **Work completed:**
  - `SAM.Units`: `UnitType` gained `WattHour`, `KilowattHour`, `MegawattHour`, `Kilogram` and `Tonne`, and
    `UnitCategory` gained `Energy` and `Mass`. All new values are appended; no ordinal moved.
  - Energy factors are based on Wh and mass factors on kg. kWh and kg are the defaults in both unit styles.
  - `Joule`/`Kilojoule` stay in `Enthaply`, so Energy does not convert to J; a cross-category conversion gives NaN.
  - `SAM.Core.Reporting.QuantityFormatter`:
    - Energy is shown in kWh to 1 dp, switching the group to MWh to 2 dp from 10 MWh.
    - Mass is shown in kg to 1 dp, switching the group to t to 2 dp from 10 t.
    - Group switching is now table-driven; Power behaviour is unchanged.
    - New per-category `DocumentOptions.Decimals` override.
    - New public `FormatNumber` (NaN/∞ → "—"), `FormatSignificant` (4 s.f. by default, scientific below 1e-3) and the
      static `DecimalsForSignificantFigures`.
    - The new helpers are deliberately not on `IQuantityFormatter`.
- **Decisions:** Count → "persons", Ratio at 0 dp and every other existing default are unchanged (Space reports depend
  on them). Optimisation counts are formatted as integers in the optimisation UI, not through `Count`. Currency and
  CO2e are not SAM.Units categories; they stay optimisation-level quantities (`SAM.Core.Optimisation`, PR3).
- **Files changed:**
  - `SAM/SAM.Units/{Enums/UnitType,Enums/UnitCategory,Query/UnitCategory,Query/UnitType,Query/UnitTypes,Convert/ByUnitType,Convert/ToSI,Convert/ToImperial}.cs`;
  - `SAM/SAM.Core.Reporting/Classes/{QuantityFormatter,DocumentOptions}.cs`;
  - new tests `SAM/SAM.Tests/{EnergyMassUnitsTests,QuantityFormatterEngineeringTests}.cs`;
  - the record.
- **Validation:**
  - Implementation session: `SAM.Tests` 3137/3137 (+66).
  - Independent review: a trial merge of PR2 with PR3 was clean; `SAM.sln` Release built with 0 errors; `SAM.Tests`
    3261/3261.
  - The existing Space report, `UnitsTests` and `QuantityFormatterTests` are unchanged and passing.
  - No existing `Format` path reaches the changed NaN handling, because `Format` throws on NaN before it.
- **Unresolved issues, risks (for UX PR5, not defects):**
  - A column made only of values below 1e-3 shows zeros (decimals are capped at 6), so PR5 needs a scientific fallback.
  - Currency (0 dp at 1 000 or more, 2 dp below) and the CO2e symbol (kgCO2e maps to the `Kilogram` unit type) must be
    handled in the optimisation presentation layer.
  - A negative `Decimals` override throws only when a value is formatted.
- **Next step:**
  - Refresh PR3 (SAM#187) onto this head, with its required finite-number validation fix.
  - Re-run the full SAM validation and wait for fresh CI before merging it.
  - SAM_UI consumes the formatter in UX PR5.

## Part O stable semantic design key (2026-10-07)

- **Status:** complete, closed.
  - SAM-BIM/SAM#184 (`feature/part-o-design-key`) merged into `sow/2026-Q4` as merge commit
    `bb8e3e6f25016a6ac9063e9d7213a86e80675084` (PR head `f15f32fc57e603b42befde0faf4742f033474117`, Q4 base at PR cut `021c36e9`;
    the base had advanced to `79c101c2` - SAM#182/#183 - by merge time).
  - Merge method: merge commit, head-commit protected. Remote and local branch removed. Record: `documentation/PartODesignKey-Q4.md`.
- **Work completed:** `Query.PartODesignKey(AnalyticalModel)` -> `PartODesignKey:v1:<sha256>`, one SHA-256 over one canonical
  stream (no FNV state mixed in) answering "has the engineering meaning of this design changed since a Part O result was produced
  from it?". Optional `PartOModelReference.DesignKey` (design references only, schema stays v1, absent = unknown) recorded by
  `Create.PartOBaselineReferenceFromDesign` and inherited by 2B/3 references. `PartOModelResolution.Inspect` (design kind) compares
  `DesignKey` when present, else the legacy `Fingerprint`. `SimulationResultProvenance.Fingerprint` unchanged in definition and output
  (its exclusion list was extracted to the shared `ParameterNames_Excluded`).
- **Decisions / assumptions:**
  - Cause verified in code and by probe: `PartOEquipmentSelection`, each pooled `VentilationUnitReference` and
    `PartOProjectTestVentilationUnit` are `SAMObject`s that mint `Guid.NewGuid()` and persist it; the UI rebuilds them on every read and
    `PersistPartOInputs.SameValue` compares guids, so an identical selection is rewritten with new guids and the byte fingerprint moves.
    Save/reopen of one object is stable - the instability is regeneration, not serialization.
  - Absent selection == explicit default; pool order/duplicates carry no meaning; parameter-set guids (build-MVID derived) and the
    fingerprint's existing exclusions are not meaning; cluster identities (incl. Part F terminal references) are meaning, so a Part F
    recalculation is a design change.
  - Not changed: Direct T3D, TAS, TM59, Part O simulation, SAM_UI.
- **Files changed:** `SAM/SAM.Analytical/Query/PartODesignKey.cs` (new), `Classes/PartOModelReference.cs`, `Create/PartOBaselineReference.cs`,
  `Query/PartOModelResolution.cs`, `Classes/SimulationResultProvenance.cs`, `SAM/SAM.Tests/PartODesignKeyTests.cs` (new, 15 tests),
  `documentation/PartODesignKey-Q4.md`.
- **Validation:** PR CI `build (Release)`, `test (Release)`, `spdx` green on the exact head. Local full `SAM.Tests` on the PR head
  2839/2839. Because the base advanced after CI, the merge of the PR head onto `79c101c2` was also built and tested in a throwaway
  worktree before merging: 3071/3071 (2839 + 232 from SAM#182/#183), no overlapping files.
- **Unresolved issues, risks:**
  - One extra streamed cluster serialization per run when the design reference is created.
  - Results saved before this have no `DesignKey` and keep the byte-fingerprint behaviour until re-run.
  - `PartOMaterialisationRecord.Fingerprint_Baseline` still uses the byte fingerprint and may show the same noise (candidate follow-up).
  - SAM_UI still rebuilds Part O inputs with fresh guids and `PersistPartOInputs.SameValue` compares guids (harmless for this question now).
- **Next step:** a separate SAM_UI PR that consumes `DesignKey` / `PartOModelResolution` for "previous compatible run found" and
  reopening a previous run from the design model. Companion docs PR SAM-BIM/SAM_UI#208 (Part O guide) is closed in the SAM_UI record.

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
