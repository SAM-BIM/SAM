# PanelsBySolid: solids with several elements — Q4 (2026-10-14)

Status: implemented on `feature/panels-by-solid-multi-element-q4`; draft PR SAM-BIM/SAM#198, owner review required. **Not merged.**

Open owner decisions: the `maxThicknessRatio` default (see "maxThicknessRatio: design analysis") and the room topology rules (see "Contemporary house: why one space").

Base: `sow/2026-Q4` at `02a51568c72704797396714e364e722c109e054c`.
PR description: holds the final head and hosted checks.
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
2. **Panel-like ratio.** `UF_Gable_Cross_South` is a 6 × 1.4 m gable, 0.3 m thick. Thickness over height is 0.214, so the 0.2 default rejected it.
3. **Normal sign followed face order.** Both roof solids came out as `Floor` with their normals pointing down.
4. **Snap junction tested only at the edge midpoint** (`Query.SnapToJunctions`).
   - The intermediate slab has a stairwell void that reaches the west facade.
   - The midpoint of the west walls' horizontal edges lies over the void, so neither wall was joined to the slab, although the slab meets both ends of the edge.
   - This left a 0.25 m slot between the GF and UF walls.
5. Secondary: the "variable thickness" diagnostic listed separations across the whole building (1.4 … 12 m).

## Change

### `Create.PanelsBySolid` (`SAM.Analytical`, public signature unchanged)

Each solid is decomposed into element planes.

- **Pairs.**
  - Two parallel faces at least `minThickness` apart whose projections overlap, with material between them.
  - The material test samples ¼, ½ and ¾ of the way through the solid.
  - The pair's separation is the element's `PhysicalThickness`. It is measured from the opposing faces and never taken from a construction.
- **Planes.** Pairs on one mid-plane form one element plane: the coplanar walls of a ring, or a face split into pieces.
- **Ownership.** Material belongs to the element in which it is most panel-like.
  - Planes are ranked by thickness over the band width of their overlap (2 × area / perimeter).
  - A pair whose material lies in an accepted, non-parallel element is a cross-cut, not an element. "Lies in" means inside the element's overlap, or within one thickness of it, which covers corners.
  - Examples: the wall under a window sill, cut horizontally by the sill and the wall's base; the top and bottom of a ring of walls.
  - The band width tells a ring's walls (thin across, long) from the ring's top and bottom faces, which have an 8 × 6 m bounding box around a 0.3 m band.
  - A bounding-box ranking was tried and rejected: low rings came out as `Floor`, and extra planes appeared in the house.
- **Panel-like.** Each remaining plane is tested against `maxThicknessRatio` (unchanged meaning: thickness over smallest in-plane extent).
- **One element, several thicknesses.**
  - A parallel plane whose slab contains, or lies within, a larger-overlap plane's slab is the same element.
  - The main (largest-overlap) thickness is stored, and the other is reported as "variable thickness".
  - Examples: a stepped wall; the 0.1 m of wall behind the house's 0.2 m-deep window niches.
- **Support.**
  - Each plane is sectioned as before (`Shell.Section`).
  - A section region is kept only where the plane's own pairs support it. Elsewhere the mid-plane only cuts across another element of the solid.
- **Orientation.** Mid-plane normals are oriented so that `Query.PanelType(normal)` is independent of face order: vertical planes are `Wall`, horizontal planes face down (`Floor`) and sloped planes face up (`Roof`).
- **Diagnostics.**
  - Planes are numbered with their normal when a solid has several.
  - Ignored non-panel-like planes are listed.
  - A not-panel-like skip names the ratio that would keep it.
- Unchanged: holes, notches, aperture claiming, Air panels and `PanelSourceParameter` stamping.

### `Query.SnapToJunctions`

A junction is accepted when any of five points along the moved edge (middle, ends, quarter points) is within the snap distance of the target face. Previously only the midpoint was tested.

### GH `SAMAnalytical.CreatePanelsBySolids`

- Version 1.0.1 (was 1.0.0), so existing instances show as outdated.
- **New Warning** when any solid is skipped as not panel-like. It names the solids and points to the ratio in the log, so a thick or low wall is never skipped silently.
- Descriptions mention multi-element solids.
- Default `maxThicknessRatio_` is **0.25 (provisional, owner decision pending)**.
- GUID, inputs and outputs are unchanged.

