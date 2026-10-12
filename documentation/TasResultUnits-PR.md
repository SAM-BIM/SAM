# Tas consumption/peak units, option A of SAM_Tas#92: the units marker, the reader query, DesignExplorer (SAM)

Branch `fix/tas-result-units-kwh` → base `sow/2026-Q4` (cut from `c080dd08`). Record date: 2026-10-12. This is the record
of record for the coordinated set; the other three PRs record their own changes and point here.

| Order | Repository | PR | What |
|---|---|---|---|
| 1 | SAM | this PR | marker, reader/writer queries, DesignExplorer through the query |
| 2 | SAM_Tas_Grasshopper | `fix/tas-result-units-kwh` | `Tas.AddBuildingResults` through the query; W/m2, W/m3 descriptions |
| 2 | SAM_OpenStudio | `fix/tas-result-units-kwh` | the OpenStudio writer stamps the marker |
| 3 | SAM_Tas | `fix/tas-result-units-kwh` | the converter stores kWh/kW and stamps the marker; the benchmark through the query |

## Current status

PR open, **not merged**. Awaiting final-head CI and **owner review**. Do not merge before the owner approves the set.

## Why

SAM_Tas#92 (merged `cff2371d`) measured that SAM_Tas stores `ConsumptionHeating/Cooling` in **Wh** and
`PeakHeating/CoolingLoad` in **W** under the `[kWh]`/`[kW]` labels of `AnalyticalModelSimulationResultParameter`; SAM_OpenStudio
writes kWh/kW into the same parameters. The owner chose option A: correct the four values at the source, keep every
displayed engineering value, never rewrite persisted models silently.

## Design (decided here; owner may redirect)

- **Marker.** `AnalyticalModelSimulationResultParameter.ResultUnitsVersion` (Integer), **appended**: parameters are stored
  in JSON by their `ParameterProperties` name inside the `SAM.Analytical` parameter set, so no existing name or ordinal
  moves (test pins all nine ordinals and names). Stored name `"Result Units Version"`. Version `1` = consumption kWh,
  peaks kW, as labelled. `AnalyticalModelSimulationResult.CurrentResultUnitsVersion = 1`, set only by writers through
  `Modify.SetResultUnitsVersion` (records, converts nothing).
- **Provenance.** `Result.Source`, which both writers have stamped since they first wrote these parameters: their assembly
  names, `AnalyticalModelSimulationResult.TasSource = "SAM.Analytical.Tas"` (since `b51d4380`, 2025-11-11) and
  `OpenStudioSource = "SAM.Analytical.OpenStudio"` (since `659b4ca`, 2026-07-20). Verified with `git log -S
  ConsumptionHeating --all` over SAM, SAM_Tas, SAM_Tas_Grasshopper, SAM_OpenStudio, SAM_UI, SAM_Validation, SAM_Systems,
  SAM_Excel, SAM_LadybugTools, SAM_gbXML, SAM_Python and SAM_Revit: these are the only two writers in history, and each
  always used `Assembly.GetExecutingAssembly().GetName().Name`. The SAM_Tas and SAM_OpenStudio PRs assert that their
  assembly name equals the constant, so a rename cannot silently orphan persisted results.
- **Reader rule** (`Query.ResultUnitsState`, `Query.TryGetConsumptionOrPeakLoad`, `Query.ConsumptionOrPeakLoad`):

  | Marker | Source (exact, ordinal) | State | Read as |
  |---|---|---|---|
  | 1 (any number type that is exactly 1, incl. `1.0`, `"1"`) | any | `Marked` | stored (kWh, kW) |
  | anything else (`0`, `2`, `1.5`, NaN, text, bool) | any | `UnsupportedVersion` | **not read** (false, NaN) |
  | absent | `SAM.Analytical.Tas` | `LegacyTas` | stored ÷ 1000, **once** |
  | absent | `SAM.Analytical.OpenStudio` | `LegacyOpenStudio` | stored (always kWh, kW) |
  | absent | anything else (incl. `"Tas"`, other case, trailing space, null) | `UnknownProducer` | **not read** |

  The size of a value never decides. Reading never changes the result (tested). Only the four consumption/peak
  parameters are read; the hours, floor area and volume are unit-free here and are read as before.
- **DesignExplorer** (`Convert/ToDesignExplorer/File.cs`): consumption and peaks from the query, no `/ 1000`; per area and per
  volume kWh/m2, kWh/m3 (consumption ÷ area) and **W/m2, W/m3** (peak kW × 1000 ÷ area): labels and values unchanged. When
  the units are not known, the eight unit-dependent cells are left **empty** (columns kept, peak hours kept) rather than
  guessed.

## Compatibility and merge order

Every consumer reads through the query, which understands both conventions, so there is **no window of wrong values**
if the PRs merge in this order:

1. **SAM** (this PR). Alone it is safe: today's Tas results are unmarked, `LegacyTas`, and read exactly as before.
2. **SAM_Tas_Grasshopper** and **SAM_OpenStudio** (either order). Each is safe on SAM alone.
3. **SAM_Tas** last. From then on new Tas results hold kWh/kW and are marked; every consumer already reads both.

