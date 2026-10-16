# Unified engineering standards consolidation - PR record

Base: `sow/2026-Q4` (`6167d474`). Branch: `docs/unified-engineering-standards-q4`. Documentation only.
Draft; merge needs owner approval (AGENTS.md). `PROJECT_PROGRESS.md` is not touched (post-merge closeout only).

## Status
Documents written and checked locally. No source, test, build or Grasshopper contract change. Not merged.

## Investigation (what already existed)
- The Grasshopper guide **was already promoted** to SAM: `docs/GH_COMPONENT_STANDARDS.md` (SAM#204, 892 lines, status-labelled rules), with the Psychrometrics pilot records
  (`SAM_Psychrometrics/docs/GH-Component-Metadata-Standard.md`, SAM `documentation/GH-Parameter-Rename-Migration-Q4.md`, `GH-Component-Standards-Q4.md`). No second Grasshopper guide was created.
- No Diagnostics standard existed. Its content was spread over four phase records and section E of the GH standard, which still said Phase 4 was "not started" and the document itself "draft PR #204, not merged".
- Phase records: SAM `documentation/Diagnostics-Phase1-PR.md`, `Diagnostics-Phase2-BySolids-PR.md`; SAM_OCCT `Diagnostics-Phase3-OCCT-PR.md`; SAM_OpenStudio `Diagnostics-Phase4A-OpenStudio-PR.md`; SAM_Tas `Diagnostics-Phase4B-TAS-PR.md`.

## Work completed
- New `docs/DIAGNOSTICS_STANDARD.md`, written from the merged source (`SAM.Core` `Diagnostic`, `Diagnostics`, enums, `Query.DiagnosticMessage`, `TryGetDiagnosticDetail`) and the phase records.
- `docs/GH_COMPONENT_STANDARDS.md` (additive): status line corrected (merged); section E points to the Diagnostics standard and the Phase 4 statement is corrected; new GH-B7 (execution triggers `_run` / `run_` / `_run_`, with counts and examples read from source), GH-I3a (`Name` versus `NickName`), and section K (new components versus legacy quick reference).
- `AGENTS.md`: short mandatory "Engineering standards" section linking both documents.

## Decisions and assumptions
- Two documents were requested; the Grasshopper one already exists, so it was extended rather than duplicated, keeping its stable rule identifiers.
- `_diagnosticDetail_` vs `diagnosticDetail_` (GH-E3) and wiring TAS adapters into components remain OPEN owner decisions; the documents prescribe neither.
- `PROJECT_PROGRESS.md` lacks closeouts for Phases 3, 4A and 4B in SAM. AGENTS.md forbids editing it on a PR branch, so the cross-reference is deferred to the post-merge closeout; the standard's appendix carries the PR and merge-SHA table meanwhile.
- No test counts are repeated; they stay in the phase records.

## Files changed
`docs/DIAGNOSTICS_STANDARD.md` (new), `docs/GH_COMPONENT_STANDARDS.md`, `AGENTS.md`, this record.

## Validation (local)
- API names, enum values, signatures and defaults in the Diagnostics standard compared with `SAM/SAM.Core/**` source.
- Trigger spellings and counts from `grep` of `Name = "…"` in the Grasshopper projects; examples read in `SAMAnalyticalCheck.cs`, `SAMAnalyticalMergeSpacesByZones.cs`, `GeometryPolycurveLoop2D.cs`, `SAMPsychrometricsPsychrometrics.cs`.
- Name/NickName wording compared with `SAM_Psychrometrics/docs/GH-Component-Metadata-Standard.md` §2.
- Markdown relative links and rule cross-references checked; `git diff --stat` shows only the files above. Not run: the Python validator tests (no Python on this machine); they are unaffected by a Markdown change.

## Risks and next step
Duplicate statements may drift between the two standards; each defers to the other for its own area. After merge: post-merge closeout including the Phase 3/4 cross-reference. Then per-repository `AGENTS.md` links (separate PRs): SAM_OCCT, SAM_OpenStudio, SAM_Tas, SAM_Tas_Grasshopper, SAM_Psychrometrics.
