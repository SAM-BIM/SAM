# PanelsBySolids: junction blocks shorter than the wall thickness close their junction — Q4 (2026-10-26)

Status: draft PR SAM-BIM/SAM#225, branch `fix/panels-by-solids-junctions-q4`, opened 2026-10-27 on owner approval; not
for merge yet. **Rebased 2026-10-27 (owner-authorised) onto `sow/2026-Q4` `1f46f7ab`** (the merge of SAM-BIM/SAM#226,
SnapToJunctions dropped targets). Tested content is now `7763fd03`, which is `cbf3f520` rebased with `CreatePanelsBySolids`
moved to **1.4.0**, because #226 took 1.3.0; later commits change this record only. The original head was `cb11d6c4`, on
`fe5bbce4`. Part of the Softhouse room recovery after SAM_OCCT draft PR SAM-BIM/SAM_OCCT#84 (Extend3D no longer invents
walls), which exposed extraction defects the invented walls had hidden.

## Problem

On Softhouse (`solids_panels_392.3dm`, PanelsBySolids minThickness 0.04, continuation 0.01) blocks at junctions were
skipped or read along the wrong axis, leaving gaps between rooms (2026-10-26 stage attribution):

| Block | What it is | Base reading |
| --- | --- | --- |
| #326, #329 | 0.1 x 0.09 m block where the A/B (and B/C) party wall turns by 2-4 degrees | skipped (PBS003, read across: 0.092 / 0.1) |
| #384, #391 | slices (76 mm, 234 mm) of the 0.45 m return wall at the house D ground-floor facade jog | #384 a 76 mm wall across the return wall; #391 skipped |
| #389 | 0.45 m facade pier, window 75 mm from its end | a 74 mm wall across the facade (its jamb) |
| #367 | 0.45 x 0.548 m facade block at the end of the 0.548 m wall #250 (first floor) | skipped: taken as #250's edge |

Mechanisms, reproduced on the real input and in minimal synthetic cases (tests below):

1. **Junction block never a candidate.** `PanelsBySolids` dropped every plane thicker than its smallest extent
   (`Slenderness > 1`, the guard against the top and bottom of a pier). A block shorter than the wall thickness is
   exactly that on its true plane; its other plane lies within the wall and is discarded as its edge.
