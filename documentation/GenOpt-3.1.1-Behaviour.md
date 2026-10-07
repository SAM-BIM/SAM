# GenOpt 3.1.1 behaviour specification (the subset Tas Generic Optimisation uses)

Status: **PR1 oracle evidence.** Every item marked **CONFIRMED** is reproduced bit for bit by the executable form of this
specification (`SAM.Tests.GenOptOracle`, verb `replay`). It replays all 31 synthetic golden traces recorded from the real
Java GenOpt 3.1.1 in `SAM/SAM.Tests/Golden/GenOpt/`. Items marked **DIFFERENCE** need owner approval (see §8).

Scope: continuous parameters only, `GPSHookeJeeves`, `GPSCoordinateSearch` and `GoldenSection`, a single simulation at a time,
`WriteStepNumber = false`. This is what Tas Generic Optimisation and SAM's `SAM.Analytical.Tas.GenOpt` write. Everything
else in GenOpt is out of scope.

Oracle: GenOpt 3.1.1 (`genopt.jar`, built 2016-03-30, as shipped with Tas), run under a Temurin Java 8 runtime
(`1.8.0_504`) through `java -classpath genopt.jar genopt.GenOpt Config.ini`. The fake simulator writes values with
round-trip precision, so objective values are exact.

## 1. Inputs as GenOpt reads them

### 1.1 Command-file numbers

Values in `Command.txt` are read with `java.io.StreamTokenizer`, not a correctly rounded parser. The model is
`JavaNumbers.ParseLikeStreamTokenizer`, CONFIRMED by every trace.

- The mantissa is accumulated as `v = v·10 + digit`, then divided by `10^d`, where d is the number of digits after the
  point; `10^d` is built by repeated multiplication. This is exact for at most 15 significant digits and at most 22
  decimal places.
- For **parameter** keywords (`Ini`, `Min`, `Max`, `Step`):
  - an `E<exp>` suffix is applied as `num *= Math.pow(10, exp)`, which rounds twice;
  - an integer-valued result passes through `Integer.toString((int)num)`, so **`-0` becomes `0`** (`ec-negative-zero-start`).
- For **numeric algorithm** keywords (e.g. `AbsDiffFunction`), exponent notation is **rejected**
  ("Expected ';', got 'E-300'"). Writers must use plain decimals.
- `Min`/`Max` omitted means `SMALL`/`BIG`, i.e. −∞/+∞ (`e11-unbounded`).

### 1.2 Settings

| Keyword | Meaning | Evidence |
|---|---|---|
| `MaxIte` | Upper limit on **simulations** (cache hits and infeasible points are not counted). | `e5-maxite-7`, `ec-plateau` |
| `MaxEqualResults` | Parsed and must be ≥ 2; **no effect** on these algorithms (only `NelderMeadONeill` calls the check). | `e4-flat-maxequalresults-2` replays identically to `e3-flat` |
| `MeshSizeDivider` r, `InitialMeshSizeExponent` s0, `MeshSizeExponentIncrement` t, `NumberOfStepReduction` m | GPS mesh (§3) | `e9-*` |
| `AbsDiffFunction`, `IntervalReduction` | GoldenSection stopping (§4) | `e10-*` |

**Objective:** only the first output (`Result` in Tas) is minimised. Later outputs are recorded only (`e12-multi-output`).

## 2. Evaluation layer

### 2.1 Coordinate rounding (GPS only)

Before every GPS evaluation each coordinate becomes `Double.parseDouble(Float.toString((float)x))`.

