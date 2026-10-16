# SAM-BIM Unified Diagnostics Standard

**Status:** canonical. Describes behaviour that is implemented and merged; nothing here is aspirational unless
labelled **Open**. Changes go through a reviewed PR in `SAM-BIM/SAM`.
**Applies to:** every SAM-BIM repository that reports engineering diagnostics (warnings, skipped input,
assumptions, refusals) to a user, a log or a Grasshopper component.
**Sibling:** [GH_COMPONENT_STANDARDS.md](GH_COMPONENT_STANDARDS.md) owns Grasshopper naming, compatibility and versioning
(its section E points back here). The Grasshopper balloon rule in section 8 below is the only part
of Grasshopper behaviour this document states.

Source of truth is the code. If this text and the code disagree, the code wins and this text is a defect.

## 1. The API (`SAM.Core`)

| Type | File | Members |
|---|---|---|
| `DiagnosticSeverity` | `SAM/SAM.Core/Enums/DiagnosticSeverity.cs` | `Info` 0, `Warning` 1, `Error` 2 |
| `DiagnosticDetail` | `SAM/SAM.Core/Enums/DiagnosticDetail.cs` | `Summary` 0 (default), `Standard` 1, `Detailed` 2 |
| `Diagnostic` | `SAM/SAM.Core/Classes/Diagnostics/Diagnostic.cs` | immutable; `Severity`, `Detail`, `Code`, `Subject`, `SourceIndex`, `Message`, `Value`, `Limit`, `Unit` |
| `Diagnostics` | `SAM/SAM.Core/Classes/Diagnostics/Diagnostics.cs` | `Detail`, `Count`, `HasErrors`, `Wants(detail)`, `Add(Diagnostic)`, lazy `Add(severity, detail, subject, Func<string>, ...)`, `Filter(detail)`, `Format()`; enumerable |
| `Query.DiagnosticMessage` | `SAM/SAM.Core/Query/DiagnosticMessage.cs` | the one-line text format |
| `Query.TryGetDiagnosticDetail` | `SAM/SAM.Core/Query/TryGetDiagnosticDetail.cs` | `int` to `DiagnosticDetail`; returns false and `Summary` for an undefined value |
| `Query.Plural` | `SAM/SAM.Core/Query/Plural.cs` | count-aware wording |

Enum values are published. Never renumber or reorder them.

`Diagnostic` and `Diagnostics` are **runtime values**: not `IJSAMObject`, not serialised, independent of `Log` /
`LogRecord` / `LogRecordType` (which are unchanged). A null `message` becomes `""`. `Code`, `Value`, `Limit`,
`Unit` and `SourceIndex` are structured data and are **not** part of the text line.

## 2. Semantics that every producer must keep

