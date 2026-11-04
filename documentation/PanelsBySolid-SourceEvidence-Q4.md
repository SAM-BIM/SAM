# PanelsBySolid source evidence (SAM producer) - Q4 (2026-11-03)

Status: draft PR from `feat/panelsbysolid-source-evidence-q4` into `sow/2026-Q4` (base `7c4591cda22f0f4ad0ec07abad08350b28ec884f`).
Draft, for owner review: not ready for review, not merged, not approved. The PR number and the final head are in the PR
description. Tested content SHA: `6eddd56d98ac9f19f613481b923f0b90096f470b`. This record-only commit follows it and changes only
this file.

History: first delivery tested at `e2bfbf2d` (record head `aeafa38f`). Owner review (2026-11-04) accepted the dedicated `Panel`
field and JSON key, certifying each piece from its own solid, and leaving diagnostics unchanged (decisions 1, 4, 6), and asked
for one fix: `Panel.SourceEvidence` returned an instance shared by panel copies while `SourceEvidence.FromJsonObject` was public
and could overwrite it. Fixed in `6eddd56d` (below); the PR stays a draft.

This is the SAM producer half of design rev 5 (`DESIGN-contact-aware-void-guard.md`, sections 1 and 2, owner sign-off
2026-11-03; machine-local evidence under `SAM_daily/2026-10-25-Softthouse/handover/evidence/2026-11-03-d203-trace/`).
The SAM_OCCT consumer (section 3: the Clean3D decision) is **not** in this PR; it is its own follow-up session and branch.
Until both land nothing changes for users: no existing code reads the record.

## Work completed

- `SourceEvidence` (`SAM/SAM.Analytical/Classes/SourceEvidence.cs`): the optional, all-or-nothing record. `Plane` (frame at
  extraction), `GuaranteedThickness` g, `Support`, `Origin`, `BoundFootprint` (`Face2D` lists in that frame, inner loops kept).
  **Immutable once constructed**: the regions and frame it returns are copies, `WithBoundFootprint` makes a new record, and
  `IJSAMObject.FromJsonObject` is an explicit interface implementation that always returns false and changes nothing (panels
  and their copies share one instance, so overwriting it would change all of them without the rebinding check). The JSON
  constructor is the only reader. JSON save/reload, `IsComplete` (frame, g > 0 and non-empty regions).
- `Create.PanelsBySolid` certifies material (`Create/PanelsBySolidSourceEvidence.cs`) once per element plane and stamps each
  host panel (never Air panels). Only for closed, valid, layered prismatic solids; otherwise no record and nothing changes:
  - closure by edge pairing (every face valid, at least 4 faces, every edge straight, every non-degenerate edge matched by
    exactly one other edge within `Tolerance.Distance`; `Shell.IsClosed` is not used);
  - positional classification about the host normal with delta = `Tolerance.Distance` metres over the actual extent (parallel:
    vertex offsets within delta; perpendicular: every edge within delta of along/across the normal, with one of each); no angles;
  - one section per layer between the parallel-face offsets inside the slab, `Support` = intersection of the material regions
    (internal point `Inside`, inner loops kept); a layer thinner than the snap tolerance fails; no `Shell.Difference`; an empty
    or null section is a failure; intersections use `Query.DefiniteIntersection`;
  - g = 2 x the least half-extent about the host plane over the element plane's pairs and nested pairs.
- `Panel` carries, copies and saves the record (`Panel.SourceEvidence`, JSON key `SourceEvidence`). All internal copy
  constructors copy it (it is never modified in place, so copies share it). `SetSourceEvidence` is internal.
- `Query.ValidSourceEvidence(panel[, out reason], tolerance)`: the record is evidence only while (a) its frame equals the
  panel's (origin 1e-9, normal and Y axis same direction to 1e-12 of 1 - cos), (b) the panel's footprint equals
  `BoundFootprint` (symmetric difference at most `tolerance * perimeter(BoundFootprint)`, definite-result intersection), and
  (e) the record is complete with g > 0. A kernel failure is no evidence.
- `Query.SnapToJunctions` rebinds at its one `Create.Panel(panel.Guid, panel, face3D, null, false)` site: a record that is valid
  on the input panel is bound to the snapped footprint and re-checked on the result; any other record is **dropped** from the
  snapped panel. Copy-only paths (zero snap distance, unsettled set, no target) return the panel unchanged, record included.
- `Query.DefiniteIntersection` is public so the consumer applies the same null/empty semantics.

## Important decisions and assumptions (1, 4 and 6 accepted by the owner on 2026-11-04)

1. **Storage.** Rev 4 said "one parameter value on the panel"; this implementation uses a dedicated `Panel` field and JSON
   property instead, so the record follows every `Create.Panel` copy without touching `ParameterSet`s and cannot be edited
   through the parameter API. Behaviour is as the design describes.
