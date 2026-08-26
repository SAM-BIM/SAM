<!-- SPDX-License-Identifier: LGPL-3.0-or-later -->
<!-- Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors -->

# Part O — target architecture

**This file is the durable record of the Approved Document O architecture.** It is not a progress log and
not PR prose: it states the separation of concepts that the Part O code must keep, the algorithm for each
iteration, and which parts are implemented. Implementation evidence lives in
[`PartO-TAS-VALIDATION.md`](PartO-TAS-VALIDATION.md); repository state lives in
[`PartF-HANDOVER.md`](PartF-HANDOVER.md).

---

## 1. The central separation

```text
regulatory requirement
        |
        v
ventilation route
        |
        v
equipment selection
        |
        v
Part O operating scenario
        |
        v
simulation
        |
        v
result
```

**These are five different concepts and they must not be collapsed into one another.** Every Part O defect
found so far has been a collapse of two adjacent boxes:

| Collapse | What it produced |
|---|---|
| requirement == equipment | A regulatory airflow read off an MVHR unit's capacity |
| route == stale system metadata | `VentilationSystemTypeName` deciding what the simulation is |
| route == "not NV" | An MVHR system invented for a dwelling nobody said had one |
| scenario == ventilation system | `BasePassive` meaning one specific system, so an NV result could only be attributed to a scenario asserting design-rate mechanical ventilation |

### The rule that follows

> **Part F calculates regulatory ventilation requirements.**
>
> It must **not** derive the regulatory airflow as a percentage of an MVHR unit's capacity.
>
> For an MVHR route, the calculated requirement is later used to **select** an appropriate physical MVHR
> unit. The unit's capacity is *equipment capability*; it is never the source of the requirement.

---

## 2. The algorithm

```text
DWELLING / ZONE DATA
        |
        v
PART F REGULATORY REQUIREMENTS
  - whole-dwelling ventilation requirement
  - wet-room extract requirements
  - other Part F requirements where implemented
        |
        v
EXPLICIT PART O VENTILATION ROUTE
  never inferred from stale system metadata
        |
        +-------------------------------------+
        |                                     |
        v                                     v
+-----------------------+          +------------------------+
| Iteration 1a          |          | Iteration 1b           |
| Base MVHR             |          | Base Natural Vent.     |
|                       |          |                        |
| Windows available     |          | Windows available      |
| No acoustic restrict. |          | No acoustic restrict.  |
|                       |          |                        |
| Continuous MVHR       |          | NO central supply      |
| supply/extract        |          | NO continuous MVHR     |
| = Part F requirement  |          | extract                |
|                       |          |                        |
| select minimum        |          | openings/background    |
| compliant MVHR unit   |          | ventilation            |
|                       |          |                        |
|                       |          | intermittent wet-room  |
|                       |          | extract only where its |
|                       |          | operation is truthfully|
|                       |          | characterised          |
+-----------------------+          +------------------------+
        |                                     |
        +------------------+------------------+
                           v
                        RESULT
                           |
                 further mitigation needed
                           |
                           v
+----------------------------------------------+
| Iteration 2                                  |
| Acoustic restriction                         |
| - FullyClosed OR BedroomNightClosed          |
|                                              |
| selected REAL MVHR unit                      |
| summer bypass                                |
| demand/boost capability                      |
| actual available airflow                     |
+----------------------------------------------+
                           |
                           v
                        RESULT
                           |
                   optional larger unit
                           |
                           v
+----------------------------------------------+
| Iteration 3                                  |
| Same acoustic restriction                    |
| selected REAL MVHR unit                      |
| boost airflow                                |
| active cooling / tempering                   |
| supply T from manufacturer performance       |
+----------------------------------------------+
                           |
                           v
                        RESULT
```

### 1a and 1b are alternatives, not steps

**Iteration 1a and Iteration 1b are two alternative BASE configurations of the same dwelling.** They are
not sequential mitigation stages. A dwelling is assessed at 1a *or* 1b according to its ventilation route;
Iteration 2 is the first thing that is genuinely "further mitigation".

Anything that models the Part O stages as a single ordered list — a numbered enum walked in order, a UI
that greys out later steps until earlier ones fail, a report that prints 1a then 1b — has the engineering
wrong.

### Naming

Engineering meaning is carried by **semantic names**, never by numbers:

| API (durable) | Grasshopper / UI (presentational) |
|---|---|
| `PartOIteration.BaseMVHR` *(future; today `BasePassive`)* | Iteration 1a |
| `PartOIteration.BaseNaturalVentilation` | Iteration 1b |
| `PartOIteration.AcousticRestricted` | Iteration 2 |
| `PartOIteration.ActiveTrimCooling` | Iteration 3 |

"1a"/"1b" must never be the only place the engineering meaning is recorded. A number in a public API is a
fact about a document's layout, not about a building.

