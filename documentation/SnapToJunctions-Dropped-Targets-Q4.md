# SnapToJunctions: a dropped panel is not a junction for any other panel — Q4 (2026-10-27)

Status: branch `fix/snap-to-junctions-dropped-targets-q4` from `origin/sow/2026-Q4` `fe5bbce4`, worktree
`SAM-BIM\SAM-wt-snap-targets`. **Local only: not pushed, no PR yet** (the owner asked to be asked first). Tested content
`2120aceb` (the code commit); later commits change this record only. Owner authorisation 2026-10-27: a separate
focused SAM PR, expected to land before SAM-BIM/SAM#225 (draft, head `cb11d6c4`). Investigation:
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
- Non-convergence is never silent. The last pass is returned with a new Warning, **`SNP003 SnapNotSettled`**
  (`BySolidsDiagnosticCode`; Value = panels still changing, Limit = pass limit). `SAMAnalyticalCreatePanelsBySolids`
  raises it as a Grasshopper Warning. A new code is needed because `SNP001` is a Summary Info code and `SNP002` carries
  one panel's area. Reusing either would change a released code's meaning (DIAGNOSTICS_STANDARD §3). Non-convergence was
  not observed on Softhouse or in any test.
- `SAMAnalyticalCreatePanelsBySolids` 1.2.0 -> **1.3.0** (the geometry result changes: GH-F2, minor), with the pin in
  `BySolidsDiagnosticDetailTests`. **Collision:** SAM#225 also moves it 1.2.0 -> 1.3.0. Whichever merges second rebases
  to 1.4.0 and extends the pin comment. This PR is expected first, so #225 would take 1.4.0. The #225 record is not
  edited here.

## Files changed

`SAM/SAM.Analytical/Query/SnapToJunctions.cs`; `SAM/SAM.Analytical/Classes/Diagnostics/BySolidsDiagnosticCode.cs`
(`SnapNotSettled`); `Grasshopper/SAM.Analytical.Grasshopper/Component/SAMAnalyticalCreatePanelsBySolids.cs` (1.3.0,
SNP003 balloon); `Grasshopper/SAM.Core.Grasshopper.Tests/BySolidsDiagnosticDetailTests.cs` (pin);
`SAM/SAM.Tests/SnapToJunctionsDroppedTargetTests.cs` (new); this record.

## Tests (`SnapToJunctionsDroppedTargetTests`, generic coordinates, a port of the investigation's `Synth.cs`)

