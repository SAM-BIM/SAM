# SAM-BIM Grasshopper Component Engineering Standard

**Status:** canonical. Established 2026-10-15 on `sow/2026-Q4`.
**Owner:** SAM-BIM. Changes to this document go through a reviewed PR in `SAM-BIM/SAM`.
**Applies to:** every SAM-BIM repository that publishes Grasshopper components.

This is the single specification for how a SAM-BIM Grasshopper component is named, described,
versioned and changed safely. It exists so that future component development is consistent, safe and
independently verifiable, and so a coding agent or a new maintainer can decide what a change is
allowed to do without re-deriving it from the source.

## 0. How to read this document

Every rule carries a status, because the pilot work proved some rules and left others open. Do not
treat a proposal as a fact.

| Status | Meaning |
|---|---|
| **VALIDATED** | Proven by committed code, a committed test, or a measured runtime result. Safe to rely on. |
| **ENFORCED** | Checked automatically by committed tooling; a violation fails a check. |
| **PROPOSED** | Recommended direction. Not yet implemented, or applies to new work only. |
| **OPEN** | A decision the owner has not made. Do not resolve it by guessing; ask. |
| **FROZEN LEGACY** | A published fact that must not change without an owner-approved migration. |

Rule identifiers (`GH-A1`, `GH-B2`, …) are stable and may be cited from a PR, a test name or a
review comment.

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

### GH-A3 — Category / Subcategory: `SAM` plus the domain area — **ENFORCED**

Use `Category = "SAM"` and `Subcategory` = the engineering domain: `Core`, `Analytical`, `Geometry`,
`Psychrometrics`, `OCCT`, `Weather`, `Architectural`. Historical subcategories such as
`Analytical01`…`Analytical04` exist and stay as they are. The Psychrometrics contract tooling records
category and subcategory and fails on a change.

### GH-A4 — Component Description: the engineering purpose, not the component name — **PROPOSED**

The component description states what it computes, from what, and the units of the significant
quantities. It is not a restatement of the name and not a list of parameter names. See section D.

### GH-A5 — Component GUID: stable identity, never regenerated for cosmetic changes — **ENFORCED, VALIDATED**

`ComponentGuid` is the component's identity in every saved definition. It is regenerated **only** when
a genuinely different component is created (for example a legacy component kept alongside a new one,
as with `SAMOCCT.AutoTune3D` vs `SAMOCCT.AutoTune3DDiscover`).

Changing a GUID for a cosmetic, naming or description change is forbidden: it would orphan every
existing instance. The Psychrometrics metadata contract asserts the three GUIDs
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

### `ParamVisibility` semantics — VALIDATED

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

### GH-C2 — No leading or trailing underscore on an output — **ENFORCED** (in the pilot)

The metadata contract's naming rule computes the expected output name as
`PascalCase(strip_underscores(name))` and fails on a deviation
(`design/grasshopper-icons/tools/param_contract.py`, `expected_name`). The `SAM_Psychrometrics` pilot
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

- **GH-D1 — Begin with a capital letter; be grammatically and technically correct — ENFORCED.**
- **GH-D2 — Explain the engineering meaning, not the data type — PROPOSED.**
- **GH-D3 — Name the expected data when it matters — PROPOSED.** e.g. "Closed solids (Breps or
  extrusions) of planar building elements: walls, floors, slabs, ceilings, roofs, gables."
- **GH-D4 — Document SI units explicitly — ENFORCED in the pilot.** `[m]`, `[°C]`, `[kg/m³]`, `[Pa]`,
  `[-]` for dimensionless. The pilot's first fix was the typo `Humidty` → `Humidity` and unit
  consistency (`[kg/m3]` → `[kg/m³]`).
- **GH-D5 — Document defaults and fallback behaviour accurately — PROPOSED.** If a code-side fallback
  applies (GH-B4.3), the description says so: "Atmospheric pressure [Pa]. Default: 101325".
- **GH-D6 — Explain important input relationships and mutually exclusive inputs — PROPOSED.** The
  Psychrometrics component's humidity inputs each state which set they belong to and that only one may
  be connected, matching the exclusivity `SolveInstance` actually enforces.
