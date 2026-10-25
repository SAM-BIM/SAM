# PanelsBySolids: fragments of elements kept as parts of their element, and collapsed snaps dropped — Q4 (2026-10-25)

Status: implemented locally on `fix/panels-by-solids-continuation-q4` (base `sow/2026-Q4` at `457defbb`). PR SAM-BIM/SAM#221 into `sow/2026-Q4` (opened 2026-10-25 on owner go-ahead). Tested content `7057b383` (after the review correction below; previously `5c169cef`); later commits change this record only, and the PR description holds the final head and hosted checks. Step B of the Softhouse "SAM model from solids" task.

Owner decisions (2026-10-25): keep the `maxThicknessRatio_` default 0.2; recover fragments in a cross-solid pass. The
owner then authorised a more generic approach than "coplanar and touching" ("extend this solution to a more generic
approach to enhance SAM capability").

## Problem

Models rebuilt from other tools (here Softhouse, rebuilt from a Tas T3D) split walls into piers between windows and
pieces between junctions, slabs into patches, and leave short stubs. Each piece is judged alone by
`maxThicknessRatio`, so real elements are skipped: on Softhouse 79 solids "not panel-like" (0.45 m piers 2.1 m long,
0.35 m walls 1.34 m long, 0.15 m slab patches 0.66 m wide). Separately, `SnapToJunctions` could trim a narrow panel to
nothing and still return it as a zero-area face (about 1e-14 m wide). One such face made `Face3D` Union return null, and
`SAMOCCT.Clean3D` then merged nothing.

## Change

1. **`Create.PanelsBySolids`** (new, `SAM/SAM.Analytical/Create/PanelsBySolids.cs`): `PanelsBySolid` on every solid, then
   a pass over every skipped solid that has element planes rejected as not panel-like (whatever reason its warning
   gives: a wall around a large opening is reported by its 13 mm jambs). Those planes ("pieces") are judged as parts of their
   element. A piece is kept when either:
   - **its element is panel-like.** Pieces of different solids (skipped ones and created Panels) join when parallel, on
     the same mid-plane and of the same thickness (within `tolerance_Continuation`, default 0.01 m), and joined:
     touching, across a junction (a non-parallel Panel at least as thick as the gap runs through it), or across an
     opening (an aperture of `apertureGeometries` touches both). The thickness over the smallest extent of all joined
     pieces together must be within `maxThicknessRatio`.
   - **it closes a gap.** A vertical or horizontal piece whose every outline edge meets a crossing Panel (within half
     its thickness of its mid-plane) or an opening. Sloped pieces (stair flights) never qualify.

   Never kept: a plane thinner than `minThickness`; a piece narrower than `minThickness` in its plane (a sliver); a piece
   thicker than its smallest extent (the top and bottom of a pier, which would join every pier of a storey); a piece
   lying within the thickness of a crossing Panel (a strip at slab level over a wall: it would enclose a sliver of
   space). The pass repeats until nothing more is kept, so kept pieces join and frame further ones. From each kept
   solid the most panel-like such plane gives the Panels.
2. **Diagnostics**: a kept solid's skip Warning is replaced by Summary Info `PBS103` (`PanelsContinuation`,
   Value = its own ratio, Limit = maxThicknessRatio), naming the other pieces or saying it closes a gap, followed by
   the kept plane's own records. Summary, not Standard: it keeps input the threshold alone would skip (standard 2.5).
3. **`Query.SnapToJunctions`**: a panel whose snapped outline collapses (area at most the tolerance, or inverted) is
   dropped with Warning `SNP002` (`SnapCollapsed`, Value = area before snapping [m2]), never returned as a zero-area face.
4. **`SAMAnalytical.CreatePanelsBySolids`** 1.0.3 -> **1.1.0** (minor: the geometry result changes). GUID, parameters and
   defaults unchanged. It calls `PanelsBySolids`; the totals line adds "Kept N solids as pieces of larger elements."
   only when N > 0; one grouped GH Warning for SNP002 drops; the description says when a piece is kept.
   `PanelsBySolid` (single solid) is unchanged; its structured overload now delegates to a private core that also
   returns the not-panel-like planes.

