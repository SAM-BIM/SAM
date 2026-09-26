<!-- SPDX-License-Identifier: LGPL-3.0-or-later -->
<!-- Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors -->

# Part O — mixed dwelling strategies: PR1 (SAM authority + NV/MVHR materialisation)

**Status: implemented, following the approved PR0 specification**
([`PartO-MixedDwellingStrategies-PR0.md`](PartO-MixedDwellingStrategies-PR0.md), §D–§F and the owner decisions at
its top). The PR0 report is unchanged; where implementation settled one of its open gates or refined a rule, that is
listed in §8 below, not written back into PR0.

Scope: SAM only. No SAM_UI grid (PR2), no active-cooling TPD route (PR3), no licensed TAS run, no SAM_Deploy bump
(PR4). The legacy `PreparePartOIteration` path (1a/1b/2/2B) is unchanged.

```text
clean pre-Part-O baseline  +  persisted PartODwellingStrategySet  (+ catalogue)
        |
        v   Modify.MaterialisePartODwellingStrategies   -- one call, deterministic, refuses rather than repairs
mixed analytical model  +  per-zone OverheatingScenarios  +  PartOMaterialisationRecord
```

---

## 1. Public types and APIs

| Kind | Name | Purpose |
|---|---|---|
| class | `PartODwellingStrategy` (`IJSAMObject`, no guid) | Selected intent for one dwelling: `ZoneGuid`, `VentilationMode` (existing `PartOVentilationMode`), `VentilationUnitReference` (null = select from project pool), `ActiveCooling`, `DesignAirFlowBasis`, `DesignFingerprint`. `IsValid`, `CanonicalText()` |
| class | `PartODwellingStrategySet` | One strategy per dwelling, schema `PartODwellingStrategies:v1`, canonical (zone-guid order, no instance guids). `IsValid`, `Conflicts`, `Set/Remove/Strategy` |
| enum | `PartOActiveCooling` | `Undefined`, `None`, `SupplyAirCooling` (recorded, refused in PR1) |
| enum | `PartODesignAirFlowBasis` | `Undefined`, `PartFRequirement`, `RetainedDesign` |
| enum member | `PartOIteration.DwellingIndependent` | Iteration-neutral identity of an assessed common space (appended) |
| parameter | `AnalyticalModelParameter.PartODwellingStrategies` | The persisted strategy set, on the baseline |
| parameter | `AnalyticalModelParameter.PartOMaterialisationRecord` | The record, on the materialised model only |
| class | `PartOMaterialisation` | Result: `AnalyticalModel` (null on any refusal), `Refusals`, `OverheatingScenarios`, `Record`, systems, units, selections, notes, warnings |
| class | `PartOMaterialisationRefusal` + enum `PartOMaterialisationRefusalReason` | Structured refusal: reason code, zone guid, subject, message |
| class | `PartOMaterialisationRecord` | Baseline / strategy / catalogue fingerprints, assessed and common-space zones, zone → system guid map; `IsCurrent(baseline, catalogue, out reason)` |
| Modify | `MaterialisePartODwellingStrategies(baseline, descriptors = null, guids_Zone_Assessed = null)` | The one materialisation |
| Modify | `ApplyPartFVentilationRates(model, mode, IEnumerable<Space> spaces, …)` | Scoped overload; the old signature delegates with `null` (bit-identical) |
| Query | `PartOBaselineFindings(model)`, `IsPartOCleanBaseline(model, out findings)` | Clean-baseline validation (D1) |
| Query | `IsTM59CommunalCorridor(space)` | Exact match of the **assigned** internal condition against `TM59InternalConditionResolver.CommunalCorridorInternalConditionName` |
| Query | `PartODwellingDesignFingerprint(cluster, zone)`, `PartOCatalogueFingerprint(descriptors, projectTest)`, `PartOStrategyFingerprint(strategies, zones)` | Deterministic SHA-256 fingerprints |
| Create | `PartOCommonSpaceOverheatingScenario(zone)`, const `PartOCommonSpaceVentilationStrategy = "UV"` | The neutral corridor scenario |

Internal seams (no public change): `Modify.RealizeBaseMVHRDwelling` is `PrepareBaseMVHR`'s per-dwelling loop body lifted
out unchanged, with two caller-stated options that default to the legacy behaviour (created system id / unit name, and a
unit gate); `AddPartOBaseMVHRSystem` has an internal overload taking the id and name.

## 2. Clean-baseline validation (D1)

`Query.PartOBaselineFindings` lists every reason a model is not a clean baseline; empty means clean. Nothing is cleaned,
undone or adopted.

