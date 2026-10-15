# SAM-BIM Grasshopper component engineering standard — Q4 (2026-10-15, corrected 2026-10-16)

**Status:** implemented on `docs/gh-component-standards-q4`; **draft PR, not merged**; owner review
required. Documentation, tooling, agent instructions and validator regression tests only.

- Repository: `SAM` (shared). Canonical standard for every SAM-BIM repository.
- PR: **SAM-BIM/SAM#204** → `sow/2026-Q4` (**draft**, not merged).
- Base: `sow/2026-Q4` at `6b12e2a65817556693ba408674211610f4a20dda`.
- **Original validated content SHA: `7dd5a4a5659a7d30d9721ebba58d31021fc5fc77`** (first submission).
- **Corrected content SHA: `5a6a617b7021ba155933910e3b0d3ed069d4ba28`** (this revision: validator,
  regression tests, standard, `AGENTS.md`). `d313c63f`, `77a25112`, `a5461a6f` and this record are
  record-only docs commits. The PR number, final branch head and hosted check results are also stated
  in the live PR description.
- Builds on merged work, reused rather than redesigned: `SAM_Psychrometrics#11` (metadata pilot),
  `SAM#203` (shared rename-migration infrastructure, merged `a1d605cf`), `SAM_Psychrometrics#12` (the
  validated single rename), and Unified Diagnostics Phases 1–3.

## Scope

One canonical specification, the agent-instruction hook, one shared validator, its regression tests and
this record. No Grasshopper component, GUID, parameter, ordering, access, optionality, visibility,
default, geometry algorithm or updater behaviour is modified, and no other repository is touched. No CI
job is added. `PROJECT_PROGRESS.md` is not changed on this branch; its closeout follows merge.

## Independent review of the first submission and how each finding was resolved

The review returned REQUEST CHANGES with four Major and three Minor findings. All seven are resolved.

### Major 1 — incomplete parameter extraction → **fixed; the reported case is now a test**

The first extractor required every parameter to be added by `result.Add(new GH_SAMParam(`. The reported
`SAMAnalyticalCreatePanelsBySolids` extraction produced seven unnamed inputs and no outputs. It now
extracts **all seven inputs and all four outputs**, with name, type, optionality, access, visibility and
registered default (`test_reported_create_panels_by_solids_case`).

Root cause and fix: a single-call-site regex was replaced by a statement-level walker
(`walk_statements`) plus a per-statement `new GH_SAMParam(...)` scan, so a parameter is recognised by
what it *is* (a `GH_SAMParam`) rather than by one surrounding call. The supported forms are listed in
the standard's Appendix 6 and each has a regression test:

| Pattern | Before | After |
|---|---|---|
| target-typed `new()` with a declared variable type | unnamed | type and attributes resolved |
| variable reuse / reassignment with `SetPersistentData` | lost | resolved per assignment |
| object initializer without constructor parentheses (`new Param_Boolean { … }`) | not recognised | resolved |
| collection expression `[ … ]`, `AddRange`, array initializer | no outputs | resolved in order |
| verbatim identifier `@boolean` used later as `boolean` | unresolved | resolved |
| bare declaration then assignment (`Param_String p;` … `p = new() { … };`) | unresolved | resolved |
| declarations inside `foreach` / `if` blocks | invisible | walked |
| expression-bodied properties (`=> [ … ]`) | wrong body taken | resolved |
| local parameter factory (`Number("_size_", "Size", 0.25)`) | unresolved | resolved via the factory body |
| parameter-manager registration and typed helpers | partially | resolved, incl. the 5-argument default form |
| `v.Optional = true` before registration | unresolved | attributed |
| inherited registration from an abstract base in the repository | unresolved | resolved |
| generic base closed by the subclass (`GH_SAMEnumComponent<ApertureType>`) | unresolved | resolved to `ApertureType` |

The `Goo…Param`/`System.Guid`/`new ("…")` scanner variants were also fixed: a `System.Guid` GUID or a
target-typed `new("…")` previously made a real component invisible (this is what caused SAM to drop
from 460 to 444 components during development).

### Major 2 — parameter type validation → **fixed; the type is in the contract**

The first `diff` compared name, nickname, optional, access, visibility and persistent default but **not**
the runtime type, so `Param_Number` → `Param_String` passed. Now:

- the type is a `FROZEN_FIELDS` member and is compared exactly
  (`test_type_change_fails_the_compatibility_check`, and the Psychrometrics pilot baseline with one type
  mutated → must fail);
