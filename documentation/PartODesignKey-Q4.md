# Part O stable semantic design key (Q4) - SAM

Branch `feature/part-o-design-key` -> base `sow/2026-Q4` (cut from `021c36e9`). Record date: 2026-10-07.

## Current status

PR open, **not merged**. `Query.PartODesignKey(AnalyticalModel)` added; design `PartOModelReference`s record it
(`DesignKey`), and `PartOModelResolution` prefers it over the byte fingerprint where present. No SAM_UI change:
"previous compatible run found" / reopening a previous run is a later SAM_UI PR that consumes this key.
`SimulationResultProvenance.Fingerprint` is unchanged in definition and output.

## Why the byte fingerprint is unstable (verified in code, and by a probe)

`SimulationResultProvenance.Fingerprint` hashes the model's serialized JSON bytes. Part O design inputs that are
`SAMObject`s mint `Guid.NewGuid()` on construction and persist it: `PartOEquipmentSelection`, each
`VentilationUnitReference` in its pool, `PartOProjectTestVentilationUnit` (and its reference). The UI rebuilds
them on every read (`PartOEquipmentSelectionControl.Value`, `?? new PartOEquipmentSelection()` fallbacks), and
`PersistPartOInputs.SameValue` compares the full JSON, guids included, so the identical selection is written
back with new guids and the design's fingerprint moves. Probe: identical pool content, new guids -> different
fingerprint; save/reopen of one object -> identical; absent selection != explicit default. So the instability is
regeneration, not serialization. Every model `ParameterSet` also carries a build-derived (MVID) guid.

Not volatile: `PartOManualEquipmentSelection` and `PartODwellingStrategySet` (keyed by zone guid, products by
identity). Result-only state (isolation context, materialisation record, baseline reference, provenance,
scenarios) never exists on a design.

## Design

`PartODesignKey:v1:<sha256 hex>`: **one** SHA-256 (`IncrementalHash`) over **one** canonical stream; the existing
FNV state is not mixed in. Stream: schema tag; tagged sections - cluster, material library, profile library,
location (serialized as the fingerprint does, streamed, closed by 0xFF); model parameters as
`parameter|set|name|valueJson` lines **without the parameter-set guid** and without the fingerprint's existing
exclusions (`SimulationResultProvenance.ParameterNames_Excluded`, now shared) and the four Part O inputs; then
the Part O inputs as sorted guid-free lines:

- equipment selection: mode + pool as `manufacturer/model/reference`, de-duplicated; **absent == default**;
- project test product: name + both capacities where usable, else `none`;
- hand-picked products: `zone guid -> product identity`;
- dwelling strategies: `PartODwellingStrategy.CanonicalText()` (+ conflicts / unknown schema markers).

Meaningful identities stay in the key: cluster guids (space/zone identity is the baseline), Part F terminal
references. A Part F recalculation (re-minted requirement guids) is therefore a design change - deliberate.
Model name/guid/description, labels and view state are excluded, as in the fingerprint.

Persistence: optional `PartOModelReference.DesignKey` (design references only; schema stays v1; absent in old
saves = unknown, never inferred; not part of `IsValid`). `Create.PartOBaselineReferenceFromDesign` records it;
a result derived from a result inherits it with the design reference.

Resolution: `PartOModelResolution.Inspect` (design kind) compares `DesignKey` when the reference has one, else
the legacy `Fingerprint`. Result-kind references are unchanged.

## Files changed

- `SAM.Analytical/Query/PartODesignKey.cs` (new)
- `SAM.Analytical/Classes/PartOModelReference.cs` (`DesignKey`)
- `SAM.Analytical/Create/PartOBaselineReference.cs` (records it)
- `SAM.Analytical/Query/PartOModelResolution.cs` (prefers it)
- `SAM.Analytical/Classes/SimulationResultProvenance.cs` (exclusion list extracted to `ParameterNames_Excluded`; behaviour identical)
- `SAM.Tests/PartODesignKeyTests.cs` (new, 15 tests)
- `documentation/PartODesignKey-Q4.md` (this record)

## Validation

- `dotnet test SAM.Tests`: **2839 passed, 0 failed** (full suite, includes the 15 new tests and the unchanged
  `SimulationResultProvenanceTests` / `PartOBaselineReferenceTests` pinning the old fingerprint).
- New tests prove: regenerated guids alone -> same key (while the byte fingerprint differs); rebuilt test product
  -> same key; absent == default; pool order/duplicates; name/labels/view state; parameter-set guid regenerated;
  equipment mode / pool / manufacturer / reference / test capacity / manual product / strategy changes -> different
  key; cluster, location, north-angle changes -> different key; save/reopen (memory and `.sam` file, then selection
  rebuilt) -> same key; key is one SHA-256 over the documented stream (recomputed independently); taking the key
  mutates nothing; reference round trip with/without key; 2B inherits it; resolution Resolved after guid
  regeneration, Changed after a real edit, legacy no-key fallback unchanged.

## Risks / notes

- Cost: `PartOBaselineReferenceFromDesign` now serializes the cluster a second time (key beside fingerprint),
  once per run, streamed (no extra copy held). Mixed Design (which supplies its own fingerprint) also takes the key.
- Existing saved results have no `DesignKey`: they keep the byte-fingerprint behaviour (and its false "changed")
  until re-run. Not inferred.
- `PartOMaterialisationRecord.Fingerprint_Baseline` still uses the byte fingerprint (out of scope; same noise can
  affect that staleness check - candidate follow-up).
- SAM_UI root cause (UI rebuilds Part O inputs with fresh guids; `PersistPartOInputs.SameValue` compares guids)
  is untouched; the key makes it harmless for this question. Candidate SAM_UI follow-up.
- A future change to what the key covers requires a new schema tag (`v2`); old keys then compare as different.

## Recommended next step

Review/merge this PR; then a separate SAM_UI PR consuming `DesignKey` / `PartOModelResolution` for "previous
compatible run found" (and optionally comparing a live design's `PartODesignKey()` to a saved reference's).
`PROJECT_PROGRESS.md` closeout is added after merge as a direct docs-only commit on `sow/2026-Q4`.
