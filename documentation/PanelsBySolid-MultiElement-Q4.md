# PanelsBySolid: solids with several elements — Q4 (2026-10-14)

Status: implemented on `feature/panels-by-solid-multi-element-q4`; draft PR, owner review required. Not merged.
Base: `sow/2026-Q4` at `02a51568c72704797396714e364e722c109e054c`.
PR: the live draft PR for this branch in `SAM-BIM/SAM`; its description holds the final head and hosted checks.
Follows: SAM#194 (`documentation/PanelsBySolid-Q4.md`), which introduced `Create.PanelsBySolid` and `Query.SnapToJunctions`.

## Why

`SAMAnalytical.CreatePanelsBySolids` dropped large parts of a more complex house. The house is
`SAM_daily/2026-10-13-House/contemporary1/contemporary1.3dm` (Rhino 9 file), used by `import-test.gh`.

Input census (headless Rhino 9 WIP RhinoCommon 9.0.26279):

- Units are metres; absolute tolerance 0.01 m.
- All 19 `_solids` and all 18 glazing solids are valid, closed, manifold and fully planar Breps.
- `_solids` is one flattened Model Object list.
- No input was lost before the component: no data-tree, unit, validity or small-face problem.

## Root causes (measured)

1. **One mid-plane per solid.** `PanelsBySolid` picked one thickness pair and sectioned the solid on that plane only. Everything not on that plane was silently dropped:
   - `GF_Wall_External` and `UF_Wall_External` are each one ring of all external walls (66 and 60 faces). Each became one wall panel, covering 16.6% and 18.0% of the paired face area.
   - Each roof solid holds two pitches; only one pitch became a panel (58%).
   - L-shaped internal walls lost a leg (69–73%).
   - In total, 17 panels covered 55.5% of the paired face area.
2. **Panel-like ratio.** `UF_Gable_Cross_South` is a 6 × 1.4 m gable, 0.3 m thick. Thickness over height is 0.214, so the 0.2 default rejected it. That left the roof space over the master bedroom open.
3. **Normal sign followed face order.** Both roof solids came out as `Floor` with their normals pointing down.
4. **Snap junction tested only at the edge midpoint** (`Query.SnapToJunctions`).
   - The intermediate slab has a 4.2 × 4.5 m stairwell/gallery void that reaches the west facade.
   - The midpoint of the west walls' horizontal edges lies over the void, so neither wall was joined to the slab, although the slab meets both ends of the edge.
   - This left a 0.25 m slot between the GF and UF walls.
5. Secondary: the "variable thickness" diagnostic listed separations across the whole building (1.4 … 12 m).

## Change

### `Create.PanelsBySolid` (`SAM.Analytical`, public signature unchanged)

Each solid is decomposed into element planes:

- **Pairs.** Parallel faces at least `minThickness` apart whose projections overlap, and that have material between them. The material test samples ¼, ½ and ¾ of the way through the solid.
- **Planes.** Pairs on one mid-plane form one element plane: the coplanar walls of a ring, or a face split into pieces.
- **Ownership.** Material belongs to the thinnest element through it. Planes are taken thin to thick. A pair whose material lies in an accepted, thinner, non-parallel element (or within one thickness of its overlap, which covers corners) is a cross-cut, not an element. Examples: the wall under a window sill, cut horizontally by the sill and the wall's base; the top and bottom of a ring of walls.
- **Panel-like.** Each remaining plane is tested against `maxThicknessRatio`.
- **One element, several thicknesses.** A parallel plane whose slab contains, or lies within, a larger-overlap plane's slab is the same element. The main (largest-overlap) thickness is stored and the other is reported as "variable thickness". Examples: a stepped wall; the 0.1 m of wall behind the house's 0.2 m-deep window niches.
- **Support.** Each plane is sectioned as before (`Shell.Section`). A section region is kept only where the plane's own pairs support it. Elsewhere the mid-plane only cuts across another element of the solid (reported as cross-sections).
- **Orientation.** Mid-plane normals are oriented so that `Query.PanelType(normal)` is independent of face order: vertical planes are `Wall`, horizontal planes face down (`Floor`) and sloped planes face up (`Roof`).
- **Diagnostics.**
  - Planes are numbered with their normal when a solid has several.
  - Ignored non-panel-like planes are listed.
  - The not-panel-like skip names the ratio that would keep the element.
- Holes, notches, aperture claiming, Air panels and `PanelSourceParameter` stamping (including `PhysicalThickness`, measured per element) are unchanged.

### `maxThicknessRatio` default 0.2 → 0.25 (decision for owner review)

Set in `Create.PanelsBySolid`, in the GH component's persistent default, and in its fallback.

| Element | thickness / smallest extent | at 0.2 | at 0.25 | at 0.3 |
|---|---:|---|---|---|
| contemporary `UF_Gable_Cross_South` (wanted) | 0.214 | rejected | **kept** | kept |
| accepted 14 Oct house `Stair tread 5–14`, 0.95 × 0.25 m blocks (not wanted) | 0.263 | rejected | **rejected** | **kept (regression)** |
| contemporary stair flight | 0.398 | rejected | rejected | rejected |