2. **Slice or jamb reading.** `ElementPlanes` takes the most panel-like plane first. For a 76 mm slice of a 0.45 m wall,
   or a 75 mm jamb beside a window, that is the thin plane across the wall; the wall plane is then not panel-like on its
   own (#389) or is dropped as its cross-cut (#384). A solid that gave Panels never reached the fragment pass.
3. **Embeds guard.** A candidate within the thickness of a crossing Panel is treated as that Panel's edge (correct for a
   strip at slab level over a wall), also when it continues its own element on both sides (#367 between facade pieces).

## Change

`SAM/SAM.Analytical/Create/PanelsBySolids.cs` (fragment pass), `PanelsBySolid.cs` (one extra private out list):

- **Junction route** (mechanism 1): a plane with `Slenderness > 1` is now a candidate flagged `Junction`. It never joins
  an element. Touching junction blocks on one mid-plane and of one thickness are judged together (the slices of a short
  wall). The block or group is kept only when it **bridges**: two opposite edges each meet a **different** wall of its
  thickness (within `tolerance_Continuation`) that lies wholly beyond the edge and across it — a coplanar Panel touching it,
  or a crossing Panel the edge abuts (`Abuts`, its face on the edge as at a jog) — **and** every other edge meets a crossing
  Panel or an opening (`Framed`). A junction candidate is only considered while a Panel of its thickness exists.
- **Replaced readings** (mechanism 2): a solid that gave Panels offers a rejected or cross-cut plane only when every one of
  its Panels crosses that plane, is thinner, and holds only its material (`ElementPlane.Owns` of the Panel's internal
  point). If the pass keeps that plane (any route), its Panels replace the first reading. `ElementPlanes` now returns the
  planes it dropped as cross-cuts (with their pairs); its own result is unchanged.
- **Embeds override** (mechanism 3): a candidate within a crossing Panel's thickness is still not kept, unless it bridges
  pieces of its own element joined to it (`Joined`) on two opposite sides. Pieces that lie within the thickness of the same
  crossing Panels (the rest of that edge) do not count.
- A solid's own Panels are never evidence for its own candidate (no effect for skipped solids, which have none).
- Diagnostics: every changed solid gets the existing Summary Info **PBS103** (`PanelsContinuation`). New reasons:
  "it is a junction block shorter than its thickness that bridges walls of its thickness, Panels of #a on one side and
  Panels of #b on the other, ..." (+ "Judged together with the touching blocks ..."); the Embeds override appends "It lies
  within the thickness of Panels of #c but continues its own element on two opposite sides, so it is not their end.";
  a replacement starts "Read on another of its planes (...)" and ends "It replaces N Panels of the t m plane first taken
  ...". Existing messages are unchanged. Detail never changes a result (test below). `BySolidsDiagnosticCode.PanelsContinuation`
  XML doc updated; no new code.
- No public API change. Reused: `Joined`, `Gap`, `Abuts`, `Embeds`, `Framed` (now with optional coplanar continuations),
  `ElementPlane.Owns`, `Geometry.Planar.Query.Union`; tolerances `tolerance_Continuation`, `tolerance_Angle`,
  `tolerance_Distance`. No fixture-specific branches or coordinates.
- `SAMAnalytical.CreatePanelsBySolids` result changes: **1.3.0 -> 1.4.0** (minor, GH_COMPONENT_STANDARDS GH-F2/GH-H2,
  as for SAM#223). The branch first took 1.2.0 -> 1.3.0; SAM#226 merged first with 1.3.0, so the rebase moved it to 1.4.0.
  The version pin in `BySolidsDiagnosticDetailTests` keeps the 1.3.0 (#226) comment and adds 1.4.0. GUID and
  parameters unchanged.

## Softhouse extraction, base `fe5bbce4` vs this branch (all 392 solids, managed probe)

Panels 376 -> 384, skipped solids 28 -> 19. Exactly 12 solids change their Panels; the other 380 have identical Panels and
codes (some PBS103 texts list one more piece of the facade element).

| Solid | Base | This branch | Mechanism |
| --- | --- | --- | --- |
| #326 | skipped | Wall 0.1 at x 4.916, y -2.527..-2.434 (bridges #292 / #293) | 1 (target) |
| #329 | skipped | Wall 0.1 at x 10.726, y -2.527..-2.436 (bridges #325 / #294) | 1 (target) |
| #384 | Wall 0.076 N-S at x -7.137 | Wall 0.45 E-W at y -0.458, x -7.175..-7.099 (with #391, bridges #385 / #389) | 1+2 (target) |
| #391 | skipped | Wall 0.45 E-W at y -0.458, x -7.099..-6.865 | 1 (target) |
| #389 | Wall 0.074 E-W (jamb) | Wall 0.45 N-S at x -6.64, y -0.233..1.355, window filled | 2 (target) |
| #367 | skipped | Wall 0.45 N-S at x -6.64, y 3.229..3.777 (1F facade hole closed) | 3 (target) |
| #350 | skipped | Wall 0.45 N-S at x -6.64, y 3.229..3.777 (2F, same as #367) | 3 (same pattern) |
| #361 | Floor 0.048 + Wall 0.076 N-S (head and jamb of a 5.35 m window) | Wall 0.45 E-W at y -2.887, x -6.415..-0.952, window filled (1F house D facade; matches #337/#353/#371) | 2 (same pattern, not in the attribution) |
| #370 | skipped | Wall 0.45 N-S at x -6.64, y -2.662..-2.434 (1F corner block; bridges #361 / #369; 2F facade #351 already covers it) | 1 (same pattern) |
| #362 | skipped | Wall 0.175 E-W at y -2.749 (window reveal block) | existing "closes a gap", now framed by #361/#370; same as base #338 (2F) and #374 |
| #386 | skipped | Wall 0.175 N-S at x -7.263, y -0.713..-0.233, z to 2.921 (reveal block by window #4) | existing "closes a gap", now framed by #384/#391 |
| #390 | skipped | Wall 0.175 N-S at x -6.778, y -0.683..-0.158, z 1.2..2.9 (reveal block by window #2) | existing "closes a gap", now framed by #384/#391 |

Not changed: #386's 0.48 m plane (thickness differs from the 0.45 m walls, so no bridge); #250, #385, #292/#293 etc.

Order: forward vs reversed **solid** order: 0 of 392 differ (base 0). Forward vs reversed solids with each solid's
**faces reversed**: base 4 differ (#341, #87, #21, #5: pre-existing per-solid face-order sensitivity of `PanelsBySolid`);
this branch 6 = the same 4 + #370 and #362, whose bottom edge is framed by 1F slab #87, which base already loses with
reversed faces. A first version of the Embeds override also kept slab-edge strip #3 under reversed faces (because base
reads its neighbour #5 as a wall then); fixed by not counting pieces within the same crossing Panels.

## Files changed

- `SAM/SAM.Analytical/Create/PanelsBySolids.cs`, `SAM/SAM.Analytical/Create/PanelsBySolid.cs`
- `SAM/SAM.Analytical/Classes/Diagnostics/BySolidsDiagnosticCode.cs` (PBS103 doc)
- `Grasshopper/SAM.Analytical.Grasshopper/Component/SAMAnalyticalCreatePanelsBySolids.cs` (1.4.0)
- `Grasshopper/SAM.Core.Grasshopper.Tests/BySolidsDiagnosticDetailTests.cs` (version pin)
- `SAM/SAM.Tests/PanelsBySolidsJunctionTests.cs` (new, 13 tests)
- this record

## Tests (`PanelsBySolidsJunctionTests`, minThickness 0.04)

Fail on base source (6), pass on this branch:
`BlockShorterThanThicknessInKinkedWall_ClosesTheKinkOnTheWallsMidPlane` (#326), `ReturnWallSlicedShorterThanThickness_GivesTheReturnWallOnItsMidPlane`
(#384/#391), `PierReadAcrossItsJambButPieceOfFacade_IsReadAsTheFacade` (#389), `FacadeReadOnJambAndHeadButPieceOfFacade_IsReadAsTheFacade`
(#361), `FacadeBlockWithinThickWallEndBetweenFacadePieces_IsKeptAsFacade` (#367), `JunctionCases_GiveTheSamePanelsAtEveryDetailLevel`.
Preservation, pass on both: block at a free wall end (stub); block between walls of another thickness; slices between
facades of another thickness; a single slice against a wall end; the pier alone; the window-frame facade alone; the facade
block with facade on one side only. Existing guards still pass: pier tops (`PiersWithoutWindowBetween_StaySkipped`), strip
within slab thickness, sliver, stair, stub with one free edge, column order tests.

## Validation after the rebase onto `1f46f7ab` (local, machine `michal`, 2026-10-27; tested content `7763fd03`)

- **Rebase:** `git range-diff fe5bbce4..cb11d6c4 1f46f7ab..HEAD`. The fix commit changes only in the version string,
  the pin and its comment. `PanelsBySolids.cs`, `PanelsBySolid.cs`, `BySolidsDiagnosticCode.cs` (auto-merged next to
  #226's SNP003) and the tests are otherwise unchanged. There was one conflict, in the pin, resolved by hand. The
  `SAM/` tree of `git archive 7763fd03` is byte-identical to the round-2 combined build of #226 (`cbf3f520` + `ba6365bd`,
  `cv28\SAM_comb2`).
- **SAM.Tests** (Release, `--no-build` after a `safebuild.sh` build): **3808 / 3808**, 0 skipped. That is the 3795 of
  `1f46f7ab` plus the 13 new tests.
- **SAM.Core.Grasshopper.Tests** (Release, `--no-build`, normal profile): **87 / 87**, 0 skipped, including the 1.4.0 pin.
  This closes the earlier gap: the previous run did not complete because of an idle Rhino under the redirected profile.
- **Combined Softhouse F** (unchanged `tools-2026-10-27-combined/scripts/softhouse.sh`, one process at a time): SAM
  `7763fd03` (`cv28\SAM_225r`, SAM.Analytical `4E6B107D`) + SAM_OCCT#84 `a77ef7d`.
  - **No-kink (acceptance, the PR contents):** 384 panels, 19 skipped solids, Clean3D 371 -> 80, 58 spaces, 100 hosted
    apertures. 48 good of 70: **Carpark 2 C109 and Bed 4 C203 good, 47 / 48 baseline-good retained**. Dining 4 A107 is
    lost, inherited from SAM_OCCT#84.
  - **Kink (comparison):** 57 spaces, 50 good, 47 / 48.
  - Both runs give `good.tsv`, `v2_rooms.tsv` and `v2_spaces.tsv` byte-identical to #226's `comb2-*-F` runs.
    `panels_extracted.json` and `panels_extended.json` are identical apart from Guids, so the Extend3D growth (no
    invented walls) is as recorded for #226.
  - All SAM.* assemblies loaded from `cv27`/`cv28` builds; none from `%APPDATA%` or McNeel.
- **The 48/48 gate is not met** in the combined chain while A107 (SAM_OCCT#84) stays lost. R was not rerun; it is
  0 spaces from the separate #37 Clean3D slab drop.
- `%APPDATA%\SAM`: all 2125 files byte-identical before and after.
- Evidence (machine-local): `SAM_daily/2026-10-25-Softthouse/handover/evidence/2026-10-27-pr225-rebase/` (README,
  `scripts/tests.sh`, `scripts/run-chains.sh`, `tests/`, `runs/`, `provenance/`).

## Validation of the original content `cbf3f520` (local, machine `michal`, 2026-10-26)

Builds with APPDATA/USERPROFILE redirected (`handover/tools-2026-10-26-r3/scripts/safebuild.sh`); nothing deployed to
`%APPDATA%\SAM`.

```sh
safebuild.sh test SAM/SAM.Tests/SAM.Tests.csproj -c Release
```

- SAM.Tests (Release, final source): **3798 passed, 0 failed, 0 skipped** (3 m 10 s); also 3798 / 3798 before the last
  Embeds refinement (3785 existing + 13 new).
- SAM.Core.Grasshopper.Tests: **not run to completion**. Built with redirected APPDATA (0 errors); the test host started
  Rhino under the redirected profile and sat idle (no licence/profile there), was stopped, and its orphaned `testhost`
  (PID 44688) could not be ended from the session, so a second in-process Rhino run was not attempted. The only GH change
  is the version string and its pin (1.3.0). To run: end that process, then
  `dotnet test Grasshopper/SAM.Core.Grasshopper.Tests/SAM.Core.Grasshopper.Tests.csproj -c Release --no-build`
  under the normal profile (`--no-build` does not deploy).
- `%APPDATA%\SAM` not modified (SAM.Analytical.dll there still 2026-10-26 06:54).
- Before/after of the new tests: base source (`git show HEAD:<file>` for the 3 source files) 6 failed / 7 passed;
  this branch 13 / 13.
- Softhouse extraction evidence (machine-local, copy to continue elsewhere):
  `SAM_daily/2026-10-25-Softthouse/handover/evidence/2026-10-26-sam-junctions/` (README, per-solid dumps, diffs, order
  checks, probe tool source).

## Open issues and risks

- Full Softhouse acceptance (48/48 in both face orders) is not met. In F, A107 is lost through SAM_OCCT#84. In R, the
  run gives 0 spaces through #37. Neither is owned by this PR.
- #361 (house D 1F facade y -2.887) and #370 are recovered by the same mechanisms but were not in the attribution; the
  base had no facade wall there.
- #362/#386/#390 become 0.175 m reveal walls through the existing "closes a gap" rule, as base already does for #338 and
  #374. #386 and #390 stop below the ceiling (z 2.921 / 2.9), so they cannot enclose a space, but they are new faces in the
  house D corner.
- Pre-existing face-order sensitivity of `PanelsBySolid` (#341, #87, #21, #5) is not addressed; it now also affects
  #370/#362 through #87.
- The version collision with SAM#226 is resolved (1.4.0). Any other open PR that bumps `CreatePanelsBySolids` must take
  the next minor.
- Reveal blocks #362/#386/#390 no longer move their neighbours, because SAM#226 removed the dropped-target pull that
  flipped the first-floor datum. The LevelFrame single-cap datum margin is still only 1.7 % (SAM_OCCT, out of scope).

## Next step

Check the hosted Build/SPDX/Test of #225 at its rebased head. Then the owner reviews #225 for merge (it is still a
draft) and decides on the house D additions (#361, #370, #386, #390). Merge is not authorised. Recommended: Claude
Opus 5.5, medium effort, the existing coordinating session, Build mode, because the evidence and context are there.
