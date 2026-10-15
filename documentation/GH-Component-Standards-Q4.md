# SAM-BIM Grasshopper component engineering standard — Q4 (2026-10-15)

**Status:** implemented on `docs/gh-component-standards-q4`; **draft PR, not merged**; owner review
required. Documentation, tooling and agent instructions only.

- Repository: `SAM` (shared). Canonical standard for every SAM-BIM repository.
- PR: **SAM-BIM/SAM#204** → `sow/2026-Q4` (**draft**, not merged).
- Base: `sow/2026-Q4` at `6b12e2a65817556693ba408674211610f4a20dda`.
- **Validated content SHA: `7dd5a4a5659a7d30d9721ebba58d31021fc5fc77`** (standard, validator, AGENTS.md).
  This record is a docs-only commit on top; the PR number, branch head and hosted check results live in
  the live PR description (a record cannot contain its own commit SHA).
- Builds on merged work, which it reuses rather than redesigns:
  `SAM_Psychrometrics#11` (metadata pilot), `SAM#203` (shared rename-migration infrastructure, merged
  `a1d605cf`), `SAM_Psychrometrics#12` (the validated single rename). Also referenced: Unified
  Diagnostics Phases 1–3 (`SAM#201` merged `293eb645`, `SAM#202` merged `fca3d4d4`, and the OCCT phase
  in `SAM_OCCT`).

## Scope

One canonical specification, the agent-instruction hook, the PR record, and one small reusable
validator. No Grasshopper component, GUID, parameter, ordering, access, optionality, visibility,
default, geometry algorithm or updater behaviour is modified. `PROJECT_PROGRESS.md` is not changed on
this branch; its closeout follows merge. No other repository is touched and no CI is added.

## Work completed

1. **`docs/GH_COMPONENT_STANDARDS.md`** — the canonical standard, sections A–J (naming; input naming
   and the four distinct requiredness mechanisms; output naming; descriptions; Diagnostics; published
   compatibility; parameter migration; versioning; parameter identity and serialization; validation and
   acceptance), plus appendices for the evidence index, the measured fleet baseline, open decisions, the
   repository adoption plan and the enforcement/CI architecture. Every rule carries a status
   (`VALIDATED` / `ENFORCED` / `PROPOSED` / `OPEN` / `FROZEN LEGACY`) and a stable `GH-x#` id, so the
   document never presents a proposal as an implemented fact.
2. **`design/grasshopper-icons/tools/param_contract.py`** — the canonical shared metadata validator.
   Repository-agnostic: `--repo` / `--output` / `--check`, defaulting to this repository. Contains the
   validated pilot's naming and description rules and its scanner, inlined.
3. **`AGENTS.md`** — a new step 6 in "Before starting work" and a short "Grasshopper components" section
   that require reading the standard before changing a GH component, state the frozen facts, and give the
   validation command. It also records that the standard is product documentation (kept on a released
   promotion, unlike the internal agent files).
4. **This record.**

## Decisions and assumptions

- **The standard lives in the tracked `SAM` repository** (`docs/`), not in the untracked `SAM-BIM`
  parent directory and not in a per-repository copy. Other repositories point at it (Appendix 4 gives the
  exact snippet and a stable GitHub URL), matching the existing agent-instruction rollout pattern.
  `SAM/documentation/` is used for PR records; `SAM/docs/` is introduced for the durable product
  standard, as explicitly requested.
- **One shared validator, not one per repository.** It is repository-parameterised by `--repo`; the
  manifest (`param_contract.json`) is the only per-repository artifact. The `SAM_Psychrometrics` pilot
  copy stays as the pilot's vendored copy; consolidating it is a recorded follow-up (Appendix 3).
- **The scanner is inlined** in the validator. `SAM/design/grasshopper-icons/tools/inventory.py` is the
  older SAM-only generation with an incompatible API (`gh_projects`, `compile_removed`, `SKIP_DIRS` and
  `long_path` do not exist there) and the pilot's copy is the generalised one. Inlining keeps the
  validator a single portable artifact with no hidden dependency on whichever `inventory.py` happens to
  sit beside it. This was discovered by running it, not assumed.
- **A parameter-class discriminator was added.** The scanner keys on `ComponentGuid`, which also matches
  the `Goo…Param` classes (64 in `SAM`) and one non-abstract concrete base (`A_SAMAnalytical`). Without
  the discriminator the SAM numbers are wrong by 64 objects.
- **The implemented Diagnostics input name is `diagnosticDetail_`, not `_diagnosticDetail_`.** It is
  already published on `SAM.Analytical.CreatePanelsBySolids` / `CreateAperturesBySolids` and on the OCCT
  set, and asserted by `SAM_OCCT/Testing/SAM.OCCT.GrasshopperTests/DiagnosticsContractTests.cs`. The
  brief's `_diagnosticDetail_` is documented as the *proposal for new components* and the discrepancy is
  recorded as an owner decision (GH-E3 / Appendix 3 note 3). Nothing was renamed.
