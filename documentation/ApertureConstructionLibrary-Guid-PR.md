# Default aperture construction library: fixed Guid for `SIM_EXT_GLZ` (Q4) - SAM

Branch `fix/aperture-library-sim-ext-glz-guid` -> base `sow/2026-Q4` (cut from `4b2e6450`). Record date: 2026-10-08.

## Current status

PR open, **not merged**. One resource value changed, and one test file added. No product source changed.

## Why

`files/resources/Analytical/SAM_ApertureConstructionLibrary.JSON` had a Window `SIM_EXT_GLZ` with
`"Guid": "4d00dd0-f646-4fbb-90e6-f8d9cd6634eb"`. Its first group has only 7 hex digits.
`SAM.Core.Query.Guid(JsonObject)` uses `Guid.Empty` when `TryParse` fails, then replaces it with
`Guid.NewGuid()`. So this construction got a **new random identity on every load**.

Two consumers identify systems by Guid and need that identity to be stable:
- SAM_UI's Glazing window (`GlazingCandidate`);
- the native Optimisation glazing choice. This trap was found in SAM_Tas PR7a-2,
  `SAM_Tas/SAM.Analytical.Tas.GenOpt/NATIVE_OPTIMISATION_PR7A2_GLAZING.md` (SAM_Tas#90).

## Work completed

- **Resource fix:** the value is now `04d00dd0-f646-4fbb-90e6-f8d9cd6634eb`. Nothing else in the file changed. A byte
  comparison confirms that the file equals the original with only that one value replaced. The original had one
  occurrence; the file keeps UTF-8 without BOM, LF line endings and no trailing newline.
- **Test** `SAM/SAM.Tests/ApertureConstructionLibraryGuidTests.cs`. It reads the library from the repository's
  source path (`Helpers.Fixtures`), as `LibraryFixtureTests` does:
  - `EveryApertureConstruction_HasGuidThatParses`: every raw `Objects[*].Guid` is a canonical non-empty Guid
    (`TryParseExact(..., "D")`).
  - `EveryApertureConstruction_GuidIsStableAcrossLoads`: two independent loads produce the same Guid set as the file,
    and every Guid in the file is distinct. It also pins `04d00dd0-…` to the Window named `SIM_EXT_GLZ`.

## Decisions / assumptions

- **Why `04d00dd0-…`:** it adds the missing leading zero, so the hex value of the first group does not change and the
  link to the old text stays visible. It is a valid, well-formed GUID: version nibble 4, variant `9`. This value
  appears nowhere else in any SAM-BIM repository.
- No persisted data can refer to the old value. It never survived a load, so a fixed replacement breaks nothing.
- `LibraryFixtureTests.ApertureConstructionLibrary_RoundTrip` could not catch this defect. It compares the second and
  third serialisations, and by then the random Guid has already been assigned.

## Files changed

- `files/resources/Analytical/SAM_ApertureConstructionLibrary.JSON` (one value)
- `SAM/SAM.Tests/ApertureConstructionLibraryGuidTests.cs` (new)
- `documentation/ApertureConstructionLibrary-Guid-PR.md` (this record)

## Validation

- Guid scan of all 21 `*.json` files under `files/resources` (every key containing `Guid`, 1817 values). Before the
  fix, this one value was the only malformed one. After the fix, none are malformed.
- Old-value references: none in this repository outside the fixed entry and its test. Across the sibling SAM-BIM
  checkouts, the only other mention is the SAM_Tas PR7a-2 record, which documents the defect (not a fixture, not
  changed).
- Duplicates: the 12 library Guids are distinct.
- Before the fix (resource stashed), both new tests fail. One reports `ApertureConstruction 'SIM_EXT_GLZ' has an
  invalid Guid '4d00dd0-…'`.
- After the fix: `dotnet test SAM/SAM.Tests -c Release` passed 3357 tests, with 0 failed.
- PR CI: see the PR checks.

## Issues / risks

- **Installed copies** (the SAM install's resource folder) keep the bad value until the next SAM deploy.
- A **persisted `ActiveSetting` settings file** may have serialised a random Guid for this entry. It keeps that value
  until the default library is reloaded.
- The library contains five entries named `SIM_EXT_GLZ`, each with a distinct Guid. That is the
  existing data and is unchanged here. Consumers must identify these entries by Guid, not by name.

## Next step

Wait for PR CI to pass and for the owner to merge. Then add the `PROJECT_PROGRESS.md` closeout on `sow/2026-Q4`.
Optional follow-up: the SAM_Tas PR7a-2 record can name the fixed value `04d00dd0-…` once this PR has merged.