---

## 3. The explicit Part O ventilation route

### The type

```csharp
namespace SAM.Analytical.Enums;

public enum PartOVentilationMode
{
    Undefined,            // nothing stated -> REFUSE
    NaturalVentilation,   // Iteration 1b
    MVHR,                 // Iteration 1a
}
```

Resolved by `SAM.Analytical.Query.PartOVentilationMode(...)` from what the assessment **states**, never
from what the model's existing objects happen to carry.

### The contract

```text
PartOVentilationMode.NaturalVentilation
    -> no continuous MVHR supply
    -> no continuous MVHR extract
    -> opening / background ventilation remains available
    -> intermittent extract remains a SEPARATE concept
    -> TAS / scenario assessed as natural ventilation

PartOVentilationMode.MVHR
    -> the Part F continuous requirement can be applied
    -> a later physical MVHR selection can satisfy that requirement
    -> TAS / scenario assessed as mechanical

missing / unsupported / ambiguous mode
    -> REFUSE
```

### The rule that was removed, and must not come back

```text
FORBIDDEN:   anything that is not "NV" == mechanical
```

`UV`, an empty string, an unrecognised word and stale model metadata are all **absences of a stated
route**, not statements that the dwelling has an MVHR system. None of them may cause Part F System 4
airflow to be written. They refuse.

`MV` is refused for the same reason and it is worth being explicit about why, because it looks like a
statement and is not one. Part F System 3 (continuous mechanical extract) and System 4 (continuous supply
and extract with heat recovery) are different buildings. What `PartFCalculator` sizes is System 4 — a
supply terminal in every habitable room. Writing that onto a dwelling whose route is stated only as
"mechanical" invents the supply half, which is the same defect as inventing the whole system for an NV
dwelling, one terminal smaller.

### Physical system metadata is evidence, never authority

A `SAM_System`, a `SystemTemplate`, or an `InternalCondition.VentilationSystemTypeName` may *corroborate*
a route and may one day be *validated against* it. In this implementation none of them is read to decide
the route, and — equally important — **none of them is written to force one**. Mutating
`VentilationSystemTypeName` so the simulation takes a chosen branch would put the decision back into the
metadata it was taken out of, and would make the model on disk a lie about the building.

The explicit Part O route is authoritative for **preparation, export and assessment**.

---

## 4. Iteration 1b — Base Natural Ventilation (implemented)

```text
BaseNaturalVentilation

no acoustic restriction imposed by the iteration

authored opening behaviour remains authoritative:
    Unrestricted / fully available
             OR
    NightClosed with authored hours

no central mechanical supply

no continuous MVHR extract

intermittent WC / bathroom / kitchen extract:
    preserve / model ONLY if its actual operation can be
    represented truthfully with existing SAM data
```

**The iteration imposes nothing on the openings.** `OpeningRestriction` is authored building data and
`PartOOpeningProperties.Schedule` is *derived* from it, so resetting a restriction to match a stage's
assumption deletes that aperture's `PartO_DayOpen_HH_HH` availability schedule from the model that reaches
TAS. Disagreement is reported, never reconciled.

**An intermittent Part F design extract rate must not be turned into a continuous 24/7 extract flow.**
Where SAM carries the rate but no truthful operation schedule or control, the data is preserved and the
runtime behaviour is *not* invented. See §6.

---

## 5. Iteration 1a — Base MVHR (NOT implemented; recorded only)

```text
Part F regulatory requirement
       |
       v
explicit BaseMVHR route
       |
       v
apply continuous supply / extract requirement
       |
       v
select minimum compliant MVHR unit from MVHR_Template
       |
       v
unit capacity is equipment capability,
NOT the source of the Part F requirement
```

This is deliberately not built yet. It is implemented only after Iteration 1b is accepted end to end.

Iterations 2 and 3 — acoustic restriction, summer bypass, boost, active cooling, manufacturer supply
temperature, larger-unit selection — are likewise recorded in §2 and not implemented.

---

## 6. Wet-room intermittent extract — what SAM actually has

Investigated 2026-08-26 against `SAM.Analytical`. Four questions, four answers:

**1. What Part F design rate exists?**

Two separate things, and they are not the same thing:

- `PartFCategory.IntermittentExtractRate_Lps` — the Table 1.1 intermittent rates, present in
  `SAM_PartFSpaceRulesUKDwellingsMVHR.json`: **15 l/s**, **15 l/s**, **6 l/s** and **30 l/s** on the four
  categories that carry one.
- The kitchen local-extract terminal built by `PartFCalculator`, which for
  `PartFExtractMethod.CookerHoodExtractingOutside` and `SeparateIntermittentExtract` carries
  `HighFlowRate_Lps` = 30 / 60 l/s from `PartFData`.

**2. Is it currently represented as intermittent?**

Partly.