- The result depends only on `f = (float)x` (IEEE round-to-nearest-even).
- **It is not "shortest decimal".** Java 8's `Float.toString` differs in three observed ways. The model is
  `Java8FloatTextModel`, verified on **240,138** floats probed through the real GenOpt (experiment E-F,
  `probe-float`):
  1. **Integer-valued floats with 1 ≤ |f| < 2^63** are written as exact integer digits. When the binary exponent e > 24,
     `floor((e − 25)·log10 2)` trailing digits are dropped (for e − 25 ≥ 2) with round-half-up.
     Example: 2^31 → `2.14748365E9`.
  2. **Other floats** use free-format digit generation with a symmetric margin of half the float spacing. The margin is
     **halved again when the significand has a single set bit** (powers of two, including subnormal ones).
     Example: 2^-27 → `7.4505806E-9`, not `7.450581E-9`.
  3. **Scientific notation** (decimal exponent < −3 or ≥ 8) always generates **at least two digits**. The decimal exponent
     comes from a first-order estimate, `floor((m − 1.5)·0.289529654 + 0.176091259 + e·0.301029995663981)`, which can be
     one too high just below a power of ten. A leading zero digit is then kept when the margin reaches the next power of
     ten. Examples: `1.4E-45`, `9.8E-45`, `1.0E-43`.
- Shortest-decimal agrees with Java on all 156,032 probed floats with 7.45e-9 < |f| < 3.37e7, but not outside it. The model agrees on every
  probed float except **42 of 2,013** in **2^83 ≤ |f| < 2^86** (≈ 9.7e24 to 7.7e25). See §8, D1.
- Fixture: `Golden/GenOpt/float-rounding-java8.tsv` holds 3,521 Java-observed values covering every class; the 42 are
  marked `unmodelled`.
- Consequence: lattice arithmetic is re-snapped every step. For example, 0.2 + 0.1 and 0.4 − 0.1 both become `0.3` and hit
  the same cache entry (`ec-float-equal-points`, `ec-cross-zero`).

### 2.2 Bounds (GPS only)

If a rounded coordinate is `< Min` or `> Max`:
- every output is `Double.MAX_VALUE`;
- there is no simulation, no cache entry and no simulation count;
- the point is **not reported**.

The initial point must be feasible, otherwise the run stops with "Initial point not feasible".

CONFIRMED: `e2-*`. `e2-infeasible-pattern` shows an infeasible pattern point whose exploration still returns feasible
improving points.

GoldenSection performs neither rounding nor a feasibility check.

### 2.3 Result database (cache)

- A sorted map (red-black tree) of evaluated points. Keys are the rounded coordinates (plus step number, always 1).
- Points are compared coordinate by coordinate: they count as equal when `(1±1e-12)` relative tolerance holds (for
  `x1 > 0`; mirrored for ≤ 0, so 0 equals only ±0), otherwise ordered descending.
- Because this equality is **not transitive**, which entry a lookup finds depends on the tree shape. A
  dictionary, a quantised key or "first match" is **not** equivalent. CONFIRMED by `ec-gs-collapse`: a first-match lookup
  returned a different cached value; the red-black model (`ApproximateRedBlackMap`, standard RB-INSERT-FIXUP, root-down
  lookup with the query as left operand, no deletion) reproduces Java.
- After float rounding, distinct GPS points differ by far more than 1e-12 relative. In practice the tolerance only matters
  for GoldenSection near convergence and for ±0.
- A cache hit:
  - returns the stored outputs;
  - takes simulation number = the current simulation count;
  - does not increase the count;
  - is still reported (CONFIRMED: `ec-plateau`, `e8-step-zero`).

### 2.4 Simulation numbering and failures

- Simulation numbers are assigned in evaluation order. GoldenSection's first two points are one batch, numbered 1, 2.
- A simulation fails when its log contains `"Error"`, or an output or objective is missing. On failure:
  - **during the first evaluation batch**: no retry; the run stops with an error (`e7-fail-first`, `e7-fail-once-first`);
  - **later**: one retry with the same simulation number. If the retry succeeds, the run continues unchanged
    (`e7-fail-once-later`, same trace as without failure); if it fails, the run stops with an error and no minimum is
    reported (`e7-fail-later`).

