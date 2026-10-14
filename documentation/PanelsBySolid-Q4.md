# Solid → mid-surface panels with unit warning — Q4 (2026-10-13)

Status: implemented on `feature/panels-by-solid-q4`; draft PR, owner review required. Not merged.
Base: `sow/2026-Q4` at `69f999cf97cf424609f2c4eece6d8a214605bccf`.
PR: the live draft PR for this branch in `SAM-BIM/SAM`; its description holds the final head and hosted checks.
Related:
- SAM-BIM/SAM_OCCT#76 (under-split guard in `SAMOCCT.CreateAdjacencyCluster`). It is independent, but the house result below needs both.
- The SAM_OCCT Air-panel / attic PR, which consumes the Air panels created here.

## Why

A Rhino house of closed solids was converted by a GH C# script. The script:

- reconstructed outlines as convex hulls, which lost the stair void, notches and holes;
- picked thickness faces by largest area only;
- used NURBS control points as outline samples;
- snapped against infinite planes inside inflated bounding boxes;
- took slider "mm" values in model units.

The Rhino → SAM conversion does not scale units. SAM is in metres, so a millimetre model silently produced millimetre analytical geometry.

## Change

- `Create.PanelsBySolid(Shell, …)` (pure SAM, metres):
  - The thickness pair is the opposite parallel faces with the **largest projected overlap**, so unrelated parallel ends are ignored.
  - The mid-surface is the **true section** (`Shell.Section`), keeping notches, holes and disconnected regions. Void faces are dropped by a point-in-material test.
  - Holes and notches claimed by aperture geometry (windows, doors) are filled; aperture footprints are tested inside the thickness band. Unclaimed holes stay open and get a separate **Air** panel. Unclaimed notches stay as the real outline.
  - Thin elements (`minThickness` 0.08 m) and non-panel-like elements (`maxThicknessRatio` 0.2) are skipped with a reason.
  - Every panel is stamped with `PanelSourceParameter` (SourceName, SourceId, SourceLayer, PhysicalThickness, SourceRole = "Opening" on Air). PhysicalThickness is not stamped on Air panels (2026-10-14).
- `Query.SnapToJunctions(panels, snapDistance)`: moves outer edges in-plane onto neighbouring mid-planes.
  - A move counts only where the target's actual face is within reach (a **local junction**), not just its infinite plane.
  - Extension goes to the farthest target ahead (wall past a ceiling T-junction up to the roof, which closes an attic). With nothing ahead, it trims to the nearest target behind.
  - Holes are untouched; Air panels are not moved and are not targets. Same Guids and parameters.
- `Geometry.Rhino.Query.ModelUnitsWarning()`: detects a non-metre Rhino document and explains the fix. It never converts.
- New GH `SAMAnalytical.CreatePanelsBySolids` (GUID `8b0f5f2c-…4d10`):
  - **refuses with an Error** in non-metre documents;
  - inputs: solids, aperture geometry, minThickness, snap, airForOpenings;
  - outputs: panels, airPanels, excluded, log;
  - takes name, id and layer from referenced Rhino objects.
- `SAMAnalytical.CreatePanelBy3DGeometry`: adds a **Warning** for non-metre documents. Its behaviour is otherwise unchanged.
- The global `Rhino → SAM` conversion is **not** changed.

## Validation (local, this machine)

```powershell
dotnet test SAM/SAM.Tests/SAM.Tests.csproj -c Debug
dotnet build Grasshopper/SAM.Analytical.Grasshopper/SAM.Analytical.Grasshopper.csproj -c Debug
```

