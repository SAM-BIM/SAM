# SnapToJunctions: a dropped panel is not a junction for any other panel — Q4 (2026-10-27)

Status: branch `fix/snap-to-junctions-dropped-targets-q4` from `origin/sow/2026-Q4` `fe5bbce4`, worktree
`SAM-BIM\SAM-wt-snap-targets`. Draft PR into `sow/2026-Q4`; the PR description gives the final head and hosted checks.
Tested content: `ba6365bd`, which is the first fix `2120aceb` plus the owner-requested revision for non-convergence. Later
commits change this record only. Owner authorisation 2026-10-27: a separate focused SAM PR, expected to land before
SAM-BIM/SAM#225 (draft, head `cb11d6c4`). The owner's review of `aedd00b0` was REVISE: the first version returned the
10th pass with a Warning, which could still carry dropped-target pulls. That is fixed in `ba6365bd`. Investigation:
`SAM_daily\2026-10-25-Softthouse\handover\evidence\2026-10-27-reveal362\README.md` (machine-local).

## Problem

`Query.SnapToJunctions` collected every non-Air panel as a snap target, snapped every panel, and only then dropped the
panels it trimmed to nothing (`SnapCollapsed`, "lies within the thickness of the elements it meets"). A dropped panel
had therefore already moved its neighbours' edges. On Softhouse with SAM#225, reveal block #362 (0.175 m, inside the
0.45 m facade #361 and return wall #370) pulled slab #134's north edge off the facade mid-plane. #134 became the largest
first-floor cap, LevelFrame moved the first-floor datum 3.475 -> 3.450 m, and Carpark 2 C109 and Bed 4 C203 were lost.
Jamb and head strips #337/#341/#353/#371 do the same on base readings (edges pulled 0.07-0.1 m off consistent
mid-planes), so the defect predates #225.

## Change

- `SAM/SAM.Analytical/Query/SnapToJunctions.cs`. The per-panel snap is a private helper, `Snap(external, self,
  targets, ...)`, which returns the snapped outline or null when it closes up. The rule: **a panel that SnapToJunctions
  drops is not a junction for any other panel.** The dropped set D starts empty. Each pass snaps every panel against
  the targets outside D, and D becomes the panels that close up. The passes stop when D is stable, and the survivors of
  that final pass are returned. D is re-evaluated, not only grown, so a panel that closed up only on a dropped target is
  revived. "Dropped stays dropped" would lose Softhouse #1. The result is order-independent: D is a set, and each edge
  takes the max over a target set. The XML doc states the rule.
- Public signatures are unchanged. The `SnapChecked` summary text is unchanged, and its edge counts come from the final
  pass (every input panel, as before). There is one `SnapCollapsed` Warning per finally dropped panel, in input order,
  with unchanged text. The passes are bounded at 10 (Softhouse needs 3). An `internal` overload takes the bound, for the
  test.
- **Non-convergence (revision `ba6365bd`).** Each pass's dropped set is compared with every earlier set. A repeated set is
  a cycle of mutually dependent drops: a panel is dropped only while another is a junction, and that one only while the
  first is not, so no consistent set exists. A cycle, or reaching the pass limit, ends the passes. **No pass is then a
  valid result**, because each still lets a dropped panel move its neighbours. So snapping is not applied: the panels
  come back unsnapped, exactly as with `snapDistance` 0. `SnapChecked` reports 0 moved edges, there are no
  `SnapCollapsed` records, and there is an **Error `SNP003 SnapNotSettled`**. The Error names the unsettled panels
  (dropped in some states of the cycle but not all; for the limit, those still changing), sorted by name. Its Value is
  their count and its Limit is the pass limit. `SAMAnalyticalCreatePanelsBySolids` raises it as a Grasshopper Error.
  The state sequence depends only on sets, so the result and the message do not depend on input order or detail
  level. Error, not Warning, because snapping could not complete (DIAGNOSTICS_STANDARD §6). `docs/DIAGNOSTICS_STANDARD.md`
  §3 now names SNP003 as the one BySolids 00x Error. A new code is needed because `SNP001` is a Summary Info code and
  `SNP002` carries one panel's area. Non-convergence was not observed on Softhouse (no SNP003 in any run).