## Files changed

- `SAM/SAM.Analytical/Create/PanelsBySolids.cs` (new), `Create/PanelsBySolid.cs`, `Query/SnapToJunctions.cs`,
  `Classes/Diagnostics/BySolidsDiagnosticCode.cs` (PBS103, SNP002)
- `Grasshopper/SAM.Analytical.Grasshopper/Component/SAMAnalyticalCreatePanelsBySolids.cs`
- `SAM/SAM.Tests/PanelsBySolidsTests.cs` (new, 14 tests), `Grasshopper/SAM.Core.Grasshopper.Tests/BySolidsDiagnosticDetailTests.cs`
  (version pin 1.1.0, one behaviour test)
- this record

## Validation (local; validated content `5c169cef`, this record follows it)

```powershell
dotnet build SAM.sln -c Release -p:SolutionDir=<repo>\
dotnet test SAM/SAM.Tests/SAM.Tests.csproj -c Release --no-build
dotnet build Grasshopper/SAM.Core.Grasshopper.Tests/SAM.Core.Grasshopper.Tests.csproj -c Release -p:SolutionDir=<repo>\
dotnet test Grasshopper/SAM.Core.Grasshopper.Tests/SAM.Core.Grasshopper.Tests.csproj -c Release --no-build
```

- `SAM.Tests`: 3752 / 3752 on `5c169cef`. (On `57a3970c` one run failed
  `QueryWaitToUnlockTests.WaitToUnlock_FileUnlockedWhileWaiting_ReturnsTrue`, a file-lock timing test that passed 7 / 7
  three times alone; not related.)
- `PanelsBySolidsTests` 14 / 14. Mutations, each caught by its own test: no framing, no embedded-strip guard, no
  opening join, no junction join, no sliver/slenderness filter, no collapse drop, candidates limited to PBS003 solids.
- `SAM.Core.Grasshopper.Tests` (headless Rhino 8, not CI): 85 / 85 on `57a3970c` (no Grasshopper change since).
- Build worktrees with `APPDATA`/`USERPROFILE` redirected: the SAM Grasshopper projects' PostBuild deploys to
  `%APPDATA%\SAM`.

## Softhouse evidence (machine-local; `SAM_daily/2026-10-25-Softthouse`)

Inputs `handover/inputs/solids_panels_392.3dm`, `windows_201.3dm`; chain PanelsBySolids -> SnapToJunctions 0.3 ->
SAMOCCT Clean3D -> Extend3D (inputAlreadyClean, directionalCapGrow, bucketBetweenLevels 0.21) -> CreateAdjacencyCluster
(scratch tool `pipe`, deployed SAM_OCCT DLLs):

| | panels / skipped solids | Clean3D | spaces (GF / 1F / 2F) | hosted windows |
| --- | --- | --- | --- | --- |
| before, minThickness 0.08 | 270 / 122 | 270 -> 83 | 30 (0 / 12 / 18) | 22 |
| before, minThickness 0.04 | 324 / 81 | 324 -> 324, OCCT rejected | 0 | - |
| this branch, minThickness 0.04 | 379 / 26 | 370 -> 83 (9 collapsed dropped) | 62 (25 / 18 / 19) | 46 |
| + SAM_OCCT `fix/occt-chain-degenerate-robustness-q4` | 379 / 26 | 370 -> 83 | 70 (27 / 22 / 21), direct rebuild | 41 |

Reference gbXML: 70 rooms (28 / 22 / 20). With both branches 69 of 70 reference rooms lie inside a model space; 9 spaces
hold two reference rooms each (open-plan kitchen + circulation, double-height kitchen voids into the 1F: no element
separates them in the source). Carports close from source elements (walls with garage-door openings, piers kept by
this pass); nothing is invented.

## Review correction (R1, R2) — 2026-10-25

Independent review of head `776fe282` (`SAM-BIM/.review-four-pr-20261025/REVIEW.md`, machine-local) found two defects;
both are corrected in `7057b383` (tested content).