| Test | Base `fe5bbce4` | Head `2120aceb` |
| --- | --- | --- |
| `BlockWithinCornerWallsThickness_DoesNotMoveSlabStep` (the base pulls the step 0.35 -> 0.125) | **FAIL** | pass |
| `StripCollapsingOnlyOnDroppedBlock_IsKept` (strip x -0.5..-0.2, y 0..0.35; revival) | **FAIL** | pass |
| `BlockWithinCornerWallsThickness_IsDroppedWithWarning` (text, value, summary counts all 5) | pass | pass |
| `ThinPartitionOnSlabStep_RemainsTargetOfBothSlabEdges` | pass | pass |
| `JunctionBlockBetweenCollinearWalls_IsKeptAndCrossWallReachesMidPlane` (#225 #326 pattern) | pass | pass |
| `ReorderedInput_GivesSameResult` (reversed, Guid-ordered; the scene needs a drop and a revive) | pass | pass |
| `DetailLevel_DoesNotChangePanels` (Summary/Standard/Detailed/null/legacy `out List<string>`) | pass | pass |
| `DroppedSetNotSettledWithinPassLimit_ReportsWarning` (internal bound 1) | n/a (new API) | pass |

The existing `PanelsBySolidsTests.SnapToJunctions_PanelWithinCrossingWall_IsDroppedWithWarning` passes on both. The base
run used the file without the new-API test (`tests/base-source-new-tests.txt`).

## Validation (local, machine `michal`, 2026-10-27; tested content `2120aceb`)

Evidence: `SAM_daily\2026-10-25-Softthouse\handover\evidence\2026-10-27-snap-targets\` (machine-local; its
`README.md` has the commands). Builds used `handover/tools-2026-10-27-combined/scripts/safebuild.sh` (APPDATA and
USERPROFILE redirected to `cv28\profile`) with `SAM_OCCT_SKIP_NATIVE_BUILD=true`. There were no native builds. Native
chain processes ran one at a time.

| Check | Result |
| --- | --- |
| SAM.Tests Release, base source + the new tests | 3790 / 3792; the 2 failures are exactly the two new failing tests |
| SAM.Tests Release, head | **3793 / 3793**. No existing expectation changed: no geometry and no SnapChecked extended/trimmed count in the BySolids scenario/diagnostic tests, so none was re-pinned |
| SAM.Core.Grasshopper.Tests Release, `--no-build`, normal profile (built with safebuild) | **87 / 87**, 0 skipped (includes the 1.3.0 pin) |
| Snap stage, managed: the new library on `2026-10-27-combined/run-F|R/panels_extracted.json` vs `2026-10-27-reveal362/out/candidateB-F|R-panels.json` | **Equal by Guid and outline, max deviation 0**: F 384 -> 371, R 380 -> 367. Identical on reversed and Guid-ordered input and at Summary detail. 13 drops each |
| Clean3D/Extend3D managed (F, kink solver as the control) vs `controls/ctl-F-drop362` | Clean 80/80 and extended 80/80: 78 SAME + 2 within 1e-6. Equal (80 SAME) to candidate B. **First-floor datum 3.475 m, 52 caps.** Level groups 0.025 / 3.475 / 6.475 / 9.450 |
| Clean3D/Extend3D managed (R) | Equal (78 SAME) to candidate B. vs `run-R`: 77 SAME + the 9.45 slab loop (Clean3D) / within 1e-6 (extended), as in the investigation |
| Combined Softhouse F, SAM `cbf3f520` + this fix, OCCT #84 `a77ef7d` **no-kink (acceptance: the PR contents)** | 58 spaces, 100 hosted, 48 good. **Carpark 2 C109 and Bed 4 C203 recovered. 47 / 48 retained**: only Dining 4 A107 is lost, inherited from #84. New good: Bath 7 B302 |
| Combined Softhouse F, the same + the Clean3D kink candidate | 57 spaces (= the `ctl-F-drop362` native diagnostic), 50 good, **47 / 48**, only A107 lost. New good: Bed 13 A304, Bath 7 B302, Bed 11 B304 |
| Invented walls (Extend3D growth vs `growth-run-F.txt`) | None. Kink: identical to `growth-ctl-F-drop362` (272.2 m2). No-kink: 288.4 m2, the extra being the angled party-wall pieces [54]/[55] (+7 m2 each, at their own extent). Every growth is an edge move of <= 0.05 m in z / <= 0.15 m in plan; the largest is still the north facade +15.33 m2 |
| Combined Softhouse R (both solvers) | 0 spaces, as expected: the separate #37 Clean3D slab drop, unchanged |
| Base-only F, SAM `2120aceb` + base OCCT (`SAM_OCCT-wt-extend3d-base\build`, `22e4fd0`) | 70 spaces, **48 / 48**. The 70-room classification is identical to the saved baseline and to the Q4 control (`fe5bbce4`, same OCCT) |
| `%APPDATA%\SAM` | The 855 hashes of `2026-10-27-combined/provenance/appdata-sam-before.sha256` match before and after. All 2125 files are byte-identical from session start to end |
| Loaded assemblies | `provenance/loaded-assemblies.tsv` (43 path + SHA256), 0 FALLBACK / `%APPDATA%` / McNeel paths. SAM.Analytical: comb `E5F94080`, base `45AC5135`, Q4 control `BE537F61`. OCCT as in `2026-10-27-combined` (solver kink `8620AE7B`, no-kink `DFF57535`, native `A939AA72`) |

One change on base needed tracing: in the base extraction, head strip #361 (0.048 m floor at z 6.376, inside the 0.45 m
facade) is now dropped as well. On Q4 its north edge trimmed onto dropped block #338 (y -2.7495), leaving a 0.79 m2
sliver. Without that pull it closes up like #353/#371. Downstream, Clean3D moves the facade's lower edge 0.099 m (row
(d) of the investigation), and Extend3D absorbs it: extended panels and the model are equal within 1e-6, and all 70
rooms are identical (`base361/`).

The overall 48/48 gate is **not** met by this fix alone in the combined chain. Dining 4 A107 stays lost, inherited from
SAM_OCCT#84.

Hosted CI: none (not pushed).

## Decisions and assumptions

- The rule is target eligibility only. No fixture IDs, coordinates, angles or tolerances changed, and no panel is
  excluded by type or origin.
- The pass bound is 10. A non-converging set returns the last pass deterministically with `SNP003`.
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

The owner reviews and authorises the push. Then push `fix/snap-to-junctions-dropped-targets-q4` to `SAM-BIM/SAM` and open a
draft PR into `sow/2026-Q4` with this record, and check hosted build/spdx/test at the final head. After it merges, rebase
SAM#225 onto `sow/2026-Q4` and take 1.4.0. Recommended: Claude Opus 5.5, medium effort, this session, Build mode,
because the evidence and context are already here.
