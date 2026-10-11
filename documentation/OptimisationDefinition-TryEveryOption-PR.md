# SAM.Core.Optimisation and SAM.Math: the "try every option" method and runnable choice variables (Q4, native Optimisation PR6b) - SAM

Branch `feature/optimisation-try-every-option` -> base `sow/2026-Q4` (cut from `86b9dcd2`). Record date: 2026-10-08.

PR6b of the native Optimisation programme. Plan of record: SAM_UI `documentation/NativeOptimisation-Plan-ModelBindings.md`
("Follow-up: construction and glazing choice"). It follows the owner decisions of 2026-10-08 on SAM_Tas#90 (PR7a-2,
`SAM_Tas/SAM.Analytical.Tas.GenOpt/NATIVE_OPTIMISATION_PR7A2_GLAZING.md`, answers 4 and 5): the glazing target is a
**choice among real glazing systems, run by "try every option"**, and **this SAM PR comes first**; PR7b in SAM_Tas then
only adds capabilities and a mapping. Sequence: PR6b (this) → PR7b SAM_Tas → PR8 SAM_UI journey → PR9 Apply best
design → PR10 SAM_Deploy.

**SAM only.** No SAM_Tas, SAM_UI or Grasshopper change.

## Current status

PR open, **not merged**. Code, tests and evidence complete; awaiting PR CI and owner review. Do not start PR7b without
the owner.

## Why

PR7a-2 replayed the measured glazing results through the kernel: snapping a continuous g to the nearest system gives a
step function on which Hooke–Jeeves stopped on a plateau (option 5, 2 951.65 kWh, while option 1 gave 1 846.73 kWh);
trying every option is exact in n simulations and gives the whole table. Before this PR the schema had only golden
section and Hooke–Jeeves, every `"discrete"` variable was non-runnable (OPT415), and the kernel had no exhaustive
method.

## The definition (fixture `glazing-choice.json`, now runnable)

```json
"variables": [
  {
    "name": "Glazing",
    "description": "Glazing construction of the office windows",
    "type": "discrete",
    "minimum": 1,
    "maximum": 3,
    "target": {
      "kind": "tbd.glazing-construction.choice",
      "reference": { "glazingConstruction": "Office glazing" },
      "options": [ "Double low-e", "Triple low-e", "Double solar control" ]
    }
  }
],
"method": { "algorithm": "try-every-option" }
```

(Abridged; the canonical file puts every property on its own line. The kind and key names stay PR6 placeholders until
PR7b defines the real ones.)

## Decisions (the hand-over questions, answered)

