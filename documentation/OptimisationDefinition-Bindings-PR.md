# SAM.Core.Optimisation: model bindings, targets and measures (Q4, native Optimisation PR6) - SAM

Branch `feature/optimisation-model-bindings` -> base `sow/2026-Q4` (cut from `721b5fae`). Record date: 2026-10-08.

This is PR6 of the native Optimisation programme. The plan of record is SAM_UI
`documentation/NativeOptimisation-Plan-ModelBindings.md` (SAM_UI#216, merged `4c296cd`). The sequence after it is:
PR7a SAM_Tas licensed spike → PR7b SAM_Tas catalogue reader and script generator (engine `tas-model`) → PR8 SAM_UI
journey → PR9 Apply best design → PR10 SAM_Deploy. PR6 changes **SAM only**; SAM_Tas, SAM_UI and SAM.Math are not
touched, and nothing executes a binding yet.

## Current status

PR open, **not merged**. Code, tests and evidence complete; awaiting PR CI and owner review. Do not start PR7a without
the owner.

## Why

The owner decided (8 Oct) that nobody hand-writes the Tas script. A design variable says **what it changes in the
model** (a target) and an output says **what it measures** (a measure); SAM_Tas generates the TasGenExecute script from
tested blocks, and an AI assistant only returns definition JSON. PR6 gives the definition those bindings, the
capabilities and catalogue to check them against, and an AI text that offers only the model's real items.

## The shape (schema stays `sam.optimisation/1`)

A binding is `{ "kind", "reference", "parameters" }`; a target may also hold `"options"`. Both are optional, so every
existing definition is unchanged. Example (fixture `systems-demo-bound-golden-section.json`, abridged):

```json
"variables": [
  {
    "name": "Setpoint",
    "minimum": -5,
    "maximum": 35,
    "target": {
      "kind": "tpd.controller.setpoint",
      "reference": { "controller": "HeatPumpController", "plantRoom": "Plant Room 1" }
    }
  }
],
"outputs": [
  { "name": "Cost", "quantity": "currency", "unit": "GBP", "measure": { "kind": "tpd.annual-cost" } },
  { "name": "Overheating", "unit": "h", "measure": { "kind": "tsd.overheating-hours", "parameters": { "threshold": 28 } } }
]
```

(The canonical file puts every property on its own line.)

| Field | Where | Meaning |
|---|---|---|
| `target` | variable, after `step` | What the variable changes in the model. |
| `measure` | output, after `aggregation` | What the output measures in the results. |
| `kind` | both, required | Engine-defined, for example `tbd.internal-condition.heating-setpoint`. SAM holds no list of kinds. |
| `reference` | both, optional | Object of text: engine-defined keys, model item names, for example `{ "internalCondition": "Office" }`. Left out when the kind refers to the whole model. |
| `parameters` | both, optional | Object of numbers, for example `{ "threshold": 28 }`. A parameter left out uses the engine's default. |
| `options` | target only, optional | Ordered list of model item names for a choice target. |

**Model:** `OptimisationBinding` (abstract: `Kind`, `Reference`, `Parameters`, `SameItem`), `OptimisationTarget`
(+ `Options`), `OptimisationMeasure`; `DesignVariable.Target`, `OptimisationOutput.Measure`, deep-copied by the existing
copy constructors.

### Refinements to the hand-over shape, and why

- **Reference values are text only.** They name model items; a number is OPT107.
- **Reference and parameter keys are written in ordinal order** (`controller` before `plantRoom`), so the same binding
  always gives the same text whatever order it was built in. Options keep their order (it is the option numbering).
  Empty reference/parameters/options are left out, like the empty constraint list.
- **A key given as `null` counts as absent**, as for every other field.
- **Reference and parameter keys are not checked by the reader** (they belong to the engine); the diagnostics check
  them against the capabilities (OPT603, OPT604). The reader stays strict for the schema's own fields: an unknown field
  in `target` or `measure` (including `options` in a measure) is OPT105.
- **Choice targets (the owner's 8 Oct follow-up, shape only).** A kind with `AcceptsOptions` takes `options`; its
  variable is `"discrete"` and is **numbered 1 to n** (`minimum` 1, `maximum` the number of options; 1 is the first
  option). `minimum`/`maximum` therefore stay required for every variable, so the reader rule is unchanged. The
  "try every option" method and the construction/glazing swap are **not** implemented; `discrete` stays reserved, so a
  choice is never runnable today (OPT415).

## Capabilities

`IOptimisationCapabilities` gains `Targets` and `Measures` (lists of `OptimisationBindingCapability`): kind, display
name, quantity, unit (null when it depends on the model, as for a TPD controller), reference keys
(`OptimisationReferenceKey`: name, display name, required) and parameters (`OptimisationBindingParameter`: name,
default, minimum, maximum, quantity, unit, display name), and `AcceptsOptions`.

- **The existing six-argument `OptimisationCapabilities` constructor is kept** and means "no targets, no measures", so
  SAM_Tas#88's `TasOptimisationCapabilities()` compiles and behaves unchanged (verified below). A new eight-argument
  constructor adds the lists.
- An engine that lists **no** kinds (`tas-script`) takes no bindings: a bound definition is OPT601 there. An engine that
  lists kinds needs **every** variable bound and **every** output bound (OPT608).
- Adding interface members is safe: nothing outside SAM implements `IOptimisationCapabilities` (checked in SAM_Tas and
  SAM_UI).

## Catalogue

`OptimisationCatalogueEntry` keeps its name-only constructor (the PR4 `TasScriptCatalogue` path) and gains two:
a **target entry** (target, current value, suggested range `Minimum`/`Maximum`, available `Options`) and a **measure
entry** (measure with the parameters it is offered with, current value from the existing results). Both carry
description, quantity and unit. `OptimisationCatalogue.HasBindings` is true when any entry has a target or measure;
then the catalogue is the complete list of what the model offers.

## Reading, writing, diagnostics

- **Reader:** strict as before. New structure findings use the existing codes: OPT105 unknown field, OPT106 repeated
  reference/parameter key, OPT107 wrong type (reference not an object, a reference value or option not text, options
  not a list), OPT108 parameter written as text, OPT109 too large, OPT110 `kind` missing.
- **Writer:** canonical; every fixture, old and new, is byte-for-byte the writer's output, and writing is idempotent.
- **New overloads (old signatures kept, so binaries built against PR3 still bind):**
  `Query.Diagnostics(definition, capabilities, catalogue)`, `Query.IsRunnable(definition, capabilities, catalogue)`,
  `Create.OptimisationDefinition(text, out diagnostics, capabilities, catalogue, extract = false)`.

### New codes: OPT6xx, model bindings

OPT5xx stays reserved for engine execution checks. OPT6xx is new; the `OptimisationDiagnostic` class comment is updated.

| Code | Needs | Meaning (example message, from the tests) |
|---|---|---|
| OPT600 | — | A binding without a kind: "The target of Setpoint has no "kind"." |
| OPT601 | capabilities | Kind the engine does not list, with "Did you mean" and the list; a measure used as a target; an engine that takes no bindings: "Setpoint has a target, but the Tas engine takes no targets, so this definition cannot run." |
| OPT602 | capabilities | Required reference key missing, or a key with an empty name: "The target of Setpoint does not say which controller ("controller" is missing from "reference")." |
| OPT603 | capabilities | Reference key the kind does not define, with "Did you mean" ("This measure refers to the whole model: remove "reference"." when it has none). |
| OPT604 | capabilities | Parameter the kind does not define, with "Did you mean". |
| OPT605 | capabilities | Parameter out of range (limits inclusive): "The "threshold" of the measure of Overheating must be from 20 to 40 °C; it is 50 °C." Hint: "Leave it out to use 28 °C." |
| OPT606 | capabilities | Declared quantity or unit is not the kind's (no conversion): "Plant energy is declared in MWh, but the Tas engine reports annual plant energy in kWh." A synonym (`degC`) or no unit is fine; carbon/mass are interchangeable. |
| OPT607 | — | Two design variables change the same model item (same kind and reference; parameters ignored). Two outputs measuring the same thing are allowed (for example two thresholds). |
| OPT608 | capabilities | A variable without a target, or an output without a measure, on an engine that lists kinds. |
| OPT609 | catalogue | Binding not in the model: kind not offered ("Annual plant energy is not available in this model, so Plant energy cannot be measured."), a model item not found ("Internal condition “office” is not in this model, …" hint "Did you mean “Office”? Available: …"), or a combination not found. Left to OPT601 when the engine does not list the kind. |
| OPT610 | capabilities | Options on a kind that takes a value. |
| OPT611 | capabilities | A choice target with fewer than two options. |
| OPT612 | — | Options on a variable that is not `"discrete"`. |
| OPT613 | — | An option without a name, or listed twice. |
| OPT614 | catalogue | An option the catalogue entry does not offer, with "Did you mean". |
| OPT615 | — | A choice variable not numbered 1 to the number of options. |

A non-finite parameter (only possible in code) reuses **OPT214**. Messages name the variable/output and the model
item in the engine's words (kind and key display names); without capabilities they fall back to the kind and key text.
Catalogue checks run only for a catalogue with model items (`HasBindings`); a name-only catalogue adds nothing.

## AI text (`Query.AIExchangeText`)

For an engine that lists targets or measures:
- the rules ask for **every design variable to have a "target" and every output a "measure", copied exactly from
  AVAILABLE**, with no invented targets, measures or model item names (names are free labels, unique);
- "Do not write code, scripts or expressions." is kept;
- AVAILABLE lists only the catalogue's items whose kind the engine lists, each as one line of JSON to copy, then what
  it is, its unit, current value, suggested range, options and the kind's parameters (default and range), for example:

  ```
  Can change (design variable targets):
  - Office heating setpoint: { "kind": "tbd.internal-condition.heating-setpoint", "reference": { "internalCondition": "Office" } }
    Zone heating setpoint; unit °C; now 21 °C; suggested range 16 to 24 °C
  Can measure (output measures):
  - Overheating hours: { "kind": "tsd.overheating-hours", "parameters": { "threshold": 28 } }
    Overheating hours; unit h; now 37 h; parameter "threshold" (resultant temperature threshold, default 28 °C, 20 to 40 °C)
  ```
- a choice target is offered only when the engine runs `"discrete"` variables (none does today), with the 1-to-n rule;
- with no model items: "No list of model items is available: keep the targets and measures already in the current
  definition."

For `tas-script` (no kinds) the text is unchanged: no "target", "measure" or kind appears, even with a model catalogue.

## Fixtures (new, LF; the `systems-demo-*.json` files are not edited)

- `systems-demo-bound-golden-section.json`: the Systems Demo controller setpoint as a `tpd.controller.setpoint` target
  with `tpd.annual-cost` (objective) and `tpd.annual-co2` measures, engine `tas-model`. The plant room and controller
  names are illustrative until PR7b reads them from the TPD.
- `zone-setpoints-glazing-hooke-jeeves.json`: heating/cooling setpoint (internal condition) and glazing g-value targets;
  plant energy, heating/cooling demand and overheating (threshold 28) measures.
- `glazing-choice.json`: a discrete choice of three glazing constructions (shape only).

The test capabilities (`OptimisationFixtures.TasModel()`) mirror the plan's V1 kinds plus a choice kind; the real
kinds, keys and units are SAM_Tas' to define in PR7b.

## Files changed

- `SAM/SAM.Core.Optimisation/`:
  - new `Classes/{OptimisationBinding,OptimisationTarget,OptimisationMeasure,OptimisationBindingCapability,OptimisationReferenceKey,OptimisationBindingParameter}.cs`, `Query/BindingDiagnostics.cs`;
  - changed `Classes/{DesignVariable,OptimisationOutput,OptimisationCapabilities,OptimisationCatalogue,OptimisationCatalogueEntry,OptimisationDefinitionReader,OptimisationNames,OptimisationDiagnostic}.cs`,
    `Interfaces/IOptimisationCapabilities.cs`, `Convert/ToJson.cs`, `Create/OptimisationDefinition.cs`,
    `Query/{Diagnostics,AIExchangeText}.cs`, `Resources/sam.optimisation-1.schema.json`.
- `SAM/SAM.Tests/`: `Helpers/OptimisationFixtures.cs`, `Optimisation{DefinitionReader,DefinitionWriter,Diagnostics,Exchange}Tests.cs`,
  new `Golden/Optimisation/{systems-demo-bound-golden-section,zone-setpoints-glazing-hooke-jeeves,glazing-choice}.json`.
- This record.

## Validation

Local, 2026-10-08. All sibling repos fetched; SAM_Tas `861d3e7`, SAM_UI `47144f0` (both `sow/2026-Q4`, unchanged).

- **SAM:** `dotnet build SAM.sln -c Release` 0 errors; `SAM.Core.Optimisation` rebuilt with **0 warnings**.
- **`SAM.Tests`** (built explicitly): full suite **3355/3355** (3272 before; +83). Optimisation suites 364 (281 before).
  - Reader: the three new fixtures read exactly; old fixtures have no target/measure; unknown field in a binding with
    "Did you mean"; `options` in a measure; reference/parameter/options structure errors (OPT106–OPT110) with paths
    and positions; a null reference value is absent; an OPT609 located at line 22, column 11.
  - Writer: all five fixtures canonical and idempotent; round trip of every binding shape (kind only, one key, two
    keys, parameters, options with escaping and non-ASCII, everything; measures likewise); exact canonical layout with
    ordinal key order; empty parts and non-finite parameters left out; parameters bit-exact; deep copy.
  - Diagnostics: every OPT600–OPT615 code with exact messages and paths, OPT214 for a parameter; range limits
    inclusive; synonym/no unit and carbon-as-mass accepted; same measure twice allowed; old fixtures keep exactly their
    old findings on `tas-script`, with or without a catalogue; no binding-capability finding without capabilities.
  - Exchange: binding rules and the kept no-code rule; the exact AVAILABLE block (values, units, ranges, parameters);
    choice offered only with `discrete`; kinds the engine does not list are not offered; no model items → stated;
    `tas-script` text has no binding words; an AI reply copying the offered lines reads back bound and runnable; an
    invented model item is OPT609; a bound definition embedded in the text reads back unchanged; no path or machine
    detail; the schema resource has the new `$defs` and still matches the reader's fields.
- **Mutations** (each applied, built, run against the optimisation suites, then reverted; all caught):

  | Mutation | Tests failed |
  |---|---|
  | M1 writer keeps reference keys in insertion order | 2 |
  | M2 reader drops the target | 42 |
  | M3 same target twice not checked (OPT607) | 1 |
  | M4 catalogue not checked (OPT609/OPT614) | 6 |
  | M5 AI text offers measures of any kind | 1 |
  | M6 unit of the kind not checked (OPT606) | 1 |
  | M7 definition copy shares the options list | 1 |
  | M8 choice offered without discrete variables | 2 |
  | M9 unbound variable accepted by a binding engine (OPT608) | 1 |
  | M10 parameter minimum made exclusive (OPT605 boundary) | 1 |

- **SAM_Tas, unchanged, against this build** (`SAM/build/SAM.Core.Optimisation.dll`, same hash in the test output):
  `MSBuild SAM.Analytical.Tas.GenOpt.csproj -restore -p:Configuration=Release -p:BuildProjectReferences=false` 0 errors;
  `SAM.Analytical.Tas.GenOpt.Tests` **257/257** (as PR4).
- **SAM_UI, unchanged, against this build:** Release `SAM_UI.sln` 0 errors (scratch profile, real `NUGET_PACKAGES`);
  `TasOptimisation*` **121/121** (as PR5a), including the byte-identity of its embedded `systems-demo-*.json` copies.
- `git diff --check` clean; new JSON files LF; no `SAM.Math` path and no `systems-demo-*.json` touched.

## Unresolved issues and risks

- **Kinds and keys are illustrative until PR7b.** SAM checks only what an engine declares; SAM_Tas defines the real
  kinds, reference keys, units and parameter ranges, and PR7a may change them (R1 g-value, D1 overheating).
- **Names, not identities (R3).** References are model item names; a renamed item is reported by OPT609 with a
  suggestion, but the binding is not repaired automatically.
- **Choice targets are shape only.** The 1-to-n numbering is a design decision for the "try every option" PR to confirm;
  no engine runs `"discrete"`.
- **Versioning (open since PR3).** An older SAM (PR3) refuses a definition with `target`/`measure` (OPT105 unknown field).
  Acceptable while nothing is released; decide before `.samopt.json` (V1.1).
- **The plan file does not yet record the choice follow-up.** It lives in SAM_UI; this record holds the design. Owner to
  choose: a separate SAM_UI docs PR, or fold it into PR8.

## Next step

1. PR CI (`build`, `test`, `spdx`) green on the head.
2. **Owner review.** Merge (merge commit, `--match-head-commit`) only after approval.
3. `PROJECT_PROGRESS.md` closeout on SAM `sow/2026-Q4` with the merge SHA.
4. Then, only with the owner: PR7a, the SAM_Tas licensed spike.