- `SAMAnalyticalCreatePanelsBySolids` 1.2.0 -> **1.3.0** (the geometry result changes: GH-F2, minor), with the pin in
  `BySolidsDiagnosticDetailTests`. **Collision:** SAM#225 also moves it 1.2.0 -> 1.3.0. Whichever merges second rebases
  to 1.4.0 and extends the pin comment. This PR is expected first, so #225 would take 1.4.0. The #225 record is not
  edited here.

## Files changed

`SAM/SAM.Analytical/Query/SnapToJunctions.cs`; `SAM/SAM.Analytical/Classes/Diagnostics/BySolidsDiagnosticCode.cs`
(`SnapNotSettled`); `Grasshopper/SAM.Analytical.Grasshopper/Component/SAMAnalyticalCreatePanelsBySolids.cs` (1.3.0,
SNP003 Error balloon); `Grasshopper/SAM.Core.Grasshopper.Tests/BySolidsDiagnosticDetailTests.cs` (pin);
`SAM/SAM.Tests/SnapToJunctionsDroppedTargetTests.cs` (new); `docs/DIAGNOSTICS_STANDARD.md` (one phrase); this record.

## Tests (`SnapToJunctionsDroppedTargetTests`, generic coordinates, a port of the investigation's `Synth.cs`)