- **Materialisation** (`MaterialisedBaseline`): a system of the Part O MVHR type guid; any `SpaceAirMovement` /
  `AirHandlingUnitAirMovement`; a sized space whose internal condition is the `ApplyPartFVentilationRates` clone
  (`<condition> - <space>` name **and** a supply/extract airflow); a `PartOMaterialisationRecord`; a
  `PartOIsolationContext`.
- **Run output** (`RunOutputBaseline`): `OverheatingScenarios`; `SimulationResultProvenance`; any model-level parameter
  value that is an `IResult`; any cluster object whose stored type is assignable to `IResult` (base type, so a later
  result class is covered); `DesignDay` records **in the cluster**.
- **DesignDay rule (PR0 design gate, pinned):** the model-level `HeatingDesignDays` / `CoolingDesignDays` are design
  inputs (weather-derived or an engineer's override) and are accepted. `DesignDay` objects in the adjacency cluster are
  written there by the TAS workflow after a run (`SAM_Tas Modify.ReplaceDesignDays`, from `WorkflowCalculator`), so they
  are run output and refuse.
- Design ventilation terminals, authored internal conditions, Part F requirements, project settings and the strategy set
  itself are baseline data.

## 3. Persistence

- `PartODwellingStrategySet` is stored as `AnalyticalModelParameter.PartODwellingStrategies`, JSON `Schema =
  "PartODwellingStrategies:v1"`. Strategies are written in zone-guid order with the product as its identity fields only
  (no guid, no type tag), so logically identical sets write identical bytes and the baseline model fingerprint does not
  move when a UI rebuilds the set.
- **Absent = legacy.** A model without the parameter is refused by the materialiser with `NoStrategies` and is otherwise
  untouched; the legacy path never reads it. Nothing is migrated or inferred from systems, scenarios, report text,
  filenames or scenario labels.
- **Fail closed on read:** an unknown schema, a zone stated twice (kept and written back as a conflict, never resolved to
  either), an unknown enum name, or a stated-but-empty product reference make the set or strategy invalid and refused.
  "No strategy" (no entry), "Natural" and "MVHR" are distinct; an entry with `VentilationMode = Undefined` is invalid.
- No airflow value is persisted in a strategy (D4). `RetainedDesign` holds only `DesignFingerprint`.

## 4. Materialisation

One call, pure (the baseline is not modified), returning a model only when **nothing** refused.

1. **Baseline** findings (§2).
2. **Strategies:** set present and valid; every strategy's zone exists and is a dwelling (`Query.PartFDwellingZones`);
   every assessed dwelling has a valid strategy. Scope: `guids_Zone_Assessed = null` means every dwelling; a dwelling
   outside a stated scope is *unassessed* — untouched, no scenario.
3. **Contradictions / gates:** `NaturalWithCooling`, `CoolingGated` (any `SupplyAirCooling`), `NaturalWithRetainedDesign`,
   `NaturalWithVentilationUnit`; `OverlappingZones`.
4. **Common spaces:** a marked non-dwelling zone all of whose spaces are assigned the TM59 communal corridor condition is
   an assessed corridor; a zone mixing corridor and other spaces refuses (`CommonSpaceUnclassifiable`); a zone with none
   is noted and not scenarioed. Names are never read.
5. **Authored plant** (baseline): a system is *effective* if it has a connected terminal with positive design flow or
   names a unit the model contains; otherwise it is template metadata (noted, untouched). Effective systems / units that
   touch an assessed dwelling and span more than one zone (or unzoned spaces) refuse `SharedSystem`; over a Natural
   dwelling refuse `NaturalOverMechanicalDuty`; in an MVHR dwelling but connected to none of its terminals refuse
   `UnconnectedAuthoredPlant`. Shared plant is never split or mutated.
6. **Part F, scoped:** `ApplyPartFVentilationRates(ContinuousDesign, spaces of MVHR dwellings)` and
   `RealizePartFVentilationTerminals(spaces of MVHR dwellings)`. Natural and unassessed dwellings receive nothing (P1/P11
   fixed at the seam).
7. **Design basis per MVHR dwelling:** `PartFRequirement` — every terminal must realise a requirement and each continuous
   requirement's terminal sum must equal it, else `DesignDiffersFromRequirement` (never reset silently). `RetainedDesign`
   — the baseline must carry terminals, and the dwelling's fingerprint after realisation must equal the strategy's, else
   `RetainedDesignStale`. The retained airflow is read from `VentilationTerminal.DesignFlowRate_Lps` only.
8. **MVHR design per dwelling**, in (zone name, zone guid) order: `RealizeBaseMVHRDwelling` (system, unit, ticV gate,
   duty, movements, transfer air, balance — Iteration 1a's code). The created system id is the dwelling label and the unit
   `MVHR <label>`, unique against every space and unit name, disambiguated `" (n)"` in canonical order. **P12 gate:** a
   reused unit stating a finite summer or winter supply temperature refuses `ConditionedReusedUnit` (never cleared).
9. **Product:** explicit reference → must be in the offered catalogue (`VentilationUnitUnresolved`), permitted by the
   project's `PartOEquipmentSelection` (`VentilationUnitNotAllowed`) and able to serve the duty (selection over that one
   product; `VentilationUnitSelection`). Null reference with a catalogue → the project's automatic candidate set
   (`CandidateDescriptors`, project test product included as in Iteration 2); manual project mode → generic unit + warning.
   No catalogue → generic units (Iteration 1a). Each dwelling selects against its own duty only.
10. **Natural invariant:** after materialisation each Natural dwelling must still have its baseline internal conditions
    (same guid), the same terminals, none newly connected, no Part O system and no air movement reaching it — else
    `Invariant`.
11. **Scenarios:** Natural → `BaseNaturalVentilation` / `NV`; MVHR → `BasePassive` / `MVHR` (the same keys homogeneous
    runs use, via `Create.OverheatingScenarios`); assessed corridor → `CommonSpace` / `DwellingIndependent` / `UV` / no
    assumptions. SAM's per-space TM59 criterion is unchanged.
12. **Record** stamped on the model (§6). The model also keeps the strategy set it was built from.

The whole building is returned; nothing is isolated. Because materialisation always rebuilds from the baseline, a
dwelling's strategy is edited and the model rebuilt — the previous mixed model is never mutated (PR0 D4).

## 5. Determinism

Canonical processing order and dwelling-derived names make the engineering state a function of (baseline, strategies,
catalogue). Pinned: two independent JSON clones give equal GUID-insensitive signatures **and** equal system/unit names;
reversed strategy and scope order give the same result; a dwelling materialised alone gets the same design and names as
when materialised with others. Generated objects take fresh guids, so `SimulationResultProvenance.Fingerprint` of two
rebuilds differs: a rebuilt model always needs a fresh TAS simulation.

## 6. Fingerprints and staleness

`PartOMaterialisationRecord`: `Fingerprint_Baseline` = `SimulationResultProvenance.Fingerprint(baseline)` (the existing
model digest); `Fingerprint_Strategies` over the assessed strategies' canonical text plus the assessed zone set;
`Fingerprint_Catalogue` over every selection-relevant descriptor field — manufacturer, model, reference, maximum supply,
maximum extract, rank, validity — plus the project test product, order-insensitive, `null` distinct from empty.
`IsCurrent(baseline, catalogue, out reason)` fails closed and names the half that moved. Levels:
baseline/strategies/catalogue ≠ record → re-materialise; model ≠ provenance → re-simulate (existing).

## 7. Tests

`SAM/SAM.Tests/PartODwellingStrategyMaterialisationTests.cs` (38 tests) — the PR0 proof matrix promoted and inverted. The
disposable PR0 proof tests (`PartOMixedStrategyProofTests.cs`, P0–P12) are removed; they remain at `444d2db3`.

| PR0 fact | PR1 pin |
|---|---|
| P1 / P11 NV and unassessed dwellings polluted, authored basis zeroed | `MvhrDwelling_LeavesNaturalAndUnassessedDwellingsExactlyAsTheBaselineHasThem`, `ScopedPartFRates_…` |
| P2 / P10 names follow call order; guids differ | `SameBaselineAndStrategies_OnIndependentClones_…`, `StrategyAndScopeOrder_DoNotChangeTheResult` |
| P3 automatic call re-selects a manual product | `DifferentProducts_StayPerDwelling_…` |
| P4 MVHR→NV keeps the design | `NaturalDwelling_OnTheSameBaseline_CarriesNoMechanicalState` |
| P5 retained design / unbalanced raise | `RetainedBalancedDesign_…`, `RetainedDesign_IsNotChangedByAnotherDwellingsStrategy`, `RetainedDesign_WhoseTerminalsMoved_IsStale_…`, `UnbalancedBaselineDesign_IsRefused_NotRescaled` |
| P7 shared system | `AuthoredSystemWithDuty_StraddlingTwoDwellings_IsRefused_…`, `AuthoredSystemWithoutDuty_IsTemplateMetadata_…` |
| P8 corridor has no strategy | `CommunalCorridor_IsIncludedAutomatically_WithAnIterationNeutralScenario`, `CorridorClassification_ReadsTheAssignedInternalCondition_NeverTheName`, `CommonSpaceZone_Mixing…`, `DwellingIndependent_IsACommonSpaceIdentityOnly` |
| P9 serialisation | `StrategySet_RoundTripsThroughTheModelJson_Canonically`, `StrategySet_OfAnUnknownSchema_…` |
| P11 / result-bearing baseline | `LegacyPreparedModel_IsRefused_AsMaterialised`, `MaterialisedOutput_IsNotABaseline`, `ModelWith{Scenarios,Provenance,SimulationResults}_…`, `DesignDay_…` |
| P12 conditioned reused unit | `ReusedConditionedUnit_IsRefused_RatherThanLeakingCooling` |
| D5 cooling | `ActiveCooling_IsRecordedButRefused`, `Natural_WithRetainedDesign_WithCooling_OrWithAProduct_IsRefused` |
| catalogue | `CatalogueFingerprint_CoversEverySelectionRelevantField_AndNotOrder`, `MaterialisationRecord_IsCurrent_…` |

Results (2026-09-27, Release): new class 38/38; `FullyQualifiedName~PartO|FullyQualifiedName~PartF` 1263/1263 (PR0's
1238 − 13 removed proofs + 38); `PartOIterationPreparationTests` 86/86; `PartOBaseMVHRTests` 34/34;
`OverheatingScenario|TM59|VentilationStrategyMap` 256/256; full `SAM.Tests` 2523/2523; `SAM.sln` Release 0 errors.

One existing test changed: `OverheatingScenarioTests.PartOIteration_HasNoFoundationStageMember` pins the exact enum
membership and now includes the appended `DwellingIndependent`.

## 8. PR0 assumptions refined or settled by implementation

1. **Common-space identity (PR0 design gate H5):** a new appended enum member `PartOIteration.DwellingIndependent`
   with an empty assumption set — not `Undefined`, because an unreadable persisted iteration loads as `Undefined`.
2. **Missing strategies (D2.1):** implemented with an explicit assessed scope. Inside the scope every dwelling must have a
   strategy; outside it a dwelling is unassessed and untouched — never read as Natural.
3. **Which common spaces are assessed:** only a non-dwelling zone whose spaces are *all* assigned the TM59 communal
   corridor condition. Other common zones are noted and not scenarioed; a mixed zone refuses.
4. **Baseline air movements:** PR0 named "Part O air movements"; PR1 refuses **any** air movement object, since they are
   derived runtime state and a false positive only refuses.
5. **Result detection:** the cluster answers no interface query (`GetObjects<IResult>()` is null), so detection walks the
   stored types and tests each against `IResult` — the PR0 intent (base type, not a list) unchanged.
6. **Explicit product sufficiency:** PR0 did not say. PR1 refuses an explicit product that cannot serve the dwelling's
   duty (fail closed), whereas the legacy manual assignment flags it. PR2 should confirm this with the owner.
7. **Retained design:** the terminal count is taken on the baseline and the fingerprint compared after scoped
   realisation, so a requirement that gained no baseline terminal makes the design stale.
8. **Isolation:** not performed by PR1; an isolated model is refused as a baseline.

## 9. Residual risks

- `Fingerprint_Baseline` includes project-setting objects that are `SAMObject`s with instance guids
  (`PartOEquipmentSelection`, `PartOProjectTestVentilationUnit`); re-creating one with equal content makes the record
  stale — a false staleness, fail-closed.
- A real baseline carrying hand-authored or template air movements, or run-written design days, is refused; the user must
  reopen the pre-Part-O source (accepted migration cost, PR0 G).
- The per-space internal-condition copy of the Part F rate (PR0 P5) is still written on MVHR spaces; it is pre-existing,
  inert while ticV is refused, and not a new store.
- No licensed TAS run of a mixed model yet, and no scale measurement (PR4).

## 10. Next step

Review and merge this SAM PR into `sow/2026-Q3`. Then **PR2 (SAM_UI)**: the scalable dwelling strategy grid; materialise
on a copy (the open model stays baseline + intent); one simulation; TM59; the explicit "accept 2B for a dwelling" design
edit onto the baseline's terminals (lineage-matched, PR0 D3), recording `RetainedDesign` + `PartODwellingDesignFingerprint`;
sidecar `PartORunResume:v3` carrying the record.