- SAM.Tests: **3471 passed / 0 failed**, including `PanelsBySolidTests` **14 / 14**: wall, slab stair void → Air, aperture-claimed hole, Air disabled, door notch filled/kept, gable, thickness pair, stair tread and balustrade skipped, L-corner snap, distant parallel plane ignored, ceiling → roof extension, zero snap.
- GH project: compiles with 0 errors. The post-build deploy step fails on `xcopy … resources / Samples` ("File not found"), which is environmental; the copy of DLLs to `%APPDATA%\SAM` does run.
- End-to-end on the real house (machine-local Rhino 8 harness reading `Untitled2.3dm` scaled to metres, not committed):
  - 23 elements → 23 panels + **1 Air panel** (3.325 m², exactly the stair void). The stairs and balustrades are skipped. All window holes and door notches are claimed and filled.
  - After snap (0.3 m), `SAMOCCT.CreateAdjacencyCluster` (rebuild, no sew) gives **11 spaces**.
  - Solve3D after the snap over-splits the attic (13 cells), so it is not part of this workflow.
- Not run: hosted CI; manual Rhino/Grasshopper test.

## Remaining / next

- Owner review.
- Air identity through `CreateAdjacencyCluster` and the unconditioned attic belong to the SAM_OCCT follow-up.
- A robust unit-conversion architecture is out of scope (owner decision).

## Addition (2026-10-14): `SAMAnalytical.CreateAperturesBySolids`

Status: implemented on the same branch as a follow-up commit; owner review required. Not merged.

### Why

Window and door solids had no conversion to SAM apertures. The existing options don't fit solids:

- `Create.Apertures(geometries)` (used by the aperture parameter) makes one aperture per face of a solid.
- `AddAperturesByGeometryAndApertureType` hosts faces directly, so the frame and the glass would give two apertures.

The house has no Rhino groups. Each window is a frame ring plus a glass pane. The front door is a U-frame on the **Window Frames** layer plus a leaf on **Doors**. A per-object conversion would give 33 + 9 apertures, including a duplicate "window" over the front door.

### Change

- `Create.AperturesBySolids(IEnumerable<ApertureSolid>, out diagnostics, out excluded, apertureConstructions, maxGap = 0.01, maxDepthRatio = 0.5)` (pure SAM, metres). It does geometry only; hosting stays in `Modify.AddAperturesByApertures`.
  - **Assemblies:** members sharing a `GroupKey` (Rhino group) form one assembly. Members within `maxGap` of each other also join one: the test is a vertex inside, or within `maxGap` of, the other solid. Members with different keys are never merged.
  - **Type:** an assembly with any Door member is a Door, otherwise a Window. So the front door frame joins its leaf.
  - **Plane:** the member face normal along which the whole assembly is thinnest. This also works for stick frames, whose members are each thinner in-plane than deep. The aperture lies on the mid-plane of that depth.
  - **Boundary:** the outline of the union of all member faces projected onto the plane, with holes dropped. It is the full opening including the frame, with true dimensions. Gaps up to `maxGap` are closed by offsetting out and back in.
  - Assemblies deeper than `maxDepthRatio` of their smallest in-plane extent are skipped with a reason, e.g. a corner window across two walls.
  - **Construction:** the first supplied construction of the aperture's type. Otherwise the default for the type and orientation, as `Create.Apertures` does; that is the external default for walls.
  - **Traceability:** `ApertureSourceParameter` (SourceName, SourceId, SourceLayer, SourceThickness), with member values joined by "; ".
- `ApertureSolid`: an input record holding the geometry (a `Shell`, or a planar `Face3D` such as glass modelled as a surface), the explicit `ApertureType`, the source fields and the `GroupKey`.
- New GH `SAMAnalytical.CreateAperturesBySolids` (GUID `b115fc0d-c40f-4a98-938b-9b829a28cb95`).
  - Inputs: `windows_` and `doors_` (Breps, explicit type per input), `apertureConstructions_`, `maxGap_`, `useRhinoGroups_`.
  - Outputs: `apertures`, `excluded`, `log`.
  - It refuses with an Error in non-metre documents.
  - Name, Guid, layer and group come from referenced Rhino objects.
  - Connect `apertures` to `SAMAnalytical.AddAperturesByAperture`.
- No existing component GUID, input or public method changed. The `PanelsBySolidTests` geometry helpers became `internal` so the new tests reuse them.

### API gaps found (reported, not changed)

