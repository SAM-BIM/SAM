# Project Progress

## Branch
`feature/partf-terminal-transfer-compliance` (PR: SAM#73 against `sow/2026-Q3`)

## Last updated
2026-08-20 — Part F transfer-air door creation landed.

## Current status
`Modify.AddTransferAirDoorsByPartF` + Grasshopper `SAMAnalytical.AddTransferAirDoorsByPartF` are
code-complete and tested. SAM_UI needed NO change: its Part F assessment already delegates to the shared
`PartFCalculator`, so there is still exactly one transfer-air algorithm, in SAM.Analytical.

## Completed
- New analytical operation `Modify.AddTransferAirDoorsByPartF(AnalyticalModel, zoneCategoryName, double? setbackFlowRateFactor, out List<Aperture> doors_Created, out List<string> notes, out List<string> refusals)`:
  re-runs the SAME `PartFCalculator` (deep-clone, idempotent, engineering inputs carried forward), then
  creates ONE default internal door for every dwelling transfer route that carries air
  (`|continuous| or |high| > PartFAirflowNetwork.Tolerance_Lps`) but has no modelled door
  (`PartFDoorTransferData.IsDoorRepresented == false`).
- Door: `SIM_INT_SLD` (default aperture construction library, Door/WallInternal) where available, else a
  plain `Internal Door` construction (noted); 760 mm wide = paragraph 1.25 reference width
  (`PartFDoorTransferData.ReferenceDoorWidth_mm`, so a 10 mm undercut = exactly 7,600 mm2); 2,100 mm high
  (programme's documented default, PartF-HANDOVER-ARCHIVE §6a); stands on the wall's bottom edge; centred
  on the clearest length of wall (panel centre preferred, clamped inside the nearest free interval clear
  of existing apertures); placement computed in the panel's own plane (orientation-independent).
- Candidate walls: panels related to BOTH route spaces by guid (`GetPanels(LogicalOperator.And, ...)`),
  filtered to `PanelGroup.Wall`; ordered largest-area-first then guid (deterministic, not refused).
- The route's `PartFDoorTransferData` is updated (ApertureGuid, IsDoorRepresented, ClearDoorWidth_mm from
  geometry), re-assessed via `PartFTransferPathBuilder.Assess`, and written through
  `AdjacencyCluster.SetPartFDoorTransferData` (the persisting path; `Panel.Apertures` returns clones).
  Provided undercut is deliberately NOT recorded: created is not compliant (CannotBeDetermined).
- Refusals (no silent geometry): no shared internal wall (e.g. floor-only adjacency), wall too small,
  no free length alongside existing apertures, panel rejected the geometry.
- New GH component `SAMAnalytical.AddTransferAirDoorsByPartF` (guid dd4e991a-40b4-4ecd-9629-9bd7e04e89fd),
  inputs `_analyticalModel`, `zoneCategoryName_`, `setbackFlowRateFactor_` (same semantics as upstream);
  outputs `analyticalModel`, `doors`, `notes`, `unresolved`.
- Tests: `SAM.Tests/PartFTransferAirDoorTests.cs` — 10 tests covering: existing door untouched +
  identity kept + inputs survive; one door created with correct panel/geometry/construction/record and
  original model unchanged; window collision avoided; rerun creates nothing; no-route and zero-flow route
  unchanged; floor-only adjacency refused; too-narrow wall refused; duplicate room names across two flats
  resolve by identity (door lands in the right dwelling's panel).
- Test infra: `SAM_ApertureConstructionLibrary.JSON` copied and seeded into `ActiveSetting`
  (`SAMResourcesModuleInitializer`) per the documented pattern, so the default-construction path is
  exercised on clean machines too.

## Decisions / assumptions
- The "SAM_UI Check Part F" algorithm IS `PartFCalculator` in SAM.Analytical; SAM_UI only renders/edits.
  No extraction was needed; the new operation wraps the shared calculator. No competing algorithm exists.
- No 0.7 m default door width exists anywhere in SAM/SAM_UI. The only Part F door width is the Approved
  Document's 760 mm reference width, which is what created doors use.
- The undercut "sizing" is the fixed paragraph 1.25 requirement (7,600 mm2; 10 mm finished / 20 mm
  unfinished datum) — it is NOT flow-dependent in the existing methodology, and was not made so.
- Doors are created only where transfer air actually flows; zero-flow routes (e.g. two adjacent supplied
  bedrooms) get none.
- The operation re-runs the calculator, so callers must pass the same `zoneCategoryName_` /
  `setbackFlowRateFactor_` as upstream (documented on the component).

## Files changed
- `SAM/SAM/SAM.Analytical/Modify/AddTransferAirDoorsByPartF.cs` (new)
- `SAM/Grasshopper/SAM.Analytical.Grasshopper/Component/SAMAnalyticalAddTransferAirDoorsByPartF.cs` (new)
- `SAM/SAM/SAM.Tests/PartFTransferAirDoorTests.cs` (new)
- `SAM/SAM/SAM.Tests/SAM.Tests.csproj` (copy SAM_ApertureConstructionLibrary.JSON)
- `SAM/SAM/SAM.Tests/SAMResourcesModuleInitializer.cs` (seed DefaultApertureConstructionLibrary)
- `SAM/PROJECT_PROGRESS.md` (this file)
- `SAM/documentation/PartF-HANDOVER.md` (state table + programme note)

## Validation
- `dotnet test SAM/SAM/SAM.Tests/SAM.Tests.csproj --filter "FullyQualifiedName~PartFTransferAirDoorTests"`:
  10/10 passed (Debug).
- Full suite, Release (as CI): **1285 passed, 0 failed** (was 1275; +10 new).
- `SAM_UI/WPF/SAM.Analytical.UI.WPF.Tests` (regression, shared calculator): **182 passed, 0 failed**.
- `SAM.Analytical.Grasshopper` builds with 0 CS errors (post-build %APPDATA% copy fails without Rhino —
  known environment condition, per handover §6).

## Issues / blockers
- None known. Not exercised: the GH component on a live canvas (no Rhino here), and a multi-panel candidate
  choice on a real architectural model (unit tests cover the rule, not a real wall split).

## Next step
- Nothing outstanding for this feature. Programme-wise the next task remains the Iteration 1 BasePassive
  manual TAS acceptance run (PartF-HANDOVER.md §5).