- **The versioning policy is the implemented one, not an invented one.** `Query.GetObsoleteSeverity`
  derives `Advisory`/`Standard`/`Breaking` from patch/minor/major; the observed Q4 practice is a patch
  bump for description corrections, appended optional parameters and alias-migrated renames, and minor
  or major reserved for genuine behavioural change. `ObsoleteSeverity` and `ParameterRenames` are
  documented as orthogonal, and `MinCompatibleVersion` as declared-but-unread.
- **No rule was invented to fill a gap.** Where the source evidence is silent or ambiguous, the document
  says so and marks it `OPEN`.

## Files changed

- `docs/GH_COMPONENT_STANDARDS.md` (new)
- `design/grasshopper-icons/tools/param_contract.py` (new)
- `AGENTS.md` (modified: step 6 and a "Grasshopper components" section)
- `documentation/GH-Component-Standards-Q4.md` (new, this record)

## Validation

Machine-local (Windows, Git for Windows; all commands from the repository root).

| Check | Command | Result |
|---|---|---|
| Whitespace / conflict markers | `git diff --check` | clean (exit 0) |
| Validator compiles | `python -m py_compile design/grasshopper-icons/tools/param_contract.py` | exit 0 |
| **Behaviour-preservation: the validator reproduces the merged pilot** | `python design/grasshopper-icons/tools/param_contract.py --repo ../SAM_Psychrometrics --check` | **`3 components checked (0 parameter classes skipped): OK`** (exit 0) |
| Validator default repo | `python design/grasshopper-icons/tools/param_contract.py --output <tmp>` | `460 components (64 parameter classes skipped)` |
| Fleet baseline scan (Appendix 2 of the standard) | `python design/grasshopper-icons/tools/param_contract.py --repo <repo> --output <tmp>` for each active GH repository | all exit 0; numbers in the standard |
| Encoding / line endings | byte inspection + `git ls-files --eol` | index LF for all three files; no BOM; no CR; `*.md` LF as required by `.gitattributes` |
| Markdown fences balanced | `docs/GH_COMPONENT_STANDARDS.md` | 6 fence markers (even) |
| File scope | `git status --short` before commit | only the three intended files |

The `--check` line is the important one: the shared copy produces **exactly** the result the merged
`SAM_Psychrometrics` pilot tooling produces against its committed manifest, which is what shows the
extraction is behaviour-preserving rather than a rewrite.

The Python used was the machine-local RhinoCode interpreter
(`%USERPROFILE%\.rhinocode\py313-rh9\python.exe`, Python 3.13.13). On this machine
`python`/`python3`/`py` on `PATH` are the Microsoft Store aliases and do not run; that must be
provisioned on another computer.

**Not run, and not claimed:** no `dotnet build` or `dotnet test` (no product code changed, so none
applies); no GitHub-hosted check locally; no Rhino canvas session.

## Unresolved issues, risks and blockers

- **Hosted CI has no metadata job.** None is added here (deliberately, per the task scope). Appendix 5
  records the feasibility evidence: pure standard-library Python, ~25 s for `SAM`, present on
  `ubuntu-latest` / `windows-2022`; the blockers are the per-repository baselines and the
  Rhino-dependency of the runtime tests.
- **The discriminator is a naming heuristic.** `Goo…Param` exclusion is by name plus base type; a
  stronger base-chain resolution to `GH_Component` is a recorded follow-up.
- **Three scanner copies now exist** (this canonical one, the pilot's, and SAM's older `inventory.py`).
  Deliberate and documented; consolidation is a follow-up and would touch the icon tooling, which is
  out of scope here.
- **`_diagnosticDetail_` vs `diagnosticDetail_`** and **`MinCompatibleVersion`** need owner decisions
  (Appendix 3).
- **The fleet baseline is a snapshot** produced by the validator at the content SHA above. Re-running it
  later is expected to differ wherever a repository has moved; it is a reference, not a freeze.
- The standard's proposed rules (`PROPOSED`) do not yet have enforcement behind them beyond the
  description-quality and naming checks; a reader must respect the status labels.

## Exact next step

1. Push `docs/gh-component-standards-q4` and open the draft PR into `sow/2026-Q4` (PR number and final
   head in the PR description).
2. Owner/architecture review of the standard, in particular the `OPEN` items: `_diagnosticDetail_`
   versus `diagnosticDetail_`, and the `MinCompatibleVersion` question.
3. After an owner decision, the follow-ups, each its own bounded task: promote the pilot's validator copy
   to the shared one; add a `--check` baseline for the first Wave-1 repository (Appendix 4); add the
   per-repository `AGENTS.md` snippet there.
4. Merge only after owner approval and passing final-head checks; then the post-merge `[skip ci]`
   `PROJECT_PROGRESS.md` closeout on `sow/2026-Q4` and guarded branch cleanup.

No other repository is started by this task, no naming exception is migrated, and the Diagnostics
redesign is not implemented.