## 3. GPS (`GPSCoordinateSearch`, `GPSHookeJeeves`)

### 3.1 Setup

- Parameters are used in their **original units** (no scaling or transformation).
- Direction for coordinate i is `±Step_i`; a negative `Step` swaps the signs (`e8-step-negative`).
- `Step = 0` never moves that coordinate: both trials are cache hits that fail (`e8-step-zero`).
- Mesh factor `Δ = 1 / r^s`, with `s = s0` initially.
- `cooPoi[i] = 0` for every coordinate (0 means "+ first", 1 means "− first").
- `nRed = 0`.

### 3.2 Main loop

The loop follows GenOpt's `do { switch(step) … } while (iterate && simulations < MaxIte)` structure literally. The
fall-through and the `break` matter.

- **Step 0.** `X[0] = evaluate(Ini)`. Report it as a sub and a main iteration ("Initial point.").
- **Step 1 – global search.** Coordinate search: none. Hooke-Jeeves:
  - If k > 0:
    - `b = (2·X[k]) − X[k−1]`, componentwise and in that order;
    - evaluate b and report it as a sub iteration ("Exploration base, Delta = Δ.").
  - Otherwise `b = X[k]`, not evaluated.
  - `G = Explore(b)`.
  - If G is non-empty and `min F(G) < F(X[k])` (strict, first minimum wins): set L = ∅, step = 3 and **`break`**. The
    update then runs only on the next pass, *after* the `MaxIte` test. So a global improvement found on the last allowed
    simulation is never recorded (`e5-maxite-7`).
- **Step 2 – local search.** `L = Explore(X[k])` (fall-through).
- **Step 3 – update.** `S = L ++ G`; j = first index of the minimum of S.
  - If `F(S[j]) < F(X[k])`: `X[k+1] = S[j]`. Report it as a sub and a main iteration ("Local search reduced cost." if
    `j < |L|`, else "Global search reduced cost.").
  - Otherwise:
    - `X[k+1] = X[k]`; `s += t`; `Δ = 1/r^s`.
    - If `nRed == m`: report "…Maximum number of step reductions reached." (sub and main), report the minimum (§5) and
      **stop: success**.
    - Else report "Iteration step did not reduce cost. Reduce step size to 'Δ'." (sub and main) and `nRed++`.
  - Then `k++` and go to step 1.
- **Exit on `MaxIte`** without stopping: report "Current lowest point." (§5); GenOpt reports "Maximum number of iteration
  exceeded." (process exit code 0, logged under "Error message").

### 3.3 Explore(base)

`Explore(base)` = coordinate polling.

For i = 0…n−1, while `simulations < MaxIte`:
- Trial = current best point (initially base) with coordinate i moved by `Δ·(cooPoi[i]==0 ? +Step_i : −Step_i)`.
- Evaluate the trial.
- **Improved** (strict): report "Cost reduced     at xi+dxi." (or `-d`) and make the trial the current best.
- **Not improved**: report "Cost not reduced at …", **flip `cooPoi[i]`** (the flip persists across iterations), and if
  `simulations < MaxIte` try the other sign.
- Returns every trial outcome, with failures returned as copies of the point they started from.

### 3.4 Mesh consequences (CONFIRMED by `e9-step-reductions-1`, `e9-step-reductions-2` and the `e1-*` traces)

- There are **m + 1 mesh levels**: `Δ = r^-s0 … r^-(s0+m·t)`. The run stops on the first failure at the last level.
- The mesh never expands.
- Coordinates lie on `Ini + Δ·Step·ℤ` (then float-rounded), **not** on `Min + n·Step`.
- After a failed iteration the pattern point equals X[k], which is a cache hit.

## 4. GoldenSection

Requirements:
- exactly one parameter, with both `Min` and `Max`;
- `Ini` and `Step` are ignored.

