# Project Progress

## Branch
`feature/parto-iteration2-mvhr-selection` (off `sow/2026-Q3` at `b8fb0c0f`, i.e. with Iteration 1a PR #77
merged as `7fb04ed9`). Open as **PR #79**, base `sow/2026-Q3`. **Not merged.**

## Last updated
2026-08-28 - Iteration 2 / MVHR selection and independently adjustable design airflows, plus fifteen
correctness fixes across five review rounds on PR #79 (thirteen raised by Codex, two found in review of
the PR itself).

## Current status (this session)

Iteration 2 is implemented and under review on PR #79. Iteration 1a shipped a design that realized the
Approved Document F requirement *exactly*, on generic plant. Iteration 2 puts a real product behind the
plant and lets the design move above the requirement - which means **four quantities now exist where one
used to serve**, and the whole iteration is about not letting them collapse into each other.

### The authority separation - requirement != design != equipment capability != operating airflow

| Authority | Lives in | Written by |
|---|---|---|
| Part F requirement | `PartFSpaceData.Terminals[].ContinuousDesignFlowRate_Lps` | `PartFCalculator` **only** - immutable to everything in Iteration 2 |
| Equipment capability | `VentilationUnitCapacityDescriptor` (the catalogue, outside the model) | never written into the model at all |
| Design airflow | `VentilationTerminal.DesignFlowRate_Lps` | the design / Part O optimisation path |
| Operating airflow | `InternalCondition`, profiles, TAS `ticV`, IZAM state | **untouched** - a later iteration |

The invariant:

```
PartFRequiredAirFlow  <=  DesignAirFlow  <=  SelectedMVHRCapacity
```

**The two constraints are enforced at different levels, deliberately.** The Part F floor is enforced at the
applicable **terminal/space** level - a room's design may never fall below what the Approved Document
requires *of that room*. The capacity ceiling is evaluated at the **AHU/system duty** - equipment serves a
dwelling, not a room, so "does it fit?" is only meaningful about the summed duty. Neither check substitutes
for the other, and a system total substitutes for neither (see fix 3 below, which is exactly that mistake).

### Targeted vs derived

`Modify.ApplyTargetedDesignAirFlow` sets **one** room's design airflow and rebalances the dwelling around
it, as a single all-or-nothing transaction.

> **Targeted adjustment = explicit design decision.**
> **Derived adjustment = consequence required to restore a valid balanced dwelling network.**

```
targeted:   Bedroom 1 supply 20 -> 24        <- the only room anyone selected
derived:    extract +4, allocated across the dwelling's extract terminals
            transfer paths recalculated on the next preparation
            AHU design duty follows
unchanged:  Bedroom 2, Living Room, and every Part F requirement
```

A Part O iteration targets the room that *failed*. The wet room whose extract rises by the matching 4 l/s
was never selected for optimisation, and reporting the two the same way would make it impossible to say
afterwards which rooms were engineering decisions. `DesignAirFlowAdjustment.IsDerived` carries the
distinction **on the report only** - it is not stored on the model and is not a fifth authority.

The allocation rule is **borrowed, not invented**: `PartFExtractAllocationStrategy.MinimumFirstCookingPriority`,
the same strategy `PartFCalculator.AllocateContinuousExtract` used to size the dwelling. It is applied to
the *change*, never recomputed from scratch, so a deliberate imbalance a designer authored survives.

### The MVHR descriptor / reference selection seam

`SAM.Analytical` **cannot** depend on `SAM_Systems`, and Iteration 2 adds no such dependency. It reuses the
seam Iteration 1a established for system templates rather than inventing a second one:

| Iteration 1a (settled) | Iteration 2 (mirror) |
|---|---|
| `SystemCapabilityDescriptor` - identity + capability bits + `Rank`, plain class, **handed in** | `VentilationUnitCapacityDescriptor` - identity + max supply/extract + `Rank` |
| `SystemCapabilitySelection` - Selected or Refused, no third answer | `VentilationUnitSelection` |
| `Query.CapableSystems` / `SelectPreferredCapableSystem` - pure, open no file | `Query.CapableVentilationUnits` / `SelectSmallestCapableVentilationUnit` |
| `SAM_Systems.Query.SystemCapabilityDescriptors(dir, app)` supplies values | catalogue stays an **argument** - see open items |
| `SpaceParameter.PartFSpaceData`, `VentilationTerminalParameter.PartFTerminalReference` | `AirHandlingUnitParameter.VentilationUnitReference` |

Only the product's **identity** is stored on the model; capacities stay in the catalogue. Duties stay
**derived, never stored** - `Query.AirHandlingUnitDesignDuty` sums over every system a unit supplies, so
the general `AHU-01 -> Zone A/B/C` arrangement stays open even though the Part O workflow is one dwelling
per unit.

Selection rule: **smallest compliant, never nearest**; both sides checked independently; nothing compliant
is an explained refusal, never an undersized fallback; two products tied on size *and* rank refuse as
ambiguous.

## Review round 1 - the four correctness fixes at `c67cf5d4`

Applied on top of the first Iteration 2 commit `41a02d4e`. Two were found by Codex, two in review of the
PR itself.

**1. Reductions consume available design headroom, not proportional total duty.** *(Codex P1)*
`Allocate` shared a negative change in proportion to each room's *total duty*, which handed a share to a
room sitting exactly on its Part F floor, saw that share breach it, and refused the whole change as
impossible - while another room held all the headroom needed. Reversing a previous targeted change, the
most ordinary thing an optimisation does, therefore failed. A reduction is now shared in proportion to
`max(0, duty - requirement)`, in tiers (cooking-priority rooms first on the extract side, then the rest),
capped at each room's own headroom, and only refuses when the total removable headroom genuinely falls
short. **Note for the next agent:** on a dwelling the real `PartFCalculator` sized, both sides hold equal
removable headroom, so a reduction can *always* be balanced - the shortfall refusal is only reachable on a
model with asymmetric requirement totals, which is why its test builds one by hand.

**2. A targeted change never touches another ventilation system's terminals.** *(Codex P1)*
A duty is summed per room and per direction, and `Modify.SetSpaceDesignFlowRate` writes *every* terminal of
that room and direction. A room holding terminals from this Part O system and from another one would have
had both rewritten - silently moving the other system's design duty while the result claimed the change
belonged to this one. New `TerminalsOfSystem` validates attribution for the targeted room **and every
candidate derived room** before anything is written, and **refuses** where a room is shared or where a
terminal belongs to no system at all. Refused rather than filtered: writing only the subset that belongs
here needs a system-scoped setter that does not exist, and inventing one would be a multi-system allocation
architecture Iteration 2 has no business introducing.

**3. The Part F floor is enforced at ROOM level, not only at the system total.** *(found in PR review)*
`ReconcileVentilationSystemDesignDuty` warned about a room below its requirement but only *refused* on the
system total. A bedroom 2.5 l/s under its requirement and a living room 2.5 l/s over it summed to a total
that agreed exactly, and the preparation passed - simulating a bedroom ventilated below its Approved
Document rate while reporting compliance. Surplus in one room is not tradeable against a shortfall in
another. New `RefuseSpace` refuses per room; above-requirement stays valid headroom and is still only
reported. This did **not** restore `Design == Required`.

**4. `ApplyTargetedDesignAirFlow` refuses an already-unbalanced dwelling before writing.** *(found in PR review)*
It previously checked balance *after* mutating and only added a warning, leaving `Successful` true - a
result claiming a valid balanced design for a dwelling that gains air it never loses. A targeted change and
its derived consequence move both sides equally and cannot close a pre-existing residual anyway. The check
is now a **pre-write refusal**, and the post-write balance assertion is a refusal rather than a warning.

## Review round 2 - the three further fixes on top of `c67cf5d4`

Codex did not re-raise fixes 1 or 2 above; it found three more. All three were verified independently
before being accepted.

**5. Every served room's Part F floor is validated before any mutation.** *(Codex P1)*
Fix 4 made a *balanced* dwelling the precondition, but balance is a property of the totals and the
Approved Document F floor is a property of each room, and one is not evidence of the other. A bathroom at
5 l/s against a 10 l/s requirement, offset by a kitchen at 15 against 10, totals 20 either way and
balances perfectly against 20 l/s of supply - so a +1 l/s bedroom target derived 1 l/s of kitchen extract,
never touched the bathroom, and reported success on a dwelling that was never compliant. The reduction
path already checked every candidate room; the **increase** path returned without any floor check, which
is why only increases were exposed.

