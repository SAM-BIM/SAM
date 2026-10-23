# SAM-BIM simulation session handoff

Persistent, concise handoff between sessions for the Q4 2026 simulation work. The approved scope is
[SIMULATION_ROADMAP_Q4_2026.md](SIMULATION_ROADMAP_Q4_2026.md); this file records progress and outstanding work only.
It authorises nothing. Update entries in place; do not append session transcripts.

**Last updated:** 2026-10-23 (handoff established after S09 completion; S10 readiness review only)

Committed directly to `sow/2026-Q4` under the owner's narrow exception (2026-10-23).

## 1. Fixed product goal and restrictions

- Goal: a reliable SAM_UI workflow that runs Tas and OpenStudio/EnergyPlus from one SAM analytical model, imports
  both engines' results correctly and presents a trustworthy comparison. Finish and validate; do not expand architecture.
- Numerical Tas/EnergyPlus agreement is not an acceptance requirement. Phase C is frozen.
- One authorised stage at a time; each stage needs its own owner start, feature branch and Draft PR.
- Workflow: Draft PR -> local build/tests -> independent review -> Ready -> one final hosted CI -> explicit merge approval.
  Never merge automatically. Do not change CI, tolerances, UI or start experiments without authorisation.
- Target only `github.com/SAM-BIM/*`; never HoareLea. Follow each repository's `AGENTS.md`.

## 2. Current stage and authorisation

| Item | State |
|---|---|
| Current stage | **S10 Tas results round-trip validation: Next, NOT authorised** |
| Phase A | Gate met (one owner-accepted exception: S06 provenance labelling Not PASS) |
| Phase B | Not accepted. S08 and S09 PASS; S10 and S11 outstanding |
| Phase C | Frozen |

S10 needs a separate owner start, including SAM_Tas write scope (roadmap owner decision 2).

## 3. Completed stages and evidence

| Stage | Evidence |
|---|---|
| S00-S07 | Roadmap rows and task log (merge SHAs recorded there) |
| S08 | SAM_Tas#104 `d08916ca`, #105 `fb9041c1`, #106 `61eb0d37`; closeout `c50e9169`; box 353/353, two-zone 623/623 |
| S09 | SAM_OpenStudio#33 merged `81acb8e794efa0f05f973e27f226b4d64c02c2e2` (head `39a6c90c`); closeout `ec204cbb`; roadmap `38681f7f5c994eebf4e58eb05dca15c54a9cf3d7` |

S09 detail: both fixtures PASS scoped native-input conformance under S07 §5.2 U1 (coverage 319/319 and 592/592),
21/21 negatives FAIL by intended mechanism; final-head hosted Build and SPDX green at `39a6c90`; local S09 120/120,
benchmark 51/51. Pack: `SAM_OpenStudio/documentation/S09-OpenStudio-InputReadBack-evidence/acceptance-2026-10-23/`.
Not simulation equivalence and not a Phase B PASS.

## 4. Repository state (verified 2026-10-23, local = origin `sow/2026-Q4`)

| Repository | HEAD | Open PRs |
|---|---|---|
| SAM | `38681f7f` | none |
| SAM_Tas | `c50e9169` | none |
| SAM_OpenStudio | `ec204cbb` | none |
| SAM_UI | `45263ff0` | none |
| SAM_Validation | `2a3acfaf` | none |

No S10 branch or Draft PR exists.

## 5. Local build and test results

None this session (readiness review, documentation only). Last recorded: S09 local results in section 3.

## 6. Hosted CI

No S10 CI. Last authoritative final-head CI: SAM_OpenStudio#33 at `39a6c90` (Build and SPDX green).

## 7. S10 readiness, open blockers and known limitations

S10 scope (roadmap): SAM-imported Tas results and B1a against an independent TSD read; F4-F6 fixed or explicitly
accepted; hour and peak definitions documented. Defect descriptions:
`SAM_UI/documentation/Licensed-TasOpenStudio-Validation-Q4.md` (F4-F6, lines ~383-392).

Code check at SAM_Tas `c50e9169`:
- **F4 still present:** `benchmark/SAM.Analytical.Tas.Benchmark/Modify/ToBenchmark.cs:184-193` maps space `PeakHour`
  from `SpaceSimulationResultParameter.LoadIndex` (raw 1-based Tas index), while model hours are 0-based.
- **F5:** mapping unchanged since diagnosis (space `Load` taken as governing peak; per
  `Licensed-TasOpenStudio-Validation-Q4.md`); not re-verified in `Convert/ToSAM/Results.cs`.
- **F6:** the `Peak()` coupling (lines 231-239) treats a measured-zero peak with hour 0 as valid; zero-peak hour
  semantics still need a fix or explicit acceptance.
- Goldens encode the current mapping (`benchmark/SAM.Analytical.Tas.Benchmark.Tests/BenchmarkFixture.cs:190,197`);
  a fix changes Tas golden values.

Blockers before implementation:
1. Owner start of S10 with SAM_Tas write scope (SAM_Tas was recorded read-only during HVAC conversion).
2. Owner decision on F4-F6: fix (proposed: 0-based annual-peak hour from `AnnualPeak`, design values in `designLoad`,
   no hour for an all-zero profile) or explicit acceptance.
3. Independent TSD reader design: must not reuse the SAM import path being validated (as S08 did for TBD).
4. Licensed Tas machine for TSD generation and acceptance; fixtures are the signed-off S08 `s08-fix-box` V2 and
   `s08-fix-two` V1/V1 (HungaryHouse excluded). Tas availability on the current machine not verified this session.
5. Comparison tolerances for results are not defined in S07 (input contract); any result tolerance needs owner approval.

## 8. Pending independent review decisions

None open. S09 review closed with merge. S10 plan will need independent review before implementation.

## 9. Next recommended action (exactly one)

Owner decides whether to authorise S10 planning (Plan mode, no code): a written S10 plan covering the independent TSD
reader, F4-F6 decisions, result tolerances and fixtures, for GPT-6 review before any branch is created.