2. **Conditions (c) and (d) are consumer-side.** Stage A "plane unmoved within `Tolerance.Distance`" and `SourceIndices.Count == 1`
   need solver state; `ValidSourceEvidence` implements (a), (b), (e) only, and its documentation says so.
3. **Stale evidence never revives through snapping.** A same-plane edit keeps the frame, so a stale record could match the
   footprint again if snapping extends the edge back to the bound footprint. Snapping therefore drops any record that is not
   valid on its input rather than carrying it. Tested (below); the fail-first mutation was run.
4. **Pieces.** Rev 3/4 text and acceptance item 2 mention "pieces by minimum/union". `PanelsBySolids` never merges pieces into
   one SAM panel: every panel, including those of the continuation/replacement pass, is read from its own solid through the same
   method, so each carries its own record. Panels merged from several sources are made in SAM_OCCT, where rev 5 excludes them
   by `SourceIndices.Count == 1`. No union or minimum is computed here. Owner to confirm this reading.
5. **Per-panel regions.** `Support` is certified once per element plane (all regions) and each panel keeps the regions whose
   bounding box overlaps the panel's by the snap tolerance. `Origin` is the panel's face at extraction (aperture-claimed openings
   and notches filled, unclaimed holes left open), as the prover used it.
6. **No new diagnostics.** A solid that is not certified gets no record and no message, so existing diagnostics (including the
   Detailed text) are unchanged. Owner may want a Detailed Info code; it would change goldens that pin Detailed output.
7. **Public surface added:** `SourceEvidence`, `Panel.SourceEvidence`, `Query.ValidSourceEvidence` (two overloads),
   `Query.DefiniteIntersection`. Nothing existing changed. No Grasshopper component, parameter, version or description touched.

## Files changed

- `SAM/SAM.Analytical/Classes/SourceEvidence.cs` (new; `6eddd56d` makes it immutable)
- `SAM/SAM.Analytical/Create/PanelsBySolidSourceEvidence.cs` (new): closure, positional classification, layered certification
- `SAM/SAM.Analytical/Query/ValidSourceEvidence.cs` (new): validity, definite intersection, region area/perimeter helpers
- `SAM/SAM.Analytical/Classes/Panel.cs`: field, copies, property, JSON
- `SAM/SAM.Analytical/Create/PanelsBySolid.cs`: certify and stamp in `PanelsByElementPlane`
- `SAM/SAM.Analytical/Query/SnapToJunctions.cs`: rebind
- `SAM/SAM.Tests/PanelsBySolidSourceEvidenceTests.cs` (new, 39 tests)
- `documentation/PanelsBySolid-SourceEvidence-Q4.md` (this record)

## Tests (`SAM.Tests/PanelsBySolidSourceEvidenceTests.cs`, xUnit, `Method_State_Expected`)