Variables: `gr = (−1 + Math.pow(5, 0.5)) / 2` (bitwise equal under .NET, E16), `dx = U − L`, `I = 1`.

1. `I *= gr`; second point `L + I·dx`; reductions = 1.
2. `I *= gr`; first point `L + I·dx`.
3. Evaluate the batch [first, second] → simulations 1, 2. Then `x0 = L`, `x1 = first`, `x2 = second`, `x3 = U`.
4. Repeat:
   - reductions++; `I *= gr`;
   - if `F(x2) < F(x1)`: `fLowBor = F(x1)`; `x0 = x1`; `x1 = x2`; `x2 = evaluate(x3 − I·dx)`;
   - else: `fLowBor = F(x2)`; `x3 = x2`; `x2 = x1`; `x1 = evaluate(x0 + I·dx)`;
   - except in AbsDiffFunction mode, two consecutive `F(x1) == F(x2)` means **nullspace**: the run stops with an error,
     after the overview is written (`e10-gs-nullspace`).
5. Continue while:
   - in AbsDiffFunction mode, `|fLowBor − min(F(x1),F(x2))| ≥ d`; and then, by switch fall-through,
   - reductions ≠ `nIntRedMax`.

`nIntRedMax`:
- `MaxIte − 1` for AbsDiffFunction or no keyword (9 if `MaxIte` ≤ 1);
- for `IntervalReduction` ρ: `Math.round((float)(ln ρ / ln gr + 1.49999)) − 1` (9 if that value is ≤ 1).

Result:
- if `F(x1) < F(x2)` the interval is [x0, x2], else [x1, x3];
- AbsDiffFunction mode ending by count is a `MaxIte` exit; otherwise success.

Every evaluation is reported as a sub iteration "Linesearch." No main iterations are written, so no minimum row either.
The output listing ends with the uncertainty interval overview: lower, upper, mid point, length, and normalised length
over (U − L). CONFIRMED: `e10-gs-absdiff`, `e10-gs-intervalreduction`, `e10-gs-no-keyword`, `e10-gs-nullspace`,
`ec-gs-collapse`.

## 5. Reporting

`OutputListingAll` rows are `simulation, main, sub, step, outputs…, parameters…, comment`. Counters start at main = 1, sub = 1.

| Report | Written to | Counter effect |
|---|---|---|
| Sub | `OutputListingAll` only | sub++ |
| Main | `OutputListingMain` only | main++, sub = 1 |

Rules:
- A point reported both ways appears in both files; its stored main point carries the incremented sub counter.
- Points whose outputs are all `MAX_VALUE` are never reported (GPS).
- **Minimum / current lowest point** rows:
  - written only when at least one main iteration was reported;
  - start from the last main point, scan main points backwards, then sub points backwards, replacing only on strictly
    smaller F;
  - print that point with its stored numbers and the comment "Minimum point." / "Current lowest point." to both files,
    without changing counters.
- Tas Generic Optimisation's own "Optimisation Results" shows the lowest-`Result` row of `OutputListingAll` instead.

## 6. Termination summary

| Outcome | GenOpt text | Exit code |
|---|---|---|
| GPS step reductions exhausted; GoldenSection criterion met | "Optimization completed successfully." | 0 |
| `MaxIte` reached (GPS); GoldenSection AbsDiff count exhausted | "Maximum number of iteration exceeded." under "Error message" | 0 |
| Simulation failure after the retry rule; GoldenSection nullspace; invalid input | "GenOpt terminated with error." | 1 |

## 7. Experiment register