1. `Modify.AddAperturesByApertures` (the cluster/model path behind `AddAperturesByAperture`) rebuilds each hosted aperture from construction and face. It creates a new Guid and **dropped instance parameters**: 0 of 25 hosted apertures kept `ApertureSourceParameter`. **Resolved 2026-10-14** under the owner's traceability brief (see the addendum below).
2. Before hosting there is no host, so the default construction is the external one; internal doors get `SIM_EXT_SLD`. The existing `SAMAdjacencyCluster.SetDefaultApertureConstruction`, run after hosting, re-defaults by host panel (8 internal doors → `SIM_INT_SLD`, verified).
3. Aperture orientation follows the source member's face. Hosting does not align it to the panel; that is existing behaviour, unchanged.
4. Rhino 8 `RhinoDoc.OpenHeadless` cannot open the Rhino 9 WIP-saved morning model, and headless GH must run off the Rhino UI thread. Both are harness-only and do not affect the product.

### Validation (local, this machine)

```powershell
dotnet test SAM/SAM.Tests/SAM.Tests.csproj -c Debug
dotnet msbuild Grasshopper/SAM.Analytical.Grasshopper/SAM.Analytical.Grasshopper.csproj -t:Compile -p:Configuration=Debug -p:BuildProjectReferences=false
```

- **SAM.Tests:** 3485 passed, 0 failed. That includes 14 new `AperturesBySolidsTests`:
  - frame + glass; frame alone;
  - glass with a 5 mm gap;
  - door frame as window + leaf → one Door;
  - leaf alone; stick frame;
  - two windows apart;
  - touching windows merged without groups and kept apart by groups;
  - corner window skipped; rotated wall;
  - constructions by type; invalid member;
  - hosting on the correct wall with an Air panel untouched.
