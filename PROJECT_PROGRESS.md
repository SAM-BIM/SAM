# Project Progress

## Branch
`feature/partf-terminal-transfer-compliance` (PR: SAM#73 against `sow/2026-Q3`)

## Last updated
2026-08-20 - Codex PR #73 review round 3 closed; reconciled with the licensed-PC schedule acceptance.

## Current status
Eight Codex findings implemented with regression tests. The TAS availability schedule export has PASSED
licensed acceptance (see below), so the Part O schedule foundation is proven end to end and the earlier
acceptance-failure hypothesis is withdrawn. Three behaviour/policy findings are explicitly DEFERRED for a
dedicated decision and are deliberately not part of this work.
Full `SAM.Tests`: **1341 passed, 0 failed** (Debug and Release; was 1329, +12 new).

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
- `SAM/SAM/SAM.Analytical/Classes/VentilationStrategyMap.cs`
- `SAM/SAM/SAM.Analytical/Modify/AddTransferAirDoorsByPartF.cs`
- `SAM/SAM/SAM.Analytical/Modify/ApplyPartFVentilationRates.cs`
- `SAM/Grasshopper/SAM.Analytical.Grasshopper/Component/SAMAnalyticalCheckPartFCompliance.cs`
- `SAM/Grasshopper/SAM.Analytical.Grasshopper/Component/SAMAnalyticalPreparePartOIteration.cs`
- `SAM/SAM/SAM.Tests/DailyAvailabilityScheduleTests.cs` (+1 theory, 5 cases)
- `SAM/SAM/SAM.Tests/PartOOpeningPropertiesTests.cs` (+2)
- `SAM/SAM/SAM.Tests/PartFPurgeAndComplianceTests.cs` (+1)
- `SAM/SAM/SAM.Tests/PartFTransferAirDoorTests.cs` (+1)
- `SAM/SAM/SAM.Tests/PartFAirflowApplicationTests.cs` (+1)
- `SAM/SAM/SAM.Tests/TMOverheatingCalculatorTests.cs` (+1)
- `SAM/SAM/SAM.Tests/VentilationStrategyTests.cs` (+1)
- `SAM/PROJECT_PROGRESS.md` (this file)

## Validation
- Focused runs for the retained fixes: 12/12 passed.
- Full `SAM.Tests` Debug: **1341 passed, 0 failed**.
- Full `SAM.Tests` Release: **1341 passed, 0 failed** (was 1329; +12 new).
- `SAM.Analytical.Grasshopper` builds with 0 CS errors (VS Framework MSBuild).

## Issues / blockers
- None blocking. The three deferred findings above are the outstanding decisions.
- The wider Codex backlog on PR #73 (rounds 1-2, 17-18 Aug) was not re-triaged in this pass. GitHub
  reports every Codex thread as unresolved because the bot never resolves them, so that count is not a
  backlog measure - several are already closed by committed work. A dedicated sweep is needed to say
  which remain.

## Next step
- Decide the three deferred findings (3815926703, 3821633849, 3802695375) as a TM59/Part O policy pass.
- Re-triage the rounds 1-2 Codex backlog against current source.