- **GH-D7 — Explain the limitation or failure condition where it is useful — PROPOSED.** e.g. "Any
  other value is reported and Summary is used."
- **GH-D8 — No placeholder or misleading text — ENFORCED.** The contract check fails on an empty
  description, a description that does not start with a capital letter, a known typo, and
  `NickName != Name`.
- **GH-D9 — A description must agree with the implementation — PROPOSED (reviewer responsibility).**
  The automated check cannot verify this; it is the one quality rule that needs a human or an agent
  reading `SolveInstance` against the text. Treat a mismatch as a defect.

Descriptions are **not** frozen by the contract, deliberately: improving one does not require
regenerating the manifest. A description change is still a metadata change and gets a patch version
bump (section H) so the *Update component* offer becomes visible.

---

## E. Engineering Diagnostics

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

The unified Diagnostics architecture is under active development (Phase 4, OpenStudio/TAS adapters, is
not started). This document describes what exists and the direction for new work. It does not
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

### GH-G7 — Required validation for a rename migration — **ENFORCED by process**

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
- the runtime parameter **type** (for example `Param_Number`, `GooPanelParam`);
- the `Access` (`item` / `list` / `tree`);
- the **order** within its side;
- the `Optional` flag;
- the visibility (`ParamVisibility`);
- any registered persistent default.

And for the component: the `ComponentGuid`.

Changing any of these changes what an existing definition means. The metadata contract freezes all of
them (`design/grasshopper-icons/tools/param_contract.py`, `diff`) and the runtime contract asserts them
at runtime in a real Rhino (`MetadataContractTests`, `DiagnosticsContractTests`).

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

### J1 — Metadata-only correction

- Build (Release) with 0 errors.
- Metadata contract check: `python design/grasshopper-icons/tools/param_contract.py --repo <repo> --check`
  (in `SAM`, the shared validator; in `SAM_Psychrometrics`, the pilot copy) → `OK`.
- No unintended metadata or API change: the diff must show only `Description` (and possibly
  `LatestComponentVersion`) in the component files.

### J2 — Compatible parameter migration

- Build and the automated tests.
- Explicit alias verification: the declaration shape, and that duplicate/malformed declarations are
  refused.
- Legacy `.gh` / `.ghx` fixture testing in real Rhino 8 (headless `Rhino.Testing` is the accepted
  substitute for a canvas session; state which was used).
- Preserved wires, persistent values and group membership.
- Saved/reopened definitions recompute correctly.
- No unintended engineering result differences elsewhere.

### J3 — Behavioural change

- Engineering-specific regression tests (golden fixtures; see the `PanelsBySolid` golden data).
- A compatibility and serialization review against section I.
- Appropriate integration tests.

### GH-J4 — What a check cannot cover — **VALIDATED**

- Description-versus-implementation agreement (GH-D9) is a reading task.
- Renames for consumers outside the SAM updater (GH-G8) are unenforceable.
- Interactive canvas behaviour (the update mark, the "+" insert UI, a bake) is a manual acceptance.
  The merged Q4 work records headless equivalents and labels them as such; do not claim a canvas check
  unless it was run.

### GH-J5 — The metadata contract is a gate with an honest exception list — **ENFORCED**

A new naming deviation fails; a listed exception that is no longer a deviation also fails. That is what
keeps the frozen-exception list meaningful instead of decorative.

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
| Metadata contract | `design/grasshopper-icons/tools/param_contract.py` (this repository, canonical); `SAM_Psychrometrics/design/grasshopper-icons/{tools/param_contract.py,tools/inventory.py,param_contract.json}` (pilot copy) |
| Runtime contract | `Grasshopper/SAM.Core.Grasshopper.Tests/` (Rhino.Testing); `SAM_Psychrometrics/Grasshopper/SAM.Core.Grasshopper.Psychrometrics.Tests/`; `SAM_OCCT/Testing/SAM.OCCT.GrasshopperTests/` |
| Measured compatibility | `SAM_Psychrometrics/docs/GH-Component-Metadata-Standard.md`; `documentation/GH-Parameter-Rename-Migration-Q4.md`; `documentation/Diagnostics-Phase1-PR.md`; `documentation/Diagnostics-Phase2-BySolids-PR.md` |

