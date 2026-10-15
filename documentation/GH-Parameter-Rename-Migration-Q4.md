# Grasshopper published-parameter rename migration (2026-Q4)

**Status:** implementation complete; local Rhino 8 validation green; **draft PR, not merged** (owner
review required). This is the shared-`SAM` half of the rename pilot; its consumer is the SAM_Psychrometrics
PR that renames exactly one published output.

- Repository: `SAM` (shared infrastructure). Consumer: `SAM_Psychrometrics`.
- Branch: `feature/gh-parameter-rename-migration-q4`, base `sow/2026-Q4` `d6c3569286082b33ac8d76dc9ec4a57bfa345bc6`.
- Validated content SHA: `d32e70061ed9e97b9c42f12819bd53fe814416d5` (code + tests). The PR record is a
  docs-only commit on top.
- Cross-repository dependency: the SAM_Psychrometrics PR renames `enthalpy` → `Enthalpy` and compiles
  against this branch. Both branches deliberately share the same name so the downstream build's dependency
  resolution clones this branch of `SAM` rather than falling back to `sow/2026-Q4`. Merge order: this PR
  first, then SAM_Psychrometrics.

## Why

Grasshopper stores a component's parameters in the definition itself and restores them from there, so a
reopened definition keeps the **file's** names, not the names the class now declares. That makes renaming a
published parameter a breaking change:

- every `Params.IndexOfInputParam` / `IndexOfOutputParam` lookup in `SolveInstance` returns `-1` for every
  existing definition, with no compile error;
- `Modify.DuplicateComponent` matches parameters by name, so the updater reports the wire and the typed-in
  value "dropped" and the person must reconnect by hand;
- the replacement is a newly constructed instance, so it has a new `InstanceGuid`, and `GH_Group` records
  membership by that identity — the component silently leaves its group.

The earlier metadata-standard pilot deliberately froze the names and recorded a deferred migration. This
change implements that migration safely, and proves it on one controlled rename.

## Work completed

1. **Declarative renames.** `GH_SAMComponent.ParameterRenames` returns the published renames a component
   has made (`ParameterRename`, legacy name → current name, per side). Default is `null`: a component that
   declares nothing behaves exactly as before.
2. **Unambiguous resolution.** `ParameterRenameMap` validates a declaration and resolves a legacy name to
   exactly one current name. Duplicate legacy names, chained renames, empty names and same-name renames are
   reported as problems and refused, never guessed.
3. **Alias-aware wire reconnection.** `Modify.RestoreConnections` resolves each captured connection through
   the map. `Modify.CaptureConnections` now also records the old parameter's runtime type, and a redirect
   whose parameter **type changed** is refused and reported (a wire to a parameter of a new type would be a
   different wire).
4. **Alias-aware persistent-data transfer.** `Modify.CopyPersistentDataFromComponent` resolves the map and
   reports a migrated value; `CopyPersistentData` now returns whether it transferred, so the log stays
   honest about what actually moved.
5. **SolveInstance compatibility.** `GH_SAMComponent.IndexOfInputParameter` / `IndexOfOutputParameter` fall
   back to the declared legacy name, so an un-updated definition still computes.
6. **Missing-wire verification.** `Query.MissingConnections` resolves the map too, so a wire restored onto a
   renamed parameter is not then reported to the person as a missing wire.
7. **Group membership.** `Modify.ReconcileGroupMembership` moves every `GH_Group.ObjectIDs` entry from the
   replaced identity to the replacement's, through the group's public surface.
8. **Partial-failure safety.** Everything after `AddObject` in `DuplicateComponent` is wrapped: if a step
   throws, the half-built replacement is removed and the original component is left untouched. A broken
   `ParameterRenames` declaration is caught and reported and never stops an otherwise-fine update.
9. **Version gating / no automatic change.** Renames stay inert until a component raises
   `LatestComponentVersion`, which is what makes it *obsolete* and offers the explicit *Update component*.
   Nothing renames on file opening.

## Instance identity: why reconciliation, not adoption

`InstanceGuid` is get-only on `IGH_DocumentObject`; the only way to set a specific value is the public
`NewInstanceGuid(Guid)`, whose purpose is to mint a fresh identity (paste/undo). `GH_Document` keeps an
identity-keyed object cache (`m_objCache`) populated at `AddObject` time, so adopting an old identity after
adding relies on non-public bookkeeping, and a duplicated identity while both objects are alive is
undefined. Adopting the old guid was therefore rejected as unsafe.

A reflection sweep of the loaded Rhino 8 Grasshopper assembly (`Grasshopper 8.35.26251.13001`, evidence in
the PR) found `GH_Group.ObjectIDs` (`List<Guid>`) as the **only** persisted `Guid` collection naming document
objects. Everything else is the document's own object list/cache (managed by `AddObject`/`RemoveObject`),
an attribute back-reference rebuilt with the attribute (`IGH_Attributes.DocObject`), a wire-level parameter
reference (managed by re-adding sources), or transient session state (selection, event args, previews).
Reconciling group membership is therefore both necessary and sufficient, and uses only public members.