- **GH project:** compiles. A Rhino session was open, so the `%APPDATA%\SAM` deploy was not run. The new DLL was not deployed.
- **End-to-end** with the machine-local Rhino 8 harness (`apbs` mode, not committed; see the machine-local note below):
  - Baseline: PanelsBySolid + snap 0.3 → `SAMOCCT.CreateAdjacencyCluster` (SAM_OCCT#77 DLLs) gives 11 spaces, 56 panels and 1 Air panel of 3.325 m² (GF Hall + UF Landing).
  - Inputs: 33 window solids (Window Frames + Glass) and 9 door solids (Doors). They give **25 apertures: 16 windows and 9 doors**, with no exclusions. The front door is frame + leaf → 1 Door of 0.9 × 2.1 m.
  - API `AddAperturesByApertures` (maxDistance 0.1):
    - all 25 are hosted **exactly once**, with no duplicates and no area change;
    - windows are on their external walls, the front door on the GF Hall external wall, and the 8 internal doors on the partitions between their two rooms;
    - the spaces are 11 before and after, with identical names and volumes;
    - the Air panel is unchanged (Guid, area, spaces, no apertures).
  - **Real GH components** in a headless `GH_Document` (`CreateAperturesBySolids` → `AddAperturesByAperture`, defaults):
    - the apertures are identical to the API output;
    - the source name, Guid and layer come from the referenced objects;
    - `Successful` = True, with the same hosting result as the API run (25 once each, 11 spaces, Air unchanged);
    - with the document set to mm, the component refuses with the metres Error.
  - The original mm `Untitled2.3dm` (scaled ×0.001 in the harness) gives the same API result.
- **Not run:** hosted CI; the manual Rhino/Grasshopper UI test.

### Manual GH test

Metres model, e.g. `SAM_daily\2026-10-13-House\morning-test\Untitled2_meters.3dm`:

1. `windows_` ← solids from the Window Frames and Glass layers.
2. `doors_` ← solids from the Doors layer.
3. `CreateAperturesBySolids.apertures` → `AddAperturesByAperture._apertures_`, with `_analyticalObject` ← the `CreateAdjacencyCluster` result.
4. Optionally add `SAMAdjacencyCluster.SetDefaultApertureConstruction` for internal doors.

Expected: log "25 aperture(s) (16 window(s), 9 door(s))", `Successful` = True, 11 spaces and 1 Air panel unchanged.

Machine-local, not committed: the harness `census8` (modes `apbs`, `groups`) is in this session's scratchpad. Copy it before switching machines if it is needed again.

## Addition (2026-10-14, later): physical thickness and traceability through hosting

Status: implemented on the same branch as a follow-up commit; owner review required. Not merged.

The owner's approved design: a panel keeps the **physical thickness** of the building element it came from. It is independent of its thermal construction; for example, a 0.300 m Rhino wall stays 0.300 m whatever the TBD construction's layers add up to. Exporters use it as the geometric thickness (SAM-BIM/SAM_Tas, same branch name, direct T3D element width).

### Change

- `PanelSourceParameter.SourceThickness` (new on this PR, never merged) becomes **`PanelSourceParameter.PhysicalThickness`** ("Physical Thickness", m).
  - It stays in the source parameter group. That way SAM_OCCT's rebuild carries it with the other source values, without a compile dependency on this PR.
  - `ApertureSourceParameter.SourceThickness` (the window/door assembly depth) is unchanged.
- `Create.PanelsBySolid`:
  - The value is the distance between the thickness pair. That pair is the opposite parallel faces with the largest projected overlap, so the value does not depend on face orientation and is measured normal to a pitched roof's slope.
  - It is not stamped on Air panels (an opening has no material).
  - New diagnostic **"variable thickness"**: further opposite-face pairs parallel to the mid-plane that overlap at least 5% of the largest pair at another separation. The main (largest-overlap) thickness is stored, and the others are named; nothing is guessed silently.
- `Modify.AddAperturesByApertures` (behind GH `AddAperturesByAperture`): each hosted aperture is still rebuilt from construction and geometry, as before. It now also receives the source aperture's **instance parameter sets** (e.g. `ApertureSourceParameter`, colour, opening properties).
  - These are the very objects the host panel holds.
  - Every piece of an aperture split across two panels gets them.
  - The construction, Guid and geometry rules are unchanged; nothing host-derived existed on the rebuilt aperture to overwrite.

### Tests (`SAM.Tests`)

- `PanelsBySolidTests` (+4, and 2 assertions updated):
  - pitched roof thickness normal to the slope;
  - orientation independence;
  - stepped wall stores the main thickness and reports the other;
  - uniform wall reports none;
  - stair-void slab: host 0.2 m, Air none.
- `AperturesBySolidsTests` (+2):
  - source and other instance parameters survive hosting;
  - both pieces of a straddling aperture keep them.
- Focused: 34 / 0. The full suite result is in the PR description.

### House model (machine-local Rhino 8 harness, test3.gh chain, `morning-test\Untitled2_meters.3dm`)

| Source element | Rhino thickness = `PhysicalThickness` [m] |
|---|---:|
| External walls, gables | 0.25 |
| Partitions | 0.10 |
| Ground and upper floor slabs | 0.20 |
| UF ceiling | 0.15 |
| Roof slopes | 0.20 |

- After SAM_OCCT#77's `CreateAdjacencyCluster`, all 55 physical panels keep source name, layer and `PhysicalThickness`. The Air panel keeps its own.
- After `AddAperturesByAperture`, all 25 apertures keep `ApertureSourceParameter`, e.g. "GF front door frame; GF front door".

### Notes for the GH acceptance

- `test3.gh` **internalises** its 23 solids, so `CreatePanelsBySolids` sees no Rhino object and stamps no name, id or layer. Reference the solids (e.g. `Query Model Objects` by layer, as the window/door inputs already do) to keep them.
- Without seed spaces `CreateAdjacencyCluster` names the spaces `Cell 1..11`, and the default text map gives them all `S37_OfficeCell`. Supply named seeds through `spaces_`: `SAMAnalytical.CreateSpace` with `name_` and `locationPoint_`, from the house's room labels on `Ground Floor::Labels` / `Upper Floor::Labels`, plus `Attic Unconditioned`.