| Test | Base `fe5bbce4` | Head `ba6365bd` |
| --- | --- | --- |
| `BlockWithinCornerWallsThickness_DoesNotMoveSlabStep` (the base pulls the step 0.35 -> 0.125) | **FAIL** | pass |
| `StripCollapsingOnlyOnDroppedBlock_IsKept` (strip x -0.5..-0.2, y 0..0.35; revival) | **FAIL** | pass |
| `BlockWithinCornerWallsThickness_IsDroppedWithWarning` (text, value, summary counts all 5) | pass | pass |
| `ThinPartitionOnSlabStep_RemainsTargetOfBothSlabEdges` | pass | pass |
| `JunctionBlockBetweenCollinearWalls_IsKeptAndCrossWallReachesMidPlane` (#225 #326 pattern) | pass | pass |
| `ReorderedInput_GivesSameResult` (reversed, Guid-ordered; the scene needs a drop and a revive) | pass | pass |
| `DetailLevel_DoesNotChangePanels` (Summary/Standard/Detailed/null/legacy `out List<string>`) | pass | pass |
| `MutuallyDependentDrops_AreReturnedUnsnappedWithError`: a genuine cycle. Crossing wall C; pier B closes up unless stub A is a junction ahead of it; A closes up only while B is a junction. Dropped sets {} -> {A} -> {A,B} -> {B} -> {}, detected after 4 passes; unsnapped input, one Error naming 'A', 'B' | n/a (new API) | pass |
| `MutuallyDependentDrops_DoNotDependOnOrderOrDetail`: all 6 orders x Summary/Standard/Detailed, plus null and legacy `out List<string>`. Same panels and same Error text | n/a | pass |
| `DroppedSetNotSettledWithinPassLimit_IsReturnedUnsnappedWithError` (internal bound 1: the first pass, which still pulls the step, is not published) | n/a | pass |

The existing `PanelsBySolidsTests.SnapToJunctions_PanelWithinCrossingWall_IsDroppedWithWarning` passes on both. The base
run used the file without the new-API tests (`tests/base-source-new-tests.txt`).

Mutation checks on `ba6365bd` (`tests/r2/mutation-*.txt`):
- M1, no repeat detection: the cycle test fails, because the cycle is reported as "did not settle within 10 passes".
- M2, publish the last pass as `2120aceb` did: all 3 failure-path tests fail.

A first M2 attempt hit the zero-snap-distance early return by mistake. That return is harmless to remove, and the
mutation was redone on the failure branch.

## Validation (local, machine `michal`, 2026-10-27; tested content `ba6365bd`, first round on `2120aceb`)

Re-run on `ba6365bd`, with results in `r2/`, `tests/r2/` and `runs/*2-*`:
- SAM.Tests Release: **3795 / 3795**.
- SAM.Core.Grasshopper.Tests: **87 / 87**.
- Snap stage on F/R: still equal to candidate B (deviation 0), with no SNP003.
- Managed F Clean3D/Extend3D: still 78 SAME + 2 within 1e-6 vs `ctl-F-drop362`, datum 3.475 with 52 caps.
- Native F, combined no-kink, combined kink and base-only: the 70-room tables are identical to the `2120aceb` runs
  below, so 47/48, 47/48 and 48/48.
- Isolated builds: `cv28\SAM_comb2` (SAM.Analytical `CEDB2709`) and `cv28\SAM_base2` (`F92A67A8`).

The settled path is unchanged by the revision. The table below is the first round (`2120aceb`), still valid for that path.

Evidence: `SAM_daily\2026-10-25-Softthouse\handover\evidence\2026-10-27-snap-targets\` (machine-local; its
`README.md` has the commands). Builds used `handover/tools-2026-10-27-combined/scripts/safebuild.sh` (APPDATA and
USERPROFILE redirected to `cv28\profile`) with `SAM_OCCT_SKIP_NATIVE_BUILD=true`. There were no native builds. Native
chain processes ran one at a time.

| Check | Result |
| --- | --- |
| SAM.Tests Release, base source + the new tests | 3790 / 3792; the 2 failures are exactly the two new failing tests |
| SAM.Tests Release, head (`2120aceb`; `ba6365bd`: 3795 / 3795) | **3793 / 3793**. No existing expectation changed: no geometry and no SnapChecked extended/trimmed count in the BySolids scenario/diagnostic tests, so none was re-pinned |
| SAM.Core.Grasshopper.Tests Release, `--no-build`, normal profile (built with safebuild) | **87 / 87**, 0 skipped (includes the 1.3.0 pin) |
| Snap stage, managed: the new library on `2026-10-27-combined/run-F|R/panels_extracted.json` vs `2026-10-27-reveal362/out/candidateB-F|R-panels.json` | **Equal by Guid and outline, max deviation 0**: F 384 -> 371, R 380 -> 367. Identical on reversed and Guid-ordered input and at Summary detail. 13 drops each |
| Clean3D/Extend3D managed (F, kink solver as the control) vs `controls/ctl-F-drop362` | Clean 80/80 and extended 80/80: 78 SAME + 2 within 1e-6. Equal (80 SAME) to candidate B. **First-floor datum 3.475 m, 52 caps.** Level groups 0.025 / 3.475 / 6.475 / 9.450 |
| Clean3D/Extend3D managed (R) | Equal (78 SAME) to candidate B. vs `run-R`: 77 SAME + the 9.45 slab loop (Clean3D) / within 1e-6 (extended), as in the investigation |
| Combined Softhouse F, SAM `cbf3f520` + this fix, OCCT #84 `a77ef7d` **no-kink (acceptance: the PR contents)** | 58 spaces, 100 hosted, 48 good. **Carpark 2 C109 and Bed 4 C203 recovered. 47 / 48 retained**: only Dining 4 A107 is lost, inherited from #84. New good: Bath 7 B302 |
| Combined Softhouse F, the same + the Clean3D kink candidate | 57 spaces (= the `ctl-F-drop362` native diagnostic), 50 good, **47 / 48**, only A107 lost. New good: Bed 13 A304, Bath 7 B302, Bed 11 B304 |
| Invented walls (Extend3D growth vs `growth-run-F.txt`) | None. Kink: identical to `growth-ctl-F-drop362` (272.2 m2). No-kink: 288.4 m2, the extra being the angled party-wall pieces [54]/[55] (+7 m2 each, at their own extent). Every growth is an edge move of <= 0.05 m in z / <= 0.15 m in plan; the largest is still the north facade +15.33 m2 |
| Combined Softhouse R (both solvers) | 0 spaces, as expected: the separate #37 Clean3D slab drop, unchanged |
| Base-only F, SAM `2120aceb` + base OCCT (`SAM_OCCT-wt-extend3d-base\build`, `22e4fd0`) | 70 spaces, **48 / 48**. The 70-room classification is identical to the saved baseline and to the Q4 control (`fe5bbce4`, same OCCT) |
| `%APPDATA%\SAM` | The 855 hashes of `2026-10-27-combined/provenance/appdata-sam-before.sha256` match before and after both rounds. All 2125 files are byte-identical from session start to end (`appdata-check-after*.txt`) |
| Loaded assemblies | `provenance/loaded-assemblies.tsv` (57 path + SHA256, both rounds), 0 FALLBACK / `%APPDATA%` / McNeel paths. SAM.Analytical: comb `E5F94080` / comb2 `CEDB2709`, base `45AC5135` / base2 `F92A67A8`, Q4 control `BE537F61`. Two harness slips (quoting in SAMDIRS) made the pipe resolve SAM from `%APPDATA%\McNeel\...\packages\8.0\SAM`. Both were detected from `assemblies.txt`, quarantined in `aborted-quoting/` and rerun. `run-chains.sh` now refuses SAMDIRS that do not name build folders. OCCT as in `2026-10-27-combined` (solver kink `8620AE7B`, no-kink `DFF57535`, native `A939AA72`) |

One change on base needed tracing: in the base extraction, head strip #361 (0.048 m floor at z 6.376, inside the 0.45 m
facade) is now dropped as well. On Q4 its north edge trimmed onto dropped block #338 (y -2.7495), leaving a 0.79 m2
sliver. Without that pull it closes up like #353/#371. Downstream, Clean3D moves the facade's lower edge 0.099 m (row
(d) of the investigation), and Extend3D absorbs it: extended panels and the model are equal within 1e-6, and all 70
rooms are identical (`base361/`).

The overall 48/48 gate is **not** met by this fix alone in the combined chain. Dining 4 A107 stays lost, inherited from
SAM_OCCT#84.

Hosted CI: see the PR description, which gives the final head.

## Decisions and assumptions

- The rule is target eligibility only. No fixture IDs, coordinates, angles or tolerances changed, and no panel is
  excluded by type or origin.
- The pass bound is 10, and a repeated set ends earlier. The failure result is global: the unsnapped input plus an
  Error, never a pass. Considered and not taken: excluding only the unsettled panels as targets. That can make
  other panels close up and so needs its own fixed point, which has no termination argument. A cycle needs mutually
  dependent drops, which is a modelling defect worth an Error.
- The acceptance chain is no-kink, the content of #225 + this fix + #84. The kink candidate is not in any PR, and its
  towers change is out of scope.
- The combined export is `cbf3f520` + `git diff fe5bbce4 2120aceb`, with the GH version hunk and pin skipped because
  #225 already says 1.3.0 (`provenance/comb-applied.diff`).

## Unresolved / risks

- The 1.3.0 collision with SAM#225 (see Change).
- Residual risk, out of scope (SAM_OCCT): LevelFrame seeds a datum from the single largest cap. With the fix, #134
  23.457 m2 vs #65 23.851 m2 at 3.475 m is a 1.7 % margin.
- Out of scope: R / #37 (Clean3D drops the merged 2F slab), the kink's towers change, PanelsBySolids reveal recovery.
- Cost: 2-3 snap passes, O(n^2) each. Softhouse takes about 1.2 s for all passes.

## Next step

Check hosted build/spdx/test on the draft PR at its final head. Then the owner reviews it and marks it ready and merges
it, before SAM#225. After the merge, write the `PROJECT_PROGRESS.md` closeout, rebase SAM#225 onto `sow/2026-Q4` and
move #225 to 1.4.0. Recommended: Claude Opus 5.5, medium effort, this session, Build mode, because the evidence and
context are already here.