- `Param_*` is `builtin`, `…Param` is `goo`, any other constructed type is `external` and is recorded in
  the manifest's `externalParameterTypes` with its component
  (`test_external_parameter_type_is_reported_not_unresolved`, using RhinoInside.Revit's `HostObject`);
- a type that cannot be identified makes the component `partial` and blocks the check
  (`test_unresolved_extraction_blocks_the_check`).

### Major 3 — discovery and classification → **fixed; ancestry plus reported exceptions**

- Classification is by **source ancestry**, not by name: a class is a component when its base chain
  reaches a known Grasshopper root through the repository's own classes, and a `…Param` class deriving
  from `GH_Param`/`GH_PersistentParam` is a parameter. `NewComputeParam : GH_SAMComponent` is a
  component; `GooThingParam` is a parameter and is listed with its ancestry reason
  (`test_a_component_named_like_a_parameter_is_a_component`,
  `test_a_parameter_class_is_not_a_component_and_is_reported`).
- A class whose chain leaves the repository at an **unknown** type is reported in
  `unclassifiedClasses`, never silently excluded (`test_unknown_ancestry_is_reported_not_silently_excluded`).
- Ancestry resolves across the whole repository, including non-Grasshopper projects — this is what
  resolved SAM_Revit's 30 Revit-derived components (base `RhinoInside.Revit.GH…` + `IGH_SAMComponent`).
- Project discovery follows **indirect** project references to a fixed point, and `.csproj` is parsed
  with `ElementTree`, so single- or double-quoted attributes both work; `<Compile Remove>` is honoured;
  an unparsable project is reported, not skipped silently
  (`test_indirect_project_reference_discovers_the_project`,
  `test_single_quoted_xml_attributes_are_supported`, `test_compile_removed_file_is_not_scanned`,
  `test_unparsable_csproj_is_reported`).
- Test projects are not scanned, and the manifest records that they were excluded and why.

### Major 4 — compatibility separated from new naming rules → **fixed**

- `--check` compares published metadata against the **approved baseline** only. `--inventory` (the
  default) performs no comparison and says so in its output ("NOT compatibility validation").
- Legitimate `list`/`tree` access is part of the frozen contract and is no longer treated as a problem
  (`test_list_and_tree_access_are_legitimate`).
- An existing nickname that differs from the name is valid: recorded as an advisory note, not a failure
  (`test_existing_nickname_is_valid_and_only_advisory`).
- New-interface naming rules are a **separate** finding with a registered exception list: a *new*
  deviation fails and a stale exception fails, but an existing registered deviation passes, so no
  existing component is forced to change its published contract
  (`test_new_naming_deviation_needs_an_approved_exception`, `test_stale_naming_exception_fails`).
- A code-side default is accepted without `SetPersistentData` and is only a naming-convention note
  (`test_code_side_default_does_not_require_set_persistent_data`).
- Read-only verification against SAM_GEM (1), SAM_BHoM (3) and SAM_Python (3): all components fully
  verified, no repository modified (`test_wave_one_repositories_are_fully_verified`).

### Minor 1 — versioning and manifest updates → **fixed by classification and documentation**

A version-only change now produces a `manifest` finding whose text says explicitly that an intended
component version bump is **not** a compatibility violation and that the manifest must be regenerated in
the same reviewed change (`test_version_only_change_is_a_manifest_refresh_not_a_compatibility_violation`).
The standard (GH-J0/GH-J1) documents the flow: the manifest diff is what a reviewer reads.

### Minor 2 — AGENTS.md asked for a check SAM cannot satisfy → **fixed**

SAM has no committed baseline, so the old instruction would have failed. `AGENTS.md` now gives the
baseline-free audit (`--repo .`, optionally `--output <file>`), states that a repository with an
approved manifest uses `--check`, and states that `partial`/`unresolved` is never a PASS.

### Minor 3 — enforcement claims → **fixed**

The standard no longer labels grammatical quality, SI-unit correctness or engineering semantics as
enforced. Section 0.1 is a new table that separates the static metadata check, the Rhino-hosted runtime
compatibility tests, and human engineering review; GH-D1/GH-D4/GH-D8, GH-A3/GH-A5 and GH-G7 were
re-labelled (`CHECKED` / `PROPOSED` / `PROCESS REQUIREMENT`).

## Files changed (this revision)

- `design/grasshopper-icons/tools/param_contract.py` — rewritten extraction, classification, comparison
  and reporting; `--check` / inventory / `--strict-descriptions` / `--show-conventions` / `--json`.
- `design/grasshopper-icons/tools/tests/test_param_contract.py` — new, 42 stdlib `unittest` tests.
- `docs/GH_COMPONENT_STANDARDS.md` — sections 0.1/0.2, C, D, I, J and Appendices 2–6 corrected.
- `AGENTS.md` — baseline-free audit versus approved-manifest check, and the fail-closed rule.

## Validation

Machine-local (Windows; Python 3.13.13 from the RhinoCode interpreter). All commands from the repository
root.

| Check | Command | Result |
|---|---|---|
| Validator + tests compile | `python -m py_compile design/grasshopper-icons/tools/param_contract.py design/grasshopper-icons/tools/tests/test_param_contract.py` | exit 0 |
| **Regression suite** | `python design/grasshopper-icons/tools/tests/test_param_contract.py` | **42 tests, OK, exit 0** |
| **Positive: the merged pilot still passes, with full verification** | `python design/grasshopper-icons/tools/param_contract.py --repo ../SAM_Psychrometrics --check` | `3 components discovered, 3 fully verified, 0 partial, 0 unresolved; 3 compared against the approved baseline` → `OK compatibility`, exit 0 |
| **Negative: a type change is caught** | regression `test_type_change_fails_the_compatibility_check`, `test_psychrometrics_pilot_type_change_is_detected` | exit 1, finding names the type |
| SAM audit (baseline-free) | `python design/grasshopper-icons/tools/param_contract.py --repo . --output <tmp>` | 460 components: 456 verified, 4 partial, 0 unresolved; 64 parameter classes excluded; 0 unclassified |
| Representative fleet | same command per repository (Appendix 2 of the standard) | see below |
| Whitespace | `git diff --check` | clean |
| Encoding / EOL | `git ls-files --eol` (after commit) | index LF, no BOM |

The `--check` line is the important one: the rewritten validator reproduces the merged pilot's committed
manifest exactly — the same 3 components, the same 6 inputs, the same 28 outputs, the same types,
optionality, access, visibility and persistent defaults — so the rewrite is behaviour-preserving for the
one repository that has an approved baseline.

### Hosted checks (GitHub)

Dispatched at the corrected content head (the repository's own checks; the PR-event runs are suppressed
because the head commit carries `[skip ci]`, the docs-only convention):

| Workflow | Result |
|---|---|
| Build (Windows) — `msbuild /t:Rebuild` Release | success |
| Test — `dotnet test SAM/SAM.Tests` Release | success, **3683 passed / 0 failed** |
| SPDX + Copyright header check | success (no `.cs` changed) |

Run ids for the final head are in the live PR description. **Not run, and not claimed:** no
Rhino canvas session; no GitHub PR-event check (see above).

### Verified versus unresolved extraction coverage (2026-10-16)

`verified` = the published contract was completely extracted. A `partial` component blocks a
compatibility PASS.

| Repository | Components | Verified | Partial | Unresolved | Parameter classes excluded |
|---|---|---|---|---|---|
| SAM | 460 | 456 | 4 | 0 | 64 |
| SAM_Tas_Grasshopper | 94 | 94 | 0 | 0 | 10 |
| SAM_Revit | 55 | 55 | 0 | 0 | 1 |
| SAM_OCCT | 36 | 36 | 0 | 0 | 1 |
| SAM_UI | 31 | 31 | 0 | 0 | 1 |
| SAM_Systems | 30 | 29 | 1 | 0 | 8 |
| SAM_Mollier | 24 | 24 | 0 | 0 | 4 |
| SAM_Topologic | 22 | 22 | 0 | 0 | 0 |
| SAM_SolarCalculator | 21 | 21 | 0 | 0 | 3 |
| SAM_Excel | 8 | 8 | 0 | 0 | 0 |
| SAM_LadybugTools | 7 | 7 | 0 | 0 | 0 |
| SAM_Solver | 7 | 7 | 0 | 0 | 0 |
| SAM_OpenStudio | 6 | 6 | 0 | 0 | 0 |
| SAM_Multitasker | 5 | 5 | 0 | 0 | 4 |
| SAM_Acoustic | 3 | 2 | 1 | 0 | 1 |
| SAM_BHoM | 3 | 3 | 0 | 0 | 0 |
| SAM_IFC | 3 | 3 | 0 | 0 | 1 |
| SAM_Psychrometrics | 3 | 3 | 0 | 0 | 0 |
| SAM_Python | 3 | 3 | 0 | 0 | 3 |
| SAM_gbXML | 2 | 2 | 0 | 0 | 0 |
| SAM_Origin | 2 | 2 | 0 | 0 | 0 |
| SAM_GEM | 1 | 1 | 0 | 0 | 0 |

The six `partial` components, with reasons (all in the manifest under `extraction.reasons`):

- `SAMAnalyticalFilterByBoundaryType`, `SAMAnalyticalFilterByPanelType` — parameter names generated by
  `foreach (… in Enum.GetValues(typeof(X)))`.
- `SAMCoreFilterByType`, `SAMCoreInspect`, `SAMAnalyticalSystemResults` — variable-parameter components
  registered through the parameter manager, where the insertable set is generated in code.
- `SAMAcousticCreateBandValues` — the parameter name comes from a loop variable.

**Only SAM_Psychrometrics has an approved baseline**, so it is the only repository for which `--check`
is meaningful (3 components compared). Everywhere else the numbers are an inventory, not compatibility
validation.

### Comparison with the existing Psychrometrics pilot

| Aspect | Pilot copy (`SAM_Psychrometrics`) | Canonical validator (this PR) |
|---|---|---|
| Components / inputs / outputs | 3 / 6 / 28 | identical — the pilot's committed manifest is reproduced exactly |
| Types compared | recorded but **not** compared | compared (Major 2) |
| Extraction completeness | not modelled; a miss was silent | `verified`/`partial`/`unresolved`; blocks the check |
| Component classification | name heuristic | ancestry, with unknown ancestry reported |
| `access != item` | a failure | frozen contract value, not a defect |
| New naming deviation | fails | still fails, via an explicit conventions finding |
| Description quality | fails | advisory by default (`--strict-descriptions` reproduces the pilot's stricter gate) |
| Project discovery | name/API heuristic | direct **and** indirect references, ElementTree |

The pilot copy stays as the pilot's vendored copy; promoting the canonical one into that repository is a
recorded follow-up (standard Appendix 3).

## Decisions and assumptions

- **Fail closed.** Incomplete extraction is reported and blocks a PASS; a truthful `partial` is preferred
  to a false PASS. This is enforced by tests that must fail.
- **The scanner stays inlined** in the validator (the repository-local `inventory.py` copies have
  diverged); consolidating them is a follow-up.
- **The scanner now accepts `System.Guid` and target-typed `new("…")` GUIDs** — a real component being
  invisible is worse than a slightly wider regex.
- **Local parameter factories and generic type-argument substitution are supported**, because 21
  SAM_SolarCalculator components and 25 SAM enum components depend on them; both are bounded analyses
  with tests, not a general C# parser.
- **Enumeration-loop names and manager-registered variable-parameter components are deliberately not
  supported** and are reported instead (6 components fleet-wide).
- **The versioning policy is the implemented one** (GH-H2), and `ObsoleteSeverity` is orthogonal to
  `ParameterRenames` (GH-H4).
- **`_diagnosticDetail_` vs `diagnosticDetail_`** and **`MinCompatibleVersion`** remain owner decisions.

## Unresolved issues, risks and blockers

- Six `partial` components (above) cannot be certified until resolved or explicitly accepted.
- No repository except SAM_Psychrometrics has a baseline, so no other repository can be gated yet.
- The naming-exception list and the 972 naming deviations in SAM are inventory data, not defects to fix
  in this PR.
- Rhino-hosted runtime tests remain local/headless; `SAM.Core.Grasshopper.Tests` is deliberately not in
  `SAM.sln`.
- Three scanner copies still exist (canonical, pilot, SAM's older `inventory.py`).

## Exact next step

1. Owner/architecture re-review of #204, in particular the six `partial` components and the two open
   decisions (`_diagnosticDetail_`, `MinCompatibleVersion`).
2. After approval: the follow-ups, each its own bounded task — promote the canonical validator into
   SAM_Psychrometrics, then add a first approved baseline for a Wave-1 repository (Appendix 4) with its
   `AGENTS.md` snippet.
3. Merge only after owner approval and passing final-head checks; then the post-merge `[skip ci]`
   `PROJECT_PROGRESS.md` closeout on `sow/2026-Q4` and guarded branch cleanup.

No other repository is started by this task, no naming exception is migrated, and the Diagnostics
redesign is not implemented.
