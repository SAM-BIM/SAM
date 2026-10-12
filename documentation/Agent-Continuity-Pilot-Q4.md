# Q4 agent continuity pilot — 2026-10-12

Status: implemented on `docs/agent-continuity-pilot-q4`; owner review required.
Base: `sow/2026-Q4` at `c080dd08`. No merge authorized by this task.

## Problem and scope

SAM's attached AGENTS.md and PROJECT_PROGRESS.md examples exactly match the
local checkout. The existing PR-record/post-merge-closeout convention is sound.
However, reading the whole progress history costs context, bootstrap status
can contradict later closeouts, and the parent SAM-BIM-only policy is local.
The parent CLAUDE.md also affects whether Claude loads repository AGENTS.md.

This two-repository pilot uses SAM and SAM_OCCT; other repositories are audited,
not mechanically edited. SAM's PR changes only AGENTS.md, a small CLAUDE.md
import and this record. PROJECT_PROGRESS.md is unchanged pending merge.

## Decisions and implementation

- Keep AGENTS.md as the shared instruction source. CLAUDE.md imports it with
  `@AGENTS.md`, without importing the progress history.
- Inspect progress headings, then latest dated Q4 closeout and task-relevant
  sections; consult old evidence when needed. Verify old status against Git
  and live PRs. No history is deleted, moved or rewritten.
- Persist SAM-BIM-only network targeting and preservation of attribution,
  legacy records and integrations inside the repository.
- Explicitly retain owner-reviewed feature PRs, frozen Q3, no internal agent
  files on master, and post-merge-only Q4 progress closeouts with known SHA
  and `[skip ci]`. Open work stays in its PR record and description.
- Require base/tested-head identity, local versus hosted validation, dependency
  order, reproducible evidence and machine-local prerequisites in handoffs.

## Organisation audit and evidence

43 immediate local Git checkouts and 43 live SAM-BIM remotes reviewed;
SAM_Solver_Upstream excluded. All configured local remote URLs target SAM-BIM.
25 repositories have AGENTS.md/progress pairs; 21 share the same normalized
instruction template. SAM, Deploy, Solver and Tas_Grasshopper have deliberate
variants. Deploy's released-tag base/gitlink rules and Tas_Grasshopper's
unrelated-lineage restriction must be preserved. Four Q4 repositories
(Acoustic, Origin, Python, Topologic) have no continuity pair. Fourteen other
checkouts have no Q4 branch; absence there does not justify adding files to master.
Reviewed continuity-file Git blob hashes all match the live Q4/default branch.
Original SAM_UI staged work and .github untracked scripts are preserved.

Progress-file findings: SAM 53,570 bytes; Tas 50,303; Deploy 97,373;
UI 113,671. 21 files still contain bootstrap "No Q4 product work has started"
text, sometimes beneath merged closeouts. Historical upstream-preservation
text occurs in 24 files; retain as dated evidence, never operational authority.
Tas_Grasshopper's PR8 carry-over instruction is stale against its recorded/live
merge; handle in a separate focused follow-up, preserving lineage restrictions.

Measured example: normalize CRLF to LF; concatenate all level 1–3 headings
and the leading latest icon closeout (up to `## Branch`). SAM progress input
falls from 53,491 to 2,699 characters (95.0%); OCCT from 16,547 to 2,317 (86.0%).
This is an icon-follow-up read-footprint example, not a universal task budget
or measured token/billing/latency improvement. Relevant earlier evidence adds
context. The pilot adds instructions rather than shortening them.

## Tool compatibility — official documentation checked 2026-10-12

- [Codex](https://learn.chatgpt.com/docs/agent-configuration/agents-md):
  project instruction discovery starts at Git root; repository policy must travel
  with the clone. Progress is a tool-read file, not an instruction import.
- [Claude Code](https://code.claude.com/docs/en/memory): current versions can
  load AGENTS.md directly, but a CLAUDE.md in/above the project changes the
  default. `@AGENTS.md` supports this layout and older versions. Prefer the
  import to Windows symlinks. Check actual session context on installed versions.
- [Cursor](https://cursor.com/docs/rules): root/nested AGENTS.md supported.
  No duplicate rule set is needed. Do not assume Claude import syntax is a
  Cursor preprocessor; AGENTS.md itself carries the shared rules.
- [Hermes](https://hermes-agent.nousresearch.com/docs/user-guide/features/context-files):
  AGENTS.md discovery follows the Git-root directory chain; higher-priority
  Hermes/override files can shadow it. No additional Hermes-specific file needed.

## Validation, limitations and next step

Local diff/whitespace checks pass; only the three documentation files change.
Progress, frozen Q3, master, production, workflows, attribution and integration
references are unchanged. No product build/test was run for this docs-only change.
Hosted checks are tracked by the live PR at its final head; not implied by local checks.
No agent-client execution benchmark has been run. On each installed client,
start a fresh Q4 checkout session and verify it identifies the branch/closeout
rules, SAM-BIM-only scope and task-specific evidence without reading full history.
Record actual input/cache tokens and handoff correctness before expanding rollout.

Next: owner reviews this pilot and its SAM_OCCT companion. After approved merge,
add the known-SHA Q4 closeout using the existing docs-only `[skip ci]` exception;
label superseded bootstrap status in the touched stream only. Do not merge these
PRs, change other streams, archive histories or roll out templates automatically.
