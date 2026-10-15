# PanelsBySolid: thin elements below minThickness, and the Diagnostics convention — Q4 (2026-10-15)

Status: implemented on `fix/panels-by-solid-thin-elements-q4`, draft PR into `sow/2026-Q4`. Not merged; owner Grasshopper visual acceptance pending.

- Base: `sow/2026-Q4` at `0649e15e` (after SAM#198 and SAM#199).
- Validated content: `5f3a3ebe` (code and tests). This record follows in a docs-only commit.
- PR description: holds the final head and the hosted checks.
- Follows: `documentation/PanelsBySolid-MultiElement-Q4.md` (SAM#198).

## What the user saw

On the contemporary house (`SAM_daily/2026-10-13-House/contemporary1`, `contemporary1.3dm` + `import-test.gh`):

- Apertures: 18 (18 windows, 0 doors).
- Panels: 28 with an input at 0.25, and 38 with it at 0.05; 11 solids skipped at 0.25.
- Skipped at 0.25: the internal walls and the ground slab, reported as "thickness 12 m over smallest extent 0.2 m = 60" (slab), and 0.3, 0.7, 1.5, 2.75 m (walls).

## Which input is changed

The input is **`minThickness_`**, not `maxThicknessRatio_`.

- The saved `import-test.gh` (read with GH_IO) has `CreatePanelsBySolids` 1.0.1 with 4 inputs: `_solids`, `apertureGeometries_`, `minThickness_` = 0.08, `snap_` = 0.3.
- `maxThicknessRatio_` is a hidden (Voluntary) input, so it stays at its default, 0.2.
- `minThickness_` is a thickness in metres: elements thinner than it are skipped. `maxThicknessRatio_` is thickness over the smallest in-plane extent.
- Reproduced exactly, both through the API (committed fixture) and through the real component (headless Rhino 8):
  - `minThickness_` 0.25 → 28 panels, 11 skipped.
  - `minThickness_` 0.05 → 38 panels, 2 skipped.
- The SAM#198 closeout says "users set 0.25 in Grasshopper". That value was meant for `maxThicknessRatio_`, but went into the visible `minThickness_`.

## Root cause

`ElementPlanes` dropped every face pair closer than `minThickness` **before** ranking.

1. At 0.25 m, the sides of the 0.15 m walls and of the 0.2 m slab were dropped.
2. The only pairs left were the element's ends, or its top and bottom: e.g. the slab's 12 m length, or a wall's 2.75 m height. They have material between them, so they became candidate elements.
3. They failed the ratio test (e.g. 12 / 0.2 = 60), so the solid was reported as "not panel-like", with that pair's distance as its "thickness".
4. The Grasshopper Warning then advised raising `maxThicknessRatio_` to the ratio in the log (up to 60).

**Following that advice was harmful.** With `minThickness` 0.25 and `maxThicknessRatio` 61, the old code created 52 panels. 22 of them were bogus:

- The 8 internal walls gave 19 panels: `Wall` and `Floor` panels 0.3–2.775 m thick, set across the wall (its height or length taken as its thickness).
- The ground slab became three `Wall` panels 2, 4.5 and 6 m thick.

The geometry was never misclassified at a sensible setting. At `minThickness` 0.05 or 0.08:

- Every internal wall is a `Wall` at 0.15 m.
- The ground slab is a `Floor` at 0.2 m.
- Coverage of their sides is ≥ 95% (new test).

## Fix (smallest reliable change)

`SAM/SAM.Analytical/Create/PanelsBySolid.cs`:

- **Pairs:** pairs down to `min(minThickness, 10 × tolerance_Snap)` (1 cm by default) take part in ranking. Closer faces are steps and reveals.
- **Ranking:** unchanged (thickness over band width).
- **Order of tests:** a plane that fails `maxThicknessRatio` is rejected first, as before.
- **Too thin:** a panel-like plane thinner than `minThickness` is marked `TooThin` and gives no panel. It still claims its material, so the element's ends and top/bottom are dropped as cross-cuts instead of becoming elements.
  - Checking the ratio first matters. A thin plane that is not panel-like (e.g. a 0.15 × 0.3 m strip at a door in the external ring) must not claim material. When it did (first attempt), `GF_Wall_External` lost a panel at `minThickness` 0.25.
- **Reason reported:** taken from the skipped plane with the largest overlap:
  - `Skipped as below minThickness: its thickness, 0.15 m, is less than minThickness (0.25 m). If it is a wall, floor or roof, lower minThickness below 0.15 m.`
  - `Skipped as not panel-like: thickness … gives a ratio of …, above maxThicknessRatio (…). A maxThicknessRatio above … would keep it.`
- **Not changed:**
  - `PhysicalThickness` stays the measured pair distance; `ConstructionThickness` is untouched.
  - Openings, Air panels and snapping are unchanged.
  - Classification uses geometry only (no name filters).
  - Thresholds and defaults are unchanged: `minThickness` 0.08, `maxThicknessRatio` 0.2.
- **New optional parameter:** `int? sourceIndex = null` (last), used only for diagnostics numbering. Source-compatible; every caller is rebuilt by BuildAll.

## Results (before → after)

Contemporary house, panels per setting (API on the committed fixture, `minThickness` / `maxThicknessRatio`):

| Setting | Before | After | Change |
|---|---|---|---|
| 0.25 / 0.2 (user, "0.25") | 28, 11 skipped | 28, 11 skipped | Same panels. The 8 internal walls and the ground slab are now reported as below minThickness at 0.15 / 0.2 m. |
| 0.05 / 0.2 (user, "0.05") | 38, 2 skipped | 38, 2 skipped | Identical |
| 0.08 / 0.2 (defaults) | 38, 2 skipped | 38, 2 skipped | Identical |
| 0.08 / 0.25 (SAM#198 acceptance) | 39, 1 skipped | 39, 1 skipped | Identical |
| 0.08 / 0.05 | 2, 17 skipped | 2, 17 skipped | Identical |
| 0.25 / 61 (old advice followed) | 52 (22 bogus) | 29, 9 skipped | No bogus panels; `GF_Wall_External` 10 → 9 (see Limitations) |

- Panels are identical (count, type and thickness per solid) before and after at every setting except 0.25 / 61.
- Stairs stay excluded: `ST_Stair_GF_to_FF` (ratio 0.398) at every sensible setting. On the simple house, all 14 treads stay excluded as not panel-like, and the two balustrades as below minThickness (0.05 m).
- Apertures are unchanged: 18/18 windows, 0 doors, with the same sizes and depths.

## Diagnostics convention (proposal, implemented in the BySolids path only)

One line format for every SAM Diagnostics output: `Severity: Subject: Message`.

- **Severity:** `Info` (what was done), `Warning` (input skipped or changed), `Error` (the run could not complete). Helper: `SAM.Core.Query.DiagnosticMessage(LogRecordType, subject, message)`; `LogRecordType.Message` is written as `Info`.
- **Subject:** deterministic source identification, e.g. `Solid #18 'GF_Slab_Ground'`, with the input index first. It is omitted for summaries.
- **Message:** full sentences starting with a capital letter, units on every value, and the value that would change the outcome.
- **Counts:** `SAM.Core.Query.Plural(count, singular, plural)` gives `1 panel`, `0 panels`, `2 Air Panels`.
- **Terms:** Solid, Panel, Aperture, Window, Door, Air Panel.
- **Summary first:**
  - `Info: Created 38 panels (0 Air Panels) from 19 solids. Skipped 2 solids.`
  - `Info: Created 18 apertures (18 windows, 0 doors) from 18 solids (18 window solids, 0 door solids; numbered windows_ first). No solids were skipped.`
- **Detail lines:** one line per Solid and per element plane. Lines with a count of zero are left out, which removes the old "holes: 0 …, notches: 0 …" noise.
- **Component runtime messages:** one Warning per skip reason, naming the solids (below minThickness_, not panel-like, other). There are no per-face messages.
- **Relation to SAM_OCCT:** its `OcctDiagnostic` (`CODE [index]: Message`) and `SolverDiagnostic` (`[Stage/Code/Severity] Message`) carry a code and severity as data.
  - Future step: give SAM_OCCT, TAS, OpenStudio and Rhino the same text form through `Core.Query.DiagnosticMessage`, keeping their codes in the message or subject.
  - Not done here: no other component is changed.
- **Output names:** both components keep their output `log` (not renamed to `Diagnostics`). Renaming would break wires in saved `.gh` files.

## Grasshopper components

`SAMAnalyticalCreatePanelsBySolids` (1.0.1 → 1.0.2) and `SAMAnalyticalCreateAperturesBySolids` (1.0.0 → 1.0.1):

- Kept: GUIDs, names, parameter order, parameter names and visibility, defaults and output names.
- Rewritten: component descriptions and tooltips. They say:
  - which solids are expected, and how pairs, mid-planes and panel types are derived;
  - that `PhysicalThickness` is the measured thickness;
  - how openings and Air Panels arise, and that nothing is invented;
  - what each threshold means, with its unit and default;
  - that `minThickness_` is a thickness, not a ratio, and how to show the hidden `maxThicknessRatio_` (zoom in, +);
  - how solids are numbered, and the `log` line format.
- Versions are bumped because descriptions are saved in `.gh` files. Placed components keep their old tooltips until replaced; behaviour and wiring are unaffected.
- `log` lines use the convention. Runtime Warnings per reason (headless run, `minThickness_` 0.25):
  - `Skipped 9 solids thinner than minThickness_ (0.25 m): #7 'UF_Wall_Internal', … #18 'GF_Slab_Ground'. minThickness_ is a thickness in metres, not a ratio. …`
  - `Skipped 2 solids as not panel-like by maxThicknessRatio_ (0.2): #0 'ST_Stair_GF_to_FF', #2 'UF_Gable_Cross_South'. …`
- Also aligned: `Query.SnapToJunctions` reports `Info: Checked 38 panels for junctions: extended 83 edges and trimmed 82 edges (snap distance 0.3 m).` The old text said "Snapped" for every panel checked.

## Files changed

- `SAM/SAM.Analytical/Create/PanelsBySolid.cs`: classification fix, `sourceIndex`, diagnostics.
- `SAM/SAM.Analytical/Create/AperturesBySolids.cs`: diagnostics wording only.
- `SAM/SAM.Analytical/Query/SnapToJunctions.cs`: diagnostic wording only.
- `SAM/SAM.Core/Query/DiagnosticMessage.cs`, `SAM/SAM.Core/Query/Plural.cs`: new.
- `Grasshopper/SAM.Analytical.Grasshopper/Component/SAMAnalyticalCreatePanelsBySolids.cs`, `…/SAMAnalyticalCreateAperturesBySolids.cs`: descriptions, summary, Warnings, versions.
- Tests:
  - `SAM/SAM.Tests/PanelsBySolidTests.cs`: +5, plus the balustrade test made stricter.
  - `PanelsBySolidComplexHouseTests.cs`: +4.
  - `DiagnosticMessageTests.cs`: new, 10.
  - `AperturesBySolidsTests.cs` and `PanelsBySolidMultiElementTests.cs`: wording assertions updated.

## Validation (local, on `5f3a3ebe` content)

- Focused tests, `dotnet test SAM/SAM.Tests/SAM.Tests.csproj --filter "FullyQualifiedName~PanelsBySolid|FullyQualifiedName~AperturesBySolids|FullyQualifiedName~DiagnosticMessage"`: **91 passed**.
- Full suite, `dotnet build SAM/SAM.Tests/SAM.Tests.csproj -c Release` then `dotnet test … -c Release --no-build`: **3552 passed, 0 failed**. That is 3533 before + 19 new.
- Grasshopper project, `dotnet build Grasshopper/SAM.Analytical.Grasshopper/SAM.Analytical.Grasshopper.csproj -c Release`: compiles.
  - The project-only build ends in a post-build `xcopy` error, because `$(SolutionDir)` is undefined. The compile and the DLL copies succeed; this is pre-existing and not caused by this change.
  - Machine-local side effect: that post-build step copies `SAM\build\*.dll` into `%APPDATA%\SAM`. Rebuild `sow/2026-Q4` with BuildAll to return to the base.
- New regression tests:
  - Thin wall and slab below minThickness, even with ratio 100: skipped as below minThickness at 0.15 / 0.2 m.
  - The slab converts to a 0.2 m Floor at `minThickness` 0.05.
  - Historic walls 0.6 and 1.0 m thick give one Wall at the measured thickness.
  - Contemporary house at 0.25: the 8 internal walls and the ground slab are reported at 0.15 / 0.2 m, 11 solids skipped including the stair. With ratio 100, no panel is thicker than 0.3 m.
  - At 0.05 and 0.08: internal walls are Wall 0.15 and the slab is Floor 0.2, with ≥ 95% side coverage.
- Simple-house regression, `morning-test/Untitled2_meters.3dm` (PanelsBySolid at defaults, snap 0.3, SAM_OCCT `Create.AdjacencyCluster`):
  - 23 panels + 1 Air; 16 stair and balustrade solids skipped.
  - Cluster **11 spaces / 56 panels / 1 Air**, identical to the accepted baseline.
- Real components (headless Rhino 8 GH_Document, the 19 + 18 objects of `import-test.gh`, referenced, picked by fixture id):
  - Apertures: 18 (18 windows).
  - Panels: 28 / 38 / 38 at `minThickness_` 0.25 / 0.05 / 0.08.
  - Log lines and Warnings as quoted above.
- The source `.3dm` and `.gh` were only read; harness copies were made in the session scratchpad.
- Hosted CI: see the PR description.

Machine-local, not committed: the Rhino 8 harness (`Census.csproj`, new modes `regress` and `ghcomp`). It was copied from the SAM#194 session scratchpad into this session's scratchpad. It loads SAM from `SAM\build` and SAM_OCCT from `SAM_OCCT\build`. Copy it before switching machines if needed again.

## Limitations and risks

- At `minThickness` 0.25 the result is still 28 panels. The skips are correct for that setting; the fix is the honest reason and the advice to lower `minThickness_`.
  - To keep the gable at 0.214, set the hidden `maxThicknessRatio_` to 0.25 and leave `minThickness_` at 0.08 → 39 panels.
- `maxThicknessRatio_` stays hidden by default, to keep saved definitions unchanged. Exposing it by default would be a separate decision.
- With an absurd ratio (≥ 0.5, i.e. no ratio filter), a thin non-element strip below minThickness can claim material from a neighbouring element plane:
  - `GF_Wall_External` gives 9 instead of 10 panels at 0.25 / 61.
  - No effect at any setting up to 0.25 / 0.25 on either house.
- Stairs are excluded only by proportion (ratio 0.398 for the contemporary stair). A stair built as thin plates would be indistinguishable from a wall by geometry alone. That ambiguity is reported, not guessed.
- **High ratios admit bogus panels (pre-existing, not changed by this PR).** Found in the acceptance review (2026-10-15): a ratio sweep 0.2–100 on the committed fixture, run on this PR and on base `0649e15e`, gave identical panels for both cases:
  - `maxThicknessRatio` ≥ 0.4: `ST_Stair_GF_to_FF` gives a **1.125 m thick Wall** (vertical mid-plane x = 3.638, 6.72 m²). At ≥ 1 it also gives a 0.176 m Floor. The stair's own log advice ("a maxThicknessRatio above 0.398 would keep it") leads to this panel.
  - `maxThicknessRatio` ≥ 0.5 with `minThickness` 0.08 or 0.05: `GF_Wall_External` gives a bogus **0.15 m Floor** (a horizontal band of the 12 × 10 m ring at z = 2.675, 13.9 m², ratio 0.15 / 0.3 = 0.5) and a **73 m² Air Panel** over the plan, and one wall plane is lost (panel count stays 10).
  - Up to 0.3 the panels are correct at `minThickness` 0.05, 0.08 and 0.25. The previously observed bogus panels (internal walls and ground slab 0.3–6 m thick) do not appear at any ratio: base gives 52 panels, 20 thicker than 0.3 m, at 0.25 / 61; this PR gives none.
  - Guidance: keep `maxThicknessRatio_` at or below 0.3. A possible follow-up (separate decision): make the Warning not advise a ratio above about 0.3, or add a geometric stair test.
- Manual Grasshopper visual acceptance: not run by the agent (owner).

## Next step

Owner Grasshopper visual acceptance after BuildAll on this branch: `import-test.gh` at `minThickness_` 0.08 (or 0.05) with `maxThicknessRatio_` 0.25. Then owner review, merge into `sow/2026-Q4`, and the `PROJECT_PROGRESS.md` closeout.