| Question | Decision | Why |
|---|---|---|
| Method value | JSON `"try-every-option"`, enum `OptimisationAlgorithm.TryEveryOption` (appended), class `TryEveryOptionMethod` with **no settings**. Reader and writer round-trip it canonically (`{ "algorithm": "try-every-option" }`); old documents are unchanged. | The options are the whole search: there is no tolerance, start, step or order to set. The only limit, the number of simulations, already exists (`stopping.maximumSimulations`). A setting of another method in it is OPT105 "… is a setting of the golden-section method, not of this one." |
| How many variables | Stated by the engine: `OptimisationAlgorithmCapability(TryEveryOption, 1, 1)`, reported by the existing **OPT412** ("Try every option optimises exactly one design variable; 2 are defined."). | V1: a choice runs on its own. A later engine can raise the maximum (one run per option combination) without a schema change. |
| A continuous (or integer) variable with try every option | **OPT409** error at `$.variables[i].type`, no capabilities needed. | Try every option has no meaning on a range. |
| A choice with golden section or Hooke–Jeeves | **OPT409** error at `$.variables[i].type` ("Glazing is a choice ("discrete"), which Hooke–Jeeves cannot search: only try every option runs a choice."), no capabilities needed. | The PR7a-2 replay: an option number is not a continuous quantity; a line or pattern search lands on plateaus. |
| Numbering | PR6's rules are kept: options numbered 1..n (OPT615), OPT610–OPT614 unchanged. | — |
| A `"discrete"` variable **without** `options` | **Allowed, numbered the same way**: minimum 1, maximum a whole number (n). Otherwise **OPT615** ("… so its minimum must be 1 and its maximum a whole number; they are 0 and 3."). | One numbering rule for every choice, so the kernel maps any choice the same way (option k ↔ k) and named options can be added later without renumbering. It serves an engine without targets whose script maps the number (none lists `Discrete` today). A general whole-number range (for example 2..6 fans) stays the reserved `"integer"` type. On an engine with targets (PR7b) a choice still needs a target (OPT608) with at least two options (OPT611). |
| Limit on the number of options (owner default 8) | **An engine capability per target kind**: `OptimisationBindingCapability.MaximumOptions` (int?, only for a kind that `AcceptsOptions`; new 8-argument constructor, the 7-argument one kept). Reported by **OPT616** at `…target.options`: "Glazing construction choice takes at most 8 options with the Tas engine (each option is one simulation), and Glazing lists 9, so this definition cannot run." Hint "Remove 1 option." The AI text shows it per item: `options (at most 8): …`. | The limit belongs to what the engine offers for a kind (the glazing filter gives ≤ 8; a later construction choice may differ), next to `AcceptsOptions`, and the schema stays free of engine policy. A definition with more stays readable and editable. A choice without a target has no kind, so only the simulation limit applies to it. |
| Simulation limit | **OPT416** error at `$.stopping.maximumSimulations` when it is below the number of options ("Try every option needs one simulation per option: Glazing has 3 options, but the simulation limit is 2."). The kernel refuses the same input. | The method promises every option; stopping part-way would report a "best" of an incomplete table. A null limit means the engine default (2000). |
| `start` / `step` | **Optional, kept and not used**: info **OPT408** ("Try every option tries each option in turn; the start and step of “Glazing” are kept but not used."). A given start must still be within 1..n (OPT205). The fixture has neither. | An AI reply may add them out of habit (the variable rules list them); an error would reject a harmless value, as golden section already accepts them with OPT408. |
| OPT415 | Unchanged: an engine that does not list `Discrete` in `VariableTypes` gives OPT415. An engine opts in with `TryEveryOption` (1, 1) **and** `Discrete`; with only one of them, OPT411 or OPT415 remains. | — |
| AI text | Offers `"discrete"`, the choice rules and `"try-every-option"` **only when the engine runs both** (`RunsChoices`); otherwise none of them appears. Choice targets come from the catalogue with their options (copy exactly; a misspelt option is OPT614 "Did you mean"). | Only what the engine runs is offered (PR3 contract). |
| OPT412 hint | It now suggests only a method that suits the variables (try every option for choices, the others for values). | Otherwise two choices would be told "or choose Hooke–Jeeves". |

## The kernel: `SAM.Math.TryEveryOption`

A sealed `Optimiser` (the closed set gains a fourth member). A SAM addition with **no GenOpt parity claim**; it uses the
shared `EvaluationContext`, so the cache, numbering, retry rule, cancellation, trace and progress are the same code as
the other methods.

- **Order:** every whole number from `Minimum` to `Maximum` of exactly one parameter, ascending, one evaluation at a
  time (the evaluator is a Tas simulation). `Initial` and `Step` are ignored; no float rounding.
- **Table:** each option is one `OptimisationEvent.OptionEvaluated` entry (new, appended) in `Entries`, coordinates =
  option number, all outputs, simulation number = position. The result is the whole table; nothing new is needed on
  `OptimisationResult`.
- **Best:** after the last option, the lowest objective is reported as `MinimumPoint` in both listings
  (`Result.Minimum`, the only main iteration). A tie goes to the **lower option number**; **NaN never wins**; when no
  objective is a number no minimum is reported. This agrees with SAM_Tas' shared `NativeGenOptOutcome` (it takes
  `Minimum` when set; its own fallback rule is the same first-lowest, NaN-skipped rule).
- **Failure: stop, not skip** (the shared rule). The first option is not retried; a later failure is retried once with
  the same number; a second failure stops the run with `EvaluationFailed`, `FailedSimulation` = the option's position,
  the options evaluated so far in `Entries`, and **no minimum**. Why: the method's claim is the best of every option; a
  table with a hole cannot name one, and `NativeGenOptOutcome` already treats `EvaluationFailed` as "no best point". The
  window can still show the partial table and which option failed. A failing first option (the current glazing) means
  the setup is broken.
- **Cancellation:** checked before each option; `Cancelled`, the options so far, no minimum. An evaluator's own
  cancellation is not a failure and is not retried.