## maxThicknessRatio: design analysis (owner question, 2026-10-14)

**What it is.** A classification heuristic.

- It skips solids that are not panel-like: stairs, treads, balustrades.
- It compares a plane's thickness with its smallest in-plane extent.
- It is not a thickness limit: `PhysicalThickness` is always the measured opposing-face distance.
- As a proportion, however, it **does reject valid thick or low walls**.

**Wall matrix (synthetic, this branch).** Single walls and 8 × 6 m rings; panels at ratio 0.20 / 0.25 / 1.0:

| wall t (m) | h × L (m) | t / min(h, L) | 0.20 | 0.25 | 1.0 | PhysicalThickness |
|---:|---|---:|:-:|:-:|:-:|---:|
| 0.1 | any tested | ≤ 0.1 | ✓ | ✓ | ✓ | 0.1 |
| 0.2 | 1 × 1 … 4 × 10 | ≤ 0.2 | ✓ | ✓ | ✓ | 0.2 |
| 0.3 | 2.4–4 × ≥ 3 | ≤ 0.125 | ✓ | ✓ | ✓ | 0.3 |
| 0.3 | any × 1 (pier) | 0.3 | ✗ | ✗ | ✓ | 0.3 |
| 0.6 | 2.4 × ≥ 3 | 0.25 | ✗ | ✓ | ✓ | 0.6 |
| 0.6 | 3–4 × ≥ 3 | ≤ 0.2 | ✓ | ✓ | ✓ | 0.6 |
| 1.0 | 2.4–3 × ≥ 3 | 0.33–0.42 | ✗ | ✗ | ✓ | 1.0 |
| 1.0 | 4 × 10 | 0.25 | ✗ | ✓ | ✓ | 1.0 |

Rings of 0.1–1.0 m walls, 2.4–4 m high, give exactly four `Wall` panels at ratio 1.0, with `PhysicalThickness` equal to the wall thickness. The one exception is a 1.0 m × 1.0 m-section ring, which is genuinely ambiguous (as thick as it is high).

The decomposition no longer depends on the threshold. Ratio sweep on the contemporary house:

- Identical panels from 0.25 to 0.35.
- The stair appears only at ≥ 0.4.
- A 0.15 m lintel plate over the 2 m terrace opening appears only at ≥ 0.5.

**Regression models, 0.20 vs 0.25:**

| Model | 0.20 → 0.25 change |
|---|---|
| `Untitled.3dm` (mm, scaled) | none (24 panels; skipped: 14 treads 0.263–0.8, 2 balustrades) |
| `Untitled2.3dm` (mm, scaled) | none (24 panels; same skips) |
| `morning-test/Untitled2_meters.3dm` (accepted 14 Oct) | none (24 panels: 23 + 1 Air); cluster 11 spaces / 56 panels / 1 Air, unchanged |
| `contemporary1.3dm` | `UF_Gable_Cross_South` (0.214) kept; nothing else |

`contemporary.3dm` contains no objects.

No previously rejected face other than the gable is accepted at 0.25, so there are no false positives on the available models. 0.30 was rejected: it turns the accepted house's ten 0.95 m tread blocks (0.263) into panels.

**Why no global ratio is the long-term answer.** A 0.95 × 0.25 m stair-tread block (0.263) is geometrically identical to a short wall pier. A 1.0 m historic wall 3 m high scores 0.33, which is *less* panel-like than the tread. Any single proportion that keeps the wall also keeps the tread.

**Recommendation.**

1. For SAM#198, **retain 0.20** (exact backward compatibility for single-element solids):
   - The PR then fixes only the multi-element defect, the orientation and the snap.
   - Keep the new Warning and the needed-ratio diagnostic.
   - Set `maxThicknessRatio_` = 0.25 explicitly on `import-test.gh` for the gable.
   - Alternative: 0.25 is evidence-safe on all available models, but it is still a proportion and still rejects 1 m walls below 4 m.
   - If 0.20 is chosen, `PanelsBySolidComplexHouseTests` passes 0.25 explicitly, and the gable and thick-wall tests document the skip.
