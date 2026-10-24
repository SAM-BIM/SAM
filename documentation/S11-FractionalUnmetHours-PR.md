# F-S11-2: fractional `SpaceSimulationResult` unmet hours (SAM)

Branch `fix/s11-fractional-unmet-hours` → base `sow/2026-Q4` (cut from `b7c2c36a`). Draft PR: SAM-BIM/SAM#219.
Record date: 2026-10-24. Tested content SHA: `47cc5dd8`. Every later commit is record-only: `eadffe50` added this
record; the owner-deferral update that follows changes only this file. The PR description names the final head.

## Current status

Draft, **not merged, not Ready**. An independent review **accepted the core fix** at `eadffe50`, and verified Build, Test
and SPDX passing there. The owner **deferred Revit compatibility** (finding 1, below). Waiting for owner merge approval.
No engine run, no merge, no SAM_OpenStudio#34 integration, no installer/release work is authorised by this PR.

## Why

F-S11-2 (recorded in Draft SAM-BIM/SAM_OpenStudio#34, `documentation/S11-OpenStudio-ResultsRoundTrip-PR.md`): EnergyPlus
reports "Setpoint Not Met Time" in hours with fractions (sub-hourly timesteps), and the OpenStudio importer passes the
annual sum to `SpaceSimulationResult.SetValue(UnmetHours, double)`. The parameter was declared
`IntegerParameterValue(0, 8760)`, so `SetValue` converted with `System.Convert.ToInt32`, which **rounds half to even**
(not truncation): 0.25 → 0, 14.5 → 14, 14.75 → 15, 15.5 → 16. It also accepted out-of-range fractions after rounding
(−0.25 → 0, 8760.25 → 8760). B1a already reads unmet hours as a double, so the loss happened inside SAM.

F-S11-1 (unrelated identity fix) is already merged as SAM-BIM/SAM_OpenStudio#35 (`4b020585`, closeout `194ee1e6`); this PR
does not touch matching.

## Change

One line of metadata in `SAM/SAM.Analytical/Enums/Parameter/SpaceSimulationResultParameter.cs`:

`[ParameterProperties("Unmet Hours", "Unmet Hours"), IntegerParameterValue(0, 8760)] UnmetHours`
→ `... DoubleParameterValue(0, 8760)] UnmetHours` (plus a two-line comment).

Kept: enum member, ordinal 34, stored key `"Unmet Hours"` in the `SAM.Analytical` parameter set, range [0, 8760]
inclusive, the JSON model. Unchanged on purpose: `OccupiedUnmetHours`, `OccupiedHours`, `OccupiedHours25/28`,
`UnmetHourFirstIndex`, `LoadIndex` and `AdjacencyClusterSimulationResultParameter.UnmetHours` stay integer. No parallel
parameter, no schema version, no migration of saved files.

## Why this is compatible (from the source, not assumed)

- **Storage is untyped.** `ParameterSet` keeps `object` values; the attribute only decides the conversion on `SetValue`
  (`ParameterizedSAMObject.SetValue` → `ParameterValue.TryConvert`). Reads (`TryGetValue<T>`) convert the stored object
  with `Core.Query.TryConvert`, whatever the attribute says.
- **JSON.** `ParameterSet.ReadParameterValue` reads a JSON number without `.`/`e` as `int`, otherwise `double`. Old files
  (`"Value": 9`) therefore load as `Int32` and stay `Int32` in memory until re-set; `TryGetValue<double>` gives 9.0 and
  `TryGetValue<int>` gives 9, exactly as before. New writes store a `double`; `Query.ToJsonNode` keeps the decimal
  (`292.0`, `14.5`) so it reads back as a double.
- **Tas.** `SAM_Tas` `Query.UnmetHours` passes an `int` count; it is now stored as the same-valued double (`292` →
  `292.0`), so the numerical meaning is identical. Readers: `SAM_Tas` benchmark `ToBenchmark` (`TryGetValue<double>`),
  S10 harness `ResultSubject.Number` (any numeric), `WorkflowCalculator`/`SAM_Tas_Grasshopper` `Core.Modify.Copy`
  (object round trip through `SetValue`).
- **OpenStudio.** `Convert.ToSAM` `SimulationResults.AddLoadTypeResult` passes the double; B1a `ToBenchmark` and the S11
  read-back `SubjectDocument` read `TryGetValue<double>`.
- **Integer readers** of the new double get `Convert.ToInt32` rounding, i.e. the old stored value, so nothing that
  asked for `int` sees a different number than before. No SAM code reads `SpaceSimulationResultParameter.UnmetHours` as
  `int` (`ToDesignExplorer` reads the *adjacency-cluster* parameter, unchanged).
- Previously rounded values cannot be recovered from saved SAM JSON; saved files are not rewritten.

## Findings for the owner / reviewer

1. **SAM_Revit export — DEFERRED by the owner (2026-10-24).** Owner decision, given in chat: Revit is unavailable and
   its shared-parameter definitions can be changed later, so no Revit fix is made now; SAM keeps fractional unmet
   hours. The risk below stays open as a known, accepted follow-up, not a blocker for this PR. Original analysis:
   risk, not verified, not changed (outside this PR's write scope). `SAM_Revit`
   `ActiveSetting` maps `UnmetHours` to Revit Space parameters `SAM_UnmetHoursCooling` / `SAM_UnmetHoursHeating`, and
   `Core.Revit.Modify.SetValues` passes the stored object to `Modify.SetValue(Parameter, object)`. `SetValue_Integer`
   accepts `int`, `string`, `bool`, `IntegerId`, `LongId`, **not `double`**; `SetValue_Double` accepts any numeric. The
   Revit parameter's storage type comes from the shared-parameter workbook used by `GenerateSharedParametersFile`, which is
   not in any SAM-BIM repository, so it could not be checked. If those parameters are **Integer**, results set after this
   change (now `double`, including Tas whole hours) would silently stop being written to Revit; if they are **Number**,
   nothing changes. Old models loaded from JSON still hold `Int32` and are unaffected until re-simulated.
   Possible later fix, **not authorised now** (a separate SAM_Revit PR, or a change to the shared-parameter definitions): in
   `SAM.Core.Revit/Modify/SetValue.cs` `SetValue_Integer`, add a branch for a finite whole-number `double` within `int`
   range → `parameter.Set((int)value)`; leave fractional doubles refused (no silent rounding into an Integer parameter).
   Or confirm the two parameters are Number type, in which case nothing is needed.
2. **NaN now stored instead of throwing.** Before: `SetValue(UnmetHours, double.NaN)` threw `OverflowException` (from
   `Convert.ToInt32`). After: it is stored, as for every other ranged `DoubleParameterValue` (`DoubleParameterValue`
   range checks are false for NaN). ±∞ are refused by the range. Producers do not set NaN (OpenStudio sums SQL rows,
   Tas counts), B1a readers drop non-finite values (`IsFinite`), but a NaN would make a later `TryGetValue<int>` throw.
   Not changed here (shared `SAM.Core` attribute behaviour); listed for the owner.
3. Small, intended behaviour differences: a numeric string `"14.5"` is now accepted (was refused by `int.TryParse`);
   `SAM_UI` `CustomParameter.ParameterTypes` now offers `double` for this parameter, so the property grid shows and edits
   fractions.

## Tests

`SAM/SAM.Tests/SpaceSimulationResultUnmetHoursTests.cs` (xUnit, 12 tests): 0.25 / 14.5 / 14.75 through `SetValue` /
`GetValue` and through a whole `AnalyticalModel` JSON save/reopen; a Tas `int` count (292) keeps its value; an old Tas
result (integer JSON, shape taken from S10 acc2 `s08-fix-box/fixture.json`) reads back 9 and survives re-save; zero and
both boundaries kept, −0.25 / 8760.25 / −1 / 8761 refused without changing the stored value; missing ≠ zero; member
ordinal 34, key and value types (this one Double, the other counters Integer).

## Validation (local, 2026-10-24, Windows, .NET SDK 10.0.401)

All builds used isolated outputs and `-p:APPDATA=<scratch>`, so no Grasshopper/`%APPDATA%\SAM` deployment happened.
Base SAM `b7c2c36a` → `SAM.Analytical.dll` `017370d9…`; tested SAM `47cc5dd8` → `SAM.Analytical.dll` `eacd1ed8…`
(full hashes in the local evidence `dll-hashes.sha256`). Every consumer test binary below was checked to load `eacd1ed8…`.

| Check | Result |
|---|---|
| New tests on the **base** metadata (`b7c2c36a` + new test file) | **8 failed, 4 passed** of 12: 0.25 → 0, 14.5 → 14, 14.75 → 15 (both `SetValue` and save/reopen), −0.25 accepted, type Integer |
| New tests at `47cc5dd8` | 12/12 pass |
| Full `SAM.Tests` at `47cc5dd8` | **3738/3738** pass (3726 existing + 12 new), 0 skipped |
| OpenStudio SQL → SAM probe, base SAM | heating 14.5 h → **14**, cooling 14.75 h → **15** (fails, reproduces F-S11-2) |
| OpenStudio SQL → SAM probe, tested SAM | **14.5 / 14.75** mapped by `Modify.AddResults(adjacencyCluster, sql)` and identical after `AnalyticalModel` JSON save/reopen; pass |
| SAM_OpenStudio Q4 `194ee1e6` (includes #35), unmodified, `tests/SAM.Analytical.OpenStudio.Tests`, `TestCategory!=Simulation` | 488 passed, 2 skipped (real-SQL fixtures not present locally), 0 failed |
| SAM_OpenStudio Q4 B1a `benchmark/SAM.Analytical.OpenStudio.Benchmark.Tests`, `TestCategory!=Simulation` | 51/51 |
| SAM_Tas Q4 `922044fa` `benchmark/SAM.Analytical.Tas.Benchmark.Tests` | 40/40 |
| SAM_Tas Q4 `SAM_Tas/SAM.Analytical.Tas.DirectT3D.ReadBack.Tests` (incl. S10 synthetic triple, S08/S10 offline replays, runner-script doubles) | 249/249 |
| Real S10 acc2 saved models (`C:\TasOut\s10\acc2\s08-fix-box\fixture.json` and `...\s08-fix-two\fixture.json`, read-only) loaded with base and tested SAM | **identical**: FixBox cooling 9 / heating 292, FixA 0 / 40, FixB 0 / 0 h, still `Int32`-backed; `OccupiedUnmetHours`, `UnmetHourFirstIndex` unchanged; equal to the accepted acc2 `subject.json` and `b1a.json` values; both fixture hashes unchanged |

**Not run / limits.**
- S10 `results-subject` regeneration and `Run-S10-Results.ps1`: not run. `results-subject` reopens the TSD through Tas COM
  (`Convert.ToSAM_AnalyticalModelSimulationResult`), a Tas operation this task does not authorise. The `results-compare`
  replay reads an existing `subject.json` (already numbers) and does not exercise this metadata, so it would prove
  nothing here. The accepted S10 baseline (including its one BLOCKED FixB heating design-day-hour row) is untouched.
- No EnergyPlus, OpenStudio CLI or Tas run. **Disclosure:** a first SAM_OpenStudio test run in a deep scratch folder was
  started without CI's `TestCategory!=Simulation` filter and stopped. Its `Simulation`-category tests all failed at
  "The OSM could not be saved" (path too long) before any CLI call; no `eplusout*`, `*.err`, `in.idf` or `run.log` was
  produced and no EnergyPlus/OpenStudio process was running. Its log is kept as evidence; the reported results above
  come from the rerun with the CI filter in a short path.
- SAM_Revit cannot be built or exercised here (Revit API); finding 1 is from source reading only.
- Hosted CI: below; the PR description names the final head and its checks.

**Hosted checks at `eadffe50`** (record-only on top of tested `47cc5dd8`), all SUCCESS, also verified by the
independent review: Build (Windows) [40112883313](https://github.com/SAM-BIM/SAM/actions/runs/40112883313), Test
[40112883328](https://github.com/SAM-BIM/SAM/actions/runs/40112883328) (synthetic merge `dfeadc3` of `eadffe50` into
`b7c2c36a`; 3738/3738 passed, including the 12 new tests), SPDX
[40112883247](https://github.com/SAM-BIM/SAM/actions/runs/40112883247).

**Provenance of probe dependencies (machine-local copies).** SAM_SQLite `build/` from the local checkout at `ec2eb86`
(`SAM.Core.SQLite.dll` `ba9400e9…`); SAM_Validation `build/` from the local checkout at Q4 `2a3acfaf`
(`SAM.Analytical.Benchmark.dll` `0dfdc8ec…`). SAM_OpenStudio and SAM_Tas sources were `git archive` exports of their
Q4 heads; the only edits were in the probe copy (below), never in a repository.

**Commands** (from the worktree, `$A` = a scratch APPDATA folder):

```
dotnet test SAM/SAM.Tests/SAM.Tests.csproj -c Release -p:APPDATA=$A --filter "FullyQualifiedName~SpaceSimulationResultUnmetHoursTests"
dotnet test SAM/SAM.Tests/SAM.Tests.csproj -c Release -p:APPDATA=$A
```

Consumer suites: export SAM_OpenStudio and SAM_Tas Q4 next to a `SAM/build` holding the tested DLLs (plus
`SAM_SQLite/build`, `SAM_Validation/build`), keep the path short (MAX_PATH), then
`dotnet test <project> -c Release -p:Platform=x64 -p:APPDATA=$A --filter "TestCategory!=Simulation"` (OpenStudio) or
`dotnet test <project> -c Release -p:APPDATA=$A` (Tas). OpenStudio probe: `FS112FractionalUnmetProbe.cs` added to the
exported `tests/SAM.Analytical.OpenStudio.Tests/C5/`, with `AddResultsBySqlTests.CreateResultsSql`/name helpers made
`internal` and the not-met rows changed to `0.625` per hour with `0.125` (heating) / `0.375` (cooling) at the peak hour
(23 × 0.625 + 0.125 = 14.5, 23 × 0.625 + 0.375 = 14.75; binary-exact).

**Evidence (machine-local, not committed; copy before switching machines):**
`SAM-BIM/.f-s11-2-20261024/` — `sam-tests/` (before/after/full TRX), `os-probe/` (probe source, patched test copy,
before/after logs, the stopped unfiltered run log), `logs/` (consumer suite logs/TRX), `fixture-probe/` (source, base/after
outputs, fixture hashes), `behaviour-probe/` (NaN and edge cases, base/after), `dll-hashes.sha256`, `ws/` (exported
workspaces).

## Files changed

- `SAM/SAM.Analytical/Enums/Parameter/SpaceSimulationResultParameter.cs` (metadata line + comment)
- `SAM/SAM.Tests/SpaceSimulationResultUnmetHoursTests.cs` (new)
- `documentation/S11-FractionalUnmetHours-PR.md` (this record)

## Next step

1. Owner merge approval at the final head named in the PR description, after its checks pass. Finding 1 is deferred;
   finding 2 (NaN) is unchanged and stays a recorded note. Recommended executor once approved: Claude Code, Claude
   Sonnet 5.5, Medium effort, existing session, Build mode. It is a mechanical guarded merge plus closeout, with the
   context already here.
2. After merge: known-SHA `PROJECT_PROGRESS.md` closeout on SAM `sow/2026-Q4` (docs-only, `[skip ci]`), carrying the Revit
   deferral; guarded cleanup of `fix/s11-fractional-unmet-hours`.
3. Then, as a separate reviewed step, integrate into SAM_OpenStudio#34 and reconcile its pinned F-S11-2 expected-failure
   test (left untouched here). The Revit follow-up waits for its own owner decision.