The precondition now runs `Query.ReconcileVentilationSystemDesignDuty` over the whole served system and
refuses on its refusals - **reusing the one definition of compliant** rather than adding a second, so this
can never drift from what `Modify.PreparePartOIteration` refuses to simulate. Only its refusals are read;
its notes and warnings are about design headroom, which is legal. An already-invalid dwelling is refused,
**never repaired** - quietly fixing a room nobody targeted would be an unrequested design decision.

**6. A product identity that means two things is refused.** *(Codex P2)*
The model stores only `VentilationUnitReference` and looks capability up again by that identity, so a
catalogue with two entries sharing manufacturer/model/reference but rated 100/100 and 200/200 leaves no
single answer to "what did we select" - `SelectedVentilationUnitCapacityDescriptor` returned whichever came
first, making a unit's adequacy depend on catalogue order. `SelectSmallestCapableVentilationUnit` now scans
the whole valid catalogue for identity collisions before choosing anything and refuses a conflicting one,
so an ambiguous identity is never written onto an air handling unit. The refusal names the pair in a fixed
ordinal order, so the same broken catalogue produces the same sentence however it was read. The lookup is
independently defensive and returns null on a conflict, for a unit selected from one catalogue and later
checked against another.

*Classification, stated deliberately:* an exact repeat (same identity, same capacities, same rank) is a
duplicated line in a hand-edited file and stays **harmless**, matching how `SelectPreferredCapableSystem`
already treats a duplicated template entry. Conflicting **rank** on one identity is treated as
**conflicting**, because rank decides selections and two answers for it is the same defect as two answers
for a capacity. The invariant: *stored product identity -> exactly one capability meaning.*

**7. A tolerance that cannot be compared against is refused.** *(Codex P2)*
Every Iteration 2 safety rule is a comparison against `tolerance_Lps`, so `double.NaN` made the derived
allocation, the imbalance refusal and the capacity check all evaluate false at once and the transaction
reported success on an unbalanced dwelling; an infinity is the same failure wearing the opposite mask.
New `Query.IsValidFlowRateTolerance` / `Query.FlowRateToleranceRefusal` define one rule (finite, `>= 0`,
zero meaning exact) and one sentence, applied at every Iteration 2 public entry point that takes a
tolerance:

| Entry point | Behaviour on an invalid tolerance |
|---|---|
| `ApplyTargetedDesignAirFlow` | refusal, zero writes |
| `SetSpaceDesignFlowRate` | refusal, zero writes |
| `SelectVentilationUnit` | refusal, nothing written to the unit |
| `SelectSmallestCapableVentilationUnit` | `VentilationUnitSelection.Refused` |
| `ReconcileVentilationSystemDesignDuty` | refusal |
| `IsVentilationUnitSufficient` | false, with the reason |
| `CapableVentilationUnits` | empty list - the return type carries no reason, and empty can never approve an undersized unit |
| `VentilationUnitCapacityDescriptor.IsSufficientFor` | false - a predicate that cannot show sufficiency does not |

Refused, never clamped: substituting a default would hide the caller's mistake behind an answer that looks
right, and the answer is a compliance statement.

## Review round 3 - two further guards on top of the round 2 head

Codex's third pass raised no P1 and did not re-raise anything. Two P2s, both verified and both real.

**8. A negative or infinite design duty is met by nothing, not by everything.** *(Codex P2)*
A capacity check is `maximum >= duty`, so a negative duty was satisfied by every non-negative capacity and
`SelectSmallestCapableVentilationUnit` returned the smallest product on the shelf as a successful answer to
a physically impossible design. Reachable through the public selector, or from a terminal deserialized with
a negative `DesignFlowRate_Lps`. A duty must now be finite and `>= 0` - zero stays valid, because a system
with no terminal of that direction really does move nothing - checked in the selector, in
`CapableVentilationUnits`, and in `IsSufficientFor` underneath both.

**9. An air handling unit the model does not hold is refused, not inserted.** *(Codex P2)*
`SelectVentilationUnit` resolved the unit by GUID and **fell back to the caller's object** when that failed.
The duty above it is resolved by the unit's *name*, which is how a ventilation system names its plant - so
a detached unit merely sharing a name with one in the model got a duty, looked selectable, and was written
to and added. The cluster would then hold two units of that name with the product reference on the wrong
one, and every name-based lookup afterwards - `Query.VentilationSystems` and the TAS export among them -
could resolve the original, unselected unit. It now refuses: a selection nothing can find again is worse
than no selection.

## Review round 4 - scoping the compliance read, and two more guards

**10. The room-level Part F check is scoped to THIS ventilation system.** *(Codex P1)*
Fixes 3 and 5 put the compliance decision on `ReconcileVentilationSystemDesignDuty`'s per-room duty, which
was summed from **every** terminal in the room regardless of system - pre-existing Iteration 1a code, but
newly load-bearing. A room holding a foreign system's terminal therefore had that air counted towards this
system's Approved Document F floor: the room check passed while this system's own terminal stayed short,
and a little headroom in a neighbouring room carried the system total too, so both halves of the check
rested on air this system does not move. The per-room duty is now filtered to the system being reconciled.

*Filtered rather than refused, deliberately:* this is a **read**, and the honest answer to "what does this
system put into this room" is available exactly. **Writes** stay conservative -
`ApplyTargetedDesignAirFlow` still refuses outright to touch a room it cannot attribute unambiguously,
because a write cannot be filtered the same way. For the single-system dwelling the Part O workflow builds
the filtered set is identical, so Iteration 1a behaviour is unchanged.

**11. Adequacy is resolved from the cluster's own unit.** *(Codex P2)*
`IsVentilationUnitSufficient` read the product reference off the **caller's** object while
`AirHandlingUnitDesignDuty` derived the duty from the model by name, so a detached same-named unit - or a
stale copy kept from before a re-selection - could report adequate on a selection the model does not hold,
suppressing the escalation an outgrown unit needs. Both halves of the comparison now come from the same
object, resolved by GUID, and a unit the model does not hold refuses.

**12. A terminal carrying an impossible duty refuses before redistribution.** *(Codex P2)*
`DesignFlowRate_Lps` is publicly settable and deserialized without a range check, so an infinite one is
reachable. `SetSpaceDesignFlowRate` shares a room total in proportion to what each terminal already
carries, and `finite * Infinity / Infinity` is `NaN` - which would have been written and reported as the
requested total while `VentilationTerminalDesignDuty_Lps` afterwards skipped it and read a silently wrong
duty. Every existing terminal duty is validated finite and non-negative before anything is written.

## Review round 5 - closing the all-or-nothing gaps fix 12 opened

No P1. Three P2s, two of which regressed the transaction's headline contract - worth noting that a
guard added in one round can break a promise made in another.

**13. Every room the transaction will write is preflighted, not just the target.** *(Codex P2)*
Fix 12 put the terminal-validity check inside the setter, and `ApplyTargetedDesignAirFlow` writes the
target first. A derived room holding a NaN terminal beside healthy ones sums to a room total that meets
its requirement - `VentilationTerminalDesignDuty_Lps` skips NaN - so every plan check passed, the target
was written, and only then did the derived write refuse. All-or-nothing, broken by the guard meant to
protect it. The check is now shared (`Modify.IsRedistributable`) and asked of the target **and every
planned derived room before the first write**.

**14. A share smaller than the tolerance is still applied.** *(Codex P2)*
The apply loop skipped a derived room whose share was within tolerance. A change well above tolerance can
divide into shares that are each below it - 1.5 l/s across two rooms against a 1 l/s tolerance gives two
0.75 l/s shares - so all of them were skipped, the target was written, nothing balanced, and the post-write
check refused a change already made. The tolerance decides whether a *change* is worth making, which is
settled before planning; it does not get to veto the pieces that change is made of. A room is now skipped
only where it genuinely does not move.

**15. A unit with no design duty is unknown, not adequate.** *(Codex P2)*
`IsVentilationUnitSufficient` ignored `AirHandlingUnitDesignDuty`'s return value, so a unit whose systems
or terminals had been removed derived 0/0 - which every non-negative capacity satisfies - and was reported
adequate. `Modify.SelectVentilationUnit` already refuses that case; adequacy now agrees with it.

## Deliberate behaviour change carried from the first commit

