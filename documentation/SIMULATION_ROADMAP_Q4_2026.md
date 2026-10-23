# SAM-BIM simulation roadmap (Q4 2026)

Canonical, persistent roadmap for the Tas, OpenStudio and Both simulation workflows. It orders the work; it does not
authorise it. **One task is active at a time, and each task still needs its own owner-approved start, feature branch
and owner-reviewed PR** under the `AGENTS.md` of the repository that owns the code. Closeouts go to that repository's
`PROJECT_PROGRESS.md` after merge, as usual; this file only changes through a reviewed PR.

Architecture and the full issue map are not repeated here. They are in the trackers
[SAM-BIM/SAM#206](https://github.com/SAM-BIM/SAM/issues/206) (cross-repository) and
[SAM-BIM/SAM_UI#227](https://github.com/SAM-BIM/SAM_UI/issues/227) (SAM_UI / SAM_OpenStudio), and in the merged
records `SAM_UI/documentation/OpenStudio-Simulation-PR229.md` and `SAM_UI/documentation/Engine-Comparison-PR231.md`.
Where this roadmap and an older tracker phase or gate disagree on priority or scope, this roadmap is newer.

## Strategy (owner-approved, 2026-10-19)

| Phase | Goal | State |
|---|---|---|
| **A** | Reliable Tas, OpenStudio and Both workflows: trustworthy outputs, and every failure detected and reported. | Active |
| **B** | Independently verify the conversion into each native model, and the results imported back into SAM. | After the Phase A gate |
| **C** | Scientific engine comparison, physics parity and ASHRAE 140 research. | **Frozen** |

**Numerical agreement between Tas and EnergyPlus is not an acceptance requirement** in any phase. A run is judged on
whether the workflow did what it reports, whether each engine received what SAM intended (Phase B), and whether SAM
reports what each engine produced (Phase B). Engine differences are recorded, not gated.

Frozen under Phase C (no new work without a new owner decision): the SAM_UI#239 C6-C8 window-opening experiments and
their proposed natural-ventilation milestone, ASHRAE 140, engine surface-heat-balance/sky/glazing investigation, the
remaining box-cooling and HungaryHouse heating residuals, output-agreement tolerances, and the G3 difference
classification (SAM_UI#233). Their evidence is kept as it is. Phases 2 and 3 of the trackers (parallel jobs
SAM_UI#234/#235, SAM#207, SAM#210; SAM MCP SAM_UI#236) are not scheduled by this roadmap.

## Baseline already delivered (not tasks)

| Item | Evidence |
|---|---|
| OpenStudio Simulation from SAM_UI (G1) | SAM_UI#237 merged `ec1b18c9` (SAM_UI `PROJECT_PROGRESS.md`, 2026-10-18) |
| Run Simulation Tas / OpenStudio / Both on one frozen snapshot and the comparison view (G2) | SAM_UI#238 merged `4ffa740f`; SAM_Validation#15 `912ae94e` |
| Licensed Both runs from the real UI (box embedded and explicit EPW, HungaryHouse) | SAM_UI#239 (open, documentation only): workflow PASS on Q4 `4ee45fef`; HungaryHouse Tas crash not reproduced |
| EPW writer radiation and sky fields (finding F1) | SAM#212 merged `5ac57033`. Embedded-weather results before it are superseded and not comparable. |

## Task register

Status values: **Active**, **Next** (first candidate after the active task), **Not started**, **Blocked** (with the
blocker), **Done** (merge SHA). Code references were checked on `sow/2026-Q4` on 2026-10-19 (SAM `a2069103`,
SAM_UI `4ee45fef`, SAM_Tas `a34a298d`); recheck them before starting a task.

| ID | Task | Repository | Scope | Related | Acceptance criterion | Status |
|---|---|---|---|---|---|---|
| S00 | Roadmap and PR #239 evidence consolidation | SAM, SAM_UI | This file; reconcile the SAM_UI#239 record and description (supersede pre-F1 embedded-weather evidence, freeze C6-C8). Documentation only. | SAM#206, SAM_UI#227, SAM_UI#239, SAM#212 | Roadmap reviewed by the owner; #239 states what is superseded, frozen and kept, and links here. | Done: SAM#213 `b4e68148`, SAM_UI#239 `d5aa34b2` |
| S01 | Tas silent-failure detection | SAM_Tas (detection), SAM_UI (reporting) | `Modify/Simulate.cs` (lines 82 and 144) discards the `TBDDocument.simulate` return code (-1 ran, 0 refused). A refusal still writes a small stub TSD, so the outcome is decided by `WaitToUnlock(path_TSD)`. Detect refusal and an empty or stub TSD, and carry the failure to Energy Simulation, Run Simulation and B1a. | SAM#92 (merged PR where the refusal was diagnosed) | A Tas refusal or empty TSD is reported as failed, with the Tas return code and log path, in every entry point and never as Success; a successful run is unchanged (persisted-value baseline comparison as in the #237 acceptance). | Done: SAM_Tas#103 `e483877c`, SAM_UI#240 `5b379191` |
| S02 | Shared simulation concurrency guard | SAM_UI | `AnalyticalWindow` has separate `openStudioSimulationRunning` and `engineComparisonRunning` flags; Energy Simulation / Run Simulation > Tas (`SimulateTas`) has none. One guard for all simulation entry points. Not parallel execution. | SAM_UI#234 (design context only) | While any simulation runs for a model, no other simulation can start from any entry point; the guard is released on success, failure, exception and cancel (tests per entry point). | Done: SAM_UI#241 `49eebe13` |
| S03 | Run settings and version traceability | SAM_UI, SAM_Validation if the schema changes | Every run records the app commit (today `SAM_COMMIT` is set by hand), producer executable versions and hashes, Tas and OpenStudio/EnergyPlus versions, weather source (embedded or explicit, hash) and design-day source, and the run options in the run manifest. | SAM_UI#228 (G0 traceability), B1a provenance | Each Tas, OpenStudio and Both run manifest contains every item without manual environment variables; an unavailable item is written as unknown with a reason, never blank. | Done: SAM_UI#242 `1e460bbe` (Both with developer-built producers still needs `SAM_COMMIT`: accepted until S06) |
| S04 | Stable canonical simulation snapshot | SAM_UI (snapshot), SAM_Validation (canonical hash) | Finding F7: the canonical model hash includes the UI-only `SAM.Analytical.UI` ParameterSet, whose GUIDs are regenerated on each load. | SAM_UI#239 F7 | The same saved model loaded twice gives the same canonical hash; an engineering edit changes it; same-run provenance unchanged. | Done: SAM_UI#243 `307cb0e3` |
| S05 | Licensed Phase A GUI acceptance | SAM_UI (record only) | Licensed runs from the real UI on the S01-S04 content: Tas only, OpenStudio only and Both; box with embedded (post-F1) and explicit EPW; one multizone model; injected Tas refusal; cancel; concurrent-start attempt. | SAM_UI#228, SAM_UI#239 (prior evidence) | Each case reports PASS / FAIL / NOT RUN / BLOCKED; every reported status, output file and failure message matches what the engines did; no numerical agreement required. | Done: SAM_UI#245 `9acce829` (all nine cases PASS; preflight SAM_UI#244 `46a43d8d`) |
| S06 | Early SAM_Deploy smoke candidate | SAM_Deploy | Payload the Benchmark producers, SAM_Validation and the OpenStudio dependencies, so an installed candidate (not a developer build) can run the workflows. Avoids the stale `deps.json` state seen in #239. | SAM_UI 2026-10-18 closeout (producers not in BuildAll payload) | An installed candidate runs Tas, OpenStudio and Both on the box model on a licensed machine. Candidate build only; any release needs separate owner approval. | Done: SAM_Deploy#66 `1657bb53`, SAM_OpenStudio#32 `a9429f7c`, SAM_Validation#16 `e9a83df0` (provenance labelling: Not PASS, accepted limitation) |
| S07 | Minimal Phase B physical-input mapping | SAM (this documentation), SAM_Validation | The minimal set of physical inputs whose conversion is checked (areas, volumes, U and g, gains, setpoints, schedules, infiltration, ventilation, weather and design days), the native field each engine uses, and the tolerances. Records the owner decisions on F2 (ventilation semantics), F8 (window opening) and F9 (zone volume); does not implement them. | SAM#206 input-equivalence table, SAM#209, SAM_UI#232 | Owner-approved mapping table with tolerances and an owner decision recorded for F2, F8 and F9. | Done: [SAM#216](https://github.com/SAM-BIM/SAM/pull/216) `68b60257`; L1 SAM#217 `113873a0`, U1 SAM#218 `b06dbe8c`. OD-G1 to OD-G3 approved during S08; OD-W1/OD-W2 and OD-L1 remain open |
| S08 | Tas independent native-input read-back | SAM_Tas, SAM_UI | Read the generated TBD independently of the SAM writer and produce the Tas input manifest for the S07 quantities. | SAM#208, SAM_UI#230 | For the box and the multizone model, every S07 quantity read back from the TBD equals SAM within the S07 tolerance or is listed as a mapping difference. | Done: SAM_Tas#104 `d08916ca`, #105 `fb9041c1`, #106 `61eb0d37`; signed-off box PASS 353/353 and two-zone PASS 623/623 on the contract route |
| S09 | OpenStudio independent native-input read-back | SAM_OpenStudio, SAM_UI | Same for the generated OSM/IDF: the OpenStudio input manifest. | SAM_OpenStudio#30 | As S08, for OpenStudio. | Complete (2026-10-23): SAM_OpenStudio#33 merged `81acb8e7`, closeout `ec204cbb`; both fixtures PASS scoped native-input conformance (S07 §5.2 U1), 21/21 negatives FAIL. Not a Phase B PASS |
| S10 | Tas results round-trip validation | SAM_Tas | SAM-imported results and B1a against an independent TSD read. Includes F4 (1-based `peakHour`), F5 (governing instead of annual peak) and F6 (zero-peak hour). | SAM_UI#239 F4-F6 | Model and space results in SAM and B1a equal the TSD values; hour and peak definitions are documented; F4-F6 fixed or explicitly accepted. | Accepted with one declared validation limitation (2026-10-23): SAM_Tas#107 merged `1d61ab05`, closeout `922044fa`; F4-F6 fixed; box PASS 59/59; two-zone 94 PASS / 0 FAIL / 1 BLOCKED, FixB Heating `designDayPeak.hourOfDay` (no independent heating time signal) stays BLOCKED as an owner-accepted limitation. Not an unqualified full PASS |
| S11 | OpenStudio results round-trip validation | SAM_OpenStudio | SAM-imported results and B1a against an independent SQL/ESO read; label total versus sensible cooling (F3). | SAM_UI#239 F3 | Model and space results equal the SQL/ESO values; metric definitions documented. | Not started |
| S12 | Final integrated release acceptance | SAM_Deploy, all producers | Phase A and Phase B evidence repeated on an installed release candidate. | All above | Final installation gate passes on a licensed machine. Release needs owner approval. | Not started |

S01-S09 are complete (task log below). S09 is scoped native-input conformance on the signed-off fixtures, merged in
SAM_OpenStudio#33.
S10 is accepted with one declared validation limitation (SAM_Tas#107; the FixB heating design-day hour of day stays
BLOCKED). S11-S12 have not started. Phase B is not complete: S11 remains outstanding, and S10's limitation must be
carried into the Phase B and release evidence.

## Acceptance gates

Gates are reported as **PASS / FAIL / NOT RUN / BLOCKED**. GitHub CI (build, unit tests, SPDX) is reported separately
from local licensed runs; a check blocked by a missing licence or executable is BLOCKED, never PASS.

- **Phase A gate:** S01-S04 merged with green final-head CI; S05 PASS for every case on a licensed machine; S06 PASS on
  an installed candidate. No case may report Success for a run that did not complete.
  Status 2026-10-20: S01-S04 merged with green final-head CI; S05 PASS for all nine cases; S06 PASS on a genuinely
  installed candidate (Tas, OpenStudio and Both from the real ribbon on a licensed machine). **The gate is met, with one
  owner-accepted, documented exception:** the S06 provenance-labelling check is Not PASS (accepted limitation; see S06 in
  the task log). Phase B starts only with an owner start of S07.
- **Phase B gate:** S07 approved; S08-S11 PASS on the box and one multizone model whose fixture provenance is cleared.
  Engine output differences are recorded, not gated.
- **Final installation gate (S12):** on an installed release candidate on a licensed machine, the S05 cases and the
  S08-S11 checks pass again; versions and hashes in the run manifests (S03) match the installed build.

## Owner decisions outstanding

1. ~~Approval of this roadmap and of the Phase C freeze (S00).~~ Approved by the owner on 2026-10-19 (S00 merged).
2. SAM_Tas was recorded as read-only while the HVAC conversion is in progress (SAM_UI#239). S01 and S08 PR-A/PR-B/PR-C
   were separately authorised and merged (SAM_Tas#103-106). S10 was separately owner-started on 2026-10-23 and merged
   as SAM_Tas#107.
3. Multizone fixture: HungaryHouse (`HungaryHouse.json`, sha256 `e16ccbd2…`) is machine-local and not committed.
   Its provenance and redistribution permission are not established; do not commit it as a fixture until the owner
   confirms. S05 used SAM_OpenStudio's committed two-cell model
   (`tests/resources/models/rawModel-WindowsWeatherDataDDYAdiabaticAirPanel.sam`, provenance in that folder's README).
   Resolved for S08/S09: the owner signed off the committed synthetic `s08-fix-box` V2 (`45524172...`) and
   `s08-fix-two` V1/V1 (`20da9b58...`) fixtures on 2026-10-21 (SAM_Tas#106). HungaryHouse remains outside this acceptance.
   The box and one multizone model remain the required equivalence fixtures; the criteria a fixture must meet are in
   [SIMULATION_INPUT_MAPPING_S07.md](SIMULATION_INPUT_MAPPING_S07.md#8-owner-decisions) (D-FIX).
4. Whether the trackers SAM#206 and SAM_UI#227 are updated to point here (not done by S00).
5. ~~F2, F8 and F9 semantics (S07), and the S07 tolerances (SAM#206 says tolerances are owner-confirmed).~~ Decided by
   the owner on 2026-10-20 (S07 plan revision 3) and recorded in
   [SIMULATION_INPUT_MAPPING_S07.md](SIMULATION_INPUT_MAPPING_S07.md). The contract Tas route is Direct T3D with
   `UseWidths = false`; production stays gbXML, and switching it is a later, separately approved task (decision 7).
6. Decided 2026-10-20: `SAM_COMMIT` remains a temporary requirement for developer-built producers in Both runs until
   S06 (stamped producers). A value set by hand must be the SAM commit the producers were built from; it does not prove
   packaged-app provenance. S06 outcome (owner decision 2026-10-20): an installed build needs no `SAM_COMMIT`, but its
   producers report the **SAM_Deploy build revision** in `samCommit` and `runnerCommit`. This is accepted as a documented
   limitation, not PASS, and no new stamping was added. The repository revisions come from that SAM_Deploy commit's gitlinks
   (`git ls-tree <revision> SAM SAM_Tas SAM_OpenStudio`). Existing developer records keep their meaning (SAM_Validation#16,
   `docs/benchmark/SCHEMA.md`).
7. **Tas production default: Direct.** Owner, 2026-10-20, during the S07 review:
   - First approval: "Change the Tas production default to SAM → T3D → TBD, T3DRoute.Direct, UseWidths=false."
   - Revised later the same day: "Record Direct T3D with UseWidths=true as the owner-preferred candidate default." Before
     production defaults change, it must be verified independently that its native areas and volumes match the
     intended panel convention on the approved fixtures. `UseWidths=false` stays an explicit option.
   - **Not implemented.** This records the intended default; it authorises no code change in S07 and does not start S08.
     Today `WorkflowSettings.T3DRoute` is `GbXML`, SAM_UI's `DirectT3D` is false and the Both producer builds gbXML; they
     stay so until a separate integration PR lands. That PR needs its own owner start; it is not part of S08.
   - Fallback direction (owner): if candidate validation fails, the proposed widths setting reverts to `false` and the
     reason is reported. This is not authorisation for a silent or automatic runtime fallback.
   - Panel convention: recorded measurements establish how widths change native geometry. They do not independently
     establish the source model's panel convention. SAM supports mid-surface panels, including PanelsBySolid output
     carrying PhysicalThickness. The fixture's provenance and declared convention determine whether width offsets are
     intended. The measured single-model figures (widths ON 11.9 % below widths OFF over 9 spaces) keep their original
     scope ([SIMULATION_INPUT_MAPPING_S07.md](SIMULATION_INPUT_MAPPING_S07.md#1-contract-route-and-current-production-behaviour-d-route) §1).
     The widths-ON reference is §4.1 of that document; its open decisions are OD-W1 and OD-W2.
   - **Acceptance criteria for the integration PR:**
     - the widths-ON candidate passes the §4.1 verification (native floor areas, surface areas and volumes against the
       reference for each fixture's declared panel convention) on the owner-approved equivalence fixtures, within the
       existing S07 tolerances, before any default changes. If it does not, the proposed widths setting reverts to
       `false` and the integration evidence states the reason;
     - new and unconfigured Tas workflows then default to Direct with the verified widths setting, consistently through
       SAM_Tas, SAM_UI and the Both producer;
     - `UseWidths=false` and an explicit gbXML selection stay available and are honoured;
     - existing explicit saved route and widths choices are preserved;
     - missing, invalid and legacy serialized settings get an explicit, owner-reviewed migration policy before
       implementation. At the pins, both writers always serialize the route (`WorkflowSettings` `T3DRoute`;
       SAM_UI `DirectT3D`), so a saved default cannot be told apart from an explicit choice. A missing or unrecognised
       `T3DRoute` reads as `GbXML`;
     - any fallback (route or widths) is reported in the run with its reason and the actual setting, and is never
       applied silently;
     - the actual route and widths setting are identifiable in the run evidence (S03 manifest);
     - targeted settings and serialization tests, plus a licensed acceptance, show the selected route and S07 input
       fidelity;
     - no Tas/EnergyPlus numerical-agreement requirement, tolerance relaxation, ventilation reinterpretation or IZAM/TPD
       expansion.

## Task log

- **S00 (Done, 2026-10-19):** SAM#213 merged `b4e681485702b92abafc2b80f893065c6e62a822` (head `bc58f4e0`);
  SAM_UI#239 merged `d5aa34b29b9a01af76bb0dc457021b83cb5adc7a` (head `d57dab70`). Documentation only; no code,
  simulation, issue change or S01 start. This status update was committed directly to `sow/2026-Q4` with the
  post-merge closeout under a narrow owner exception ("Close S00 in the roadmap", 2026-10-19); later roadmap changes
  go through a reviewed PR.
- **S01 (Done, 2026-10-19):** SAM_Tas#103 merged `e483877c` (detection) and SAM_UI#240 merged `5b379191` (reporting).
- **S02 (Done, 2026-10-19):** SAM_UI#241 merged `49eebe13`. The Part O Hub simulations are not guarded (owner decision
  open in the SAM_UI closeout).
- **S03 (Done, 2026-10-20):** SAM_UI#242 merged `1e460bbe`. Every Tas, OpenStudio and Both manifest carries
  `sam-ui.run-provenance/1`; unknown items have reasons.
- **S04 (Done, 2026-10-20):** SAM_UI#243 merged `307cb0e3`. Snapshot and canonical hash stable across sessions and builds.
- **S05 (Done, 2026-10-20):** SAM_UI#245 merged `9acce829`; record `SAM_UI/documentation/S05-LicensedAcceptance.md`.
  All nine cases PASS from the real ribbon on a fresh build of S01-S04. The rebuild found that the laptop's local SAM build
  predated F1, so the earlier overnight evidence and the preflight (SAM_UI#244) used the pre-F1 EPW writer. The S05
  embedded-weather run exported exactly SAM#212's corrected EPW.
- **S06 (Done, 2026-10-20):** SAM_Deploy#66 merged `1657bb53c7b1d1a82f0c5d9c57c7d7a02705cd31`. Supporting PRs: SAM_OpenStudio#32 merged
  `a9429f7cea975a8f660bac4407779c3828e86627` (the OpenStudio CLI that matches the SDK is preferred) and SAM_Validation#16
  merged `e9a83df0c2e1162d0a22eb1e010a508044117e12` (installed-build revision fields documented). The record is
  `SAM_Deploy/DEPLOY_S06_SMOKE_CANDIDATE.md`, with evidence in `SAM_Deploy/evidence/s06-20261020/`.
  - **Payload:** self-contained Benchmark producers, gated by `installer.yml`; `BuildAll_Release` builds the solutions in
    their listed order; 24 gitlinks change (23 Q4 tips plus SAM_OpenStudio at #32's merge).
  - **Acceptance:** the hosted installer EXE (run #227, `717533c`) was genuinely installed into an isolated profile and
    uninstalled cleanly; the owner's installation was not touched. From it, Tas, OpenStudio and Both ran from the real
    ribbon on a licensed laptop: all PASS, numerically the same as S05. OpenStudio's raw comparison reports
    `exactMatch: false`; its only differences are 4 run-generated subsurface names, which map 1:1 through their
    host-panel references, with equal values. The SDK-matching 3.10 CLI was selected with OpenStudio 3.11 first on
    `PATH`; no `SAM_COMMIT` or `SAM_BENCHMARK_*` was set.
  - **Repinned build:** run #228 at `b3b8e9e` passed Validate, the producer gates and H12. Its installed payload
    equals #227's apart from expected stamps and debug symbols (0 drift), so the licensed evidence carries forward.
  - **Accepted limitation (owner, 2026-10-20):** provenance check 9 is Not PASS. Installed producers report the
    SAM_Deploy build revision as `samCommit`/`runnerCommit`; recover the repository revisions from that commit's
    gitlinks.
  - **Not covered:** Rhino, Grasshopper and Revit were installed but not started; explicit CLI-path override is
    unit-tested only; no release, tag or deployment.
- **S07 (Done, 2026-10-20):** [SAM#216](https://github.com/SAM-BIM/SAM/pull/216) merged `68b602571c90e21d0bbd70609cbe4fc884b97a53`
  (head `d11e92ad`), documentation only; closeout `c817804f`. The mapping contract is `SIMULATION_INPUT_MAPPING_S07.md`.
  Owner decisions D-ROUTE, D-F2, D-F8, D-F9, D-FIX, the film convention and every tolerance are approved; **OD-G1 to OD-G3
  (section 6.1) and OD-W1/OD-W2 (section 4.1) remain open.** Direct as the future Tas production default is owner decision 7
  and is not implemented.
- **S08 (In progress, 2026-10-20):** PR-A merged as [SAM_Tas#104](https://github.com/SAM-BIM/SAM_Tas/pull/104) `d08916ca578cf4ab13ec64503cde43cf118ffbbf`
  (three revisions, validated content `6e0b55e4`; closeout `3d16b5c7` in `SAM_Tas/PROJECT_PROGRESS.md`; record
  `SAM_Tas/Documentation/S08-PRA-InputEquivalenceCore-PR.md`). It is the licence-free, COM-free evaluation core
  (`benchmark/SAM.Analytical.InputEquivalence`): reader-result types, the S07 abs-or-rel rule and tolerance catalogue, the
  section 6 outcomes and rank gate with a strict PASS allow-list, independent expected values, boundary-geometry identity and the
  OD-G rules as an explicitly provisional, fail-closed rule set. **S08 is not complete:** PR-B (licensed native reader,
  calibration, alignment probe) and PR-C (fixtures and acceptance) are not started. They need the owner's answers on SAM_Tas
  write scope, fixtures (D-FIX), OD-G1 to OD-G3, OD-W1/OD-W2, the alignment probe and Sizing diff inside S08, and the
  leap-year policy. S09-S12 are Not started and Phase C stays frozen.
- **Roadmap commit (2026-10-20):** this S07 and S08 status update was committed directly to `sow/2026-Q4` under a narrow
  owner exception ("for SAM you can commit directly to branch sow/2026-Q4 without PR to update roadmap"), as for S00. It
  changes only the S07 and S08 rows, the summary line under the table, decision 2 and this log; later roadmap changes go
  through a reviewed PR unless the owner says otherwise.

- **Roadmap reconciliation (2026-10-23, narrow owner-authorised direct Q4 update):** the owner explicitly authorised
  updating this roadmap directly on `sow/2026-Q4` in chat. This documentation-only checkpoint updates the current S07-S09
  rows, summary and related authorisation/fixture decisions; dated earlier task-log entries remain historical snapshots.
  - **S08 complete:** PR-B SAM_Tas#105 merged `fb9041c18299a23d30b4443501fc47a2ee8ba8d1`; PR-C #106 merged
    `61eb0d37af62dc2142248ddea30f0b043797e75f`. The committed SAM_Tas closeout records licensed acceptance on
    Direct/UseWidths=false/Sizing=false/Simulate=false: box PASS 353/353, two-zone PASS 623/623, complete coverage,
    all 7 negatives FAIL, reader/writer controls unchanged. Fixture provenance and OD-G1 to OD-G3 were owner-approved.
    Sources: `SAM_Tas/PROJECT_PROGRESS.md` and `Documentation/S08-PRC-FixturesAcceptance-PR.md` in that repository.
  - **S09 in progress:** SAM_OpenStudio#33 is OPEN at `d26665bd2e397d011cd1dda382c3e93004e1c231`, base `7f216657`.
    Revision-12 code `8edf793a` implements approved S07 U1. Independent review found no blocking U1 defect;
    local Release x64 S09 120/120 and non-simulation benchmark 51/51 passed. Both fixtures PASS, with coverage
    319/319 and 592/592; unused-face optics remain visible as informational rows (105/216, differing 69/138).
    Final-head hosted Build `39934532092` and SPDX `39934532259` passed. Writer/importer 471 passed / 2 skipped is
    the implementation agent's recorded local evidence, not rerun by the reviewer. Record and evidence are in
    `SAM_OpenStudio/documentation/S09-OpenStudio-InputReadBack-PR.md` and its `revision-12` evidence folder.
  - **Next step:** refresh the S09 acceptance/negative pack (the main committed pack predates L1/U1), obtain the owner's
    merge decision, then add the known-merge-SHA OpenStudio closeout and reconcile this roadmap's S09 row again.
    This checkpoint does not accept S09 as closed, merge any PR, start S10, relax Phase B gates or unfreeze Phase C.
- **S09 completion (2026-10-23, narrow owner-authorised direct Q4 update):** SAM_OpenStudio#33 merged into
  `sow/2026-Q4` as `81acb8e794efa0f05f973e27f226b4d64c02c2e2` (approved head `39a6c90c24b31e82d26f44ed9fefaf0037d41726`,
  base `7f216657`); the docs-only closeout is `ec204cbb0dbfedbcd5ea18428d24382fd0429b87` in SAM_OpenStudio. This
  supersedes the "in progress" S09 entry above (kept as a historical snapshot).
  - **Evidence:** independent review accepted the code and final evidence. Acceptance pack at `d26665bd`
    (`SAM_OpenStudio/documentation/S09-OpenStudio-InputReadBack-evidence/acceptance-2026-10-23/`): both fixtures PASS
    (coverage 319/319 and 592/592), 21/21 negatives FAIL by intended mechanism, 24 packed reports match raw reports.
    Final-head hosted Build and SPDX green at `39a6c90`; local S09 120/120, benchmark 51/51. The acceptance run is local
    only.
  - **Scope:** scoped native-input conformance under S07 §5.2 (U1). Not simulation equivalence and not a Phase B PASS:
    the Phase B gates (S08-S11 PASS) are unchanged, S10-S12 have not started, Phase C stays frozen. The existing S08
    completion record above is unchanged.
- **S10 (Accepted with one declared validation limitation, 2026-10-23; narrow owner-authorised direct Q4 update):**
  [SAM_Tas#107](https://github.com/SAM-BIM/SAM_Tas/pull/107) merged into `sow/2026-Q4` as
  `1d61ab052157fac868ef7beeae53054ff6c75c66`. The approved head was `662977a6ace1fa0ca27e189c66080739ad575daa`, with base
  `c50e9169`. The docs-only closeout is `922044fa9ca35062979689f4e359ea85bee577dd` in SAM_Tas.
  - **Work:** B1a annual peaks, with F4-F6 fixed. An independent TSD/TBD reference and comparison, with 52 negative
    controls. A receipt-verified, stop-on-failure licensed runner.
  - **Evidence:** licensed attempt 2 at `C:\TasOut\s10\acc2` (machine-local); pack committed as
    `SAM_Tas/Documentation/S10-Results-evidence/`.
    - Box: PASS 59/59, with 52/52 negatives as intended.
    - Two-zone: 94 PASS / 0 FAIL / 1 BLOCKED, with 51/52 negatives as intended. The only miss is the unmutated control,
      which is blocked by the same row.
    - Final-head hosted Build and SPDX green; the independent reviews accepted the execution and the evidence.
  - **Declared limitation (owner-accepted 2026-10-23):** `SAM|FixB|Heating|designDayPeak.hourOfDay` stays BLOCKED, and
    S10-C9 stays unresolved for heating.
    - Cause: no independent signal places the heating design day in time. The heating gains are constant zero, the TBD
      heating dry bulb is constant, and FixB's load is not a full-day tie.
    - Still checked: FixB's design load, its native peak index, and its annual and B1a values all pass.
    - Unchanged: no code, tolerance, fixture or status was changed, and unsupported heating design-day hours still
      BLOCK in general.
  - **Scope:** not an unqualified full PASS. The Phase B gate text (S08-S11 PASS) is unchanged, and how this limitation
    is treated at Phase B acceptance is an owner decision at that gate. S11-S12 have not started; Phase C stays frozen.
    The source records are `SAM_Tas/PROJECT_PROGRESS.md` and `Documentation/S10-TasResultsRoundTrip-PR.md`.
