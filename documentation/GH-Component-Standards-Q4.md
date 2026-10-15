# GH component standards — PR #204 correctness repair

**Status:** repaired on `docs/gh-component-standards-q4`; PR **SAM-BIM/SAM#204** remains draft,
unmerged, awaiting independent re-review and owner approval. Base `sow/2026-Q4` at
`6b12e2a65817556693ba408674211610f4a20dda`.

- Prior reviewed head: `c2067f9a98ede0643a1d7ac0b6159c104745947c`.
- Validated repair content SHA: `692f35f642db4838c29cda8e820101e2fc90401c` (validator, tests and canonical standard).
- The following record-only commit preserves that tested content SHA. The final PR head and hosted
  checks are identified in the live PR description.
- No production component, migration/updater implementation, GUID, parameter, geometry algorithm,
  workflow or other repository source is changed. `PROJECT_PROGRESS.md` is unchanged: closeout only
  after an authorized merge. Four changed files: this record, `docs/GH_COMPONENT_STANDARDS.md`,
  `design/grasshopper-icons/tools/param_contract.py`, and its `tests/test_param_contract.py`.

## Reproduced before changing code

Sol's machine-local independent script was accessible and rerun at c2067f9a. Twenty cases returned
**10 unexpected compatibility acceptances**, harness exit **1**. In particular:

| Defect | Expected before repair | Observed before repair |
|---|---|---|
| RevitAlignWalls, four inputs/one output | unsupported extraction must refuse certification | zero/zero, verified; copied-source input rename returned OK/0 |
| VendorA.HostObject vs VendorB.HostObject | distinct type contract | both reduced to HostObject; OK/0 |
| GooValueParam<int> vs GooValueParam<string> | distinct closed types | both reduced to GooValueParam; OK/0 |
| Unknown class ancestry added | discovery failure | unclassified record ignored; OK/0 |
| Malformed project XML added | discovery failure | project error ignored; OK/0 |
| Pilot type-negative test | source present; exact type mutation rejected | wrote only a manifest; passed on removed components, not type comparison |

The other false passes concerned helper-returned outputs, whitespace around wrapper constructors,
declarative Optional mutation, persistent data after wrapping and multi-level registration inheritance.
The pre-repair JSON and original independent report remain machine-local; no such external file is
needed to run the committed regression suite.

## Architecture checkpoint and final boundary

Chosen: **strict, narrower Python certification**. Keep the existing stdlib inventory extractor, but
require a proven returned collection or supported straight-line manager registration before certifying
it. Reject unknown effects instead of expanding a general C# parser to raise coverage.

- Roslyn would provide syntax/symbol/project analysis for wider certification, but adds a .NET tool,
  dependencies and project-resolution policy. That is a separately reviewed subsystem.
- Runtime inspection is complementary and can see constructor effects and saved-definition behavior;
  it requires the relevant Grasshopper/Rhino/Revit hosts and dependencies. It is not a portable source gate.
- Existing alias migration from SAM#203 / Psychrometrics#12 is reused unchanged.

Important limits: this is a bounded source check, not arbitrary C#/MSBuild semantic verification.
Nontrivial Goo/third-party constructor defaults, factories, symbolic defaults, control flow, unsupported
returned expressions, unread external bases and ParamDefinition registrations block acceptance. Known
Grasshopper built-ins and empty local subclasses inheriting their defaults are the constructor subset.
Unresolved display identity/version metadata also cannot pass as matching null values.

## Corrections

1. **Extraction:** wrapper whitespace is supported; parameter object assignments are tracked in order,
   including Optional and persistent data after wrapping. Reassignment does not alter an earlier
   wrapped object. Unknown reassignment invalidates the binding. Returned collection shape is checked;
   helper, filtered and discarded collections cannot be certified. Inheritance follows multiple source
   bases to the declaring registration. Explicit empty sides are supported; absence of evidence is
   unresolved. RevitAlignWalls is deliberately **unresolved**, with a ParamDefinition reason, rather
   than implementing a new Revit extractor here. CreatePanelsBySolids still extracts 7 inputs/4 outputs
   but is **partial** because its custom Goo constructor effects are not certified.
2. **Type identity:** preserve qualification and generic arguments; resolve supported aliases,
   primitives, local declarations and unambiguous imports. Unknown/ambiguous identity blocks PASS.
   Type changes still produce their precise compatibility findings. Schema **3** replaces lossy v2;
   v2 baselines require reviewed refresh. The approved schema-less pilot remains readable because its
   built-in types are reliably resolved.
3. **Discovery:** unknown objects and malformed XML are fatal extraction findings with paths and
   explicit counts. Ordinary helper classes referencing ComponentGuid are not mistaken for objects.
   Concrete base classes and same-line class declarations are no longer omitted by the old source filter.
4. **Regression evidence:** the pilot negative now copies actual component source, proves an unchanged
   baseline passes, changes only one Param_Number to Param_String, then asserts the exact parameter/type
   finding and absence of removed-component or extraction failures. New tests cover every independent
   case plus alias equivalence, constructor effects, discarded returns and uncertain component metadata.
5. **Documentation:** GH-D8 now describes advisory behavior and --strict-descriptions accurately;
   local automated Rhino tests are distinguished from static checks and human review. Adoption and
   coverage claims now reflect actual extraction limits.

## Reproducible validation (local Windows, Python 3.13.13)

Run from the SAM repository. The WindowsApps python alias is not the interpreter used. Set TEMP/TMP to
an existing writable directory outside tracked source if the sandbox's default temporary directory
cannot create/delete fixtures. `-B` prevents repository bytecode files.