2. **Follow-up PR (robust correction).**
   - Exclude non-building objects explicitly: do not wire them, or add an exclusion input or layer rule.
   - Then default the ratio to 1.0, meaning "the thickness is the element's thinnest direction".
   - The matrix shows this keeps 0.1–1.0 m walls with exact `PhysicalThickness`.
   - It changes the accepted `test3.gh` workflow, which feeds stair solids, so it needs owner approval.

## Excluded solid: the stair (intentional)

| Property | Value |
|---|---|
| Name / layer | `ST_Stair_GF_to_FF`, `SAM_Demo_LHouse::03_Stairs` |
| Rhino id | `d8182294-90be-4fe7-bbf8-e898ffeb4344` |
| Brep | valid, closed, manifold; 37 planar faces; volume 7.56 m³ |
| Extent | x 3.075–4.2 (1.125 m wide), y 2.52–7.0 (4.48 m going), z 0–3.0 (3.0 m rise) |
| Shape | a solid stepped mass on a flat base: 17 risers of 0.176 m, 16 goings of 0.28 m, base 1.125 × 4.48 m |
| Best element plane | its two sides, 1.125 m apart over the stepped profile; smallest extent 2.824 m → ratio 0.398 |
| Other planes | tread tops against the base: ratio ≥ 0.63 |

Reason: not panel-like at 0.20 or 0.25. It is kept only at ≥ 0.4. It is intentional: a stair is an object inside the hall/stairwell, not a space boundary.

- If included, it would become two panels: 1.125 m-thick "walls" and a floor inside the hall.
- The stairwell boundary is the slab void, discussed below.
- The component now reports it as a Warning.

## Contemporary house: why one space (diagnosis)

`SAMOCCT.CreateAdjacencyCluster` finds no closed cell from the corrected panels, because the drawn envelope is open in two places:

- **E1 front door.** The GF external wall has a 1.0 × 2.1 m opening at x 5.7–6.0, y 8–9. The model has no door geometry, so it is an unclaimed notch.
- **E2 roof cut-out over the terrace.** The main roof is cut out over the terrace recess (x 6–10, y 0–2.5). The UF walls around the recess stop at z 5.75, leaving roughly 8 m² of gaps up to the roof.

Harness-only dry run, starting from E1 + E2 closed by stand-ins (a door solid and three infill blocks; not product code):

| Step (each adds to the previous) | Spaces formed |
|---|---|
| S1: envelope closed | 1 (547.6 m³: all 10 labelled rooms) |
| S2: + **R1** Air in the 7 unclaimed doorway notches (internal walls, notch at the wall base) | GF Bathroom, GF Kitchen/Dining, UF Master separate |
| S3: + **R2** Air in the slab notch over the GF footprint (stairwell void, 18.2 m²) | GF {Hall, Study, Living} and UF {Bathroom, Bed 3, Bed 2, Landing} separate |
| S4: + Air in the 1.2 m **gap between two separate GF wall solids** on y = 7 (x 3.0–4.2, beside the stair) | GF Living separates |
| S5: + stand-in attic floor at the UF wall plate (z 5.75–5.9) | all 5 UF rooms separate, plus `Attic_Unconditioned` 64.0 m³ |

S1 → S2 → S3 → S4 → S5 gives 1 → 4 → 5 → 6 → 10 named spaces (S5: 9 rooms + the attic), plus 8 unnamed sliver cells in S5.

- In S5, GF Hall + Study stay one space. That residual leak was not localised in the harness.
- Small sliver cells (≤ 8.6 m³) come from the crude stand-ins.

Classification:

- **Missing analytical boundaries: source-model omissions, not conversion defects.**
  - E1 front door and E2 terrace infill (envelope).
  - The attic floor or ceiling at the UF wall plate (no solid exists; UF partitions stop at 5.75 m under the roof).
  - The intended unconditioned attic needs this boundary in the model.
  - No geometric rule should invent these.
