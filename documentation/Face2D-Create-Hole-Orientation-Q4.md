# Face2D.Create hole orientation after the hole union — Q4 (2026-11-19)

Status: draft PR into `sow/2026-Q4` (base `aa1cb777`), branch `fix/face2d-create-hole-orientation-q4`, left for owner
review. **Tested content `413b145c`** (code `fab42105`, tests added in `413b145c`); later commits change this record
only, and the PR description holds the final head and hosted checks. Not merged; no merge is authorised. SAM_OCCT#90
stays closed.

**Revalidated 2026-11-23 on current Q4** (after SAM#232): Q4 `a50e5c4a` merged into the branch by an ordinary merge,
**tested head `23bb6e06`** (parents `f0068098`, `a50e5c4a`; no code change beyond the merge). See "Revalidation after
SAM#232" below. The older numbers in "Validation" (base `aa1cb777`) are historical.

## Problem

`Face2D.Create(external, holes, method)` sets each hole to the orientation `method` asks for (Opposite: opposite of the
outer loop; Similar: like it). When more than one hole remains it then replaces them with the `Polygon2D.Union` output,
which is always clockwise in the plane, and never re-applies the orientation. With an outer loop that is clockwise in the
plane (a face whose plane normal is -z) the unioned holes end up wound like the outer loop. The native `make_face`
(`CellComplexBuilder.cpp:78-109`) rejects such a face and drops it silently (the build still returns status 0; only
`history input count < faces` shows it). One hole never reaches the union, so the defect needs a clockwise outer loop and
two or more holes. It was found with three gbXML stair openings cut into the one 3.475 m cap of the Softthouse model:
the cap was dropped and the build collapsed from 59 / 57 to 20 / 21 spaces.

## Change (`SAM/SAM.Geometry/Geometry/Planar/Classes/Face2D.cs`)

After the hole union, when `edgeOrientationMethod != Undefined`, the orientation computed for single holes is defined
and the external edge is a `Polygon2D`, `SetOrientation(orientation)` is applied to every unioned polygon. Unchanged:
`Undefined` (union output kept), the polygon eligibility checks (inside test, internal point, area equal to the outer),
the single-hole path, the `Face3D`->`Face2D` implicit conversion (uses `Undefined`) and the shared union operation
`Polygon2D.Union`. No public signature, JSON, tolerance or GH metadata change.

## Reload of saved panels (checked, not assumed)

Saved panels do **not** bypass `Face2D.Create`. `Panel` JSON stores `PlanarBoundary3D` loops; `Panel.GetFace3D()` ->
`PlanarBoundary3D.GetFace3D()` -> `GetFace2D()` calls `Geometry.Planar.Create.Face2D(external, holes)` (default
Opposite, `SAM.Analytical/Classes/PlanarBoundary3D.cs:249-255`) on every call, so a reloaded panel is rebuilt through the
fixed code. Evidence: the same saved `B.json` bytes flatten to inner loops +5.9072 / +5.7141 / +5.7423 (like the outer
+426.0657, native status 20) on the base build and -5.9072 / -5.7141 / -5.7423 (native status 40, accepted) on the fixed
build. Because of this the saved cases were replayed as they are, and the cases were also regenerated from face creation
(`variants`: `Face3D.Create` per cap), which gives the same results (below). Saved-vs-regenerated: base-regenerated is
JSON-identical to the saved files apart from Guids; fixed-regenerated differs only in the A and B cap hole winding.

## Tests (`SAM/SAM.Tests/Face2DHoleOrientationTests.cs`, new; xUnit, AAA, SPDX)

10 x 10 outer, two 2 x 2 holes, both outer windings: Opposite and Similar give the requested hole orientation, two holes
retained, net area 92; the default is Opposite even when the holes are given wound like the outer; `Undefined` keeps the
union output; overlapping holes that union into one (area 7, net 93) for both windings and both methods; the single-hole
path; and a saved panel (JSON round trip on a -z plane) whose `GetFace3D()` winds both holes opposite to the outer. The
existing `Face2DHoleLoopTests` (degenerate / sliver holes, two small holes kept) are untouched and pass.

Fail-first (source of `aa1cb777`, new tests only): 6 of 30 hole tests fail, exactly the cases whose requested winding is
not clockwise (Opposite with a clockwise outer, Similar with a counter-clockwise outer, the default with a clockwise
outer, overlapping holes in those two combinations, the saved-panel reload with a clockwise outer); with the fix 30 / 30.

## Validation (local, machine `michal`, 2026-11-19)

| Check | Result |
| --- | --- |
| `SAM.Tests` full, Debug, at `fab42105` content | 4034 / 4034 |
| `SAM.Tests` full, Debug, at `413b145c` | 4036 / 4036 (2 m 45 s) |
| Fail-first on base source | 6 of 30 fail (above); fix 30 / 30 |

### Softthouse replays (stock OCCT adapter, pinned native, one process at a time)

Setup: SAM `aa1cb777` Release build (`sam_base`, `SAM.Geometry.dll` `6DB895E1`) and the same build with only
`SAM.Geometry.dll` / `.pdb` replaced by the fixed build (`sam_fix`, `8BB3E05D`); each placed first in `SAMDIRS`, because the
adapter build directory carries its own copy of `SAM.Geometry.dll` and would otherwise win. Stock adapter = merged Q4
`4425901` (`69cc255` build `_kink_build/SAM_OCCT_bvfinal4`; `SAM.Geometry.OCCT.Solver` `CC43D447`,
`SAM.Analytical.OCCT.Solver` `0A9D2857`), pinned native `438089F8...1929` (ABI 4). Saved inputs: Q4 `panels_extended.json`
(F `d56f3036`, R `2941c480`) and the saved openings-only (A) and openings-plus-Air (B) panel lists; C0 = Q rebuilt with no
opening (neutrality control). Provenance: `assemblies*.txt` of all 72 stage runs list the expected `SAM.Geometry.dll` from
the intended directory, 0 `FALLBACK` lines (nothing loaded from `%APPDATA%\SAM`), native `438089F8`; `%APPDATA%\SAM` is
unchanged (2125 files, newest 2026-11-16).

| Case (F = forward, R = reversed faces) | Base spaces | Base G1 / G2 / G3a | Fixed spaces (saved input) | Fixed (regenerated input) | Fixed G1 / G2 / G3a |
| --- | --- | --- | --- | --- | --- |
| Q4 F | 59 | 47/47, 48/48, 18/18 | 59 | 59 | 47/47, 48/48, 18/18 |
| A F (openings) | 20 | 17/47, 17/48, 0/18 | 56 | 56 | 47/47, 48/48, 18/18 |
| B F (openings + Air) | 20 | 17/47, 17/48, 0/18 | **59** | **59** | 47/47, 48/48, 18/18 |
| Q4 R | 57 | 45/47, 46/48, 16/18 | 57 | 57 | 45/47, 46/48, 16/18 |
| A R | 21 | 17/47, 17/48, 0/18 | 54 | 54 | 45/47, 46/48, **13/18** |
| B R | 21 | 17/47, 17/48, 0/18 | **57** | **57** | 45/47, 46/48, 16/18 |

- Base reproduces the recorded failure on the unmodified `aa1cb777` SAM (20 / 21 spaces; Q4 59 / 57 as recorded), so the
  corrected build is the only difference. Fixed Q4 and C0 equal base Q4 (spaces, volumes, gates): the fix changes nothing
  without two or more holes.
- **B, both orders, saved and regenerated:** the set of rooms equals Q4 of the same order (59 / 57 cells, same total
  volume 2803.620 / 2724.231 m3, same closed / volume-valid / z signature), the same gates, and the Air panels are the
  shared boundary. A (no Air) merges the stair floors (56 / 54 spaces; reverse also loses the house-D ground room, G3a
  13/18): Air is what keeps the two floors as two rooms. The existing gates do not see a stair merge; the per-stair report does.
- Fixed results are identical between saved and regenerated inputs in every run (spaces, volumes, gates).

Per stair, B forward (rooms closed, 0 naked edges; Air has no construction and keeps its full area):

| Stair | Above / below room (m3) | Air area / owners | Solid floor kept |
| --- | --- | --- | --- |
| 738 house D | 79.389 / 216.913 | 5.7141 of 5.7141, owners Cell 28 + Cell 29 | FloorInternal 16.4296 + 1.4876 |
| 779 house B | 152.762 / 207.519 | 5.7423 of 5.7423, Cell 3 + Cell 4 | FloorInternal 27.0033 |
| 780 house A | 132.711 / 207.295 | 5.9072 of 5.9072, Cell 1 + Cell 2 | FloorInternal 25.4202 |

B reverse: 779 (152.762 / 207.519, Air 5.7423, Cell 2 + Cell 3) and 780 (132.711 / 207.295, Air in two panels 5.7739 +
0.1333 = 5.9072, Cell 1 + Cell 4) as Q4 reverse. **House-D reverse exception, unchanged:** Q4 reverse already has no room
above stair 738 (known #370 gap); B keeps the 5.7141 Air panel on the ground room only (217.02 m3, as Q4 reverse), so the
shared boundary is not evaluable on either side before or after the fix.

Evidence (machine-local, copy before switching machines):
`C:\Users\michal\Documents\SAM_daily\2026-10-25-Softthouse\handover\evidence\2026-11-19-face2d-orientation-fix\` (`runs/`
per case: `chain.log`, `assemblies*.txt`, `gates.txt`, `good.tsv`, `v2_spaces.tsv`, `v2_rooms.tsv`, `stairs.txt`;
`reload/` flatten of the saved cap on both builds; `scripts/` `env.sh`, `run3.sh`, `runset.sh`, `stairs.sh`, `all.sh`;
`probe/`). Reproduce: build `sam_base` / `sam_fix` as above, `sh scripts/runset.sh <base|fix> <saved|regen-fix> <out>`,
`sh scripts/stairs.sh <base|fix> <runs root>`.

## Revalidation after SAM#232 (local, machine `michal`, 2026-11-23, tested head `23bb6e06`)

**Tests** (Release, isolated profile via `safebuild.sh`, nothing deployed): focused `Face2DHoleOrientationTests` +
`Face2DHoleLoopTests` + `PanelsBySolidsFaceOrderTests` **33 / 33**; full `SAM.Tests` **4039 / 4039** (4023 of #232 + 16
of this PR; 3 m 29 s). Builds: `SAM.sln` Release of the `a50e5c4a` export (Q4 alone) and of the worktree at `23bb6e06`.

**Replays** (full SAM Release build first in `SAMDIRS`, stock adapter `SAM.Geometry.OCCT.Solver` `CC43D447` /
`SAM.Analytical.OCCT.Solver` `0A9D2857`, pinned native `438089F8`; all 12 runs: 0 `FALLBACK`, `SAM.Geometry.dll` from the
intended build dir, `%APPDATA%\SAM` untouched). Fresh inputs: the no-opening control (Q) is rebuilt from the source solids
(`solids_panels_392.3dm`) in stored (F) and reversed (R) order; A (three stair openings cut into the 3.475 m cap) and B
(A + one Air panel per stair) are generated by each build from its own extracted panels of that order (gbXML Air
rectangles 738 / 779 / 780).

| Run | Q4 alone spaces (G1/G2/G3a) | Q4 + #231 spaces (G1/G2/G3a) |
| --- | --- | --- |
| Q-F control | 59 (47/47, 48/48, 18/18) | 59 (same) |
| A-F openings | 20 (17/47, 17/48, 0/18) | 56 (47/47, 48/48, 18/18) |
| B-F openings + Air | 20 (17/47, 17/48, 0/18) | **59** (47/47, 48/48, 18/18) |
| Q-R control | 58 (45/47, 46/48, 18/18) | 58 (same) |
| A-R | 21 (17/47, 17/48, 0/18) | 55 (45/47, 46/48, 18/18) |
| B-R | 21 (17/47, 17/48, 0/18) | **58** (45/47, 46/48, 18/18) |

- Controls: 59 forward / 58 reversed on both builds (the #232 reference); the closed 79.389 m3 Kitchen 1 z3.5 +
  Circulation 9 D206 cell is present in both orders and both builds. The Q4-alone and Q4 + #231 controls have identical
  room partitions, closure and volumes (`compare.txt`): #231 changes nothing without two or more holes.
- **B vs its own control, both orders, Q4 + #231:** identical partition (rooms per cell, closed, naked 0, volume) - nothing
  gained, lost, merged or split. Q4 alone with the same openings collapses to 20 / 21 spaces (native drops the cap).
- A (no Air) merges the three stair floors into one space on both orders (296.302 / 360.281 / 340.006 m3 forward):
  Air is what keeps two rooms. Unlike 2026-11-19 the reversed run no longer loses the house-D ground room (G3a 18/18
  instead of 13/18), because #232 now recovers the upper room.
- **Stairs, B, Q4 + #231** (`runs/fix-B-{F,R}/stairs.txt`): all three Air areas are complete (5.7141 / 5.7423 / 5.9072 m2;
  780 reversed is two panels 5.7739 + 0.1333) and each is shared by exactly the two expected rooms, both closed with 0
  naked edges. Forward 738: Cell 28 above 79.389 / Cell 29 below 216.913; 779: 152.762 / 207.519; 780: 132.711 / 207.295.
  **Reversed 738** (the case that had no upper room in the 2026-11-19 run): Cell 32 above (79.389 m3, closed) and Cell 30
  below (217.02 m3, closed) share the 5.7141 m2 Air panel `8c415e9a` (its two owners are Cell 30 + Cell 32), together with
  FloorInternal 16.4606 + 1.4876 m2. Reversed 779: 152.762 / 207.519, 780: 132.711 / 207.295.
- No first differing room, face or boundary: every comparison above passed.

Commands (Git Bash, evidence root below): `sh scripts/build.sh`; `sh scripts/tests.sh`; `sh scripts/run.sh q4`;
`sh scripts/run.sh fix`; `sh scripts/compare.sh`. Needs `_kink_build/{SAM_OCCT_bvfinal4/build,native-frozen,profile}`,
`tools-2026-10-27-combined` (pipe, safebuild), `handover/inputs`, and the 2026-11-19 `probe/bin` and `evidence/gbxml-air.tsv`.
Machine-local evidence (copy before switching machines): `C:\Users\michal\Documents\SAM_daily\2026-10-25-Softthouse\handover\evidence\2026-11-23-face2d-orientation-revalidation\`
(`runs/<q4|fix>-<Q|A|B>-<F|R>/` with `chain.log`, `assemblies*.txt`, `gates.txt`, `good.tsv`, `v2_spaces.tsv`, `stairs.txt`,
`partition.tsv`; `variants/`; `suites/` TRX and logs; `provenance/hashes.txt`; `compare.txt`) and the earlier
`2026-11-23-solid-face-order\` (#232).

## Decisions and assumptions

- The orientation is re-applied only when the caller asked for one and the external edge is a `Polygon2D`, matching the
  existing single-hole rule; other edge types keep the union output as before.
- The change is in `Face2D.Create` (the owner of the "opposite of outer" contract), not in the union and not in the native
  or SAM_OCCT flatten step.
- Grasshopper versions: no component code, parameter or GUID change; results that change were an invalid face (silently
  dropped natively). Whether a version bump is warranted under GH-H2 is the reviewing owner's decision. Components whose
  output can change indirectly are those that build faces with two or more holes through `Face3D.Create` /
  `Panel.GetFace3D()` (e.g. `CreatePanelsBySolids`, `CreateAperturesBySolids`, SAM_OCCT `Clean3D` / `Extend3D`).
- Deployed Grasshopper needs a coherent `SAM.Geometry.dll` redeploy (`BuildAlls_v5.bat`) to see the fix.

## Unresolved / separate

- The production source of stair Air boundaries (here the gbXML Air rectangles, projected to the cap plane) and the
  treatment of the under-wall gaps (strips beside the wall stay filled) are separate engineering decisions; this PR does
  not touch either. SAM_OCCT#90 stays closed.
- Native `make_face` still drops a face with same-wound holes silently (no status or diagnostic). A native change (counting
  or reporting skipped faces) is owner-gated and separate.
- The house-D reverse gap (#370) and the existing gates not detecting a stair merge are unchanged.
- Fixed A reverse loses the house-D ground room (G3a 13/18): a property of openings without Air, not of this fix.

## Next step

Owner review of this draft PR (code, tests and the evidence above); then hosted CI at the final head. Merge only on owner
approval, followed by the `PROJECT_PROGRESS.md` closeout on `sow/2026-Q4` with the merge SHA. Recommended: Claude Opus 5.5,
medium effort, this session, Build mode.
