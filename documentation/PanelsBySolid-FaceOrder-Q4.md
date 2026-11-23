# PanelsBySolid material test independent of face order — Q4 (2026-11-23)

Status: local feature branch `fix/panels-by-solid-face-order-q4` from `sow/2026-Q4` `aa1cb777` (merged base; SAM#231,
the separate hole-winding fix, is not included). **Tested content `170a2906`** (fix and tests `242ec07f`; `170a2906` reads
the solid's faces only once the diagonal ray is inconclusive). Draft PR for owner review; push and draft PR authorised by
the owner, merge not authorised. Later commits change this record only; the PR description holds the final head and
hosted checks.

## Problem

The Softhouse solids with their faces listed in reverse order (pipeline `REVFACES=1`) lose a closed house-D first-floor
cell (Kitchen 1 z3.5 and Circulation 9 D206, 79.389 m3 in stored order). SAM_OCCT `Extend3D-No-Invented-Walls-Q4.md`
traced it to `CreatePanelsBySolids` skipping the 0.45 x 0.228 m junction block at the west facade on the reversed order
only. Traced here to the first differing decision:

1. The block's junction test (`JunctionBridges`) passes `Bridges` in both orders (walls of its thickness on its two long
   edges) but fails `Framed` on the reversed order: its bottom edge is framed by the Floor of the first-floor slab
   strip below it, and that slab strip is skipped on the reversed order.
2. The slab strip (0.15 m, 12.33 m2 overlap of its top and bottom faces) is skipped because its top/bottom pair fails
   `HasMaterialBetween`: of the three sample points (identical in both orders) the one at 3/4 of the thickness is
   reported outside the solid on the reversed order only.
3. `Shell.Inside` decides that point by the first face-directed ray, in face order, that meets no edge (its
   bounding-box diagonal ray meets an edge). The slab strip is not watertight (two near-coincident side faces at a
   slot): of its 12 clean face rays 10 say inside and 2 say outside. Stored order starts with an inside ray, reversed
   order with an outside one.

So a point-in-solid answer, and from it a panel and a room, depended on the order in which the same faces are listed.

## Change

`SAM/SAM.Analytical/Create/PanelsBySolidContains.cs` (new, private `Create.Contains`): the bounding-box diagonal ray
decides when it meets no edge (as `Shell.Inside`); otherwise one ray towards each face and the parity of the rays that
meet no edge decide by majority (a tie or no such ray is not inside, as `Shell.Inside` returns when no ray is clean). It
stops as soon as the remaining rays cannot change the majority, so the answer is the same in any face order. On a
watertight solid every clean ray agrees, so the answer equals `Shell.Inside`. The faces (`Shell.Face3Ds` copies each one)
are read only when the diagonal ray does not decide; a solid without faces is not inside, as before.

Used by the three material checks of the extraction: the pair material test (`HasMaterialBetween`), the section-region
filter (`PanelsByElementPlane`) and the source-evidence layers (`TryCertifyMaterial`). `Shell.Inside` itself and every
other caller are unchanged. No public signature, tolerance, JSON, Diagnostics or Grasshopper metadata change.

## Tests (`SAM/SAM.Tests/PanelsBySolidsFaceOrderTests.cs`, new; xUnit, AAA, SPDX)

Synthetic geometry only (no fixture indices or coordinates): a 0.15 m slab with a stray face inside it 0.1 m from one
side, in every rotation of its face order and each rotation reversed.

- The slab alone gives one 0.15 m Floor of 30 m2, physically identical in every order.
- A 0.45 m facade ending against a 0.228 m block, a 0.45 m crossing wall on the block's other edge, the slab below and a
  ceiling: the block is kept as a junction on the facade's mid-plane (x 0.225, 0.684 m2, 0.45 m) in every order, and every
  solid's Panels (type, thickness, area, extent, normal line, holes, role) equal those with a watertight slab.
- Controls on the same slab, in every order, stay skipped as with a watertight slab: an isolated block, a block at a free
  wall end, a solid stepped stair flight and a 0.03 m reveal lining.