- 0.3 was tried first. It turned ten stair-tread blocks of the accepted house into panels (58 cluster panels / 2 Air instead of 56 / 1), so it was rejected.
- A 0.95 m tread block is geometrically the same as a short wall pier, so no extent measure separates the two populations. 0.25 lies between them.
- Owner alternative: keep 0.2 and set `maxThicknessRatio_` = 0.25 on the definition.

### `Query.SnapToJunctions`

A junction is accepted when any of five points along the moved edge (middle, ends, quarter points) is within the snap distance of the target face. Previously only the midpoint was tested.

### GH `SAMAnalytical.CreatePanelsBySolids`

- Version 1.0.1 (was 1.0.0), so existing instances show as outdated.
- Description and the `_solids` / `maxThicknessRatio_` texts mention multi-element solids.
- Default ratio 0.25.
- GUID, inputs and outputs are unchanged.

## Files

- `SAM/SAM.Analytical/Create/PanelsBySolid.cs`
- `SAM/SAM.Analytical/Query/SnapToJunctions.cs`
- `Grasshopper/SAM.Analytical.Grasshopper/Component/SAMAnalyticalCreatePanelsBySolids.cs`
- Tests:
  - `SAM/SAM.Tests/PanelsBySolidMultiElementTests.cs` (new, 11 cases)
  - `SAM/SAM.Tests/PanelsBySolidComplexHouseTests.cs` (new, 17 cases)
- Fixture: `SAM/SAM.Tests/Golden/PanelsBySolid/contemporary1_solids.json` (new, 394 KB).
  - It holds the 19 solids and 18 glazing solids of the house, converted exactly as the component converts them (`GH_Brep.ToSAM(true)` → `Shell`), with name, layer and Rhino id.
  - Copied to the output by the existing `Golden\**\*.json` item.
- This record.

## Validation (local, this machine; Validated content SHA: see PR description)

```powershell
dotnet test SAM/SAM.Tests/SAM.Tests.csproj -c Debug
dotnet msbuild Grasshopper/SAM.Analytical.Grasshopper/SAM.Analytical.Grasshopper.csproj -t:Compile -p:Configuration=Debug -p:BuildProjectReferences=false
```

Build with `APPDATA` and `USERPROFILE` redirected (and `NUGET_PACKAGES` set to the real cache). Otherwise the project's post-build step deploys to `%APPDATA%\SAM`.

- **SAM.Tests (full suite):** 3518 passed, 1 failed: `QueryWaitToUnlockTests.WaitToUnlock_FileUnlockedWhileWaiting_ReturnsTrue`, a timing-based file-lock test that is unrelated to this change. It ran while the machine was loaded and passed 3/3 when rerun alone. An earlier full run on this branch (before the snap change and the 0.25 default) passed 3516/3516.
- **Focused:** `PanelsBySolid*` and `AperturesBySolids*` 62 / 62. Existing PanelsBySolid tests are unchanged and pass, including the stepped wall, tread, balustrade and snap tests.
- **Mutation checks:**
  - With the previous `PanelsBySolid.cs`, 14 of the new tests fail.
  - With the previous `SnapToJunctions.cs`, the new slab-void snap test fails.
- **GH project:** compiles, 0 errors.

### Complex house coverage

Fixture-based, Rhino-free, in CI as `PanelsBySolidComplexHouseTests`.

The implementation-independent oracle takes every pair of opposite faces 0.08–0.6 m apart with at least 1 m² of overlap and material between them. Every such element side of every solid except the stair is ≥ 95% covered by a parallel panel of the same solid (test passes; zero exceptions).

Other assertions:

- No two panels of one solid overlap within the element thickness.
- Panel types: walls `Wall`; roofs `Roof` with an upward normal; slabs `Floor` with a downward normal.
- `PhysicalThickness`: external walls and gables 0.3; internal walls 0.15; roofs and intermediate slab 0.25; ground slab 0.2.
- Snap keeps every panel valid and every Guid.

Side-face coverage per solid. "Before" is the original code, "after" is this branch, both measured by the harness metric. Faces paired 0.05–0.6 m with ≥ 0.01 m² of overlap count, which also includes door nibs and window heads; that is why some "after" values are below 100%.

| Solid | before | after | note |
|---|---:|---:|---|
| GF_Wall_External (ring, 66 faces) | 16.6% (1 panel) | 93.3% (10 panels) | remainder: the ring's 13.9 m² top face, counted whole because it pairs with the 0.6 m² head over the terrace door, plus window heads; all cross-cuts that lie in the wall panels |
| UF_Wall_External (ring, 60 faces) | 18.0% (1) | 99.9% (10) | |
| RF_Roof_Main_Ridge_X (2 pitches) | 58.7% (1) | 99.1% (2) | |
| RF_Roof_Cross_Ridge_Y (2 pitches) | 58.3% (1) | 98.8% (2) | |
| UF_Gable_Cross_South | 0% (rejected) | 100% (1) | |
| GF_Wall_Internal ba24690f (L) | 69.2% (1) | 99.7% (2) | |
| GF/UF_Wall_Internal with door nibs | 72.9–100% | 72.9–100% | nib ends and door heads only (end faces, not element sides) |
| other gables, slabs | 99.4–100% | 99.4–100% | |
| ST_Stair_GF_to_FF | 0% | 0% | intentional: not panel-like (ratio 0.398) |
| **Total** | **55.5%** | **97.2%** | |