- **R1 — order-dependent recovery.** `KeepPiecesOfElements` looked element membership up by list position
  (`Find(pieces.IndexOf(candidate))`) after earlier solids of the same pass had removed and appended pieces, so a later
  solid could inherit another element: wall / joined pier / isolated pier gave 1,1,1 in that order and 1,0,1 in
  another, with the isolated pier's PBS103 citing the wrong pieces. Now each pass decides every solid against one
  immutable snapshot (union-find over the snapshot, membership keyed by `Piece`), and applies the kept solids only after
  the pass; their Panels join from the next pass.
- **R2 — m² compared with m.** `SnapToJunctions` dropped a snapped outline when `area <= tolerance_Distance`, deleting
  valid small faces (a lone 20 x 20 mm face at 1 mm tolerance got SNP002 although nothing moved). Collapsed now means a
  mean width (2 x area / perimeter, a length) within the distance tolerance, or an inverted outline. SAM's
  `ThinnessRatio` was considered and not used: it is dimensionless and would reject long, legitimate strips. The SNP002
  text now states the actual condition and prints the area with significant digits; SNP001 counts the input panels
  (it counted the output, so a dropped panel was not counted as checked).

Tests added: `SAM/SAM.Tests/PanelsBySolidsOrderTests.cs` (wall / joined / isolated in all 6 orders with exact PBS103
evidence; two disconnected elements + an isolated pier in all 120 orders; two columns with two candidate planes each in
all 120 orders, plane and evidence fixed; a stub framed only by a recovered pier, 12 orders, kept on a later pass) and in
`PanelsBySolidsTests.cs` `SnapToJunctions_LoneSmallFace_IsKeptWithoutWarning` (20 mm @ 1 mm, 0.5 mm @ 1e-6 m); the
collapse test now runs at 1e-6 and 1e-3 m.

Evidence (machine-local, `SAM-BIM/.evidence-r1r2-20261025/`):
- Review probes before / after: `probe-before.txt` (1,1,1 / 1,0,1 / 0,1,1; 20 mm face dropped with SNP002),
  `probe-after.txt` (1,1,0 / 1,0,1 / 0,1,1; face kept, "Checked 1 panel").
- New tests on the reviewed production code: 5 failed / 8 passed (`sam221-fail-before.trx`: both small-face rows and
  three order tests; the stub and collapse tests pass as guards). After: 13 / 13 (`sam221-pass-after.trx`).
- Full, on `7057b383`: `SAM.Tests` 3759 / 3759 (`sam221-full.trx`; 3752 + 7 new rows); `SAM.Core.Grasshopper.Tests`
  85 / 85 (`sam221-gh.trx`, headless Rhino 8, not CI).

```powershell
dotnet build SAM/SAM.Tests/SAM.Tests.csproj -c Release -m:1        # APPDATA/USERPROFILE redirected
dotnet test SAM/SAM.Tests/SAM.Tests.csproj -c Release --no-build
dotnet run --project <SAM-BIM>/.review-four-pr-20261025/probe/probe.csproj -c Release --no-restore
```

## Risks and limits

- Known miss: Softhouse #385, a 0.45 m wall that is mostly a 1.8 x 2.87 m opening (13 mm jambs). Its 0.45 m face pair
  is dropped inside `ElementPlanes` before it can be rejected: the ownership step samples one point of the frame-shaped
  overlap, the point falls in a jamb, and the thin jamb plane claims it. Dining 1 (house D) stays open because of it.
  A fix belongs in the ownership sampling of `PanelsBySolid` (all models), so it was left for a separate change.

- The rules are geometric heuristics: a coplanar same-thickness non-building piece joined to a wall would be kept. No
  such case seen; the stair, free-edge stub, sliver and separated-pier tests stay skipped.
- PBS103 is the one 1xx code at Summary (documented on the constant and the class).

## Next step

Owner review of the PR and its hosted checks; merge only on owner approval, then the PROJECT_PROGRESS.md closeout on `sow/2026-Q4`. Independent of the Rhino UnitScale and Face3D Union branches.