- The **kitchen** terminal is: `OperatingMode = HighBoost`, `IsInBalancedFlow = false`, and
  `ContinuousDesignFlowRate_Lps` left null — so it is already outside every continuous total and outside
  everything `Modify.ApplyPartFVentilationRates` writes.
- The **bathroom / WC** intermittent rate is **not**. `PartFCategory.IntermittentExtractRate_Lps` is
  parsed from the rules JSON, stored on the category, and **read by nothing** — no calculator, no report,
  no export. It is inert data. What a wet room actually receives from `PartFCalculator` is a *continuous*
  `GeneralExtract` terminal with `IsInBalancedFlow = true`, because the calculator is unconditionally
  System 4 shaped (paragraph 1.67) whatever the dwelling's real strategy.

**3. Does SAM carry a trustworthy runtime schedule or control for it?**

**No.** Nothing on `PartFVentilationTerminalRequirement`, `PartFSpaceData` or `PartFCategory` states when
an intermittent extract runs, for how long, or what triggers it. There is no occupancy link, no humidity
control, no daily availability schedule and no duty cycle. The only operating fact recorded is the enum
value `PartFOperatingMode.HighBoost`, which names a *rate*, not an operation.

**4. Does TAS currently have a truthful write path for it?**

**No.** `SAM.Analytical.Tas` exports the supply side only — `SupplyAirFlow` / `SupplyAirFlowPerArea` /
`SupplyAirFlowPerPerson` reach the TBD as `freshAirRate` and the `ticV` factor, plus the
`SAMZoneMetadata` decomposition in the zone description. `InternalConditionParameter.ExhaustAirFlow` is
read in exactly one place in the whole repository — `PartODiagnosticLog`, for reporting — and is written
to no TBD field. There is therefore no TAS representation of an intermittent extract to be truthful or
untruthful with, let alone a scheduled one.

### The consequence, and it is deliberate

Iteration 1b is accepted as:

```text
natural ventilation through openings
+
zero continuous mechanical ventilation
```

with the intermittent wet-room extract **preserved as data and not modelled as runtime behaviour**. This
is not a gap that blocks the NV/opening workflow: SAM has the rate but not the operation, TAS has neither,
and inventing a schedule would be exactly the failure mode this architecture exists to prevent. It is a
documented future runtime-control item.

---

## 7. What "no mechanical airflow applied" claims, and what it does not

**It claims:** SAM has **not** invented an MVHR or MVRE system for a dwelling nobody said had one.

**It does not claim:** that the dwelling's natural-ventilation Part F design has been sized. System 1
background/trickle ventilator provision and purge ventilation are calculated **nowhere** in SAM.
`PartFCalculator` takes no ventilation-strategy input at all and remains System 4 shaped for every route.

**Never report the NV result as "Part F NV sizing".** This wording is pinned by a test.

---

## 8. Result identity

`OverheatingScenario.Key` is derived from the assessment scope, the zone guid, the iteration, the system
template and the operating assumptions. It is a permanent identity: two engineers stating the same
assessment get the same guid, and a scenario reloaded from JSON is recognisably the same one.

Two consequences follow:

- **The iteration name is inside the key.** Renaming `BasePassive` to `BaseMVHR` re-keys every assessment
  ever attributed to it. That rename is a migration, not an edit, and it is not done here.
- **An iteration's operating assumptions are inside the key.** `BasePassive` asserts
  `Mechanical Ventilation At Design Rate = True`. Attributing a natural-ventilation result to it would
  therefore mint a permanent identity that states something false about the building. That — not style —
  is why `BaseNaturalVentilation` had to be added rather than reused.

`OverheatingScenario:v2` — making opening behaviour a property of the model rather than of the stage — is
still deferred. Nothing here depends on the old assumption that `BasePassive` means one specific
ventilation system; that dependency is what `PartOVentilationMode` removes.

---

## 9. Implementation status

| Concept | Status |
|---|---|
| Explicit `PartOVentilationMode` route | **Implemented** |
| Route refuses missing / unknown / ambiguous / mixed | **Implemented** |
| Iteration 1b `BaseNaturalVentilation` | **Implemented**, licensed acceptance in `PartO-TAS-VALIDATION.md` |
| Authored opening behaviour preserved through preparation | **Implemented** |
| Iteration 1a `BaseMVHR` + MVHR unit selection | Not implemented — §5 |
| Iteration 2 acoustic restriction / bypass / boost | Not implemented — §2 |
| Iteration 3 active cooling / manufacturer performance | Not implemented — §2 |
| Per-zone (mixed NV + mechanical) airflow application | Not implemented — refuses |
| System 1 background ventilator / purge sizing | Not implemented anywhere — §7 |
| Intermittent wet-room extract runtime control | Not implemented — §6 |
| `OverheatingScenario:v2` | Deferred — §8 |
