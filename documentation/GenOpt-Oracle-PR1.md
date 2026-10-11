# PR1 — GenOpt 3.1.1 behavioural oracle (record)

Branch `feature/genopt-oracle-traces`, base `sow/2026-Q4` @ `021c36e9`.

This is the first PR of the Java-free GenOpt replacement. It is the **semantic gate**: PR2 (the native kernel in
`SAM.Math`) does not start until the exit criteria below are met and the owner decisions in §5 are answered.

## 1. Status

Evidence complete; awaiting owner review of decisions D1–D4.
- No product code changed.
- `SAM.sln` unchanged.
- The oracle tool is test tooling built on its own; like `SAM.Tests`, it is not in the solution.

## 2. Work completed

### Specification

`documentation/GenOpt-3.1.1-Behaviour.md` is the GenOpt 3.1.1 behaviour specification for the subset Tas uses:
- GPSHookeJeeves, GPSCoordinateSearch, GoldenSection;
- the evaluation layer: number parsing, coordinate rounding, bounds, result database, numbering, failure and retry rules;
- reporting and termination.

### Oracle tool: `SAM/SAM.Tests.GenOptOracle` (net8.0 console)

| Verb | What it does |
|---|---|
| `sim` | Fake simulator GenOpt calls. Mirrors the Tas file layout: `Variables.txt` → `Output.txt` `name::value`, plus `Error.txt`. |
| `cases` | Runs 31 synthetic experiments through a locally supplied Java GenOpt, **twice each** to check determinism, and writes sanitised golden traces. |
| `replay` | Executable form of the specification; replays every golden trace and compares bit for bit. |
| `probe-float`, `analyse-float`, `export-float-fixture` | Experiment E-F (coordinate rounding) and its fixture. |

### Golden fixtures and tests

- `SAM/SAM.Tests/Golden/GenOpt/`: 31 traces (`*.json`, data rows only) and `float-rounding-java8.tsv` (3,521
  Java-observed values).
- `SAM/SAM.Tests/GenOptOracleFixtureTests.cs` (66 tests):
  - fixture integrity;
  - sanitisation: no drive paths, user, temp or OneDrive paths, Tas/EDSL material, LBNL banner or copyright text, or
    e-mail addresses.

## 3. Results (exit criteria)

| # | Criterion | Result |
|---|---|---|
| 1 | Every Phase-1 semantic CONFIRMED or an approved difference | `replay`: **31/31 traces MATCH bit for bit** (coordinates, outputs, simulation numbers, main/sub counters, comments, termination, retries, GoldenSection overview). Register in spec §7. One difference, D1, awaits approval. |
| 2 | Float rounding resolved | Shortest-decimal is **wrong** outside 7.45e-9…3.37e7 (Java differs on 11,440 of 240,138 probed floats). `Java8FloatTextModel` reproduces Java on all but **42**, all in 2^83 ≤ \|f\| < 2^86 (≈9.7e24–7.7e25). **D1.** |
| 3 | Cache semantics resolved | GenOpt's database is a red-black tree with a non-transitive 1e-12 comparator. "First match" fails (`ec-gs-collapse`); the RB-tree model matches. Dictionary or quantised keys are **not** equivalent. |
| 4 | Determinism | Every case run twice through Java: identical (31/31). |
| 5 | Sanitisation | Automated test (verified to fail on a planted path) plus manual review. All fixtures are synthetic; no historical Tas material committed. |
| 6 | Gate T | **PASSED.** Direct `TasGenExecute.exe` invocation works without Java or GenOpt; results are bit-identical to the 2025-12-05 Java GenOpt run. Evidence lands in SAM_Tas PR1-T. |
| 7 | Licence questions | Listed in §5 for the owner. |

### New findings that change the earlier plan

1. GenOpt reads command-file numbers with `StreamTokenizer` arithmetic:
   - the exponent path rounds twice;
   - integer-valued values pass through `(int)`, so `-0` → `0`;
   - algorithm keywords reject exponent notation.
