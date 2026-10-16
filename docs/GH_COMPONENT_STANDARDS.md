# SAM-BIM Grasshopper Component Engineering Standard

**Status:** canonical; merged via [SAM#204](https://github.com/SAM-BIM/SAM/pull/204) into `sow/2026-Q4`.
Revised 2026-10-15 after two independent reviews. The validator certifies a deliberately bounded
source subset; incomplete extraction or discovery blocks compatibility acceptance.
**Owner:** SAM-BIM. Changes to this document go through a reviewed PR in `SAM-BIM/SAM`.
**Applies to:** every SAM-BIM repository that publishes Grasshopper components.

This is the single specification for how a SAM-BIM Grasshopper component is named, described,
versioned and changed safely. It exists so that future component development is consistent, safe and
independently verifiable, and so a coding agent or a new maintainer can decide what a change is
allowed to do without re-deriving it from the source.

Read section 0.1 and 0.2 first: they state exactly what is checked automatically, what a green result
does and does not prove, and what the validator refuses to certify.

## 0. How to read this document

Every rule carries a status, because the pilot work proved some rules and left others open. Do not
treat a proposal as a fact.

| Status | Meaning |
|---|---|
| **VALIDATED** | Proven by committed code, a committed test, or a measured runtime result. Safe to rely on. |
| **CHECKED** | A static check in the metadata validator performs it, and a committed regression test covers it. Not a runtime or engineering guarantee. |
| **PROPOSED** | Recommended direction. Not checked by anything; a reviewer must judge it. |
| **OPEN** | A decision the owner has not made. Do not resolve it by guessing; ask. |
| **FROZEN LEGACY** | A published fact that must not change without an owner-approved migration. |

Rule identifiers (`GH-A1`, `GH-B2`, …) are stable and may be cited from a PR, a test name or a
review comment.

### 0.1 What is checked automatically, and what is not — read this before trusting a green result

Three different things are often conflated. Static checks and Rhino-hosted NUnit tests are automated;
only the runtime tests exercise saved-definition compatibility. Engineering review remains separate:

| Layer | What it establishes | Where |
|---|---|---|
| **Static metadata check** | The published parameter contract in the source still matches the approved baseline: GUID, component name/nickname/category/subcategory, and per parameter the name, runtime **type**, order, access, optionality, visibility and registered default; plus that extraction of that contract was complete. | `design/grasshopper-icons/tools/param_contract.py` and its regression tests |
| **Runtime compatibility test** | A saved definition opens, keeps its wires, typed-in values and groups, and recomputes. Rhino/Grasshopper-hosted; repository-local; not run by the static checker. | e.g. `Grasshopper/SAM.Core.Grasshopper.Tests/`, `SAM_Psychrometrics/Grasshopper/SAM.Core.Grasshopper.Psychrometrics.Tests/` |
| **Engineering review** | Description grammar and engineering meaning, SI-unit correctness, whether a description agrees with `SolveInstance`, mutually exclusive inputs, limitation wording, whether a rename is acceptable at all. | A human or agent reading the code. **Not automated.** |

So: **grammatical quality, SI-unit correctness and engineering semantics are NOT automatically
enforced.** The static checker detects an empty description, a description that does not start with a
capital letter, and one known typo; everything else in section D is a review obligation.

### 0.2 What the validator can be trusted for — three distinct outcomes

The validator refuses to answer a question it cannot answer. Every component carries an
`extraction.status`:

| Outcome | Meaning | Effect |
|---|---|---|
| `verified` | every parameter was extracted with name, runtime type, access, optionality, visibility and registered default | may be compared against the baseline |
| `partial` | extracted, but at least one attribute could not be established from the source | **blocks a compatibility PASS** |
| `unresolved` | the parameter set itself could not be established | **blocks a compatibility PASS** |

- **Inventory mode** (`python param_contract.py --repo <repo>`) writes or refreshes the manifest and
  reports coverage. It is explicitly labelled *not* compatibility validation: there is no approved
  baseline yet, so nothing has been compared. It still reports how many components are verified.
- **Compatibility mode** (`--check`) compares against the committed manifest and fails if any
  component is `partial`/`unresolved`, if the baseline is missing, or if any frozen attribute changed.
- A successful `--check` prints `OK compatibility`. Anything else is not a pass.

The governing rule, which the tests enforce: **never report a component or repository as verified when
the validator has not completely and reliably extracted its published contract.**

The mechanism this standard depends on lives in the shared `SAM` repository:

- `Grasshopper/SAM.Core.Grasshopper/Classes/GH_SAMComponent.cs`
- `Grasshopper/SAM.Core.Grasshopper/Classes/ParameterRename.cs`
- `Grasshopper/SAM.Core.Grasshopper/Classes/ParameterRenameMap.cs`
- `Grasshopper/SAM.Core.Grasshopper/Modify/` (`CaptureConnections`, `RestoreConnections`,
  `CopyPersistentData`, `CopyPersistentDataFromComponent`, `UpdateComponent`, `UpdateComponents`,
  `ReadLegacyParameterData`, `ExpandVariableParameters`)
- `Grasshopper/SAM.Core.Grasshopper/Query/` (`Obsolete`, `HasComponentVersion`, `MissingConnections`)
- `Grasshopper/SAM.Core.Grasshopper/Enums/` (`ObsoleteSeverity`, `ParamVisibility`)

---

## A. Component naming

### GH-A1 — Component Name: PascalCase operation under a dotted prefix — **FROZEN LEGACY** for published names

The published `Name` is the component's public identity. Existing instances follow
`<AreaPrefix>.<Operation>`, for example `SAMAnalytical.CreatePanelsBySolids`,
`SAMPsychrometrics.Psychrometrics`, `SAMOCCT.Solve3D`, `SAMCore.Update`. New components follow the
same shape: PascalCase after the dot, no underscores, no abbreviations that lose meaning.

An existing `Name` is **frozen**. It is not renamed to tidy it up; see section F.

### GH-A2 — Component NickName: short, recognisable, and equal to `Name` for new components — **PROPOSED**

For a new component, set `NickName = Name`. The `SAM_Psychrometrics` pilot changed no nickname and
asserts `NickName == Name` for every parameter; the same expectation is set for the component itself.
An existing nickname (for example `SAMGeometrySplit`'s short `Geo` parameter nicknames) is never
overwritten blindly — it is a published display value with the same compatibility questions as a name.

### GH-A3 — Category / Subcategory: `SAM` plus the domain area — **CHECKED**

Use `Category = "SAM"` and `Subcategory` = the engineering domain: `Core`, `Analytical`, `Geometry`,
`Psychrometrics`, `OCCT`, `Weather`, `Architectural`. Historical subcategories such as
`Analytical01`…`Analytical04` exist and stay as they are. The metadata validator records category and
subcategory and fails on a change.

### GH-A4 — Component Description: the engineering purpose, not the component name — **PROPOSED**

The component description states what it computes, from what, and the units of the significant
quantities. It is not a restatement of the name and not a list of parameter names. See section D.

### GH-A5 — Component GUID: stable identity, never regenerated for cosmetic changes — **CHECKED, VALIDATED**

`ComponentGuid` is the component's identity in every saved definition. It is regenerated **only** when
a genuinely different component is created (for example a legacy component kept alongside a new one,
as with `SAMOCCT.AutoTune3D` vs `SAMOCCT.AutoTune3DDiscover`).

Changing a GUID for a cosmetic, naming or description change is forbidden: it would orphan every
existing instance. The validator compares GUIDs against the baseline, and the Psychrometrics metadata
contract asserts the three GUIDs at runtime
(`Grasshopper/SAM.Core.Grasshopper.Psychrometrics.Tests/MetadataContractTests.cs`).

### Approved legacy exceptions (component naming)

- `SAMAnalytical.CreatePanelsBySolids`, `SAMAnalytical.CreateAperturesBySolids` name the operation in
  the plural (`BySolids`). Kept: published names.
- Component names and nicknames that predate this standard are kept as they are. No repository-wide
  renaming is authorised by this document.

---

## B. Input naming — Ladybug-inspired convention

SAM-BIM uses the Ladybug Tools / Honeybee input-marking convention. It is a **naming** convention.
It is not a mechanism, and it is not evidence of how Grasshopper behaves.

| Pattern | Meaning |
|---|---|
| `_name` | mandatory input |
| `_name_` | optional input with a defined default |
| `name_` | optional input without a defined default |

- **GH-B1 — New input names use camelCase with the marker that matches the design — PROPOSED.**
  The marker must match what the component actually does, per GH-B4.
- **GH-B2 — Do not change published input names to fit the convention — FROZEN LEGACY.**
  See section F.
- **GH-B3 — The marker is documentation, not enforcement — VALIDATED.**
  Nothing in Grasshopper or in SAM reads the underscore. The `Optional` property, the registered
  persistent data and any code-side fallback are independent declarations; the marker only records
  the intent for a reader. In
  `Grasshopper/SAM.Core.Grasshopper.Psychrometrics/Component/SAMPsychrometricsPsychrometrics.cs` the
  name `"_relativeHumidity_"` and `Optional = true` are set on the same initializer but neither
  derives from the other.

### GH-B4 — Requiredness and defaults: four different things that are often confused — **VALIDATED**

A reviewer or agent must keep these distinct. Verified in `GH_SAMVariableOutputParameterComponent`
registration and in the components themselves:

1. **Grasshopper `Optional`.** A property of the parameter object. An `Optional = true` input does not
   raise a "no data" error when it is unwired. It says nothing about a value existing afterwards.
2. **Registered persistent default (`IGH_Param.SetPersistentData`).** A value baked into the freshly
   created parameter (`_pressure_` in `SAMPsychrometrics.Psychrometrics` and
   `SAMPsychrometricsDryBulbTemperature` carries `101325`). A new instance arrives with that value in
   its persistent data tree; the parameter archive in a saved definition overrides it on reopen.
3. **Code-side fallback default.** Applied inside `SolveInstance` when the input is unwired or NaN, for
   example `pressure = 101325` in `SAMPsychrometricsPsychrometrics`. This is a real and legitimate
   design. It is invisible in the parameter metadata, so it *must* be stated in the description and
   covered by a test.
4. **Runtime validation / mandatory requirement.** Enforced in `SolveInstance`: either an
   `AddRuntimeMessage(GH_RuntimeMessageLevel.Error, …)` + `return`, or a guard on a mutually exclusive
   set (the four humidity indications above, exactly one of which must be supplied).

**GH-B5 — Code-side defaults are valid when they are documented and tested — VALIDATED.**
Do not require `SetPersistentData` merely to satisfy the naming convention, and do not add
`SetPersistentData` as a substitute for documenting the fallback. `CreatePanelsBySolids` documents
`minThickness_` (0.08 m), `snap_` (0.3 m), `maxThicknessRatio_` (0.2) and `airForOpenings_` (true) in
the parameter description while applying the fallback in code.

**GH-B6 — Never change `Optional`, `ParamVisibility` or a default to satisfy a naming convention —
VALIDATED (forbidden).** Those three are published behaviour. Changing one is a behavioural change
(section H) and is not a formatting fix.

### GH-B7 — Execution triggers are inputs, not a fourth marker — **VALIDATED (observed), PROPOSED for new components**

An execution trigger is a Boolean input that makes the component do its work (or not). It uses the same
marker as any input, according to whether the component works without it. Observed in the source (counts are
`Name = "…"` initializers under `SAM/Grasshopper` and the SAM_OCCT, SAM_Tas_Grasshopper and SAM_OpenStudio
Grasshopper folders):

| Spelling | Meaning in practice | Example |
|---|---|---|
| `_run` (278 initializers) | Required trigger. `Param_Boolean`, `ParamVisibility.Binding`, persistent `false`: nothing runs until the user connects a toggle. | `SAMAnalyticalCheck`: Description "Connect a boolean toggle to run."; `SolveInstance` does `IndexOfInputParam("_run")` and returns when it is false |
| `run_` (8) | Optional trigger. `Optional = true`; when unset the component still returns something (usually its input). | `SAMAnalyticalMergeSpacesByZones`: "If boolean set to false input Analytical Object will be return" |
| `_run_` (2) | Trigger with a defined default. | `GeometryPolycurveLoop2D` |

Rules: a trigger is never counted as an engineering input in a description, its description says what happens
when it is false, and for a new component that is cheap to compute prefer no trigger at all. An existing trigger is never
renamed to fit a spelling: `_run` is a published `Name` (section F). The markers `_`/`_x_`/`x_` of GH-B still apply
and are as unenforced as for any input (GH-B3).


`ParamVisibility` is a `[Flags]` enum (`Grasshopper/SAM.Core.Grasshopper/Enums/ParamVisibility.cs`)
evaluated by `GH_SAMVariableOutputParameterComponent`:

| Value | Registered on a fresh component | Removable on the canvas |
|---|---|---|
| `Voluntary` (0) | no | yes |
| `Mandatory` (1) | no | no |
| `Default` (2) | yes | yes |
| `Binding` (3) = `Mandatory \| Default` | yes | no |

- Registration tests `HasFlag(Default)` (`RegisterInputParams` / `RegisterOutputParams`,
  `Modify/ReadLegacyParameterData`); removal tests `HasFlag(Mandatory)` (`CanRemoveParameter`).
- `Voluntary` is the mechanism behind an input that is *hidden until inserted* — the "zoom in and
  click +" parameter. `SAMAnalytical.CreatePanelsBySolids`' `maxThicknessRatio_` and `diagnosticDetail_`
  are described that way on the canvas.
- `Mandatory` alone is never used anywhere in SAM and means "not registered and not removable"; treat
  it as a legacy combination, not a supported option.

---

## C. Output naming

### GH-C1 — New outputs are PascalCase, capitalised, with no underscore markers — **PROPOSED**

`Panels`, `Spaces`, `AnalyticalModel`, `Diagnostics`, `AdjacencyCluster`, `Successful`.

### GH-C2 — No leading or trailing underscore on an output — **CHECKED (convention)**

The metadata validator computes the expected output name as
`PascalCase(strip_underscores(name))` and reports a deviation. That is a **convention** finding, not a
compatibility failure: an existing deviation is legitimate and stays until it is registered as an
approved exception, and only a *new* deviation fails (section J). The `SAM_Psychrometrics` pilot
applied this to one output: `enthalpy` → `Enthalpy`, through the migration in section G.

### GH-C3 — Existing camelCase outputs stay until an approved migration — **FROZEN LEGACY, VALIDATED**

This is not a style question. Measured in Rhino 8: a reopened definition keeps the names the **file**
has, so renaming an output breaks every `Params.IndexOfOutputParam` lookup in every existing
definition, silently, and the SAM updater loses the wire and any typed-in value. Evidence:
`Grasshopper/SAM.Core.Grasshopper.Psychrometrics.Tests/SavedDefinitionCompatibilityTests.cs` and
`SAM_Psychrometrics/docs/GH-Component-Metadata-Standard.md` §2.

The `SAM_Psychrometrics` pilot froze 22 naming deviations and migrated exactly one. Those 22 stay
frozen. `SAM_Psychrometrics/docs/GH-Component-Metadata-Standard.md` is the record.

### GH-C4 — Units and engineering detail belong in the description, not the name — **PROPOSED**

`Panels`, not `Panels_m2`.

---

## D. Descriptions and tooltips

Every component, input and output has a description. It is the only place the engineering meaning is
written down for the person on the canvas.

- **GH-D1 — Begin with a capital letter; be grammatically and technically correct — CHECKED (for the
  first letter only).** The validator detects an empty description and one that does not begin with a
  capital letter. Grammar and technical correctness are a review obligation, not a check.
- **GH-D2 — Explain the engineering meaning, not the data type — PROPOSED.**
- **GH-D3 — Name the expected data when it matters — PROPOSED.** e.g. "Closed solids (Breps or
  extrusions) of planar building elements: walls, floors, slabs, ceilings, roofs, gables."
- **GH-D4 — Document SI units explicitly — PROPOSED, NOT CHECKED.** Nothing verifies units
  automatically. The validator reports the description text only; a unit error is invisible to it. The
  pilot's metadata review fixed `[kg/m3]` → `[kg/m³]` by hand, and the pilot's *runtime* test asserts a
  few description strings — that is all.
- **GH-D5 — Document defaults and fallback behaviour accurately — PROPOSED.** If a code-side fallback
  applies (GH-B4.3), the description says so: "Atmospheric pressure [Pa]. Default: 101325".
- **GH-D6 — Explain important input relationships and mutually exclusive inputs — PROPOSED.** The
  Psychrometrics component's humidity inputs each state which set they belong to and that only one may
  be connected, matching the exclusivity `SolveInstance` actually enforces.
- **GH-D7 — Explain the limitation or failure condition where it is useful — PROPOSED.** e.g. "Any
  other value is reported and Summary is used."
- **GH-D8 — No placeholder or misleading text — CHECKED (advisory, narrowly).** Empty descriptions,
  descriptions that do not start with a capital letter, the known typo (`Humidty`), and differences
  between `NickName` and `Name` produce advisory notes. They fail only with `--strict-descriptions`.
  Existing nicknames remain legitimate (GH-I3); misleading engineering text requires review.
- **GH-D9 — A description must agree with the implementation — PROPOSED (reviewer responsibility).**
  No check can verify this. It is the one quality rule that needs a human or an agent reading
  `SolveInstance` against the text. Treat a mismatch as a defect.

Descriptions are **not** frozen by the contract, deliberately: improving one does not require
regenerating the manifest. A description change is still a metadata change and gets a patch version
bump (section H) so the *Update component* offer becomes visible.

---

## E. Engineering Diagnostics

The canonical Diagnostics specification is [DIAGNOSTICS_STANDARD.md](DIAGNOSTICS_STANDARD.md) (API, severity and
detail semantics, adapters, testing). This section keeps only the Grasshopper-facing contract questions.

### E1 — What is implemented today — **VALIDATED**

Three layers exist and are committed:

1. **`SAM.Core`** (Unified Diagnostics Phase 1, SAM#201): `DiagnosticSeverity` (Info 0, Warning 1,
   Error 2), `DiagnosticDetail` (Summary 0, Standard 1, Detailed 2), immutable `Diagnostic` and the
   `Diagnostics` collection. `Add` drops Info above the collection's `Detail`; **Warnings and Errors
   are never dropped**. `Filter(level)` can only narrow. Runtime values, not serialised, independent
   of `Log`. Files: `SAM/SAM.Core/Enums/DiagnosticSeverity.cs`, `DiagnosticDetail.cs`,
   `SAM/SAM.Core/Classes/Diagnostics/Diagnostic.cs`, `Diagnostics.cs`.
2. **`SAM.Core.OCCT`** (Phase 3): `DiagnosticRecord` / `DiagnosticMessage` adapters and
   `SAM.Core.Query.TryGetDiagnosticDetail`.
3. **Grasshopper**: `SAM_OCCT` components expose a `Diagnostics` string-list output and a hidden
   optional `diagnosticDetail_` integer input; `SAM.Analytical.CreatePanelsBySolids` /
   `CreateAperturesBySolids` accept `diagnosticDetail_` and report in their existing `log` output.
   Severity maps to the Grasshopper message level (Info → log only, Warning → GH Warning, Error → GH
   Error).

### E2 — The preferred convention for new SAM-BIM Diagnostics — **PROPOSED**

- Detail enum: **0 = Summary (default), 1 = Standard, 2 = Detailed**, matching the implemented
  `SAM.Core.DiagnosticDetail`. A higher level includes every lower level.
- Detail controls **information granularity only**, never severity. Important warnings and errors
  remain visible at every level.
- Preferred new input: `_diagnosticDetail_`.
- Preferred output: `Diagnostics`.

The enum values are implemented and must not be renumbered; the Preferred new input/output names are
the *proposal for new components*, and they conflict with what is already published — see GH-E3.

### GH-E3 — `diagnosticDetail_` is already published; `_diagnosticDetail_` is a proposal — **OPEN**

| Where | Published input name |
|---|---|
| `SAM.Analytical.CreatePanelsBySolids` 1.0.3, `CreateAperturesBySolids` 1.0.2 | `diagnosticDetail_` |
| `SAMOCCT.Solve3D` 0.7.1, `SAMOCCT.CreateAdjacencyCluster` 0.2.1 (and the rest of the OCCT set) | `diagnosticDetail_` |

The implemented name (`diagnosticDetail_`) reads as "optional, no default" under section B, while the
input does have a code-side default of 0 — the convention's spelling for that is `_diagnosticDetail_`.
The owner must choose: keep `diagnosticDetail_` as the SAM-BIM convention, or adopt `_diagnosticDetail_`
for new components and accept that the existing inputs are a frozen legacy exception. **No existing
input is renamed by this document.** The OCCT components already assert the published name in
`SAM_OCCT/Testing/SAM.OCCT.GrasshopperTests/DiagnosticsContractTests.cs`.

### GH-E4 — Do not infer severity from message text — **VALIDATED**

`SAM.Core.OCCT` maps an explicit severity; `OcctDiagnosticReporter.ToRuntimeMessageLevel` switches on
the enum. Text matching was removed in the OCCT work.

### GH-E5 — Default Summary changes the default output content — **VALIDATED, owner-accepted**

After the Diagnostics work, the default `log` / `Diagnostics` text is Summary, so per-item Info lines
that used to appear by default now need `diagnosticDetail_ = 1` or `2`. Warnings and Errors are
unaffected. This is an accepted behaviour change; it is recorded so it is not reported as a
regression.

### GH-E6 — Scope limit — **OPEN**

The unified Diagnostics roadmap (SAM#201, SAM#202, SAM_OCCT#78, SAM_OpenStudio#28, SAM_Tas#101) is complete;
the OpenStudio and TAS adapters are library API and added no Grasshopper input. This document describes what exists and the direction for new work. It does not
authorise a Diagnostics redesign, and nothing here should be read as describing a capability that is
not in the code above.

---

## F. Published component compatibility

### GH-F1 — The verified Rhino 8 serialization behaviour — **VALIDATED**

Measured in a real Rhino 8 / Grasshopper run, not inferred:

- `GH_DocumentIO` writes a `ParameterData` chunk holding, per parameter, its own `InstanceGuid`,
  `Name`, `NickName`, `Description`, `Optional`, `Access` and persistent data.
- On reopen **the parameter names in the file win over the names the component class declares.** A
  renamed parameter keeps carrying the old name, so `Params.IndexOfInputParam("<new name>")` returns
  `-1` for every existing definition.
- Wires reference the parameter `InstanceGuid`, so a wire survives a reread but is lost when the
  component is *replaced* by the updater and the replacement's parameter is matched by name.
- Consequences: a rename produces *Invalid data* and no output, with **no compile error and no failed
  build**. Compilation cannot prove backward compatibility.

Evidence: `SAM_Psychrometrics/docs/GH-Component-Metadata-Standard.md` §2 ("Measured evidence"),
`SAM_Psychrometrics/Grasshopper/SAM.Core.Grasshopper.Psychrometrics.Tests/SavedDefinitionCompatibilityTests.cs`,
and the fixture `Fixtures/Psychrometrics-Legacy.ghx` saved by the pre-pilot build.

### GH-F2 — Safe metadata correction vs compatibility migration vs behavioural change — **VALIDATED**

| Class | Examples | Published parameter set | Version bump |
|---|---|---|---|
| **Safe metadata correction** | description text, typo, unit wording, nickname spelling fix that is verified harmless | unchanged: no `Name`, order, `Access`, `Optional`, visibility, persistent default or GUID touched | patch |
| **Compatibility migration** | parameter `Name` change with a declared alias | `Name` changes; every other attribute and the GUID stay | patch, with a `ParameterRename` declaration (section G) |
| **Behavioural / contract change** | computed result, default value, `Optional`, visibility, `Access`, order, a removed or added parameter, a GUID change | anything | minor or major, per section H |

A nickname change and a description change are *metadata*; a nickname is still a published display
value, so verify before changing one (GH-I3).

### GH-F3 — Existing parameter names stay unchanged unless an explicit, tested migration is provided —
**FROZEN LEGACY**

This is the single most important rule in this document. It follows from GH-F1: the file wins, so an
unmigrated rename is a silent break for every existing definition.

---

## G. Parameter migration

The mechanism is implemented in the shared `SAM` repository and validated end to end on one controlled
rename. It is reused, not redesigned. The implementation record is
`documentation/GH-Parameter-Rename-Migration-Q4.md`; the consumer record is
`SAM_Psychrometrics/docs/GH-Component-Metadata-Standard.md` §5.

### GH-G1 — A rename is declared, never inferred — **VALIDATED**

The component declares its published renames:

```csharp
public override IReadOnlyList<ParameterRename> ParameterRenames => parameterRenames;

private static readonly ParameterRename[] parameterRenames =
{
    new ParameterRename(GH_ParameterSide.Output, "enthalpy", "Enthalpy"),
};
```

`GH_SAMComponent.ParameterRenames` defaults to `null`: a component that declares nothing behaves
exactly as before, and nothing renames on file open.

### GH-G2 — Alias resolution is conservative and refuses ambiguity — **VALIDATED**

`ParameterRenameMap.Create` validates the declaration and resolves a legacy name to exactly one
current name. Refused and reported rather than guessed: duplicate legacy names (`X→A` and `X→B`),
chained renames (a legacy name that is another rename's target), empty names, and a rename to the same
name. A declaration that throws is caught, reported, and never stops an otherwise-fine update.
A name no rename mentions resolves to itself.

### GH-G3 — Alias-aware behaviour — **VALIDATED**

| Concern | Member |
|---|---|
| Wire reconnection | `Modify.RestoreConnections(component, connections, map, out log)` — redirects a legacy name, and **refuses** a redirect whose parameter runtime type changed (`ParamConnection.ParamTypeName` vs the new parameter's type) |
| Persistent-data transfer | `Modify.CopyPersistentDataFromComponent(from, to, map, out log)` — same type guard; a value that cannot be migrated is reported, never discarded silently |
| `SolveInstance` compatibility | `GH_SAMComponent.IndexOfInputParameter` / `IndexOfOutputParameter` fall back to the declared legacy name, so an *un-updated* definition still computes |
| Missing-wire verification | `Query.MissingConnections(component, snapshots, map)` resolves the alias, so a restored wire is not reported to the person as missing |
| Group membership | `Modify.ReconcileGroupMembership(document, old, new, out log)` moves `GH_Group.ObjectIDs` entries from the replaced object to the replacement |
| Updater entry point | `Modify.DuplicateComponent` reads and validates the replacement's renames, and is transactional after `AddObject` |

The original (pre-rename) public signatures were kept and delegate with an empty map: the change is
source-compatible for callers.

### GH-G4 — Version gating: never migrate on open — **VALIDATED**

A rename only takes effect through the explicit *Update component* offer, which becomes visible
because `LatestComponentVersion` was raised (see section H). Opening a file never changes it.

### GH-G5 — Identity is reconciled, not adopted — **VALIDATED**

A replacement is a newly constructed instance with a new `InstanceGuid`; `InstanceGuid` is get-only
and the only way to force a specific value is `NewInstanceGuid(Guid)`, whose purpose is minting a
fresh identity. `GH_Document` keeps an identity-keyed object cache populated at `AddObject`, so
adopting an old identity is unsafe. A reflection sweep of Rhino 8 Grasshopper
(`8.35.26251.13001`) found `GH_Group.ObjectIDs` as the only persisted `Guid` collection naming
document objects, so explicit group reconciliation is necessary and sufficient.

### GH-G6 — Controlled failure — **VALIDATED**

If a step after `AddObject` throws, the half-built replacement is removed and the original component
is left in the document. A broken `ParameterRenames` declaration is reported and never blocks the
update.

### GH-G7 — Required validation for a rename migration — **PROCESS REQUIREMENT (not automated)**

1. Build and the automated test suites.
2. An explicit alias-verification test (declaration shape, and duplicate/malformed declarations refused).
3. A committed legacy `.gh`/`.ghx` fixture saved by the *pre-change* build, driven through the updater
   in a real headless Rhino 8 (Rhino.Testing), asserting preserved wires, preserved persistent values,
   preserved groups.
4. Save/reopen/recompute: the upgraded definition recomputes identical engineering results.
5. No unintended engineering difference for anything not renamed.

Reference implementation of exactly this: `SAM_Psychrometrics/Grasshopper/SAM.Core.Grasshopper.Psychrometrics.Tests/`
(`ParameterRenameMigrationTests.cs`, `MetadataContractTests.cs`, `SavedDefinitionCompatibilityTests.cs`,
`Fixtures/Psychrometrics-Legacy.ghx`) and, on the shared side,
`Grasshopper/SAM.Core.Grasshopper.Tests/ParameterRenameMigrationTests.cs` (13 tests) with the fixture
components in `Grasshopper/SAM.Core.Grasshopper.Tests.Fixtures/TestComponents.cs`.

### GH-G8 — Residual limitation — **VALIDATED, unavoidable**

A rename is still breaking for any consumer that looks the old name up **outside** the SAM updater —
for example a C# script component or an external tool that matched on the historical string. The
`SolveInstance` fallback only covers the component's own lookups. There is no runtime enforcement in
this state and none is possible; this is documented in the consumer PR and it is the reason each
rename needs its own approval.

---

## H. Component versioning

### GH-H1 — The version difference drives the severity label — **VALIDATED**

`GH_SAMComponent.Obsolete` / `Query.GetObsoleteSeverity` compare the version recorded in the saved
definition (`SAM_ComponentVersion`, written from `LatestComponentVersion` at construction) with the
class's current `LatestComponentVersion`:

| Difference | `ObsoleteSeverity` |
|---|---|
| equal | `None` |
| patch differs only | `Advisory` (logged as `[patch]`) |
| minor differs | `Standard` |
| major differs | `Breaking` (logged as `[breaking]`) |
| either version unparseable | `Standard` |

`Obsolete` (severity ≠ `None`) is what makes the *Update component* / *Get the latest component* menu
items appear (`Modify/AppendNewComponentAdditionalMenuItem.cs`, `Modify.UpdateComponents`).

### GH-H2 — The de-facto policy, as implemented and practised — **VALIDATED**

Raise the **patch** component for every metadata-only or additive-compatible change; reserve minor and
major for genuine behavioural or contract-breaking changes. Observed in the merged work:

| Change | Examples | Bump |
|---|---|---|
| Description-only correction | `SAMPsychrometrics.Psychrometrics` 1.0.5→1.0.6; `DryBulbTemperature` / `DewPointTemperature` 1.0.1→1.0.2 | patch |
| Additive optional input appended last | `SAMAnalytical.CreatePanelsBySolids` 1.0.2→1.0.3; `CreateAperturesBySolids` 1.0.1→1.0.2; `SAMOCCT.Solve3D` 0.7.0→0.7.1; `CreateAdjacencyCluster` 0.2.0→0.2.1; `CreateShells` 0.3.0→0.3.1 | patch |
| Compatible parameter rename with a declared alias | `SAMPsychrometrics.Psychrometrics` 1.0.6→1.0.7 (`enthalpy`→`Enthalpy`) | patch |
| Genuine behavioural / contract-breaking change | not yet exemplified in Q4; e.g. a changed default, a removed parameter, a different geometry result | minor or major, chosen by the impact, and the reviewing owner |

### GH-H3 — A patch bump is not a claim that the change is harmless — **VALIDATED**

The bump exists to make the *Update component* offer visible. Safety comes from the declared alias
(GH-G1) and the type guard (GH-G3), not from the number. A patch-level change may therefore carry the
most consequential work in a release (the `enthalpy` rename did); this is intentional and is not a
reason to inflate the version.

### GH-H4 — `ObsoleteSeverity` and migration compatibility are orthogonal — **VALIDATED**

| | `ObsoleteSeverity` | `ParameterRenames` |
|---|---|---|
| Answers | how big is the difference between the saved component and the current class? | which published names changed, so the updater can carry wires and values? |
| Derived from | the version strings | an explicit declaration |
| Effect | labels the update (`[patch]` / `[breaking]`) and makes the menu item appear | changes what the updater does: alias redirection, type-guarded reconnection, group reconciliation |
| Set by | raising `LatestComponentVersion` | adding a `ParameterRename` |

A "breaking" label alone migrates nothing. A declared rename alone does nothing until the version is
raised. **Never use `ObsoleteSeverity` as evidence that a migration is safe, and never rely on a patch
bump to protect an unmigrated rename.**

### GH-H5 — `MinCompatibleVersion` is declared but not consulted — **VALIDATED (do not rely on it)**

`IGH_SAMComponent.MinCompatibleVersion` exists and every implementation returns
`LatestComponentVersion`, but nothing in `SAM.Core.Grasshopper` reads it. It is not an enforcement
mechanism today. Do not use it as one, and do not change its behaviour in a documentation PR.

### GH-H6 — No versioning of the standard's rules by component — **PROPOSED**

A component's `LatestComponentVersion` versions the *component*, not its compliance with this
document. There is no plan to version each rule.

---

## I. Parameter identity and serialization

### GH-I1 — What may never change without a separately approved migration — **FROZEN LEGACY**

For every published parameter:

- the `Name`;
- the runtime parameter **type** — the class the parameter is constructed from, for example
  `Param_Number` becoming `Param_String`, `GooPanelParam`, or a third-party parameter such as
  RhinoInside.Revit's `HostObject`;
- the `Access` (`item` / `list` / `tree`);
- the **order** within its side;
- the `Optional` flag;
- the visibility (`ParamVisibility`);
- any registered persistent default.

And for the component: the `ComponentGuid`.

Changing any of these changes what an existing definition means. The metadata contract freezes all of
them (`design/grasshopper-icons/tools/param_contract.py`, `compare` and `FROZEN_FIELDS`) and the
runtime contract asserts them at runtime in a real Rhino (`MetadataContractTests`,
`DiagnosticsContractTests`).

Two notes on the type, because it is the attribute most easily lost by an extractor:

- Type identity preserves namespaces and closed generic arguments: `VendorA.HostObject` differs from
  `VendorB.HostObject`, and `GooValueParam<int>` differs from `GooValueParam<string>`. Literal aliases,
  `global::`, primitive type aliases, local declarations and unambiguous source imports are resolved.
  Known Grasshopper built-ins retain their pilot-compatible short names; arbitrary external types
  retain their qualified identity in `externalParameterTypes`.
- A type that cannot be identified at all is **not** treated as equal to anything. It makes the
  component `partial` and blocks the check (GH-J6).
- Type identity alone does not prove constructor defaults. Nontrivial custom Goo/third-party
  constructors are inventory-only until their effects can be verified. Known built-ins and empty
  source subclasses inheriting those defaults are the currently certified subset.

### GH-I2 — When a controlled migration permits an exception — **VALIDATED**

Exactly one thing is exemptable, and only with the section G mechanism plus section J acceptance: a
parameter `Name`, redirected by a declared `ParameterRename`, with an unchanged type. Everything else
in GH-I1 remains frozen even under a rename.

### GH-I3 — Nickname and display metadata: verify first — **OPEN**

`NickName` is serialised per parameter and restored from the file, like `Name`. The pilot treats
`NickName == Name` as the new-interface expectation and changes no existing nickname. Whether a
nickname change is *harmless* for an existing definition has not been independently measured the way
the name behaviour has, so:

- changing a nickname is a metadata correction (GH-F2), subject to review;
- **do not** change a nickname as a way to change what a person sees while claiming the name is
  untouched, without stating the compatibility reasoning in the PR.

### GH-I3a — `Name` versus `NickName`: what each is for — **VALIDATED (Name), OPEN (NickName)**

Both are properties of the Grasshopper parameter (`IGH_Param.Name`, `IGH_Param.NickName`) and both are written
into the saved `ParameterData` chunk beside `Description`, `Optional`, `Access` and persistent data
(`SAM_Psychrometrics/docs/GH-Component-Metadata-Standard.md` §2, measured in Rhino 8).

| | `Name` | `NickName` |
|---|---|---|
| Grasshopper role | the parameter's full name (tooltip, identity-like label) | the short label drawn on the canvas; a user can edit it on an instance |
| SAM use | the lookup key: `Params.IndexOfInputParam("…")` / `IndexOfOutputParam("…")` in `SolveInstance`; the SAM updater (`Modify.DuplicateComponent`) recovers wires and values by name | display; the SAM lookups inspected (`SolveInstance`, `Modify.DuplicateComponent`) use `Name`, not `NickName` |
| On reopen | the file's name wins over the class's declared name; a renamed declaration silently returns -1 and the component reports Invalid data | also restored from the file; effect of changing it on an existing definition **not independently measured** (GH-I3) |
| SAM-BIM convention | published, frozen | equal to `Name` for every new parameter; every inspected component sets `Name == NickName` (for example `_run`, `_relativeHumidity_`) |

Consequences: for a **new** component set `NickName = Name`, so a person sees the same text that code looks up. For an
**existing** parameter change neither; a differing nickname is an intentional legacy value, kept. A user editing a
nickname on the canvas is harmless to SAM because lookups use `Name`; a code change to `Name` is not. Any `Name`
change needs the migration of section G, a saved `.gh`/`.ghx` roundtrip test, and a check that `SolveInstance` still
resolves every parameter after reopening (GH-G7).

### GH-I4 — Order is identity-adjacent — **VALIDATED**

Order is asserted by the contract and by the runtime tests. Appending a new voluntary parameter at the
**end** is the only order-safe addition (observed in `CreatePanelsBySolids` and the OCCT set, where
`diagnosticDetail_` is appended last and asserted as such). Inserting or reordering is a
contract-breaking change.

### GH-I5 — Descriptions are serialised but not frozen — **VALIDATED**

`Description` is stored per definition, so a new description reaches a person on a fresh placement or
after *Update component*; it cannot change how an existing definition computes. The contract
deliberately does not freeze it (GH-D9).

---

## J. Validation and acceptance

The minimum verification for a change depends on its class (GH-F2). Prefer automated verification over
inspection; where a check cannot be automated today, say so explicitly in the PR.

### GH-J0 — The three outcomes of a compatibility check — **CHECKED**

`--check` compares the source against the committed manifest and reports findings in categories:

| Category | Fatal? | What it means |
|---|---|---|
| `compatibility` | yes | a frozen attribute (GH-I1) changed: identity, parameter name, **type**, order, access, optionality, visibility, registered default; or a component was added/removed |
| `extraction` | yes | a component could not be fully extracted (`partial`/`unresolved`), the baseline lacks required extraction evidence, an object is unclassified, or a project cannot be parsed |
| `conventions` | yes | a naming-rule deviation that is not in the approved exception list, or an exception that is no longer a deviation |
| `manifest` | yes | the component version changed, so the manifest must be regenerated in this reviewed change — explicitly **not** a compatibility violation |
| `advisory` | no (unless `--strict-descriptions`) | description quality and nickname differences |

**Never weaken a check to obtain a green result.** If a repository cannot be validated reliably, report
its coverage limitation (a `partial` count with reasons) instead of lowering the bar.

### GH-J1 — Metadata-only correction

- Build (Release) with 0 errors.
- Compatibility check: `python design/grasshopper-icons/tools/param_contract.py --repo <repo> --check`
  → `OK compatibility`.
- If the version was bumped (as it should be, section H), the manifest is regenerated in the same
  reviewed change with `--repo <repo>`; the manifest diff is what a reviewer reads.
- No unintended metadata or API change: the diff must show only `Description` (and possibly
  `LatestComponentVersion`) in the component files.

### GH-J2 — Compatible parameter migration

- Build and the automated tests.
- Explicit alias verification: the declaration shape, and that duplicate/malformed declarations are
  refused.
- Legacy `.gh` / `.ghx` fixture testing in real Rhino 8 (headless `Rhino.Testing` is the accepted
  substitute for a canvas session; state which was used).
- Preserved wires, persistent values and group membership.
- Saved/reopened definitions recompute correctly.
- No unintended engineering result differences elsewhere.

### GH-J3 — Behavioural change

- Engineering-specific regression tests (golden fixtures; see the `PanelsBySolid` golden data).
- A compatibility and serialization review against section I.
- Appropriate integration tests.

### GH-J4 — What a check cannot cover — **VALIDATED**

- Description-versus-implementation agreement, grammar and SI-unit correctness (section 0.1).
- Renames for consumers outside the SAM updater (GH-G8) are unenforceable.
- Interactive canvas behaviour (the update mark, the "+" insert UI, a bake) is a manual acceptance.
  The merged Q4 work records headless equivalents and labels them as such; do not claim a canvas check
  unless it was run.

### GH-J5 — The metadata contract is a gate with an honest exception list — **CHECKED**

A new naming deviation fails; a listed exception that is no longer a deviation also fails. That is what
keeps the frozen-exception list meaningful instead of decorative. An existing deviation that *is*
registered passes, so no existing component is forced to change its published contract.

### GH-J6 — The extraction gate — **CHECKED, fail-closed**

A component whose contract was not completely extracted makes `--check` fail with an `extraction`
finding. This is deliberate: it is better to report `partial` than to certify a contract the validator
did not actually read. The regression suite proves this with cases that must fail — a missing output
set, a parameter name that is not a literal, a parameter registered outside the declaration, a
parameter whose type cannot be determined, and a baseline that is missing or itself incomplete.

---

## K. Quick reference — new components versus legacy components

The detailed rules and their status labels are in sections A to J; this table only separates what is required of a
**newly created** component from what is a **recommendation for, or a freeze on,** a legacy one.

| Topic | New component | Legacy component |
|---|---|---|
| Input names (GH-B1, B7) | camelCase with the marker that matches the design: `_x` required, `_x_` optional with default, `x_` optional without default, `_run` / `run_` trigger | keep the published name; never rename to fit |
| Requiredness (GH-B4, B6) | decided by `Optional`, `ParamVisibility`, registered default and code fallback; the marker records intent | frozen: never change `Optional`, visibility or default |
| Output names (GH-C1, C2) | capitalised, PascalCase, no underscores: `Panels`, `AnalyticalModel`, `Diagnostics` | camelCase outputs stay until an approved migration (GH-C3) |
| NickName (GH-A2, I3a) | `NickName = Name` | keep; do not overwrite blindly |
| Descriptions (GH-D) | purpose, SI units, defaults and fallbacks, optional behaviour, limitations, mutual exclusions | improve freely as a metadata change with a patch version bump (GH-D, H) |
| GUID, Name, order, access, type (GH-A5, I1, I4) | choose once; append new optional inputs last | frozen |
| Renaming a published `Name` | not applicable | only with a declared `ParameterRename`, saved `.gh`/`.ghx` roundtrip tests and a `SolveInstance` check (G, GH-G7) |
| Diagnostics output / input | follow [DIAGNOSTICS_STANDARD.md](DIAGNOSTICS_STANDARD.md); whether to add `diagnosticDetail_` / `_diagnosticDetail_` is a per-component decision (GH-E3) | add nothing universally |

---

## Appendix 1 — Evidence index

| Rule area | Source of truth |
|---|---|
| Input/output markers and defaults | `Grasshopper/SAM.Core.Grasshopper.Psychrometrics/Component/SAMPsychrometricsPsychrometrics.cs`; `SAMPsychrometricsDryBulbTemperature.cs`; `SAMPsychrometricsDewPointTemperature.cs` |
| Visibility semantics | `Grasshopper/SAM.Core.Grasshopper/Classes/GH_SAMVariableOutputParameterComponent.cs`; `Enums/ParamVisibility.cs`; `Modify/ReadLegacyParameterData.cs` |
| Versioning and obsolete labels | `Classes/GH_SAMComponent.cs`; `Query/Obsolete.cs`; `Enums/ObsoleteSeverity.cs`; `Modify/AppendNewComponentAdditionalMenuItem.cs` |
| Rename mechanism | `Classes/ParameterRename.cs`; `Classes/ParameterRenameMap.cs`; `Modify/CaptureConnections.cs`; `Modify/RestoreConnections.cs`; `Modify/CopyPersistentData.cs`; `Modify/CopyPersistentDataFromComponent.cs`; `Modify/UpdateComponent.cs`; `Modify/UpdateComponents.cs`; `Query/MissingConnections.cs`; `Classes/ParamConnection.cs` |
| Legacy parameter archive | `Grasshopper/SAM.Core.Grasshopper/Modify/ReadLegacyParameterData.cs` |
| Diagnostics | `SAM/SAM.Core/Enums/DiagnosticDetail.cs`, `DiagnosticSeverity.cs`; `SAM/SAM.Core/Classes/Diagnostics/Diagnostics.cs`, `Diagnostic.cs`; `SAM_OCCT/Grasshopper/Shared/OcctDiagnosticReporter.cs`; `SAM_OCCT/Grasshopper/…/SAMOCCTCreateAdjacencyCluster.cs`; `Grasshopper/SAM.Analytical.Grasshopper/Component/SAMAnalyticalCreatePanelsBySolids.cs` |
| Metadata contract | `design/grasshopper-icons/tools/param_contract.py` (this repository, canonical); `design/grasshopper-icons/tools/tests/test_param_contract.py` (69 regression tests); `SAM_Psychrometrics/design/grasshopper-icons/{tools/param_contract.py,tools/inventory.py,param_contract.json}` (pilot copy) |
| Runtime contract | `Grasshopper/SAM.Core.Grasshopper.Tests/` (Rhino.Testing); `SAM_Psychrometrics/Grasshopper/SAM.Core.Grasshopper.Psychrometrics.Tests/`; `SAM_OCCT/Testing/SAM.OCCT.GrasshopperTests/` |
| Measured compatibility | `SAM_Psychrometrics/docs/GH-Component-Metadata-Standard.md`; `documentation/GH-Parameter-Rename-Migration-Q4.md`; `documentation/Diagnostics-Phase1-PR.md`; `documentation/Diagnostics-Phase2-BySolids-PR.md` |

## Appendix 2 — Measured coverage (2026-10-15, final correctness repair)

Produced by the canonical validator:

```
python design/grasshopper-icons/tools/param_contract.py --repo <repo> --output <tmp.json>
```

`verified` means the published contract of that component was completely extracted and may be compared
against a baseline. `partial` means it was extracted but at least one attribute could not be
established; the reasons are in the manifest. A `partial` component **blocks a compatibility PASS**.

| Repository | Components | Verified | Partial | Unresolved | Parameter classes excluded |
|---|---|---|---|---|---|
| SAM | 460 | 37 | 423 | 0 | 64 |
| SAM_Tas_Grasshopper | 94 | 5 | 85 | 4 | 10 |
| SAM_Revit | 55 | 9 | 13 | 33 | 1 |
| SAM_OCCT | 36 | 0 | 36 | 0 | 1 |
| SAM_UI | 31 | 0 | 31 | 0 | 1 |
| SAM_Systems | 30 | 0 | 27 | 3 | 8 |
| SAM_Mollier | 24 | 7 | 15 | 2 | 4 |
| SAM_Topologic | 22 | 7 | 15 | 0 | 0 |
| SAM_SolarCalculator | 21 | 0 | 21 | 0 | 3 |
| SAM_Excel | 8 | 1 | 6 | 1 | 0 |
| SAM_LadybugTools | 7 | 2 | 5 | 0 | 0 |
| SAM_Solver | 7 | 0 | 7 | 0 | 0 |
| SAM_OpenStudio | 6 | 0 | 6 | 0 | 0 |
| SAM_Multitasker | 5 | 0 | 4 | 1 | 4 |
| SAM_Acoustic | 3 | 0 | 3 | 0 | 1 |
| SAM_BHoM | 3 | 0 | 3 | 0 | 0 |
| SAM_IFC | 3 | 0 | 3 | 0 | 1 |
| SAM_Psychrometrics | 3 | 3 | 0 | 0 | 0 |
| SAM_Python | 3 | 0 | 3 | 0 | 3 |
| SAM_gbXML | 2 | 1 | 1 | 0 | 0 |
| SAM_Origin | 2 | 1 | 1 | 0 | 0 |
| SAM_GEM | 1 | 0 | 1 | 0 | 0 |

**Corrected totals:** 22 repositories, **826 discovered / 73 verified / 709 partial / 44 unresolved**;
0 unclassified objects and 0 unparseable projects in these checkouts. The earlier 823/817 claim omitted
Psychrometrics' three components; the earlier table summed to 826/820. That 820 label was unsound:
unsupported declarations and constructor effects were treated as complete. These totals use the final
strict subset, rather than treating an empty scan as proof. Revit's 33 empty uncertain contracts are
now unresolved. The source metadata for CreatePanelsBySolids still yields 7 inputs/4 outputs, but its
Goo constructor defaults require additional evidence and therefore it is partial.

**Coverage limitations, stated explicitly.** SAM_Psychrometrics is the only repository with an approved
baseline, so it is the only repository for which `--check` is meaningful; for every other repository
these numbers are an **inventory**, not compatibility validation. The six originally identified partial components remain included in the extraction gaps listed in
Appendix 3; they are not the complete coverage limitation. `parameter classes excluded` counts the `Goo…Param` classes, which are
reported in the manifest with the ancestry that excluded them.

## Appendix 3 — Open decisions and known limitations

1. **Classification is by ancestry, and unresolved ancestry is reported.** A class is a published
   component when its base chain reaches a known Grasshopper root (`GH_Component`, `GH_SAMComponent`,
   `GH_SAMVariableOutputParameterComponent`, `GH_SAMAsyncComponent`, `GH_SAMEnumComponent`,
   `IGH_Component`, `IGH_SAMComponent`) through the repository's own classes; a `Goo…Param` class is a
   parameter. A class whose chain leaves the repository at an **unknown** type is neither silently
   included nor silently excluded — it is listed in `unclassifiedClasses` with its base chain, and it is
   not counted as a component. Ancestry is resolved across the whole repository (including
   non-Grasshopper projects), so a base type in another project of the same repository still resolves.
   Unclassified objects and unparseable projects are fatal extraction findings with source paths.
   Recognizing a Revit component through `IGH_SAMComponent` does not establish its parameter contract;
   `ParamDefinition[]` declarations remain unresolved by this tool.
2. **The six originally reported partial components remain extraction gaps.** They are a subset
   of the larger, corrected coverage limitations in Appendix 2:
   - `SAMAnalyticalFilterByBoundaryType`, `SAMAnalyticalFilterByPanelType` — parameter names generated by
     `foreach (… in Enum.GetValues(typeof(X)))`; resolving them means enumerating an enum's members,
     which is deliberately out of scope (see note 4).
   - `SAMCoreFilterByType`, `SAMCoreInspect`, `SAMAnalyticalSystemResults` — variable-parameter
     components registered through the parameter manager, where the insertable set is generated in code
     and cannot be enumerated from the declaration.
   - `SAMAcousticCreateBandValues` — the parameter name comes from a loop variable, so it is not a
     literal.
   Each blocks a compatibility PASS. There is no CLI switch to waive incomplete extraction.
3. **`diagnosticDetail_` vs `_diagnosticDetail_`** (GH-E3) — owner decision.
4. **Supported declaration forms** (see Appendix 6) are the ones the repositories actually use. Anything
   outside them is reported as `partial`/`unresolved` with a reason; the tool never invents a
   contract. Extending the supported set is a bounded follow-up with its own regression test.
5. **Scanner copies.** The scanner exists in the canonical validator (inline) and in the
   `SAM_Psychrometrics` pilot copy and `SAM/design/grasshopper-icons/tools/inventory.py` (older,
   API-incompatible generation). Consolidating them is a follow-up; the *data* (the manifest) is already
   repository-specific.
6. **No repository except SAM_Psychrometrics has an approved baseline**, so `--check` is meaningful only
   there. Everywhere else the numbers in Appendix 2 are an inventory.
7. **CI.** No SAM-BIM CI job runs this validator yet. The runner images have Python available; the
   blocker is the per-repository baseline workflow, not the Python. See Appendix 5.
8. **`MinCompatibleVersion`** (GH-H5) is dead code in effect; removing or wiring it is a separate,
   owner-approved change.
9. **`SAM_OpenStudio_base`** appeared as a second checkout of SAM_OpenStudio during this investigation
   and is not a SAM-BIM repository; it is excluded from all counts above.

## Appendix 4 — Repository adoption plan

The canonical specification lives in `SAM-BIM/SAM`. Another repository does not copy it; it points at
it. Adoption per repository is: (1) a one-sentence instruction in that repository's `AGENTS.md`,
(2) an inventory whose result is reviewed and committed as the baseline, (3) a `--check` run. The
proposed instruction snippet, written to be appended to an existing `AGENTS.md` "Before starting work"
list, is:

```markdown
- Before changing, adding or removing a Grasshopper component or any of its parameters,
  descriptions, defaults, visibility or version, read the SAM-BIM standard:
  https://github.com/SAM-BIM/SAM/blob/sow/2026-Q4/docs/GH_COMPONENT_STANDARDS.md
  Published parameter names, order, runtime type, access, optionality, visibility and defaults are
  frozen; a rename needs a declared ParameterRename alias and the migration acceptance in that document.
- If this repository has an approved manifest (design/grasshopper-icons/param_contract.json), validate:
    python <SAM>/design/grasshopper-icons/tools/param_contract.py --repo . --check
  Otherwise it has none yet: run the inventory, review the coverage, and propose the baseline in a PR:
    python <SAM>/design/grasshopper-icons/tools/param_contract.py --repo .
  A `partial`/`unresolved` component is never a PASS; the manifest records why.
```

Order — small and low-risk first, and never mixing a description sweep with a rename:

| Wave | Repositories | Why |
|---|---|---|
| 0 | SAM (this PR's standard), SAM_Psychrometrics (already done) | The reference and the pilot. |
| 1 | SAM_GEM, SAM_Python, SAM_BHoM, SAM_gbXML, SAM_Origin, SAM_IFC, SAM_Acoustic | 1–3 components each. Review extraction reasons and constructor/type dependencies before proposing a baseline; small does not mean fully verified. |
| 2 | SAM_LadybugTools, SAM_Solver, SAM_OpenStudio, SAM_Multitasker, SAM_Excel, SAM_SolarCalculator, SAM_Topologic | Small inventories; resolve coverage gaps before baseline acceptance. |
| 3 | SAM_Mollier, SAM_UI, SAM_Systems | Mid-size inventories with extraction gaps. Description corrections only, no renames. |
| 4 | SAM_OCCT, SAM_Revit | Both own runtime contract tests; static coverage is incomplete, especially Revit ParamDefinition and external bases. |
| 5 | SAM_Tas_Grasshopper | 94 components with incomplete static coverage; inspect dependency and inheritance gaps first. |
| 6 | SAM | 460 components and **no baseline yet**. Review the corrected coverage before proposing a baseline; fixes are per-component, owner-prioritised. |

Per repository, separate the three change classes (GH-F2) into separate PRs: description corrections
first (safe, no metadata freeze change), then additive optional parameters, then — only with an owner
decision and the full GH-G7 acceptance — any naming migration. **This document does not start any of
these waves**, and does not authorise a rename anywhere.

## Appendix 5 — Enforcement architecture and CI feasibility

Four distinct things, deliberately not conflated:

1. **Universal naming and description rules** — repository-independent: the naming rules, the
   description-quality rules (empty, capital letter, known typo), the nickname note. Lives once, in
   `design/grasshopper-icons/tools/param_contract.py` in `SAM`. Reuse by `--repo`, not by copy.
2. **Repository-specific published metadata baseline** — the rendered `param_contract.json`, committed
   per repository. Data, not code. It carries the `namingExceptions` list of approved legacy deviations
   and the per-component `extraction` status.
3. **Compatibility comparison** — the static `--check` against that baseline, which fails on a frozen
   attribute change, on incomplete extraction, on a new/stale naming deviation, and on an unrefreshed
   version bump (GH-J0).
4. **Runtime migration compatibility tests** — Rhino-hosted, repository-local, per migration; not
   shareable, because each one pins that repository's real components and a real saved definition.

Feasibility, measured rather than assumed:

- The validator is pure standard-library Python 3 (`json`, `os`, `re`, `sys`, `argparse`, `fnmatch`,
  `xml.etree.ElementTree`). A full scan of `SAM` (460 components, ~1000 files over 6 Grasshopper
  projects among 25 project files) is machine-dependent; current coverage is recorded in Appendix 2. **Python
  availability is not the blocker** — `ubuntu-latest` and `windows-2022` both ship Python 3, and the
  local runs here used the RhinoCode Python 3.13.
- The regression suite is `design/grasshopper-icons/tools/tests/test_param_contract.py` (stdlib
  `unittest`, no dependencies, no network): 69 tests cover the supported and the deliberately
  unsupported declaration forms, type changes, custom and external parameter types, indirect
  inheritance, unusual component names, collection-based registration, legitimate List/Tree access,
  existing nicknames, code-side defaults, and missing or incomplete baselines.
- The real blockers are: (a) a `--check` needs a committed baseline, so a CI job can only be switched on
  per repository *after* that repository's baseline PR; (b) Rhino-dependent runtime tests cannot run on
  the hosted runners, so they stay local/headless, exactly as `SAM.Core.Grasshopper.Tests` does today
  (it is deliberately **not** in `SAM.sln`).
- Recommended shape when the owner is ready: one workflow step in the repository's own `test.yml`
  (checkout `SAM` at a pinned ref, then `python <path>/param_contract.py --repo . --check`), plus the
  regression suite in `SAM`. Do not put Rhino-hosted tests in CI and do not add a fleet-wide job before
  the per-repository baselines exist.

This PR adds no CI job and no baseline for any repository other than the pilot already in place.

## Appendix 6 — Certification boundary and architecture decision

The scanner may inventory more metadata than it can certify. `verified` requires evidence from the
supported declaration subset; successfully finding some `new` expressions is insufficient.

| Certified source form | Required evidence |
|---|---|
| Straight-line `GH_SAMParam[]` properties | One proven returned collection, with wrappers added in order; direct array/collection expressions, a single list with `Add`/`AddRange`, `ToArray()` or `[.. list]` |
| Empty side | Explicit empty array/collection return or an empty manager override; absence of registration is not evidence of emptiness |
| Parameter construction | Declared or target-typed `new`, initializer with or without parentheses, whitespace variations, bare declaration followed by assignment; constructor arguments/effects must be supported |
| Parameter reuse/mutation | Assignment order is tracked; `Name`, `NickName`, `Optional`, `Access` and persistent data assignments update the correct object even after wrapping; reassignment does not mutate earlier objects |
| Parameter manager | Straight-line supported `AddParameter` or typed `Add*Parameter`; unsupported calls and control flow block certification |
| Inheritance | Follow repository bases to the actual registration, including multiple levels; missing external registration or ambiguous ancestry blocks certification |
| Type identity/defaults | Qualified and generic identity retained; aliases/imports resolved only with evidence; known built-in defaults or an empty source subclass inheriting them |

**Inventory-only / partial or unresolved:** helper-returned output sets; local parameter factories;
branches/loops and filtered/discarded returned collections; symbolic defaults; unknown parameter types;
nontrivial Goo/third-party constructors; Revit `ParamDefinition[]`; unread external bases. Each has a
specific extraction reason and cannot obtain compatibility PASS. The original six partial components
remain among these limitations. No component is changed to fit the extractor.

Schema 3 records qualified type identities and extraction evidence. Schema 2 baselines need a reviewed
refresh because their type identity was lossy. The approved schema-less Psychrometrics pilot remains
readable: its built-in types resolve reliably and its current source must still be fully extracted.

The bounded choice for PR #204 was:

| Approach | Decision |
|---|---|
| Strict Python subset | Chosen: existing standard-library tool, small explicit certification boundary, reject uncertainty; supports the approved pilot without requiring host applications |
| Roslyn semantic analysis | Separate reviewed follow-up for broader C#, symbols, constructors and MSBuild inclusion; adds a .NET tool/dependencies and project-resolution policy |
| Grasshopper runtime inspection | Complementary evidence for actual constructed parameters and saved definitions; requires each relevant host/dependency environment, including Revit for its components |

Do not grow this into a general C# parser to improve the count. Broader certification requires a new
reviewed semantic/runtime approach. A passing static check applies to the supported declarations and
approved baseline; it is not a substitute for compilation, runtime compatibility tests or engineering
review.
