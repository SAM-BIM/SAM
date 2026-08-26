<!-- SPDX-License-Identifier: LGPL-3.0-or-later -->
<!-- Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors -->

# Part O / TAS validation

**This is an engineering validation record, not a diary.** It states what has been validated against real
TAS output, what that validation proves, which defects it exposed, and what remains unvalidated. The
chronological narratives it was consolidated from are preserved verbatim in
[`PartF-HANDOVER-ARCHIVE.md`](PartF-HANDOVER-ARCHIVE.md) (archived §11e, §11o, §11t, §11u, §11v).

Companion documents: [`PartF-HANDOVER.md`](PartF-HANDOVER.md) for current repository state and next work;
[`PartF-ADF-Volume1-2021-Traceability.md`](PartF-ADF-Volume1-2021-Traceability.md) for Approved Document F
regulatory and calculation traceability.

---

## Purpose and scope

**The question this record answers:** when SAM assesses a real TAS simulation against CIBSE TM59, does it
produce the same numbers TAS's own Domestic Overheating report produces for the same `.tsd`, and does each
result reach the space it belongs to?

**What is deliberately NOT claimed anywhere in this document.**

- **A TM59 pass is not Part O compliance.** Passing temperatures do not establish that every Approved
  Document O modelling assumption behind the simulation was applied. The report layer states
  `TM59 occupied-space assessment: PASS`, never "Part O compliant", on purpose.