Hazards, all from merging out of order:
- SAM_Tas before SAM: SAM_Tas does not compile (it calls the new query); its CI and Q4 build would go red. No silent damage.
- SAM_Tas before SAM_Tas_Grasshopper: the old component divides the new kWh by 1000 again (1000× too small). Avoid.
- A SAM_Deploy pin set mixing new SAM_Tas with old SAM_Tas_Grasshopper has the same effect. Pin all four together.

Between PR merges, rerun each dependent PR's CI so it builds against merged `sow/2026-Q4` (the PRs share one branch name,
so their pre-merge CI builds against each other's branches; see Validation).

## Behaviour changes (all outside the Tas values, which are unchanged)

- **OpenStudio results in DesignExplorer were 1000× too small** (DesignExplorer divided every result by 1000); they are now
  read as kWh/kW. This is a correction, visible to anyone who exported OpenStudio results to DesignExplorer.
- A result whose units are unknown (no SAM-BIM writer has ever produced one) now gives empty DesignExplorer cells
  instead of a ÷ 1000 guess.
- Persisted models are **not** migrated: an old Tas result keeps its Wh/W and no marker; every consumer here reads it
  correctly. A user reading the raw parameter (generic property views, scripts) still sees Wh/W on old Tas results and
  sees kWh/kW on new ones; scripts that divided Tas values by 1000 themselves must stop for new results.

## Owner decisions

1. Marker form and names as above (`ResultUnitsVersion` = 1; `Query.TryGetConsumptionOrPeakLoad`; `Modify.SetResultUnitsVersion`).
2. Provenance by exact `Source` match on the two assembly names; nothing else is treated as Tas or OpenStudio.
3. Unknown units are refused, not guessed: DesignExplorer cells empty (alternative: `0`, the file's convention for missing
   values), Grasshopper NaN plus a warning, benchmark `available:false`. The exact ambiguous cases are: any result whose
   `Source` is not exactly one of the two names and has no marker, and any marker other than 1. No SAM-BIM writer
   produces either.
4. OpenStudio results in DesignExplorer corrected (previously 1000× too small).
5. No display note for old unmarked Tas results in generic parameter views (SAM_UI has no reader of these parameters;
   `Modify/RemoveResults` only removes them). Owner may want one later.
6. Merge order SAM → (SAM_Tas_Grasshopper, SAM_OpenStudio) → SAM_Tas, same session, CI rerun against merged Q4 between steps.

## Files changed

- `SAM/SAM.Analytical/Enums/Parameter/AnalyticalModelSimulationResultParameter.cs`: `ResultUnitsVersion` appended.
- `SAM/SAM.Analytical/Enums/ResultUnitsState.cs` (new).
- `SAM/SAM.Analytical/Classes/Result/AnalyticalModelSimulationResult.cs`: `CurrentResultUnitsVersion`, `TasSource`, `OpenStudioSource`.
- `SAM/SAM.Analytical/Query/ResultUnitsState.cs` (new): `ResultUnitsState`, `TryGetConsumptionOrPeakLoad`, `ConsumptionOrPeakLoad`.
- `SAM/SAM.Analytical/Modify/SetResultUnitsVersion.cs` (new).
- `SAM/SAM.Analytical/Convert/ToDesignExplorer/File.cs`: reads through the query.
- `SAM/SAM.Tests/ResultUnitsTests.cs` (new, 43 cases).
- `documentation/TasResultUnits-PR.md` (this record).

## Validation

- `dotnet build SAM.sln -c Release`: 0 errors. `SAM.Tests` full suite **3457/3457** (43 new). The test bin's and
  `SAM\build`'s `SAM.Analytical.dll` are the same build (SHA-256 `6e5658aa…`).
- New tests: ordinals/names pinned; state matrix (Tas/OpenStudio × marked/unmarked, unknown producers incl. near-miss
  names, unsupported markers of every type, version 1 as int/double/text); recorded #92 numbers (15 227 840.66370964 Wh
  → 15 227.84 kWh, 38 727.40625 W → 38.727 kW, 2 523 751.8729965687 Wh → 2 523.75 kWh); marked reads bit-identical to
  legacy and is not divided again; reading does not mutate; JSON round trip of a model; DesignExplorer legacy = marked =
  the pre-correction arithmetic on the recorded numbers and on a 5 000-case seeded sweep (rounded cells identical, no
  tie flips); OpenStudio with and without the marker; unknown units → empty cells, columns kept; CSV lines identical.
- Mutation: 7/7 killed (legacy not divided; marked divided; case-insensitive source; any marker accepted; DesignExplorer
  ÷ 1000 again; peak per area without × 1000; unknown producer read as stored).
- Licensed confirmation (with SAM_Tas's converter, on copies; `C:\TasOut\units-fix`, local): see the SAM_Tas record.
- Downstream on this build: SAM_Validation benchmark 77/77 and comparator 87/87 (no change needed there).
- PR CI (`build`, `test`, `spdx`): see the PR.

## Unresolved issues and risks

- The merge order above is the safety; merging SAM_Tas early breaks Grasshopper values (or SAM_Tas's build).
- Old Tas results stay Wh/W on disk by design; raw readers outside this set must use the query.

## Next step

Owner review of the four PRs. After approval: merge SAM, rerun SAM_Tas_Grasshopper and SAM_OpenStudio CI against merged
Q4, merge them, rerun SAM_Tas CI, merge SAM_Tas; closeouts on each `sow/2026-Q4` (docs-only, `[skip ci]`).