`Query.ReconcileVentilationSystemDesignDuty` compared design duty and requirement with an **absolute**
difference, hard-coding `Design == Required` and making the Iteration 2 invariant impossible to express.
It is now **one-sided**: below the requirement refuses (at room level *and* system level, per fix 3), above
it is design headroom and is reported. `PartOBaseMVHRTests.ADutyThatDisagreesWithTheRequirement_Refuses`
was split into `ADutyBelowTheRequirement_Refuses` and
`ADutyAboveTheRequirement_IsReportedAsHeadroomAndNotRefused`. This is the only Iteration 1a behaviour change
in the PR.

### Files changed (this session)

`SAM` - **all Iteration 2 production changes are in `SAM.Analytical` alone.**
- `SAM/SAM/SAM.Analytical/Classes/System/VentilationUnitReference.cs` (new) - the product identity stored on the unit.
- `SAM/SAM/SAM.Analytical/Classes/System/VentilationUnitCapacityDescriptor.cs` (new) - the catalogue entry; capability only.
- `SAM/SAM/SAM.Analytical/Classes/System/VentilationUnitSelection.cs` (new) - Selected or Refused, plus headroom.
- `SAM/SAM/SAM.Analytical/Classes/System/DesignAirFlowAdjustment.cs` (new) - one room's move, carrying `IsDerived`.
- `SAM/SAM/SAM.Analytical/Classes/System/DwellingDesignAirFlowChange.cs` (new) - targeted + derived + duties, all-or-nothing.
- `SAM/SAM/SAM.Analytical/Query/CapableVentilationUnits.cs` (new) - the pure selection rule.
- `SAM/SAM/SAM.Analytical/Query/PartFRequiredFlowRate.cs` (new) - the immutable floor, per terminal / space / system.
- `SAM/SAM/SAM.Analytical/Query/AirHandlingUnitDesignDuty.cs` (new) - AHU<->system resolution, derived duty, capacity check;
  the identity lookup is conflict-defensive (fix 6).
- `SAM/SAM/SAM.Analytical/Query/IsValidFlowRateTolerance.cs` (new) - the one tolerance rule and the one refusal sentence (fix 7).
- `SAM/SAM/SAM.Analytical/Modify/SelectVentilationUnit.cs` (new) - binds a selection to one unit.
- `SAM/SAM/SAM.Analytical/Modify/SetSpaceDesignFlowRate.cs` (new) - the primitive; writes what it is told, does **not** rebalance.
- `SAM/SAM/SAM.Analytical/Modify/ApplyTargetedDesignAirFlow.cs` (new) - the transaction; fixes 1, 2 and 4 live here.
- `SAM/SAM/SAM.Analytical/Query/VentilationSystemDesignDuty.cs` - one-sided reconciliation; fix 3 (`RefuseSpace`) here.
- `SAM/SAM/SAM.Analytical/Enums/Parameter/AirHandlingUnitParameter.cs` - `VentilationUnitReference` member (enum was empty).
- `SAM/SAM/SAM.Analytical/Classes/PartOIterationPreparation.cs` - `VentilationUnitSelections`.
- `SAM/SAM/SAM.Analytical/Modify/PreparePartOIteration.cs` - optional catalogue threaded into the per-dwelling loop.
- `SAM/SAM/SAM.Tests/PartOVentilationUnitSelectionTests.cs` (new) - the Iteration 2 suite.
- `SAM/SAM/SAM.Tests/PartOBaseMVHRTests.cs` - the split reconciliation tests plus the room-level floor regression.
- `SAM/PROJECT_PROGRESS.md` (this file).

**No `SAM_Systems` and no `SAM_Tas` production changes in Iteration 2.**

*Not part of this PR:* diagnosing why `SAM_Tas` would not compile produced
`SAM_SolarCalculator/build/SAM.{Core,Geometry}.SolarCalculator.dll` as local build artefacts. They are
untracked deployment output, they belong to no repository's source, and BuildAlls regenerates them.

## Tests

`PartOVentilationUnitSelectionTests` - **67 facts** (37 at `41a02d4e`, and one replaced and thirty added
across five review rounds - eight of them `[Theory]` cases):
- Selection: smallest compliant, exact match, undersized rejected, supply/extract independent, refusal
  determinism, rank ties, catalogue-order independence.
- Authority separation: capacity never written into a requirement, capacity never taken up as design,
  design changes write no runtime airflow.
- Dwelling isolation: two dwellings select independently and pick different products; changing one does
  not touch the other.
- Targeted vs derived: exactly one targeted room; derived movements flagged and totalling the balancing
  delta; derived extract follows the existing allocation strategy.
- Network recalculation: re-preparation rebuilds transfer paths and air movements; supply == extract
  afterwards; AHU duty follows.
- Capacity: below rating keeps the unit; exactly at rating valid; above rating exhausts and escalates.
- Identity: product survives serialization on the unit and through the cluster; two units share a product
  with independent duties.

New with the review fixes:
- `AReductionConsumesAvailableHeadroom_AndReversesATargetedChangeExactly` (replaces the test that pinned
  the wrong behaviour, `ATargetedChangeThatCannotBeBalanced_RefusesAndWritesNothing`)
- `AReductionBeyondAllAvailableHeadroom_RefusesAndWritesNothing`
- `AReductionExactlyAtAvailableHeadroom_Succeeds`
- `ASpaceSharedWithAnotherVentilationSystem_RefusesAndTouchesNeitherSystem`
- `ASpaceHoldingAnOrphanTerminal_Refuses`
- `AnAlreadyUnbalancedDwelling_RefusesBeforeWritingAnything`
- `EverySuccessfulTransaction_LeavesTheDwellingBalanced`

`PartOBaseMVHRTests` - **34 facts**, including the new
`ARoomBelowItsRequirement_RefusesEvenWhereTheSystemTotalAgrees` (fix 3).

## Validation

- **`SAM.Tests`: 1551 passed, 0 failed** (1548 / 1545 / 1541 / 1527 after rounds 4/3/2/1; 1513 at Iteration 1a).
- **`SAM.Analytical.Systems.Tests`: 40 passed, 0 failed.**
- **`SAM.Analytical.Tas.TM59.Tests`: 649 passed, 0 failed** - run against the deployed Iteration 2 DLL
  after a BuildAll, with the new guards confirmed present in it, so genuinely against this work. No `SAM_Tas` production code calls any changed API; its only touchpoint is
  `PreparePartOIteration`'s four-argument form, which is source-compatible and defaults to Iteration 1a
  behaviour.
- Release build clean; PR #79 CI green at `41a02d4e` and `c67cf5d4` (`build (Release)`, `test (Release)`,
  `spdx`).

**Trap worth knowing.** `SAM_Tas` references **deployed** DLLs by `HintPath`
(`..\..\..\SAM\build\SAM.Analytical.dll`), not project references, so running its suite without a BuildAll
silently tests whatever was last deployed rather than your working tree - an earlier "649 passed" in this
session was exactly that and meant nothing. Rebuilding it also needs
`SAM_SolarCalculator\build\SAM.{Core,Geometry}.SolarCalculator.dll` present, and the COM-interop projects
need **.NET Framework MSBuild** (`dotnet build` fails with MSB4803 on `ResolveComReference`). Deploy first,
confirm `SAM\build\SAM.Analytical.dll` is newer than your changes, then run the suite.

## Codex reviews of PR #79

| Finding | Severity | Resolution |
|---|---|---|
| Allocate reductions from available headroom before refusing | P1 | Fixed - fix 1 above; the test that pinned the wrong behaviour was replaced with a reversibility test |
| Scope balancing terminals to the selected ventilation system | P1 | Fixed - fix 2 above; refuses on a shared or unattributed room, with a regression asserting zero writes in both systems |
| Record Iteration 2 in `PROJECT_PROGRESS.md` | - | Fixed - this update |

Fixes 3 and 4 were **not** found by Codex; they came out of reviewing the PR against the agreed invariant.

**Round 2, at `c67cf5d4`.** Codex did not re-raise round 1's findings (GitHub re-anchors old comment
bodies to the new head - check `original_commit_id`, not `commit_id`, to tell an old comment from a new
one). Three new findings, all verified independently and all real:

| Finding | Severity | Resolution |
|---|---|---|
| Refuse balanced dwellings with room-level shortfalls | P1 | Fixed - fix 5 |
| Reject duplicate identities with conflicting capacities | P2 | Fixed - fix 6 |
| Reject NaN tolerances before balancing | P2 | Fixed - fix 7 |