- **Agreement with TAS is not correctness.** It establishes that two independent implementations of the
  TM59 arithmetic agree on the same input series. Where SAM deliberately assesses something TAS's canned
  report does not, agreement is not available and is not expected — see
  [Known scope/classification differences](#known-scopeclassification-differences).
- **One flat is not a model set.** See [Remaining validation work](#remaining-validation-work).

### Validation assets

| | |
|---|---|
| TAS | **9.5.7.0**, `C:\Program Files\Environmental Design Solutions Ltd\Tas` |
| Model | `SAM_zoningAM_v2zonesisDomestic.sam` — Zone Category `Flats` → Flat 1 / Flat 2 / Flat 3 (`IsDwelling` true) + Corridor (`false`), **9 spaces**. The canonical fixture shape: three dwellings each containing a room named exactly `Bedroom 2`, plus a communal corridor. |
| Weather | DSY, `C:\Users\Public\Documents\Tas Data\Databases\CIBSE Weather 2021.twd`, readable via `SAM.Weather.Tas.Convert.ToSAM_WeatherDatas(path)` |
| Flat1 BasePassive evidence | `…\SAM_daily\2026-07-15 PartO\Simulation\` — SAM diagnostic logs `Flat1.BasePassive.partO.20260818-122737.jsonl` (run `c4aa16f2-990c-41b6-ae8f-c78eedd14294`) and `…-134420.jsonl`, alongside TAS's own `Domestic Overheating (CIBSE TM59).xlsx` |
| Earlier full run | `…\SAM_daily\2027-08-03-HVAC\` — `000000_SAM_AnalyticalModel.{tbd,tsd,json}`, `.timing.csv` (78 s), `Report XMLs\…DomOv.xml`. **Also the only real TPD on this machine**, and therefore the asset the TPD identity question must be settled against. |

Both comparisons below were made by parsing the real evidence files directly — the `.xlsx` via its raw
OOXML, the JSONL via its own space records — not from any prose summary of them.

---

## Validation routes

Three routes reach TM59. **Preparation differs; assessment does not** — all three converge on the
engine-neutral `TM59AssessmentCalculator`, which never sees the words TSD, TPD or TAS.

```
TSD-simple:  TSD ─────────────────────────────────────────────────► model ─┐
TPD-full:    TPD ─► pass 1 ─► TBD COPY ─► pass 2 ─► TSD ─────────► model ─┼─► TM59AssessmentCalculator
TPD-approx:  TPD + companion TSD ─► synthesised (MRT + ZT) / 2 ──► model ─┘
```

| Route | Validated against real TAS output? |
|---|---|
| **TSD-simple** | **Yes** — Flat1 BasePassive, one `.tsd`, space by space. This is the whole of the evidence below. |
| **TPD-full** | **No.** Its intended transfer is refused rather than approximated — see [TPD/TBD route limitation](#tpdtbd-route-limitation). |
| **TPD-approx** | **No.** Unit-tested only, and it carries an unresolved identity risk (same section). |

---

## Flat1 BasePassive validation

### Simulation identity

**Result: identity resolves by TAS zone guid on every space, and nothing was silently dropped.**

Read one by one from the per-space records of `Flat1.BasePassive.partO.20260818-122737.jsonl`, not only
from the aggregate `run` record:

- `identityMode: "zoneGuid"` on **all 9** spaces;
- `simulatedZoneGuidRaw` populated on all 9 and **equal to `designZoneGuidRaw`** on every one — the same
  string including braces and casing (e.g. `{04860158-BDB1-4295-9EC6-DE80B409774D}` both sides), so the
  format-mismatch risk that would have flipped the run from "9 resolved by unique name" to "9 unresolved"
  did not materialise;
- `simulationSpaceMapIsComplete: true`, `overheatingScenarioMapIsComplete: true`,
  **`unassociatedCount: 0`**, `refusalCount: 0`;
- `workflowSuccessful: true`, `tM59Successful: true`.

**Why this matters more than it looks.** `SimulationSpaceMap` resolves stable key first, unique name as
fallback, and refuses on ambiguity. Three flats each holding a room named exactly `Bedroom 2` is the shape
the unique-name fallback structurally cannot serve, so "the fallback was succeeding" was never good enough
— identity had to be shown resolving on the key itself. It now does.

**Two identity caveats that this evidence does NOT cover.**

1. **The design side is still name-assigned.** `Modify.UpdateIds` strips `SpaceParameter.ZoneGuid` from
   every space before the loop that would read it, so the TBD zone is found by **name** and `zone.GUID` is
   then stamped. The diagnostic log records this honestly as
   `zoneGuidProvenance: "assignedDuringWorkflow"`. A `zoneGuid` identity mode is real — both sides match
   on the key by the time anything reads it — but it does not prove the chain one layer up was ever free
   of a name match. Whether to preserve the guid through the strip or refuse an ambiguous name match is an
   open decision, recorded in the active handover.
2. **This is TSD evidence only.** Nothing here says anything about the TPD route's identity.

### SAM vs native TAS TM59 comparison

Michal reran Flat1 BasePassive in TAS itself and pulled the native "Domestic Overheating (CIBSE TM59)"
report for the same `.tsd` the SAM diagnostic run was built from. Compared space by space.

**8 of 9 spaces agree with TAS on every number that governs pass/fail.** The 9th, `Corridor_1`, disagrees
by design and not by defect — see [Known scope/classification differences](#known-scopeclassification-differences).

| Space | Criterion | TAS | SAM | Agreement |
|---|---|---|---|---|
| `Bedroom 2_3`, `Bedroom 2_6` | mechanical | occupied / max / exceeding-26 / Pass | identical | exact |
| `Kitchen_4`, `Kitchen_7` | mechanical | occupied / max / exceeding-26 / Pass | identical | exact |
| `Studio 1_0` | naturalBedroom | day 37; night 3285 / 32 / 11; Pass | identical on all four | exact |
| `Ensuite_5`, `Ensuite_8` | corridor-style | 0 / 0 / 0, Pass | 0 hours >28 °C, Pass | exact |
| `Bathroom_2` | corridor-style | 0 / 0 / 0, Pass | 2 hours >28 °C, Pass | scope difference, investigated, **not a defect** |
| `Corridor_1` | corridor-style | 0 / 0 / 0, Pass (TAS "Other" bucket) | 337 vs 262, SignificantRisk | **by design** |

### Occupied-space criteria

The report layer's own rendered text was then rebuilt from the real numbers and compared against TAS
figure by figure. **Every applicable occupied-space TM59 criterion matches the TAS native report exactly:**

| Space | Check | TAS | SAM `report` |
|---|---|---|---|
| `Studio 1_0` | Criterion 1 | 37 / 110 | 37 / 110, margin +73 |
| `Studio 1_0` | Criterion 2 | 11 / 32 | 11 / 32, margin +21 |
| `Bedroom 2_3` | >26 °C | 131 / 262 | 131 / 262, margin +131 |
| `Kitchen_4` | >26 °C | 135 / 142 | 135 / 142, margin +7 |
| `Bedroom 2_6` | >26 °C | 129 / 262 | 129 / 262, margin +133 |
| `Kitchen_7` | >26 °C | 129 / 142 | 129 / 142, margin +13 |

`OCCUPIED SPACE ASSESSMENT: PASS` throughout — **including** with `Corridor_1` at `SignificantRisk`, which
is asserted behaviourally by `ASignificantRiskCorridor_DoesNotFailTheOccupiedSpaceAssessment` and is a
deliberate separation of two concepts: `TM59ComplianceStatus` (Pass / Fail / NotApplicable) and
`TM59RiskStatus` (Acceptable / SignificantRisk).

**Association was complete on this run.** All 9 assessment spaces produced a result, so the report's
"SPACES NOT ASSESSED" section correctly reads "Every space the assessment covered produced a result".
The mechanism for a real gap — a space present but unresolved — is unit-tested, because this run does not
exercise it live.

### Full-year >28 °C / corridor-style results

SAM evaluates an occupancy-independent full-year >28 °C check on spaces that carry no occupied TM59
criterion. The distinguishing tell is the limit: `MaxExceedableHours` is overridden to
`8760 × 3% ≈ 262`, not `OccupiedHours × 3%` as the base type computes it for occupied criteria.

Real figures on this run: `Bathroom_2` 2 / 262, `Ensuite_5` and `Ensuite_8` 0 / 262 each, `Corridor_1`
337 / 262 (margin −75, `RiskStatus = SignificantRisk`).

**`Corridor_1` exceeding its threshold does not fail the occupied-space TM59 assessment**, and the report
does not claim it does.

### Known scope/classification differences

These are differences in **what is assessed**, not disagreements about arithmetic. None is a defect and
none was "fixed" to match TAS.

1. **TAS's canned report has no communal-corridor criterion.** A zero-occupancy circulation space is
   bucketed "Other" and reported 0/0/0/Pass — reporting "not run", not "0 measured". SAM deliberately runs
   its own criterion there. `Corridor_1`'s disagreement is entirely this.
2. **SAM's ancillary check is occupancy-independent, by design.**
   `TM59CorridorExtendedResult.GetHoursNumberExceeding28()` scans the whole 8760-hour year and is not
   gated on occupancy — confirmed deliberate by the `MaxExceedableHours` override above. That is why
   `Bathroom_2` (0 occupied hours) logs 2 hours >28 °C where TAS shows 0. Traced, confirmed apples-to-
   oranges, **no code change**: the occupancy-independent check is arguably the more conservative
   behaviour, not a bug to match TAS's silence on.
3. **"Corridor" is the catch-all bucket at the calculation level — resolved at the report level by
   InternalCondition.** `TMOverheatingCalculator.Calculate_TM59` still routes a space to `CorridorResults`
   when its `TM59Manager.TM59SpaceApplications` comes back empty **or** when its stated strategy is `UV` —
   which `SAM_Systems`' capability index describes as "Unconditioned. Provides nothing," not "communal
   corridor" — so the calculation-level bucket still catches a bathroom the same way it catches an actual
   corridor. **What changed:** `TM59AssessmentReport` now partitions that bucket itself, matching each
   result's space (by `Reference`/Guid, never by name) against its restored/resolved `InternalCondition`.
   Only `TM59_Communal Corridor (including pipework gains)` —
   `TM59InternalConditionResolver.CommunalCorridorInternalConditionName` — is reported under
   `COMMUNAL CORRIDOR RISK` and counted towards `CorridorRiskStatus`; everything else in the bucket (a
   bathroom, hall, ensuite, or any space with no `InternalCondition` to check) is reported under
   `SUPPLEMENTARY >28 C CHECKS` instead — real engineering information that never contributes to
   `CorridorRiskStatus` and is never presented as a mandatory communal-corridor criterion. `Corridor_1` in
   this validation run carries exactly that condition, so it is the one row `COMMUNAL CORRIDOR RISK`
   shows; `Bathroom_2` carries `TM59_Bathroom` and reports under `SUPPLEMENTARY >28 C CHECKS` instead. The
   old `FULL-YEAR >28 C / CORRIDOR-STYLE RESULTS` heading and its ambiguity disclaimer are retired along
   with the ambiguity they existed to flag.
4. **`Kitchen_4` / `Kitchen_7` reported `TM59 Application: Sleeping, Cooking`, not `Cooking` alone — traced
   to a token collision and FIXED.** Both are multi-bedroom-apartment kitchens, so
   `TM59InternalConditionResolver` names their `InternalCondition` `"1 Bed Apt. Kitchen"` — the apartment's
   bedroom **count** is part of the condition's own name. `TM59Manager.TM59SpaceApplications
   (InternalCondition, TextMap)` classified that whole name through `TextMap.GetSortedKeys`, which checks
   it word by word against every TM59 keyword — and "Bed" is, independently, one of the literal "Sleeping"
   keywords. The same token that stated the apartment's bedroom count was read as evidence that *this* room
   is used for sleeping.

   **The fix.** `TM59Manager.RoleMatchName` strips a leading `"N Bed Apt."` qualifier from an
   InternalCondition's name before it is matched against the Sleeping/Living/Cooking keyword lists —
   applied only to the InternalCondition-based `IsSleeping`/`IsLiving`/`IsCooking` overloads (and therefore
   `TM59SpaceApplications(InternalCondition, TextMap)`, which calls them). Space-name classification
   (`IsSleeping(Space, TextMap)` and its siblings) is untouched, so `SAM_Tas`'s `RoomUse.cs`, `ToSAP.cs` and
   the legacy `OverheatingCalculator.cs` — which call the same InternalCondition-based overloads directly —
   now also classify a multi-bedroom apartment kitchen as `Cooking` alone, correctly. `Kitchen_4`/`Kitchen_7`
   now report `TM59 Application: Cooking`. Pinned by `TM59SpaceApplicationClassificationTests` (`SAM.Tests`),
   including that a plain `"Double Bedroom"`/`"Single Bedroom"` condition (no apartment-size prefix) and raw
   Space-name classification are both unaffected.

   **The mechanical `>26 °C` numbers are unchanged** — `Sleeping` only ever affected which
   natural-ventilation result type a space would be routed to, never the mechanical criterion Kitchen_4/
   Kitchen_7 are actually assessed under, and the "SAM vs native TAS TM59 comparison" table above still
   matches TAS exactly (135/142 and 129/142, unchanged).

   **CLOSED — the related "room"/"bedroom" substring collision.** `GetSortedKeys` (the primitive the fix
   above still delegated to) does `value.Contains(token) || token.Contains(value)`, so a bare apartment
   `"N Bed Apt. Living Room"` condition (no `/Kitchen` suffix) also misread Sleeping — "room" is its own
   token there, and `"bedroom".Contains("room")` is true. **Fixed** by routing InternalCondition role
   matching through `Query.TM59TextMapMatches` instead (`TM59Manager.IsRole`) — the same deterministic,
   whole-token/whole-phrase matcher `TM59InternalConditionResolver` already uses for Space classification,
   reused rather than special-cased for "Living Room". It requires an alias's tokens to appear as an exact,
   contiguous sequence in the name, so "room" can never match the alias "bedroom" as a substring.
   `"N Bed Apt. Living Room"` now classifies as `Living` alone, confirmed end to end through
   `TMOverheatingCalculator.Calculate_TM59` — the space is *not* routed to the bedroom result type, so
   Criterion 2 correctly reads N/A while Criterion 1 still applies
   (`TM59AssessmentCalculatorTests.ApartmentLivingRoomCondition_IsNotRoutedAsABedroom_SoCriterion2IsNotApplicable`).
   Pinned by `TM59SpaceApplicationClassificationTests`, which also confirms `"N Bed Apt. Kitchen"`,
   `"N Bed Apt. Living Room/Kitchen"`, `"Double Bedroom"`/`"Single Bedroom"`, `"Studio"` and raw Space-name
   classification are all unchanged by this second fix.

### Report structure (refined)

The verification report (`TM59AssessmentReport` / `TM59AssessmentReportFormatter`) was reworked for
presentation, auditability and correct TM59 semantics — the validated arithmetic above is unchanged:

- Heading is now `CIBSE TM59:2017 OVERHEATING ASSESSMENT`, with an explicit
  `Part O modelling assumptions: NOT VERIFIED BY THIS RESULT REPORT` line near the header — this report
  assesses simulated temperatures against TM59:2017 only; it never claims Part O compliance.
- `NATURAL VENTILATION` is one row per space (grouped by `Reference`/Guid, never by name), showing
  Criterion 1 and Criterion 2 side by side with an `Overall` verdict; a new `ASSESSMENT HOURS` table
  states the exact `Occupied Summer Hours` / `Annual Night Occupied Hours` basis each space was assessed
  against, read directly off the result (`TM59NaturalVentilationResult.SummerOccupiedHours` /
  `TM59NaturalVentilationBedroomResult.AnnualNightOccupiedHours`, or their extended-result equivalents),
  never reconstructed from a rounded `Limit`.
- `MECHANICAL VENTILATION` gained an `Internal Condition` column and an `Annual Occupied Hours` column (the
  one basis this criterion has, already on `TMResult.OccupiedHours`); the ambiguous `Use` column is now
  headed `TM59 Application`.
- The former single `FULL-YEAR >28 C / CORRIDOR-STYLE RESULTS` section is split into `COMMUNAL CORRIDOR
  RISK` (positively identified corridors only, contributing to `CorridorRiskStatus`) and
  `SUPPLEMENTARY >28 C CHECKS - INFORMATION ONLY` (everything else the same calculation produced,
  explicitly labelled as information only rather than a mandatory corridor criterion) — see item 3 above.
  Both gained an `Annual Hours` column (`TM59CorridorResult.AnnualHours`, the real annual series length,
  not `Limit` divided back through its exceedance factor).
- A new `ASSESSMENT BASIS` section states the fixed TM59:2017 assessment periods plus the genuinely
  data-derived figures: the real annual series length
  (`TM59AssessmentReport.AnnualHours`, read via the shared `TMExtendedResult.GetAnnualHours()` — never
  assumed to be 8760), and — only when that series confirms a standard 365- or 366-day year — the calendar
  "clock hours" behind the natural-ventilation study period (`1 May - 30 Sep`, always 153 days/3672 hours
  regardless of leap year, since the range never includes February) and the bedroom night-time window
  (`22:00-07:00`, 3285 hours for 365 days or 3294 for 366). A non-standard or partial series prints the
  descriptive text without a clock-hours figure, rather than a misleading one. These are calendar constants
  of the period definitions themselves, not per-space occupancy data — the space-specific `Occupied Summer
  Hours` / `Annual Night Occupied Hours` / `Annual Occupied Hours` in `ASSESSMENT HOURS` and
  `MECHANICAL VENTILATION` are unaffected and remain read directly off each result. Descriptive only; it
  recomputes nothing.
- The legend gained a `TM59 Application` block explaining `Sleeping`/`Living`/`Cooking` as assessment
  roles (a Studio carries several at once) rather than the complete architectural room type. The values
  listed are enumerated from the real `TM59SpaceApplication` enum rather than an independently typed-out
  list, so a future application added to the enum appears automatically; `Undefined` is deliberately
  excluded, since `TM59AssessmentReport`'s own `Use` method never emits it. `Actual`'s wording was adjusted
  to "hours exceeding the stated temperature/comfort threshold" — more accurate now that
  `SUPPLEMENTARY >28 C CHECKS - INFORMATION ONLY` is explicitly not a mandatory TM59 criterion. The
  assessment-period prose the legend used to carry now lives in `ASSESSMENT BASIS` instead, and the
  SAM-vs-TAS validation history that used to live there is this document.
- Weather-data identity (e.g. the DSY file the run used) was investigated for `ASSESSMENT BASIS` and left
  out: `TM59AssessmentResult`/`TM59AssessmentReport` carry no weather-data reference at all today (`Source`
  is the model/TSD path only), so stating it would mean adding new plumbing through the calculator and
  result types rather than reading something already reliably available — out of scope for this pass.

---

## TPD/TBD route limitation

**The intended TPD-full transfer cannot be performed, and is refused rather than approximated.** The
intended sequence was: read the first pass's supply air temperature and supply airflow, inject both into a
TBD copy, simulate, take `ResultantTemperature` from the second pass. The blockage is specifically on the
**write** side.

- **The read half is available and already happening.** `Convert.ToSAM_SpaceSystemResults` asks the
  `SystemZone` for every `SpaceDataType`, and that enum already contains `SupplyAirTemperature = 3` and
  `FlowRate = 1` alongside `ZoneTemperature = 9`. The supply conditions are in hand.
- **The write half has nowhere to go.** Reflected over the whole `Interop.TBD` assembly (143 types): **no
  TBD type exposes a per-zone supply air temperature of any kind** — not the zone, not its internal
  condition, not the thermostat, not the internal gain. That is by design: TBD introduces ventilation air
  at outside or adjacent-zone conditions, and conditioned supply air is a TPD concept. It is exactly why
  TAS keeps the two models apart.

  *Correcting record, kept because it shows how the conclusion was tested:* an earlier version of this
  claim said the only temperature-valued member in `Interop.TBD` is `IWeatherYear.groundTemperature`.
  **That was wrong** — the scan matched `Temperature` case-sensitively and missed every member spelled
  `Temp` (`IControls` frost-protection / authority / night-setback, `IEmitter` outside-temperature
  cut-offs, `ISurfaceOutputSpec.dryBulbTemp`). The load-bearing conclusion was re-verified member by
  member and survives: none of those is a zone supply condition.
- **Injecting the airflow alone would be worse than partial progress.** TBD's ventilation profile does
  accept an hourly series, so the airflow half is mechanically writable — but air introduced *without its
  temperature* enters at outside conditions, stating a system that does not exist. And once the thermostat
  pins the zone air temperature (which is what the supported transfer does), an injected flow moves only
  plant load and leaves `ResultantTemperature` untouched. Half of this transfer is not a fraction of the
  answer.
- **`UpdateSpaceAirflows` / `UpdateFanAirflows` are not it.** Both operate on the TPD and set scalar
  design sizing values. Neither hourly nor TBD-side.

**So `ResultantTemperatureTransfer.SupplyAirTemperatureAndAirflow` is refused with the reason stated, and
the supported transfer is `ZoneTemperatureToThermostatLimits`, named as the approximation it is.** That
approximation is not laziness: `ResultantTemperature` is a function of air and mean radiant temperature, so
pinning air temperature to the first pass's *achieved* value leaves TBD to compute only the radiant half —
the half TPD cannot give. It closes the loop; the intended transfer cannot.

**If a future TAS release exposes a per-zone supply air temperature on the TBD side, the one place that
has to change is `ResultantTemperaturePreparation.TransferRefusal`.**

### Open risk on the TPD-approx route — the two identity namespaces have never been shown to match

The approximate route correlates a space to a TPD result by matching `SpaceParameter.ZoneGuid` (mapped
from `TSD.ZoneData.zoneGUID`) against `SystemSpaceResult.Reference`. That reference comes from
`(systemZone as dynamic).GUID` — and **`ISystemZone` declares no `GUID`**, verified by reflection, so it
binds to `ISystemComponent.GUID`: **the TPD component's own guid**. Nothing establishes that a TPD
component guid equals a TBD/TSD zone guid, and the identity evidence in this document covers **TSD only**.

**If they differ**, identity mode never engages, the whole model falls to name mode, and three rooms all
called `Bedroom 2` are entirely refused — safe, but useless.

**What was done about it:** the risk is documented on the class; the fall back to names emits **one
model-wide diagnostic naming the cause** rather than N baffling per-room messages; and
`WhereTheTwoIdentityNamespacesDisagree_ItSaysSoOnceAndFallsToNames` gives the two sides deliberately
different strings so the assumption cannot be silently re-encoded in a fixture — which is what the
original tests did.

**The probable fix, NOT applied:** `IZoneLoad.GUID`. `ITSDData` exposes `GetZoneLoadForGuid(string)`, so a
zone load is addressable by the guid the TBD/TSD side uses. But `Reference` also correlates a
`SystemSpaceResult` to its `SystemSpace`/`SystemZone` in the energy-centre model and is persisted in JSON.
**Verify against the real TPD in `…\SAM_daily\2027-08-03-HVAC\` before changing it.**

### Also recorded, not fixed

`Modify.CalculateResultantTemperature` (the authoritative two-pass route) reads
`GetValues(new Range<int>(0, 8759))` **unbounded**, so a TPD simulated over part of the year yields `0.0`
for the rest, and those zeros go into **both** thermostat limit profiles of the TBD copy — the second pass
then runs against a 0 °C setpoint outside the simulated period. Pre-existing, left alone under "preserve
behaviour exactly". Note the irony: the authoritative route lacks the alignment/count protection that was
added to the approximate one.

---

## Validation defects found and fixed

Every one of these was found **by** validating against real TAS output, and each is the reason this record
exists rather than a green test suite being taken as sufficient.

| # | Defect | Fixed in | How validation exposed it |
|---|---|---|---|
| 1 | **Criterion 1's report `Limit` read the annual `MaxExceedableHours`, not `MaxExceedableSummerHours`** — the basis TAS's own report states this criterion on, paired with "Occupied Summer Hours". For `Studio 1_0` it showed `262` where TAS's real figure is `110`. | SAM `3fcfb880` | Only visible against a real report. **Every existing test happened to use `110` for both the annual and the summer value**, so the wrong field and the right one were indistinguishable by coincidence. Fixtures now carry a deliberately wrong-looking annual placeholder (`999`) so that coincidence cannot recur, plus a test pinning the real `Studio 1_0` figures. Mutation-tested: reverting fails 5 tests. |
| 2 | **`Query.Simplify` argument-order bug** in the non-bedroom natural-ventilation branch — the summer-hours pair and the annual limit were rotated by one constructor position, so every simplified (non-extended) result reported `SummerOccupiedHours` / `MaxExceedableSummerHours` under swapped meanings. Pass/Fail was unaffected (derived from `HoursExceedingComfortRange` directly). | SAM `07d1c728` | Found independently while establishing which pair TAS states its criteria on — i.e. by the same question that produced defect 1. Isolated with its own regression test, in its own commit ahead of the reporting work. |
| 3 | **Diagnostic log completeness** — `occupiedHours` / `maxExceedableHours` logged the whole-year annual pair while TAS states the day criterion against `3672` / `110`. For `Studio 1_0` the log read 8760/262 against TAS's 3672/110 **even though the exceedance count and Pass/Fail matched exactly**. Not a TM59 defect: the assessment used the right basis throughout, the log displayed a misleading pair beside it. | SAM_Tas `2750a21` | Directly, by diffing the log against TAS's report. `summerOccupiedHours` / `maxExceedableSummerHours` are now logged for `natural` / `naturalBedroom` rows, read the same way `Query.Simplify` already reads them. 2 new tests. |
| 4 | **`Convert.ToSAM(TSD.ZoneData)` never stamped `SpaceParameter.ZoneGuid`**, so `Query.SimulationSpaceKey` returned null for every simulated space and identity silently fell to the unique-name fallback. | SAM_Tas `d76be13` | Exposed by the diagnostic logging built to answer "does identity actually resolve on the key?" — the honest answer was no. Root cause is one layer down and **still open**: `SAM.Core.Create.ParameterSet` / `TypeMap` reads the source property by the **SAM-side** name and stores under the **TAS-side** name, inverted both ways, and `SpaceParameter.ZoneGuid` is registered as `"Zone Guid"` — *with a space* — against `"zoneGUID"`. The space defeats even a case-insensitive match. Repaired by one guarded explicit stamp; the shared helper deliberately untouched. |
| 5 | **A simplified bedroom result carried the night-time numbers but no verdict to report against them** — `Pass` on a TM59 result is Criterion 1 alone. `TM59NaturalVentilationBedroomResult` gained `Criterion2`. | SAM `3a386baf` | Surfaced while building a report that has to state Criterion 1 and Criterion 2 separately, because that is how TAS states them. |

**One test-coverage limit, stated plainly.** The `TsdZoneIdentityStampTests` for defect 4 do **not** call
the converter, so they will not fail if the stamp is reverted: `TSD.ZoneData` is COM interop and
`Convert.ToSAM` reaches `ActiveSetting.Setting`, whose `GetDefault()` references `typeof(TAS3D.Zone)`,
`typeof(TBD.zone)` and `typeof(TSD.ZoneData)` — exercising it needs the whole TAS interop surface loaded
inside a `net8.0` project built specifically to exclude it. Judged not worth breaking the COM-free
boundary for. **That converter line is covered by the manual acceptance run above and by nothing else.**

---

## Current confidence / limitations

**What is established.**

1. On the **TSD-simple** route, for **one** real flat at **BasePassive**, SAM's TM59 numbers match TAS's
   own Domestic Overheating report **exactly** on every occupied-space criterion the case exercises.
2. On that run, result association is by **TAS zone guid** on every space, with `unassociatedCount: 0` and
   no refusals — so no result reached the wrong room and none vanished.
3. The differences that remain are **scope and classification** differences, each traced to a named,
   deliberate design decision, not to arithmetic.
4. The validation exercise itself found **five real defects** that the test suites did not, two of which
   (defects 1 and 2) were invisible to any test that did not have a real TAS report to compare against.

**What is NOT established.**

- **Nothing about Part O compliance.** This is TM59 arithmetic and result association only.
- **Nothing about a second model.** One flat, one iteration, one `.tsd`.
- **Nothing about the plain `natural` (non-bedroom) criterion** — Flat1 exercises `mechanical`,
  `naturalBedroom` and `corridor-style` and never that one. Note that defect 2 lived in exactly that
  branch, which is the branch with the least real-output coverage.
- **Nothing about `AcousticRestricted` or `ActiveTrimCooling`.** Both are refused by
  `Query.PartOIterationOperatingMode` today, pending engineering decisions, so neither can produce numbers
  to compare.
- **Nothing about either TPD route.** No real TPD has been run through them, and the TPD-approx identity
  namespaces have never been shown to agree.
- **Nothing about the design-side identity provenance.** Still name-assigned.
- **Nothing live about the refusal path.** `unassociatedCount: 0` on every real run means the
  space-present-but-unresolved gap is unit-tested only.

---

## Natural-ventilation Part O workflow — end-to-end licensed acceptance (2026-08-25)

> **Superseded in its mechanics, not in its findings.** This run stated the strategy as the string
> `"NV"` at `PartOIteration.BasePassive`. That pairing now REFUSES: the route is explicit
> (`PartOVentilationMode`) and Iteration 1b is `BaseNaturalVentilation`. Everything this section
> establishes still holds and is re-established, in both opening cases, by the 2026-08-26 A/B
> acceptance below. Reproduce that one.

**What this section establishes:** one naturally ventilated dwelling can now be carried from an authored SAM
model to a TM59 natural-ventilation result without SAM inventing a mechanical ventilation system for it, and
with the authored opening operation reaching the TAS aperture control intact.

**What it does NOT establish.** Zero continuous mechanical supply and extract means **SAM has not invented an
MVHR/MVRE system**. It does **not** mean the dwelling's natural-ventilation Approved Document F design has
been sized: System 1 background/trickle ventilator sizing and purge provision are not calculated anywhere in
this path. Do not report this as "Part F NV sizing". A TM59 pass is still not Part O compliance, for all the
reasons stated at the top of this document.

### The defect this closes

`PartFCalculator` is unconditionally System 4 shaped. Paragraph 1.67 gives **every** habitable room a
mechanical supply terminal and every wet room a continuous extract terminal, and nothing in
`SAM.Analytical/Classes/PartF/` takes a ventilation strategy as an input.
`SAMAnalytical.PreparePartOIteration` then called `Modify.ApplyPartFVentilationRates` **unconditionally, and
before `_ventilationStrategies` was read at all**, writing those rates onto `InternalConditionParameter.
SupplyAirFlow` / `ExhaustAirFlow` — the values `Query.CalculatedSupplyAirFlow` and the TAS export read.

A naturally ventilated dwelling was therefore simulated with mechanical supply and extract it does not have,
and the run reported `successful = true`. That is worse than a refusal: nothing in the result says the system
was invented. The second failure mode was the mirror image — an NV dwelling never run through a Part F
component hit `count == 0` in `ApplyPartFVentilationRates`, got `null` back, and was refused with "run a Part
F component first" for airflow it does not need.

### The gate

`Query.PartOPartFAirflowApplication` reads the strategy the assessed zones state — the **same** dictionary
`Create.OverheatingScenarios` is given, so the airflow decision and the TM59 criterion can never read
different answers — and returns one of three answers, which `Modify.PreparePartOIteration` acts on **before**
anything is applied:

| Assessed zones | Answer | Behaviour |
|---|---|---|
| every zone states `NV` | `SkipNaturalVentilation` | nothing applied; absence of `PartFSpaceData` is not an error; a note and a warning state what was not applied and why |
| no zone states `NV` (`MV`, `MVRE`, `UV`, or no zones at all) | `Apply` | unchanged — the behaviour this preparation has always had |
| `NV` beside anything else, silence included | `RefuseMixed` | **no model returned**, no scenarios stated, every zone named beside its strategy |

Only `NV` skips. `UV` is not mechanical either, but it selects the corridor criterion rather than the
natural-ventilation one and has never been the subject of this gate, so it keeps the existing behaviour.

The mixed case refuses rather than warning — unlike the opening-restriction disagreement, which only
mislabels which assumption a true result was obtained under. Here continuing would put continuous mechanical
supply and extract into the naturally ventilated half of the building, or strip the mechanically ventilated
half of the rates it is sized for. `ApplyPartFVentilationRates` is whole-model, so a per-zone application is
the real fix and is a separate change.

### Acceptance run

| | |
|---|---|
| Base model | `C:\TasOut\v40\A0.sam` — the 9-space TM59 residential model (Flat 1 / Flat 2 / Flat 3 / Corridor, 20 apertures each already carrying an **Unrestricted** `PartOOpeningProperties`) |
| Authored | Part F sized with the shipped rule set (8 of 9 spaces sized), then **one** aperture — `SIM_EXT_GLZ` on `Bedroom 2_3`, guid `956264f2` — re-authored `NightClosed`, hours 08–23, exactly as `SAMAnalytical.AddOpeningPropertiesByPartO` builds it. The other 19 openings are left Unrestricted and act as the control. |
| Prepared | `Modify.PreparePartOIteration(BasePassive, all zones, "NV")` — the production path |
| Weather | `C:\Users\Public\Documents\Tas Data\Databases\CIBSE Weather 2021.twd` |
| Run | `AnalyticalModel -> ToGbXML -> WorkflowCalculator`, `Sizing = true`, `Simulate = true`, days 1..365 |
| Produced | `nv.tbd` (414 KB), `nv.tsd` (4.8 MB) |
| Control | the identical model prepared as `MVRE`, sizing only, into `mv.tbd` |

**`build_tests/Fixtures/original_v1.sam` was examined and rejected as the fixture.** It is a 3-storey,
5-per-floor office massing: 15 spaces named `Floor_0_Zone_NORTH` …, **0 `Zone` objects, 0 `InternalCondition`
objects, and 0 of its 66 apertures carry any `OpeningProperties`**. It can state neither a dwelling, nor a
ventilation strategy, nor a restriction without being rebuilt rather than adapted, and it sits in a build
**output** directory rather than a curated fixture library.

#### Preparation, before TAS

```
PREPARE|airflowApplication=SkipNaturalVentilation|successful=True|openingCompatibility=Incompatible|scenarios=4
SCENARIO|Flat 1 - BasePassive - NV|strategy=NV|scope=Dwelling
SCENARIO|Corridor - BasePassive - NV|strategy=NV|scope=CommonSpace
SCENARIO|Flat 2 - BasePassive - NV|strategy=NV|scope=Dwelling
SCENARIO|Flat 3 - BasePassive - NV|strategy=NV|scope=Dwelling
APERTURE|956264f2-…|restriction=NightClosed|from=8|to=23|schedule=PartO_DayOpen_08_23|values=000000001111111111111110
```

Every space came out with `supplyAirFlowStated=False`, `exhaustAirFlowStated=False`. The Part F sizing it
declined to use is still on the model — `Bedroom 2_3` carries `continuousSupply_lps=45.5`, `Studio 1_0`
`32.5 / 44`, and so on for all eight sized spaces — so this is a decision not to apply it, not an absence of
anything to apply.

Prepared as `MVRE` instead, the same model gives `airflowApplication=Apply` and eight spaces with
per-space cloned internal conditions carrying the Part F rates (`Bedroom 2_3`: `calculatedSupply_m3s=0.0455`).

#### The opening profile in the produced TBD

Read back through Tas's own 0-based accessors, independently of the code that wrote it:

```
APERTURETYPE|element=Windows: SIM_EXT_GLZ_E67C225E -pane|name=Opening Cd0.411 F1 S00FFFE|Cd=0.410591
             |profileType=ticFunctionProfile
             |function=zdwno,0,19.00,21.00,99.00
             |factor=1
             |schedule=PartO_DayOpen_08_23
             |values=000000001111111111111110
```

The two other shared aperture definitions in the same TBD carry the same function and **no schedule** — the
19 Unrestricted openings — so the schedule reached exactly the one opening that asked for it. The workflow's
own note agrees: *"1 opening(s) requested an availability schedule, 1 of those read a schedule back off the
TBD profile."*

The 24 values are read SAM-hour-ordered (SAM hour `h` is TBD slot `h + 1`, because TBD's 24-slot hourly
indexed properties are 1-based hour-**ending**), so the string is directly comparable with
`DailyAvailabilitySchedule.ValuesText`.

#### No continuous mechanical airflow in the produced TBD

`SAMZoneMetadata` is written into the TBD zone description, because TAS has no field for SAM's airflow
decomposition. That is where "was a Part F continuous mechanical supply carried onto this zone" is readable
straight out of the file. Same zone, same model, two preparations:

| | NV run (`nv.tbd`) | MVRE control (`mv.tbd`) |
|---|---|---|
| `Bedroom 2_3` description | `{"ventilation":{"profile":false},"native":{"freshAirRate":8}}` | `{"ventilation":{"flow":0.0455,"flowPerArea":0,"flowPerPerson":0,"airChangesPerHour":0,"profile":false},"native":{"freshAirRate":0}}` |
| every zone `freshAirRate` | `8` l/s/p — the authored value, untouched | `0` — cleared by the Part F application (only the unsized `Corridor_1` kept its `8`) |

In the NV run the zone carries **no supply-air statement at all**. In the control it carries the Part F
0.0455 m³/s and its authored per-person outside-air rate has been zeroed.

`ticV.factor` is `1` ACH in both, and is not evidence either way: `"profile":false` — no SAM Ventilation
profile is assigned on this model, so TBD Building Simulator mechanical ventilation is not activated in
either run and `Query.VentilationAirChangesPerHour`'s value is never written. See
`Modify.UpdateInternalCondition` for why the presence of airflow data must not switch it on by itself.

#### TM59, from the produced TSD

`Tas.TSDQueryTM59Results`' own sequence — `Convert.ToSAM(tsd)`, `Create.TM59AssessmentCalculator`,
`OverheatingScenarioMap` → `VentilationStrategyMap`, `RestoreDesignInternalConditions`, `Calculate`:

```
COUNTS|naturalVentilation=5|mechanicalVentilation=0|corridor=4
NATURALVENTILATION|Studio 1_0        |type=TM59NaturalVentilationBedroomExtendedResult
NATURALVENTILATION|Bedroom 2_3       |type=TM59NaturalVentilationBedroomExtendedResult
NATURALVENTILATION|Living Kitchen_4  |type=TM59NaturalVentilationExtendedResult
NATURALVENTILATION|Bedroom 2_6       |type=TM59NaturalVentilationBedroomExtendedResult
NATURALVENTILATION|Kitchen_7         |type=TM59NaturalVentilationExtendedResult
CORRIDOR|Corridor_1, Bathroom_2, Ensuite_5, Ensuite_8|type=TM59CorridorExtendedResult
```

No ventilation-strategy refusals, no association refusals. **Zero mechanical results** — the NV route was
taken for every occupied space. The four corridor-criterion results are the communal corridor plus the three
wet rooms, which carry no TM59 space application; that is the documented pre-existing classification, not
something this change introduced. Pass/fail is not the point and is not claimed as compliance: what is
proven is that the correct NV model, with the correct opening operation, was simulated and assessed through
the natural-ventilation route.

**One trap worth recording.** The design model handed to the TM59 assessment must be the **workflow output**
model, not the pre-workflow one. `SimulationSpaceMap` resolves on the `ZoneGuid` that `Modify.UpdateIds`
stamps during the workflow; passing the pre-workflow model produced *"does not resolve to exactly one
simulated space"* for every space and zero results. Pre-existing identity behaviour, unchanged here.

### What is still not covered

- **Mixed NV + mechanical models refuse.** Per-zone airflow application is not implemented; a building with
  naturally and mechanically ventilated zones side by side must be prepared as separate iterations.
- **System 1 sizing.** Background/trickle ventilator and purge provision for a naturally ventilated dwelling
  are not calculated. `PartFCalculator` remains System 4 shaped for every strategy.
- **Wet-room intermittent extract.** Untouched, and confirmed already outside the balanced continuous totals
  (`PartFCalculator` sets `IsInBalancedFlow = false` and leaves `ContinuousDesignFlowRate_Lps` null for
  `CookerHoodExtractingOutside` and `SeparateIntermittentExtract`), so it was never part of what is applied.
- **`OverheatingScenario:v2`.** The stage still asserts `Openings Restricted`, so a NightClosed opening under
  `BasePassive` is still reported as `Incompatible` and still only warned about. Deliberately deferred.

---

## Iteration 1b / Base Natural Ventilation — licensed A/B acceptance (2026-08-26)

**The milestone:** *Iteration 1b / Base Natural Ventilation is proven end to end from an explicitly
prepared SAM dwelling, through authored opening behaviour, TAS simulation and comparable Part O / TM59
results, without inventing an MVHR system.*

The 2026-08-25 acceptance below proved one NV case. This one proves **two cases off one dwelling**, so a
result difference is attributable to the authored opening availability and to nothing else — and it runs
them through the explicit `PartOVentilationMode` route and `PartOIteration.BaseNaturalVentilation` rather
than the string gate and `BasePassive`. See [`PartO-ARCHITECTURE.md`](PartO-ARCHITECTURE.md) for the design
this validates.

### The two cases

| | NV-OPEN | NV-NIGHT |
|---|---|---|
| Base model | `C:\TasOut\v40\A0.sam` — 9-space TM59 residential, Flat 1/2/3 + Corridor | same |
| Part O route (stated) | `NV` → `PartOVentilationMode.NaturalVentilation` | same |
| Iteration | `BaseNaturalVentilation` | same |
| Authored opening | `Unrestricted` | `NightClosed`, openingHour 08, closingHour 23 |
| Aperture authored on | `SIM_EXT_GLZ` of `Bedroom 2_3`, guid `956264f2-…` | same aperture, same guid |
| Weather | `CIBSE Weather 2021.twd` | same |
| Run | `Sizing = true`, `Simulate = true`, days 1..365 | same |

Both cases are authored through **one code path** — the same `PartOOpeningProperties` constructor on the
same aperture, given the same 08/23 hours, differing only in the `OpeningRestriction`. Authoring one case
and leaving the other alone would have made this a comparison of two authoring routes rather than of two
opening availabilities.

Harness: `C:\TasOut\inv`, modes `partoauthor <in> <out> <report> NV "Bedroom 2_3" <OPEN|NIGHT>` →
`togbxml` → `workflowsim` → `partodump` → `partotm59` → `tsdcompare`. Outputs in `C:\TasOut\p1b`.

### Preparation, before TAS

Both cases:

```text
PREPARE|ventilationMode=NaturalVentilation|airflowApplication=SkipNaturalVentilation|successful=True|scenarios=4
SCENARIO|Flat 1 - BaseNaturalVentilation - NV   |scope=Dwelling   |key=6dfe14cb-6cf3-8798-a61a-d337dbc40878
SCENARIO|Corridor - BaseNaturalVentilation - NV |scope=CommonSpace|key=37242d48-94e8-8f66-9949-1746c7e5de04
SCENARIO|Flat 2 - BaseNaturalVentilation - NV   |scope=Dwelling   |key=1d7b7a5b-ca5b-8830-8a15-1904b8b99463
SCENARIO|Flat 3 - BaseNaturalVentilation - NV   |scope=Dwelling   |key=ae358ba0-3ad0-8637-a233-6bd199998654
```

Real Part F sizing ran first and sized 8 spaces on both, so there **was** something a mechanical
preparation would have applied. `SkipNaturalVentilation` is a decision not to use it, not an absence of
anything to use.

Diffing the two authoring reports gives exactly four differences, all of them the one aperture and its
consequences:

- `openingCompatibility` `Compatible` → `Incompatible`;
- two `NOTE` lines and one `WARNING` reporting that 1 of the model's 20 operable openings restricts while
  the stage assumes unrestricted operation — **reported, nothing changed**;
- `APERTURE|956264f2-…|restriction=Unrestricted|schedule=-` →
  `restriction=NightClosed|from=8|to=23|schedule=PartO_DayOpen_08_23|values=000000001111111111111110`.

Every Part F rate, every space's airflow state and all four scenario keys are byte-identical. **The
scenario keys being equal is correct, not an oversight**: opening behaviour is a property of the model, not
of the stage, so both cases are the same assessment of the same zones at the same iteration.

### Geometry reaching TAS

Both gbXML exports are 303 200 bytes and identical apart from the per-run `BuildingStorey` GUID and the
export timestamp. Opening availability does not travel through gbXML — it is applied to the TBD afterwards
by `Modify.SetApertureType` — so identical geometry is the expected result and confirms the two runs
simulate the same building.

### The opening profile in the produced TBDs

**NV-OPEN** — 2 shared aperture types across the model's 20 openings, neither carrying an availability
schedule:

```text
APERTURETYPE|element=Windows: SIM_EXT_GLZ -pane          |Cd=0.410591|profileType=ticFunctionProfile|function=zdwno,0,19.00,21.00,99.00|factor=1|schedule=-|values=-
APERTURETYPE|element=Windows: SIM_EXT_GLZ_AAF00869 -pane |Cd=0.477327|profileType=ticFunctionProfile|function=zdwno,0,19.00,21.00,99.00|factor=1|schedule=-|values=-
```

**NV-NIGHT** — the same two, plus one:

```text
APERTURETYPE|element=Windows: SIM_EXT_GLZ_E67C225E -pane |Cd=0.410591|profileType=ticFunctionProfile|function=zdwno,0,19.00,21.00,99.00|factor=1|schedule=PartO_DayOpen_08_23|values=000000001111111111111110
```

which is the required shape exactly: `profileType = ticFunctionProfile`,
`function = zdwno,0,19.00,21.00,99.00`, `schedule = PartO_DayOpen_08_23`,
`values = 000000001111111111111110`. Read out of the TBD through Tas's own 0-based accessors, so it
observes the file independently of the code that wrote it.

**The two TBD dumps differ by exactly that one line.** Every zone, internal condition, fresh-air rate,
ventilation profile factor and zone description is identical.

### No invented continuous MVHR supply or extract, in either case

The evidence a Part F continuous mechanical supply was carried onto a zone is the `SAMZoneMetadata`
decomposition in the TBD zone description, because TAS has no field of its own for it:

```text
NV-OPEN  / NV-NIGHT, every zone:
   DESC|[LevelName]=Level 0; [SAM_META_V1]={"ventilation":{"profile":false},"native":{"freshAirRate":8}}

CONTROL — the MVRE run of the same model (2026-08-25):
   DESC|[LevelName]=Level 0; [SAM_META_V1]={"ventilation":{"flow":0.0455,"flowPerArea":0,"flowPerPerson":0,"airChangesPerHour":0,"profile":false},"native":{"freshAirRate":0}}
```

Count of `"flow"` keys: **NV-OPEN 0, NV-NIGHT 0, MVRE control 8.** No `SupplyAirFlow`,
`SupplyAirFlowPerArea`, `SupplyAirFlowPerPerson` or `SupplyAirChangesPerHour` was stated on any zone in
either NV case.

`freshAirRate = 8` l/s/person is the **model's own authored TM59 outside-air rate**, preserved untouched —
and the control shows what applying the Part F rates does to it: the mechanical run drives it to 0 and
replaces it with `flow = 0.0455 m³/s`. Both NV cases keep the authored value.

Continuous mechanical extract: `InternalConditionParameter.ExhaustAirFlow` was never stated on any space
(`exhaustAirFlowStated=False` for all nine, both cases), and in any event `SAM.Analytical.Tas` has no TBD
write path for exhaust at all — see [`PartO-ARCHITECTURE.md`](PartO-ARCHITECTURE.md) §6.

### The two simulations genuinely differ

The TM59 verdicts agree (below), so this had to be checked rather than assumed: *"the two cases agree"* and
*"the opening schedule did nothing"* are very different findings. `tsdcompare` reads the same
resultant-temperature series the TM59 assessment reads, out of both TSDs:

| Space | Differing hours / 8760 | Max delta (K) | Mean open → night (°C) |
|---|---:|---:|---|
| **Bedroom 2_3** ← the authored space | 2 334 | **0.674** | 18.9723 → 18.9736 |
| Studio 1_0 | 4 633 | 0.142 | 18.8813 → 18.8813 |
| Kitchen_7 | 643 | 0.121 | 18.6995 → 18.6996 |
| Living Kitchen_4 | 2 137 | 0.069 | 19.0336 → 19.0340 |
| Bathroom_2 | 4 550 | 0.056 | 19.2985 → 19.2987 |
| Bedroom 2_6 | 443 | 0.040 | 18.9433 → 18.9433 |
| Ensuite_5 | 1 335 | 0.012 | 19.3607 → 19.3610 |
| Corridor_1 | 385 | 0.008 | 22.4341 → 22.4342 |
| Ensuite_8 | 230 | 0.006 | 19.2725 → 19.2726 |
| **Total** | **16 690 / 78 840** | | |

The largest single-hour difference in the whole model, by a factor of nearly five, is on **the one space
whose window was restricted**, and the night case is very slightly warmer there — the expected direction
for losing night purge. Every other space shows a small knock-on through inter-zone air movement. The
effect is small because only 1 of the flat's 20 openings was restricted and the `zdwno` function opens a
window only above 19–21 °C in any case.

### TM59 results

Both cases, from their own TSD, through `Tas.TSDQueryTM59Results`' sequence:

```text
COUNTS|naturalVentilation=5|mechanicalVentilation=0|corridor=4
```

No ventilation-strategy refusals, no association refusals, no map refusals, **zero mechanical results** —
the NV route was taken for every occupied space in both cases.

| | NV-OPEN | NV-NIGHT |
|---|---|---|
| Ventilation route | NaturalVentilation | NaturalVentilation |
| Iteration | BaseNaturalVentilation | BaseNaturalVentilation |
| Opening restriction | Unrestricted | NightClosed 08–23 |
| Mechanical supply | 0 | 0 |
| Continuous MVHR extract | 0 | 0 |
| NV results / mechanical / corridor | 5 / 0 / 4 | 5 / 0 / 4 |
| Bedrooms assessed (`…BedroomExtendedResult`) | Studio 1_0, Bedroom 2_3, Bedroom 2_6 | same three |
| Living spaces (`TM59NaturalVentilationExtendedResult`) | Living Kitchen_4, Kitchen_7 | same two |
| Corridor-criterion spaces | Corridor_1, Bathroom_2, Ensuite_5, Ensuite_8 | same four |
| Outcome | every space passes | every space passes |

Key metrics, `OccupiedHoursExceedingComfortRange` against `MaxExceedableHours` (criterion 1) and
`NightHoursNumberExceeding26` against `AnnualMaxExceedableNightHours` (criterion 2):

| Space | Criterion 1, open | Criterion 1, night | Criterion 2, open | Criterion 2, night |
|---|---|---|---|---|
| Studio 1_0 | 4 / 262 | 4 / 262 | 0 / 32 | 0 / 32 |
| Bedroom 2_3 | 0 / 262 | 0 / 262 | 0 / 32 | 0 / 32 |
| Bedroom 2_6 | 0 / 262 | 0 / 262 | 0 / 32 | 0 / 32 |
| Living Kitchen_4 | 2 / 142 | 2 / 142 | n/a | n/a |
| Kitchen_7 | 0 / 142 | 0 / 142 | n/a | n/a |

**The two cases produce the same verdicts, and that is a truthful result rather than a null one.** This
dwelling is nowhere near its comfort limits under Leeds TRY — 0–4 exceeding hours against allowances of
110–262 — so a 0.67 K night-time change on one room does not move any criterion. No expected numerical
delta was manufactured; what is claimed is what the acceptance was defined to claim: the two simulations
are identical except for the authored opening availability, both are assessed as Natural Ventilation, and
the result pipeline returns truthful comparable results for both.

### What changed between the two cases, in full

1. The `OpeningRestriction` on one aperture, and the `PartO_DayOpen_08_23` schedule derived from it.
2. The opening-compatibility report about (1) — a warning, acted on by nothing.
3. One additional TBD `ApertureType` carrying that schedule.
4. Consequently, the simulated hourly temperatures.

Nothing else. Same base model, same Part F sizing, same route, same iteration, same scenario keys, same
weather, same simulation period, same geometry, same internal conditions, same zone ventilation.

### What this acceptance does not cover

- **Iteration 1a (`BaseMVHR`) equipment selection.** The Part F requirement is applied on the MVHR route,
  but no physical MVHR unit is selected against it. Not implemented.
- **Iterations 2 and 3.** Acoustic restriction, summer bypass, boost, active cooling, manufacturer supply
  temperature and larger-unit selection are recorded in the architecture and not implemented.
- **Mixed-route models refuse.** Per-zone airflow application is not implemented.
- **System 1 sizing.** Background/trickle ventilator and purge provision for a naturally ventilated
  dwelling are calculated nowhere. `PartFCalculator` remains System 4 shaped for every route.
- **Wet-room intermittent extract runtime behaviour.** Preserved as data, deliberately not modelled — SAM
  has the Table 1.1 rate but no operating schedule, and TAS has no exhaust write path. Architecture §6.
- **A case that actually fails.** Both cases pass comfortably on this model and this weather, so the
  result pipeline has been shown to report a pass truthfully and has not been shown to report a fail.
- **`OverheatingScenario:v2`.** The stage still asserts `Openings Restricted`, so NV-NIGHT is still
  reported `Incompatible` and still only warned about, and the two cases still share a scenario key.

---

## Remaining validation work

In rough order of value:

1. **A second real model, and specifically one containing a plain non-bedroom naturally-ventilated room.**
   Rerun and diff against its own TAS Domestic Overheating report the same way. This closes the largest
   gap and covers the least-covered criterion branch.
2. **Investigate before concluding.** If a new discrepancy appears, treat it the way the two Flat1
   findings were treated: both looked like defects on first read and one of them was not. The corridor
   "discrepancy" turned out to be intentional occupancy-independent behaviour.
3. **Settle the TPD-approx identity question against the real TPD** in `…\SAM_daily\2027-08-03-HVAC\` —
   is `SystemSpaceResult.Reference` the same string as the TBD/TSD zone guid? If not, `IZoneLoad.GUID` is
   the probable correction, with the JSON-persistence consequence checked first.
4. **Validate `AcousticRestricted` / `ActiveTrimCooling`** once their operating assumptions are confirmed.
   Until then, the only thing worth confirming on a live canvas is that
   `Query.PartOIterationOperatingMode`'s **refusal fires correctly** — which is not the same exercise as
   the BasePassive comparison and must not be mistaken for it.
5. **Exercise the unresolved-space refusal path live**, so "nothing was silently dropped" rests on
   observed behaviour rather than on unit tests plus a run that never triggered it.
6. **Decide the design-side `ZoneGuid` provenance** — preserve the guid through `Modify.UpdateIds`'s
   strip, or refuse an ambiguous name match — so the whole identity chain, not just its simulated half,
   is free of a name join.
7. **CLOSED — the "N Bed Apt." token collision and the related "room"/"bedroom" substring collision in
   `TM59Manager`'s Sleeping/Living/Cooking matching are both fixed**, for Kitchen, Living Room and Living
   Room/Kitchen apartment conditions (`TM59Manager.RoleMatchName` and `TM59Manager.IsRole`, the latter
   routing InternalCondition role matching through `Query.TM59TextMapMatches` instead of
   `TextMap.GetSortedKeys` — see [Known scope/classification differences](#known-scopeclassification-differences)
   item 4). A second real model containing a plain non-bedroom naturally-ventilated Living Room (item 1
   above) remains valuable as live-data confirmation, but is no longer needed to close this defect - it is
   confirmed end to end against a synthetic fixture by
   `TM59AssessmentCalculatorTests.ApartmentLivingRoomCondition_IsNotRoutedAsABedroom_SoCriterion2IsNotApplicable`
   and pinned by `TM59SpaceApplicationClassificationTests`.