- **Cache:** the options are distinct points, so the cache never answers one: every option is a counted simulation.
- **Validation (throws before the first evaluation):** exactly one parameter; whole-number, finite `Minimum` ≤
  `Maximum` (|value| ≤ 2^53); `Maximum − Minimum + 1` ≤ `MaximumSimulations` ("Try every option needs one simulation
  per option: 5 options, but MaximumSimulations is 4.").

## Diagnostic codes added or extended

| Code | Layer | Meaning |
|---|---|---|
| OPT408 | method (info) | Extended: try every option does not use start/step. |
| OPT409 | method | New: try every option on a non-choice, or a choice with golden section / Hooke–Jeeves. |
| OPT412 | capability | Unchanged rule; message names "Try every option"; hint suggests only suitable methods. |
| OPT416 | method | New: the simulation limit is below the number of options. |
| OPT615 | meaning | Extended: a choice without named options is numbered from 1 to a whole number. |
| OPT616 | binding capability | New: more options than the kind's `MaximumOptions`. |

## For PR7b (SAM_Tas): only capabilities and a mapping

```csharp
new OptimisationAlgorithmCapability(OptimisationAlgorithm.TryEveryOption, 1, 1)        // with GoldenSection, HookeJeeves
new[] { DesignVariableType.Continuous, DesignVariableType.Discrete }                   // VariableTypes
new OptimisationBindingCapability("tbd.glazing-construction.choice", "Glazing system",
    OptimisationQuantity.Unspecified, null, new[] { glazingConstruction }, null, true, 8) // at most 8 options
```

Mapping: `TryEveryOptionMethod` → `new SAM.Math.TryEveryOption { MaximumSimulations = stopping ?? 2000 }` with one
`OptimisationParameter(name, 1, 1, n, 1)`; option k ↔ `target.Options[k − 1]`. SAM_Tas' `ToGenOptDocument` and
`ToSAM_Optimiser` (GenOpt-format route) need no change: they never see a try-every-option definition that is runnable on
`tas-script`.

## Compatibility

- `OptimisationAlgorithm.TryEveryOption` and `OptimisationEvent.OptionEvaluated` are appended (existing values keep
  their numbers). `DesignVariableType` is unchanged (comments only). The old `OptimisationBindingCapability` and
  `OptimisationCapabilities` constructors are kept.
- Every existing fixture except `glazing-choice.json` is byte-identical and keeps exactly its findings.
- SAM_Tas and SAM_UI compile and pass against this build unchanged (see Validation). SAM_UI's algorithm name helper
  falls back to the enum name for the new value (PR8 words it).

## Files changed

- `SAM/SAM.Math/`: new `Classes/Optimisation/TryEveryOption.cs`; `Classes/Optimisation/EvaluationContext.cs`
  (`ReportLowest`), `Classes/Optimisation/Optimiser.cs` (comment), `Enums/OptimisationEvent.cs` (`OptionEvaluated`),
  `Enums/OptimisationOutcome.cs` (comment).
- `SAM/SAM.Core.Optimisation/`: new `Classes/TryEveryOptionMethod.cs`; `Enums/OptimisationAlgorithm.cs`,
  `Enums/DesignVariableType.cs` (comment), `Classes/OptimisationNames.cs` (method names table),
  `Classes/OptimisationDefinitionReader.cs` (three methods), `Classes/OptimisationBindingCapability.cs`
  (`MaximumOptions`), `Query/Diagnostics.cs` (OPT408/409/412/416/615), `Query/BindingDiagnostics.cs` (OPT616),
  `Query/AIExchangeText.cs`, `Resources/sam.optimisation-1.schema.json` (`tryEveryOption`).
- `SAM/SAM.Tests/`: new `OptimisationTryEveryOptionTests.cs`; `Helpers/OptimisationFixtures.cs` (`TasModel(true)` runs
  a choice; glazing choice kind limited to 8; `Everything` runs try every option), the four `Optimisation*Tests.cs`
  suites, `Golden/Optimisation/glazing-choice.json` (now `"try-every-option"`, no start/step).
- This record.

## Validation

Local, 2026-10-08. All sibling repos fetched and at `origin/sow/2026-Q4`: SAM `86b9dcd2`, SAM_Tas `4abad64b`, SAM_UI
`96e20c9e`, SAM_Systems `5404926`.

- **SAM:** `dotnet build SAM.sln -c Release` 0 errors (12 warnings, all pre-existing in the Grasshopper projects);
  `SAM.Core.Optimisation` rebuilt with **0 warnings**.
- **`SAM.Tests`** (built explicitly): full suite **3414/3414** (3357 before, +57). Optimisation suites 421 (364
  before).
  - Kernel (`OptimisationTryEveryOptionTests`, 20): order and the whole table (objectives, recorded outputs, simulation
    numbers, counters); minimum..maximum ignoring initial/step; a single option; ties to the lower option (3 cases);
    NaN never wins; no number → table kept, no minimum; first-option failure without retry; a later failure retried
    once and the table complete; two failures stop with the options so far and no best; exception / wrong output count
    / null; cancellation before, during and thrown by the evaluator; every option a counted simulation, never a cache
    hit; progress per entry; reuse; validation messages; a limit equal to the option count.
  - Reader: `"try-every-option"` read and normalised (`"Try every option"`, `"tryEveryOption"`, OPT113); another
    method's setting named (golden-section, hooke-jeeves); an unknown setting lists "algorithm"; OPT111/OPT110 list the
    new value; golden section still names the Hooke–Jeeves setting; OPT409 located at line 12, column 7.
  - Writer: the method is written as its algorithm only and reads back; copy and `Clone`; the fixture is canonical and
    idempotent.
  - Diagnostics: OPT409 both directions (continuous, integer, golden section, Hooke–Jeeves), OPT412 with a second
    choice and with a continuous second variable, OPT412 hint, OPT408 info, OPT205 for a start outside 1..n, OPT416 at
    and below the limit and for a choice without named options, OPT615 without options (0..3, 1..3.5; 1..1 is only
    OPT204), OPT616 at 8 / 9 / 10 options, `MaximumOptions` only on a choice kind, OPT415 and OPT411 for engines with
    only one of `Discrete` / `TryEveryOption`; the choice fixture runnable on `TasModel(true)` with the catalogue and
    OPT411 + OPT415 on `TasModel()`.
  - AI text: the choice and `"discrete"` and `"try-every-option"` only when the engine runs both (three engines without);
    the exact rule lines and the `options (at most 8)` entry; no limit text without a limit; an AI reply copying the
    offered choice is runnable, a misspelt option is OPT614 with "Did you mean", Hooke–Jeeves on it is OPT409; the
    choice embedded in the text reads back unchanged; the schema has `tryEveryOption`, three method `oneOf` entries and
    the reader's algorithm texts.
- **Mutations** (each applied, built, run against the optimisation suites, then reverted; all caught):

  | Mutation | Tests failed |
  |---|---|
  | M1 kernel tie goes to the later option | 5 |
  | M2 kernel skips a failed option and continues | 3 |
  | M3 kernel tries the options in reverse order | 10 |
  | M4 kernel does not check the simulation limit | 1 |
  | M5 NaN can win | 2 |
  | M6 try every option accepts a continuous variable (OPT409) | 4 |
  | M7 a choice may use golden section / Hooke–Jeeves (OPT409) | 3 |
  | M8 simulation limit not checked (OPT416) | 2 |
  | M9 option limit made exclusive (OPT616 boundary) | 1 |
  | M10 AI text offers a choice without try-every-option | 1 |
  | M11 reader accepts other methods' settings in try-every-option | 4 |
  | M12 choice without options not numbered from 1 (OPT615) | 2 |
  | M13 OPT412 suggests any method | 1 |
  | M14 method text renamed (reader/writer vocabulary) | 33 |

- **SAM_Tas, unchanged, against this build:** `MSBuild SAM.Analytical.Tas.GenOpt.csproj -restore
  -p:Configuration=Release -p:BuildProjectReferences=false` 0 errors (scratch profile, real `NUGET_PACKAGES`);
  `SAM.Analytical.Tas.GenOpt.Tests` **257/257** (as PR6/PR7a-2; the 25 golden replays through the kernel unchanged).
- **SAM_UI, unchanged, against this build:** Release `SAM_UI.sln` 0 errors (scratch profile, real `NUGET_PACKAGES`);
  `TasOptimisation*` **121/121**, including the byte identity of its embedded `systems-demo-*.json` copies.
- `git diff --check` clean; the new JSON is LF; no fixture other than `glazing-choice.json` changed.

## Unresolved issues and risks

- **Versioning (open since PR3):** an older SAM reads `"try-every-option"` as OPT111 (unknown value). Acceptable while
  nothing is released; decide before `.samopt.json`.
- **`"integer"` stays reserved:** no engine runs it; the kernel could later run it with try every option or a mesh.
- **Kinds and keys are still placeholders** until PR7b (`tbd.glazing-construction.choice`, `glazingConstruction`).
- **Consumers of `OptimisationEvent`/`OptimisationAlgorithm`** (SAM_Tas, SAM_UI, Grasshopper) never see the new values
  until PR7b/PR8 use them; their switches have default branches.
- `SAM.Core.Optimisation` and the new `SAM.Math` are not deployed yet (PR10).

## Next step

1. PR CI (`build`, `test`, `spdx`) green on the head.
2. **Owner review.** Merge (merge commit, `--match-head-commit`) only after approval.
3. `PROJECT_PROGRESS.md` closeout on SAM `sow/2026-Q4` with the merge SHA.
4. Then the PR7b prompt for the owner (SAM_Tas catalogue reader, unique-name option writing, pane swap, capabilities and
   the kernel mapping above). Do not start PR7b without the owner.
