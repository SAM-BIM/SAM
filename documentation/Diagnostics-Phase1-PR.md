# Unified Diagnostics, Phase 1 (SAM.Core) - PR record

Base: `sow/2026-Q4` (`a71435dd`). Branch: `feature/core-diagnostics-phase1-q4`. PR: see PR description.

## Status
Implementation complete, local tests pass. Awaiting owner review. Not merged.

## Work completed
Additive types in `SAM.Core`: `DiagnosticSeverity` (Info=0, Warning=1, Error=2), `DiagnosticDetail`
(Summary=0, Standard=1, Detailed=2), `Diagnostic` (immutable: Severity, Detail, Code, Subject, SourceIndex,
Message, Value, Limit, Unit), `Diagnostics` (collection: Detail, Count, Add x2, Wants, Filter, HasErrors, Format).

## Decisions
- Runtime values, not `IJSAMObject`: nothing persists them; avoids a serialisation contract. Independent of `Log`/`LogRecord`/`LogRecordType`, which are unchanged.
- `Diagnostics` is collected at a requested `Detail` (default Summary). `Add` drops Info above it; Warning/Error always kept. `Wants(detail)` = `detail <= Detail`. `Filter(level)` returns Info up to `level` plus all Warnings/Errors.
- Lazy `Add(severity, detail, subject, Func<string>, ...)` builds the message only if kept (no allocation for dropped Detailed records at Summary).
- `Diagnostic.ToString()`/`Format()` reuse `Query.DiagnosticMessage` (`Severity: Subject: Message`); Code and numeric values are structured only, not in the text, so existing line format is unchanged.
- netstandard2.0 / LangVersion 12 compatible (no records, no `IReadOnlyList` extras).
- Version: no assembly/package version is declared in the SAM.Core project and no policy requires a bump for additive API; none made.

## Files changed
`SAM/SAM.Core/Enums/DiagnosticSeverity.cs`, `DiagnosticDetail.cs`; `SAM/SAM.Core/Classes/Diagnostics/Diagnostic.cs`, `Diagnostics.cs`;
`SAM/SAM.Tests/DiagnosticsTests.cs`; this record. No geometry, panel, Grasshopper or other-repo changes.

## Validation (local)
`dotnet test SAM/SAM.Tests/SAM.Tests.csproj` on the branch: 3567 passed, 0 failed, 0 skipped (includes the new DiagnosticsTests).
GitHub checks: see PR.

## Risks / not done
Out of scope by design: BySolids migration, Grasshopper parameters, OCCT and OpenStudio/TAS adapters.

## Next step
Owner review and merge; then Phase 2 (migrate PanelsBySolid/AperturesBySolids to `Diagnostics`).

## Known limitation: drop-on-add (owner-approved)
`Add` discards Info records above the collection's `Detail`, so `Filter(level)` can only narrow what was kept.
A collection built at Summary cannot later be filtered up to Standard or Detailed. Callers that need to re-filter
at several levels must collect at `Detailed` and call `Filter`. Warnings and Errors are unaffected.
