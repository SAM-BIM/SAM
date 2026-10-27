# Project Progress - SAM (2026-Q4)

## Q4 closeout: SAM#227 (Face2D: a hole ring collapsing below 3 points converts to null, not a 2-point loop) - 2026-10-27

- **PR/status:** merged into `sow/2026-Q4` with `--merge --match-head-commit`, no auto-merge, bypass or squash, on owner authorisation (merge order SAM_OCCT#86 first) -> `7b1ce8a359b143d81b98e982cd1be3f43ef0bec6` (parents `91f25290`, head `40164d96321461affe8660bbdf821d7b18c42aeb`; tested content `b852b687`, later commits record-only). Independent reviews: REVIEW (REVISE R1) and the R1 closeout review (closed), machine-local under `SAM-BIM/.review-sam227-*`; no GitHub reviews or comments, codex included. Record `documentation/Face2D-Degenerate-Hole-Loop-Q4.md`.
- **Work:** C1 `LinearRing.ToSAM` returns null when the points `Polygon2D` keeps (exact closing point excluded) are fewer than 3, in the tolerance and tolerance 0 paths; C2 `ToSAM_Polygon2Ds` leaves null conversions out. Stops `Face2D.Create`'s multiple-hole re-union making a merged face invalid, which `Spatial.Query.Union` then dropped (seed-400: 126 m2 lost). No public API, tolerance, area filter, `Face2D.Create`, `Spatial.Query.Union` or GH metadata change; no GH bump was made (recommended under GH-H2 and merged as submitted; not a rule for future library changes).
- **Validation:** local: new tests 15/37 fail on base, 37/37 on the fix; mutations (C1 or C2 reverted, area fill) caught; SAM.Tests 3843/3843 (base + new tests 3828/3843, exactly the 15); SAM.Core.Grasshopper.Tests 87/87 both; 2000-seed synthetic raw slab losses 12 -> 0, Clean3D guard Warnings 18 -> 0, other 3988 runs bit-identical; consumers SAM_SolarCalculator (564/1 skipped both), SAM_gbXML, SAM_IFC build (SAM_Systems, SAM_Validation, SAM_Building, SAM_Tas fail identically on unmodified SAM: infrastructure). Softhouse (#84 `26ddbaf`, no kink): F identical; R 59/45/44 of 48 both, no recovery Warning with the fix; R model deltas proven to be panel order alone (order-restored replay equals the baseline byte-for-byte). Hosted at `40164d96`: Build `40563058652`, Test `40563058688`, SPDX `40563058642` green; post-merge Build `40564673788` green. Evidence machine-local: `SAM_daily/2026-10-25-Softthouse/handover/evidence/2026-10-27-sam-root-face2d-loop/` (incl. `closeout/`).
- **Unresolved / limits:** older `Polygon.ToSAM` area-vs-length hole filter; no general invalid-union fallback in `Spatial.Query.Union`; `Shell.Volume()` relies on raw triangle winding because `Triangle3D.Reverse()` is a no-op (LINQ `Reverse` on an array, result discarded); OCCT order sensitivity (#84 free-edge design task). Full Softhouse acceptance (48 identical references on F and R) not met.
- **Cleanup:** remote `fix/face2d-degenerate-hole-loop-q4` deleted with a lease on `40164d96` after merged/unprotected/no-open-PR checks; local branch kept because worktree `SAM-wt-face2d-hole-loop` uses it.
- **Next step:** owner chooses among the separate defects above (the `Triangle3D.Reverse()` no-op is small and testable) and the #84 free-edge design task. Copy the machine-local evidence before switching machines.

## Q4 closeout: SAM#225 (PanelsBySolids junction blocks shorter than the wall thickness) - 2026-10-27

- **PR/status:** merged into `sow/2026-Q4` with `--merge --match-head-commit`, no auto-merge, bypass or squash, on owner authorisation -> `849a5c0a688c80011a8b08d1dc25354c275e6936` (parents `61628b62`, head `d3d07658c7df6a9fed65f109178da5b66fd049c2`; tested content `7763fd03`, the last commit is record-only). Before merging it was rebased, with owner authorisation, from `fe5bbce4` (head `cb11d6c4`) onto `1f46f7ab` (SAM#226). There were no GitHub reviews or comments, codex included. Record `documentation/PanelsBySolids-Junctions-Q4.md`.
- **Work:** the `PanelsBySolids` fragment pass gains a junction route: blocks shorter than the wall thickness that bridge two walls of their thickness are kept. Jamb/slice readings are replaced by the wall plane they cross. The Embeds override covers a block that continues its own element on two opposite sides. All are reported by the existing PBS103 with new reason text; no new code and no public API change. `SAMAnalyticalCreatePanelsBySolids` 1.3.0 -> **1.4.0** (#226 took 1.3.0), with its pin. Softhouse extraction: 376 -> 384 panels, 28 -> 19 skipped solids, 12 of 392 solids change.
- **Owner decision:** the house D additions are accepted. #361 recovers the facade plane from its jamb/head reading and hosts six apertures. #370 is the short facade connection between neighbouring wall pieces. #386/#390 are intermediate extraction pieces dropped during snapping, which with #226 no longer move neighbours. This accepts the SAM extraction change, not full Softhouse recovery.
- **Validation:** local, rebased content `7763fd03`: SAM.Tests 3808/3808, SAM.Core.Grasshopper.Tests 87/87 (the first complete GH run for this PR). The `range-diff` against `cbf3f520` shows only the version, the pin and its comment. Combined Softhouse F (SAM `7763fd03` + SAM_OCCT#84 `a77ef7d`, no kink) gives 58 spaces, C109 and C203 good, 47/48 retained (A107 lost via #84); the kink comparison gives 57 spaces, 47/48. The room tables are byte-identical to the #226 combined runs, and the panels match apart from Guids. `%APPDATA%\SAM` unchanged. Hosted at `d3d07658`: Build `40488624723`, SPDX `40488624641` and Test `40488624912` (3808 tests) green. Machine-local evidence: `SAM_daily/2026-10-25-Softthouse/handover/evidence/2026-10-27-pr225-rebase/`.
- **Unresolved / limits:** the full Softhouse gate is not met. Dining 4 A107 is lost (SAM_OCCT#84). The reversed-face run gives 0 spaces (the #37 Clean3D slab drop). The Clean3D kink candidate changes the towers GH pin. The LevelFrame single-cap datum margin is only 1.7 %. The pre-existing `PanelsBySolid` face-order sensitivity (#341, #87, #21, #5) now also affects #370/#362. The #226 closeout's "rebase SAM#225" next step is done.
- **Cleanup:** remote `fix/panels-by-solids-junctions-q4` deleted with a lease on `d3d07658`, after checking it was fully merged into Q4, unprotected and in no open PR. The local branch is kept because worktree `SAM-wt-junctions` uses it; the worktree and evidence are untouched.
- **Next step:** owner chooses the next Softhouse blocker: A107 in SAM_OCCT#84, the R / #37 Clean3D slab drop, or the kink/towers disposition. Copy the machine-local evidence folders explicitly when switching machines.

## Q4 closeout: SAM#226 (SnapToJunctions: a dropped panel is not a junction) - 2026-10-27

- **PR/status:** merged into `sow/2026-Q4` with `--merge --match-head-commit`, no auto-merge, bypass or squash, on owner authorisation -> `1f46f7abb96efdb4b33c2808d9b6963c88a74b77` (parents `fe5bbce4`, head `ecae45cc8766c4a79bd9d6d51aed50c45758f534`; tested content `ba6365bd`, the last commit is record-only). Independent reviews ACCEPT at that head (owner-run, including codex); no GitHub reviews or comments existed. Record `documentation/SnapToJunctions-Dropped-Targets-Q4.md`.
- **Work:** `Query.SnapToJunctions` settles the dropped set by repeated passes: each pass snaps against the targets outside the set, and the set becomes the panels that close up. It is re-evaluated, so a panel that closed up only on a dropped target is revived. A repeated set (cycle) or the 10-pass limit returns the panels unsnapped with the new Error `SNP003 SnapNotSettled` (a GH Error balloon); `docs/DIAGNOSTICS_STANDARD.md` §3 names it as the one BySolids 00x Error. `SAMAnalyticalCreatePanelsBySolids` 1.2.0 -> 1.3.0 with its pin. Public signatures and the `SnapChecked`/`SnapCollapsed` texts are unchanged.
- **Cause fixed:** on Softhouse with SAM#225, dropped reveal #362 pulled slab #134 off the facade mid-plane. That flipped the OCCT first-floor datum 3.475 -> 3.45 and lost Carpark 2 C109 and Bed 4 C203. Base jamb/head strips had the same kind of pull. Investigation: machine-local `SAM_daily/2026-10-25-Softthouse/handover/evidence/2026-10-27-reveal362/`.
- **Validation:** local SAM.Tests 3795/3795 (base source + new tests 3790/3792, exactly the 2 new failing tests) and SAM.Core.Grasshopper.Tests 87/87; no existing expectation re-pinned. Hosted at `ecae45cc`: Build, SPDX and Test (3795 tests) green. Managed snap equals the investigation candidate (deviation 0); Clean3D/Extend3D equals the drop-#362 control (datum 3.475, 52 caps). Native Softhouse F: combined #225 + this + SAM_OCCT#84 gives 47/48 (C109 and C203 recovered; A107 lost, inherited from #84); base-only Q4 + this gives 48/48; R 0 spaces (separate #37). `%APPDATA%\SAM` byte-identical. Machine-local evidence: `.../handover/evidence/2026-10-27-snap-targets/`.
- **Unresolved / limits:** SAM#225 also uses CreatePanelsBySolids 1.3.0 and must move to 1.4.0. The full-model 48/48 gate is not met (A107 / SAM_OCCT#84). Open elsewhere: R / #37 Clean3D slab drop; LevelFrame single-cap datum margin 1.7 % (SAM_OCCT). The SNP003 balloon is checked in source and headless tests only. By design, a cycle disables snapping for the whole model, reported as an Error.
- **Cleanup:** remote `fix/snap-to-junctions-dropped-targets-q4` deleted with a lease on `ecae45cc` after checking it was fully merged into Q4, unprotected and in no open PR. Local branch kept because worktree `SAM-wt-snap-targets` uses it; worktree and evidence untouched.
- **Next step:** rebase SAM#225 onto `sow/2026-Q4` `1f46f7ab`, move it to 1.4.0 (extend the pin comment), and rerun its tests and the combined Softhouse chain. Copy the machine-local evidence folders explicitly when switching machines.

## Q4 closeout: SAM#223, #224 (PanelsBySolid ownership sample, AperturesBySolids units) - 2026-10-25

- **PR/status:** both merged into `sow/2026-Q4` with `--merge --match-head-commit`, no auto-merge, bypass or squash, on owner authorisation after the independent review `.review-pr223-224-20261025/revision3/REVIEW.md` (ACCEPT both). No GitHub reviews or comments existed on either PR.
  - SAM#223 -> `0c9c5894f923b15b76580f40507ce435032758ba` (parents `81a781ab`, head `c46cb5546ee71bd65514ceac5b44a793b74d1b0f`). `PanelsBySolid` material sampled at the deepest point of a face pair's overlap; `CreatePanelsBySolids` 1.2.0; record `documentation/PanelsBySolid-OwnershipSample-Q4.md`.
  - SAM#224 -> `10b0dc94` (parents `0c9c5894`, head `1a32cae7ca5491f41fdfcba557e30f1bb4ca559d`). `AperturesBySolids` strict-enclosure units so framed windows in a row stay separate; `CreateAperturesBySolids` 1.1.0; record `documentation/AperturesBySolids-Units-Q4.md`.
- **Decisions:** #224 met one adjacent-line conflict (`Grasshopper/SAM.Core.Grasshopper.Tests/BySolidsDiagnosticDetailTests.cs`). Q4 was merged into its branch (merge `629cca4b`, no rebase or force-push), keeping both assertions (CreatePanelsBySolids 1.2.0, CreateAperturesBySolids 1.1.0) and comments. The diff of the integration head against Q4 and against the accepted head shows no other production, parameter, GUID, default, fixture or tolerance change; the record-only commit `1a32cae7` followed the tested merge.
- **Validation:** pre-merge both PRs OPEN, non-draft, MERGEABLE, Build/Test/SPDX green at the accepted heads. Merged #224 content, local (APPDATA redirected; suites one after the other): SAM.Tests 3785/0/0, SAM.Core.Grasshopper.Tests 87/0/0, `git diff --check` clean. A first GH run with USERPROFILE and LOCALAPPDATA also redirected hung at Rhino start-up (aborted by the hang timeout); the rerun with only APPDATA redirected passed. Hosted on `1a32cae7`: Build `40234264192`, Test `40234264215`, SPDX `40234264211` all green. Post-merge Q4: Build/Test green on `0c9c5894` and `10b0dc94`. Machine-local evidence: `SAM-BIM/.evidence-pr223-224-integration/`. No OCCT, simulation, installation or deployment run.
- **Unresolved / declared limits:** the deepest-point sample is a heuristic, not a whole-volume proof. Unit enclosure tests vertices against a convex hull within `maxGap` (a re-entrant outline nested in a re-entrant frame could be accepted); a free member touching two explicit groups joins one by input order (inherited); ambiguous sets fall back silently with no diagnostic; GH behaviour validated by the headless suite only (no interactive canvas or saved-definition round trip); GH metadata extraction `partial`, not a compatibility PASS. Softhouse 101 apertures is agent aperture-only evidence; the hosted 96 is historical. Known Softhouse gaps from the previous closeout remain.
- **Cleanup:** remote `fix/panels-by-solid-ownership-sample-q4` and `fix/apertures-by-solids-units-q4` deleted with leases on `c46cb554` and `1a32cae7` after checking each fully merged into Q4, unprotected, in no open PR, local tip equal to remote. Local branches kept because `SAM-wt-ownership-sample` and `SAM-wt-aperture-units` use them; worktrees and evidence folders untouched.
- **Next step:** owner decides the next Softhouse step (remaining ownership/relation gaps, open-plan virtual boundaries). Copy `.review-pr223-224-20261025/`, `.evidence-pr223-224-corrections/` and `.evidence-pr223-224-integration/` explicitly when switching machines.

## Q4 closeout: SAM#220, #222, #221 (Rhino unit scale, spatial union degenerate face, PanelsBySolids fragments) - 2026-10-25

- **PR/status:** all three merged into `sow/2026-Q4` with `--merge --match-head-commit`, in this order, without auto-merge, bypass or squash. Owner authorisation was given in chat after the independent review `.review-four-pr-20261025/revision2/REVIEW.md` (ACCEPT). No GitHub reviews or comments existed on any PR.
  - SAM#220 -> `626a23e5e294d51e64f041cb56ba267afd1db54b` (parents `457defbb`, head `1c41332d4d696220578af1d1473f05eef776a0d9`). Rhino `UnitScale` with no active document; record `documentation/Rhino-UnitScale-NoActiveDoc-Q4.md`.
  - SAM#222 -> `924fec5245774d8234caa55db4c59d5ca5a6d8b6` (parents `626a23e5`, head `bf508d6ee49749585b4d1cf7b19769c107672774`). `Union.cs` degenerate-face handling; record `documentation/Spatial-Union-Degenerate-Face-Q4.md`.
  - SAM#221 -> `945eea51e9e80795aa6fd45ad5a4cf929c6bee33` (parents `924fec52`, head `2bb1cba501207a89e7381cf7e9d05a98d4f1f741`). `PanelsBySolids` fragment recovery with stable pass-snapshot decisions, mean-width small-face guard in Clean/SnapToJunctions, diagnostics codes; record `documentation/PanelsBySolids-Fragments-Q4.md`.
- **Decisions:** later merge trees legitimately include earlier task changes, so combined content (not per-head equality) was verified: each merge diff against its first parent touches only the PR's own files. Stop conditions (head change, failing checks, conflict) did not occur.
- **Validation:** pre-merge each PR was OPEN, MERGEABLE/CLEAN with Build, Test and SPDX green at the approved head. #221 final-head hosted Test `40223873726` (3759 passed, synthetic merge `e56fbd4`), Build `40223873727`, SPDX `40223873741`. Post-merge Q4: Build/Test green on `626a23e5` (`40225289206`/`40225289149`), `924fec52` (`40225314741`/`40225314745`) and the final combined tip `945eea51` (Build `40225328796`, Test `40225328674`, 3761/3761). Independent reviewer local results: SAM 3759/3759, SAM GH 85/85, OCCT unit 1038/1038. No local rebuild, engine, simulation or deployment run in this closeout.
- **Unresolved / declared limits:** acceptance covers the bounded robustness/fragment changes only, not full Softhouse or simulation acceptance. Known Softhouse ownership miss, two missing relations and six not-enclosed cells remain. The mean-width test is a tolerance-scale heuristic, not proof for every face shape.
- **Cleanup:** remote `fix/rhino-unitscale-no-doc-q4`, `fix/spatial-union-degenerate-face-q4` and `fix/panels-by-solids-continuation-q4` deleted with leases on the approved heads after checking each was fully merged into `sow/2026-Q4`, unprotected and in no open PR as head/base. Local branches are kept because worktrees `SAM-wt-unitscale`, `SAM-wt-union-degenerate` and `SAM-wt-bysolids-continuation` use them; worktrees untouched.
- **Next step:** owner decides the next Softhouse step (open-plan virtual boundaries, then the remaining ownership/relation gaps); see the SAM_OCCT closeout for #83. Copy local evidence folders explicitly when switching machines.

## Q4 S11 F-S11-2 closeout (#219, fractional SpaceSimulationResult unmet hours) - 2026-10-24

- **PR/status:** SAM-BIM/SAM#219 merged into `sow/2026-Q4` as `ac68ad0fa7d66fe387e3ff7e009c91be5eb10a6d`, with base parent `b7c2c36aed7fcc6effd2f7e5888f2148e6e1c4c5` and approved head `db48598b30cf2711af40a96cb55bbe6c5e5fb713`. The merge tree equals the approved head. Owner authorization was given in chat; the PR was marked Ready and merged with `--merge --match-head-commit`, without auto-merge or bypass. The independent review accepted tested content `47cc5dd8` and the later record-only commits `eadffe50`/`db48598b`; no review findings remained.
- **Completed:** `SAM/SAM.Analytical/Enums/Parameter/SpaceSimulationResultParameter.cs`: `UnmetHours` changed from `IntegerParameterValue(0, 8760)` to `DoubleParameterValue(0, 8760)`; member, ordinal, key `"Unmet Hours"`, range and JSON shape are unchanged. `OccupiedUnmetHours`, `OccupiedHours`, `UnmetHourFirstIndex` and the adjacency-cluster `UnmetHours` stay integer. The old metadata rounded half to even (`Convert.ToInt32`: 14.5 -> 14, 14.75 -> 15); fractional EnergyPlus not-met time now survives. Old integer JSON still loads and reads through `TryGetValue<double>` and `TryGetValue<int>`; saved files are not rewritten and previously rounded values are not recoverable. Files: that metadata line, `SAM/SAM.Tests/SpaceSimulationResultUnmetHoursTests.cs` (12 tests) and the record `documentation/S11-FractionalUnmetHours-PR.md`.
- **Decisions:** Revit compatibility is DEFERRED by the owner (2026-10-24) and does not block this fix: SAM_Revit `SetValue_Integer` refuses `double`, so newly set values would not be written if the `SAM_UnmetHoursCooling/Heating` Revit parameters are Integer-typed (type comes from an external workbook); no SAM_Revit or shared-parameter change is authorised. NaN: the owner keeps the documented ranged-double behaviour (`SetValue(NaN)` previously threw `OverflowException`, now stores NaN; B1a readers drop non-finite values); no NaN guard or shared validation change is requested.
- **Validation:** local (recorded in the PR record): new tests 8 fail / 4 pass on base metadata and 12/12 at `47cc5dd8`; `SAM.Tests` 3738/3738; engine-free OpenStudio synthetic SQL probe 14.5/14.75 h preserved through import and JSON save/reload (base 14/15); SAM_OpenStudio Q4 tests (`TestCategory!=Simulation`) 488 pass + 2 fixture skips; B1a 51/51; SAM_Tas benchmark 40/40 and ReadBack 249/249; S10 acc2 saved models identical base vs fixed. No EnergyPlus, OpenStudio CLI, Tas run or deployment (APPDATA redirected); S10 `results-subject` not regenerated (needs Tas COM). Final-head hosted Build `40119859776`, Test `40119860103` (synthetic merge `7807e0f`, 3738/3738) and SPDX `40119859840` passed. Post-merge Q4 Build `40120261815` and Test `40120261755` on `ac68ad0f` were in progress at this closeout. Machine-local evidence `SAM-BIM/.f-s11-2-20261024/` and review `SAM-BIM/.review-s11-fractional-20261024/`; copy explicitly when switching machines.
- **Next step / unresolved:** the separately authorised S11 offline integration on existing Draft SAM-BIM/SAM_OpenStudio#34: integrate F-S11-1 (SAM_OpenStudio#35, `4b020585`) and this fix, replace the defect-pinning tests with positive regressions, and prove 14.5/14.75 h through SQL import, SAM save/reload and B1a, engine-free. S11 real-run acceptance, S12 and Phase B remain unauthorised. AddResults suffix-only design-day/surface lookups and exact-name mapping for identical generated names remain separately tracked. Revit compatibility remains deferred. Guarded cleanup of the merged task branch follows this closeout under AGENTS.md.

## Q4 S07 amendment U1 closeout (#218, OpenStudio opaque optical face scope) - 2026-10-23

- **PR/status:** SAM-BIM/SAM#218 merged into `sow/2026-Q4` as `b06dbe8c6341a810af7bef7b08570a0ddad0c31c`, with base parent `ba686d244f12db2ba99e85b27eaae957de064378` and approved head `ff518a37bb7387ea1b62efbf709bba9e281367ca`. The merge tree equals the reviewed head. Owner authorization was given in chat; merge used `--merge --match-head-commit`, without auto-merge or bypass. Final checks passed and no review findings remained.
- **Completed:** documentation-only U1 in `documentation/SIMULATION_INPUT_MAPPING_S07.md` (the only PR file): OpenStudio row-3a optical values are gated on engine-used faces; valid comparable unused-face rows remain visible as informational `OUT-OF-ENGINE-SCOPE`, explicitly allowed by the scoped PASS rule. This is distinct from SAM absence and accepted limitations. Used-face optics, all-layer thickness/thermal properties, invalid/missing/unreadable values and unresolved face roles remain gated. Tas/S08, OD-L1, tolerances and roadmap gates are unchanged. Content `ff89b9bdef5e99f0f2e9f2e50f3ca109fe222934`; final commit adds the PR record only.
- **Validation:** local `git diff --check` and exact scope/merge-tree checks passed; no local suites, engines, sizing, acceptance/export or packing scripts run for this documentation change. Final-head hosted Build `39906323656`, Test `39906323573` (3726/3726) and SPDX `39906323541` passed; Test used synthetic merge `533e135`. Independent primary-source review supports ordinary opaque construction endpoint semantics: EnergyPlus 25.1 I/O Reference, Surface Construction Elements and Thermal Zone Geometry; pinned `v25.1.0/src/EnergyPlus/HeatBalanceSurfaceManager.cc` (inside/outside layer assignments and boundary handling). Special boundary algorithms/optical overrides are not blanket-covered; unknown roles remain fail-closed. Simulation effects remain unmeasured. Machine-local review: `C:\Users\michal\.codex\visualizations\2026\10\22\01a16513-2642-7d60-8e9f-c434cf4fd331\s09-review\SAM-218-REVIEW.md`; copy explicitly when switching machines. Post-merge Q4 CI has not been checked at this closeout.
- **Next step / unresolved:** U1 is approved by this merge, but its downstream implementation in SAM_OpenStudio#33 needs separate owner authorization and independent review. #33 remains unmerged; no S09 acceptance/closeout or S10 start is authorized. OD-L1/Tas fallback and stale roadmap status remain separate work. Guarded cleanup of the fully merged U1 task branch follows this closeout under AGENTS.md.

## Q4 S07 amendment L1 closeout (#217, lighting split) - 2026-10-21

- **PR/status:** [SAM-BIM/SAM#217](https://github.com/SAM-BIM/SAM/pull/217) is complete (documentation only).
  - Merged into `sow/2026-Q4` as `113873a0ba376c7b18c3cd6d3556193553b829a9`. Parents: base `ee377167` and approved head
    `1bf94c491ac5b81029dcf16d56a71e7a947b6fd7`, pinned with `--match-head-commit`. The merge tree equals that head.
  - The owner authorised the merge in chat (2026-10-21). Immediately before the merge: head and base unchanged,
    MERGEABLE/CLEAN, final-head checks PASS. The independent review (Revise, R1-R5) was addressed in revision 1, and
    the owner reviewed revision 1 in the same session. There are no GitHub reviews or comments on the PR.
- **Completed:** amendment L1 to `documentation/SIMULATION_INPUT_MAPPING_S07.md` (record: section "Amendment L1 PR record").
  - The row 4c identity between `LightingViewCoefficient` and EnergyPlus `Fraction Visible` is withdrawn. SAM
    `LightingRadiantProportion` is the total radiant share p, split for OS as F_V = p·s, F_R = p·(1 − s), F_RA 0,
    with F_C = 1 − p in both engines (T-LIGHT-SPLIT, §5.1).
  - **Approved (D-LIGHT, 2026-10-21):** reference category suspended LED office lighting; p₀ = 0.576,
    s = 0.2415 / 0.576 at full precision (F_R 0.3345, F_V 0.2415, F_C 0.424), a midpoint-derived reference from IEA
    SHC Task 61 D1 Table 6 (RP-1681, secondary); authored p, including 0, preserved; one absent-value fallback with
    diagnostics.
  - **Revision 1:** T-LIGHT-SPLIT applies to an authored p only; an absent or NaN p is DOCUMENTED-DEFAULT, needing
    owner acceptance per quantity and fixture, never PASS. A-Tas Theory Manual p. 52 (eq 9-7, 9-8, 9-11) is cited
    with the qualification that its applicability to installed Tas 9.5.7 remains unconfirmed. Regulatory, NCM,
    TM59 and template category limits are stated. EDSL spectrum factors are corrected.
  - Content SHAs: `eb7e3ff7` (original) and `48e7a8dc` (revision 1), each followed by a record-only commit.
- **Open owner decision OD-L1 (§8):** whether p₀ suits cases outside its category, and whether a writer may replace a
  prescribed NCM or template value with it. Also still open: OD-G1 to OD-G3, OD-W1/OD-W2, the LED view coefficient.
- **Validation:**
  - **Local documentation checks only; no simulation, sizing or native-engine run.** Citations resolve at the pinned
    SHAs (SAM `6836595d`, SAM_Tas `e84aeff6` and `c50e9169`, SAM_OpenStudio `7f216657`, SAM_LadybugTools
    `0ffbaecf`); split arithmetic and spectrum factors recomputed; `git diff --check` clean; the diff touches only the
    mapping document (+361/-4 against `ee377167`).
  - **Hosted final-head checks on `1bf94c49`, all PASS:** Build (Windows) [39665103737](https://github.com/SAM-BIM/SAM/actions/runs/39665103737),
    Test [39665103708](https://github.com/SAM-BIM/SAM/actions/runs/39665103708) and SPDX [39665103716](https://github.com/SAM-BIM/SAM/actions/runs/39665103716).
  - **Post-merge Q4:** Build (Windows) [39667707679](https://github.com/SAM-BIM/SAM/actions/runs/39667707679) and Test
    [39667707673](https://github.com/SAM-BIM/SAM/actions/runs/39667707673) on `113873a0` were in progress when this was written.
- **Not changed:** no code, fixture, model, workflow or roadmap change. No writer is implemented: until the
  downstream changes land, the pinned OS, Tas and Ladybug writers keep the behaviour recorded in §2 item 2.
- **Known stale records (not fixed here):** the roadmap S08 entry still says PR-B/PR-C are not started although
  SAM_Tas#106 merged; the S09 row says "Not started" while SAM_OpenStudio#33 is open; the roadmap log does not record
  amendment L1. Only a reviewed roadmap PR (or an owner exception) may change them.
- **Cleanup:** remote `docs/s07-lighting-split` deleted with a lease on `1bf94c49` after checking that it was the
  approved head, fully merged into `sow/2026-Q4`, and used by no open PR or other worktree. No same-named branch
  exists in SAM_Tas, SAM_UI, SAM_OpenStudio, SAM_Validation or SAM_LadybugTools. The checkout was fast-forwarded to
  Q4 and the local branch deleted; other SAM worktrees were left untouched.
- **Exact next step:** revise SAM_OpenStudio#33 (branch `feature/s09-openstudio-input-readback`) to §5.1: writer
  lighting block, importer, mapping and coverage documents, S09 read-back rows, expected vectors and negatives, with
  its own review. Recommended: Claude Opus 5.5, High effort, existing #33 work in a new session, Plan mode first,
  attaching SAM_OpenStudio and SAM. The SAM_Tas fallback and S08 read-back change needs OD-L1 and a separate owner
  write approval; the SAM_LadybugTools change is a separate task.

## Q4 simulation roadmap S07 closeout (#216) - 2026-10-20

- **PR/status:** [SAM-BIM/SAM#216](https://github.com/SAM-BIM/SAM/pull/216) is complete (documentation only).
  - Merged into `sow/2026-Q4` as `68b602571c90e21d0bbd70609cbe4fc884b97a53`. Parents: base `6836595d` and approved head `d11e92ad34aecc7d780bd011237b84ff7771529c`, pinned with `--match-head-commit`. The merge tree equals that head.
  - The owner authorised the merge in chat ("I approve merging SAM#216 at head d11e92ad…", 2026-10-20). Immediately before the merge: head and base unchanged, not draft, MERGEABLE/CLEAN, no reviews or comments, final-head checks PASS.
- **Completed:** `documentation/SIMULATION_INPUT_MAPPING_S07.md` (new) is the S07 Phase B physical-input mapping contract, in three revisions.
  - Tested content SHAs: `32d519b8`, `03794f42` and `dee6f7f0`; each was followed by a record-only commit.
  - **Approved by the owner, 2026-10-20 (S07 plan revision 3):**
    - D-ROUTE: contract route Direct T3D with `UseWidths=false`;
    - D-F2: ventilation V0/V1/V2, with requirement separated from delivery;
    - D-F8: window opening;
    - D-F9 (A + C): zone volume;
    - D-FIX: fixture criteria;
    - the film convention and every §5 tolerance.
  - The §6 gate is a strict PASS allow-list. Rank 3 covers only an unaccepted DOCUMENTED-DEFAULT on a gated quantity; an accepted one is rank 5, Not PASS.
  - Tas T-FRAME is derived from pinned source; four native uncertainties are left for S08 read-back.
  - **Proposed, not approved:** §6.1 fail-closed handling of the three gate gaps (OD-G1 to OD-G3) and the §4.1 widths-ON reference (OD-W1, OD-W2).
  - `documentation/SIMULATION_ROADMAP_Q4_2026.md`: S07 row and closing line ("S07 In review"); decisions 3 and 5 updated; new owner decision 7.
- **Owner decision 7 (recorded, not implemented):**
  - Direct is the future Tas production default. It was first approved with `UseWidths=false`, then revised the same day to `UseWidths=true` as the owner-preferred candidate.
  - The candidate needs independent verification against the intended geometry on the approved fixtures. `UseWidths=false` stays an explicit option.
  - If candidate validation fails, the proposed widths setting reverts to `false` with the reason reported. No silent or automatic runtime fallback.
  - Panel convention comes from the fixture's provenance and declaration, not from engine measurements. SAM supports mid-surface panels, e.g. `PanelsBySolid` with `PhysicalThickness`.
  - Production (`WorkflowSettings`, SAM_UI, the Both producer) stays gbXML until a separate integration PR, against the decision 7 criteria.
- **Validation:**
  - **Local documentation checks only; no simulation or native-engine validation.** The seven Q4 pins equal the `ls-remote` tips. All 111 S07 citations and line ranges resolve at the pinned SHAs. Links and anchors resolve, and every URL is `github.com/SAM-BIM/*`. `git diff --check` is clean, and the diff touches the two documentation files only (+458/-4 against `6836595d`).
  - **Hosted final-head checks on `d11e92ad`, all PASS:** Build (Windows) [39437732754](https://github.com/SAM-BIM/SAM/actions/runs/39437732754), Test [39437732848](https://github.com/SAM-BIM/SAM/actions/runs/39437732848) and SPDX [39437732816](https://github.com/SAM-BIM/SAM/actions/runs/39437732816).
  - **Post-merge Q4:** Build (Windows) [39439028018](https://github.com/SAM-BIM/SAM/actions/runs/39439028018) and Test [39439028013](https://github.com/SAM-BIM/SAM/actions/runs/39439028013) on `68b60257` were in progress when this was written.
- **Not changed:** no code, schema, workflow, fixture, gitlink or tracker change. No simulation, no tolerance change, no route or default switch, no S08 start, no Phase C change.
- **Known stale record:** the roadmap's S07 row and closing line still read "In review". Only a reviewed PR may change the roadmap, so mark S07 Done (`68b60257`) in the next roadmap PR. This closeout does not change it.
- **Cleanup:** remote `docs/s07-physical-input-mapping` was deleted with a lease on `d11e92ad`.
  - Checked first: the remote head was the approved head; it was fully merged into `sow/2026-Q4`; no open PR used it as head or base; no worktree used it.
  - No same-named branch exists in SAM_Tas, SAM_UI, SAM_OpenStudio or SAM_Validation, so no downstream CI resolution is shadowed.
  - The checkout was fast-forwarded to Q4 and the local branch deleted. Other SAM worktrees and unrelated untracked folders were left untouched.
- **Exact next step:** none for S07. S08 (Tas independent native-input read-back) is Plan-first and needs:
  - an explicit owner start;
  - a separate SAM_Tas write approval (roadmap decision 2);
  - owner-selected or cleared equivalence fixtures with provenance, hashes, a declared panel convention and the D-FIX criteria;
  - owner answers to OD-G1 to OD-G3 and OD-W1/OD-W2 before the S08/S09 gate.

  Recommended: a new Claude Code session, Claude Opus 5.5, High effort, Plan mode, attaching SAM, SAM_Tas and SAM_UI. The Direct default switch is separate integration work, not part of S08.

## Q4 simulation roadmap S06 closeout (#215) - 2026-10-20

- **PR/status:** [SAM-BIM/SAM#215](https://github.com/SAM-BIM/SAM/pull/215) is complete (documentation only).
  - Merged into `sow/2026-Q4` as `d8c33f8da07b929646ddaedfa5db4b8363205415`. Parents: base `df27563b` and approved head `90cf9a5ee9a3b2003ebf65ee5c93f77e637c9bb8`, pinned with `--match-head-commit`. The merge tree equals that head.
  - The owner authorised the merge in chat ("merge the roadmap PR when checks pass", 2026-10-20). Immediately before the merge: not draft, MERGEABLE/CLEAN, no reviews or comments, `sow/2026-Q4` unprotected.
- **Completed:** `documentation/SIMULATION_ROADMAP_Q4_2026.md` records **S06 Done**:
  - SAM_Deploy#66 `1657bb53`, SAM_OpenStudio#32 `a9429f7c`, SAM_Validation#16 `e9a83df0`;
  - closeouts SAM_Deploy `3b7edb0`, SAM_OpenStudio `7f21665`, SAM_Validation `2a3acfa`.

  It also records:
  - **The genuine-installer acceptance:** the hosted installer run #227 installed into an isolated profile; Tas, OpenStudio and Both PASS from the real ribbon, numerically the same as S05. OpenStudio's raw `exactMatch: false` comes only from run-generated subsurface names, mapped 1:1 through their host-panel references.
  - **The repinned build:** installer run #228 at `b3b8e9e`, payload-equivalent to #227 with 0 drift, so the licensed evidence carries forward.
  - **Provenance check 9:** **Not PASS, accepted limitation** (owner, 2026-10-20). Installed producers report the SAM_Deploy build revision as `samCommit`/`runnerCommit`; recover the repository revisions from that commit's gitlinks. Owner decision 6 now records this S06 outcome.
  - **Phase A gate met, with that explicit exception.** "No task is active. S07 is the next candidate and needs an explicit owner start."
- **Validation:**
  - Hosted PR-head checks on `90cf9a5e`: Build (Release) [39415452427](https://github.com/SAM-BIM/SAM/actions/runs/39415452427), test (Release) [39415452550](https://github.com/SAM-BIM/SAM/actions/runs/39415452550) and SPDX [39415452429](https://github.com/SAM-BIM/SAM/actions/runs/39415452429), all PASS.
  - Post-merge Q4: Build (Windows) [39416082276](https://github.com/SAM-BIM/SAM/actions/runs/39416082276) and Test [39416082362](https://github.com/SAM-BIM/SAM/actions/runs/39416082362) PASS on `d8c33f8d`.
  - Diff scope: the roadmap file only (+31/-6), and `git diff --check` is clean.
- **Not changed:** no code, schema, workflow, gitlink or tracker change. No S07 start, no F2/F8/F9 or tolerance decision, no Phase C change, no release, tag or deployment.
- **Cleanup:** remote `docs/roadmap-s06-closeout` was deleted with a lease on `90cf9a5e`, after confirming it was fully merged and that no open PR used it. The checkout was fast-forwarded to Q4 and the local branch deleted. The untracked local folders `600-*` in the SAM checkout are unrelated and were left untouched.
- **Exact next step:** none for S06. S07 (minimal Phase B physical-input mapping) needs an explicit owner start. Any release is a separate owner decision.

## Q4 simulation roadmap Phase A status (#214) closeout - 2026-10-20

- **PR/status:** [SAM-BIM/SAM#214](https://github.com/SAM-BIM/SAM/pull/214) complete (documentation only), merged into `sow/2026-Q4` as `572dd7e6e43110a6970ed8c8a59d49585ba15735`; parents base `62b54156` and approved head `5c36d26cdd2447bc2be55494eb6caddd2f5b224d` (`--match-head-commit`; merge tree equals that head). Owner authorisation in chat, 2026-10-20 (S03/S04/S05 closeout session).
- **Completed:** `documentation/SIMULATION_ROADMAP_Q4_2026.md` records S01-S05 Done (SAM_Tas#103 `e483877c`; SAM_UI#240 `5b379191`, #241 `49eebe13`, #242 `1e460bbe`, #243 `307cb0e3`, #245 `9acce829`; preflight #244 `46a43d8d`), S06 Next, the Phase A gate as not yet passed (S06), owner decision 6 (`SAM_COMMIT` until S06) and the S05 multizone fixture (SAM_OpenStudio's committed two-cell model).
- **Validation:** hosted final head Build (Release) `39379747697`, test (Release) `39379747684` and SPDX `39379747688` PASS.
- **Cleanup:** remote and local `docs/roadmap-phase-a-status` deleted (lease on `5c36d26c`).
- **Unresolved:** owner decisions 2 (SAM_Tas for S08/S10), 3 (Phase B multizone fixture), 4 (trackers), 5 (F2/F8/F9, S07 tolerances). Local note: this laptop's `SAM\build` was pre-F1 until the 2026-10-20 rebuild (see SAM_UI #245 record).
- **Next step:** S06 (early SAM_Deploy smoke candidate) after an explicit owner start: new session, Claude Opus 5.5, effort High, Build mode.

## Q4 simulation roadmap (S00) closeout - 2026-10-19

- **PR/status:** [SAM-BIM/SAM#213](https://github.com/SAM-BIM/SAM/pull/213) complete, merged into `sow/2026-Q4` as `b4e681485702b92abafc2b80f893065c6e62a822` (parents base `a2069103` and approved head `bc58f4e06108a750c615a07539058fda0dfae192`; merge tree equals that head). Companion [SAM-BIM/SAM_UI#239](https://github.com/SAM-BIM/SAM_UI/pull/239) merged as `d5aa34b29b9a01af76bb0dc457021b83cb5adc7a` (closeout in SAM_UI `PROJECT_PROGRESS.md`). The owner approved both after the pre-merge checks.
- **Completed/files:** `documentation/SIMULATION_ROADMAP_Q4_2026.md`, the canonical simulation roadmap. Phase A: reliable Tas/OpenStudio/Both workflows and failure handling (active). Phase B: independent native-input and results verification. Phase C: engine comparison science, physics parity and ASHRAE 140 (frozen). Numerical Tas/EnergyPlus agreement is not an acceptance requirement. Ordered register S00-S12 with repository, scope, related issues/PRs, acceptance and status; Phase A, Phase B and final installation gates; owner decisions. One task active at a time; the roadmap authorises no later task. Documentation only.
- **Owner exception (narrow):** the S00 status in the roadmap was set to Done in this closeout commit, directly on `sow/2026-Q4`, because the owner instructed "Close S00 in the roadmap" (2026-10-19). Any other roadmap change goes through a reviewed PR.
- **Validation:** final-head GitHub checks at `bc58f4e0`: Build `39274437960`, Test `39274437963` and SPDX `39274438091` PASS. Post-merge Q4 Build `39275158731` and Test `39275158341` PASS. `git diff --check` clean. Roadmap links (SAM#206, SAM_UI#227, SAM#213, SAM_UI#239) resolve; referenced SAM_UI records exist. `sow/2026-Q4` has no branch protection or rulesets, so every PR workflow was treated as required.
- **Unresolved / owner decisions (roadmap list):** whether SAM_Tas may change now (S01, S08, S10); HungaryHouse provenance and redistribution permission (not committed); whether trackers SAM#206/SAM_UI#227 are updated to point to the roadmap; F2/F8/F9 semantics and S07 tolerances.
- **Cleanup:** after the merge, remote `docs/simulation-roadmap-q4` deleted with an expected-SHA lease (`bc58f4e0`), and the local branch deleted.
- **Next step:** no task is active. S01 (Tas silent-failure detection) is the next candidate and needs an explicit owner start. Recommended: Claude Opus 5.5, Medium effort, new session, Plan mode first (it touches SAM_Tas and needs decision 2).

## Q4 EPW weather export F1 (physically correct radiation and sky fields) closeout - 2026-10-19

- **PR/status:** [SAM-BIM/SAM#212](https://github.com/SAM-BIM/SAM/pull/212) complete, merged into `sow/2026-Q4` as `5ac57033f4825e088d4b73ed59947b6739fdef6a` (parents base `618db819300df8b698b3bbe5174a203244134738` and pinned approved head `8de080697dd77d2cc6fb0d357f593f3175dae387`; merge tree equals that head). The owner passed the five-gate acceptance and authorised the merge. Production content `b721c5f4`; `a2cb16cf` adds one review test and evidence; `90822fac`, `a55c39b8` and `8de08069` change only the record.
- **Defect (finding F1 from SAM_UI PR #239):** `SAM.Weather.Query.DataString`, used by `Convert.ToEPW` and SAM_OpenStudio `EmbeddedAnnualWeatherPath`, wrote global horizontal and horizontal IR as 0, the horizontal beam (GHI - DHI) into the direct-normal field, sky cover as a 0-1 fraction (a factor 10 lost on every export/import cycle), and missing radiation and dew point as 0.
- **Fix:** GHI stated (or direct + diffuse). DNI = horizontal beam / hour-mean cos zenith on the EPW time base (hour ending, local standard time; NOAA/Spencer geometry, 60 sub-steps), bounded by the pure-Rayleigh-atmosphere beam (Kasten-Young air mass, Kasten 1996). Without latitude, longitude and time zone, DNI is 9999. ETR fields are astronomical. IR is 9999, so EnergyPlus derives it. A missing dew point is derived from DBT and RH. Cloud cover x 10 is written to total and opaque sky cover. EPW missing codes are used instead of 0. Public overloads are kept; location overloads are added. Files: `SAM/SAM.Weather/Query/{DataString.cs,EPWSolarGeometry.cs}`, `SAM/SAM.Tests/WeatherEPWExportTests.cs` (43 cases), five TMYx fixtures in `SAM/SAM.Tests/Resources/Weather/` (gzip, SHA-256, attribution in `THIRD_PARTY.md`). Record: `documentation/EPW-Weather-Radiation-F1-PR.md`. Evidence: `documentation/evidence/epw-weather-f1/`.
- **Verification:** the test geometry is independent (Michalsky 1988). For the self-consistent Sydney, Singapore and Fairbanks references, annual DNI is within +0.3 %, the sun-up median within 0.3 % and N/E/S/W facade beam within 0.4 %. The Rayleigh bound binds in 0 hours for them. It binds only for Budapest (4.0 % of beam hours, sun below ~4.7 deg) and Phoenix (6.0 %, below ~21 deg), where it removes 0.18 % and 0.38 % of the stated horizontal beam. Fail-first: 34 of 38 original tests fail on the old writer. EnergyPlus 25.2 box (the PR #239 `in.idf`), corrected embedded against explicit EPW: transmitted solar +3.6 % (was -28.5 %), heating -4.2 % (was +29 %), sensible cooling +4.1 % (was -17 %).
- **Checks:** local `SAM.Tests` 3726/3726 in Release and Debug at `a2cb16cf`. Sandboxed `SAM.sln` Release rebuild had 0 errors at `b721c5f4` (same production code). Exact final head `8de08069`: build (Release) `39228366750`, test (Release) `39228366811` and spdx `39228366717` passed. Post-merge on `5ac57033`: Build (Windows) [39229005438](https://github.com/SAM-BIM/SAM/actions/runs/39229005438), Test [39229005513](https://github.com/SAM-BIM/SAM/actions/runs/39229005513) (3726 passed), SPDX [39229041812](https://github.com/SAM-BIM/SAM/actions/runs/39229041812) (dispatched; it has no push trigger), all succeeded.
- **Scientific limitations (accepted by the owner):** (1) The Budapest (ERA5-based) and Phoenix TMYx files are internally inconsistent: their DNI and GHI/DHI are about 30 min apart. SAM keeps only GHI/DHI, so embedded exports of such files carry that mismatch as an east/west facade asymmetry (Budapest W +39 % / E -7 % against the file's DNI). The Rayleigh bound is a physical safeguard only, not a correction, and the source data are never modified. When the original EPW exists, prefer the explicit-EPW route. (2) Opaque sky cover = SAM cloud cover (stand-in): under 1 % on loads. Clark-Allen IR against a dataset's IR column: up to 1.7 %. (3) The old IR of 0 had been treated by EnergyPlus as missing and derived as a clear sky (about 3 K too cold), not as a 0 W/m2 sky. (4) The TAS embedded route was not re-run (no licence used). (5) Embedded-weather results change and are not comparable with earlier runs, including PR #239 B1a. (6) Out of scope and unchanged: culture-dependent DBT/RH/wind/pressure text, missing DBT/RH/wind written as 0, the importer accepting EPW missing codes as values (planned as F2), and leap-year day labelling.
- **Machine-local evidence:** EnergyPlus runs and weather files in `C:\TasOut\f1-epw-weather-20261019` (input SHA-256 in `run-inputs.sha256`), which depend on `C:\TasOut\licensed-e2e-20261019`. Copy both folders to reproduce elsewhere.
- **Cleanup:** remote `fix/epw-weather-radiation-q4` was deleted with an expected-SHA lease (`8de08069`) after it was verified fully merged and used by no open PR. The local branch is kept because the worktree `SAM-wt-epw-radiation` uses it; worktrees are not removed automatically.
- **Next step:** F2, EPW culture and missing-value correctness (writer invariant formatting and missing codes for DBT/RH/wind; importer invariant parsing and field-specific missing codes). It needs owner approval of its scope first. Suggested settings: Opus 5.5, Medium effort, this session, Build mode. Later: re-run the PR #239 box and HungaryHouse embedded-weather comparisons with a licensed TAS.

## Q4 Unified engineering standards consolidation closeout - 2026-10-16

- **PR/status:** [SAM-BIM/SAM#205](https://github.com/SAM-BIM/SAM/pull/205) complete, merged into `sow/2026-Q4` as `11afe033d3c6212c4a75b41611b73f9474be3b18`, pinned to approved head `04775336ef236622f2c3bd32854a567f18033ddb`. Owner approved the architectural review and the merge. Task branch `docs/unified-engineering-standards-q4` deleted after verification. Documentation only; no source, test, build or Grasshopper contract change.
- **Work:** new `docs/DIAGNOSTICS_STANDARD.md` (SAM.Core API as implemented, producer-owned severity, Warnings/Errors survive filtering, detail never affects engineering results, four adapter shapes from SAM#202, SAM_OCCT#78, SAM_OpenStudio#28, SAM_Tas#101, legacy compatibility, testing rules). `docs/GH_COMPONENT_STANDARDS.md` extended: GH-B7 execution triggers, GH-I3a Name vs NickName, section K new-versus-legacy quick reference, corrected status and Diagnostics-roadmap statements. `AGENTS.md` gained a mandatory "Engineering standards" section. Record: `documentation/Unified-Engineering-Standards-Q4.md`.
- **Unified Diagnostics roadmap cross-reference (complete):** Phase 1 SAM#201 (merge `293eb645`), Phase 2 SAM#202 (`fca3d4d4`), Phase 3 SAM_OCCT#78 (`de2744a7`), Phase 4A SAM_OpenStudio#28 (`f2235d45`), Phase 4B SAM_Tas#101 (`ba9c95c8`). Records and acceptance evidence are in each repository's `documentation/Diagnostics-Phase*-PR.md` and `PROJECT_PROGRESS.md`; not repeated here.
- **Checks:** PR head `04775336` build (Release), test (Release), spdx green; post-merge on `11afe033` Build (Windows) and Test green. The Python validator tests were not run (no Python on the authoring machine; Markdown-only change).
- **Decisions/open:** the `_diagnosticDetail_` spelling (GH-E3) and wiring the TAS adapters into TAS Grasshopper components remain owner decisions.
- **Next step:** separate PRs adding the AGENTS.md pointer to the standards in SAM_OCCT, SAM_OpenStudio, SAM_Tas, SAM_Tas_Grasshopper and SAM_Psychrometrics (Sonnet, medium, new session, Build mode).

## Q4 GH component standards and validator acceptance closeout - 2026-10-15

- **PR/status:** [SAM-BIM/SAM#204](https://github.com/SAM-BIM/SAM/pull/204) complete, merged into `sow/2026-Q4` as `a99083a73b107609d1fbc3b4a39266a9a05686f9` (base parent `6b12e2a65817556693ba408674211610f4a20dda`, pinned approved head `4a8afbebb186ace81eb5d49aed32deb502120781`). Owner explicitly authorized final acceptance and merging. No comments/reviews/review threads, required reviews or branch protection to bypass. Merge tree equals the accepted head exactly. Repair content `692f35f642db4838c29cda8e820101e2fc90401c`; final head adds only the PR record.
- **Completed/files:** canonical product standard `docs/GH_COMPONENT_STANDARDS.md`; shared bounded-source validator `design/grasshopper-icons/tools/param_contract.py`; 69 regression tests in `tools/tests/test_param_contract.py`; additive AGENTS.md guidance; `documentation/GH-Component-Standards-Q4.md` record. No production component, parameter rename, GUID, updater, Diagnostics implementation, workflow or unrelated repository change. No Auto-fix modifications. This closeout supersedes the historical draft/pending and failed-BuildAll status in the original PR record.
- **Correctness:** incomplete/unsupported extraction blocks compatibility PASS; qualified/closed-generic type identity retained; unknown ancestry/project discovery errors are fatal certification findings with paths; unsupported constructors/factories/control flow/Revit declarations remain explicitly partial or unresolved. Preserve the approved schema-less Psychrometrics baseline; schema 2 needs a reviewed refresh. Source marker intent never substitutes for runtime Optional/default/visibility evidence.
- **Fresh local acceptance at approved head:** 69 tests PASS, 0 skips (42.085s); original independent harness unchanged, 20 cases match expected exits, 0 unexpected acceptances; Psychrometrics approved baseline PASS (3 discovered/verified/compared). SAM inventory 460 discovered / 37 verified / 423 partial / 0 unresolved, 0 unclassified / 0 unparseable projects. Inventory is not compatibility validation; SAM still has no approved baseline.
- **Hosted final-head checks:** exact `4a8afbeb` Build [38788757010](https://github.com/SAM-BIM/SAM/actions/runs/38788757010), Test [38788762250](https://github.com/SAM-BIM/SAM/actions/runs/38788762250), SPDX [38788767956](https://github.com/SAM-BIM/SAM/actions/runs/38788767956) SUCCESS. Test log independently verified 3683 passed, 0 failed. These jobs do not run Python validator or Rhino migration acceptance.
- **Post-merge CI:** all complete and SUCCESS at immutable merge `a99083a7`: Build [38793868502](https://github.com/SAM-BIM/SAM/actions/runs/38793868502), Test [38793868569](https://github.com/SAM-BIM/SAM/actions/runs/38793868569) (3683/3683 passed), SPDX [38793919140](https://github.com/SAM-BIM/SAM/actions/runs/38793919140). SPDX dispatched explicitly because it has no push trigger. Completed Build log inspected for compiler/MSBuild errors; none found. This docs-only `[skip ci]` closeout follows successful validation under AGENTS.md.
- **Integration:** separate [SAM_Multitasker#12](https://github.com/SAM-BIM/SAM_Multitasker/pull/12) merged as `e58b00ba999a993fe86ebeadd7a30b40100dc9e8`, preserving Roslyn types with explicit aliases. The proven first breaking SAM change was #201, not #204. Latest preserved full `BuildAlls_v5.bat nopause` PASS, exit 0, `00:07:41.6286121`, 2026-10-15 21:07:51–21:15:36 Europe/Warsaw, all 26 repositories, Revit 2025/2026/2027 and final OCCT checks. Configuration includes this exact SAM PR head and Multitasker content `cfe9d116c1d9837e23ada308d535029eaf6de773`, unchanged by its merge. Verified full-log SHA256 `889e6260a8b59bef0d24bc8506b8734beb06eab1e745fe601335d259ace536bc` against committed Multitasker evidence `documentation/evidence/diagnostic-collision-2026-10-15/`. No new build failures; existing warnings remain. No new BuildAll run was needed for the identical merge tree.
- **Reproduction/evidence:** use Python `C:/Users/michal/.rhinocode/py313-rh9/python.exe -B`, writable TEMP/TMP, then `design/grasshopper-icons/tools/tests/test_param_contract.py`; `design/grasshopper-icons/tools/param_contract.py --repo ../SAM_Psychrometrics --check`; for SAM use inventory only, `--repo . --output <temporary-json>`. The committed tests reproduce critical cases; the supplementary unchanged independent harness and fresh logs/results are machine-local at `C:/Users/michal/.codex/visualizations/2026/10/15/01a140c3-07d7-7db2-83db-a3f98397ac92/pr204-final-acceptance/` (`independent_review.py`, `results.json`, `regression-tests.log`, `independent-cases.log`, `pilot-check.log`, `sam-inventory.json`, `post-merge-*.log/json`). Copy/provision these supplementary files/interpreter if needed elsewhere. Full BuildAll raw log remains in adjacent `multitasker-regression/BuildAlls_v5-after.log`. Reproducible setup/boundary/coverage details remain in the committed PR record and canonical standard.
- **Limits:** strict Python subset, not arbitrary C#/MSBuild semantic analysis or runtime saved-definition proof. Fleet inventory is 826 discovered / 73 verified / 709 partial / 44 unresolved across 22 named checkouts. Only Psychrometrics has an approved baseline. Those gaps remain deliberately uncertified; no attempt to eliminate them, add baselines or expand Diagnostics here. No fresh canvas/runtime acceptance is claimed.
- **Next:** this task is complete. After this closeout, remove only the fully merged feature branch under the expected-SHA lease and no-open-PR/no-worktree guards. Recommended later rollout candidate: SAM_GEM (one partial component); investigate its custom Goo constructor defaults and symbolic Tolerance.Distance before proposing any baseline. Suggested future investigation: GPT-6.1 Sol, High effort, new session, Plan mode, SAM-BIM workspace attaching SAM and SAM_GEM; bounded single-component review. No next repository work is started or authorized here.

## Q4 GH published-parameter rename migration (shared SAM infrastructure) closeout - 2026-10-15

- **PR**: **SAM-BIM/SAM#203** → `sow/2026-Q4`, **merged 2026-10-15**, merge commit `a1d605cfb14affc8f3cd5260537526aada5f620a` (parents `d6c3569286082b33ac8d76dc9ec4a57bfa345bc6` base and pinned head `9b171a6c1747d8b5a6aa1d22455dd90f1d4dcbf9`). Validated content `d32e70061ed9e97b9c42f12819bd53fe814416d5`; `691cc499` and `9b171a6c` are record-only docs commits. Owner authorised the merge; `sow/2026-Q4` has no branch protection or required reviews, no review comments, no conflicts.
- **Status**: complete. Shared mechanism only; the single controlled rename is in SAM_Psychrometrics#12, merged immediately after as `bfb3bfd8a6eca66be610b8e8bad172d129dea3c4`.
- **Hosted CI** (GitHub), all green: PR head build (Release) `38755020653`, test (Release) `38755020656`, spdx `38755020847`; post-merge on `a1d605cf` build (Windows) `38762414857`, test `38762414838`.
- **Work**: `GH_SAMComponent.ParameterRenames` (declared published renames, default `null`) with `IndexOfInputParameter`/`IndexOfOutputParameter` legacy-name fallback; `ParameterRename` and `ParameterRenameMap` (validation and unambiguous resolution; ambiguous or conflicting declarations are refused and reported, never guessed); alias-aware `Modify.RestoreConnections`, `Modify.CopyPersistentDataFromComponent` and `Query.MissingConnections`, with a parameter-type guard on any redirect; `Modify.ReconcileGroupMembership` moves `GH_Group.ObjectIDs` to the replacement; `Modify.DuplicateComponent` is transactional after `AddObject`.
- **Decisions**:
  - Instance identity is **not** adopted: `InstanceGuid` is read-only and the document keeps an identity-keyed object cache populated at `AddObject`. A reflection sweep of the loaded Rhino 8 Grasshopper assembly (`Grasshopper 8.35.26251.13001`) found `GH_Group.ObjectIDs` as the only persisted Guid collection naming document objects, so group membership is reconciled explicitly instead.
  - A component that declares no rename is unchanged (the default `ParameterRenames` is `null`); nothing renames on file open — a version bump makes a component obsolete and offers the explicit *Update component*.
  - Overloads were added only; the original `RestoreConnections` / `MissingConnections` / `CopyPersistentDataFromComponent` signatures are unchanged.
- **Files**: `Grasshopper/SAM.Core.Grasshopper/Classes/{ParameterRename.cs,ParameterRenameMap.cs,GH_SAMComponent.cs,ParamConnection.cs}`; `Modify/{CaptureConnections.cs,RestoreConnections.cs,CopyPersistentData.cs,CopyPersistentDataFromComponent.cs,UpdateComponent.cs,UpdateComponents.cs}`; `Query/MissingConnections.cs`; `Tests.Fixtures/TestComponents.cs`; `Tests/ParameterRenameMigrationTests.cs` (13 tests); `documentation/GH-Parameter-Rename-Migration-Q4.md`.
- **Tests (local, Windows, Rhino 8 `8.35.26251`, Release)**: `dotnet build SAM.sln` 0 errors; `dotnet test SAM/SAM.Tests` **3683 passed, 0 failed** on two full runs after one earlier non-reproducible one-off (name not captured; the documented `FLAKE_HUNT_2026-10-08` intermittent class, unrelated to this change); `dotnet test Grasshopper/SAM.Core.Grasshopper.Tests -p:SolutionDir=<repo>\` (headless Rhino 8) **84 passed, 0 failed** (71 existing + 13 new).
- **Independent acceptance review**: the RhinoCode Python 3.13 metadata contract checker passed (`3 components checked: OK`) and was proven live against a stale manifest (3 PROBLEM(S)); an alias-resolution probe confirmed duplicate legacy names are refused and well-formed declarations cannot cross-match. One LOW residual (duplicate canonical targets are not flagged) is unreachable for this pilot.
- **Risks / limitations**: a published rename is breaking for any consumer that looks the old name up outside the SAM updater (for example a C# script component) — recorded in the PR record; the *Get the latest component* right-click item creates a fresh component and is not the alias-aware path (pre-existing); `SAM.Core.Grasshopper.Tests` is not in `SAM.sln` (needs Rhino) so the hosted jobs do not run it; no interactive canvas check.
- **Cleanup**: `feature/gh-parameter-rename-migration-q4` deleted with an expected-SHA lease (`9b171a6c`) after both merges, plus the local branch; no other branch, worktree or frozen quarter touched.
- **Next step**: none for this stream. The remaining 22 Psychrometrics naming deviations stay frozen (see the SAM_Psychrometrics closeout).

## Q4 Unified Diagnostics, Phase 2 (BySolids integration) closeout - 2026-10-15

- **PR**: **SAM-BIM/SAM#202** -> `sow/2026-Q4`, **merged 2026-10-15**, merge commit `fca3d4d4a2e549f1c1d41710d5ed56f7c9f24d29`, pinned to the approved head `f0f87f419fc2fa47a0457182ef3ed67855a65c55` (validated content `37b86cc5`; `f0f87f41` adds only the PR record). Owner authorised the merge in chat after the Windows acceptance.
  - No review comments; no required reviews or branch protection bypassed. Task branch `feature/bysolids-diagnostics-phase2-q4` deleted (remote with lease at `f0f87f41`, and local).
- **Status**: complete. Phase 2 only; Phase 3 not started.
- **Hosted CI** (GitHub), all green: PR head `f0f87f41` build (Release), test (Release), spdx; post-merge on `fca3d4d4` Build (Windows), Test.
- **Work**: `PanelsBySolid`, `SnapToJunctions`, `AperturesBySolids` take a `Core.Diagnostics` (new overloads; legacy `out List<string>` overloads unchanged, delegating at Detailed); stable codes in `Analytical.BySolidsDiagnosticCode`; `Core.Query.TryGetDiagnosticDetail`; GH `CreatePanelsBySolids` 1.0.3 and `CreateAperturesBySolids` 1.0.2 gain the hidden optional `diagnosticDetail_` (0 Summary default, 1 Standard, 2 Detailed). Geometry, classification and engineering results unchanged.
- **Windows acceptance (local, 2026-10-15)** found and fixed three defects, none in engineering code (commit `37b86cc5`): (1) a `where T : GH_SAMComponent` constraint in the new GH test fixture broke NUnit discovery of the whole `SAM.Core.Grasshopper.Tests` assembly; (2) the behaviour tests ran in a non-metre headless document, so the components returned empty logs; (3) the invalid-level message was capitalised in `log` (`DiagnosticDetail_`), now `Input diagnosticDetail_ is 5, which is not ...`.
- **Decisions**:
  - Default Summary changes the default `log` content (per-solid Info lines move to level 1); Warnings are never dropped. Owner-flagged, accepted.
  - 28 vs 29 panels is not a regression: `minThickness_` 0.25 with `maxThicknessRatio_` 0.2 gives 28 (owner's run); with ratio 0.25 gives 29 (golden `house-min-0.25`). Same fixture, reproduced on the real component.
  - `PBS007` (solid gave no panel and no other Warning) is structured-only and filtered from the legacy list; no fixture triggers it.
- **Tests (local Windows)**: `dotnet build SAM.sln -c Release` 0 errors; `dotnet test SAM/SAM.Tests -c Release` **3683 passed, 0 failed**; `dotnet test Grasshopper/SAM.Core.Grasshopper.Tests -c Release -p:SolutionDir=<repo>\` (headless Rhino 8) **71 passed, 0 failed** (56 existing + 15 new). Headless Rhino 8 harness on a copy of the real `contemporary1/import-test.gh` (38 objects, 34 wires): update 1.0.1 to 1.0.3 and 1.0.0 to 1.0.2 keeps all 34 wires and values; levels 0, 1, 2, 5, -1 give identical panels, exclusions and apertures; SAMOCCT.CreateAdjacencyCluster receives identical panels. Harness is machine-local (Census.csproj mode `p2gh`), not committed.
- **Not run**: interactive canvas checks (the "update component" mark, the + zoom UI, a bake). Headless equivalent only.
- **Risks**: default `log` content change; GH test project build needs `-p:SolutionDir=<repo>\` and overwrites `%APPDATA%\SAM` and `Documents\SAM\resources`; `SAM_OCCT_NATIVE_FAILED` (status 40) is reported by the OCCT step on the house both before and after the change (independent of Phase 2, not investigated).
- **Record**: `documentation/Diagnostics-Phase2-BySolids-PR.md`.
- **Next step**: Phase 3 (OCCT, then OpenStudio/TAS adapters), each its own owner-reviewed PR; optionally the owner's canvas check of `import-test.gh`. Recommended: Opus, high effort, new session, Plan mode first (cross-repo).

## Q4 Unified Diagnostics, Phase 1 (SAM.Core) closeout - 2026-10-15

- **PR**: **SAM-BIM/SAM#201** -> `sow/2026-Q4`, **merged 2026-10-15**, merge commit `293eb64555a627f7acc5653a8d7e96640a9cec2d`, pinned to the approved head `d4dcaf695fdcaaea0a59d29c7da9ae8730f1c9b3` (validated content `967a8639`; `d4dcaf69` adds only the drop-on-add documentation: record text and an XML doc comment).
  - No branch protection or required reviews on `sow/2026-Q4`; none bypassed. No review comments. The owner approved the Phase 1 architecture and authorised the merge.
  - Task branch `feature/core-diagnostics-phase1-q4` deleted (remote with lease at `d4dcaf69`, and local).
- **Status**: complete. Phase 1 only; Phase 2 not started.
- **Hosted CI** (GitHub), all green: PR head `d4dcaf69` build (Release), test (Release), spdx; post-merge on `293eb645` Build (Windows), Test.
- **Work**: additive `SAM.Core` types `DiagnosticSeverity` (Info 0, Warning 1, Error 2), `DiagnosticDetail` (Summary 0, Standard 1, Detailed 2), immutable `Diagnostic` (Severity, Detail, Code, Subject, SourceIndex, Message, Value, Limit, Unit) and collection `Diagnostics` (Detail, Count, Add x2 incl. lazy `Func<string>`, Wants, Filter, HasErrors, Format).
- **Decisions**:
  - Runtime values, not `IJSAMObject`; independent of `Log`/`LogRecord`/`LogRecordType` (unchanged).
  - Drop-on-add: `Add` discards Info above the collection's `Detail`. `Filter(level)` can only narrow, so re-filtering at several levels requires collecting at `Detailed`. Warnings and Errors are never dropped. Documented in the record and the class XML doc.
  - `ToString`/`Format` reuse `Query.DiagnosticMessage`; Code and numeric values are structured only, not in the text.
  - No version bump (none declared in SAM.Core; additive API).
- **Files**: `SAM/SAM.Core/Enums/DiagnosticSeverity.cs`, `DiagnosticDetail.cs`; `SAM/SAM.Core/Classes/Diagnostics/Diagnostic.cs`, `Diagnostics.cs`; `SAM/SAM.Tests/DiagnosticsTests.cs`; `documentation/Diagnostics-Phase1-PR.md`. Diff vs base was 6 added files, none modified.
- **Tests**: local full `SAM.Tests` Release 3567 passed, 0 failed, 0 skipped on `967a8639`; `SAM.Core` rebuilt with 0 errors on `d4dcaf69`. Command: `dotnet test SAM/SAM.Tests/SAM.Tests.csproj`.
- **Risks**: none known; nothing consumes the types yet.
- **Record**: `documentation/Diagnostics-Phase1-PR.md`.
- **Next step**: Phase 2 (migrate `PanelsBySolid` / `AperturesBySolids` to `Diagnostics`, then Grasshopper detail parameter, then OCCT and OpenStudio/TAS adapters), each as its own owner-reviewed PR. Recommended: Opus, high effort, new session, Plan mode first (public Grasshopper surface and cross-repo adapters).

## Q4 PanelsBySolid thin elements below minThickness and Diagnostics convention closeout - 2026-10-15

- **PR**: **SAM-BIM/SAM#200** -> `sow/2026-Q4`, **merged 2026-10-15** (merge commit, pinned to the approved head).
  - Merge commit `750fa3364d015bf1b50eab025749cde0e7083715`; parents `0649e15e` (base) and head `e8f61bb3b8ab31dc94ce532381f1c780f924095a`.
  - Validated content `5f3a3ebe` (code and tests); `abaf8267`, `5547fa7b` and `e8f61bb3` change the record only.
  - No branch protection or required reviews on `sow/2026-Q4`; none bypassed. No review comments or Auto-fix commits. Draft marked ready immediately before merge; the owner authorised the merge.
  - Task branch `fix/panels-by-solid-thin-elements-q4` deleted (remote with lease at `e8f61bb3`, and local).
- **Status**: complete. Owner Rhino 8 / Grasshopper visual acceptance passed (`import-test.gh`; `minThickness_` 0.25 -> 28 panels, 11 solids skipped for the documented reasons).
- **Hosted CI** (GitHub), all green:
  - PR head `e8f61bb3`: build (Release), test (Release), spdx.
  - Post-merge on `750fa336`: Build (Windows), Test.
- **Work**:
  - **Root cause**: `ElementPlanes` dropped face pairs closer than `minThickness` before ranking, so a 0.15 m wall's ends or top/bottom became its "thickness" (up to 12 m) and the Warning advised raising `maxThicknessRatio_` up to 60. Following it (0.25 / 61) created 22 bogus panels.
  - **Fix**: pairs down to `min(minThickness, 10 × tolerance_Snap)` are ranked. A panel-like plane below `minThickness` claims its material and is reported as `below minThickness` at its real thickness. Panels are identical at every sensible setting; thresholds and defaults unchanged (0.08 m / 0.2).
  - **Diagnostics convention** (BySolids path only): `Severity: Subject: Message` via new `Core.Query.DiagnosticMessage` and `Core.Query.Plural`; one runtime Warning per skip reason.
  - **GH**: `CreatePanelsBySolids` 1.0.2 and `CreateAperturesBySolids` 1.0.1, descriptions only. GUIDs, parameter names/order/visibility, defaults and the `log` output name unchanged.
  - **API**: `PanelsBySolid` gains optional last parameter `int? sourceIndex = null` (source-compatible; compiled callers must rebuild; no caller outside SAM).
- **Tests**: local full `SAM.Tests` Release 3552 passed, 0 failed (3533 + 19 new) on `abaf8267`; clean rebuild of SAM.Tests and the Grasshopper project (with `SolutionDir`) 0 errors.
  - Acceptance ratio sweep (0.2–100 at `minThickness` 0.05 / 0.08 / 0.25, committed fixture `SAM/SAM.Tests/Golden/PanelsBySolid/contemporary1_solids.json`) vs base `0649e15e`: no internal-wall or slab panel above its real thickness at any ratio (base: 20 panels > 0.3 m at 0.25 / 61).
  - The extra panel at ratio 0.2 -> 0.25 is `UF_Gable_Cross_South` (0.3 m Wall, ratio 0.214); `PhysicalThickness` 0.3 m vs construction `SIM_EXT_SLD` 0.208 m.
  - Probe harness was machine-local (session scratchpad), not committed.
- **Limitations (pre-existing, identical on base)**:
  - `maxThicknessRatio` ≥ 0.4: `ST_Stair_GF_to_FF` becomes a 1.125 m Wall (≥ 1: also a 0.176 m Floor). The quoted keep-ratio "above 0.398" is not a safe setting.
  - ≥ 0.5 at `minThickness` 0.08 / 0.05: `GF_Wall_External` gives a bogus 0.15 m Floor and a 73 m² Air Panel and loses a Wall plane.
  - Keep `maxThicknessRatio_` ≤ 0.3. `maxThicknessRatio_` stays hidden. A stair of thin plates is indistinguishable from a wall by geometry.
  - Machine-local: GH project builds copy DLLs into `%APPDATA%\SAM`.
- **Record**: `documentation/PanelsBySolid-ThinElements-Q4.md`.
- **Next step**: none required for #200. Optional, owner decision: a Diagnostics detail-level design (`_diagnosticDetail_`, structured records shared with SAM_OCCT) was proposed in the session (design only, not implemented); also consider tightening the ratio Warning advice or a geometric stair test.

## Q4 PanelsBySolid multi-element solids and Vector3D recursion closeout - 2026-10-14

- **PRs** -> `sow/2026-Q4`, **merged 2026-10-14**, merge commits pinned to the approved heads, in this order:
  - **SAM-BIM/SAM#199**: merge commit `2133ef022dd01ee740faf7aa8da1faed159d4c43`, head `d701c675896e8903deb09790695e8f234ec9d9ba`.
  - **SAM-BIM/SAM#198**: merge commit `5896dfb5b2dd44491a6f29f79f36651911479bc4`, head `bb36906f9087c8645ad5a2417432746bac238e04`. This head is the validated content `6c0fd503` plus a merge of the updated base (#199) before merging.
  - `sow/2026-Q4` has no branch protection or required reviews; none bypassed. No review comments existed on either PR. Both were drafts, marked ready immediately before merge.
- **Status**: complete. The owner closed the task on 2026-10-14; no further development is planned.
- **Hosted CI** (GitHub), all green:
  - #199 head: build, test, spdx.
  - #198 synced head `bb36906f`: build, test, spdx.
  - Post-merge on `2133ef02`: build (Release), test (Release).
  - Post-merge on `5896dfb5`: build (Release), test (Release).
- **#198 work**: `Create.PanelsBySolid` decomposes each solid into element planes (public API unchanged).
  - **Why**: wall rings, two-pitch roofs and L-walls previously gave one panel per solid. On the contemporary test house (`SAM_daily/2026-10-13-House/contemporary1`), 17 panels covered 55.5% of the paired faces.
  - **Pairs**: opposite faces with material between them; `PhysicalThickness` is the measured face distance, independent of `ConstructionThickness`.
  - **Ownership**: planes are ranked by thickness over the band width of their overlap.
  - **Nested slabs**: window niches and stepped walls are one element, reported as variable thickness.
  - **Orientation**: element types no longer depend on face order.
  - **Snap**: `Query.SnapToJunctions` checks junctions along the whole edge.
  - **GH**: `SAMAnalytical.CreatePanelsBySolids` is 1.0.1 and warns whenever a solid is skipped as not panel-like (the log gives the ratio that would keep it).
- **#199 work**: `implicit operator Point3D(Vector3D)` returned a `Vector3D` and recursed until the stack overflowed. It now returns a `Point3D`.
- **Owner decisions**:
  - `maxThicknessRatio` default stays **0.20** (backward compatible). Users set 0.25 or another value in Grasshopper; the contemporary house needs 0.25 for its 6 × 1.4 m gable (ratio 0.214).
  - No automatic geometry completion, synthetic boundaries, room separation or extra Air generation.
  - Stairs, treads and balustrades stay excluded (the contemporary stair `d8182294-90be-4fe7-bbf8-e898ffeb4344`, ratio 0.398).
  - The contemporary model is intentionally incomplete and will be completed manually in Rhino. The original `.3dm` and `.gh` are unchanged (SHA-256 verified).
- **Tests**:
  - New: `PanelsBySolidMultiElementTests` (20) and `PanelsBySolidComplexHouseTests` (18), using the fixture `SAM/SAM.Tests/Golden/PanelsBySolid/contemporary1_solids.json`; and `VectorPointConversionTests` (4).
  - Local full SAM.Tests run on the synced #198 head: **3533 passed, 0 failed**.
  - Coverage of element faces on the contemporary house at ratio 0.25: 55.5% → 97.2%, with 39 panels. At the 0.20 default: 38 panels and a Warning naming the stair and the gable.
  - The accepted 14 Oct simple house still gives 11 spaces / 56 panels / 1 Air (machine-local Rhino 9 WIP harness, not committed).
- **Records**: `documentation/PanelsBySolid-MultiElement-Q4.md` and `documentation/Vector3DPoint3DRecursion-Q4.md`.
- **Known limitations** (accepted by the owner, no tasks opened):
  - The 0.20 proportion rejects valid thick or low walls; the user raises `maxThicknessRatio_`.
  - Unclaimed notches (doorways, slab voids at an edge) stay open; only unclaimed holes become Air.
  - Snap only translates edges.
  - The contemporary house does not form closed spaces until its envelope openings, attic floor and room separations are modelled.
  - Manual Grasshopper acceptance was not run by the agent.
- **Cleanup**: remote task branches deleted with expected-SHA leases after verifying the head, full merge and no open PRs: `fix/vector3d-point3d-recursion-q4` (`d701c675`) and `feature/panels-by-solid-multi-element-q4` (`bb36906f`). Their local branches were deleted, and the checkout returned to `sow/2026-Q4`.
- **Next step**: none for this stream. The owner may run the Grasshopper acceptance in the record after deploying SAM from `sow/2026-Q4`.

## Q4 Rhino Text Dot support for analytical Spaces closeout - 2026-10-14

- **PR**: SAM-BIM/SAM#197 -> `sow/2026-Q4`, **merged 2026-10-14**, merge commit `32fe872feee4cfa22900ec4c06db249c948ba144`, approved head `cb831d43f475761bcfd92d4f03513c717553ee52` (validated content `ab8a12d3`; `cb831d43` is the record-only docs commit that added the PR link and that SHA). `sow/2026-Q4` has no branch protection or required reviews; none bypassed. No review comments.
- **Status**: complete. PR-head CI (build Release, test Release, spdx) green on `cb831d43`; post-merge CI on `32fe872f`: build (Release) and test (Release) succeeded (GitHub).
- **Why**: `SAMAnalytical.CreateSpace` 1.0.2 failed with `Data conversion failed from Model Text Dot to ISAMGeometry` when a Rhino Text Dot reached it through `Query Model Objects > Model Object`. The Rhino 8 `Grasshopper.Rhinoceros.Model.ModelObject` is an `IGH_Goo` but not a `GH_GeometricGoo` and exposes no public `Value`; `GooSAMGeometry.CastFrom` handled neither it nor a `Rhino.Geometry.TextDot`.
- **Work**: new `SAM.Geometry.Grasshopper.Query.TryGetTextDot` resolves a Text Dot from a `TextDot`, `GH_TextDot`, `RhinoObject`/`ObjRef`, or any goo whose `IGH_Goo.CastTo<TextDot>` succeeds (the Model Object). `GooSpace.CastFrom` builds a `Space` from it (text = name, blank = `Cell`; position = location) so the Text Dot is not routed through `ISAMGeometry`; `GooSAMGeometry.CastFrom` maps a Text Dot to its location `Point3D` (fixes the reported error on `locationPoint_`); `GooSpaceParam` selects `ObjectType.Point | ObjectType.TextDot` and skips unexpected objects; `SAMAnalytical.CreateSpace` is 1.0.3 and warns (does not scale) in a non-metre document via `Geometry.Rhino.Query.ModelUnitsWarning`. No Rhino dependency added to `SAM.Core`/`SAM.Analytical`; no existing input, GUID or default changed. `SAM_Tas_Grasshopper` and other `GooSpaceParam` consumers inherit the behaviour with no change.
- **Files changed**: `Grasshopper/SAM.Geometry.Grasshopper/Query/TryGetTextDot.cs` (new) and `Classes/GooSAMGeometry.cs`; `Grasshopper/SAM.Analytical.Grasshopper/Classes/GooSpace.cs` and `Component/SAMAnalyticalCreateSpace.cs`; tests `Grasshopper/SAM.Core.Grasshopper.Tests/TextDotSpaceTests.cs` (new), `SpaceTextDotTests.cs` (new), `SAM.Core.Grasshopper.Tests.csproj`, `GrasshopperTestSetup.cs`; record `documentation/SpaceTextDot-Q4.md`.
- **Validation**: full `SAM.sln` Release build 0 errors; `SAM.Core.Grasshopper.Tests` (headless Rhino 8 + Grasshopper via Rhino.Testing, run locally - not part of `SAM.sln`/CI) 56/56, including 11 new tests: a real `Grasshopper.Rhinoceros.Model.ModelObject` from a headless `RhinoDoc` Text Dot yields the expected `Space` name/location and `Point3D`; blank text -> `Cell`; a SAM `ISpace` is unchanged. Hosted checks (GitHub) as above.
- **Manual acceptance**: PASSED on 2026-10-14 (owner-run, machine-local, not committed): both Rhino/Grasshopper workflows - (1) Rhino Text Dot -> `Space` parameter -> SAM Analytical Space; (2) Rhino Text Dot -> Query Model Objects -> Model Object -> `SAMAnalytical.CreateSpace` 1.0.3 - names and locations correct, the original conversion error resolved.
- **Unresolved / risks**: none introduced. The interactive check is owner-run only (the agent did not run the UI). A new Warning appears for existing non-metre `CreateSpace` usage (warning only, no function change). No workspace file was promoted to `master`.
- **Next step**: none required for this stream.

## Q4 About enum member rename closeout - 2026-10-14

- **PR**: SAM-BIM/SAM#196 -> `sow/2026-Q4`, **merged 2026-10-14**, merge commit `555d4891b9367d9c2f5d7d63796d9032e809ba18`, approved head `7aef6d6bbb673bdc5a066d820f33f1735194d605`.
- **Status**: complete. PR-head CI (build Release, test Release, spdx) green; post-merge CI on `555d4891`: build (Release) and test (Release) succeeded (GitHub).
- **Why**: the Grasshopper `SAM.About` right-click menu lists enum member names, so #195 still showed "HoareLea". `AboutInfoType.HoareLea` renamed `SAMBIM` (value 0 unchanged). `GH_SAMEnumComponent` saves the integer value, so existing definitions are unaffected; no other references in the SAM-BIM repositories. Corrects the #195 entry, which said the member was kept.
- **Validation**: `SAM.Core.Grasshopper` MSBuild compile, 0 errors (local). SAM_UI About reported fixed by the owner; the Grasshopper menu after this rename is not yet re-checked.
- **Cleanup**: remote and local `fix/about-info-type-sambim` deleted after confirming merge and no open PR.
- **Next step**: owner re-checks the `SAM.About` menu in Rhino after the next SAM build.

## Q4 About branding closeout - 2026-10-14

- **PR**: SAM-BIM/SAM#195 -> `sow/2026-Q4`, **merged 2026-10-14**, merge commit `783d31a2a5ae160d818aba916f4e65cec0532e5f`, approved head `6c5df98afac99909593291275130bd5df0d70cb7`.
- **Status**: complete. PR-head CI (build Release, test Release, spdx) green on `6c5df98a`; post-merge CI on `783d31a2`: build (Release) and test (Release) succeeded (GitHub).
- **Work**: the shared About text (`SAM.Core` `Query/AboutInfoTypeText.cs`, `Enums/AboutInfoType.cs`; used by the Grasshopper `SAM.About` component and the SAM_UI About button) and the author/contact fields of six Grasshopper `Kernel/AssemblyInfo.cs` files now present SAM-BIM, https://sambim.xyz and https://github.com/SAM-BIM. `SAM.Core.Grasshopper` icon `HL_Logo24` -> `SAM_Small`. Record: `documentation/AboutBranding-Q4.md`.
- **Decisions**: enum member `AboutInfoType.HoareLea` was kept in #195 but renamed `SAMBIM` in #196 (see below); the Hoare Lea origin remains as a historical credit; `HoareLea/ScriptsHydra` URLs in `ExportHydra.cs` and unused `HL_Logo24` resources retained. No GUID, assembly, namespace or component identity changed. SAM_UI needed no change (it reads the SAM.Core text).
- **Remote separation**: local `upstream` remotes pointing at HoareLea removed from SAM and SAM_UI (local config, no commit); `sow/2026-Q4` tracking fixed to `origin/sow/2026-Q4` in SAM_Acoustic, SAM_Origin, SAM_Topologic. Two empty stray files in SAM_OCCT moved out of the repository (not part of any development).
- **Validation (local)**: MSBuild compile of the six Grasshopper projects: 0 compile errors. Analytical post-build copy fails only when the csproj is built standalone (`$(SolutionDir)` undefined); not a compile error. `BuildAlls_v5.bat pull sow/2026-Q4 nopause` on clean checkouts: all 26 repositories pulled fast-forward from SAM-BIM origins, build exit 0, 0 `: error` lines, OCCT runtime verified (machine-local, log not committed).
- **Cleanup**: remote `docs/sam-bim-about-branding` deleted with an expected-SHA lease on `6c5df98a` after confirming it is merged and no open PR uses it; local branch deleted.
- **Unresolved**: manual runtime checks not performed - Rhino/Grasshopper `SAM.About` right-click text, plugin properties (author, contact, icon), opening an old `.gh` with `SAM.About`; SAM_UI About button. `gh pr create` triggers a Tetra Tech SSO prompt via fork discovery; use `gh api repos/SAM-BIM/<repo>/pulls` instead.
- **Next step**: owner runs the manual About checks in Rhino and SAM_UI; no further code work required.

## Q4 PanelsBySolid / AperturesBySolids closeout - 2026-10-14

- **PR**: SAM-BIM/SAM#194 -> `sow/2026-Q4`, **merged 2026-10-14**, merge commit `755bc51479e114e5b99179fb04d17b8b3ffcc05a`, approved head `f84a2ef6718f7e7565dcd7b3cb3643936b1f649d`.
- **Status**: complete. Post-merge CI on `755bc514`: Build (Windows) and Test succeeded (GitHub). PR-head CI (build Release, spdx, test Release) was green on `f84a2ef6` before merge.
- **Work**: thin solids converted to mid-surface panels with Air openings (`Create.PanelsBySolid`), apertures from window/door solids (`Create.AperturesBySolids`, `ApertureSolid`), source-parameter enums, `Query.SnapToJunctions`, `Modify.AddApertures` traceability, physical thickness kept through hosting, Grasshopper components `SAMAnalyticalCreatePanelsBySolids` / `SAMAnalyticalCreateAperturesBySolids` and `ModelUnitsWarning`. Tests: `PanelsBySolidTests`, `AperturesBySolidsTests`. Record: `documentation/PanelsBySolid-Q4.md`.
- Manual Rhino/Grasshopper/TAS acceptance PASSED on 2026-10-14 (owner-run, machine-local, not committed): 11 correctly named TAS zones, 57 surfaces, 25/25 apertures matched, 50 aperture parts rebound, UseBEWidths ON volume 481.784 m³, no TAS validation errors, no unnecessary OpeningProperties warnings, PhysicalThickness preserved independently of TBD constructions.
- Merge order (sequential, merge commits, each pinned to its verified head SHA): SAM#194 `755bc514` → SAM_OCCT#76 `79d0332e` → SAM_OCCT#77 `3e688afc` → SAM_Tas#100 `4b253dc7`. `sow/2026-Q4` has no branch protection or required reviews; none bypassed. No review or codex comments existed on any of the four PRs. PRs were drafts and were marked ready immediately before merge.
- Dependency order: SAM_OCCT#77 and SAM_Tas#100 rely on this content; SAM_Tas#100 CI resolved SAM by the same branch name, whose head equalled the content merged here.
- Remote task branches deleted with lease guards after verifying head SHA, full merge and no open PRs: SAM `feature/panels-by-solid-q4`, SAM_OCCT `fix/sew-under-split-q4` and `feature/air-panels-attic-q4`, SAM_Tas `feature/panels-by-solid-q4`. Local branches left in place (upstream gone).
- Unresolved (non-blocking): the informational wording "SAM panels are the physical inner surfaces" (SAM_Tas `WorkflowCalculator.cs` UseWidths note and `DIRECT_T3D_ROUTE.md`) is misleading for mid-surface panels; deliberately not changed during the merge. Small follow-up PR. The 19 pre-existing SAM_OCCT native integration failures remain documented and were not reinvestigated.
- **Next step**: none required for this stream. Optional small follow-up PR for the wording above.

## Q4 agent instructions rollout closeout - 2026-10-12

- SAM-BIM/SAM#193 merged as `654147b95fb5d98bcb108da9a0797859c62b9c9d` (merge commit pinned to approved head `cb3a1565f9137ef66cfdc10c94e5ca6031d3bb01`; base `sow/2026-Q4` `3514e9c4e3a038d817a57def75dc4523a35a87c3`; validated content `3a18f2ebc8c55d03ce70b4fd0b309f7de02140ce`). Owner-authorized 2026-10-12 after an independent review (`AGENT_ROLLOUT_REVIEW_2026-10-12.md`, local to the owner's workspace, not committed).
- Docs only: AGENTS.md, plus the rollout record `documentation/Agent-Continuity-Rollout-Q4.md` (which also holds the cross-repository status table). No product code, build, workflow, dependency pin or tool changed; this closeout is the only `PROJECT_PROGRESS.md` change.
- Validation: local documentation checks and commands are in `documentation/Agent-Continuity-Rollout-Q4.md` (`git diff --check`, file scope, UTF-8/LF/no BOM, import). Hosted checks at the approved head: build (Release) success (run 38258305628); spdx success (run 38258305626); test (Release) success (run 38258305587). Dependency selection: the build log shows no sibling dependency clones.
- Cleanup: remote `docs/agent-instructions-rollout-q4` deleted with an expected-SHA lease on `cb3a1565f9137ef66cfdc10c94e5ca6031d3bb01` after confirming it is contained in `sow/2026-Q4` and no open PR uses it as head or base; the local branch and its worktree are preserved (worktree in use). Other branches, worktrees and the frozen quarters were not touched.
- Remaining gaps: no agent-client behaviour or token benchmark has been run.
- Exact next step: Continue from the shared `AGENTS.md`. Open item carried from the pilots: verify on each installed agent client, in a fresh Q4 session, that it follows the instructions without loading the full progress history, and record the result.

## Q4 Tas result units correction closeout - 2026-10-12

- SAM-BIM/SAM#192 merged as `be9c00cadb237cdc5a64351dce90dd7731be438c`, approved head `9268ccb736376b0f1f013cf538e95219e30ca01c` (validated content head `b28daa77`; later commits changed only the PR record). Owner-approved; merged first. Adds the `ResultUnitsVersion` parameter, the reader helpers (`Query.TryGetConsumptionOrPeakLoad`, `Modify.SetResultUnitsVersion`) and the DesignExplorer correction (also fixes OpenStudio's existing double conversion there).
- Coordinated set (option A of SAM_Tas#92, owner-approved 2026-10-12): SAM#192 -> `be9c00cadb237cdc5a64351dce90dd7731be438c` (head `9268ccb7`), SAM_Tas_Grasshopper#13 -> `8054867fc317135e30792defe9796addfd12cef2` (head `d57b150c`), SAM_OpenStudio#26 -> `e0f9d17cc392a7217694d3ed8805c187c9d4a58b` (head `310bcffc`), SAM_Tas#98 -> `92bfad33c9812d0be4190fc18dfc1bb18d63adaa` (head `72c4f3fe`). Merge commits, each pinned to its approved full head SHA, in the approved order SAM -> Grasshopper/OpenStudio -> SAM_Tas the same day.
- Validation: SAM.Tests 3457/3457 (43 new), mutation 7/7 killed, SAM_Validation 77/77 and 87/87 unchanged (recorded in the PR record). Merged-Q4 CI green: Build `38249007619`, Test `38249007598` at `be9c00ca`. PR checks build/test/spdx were green on the exact head before merge; no review findings.
- Dependency CI: each downstream PR CI was rerun after SAM#192 merged and its task branch was deleted; the logs show `Cloning SAM-BIM/SAM @ sow/2026-Q4` (Grasshopper run `38247874467` attempt 2, OpenStudio run `38247876785` attempt 2, SAM_Tas run `38247884982` attempt 3, all success on the unchanged approved heads).
- Compatibility: results carry `ResultUnitsVersion = 1` (consumption kWh, peaks kW). Unmarked results are read by exact recorded `Source` (`SAM.Analytical.Tas` = legacy Wh/W, divided once; `SAM.Analytical.OpenStudio` = kWh/kW); any other producer or marker gives unknown units, which DesignExplorer shows as empty cells. Persisted results are not migrated. Limitation: generic parameter views and external scripts reading the raw parameter see Wh/W on old unmarked Tas results and kWh/kW on new ones; scripts that applied their own /1000 to Tas values must stop doing so for newly corrected results. Details: SAM `documentation/TasResultUnits-PR.md`.
- Cleanup: remote task branch `fix/tas-result-units-kwh` deleted with a lease on the approved head after confirming it was in `sow/2026-Q4` and no open PR used it; local branch deleted and the checkout returned to `sow/2026-Q4` (clean, equal to origin). No other branch touched.
- Remaining: none for the units correction. Next: PR10 packaging and installed-build validation (SAM_Deploy); not started. No deploy, publish or release was made.

## Q4 agent continuity pilot closeout — 2026-10-12

- SAM-BIM/SAM#191 merged as `02bbafb8db289ac2bd3fc3a35c54cff86e9eadbb`, reviewed head `0426d7854be0ee67956f352164973a7e2fd8ac4e` (validated content head `4599115091b63186dfe3f4263ba23cbebd0a80ec`). Merge commit; branch preserved. Owner-approved; ZiolkowskiJakub's review is required only for merges to `master`. Docs only: `AGENTS.md` (task-specific progress reads, SAM-BIM-only operations, quarter transition with release-promotion closeout on the outgoing `sow` branch, handoff evidence rules), new `CLAUDE.md` (`@AGENTS.md` import), record `documentation/Agent-Continuity-Pilot-Q4.md`.
- Validation: local Linux cloud run of the record's Bash block passes at the content and final heads (only the three files changed; `PROJECT_PROGRESS.md` untouched on the PR; footprint measurement `53491 2699`). GitHub `build (Release)` and `test (Release)` (windows-2022) and `spdx` green on the exact head: runs `38245363526`, `38245363686`, `38245363968`. The PowerShell block was not run on Windows; no agent-client token benchmark. The Copilot re-review failed (HTTP 402 quota); Claude acted as reviewer at the owner's direction.
- Owner-authorized layout correction in this commit: seven Q4 closeouts (SAM#184–#190, 2026-10-07/08) moved verbatim from beneath the frozen Q3 heading to above it. The PR record says six; it missed SAM#184. The bootstrap `## Current status` is labelled superseded. No entry text rewritten.
- Remaining: SAM_OCCT#74 (independent per owner) is reviewed and merged on its own. Next: on each installed agent client, start a fresh Q4 session and verify that it follows `AGENTS.md` without reading full history, recording tokens and handoff correctness before any wider rollout. Quarter-transition steps happen only on owner instruction.

## Q4 Grasshopper icon migration closeout — 2026-10-11

- SAM-BIM/SAM#179 merged as `6af268b588b32892c5c41bb8b449cd61db65532e`, reviewed head `d2c7f729534985c5a58743d068c8e0de84e8b384`. Merge commit; branches preserved. Frozen approved design applied to 522 concrete objects; 521 resource-token swaps plus documented base-class Icon override. APIs, GUIDs, namespaces and behaviour preserved.
- Files: Grasshopper icon getters/resources, `design/grasshopper-icons`, icon validation records. Fresh verification: 0 unrelated production C# changes, 522 resource references, 0 errors; all six assemblies' embedded icons pass. Recorded exact-head Release build and full suite: 3414 passed / 0 failed / 0 skipped. No fresh Rhino-hosted smoke claimed.
- Maintainer cleared GitHub's fork Actions suspension in the UI; no branch rules/token privileges changed. Fresh exact-head build/test/SPDX runs `38139444492`, `38139446325`, `38139448084` all succeeded before merge.
- Remaining: none for SAM#179; sibling OpenStudio/LadybugTools workstreams continue independently. Next: owner visual review in installed Rhino/Grasshopper; no installer/submodule repin included. Docs-only Q4 `[skip ci]` closeout after known merge.

## Branch

`sow/2026-Q4` - bootstrapped 2026-10-06 from `master` `56a70592`. Frozen Q3 record: `sow/2026-Q3` @ `3fcd64bf` (not modified).

## Last updated

2026-10-08 (native Optimisation PR6b "try every option" closeout, SAM#190; default aperture library `SIM_EXT_GLZ` Guid fix closeout, SAM#189; native Optimisation PR6 model bindings closeout, SAM#188; native Optimisation PR3 `SAM.Core.Optimisation` closeout, SAM#187; native Optimisation PR2 energy/mass units and display formatting closeout, SAM#186; Java-free GenOpt PR6 comment-only closeout, SAM#185). Earlier: 2026-10-07 (Part O stable semantic design key closeout, SAM#184).

## Current status

Superseded bootstrap status (2026-10-06), kept verbatim: this section through `## Next step` describes the bootstrap state. Current state is in the dated closeout sections.

Q4 branch cut from `master` `56a70592`, which is the exact commit pinned in SAM_Deploy's frozen Q3 baseline (`v20261006.1`). Bootstrap added only internal docs (this file, `AGENTS.md`). No product source changed. No Q4 product work has started.

## Q4 priorities

Not yet set by the owner. Record them here at the first Q4 planning pass. Known carry-over work is listed below.

## Known carry-over work

- **SAM Grasshopper icon redesign - PR #166** (`feature/sam-gh-icon-redesign` @ `cf4d924a`, open, base `sow/2026-Q3`, not merged). Analysed 2026-10-06: the branch carries only its own 6 icon-only commits (`f57b54a`, `a7d65b0`, `55745b8`, `7300a7d`, `be024ba`, `cf4d924`) on top of Q3 commit `bc85ba61`. Those commits are not reachable from `sow/2026-Q4` (Q4 is built on the promoted `master` line), so a plain retarget would list 495 commits. Replaying exactly those commits onto `sow/2026-Q4` @ `e29cb86e` is conflict-free (verified commit-by-commit with `git merge-tree`; identical to the net-diff merge). Planned action: rebase-onto Q4 as a new branch + PR, then close this one; owner-approved controlled task, not yet executed. **Update:** migrated; replacement Q4 PR SAM#179 (see the Q4 icon-redesign migration section); this old PR stays open for now.
- Branch `codex/part-o-cooling-control-room` - Q3 complete: all commits already in sow/2026-Q3.
- Branch `docs/parto-regression-run-2026-09-23` - owner decision: 2 commits not in Q3 (docs, last 2026-09-24).

## Repository-specific next steps

- Await Q4 planning. Open PRs for Q4 work against `sow/2026-Q4`.
- Follow the continuity convention in `AGENTS.md` for every PR and closeout.

## Decisions / assumptions

- Q4 base is `master` `56a70592`; the internal files were recovered from `sow/2026-Q3` into this branch only, never onto `master`.
- Q4 history intentionally does not contain the Q3 branch history (the maintained `master` is the promoted Q3 line, which is not a descendant of `sow/2026-Q3`); the frozen `sow/2026-Q3` branch is the permanent record.
- Historical Q2/Q3 content below is kept as evidence; its branch names, SHAs and next steps describe Q3 and are not current instructions.

## Validation

- Bootstrap verified 2026-10-06: `sow/2026-Q4` was created at exactly `56a70592` and the push was a normal (non-forced) branch creation.

## Issues / blockers

- None at bootstrap.

## Next step

- Owner to set Q4 priorities; then start the first Q4 task from this branch.

## Q4 operational cleanup (2026-10-06)

- Reviewed every active Q2/Q3 reference in this repository on `sow/2026-Q4` (workflow branch filters, dependency-branch resolution, `.gitmodules`/validation, docs). Historical Q2/Q3 mentions (feature documentation records, the frozen Q3 section below) are intentionally unchanged.
- No change needed: this repository has no active Q2/Q3 operational reference.
- Checked, no action: the `github.repository_owner == 'SAM-BIM'` build guard (intentional; its comment names HoareLea only to explain why the guard exists), CODEOWNERS (SAM-BIM owners), and workflow secrets (no HoareLea-named secret). The local `upstream` (HoareLea) remote is preserved.
- Carry-over: **SAM Grasshopper icon redesign - PR #166** (`feature/sam-gh-icon-redesign` @ `cf4d924a`, open, base `sow/2026-Q3`, not merged). Analysed 2026-10-06: the branch carries only its own 6 icon-only commits (`f57b54a`, `a7d65b0`, `55745b8`, `7300a7d`, `be024ba`, `cf4d924`) on top of Q3 commit `bc85ba61`. Those commits are not reachable from `sow/2026-Q4` (Q4 is built on the promoted `master` line), so a plain retarget would list 495 commits. Replaying exactly those commits onto `sow/2026-Q4` @ `e29cb86e` is conflict-free (verified commit-by-commit with `git merge-tree`; identical to the net-diff merge). Planned action: rebase-onto Q4 as a new branch + PR, then close this one; owner-approved controlled task, not yet executed.
- Full cross-repository record, migration table and owner decisions: `SAM_Deploy:sow/2026-Q4` `PROJECT_PROGRESS.md`.

## Q4 icon-redesign migration (2026-10-06)

- Old PR: SAM-BIM/SAM#166 (`feature/sam-gh-icon-redesign` @ `cf4d924a`, base `sow/2026-Q3`) - **preserved, open, untouched**.
- New branch `feature/sam-gh-icon-redesign-q4` cut from `sow/2026-Q4` @ `5f57055d`; new PR **SAM-BIM/SAM#179** (base `sow/2026-Q4`), replay-only tip `cbf392f8`, feature head `1646852b`. **Not merged.**
- Replayed (old -> new, `cherry-pick -x`): `f57b54a`->`3412211`, `a7d65b0`->`4041941`, `55745b8`->`966421e`, `7300a7d`->`345dd7c`, `be024ba`->`afb3aba`, `cf4d924`->`cbf392f`; plus one new docs commit `1646852` pointing `documentation/GH-IconRedesign-PR1.md` at the new PR. No Q3 history imported.
- High-scrutiny review: `Grasshopper/` is byte-identical at the Q3 base, the Q3 tip and Q4 (`fe6cdeb4`), so no file touched by the PR changed on Q3/Q4 after it was cut. The 1849 files are 521 one-token `Icon` swaps, additions-only `.resx`/`Resources.Designer.cs` (6 each), 354 resource PNGs (copies of the 337 kit PNGs), the `design/grasshopper-icons` kit and the PR record. The one non-swap line is the `Icon` override added to `A_SAMAnalytical` (documented in the PR record).
- Verified at the replay-only tip, before push: tree identical to the net-diff merge of the old feature onto Q4 (`c40b597c`); same aggregate and per-commit `git patch-id`, file set, numstat and blobs (all PNG/resx/designer) as the old PR; ComponentGuid lines 533 unchanged; no EOL/BOM churn; no workflow/solution/project/`.gitmodules`/`AGENTS.md`/`PROJECT_PROGRESS.md` change; `git diff --check` output identical to the old PR (inherited, not cleaned).
- Validation: SAM_UI kit `check_source.py origin/sow/2026-Q4` (SAM's own kit predates it): 521 swaps, ComponentGuids unchanged, only flag the documented `A_SAMAnalytical` line; Release `msbuild SAM.sln` (APPDATA/USERPROFILE redirected) 0 errors; `check_assemblies.py` OK; PR CI `build`, `test`, `spdx` green; mergeable. Not run: Rhino-hosted `SAM.Core.Grasshopper.Tests` (Rhino does not start under the redirected profile; not in CI).
- Next: owner decides whether/when to close the old PR; merge remains the maintainer's call.

## Q4 runtime-URL cleanup (2026-10-06)

- **Status:** complete. SAM-BIM/SAM#180 merged into `sow/2026-Q4` as merge commit `7d1c1c75bd67730d52faa610abd8d6721366bdc0` (PR head `dc540d5ff1c17545d54af19ed2c1881f3afbef44`, Q4 base `9b11dd26`); merge method: merge commit (repository convention). Remote and local `fix/sam-bim-runtime-urls-q4` removed.
- **Work completed:** The update check (`LatestVersion`) now reads `api.github.com/repos/SAM-BIM/SAM_Deploy/releases/latest` instead of `HoareLea/SAM_Deploy` (HoareLea latest was `v20260630.1`, so every current install `v20261006.1` was told an update was available); the model description "Delivered by SAM" text and the two "source code" menu actions now name `https://github.com/SAM-BIM/SAM`. SAM-BIM is the authoritative ecosystem; HoareLea is no longer the synchronised operational source. Record: the PR's `SAM-BIM-RuntimeUrls-Q4.md` document.
- **Decisions / owner classifications:** `Modify/ExportHydra.cs` clones the private `HoareLea/ScriptsHydra`: KEEP - intentional external/private dependency (no SAM-BIM equivalent; none to be invented). Assembly author/contact strings (`Hoare Lea`, `@hoarelea.com` in `Kernel/AssemblyInfo.cs`) are provenance/metadata, not repository ownership: KEEP unchanged. The unused `AboutInfoType.HoareLea` enum/text is KEEP (dead). These are owner decisions and not baseline blockers.
- **Files changed:** `SAM/SAM.Core/Query/LatestVersion.cs`, `SAMAnalyticalCreateAnalyticalModelByAdjacencyCluster.cs`, `SAMAnalyticalCreateAnalyticalModelBySpaces.cs`, `SAMCoreFilterByType.cs`, `SAMCoreInspect.cs`, `documentation/SAM-BIM-RuntimeUrls-Q4.md` (5 product lines, one token each).
- **Validation:** `msbuild SAM.sln -p:Configuration=Release` (APPDATA/USERPROFILE redirected): 0 errors; `SAM.Core.dll` contains the new endpoint and not the old. `SAM.Tests`: second full run 2819/2819 passed (first run: 1 intermittent failure from the PartFData race below). PR CI build, test, spdx green.
- **Unresolved issues, risks:** Pre-existing intermittent race in `PartFData.GetPartFCategory` (shared dictionary written from several threads; `PartODwellingStrategyMaterialisationTests.SystemsScope_ScaffoldingIsTheAddMechanicalSystemsShape` failed once): a separate product bug that will get its own PR, not fixed here.
- **Next step:** Separate PR for the PartF concurrency defect; the 18 deferred icon PRs remain open and untouched. **Update:** the PartFData race is fixed by SAM#181 (see the Q4 Part F concurrency section).

## Q4 Part F classification concurrency (2026-10-07)

- **Status:** complete, closed. SAM-BIM/SAM#181 merged into `sow/2026-Q4` as merge commit `3a501cfe9b7061fe7bf6e940ca72620fc0cd5b7b` (PR head `bcbd0c7c1254c9312d3eb5047ae042f5f44cbd44`, Q4 base `fe1d60b5`); merge method: merge commit, head-commit protected. Remote and local `fix/partfdata-thread-safe-cache-q4` removed. Record: `documentation/PartFData-ThreadSafeCache-Q4.md`.
- **Work completed:** Fixed all three unsynchronised shared collections on the `PartFData.GetPartFCategory(Space, ...)` path (one `PartFData` is shared via `ActiveSetting`; xUnit runs classes in parallel): (1) `PartFData.dictionary_SpaceUse` - published before filled; the original intermittent failure (`InvalidOperationException` from `Dictionary.TryInsert`, 1/2819 in the SAM#180 full run); (2) `PartFData.textMap_Legacy` - same fill-after-publish pattern (partial/wrong legacy match, `Collection was modified`); (3) `SpaceSemanticsResolver.cache`/`cacheKey` - plain dictionaries written on every `Resolve`.
- **Decisions:** (1)/(2) double-checked locking, built in a local under a private lock and published via a `volatile` field only when complete; first-match / longest-match / tie-gives-null behaviour unchanged. (3) cache read and write each under `lock_Cache`, `ResolveCore` outside the lock; the cache pair always updated together. `ConcurrentDictionary`/`Lazy<T>` rejected (see record). `spaceSemanticsResolver ??=` left as is (benign: fully built before assignment). No equivalent unsafe lazy shared collection remains in `PartFData.cs`.
- **Files changed:** `SAM/SAM.Analytical/Classes/PartF/PartFData.cs`, `SAM/SAM.Analytical/Classes/Semantics/SpaceSemanticsResolver.cs`, `SAM/SAM.Tests/PartFDataConcurrencyTests.cs` (new, 5 tests), `documentation/PartFData-ThreadSafeCache-Q4.md`.
- **Validation:** Each race reproduced against the unfixed code with the new tests (all failed in round 0). Fixed: focused tests 5/5 in 10 consecutive runs; full `SAM.Tests` Debug 2824/2824 and Release 2824/2824 (2819 + 5); `msbuild SAM.sln` Release rebuild 0 errors (110 pre-existing warnings, none in changed files). PR CI `spdx`, `build`, `test` green on the exact head.
- **Unresolved issues, risks:** None for this stream. The `"SAM.Analytical.ActiveSetting default Part F data"` xUnit collection is intentionally kept (also serialises other shared state). `TM59InternalConditionResolver` has a similar per-Guid cache but is per-call, not shared; not examined further.
- **Next step:** None for Part F concurrency. The 18 Q4 icon PRs remain deliberately deferred.

## Java-free GenOpt replacement — PR1, GenOpt 3.1.1 semantic oracle (2026-10-07)

- **Status:** complete, closed.
  - SAM-BIM/SAM#182 (`feature/genopt-oracle-traces`) merged into `sow/2026-Q4` as merge commit
    `0a67e1f5b0a4e9815b68ab549b634e2310113561` (PR head `3b3ea4a0edfcfcd4edba243368061d376abea356`, Q4 base `021c36e9`).
  - Merge method: merge commit, head-commit protected. Remote and local branch removed.
  - Companion: SAM-BIM/SAM_Tas#85 (Gate T, merged `1873e578`).
  - Records: `documentation/GenOpt-Oracle-PR1.md` (PR record), `documentation/GenOpt-3.1.1-Behaviour.md` (specification).
- **Goal of the stream:** replace the Java GenOpt 3.1.1 used by Tas Generic Optimisation with a native, Tas-agnostic
  SAM kernel (`SAM.Math`), shared by SAM_UI and Grasshopper. Phase 1 is GPSHookeJeeves, GPSCoordinateSearch and
  GoldenSection only; the first Tas evaluator calls `TasGenExecute.exe`. PR1 was the semantic gate.
- **Work completed:** no product code.
  - Behaviour specification for the subset Tas uses.
  - Test tooling `SAM/SAM.Tests.GenOptOracle` (verbs `cases`, `replay`, `probe-float`, `analyse-float`,
    `export-float-fixture`, `sim`).
  - 31 synthetic golden traces plus a Java-observed float fixture in `SAM/SAM.Tests/Golden/GenOpt/`.
  - `GenOptOracleFixtureTests` (66 tests: integrity and sanitisation).
- **Key findings** (all proven by `replay`, 31/31 bit-exact against real Java GenOpt):
  - **31 deterministic oracle cases.** Each was run twice through Java GenOpt with identical results. They cover GPS-HJ,
    coordinate search, GoldenSection, bounds, failures and retry, MaxIte, mesh settings, ties, Step 0 and negative
    Step, multiple outputs and the cache cases.
  - **Cache.** GenOpt's result cache is a red-black tree with a non-transitive 1e-12 point comparator. A simple
    dictionary or first-match lookup is **not** equivalent (`ec-gs-collapse`). The native kernel needs
    approximate sorted-tree behaviour.
  - **Coordinate rounding.** GPS rounding `parseDouble(Float.toString((float)x))` is **not** shortest-decimal under
    Java 8: Java differs on 11,440 of 240,138 probed floats. `Java8FloatTextModel` reproduces all but **42**, all in
    2^83 ≤ |x| < 2^86 (≈9.7e24–7.7e25).
  - **Command-file numbers** go through `StreamTokenizer`: `-0` → `0`, and algorithm keywords reject exponent notation.
  - A global improvement found on the last allowed simulation is never recorded.
  - `MaxEqualResults` has no effect on these algorithms.
- **Decisions / assumptions:**
  - The oracle is local only: the Tas-shipped `genopt.jar` run under a Temurin JRE 8 by explicit path. Neither is
    committed.
  - Fixtures are synthetic data rows only (no GenOpt banner or copyright header, no Tas material).
  - The tool is not in `SAM.sln` (like `SAM.Tests`).
  - Licensed and scratch Tas files are not committed.
- **Open owner decisions** (blocking PR2 merge, not PR2 start):
  - **D1:** accept 2^83 ≤ |x| < 2^86 as outside the parity domain?
  - **D2:** accept the float model's provenance? Structure informed by the JDK 8 `FloatingDecimal` design, validated
    only by black-box probing, no source copied.
  - **D3:** GenOpt attribution: none, a header line, or `NOTICE`/`THIRD_PARTY.md`? Licence: BSD-3-style with an
    "Enhancements" paragraph and a DOE notice, which appears to match `BSD-3-Clause-LBNL`. No notice was added.
  - **D4:** is storing sanitised GenOpt output data rows as fixtures acceptable?
  - **Update:** D1–D4 resolved by the owner on 2026-10-07 and implemented in PR2 (see the PR2 section below).
- **Files changed:** 52 files.
  - `documentation/GenOpt-3.1.1-Behaviour.md`, `documentation/GenOpt-Oracle-PR1.md`
  - `SAM/SAM.Tests.GenOptOracle/*`
  - `SAM/SAM.Tests/Golden/GenOpt/*`
  - `SAM/SAM.Tests/GenOptOracleFixtureTests.cs`
  - `SAM/SAM.Tests/SAM.Tests.csproj`
- **Validation:**
  - Full `SAM.Tests` (Release): 2890/2890.
  - The sanitisation test fails on a planted path.
  - `cases`: 31/31 deterministic. `replay`: 31/31 MATCH.
  - `probe-float`: 42 mismatches, all in the D1 band.
  - PR CI: `build`, `test`, `spdx` green.
- **Unresolved issues, risks:**
  - D1–D4 are open.
  - The oracle tool is not built by CI. `replay` is its regression check.
- **Next step:**
  1. Owner resolves D1–D4.
  2. Then PR2 `feature/native-optimiser-kernel`: a generic kernel in `SAM.Math` porting `SpecReplay` semantics
     (float model, red-black approximate cache, GPS and GoldenSection, progress, cancellation, trace). It must replay
     all 31 traces and the float fixture from `SAM.Tests`.
  3. Then SAM_Tas PR3 (evaluator).
  - **Update:** steps 1 and 2 are done (PR2, SAM#183, below).

## Java-free GenOpt replacement — PR2, native optimisation kernel in `SAM.Math` (2026-10-07)

- **Status:** complete, closed.
  - SAM-BIM/SAM#183 (`feature/native-optimiser-kernel`) merged into `sow/2026-Q4` as merge commit
    `0989ad819b34a7b797cc8c6ddf977081fbff1165` (PR head `647d17b5ad4a886e2a198a898d41feade5b65ea1`, Q4 base `e4a6f8e4`).
  - Merge method: merge commit, head-commit protected.
  - Remote and local branch removed.
  - Record: `documentation/GenOpt-NativeKernel-PR2.md`.
- **Work completed:** a generic kernel in `SAM/SAM.Math/Classes/Optimisation` (namespace `SAM.Math`).
  - It implements the PR1 specification (`SpecReplay` semantics), not textbook pattern search.
  - Algorithms: `HookeJeeves`, `CoordinateSearch` (shared `GeneralisedPatternSearch`), `GoldenSection`.
  - The internal `EvaluationContext` evaluation layer:
    - `Java8FloatText` rounding, with its own exact decimal→double conversion;
    - bounds;
    - `ApproximatePointCache<T>` (red-black tree, 1e-12 comparator);
    - numbering;
    - retry-once-then-stop;
    - cancellation;
    - main/sub counters, minimum report, progress.
  - API:
    - `Run(OptimisationProblem, IObjectiveEvaluator, IProgress<OptimisationProgress>, CancellationToken)` →
      `OptimisationResult`;
    - trace entries map one-to-one to OutputListingAll and OutputListingMain rows.
  - Dependencies: no Tas, EDSL, GenOpt-file-format or Java dependency. GenOpt text, `StreamTokenizer` parsing and the
    synthetic functions live only in the test adapter `SAM.Tests/Helpers/GenOptGoldenTrace.cs`.
- **Owner decisions (2026-10-07):**
  - **D1:** the 2^83 ≤ |x| < 2^86 band is accepted as outside parity and documented. A test pins the 42 values.
  - **D2:** the float-model provenance is accepted. The statement is in the class comment, the spec and the record.
  - **D3:** acknowledgment line in the `Optimiser` class comment, plus a GenOpt entry in `THIRD_PARTY.md` (SAM's
    established convention). The entry holds the licence and provenance details, with the copyright line taken
    verbatim from `legal.html` in `genopt.jar`.
  - **D4:** fixtures are accepted with the console transcript stripped from all 31 traces. The oracle tool still
    compares it for determinism, and a fixture test enforces the rule.
- **Decisions / assumptions:**
  - `SAM.Math` stays `netstandard2.0` with C# 7.3, and gains no new references.
  - Supported runtime: .NET 8. SAM no longer supports or uses .NET Framework (owner, 2026-10-07). No .NET Framework
    compatibility work or testing is in scope.
  - Additions outside GenOpt parity: cancellation (`Cancelled`, with the partial trace); evaluator-defined failure
    (exception, null or wrong output count counts as a failed attempt); validation before the first evaluation.
- **Files changed:** 66.
  - New in `SAM.Math`: `Classes/Optimisation/*` (19), `Enums/*` (3), `Interfaces/IObjectiveEvaluator.cs`.
  - New in `SAM.Tests`: `OptimisationGoldenTraceTests.cs`, `OptimisationKernelTests.cs`, `Java8FloatTextTests.cs`,
    `ApproximatePointCacheTests.cs`, `Helpers/GenOptGoldenTrace.cs`.
  - Modified: `GenOptOracleFixtureTests.cs`, `SAM.Tests.csproj`, `Golden/GenOpt/*.json` (31, deletions only),
    `SAM.Tests.GenOptOracle/CaseRunner.cs`, `THIRD_PARTY.md`, `documentation/GenOpt-3.1.1-Behaviour.md`.
  - New: `documentation/GenOpt-NativeKernel-PR2.md`.
- **Validation:**
  - All 31 golden traces replay **bit for bit** in `SAM.Tests`. They were re-run on the merged `sow/2026-Q4`, with 232
    GenOpt-related tests passing.
  - Full `SAM.Tests` Release: 3056/3056 (2890 + 166) on 5 consecutive runs.
  - Mutation check: 5 kernel mutations, each caught.
  - Kernel float model against the oracle model: 7,775,409 floats, 0 differences (local).
  - Oracle `replay` on the stripped fixtures: 31/31 MATCH.
  - PR CI on the head: `build`, `test`, `spdx` green.
- **Unresolved issues, risks:**
  - No golden trace visits a value where Java 8 and shortest-decimal rounding disagree. The fixture and two
    end-to-end tests cover this instead.
  - One unidentified intermittent `SAM.Tests` failure: once in 6 local full runs, a parameterless `[Fact]`, name not
    captured. If it recurs, capture the name and open a separate issue.
  - The `Java8FloatText` class comment still says why it avoids `double.Parse`, citing the .NET Framework parser.
    It was left unchanged on purpose (no product-code change for the runtime correction). It is not a compatibility
    requirement.
- **Next step:**
  - SAM_Tas PR3: a native GenOpt compatibility adapter plus `TasGenExecuteObjectiveEvaluator`, following
    `TASGENEXECUTE_PROTOCOL.md` §2.3. It consumes the `SAM.Math` kernel.
  - Only when the owner requests it.

## Java-free GenOpt replacement — PR6, `Java8FloatText` comment (2026-10-08)

- **Status:** complete, closed. SAM-BIM/SAM#185 (`docs/java8floattext-runtime-comment`) merged into `sow/2026-Q4` as
  merge commit `bac7507c35e4d380aca11c2635d00ef4c2203057` (parents: Q4 base `f391e022` + reviewed PR head
  `eb1efdcb3166e20d0ff99d335b63253063765dd6`; merge tree `bbeef13b` identical to the head tree); merge method: merge
  commit with `--match-head-commit`. PR CI (`build`, `test`, `spdx`) green on the head; post-merge `Build (Windows)` and
  `Test` on `bac7507c` green. Branch removed locally and on origin.
- **Work completed:** comment only in `SAM.Math/Classes/Optimisation/Java8FloatText.cs`: the sentence ".NET Framework parser
  is not correctly rounded" (deferred from PR3 to PR6) now states that the class converts with its own exact arithmetic
  so its result does not depend on any runtime's `double.Parse`, without naming .NET Framework hosts. No code or binary
  behaviour change. `Java8FloatText` is not obsoleted by the Java retirement: it models GenOpt's arithmetic, which the
  native optimiser reproduces.
- **Review:** Codex P2 on the first wording (named .NET Framework Revit/Rhino hosts, contradicting the 2026-10-07 owner
  decision that SAM no longer supports .NET Framework) fixed in `eb1efdcb`, thread resolved.
- **Programme context:** independent part of the PR6 set: SAM_Tas#87 (legacy Java route retired, `NativeGenOptOutcome`;
  merge `8dffaa3d`), SAM_Tas_Grasshopper#12 (merge `c43c2cad`), SAM_UI#212 (merge `a985d7d9`). Record: SAM_Tas
  `SAM_Tas/SAM.Analytical.Tas.GenOpt/NATIVE_GENOPT_PR6.md`.
- **Files changed:** 1 (`Java8FloatText.cs`). **Validation:** `dotnet build SAM.Math.csproj -c Release` 0 errors; CI green.
- **Unresolved issues, risks:** none for SAM. SAM_Deploy follow-up (separate): ship one current `SAM.Math.dll` to every
  SAM/Rhino location.
- **Next step:** none for this PR.

## Energy and mass units, engineering display formatting — native Optimisation PR2 (2026-10-08)

- **Status:** complete, closed. SAM-BIM/SAM#186 (`feature/units-energy-mass-formatting`) merged into `sow/2026-Q4` as
  merge commit `b52beaf3d2cae1f88d568f5333eed81d677c329e` (parents: Q4 base `b081065e` + reviewed PR head
  `5273ce725d513ab982aa102d24f8064aed0ab58c`; merge tree `016a578b` identical to the head tree); merge method: merge
  commit, after the owner's acceptance. PR CI (`build`, `test`, `spdx`) green on the head; post-merge `Build (Windows)`
  and `Test` on `b52beaf3` green. Codex: no review, comment or thread. Branch removed on origin. Record:
  `documentation/Units-EnergyMass-Formatting-PR.md`. PR2 of the staged native Optimisation plan
  (`opus-5-5-robust-blum.md`).
- **Work completed:**
  - `SAM.Units`: `UnitType` gained `WattHour`, `KilowattHour`, `MegawattHour`, `Kilogram` and `Tonne`, and
    `UnitCategory` gained `Energy` and `Mass`. All new values are appended; no ordinal moved.
  - Energy factors are based on Wh and mass factors on kg. kWh and kg are the defaults in both unit styles.
  - `Joule`/`Kilojoule` stay in `Enthaply`, so Energy does not convert to J; a cross-category conversion gives NaN.
  - `SAM.Core.Reporting.QuantityFormatter`:
    - Energy is shown in kWh to 1 dp, switching the group to MWh to 2 dp from 10 MWh.
    - Mass is shown in kg to 1 dp, switching the group to t to 2 dp from 10 t.
    - Group switching is now table-driven; Power behaviour is unchanged.
    - New per-category `DocumentOptions.Decimals` override.
    - New public `FormatNumber` (NaN/∞ → "—"), `FormatSignificant` (4 s.f. by default, scientific below 1e-3) and the
      static `DecimalsForSignificantFigures`.
    - The new helpers are deliberately not on `IQuantityFormatter`.
- **Decisions:** Count → "persons", Ratio at 0 dp and every other existing default are unchanged (Space reports depend
  on them). Optimisation counts are formatted as integers in the optimisation UI, not through `Count`. Currency and
  CO2e are not SAM.Units categories; they stay optimisation-level quantities (`SAM.Core.Optimisation`, PR3).
- **Files changed:**
  - `SAM/SAM.Units/{Enums/UnitType,Enums/UnitCategory,Query/UnitCategory,Query/UnitType,Query/UnitTypes,Convert/ByUnitType,Convert/ToSI,Convert/ToImperial}.cs`;
  - `SAM/SAM.Core.Reporting/Classes/{QuantityFormatter,DocumentOptions}.cs`;
  - new tests `SAM/SAM.Tests/{EnergyMassUnitsTests,QuantityFormatterEngineeringTests}.cs`;
  - the record.
- **Validation:**
  - Implementation session: `SAM.Tests` 3137/3137 (+66).
  - Independent review: a trial merge of PR2 with PR3 was clean; `SAM.sln` Release built with 0 errors; `SAM.Tests`
    3261/3261.
  - The existing Space report, `UnitsTests` and `QuantityFormatterTests` are unchanged and passing.
  - No existing `Format` path reaches the changed NaN handling, because `Format` throws on NaN before it.
- **Unresolved issues, risks (for UX PR5, not defects):**
  - A column made only of values below 1e-3 shows zeros (decimals are capped at 6), so PR5 needs a scientific fallback.
  - Currency (0 dp at 1 000 or more, 2 dp below) and the CO2e symbol (kgCO2e maps to the `Kilogram` unit type) must be
    handled in the optimisation presentation layer.
  - A negative `Decimals` override throws only when a value is formatted.
- **Next step:** none for this PR. PR3 (SAM#187) was refreshed onto this head and merged; see the next section. SAM_UI
  consumes the formatter in UX PR5.

## `SAM.Core.Optimisation` declarative Optimisation Definition — native Optimisation PR3 (2026-10-08)

- **Status:** complete, closed.
  - SAM-BIM/SAM#187 (`feature/optimisation-definition-core`) merged into `sow/2026-Q4` as merge commit
    `4eb0c49bcc65a2c344f3a2e8ef33be2bbd10096e`. Parents: Q4 head `b515e442` (after SAM#186 and its closeout) and PR head
    `466f43e41777c8290563a730f9467af29f805372`. The merge tree `e271c777` is identical to the head tree.
  - Merge method: merge commit with `--match-head-commit`, performed by the owner.
  - PR CI (`build`, `test`, `spdx`) green on `466f43e4`; post-merge `Build (Windows)` and `Test` on `4eb0c49b` green.
  - Reviews:
    - Owner approval on the first head `6cbb81ac`.
    - The independent acceptance review returned CHANGE with one required fix, applied in `466f43e4`.
    - Codex: no review, comment or thread.
  - Branch removed on origin and locally.
  - Record: `documentation/OptimisationDefinition-Core-PR.md`. PR3 of the staged native Optimisation plan
    (`opus-5-5-robust-blum.md`).
- **Work completed:** a new netstandard2.0 library `SAM/SAM.Core.Optimisation`. It references SAM.Units and
  System.Text.Json 8.0.5 only: no SAM.Math, UI or Tas dependency.
  - **Model:** the typed, portable definition `sam.optimisation/1` (`OptimisationDefinition`, `OptimisationModel`,
    `DesignVariable`, `OptimisationOutput`, `OptimisationObjective`, `OptimisationConstraint`,
    `GoldenSectionMethod`/`HookeJeevesMethod`, `StoppingCriteria`). It has no path fields.
  - **Reader:** `Create.OptimisationDefinition` is strict.
    - Errors (OPT1xx, with line and column): unknown, duplicate and wrong-type fields, numbers as text, NaN, 1e400,
      an unknown or newer schema.
    - Tolerated: comments, trailing commas and BOM.
    - Enum values and unit synonyms are normalised with an info note.
    - `extract` takes the JSON object out of a fenced or prose AI reply, with a warning.
  - **Writer:** `Convert.ToJson` is canonical and gives a bit-exact double round trip.
  - **Validation:** `Query.Diagnostics` / `IsRunnable` in layers, each finding with its path and a hint:
    - OPT2xx meaning, including **OPT214, a number that is not finite (the review fix)**;
    - OPT3xx units (declared, never verified);
    - OPT40x method;
    - OPT41x capability (definition still loads; not runnable).
  - **Capabilities and catalogue:** the `IOptimisationCapabilities` / `OptimisationCapabilities` model, and
    `OptimisationCatalogue`.
  - **AI text:** `Query.AIExchangeText` sets the contract: exactly one raw JSON object, and only what the engine runs
    is offered. The current definition is embedded as canonical JSON with no fence, and the text has no paths.
  - **Schema:** an embedded JSON Schema 2020-12 resource (`Query.SchemaText`), identical to the reader's field tables
    (enforced by a test).
- **Decisions:**
  - JSON, not YAML.
  - `sense` is required (no implicit minimise).
  - Unsupported features (maximise, constraints, integer/discrete variables) are kept, written back and non-runnable.
  - `K` means a temperature difference.
  - Method settings and `maximumSimulations` are optional; a null value means the engine's default.
  - Golden-section start and step are kept, for `Variables.txt` parity in PR4.
  - No `IJSAMObject` embedding, no unit conversion, and `.samopt.json` deferred: all V1.1.
  - **Systems Demo fixtures:**
    - checked against Script.txt `d68a7e2e`;
    - Setpoint and CO2 have no unit;
    - Result = Cost is annual cost in GBP.
- **Files changed:**
  - the new project `SAM/SAM.Core.Optimisation/**`;
  - `SAM.sln`;
  - `SAM/SAM.Tests/SAM.Tests.csproj`;
  - new tests `SAM/SAM.Tests/Optimisation{DefinitionReader,DefinitionWriter,Diagnostics,Exchange}Tests.cs` and
    `Helpers/OptimisationFixtures.cs`;
  - fixtures `SAM/SAM.Tests/Golden/Optimisation/systems-demo-{golden-section,hooke-jeeves}.json`;
  - the record.
- **Validation:**
  - `SAM.Tests` (built explicitly):
    - first head: 3195/3195;
    - independent trial merge with SAM#186: 3261/3261;
    - final head on top of SAM#186: **3272/3272** (+11: OPT214 cases and SAM.Units energy/mass resolution).
  - `dotnet build SAM.sln -c Release`: 0 errors, and the new project 0 warnings.
  - Mutations, each caught and then reverted:
    - M1, the writer uses G15: caught by 4 tests;
    - M2, the sense capability is not checked: 1;
    - M3, unknown fields are accepted: 3;
    - M4, the AI text always offers constraints: 1;
    - M5, the finite check always passes: 6.
  - No SAM.Math path changed.
- **Unresolved issues, risks:**
  - The library is not deployed yet (plan PR8, SAM_Deploy).
  - **Versioning:** strict unknown-field rejection means an older SAM refuses a newer `/1` document that has an added
    field. Decide the versioning policy before `.samopt.json`.
  - `AIExchangeText` embeds unsupported items from an imported definition: PR7 must strip or flag them.
  - `"minimize"` / `"maximize"` are errors with a hint, not normalised synonyms.
  - The column of a JSON syntax error counts bytes.
- **Next step:**
  - PR1–PR3 of the plan are all merged (SAM_UI#214, SAM#186, SAM#187).
  - PR4 (SAM_Tas `OptimisationDefinition` adapter parity) needs the owner's explicit authorisation.
  - Before PR4, fast-forward the local SAM and SAM_Tas checkouts and rebuild their `build\` folders from the merged Q4
    heads.

## Model bindings (targets and measures) in the Optimisation Definition — native Optimisation PR6 (2026-10-08)

- **Status:** complete, closed.
  - SAM-BIM/SAM#188 (`feature/optimisation-model-bindings`) merged into `sow/2026-Q4` as merge commit
    `6e5727f6ae156e683d58ea51f05504d84d504a0c`. Parents: Q4 head `721b5fae` and PR head
    `8464827c8371310b59502d234572362e6e89e0be`. The merge tree `8243136c` is identical to the head tree.
  - Merge method: merge commit with `--match-head-commit`, after the owner's explicit approval.
  - PR CI (`build (Release)`, `test (Release)`, `spdx`) green on `8464827c`. Post-merge `Build (Windows)` and `Test` on `6e5727f6` green. No review comments or threads.
  - Branch removed on origin and locally.
  - Record: `documentation/OptimisationDefinition-Bindings-PR.md`.
  - Plan of record: SAM_UI `documentation/NativeOptimisation-Plan-ModelBindings.md` (SAM_UI#216). The owner's
    construction/glazing-choice follow-up was added to it by SAM_UI#217.
- **Work completed (SAM only; SAM_Tas, SAM_UI and SAM.Math unchanged):**
  - **Bindings.** `DesignVariable.Target` (`OptimisationTarget`) and `OptimisationOutput.Measure`
    (`OptimisationMeasure`), both optional. Each binding has:
    - an engine-defined `kind`;
    - a `reference` (text map naming the model item);
    - numeric `parameters`;
    - for a target only, `options` (ordered model item names, for a choice).
  - **Capabilities.** `IOptimisationCapabilities.Targets` / `Measures` list `OptimisationBindingCapability`: kind,
    display name, quantity, unit, `OptimisationReferenceKey`s, `OptimisationBindingParameter`s (default, range) and
    `AcceptsOptions`. The six-argument `OptimisationCapabilities` constructor is kept and means "no kinds".
  - **Catalogue.** Entries can carry a target (current value, suggested range, available options) or a measure (current
    value); `OptimisationCatalogue.HasBindings`. Name-only entries are unchanged.
  - **Reader and writer.** The reader stays strict for schema fields (OPT105–OPT110 apply inside bindings). The writer
    is canonical, with reference and parameter keys in ordinal order.
  - **Diagnostics: new range OPT600–OPT615** (OPT5xx stays reserved for engine execution checks):
    - no kind;
    - kind not offered by the engine (including a measure used as a target, or an engine that takes none);
    - reference key missing or unknown;
    - parameter unknown or out of range;
    - unit or quantity not the kind's;
    - the same target twice;
    - an unbound variable or output on an engine that lists kinds;
    - not in the model's catalogue;
    - the option rules.

    OPT214 covers a non-finite parameter.
  - **New overloads (old signatures kept):** `Query.Diagnostics(def, caps, catalogue)`,
    `Query.IsRunnable(def, caps, catalogue)`, `Create.OptimisationDefinition(text, out d, caps, catalogue, extract)`.
  - **AI text.** For an engine with kinds, every variable and output must be bound, copied from AVAILABLE. Only the
    catalogue items whose kind the engine lists are offered, with current values, units, ranges and parameters. A
    choice is offered only when the engine runs `discrete`. The "no code" rule is kept, and the `tas-script` text is
    unchanged.
  - **Schema resource.** New `target`, `measure`, `reference` and `parameters` `$defs`.
- **Decisions:**
  - Stay on `sam.optimisation/1`.
  - Reference values are text; a null map value counts as absent.
  - An engine that lists no kinds takes no bindings. An engine that lists kinds needs every variable and output bound.
  - **Choice variable:** `discrete`, options numbered 1..n (`minimum` 1, `maximum` = option count). Shape only: no engine
    runs `discrete`, and the "try every option" method and the TBD swap are not built.
  - The same measure twice is allowed (for example two thresholds).
  - Catalogue checks apply only when the catalogue lists model items.
- **Files changed:**
  - `SAM/SAM.Core.Optimisation/`:
    - 7 new files (`Classes/{OptimisationBinding,OptimisationTarget,OptimisationMeasure,OptimisationBindingCapability,OptimisationReferenceKey,OptimisationBindingParameter}.cs`, `Query/BindingDiagnostics.cs`);
    - 13 changed (definition classes, capabilities, catalogue, reader, names, diagnostic comment, interface, writer,
      Create, Diagnostics, AIExchangeText, schema);
  - `SAM/SAM.Tests/`: fixtures helper, the four optimisation suites, and 3 new LF fixtures
    (`systems-demo-bound-golden-section.json`, `zone-setpoints-glazing-hooke-jeeves.json`, `glazing-choice.json`);
    the `systems-demo-*.json` fixtures are not edited;
  - the record.
- **Validation:**
  - `dotnet build SAM.sln -c Release`: 0 errors; `SAM.Core.Optimisation` 0 warnings.
  - `SAM.Tests`: **3355/3355** (3272 before, +83; optimisation suites 364, 281 before).
  - Mutations M1–M10, all caught (2, 42, 1, 6, 1, 1, 1, 2, 1, 1 failing tests).
  - Against this build, with no change to either repo:
    - SAM_Tas `SAM.Analytical.Tas.GenOpt` built, and its tests **257/257**;
    - SAM_UI Release `SAM_UI.sln` 0 errors, and `TasOptimisation*` **121/121**.
  - `git diff --check` clean.
- **Unresolved issues, risks:**
  - Kinds, reference keys, units and parameter ranges in the fixtures are placeholders; SAM_Tas defines the real ones in
    PR7b (PR7a may change them: R1 g-value, D1 overheating).
  - References are names (R3): a renamed item gives OPT609 with a suggestion; nothing repairs it automatically.
  - Versioning (open since PR3): a PR3-era SAM refuses `target`/`measure` with OPT105. Decide before `.samopt.json`.
  - `SAM.Core.Optimisation` is still not deployed (plan PR10).
- **Next step:** PR7a, the SAM_Tas licensed spike (evidence only), when the owner authorises it. Hand-over prompt
  `SAM-BIM\NEXT_SESSION_PROMPT_PR7A.md` on the authoring laptop (local, not in git; it points only to committed files).

## Default aperture library: fixed Guid for `SIM_EXT_GLZ` (2026-10-08)

- **Status:** complete, closed.
  - SAM-BIM/SAM#189 (`fix/aperture-library-sim-ext-glz-guid`) merged into `sow/2026-Q4` as merge commit
    `73fad11c53bfe5a2da555dda04d6149d0caf7d33`. Parents: Q4 head `4b2e6450` and PR head
    `0461b0f02c0c00d5257b98cabbe7358b432c3273`. The merge tree `726895ac` is identical to the head tree.
  - Merge method: merge commit with `--match-head-commit`, after the owner's explicit approval.
  - PR CI (`build (Release)`, `test (Release)`, `spdx`) green on `0461b0f0`. Post-merge `Build (Windows)` and
    `Test` on `73fad11c` were still running when this closeout was written.
  - Branch removed on origin and locally.
  - Record: `documentation/ApertureConstructionLibrary-Guid-PR.md`.
- **Work completed:**
  - `files/resources/Analytical/SAM_ApertureConstructionLibrary.JSON`: the Window `SIM_EXT_GLZ` had the malformed
    Guid `4d00dd0-f646-4fbb-90e6-f8d9cd6634eb` (7 hex digits). `Core.Query.Guid(JsonObject)` replaces an unparseable
    Guid with `Guid.NewGuid()`, so this construction got a new identity on every load. The value is now
    `04d00dd0-f646-4fbb-90e6-f8d9cd6634eb` (the missing leading zero added). The file is otherwise byte-identical:
    UTF-8 without BOM, LF line endings, no trailing newline.
  - New `SAM/SAM.Tests/ApertureConstructionLibraryGuidTests.cs` (2 tests). Every library Guid parses; the Guids are
    distinct and stable across two loads; `04d00dd0-…` is pinned to the Window `SIM_EXT_GLZ`.
- **Why it matters:** SAM_UI's Glazing window (`GlazingCandidate`) and the native Optimisation glazing choice
  (SAM_Tas PR7a-2, `SAM_Tas/SAM.Analytical.Tas.GenOpt/NATIVE_OPTIMISATION_PR7A2_GLAZING.md`, SAM_Tas#90) identify
  systems by Guid.
- **Decisions / assumptions:**
  - No persisted data can refer to the old value, because it never survived a load.
  - `LibraryFixtureTests.ApertureConstructionLibrary_RoundTrip` could not catch this defect. It compares
    serialisations made after the random Guid was already assigned.
- **Validation:**
  - Guid scan of all 21 `*.json` files under `files/resources` (1817 values). This was the only malformed value;
    after the fix there are none.
  - The old value is referenced nowhere in the SAM-BIM checkouts except the SAM_Tas PR7a-2 record, which documents
    the defect.
  - Both new tests fail on the old value.
  - `SAM.Tests` Release: **3357/3357**.
- **Unresolved issues, risks:**
  - Installed SAM resource copies keep the bad value until the next deploy. The owner will run Build All and
    deploy.
  - A persisted `ActiveSetting` file may still hold a random Guid that was serialised earlier.
  - The library has five entries named `SIM_EXT_GLZ`, each with a distinct Guid. This data is unchanged.
    Consumers must identify them by Guid.
- **Next step:**
  - Owner: Build All and deploy, so installed copies pick up the fixed Guid.
  - Optional: update the SAM_Tas PR7a-2 record to name `04d00dd0-…`.
  - Optional: handle the persisted-settings case in a future change (owner: "we fix in future").

## "Try every option" method and runnable choice variables — native Optimisation PR6b (2026-10-08)

- **Status:** complete, closed.
  - SAM-BIM/SAM#190 (`feature/optimisation-try-every-option`) merged into `sow/2026-Q4` as merge commit
    `d5253b4a075886039e46de57e7e6e5a63c51ea8b`. Parents: Q4 head `86b9dcd2` and PR head
    `4312ba4432bc1fc9e43ddcd46cbfe16a7f439594`. The merge tree `d323035b` is identical to the head tree.
  - Merge method: merge commit with `--match-head-commit`, after the owner's explicit approval.
  - PR CI (`build (Release)`, `test (Release)`, `spdx`) green on `4312ba44`. No review comments or threads.
  - Branch removed on origin and locally.
  - Record: `documentation/OptimisationDefinition-TryEveryOption-PR.md`. Follows the owner decisions of 2026-10-08 on
    SAM_Tas#90 (PR7a-2): the glazing target is a choice among real systems run by "try every option"; SAM first.
- **Work completed (SAM only; SAM_Tas, SAM_UI, Grasshopper unchanged):**
  - **Schema:** method `"try-every-option"` (`OptimisationAlgorithm.TryEveryOption`, appended; `TryEveryOptionMethod`,
    no settings). Reader/writer canonical; another method's setting in it is OPT105 naming that method. Schema resource
    gains `$defs.tryEveryOption`.
  - **Diagnostics:**
    - OPT409 (method, no capabilities needed): try every option on a non-choice, or a choice with golden section /
      Hooke–Jeeves;
    - OPT416: `maximumSimulations` below the number of options;
    - OPT615 extended: a `"discrete"` variable without `options` is allowed, numbered 1 to a whole n;
    - OPT616: more options than the kind's new `OptimisationBindingCapability.MaximumOptions` (8-argument constructor;
      the 7-argument one kept);
    - OPT408 info: try every option keeps but does not use start/step (a start must still be in 1..n, OPT205);
    - OPT412 (one variable via `OptimisationAlgorithmCapability(TryEveryOption, 1, 1)`) hint suggests only suitable
      methods; OPT415 unchanged.
  - **AI text:** `"discrete"`, the choice rules and `"try-every-option"` are offered only when the engine runs both;
    choice entries show `options (at most N)`.
  - **Kernel (`SAM.Math.TryEveryOption`):** every whole number Minimum..Maximum of one parameter, in order, one at a
    time; each option an `OptimisationEvent.OptionEvaluated` entry (appended enum value); the lowest objective reported
    as `MinimumPoint` (ties to the lower option, NaN never wins, none if no number). Shared evaluation layer: a failed
    option **stops** the run (first not retried, later retried once), no best; cancellation; every option a counted
    simulation. More options than `MaximumSimulations` throws before evaluating.
- **Decisions:** stop-not-skip on failure (a table with a hole cannot name a best; matches SAM_Tas
  `NativeGenOptOutcome`); option limit is an engine capability per target kind, not schema; a choice runs on its own
  through the engine's capability (1, 1), not a schema rule; `"integer"` stays reserved.
- **Files changed:** 23 (+1331/−60): `SAM.Math` (new `TryEveryOption.cs`; `EvaluationContext.ReportLowest`; enum and
  comments), `SAM.Core.Optimisation` (new `TryEveryOptionMethod.cs`; names, reader, binding capability, diagnostics,
  binding diagnostics, AI text, schema), `SAM.Tests` (new `OptimisationTryEveryOptionTests.cs`; fixtures helper; the
  four optimisation suites; `glazing-choice.json` now `"try-every-option"`, no start/step), the record.
- **Validation:**
  - `dotnet build SAM.sln -c Release` 0 errors; `SAM.Core.Optimisation` 0 warnings.
  - `SAM.Tests` **3414/3414** (3357 before, +57); optimisation suites 421 (364 before).
  - Mutations M1–M14 all caught (5, 3, 10, 1, 2, 4, 3, 2, 1, 1, 4, 2, 1, 33 failing tests).
  - Unchanged downstream against this build: SAM_Tas `SAM.Analytical.Tas.GenOpt.Tests` **257/257**; SAM_UI Release
    `SAM_UI.sln` 0 errors and `TasOptimisation*` **121/121**.
- **Unresolved issues, risks:**
  - Versioning (open since PR3): an older SAM reads `"try-every-option"` as OPT111.
  - Kinds/keys still PR6 placeholders until PR7b (`tbd.glazing-construction.choice`, `glazingConstruction`).
  - New enum values are unseen by consumers until PR7b/PR8 (their switches have defaults; SAM_UI's algorithm name falls
    back to the enum name).
  - `SAM.Core.Optimisation` and the new `SAM.Math` are not deployed yet (PR10).
- **Next step:** PR7b in SAM_Tas (catalogue reader, unique-name option writing, pane swap, engine `tas-model`,
  capabilities `TryEveryOption` (1, 1) + `Discrete` + glazing choice `MaximumOptions` 8, and the kernel mapping in the
  record), only when the owner starts it. Before PR7b, rebuild SAM's `build\` from this merge (the SAM_Tas and SAM_UI
  HintPaths read it).

## Part O stable semantic design key (2026-10-07)

- **Status:** complete, closed.
  - SAM-BIM/SAM#184 (`feature/part-o-design-key`) merged into `sow/2026-Q4` as merge commit
    `bb8e3e6f25016a6ac9063e9d7213a86e80675084` (PR head `f15f32fc57e603b42befde0faf4742f033474117`, Q4 base at PR cut `021c36e9`;
    the base had advanced to `79c101c2` - SAM#182/#183 - by merge time).
  - Merge method: merge commit, head-commit protected. Remote and local branch removed. Record: `documentation/PartODesignKey-Q4.md`.
- **Work completed:** `Query.PartODesignKey(AnalyticalModel)` -> `PartODesignKey:v1:<sha256>`, one SHA-256 over one canonical
  stream (no FNV state mixed in) answering "has the engineering meaning of this design changed since a Part O result was produced
  from it?". Optional `PartOModelReference.DesignKey` (design references only, schema stays v1, absent = unknown) recorded by
  `Create.PartOBaselineReferenceFromDesign` and inherited by 2B/3 references. `PartOModelResolution.Inspect` (design kind) compares
  `DesignKey` when present, else the legacy `Fingerprint`. `SimulationResultProvenance.Fingerprint` unchanged in definition and output
  (its exclusion list was extracted to the shared `ParameterNames_Excluded`).
- **Decisions / assumptions:**
  - Cause verified in code and by probe: `PartOEquipmentSelection`, each pooled `VentilationUnitReference` and
    `PartOProjectTestVentilationUnit` are `SAMObject`s that mint `Guid.NewGuid()` and persist it; the UI rebuilds them on every read and
    `PersistPartOInputs.SameValue` compares guids, so an identical selection is rewritten with new guids and the byte fingerprint moves.
    Save/reopen of one object is stable - the instability is regeneration, not serialization.
  - Absent selection == explicit default; pool order/duplicates carry no meaning; parameter-set guids (build-MVID derived) and the
    fingerprint's existing exclusions are not meaning; cluster identities (incl. Part F terminal references) are meaning, so a Part F
    recalculation is a design change.
  - Not changed: Direct T3D, TAS, TM59, Part O simulation, SAM_UI.
- **Files changed:** `SAM/SAM.Analytical/Query/PartODesignKey.cs` (new), `Classes/PartOModelReference.cs`, `Create/PartOBaselineReference.cs`,
  `Query/PartOModelResolution.cs`, `Classes/SimulationResultProvenance.cs`, `SAM/SAM.Tests/PartODesignKeyTests.cs` (new, 15 tests),
  `documentation/PartODesignKey-Q4.md`.
- **Validation:** PR CI `build (Release)`, `test (Release)`, `spdx` green on the exact head. Local full `SAM.Tests` on the PR head
  2839/2839. Because the base advanced after CI, the merge of the PR head onto `79c101c2` was also built and tested in a throwaway
  worktree before merging: 3071/3071 (2839 + 232 from SAM#182/#183), no overlapping files.
- **Unresolved issues, risks:**
  - One extra streamed cluster serialization per run when the design reference is created.
  - Results saved before this have no `DesignKey` and keep the byte-fingerprint behaviour until re-run.
  - `PartOMaterialisationRecord.Fingerprint_Baseline` still uses the byte fingerprint and may show the same noise (candidate follow-up).
  - SAM_UI still rebuilds Part O inputs with fresh guids and `PersistPartOInputs.SameValue` compares guids (harmless for this question now).
- **Next step:** a separate SAM_UI PR that consumes `DesignKey` / `PartOModelResolution` for "previous compatible run found" and
  reopening a previous run from the design model. Companion docs PR SAM-BIM/SAM_UI#208 (Part O guide) is closed in the SAM_UI record.

---

# Historical record - 2026-Q3 (frozen)

Source: last revision of the file on `sow/2026-Q3`, commit `689f75d5` (the file was removed from the Q3 tip by `8ab6a62b`; `sow/2026-Q3` tip is `3fcd64bf`). Preserved verbatim except that heading levels are shifted down one. Everything below describes Q3 and is not a current instruction.

## SAM Part O PR1 progress

Base: `sow/2026-Q3`. PR1 merged as SAM-BIM/SAM#176 at `fc2345667fb2726b2364264eda14a6a219250144` on 2026-10-04. Local base updated.

### Completed
Persisted CoolingStatSpaceGuid on dwelling strategies and cooled records; refused missing or out-of-dwelling selections; validated record against selected room. Legacy cooled strategies retain no room and require explicit confirmation. Uncooled canonical strategy text remains unchanged.

### Files changed
PartODwellingStrategy, PartOCooledDwelling, PartOMaterialisationRecord, MaterialisePartODwellingStrategies, refusal enum, focused tests; this progress file.

### Validation
Focused PartODwellingStrategy tests: 145 passed; broader PartO suite: 982 passed; PR Windows build, test and SPDX passed.

### PR2 validation (2026-10-04)
No SAM code changes were needed. Existing SimulationResultProvenance tests (23 passed) cover changed weather, changed model/scenarios, missing or rewritten results, and relative result resolution. PR2 diagnostics merged in SAM_Tas#81 and SAM_UI#192. No physics changed.

### Solver2D clustered anchors (SAM_UI#58), 2026-10-05
Merged as SAM-BIM/SAM#177 at `2eecf11ac6ae1a07384ab84d54ec8d90b2120841` (head `478cdd70`); local `sow/2026-Q3` = origin; branch deleted. Three exact changes to `Solver2D.cs`, none of which moves a label:
- same-ray blocker skip (deep axis-aligned overlap, margin MacroDistance, at most 8 box checks per candidate);
- identical-search resume cursor (`CandidateSequence`, limit area by reference);
- spatial index built at every size, with no halo, MacroDistance query expansion, median-short-side cells, and an outlier list.

`WorkBudget` and all backstops are unchanged. 400 coincident labels: 620 263 → 1 336 units, 164 → 0 fallbacks. Heterogeneous near-coincident and tight clusters went from 3.8M–11.4M to 92k–192k uncapped. Healthy placement fingerprints are identical. Tests: differential oracle `ReferenceSolver2D` over 11 inputs (mutation-checked); SAM.Tests 2 818 passed; PR CI build, test and SPDX green. The remaining heterogeneous-pile cost (up to 38 % of the budget) is accepted; a layout-changing fast path is tracked in SAM_UI#199. SAM_UI end-to-end test: SAM_UI#200.

### Next step
Solver2D #58 work is complete in SAM; do not start SAM_UI#199 unless requested. PR2 is complete. Next task, only when requested: real end-to-end acceptance through SAM_UI → Part O → Prepare & Run → Iteration 3 using the prepared Nuaire sample. Do not start PR3.
