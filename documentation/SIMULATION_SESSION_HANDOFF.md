# SAM-BIM simulation session handoff

Persistent, concise handoff between sessions for the Q4 2026 simulation work. The approved scope is
[SIMULATION_ROADMAP_Q4_2026.md](SIMULATION_ROADMAP_Q4_2026.md); this file records progress and outstanding work only.
It authorises nothing. Update entries in place; do not append session transcripts.

**Last updated:** 2026-10-23 (S10 merged and accepted with one declared validation limitation)

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
| Current stage | **S11 OpenStudio results round-trip validation: Next, NOT authorised** |
| Last completed | S10, accepted with one declared validation limitation (not an unqualified full PASS) |
| Phase A | Gate met (one owner-accepted exception: S06 provenance labelling Not PASS) |
| Phase B | Not accepted. S08 and S09 PASS; S10 accepted with one declared limitation; S11 outstanding |
| Phase C | Frozen |

S11 needs a separate owner start. How S10's limitation is treated at Phase B acceptance is an owner decision at that gate;
the Phase B gate text is unchanged.

## 3. Completed stages and evidence

| Stage | Evidence |
|---|---|
| S00-S07 | Roadmap rows and task log (merge SHAs recorded there) |
| S08 | SAM_Tas#104 `d08916ca`, #105 `fb9041c1`, #106 `61eb0d37`; closeout `c50e9169`; box 353/353, two-zone 623/623 |
| S09 | SAM_OpenStudio#33 merged `81acb8e794efa0f05f973e27f226b4d64c02c2e2` (head `39a6c90c`); closeout `ec204cbb`; roadmap `38681f7f5c994eebf4e58eb05dca15c54a9cf3d7` |
| S10 | SAM_Tas#107 merged `1d61ab052157fac868ef7beeae53054ff6c75c66` (head `662977a6`); closeout `922044fa`; roadmap `4583e60e0a1a86596ba1b39a1fdb5ea9007773cc`; box 59/59, two-zone 94/0/1 |

S09 detail: both fixtures PASS scoped native-input conformance under S07 §5.2 U1 (coverage 319/319 and 592/592),
21/21 negatives FAIL by intended mechanism; final-head hosted Build and SPDX green at `39a6c90`; local S09 120/120,
benchmark 51/51. Pack: `SAM_OpenStudio/documentation/S09-OpenStudio-InputReadBack-evidence/acceptance-2026-10-23/`.
Not simulation equivalence and not a Phase B PASS.

S10 detail:
- **Work:**
  - B1a annual peaks, with F4-F6 fixed;
  - an independent TSD/TBD reference and comparison, which does not reuse the SAM import path, with 52 negative
    controls;
  - a receipt-verified licensed runner (`Build-S10-Receipt.ps1`, `Run-S10-Results.ps1`) that stops on infrastructure
    failure.
- **Licensed attempt 2:** run at `C:\TasOut\s10\acc2` (machine-local) on the signed-off S08 fixtures.
  - Box: PASS 59/59, with 52/52 negatives as intended.
  - Two-zone: 94 PASS / 0 FAIL / 1 BLOCKED, with 51/52 negatives as intended. The only miss is the unmutated control,
    which is blocked by the same row.
- **Final-head checks:** hosted Build and SPDX green at `662977a6`. Totals: producer 40, core 258, read-back 249, TM59
  1249, GenOpt 432.
- **Evidence and record:** pack `SAM_Tas/Documentation/S10-Results-evidence/` (with a README); record
  `SAM_Tas/Documentation/S10-TasResultsRoundTrip-PR.md`.

## 4. Repository state (verified 2026-10-23, origin `sow/2026-Q4`)

| Repository | HEAD | Open PRs |
|---|---|---|
| SAM | this handoff commit (parent `4583e60e`) | none |
| SAM_Tas | `922044fa` | none |
| SAM_OpenStudio | `ec204cbb` | none |
| SAM_UI | `45263ff0` | none |
| SAM_Validation | `2a3acfaf` | none |

No S11 branch or Draft PR exists.

## 5. Local build and test results

Last recorded (S10, SAM_Tas content `9871f437`): controlled receipt build exit 0; producer 40/40, core 258/258,
read-back 249/249. The full record is in the S10 PR record.

## 6. Hosted CI

Last authoritative final-head CI: SAM_Tas#107 at `662977a6`, Build `40010831380` and SPDX `40010831293`, both green.

## 7. Open limitations and known gaps

- **S10 declared validation limitation (owner-accepted 2026-10-23):** `SAM|FixB|Heating|designDayPeak.hourOfDay` on
  `s08-fix-two` stays BLOCKED, and S10-C9 stays unresolved for heating.
  - Cause: no independent signal places the heating design day in time. The heating gains are constant zero, the TBD
    heating dry bulb is constant, and FixB's load is not a full-day tie.
  - Still checked and passing: FixB's design load, its native peak index, and its annual and B1a values.
  - **This must stay visible in Phase B and release evidence (S12).** Closing it needs independent heating timing
    evidence and separately authorised work.
- **S10 other limits:**
  - the calendar design-day layout is measured on two fixtures only;
  - libraries from other repositories and third parties are pinned by hash in the build receipt, not source-bound;
  - `Pack-S10-Evidence.ps1` still uses `Get-FileHash`.
- **Follow-ups recorded in SAM_Tas:** the SAM_Validation `METRICS.md` Tas column; the `designDaySource` label; the
  OpenStudio zero-peak rule (S11).
- **S11 scope (roadmap):** SAM-imported OpenStudio results and B1a against an independent SQL/ESO read, labelling total
  versus sensible cooling (F3, SAM_UI#239). Like S10, it needs an independent reader, owner-approved result tolerances
  and the signed-off fixtures.

## 8. Pending independent review decisions

None open. S10 reviews closed with merge. An S11 plan will need independent review before implementation.

## 9. Next recommended action (exactly one)

Owner decides whether to authorise S11 planning (Plan mode, no code): a written S11 plan covering the independent
SQL/ESO reader, F3 labelling, result tolerances and fixtures, for independent review before any branch is created.
Recommended: Claude Opus 5.5, effort High, new session, Plan mode, because the reader independence and tolerances
need careful design.