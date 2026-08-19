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
4. **`Kitchen_4` / `Kitchen_7` report `TM59 Application: Sleeping, Cooking`, not `Cooking` alone — a naming
   artifact, traced, not a defect requiring a code change.** Both are multi-bedroom-apartment kitchens, so
   `TM59InternalConditionResolver` names their `InternalCondition` `"2 Bed Apt. Kitchen"` /
   `"3 Bed Apt. Kitchen"` — the apartment's bedroom **count** is part of the condition's own name.
   `TM59Manager.TM59SpaceApplications(InternalCondition, TextMap)` classifies that whole name through
   `TextMap.GetSortedKeys`, which checks it word by word against every TM59 keyword — and "Bed" is,
   independently, one of the literal "Sleeping" keywords. The same token that states the apartment's
   bedroom count is read as evidence that *this* room is used for sleeping. Confirmed at the token level
   by `TM59SpaceApplicationClassificationTests` (`SAM.Tests`), which pins the current outcome for both
   condition names and shows a plain `"Double Bedroom"`/`"Single Bedroom"` condition (no apartment-size
   prefix) is unaffected.

   **Why the classification source was not changed in this pass.** `TM59Manager.IsSleeping`/`IsLiving`/
   `IsCooking`/`TM59SpaceApplications` are shared entry points with their own explicit "stay untouched -
   SAM_Tas depends on their exact current behaviour" guard (see `TM59InternalConditionResolver.cs`), and
   SAM_Tas's `RoomUse.cs`, `ToSAP.cs` and the legacy `OverheatingCalculator.cs` all call the same
   InternalCondition-based overloads directly — a fix there has a blast radius well beyond this report. It
   is also not the whole story: `GetSortedKeys` has a separate, pre-existing "room" vs "bedroom" substring
   collision (the reason `TM59InternalConditionResolver` built its own whole-token matcher rather than
   reuse it for Space classification), so a narrow fix for the "N Bed Apt." prefix alone would still leave
   a plain non-bedroom Living Room in the same apartment misclassified. **The numeric TM59 outcome is
   unaffected either way** — `Sleeping` only changes which natural-ventilation result type a space is
   routed to, both kitchens are mechanically ventilated, and the "SAM vs native TAS TM59 comparison" table
   above already shows both matching TAS exactly. The new Internal Condition report column makes the real
   condition name visible, which is what surfaced this in the first place; a real fix to the matching
   engine is left as follow-up work, not bundled into a report-presentation task. See
   [Remaining validation work](#remaining-validation-work).

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
- `MECHANICAL VENTILATION` gained an `Internal Condition` column and an `Occupied Hours` column (the one
  basis this criterion has, already on `TMResult.OccupiedHours`); the ambiguous `Use` column is now headed
  `TM59 Application`.
- The former single `FULL-YEAR >28 C / CORRIDOR-STYLE RESULTS` section is split into `COMMUNAL CORRIDOR
  RISK` (positively identified corridors only, contributing to `CorridorRiskStatus`) and
  `SUPPLEMENTARY >28 C CHECKS` (everything else the same calculation produced) — see item 3 above. Both
  gained an `Annual Hours` column (`TM59CorridorResult.AnnualHours` / `TM59CorridorExtendedResult.
  GetAnnualHours()`, the real annual series length, not `Limit` divided back through its exceedance
  factor).
- The legend was trimmed to the engineering meaning of each column and criterion; the SAM-vs-TAS
  validation history that used to live there is this document instead.

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
7. **Fix the "N Bed Apt." token collision in `TM59Manager`'s Sleeping/Living/Cooking matching**, found
   while adding the report's Internal Condition column (`Kitchen_4`/`Kitchen_7`, see [Known
   scope/classification differences](#known-scopeclassification-differences) item 4). Scope it as its own
   change: `TM59Manager.IsSleeping`/`IsLiving`/`IsCooking`/`TM59SpaceApplications` are shared with
   SAM_Tas's `RoomUse.cs`, `ToSAP.cs` and `OverheatingCalculator.cs`, and `GetSortedKeys` carries a
   separate pre-existing "room"/"bedroom" substring collision that a narrow prefix-strip would not fix -
   both need deciding together, against `TM59SpaceApplicationClassificationTests` as the pinned baseline.
