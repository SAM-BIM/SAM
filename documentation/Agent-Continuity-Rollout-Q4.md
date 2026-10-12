# Agent instructions rollout — Q4 (2026-10-12)

Status: implemented on `docs/agent-instructions-rollout-q4`; owner review required. Not merged.
Kind: Follow-up to the merged pilot.
Base: `sow/2026-Q4` at `3514e9c4e3a038d817a57def75dc4523a35a87c3`.
PR: the live PR for this branch in `SAM-BIM/SAM` (its number is not committed here).
Tested content SHA: `3a18f2ebc8c55d03ce70b4fd0b309f7de02140ce` (changes only AGENTS.md).
Final PR head and hosted check results: recorded in the live PR description. A record
cannot contain its own commit SHA, so this file is the only change after the tested SHA.
Baseline: SAM-BIM/SAM#191 (merge `02bbafb8`, closeout `46e96b72`) and SAM-BIM/SAM_OCCT#74
(merge `3d0a4725`, closeout `c825a40`).

## Scope

Documentation only. No product code, build, workflow, dependency pin or tool changed.
`PROJECT_PROGRESS.md` is not changed on this branch; its closeout follows merge.

## Changes

- `AGENTS.md`: adds the agreed merge conditions, the guarded post-merge branch-cleanup rule (including dependency-branch resolution), the model/effort handoff rule, a release/promotion approval line and `preserve existing local changes`; generalizes the promotion-exclusion list to `Agent-Continuity-*-Q4.md`. Pilot text is otherwise unchanged. `CLAUDE.md` is not touched.

Shared rules added or confirmed in `AGENTS.md`: Git and committed records are authoritative; SAM-BIM-only
operations with validated URLs; heading-first, task-specific progress reads; feature branch + owner-reviewed PR
for everything except the post-merge `[skip ci]` progress closeout; tested-SHA, local-versus-hosted,
dependency-order and remaining-gap evidence; merge only after owner approval, final-head checks and resolved
findings; guarded post-merge cleanup of the task's branches (expected-SHA check, full-merge check, no open PR,
lease-guarded delete, no worktree deletion); frozen quarters; internal agent files stay off master promotions;
model/effort/session/mode on every recommendation.

## Preserved repository-specific rules

- Pilot rules kept: frozen Q3, no internal agent files on master, post-merge-only Q4 closeouts. The progress-file layout correction was already done in the pilot closeout; this PR does not touch `PROJECT_PROGRESS.md`.

## Validation

Local, documentation checks only (no product build or test was run, none applies):

```bash
base=3514e9c4e3a038d817a57def75dc4523a35a87c3
tested=3a18f2ebc8c55d03ce70b4fd0b309f7de02140ce
final=$(git rev-parse HEAD)   # must equal the final head in the PR description
for revision in "$tested" "$final"; do
  git diff --check "$base" "$revision"; echo "check $revision exit=$?"
  git diff --name-only "$base" "$revision"
  git diff --quiet "$base" "$revision" -- PROJECT_PROGRESS.md; echo "progress $revision exit=$?"
done
git diff --name-only "$tested" "$final"
git show "$final":CLAUDE.md | grep -n '^@AGENTS.md'
```

Expected: every `exit=` is 0; the full-PR file lists contain only AGENTS.md and this record
(at the tested SHA only AGENTS.md); the tested-to-final list contains only this record; the import
line is found. PowerShell works with `git rev-parse HEAD` and `if ($LASTEXITCODE -ne 0)` checks.

Result at the tested SHA (2026-10-12, Windows, Git for Windows): `git diff --check` exit=0;
`PROJECT_PROGRESS.md` unchanged exit=0; changed files: AGENTS.md; no CR, no BOM, final newline;
no unreplaced template tokens; `sow/2026-Q4` active, `sow/2026-Q3` frozen, no `sow/2026-Q2` reference.
The same checks at the final head and the hosted PR checks are reported in the PR description;
they are not asserted here. No agent-client token/behaviour benchmark was run for this rollout.

## Remaining decisions and next step