2. Java 8 `Float.toString` is not shortest-decimal (spec §2.1).
3. A global-search improvement found on the last allowed simulation is never recorded (`switch`/`break` structure).
4. `MaxEqualResults` has no effect on GPS or GoldenSection.
5. The TasGenExecute compile-error text contains only lowercase "error", so GenOpt detects it only through the missing
   `Result`.

## 4. Decisions and assumptions

- **Oracle:** `genopt.jar` as shipped with Tas (built 2016-03-30), run under Temurin JRE 1.8.0_504 by explicit path.
  - Java gets `-Duser.home=<work>` so the user profile is not touched.
  - Neither the jar nor the JRE is committed.
- **`UnitsOfExecution = 1`** in all cases: sequential, as Phase 1 will be.
- **Probing:** GPS initial points with `MaxIte = 1` are used to probe rounding; the result depends only on `(float)x`.
- **Not added to `SAM.sln`:** the oracle tool follows `SAM.Tests`, which is also built by project file.

## 5. Owner decisions required (blocking PR2 merge, not PR2 start)

| ID | Question |
|---|---|
| D1 | Accept 2^83 ≤ \|x\| < 2^86 as outside the parity domain (native kernel uses the model everywhere)? |
| D2 | Accept the float model's provenance? Its structure was informed by knowledge of the JDK 8 `FloatingDecimal` design, then validated only by black-box probing; no source was copied. Red-black tree per textbook. |
| D3 | GenOpt attribution for the spec and the native kernel: none, a header line, or `NOTICE`/`THIRD_PARTY.md`? GenOpt licence: BSD-3-style with "Enhancements" clause and DOE notice; appears to match `BSD-3-Clause-LBNL`. |
| D4 | Is storing GenOpt output data rows (without the banner or copyright header) in fixtures acceptable? |

## 6. Files changed

- `documentation/GenOpt-3.1.1-Behaviour.md`, `documentation/GenOpt-Oracle-PR1.md`
- `SAM/SAM.Tests.GenOptOracle/*` (new tool)
- `SAM/SAM.Tests/Golden/GenOpt/*` (31 `.json` + 1 `.tsv`)
- `SAM/SAM.Tests/GenOptOracleFixtureTests.cs`
- `SAM/SAM.Tests/SAM.Tests.csproj` (copy the `.tsv` fixture)

## 7. Validation performed

- `dotnet build SAM/SAM.Tests.GenOptOracle -c Release`: 0 warnings, 0 errors.
- `dotnet test SAM/SAM.Tests --filter GenOptOracleFixtureTests` (Release): 66/66 passed. A planted fixture containing a
  drive path made the sanitisation test fail, then was removed.
- `SAM.Tests.GenOptOracle cases …`: 31 cases, all deterministic across two Java runs.
- `SAM.Tests.GenOptOracle replay`: 31/31 MATCH.
- `SAM.Tests.GenOptOracle probe-float` (seed 20261007, 100k uniform + 100k focused + structured sets = 240,138 floats):
  42 model mismatches, all in the D1 band (exit code 1 is expected for that reason).
- Full `SAM.Tests` (Release): **2890/2890 passed** (the previous 2824 plus these 66).
- **Not yet run:** PR CI.

## 8. Unresolved issues and risks

- **D1–D4** are open.
- **Tool not in CI:** the oracle tool is not built by CI (not in `SAM.sln`), so it could rot. Mitigations: the
  specification and fixtures are what PR2 depends on; `replay` is the regression check whenever the tool is touched.

## 9. Next step

1. Owner reviews D1–D4 on this PR.
2. Merge SAM_Tas PR1-T (Tas protocol evidence).
3. Merge this PR, then make the `PROJECT_PROGRESS.md` closeout on `sow/2026-Q4`.
4. Start PR2 (`feature/native-optimiser-kernel`). Port `SpecReplay`'s semantics into a generic `SAM.Math` kernel and
   replay all 31 traces plus the float fixture from `SAM.Tests`.