- Certification through `PanelsBySolid`/`PanelsBySolids`: prismatic wall; stepped wall (D203 #250 profile, g 0.548122); slab with
  stair void (host recorded, Air panel not); rectangular tunnel past the mid-plane (support 8.25 of 8.55 m2, pocket excluded);
  diamond tunnel, 45 degree chamfer, 30 m wall 2 mm off at the far end -> no record; 30 m wall 0.5 micrometre off -> record.
- Direct (`Create.TryCertifyMaterial`, internal seams for the section and intersection): positional bound (5e-7 accepted,
  2e-6 rejected); end face missing; T-junction split face; layer thinner than the snap tolerance; null and empty section; null
  intersection with overlapping bounds; `DefiniteIntersection` disjoint/overlapping/real.
- Save/reload: record equal after `Convert.ToString` -> `Create.IJSAMObject<Panel>` (frame, g, region areas and perimeters) and
  still valid; JSON round trip; panel JSON without the key; incomplete record dropped on load.
- Validity: copies stay valid; same-plane trim, extension and split (frame kept, record invalid, "footprint differs"); merged
  pieces; `Move` 1 mm; rotation; scaling; `FlipNormal`; same footprint on a rebuilt frame.
- `SnapToJunctions`: rebind to the snapped footprint (facade 17.4 -> 18.0 m2, `BoundFootprint` 18.0, `Origin` 17.4); valid after
  save/reload and a second pass; **same-plane edit then snapping back to the bound footprint leaves no record** (the trimmed
  edge returns to exactly 18.0 m2, the unedited panel keeps its record); panel without a record stays without; zero snap
  distance keeps records.

- Shared-record immutability (added in `6eddd56d`): overwriting a panel's record, the one `ValidSourceEvidence` returns, and a
  copy's record through `IJSAMObject.FromJsonObject` with a forged thickness and no support is refused and leaves the original
  panel and its copy intact and valid; the `Support`, `Origin`, `BoundFootprint` and `Plane` handed to a caller are copies
  (moved, emptied and offset without effect on the panel or its copy); a record is still read by
  `Core.Create.IJSAMObject<SourceEvidence>`.

Not in this PR (consumer side, SAM_OCCT): Stage A plane move of 2e-6, `Absorb`, the Clean3D decision, controls A-D, T1, T2, W.

## Validation

Local, Windows, Debug, `dotnet test SAM/SAM.Tests/SAM.Tests.csproj --no-build`, with `APPDATA` pointed at a scratch folder so the
build deployed nothing to `%APPDATA%\SAM`. Reproduce from `SAM/` of the worktree:

```
APPDATA=<scratch> dotnet build SAM.Tests/SAM.Tests.csproj -c Debug
APPDATA=<scratch> dotnet test SAM.Tests/SAM.Tests.csproj --no-build
APPDATA=<scratch> dotnet test SAM.Tests/SAM.Tests.csproj --no-build --filter "FullyQualifiedName~PanelsBySolidSourceEvidenceTests"
```

| Check | Result |
| --- | --- |
| Baseline build of `sow/2026-Q4` `7c4591cd` | 0 errors |
| Full `SAM.Tests` suite at the working tree that became `e2bfbf2d` | **4017 passed, 0 failed, 0 skipped** (36 new) |
| Full `SAM.Tests` suite at `e2bfbf2d` | **4017 passed, 0 failed, 0 skipped**, 3 m 44 s |
| New tests alone at `e2bfbf2d` | 36 passed, 0 failed |
| Fail-first mutation: `RebindSourceEvidence` returns early for an invalid record (stale record carried by snapping) | `SnapToJunctions_SamePlaneEditThenSnapBackToTheBoundFootprint_StaleEvidenceStaysDropped` failed, 35 others passed; reverted |
| Full `SAM.Tests` suite at the working tree that became `6eddd56d` (after the immutability fix) | **4020 passed, 0 failed, 0 skipped**, 3 m 28 s (39 new) |
| New tests alone at that tree | 39 passed, 0 failed |
| Fail-first for the fix: the three new immutability tests against `SourceEvidence.cs` from `aeafa38f` | `SourceEvidence_OverwrittenThroughTheJsonReader_ChangesNeitherThePanelNorItsCopy` failed; the two copy tests passed (the getters already returned copies); fix restored |
| Hosted checks at `aeafa38f`, as reported by the owner from the logs (not re-read here) | Release solution build and SPDX passed; Release `SAM.Tests` 4017/4017; no reviews or comments on the PR |
| Extraction cost: `PanelsBySolidComplexHouseTests`, `PanelsBySolidsTests`, `PanelsBySolidMultiElementTests` (59 tests), two runs each | with change 10 s and 11 s; base 12 s and 11 s: no measurable overhead |

Existing tests are unchanged (no golden or parameter-set edit).

## Unresolved issues, risks, blockers

- **Not run:** hosted CI at the head that carries the immutability fix (it is in the PR description once it reports); local
  Release configuration (Release ran in hosted CI at `aeafa38f`); `SAM.Core.Grasshopper.Tests` (needs Rhino; no
  Grasshopper surface touched); Chain N0/N1/N2 on the saved Softthouse R/F sets and the 392-solid census (need the machine-local
  data and the SAM_OCCT consumer).
- **Real-data confirmations still open** (design section 7): footprint-equality tolerance against the real `SnapToJunctions`
  rounding (367 of 367 frames matched in the prover; here only the synthetic corner and a JSON round trip are covered); record
  size (three region lists per panel; not measured); coverage 80.9 % of solids about a horizontal normal. Slab/roof host normals:
  one slab with a void is tested, no census.
- Residuals the proof does not check, as designed: self-intersection and outward orientation of the solid.
- `Face2D.Intersection` null semantics: a null with overlapping bounds is treated as failure (fail closed).

## Next step

Owner: review the new head and its hosted checks, then mark the PR ready. The consumer is in its own session: SAM_OCCT branch
`fix/clean3d-contact-void-guard-q4` from `sow/2026-Q4`, **new session, Plan then Build, Sonnet 5.5, High effort** (a large solver
change that needs a plan against `SnappedPanel`/Clean3D first), controls A-D, T1, T2, W first. It reads `Panel.SourceEvidence`
through `Query.ValidSourceEvidence`, adds conditions (c) and (d), and must build against this SAM branch (check how its CI
resolves the SAM dependency by branch name before relying on it). Merge order: SAM first. After merge, add the
`PROJECT_PROGRESS.md` closeout with the merge SHA. Commit and push before switching computers.
