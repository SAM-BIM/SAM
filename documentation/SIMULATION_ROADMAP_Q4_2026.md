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
| S00 | Roadmap and PR #239 evidence consolidation | SAM, SAM_UI | This file; reconcile the SAM_UI#239 record and description (supersede pre-F1 embedded-weather evidence, freeze C6-C8). Documentation only. | SAM#206, SAM_UI#227, SAM_UI#239, SAM#212 | Roadmap reviewed by the owner; #239 states what is superseded, frozen and kept, and links here. | Active |
| S01 | Tas silent-failure detection | SAM_Tas (detection), SAM_UI (reporting) | `Modify/Simulate.cs` (lines 82 and 144) discards the `TBDDocument.simulate` return code (-1 ran, 0 refused). A refusal still writes a small stub TSD, so the outcome is decided by `WaitToUnlock(path_TSD)`. Detect refusal and an empty or stub TSD, and carry the failure to Energy Simulation, Run Simulation and B1a. | SAM#92 (merged PR where the refusal was diagnosed) | A Tas refusal or empty TSD is reported as failed, with the Tas return code and log path, in every entry point and never as Success; a successful run is unchanged (persisted-value baseline comparison as in the #237 acceptance). | Next; SAM_Tas write access needs owner confirmation (see decisions) |
| S02 | Shared simulation concurrency guard | SAM_UI | `AnalyticalWindow` has separate `openStudioSimulationRunning` and `engineComparisonRunning` flags; Energy Simulation / Run Simulation > Tas (`SimulateTas`) has none. One guard for all simulation entry points. Not parallel execution. | SAM_UI#234 (design context only) | While any simulation runs for a model, no other simulation can start from any entry point; the guard is released on success, failure, exception and cancel (tests per entry point). | Not started |
| S03 | Run settings and version traceability | SAM_UI, SAM_Validation if the schema changes | Every run records the app commit (today `SAM_COMMIT` is set by hand), producer executable versions and hashes, Tas and OpenStudio/EnergyPlus versions, weather source (embedded or explicit, hash) and design-day source, and the run options in the run manifest. | SAM_UI#228 (G0 traceability), B1a provenance | Each Tas, OpenStudio and Both run manifest contains every item without manual environment variables; an unavailable item is written as unknown with a reason, never blank. | Not started |
| S04 | Stable canonical simulation snapshot | SAM_UI (snapshot), SAM_Validation (canonical hash) | Finding F7: the canonical model hash includes the UI-only `SAM.Analytical.UI` ParameterSet, whose GUIDs are regenerated on each load. | SAM_UI#239 F7 | The same saved model loaded twice gives the same canonical hash; an engineering edit changes it; same-run provenance unchanged. | Not started |
| S05 | Licensed Phase A GUI acceptance | SAM_UI (record only) | Licensed runs from the real UI on the S01-S04 content: Tas only, OpenStudio only and Both; box with embedded (post-F1) and explicit EPW; one multizone model; injected Tas refusal; cancel; concurrent-start attempt. | SAM_UI#228, SAM_UI#239 (prior evidence) | Each case reports PASS / FAIL / NOT RUN / BLOCKED; every reported status, output file and failure message matches what the engines did; no numerical agreement required. | Not started |
| S06 | Early SAM_Deploy smoke candidate | SAM_Deploy | Payload the Benchmark producers, SAM_Validation and the OpenStudio dependencies, so an installed candidate (not a developer build) can run the workflows. Avoids the stale `deps.json` state seen in #239. | SAM_UI 2026-10-18 closeout (producers not in BuildAll payload) | An installed candidate runs Tas, OpenStudio and Both on the box model on a licensed machine. Candidate build only; any release needs separate owner approval. | Not started |
| S07 | Minimal Phase B physical-input mapping | SAM (this documentation), SAM_Validation | The minimal set of physical inputs whose conversion is checked (areas, volumes, U and g, gains, setpoints, schedules, infiltration, ventilation, weather and design days), the native field each engine uses, and the tolerances. Records the owner decisions on F2 (ventilation semantics), F8 (window opening) and F9 (zone volume); does not implement them. | SAM#206 input-equivalence table, SAM#209, SAM_UI#232 | Owner-approved mapping table with tolerances and an owner decision recorded for F2, F8 and F9. | Not started |
| S08 | Tas independent native-input read-back | SAM_Tas, SAM_UI | Read the generated TBD independently of the SAM writer and produce the Tas input manifest for the S07 quantities. | SAM#208, SAM_UI#230 | For the box and the multizone model, every S07 quantity read back from the TBD equals SAM within the S07 tolerance or is listed as a mapping difference. | Not started |
| S09 | OpenStudio independent native-input read-back | SAM_OpenStudio, SAM_UI | Same for the generated OSM/IDF: the OpenStudio input manifest. | SAM_OpenStudio#30 | As S08, for OpenStudio. | Not started |
| S10 | Tas results round-trip validation | SAM_Tas | SAM-imported results and B1a against an independent TSD read. Includes F4 (1-based `peakHour`), F5 (governing instead of annual peak) and F6 (zero-peak hour). | SAM_UI#239 F4-F6 | Model and space results in SAM and B1a equal the TSD values; hour and peak definitions are documented; F4-F6 fixed or explicitly accepted. | Not started |
| S11 | OpenStudio results round-trip validation | SAM_OpenStudio | SAM-imported results and B1a against an independent SQL/ESO read; label total versus sensible cooling (F3). | SAM_UI#239 F3 | Model and space results equal the SQL/ESO values; metric definitions documented. | Not started |
| S12 | Final integrated release acceptance | SAM_Deploy, all producers | Phase A and Phase B evidence repeated on an installed release candidate. | All above | Final installation gate passes on a licensed machine. Release needs owner approval. | Not started |