Panels: 17 before → 39 after, 0 Air. 33 `Wall`, 4 `Roof`, 2 `Floor`.

### Real component on the real model

Machine-local Rhino 9 WIP harness, not committed:

- `SAMAnalytical.CreatePanelsBySolids` v1.0.1 was run in a headless `GH_Document`.
- Inputs as in `import-test.gh`: 19 referenced solids and 18 glazing solids, defaults otherwise.
- Result: 39 panels and no runtime messages.
- All 39 carry source name, id and layer.
- `PhysicalThickness` values: 0.3 ×24, 0.25 ×5, 0.15 ×9, 0.2 ×1.
- Log: "1 solid(s) produced no panel" (the stair, with its ratio).

### Regression: the accepted 2026-10-14 simple house

`morning-test/Untitled2_meters.3dm` was run through the same chain: PanelsBySolid with the 42 window and door solids, snap 0.3, then SAM_OCCT `Create.AdjacencyCluster` with the component's rebuild recipe and the SAM_OCCT `build` DLLs.

- 23 panels + 1 Air (3.325 m²).
- 16 stair and balustrade solids skipped.
- Cluster: **11 spaces, 56 panels, 1 Air**.
- This is identical to the accepted baseline in `PanelsBySolid-Q4.md`.

## Downstream on the contemporary house: source-model openings (not conversion defects)

`SAMOCCT.CreateAdjacencyCluster` builds no closed cell from the corrected panels.

A free-edge map of the snapped panels shows the cause: the drawn envelope has two openings, which no panel conversion can close.

1. **Front door.**
   - The GF external wall has a 1.0 × 2.1 m opening at x 5.7–6.0, y 8–9.
   - The model has no door geometry; the opening layers hold glazing only.
   - The opening is an unclaimed notch, kept as the wall's real outline.
2. **Roof cut-out over the terrace.**
   - The main roof is cut out over the terrace recess (x 6–10, y 0–2.5).
   - The UF walls around the recess stop at z 5.75.
   - Nothing fills the gaps up to the roof (about 0.2–1.3 m high, roughly 8 m²).

With a door solid added as aperture geometry and three infill blocks added in the harness only, the cluster closes: **1 space, 547.6 m³**.

It is one space, not rooms, because of the model's topology:

- **Doorways:** the internal doorways (5) and open-plan openings (2) have no doors.
- **Stairwell void:** the 4.2 × 4.5 m void reaches the slab edge, so it is a notch rather than a hole and gets no Air panel.
- **UF rooms:** there is no UF ceiling, and the UF partitions stop at 5.75 m under the sloped roof.

These are modelling and topology decisions for the owner (see Next).

## Unresolved / risks

- **Owner decision on the default ratio:** 0.25 here, or 0.2 with the input set on the definition.
- **Owner decision on intended topology:** how unclaimed notches should be treated (doorways, a void reaching a slab edge). Today they are kept as outline. Air for every notch would also turn the plan concavities of an L-shaped slab and the external front door into Air, so it needs context (spaces or walls) and was not changed.
- `SnapToJunctions` can still only translate edges. A wall under a sloped roof cannot grow a gable-shaped top (unchanged behaviour).
- Pre-existing, unrelated, not changed: `Vector3D` → `Point3D` implicit operator in `SAM.Geometry` (`Vector3D.cs`) recurses forever (it returns `new Vector3D`). It causes a stack overflow for any caller that converts a vector to a point. Small follow-up PR.
- Harness-only observation: Rhino 9 WIP `RhinoDoc.OpenHeadless` returns an empty document for `Untitled2_meters.3dm`; `File3dm.Read` reads it.
- Manual Rhino/Grasshopper UI acceptance: **not run** by the agent.

## Machine-local evidence (not committed)

- The harness is a Rhino.Testing NUnit project.
  - Configured for `C:\Program Files\Rhino 9 WIP\System`.
  - Tests: census, coverage, ratio sweep, fixture export, real component, simple-house chain, OCCT cluster, naked edges, repaired model.
- It lives in this session's scratchpad. Copy it before switching machines if needed again.
- The committed fixture and tests reproduce the conversion result without Rhino.

## Next

1. Owner review of the PR, including the ratio default.
2. Manual Grasshopper check (steps in the PR description).
3. Owner topology decision, then a separate PR if wanted: doors in the openings, or Air for doorways and voids, using space or wall context. Fix the model's envelope openings (front door solid; infill walls over the terrace recess).