1. **The producer owns the severity.** Choose the severity where the situation is detected, as an enum
   value. Never derive it from message text, code names or wording. (OCCT replaced a hard-coded code list
   and `Contains` text matching; TAS reads only its producers' own structure or marker constant.) A
   severity the producer never stated is not an error: TAS maps `LogRecordType.Undefined` to Warning rather than
   inventing a failure.
2. **Warnings and Errors survive every filter.** `Detail` applies to `Info` only. `Add` keeps every Warning and
   Error at any collection level; `Filter(level)` returns Info up to `level` plus *all* Warnings and Errors.
   Meaning: Info is what was done, Warning is input that was skipped or changed (or an approximation that can
   change results), Error is a run that could not complete.
3. **Detail never affects engineering results.** The level is a presentation concern. No geometry,
   classification, solver, simulation or numeric path may read it. Each adapter PR proved this with a
   same-input comparison at every level (panels identical at levels 0/1/2; TAS `.t3d`/`.tbd` and all TSD
   series identical; OCCT panels, cluster and `Successful` identical).
4. **A higher level includes every lower level** (Detailed ⊇ Standard ⊇ Summary).
5. **Do not hide a material Info at Summary.** Put a line at Summary when it records an approximation,
   fallback, assumption, dropped input or data source that can change results. OpenStudio's review found
   that a coarse per-code detail table hid such lines; the table was revised against every call site.
   Lower an Info only when it is known inert, and make unknown or reworded text stay visible.
6. **Drop-on-add is intentional.** `Add` discards Info above the collection's `Detail`, so `Filter` can only
   narrow. A caller that needs several views collects at `Detailed` and filters.
7. **Lazy messages.** Use `Wants(detail)` or the `Func<string>` overload for anything costly (lists, formatting
   many values) so a dropped record costs nothing.
8. **Default `Summary` changes default output.** Per-item Info lines that were once always shown now need level 1
   or 2. This was owner-accepted in Phases 2 and 3 and is recorded so it is not reported as a regression. A
   consumer that parses Info lines must request the level it needs (OCCT: level 2 equals the pre-change list).

## 3. Message and identity conventions

- Line format: `Severity: Subject: Message` via `Query.DiagnosticMessage` (`Severity` is `Info`, `Warning` or `Error`; the
  subject is omitted for summaries). Message: full sentences, first letter capitalised, units on every value.
- `Subject` identifies the source deterministically (`Solid #3 'Name'`). `SourceIndex` is the index in the
  producer's own input or record list so a structured record maps back to its legacy line.
- `Code` is stable and machine-readable. Released codes are never renamed, reused or renumbered; new codes may be added.
  Existing families: `PBS###` / `AGS###` / `SNP###` / `BYS###` (`SAM.Analytical.BySolidsDiagnosticCode`, where, for BySolids, 00x are Warnings,
  x00 totals, 1xx Standard Info and 2xx Detailed Info), `SAM-OS-*` / `SAM-OSI-*` (OpenStudio), `TAS-*`
  (`TasDiagnosticCodes`), `OPT###` (optimisation), OCCT `DiagnosticCode` names.
- `Value` / `Limit` / `Unit` carry the measured number, the threshold and the unit when a comparison produced the
  message. They are optional and must not replace the units written in the message.
- Group repeats rather than flooding: BySolids merges repeats of one reason within one plane; Grasshopper runtime messages group
  repeats (section 8). Grouping must not remove the full text from the Diagnostics output.

## 4. Three producer architectures, three adapter shapes

The unified types did not replace the producers' own diagnostics; each repository wrote an additive adapter around what it had.
Choose the shape that matches the producer, in this order of preference:

| Producer situation | Example | Shape | Rule learned |
|---|---|---|---|
| Producer you control, text-only output | SAM `PanelsBySolid`, `SnapToJunctions`, `AperturesBySolids` (SAM#202) | New overloads take a `Core.Diagnostics`; the legacy `out List<string>` overloads stay and delegate at `Detailed` | Keep the legacy signature and text. The legacy list equals the golden baseline. |
| Typed severity already exists, several mechanisms | OCCT (SAM_OCCT#78): `OcctDiagnostic`, `SolverDiagnostic`, plain strings | `SAM.Core.OCCT` adapters: `DiagnosticRecord` pairs the shared `Diagnostic` with the exact legacy text; severity maps one to one; one record per line where the line is produced | Where severity existed, map it. Where a line was unclassified legacy text, it is Info rather than an invented severity. |
| One typed diagnostic with a code and a severity | OpenStudio (SAM_OpenStudio#28): `OpenStudioDiagnostic` | `OpenStudioDiagnosticRecord` (record validated against its source so it cannot be inconsistent); `ToDiagnostics(detail = Detailed)` | Default adapter detail is `Detailed` so nothing the legacy output showed is dropped. The detail table is pinned by tests against every call site. |
| Many unrelated mechanisms, mostly strings | TAS (SAM_Tas#101): eight producers | One adapter per mechanism, plus one funnel edit (`AddNote`/`AddNotes`) that writes the same legacy text and a structured record side by side | Derive severity from structure the producer already has (a refusal list, a `Skipped` list, a documented marker constant). Where there is none, do not adapt it; document why. |

Adapter rules common to all three:

- **Additive.** No removed or changed public member. Legacy text, order and codes are unchanged. Verify with a public
  API diff (TAS: 0 removed, 0 changed) and, when a funnel was edited, an IL comparison of the changed methods.
- **No producer algorithm change.** Geometry, solver, simulation and numeric results are byte- or bit-identical to
  baseline; the PR proves it on real input, not only with unit tests.
- **Undefined enum values** are mapped deliberately and documented (OpenStudio: Error structurally, while its legacy
  `IsValid` ignores it; TAS: Warning). State the choice where the mapping lives.
- **Record vs diagnostic consistency.** If a record pairs a `Diagnostic` with legacy text, the constructor should reject a mismatch.

## 5. Legacy compatibility

- Legacy `out List<string>` / string-list outputs keep their text and order. They correspond to `Detailed` unless the owner decided otherwise
  (BySolids and OCCT outputs default to Summary by owner decision, see 2.8).
- Structured-only records may exist with no legacy line (`PBS007`) when they are *additional* Warnings that cannot hide a failure.
- A new Grasshopper input is a published-contract change. OpenStudio and TAS added none, by owner decision; see 8 and GH-E3 in the
  Grasshopper standard.

## 6. Error handling

- Failure paths carry an Error. If a failure path ends without one, add a component-level Error (OCCT does for Solve3D and CreateShells).
- Do not swallow exceptions into silent success. Existing catch sites were preserved; uncaught exceptions still propagate.
- Null input to an adapter: either skip nulls (`FormatDiagnostics`) or throw `ArgumentNullException` with the parameter name, and say which in the XML comment. TAS kept an
  `AddNotes(null)` throw to match the prior `AddRange(null)` behaviour.
- Fallback to unfiltered legacy output as Info (nothing hidden) if structured records ever disagree with the legacy list (OCCT Solve3D).

## 7. Testing rules

Each integration PR carried the same classes of evidence; reuse them.

1. **Severity preserved:** a test per producer mechanism that a Warning/Error stays Warning/Error at every level.
2. **Survival under filtering:** Warnings and Errors present at `Summary`.
3. **Mutation checks:** break the adapter on purpose (filter drops Warnings; Warning mapped to Info; Errors merged by code) and show tests fail (OCCT: 5, 5, 1 failures).
4. **Pinned tables:** every released code and every detail assignment (OpenStudio `DiagnosticDetailTableTests`).
5. **Real objects:** tests run the real converters, evidence objects and gates, not stubs.
6. **Engineering invariance:** same input at levels 0, 1, 2 gives identical engineering outputs; base vs head comparison for any real-input path.
7. **Grasshopper:** contract tests assert published input names and defaults; headless Rhino 8 (`NUnit` + `Rhino.Testing`) loads a saved definition and checks wires survive. Interactive canvas balloons are *not* proven by headless runs; record them as not run.
8. Record local vs hosted CI honestly, with the commands and the validated SHA (see AGENTS.md).

## 8. Grasshopper reporting (summary only)

Implemented in SAM BySolids and SAM_OCCT components: severity maps to the runtime message level (Info: `Diagnostics`/`log` output only;
Warning: GH Warning; Error: GH Error). OCCT shows up to 3 Warnings per code individually and then one "N more ... see Diagnostics" message; Errors are not merged unless text is identical.
Components that expose detail use the hidden optional integer `diagnosticDetail_` (0 default); an invalid value raises a Warning and falls back to Summary
(`Query.TryGetDiagnosticDetail`). OpenStudio and TAS components expose no detail input and raise no new balloons: their adapters are library API only.

**Open:** whether new components should spell the input `_diagnosticDetail_` (GH-E3), and wiring the TAS adapters into TAS Grasshopper components,
are owner decisions per component. This standard does not prescribe new inputs or balloon behaviour universally.

## 9. Checklist before changing Diagnostics

1. Read this document and the phase record of the repository you touch (`documentation/Diagnostics-Phase*-PR.md`).
2. Identify the producer's authoritative severity. If it has none, stop and ask.
3. Keep legacy signatures, text and order; add, do not replace.
4. Assign detail per section 2.5; add the pinned test.
5. Prove engineering outputs unchanged on real input.
6. If a Grasshopper contract (input, output, GUID, version) changes, follow GH_COMPONENT_STANDARDS.md instead of improvising.

## Appendix: evidence

| Phase | PR | Record |
|---|---|---|
| 1 Core | [SAM#201](https://github.com/SAM-BIM/SAM/pull/201), merge `293eb645` | `documentation/Diagnostics-Phase1-PR.md` |
| 2 BySolids | [SAM#202](https://github.com/SAM-BIM/SAM/pull/202), merge `fca3d4d4` | `documentation/Diagnostics-Phase2-BySolids-PR.md` |
| 3 OCCT | [SAM_OCCT#78](https://github.com/SAM-BIM/SAM_OCCT/pull/78), merge `de2744a7` | SAM_OCCT `documentation/Diagnostics-Phase3-OCCT-PR.md` |
| 4A OpenStudio | [SAM_OpenStudio#28](https://github.com/SAM-BIM/SAM_OpenStudio/pull/28), merge `f2235d45` | SAM_OpenStudio `documentation/Diagnostics-Phase4A-OpenStudio-PR.md` |
| 4B TAS | [SAM_Tas#101](https://github.com/SAM-BIM/SAM_Tas/pull/101), merge `ba9c95c8` | SAM_Tas `documentation/Diagnostics-Phase4B-TAS-PR.md` |

Test counts and acceptance results live in those records and in each repository's `PROJECT_PROGRESS.md`; they are not repeated here.