No task is proven complete by existing evidence: #237/#238/#239 deliver the workflows (baseline above) but not S01-S04
behaviour, and no input manifest or independent result read-back exists yet.

## Acceptance gates

Gates are reported as **PASS / FAIL / NOT RUN / BLOCKED**. GitHub CI (build, unit tests, SPDX) is reported separately
from local licensed runs; a check blocked by a missing licence or executable is BLOCKED, never PASS.

- **Phase A gate:** S01-S04 merged with green final-head CI; S05 PASS for every case on a licensed machine; S06 PASS on
  an installed candidate. No case may report Success for a run that did not complete.
- **Phase B gate:** S07 approved; S08-S11 PASS on the box and one multizone model whose fixture provenance is cleared.
  Engine output differences are recorded, not gated.
- **Final installation gate (S12):** on an installed release candidate on a licensed machine, the S05 cases and the
  S08-S11 checks pass again; versions and hashes in the run manifests (S03) match the installed build.

## Owner decisions outstanding

1. Approval of this roadmap and of the Phase C freeze (S00).
2. SAM_Tas was recorded as read-only while the HVAC conversion is in progress (SAM_UI#239). Confirm whether S01, S08
   and S10 may change SAM_Tas now, or whether S01 starts after that work lands.
3. Multizone fixture: HungaryHouse (`HungaryHouse.json`, sha256 `e16ccbd2…`) is machine-local and not committed.
   Its provenance and redistribution permission are not established; do not commit it as a fixture until the owner
   confirms. Otherwise choose or create a multizone fixture for S05 and Phase B.
4. Whether the trackers SAM#206 and SAM_UI#227 are updated to point here (not done by S00).
5. F2, F8 and F9 semantics (S07), and the S07 tolerances (SAM#206 says tolerances are owner-confirmed).

## S00 record (remove or condense when S00 merges)

- **Branch/base:** `docs/simulation-roadmap-q4` from `sow/2026-Q4` `a2069103`. Companion change: SAM_UI#239
  (`validation/licensed-tas-openstudio-e2e-q4`) record and description reconciled to this roadmap.
- **Inspected:** SAM#206 and its checklist comments, SAM_UI#227, SAM_UI#228-#239 states (#237/#238 merged, #239 open,
  the others open), SAM#207-#212 (#212 merged), SAM_OpenStudio#30 (open), SAM and SAM_UI `PROJECT_PROGRESS.md` latest
  Q4 closeouts, the SAM_UI#239 record and description, the code references cited in the register.
- **Not done (out of scope):** no code, simulation, issue creation or change, merge or S01 start.
- **Next step:** owner review of this PR and SAM_UI#239. After approval and merge, the owner decides whether S01 starts.