## Appendix 2 — Measured baseline (2026-10-15)

Produced by the canonical validator at this PR's content commit:

```
python design/grasshopper-icons/tools/param_contract.py --repo <repo> --output <tmp.json>
```

"Components" excludes the `Goo…Param` classes the scanner would otherwise collect (GH-Appendix 3,
note 1). Deviations and problems are counts from the standard's own rules; a nonzero value is a
*registered baseline*, not necessarily a defect to fix.

| Repository | Components | Naming deviations | Description/version problems |
|---|---|---|---|
| SAM | 460 | 1120 | 1493 |
| SAM_Tas_Grasshopper | 94 | 370 | 370 |
| SAM_Revit | 55 | 26 | 38 |
| SAM_OCCT | 36 | 155 | 188 |
| SAM_UI | 31 | 179 | 81 |
| SAM_Systems | 30 | 58 | 32 |
| SAM_Mollier | 24 | 107 | 37 |
| SAM_Topologic | 22 | 10 | 12 |
| SAM_SolarCalculator | 21 | 348 | 210 |
| SAM_Excel | 8 | 27 | 3 |
| SAM_Solver | 7 | 68 | 68 |
| SAM_LadybugTools | 7 | 3 | 3 |
| SAM_OpenStudio | 6 | 24 | 9 |
| SAM_Multitasker | 5 | 8 | 8 |
| SAM_Acoustic | 3 | 1 | 0 |
| SAM_BHoM | 3 | 0 | 3 |
| SAM_IFC | 3 | 5 | 0 |
| SAM_Psychrometrics | 3 | 22 (frozen, all registered) | 0 |
| SAM_Python | 3 | 6 | 3 |
| SAM_gbXML | 2 | 4 | 0 |
| SAM_Origin | 2 | 2 | 1 |
| SAM_GEM | 1 | 3 | 2 |

SAM_Psychrometrics is the only repository with a committed manifest and a passing `--check`; its 22
deviations are the documented frozen exceptions (one rename already migrated). The distribution
matters more than any single number: most repositories are small, and the deviation count is
dominated by camelCase outputs.

## Appendix 3 — Open decisions and known limitations

1. **Scanner discriminator.** The contract scanner keys on `ComponentGuid`, which also matches the
   `Goo…Param` parameter classes and, in `SAM`, one non-abstract base (`A_SAMAnalytical`). The canonical
   validator excludes them (GH Appendix 2 note) and reports how many it skipped, but the exclusion is a
   naming heuristic. A stronger discriminator (resolve the base-type chain to `GH_Component`) is a
   follow-up.
2. **Scanner copies.** The scanner exists in the canonical validator (inline) and in the
   `SAM_Psychrometrics` pilot copy and `SAM/design/grasshopper-icons/tools/inventory.py` (older, API-
   incompatible generation). Consolidating them is a follow-up; the canvas of *data* (the manifest) is
   already repository-specific.
3. **`diagnosticDetail_` vs `_diagnosticDetail_`** (GH-E3) — owner decision.
4. **CI.** No SAM-BIM CI job runs this validator yet. The runner images have Python available; the
   blocker is the per-repository baseline workflow, not the Python. See Appendix 5.
5. **`MinCompatibleVersion`** (GH-H5) is dead code in effect; removing or wiring it is a separate,
   owner-approved change.
6. **`SAM_OpenStudio_base`** appeared as a second checkout of SAM_OpenStudio during this investigation
   and is not a SAM-BIM repository; it is excluded from all counts above.

## Appendix 4 — Repository adoption plan

The canonical specification lives in `SAM-BIM/SAM`. Another repository does not copy it; it points at
it. Adoption per repository is: (1) a one-sentence instruction in that repository's `AGENTS.md`,
(2) a rendered baseline manifest, (3) a `--check` run. The proposed instruction snippet, written to be
appended to an existing `AGENTS.md` "Before starting work" list, is:

