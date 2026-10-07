# PR2 — native optimisation kernel in `SAM.Math` (record)

Branch `feature/native-optimiser-kernel`, base `sow/2026-Q4` @ `e4a6f8e4` (PR1 closeout). Second PR of the Java-free
GenOpt replacement. PR1 (SAM#182, oracle and specification) and PR1-T (SAM_Tas#85, TasGenExecute protocol, Gate T) are
merged.

## 1. Status

Implementation and evidence complete. Owner decisions D1–D4 are resolved (§4). Awaiting PR CI and review.

## 2. Work completed

A generic optimisation kernel in `SAM/SAM.Math/Classes/Optimisation` (namespace `SAM.Math`). It implements the proven
PR1 behaviour specification (`documentation/GenOpt-3.1.1-Behaviour.md`, the semantics of the oracle's `SpecReplay`),
not textbook pattern search.

| Type | Role |
|---|---|
| `Optimiser` (abstract) | `Run(problem, evaluator, IProgress<OptimisationProgress>, CancellationToken)` → `OptimisationResult`. `MaximumSimulations` (GenOpt `MaxIte`, default 2000). Reusable; settings are read when the run starts. |
| `GeneralisedPatternSearch` → `HookeJeeves`, `CoordinateSearch` | GPS main loop, followed literally (spec §3), including the `switch`/`break` timing that drops a global improvement found on the last permitted simulation. Mesh settings: `MeshSizeDivider` (≥ 2), `InitialMeshSizeExponent` (≥ 0), `MeshSizeExponentIncrement` (≥ 1), `NumberOfStepReduction` (≥ 1); defaults 2/0/1/4. |
| `GoldenSection` | IntervalDivider (spec §4): `StoppingCriterion` MaximumSimulations / AbsoluteDifference / IntervalReduction, nullspace stop, result `GoldenSectionInterval`. |
| `EvaluationContext` (internal) | Evaluation layer (spec §2, §5): float rounding and bounds (GPS only); approximate cache; batch de-duplication; numbering; retry-once-then-stop; cancellation; main/sub counters; minimum report; progress. |
| `Java8FloatText` | Java 8 `parseDouble(Float.toString((float)x))` model with its own exact, round-half-even decimal→double conversion (independent of `double.Parse`). |
| `ApproximatePointCache<T>` | Red-black tree with the 1e-12 relative, non-transitive point comparator. |
| `IObjectiveEvaluator`, `DelegateObjectiveEvaluator`, `ObjectiveEvaluationRequest` (simulation, attempt, coordinates), `ObjectiveEvaluation` | Evaluator contract. |
| `OptimisationParameter`, `OptimisationProblem` | Inputs (original units; `OutputCount`, first output = objective). |
| `OptimisationResult`, `OptimisationTraceEntry`, `OptimisationProgress`, enums `OptimisationOutcome`, `OptimisationEvent`, `GoldenSectionStoppingCriterion` | Structured trace/result: `Entries` = OutputListingAll rows, `MainIterations` = OutputListingMain rows. Each entry holds simulation, main/sub counters, coordinates, outputs, event, Δ, parameter index and direction. Also `Minimum`, `Interval`, `Simulations`, `Retries`, `FailedSimulation`, `FailureMessage`. |

The kernel adds three things that are not GenOpt parity claims; GenOpt has no equivalent:
- **Cancellation.** The token is checked before every new simulation and passed to the evaluator. The outcome is
  `Cancelled`, with the partial trace and no minimum. An `OperationCanceledException` thrown while the token is *not*
  cancelled counts as an ordinary failure.
- **Failure detection is the evaluator's.** Any of these counts as a failed attempt, under the same retry rule: a
  `Failure` result, an exception, a null result, or a wrong number of outputs (GenOpt's "missing output").
- **Input validation.** Invalid input throws before the first evaluation.

GenOpt specifics stay out of `SAM.Math`. The command-file number parsing (`StreamTokenizer` arithmetic), the keyword text,
the listing comment text and the synthetic functions live in the test adapter `SAM.Tests/Helpers/GenOptGoldenTrace.cs`.
`SAM.Math` gains no package or project reference. The target stays `netstandard2.0` with C# 7.3.

## 3. Tests (`SAM.Tests`, 166 new)

- `OptimisationGoldenTraceTests` (94): all 31 golden traces replay **bit for bit**. That covers rows (coordinates,
  outputs, simulation numbers, main/sub counters, comments), outcome, retries and the golden-section overview.
  Determinism (each case run twice) and minimum-reporting rules are also checked.
- `Java8FloatTextTests` (12):
  - every modelled fixture value matches Java;
  - `Round` = the model of `(float)x`;
  - D1: exactly the 42 known values in 2^83 ≤ |f| < 2^86 differ;
  - the spec examples;
  - mid-range agreement with shortest-decimal;
  - the exact conversion matches the .NET 8 parser on 20k random cases plus edge cases (ties, subnormals, overflow).
- `ApproximatePointCacheTests` (8): ±0, the 1e-12 boundaries, mirrored negatives, non-transitivity, descending order,
  replace-on-equal, key copy, and a tree-shape-dependent hit that a first-match lookup gets wrong.
- `OptimisationKernelTests` (52), covering:
  - counting: for every trace, evaluator calls = simulations + retries; consecutive numbers; retries repeat the number;
    no out-of-bounds call; ≤ `MaxIte`;
  - cache hits are reported but not counted;
  - Java 8 rounding applied end to end;
  - bounds: infeasible start; out-of-bounds trials not simulated or reported;
  - failures: first batch without retry; a later failure retried once and the run continues; two failures stop with no
    minimum; exception, wrong count and null; the golden-section first batch;
  - cancellation: before the run, during it, thrown by the evaluator, and an OCE without cancellation;
  - progress: one notification per entry, in order;
  - golden section with `MaxIte ≤ 1` (9 reductions); reuse; validation;
  - dependency hygiene: `SAM.Math` references nothing matching Tas/EDSL/GenOpt/Java/IKVM.
- `GenOptOracleFixtureTests`: also asserts that no trace stores a console transcript (D4).

## 4. Owner decisions (2026-10-07)

| ID | Decision | Implemented as |
|---|---|---|
| D1 | Accept 2^83 ≤ \|x\| < 2^86 as outside exact parity, documented | Spec §8; kernel uses the model everywhere; `Java8FloatTextTests` pins the 42 values. |
| D2 | Accept the float model's provenance | Provenance statement in the `Java8FloatText` class comment, spec §8, this record. |
| D3 | THIRD_PARTY entry, because SAM has an established `THIRD_PARTY.md` convention | `Optimiser` class comment: "Behaviour-compatible with GenOpt 3.1.1. Implementation written independently from the behavioural specification; no GenOpt source code copied." The `THIRD_PARTY.md` GenOpt entry holds the licence and provenance details, with the copyright line taken verbatim from `legal.html` in the Tas-shipped `genopt.jar`. |
| D4 | Accept sanitised data rows, but strip the console transcript | `console` removed from all 31 traces (deletions only). The oracle tool no longer serialises it but still compares it between its two Java runs for determinism. A fixture test enforces this. |

Other decisions and assumptions:
- Names use British spelling (`Optimiser`, `Optimisation…`) to match SAM's documentation.
- `Optimiser` and `GeneralisedPatternSearch` have internal constructors: the algorithm set is closed.
- The kernel is sequential (Phase 1), so `Explore` and batching follow `UnitsOfExecution = 1`.
- The kernel computes the golden ratio as `(-1 + Math.Pow(5, 0.5)) / 2`, and the GPS mesh as `1 / Math.Pow(r, s)`.
  These are bit-equal to Java under .NET 8 (E16 and the traces).

## 5. Files changed

- `SAM/SAM.Math/Classes/Optimisation/*` (19 new), `SAM/SAM.Math/Enums/*` (3 new), `SAM/SAM.Math/Interfaces/IObjectiveEvaluator.cs`.
- `SAM/SAM.Tests/`:
  - `OptimisationGoldenTraceTests.cs`, `OptimisationKernelTests.cs`, `Java8FloatTextTests.cs`,
    `ApproximatePointCacheTests.cs`, `Helpers/GenOptGoldenTrace.cs` (new);
  - `GenOptOracleFixtureTests.cs` (D4 assert);
  - `SAM.Tests.csproj` (explicit `SAM.Math` reference).
- `SAM/SAM.Tests/Golden/GenOpt/*.json` (31: `console` removed).
- `SAM/SAM.Tests.GenOptOracle/CaseRunner.cs` (console kept for determinism only).
- `THIRD_PARTY.md`, `documentation/GenOpt-3.1.1-Behaviour.md` (status, §8 decisions), this record.

## 6. Validation

- `dotnet build SAM/SAM.Math -c Release`: 0 errors, no warning in the new files.
- New tests: 166/166 passed (Release).
- **Mutation check**, each applied alone, then reverted:
  - shortest-decimal rounding → 2 tests fail;
  - global improvement recorded immediately → 12+ traces fail;
  - retry allowed in the first batch → `e7-fail-first`, `e7-fail-once-first` and 2 kernel tests fail;
  - direction memory not persisted → 12+ traces fail;
  - first-match cache → `ec-gs-collapse` fails.
  - Finding: no golden trace visits a value where Java 8 and shortest-decimal disagree. The two rounding tests (`Round`,
    end-to-end 2^-27/2^31) were added to cover that.
- **Port equivalence (local, not committed):** the kernel's `Java8FloatText.ParseOfToString` against the oracle's
  validated `Java8FloatTextModel` on 7,775,409 floats: every exponent's first and last 2,048 significands for both
  signs, a stride of 509 over all positive bit patterns, and 2M random. **0 differences.**
- Oracle tool rebuilt (0 warnings); `replay` on the stripped fixtures: **31/31 MATCH**, exit 0.
- Full `SAM.Tests` Release: **3056/3056** (2890 + 166) on 5 consecutive runs. One earlier run had 1 intermittent failure
  (3055/3056) in a parameterless `[Fact]`. Its name was lost because the output was truncated, and it did not recur in
  the 5 later runs (see §7).
- PR CI: see the PR.
- No Java, JRE, `genopt.jar`, Tas, EDSL or licensed material was needed or committed.

## 7. Unresolved issues and risks

- **Golden coverage of float rounding.** The 31 traces do not by themselves distinguish the Java 8 model from
  shortest-decimal. That coverage comes from the fixture and the two new tests. A future trace that visits such a value
  would strengthen it, but needs Java (oracle regeneration); not required.
- `AlgorithmError` mirrors GenOpt's internal consistency check; analysis says it is unreachable.
- **Unidentified intermittent test failure.** It happened once in 6 local full runs (stack: a parameterless `[Fact]`
  invoked by reflection). The new kernel tests are deterministic, synchronous and share no state, so a pre-existing
  parallelism flake is the likely cause, but this is not proven. If it recurs in CI or locally, capture the test name
  (`dotnet test … > log`) and open a separate issue.

## 8. Next step

1. PR CI green on the head; review; merge into `sow/2026-Q4` (merge commit).
2. Then the `PROJECT_PROGRESS.md` closeout on `sow/2026-Q4` (direct docs-only commit).
3. Then SAM_Tas PR3: `TasGenExecuteObjectiveEvaluator` (spec in `SAM_Tas/.../TASGENEXECUTE_PROTOCOL.md` §2.3). Only
   when requested.