**Round 3.** No P1, nothing re-raised. Two new findings, both verified and both real:

| Finding | Severity | Resolution |
|---|---|---|
| Reject negative design duties before selecting | P2 | Fixed - fix 8 |
| Refuse air handling units outside the cluster | P2 | Fixed - fix 9 |

**Round 4.** One P1 and two P2s, all verified and all real:

| Finding | Severity | Resolution |
|---|---|---|
| Scope room-floor checks to this ventilation system | P1 | Fixed - fix 10 |
| Resolve adequacy from the cluster-owned unit | P2 | Fixed - fix 11 |
| Reject non-finite existing terminal duties | P2 | Fixed - fix 12 |

**Round 5.** No P1. Three P2s, all verified and all real:

| Finding | Severity | Resolution |
|---|---|---|
| Preflight derived-room duties before mutating the target | P2 | Fixed - fix 13 |
| Apply shares whose aggregate exceeds the tolerance | P2 | Fixed - fix 14 |
| Refuse adequacy when no design duty exists | P2 | Fixed - fix 15 |

**Trend worth reading before commissioning a sixth round.** Rounds 1-4 each found a P1; round 5 found
none, and its three P2s were all consequences of round 4's own guards rather than defects in the Iteration
2 design. The findings are converging on input-hardening against states the Part O workflow does not
produce (detached objects, infinite property values, disconnected systems). That is worth having, and it
is no longer telling us anything about the architecture.

## Remaining ambiguity / open items

- **The deliberate deferred seam: no shipped product catalogue.** Selection is a pure function over supplied
  descriptors, and the `SAM_Systems` reader mirroring `Query.SystemCapabilityDescriptors` /
  `CapabilityIndex.JSON` is what would make it reachable from Grasshopper. The Grasshopper component still
  calls the four-argument `PreparePartOIteration`; no input is exposed until there is a catalogue to feed
  it. **This is deferred on purpose, not missing by accident.**
- `SAM_Tas` validation outstanding - see Validation above.
- Out of scope and untouched: heat-recovery efficiency, `HeatRecoveryUnit` -> `SystemExchanger`,
  `AirSystem` materialisation, runtime/`ticV` mapping, fan curves, pressure/duct/SFP/acoustics, the direct
  Part O/TBD route, and any broad Part O optimisation search. Iteration 2 adds the architectural operation
  that turns one targeted room change into a valid rebalanced design - **not** an algorithm that decides
  which room to target.

## Next step

**Final review and merge decision on PR #79.** All five suites are green and `SAM_Tas` has been validated
against a deployed build. What remains is the fresh Codex review on the new head, then the merge decision.
**Iteration 3 is not started and must not be started as part of this PR.**

---

## Previous session (2026-08-27, Iteration 1a accepted)
**The block was conservation, not the inter-zone air movement record.** TAS refuses to simulate a TBD in
which any one zone's air movements do not balance - building-wide balance is not enough - and every room
of a balanced heat recovery dwelling is individually out of balance by design. Two objects close it, and
neither adjusts a design duty:

- `Modify.AddPartFTransferAirMovements` routes each space's net through `PartFAirflowNetwork`, the same
  network Approved Document F paragraph 1.25 is assessed over. Where a net cannot be routed it **refuses
  and names the room** rather than inventing a route or connecting the room to outside.
- The unit's exhaust, added by `Modify.AddAirMovementObjects` as a movement to a destination of `null`.

`Query.AirMovementResidual` then sums every movement at each node - never matching route against route,
because these flows split and recombine - and `Modify.PreparePartOIteration` refuses on any node that does
not come out at zero.

Licensed acceptance, same dwelling / weather / period as Iteration 1b: **`differing=78835` of 78 840**
hourly temperatures, against `differing=0` before this work. TM59 takes the mechanical route with zero
strategy refusals, and every sized space reads `freshAirRate=0`, so the mechanical ventilation is the air
movements and nothing else. Evidence in
[`documentation/PartO-TAS-VALIDATION.md`](documentation/PartO-TAS-VALIDATION.md) §"Iteration 1a / Base
MVHR - the block resolved (2026-08-27)".

Full `SAM.Tests`: **1464 passed, 0 failed** (was 1455, +9). `SAM.Analytical.Tas.TM59.Tests`: **633
passed, 0 failed**, unchanged.

Two pre-existing defects were found and deliberately **not** fixed here: TAS reads an air movement's
stored flow as a mass flow in kg/s while SAM writes m3/s, and `SAM.Analytical.Tas.Modify.Simulate` reports
a refused simulation as a success.

---

## Previous session (2026-08-26, Iteration 1b)
**Milestone: Iteration 1b / Base Natural Ventilation is proven end to end** from an explicitly prepared SAM
dwelling, through authored opening behaviour, TAS simulation and comparable Part O / TM59 results, without
inventing an MVHR system.

The target architecture is now recorded durably in
[`documentation/PartO-ARCHITECTURE.md`](documentation/PartO-ARCHITECTURE.md) - the five-box separation
(requirement -> route -> equipment -> operating scenario -> simulation -> result), the full iteration
algorithm including the unimplemented 1a/2/3, and the wet-room investigation. It is indexed from
`PartF-HANDOVER.md`.

Full `SAM.Tests`: **1425 passed, 0 failed** (was 1397, +28). `SAM.Analytical.Tas.TM59.Tests`:
**624 passed, 0 failed** (was 620, +4).

**Licensed TAS A/B acceptance: PASSED.** See
[`documentation/PartO-TAS-VALIDATION.md`](documentation/PartO-TAS-VALIDATION.md) §"Iteration 1b / Base
Natural Ventilation - licensed A/B acceptance (2026-08-26)".

### What was wrong with the gate this replaces
The NV gate asked one question of a string - is it `"NV"`? - and treated **every other answer as a
mechanical dwelling**. `UV`, an empty Grasshopper panel, a typo, a stale word and a model with no zones all
reached `ApplyPartFVentilationRates` and wrote Approved Document F System 4 supply and extract onto every
sized space, successfully, with nothing downstream saying an MVHR system had been invented. It closed the
NV hole and left the rule that caused it in place.

Separately, `PartOIteration.BasePassive` asserts `Mechanical Ventilation At Design Rate = True`, and that
assumption is **inside the derived `OverheatingScenario.Key`**. Preparing an NV dwelling there produced a
true simulation filed permanently under a false claim.

## Completed (this session)
- **`Enums.PartOVentilationMode`** (new): `Undefined` / `NaturalVentilation` / `MVHR`. The stated Part O
  route. No fallback member, deliberately.
- **`Query.PartOVentilationMode(string, out refusal)`** (new): a total, explicit mapping.
  `NV`/`NaturalVentilation`/`Natural Ventilation`/`BaseNaturalVentilation` and
  `MVHR`/`MVRE`/`BaseMVHR` resolve; everything else refuses **with the reason**. `MV` refuses because
  "mechanical" is not a route - Part F System 3 (continuous extract) and System 4 (supply and extract with
  heat recovery) are different buildings and only System 4 is what `PartFCalculator` sizes. `UV` refuses
  because it selects the TM59 corridor criterion for a common space and says nothing about a dwelling.
- **`Query.PartOVentilationMode(zones, dictionary, out refusal)`** (new): the one route the assessed zones
  state, naming every zone that does not settle one. Mixed routes refuse; **no assessed zones refuses**,
  where the old gate kept applying.
- **`PartOIteration.BaseNaturalVentilation`** (new member, appended so nothing is renumbered) with its
  operating assumptions: `Openings Restricted = False`, **`Mechanical Ventilation At Design Rate = False`**,
  no boost, no summer bypass. `BasePassive` is documented as the historical name for `BaseMVHR` and
  deliberately **not** renamed - the member name is inside the key, so renaming is a migration.
- **`Query.PartOIterationVentilationMode`** (new): the route each iteration is defined over. `BasePassive`
  -> MVHR, `BaseNaturalVentilation` -> NaturalVentilation, everything else refuses (delegating to
  `PartOIterationOperatingMode` so the two cannot drift).
- **`Query.PartOPartFAirflowApplication`** rewritten as a total function of the route alone - no string, no
  model, no `SystemTemplate`. `RefuseMixed` becomes `RefuseUnstatedRoute`, which covers every way a route
  fails to settle.