- **Missing Air panels: a rule gap, not dropped geometry.** The wall and slab material around every opening is fully represented (coverage test). Today `PanelsBySolid` turns only unclaimed *holes* into Air; unclaimed *notches* are kept as outline. Two candidate explicit rules:
  - **R1:** an unclaimed notch at the base of a wall is a doorway, and becomes Air when `airForOpenings_` is on. It would also make an empty *external* door opening (E1) Air, i.e. open to outside, so a door solid should be added there.
  - **R2:** a slab notch lying over the floor below (the footprint of the ground slab, or more generally of the solids below) is a void, and becomes Air. Plan concavities and the terrace recess are not over the footprint and stay outline (verified: 4 of 5 slab notches stay).
  - The y = 7 opening between two separate wall solids is not a notch of any solid. It needs either a modelled boundary or a cross-solid rule (R3), and is left to the owner.
- **Harness limitations.**
  - The stand-ins are approximate.
  - Air panels must be derived from the *snapped* outlines (pre-snap notches left 0.1 m slots).
  - The cluster uses the component's rebuild recipe with the label Text Dots as seeds.
  - Visual acceptance was not run.

None of R1, R2 or R3 is implemented in this PR. Each needs explicit owner approval (the rules above), so that rooms are not merged or fabricated implicitly.

## Files

- `SAM/SAM.Analytical/Create/PanelsBySolid.cs`
- `SAM/SAM.Analytical/Query/SnapToJunctions.cs`
- `Grasshopper/SAM.Analytical.Grasshopper/Component/SAMAnalyticalCreatePanelsBySolids.cs`
- Tests:
  - `SAM/SAM.Tests/PanelsBySolidMultiElementTests.cs` (new, 20 cases, including the thick-wall theories)
  - `SAM/SAM.Tests/PanelsBySolidComplexHouseTests.cs` (new, 17 cases)
- Fixture: `SAM/SAM.Tests/Golden/PanelsBySolid/contemporary1_solids.json` (new, 394 KB).
  - It holds the house's 19 solids and 18 glazing solids, converted exactly as the component converts them (`GH_Brep.ToSAM(true)` → `Shell`), with name, layer and Rhino id.
  - Copied to the output by the existing `Golden\**\*.json` item.
- This record.

## Validation (local, this machine)

```powershell
dotnet test SAM/SAM.Tests/SAM.Tests.csproj -c Debug
dotnet msbuild Grasshopper/SAM.Analytical.Grasshopper/SAM.Analytical.Grasshopper.csproj -t:Compile -p:Configuration=Debug -p:BuildProjectReferences=false
```

Build with `APPDATA` and `USERPROFILE` redirected (and `NUGET_PACKAGES` set to the real cache). Otherwise the project's post-build step deploys to `%APPDATA%\SAM`.

- **SAM.Tests (full suite, final code):** 3528 passed, 0 failed.
- **Focused:** `PanelsBySolid*` and `AperturesBySolids*` 71 / 71. The existing tests are unchanged and pass.
- **Mutation checks:**
  - With the previous `PanelsBySolid.cs`, 14 of the new tests fail.
  - With the previous `SnapToJunctions.cs`, the new slab-void snap test fails.
- **GH project:** compiles, 0 errors.
- **Hosted CI** on the first head `468891df`: build, test and spdx green. The final head and checks are in the PR description.

### Complex house coverage

Fixture-based, Rhino-free, in CI as `PanelsBySolidComplexHouseTests`.

The implementation-independent oracle takes every pair of opposite faces 0.08–0.6 m apart with at least 1 m² of overlap and material between them. Every such element side of every solid except the stair is ≥ 95% covered by a parallel panel of the same solid (test passes; zero exceptions).

Other assertions:

- No two panels of one solid overlap within the element thickness.
- Panel types: walls `Wall`; roofs `Roof` with an upward normal; slabs `Floor` with a downward normal.
- `PhysicalThickness`: external walls and gables 0.3; internal walls 0.15; roofs and intermediate slab 0.25; ground slab 0.2.
- Snap keeps every panel valid and every Guid.

Side-face coverage per solid, measured by the harness metric. Faces paired 0.05–0.6 m with ≥ 0.01 m² of overlap count, which also includes door nibs and window heads:

