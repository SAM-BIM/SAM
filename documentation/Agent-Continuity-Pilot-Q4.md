# Q4 agent continuity pilot — 2026-10-12

Status: implemented on `docs/agent-continuity-pilot-q4`; owner review required.
Base: `sow/2026-Q4` at `c080dd088cff1957f66cdb3327d7e54dd751b0f2`.
PR: [SAM#191](https://github.com/SAM-BIM/SAM/pull/191).
Companion: [SAM_OCCT#74](https://github.com/SAM-BIM/SAM_OCCT/pull/74) (independent; see below).
Locally validated instruction/content head: PENDING (recorded by the following record-only commit).
Superseded validated content heads: `6e94ae91e5d93b5ff3b4f17d072b88981c6128d1`, `65e7d68b48b1c04a470f29e75edb8126fed47a9a`.
The final PR head is recorded in the live PR description because committing that
SHA into this file would create another head. This record contains the immutable
tested content SHA and commands; only this record changes afterward. The final
docs diff is checked separately, and the post-merge closeout records the merge SHA.

## Problem and scope

Reading the whole progress history costs context, bootstrap status can
contradict later closeouts, and the parent SAM-BIM-only policy was held only in
a machine-local parent instruction file. The parent CLAUDE.md also affects
whether Claude loads repository AGENTS.md. The existing PR-record/post-merge-
closeout convention is sound and is kept.

This two-repository pilot uses SAM and SAM_OCCT; other repositories are audited,
not mechanically edited. SAM's PR changes only AGENTS.md, a small CLAUDE.md
import and this record. PROJECT_PROGRESS.md is unchanged pending merge.

## Decisions and implementation

- Keep AGENTS.md as the shared instruction source. CLAUDE.md imports it with
  `@AGENTS.md`, without importing the progress history.
- Inspect progress headings, then latest dated Q4 closeout and task-relevant
  sections; consult old evidence when needed. Verify old status against Git
  and live PRs. Classify entries by date, PR and base branch, not by the
  heading they sit under. No history is deleted, moved or rewritten.
- Persist SAM-BIM-only network targeting and preservation of attribution,
  legacy records and integrations inside the repository.
- Explicitly retain owner-reviewed feature PRs, frozen Q3, no internal agent
  files on master, and post-merge-only Q4 progress closeouts with known SHA
  and `[skip ci]`. Open work stays in its PR record and description.
- Owner-requested quarter-transition guidance: a reviewed product promotion
  excluding internal agent files and this record; Q4 closeouts finished before
  freezing Q4; Q1 bootstrapped from the approved product baseline carrying useful
  instructions and unresolved evidence; Q1 progress initialization only with
  explicit bootstrap authorization. No release, freeze or branch creation here.
- A release-promotion PR into master records its closeout on the outgoing
  `sow` branch, never on master.
- Require base/tested-head identity, local versus hosted validation, dependency
  order, reproducible evidence and machine-local prerequisites in handoffs.

## Known progress-file layout problem (not changed here)

In SAM `PROJECT_PROGRESS.md` at the base, line 229 opens
`# Historical record - 2026-Q3 (frozen)` ("Everything below describes Q3"), yet six
live Q4 closeouts dated 2026-10-08 sit beneath it: SAM#185 (GenOpt PR6), #186
(units/formatting), #187 (`SAM.Core.Optimisation`), #188 (model bindings), #189
(aperture Guid) and #190 (try every option). The `## Current status` bootstrap text
("No Q4 product work has started") is also superseded. AGENTS.md now warns that
section position is not a period marker. Per the post-merge-only rule this branch
does not edit the file. The correction (move those six entries verbatim above the
Q3 heading and label the bootstrap status; no content rewrite) touches other
streams' entries, which AGENTS.md otherwise forbids, and is not itself a closeout.
It therefore needs explicit owner authorization, and if authorized is made inside
this pilot's post-merge docs-only `[skip ci]` closeout commit on `sow/2026-Q4`,
which records that authorization. Without it, only this pilot's closeout is added.

## Organisation audit and evidence

Machine-local evidence: gathered 2026-10-12 on the owner's workstation from its
local checkouts, parent instruction files and owner-attached copies of SAM's
AGENTS.md/PROJECT_PROGRESS.md (which matched that checkout). These inputs are not
committed and cannot be reproduced from this repository; another computer needs
the same set of SAM-BIM checkouts to repeat the audit.

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

Progress-file findings (machine-local except SAM): SAM 53,570 bytes; Tas 50,303;
Deploy 97,373; UI 113,671. 21 files still contain bootstrap "No Q4 product work
has started" text, sometimes beneath merged closeouts. Historical
upstream-preservation text occurs in 24 files; retain as dated evidence, never
operational authority. Tas_Grasshopper's PR8 carry-over instruction is stale
against its recorded/live merge; handle in a separate focused follow-up,
preserving lineage restrictions.

Measured example: normalize CRLF to LF; join all level 1–3 headings with `\n`,
then `\n` and the leading latest icon closeout (lines before `## Branch`). SAM
progress input falls from 53,491 to 2,699 characters (95.0%); OCCT (measured in
the companion checkout) from 16,547 to 2,317 (86.0%). This is an
icon-follow-up read-footprint example, not a universal task budget or measured
token/billing/latency improvement. Relevant earlier evidence adds context. The
pilot adds instructions rather than shortening them.

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

## Companion PR independence

Owner-stated: SAM#191 and SAM_OCCT#74 are independent, with no merge-order
dependency; either may merge first, and each gets its own closeout on its own
base branch. Verified on the SAM side: this PR changes only AGENTS.md, CLAUDE.md
and this record, and refers to SAM_OCCT#74 only by link. SAM_OCCT#74's contents
were not inspected in the SAM#191 review session; its own record is authoritative.

## Validation, limitations and next step

Review disposition:
- Copilot (GitHub review on `e1c60ab`): asked for the final tested head in this
  record. Addressed by recording the immutable tested content SHA and commands
  here, with AGENTS.md describing the following record-only commit; the final head
  is in the PR description. The Copilot re-review requested on `779a5fc` failed
  before reviewing (HTTP 402, monthly Copilot quota exceeded); the owner then
  directed Claude to act as reviewer instead.
- Kimi: review supplied externally by the owner (not posted on GitHub, so not
  verifiable from GitHub state); it also asked for reproducible commands and clear
  tested-head identity, handled as above.
- Claude review of `d0f750b` (2026-10-12): flagged the progress-file layout,
  release-closeout location, unlabelled machine-local evidence, the Kimi
  provenance, exit-code capture and companion order. Addressed in `6e94ae9`.
- Claude review of `779a5fc` (2026-10-12, acting reviewer): the layout-correction
  route conflicted with the direct-Q4 and other-stream rules; companion claims
  exceeded what was inspected; the Bash equivalent was not runnable; AGENTS.md
  called the pilot record "this". Addressed in the content head recorded above.
Verified live master: no AGENTS.md, PROJECT_PROGRESS.md or CLAUDE.md in SAM;
no master cleanup is needed. OCCT's legacy CLAUDE.md is preserved separately.

Run from a checkout of this PR at its final head (PowerShell; Git and Python 3 on PATH):

```powershell
$baseRevision = 'c080dd088cff1957f66cdb3327d7e54dd751b0f2'
$testedRevision = 'PENDING'
$finalRevision = git rev-parse HEAD   # must equal the final head in the PR description
foreach ($revision in $testedRevision, $finalRevision) {
  git diff --check $baseRevision $revision; "check $revision exit=$LASTEXITCODE"
  git diff --name-only $baseRevision $revision
  git diff --quiet $baseRevision $revision -- PROJECT_PROGRESS.md; "progress $revision exit=$LASTEXITCODE"
}
git diff --name-only $testedRevision $finalRevision
python -c "import re,subprocess as s;t=s.run(['git','show','c080dd088cff1957f66cdb3327d7e54dd751b0f2:PROJECT_PROGRESS.md'],capture_output=True,check=True).stdout.decode('utf-8').replace('\r\n','\n');L=t.split('\n');h=[l for l in L if re.match(r'#{1,3} ',l)];p=L[:L.index('## Branch')];print(len(t),len('\n'.join(h))+1+len('\n'.join(p)))"
```

Bash equivalent (run the same `python` line as `python3`):

```bash
base=c080dd088cff1957f66cdb3327d7e54dd751b0f2
tested=PENDING
final=$(git rev-parse HEAD)   # must equal the final head in the PR description
for revision in "$tested" "$final"; do
  git diff --check "$base" "$revision"; echo "check $revision exit=$?"
  git diff --name-only "$base" "$revision"
  git diff --quiet "$base" "$revision" -- PROJECT_PROGRESS.md; echo "progress $revision exit=$?"
done
git diff --name-only "$tested" "$final"
```

Expected: every `exit=` is 0; both full-PR file lists contain only AGENTS.md,
CLAUDE.md and this record; the tested-to-final list contains only this record;
the measurement prints `53491 2699`. These are local documentation checks, not
product test or CI results.

Results: PENDING (recorded by the following record-only commit).

Progress, frozen Q3, master, production, workflows, attribution and integration
references are unchanged. No product build/test was run for this docs-only change.
Hosted checks are tracked by the live PR at its final head; not implied by local checks.
No agent-client execution benchmark has been run. On each installed client,
start a fresh Q4 checkout session and verify it identifies the branch/closeout
rules, SAM-BIM-only scope and task-specific evidence without reading full history.
Record actual input/cache tokens and handoff correctness before expanding rollout.

Next: owner reviews this pilot and its SAM_OCCT companion. After approved merge,
add the known-SHA Q4 closeout using the existing docs-only `[skip ci]` exception;
label superseded bootstrap status in the touched stream only. Correct the
progress-file layout above only with explicit owner authorization. Do not merge
before owner/code-owner approval, resolution of open review findings and green
final-head checks. Do not change other streams, archive histories or roll out
templates automatically.
