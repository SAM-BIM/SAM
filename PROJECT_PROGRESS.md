# Project Progress

## Branch
`feature/partf-transfer-door-panel-selection` (off `sow/2026-Q3`; PR #73 already merged). UNCOMMITTED —
implementation and tests complete and reviewed-pending; do not push until reviewed.

## Last updated
2026-08-25 - transfer-door panel selection for split shared walls implemented, tested, ready for review.

## Current status (this session)
`Modify.AddTransferAirDoorsByPartF` no longer refuses a route just because two shared wall panels can each
take the generated door. The host panel is resolved by a fixed selection hierarchy: host validity
(`TryTransferAirDoorGeometry`, unchanged, always first) -> geometric relevance (the panel the direct line
between the two space locations passes through scores 0, the others score the distance from that line) ->
shorter valid shared wall (geometric ties within `Core.Tolerance.Distance`) -> the stable first candidate
from the guid-sorted list (equal lengths within `Core.Tolerance.Distance`). A route is never refused merely
because two candidates are equal; a space with no valid location (`Space.IsPlaced()` false - missing or
NaN) still refuses cleanly because there is no valid primary geometric ranking. Selection is independent of
candidate creation/enumeration order; guid order is the absolute final deterministic fallback only.
Full `SAM.Tests`: **1352 passed, 0 failed** (Debug and Release; was 1344, +8 net).

## Real-model acceptance run (SAM_zoningAM_v1.sam, 2026-08-05-PartO)
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

## Completed (this session)
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
- Tests (`PartFTransferAirDoorTests`, 23 total):
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
  - `ExampleModelFlatPairs_SplitSharedWalls_DoorLandsOnTheCrossedPanel`: reproduces the three reported pairs
    (Flat 1 Studio 1_0->Bathroom_2, Flat 2 Kitchen_4->Ensuite_5, Flat 3 Kitchen_7->Ensuite_8) as split
    walls; all 5 routes' doors land on the crossed panel.
  - The previous "two walls both fit -> refuse" behaviour is gone by design; single-candidate behaviour is
    unchanged.

## Files changed
- `SAM/SAM/SAM.Analytical/Modify/AddTransferAirDoorsByPartF.cs`
- `SAM/SAM/SAM.Tests/PartFTransferAirDoorTests.cs`
- `SAM/Grasshopper/SAM.Analytical.Grasshopper/Component/SAMAnalyticalAddTransferAirDoorsByPartF.cs`
- `SAM/documentation/PartF-HANDOVER.md`
- `SAM/PROJECT_PROGRESS.md` (this file)

## Validation
- `SAM.Analytical` Debug build: 0 errors.
- Focused `PartFTransferAirDoorTests`: 23/23 passed.
- Full `SAM.Tests` Debug: **1352 passed, 0 failed**. Full `SAM.Tests` Release: **1352 passed, 0 failed**.
- `SAM.Analytical.Grasshopper` compiles with 0 CS errors; its post-build deploy step fails locally with the
  pre-existing `::erase` quirk (environmental - CI uses `RunPostBuildEvent=OnOutputUpdated` and is green).
- Real-model acceptance run above: 5 doors created, 0 refusals; Kitchen->Ensuite in Flats 2 and 3 land on
  the horizontal partitions (geometric winners); Flat 1's equal-length L-corner pair resolves to the stable
  first candidate `3e01ed80`.

## Remaining ambiguity / open items
- Flat 1's Studio->Bathroom pair is a true geometric AND dimensional tie (two identical 5 m legs of the
  L-shaped partition, the centroid diagonal passing exactly through their corner): it is resolved only by
  the documented final fallback (stable first candidate `3e01ed80`). Recorded here so it is not mistaken
  for a geometric choice.
- A space with no valid location (missing/NaN) still refuses cleanly in the multi-candidate branch; covered
  by `TwoSharedWalls_SpaceLocationInvalid_RefusedCleanly`.
- No commit/push yet - awaiting review.

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