## Decisions and assumptions

- Renames are **declared**, not inferred. No heuristic scans old definitions.
- A redirect is attempted only when the old and new parameters have the same runtime type; otherwise it is
  refused and the pre-existing dropped-wire report still fires (no silent data loss).
- Conflicting/malformed declarations are reported and refused; the update itself still completes.
- Group reconciliation runs for **every** successful replacement, with or without a rename, because a
  replacement always changes `InstanceGuid`.
- `RestoreConnections`, `MissingConnections` and `CopyPersistentDataFromComponent` gained overloads; the
  original signatures delegate with an empty map, so this is source-compatible for callers.
- No behaviour change for a component that declares no renames (verified by the existing 71 tests plus a
  new one that asserts no migration is ever logged).

## Files changed

Added under `Grasshopper/SAM.Core.Grasshopper/`:

- `Classes/ParameterRename.cs` — one declared rename (side, legacy name, current name).
- `Classes/ParameterRenameMap.cs` — validation and unambiguous resolution.

Modified:

- `Classes/GH_SAMComponent.cs` — `ParameterRenames`, `IndexOfInputParameter`, `IndexOfOutputParameter`.
- `Classes/ParamConnection.cs` — records the old parameter's runtime type name.
- `Modify/CaptureConnections.cs` — populates the type name.
- `Modify/RestoreConnections.cs` — alias-aware, type-checked, ambiguity-refusing.
- `Modify/CopyPersistentData.cs` — returns whether it transferred.
- `Modify/CopyPersistentDataFromComponent.cs` — alias-aware, with diagnostics.
- `Modify/MissingConnections` (`Query/MissingConnections.cs`) — alias-aware verification.
- `Modify/UpdateComponent.cs` — reads and validates the replacement's renames; transactional after add.
- `Modify/UpdateComponents.cs` — reconciles group membership for each successful replacement.

Tests and fixtures:

- `Grasshopper/SAM.Core.Grasshopper.Tests.Fixtures/TestComponents.cs` — three fixture components: a declared
  rename, an ambiguous rename, and a rename declaration that throws.
- `Grasshopper/SAM.Core.Grasshopper.Tests/ParameterRenameMigrationTests.cs` — 13 tests.

Nothing was removed and no existing public signature changed.

## Validation (local, Windows, Rhino 8 `8.35.26251`, `Release`)

| Check | Command | Result |
|---|---|---|
| Full solution build | `dotnet build SAM.sln -c Release` | 0 errors |
| Core regression | `dotnet test SAM/SAM.Tests/SAM.Tests.csproj -c Release` | **3683 passed, 0 failed** (baseline) |
| Grasshopper (real headless Rhino 8) | `dotnet test Grasshopper/SAM.Core.Grasshopper.Tests/SAM.Core.Grasshopper.Tests.csproj -c Release -p:SolutionDir=<repo>\` | **84 passed, 0 failed** (71 existing + 13 new) |
| Whitespace | `git diff --check` | clean |

Covered by `ParameterRenameMigrationTests`: legacy-name lookup fallback; renamed output wire restored;
renamed input wire and persistent value preserved; a type change refused and reported; a conflicting
declaration reported and not applied; a throwing declaration tolerated; group membership moved to the
replacement (with and without a rename); an unrenamed component unchanged; idempotent re-runs; forced
replacement failure leaves the document unchanged; and direct `ParameterRenameMap` resolution.

**Hosted checks:** pending; record the final head and results before marking ready.

**NOT TESTED:** no interactive (mouse-driven) Rhino session. The IDENTITY reasoning is a reflection sweep
plus the runtime tests, not a canvas observation. The transactional cleanup after `AddObject` is defensive:
the only deterministically injectable failure is the constructor throw, which the tests exercise; the
per-step post-add failures are unreachable in practice because each step already catches internally.

## Unresolved issues, risks and blockers

- `SAM.Core.Grasshopper.Tests` is not part of `SAM.sln` (it needs Rhino), so the hosted `build` check does
  not compile it and the hosted `test` job does not run it, as before. The new tests were run locally.
- The mechanism is additive and inert until a component declares a rename; the risk is therefore confined
  to renamed components.
- Renaming a parameter is still a breaking change for any **other** consumer that looks up the old name
  outside the SAM updater (for example a C# script in the definition). That is inherent, and the
  `SolveInstance` fallback only covers the component's own lookups; it is documented in the consumer PR.

## Exact next step

1. Push this branch and open one **draft** PR into `sow/2026-Q4`.
2. Open the SAM_Psychrometrics draft PR (same branch name) and confirm the downstream build resolves this
   branch of `SAM` from its log.
3. Owner review. Merge this PR first, then SAM_Psychrometrics; do not release or repin an installer.