| ID | Question | Result | Evidence |
|---|---|---|---|
| E1 | Full GPS-HJ traces, direction memory, pattern timing | CONFIRMED | `e1-hj-quad-1d/2d/3d` |
| E2 | Bounds, start on bound, infeasible pattern point | CONFIRMED | `e2-*` |
| E3/E4 | Ties; MaxEqualResults | CONFIRMED (no effect) | `e3-flat`, `e4-flat-maxequalresults-2` |
| E5 | MaxIte cut | CONFIRMED (incl. the unrecorded last global improvement) | `e5-maxite-7` |
| E6/E-F | Coordinate rounding | CONFIRMED with difference D1 | probe (240,138 floats), `float-rounding-java8.tsv` |
| E7 | Failures and retry | CONFIRMED | `e7-*` |
| E8 | Step 0, negative Step | CONFIRMED | `e8-*` |
| E9 | Mesh parameters, m + 1 levels | CONFIRMED | `e9-*` |
| E10/E16 | GoldenSection modes, nullspace, `pow` agreement | CONFIRMED | `e10-*`, `ec-gs-collapse` |
| E11 | Unbounded | CONFIRMED | `e11-unbounded` |
| E12 | Multiple outputs | CONFIRMED | `e12-multi-output` |
| E13 | Coordinate search | CONFIRMED | `e13-coordinate-search` |
| E-C | Cache equality, numbering, ±0, tiny values, approximate tree | CONFIRMED | `ec-*` |
| E-P | Command-file number parsing | CONFIRMED (new finding, §1.1) | all traces, `ec-negative-zero-start`, `ec-gs-collapse` set-up |
| E14 | Tas `Output.txt` precision (15 significant digits) | Tas protocol: SAM_Tas PR1-T (Gate T) | — |

## 8. Differences and decisions requiring owner approval

- **D1 – float rounding band.** For 2^83 ≤ |coordinate| < 2^86 (≈ 9.7e24–7.7e25), 42 of 2,013 probed values differ from
  the model by one unit in the last printed digit. The pattern is consistent with 64-bit overflow inside Java's
  integer-arithmetic digit generator. Proposal: treat this band as outside the parity domain (no building parameter comes
  close); the native kernel uses the model everywhere.
- **D2 – provenance of the float model and cache structure.** The float model's structure (integer path,
  single-bit-significand margin, two-digit scientific rule, exponent estimate) was informed by general knowledge of the
  design of JDK 8's `FloatingDecimal` (OpenJDK). It was then fixed and validated only by black-box comparison. No JDK
  source was copied. The red-black tree follows the standard textbook algorithm. Owner to confirm this provenance is
  acceptable for SAM, or ask for a different treatment.
- **D3 – GenOpt attribution.** The specification and the planned native kernel are derived from reading the GenOpt 3.1.1
  source (licence: 3-clause BSD-style with an additional "Enhancements" paragraph and a DOE notice; appears to match
  SPDX `BSD-3-Clause-LBNL`). How to attribute this — no notice, a header line, or `NOTICE`/`THIRD_PARTY.md` entries — is
  an owner/legal decision. PR1 adds none.
- **D4 – fixture content.** Golden traces store only GenOpt's data rows and a sanitised termination text. The GenOpt
  banner and the copyright header of the output files are not stored. Owner to confirm this is acceptable.

## 9. Reproducing the oracle

```
dotnet build SAM/SAM.Tests.GenOptOracle -c Release
SAM.Tests.GenOptOracle.exe cases       --java <java.exe> --genopt-jar <genopt.jar> --work <dir> --golden SAM/SAM.Tests/Golden/GenOpt
SAM.Tests.GenOptOracle.exe replay      --golden SAM/SAM.Tests/Golden/GenOpt
SAM.Tests.GenOptOracle.exe probe-float --java <java.exe> --genopt-jar <genopt.jar> --work <dir>
SAM.Tests.GenOptOracle.exe analyse-float --results <dir>/float-probe-report.all.tsv
```

Requirements and notes:
- The work directory and the tool path must not contain spaces, because GenOpt splits the simulation command on whitespace.
- Java is passed `-Duser.home=<work>/java-user-home` so GenOpt's preference file stays out of the user profile.
- `genopt.jar` and the Java runtime are local inputs and are never committed.