Fail-first (base call sites, new tests): 3 of 3 fail, first at "rotation 0 reversed" (the slab is dropped; the junction
block and the controls' slab with it). With the change 3 / 3 pass.

## Validation (local, machine `michal`, 2026-11-23)

| Check | Result |
| --- | --- |
| `SAM.Tests` full, Release, `aa1cb777` | 4020 / 4020 |
| `SAM.Tests` full, Release, `242ec07f` | 4023 / 4023; per test identical to base except the 3 new tests |
| `SAM.Tests` focused / full, Release, `170a2906` | 3 / 3; 4023 / 4023, per test identical to `242ec07f` |
| Fail-first | 3 / 3 fail on base call sites, 3 / 3 pass with the change |

### Softhouse chain (merged Q4, both face orders)

Setup: the saved harness `tools-2026-10-27-combined` (`softhouse.sh`: PanelsBySolids 0.04 / 0.2 / continuation 0.01,
SnapToJunctions 0.3, Clean3D, Extend3D, OCCT AdjacencyCluster, Rooms2, good classification), inputs
`solids_panels_392.3dm` `4c51b625`, `windows_201.3dm` `00b31a93`, gbXML `softhouse3.xml` `e77e7ca4`. SAM `aa1cb777`
Release (`SAM.Analytical` `e18f0e22`), `242ec07f` content (`40858b4c`; only `SAM.Analytical` and the three assemblies
referencing it differ) and `170a2906` (`b6265916`; every assembly differs from the earlier builds only because SourceLink
embeds the HEAD commit), each first in `SAMDIRS`, then the stock adapter (SAM_OCCT merged Q4 `4425901` content, built in
`_kink_build/SAM_OCCT_bvfinal4`: `SAM.Geometry.OCCT.Solver` `cc43d447`, `SAM.Analytical.OCCT.Solver` `0a9d2857`) and the
pinned native `438089f8` (TKBO `1fa732fe`). Isolated `APPDATA`; 0 `FALLBACK` lines in every loaded-assembly log;
`%APPDATA%\SAM` unchanged (2125 files, newest 2026-11-16). One native process at a time; no native rebuild, no deploy.

| | base F | fixed F | base R | fixed R |
| --- | --- | --- | --- | --- |
| panels / skipped solids | 384 / 19 | 384 / 19 | 380 / 23 | 383 / 20 |
| Clean3D / spaces | 371 -> 79 / 59 | 371 -> 79 / 59 | 367 -> 79 / 57 | 369 -> 80 / **58** |
| G1 / G2 / G3a | 47/47, 48/48, 18/18 | same | 45/47, 46/48, 16/18 | 45/47, 46/48, **18/18** |
| frozen 48 GOOD | 47 | 47 | 45 | 45 |
| Kitchen 1 z3.5 + Circulation 9 D206 | closed cell 79.389 m3 | same | no space | **closed cell 79.389 m3** |

- "fixed" is `242ec07f`; `170a2906` replayed F and R give byte-identical `good.tsv`, `v2_spaces.tsv`, `v2_rooms.tsv`,
  gates and D206-cell panels (`runs/fix2-*`, `runs/fix2-vs-fix.txt`): 59 / 58 spaces, the closed 79.389 m3 cell in both.
- Base F and base R reproduce the merged bend-veto controls (`2026-11-19-bend-veto-impl/r4/chains/bv-{F,R}`)
  byte-for-byte (`good.tsv`, `v2_spaces.tsv`, `v2_rooms.tsv`). Fixed F is byte-identical to base F.
- Fixed R recovers the cell: 15 faces, 0 naked segments, closed, volume valid, 79.389 m3, box (-6.64,-2.887,3.475)-
  (-0.902,4.889,6.475), first floor, both rooms in it as on F. Its bounding panels equal F's up to normal sign except one
  internal floor split line 0.03 m apart (22.14 + 2.78 m2 on F, 22.17 + 2.75 m2 on R); totals per type are equal
  (external walls 23.83, internal walls 57.26, floors 52.92 m2). The west facade notch is closed on R.
- Rooms, all 70 scored in each run: on R only Kitchen 1 z3.5 and Circulation 9 D206 change (no space -> the shared
  closed cell, as on F). Nothing lost, split or newly shared elsewhere; nothing changes on F. Ground floor houses A-D are
  unchanged in both orders (207.295 / 207.519 / 208.048 / 216.913 F, D 217.02 R).
- C109 / C203: unchanged. GOOD and separate on F (68.623 / 40.831 m3); on R both share one closed 109.453 m3 cell before
  and after (the only G1 / G2 failures).
- Stored vs reversed after the fix, all present before it: C109 / C203 as above; cell volumes of house D ground floor
  (216.913 / 217.02), Toilet 1 D102 (9.6 / 9.493) and Bed 3 / Circulation 10 C205-C206 (133.889 / 133.89). Same GOOD
  status and room sharing in both orders otherwise.
- The saved three-stair Air experiment was not re-run; stair owners are not measured here.

Evidence (machine-local, copy before switching machines):
`C:\Users\michal\Documents\SAM_daily\2026-10-25-Softthouse\handover\evidence\2026-11-23-solid-face-order\` (`README.md`;
`trace/` first-differing-decision logs and the instrumentation diff; `probe/` standalone PanelsBySolids probe; `runs/`
per run `chain.log`, `assemblies*.txt`, `gates.txt`, `good.tsv`, `v2_*.tsv`, `d206-cell.txt`, and `rooms-all.tsv` /
`rooms-diff.txt`; `suites/` and `r2/suites/` TRX and logs; `provenance/hashes.txt`; `scripts/`). Reproduce: `sh
scripts/replay.sh base`, `sh scripts/replay.sh fix` (or `fix2`), `sh scripts/rooms.sh`, `sh scripts/full-suites.sh` (needs `_kink_build` with
`SAM_OCCT_bvfinal4/build`, `SAM/build`, `native-frozen/`, `profile/`).

## Decisions and assumptions

- The order dependence is removed where the extraction asks the question, not in `Shell.Inside`: changing the shared
  geometry query would change every caller (spaces, adapters) and its cost, outside this task.
- Majority over clean rays is the order-free reading of the same evidence `Shell.Inside` uses; a tie stays "not inside".
- Grasshopper: no component, parameter or GUID change. `CreatePanelsBySolids` output can change only for solids that are
  not watertight, where it previously depended on face order. Whether that warrants a version bump (GH-H2) is the
  reviewing owner's decision.

## Unresolved / separate

- Remaining reversed-order extraction differences, a different mechanism: an element plane's smallest extent is the
  bounding box of its footprint in the in-plane axes of whichever face of the pair came first, so a skewed footprint
  gets another extent in another order (e.g. a 0.135 m slab strip read as 0.15 m over 0.925 m and kept in stored order,
  0.15 over 0.135 and skipped reversed; several skip messages differ by a few millimetres; one notch claimed differently).
  The smallest open decision: whether `ElementPlane.MinExtent` should be measured frame-independently (oriented extent),
  which would also change stored-order results. Not changed here.
- C109 / C203 share a cell on the reversed order (pre-existing, unchanged).

## Next step

Owner review of the draft PR (code, tests and the evidence above) and of the hosted checks at the final head. Merge only
on owner approval, followed by
the `PROJECT_PROGRESS.md` closeout on `sow/2026-Q4` with the merge SHA. Recommended: Claude Opus 5.5, medium effort, this
session, Build mode (the evidence and harness are already loaded).