```powershell
$pythonExe = 'C:\Users\michal\.rhinocode\py313-rh9\python.exe'
& $pythonExe -B design/grasshopper-icons/tools/tests/test_param_contract.py
& $pythonExe -B design/grasshopper-icons/tools/param_contract.py --repo ../SAM_Psychrometrics --check
# Inventory only; choose an output outside the target repository:
& $pythonExe -B design/grasshopper-icons/tools/param_contract.py --repo . --output "$env:TEMP\sam-pr204-inventory.json"
git diff --check
```

| Check | Result / exit code |
|---|---|
| Full committed validator regression suite | **69 tests passed**, no skips, exit **0** |
| Independent Sol script, unchanged file | **20 cases, zero unexpected acceptances**, exit **0** |
| Exact namespace and closed-generic source mutations | unchanged fixture passes **0**; changed type rejected **1**, type finding asserted |
| Optional and persistent-default changes | rejected **1** with the changed frozen field identified |
| Unknown ancestry / malformed XML | rejected **1**, discovery cause and path identified |
| Unsupported helper / Revit declaration | rejected **1**, extraction cause identified; no component-removal excuse |
| Missing / incomplete baseline | rejected **2 / 1** |
| Unchanged List/Tree, nickname and supported type spelling | pass **0** |
| Approved Psychrometrics baseline | **3 discovered / 3 verified / 3 compared; OK compatibility**, exit **0** |
| Whitespace and Python source syntax | exit **0** |

Committed tests are self-contained except clearly skipped read-only real-repository tests when sibling
checkouts are absent. The pilot mutation test uses actual copied source and a generated valid baseline;
the separate pilot comparison uses its already approved committed baseline. Synthetic static fixtures
are not claimed as compiled or hosted runtime tests.

Machine-local extra evidence directory (copy/provision it if needed elsewhere):
`C:\Users\michal\.codex\visualizations\2026\10\15\01a140c3-07d7-7db2-83db-a3f98397ac92\pr204-review\`.
It contains `before-repair-results.json`, `independent_review.py`, `independent-after-repair.txt`,
`results.json`, `repair-suite-final.txt`, `fleet-repair.txt`, `fleet/` and the BuildAll log. All critical
failure cases are also reproduced by the committed tests, so this directory is supplementary.

## Corrected coverage

22 named checkouts from the standard's Appendix 2: **826 components discovered, 73 verified, 709
partial, 44 unresolved**; **0 unclassified objects, 0 unparseable projects**. The complete per-repository
table is in `docs/GH_COMPONENT_STANDARDS.md`; its inventory command is rerunnable per checkout.

| Repository | Components | Verified | Partial | Unresolved |
|---|---|---|---|---|
| SAM | 460 | 37 | 423 | 0 |
| SAM_Revit | 55 | 9 | 13 | 33 |
| SAM_Psychrometrics | 3 | 3 | 0 | 0 |
| SAM_GEM | 1 | 0 | 1 | 0 |
| SAM_BHoM | 3 | 0 | 3 | 0 |
| SAM_Python | 3 | 0 | 3 | 0 |

The earlier **823/817** omitted Psychrometrics' three entries; the old table actually summed to
**826/820**. The old verified count was unsound because unknown contracts/effects were accepted.
The lower final count is intentional; running a scanner is not compatibility evidence.

The six originally documented gaps remain partial: SAMAnalyticalFilterByBoundaryType,
SAMAnalyticalFilterByPanelType, SAMCoreFilterByType, SAMCoreInspect, SAMAnalyticalSystemResults,
SAMAcousticCreateBandValues. They are now explicitly a subset of the unresolved coverage, not six
successful checks and not the complete limitation. Only the pilot currently has an approved baseline.

## Requested BuildAlls_v5 acceptance

Owner requested running the existing workspace driver after the repairs:

```powershell
& 'C:\Users\michal\Documents\GitHub\SAM-BIM\BuildAlls_v5.bat' nopause
```

Executed current branches, normal Debug Restore/Clean/Rebuild, no pull or branch switching.
**FAILED, exit 1**, duration **00:01:44.7517270**, stopped at SAM_Multitasker. Error:

```text
SAM_Multitasker/SAM_Multitasker/SAM.Core.Multitasker/Classes/Multitasker.cs(55,57): error CS0029:
Cannot implicitly convert ImmutableArray<Microsoft.CodeAnalysis.Diagnostic>
to ImmutableArray<SAM.Core.Diagnostic>
```

Source confirms `ImmutableArray<Diagnostic> immutableArray = script.Compile();` inside namespace
SAM.Core.Multitasker resolves Diagnostic to SAM.Core rather than the imported Roslyn type. This C#
source is untouched by #204. It needs a separately scoped SAM_Multitasker repair; no other repository
source was edited here. Consequently the full fleet build did not complete and is not green.
Normal build/deployment outputs were written by the owner-requested driver. No fresh Rhino canvas or
saved-definition compatibility test is claimed. BuildAll does not execute the Python validator.

## Hosted checks and remaining next step

Historical Build/Test/SPDX dispatches at c2067f9a passed (38780639246 / 38780644121 / 38780649528),
including 3683 C# tests. They do **not** verify this repair and do **not** run Python or Rhino migration
tests. Final repair-head check links/results belong in the live PR description; distinguish them from
local validator evidence above.

Next: independent re-review of this bounded certification approach and the committed negative tests,
then owner merge decision after final-head checks. Keep the draft PR unmerged. No new product decision
is required for these corrections; Diagnostics naming, MinCompatibleVersion, broader Roslyn/runtime
coverage and baseline adoption remain separate decisions. The workspace-wide build remains blocked by
the specific SAM_Multitasker C# error above. Do not broaden #204 to fix it.