| Solid | before | after | note |
|---|---:|---:|---|
| GF_Wall_External (ring, 66 faces) | 16.6% (1 panel) | 93.3% (10 panels) | remainder: the ring's 13.9 m² top face, counted whole because it pairs with the 0.6 m² head over the terrace door, plus window heads; all cross-cuts that lie in the wall panels |
| UF_Wall_External (ring, 60 faces) | 18.0% (1) | 99.9% (10) | |
| RF_Roof_Main_Ridge_X (2 pitches) | 58.7% (1) | 99.1% (2) | |
| RF_Roof_Cross_Ridge_Y (2 pitches) | 58.3% (1) | 98.8% (2) | |
| UF_Gable_Cross_South | 0% (rejected) | 100% (1) at 0.25 | 0% at 0.20, with a Warning naming ratio 0.214 |
| GF_Wall_Internal ba24690f (L) | 69.2% (1) | 99.7% (2) | |
| GF/UF_Wall_Internal with door nibs | 72.9–100% | 72.9–100% | nib ends and door heads only (end faces, not element sides) |
| other gables, slabs | 99.4–100% | 99.4–100% | |
| ST_Stair_GF_to_FF | 0% | 0% | intentional (see above) |
| **Total** | **55.5%** | **97.2%** | |

Panels: 17 before → 39 after, 0 Air. 33 `Wall`, 4 `Roof`, 2 `Floor`.

### Real component on the real model

Machine-local Rhino 9 WIP harness:

- `SAMAnalytical.CreatePanelsBySolids` v1.0.1 was run in a headless `GH_Document`.
- Inputs as in `import-test.gh`: 19 referenced solids and 18 glazing solids, defaults otherwise.
- Result: 39 panels.
- One Warning: "1 solid(s) skipped as not panel-like … #0 ST_Stair_GF_to_FF".
- All panels carry source name, id and layer.
- `PhysicalThickness` values: 0.3 ×24, 0.25 ×5, 0.15 ×9, 0.2 ×1.

### Regression: the accepted 2026-10-14 simple house

`morning-test/Untitled2_meters.3dm` was run through PanelsBySolid (42 window and door solids), snap 0.3, then SAM_OCCT `Create.AdjacencyCluster` with the component's rebuild recipe.

- Result: 23 panels + 1 Air (3.325 m²); 16 stair and balustrade solids skipped.
- Cluster: **11 spaces, 56 panels, 1 Air**.
- This is identical to the accepted baseline in `PanelsBySolid-Q4.md`.

## Unresolved / risks

- **Owner decisions:**
  - the ratio default (0.20 recommended for this PR; 0.25 provisional in the code);
  - the room-topology rules R1, R2 and R3;
  - the model fixes E1, E2 and the attic floor.
- `SnapToJunctions` can only translate edges. A wall under a sloped roof cannot grow a gable-shaped top (unchanged behaviour).
- **Separate follow-up, not part of this PR:** the `Vector3D` → `Point3D` implicit operator in `SAM.Geometry` `Geometry/Spatial/Classes/Vector3D.cs` returns `new Vector3D(...)`, which converts to itself recursively. Any implicit vector-to-point conversion overflows the stack. Found by the harness; no PR #198 code path uses it.
- Harness-only observation: Rhino 9 WIP `RhinoDoc.OpenHeadless` returns an empty document for `Untitled2_meters.3dm`; `File3dm.Read` reads it.
- Manual Rhino/Grasshopper UI acceptance: **not run** by the agent.

## Machine-local evidence (not committed)

- The harness is a Rhino.Testing NUnit project.
  - Configured for `C:\Program Files\Rhino 9 WIP\System`.
  - Tests: census, coverage, ratio sweep, regression ratio diff, wall matrix, fixture export, real component, simple-house chain, OCCT cluster, naked edges, room-topology dry run.
- It lives in the session scratchpad. Copy it before switching machines if needed again.
- The committed fixture and tests reproduce the conversion result without Rhino.

## Next

1. **Owner:** decide the ratio default.
   - If 0.20: revert the three defaults, and pass 0.25 explicitly in `PanelsBySolidComplexHouseTests`.
2. **Owner:** run the Grasshopper acceptance (steps in the PR description).
3. **Owner:** approve or reject rules R1, R2 and R3, and fix the model (E1 door solid, E2 infill walls, attic floor solid). Then a separate PR for the approved rules.
4. Separate small PR: the `Vector3D` → `Point3D` recursion.
5. Separate PR (robust ratio): explicit exclusion of non-building objects, then ratio default 1.0.