- **`Modify.PreparePartOIteration`** restructured into four ordered gates: the stated route, the
  iteration's route (a mismatch refuses in **both** directions), the airflow (the Approved Document F
  operating condition is now asked for **only** on the MVHR route), the authored openings (reported, never
  acted on). `PartOIterationPreparation.VentilationMode` is on the result.
- **`SAMAnalyticalPreparePartOIteration`** `1.0.2` -> `1.0.3`: new `ventilationMode` output, rewritten
  documentation, route-aware messages. No decision moved back into the component.

### The route is stated, never inferred, and never written
No `SAM_System`, `SystemTemplate` or `InternalCondition.VentilationSystemTypeName` is read to decide what is
simulated - and none is **mutated to force it**, which would put the decision back into the metadata it was
taken out of. `AStaleMVREOnTheModel_DoesNotOverrideAnExplicitNVRoute_AndIsNotRewritten` prepares a dwelling
carrying `VentilationSystemTypeName = "MVRE"` on an explicit NaturalVentilation route and asserts both
halves.

## Deliberate behaviour changes (each reported, none accidental)
| Input | Before | Now |
|---|---|---|
| `UV` | Applied System 4 airflow | Refuses, naming the corridor criterion |
| `MV` | Applied System 4 airflow | Refuses, naming Systems 3 and 4 |
| Unrecognised word / empty panel | Applied System 4 airflow | Refuses, quoting the word |
| No assessed zones | Applied System 4 airflow | Refuses |
| `NV` at `BasePassive` | Skipped, prepared | Refuses; use `BaseNaturalVentilation` |
| `MVRE` at `BaseNaturalVentilation` | n/a (member is new) | Refuses; use `BasePassive` |
| `MVRE` / `MVHR` at `BasePassive` | Applied | Unchanged |
| `NV` at `BaseNaturalVentilation` | n/a (member is new) | Skips, prepares |

## Files changed
- `SAM/SAM/SAM.Analytical/Enums/PartOVentilationMode.cs` (new)
- `SAM/SAM/SAM.Analytical/Query/PartOVentilationMode.cs` (new)
- `SAM/SAM/SAM.Analytical/Query/PartOIterationVentilationMode.cs` (new)
- `SAM/SAM/SAM.Analytical/Enums/PartOIteration.cs`
- `SAM/SAM/SAM.Analytical/Enums/PartOPartFAirflowApplication.cs`
- `SAM/SAM/SAM.Analytical/Query/PartOPartFAirflowApplication.cs`
- `SAM/SAM/SAM.Analytical/Query/PartOOperatingAssumptions.cs`
- `SAM/SAM/SAM.Analytical/Query/PartOIterationOperatingMode.cs`
- `SAM/SAM/SAM.Analytical/Modify/PreparePartOIteration.cs`
- `SAM/SAM/SAM.Analytical/Classes/PartOIterationPreparation.cs`
- `SAM/Grasshopper/SAM.Analytical.Grasshopper/Component/SAMAnalyticalPreparePartOIteration.cs`
- `SAM/SAM/SAM.Tests/PartOIterationPreparationTests.cs`
- `SAM/SAM/SAM.Tests/OverheatingScenarioTests.cs`
- `SAM/documentation/PartO-ARCHITECTURE.md` (new)
- `SAM/documentation/PartO-TAS-VALIDATION.md`
- `SAM/documentation/PartF-HANDOVER.md`
- `SAM/PROJECT_PROGRESS.md` (this file)
- `SAM_Tas/SAM_Tas/SAM.Analytical.Tas.TM59.Tests/PartONaturalVentilationWorkflowTests.cs` (separate repo)

## Tests
Section I of `PartOIterationPreparationTests` was rewritten - the cases it has to cover are different cases
now - and sections J and K added:

- `AStatedRoute_ResolvesToItsMode` [Theory x8], `AnythingElse_IsNoRouteAtAll` [Theory x9, including `MV`,
  `UV`, `EOL`, `CAV`, `MVHRR`, empty and null], `MVAndUV_AreRefusedWithTheReasonTheyAreNotRoutes`.
- `AnUnstatedRoute_RefusesAndAppliesNothing` [Theory x4] - on the production path, asserting no model, no
  scenarios and the supplied model unmutated.
- `AZoneStatingNothing_IsNamedInTheRefusal`, `NoAssessedZones_Refuse`, `MixedRoutes_*`,
  `NVBesideAZoneStatingNothing_Refuses`.
- `TheMVHRRoute_KeepsTheExistingApplication` [Theory: MVRE, MVHR] - the mechanical-path guard, proving both
  spellings are one route.
- `EachBaseIteration_StatesItsRoute`, `AnNVDwellingAtTheMVHRIteration_Refuses`,
  `AnMVHRDwellingAtTheNVIteration_Refuses`,
  `TheTwoBaseIterations_AssertOppositeMechanicalVentilationAssumptions`,
  `TheTwoBaseIterations_KeyDifferentlyOverTheSameZone`.
- `AStaleMVREOnTheModel_DoesNotOverrideAnExplicitNVRoute_AndIsNotRewritten`.
- `Iteration1b_Open_LeavesTheOpeningUnrestrictedAndCompatible`,
  `Iteration1b_Night_KeepsTheAuthoredRestrictionAndItsSchedule`,
  `Iteration1b_OpenAndNight_DifferOnlyInTheOpeningAvailability`.

Every pre-existing opening / idempotency / airflow test still passes; the NV ones now state
`BaseNaturalVentilation`.

`SAM_Tas` - `PartONaturalVentilationWorkflowTests` (11, COM-free): the fixture is parameterised by
`OpeningRestriction`, so it builds both acceptance cases. Added
`TheUnrestrictedAperture_ResolvesTheApertureControlWithNoAvailabilityRestriction`,
`BothCases_ExportAsNaturalVentilation`, `BothCases_SelectTheSameNaturalVentilationTM59Route`,
`TheTwoCases_DifferOnlyInTheOpeningAvailability`.

## Validation
- `SAM.Analytical` Debug build: 0 errors, no new warnings from the new files.
- `SAM.Analytical.Grasshopper`: 0 CS errors (the post-build deploy step fails locally with the pre-existing
  environmental `*Undefined*` path quirk; CI is green).
- Full `SAM.Tests` Debug: **1425 passed, 0 failed**.
- `SAM.Analytical.Tas.TM59.Tests` Debug: **624 passed, 0 failed**.
- Licensed headless TAS, both cases (`CIBSE Weather 2021.twd`, `Sizing = true`, `Simulate = true`,
  days 1..365), base model `C:\TasOut\v40\A0.sam`, outputs in `C:\TasOut\p1b`:
  - The two produced **TBDs differ by exactly one line** - the extra `ApertureType` carrying
    `schedule=PartO_DayOpen_08_23`, `values=000000001111111111111110`.
  - `"flow"` keys in the TBD zone descriptions: **NV-OPEN 0, NV-NIGHT 0, MVRE control 8.** No continuous
    mechanical supply was invented in either case; the authored `freshAirRate` 8 l/s/p survives, where the
    mechanical control zeroes it and writes `flow = 0.0455`.
  - TM59 from each produced TSD: **5 natural-ventilation, 0 mechanical, 4 corridor** in both, no refusals
    of any kind, every space passing.
  - **The two simulations genuinely differ**: 16 690 of 78 840 hourly resultant temperatures, with the
    largest delta in the whole model - 0.674 K - on `Bedroom 2_3`, the one space whose window was
    restricted, and in the expected direction.

## Acceptance-model decision
The **fallback** in the brief was taken deliberately: the licensed A/B adapts `C:\TasOut\v40\A0.sam` (the
9-space TM59 residential model already proven by the 2026-08-25 acceptance) rather than constructing TAS
geometry from scratch. Building a simulatable dwelling through SAM APIs means closed shells, constructions
and adjacency the TAS importer accepts - unrelated complexity that would put the acceptance's own
correctness in question. The **preferred** option is used where it costs nothing: both COM-free suites build
their dwellings from scratch through normal SAM APIs. `build_tests/Fixtures/original_v1.sam` remains
rejected (office massing, 0 zones, 0 internal conditions, 0 opening properties, in a build output
directory).