- Merge needs owner approval, passing final-head checks and resolved/accepted review findings. Code-owner review is required only for merges to `master` (owner decision recorded in the pilot closeouts).
- After merge: a docs-only `[skip ci]` closeout in `PROJECT_PROGRESS.md` on `sow/2026-Q4` with the merge SHA, then guarded cleanup of `docs/agent-instructions-rollout-q4` per `AGENTS.md`. Stale bootstrap wording already in `PROJECT_PROGRESS.md` is not edited by this PR; labelling it is a closeout matter that needs explicit owner authorization.
- Machine-local: the 29-repository rollout inventory, worktrees and cross-repository status table are in the
  local parent `PROJECT_PROGRESS.md` checkpoint (not Git-tracked). The committed cross-repository table is in
  SAM-BIM/SAM's `documentation/Agent-Continuity-Rollout-Q4.md` (section "Cross-repository rollout status").

## Cross-repository rollout status (2026-10-12)

One PR per repository on `docs/agent-instructions-rollout-q4` into `sow/2026-Q4`, all open and unmerged. Heads are the PR heads at creation; each PR description carries its own final head and hosted check results. This PR (SAM#193) lists its own head in its description because that head changes when this table is committed.

| Repository | PR | Base | Tested content | Head at creation | Kind / preserved special rule |
|---|---|---|---|---|---|
| SAM | [#193](https://github.com/SAM-BIM/SAM/pull/193) | `3514e9c4` | `3a18f2eb` | see PR description | pilot follow-up |
| SAM_Acoustic | [#12](https://github.com/SAM-BIM/SAM_Acoustic/pull/12) | `c1ae91d7` | `50ba3dc5` | `9dd5c731` | new AGENTS.md + CLAUDE.md; no PROJECT_PROGRESS.md (owner decision) |
| SAM_BHoM | [#12](https://github.com/SAM-BIM/SAM_BHoM/pull/12) | `c8392c8c` | `b6f1fb51` | `ae241d79` | template rollout |
| SAM_Deploy | [#63](https://github.com/SAM-BIM/SAM_Deploy/pull/63) | `0cbdaca7` | `c80875c8` | `76473b09` | gitlink + release controls, released-tag base |
| SAM_Excel | [#10](https://github.com/SAM-BIM/SAM_Excel/pull/10) | `e6bea8c7` | `de96fa20` | `4efb0283` | template rollout |
| SAM_gbXML | [#13](https://github.com/SAM-BIM/SAM_gbXML/pull/13) | `6408cdae` | `1cfb975d` | `acc762ba` | template rollout |
| SAM_GEM | [#12](https://github.com/SAM-BIM/SAM_GEM/pull/12) | `55e7b5ac` | `7cd24376` | `876ac29f` | template rollout |
| SAM_IFC | [#11](https://github.com/SAM-BIM/SAM_IFC/pull/11) | `a3f925b4` | `81bf8868` | `4afc55e1` | template rollout |
| SAM_LadybugTools | [#14](https://github.com/SAM-BIM/SAM_LadybugTools/pull/14) | `223ef21f` | `e54fae4f` | `addae8aa` | template rollout |
| SAM_Mollier | [#11](https://github.com/SAM-BIM/SAM_Mollier/pull/11) | `2b99d895` | `3a874134` | `3778580b` | template rollout |
| SAM_Multitasker | [#11](https://github.com/SAM-BIM/SAM_Multitasker/pull/11) | `eb8a68a2` | `86178807` | `2f821884` | template rollout |
| SAM_OCCT | [#75](https://github.com/SAM-BIM/SAM_OCCT/pull/75) | `c825a406` | `d7abc9be` | `e8b39284` | OCCT CLAUDE.md conventions kept; pilot follow-up |
| SAM_OpenStudio | [#27](https://github.com/SAM-BIM/SAM_OpenStudio/pull/27) | `3f5940e5` | `edb30215` | `4c24edf8` | template rollout |
| SAM_Origin | [#11](https://github.com/SAM-BIM/SAM_Origin/pull/11) | `54d96bc3` | `c6d9128f` | `8568ee0e` | new AGENTS.md + CLAUDE.md; no PROJECT_PROGRESS.md (owner decision) |
| SAM_Psychrometrics | [#10](https://github.com/SAM-BIM/SAM_Psychrometrics/pull/10) | `21c485e9` | `edb51ab3` | `7fef2ad4` | template rollout |
| SAM_Python | [#8](https://github.com/SAM-BIM/SAM_Python/pull/8) | `de2a98bc` | `88ebe762` | `e2252c9d` | new AGENTS.md + CLAUDE.md; no PROJECT_PROGRESS.md (owner decision) |
| SAM_Revit | [#23](https://github.com/SAM-BIM/SAM_Revit/pull/23) | `dacdf185` | `57f05034` | `15276a19` | template rollout |
| SAM_Revit_UI | [#28](https://github.com/SAM-BIM/SAM_Revit_UI/pull/28) | `53a31105` | `ae9842d2` | `c9c134dc` | template rollout |
| SAM_Rhino_UI | [#13](https://github.com/SAM-BIM/SAM_Rhino_UI/pull/13) | `1e943b35` | `805b3e92` | `d9b30657` | template rollout |
| SAM_SolarCalculator | [#31](https://github.com/SAM-BIM/SAM_SolarCalculator/pull/31) | `0e4a5ec8` | `82d6875b` | `8d46c0a3` | template rollout |
| SAM_Solver | [#17](https://github.com/SAM-BIM/SAM_Solver/pull/17) | `dc4e76ef` | `3a750b69` | `8fd4fd9f` | SAM_Solver_Upstream ignored |
| SAM_SQLite | [#6](https://github.com/SAM-BIM/SAM_SQLite/pull/6) | `b2642239` | `106e9592` | `fe40b301` | template rollout |
| SAM_Systems | [#39](https://github.com/SAM-BIM/SAM_Systems/pull/39) | `c0dc4281` | `45f180a7` | `875edf66` | template rollout |
| SAM_Tas | [#99](https://github.com/SAM-BIM/SAM_Tas/pull/99) | `347e9efe` | `9fa32480` | `297bd7c1` | template rollout |
| SAM_Tas_Grasshopper | [#14](https://github.com/SAM-BIM/SAM_Tas_Grasshopper/pull/14) | `a3d654df` | `03a563c6` | `9fe5a921` | contrib/sow-2026-Q3 protected; stale PR #8 text corrected |
| SAM_Topologic | [#10](https://github.com/SAM-BIM/SAM_Topologic/pull/10) | `d75653c9` | `baea9f60` | `517282ba` | new AGENTS.md + CLAUDE.md; no PROJECT_PROGRESS.md (owner decision) |
| SAM_UI | [#225](https://github.com/SAM-BIM/SAM_UI/pull/225) | `bd06c6e1` | `0f632240` | `507a6fdf` | template rollout |
| SAM_Validation | [#13](https://github.com/SAM-BIM/SAM_Validation/pull/13) | `b7d168dc` | `5fa1906e` | `3db112f6` | template rollout |
| SAM_Windows | [#14](https://github.com/SAM-BIM/SAM_Windows/pull/14) | `4f7dc060` | `89730dbd` | `3dc64ed3` | template rollout |

Excluded (no `sow/2026-Q4` base on the SAM-BIM remote, checked live 2026-10-12; no quarter bootstrap was invented, and `master`/default branches were not touched): SAM_AssemblyResolver, SAM_Building (Q3 equals master; failing icon work archived), SAM_Finance, SAM_GraphQL (master only), SAM_IAPWS, SAM_IoT, SAM_MachineLearning, SAM_SQL, SAM_Template, SAM_samples, plus the default-branch-only repositories sam-viewer (master), SAM.github.io, sam-bim.github.io and `.github` (main). Enabling any of them needs an owner-approved Q4 bootstrap.

Not repositories: `.icon-migration/work/*` and `.icon-migration/closeout/*` are Git worktrees of the repositories above, and `SAM`/`SAM_OCCT` also have Codex worktrees of the pilot branches; `_revitsnap` and `build_tests` contain no Git repository. All are preserved; no branch or worktree was deleted. This inventory of local checkouts is machine-local and must be re-derived on another computer (`git worktree list` in each repository).

Repository-specific rules preserved: SAM_Deploy released-tag base, `master` as the older Q2 line, gitlink/`.gitmodules` controls and release approval; SAM_Tas_Grasshopper unrelated `contrib/sow-2026-Q3` lineage (now also excluded from branch cleanup); SAM_Solver `SAM_Solver_Upstream` exclusion; SAM_OCCT `CLAUDE.md` conventions and legacy master `CLAUDE.md`.

Order: the 29 PRs are independent documentation changes with no dependency order. Merge each on its own owner approval; each gets its own post-merge closeout (or, for the four repositories without a progress file, the owner decision above) and guarded branch cleanup.
