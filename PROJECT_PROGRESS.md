# Project Progress - SAM (2026-Q4)

## Branch

`sow/2026-Q4` - bootstrapped 2026-10-06 from `master` `56a70592`. Frozen Q3 record: `sow/2026-Q3` @ `3fcd64bf` (not modified).

## Last updated

2026-10-06 (Q4 operational cleanup).

## Current status

Q4 branch cut from `master` `56a70592`, which is the exact commit pinned in SAM_Deploy's frozen Q3 baseline (`v20261006.1`). Bootstrap added only internal docs (this file, `AGENTS.md`). No product source changed. No Q4 product work has started.

## Q4 priorities

Not yet set by the owner. Record them here at the first Q4 planning pass. Known carry-over work is listed below.

## Known carry-over work

- **SAM Grasshopper icon redesign - PR #166** (`feature/sam-gh-icon-redesign` @ `cf4d924a`, open, base `sow/2026-Q3`, not merged). Analysed 2026-10-06: the branch carries only its own 6 icon-only commits (`f57b54a`, `a7d65b0`, `55745b8`, `7300a7d`, `be024ba`, `cf4d924`) on top of Q3 commit `bc85ba61`. Those commits are not reachable from `sow/2026-Q4` (Q4 is built on the promoted `master` line), so a plain retarget would list 495 commits. Replaying exactly those commits onto `sow/2026-Q4` @ `e29cb86e` is conflict-free (verified commit-by-commit with `git merge-tree`; identical to the net-diff merge). Planned action: rebase-onto Q4 as a new branch + PR, then close this one; owner-approved controlled task, not yet executed.
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