## Remaining ambiguity / open items
- **Iteration 1a (`BaseMVHR`) is not implemented.** The Part F requirement is applied on the MVHR route,
  but no physical unit is selected against it. The algorithm is recorded in `PartO-ARCHITECTURE.md` §5 and
  is the next piece.
- **Iterations 2 and 3 are not implemented** - recorded in `PartO-ARCHITECTURE.md` §2.
- **Mixed-route models refuse.** `ApplyPartFVentilationRates` is whole-model; a per-zone application is a
  separate change with its own transfer-air and balance consequences.
- **System 1 sizing is not implemented anywhere.** Zero continuous mechanical airflow means no MVHR/MVRE
  system was invented; it does NOT mean the dwelling's natural-ventilation Part F design has been sized. Do
  not report the NV result as "Part F NV sizing".
- **Wet-room intermittent extract has no runtime behaviour, deliberately.** SAM parses
  `PartFCategory.IntermittentExtractRate_Lps` from the rule set and reads it nowhere; a wet room actually
  receives a *continuous* balanced extract because `PartFCalculator` is System 4 shaped for every route;
  nothing carries an operating schedule or control for an intermittent extract; and `SAM.Analytical.Tas`
  has **no TBD write path for exhaust at all** (`ExhaustAirFlow` is read only by `PartODiagnosticLog`).
  The rate is preserved as data. Full write-up in `PartO-ARCHITECTURE.md` §6.
- **`OverheatingScenario:v2` remains deferred.** The stage still asserts `Openings Restricted`, so NV-NIGHT
  is still reported `Incompatible` and still only warned about, and the two acceptance cases still share a
  scenario key - correctly, since opening behaviour is a property of the model rather than of the stage.
- **Neither acceptance case fails.** The result pipeline has been shown to report a pass truthfully; it has
  not been shown on this model and weather to report a fail.
- The TM59 assessment must be given the **workflow output** model, not the pre-workflow one -
  `SimulationSpaceMap` resolves on the `ZoneGuid` that `Modify.UpdateIds` stamps during the workflow.
  Pre-existing behaviour, recorded because it silently produces zero results.