```markdown
- Before changing, adding or removing a Grasshopper component or any of its parameters,
  descriptions, defaults, visibility or version, read the SAM-BIM standard:
  https://github.com/SAM-BIM/SAM/blob/sow/2026-Q4/docs/GH_COMPONENT_STANDARDS.md
  Published parameter names, order, access, optionality, visibility and defaults are frozen;
  a rename needs a declared ParameterRename alias and the migration acceptance in that document.
  Validate metadata with:
  python <SAM>/design/grasshopper-icons/tools/param_contract.py --repo . --check
```

Order — small and low-risk first, and never mixing a description sweep with a rename:

| Wave | Repositories | Why |
|---|---|---|
| 0 | SAM (this PR's standard), SAM_Psychrometrics (already done) | The reference and the pilot. |
| 1 | SAM_Acoustic, SAM_GEM, SAM_gbXML, SAM_Origin, SAM_BHoM, SAM_IFC, SAM_Python | 1–3 components each, 0–6 deviations: a first manifest and a first green `--check` with almost no exceptions. |
| 2 | SAM_Excel, SAM_LadybugTools, SAM_Multitasker, SAM_OpenStudio, SAM_Topologic, SAM_Solver | Small, but each carries real description/version problems to triage first. |
| 3 | SAM_Systems, SAM_Mollier, SAM_SolarCalculator, SAM_UI | Mid-size; description corrections only, no renames. |
| 4 | SAM_OCCT, SAM_Revit | Own their own contract tests already; align their tests with the shared vocabulary. |
| 5 | SAM_Tas_Grasshopper | 94 components and 370 deviations — the largest GH surface after SAM. Bulk description work. |
| 6 | SAM | 460 components, 1120 deviations. Baseline only; fixes are per-component, owner-prioritised. |

Per repository, separate the three change classes (GH-F2) into separate PRs: description corrections
first (safe, no metadata freeze change), then additive optional parameters, then — only with an owner
decision and the full GH-G7 acceptance — any naming migration. **This document does not start any of
these waves**, and does not authorise a rename anywhere.

## Appendix 5 — Enforcement architecture and CI feasibility

Three distinct things, deliberately not conflated:

1. **Universal naming and description validation** — repository-independent: the naming rules, the
   description-quality rules, the capital-letter rule, `NickName == Name`, the access rule. Lives once,
   in `design/grasshopper-icons/tools/param_contract.py` in `SAM`. Reuse by `--repo`, not by copy.
2. **Repository-specific published metadata baseline** — the rendered `param_contract.json`, committed
   per repository. Data, not code.
3. **Approved legacy naming exceptions** — the `namingExceptions` list inside that baseline, kept
   honest by `--check` (a new deviation fails; a stale exception fails).
4. **Runtime migration compatibility tests** — Rhino-hosted, repository-local, per migration; not
   shareable, because each one pins that repository's real components and a real saved definition.

Feasibility, measured rather than assumed:

- The validator is pure standard-library Python 3. A full scan of `SAM` (460 components, ~1000 files)
  takes roughly 25 s; the whole active fleet is a few minutes. **Python availability is not the
  blocker** — `ubuntu-latest` and `windows-2022` both ship Python 3, and the local runs here used the
  RhinoCode Python 3.13.
- The real blockers are: (a) the scanner discriminator above; (b) Rhino-dependent runtime tests cannot
  run on the hosted runners, so they stay local/headless, exactly as `SAM.Core.Grasshopper.Tests` does
  today (it is deliberately **not** in `SAM.sln`); (c) a `--check` needs a committed baseline, so a CI
  job can only be switched on per repository *after* that repository's baseline PR.
- Recommended shape when the owner is ready: one workflow step in the repository's own `test.yml`
  (`uses: actions/checkout@v4` of `SAM` at a pinned ref, then
  `python <path>/param_contract.py --repo . --check`). Do not put Rhino-hosted tests in CI and do not
  add a fleet-wide job before the per-repository baselines exist.

This PR adds no CI job and no baseline for any repository other than the pilot already in place.
