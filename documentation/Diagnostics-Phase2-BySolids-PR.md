# Unified Diagnostics, Phase 2 (BySolids integration) - PR record

Base: `sow/2026-Q4` (`45f495e3`, after Phase 1 SAM#201, merge `293eb645`). Branch: `feature/bysolids-diagnostics-phase2-q4`.
Validated content SHA: `6f765b73` (code, tests, golden). A record-only commit follows; the final head and hosted checks are in the PR description.
PR: see PR description. Not merged; merge needs owner approval (AGENTS.md).

## Status
Implementation complete. Linux build and tests pass (below). The Rhino 8 / Grasshopper tests and the interactive acceptance have **not** been run (no Rhino here). Awaiting owner review and the Rhino acceptance.

## Investigation: what was emitted before
`PanelsBySolid`, `SnapToJunctions`, `AperturesBySolids` returned `out List<string>`; every line already followed `Severity: Subject: Message` (`Core.Query.DiagnosticMessage`, PR #200). Severity existed only as a string prefix.

| Producer | Line | Was | Now (structured) |
|---|---|---|---|
| PanelsBySolid | not a closed solid / below minThickness / not panel-like / no face pair / mid-plane misses / section region failed | Warning | Warning, any level (PBS001-PBS006) |
| PanelsBySolid | Created N panels per element plane | Info | Info, Standard (PBS102) |
| PanelsBySolid | Variable thickness | Info | Info, Standard (PBS101) |
| PanelsBySolid | Ignored other face-pair planes (not panel-like / below minThickness; builds a Take(5) list) | Info | Info, Detailed, lazy (PBS201, PBS202) |
| SnapToJunctions | Checked N panels for junctions | Info | Info, Summary (SNP001) |
| AperturesBySolids | member not closed or planar; assembly skipped (no planar faces / no opening / not planar / no construction) | Warning | Warning, any level (AGS001-AGS005) |
| AperturesBySolids | Created apertures per assembly | Info | Info, Standard (AGS101) |
| GH components | totals line, first line of `log` | Info | Info, Summary (PBS100 / AGS100) |

Cost: only the PBS201/202 messages are non-trivial (formatting up to 5 planes); they are lazy. Everything else is one `string.Format`; the Created message is lazy too. Nothing in the geometry path depends on the level.

## Work completed
- `PanelsBySolid`, `SnapToJunctions`, `AperturesBySolids`: new overloads with a `Core.Diagnostics` parameter (null accepted). The existing `out List<string>` overloads delegate to them with a `Detailed` collection and return the same lines (public signatures untouched).
- Structured values: `Code` (`Analytical.BySolidsDiagnosticCode`), `Subject`, `SourceIndex` (solid index; first member of an assembly), `Value`/`Limit`/`Unit` (thickness, minThickness, ratio, maxThicknessRatio, depth, snap distance).
- GH `CreatePanelsBySolids` 1.0.3 and `CreateAperturesBySolids` 1.0.2: optional input `diagnosticDetail_` (Param_Integer, 0 Summary default, 1 Standard, 2 Detailed), appended last, `Voluntary` (hidden like `maxThicknessRatio_`). `log` stays the output name and stays a string list. Skip reasons for the runtime Warnings are classified by diagnostic code, no longer by `Contains("below minThickness")` on message text (which would have broken at Summary).
- `Core.Query.TryGetDiagnosticDetail(int, out DiagnosticDetail)`: defined levels only; otherwise Summary and `false`.
- Tests and golden baseline (below).

## Decisions and assumptions
- **Default Summary changes the default `log` content.** Before: totals plus one Info line per solid. Now at the default: totals, every Warning, and the snap line; `diagnosticDetail_` 1 restores the per-solid lines, 2 adds the ignored planes. Warnings, panels, apertures are unchanged. Needed by the spec (Summary default); flag for the owner because anything parsing `log` Info lines at the default would notice.
- Legacy `out List<string>` = `Detailed`, so the lines are identical to before. Two deliberate deviations: (1) several failing section regions on one element plane give one grouped Warning (`N section regions could not be turned into panels.`; a single failure keeps the exact old text); (2) the new Warning `PBS007` (a solid gave no panel and nothing else said why) is structured-only and filtered out of the legacy list. It is never emitted for the fixtures; before, such a solid was explained only by an Info line that Summary now hides.
- Per-solid skip Warnings stay one line per solid: each carries the value that would keep that solid (GH Warnings point to them). Grouping is only applied to repeats within one plane.
- Invalid `diagnosticDetail_` (anything but 0, 1, 2): GH runtime Warning, a Warning line in `log` (BYS001) right after the totals, and Summary is used. Chosen over clamping so the result is the documented default. Not-a-number input is rejected by Grasshopper's own conversion.
- `Diagnostics` drop-on-add (Phase 1, owner-approved) is used as is: the components collect at the requested level; nothing re-filters upward.
- Version: GH components bumped (policy: component changes bump `LatestComponentVersion`); no assembly version is declared in SAM.Analytical/SAM.Core.
- Limitations from PR #200 are unchanged and untouched: `maxThicknessRatio_` >= 0.4 makes the stair a wall, >= 0.5 breaks `GF_Wall_External`; keep it <= 0.3. No classification change.

## Files changed
- `SAM/SAM.Analytical/Create/PanelsBySolid.cs`, `Create/AperturesBySolids.cs`, `Query/SnapToJunctions.cs`
- new `SAM/SAM.Analytical/Classes/Diagnostics/BySolidsDiagnosticCode.cs`, `SAM/SAM.Core/Query/TryGetDiagnosticDetail.cs`
- `Grasshopper/SAM.Analytical.Grasshopper/Component/SAMAnalyticalCreatePanelsBySolids.cs`, `SAMAnalyticalCreateAperturesBySolids.cs`
- tests: `SAM/SAM.Tests/BySolidsDiagnosticsTests.cs`, `BySolidsScenarios.cs`, `Golden/PanelsBySolid/bysolids_baseline_q4.json` (new); `PanelsBySolidComplexHouseTests.cs` (two members `private` -> `internal`); `Grasshopper/SAM.Core.Grasshopper.Tests/BySolidsDiagnosticDetailTests.cs` (new)
- this record. `PROJECT_PROGRESS.md` is not touched (post-merge closeout only).

## Validation
Reproduce (Linux needs a Roslyn newer than Ubuntu's .NET 8.0.131, which mis-binds `new([...])` in `AddTransferAirDoorsByPartF.cs`; hosted CI uses current 8.x and does not need this):
`dotnet test SAM/SAM.Tests/SAM.Tests.csproj -c Release [-p:CustomBeforeMicrosoftCommonProps=<props importing Microsoft.Net.Compilers.Toolset 4.11.0> -p:UseSharedCompilation=false]`.

| Check | Where | Result |
|---|---|---|
| Baseline before change: BySolid + Diagnostic tests | local Linux | PASS 220/220 |
| Golden baseline captured from the unmodified legacy API (deterministic: two captures identical) | local Linux | done, committed |
| New `BySolidsDiagnosticsTests` (116 cases) | local Linux | PASS 116/116 |
| Full `SAM.Tests` Release on `6f765b73` | local Linux | 3682 passed, 1 failed, 0 skipped. The failure, `PartFStudioReferenceTests.Studio_CompactSchematic`, hard-codes `\r\n` but `PartFSchematic` uses `Environment.NewLine`; Linux-only, unrelated (PartF untouched); Windows CI is the authority |
| Mutation check: legacy wrapper collecting at Summary | local Linux | caught (8 failures, golden and existing tests) |
| `SAM.Analytical.Grasshopper` and `SAM.Core.Grasshopper.Tests` compile | local Linux, compile only (WinForms SDK disabled, post-build copy neutralised) | 0 errors; nothing executed |
| `SAM.Core.Grasshopper.Tests` incl. `BySolidsDiagnosticDetailTests` (15 cases) | needs Rhino 8 | **NOT RUN** |
| Interactive Rhino 8 / Grasshopper acceptance | needs Rhino 8 | **NOT RUN** |
| Hosted GitHub checks | GitHub | see PR description |

What the Linux tests establish: the contemporary house at four settings (default, ratio 0.25, minThickness 0.25, 0.05/0.3), synthetic solids covering every skip reason, house and synthetic apertures. Against the baseline captured before the change: legacy lines identical; structured Detailed lines identical; engineering fingerprints (type, area, PhysicalThickness, normal, extents, holes, source stamps, snapped panels, aperture hosting in an `AdjacencyCluster`) identical at Summary, Standard and Detailed; each level equals the Detailed records filtered to it; Warnings identical at every level with code, subject, source index, value and limit; every solid with no panel has a Warning at every level; repeated runs identical (order and values). Panel counts 38 / 39 / 29 / 39 and the skipped solids are asserted.
Not covered: spatial adjacency (SAM_OCCT `Solve3D`, out of scope); `PBS007` and the grouped-regions Warning have no fixture that triggers them.

## Rhino 8 acceptance (owner, Windows)
1. Check out this branch, build SAM Release (the Grasshopper projects copy into `%APPDATA%\SAM`), restart Rhino 8; Rhino model units metres.
2. `dotnet test Grasshopper/SAM.Core.Grasshopper.Tests/SAM.Core.Grasshopper.Tests.csproj -c Release`: expect the existing tests plus the 15 `BySolidsDiagnosticDetailTests` to pass (headless Rhino 8). Report any failure verbatim; a failure in the new fixture may be a test mechanics issue (it drives `CollectData`/`ComputeData` directly) as well as a product issue.
3. Open `import-test.gh` (saved before this change; machine-local, not in the repository). The `CreatePanelsBySolids` and `CreateAperturesBySolids` components must load with their wires and values, and no `diagnosticDetail_` input visible (it is hidden). Expect the "update component" mark (1.0.2 -> 1.0.3, 1.0.1 -> 1.0.2); run `SAM.CoreUpdate` and check wires and values survive.
4. Default: `log` = the totals line, the Warnings (e.g. the stair and the 0.3 m gable at the default 0.2 ratio), and the snap line; no per-solid Info. Panel count identical to before (38 at the default ratio, 39 at `maxThicknessRatio_` 0.25).
5. Zoom in on the component and click + to show `diagnosticDetail_`; set 1: one Info line per solid (and per roof plane) appears; set 2: additionally one `Ignored 1 other face-pair plane ...` line on this house (at 0.25 / 0.08). Panels, `excluded`, `airPanels` outputs identical for 0, 1, 2 (compare counts and a bake).
6. Set 5 (or -1): an orange runtime Warning names `diagnosticDetail_`, `log` line 2 is `Warning: diagnosticDetail_ 5 is not 0 ...`, the component does not error, and the Panels are unchanged. Reset to 0.
7. `CreateAperturesBySolids` with the 18 house window solids: 18 apertures at every level; `log` has the totals only at 0, one line per assembly at 1.
8. Feed the Panels and Apertures on to `SAMOCCT.CreateAdjacencyCluster` as before; adjacency and `AddAperturesByAperture` results unchanged.

## Risks / not done
- The default `log` content changes (see Decisions).
- Component behaviour is unexecuted until steps 2-8 are run.
- Phase 2 only: no change to SAM_OCCT, SAM_Tas, SAM_OpenStudio, no `log` -> `Diagnostics` rename, no other components.

## Next step
Owner review and Rhino acceptance (above), then merge; post-merge `PROJECT_PROGRESS.md` closeout on `sow/2026-Q4`. Later phases (OCCT, OpenStudio/TAS adapters) stay separate PRs. Recommended: Opus, high effort, new session, Plan mode (cross-repo).