## Next step
1. Push, update PR [SAM#76](https://github.com/SAM-BIM/SAM/pull/76) and
   [SAM_Tas#43](https://github.com/SAM-BIM/SAM_Tas/pull/43), run CI. **Do not merge.**
2. **Iteration 1a / `BaseMVHR`**: apply the Part F continuous requirement on the explicit MVHR route and
   select the minimum compliant unit from `MVHR_Template`. The unit's capacity is equipment capability and
   must never become the source of the requirement.
3. Per-zone Part F airflow application, which is what turns today's mixed-route refusal into a real answer.
   It needs a decision on transfer air and dwelling balance across a route boundary before any code.
4. `OverheatingScenario:v2` - moving `Openings Restricted` from the stage to the model.

---

## Historical session note - the first NV gate (same branch, commit `0c8a04eb`)

The commit that closed the NV hole. `PreparePartOIteration` stopped carrying Approved Document F continuous
mechanical supply and extract onto a naturally ventilated dwelling, and the whole preparation moved into the
library so the component and the tests run the same code. `SAM.Tests` 1397, `SAM.Analytical.Tas.TM59.Tests`
620; licensed acceptance recorded in `PartO-TAS-VALIDATION.md` §"Natural-ventilation Part O workflow".

Its mechanics are superseded by the work above - the gate was `Query.PartOPartFAirflowApplication(zones,
dictionary, out diagnostic)` reading the string `"NV"`, at `PartOIteration.BasePassive`, and both of those
now refuse - but the defect it identified and the wording it pinned are unchanged:

- `SAMAnalyticalPreparePartOIteration.SolveInstance` called `Modify.ApplyPartFVentilationRates`
  **unconditionally, and before `_ventilationStrategies` was read at all**.
- `PartFCalculator` is unconditionally System 4 shaped - paragraph 1.67 gives every habitable room a
  mechanical supply terminal, and nothing in `SAM.Analytical/Classes/PartF/` takes a ventilation strategy.
- So an NV dwelling was simulated with mechanical supply/extract it does not have, **successfully**, with
  nothing in the result saying the system was invented. Mirror failure: an NV dwelling with no
  `PartFSpaceData` was refused outright with "run a Part F component first".

---

## Historical session notes (previous branch `feature/partf-transfer-door-panel-selection`, merged as PR #74)

### Branch
`feature/partf-transfer-door-panel-selection` (off `sow/2026-Q3`; PR #73 already merged). UNCOMMITTED —
implementation and tests complete and reviewed-pending; do not push until reviewed.

### Last updated
2026-08-25 - transfer-door panel selection for split shared walls implemented, tested, ready for review.

### Status at the time
`Modify.AddTransferAirDoorsByPartF` no longer refuses a route just because two shared wall panels can each
take the generated door. The host panel is resolved by a fixed selection hierarchy: host validity
(`TryTransferAirDoorGeometry`, unchanged, always first) -> geometric relevance (the panel the direct line
between the two space locations passes through scores 0, the others score the distance from that line) ->
shorter valid shared wall (geometric ties within `Core.Tolerance.Distance`) -> the stable first candidate
from the guid-sorted list (equal lengths within `Core.Tolerance.Distance`). A route is never refused merely
because two candidates are equal; a space with no valid location (`Space.IsPlaced()` false - missing or
NaN) still refuses cleanly because there is no valid primary geometric ranking. Selection is independent of
candidate creation/enumeration order; guid order is the absolute final deterministic fallback only.
Full `SAM.Tests`: **1354 passed, 0 failed** (Debug and Release; was 1344, +10 net).

### Real-model acceptance run (SAM_zoningAM_v1.sam, 2026-08-05-PartO)
Ran `AddTransferAirDoorsByPartF("Flats", ...)` against the ACTUAL model (9 spaces, 50 panels): **5 doors
created, 0 refusals**, exactly one door per route, no duplicates.

- **Studio 1_0 -> Bathroom_2**: candidates `7e09a798` (vertical, x=5, y in [5,10], 5 m) and `3e01ed80`
  (horizontal, y=5, x in [5,10], 5 m) - the two legs of the L-shaped partition meeting at (5,5). The
  centroid diagonal passes exactly through the corner: GEOMETRIC TIE, and both legs are the SAME length
  (5 m), so the stable first candidate won: `3e01ed80` (horizontal partition). Door `b9517704` created,
  centred at x=7.5. (The two legs are genuinely identical - only the documented final fallback separates
  them.)
- **Kitchen_4 -> Ensuite_5**: candidates `69de3fb5` (vertical, 5 m) and `fe27dac4` (horizontal, y=5,
  x in [25,31], 6 m). Geometric winner `fe27dac4` (score 0 - crossed at x=25.75; the other stands 0.833 m
  off). Door `80cd38c7` centred at x=28 - the horizontal partition from the screenshot.
- **Kitchen_7 -> Ensuite_8**: same shape: geometric winner `ab1b0798` (horizontal, y=5, x in [46,52], 6 m;
  other `b154e0b7` 0.833 m off). Door `d75ec2e5` centred at x=49.
- Bedroom 2_3 -> Kitchen_4 and Bedroom 2_6 -> Kitchen_7 (single candidates) created as before.

### Completed then
- `Modify.AddTransferAirDoorsByPartF`: the `candidates.Count > 1` refusal was replaced by the selection
  hierarchy: host validity (`TryTransferAirDoorGeometry` / `Query.ApertureHost`, always first, unchanged) ->
  geometric relevance (`TransferAirDoorPanelScore`: 0 where the centre segment passes through the panel,
  otherwise the distance from the segment to the panel, with exact handling for degenerate segments and
  segments parallel to the panel plane) -> shorter valid shared wall (`WallLength`, the bottom-edge length,
  for geometric ties within `Core.Tolerance.Distance`) -> the stable first candidate from the guid-sorted
  list (equal lengths within `Core.Tolerance.Distance`). A route is never refused merely because candidates
  are geometrically and dimensionally equal. A concise `notes` entry names the chosen panel, the reason and
  the rejected candidates' distances. Remaining refusals: unscoreable panel geometry (defensive), and a
  space that is not `Space.IsPlaced()` (missing or NaN location) - no valid primary geometric ranking, so
  no winner is ever manufactured from invalid geometry.
- `SAMAnalyticalAddTransferAirDoorsByPartF` (GH): component description documents the selection hierarchy.
- Codex review fixes (PR #74, both accepted):
  - **P1 - no NaN refusal for walls beyond the segment.** A candidate whose plane the direct line crosses
    only BEYOND the bounded segment used to score NaN and refuse the whole route. It now scores the finite
    distance from its nearer endpoint to the panel region (`DistanceToPanel`) and simply loses.
  - **P2 - coincident locations scored against the panel region.** A point facing the middle of a large
    wall was scored by its distance to the wall's EDGES, overstating the offset; the score is now the
    perpendicular offset where the projection falls inside the panel, edge distance only otherwise.
    `DistanceToPanel(Face3D, Point3D)` is the single helper for both; the NaN guard in the selection block
    is now truly defensive-only.
- Tests (`PartFTransferAirDoorTests`, 25 total):
  - `SplitWall_DirectLineCrossesOnePanel_DoorCreatedThere` [Theory, both creation orders]: door in the
    crossed panel, other panel untouched, selection note present.
  - `TwoParallelWalls_DifferentLengths_ShorterWallSelected` [Theory, both creation orders]: geometric tie,
    10 m vs 4 m -> the 4 m wall wins in both orders.
  - `TwoParallelWalls_EqualLengths_StableFirstCandidateSelected` [Theory, both creation orders]: geometric
    tie, both 10 m -> the guid-first panel wins in both orders, "stable first candidate" reported.
  - `SplitWall_DirectLineHitsTheJoint_StableFirstCandidateSelected`: joint crossing is a geometric tie,
    equal 5 m lengths -> guid-first panel selected.
  - `TwoSharedWalls_SpaceLocationInvalid_RefusedCleanly` [Theory, missing and NaN location]: still refused
    with "no valid location", candidates untouched, no winner manufactured.
  - `SplitWall_SecondCandidateBeyondTheLocations_DoorStillCreated` [Codex P1]: crossed wall wins, the
    beyond-the-segment wall scores 2 m and loses, no refusal.
  - `CoincidentLocations_ProjectionInsidePanel_ShorterPerpendicularWallWins` [Codex P2]: the wall whose
    interior faces the point wins over a narrower wall whose edges are nearer.
  - `ExampleModelFlatPairs_SplitSharedWalls_DoorLandsOnTheCrossedPanel`: reproduces the three reported pairs
    (Flat 1 Studio 1_0->Bathroom_2, Flat 2 Kitchen_4->Ensuite_5, Flat 3 Kitchen_7->Ensuite_8) as split
    walls; all 5 routes' doors land on the crossed panel.
  - The previous "two walls both fit -> refuse" behaviour is gone by design; single-candidate behaviour is
    unchanged.

### Files changed then
- `SAM/SAM/SAM.Analytical/Modify/AddTransferAirDoorsByPartF.cs`
- `SAM/SAM/SAM.Tests/PartFTransferAirDoorTests.cs`
- `SAM/Grasshopper/SAM.Analytical.Grasshopper/Component/SAMAnalyticalAddTransferAirDoorsByPartF.cs`
- `SAM/documentation/PartF-HANDOVER.md`
- `SAM/PROJECT_PROGRESS.md` (this file)

### Validation then
- `SAM.Analytical` Debug build: 0 errors.
- Focused `PartFTransferAirDoorTests`: 25/25 passed.
- Full `SAM.Tests` Debug: **1354 passed, 0 failed**. Full `SAM.Tests` Release: **1354 passed, 0 failed**.
- `SAM.Analytical.Grasshopper` compiles with 0 CS errors; its post-build deploy step fails locally with the
  pre-existing `::erase` quirk (environmental - CI uses `RunPostBuildEvent=OnOutputUpdated` and is green).
- Real-model acceptance run re-checked after the Codex fixes: 5 doors, 0 refusals, same panel selections
  as before.

### Open items then
- Flat 1's Studio->Bathroom pair is a true geometric AND dimensional tie (two identical 5 m legs of the
  L-shaped partition, the centroid diagonal passing exactly through their corner): it is resolved only by
  the documented final fallback (stable first candidate `3e01ed80`). Recorded here so it is not mistaken
  for a geometric choice.
- A space with no valid location (missing/NaN) still refuses cleanly in the multi-candidate branch; covered
  by `TwoSharedWalls_SpaceLocationInvalid_RefusedCleanly`.
- No commit/push yet - awaiting review.

---

## Historical session notes (previous branch, merged as PR #73)
`feature/partf-terminal-transfer-compliance` (PR: SAM#73 against `sow/2026-Q3`)

## Current status (previous session - merged as PR #73)
Eight Codex findings implemented with regression tests. The TAS availability schedule export has PASSED
licensed acceptance (see below), so the Part O schedule foundation is proven end to end and the earlier
acceptance-failure hypothesis is withdrawn. Three behaviour/policy findings are explicitly DEFERRED for a
dedicated decision and are deliberately not part of this work.
Full `SAM.Tests`: **1344 passed, 0 failed** (Debug and Release; was 1341, +3 new in the final pass).

## Completed (this session)
- `Core.Query.TryGetEnum`: case-insensitive enum fallback made culture-invariant (`ToUpperInvariant`) -
  under a Turkish culture `"nightclosed"` previously uppercased with a dotted capital I and was silently
  rejected, so `SAMAnalytical.AddOpeningPropertiesByPartO` substituted `Unrestricted`. (Codex 3819617732)
- `VentilationStrategyMap`: the same defect class - strategy normalisation is now `ToUpperInvariant`, so
  a lowercase "disp" no longer reads as an unrecognised strategy under a Turkish culture.
  (Codex 3802695368)
- `DailyAvailabilitySchedule.FromJsonObject`: each `Values` element must be a genuine JSON boolean
  (`JsonValue` + `TryGetValue<bool>`); a string/number/null element now fails deserialization cleanly
  instead of `GetValue<bool>()` throwing out of the load. (Codex 3821025633)
- `PartOOpeningProperties.FromJsonObject`: a PRESENT but unrecognised `OpeningRestriction` name fails
  deserialization (via `TryGetEnum`) instead of silently reading as `Unrestricted`; the ABSENT key keeps
  meaning `Unrestricted`, which is the correct legacy behaviour. (Codex 3815926714)
- `PartFPurgeAssessor`: a non-finite provided opening area (NaN/infinity) is `CannotBeDetermined` instead
  of falling through the NaN comparisons to PASS. (Codex 3821633858)
- `Modify.AddTransferAirDoorsByPartF`: `doors_Created` now returns the PERSISTED aperture (re-read from
  the cluster after `SetPartFDoorTransferData`'s replacement), so the returned door carries the
  `PartFDoorTransferData` record like the door in the returned model. (Codex 3820935573)
- `Modify.ApplyPartFVentilationRates`: the generated-name set is seeded from every EXISTING
  internal-condition name, so a clone can no longer collide with an untouched condition whose name already
  matches the `<condition> - <space>` pattern (TAS identifies conditions by name). (Codex 3821025626)
- `TMOverheatingCalculator.Collect`: an hour is assessed only where BOTH comfort bounds exist, never
  against the `IndexedDoubles` default of 0. The comfort series is bounded by the weather year
  (365 days = 8760 hours), so an 8784-hour leap-year simulation previously assessed its last 24 hours
  against a 0 degC comfort limit and manufactured exceedances. (Codex 3821025624)
- `SAMAnalyticalCheckPartFCompliance`: `extractAllocationStrategy_` now guards with `Enum.IsDefined` like
  the operating-mode branch below it, so numeric text such as "9" is refused instead of selecting an
  undocumented hybrid allocation. (Codex 3796859272)
- `SAMAnalyticalPreparePartOIteration`: the BasePassive restriction-reset lines are ALSO raised as GH
  runtime warnings, not only on the `notes` output, so the reset is visible on the canvas rather than
  buried in a list output.

## Final pre-merge pass (this session) - backlog triage and fixes

The rounds 1-2 Codex backlog (17-19 Aug) was re-triaged against current source. Every thread now has a
classification. Implemented in this pass:

- `TMOverheatingCalculator.Collect`: the loop is bounded by the SHORTER of the two hourly series. A
  truncated resultant-temperature series previously threw `ArgumentOutOfRangeException` out of the whole
  TM52/TM59 run. (Codex 3796859276)
- `TM59AssessmentCalculator.RestoreDesignInternalConditions`: the design condition is restored onto a COPY
  of each simulated space, never onto the caller's instance - the cluster getter's shallow copy shares
  Space objects, so the previous in-place assignment contaminated the caller's raw simulation model.
  (Codex 3811381358)
- `TMOverheatingCalculator.Evaluate`: TM59 space applications are classified from the model's own restored
  space (`space_Temp`), not the stale listed instance - explicitly scoped assessments receive the map's
  retained pre-restore spaces, which selected the wrong TM59 result type or corridor fallback.
  (Codex 3804690049)
- `SAMAnalyticalSetPartFCommissioningData`: a `_zoneName` matching more than one zone is refused rather
  than resolved to the first, so commissioning evidence can no longer be written to the wrong dwelling.
  (Codex 3796737963)
- `SAM_PartFSpaceRulesUKDwellingsMVHR.json`: removed the stale claim that a kitchen's 13 l/s Table 1.2
  minimum can raise the continuous design rate (the code sets it to max(bedroom-or-habitable, area) only),
  in the migration note and the two formula strings. (Codex 3803896492)

**Classified DEFERRED POLICY (next branch - behaviour/policy changes to reported outcomes):**
imbalanced disconnected airflow components (3795669833/3814057499), reversed high-rate schematic paths
(3795832901), zone-marking basis for scenario classification (3796737956), creation-time strategy
vocabulary validation (3796737970), preservation of site evidence across excluded spaces (3804873037),
clearing stale door-transfer records (3804873038), headline status when requested spaces produce no
result (3805522563), overall compliance with unclassified rooms (3805522570), floor-finish requirement
for undercuts (3806862925), free area proven from an upper-bound width (3806862928), opening angle vs
purge row validation (3806914162), unreachable transfer endpoints blocking compliance (3814057488).

**Confirmed already fixed by earlier commits:** 3795669854, 3795768191, 3795768196, 3795832892,
3795832897, 3795895972, 3795895977, 3795895959/3796859273 (Solver2D budget), 3796859271, 3814305690.

## TAS availability schedule export - licensed acceptance PASSED
The schedule foundation is proven on the licensed TAS PC against SAM_Tas `7ef2aff3`:

- 20 schedules requested, **20 assigned**, **0 assignment or persistence warnings**.
- `PartO_DayOpen_08_23` visually correct in TAS: 00:00-08:00 OFF, 08:00-23:00 ON, 23:00-24:00 OFF.
- Slot 24 verified separately with `openingHour = 23`, `closingHour = 24`.

The root cause was the TAS adapter's schedule mapping, fixed in SAM_Tas: TBD's hourly indexed properties
are 1-based hour-ending (SAM hour `h` -> TBD slot `h + 1`), and COM hands VARIANT_BOOL TRUE back as -1.

The earlier "BasePassive reset" acceptance-failure hypothesis recorded here is **withdrawn** - it was not
the defect. `Modify.ResetPartOOpeningRestrictions` behaves as designed; the GH warnings added above remain
useful for reading a BasePassive run, but they are diagnostics, not a fix.

## Deferred - valid findings NOT implemented here
These are behaviour/policy changes that alter reported outcomes. They need a dedicated decision and must
not be folded into a review-fix commit.

- **Codex 3815926703 - zero occupied-hour TM59 spaces.** Technically valid concern, but changing the
  behaviour from an assessment result to a refusal changes report outcomes and requires a dedicated TM59
  policy decision. (Today: a naturally ventilated space with no occupied hour reads zero exceedances as a
  PASS, and a bedroom's Criterion 2 passes 0 against a limit of 0; a mechanically ventilated one reads
  `0 < 0` as a FAIL.)
- **Codex 3821633849 - Criterion 1 annual vs summer basis.** `TM59AssessmentReport` pairs the ANNUAL
  `GetOccupiedHoursExceedingComfortRange()` with the SUMMER limit and basis, while `Pass` derives from the
  annual count against the annual `MaxExceedableHours`, so a row can show an actual above its displayed
  limit and still read Pass. Confirmed real. Filtering the actual and the verdict to summer would change
  pass/fail for every naturally ventilated space (annual 262 against TAS's own 110 on the Flat1
  BasePassive reference), so it is a regulatory decision, not a review fix.
- **Codex 3802695375 - `successful` is true when no scenarios are produced.** `successful` is
  `refusals.Count == 0`, and a model with no zones adds only a warning. That case is deliberately
  supported - a single-house model carries no zones - so requiring at least one scenario would flip a
  supported configuration to failure. Needs a decision on what `successful` should mean.

D2 (aperture matching) and D3 (schedule-removal transition, Codex 3821601792) remain parked. **No D2 or
D3 code exists in this repository or in SAM_Tas.**

## Files changed
- `SAM/SAM/SAM.Core/Query/TryGetEnum.cs`
- `SAM/SAM/SAM.Analytical/Classes/DailyAvailabilitySchedule.cs`
- `SAM/SAM/SAM.Analytical/Classes/OpeningProperties/PartOOpeningProperties.cs`
- `SAM/SAM/SAM.Analytical/Classes/PartF/PartFPurgeAssessor.cs`
- `SAM/SAM/SAM.Analytical/Classes/TMOverheatingCalculator.cs`
- `SAM/SAM/SAM.Analytical/Classes/TM59AssessmentCalculator.cs`
- `SAM/SAM/SAM.Analytical/Classes/VentilationStrategyMap.cs`
- `SAM/SAM/SAM.Analytical/Modify/AddTransferAirDoorsByPartF.cs`
- `SAM/SAM/SAM.Analytical/Modify/ApplyPartFVentilationRates.cs`
- `SAM/Grasshopper/SAM.Analytical.Grasshopper/Component/SAMAnalyticalCheckPartFCompliance.cs`
- `SAM/Grasshopper/SAM.Analytical.Grasshopper/Component/SAMAnalyticalPreparePartOIteration.cs`
- `SAM/Grasshopper/SAM.Analytical.Grasshopper/Component/SAMAnalyticalSetPartFCommissioningData.cs`
- `files/resources/Analytical/SAM_PartFSpaceRulesUKDwellingsMVHR.json`
- `SAM/SAM/SAM.Tests/DailyAvailabilityScheduleTests.cs` (+1 theory, 5 cases)
- `SAM/SAM/SAM.Tests/PartOOpeningPropertiesTests.cs` (+2)
- `SAM/SAM/SAM.Tests/PartFPurgeAndComplianceTests.cs` (+1)
- `SAM/SAM/SAM.Tests/PartFTransferAirDoorTests.cs` (+1)
- `SAM/SAM/SAM.Tests/PartFAirflowApplicationTests.cs` (+1)
- `SAM/SAM/SAM.Tests/TMOverheatingCalculatorTests.cs` (+2)
- `SAM/SAM/SAM.Tests/TM59AssessmentCalculatorTests.cs` (+2)
- `SAM/SAM/SAM.Tests/VentilationStrategyTests.cs` (+1)
- `SAM/PROJECT_PROGRESS.md` (this file)

## Validation
- Focused runs for the retained fixes: 12/12 passed.
- Full `SAM.Tests` Debug: **1341 passed, 0 failed** (was 1329; +12 new).
- Full `SAM.Tests` Release: **1341 passed, 0 failed**.
- Final pass: focused TM59 runs 19/19, then full `SAM.Tests` Debug **1344 passed, 0 failed** and
  Release **1344 passed, 0 failed**.
- `SAM.Analytical.Grasshopper` and `SAM.Analytical` compile with 0 CS errors; `SAM.Analytical.Grasshopper`
  post-build deploy step fails locally with the pre-existing `*Undefined*` xcopy quirk (environmental -
  CI uses `RunPostBuildEvent=OnOutputUpdated` and is green).

## Issues / blockers
- None blocking. The deferred findings above are the outstanding decisions.
- Fresh Codex review on the current head is unavailable: `@codex review` is refused by
  `chatgpt-codex-connector[bot]` ("create a Codex account and connect to github"). The delta after the
  latest Codex-reviewed SHA (`9b4e46fe`) was reviewed manually. GitHub reports every Codex thread as
  unresolved because the bot never resolves them - every thread is now triaged here.

## Next step
- Merge PR #73, then SAM_Systems #14, SAM_Tas #29, SAM_Tas_Grasshopper #4 (dependency chain).
- Decide the deferred findings (3815926703, 3821633849, 3802695375 and the DEFERRED POLICY set above)
  as a TM59/Part O policy pass on the NEXT branch.
