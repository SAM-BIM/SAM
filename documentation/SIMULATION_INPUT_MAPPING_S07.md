# S07 - Minimal Phase B physical-input mapping contract

Task S07 of the [simulation roadmap](SIMULATION_ROADMAP_Q4_2026.md). Documentation only: this document implements nothing and
changes no behaviour.

Phase A is met ([SIMULATION_ROADMAP_Q4_2026.md](SIMULATION_ROADMAP_Q4_2026.md)). S07 defines what S08 (Tas read-back) and S09
(OSM/IDF read-back) compare, against which reference, how tolerantly, and how outcomes aggregate.

**Owner approval.** Decisions D-ROUTE, D-F2 (V0/V1/V2), D-F8, D-F9 (A + C), D-FIX, the film convention and every tolerance
in this document are **approved by the owner, 2026-10-20 (S07 plan revision 3)**. The owner can amend any of them during
review of the pull request that introduces this document.

**Amendment L1 (lighting, 2026-10-21).** The owner approved replacing the revision 1-3 identity between the Tas lighting
view coefficient and the EnergyPlus `Fraction Visible` with the lighting energy-split convention in §5.1:
- the reference category and the baseline p₀ = 0.576 with s = 0.2415 / 0.576;
- preservation of authored radiant proportions;
- one absent-value fallback for both engines;
- T-LIGHT-SPLIT as an ACCEPTED-TRANSFORMATION for conformance to that declared input convention.

The amendment establishes no engine-physics equivalence. Implementing it in the writers is downstream work (§5.1).

## Evidence pins

Local = `git ls-remote origin refs/heads/sow/2026-Q4`, checked 2026-10-20; every remote is `github.com/SAM-BIM/*`. Each
prefix is a path inside that repository at the pinned SHA, and the aliases below stand for it throughout the document.

| Repo | SHA | Prefix |
|---|---|---|
| [SAM](https://github.com/SAM-BIM/SAM) | `6836595d73fc1eeb1bdcafd743918749d6fc6a6d` | `SA/` = `SAM/SAM.Analytical/` |
| [SAM_Tas](https://github.com/SAM-BIM/SAM_Tas) | `e84aeff687033354e3f17891635fdf10d42ad68f` | `MA/` = `SAM_Tas/SAM.Analytical.Tas/` |
| [SAM_Tas_Grasshopper](https://github.com/SAM-BIM/SAM_Tas_Grasshopper) | `e94880e6088e6d9873d2fb6872f4c48939204b10` | `TG/` = `Grasshopper/SAM.Analytical.Grasshopper.Tas/Component/` |
| [SAM_OpenStudio](https://github.com/SAM-BIM/SAM_OpenStudio) | `7f216657bd03b740644ae12d6ff099396ee997e0` | `OS/` = `SAM_OpenStudio/SAM.Analytical.OpenStudio/Convert/ToOpenStudio/` |
| [SAM_UI](https://github.com/SAM-BIM/SAM_UI) | `45263ff0d13381a9baf95b03a51a8e758b20a9e1` | |
| [SAM_Validation](https://github.com/SAM-BIM/SAM_Validation) | `2a3acfafb3919b4a0b81d67ef16a4b9d1e222f5d` | |
| [SAM_gbXML](https://github.com/SAM-BIM/SAM_gbXML) | `43b5d018ac7a8a14f332e8bcde9cb0d613bf1efb` | |
| [SAM_LadybugTools](https://github.com/SAM-BIM/SAM_LadybugTools) (added by amendment L1, checked 2026-10-21) | `0ffbaecf1097a33de2044ee919c963eaeb0a0da2` | `LB/` = `SAM_LadybugTools/SAM_LadybugTools.Analytical/Convert/` |

Names not established from code or reliable documentation are marked **UNVERIFIED**; IDF names produced only by the OpenStudio translator are marked *(translator)*.

---

## 1. Contract route and current production behaviour (D-ROUTE)
- **Contract Tas route:** SAM → T3D → TBD with `WorkflowSettings.T3DRoute = Direct` and `UseWidths = false`.
  - Pipeline: `WorkflowCalculator.Calculate` → `Convert.ToT3D` (`MA/Convert/ToT3D/T3DDocument.cs`, `MA/Convert/ToT3D/Building.cs`, `MA/Convert/ToT3D/Element.cs`, `MA/Convert/ToT3D/Window.cs`, `MA/Convert/ToT3D/New/Window.cs`; plan `MA/Query/T3DImportPlan.cs`, `MA/Classes/ToT3DOptions.cs`) → `Convert.ToTBD(t3DDocument, …)` (`T3DDocument.ExportNew`) → the post-export steps of `MA/Classes/WorkflowCalculator.cs`:
    1. `UpdateSpaces` (L677), `UpdateFacingExternal` (L690);
    2. `UpdateBuildingElements` → `UpdateConstructions` (L710), `UpdateIds` (L715), `UpdateReversed` (L724);
    3. `UpdateApertureDefinitions` (L741), `UpdateZones` (L752), `AddDesignDays` (L758), `SetApertureTypes` (L767).
  - The gbXML-only steps `AssignAdiabaticConstruction` and `UpdateAdiabatic` (L698, L706) do not run.
- **Not the older API:** `Convert.ToTBD(AnalyticalModel, …)` (`MA/Convert/ToTBD/TBD.cs:77-161`) writes `zone.floorArea` / `zone.volume` itself (via `MA/Convert/ToTBD/Building.cs:47` and `MA/Modify/Update.cs:612,623`). It is outside the contract.
- **Production today is gbXML, and documenting this contract does not change it:**
  - `WorkflowSettings.T3DRoute` defaults to `GbXML` (`MA/Classes/WorkflowSettings.cs:20-32`).
  - SAM_UI defaults to `DirectT3D = false` (`SAM_UI/SAM.Analytical.UI/Classes/TBDConversionOptions.cs:30-34`).
  - The Both benchmark producer prepares a shared gbXML and builds `WorkflowSettings` with no route, i.e. gbXML (SAM_Tas repository root, `benchmark/SAM.Analytical.Tas.Benchmark.Cli/Program.cs:17,128-160`). It also sets `AddIZAMs = true`.

  Running S08 on the contract route therefore needs either a later, separately approved integration task, or an S08 harness that selects Direct explicitly. **S07 implies no production switch.**
- **Future production default (owner, 2026-10-20; owner decision 7 in [SIMULATION_ROADMAP_Q4_2026.md](SIMULATION_ROADMAP_Q4_2026.md#owner-decisions-outstanding)):**
  - First approval: "Change the Tas production default to SAM → T3D → TBD, T3DRoute.Direct, UseWidths=false."
  - Revised later the same day: "Record Direct T3D with UseWidths=true as the owner-preferred candidate default." Production defaults change only after independent verification, on the approved fixtures, that the native areas and volumes match the intended panel convention (§4.1). `UseWidths=false` stays available as an explicit option. Any fallback must be reported with its reason and the actual setting, never applied silently. Owner fallback direction: if candidate validation fails, the proposed widths setting reverts to `false` and the reason is reported (§4.1).
  - Neither statement is implemented. Neither authorises a code change in S07 or starts S08. The three defaults above stay gbXML until a separate integration PR lands against the criteria of roadmap decision 7.
  - **Panel convention and what the measurements show:** Recorded measurements establish how widths change native geometry. They do not independently establish the source model's panel convention. SAM supports mid-surface panels, including PanelsBySolid output carrying PhysicalThickness. The fixture's provenance and declared convention determine whether width offsets are intended.
    - **Measured, original scope:** one real model at the SAM_Tas pin, 9 spaces. Widths OFF reproduced SAM's shell volume exactly (3544.0 m³ in total); widths ON reduced it by 11.9 % (3121.4 m³) (`MA/DIRECT_T3D_ROUTE.md:131-136,258-286`). These figures show what widths do to native geometry for that model, not which convention its panels follow.
    - **Mid-surface support in code:** `Create.PanelsBySolid` turns a closed solid of thin building elements into analytical mid-surface panels (`SA/Create/PanelsBySolid.cs:15-25`) and sets `PanelSourceParameter.PhysicalThickness` on each panel (L404). The direct converter uses that thickness as the element width when it is finite and > 0, and gives each thickness its own element (`MA/Query/T3DImportPlan.cs:513-520,550-565,901-910`).
    - **Source wording not relied on:** the SAM_Tas direct-route record (`MA/DIRECT_T3D_ROUTE.md:133,284`) and the widths-ON workflow note (`MA/Classes/WorkflowCalculator.cs:618-621`) describe SAM panels as inner surfaces. SAM_Tas already records that wording as misleading for mid-surface panels (SAM_Tas repository root, `PROJECT_PROGRESS.md:44`). This document does not change those sources.
  - At the SAM_Tas pin, a failed Direct conversion stops the run with an error rather than falling back to another route (`MA/Classes/WorkflowCalculator.cs:632-640`). SAM_UI and Both-producer fallback behaviour was not checked.
- **Contract-route facts from evidence:**
  - **Geometry:** with widths OFF, T3D = TBD = SAM shell area and volume. This was measured on a real model (`MA/DIRECT_T3D_ROUTE.md:258-275`). S08 re-verifies it per fixture.
  - **Floor-area flag:** `element.zoneFloorArea` (`MA/Convert/ToT3D/T3DDocument.cs:269`).
  - **Frame:** `window.frameWidth` (written when the spec value is not NaN), `isPercFrame = true` and `framePerc` (written when the frame percentage is not NaN) (`MA/Convert/ToT3D/T3DDocument.cs:306-314`). The rule is T-FRAME in §5.
  - **Aperture identity:** `MA/Modify/UpdateIds.cs:12` stamps SAM identities on the TBD. Apertures are matched by `MA/Query/Match.cs`, `MA/Query/PaneStampMatch.cs` and `MA/Query/ApertureByElementName.cs`. S08 keys every comparison on these identities, not on names.
  - **Layer orientation:**
    - TAS chooses the reversed side of a partition, and `reverseElement` has no effect.
    - `MA/Modify/UpdateReversed.cs:25` sets it so that the space earlier in `AdjacencyCluster.GetSpaces()` sees the layers as listed. Horizontal surfaces are left to TAS (`MA/DIRECT_T3D_ROUTE.md:101-115`).
    - S08 compares layer order per zone side.

## 2. Findings carried forward (other engines and non-geometry quantities)
1. **U and g are engine-derived in both engines.** Both receive layers (`MA/Modify/UpdateMaterial.cs:167-240`, `MA/Modify/UpdateConstruction.cs:9-66`; `OS/Construction.cs`, `OS/Material.cs`, `OS/Aperture.cs:153-283`). Only the OS no-pane-layer fallback writes U and SHGC directly (`OS/Construction.cs:85-172`).
2. **Gain densities:**
   - Both engines compute SAM total W ÷ stored `SpaceParameter.Area` (`MA/Modify/UpdateInternalCondition.cs:94-271`; `OS/InternalCondition.cs:45-190`).
   - OS uses the authored occupancy and equipment radiant fractions, falling back to 0.3 / 0 only when a value is absent or NaN (`OS/InternalCondition.cs:81-160`). The latent-equipment radiant fraction is 0 (L183).
   - **Lighting (amendment L1):**
     - SAM `LightingRadiantProportion` is the **total** radiant share (long-wave plus short-wave) of the lighting gain entering the room, as in Tas.
     - `LightingViewCoefficient` is a Tas comfort coefficient, not an energy fraction. It has no EnergyPlus input.
     - Pinned behaviour:
       - the OS writer writes the view coefficient as `Fraction Visible`, with fallbacks 0.32 / 0.25 (`OS/InternalCondition.cs:125-136`);
       - the OS importer stores `Fraction Visible` in the view coefficient (SAM_OpenStudio repository root, `SAM_OpenStudio/SAM.Analytical.OpenStudio/Convert/ToSAM/InternalCondition.cs:236-237`);
       - the Tas writer leaves an absent radiant proportion to the TBD's value (`MA/Modify/UpdateInternalCondition.cs:96-99`).

       All three conflict with §5.1, and the required changes are listed there.
3. **Ventilation:**
   - **Tas:** the four SAM airflow bases are a requirement, and the Ventilation profile is the delivery selection (`MA/Modify/UpdateInternalCondition.cs:273-290`; `SAM_Tas/SAM.Analytical.Tas.TM59.Tests/VentilationRequirementMetadataTests.cs:181`). The requirement crosses in `TBD.zone.description` `[SAM_META_V1]=` (`MA/Classes/SAMZoneMetadata.cs:45-94`). `freshAirRate` (`MA/Modify/UpdateInternalCondition.cs:141`) does not supply the zone.
   - **OS:** always delivers through `DesignSpecificationOutdoorAir` "Sum", unscheduled when there is no profile (`OS/InternalCondition.cs:261-293`). A per-space `OutsideSupplyAirFlow` DSOA replaces the SpaceType DSOA (`OS/AnalyticalModel.cs:292-298`).
   - Exhaust is mapped by neither engine.
4. **Window opening:**
   - Tas writes aperture types (`MA/Modify/SetApertureType.cs:259-313`).
   - The SAM OS converter maps no `OpeningProperties` (`OS/Aperture.cs:290-296`). EnergyPlus itself can model opening airflow (AirflowNetwork), so this is a **converter limitation**, not an engine one.
5. **Weather:**
   - Tas receives in-memory `WeatherData` through `AddWeatherYear` / `SetAnnualParameter`, float32, no file (`SAM_Tas/SAM.Weather.Tas/Modify/UpdateWeatherData.cs:51-106`).
   - OS receives an EPW; an explicit EPW wins over the embedded export (`OS/AnalyticalModel.cs:83-91`, `OS/Weather.cs`).
6. **Schedules:**
   - Tas: value, 24-hour or yearly profile; one profile for all non-HDD day types (`MA/Modify/Update.cs:16-81`, `MA/Modify/AddInternalCondition.cs:27-42`).
   - OS: `ScheduleFixedInterval`, 1 h, from 1 Jan, rotated to the EPW weekday (`OS/Profile.cs:26-307`).
   - The hour-label convention and leap-year handling are UNVERIFIED.
7. **Recorded in passing, not fixed:**
   - `MA/Modify/UpdateInternalCondition.cs:326,331`: the `IsNaN` guard looks inverted.
   - `SA/Classes/Profile.cs:1041-1057`: `Count` = Max−Min.
   - Tas zeroes per-area gains when Area is NaN or 0.
   - Tas skips infiltration when there is no profile.
   - OS drops holes (`OS/Panel.cs:68-108`).
   - OS docs drift.
   - The "inner surfaces" wording is misleading for mid-surface panels (SAM_Tas repository root, `PROJECT_PROGRESS.md:44`).

---

## 3. Reference: the effective run input
The reference is **SAM intent for the run actually performed**: the S04 frozen snapshot, plus the S03 manifest overrides, resolved with this precedence:

| Input | Precedence (first present wins) |
|---|---|
| OS weather | explicit EPW → embedded `WeatherData` exported by the SAM#212 writer |
| OS design days | `OpenStudioRunOptions.DdyPath` → conversion-option DDY → model `Heating/CoolingDesignDays`. DDY filtered to 2 named days unless `ImportAllDesignDays`. Never merged. |
| Tas weather | `WeatherData` passed to `WorkflowCalculator`, else the model's. How SAM_UI builds it from an explicit EPW is UNVERIFIED; taken from the S03 manifest. |
| Tas design days | `MA/Query/DesignDays_Authoritative.cs:45-83`: caller lists → supplied weather's derived days → model lists |
| OS outdoor air | per-space `OutsideSupplyAirFlow` DSOA → SpaceType DSOA |
| Occupancy | `SpaceParameter.Occupancy` → Area / `AreaPerPerson` |
| Gain density denominator | stored `SpaceParameter.Area`. Missing for per-area inputs → S-INVALID. |

**Independence rule.**
- Expected values are computed from raw stored parameters with the formulas in this contract. SAM core `Calculated*` queries may be used only as a cross-check.
- Converter code (SAM_Tas and SAM_OpenStudio writers) is never called to produce an expected value.
- Native values come from TBD/OSM/IDF read-back.
- `[SAM_META_V1]` is a SAM-written carrier: it shows that a requirement crossed, never that it was delivered.

### Design-day roles (kept distinct)
| Role | Source | Purpose in Phase B |
|---|---|---|
| **Authored** | model `AnalyticalModelParameter.Heating/CoolingDesignDays`, or caller-supplied lists (e.g. `TG/TasUpdateDesignDays.cs:136`, which calls `MA/Modify/AddDesignDays`) | reference when selected by §3 precedence |
| **Weather-derived** | `SA/Query/CoolingDesignDay.cs:12`, `SA/Query/HeatingDesignDay.cs:12` (from `WeatherData`) | reference when selected by precedence (Tas step 2) |
| **Native read-back** | Tas `MA/Query/DesignDays.cs:42,61` (path / `SAMTBDDocument` → SAM `DesignDay`; also used by `TG/TasCreateDesignDays.cs:35` → L113); OS `SizingPeriod:DesignDay` objects | the S08/S09 comparison side |
| **Governing** | identified from simulation results (`MA/Convert/ToSAM/Results.cs:43,200-205`) | **results (S10/S11), not input equivalence** |

The Tas native read-back uses a SAM_Tas reader. It is independent of the writer only if S08 confirms that it reads the TBD fields rather than writer state. Otherwise S08 adds a raw-field read.

---

## 4. Geometric reference (F9 basis)
- **Independent geometric floor area:** the sum of the authored floor-panel polygon areas (`SA/Query/FloorArea.cs:73`, `GeometricalFloorPanels` at L123). It is computed directly, not via `CalculatedArea`, which prefers the stored Area.
- **Independent geometric volume:** `shell.Volume()` of the closed shell of the space's authored panel polygons. It is computed directly, not via `Query.Volume` / `CalculatedVolume`, which prefer the stored Volume.
- **Panel convention:** recorded per fixture (inner-face or mid-surface), from the fixture's provenance and declared convention. It is not inferred from engine measurements (§1). On the contract route (`UseWidths=false`) both engines are expected to use the polygons as given, so the convention does not alter the engine comparison. It only interprets what the geometric volume represents.
- **The three volumes:**
  - **geometric** (the engine-comparison reference);
  - **stored** `SpaceParameter.Volume` (reported; used for Tas `ticV` ACH and `CalculatedInfiltrationAirFlow`);
  - **net-air** (no SAM use found; out of scope, UNVERIFIED).

### 4.1 Widths-ON reference (candidate default `UseWidths=true`; drafted at the owner's request 2026-10-20, detail for owner review)
The contract route (§1, D-ROUTE) stays `UseWidths=false`, and the §4 reference above is unchanged for it. This subsection defines the reference used **only** to verify the owner-preferred candidate default before any production change. Tolerances are those of rows 1a, 1b, 2, 7b and 8b, unchanged.

- **What TAS does with widths ON (measured on two geometries only):** TAS treats every input polygon as a centre line and offsets each zone surface inwards by half its building-element width; on the direct route this includes floors and roofs (`MA/Classes/ToT3DOptions.cs:13-24`; `MA/DIRECT_T3D_ROUTE.md:131-136,280-283`).
  - Box 5 × 4 × 3 m with walls 0.30, roof 0.35 and floor 0.40 m: floor 17.39 m², volume 45.649 m³. Widths OFF gives 20 m² and 60 m³.
  - Real-model zone: height 4 − (0.3335 + 0.4127) / 2 = 3.627 m.
- **Element width:** the panel's `PanelSourceParameter.PhysicalThickness` when it is finite and > 0 (not used for a ghost, i.e. air, element: L517); otherwise the construction's `DefaultThickness` when non-zero and not NaN; otherwise the sum of its layers; 0 when none applies. Panels of one construction at different physical thicknesses get separate elements (`MA/Query/T3DImportPlan.cs:513-520,550-565,901-910`). S08 recomputes the width from raw SAM parameters (§3 independence rule) and never calls the converter.
- **The fixture's declared panel convention decides the expected value** (D-FIX already requires a recorded convention):
  - **centre-line (mid-surface) panels:** offsets are intended. Expected floor area and volume come from the solid bounded by each bounding panel's plane moved into the space by half its element width. An internal partition contributes half its width to each zone it bounds. Expected surface areas are the faces on the moved planes, clipped by the neighbouring moved planes;
  - **inner-face panels:** offsets are not intended. The expected value stays the §4 polygon geometry, so a widths-ON native result outside tolerance is FAIL: the candidate default does not fit that fixture's geometry;
  - **convention not recorded:** MODEL-INTERPRETATION-REQUIRED (rank 3).
- **Dependent rows:** under widths ON, the "geometric V" of rows 7b and 8b means the volume expected by this subsection. D-F9 C (stored ≈ geometric) is not redefined here; see OD-W2.
- **UNVERIFIED until read-back (rank 4 while readable):**
  - TAS's offset for non-orthogonal or non-convex zones;
  - surface and opening areas under widths ON (whether TAS keeps, shifts or clips the aperture polygons);
  - a panel whose two zones would assign different widths; zero-width (air) panels; ground floors;
  - identity mapping. With widths ON every surface is offset from its panel, and the post-export steps match by identity first and by a half-width offset test second (`MA/DIRECT_T3D_ROUTE.md:122`). S08 read-back must confirm the identity mapping still holds.
- **Owner fallback direction (recorded):** if candidate validation fails, the proposed widths setting reverts to `false` and the reason is reported. This governs the default proposed for the integration and is decided before implementation. It does **not** authorise a silent or automatic runtime fallback: every run reports its actual setting and any fallback reason (roadmap decision 7).
- **Open owner decisions:**
  - **OD-W1:** approve this reference. Also decide whether the widths-ON verification runs inside S08, as an extra configuration with its own approval, or in the default-switch integration task. It is not added to S08 by default.
  - **OD-W2:** which volume D-F9 C compares the stored `SpaceParameter.Volume` with under widths ON.

---

## 5. Mapping contract (tolerances per quantity and unit)

All tolerances in this section: approved by the owner, 2026-10-20 (S07 plan revision 3).

Numeric test, used only when both values are present: `d = |n − s|`. A value is within tolerance when `d ≤ abs` **or** `d / max(|n|,|s|) ≤ rel`.

| # | Quantity (unit) | SAM reference | Tas native (contract route) | OS native / IDF | Rule | rel | abs | Evidence | Limitation |
|---|---|---|---|---|---|---|---|---|---|
| 1a | Zone floor area (m²) | §4 geometric; stored reported | `TBD.zone.floorArea` (TAS-computed from `element.zoneFloorArea`) | geometric; IDF `Zone` *(translator)* | native vs geometric | 0.5 % | 0.01 m² | `MA/Convert/ToT3D/T3DDocument.cs:269`; `MA/DIRECT_T3D_ROUTE.md:258-275` | Air panels as virtual floor: UNVERIFIED |
| 1b | Surface gross area; pane and frame area (m²) | `Face3D.GetArea()`; `Query.Area(aperture, out pane, out frame)` | TBD surface area keyed by `UpdateIds` identity; `framePerc` | `Surface` / `SubSurface` (`OS/Panel.cs`, `OS/Aperture.cs:100-130`) | per panel and opening by identity | 0.5 % | 0.01 m² | `MA/Modify/UpdateIds.cs:12`; `MA/Query/Match.cs`, `MA/Query/PaneStampMatch.cs` | OS hole drop → EXPORT-DEFECT; frame → T-FRAME |
| 2 | Zone volume (m³) | §4 geometric; stored reported | `TBD.zone.volume` (measured = SAM shell, widths OFF) | geometric; IDF `Zone` *(translator)* | native vs geometric; stored vs geometric per D-F9 | 0.5 % | 0.01 m³ | `MA/DIRECT_T3D_ROUTE.md:258-275`; `OS/Space.cs:16-37` | — |
| 3a | Opaque layers: thickness (m), λ (W/m·K), ρ (kg/m³), c (J/kg·K), absorptance and emissivity (–) | `ConstructionLayers` + `MaterialLibrary` | `TBD.material.*`, `Construction.materialWidth[i]`; side per `UpdateReversed` | `StandardOpaqueMaterial` → `Material`; `Material:AirGap` R = 1/h | layer by layer, per zone side | 1e-5 | 1e-6 (unit) | `MA/Modify/UpdateMaterial.cs:167-240`; `MA/Modify/UpdateReversed.cs:25`; `OS/Material.cs:77-118,221-259` | missing material → EXPORT-DEFECT |
| 3b | Derived U, opaque (W/m²·K) | recompute from SAM layers with films F-OPQ | — (`GetUValue()` informational) | — (E+ report informational) | independent recompute from native layers vs from SAM layers | 1 % | 0.005 | `MA/Query/ThermalTransmittance.cs` | engine films differ by design |
| 3c | Glazing layers (τ, ρ, ε –; gas m); SimpleGlazing U (W/m²·K), g (–); derived U, g | pane layers, or `ThermalTransmittance` + `TotalSolarEnergyTransmittance` | transparent `TBD.material.*` | `StandardGlazing` / `Gas`; `SimpleGlazing` → `WindowMaterial:SimpleGlazingSystem`; `WindowProperty:FrameAndDivider` | layers identity; SimpleGlazing direct; derived U with F-GLZ-U, derived g with F-GLZ-g (informational) | 1e-5; SG 0.1 %; derived 1 % | 1e-6; 0.005; 0.001 | `OS/Construction.cs:59-172`; `SA/Classes/Glazing.cs:179-186` | — |
| 4a | Gain density: occupancy S/L, lighting, equipment S/L (W/m²); people (p/m²) | raw parameters per §3 | `ticOSG/ticOLG/ticLG/ticESG/ticELG` `profile.factor` | `People` / `Lights` / `ElectricEquipment` definitions (+ latent instance) | density identity | 0.1 % | 0.001 W/m²; 1e-5 p/m² | `MA/Modify/UpdateInternalCondition.cs:94-271`; `OS/InternalCondition.cs:71-190` | — |
| 4b | Zone gain total (W) | SAM total | density × `zone.floorArea` | density × geometric floor | vs SAM | 0.5 % | 0.1 W | as 4a | area basis reported |
| 4c | Radiant fraction of the sensible gain (–): occupancy, lighting, equipment | authored `OccupancyRadiantProportion`, `LightingRadiantProportion` (p: total radiant share of the room gain, §5.1), `EquipmentRadiantProportion` | `occupantRadProp`, `lightingRadProp`, `equipmentRadProp` | `People` / `ElectricEquipment` `Fraction Radiant`; `Lights` per T-LIGHT-SPLIT (`Return Air Fraction`, `Fraction Radiant`, `Fraction Visible`) | identity; lighting per T-LIGHT-SPLIT | — | 0.001 | `OS/InternalCondition.cs:81-160`; §5.1 | absent → DOCUMENTED-DEFAULT (lighting: the §5.1 common default); lighting `Return Air Fraction` ≠ 0 → EXPORT-DEFECT; view coefficients are not compared (§5.1) |
| 5 | Setpoints (°C); humidistat (%RH) | heating/cooling/humidity profiles | `Thermostat` `ticLL/ticUL/ticHLL/ticHUL` | `ThermostatSetpointDualSetpoint`; `ZoneControlHumidistat` | hourly (alignment per §7) | — | 0.01 K; 0.1 %RH | `MA/Modify/UpdateInternalCondition.cs:338-390`; `OS/Thermostat.cs:21-77` | OS thermostat dropped if heating > cooling → EXPORT-DEFECT |
| 6 | Schedule hourly value; EFLH (h) | `Profile` → 8760 | value / hourly / yearly profile | `ScheduleFixedInterval` | per hour after alignment; EFLH summary | EFLH 0.1 % | 1e-4 (fraction); 0.01 K | `MA/Modify/Update.cs:16-81`; `OS/Profile.cs:26-307` | Tas weekday sub-profiles → UNSUPPORTED-MAPPING |
| 7a | Infiltration (1/h) | ACH + profile | `ticI` factor | `ZoneInfiltration:DesignFlowRate` | identity | 0.1 % | 1e-4 1/h | `MA/Modify/UpdateInternalCondition.cs:149-158`; `OS/InternalCondition.cs:192-259` | Tas skips without a profile → EXPORT-DEFECT |
| 7b | Infiltration flow (m³/s) | ACH × geometric V / 3600 | ACH × `zone.volume` / 3600 | ACH × geometric V / 3600 | vs SAM | 0.5 % | 1e-5 m³/s | as 7a | D-F9 |
| 8a | Ventilation **requirement**: four bases (m³/s; m³/s·m²; m³/s·p; 1/h) | raw parameters | carrier `[SAM_META_V1]` (crossed only); `freshAirRate` (info) | DSOA rate fields | per D-F2 | 0.1 % | 1e-6 (unit) | `MA/Classes/SAMZoneMetadata.cs:45-94`; `OS/InternalCondition.cs:261-293` | carrier ≠ delivery |
| 8b | **Delivered** thermal outdoor air (m³/s, hourly) | per D-F2 state | `ticV` ACH × `zone.volume` / 3600 × profile | DSOA × fraction schedule via Ideal Loads *(translator)* | per D-F2 | 0.5 % | 1e-5 m³/s | `MA/Modify/UpdateInternalCondition.cs:273-336` | `ticV` ACH uses the stored V |
| 8c | Exhaust (m³/s) | `Exhaust*` | not mapped | diagnostic only | — | — | — | `OS/InternalCondition.cs:295-360` | UNSUPPORTED in both; no balance or thermal assumption |
| 8d | Window opening: Cd, factor (–), function string, schedule | `OpeningProperties` | `ApertureType.dischargeCoefficient`, profile | not mapped by the converter | per D-F8 | 0.1 % | 0.001; string exact | `MA/Modify/SetApertureType.cs:259-313`; `OS/Aperture.cs:290-296` | OS converter limitation |
| 8e | IZAM / TPD delivery; Air-panel mixing | AHU / IZAM data; `AirBoundaryAirChangesPerHour` | IZAMs (`MA/Modify/UpdateIZAMs.cs`) | `ConstructionAirBoundary` | **outside S07**; recorded | — | — | `OS/AnalyticalModel.cs:474-489` | equivalence fixtures carry none (D-FIX) |
| 9 | Weather: DB, dew point (°C); RH (%); global, diffuse (W/m²); wind (m/s); direction (°); cloud (–); lat/lon (°), elevation (m), time zone (h) | effective weather (§3) | `GetWeatherYear` + `SetAnnualParameter` 0..6 | EPW (+ S03 hash) | Tas float32 identity; OS at EPW precision | Tas 1e-5 | OS: 0.05 °C; 0.5 %; 0.5 W/m²; 0.05 m/s; 0.5°; 0.5 tenths; 1e-4°; 0.5 m. DNI informational | `SAM_Tas/SAM.Weather.Tas/Modify/UpdateWeatherData.cs:51-106`; `OS/Weather.cs:40-272` | Tas has no file hash; ground temperature not gated |
| 10 | Design days | effective source (§3 roles) | `TBD.DesignDay`: name, `yearDay`, `repetitions`, 24 h `GetWeatherDay()` | `SizingPeriod:DesignDay` (T-DD) or DDY clone | **Tas: hourly rule. OS: parametric rule. Not cross-compared.** | Tas 1e-5 | T-DD table | `MA/Modify/AddDesignDays.cs:43-134`; `OS/DesignDays.cs:105-302` | OS hourly reconstruction out of scope |

### Film convention (derived U and g)
SAM has no U film constants in code; the only SAM film constants are the EN 410 glazing ones in `SA/Classes/Glazing.cs:179-186`. Approved by the owner, 2026-10-20 (S07 plan revision 3):
- **F-OPQ** (ISO 6946:2017, Table 7):
  - Rse = 0.04 m²K/W.
  - Rsi = 0.13 (horizontal heat flow: walls).
  - Rsi = 0.10 (upward: roofs, ceilings).
  - Rsi = 0.17 (downward: floors).
  - Internal elements: Rsi on both sides.
  - Ground and adiabatic elements: layer resistance plus Rsi only; not gated.
- **F-GLZ-U** (EN 673): hi = 7.7, he = 25 W/m²K (Rsi 0.13, Rse 0.04).
- **F-GLZ-g** (EN 410, as `SA/Classes/Glazing.cs`): he = 23, hi = 8 W/m²K.

Engine-reported U and g remain informational because each engine uses its own films.

### Accepted transformations (executable; recomputed independently per §3)
- **T-DD** (OS embedded design day, `OS/DesignDays.cs:204-302`), from the SAM day's 24 hours:

  | Parameter | Expected value | Tolerance |
  |---|---|---|
  | Name | sanitised SAM name | exact |
  | Month, day | SAM date | exact |
  | DayType | `WinterDesignDay` / `SummerDesignDay` by list | exact |
  | Max DB | max DB | 0.01 K |
  | Range | max − min | 0.01 K |
  | Humidity | `DewPoint`, Magnus a = 17.625, b = 243.04 at the max-DB hour's RH | 0.01 K |
  | Pressure | mean if within 10 % of p_std = 101325·(1 − 2.25577e-5·z)^5.2559, else p_std; p_std when absent | 1 Pa |
  | Wind | mean speed | 0.01 m/s |
  | Direction | circular mean | 0.5° |
  | Solar model | `ASHRAEClearSky` | exact |
  | Clearness | 0 heating / 1 cooling | exact |

  **DDY source:** S03 file hash; names and parameters identical to the DDY object.
- **T-FRAME:**
  - OS: subsurface = pane polygon; frame width = `DefaultFrameWidth`, else frame thickness; conductance = 1/Σ(t/λ) (`OS/Aperture.cs:100-130,153-283`).
  - Tas, **writer behaviour (verified in code at the pins):**
    - **Frame percentage:** `FramePercent = aperture.GetFrameFactor() × 100` when `GetFrameFactor()` is not NaN, otherwise NaN (`MA/Query/T3DImportPlan.cs:743-748`).
      - It is rounded to one decimal only when `ToT3DOptions.SharedWindowTypes = true`. That option defaults to false (`MA/Classes/ToT3DOptions.cs:33-52`), and the Direct route sets only `UseWidths` (`MA/Classes/WorkflowCalculator.cs:613`). On the contract route the percentage is therefore not rounded.
      - `GetFrameFactor()` (`SA/Classes/Aperture.cs:262-281`) returns 0 when the frame area is 0 or NaN, 1 when the pane area is 0 or NaN, and otherwise A_frame / (A_frame + A_pane). The areas are net face areas (outer edge minus holes, `SAM/SAM.Geometry/Geometry/Base/Classes/Face.cs:135-148`) of the parts built by `Aperture.GetFace3Ds` (`SA/Classes/Aperture.cs:119-198`):
        - no frame construction layers: the pane is the whole aperture face and there is no frame (L133-145);
        - authored holes: the frame is the face with holes, and the panes are the holes (L147, L185-193);
        - no holes and a frame thickness > 0: the panes are the face offset inwards by `DefaultFrameWidth` when it is present, non-zero and not NaN, otherwise by the frame thickness (L150-182). If the offset gives no pane, or the frame thickness is 0 or NaN, there is no frame.
      - It never returns NaN at the SAM pin. So on the contract route `framePerc` is always written and `isPercFrame` is always true, including 0 % for an aperture without frame layers. The writer's NaN branch is not reachable from the SAM pin.
    - **Frame width:** with an `ApertureConstruction`, `DefaultFrameWidth` is assigned first when present and not NaN; `GetFrameThickness()` then overrides it when not NaN (`MA/Query/T3DImportPlan.cs:836-847`). `GetFrameThickness()` is the sum of the frame layer thicknesses, and 0 when there are no frame layers (`SA/Classes/ApertureConstruction.cs:143-157,180-183`). `DefaultFrameWidth` therefore reaches the writer only when the frame thickness is NaN. With no `ApertureConstruction` the spec keeps its NaN default (`MA/Classes/T3DImportPlan.cs:109,112`).
    - **Write:** `window.frameWidth` only when the spec width is not NaN; `window.isPercFrame = true` and `window.framePerc` only when the spec percentage is not NaN (`MA/Convert/ToT3D/T3DDocument.cs:306-314`).
  - Tas, **expected values (computed per §3 from raw SAM data):** the frame percentage is 100 × A_frame / (A_frame + A_pane) from the aperture geometry by the rule above, with 0 and 100 at its edge cases; `Aperture.GetFrameFactor()` is a cross-check only. The written frame width is the frame layer thickness sum when not NaN, else `DefaultFrameWidth`, else none.
  - Tas, **native interpretation (UNVERIFIED until S08 read-back; rank 4 while readable and unresolved):**
    1. whether TAS makes the frame zone surface `framePerc` % of the opening polygon area (the opening polygon is the aperture's outer edge, `MA/Query/T3DImportPlan.cs:693`) and the pane surface the rest. S08 compares both TBD surface areas, by identity, with A_frame and A_pane (row 1b);
    2. whether `frameWidth` has any geometric or thermal effect when `isPercFrame = true`;
    3. what TAS builds for `framePerc` = 0 (no frame surface, or one with zero area) and for 100;
    4. whether `isPercFrame`, `framePerc` and `frameWidth` can be read back from the TBD, or only from the T3D.
  - Tolerance: 0.5 % area; 1e-5 conductance.
- **T-OMIT0:** OS omits a gain when the SAM total ≤ 0. Verified only when no object of that type exists for the zone.
- **T-OA-SYS:** OS Ideal-Loads outdoor air (system load) versus Tas `ticV` (zone-entering air). Only **flow-rate** equality is accepted.
- **T-LIGHT-SPLIT** (amendment L1; OS `Lights`). The symbols are defined in §5.1: p is the SAM `LightingRadiantProportion`, or p₀ when absent; s = 0.2415 / 0.576 at full precision.

  | Native quantity | Expected value | Tolerance |
  |---|---|---|
  | `Return Air Fraction` F_RA (blank reads as 0) | 0 | exact |
  | `Fraction Radiant` + `Fraction Visible` | p | 0.001 |
  | `Fraction Visible` F_V | p·s | 0.001 |
  | `Fraction Radiant` F_R | p·(1 − s) | 0.001 |
  | convective 1 − F_RA − F_R − F_V | 1 − p (Tas: 1 − `lightingRadProp`) | 0.001 |

  - When verified, the outcome is ACCEPTED-TRANSFORMATION for conformance to the declared input convention of §5.1.
  - It establishes no engine-physics equivalence (see "Scope" in §5.1).
  - The Tas side of row 4c stays identity on `lightingRadProp`.

### 5.1 Lighting energy split and view coefficients (amendment L1, approved by the owner 2026-10-21)

These quantities have different physical meanings. None of them is substituted for another.

| Symbol | Meaning | Denominator | Fields | Source |
|---|---|---|---|---|
| p | Total radiant share (long-wave plus short-wave) of the lighting gain entering the room. Tas injects the convective remainder into the zone air and distributes the radiant part among the zone surfaces. | lighting gain entering the room | SAM `LightingRadiantProportion`; Tas `lightingRadProp` | [EDSL, Internal Conditions](https://docs.edsl.net/tbd/CoreConcepts/Internal%20Conditions.html) (Radiant Proportion); [EDSL help, Specifying the Internal Conditions](https://www.edsl.net/htmlhelp/Building_Simulator/Modelling_Internal_Conditions.htm) |
| s | Short-wave share of the emitter's radiant output. EDSL publishes tungsten 0.111 and fluorescent 0.313 and uses s only in the comfort spectrum factor. | radiant output | none: no SAM or TBD field; a declared contract constant here | EDSL, Internal Conditions (Ceiling/Floor Emitters) |
| F_RA, F_R, F_V, F_C | Return air; long-wave, absorbed by surface area × thermal absorptance; short-wave (visible), combined with diffuse solar and absorbed by area × solar absorptance, with the glazing-transmitted part leaving the zone; convective, F_C = 1 − F_RA − F_R − F_V. A sum above 1 is an input error. | total lighting electric input | EnergyPlus `Lights` | [EnergyPlus 25.1 I/O Reference, Lights](https://bigladdersoftware.com/epx/docs/25-1/input-output-reference/group-internal-gains-people-lights-other.html); [Engineering Reference, Zone Internal Gains](https://bigladdersoftware.com/epx/docs/25-1/engineering-reference/zone-internal-gains.html) |
| C | View coefficient: how much the emitter's radiation contributes to the occupants' perceived mean radiant and resultant temperature. Not an energy fraction. | – | SAM `LightingViewCoefficient`; Tas `lightingViewCoefficient`; no EnergyPlus input | EDSL, Internal Conditions (View Coefficient); [EDSL help, Calculation of View Coefficients](https://www.edsl.net/htmlhelp/Building_Simulator/GuidetotheCalculationofViewCoefficients.htm) |

**Convention.** SAM and Tas have no luminaire-return allocation, so:
- F_RA = 0
- F_V = p·s
- F_R = p·(1 − s)
- F_C = 1 − p

s is a declared assumption, not SAM data, and it is not derivable from lm/W.

Arithmetic check only (not a default): the EDSL fluorescent values p 0.48, s 0.313 give F_V 0.15024, F_R 0.32976 and
F_C 0.52.

**Reference category and baseline.**
- **Category:** suspended LED office lighting. The whole gain enters the conditioned room, with no separate
  luminaire-return allocation.
- **Baseline:**
  - p₀ = 0.576;
  - s = 0.2415 / 0.576 (0.4192708…). This is retained at full calculation precision and never rounded.
  - The fractions of lighting electric input are F_R 0.3345, F_V 0.2415, F_C 0.424, F_RA 0.

**Derivation: a midpoint-derived reference from cited historical ranges.**

| Source | Content used | Kind |
|---|---|---|
| IEA SHC Task 61 / EBC Annex 77, Technical Report D1 "Literature Review" (eds. Gentile and Osterhaus, October 2021, doi [10.18777/ieashc-task61-2021-0013](https://doi.org/10.18777/ieashc-task61-2021-0013)), Table 6, printed p. 59 | Linear pendant LED (min/max), as % of lighting power: conditioned-space fraction 100; long-wave 31.5–35.4; short-wave 19.3–29.0; total radiative 54.7–60.5; total convective 39.5–45.3. Rated efficacy 86 lm/W. | secondary reproduction of a published range |
| Liu, Zhou, Lochhead, Zhong, Huynh and Maxwell (2017), "Low-energy LED lighting heat gain distribution in buildings, part II: LED luminaire selection and test results", *Science and Technology for the Built Environment* 23(4), 688–708, doi [10.1080/23744731.2016.1250563](https://doi.org/10.1080/23744731.2016.1250563) (ASHRAE RP-1681) | the original study that Table 6 cites | **not read**: paywalled |
| ASHRAE Handbook—Fundamentals 2017 (SI), Ch. 18, Table 3 | Linear pendant LED fixture: space fraction 1.0, radiative fraction 0.55–0.60, with the note to use middle values in most cases. The table cites Zhou et al. (2016). | published range; cross-check of p₀ |

- **Per-component midpoints:**
  - long-wave (31.5 + 35.4) / 2 = 33.45 %;
  - short-wave (19.3 + 29.0) / 2 = 24.15 %;
  - their sum, 57.6 %, equals the midpoint of the total-radiative range;
  - convective 42.4 %.
- p₀ lies inside the ASHRAE range. No per-fitting pair is published in the sources read, and none is constructed
  here.
- **Limitations:**
  - The values rest on a secondary reproduction.
  - The original study's per-fitting values and uncertainties are not verified. The paper is paywalled, and the
    related M.S. thesis (Zhong, Iowa State University, 2016) was not accessible.
  - The baseline is not universal, not manufacturer-certified and not current-market typical. It derives from
    luminaires tested about 2016 (Table 6: 86 lm/W).
- **Applicability:**
  - Suspended (pendant) LED with the whole fitting in the conditioned room.
  - ASHRAE Table 3 conditions: about 5 L/(s·m²) supply air at 15–16.7 °C, room air at 22–24 °C, 9.7–28 W/m².
    ASHRAE advises the lower bounds at higher power densities.
- **Not applicable to:**
  - recessed troffers, downlights and retrofit kits. Their space fraction is below 1 in ASHRAE Table 3, and that
    luminaire-return share cannot be represented in SAM;
  - high-bays;
  - non-LED lamps.

**Default resolution, common to both engines.**
1. **Authored p, including 0, is preserved.** Tas receives `lightingRadProp = p`; OS receives T-LIGHT-SPLIT. A NaN
   is treated as absent.
2. **Absent p resolves to p₀ in both writers.**
   - Each writer emits an information diagnostic per [DIAGNOSTICS_STANDARD](../docs/DIAGNOSTICS_STANDARD.md) that
     names p₀, s, the reference category and this section.
   - Neither engine inherits a template value silently.
   - This is a common case: the SAM internal-condition libraries (`files/resources/Analytical/SAM_InternalConditionLibrary.JSON`,
     `SAM_InternalConditionLibrary_TM59.JSON`) store no radiant proportion.
3. **Runtime applicability and spectral assumptions stay visible.** Whenever p₀ or s is applied, the run diagnostics
   and the S08/S09 reports state:
   - the reference category;
   - the values;
   - the source;
   - the applicability limits.
4. **Outcomes under §6:**
   - An absent p with the native at p₀ (OS: F_R and F_V by s) is DOCUMENTED-DEFAULT, accepted per quantity and
     fixture.
   - Until the SAM_Tas change below lands, Tas keeps the TBD value for an absent p. That is "other non-zero", so it
     is EXPORT-DEFECT.
   - The committed S08/S09 equivalence fixtures author p and are not affected.
5. **Imports (OS → SAM, Ladybug → SAM):**
   - p ← F_R + F_V, and the view coefficient is not set.
   - An information diagnostic is raised when the imported F_V / (F_R + F_V) differs from s.
   - A warning is raised when F_RA > 0: that share becomes a convective room gain.
   - SAM files imported under the previous mapping hold F_R in p and F_V in the view coefficient. They cannot be
     identified and are not migrated.
6. **Unchanged:** `SA/Create/InternalCondition.cs:81,85` writes p = 0.3 and C = 0.49 as authored values. They are
   preserved in both engines; changing them is a separate decision.

**Tas values observed (labelled; factory provenance not proven).**
- All 296 internal gains of the NCM v6.1.b template carry radiant proportion 0.30 and view coefficient 0.49. The
  template is SAM_Tas repository root, `files/resources/Analytical/Tas/NCMActivities_v6.1.b (Part L 2021).tic`,
  sha256 `509fc55d5064…`; it was decoded read-only.
- One machine-local gain created through Tas 9.5.7 COM shows the same values.
- EDSL documents no source for 0.30. Its documented typical lighting values are fluorescent 0.48 and tungsten 0.80.

**View coefficient (separate quantity).**
- C enters no energy fraction and is never substituted for one. It has no OS input.
- It is outside S07 input equivalence: it is not compared and not gated (§6.1). The OS writer reports an authored C
  with SAM-OS-IC-001 information.
- **EDSL tabulation:** for a 5 × 5 m room with observer-to-emitter-plane distance e = 1.3 m, fluorescent ceiling
  lighting has C = 0.490 (formula 6.4-1), with spectrum factor F_s = (α/ε)·s + 1 − s = 0.943 at s = 0.313 and
  α/ε = 0.8 / 0.98. The 0.49 above is therefore a fluorescent ceiling-lighting value.
- **Unresolved: the LED comfort coefficient.**
  - At the baseline s, F_s = 0.923.
  - A pendant lies below the ceiling plane, so the tabulated geometry does not hold, and formula 6.4-1 needs the
    actual room and mounting geometry.
  - No LED value is defined. An authored C passes to Tas; an absent C keeps the TBD value.
- Its effect through a Tas thermostat that controls on resultant temperature is UNVERIFIED. SAM writes no thermostat
  radiant proportion.

**Scope.** T-LIGHT-SPLIT verifies conformance to this declared input convention only.
- The engines distribute the radiant part differently:
  - EnergyPlus distributes F_V by solar absorptance, including glazing transmission, and F_R by thermal absorptance;
  - Tas's distribution is UNVERIFIED.
- Matching fractions are not evidence of identical surface absorption or zone response.
- S10/S11 validate result round trips; they do not establish engine-physics equivalence.

**Required downstream changes (not implemented by this amendment).** Every one preserves authored inputs and keeps
the applied values visible (default-resolution item 3).

- **SAM_OpenStudio#33** (continue the existing PR, branch `feature/s09-openstudio-input-readback`):
  - **Writer lighting block** (`OS/InternalCondition.cs:125-136`):
    - F_R = p·(1 − s), F_V = p·s; leave F_RA unset (0);
    - absent p → p₀ with the diagnostic;
    - never read C for `Lights`;
    - report C with SAM-OS-IC-001, next to the occupancy and equipment messages (L314-327). Correct their
      "no visible-fraction field" reason at the same time.
  - **Importer:** SAM_OpenStudio repository root, `SAM_OpenStudio/SAM.Analytical.OpenStudio/Convert/ToSAM/InternalCondition.cs:236-237`,
    per default-resolution item 5.
  - The mapping and coverage documents and their manifests.
  - **S09 read-back:**
    - rows for F_RA, F_R + F_V, F_V, F_R and the convective remainder, with expected values from raw SAM p and the
      §5.1 constants only;
    - remove the view-coefficient `lighting.visible` comparison and its DocumentedDefault exception.
  - **Expected vectors** (tolerance 0.001):

    | p | F_V | F_R | F_C |
    |---|---|---|---|
    | 0.37 | 0.155130 | 0.214870 | 0.63 |
    | 0.34 | 0.142552 | 0.197448 | 0.66 |
    | 0.41 | 0.171901 | 0.238099 | 0.59 |
    | p₀ | 0.2415 | 0.3345 | 0.424 |

  - **Negatives:**
    - F_V = 0.25 with p authored (the previous behaviour);
    - F_V = 0;
    - F_R = p with F_V > 0;
    - F_RA = 0.1;
    - F_R and F_V swapped.

    A copy with C authored must leave every lighting row unchanged.
- **SAM_Tas**, a separate PR needing its own owner write approval:
  - absent-p fallback to p₀, with the diagnostic and NaN treated as absent, in `MA/Modify/UpdateInternalCondition.cs:96-99`
    (no NaN guard at the pin) and `MA/Modify/UpdateInternalConditionTemplate.cs:78-81`;
  - this changes Tas production results for gains without an authored p (0.30 → 0.576);
  - no change to C or to an authored p.
- **SAM_LadybugTools**, a separate task: the same convention for `LB/ToLadybugTools/ProgramType.cs:110-118` and
  `LB/ToSAM/InternalCondition.cs:59-60`.

---

## 6. Input states, outcomes and gate
| SAM state | Definition | Native → outcome |
|---|---|---|
| S-ZERO | present, = 0 | 0 → MATCH; omitted under a listed T-* → ACCEPTED-TRANSFORMATION; otherwise → EXPORT-DEFECT |
| S-UNSPEC | absent / NaN, no SAM derivation | none → NOT-APPLICABLE; contract-listed default → DOCUMENTED-DEFAULT; other non-zero → EXPORT-DEFECT |
| S-INVALID | out of range, or dependent value NaN | not compared → INPUT-INVALID |
| S-VALUE | valid non-zero | compared; missing native → EXPORT-DEFECT, unless listed UNSUPPORTED-MAPPING or MAPPING-DIFFERENCE |

Absence is never compared numerically, and `abs` never turns a missing or zeroed native object into a pass.
**MAPPING-DIFFERENCE** is a known converter behaviour that conflicts with the stated SAM intent (D-F2 V1 in OS). It is reported and gated like UNSUPPORTED-MAPPING.

**Gate per engine per fixture, in the roadmap vocabulary.** Every applicable quantity must be resolved. The highest-ranked trigger decides the status. The report lists the count per outcome and every non-PASS quantity, so coexisting items stay visible.

| Rank | Roadmap status | Trigger (any applicable quantity) |
|---|---|---|
| 1 | **FAIL** | EXPORT-DEFECT; out of tolerance without a contract explanation |
| 2 | **BLOCKED** (technical blocker named) | INPUT-INVALID; UNVERIFIED that cannot be read (licence, executable or field unavailable) |
| 3 | **BLOCKED** (owner decision named) | unaccepted UNSUPPORTED-MAPPING or MAPPING-DIFFERENCE; **unaccepted** DOCUMENTED-DEFAULT on a gated quantity; MODEL-INTERPRETATION-REQUIRED not recorded |
| 4 | **NOT RUN** (quantities named) | UNVERIFIED that is readable but not yet resolved, including §7 schedule alignment |
| 5 | **Not PASS — accepted limitation** (items named) | only owner-accepted UNSUPPORTED-MAPPING / MAPPING-DIFFERENCE / DOCUMENTED-DEFAULT remain. An owner-accepted DOCUMENTED-DEFAULT belongs here, not in rank 3. Accepted per quantity and per fixture, as with S06. **Never PASS.** |
| 6 | **PASS** | all MATCH / WITHIN-TOLERANCE / verified ACCEPTED-TRANSFORMATION / NOT-APPLICABLE (proven SAM absence) |

"INCOMPLETE" in revision 2 is replaced by ranks 3–4.

PASS stays a strict allow-list. An unsupported, unresolved or unverified item, and an owner-accepted limitation, never gives PASS.

### 6.1 Gate completeness: proposed fail-closed handling (for owner review; not approved)

Three cases are not decided by the approved rules above. None of them can reach PASS, but their status is not stated. The handling below is a **draft for the owner**. It accepts no limitation, adds no PASS route and changes no tolerance.

**Gated quantity (proposed definition, needed by rank 3 and G2):** every §5 quantity with a tolerance, **except** the ones §5 or the film convention marks as informational, not gated, diagnostic only or outside S07: engine-reported U and g (3b, 3c); derived g (3c); DNI and ground temperature (9); ground and adiabatic element U (film convention F-OPQ); exhaust (8c); IZAM/TPD delivery and air-panel mixing (8e); the lighting, occupancy and equipment view coefficients (§5.1, amendment L1). Stored floor area and volume are governed by D-F9, not by this definition.

| Case | Situation | Proposed outcome | Resulting rank |
|---|---|---|---|
| **G1** | S-UNSPEC; native object present with value 0 | DOCUMENTED-DEFAULT when 0 is the contract-listed default for that quantity (e.g. the OS equipment radiant-fraction fallback 0, `OS/InternalCondition.cs:155-157`). Otherwise **UNLISTED-DEFAULT**, handled as an unaccepted DOCUMENTED-DEFAULT. Never NOT-APPLICABLE (that needs native absence) and never MATCH: zero does not mean absence for every quantity (a 0 °C setpoint is not "no setpoint"). | as DOCUMENTED-DEFAULT: rank 3 when gated and unaccepted; rank 5 when accepted; G2 when not gated |
| **G1-NaN** | S-UNSPEC; native object present with value NaN | EXPORT-DEFECT, as G3 | 1 (FAIL) |
| **G2** | Unaccepted DOCUMENTED-DEFAULT on a quantity that is not gated | Reported by name; the fixture is BLOCKED until the owner accepts it (then rank 5) | 3 (owner decision named) |
| **G3** | S-VALUE; native field present and read as NaN | EXPORT-DEFECT: NaN is not a comparable value, and neither `abs` nor `rel` can pass it. A listed UNSUPPORTED-MAPPING or MAPPING-DIFFERENCE does not cover it, because those describe a missing or a known different value, not an invalid one. | 1 (FAIL) |

S-ZERO with a native NaN is already EXPORT-DEFECT under "otherwise". **Reader precondition (G1-NaN, G3):** the S08/S09 readers report "field unreadable" separately from "read as NaN". An unreadable field is UNVERIFIED (rank 2 or 4), never a NaN value.

**Until the owner decides:** a fixture showing any G case reports the proposed outcome, labelled "proposed", and its status is the higher-ranked of (a) the status from the approved rules and (b) BLOCKED, with the owner decision named as the open G case. It is never PASS.

**New owner decisions (open):**
- **OD-G1:** approve G1 and the UNLISTED-DEFAULT outcome, or choose another fail-closed handling.
- **OD-G2:** approve the gated-quantity definition and G2. Alternative: allow a non-gated quantity's default to be accepted once for all fixtures, instead of per quantity and per fixture as rank 5 requires.
- **OD-G3:** approve G1-NaN, G3 and the reader precondition. Not proposed: treating a native field that S08/S09 prove uses NaN for "not set" as native absence for S-UNSPEC. That would need per-field read-back evidence and its own owner approval.

## 7. Schedule time alignment
The Tas hour labels (`hourlyValues[1]`, yearly index 1), the first OS fixed-interval value and leap-year handling are UNVERIFIED. Until S08/S09 verify them by read-back:
- hourly-aligned quantities (5, 6, 7a profile, 8b) are only "equal under assumed alignment";
- the fixture is at most **NOT RUN** for alignment;
- an EFLH match is supporting evidence only.

---

## 8. Owner decisions

D-ROUTE, D-F2 (V0/V1/V2), D-F8, D-F9 (A + C) and D-FIX below: approved by the owner, 2026-10-20 (S07 plan revision 3). The Recommendation column records the approved choice. D-LIGHT: approved by the owner, 2026-10-21 (amendment L1). Open: OD-G1 to OD-G3 (§6.1) and OD-W1, OD-W2 (§4.1).

| ID | Alternatives / content | Recommendation |
|---|---|---|
| **D-ROUTE** | Contract = Direct T3D with `UseWidths=false` (§1). Production (SAM_UI, the Both producer) stays gbXML until a separately approved integration. S08 either uses a harness selecting Direct, or waits for that integration. Later the same day the owner approved Direct as the **future** production default, first with `UseWidths=false` and then with `UseWidths=true` as the owner-preferred candidate. The candidate is subject to the §4.1 verification, and `UseWidths=false` stays an explicit option (§1). It is implemented only by that separate integration PR, against the criteria in roadmap owner decision 7. The contract route stays `UseWidths=false`. | Approve the contract route; production switch out of S07 |
| **D-F2 ventilation** | **Requirement and delivery are separate quantities.** Requirements are preserved for MEP sizing and later IZAM/TPD work, and never activate thermal ventilation by themselves. States:<br>**V0** no requirement.<br>**V1** requirement only, no delivery selected: Tas no `ticV` = MATCH with intent; OS unscheduled DSOA delivery = **MAPPING-DIFFERENCE** (reported; any OS change is a separate decision with compatibility consequences).<br>**V2** Building Simulator delivery **explicitly selected** (Ventilation profile): supported in both, compared on 8b with T-OA-SYS. The selection, not the building type, defines V2.<br>IZAM/TPD delivery is outside S07. Exhaust is UNSUPPORTED in both, with no balance or thermal assumption.<br>Rejected: P1 (missing profile = constant delivery; reverses the Tas design and test); P2 (missing profile = invalid; contradicts V1). | **V0/V1/V2 as stated.** Recorded, not fixed: `ticV` + AHU IZAM `fromOutside` on the same zone; space DSOA replacing the SpaceType DSOA; the Both producer's `AddIZAMs = true`. |
| **D-F8 window opening** | Tas read-back of 8d. OS: converter limitation (EnergyPlus supports opening airflow via AirflowNetwork; the SAM converter does not map `OpeningProperties`), reported. Common equivalence fixtures are closed-window (no `OpeningProperties`). No natural-ventilation implementation. | **As stated** |
| **D-F9 volume** | A: engine reference = §4 geometric; stored reported. C: fixtures have stored ≈ geometric within 0.5 %, or a recorded interpretation (convention, intended volume). The difference is MODEL-INTERPRETATION-REQUIRED (rank 3 until recorded), not a defect. A recorded interpretation **never waives** a native-vs-geometric FAIL; it only fixes how 7b and 8b are reported. | **A + C** |
| **D-FIX fixtures** | Roadmap box + multizone requirement **retained**. The committed two-cell model has non-zero supply and exhaust and no Ventilation profile: it is kept as **divergence evidence** (V1 MAPPING-DIFFERENCE; exhaust unsupported), not as an equivalence fixture. The box and HungaryHouse are machine-local. | The owner selects or clears both equivalence fixtures before the S08/S09 gate, against the criteria below. S07 creates none. |
| **D-LIGHT lighting split** (amendment L1) | The revision 1-3 identity `LightingViewCoefficient` ↔ `Fraction Visible` is withdrawn: the two quantities differ physically (§5.1). SAM `LightingRadiantProportion` is the total radiant share p, split for OS by s under T-LIGHT-SPLIT, so the convective remainder is 1 − p in both engines. The reference category is suspended LED office lighting with the whole gain entering the room. Baseline: p₀ = 0.576, s = 0.2415 / 0.576 at full precision, giving F_R 0.3345, F_V 0.2415, F_C 0.424. It is a midpoint-derived reference from cited historical ranges, with the §5.1 limitations. Authored p, including 0, is preserved. One absent-value fallback (p₀) applies to both engines, with diagnostics and source and applicability records. The view coefficient is kept separate, and its LED value is unresolved. | **Approved 2026-10-21.** T-LIGHT-SPLIT may be ACCEPTED-TRANSFORMATION for conformance to the declared input convention; it does not establish engine-physics equivalence. Writer changes are downstream (§5.1): SAM_OpenStudio#33 and a separate, separately approved SAM_Tas PR. |

**Fixture criteria:**
- committed, with provenance and a hash;
- closed-window (no `OpeningProperties`);
- a declared ventilation state; no exhaust and no AHU/IZAM data unless the owner accepts them;
- stored ≈ geometric volume, or a recorded interpretation;
- a recorded panel convention;
- gated inputs authored (no DOCUMENTED-DEFAULT) unless accepted.

---

## Recorded in passing (not fixed)

Nothing below is changed by S07. Each item is evidence for a later, separately approved task.

- §2 item 7: the inverted-looking `IsNaN` guard at `MA/Modify/UpdateInternalCondition.cs:326,331`; `Profile.Count` = Max-Min
  (`SA/Classes/Profile.cs:1041-1057`); Tas zeroing per-area gains when Area is NaN or 0; Tas skipping infiltration without a
  profile; OS dropping holes (`OS/Panel.cs:68-108`); OS documentation drift; the "inner surfaces" wording for mid-surface
  panels.
- §8 D-F2: `ticV` plus an AHU IZAM `fromOutside` on the same zone; a space DSOA replacing the SpaceType DSOA; the Both
  producer's `AddIZAMs = true`.
- §6 completeness: the three gaps found while checking this document (S-UNSPEC with a native zero or NaN; an unaccepted
  DOCUMENTED-DEFAULT on a non-gated quantity; S-VALUE with a native NaN) now have proposed fail-closed handling in §6.1,
  awaiting owner decisions OD-G1 to OD-G3.
- T-FRAME (§5): the direct writer's frame width (the frame layer thickness, `DefaultFrameWidth` only when that is NaN;
  `MA/Query/T3DImportPlan.cs:836-847`) uses the opposite precedence to SAM's own frame geometry (`DefaultFrameWidth`
  first; `SA/Classes/Aperture.cs:150-168`). `Aperture.GetFace3Ds` reads `Type.FrameConstructionLayers` without a null
  check (`SA/Classes/Aperture.cs:133-134`).
- **Amendment L1 (lighting).** These items are recorded, not fixed:
  - The revision 1-3 row 4c identity `LightingViewCoefficient` ↔ `Fraction Visible` was a physical-meaning error.
    With the SAM code defaults (p 0.3, C 0.49) the pinned OS writer gives a convective share of 0.21, where Tas
    gives 0.70. With p ≥ 0.52 and C 0.49 the EnergyPlus fractions sum above 1.
  - SAM_LadybugTools carries the same mapping (`LB/ToLadybugTools/ProgramType.cs:110-118`,
    `LB/ToSAM/InternalCondition.cs:59-60`).
  - `SA/Create/InternalCondition.cs:86-87` sets `OccupancyViewCoefficient` twice (0.227, then 0.372) and never sets
    `EquipmentViewCoefficient`. This is a separate finding.
  - `MA/Modify/UpdateInternalCondition.cs:96-99` has no NaN guard for `LightingRadiantProportion`.

## PR record

Task S07 of [SIMULATION_ROADMAP_Q4_2026.md](SIMULATION_ROADMAP_Q4_2026.md). Documentation only; no behaviour change.

- **Status:** In review: [SAM#216](https://github.com/SAM-BIM/SAM/pull/216), base `sow/2026-Q4` (`6836595d`), branch
  `docs/s07-physical-input-mapping`. Revision 2 followed the independent review of head `c3327dcb`; revision 3 follows
  the review of head `c7461845`. Not merged; merge needs explicit owner approval. Validated content SHAs: revision 1
  `32d519b8ce07eba9cfc4d0bb2e547ad2211ae676`; revision 2 `03794f428680fb67af383a615c19d2d895a74b5a`; revision 3
  `dee6f7f07959adb42831c2cfaa1ecbb1ffb458a6` (the commit the local checks ran on; a record-only commit follows it). Final PR head and checks: in the PR description.
- **Work completed:**
  - revision 1: this document (plan revision 3, sections 1-8, owner choices recorded, evidence pins re-checked); the
    roadmap updated for the S07 row, decision items 3 and 5, and the contract-route note;
  - revision 2: §6 rank 3 limited to unaccepted DOCUMENTED-DEFAULT on gated quantities (accepted ones are rank 5);
    Tas T-FRAME written from the pinned writer and SAM source, with four bounded native uncertainties for S08; §6.1
    drafts fail-closed handling for the three gate gaps (OD-G1 to OD-G3); the owner's approval of Direct as the future
    production default recorded (§1, §8, roadmap decision 7) with acceptance criteria for its integration, including the
    owner's later revision to `UseWidths=true` as the preferred candidate; §4.1 adds the widths-ON reference that the
    candidate is verified against; the roadmap's stale closing line replaced;
  - revision 3: the widths measurements are no longer read as proof of the panel convention (§1, §4, §4.1, roadmap
    decision 7). They show how widths change native geometry; the fixture's provenance and declared convention decide
    whether offsets are intended. SAM's mid-surface `PanelsBySolid` output with `PhysicalThickness` is cited, and the
    measured figures keep their original single-model scope. The owner's fallback direction is recorded (if candidate
    validation fails, the proposed widths setting reverts to `false` and the reason is reported; no silent or automatic
    runtime fallback), and OD-W3 is removed.
- **Decisions:** D-ROUTE, D-F2 (V0/V1/V2), D-F8, D-F9 (A + C), D-FIX, the film convention and every tolerance are
  approved by the owner, 2026-10-20 (S07 plan revision 3); the owner can amend any of them during review. The owner also
  approved, 2026-10-20, Direct as the future Tas production default. The approval was first given with
  `UseWidths=false`, then revised to `UseWidths=true` as the owner-preferred candidate, pending independent verification
  against the intended geometry (§4.1) on the approved fixtures. `UseWidths=false` stays an explicit option. If
  validation fails, the proposed widths setting reverts to `false` with the reason reported. None of this is
  implemented: current production stays gbXML and the contract route stays `UseWidths=false`. OD-G1 to OD-G3, OD-W1 and
  OD-W2 are open; neither this revision nor passing CI approves them. The two equivalence fixtures are not yet selected
  (D-FIX).
- **Files changed:** `documentation/SIMULATION_INPUT_MAPPING_S07.md` (new), `documentation/SIMULATION_ROADMAP_Q4_2026.md`.
- **Checks (local documentation checks; no native-engine validation and no GitHub result implied):**
  - the seven `sow/2026-Q4` tips were re-read with `git ls-remote` on 2026-10-20 (for each revision) and equal the pins
    above;
  - every file citation in this document resolves at its pinned SHA and every line range lies inside its file; each
    native name used was found in the pinned code or is marked UNVERIFIED / *(translator)*. Reproduce one citation with
    `git -C <repo> show <sha>:<path> | sed -n '<a>,<b>p'`;
  - revision 2 semantic checks against the pinned code: `MA/Query/T3DImportPlan.cs:743-748,836-847`,
    `MA/Convert/ToT3D/T3DDocument.cs:306-314`, `MA/Classes/ToT3DOptions.cs:33-52`, `MA/Classes/T3DImportPlan.cs:109,112`,
    `MA/Classes/WorkflowCalculator.cs:613`, `SA/Classes/Aperture.cs:119-198,262-281`,
    `SA/Classes/ApertureConstruction.cs:143-157,180-183`, `SAM/SAM.Geometry/Geometry/Base/Classes/Face.cs:135-148`,
    `OS/InternalCondition.cs:155-157`; for §1 and §4.1, `MA/Classes/ToT3DOptions.cs:13-24`,
    `MA/Classes/WorkflowCalculator.cs:618-621,632-640` and `MA/DIRECT_T3D_ROUTE.md:122,131-136,258-286`; and, for roadmap
    decision 7, `MA/Classes/WorkflowSettings.cs:20-32,189-192,360` and
    `SAM_UI/SAM.Analytical.UI/Classes/TBDConversionOptions.cs:25,34,173,242`;
  - revision 3 semantic checks: `SA/Create/PanelsBySolid.cs:15-25,404`, `MA/Query/T3DImportPlan.cs:513-520,550-565,901-910`,
    `MA/DIRECT_T3D_ROUTE.md:133,284` and SAM_Tas `PROJECT_PROGRESS.md:44`;
  - revision 1 evidence corrections: the OpenStudio, Tas and SAM path prefixes; the benchmark producer, SAM_UI and
    `PROJECT_PROGRESS.md` paths; `freshAirRate` at `MA/Modify/UpdateInternalCondition.cs:141`; the older
    `ToTBD(AnalyticalModel, …)` chain `MA/Convert/ToTBD/Building.cs:47` -> `MA/Modify/Update.cs:612,623`;
    `GeometricalFloorPanels` at `SA/Query/FloorArea.cs:123`; bare file names expanded to prefixed paths;
  - §5 has all ten categories, each with SAM, Tas and OS fields, a rule, per-unit tolerances (or a stated reason) and
    evidence; no tolerance changed in revisions 2 or 3;
  - the §6 gate was checked as an allow-list: only MATCH, WITHIN-TOLERANCE, verified ACCEPTED-TRANSFORMATION or proven
    NOT-APPLICABLE give PASS; every §6.1 proposal ends at rank 1, 3 or 5, never PASS;
  - relative links resolve and every link targets `github.com/SAM-BIM/*`; S08-S12 read "Not started" and Phase C
    "Frozen" in the roadmap; the diff against the base touches only the two `documentation/*.md` files.
- **GitHub checks:** build, test, spdx: see the PR description for the final head.
- **Risks and open items:** OD-G1 to OD-G3 (§6.1); OD-W1 and OD-W2 (§4.1); the equivalence fixtures are not selected,
  and the widths-ON candidate can be verified only once each has a provenance-backed, declared panel convention (D-FIX,
  §4.1); the SAM_Tas "inner surfaces" wording that conflicts with mid-surface sources stays in its source documents (not
  changed here); the four Tas T-FRAME native uncertainties (§5) and schedule hour labels and leap-year handling (§7) stay
  UNVERIFIED until S08/S09 read-back; the Tas design-day reader may need a raw-field read (§3); the approved Direct
  default is not implemented.
- **Next step:** independent review of revision 3 and the owner's merge decision. After merge: the known-SHA
  `PROJECT_PROGRESS.md` closeout on `sow/2026-Q4` and guarded branch cleanup. S08 does not start automatically: it is
  Plan-first and needs an explicit owner start, a separate SAM_Tas write approval (roadmap decision 2) and
  owner-selected or cleared equivalence fixtures with provenance, hashes and the D-FIX criteria. The Direct default
  switch is a separate integration PR (roadmap decision 7), not part of S08; where its widths-ON verification runs is
  OD-W1.

## Amendment L1 PR record (lighting split)

The "PR record" above is the historical record of SAM#216, which merged as `68b60257` (closeout in `PROJECT_PROGRESS.md`).
This section records the documentation-only amendment.

- **Status:** draft, for owner review: [SAM#217](https://github.com/SAM-BIM/SAM/pull/217). Base `sow/2026-Q4` (`ee377167`),
  branch `docs/s07-lighting-split`. Not merged; merging needs explicit owner approval.
- **Work completed:**
  - the row 4c lighting identity is replaced by §5.1 and T-LIGHT-SPLIT;
  - §2 item 2 is corrected, with the pinned writer, importer and Tas behaviours that conflict with §5.1;
  - new: the common absent-value policy, runtime-visibility requirement, view-coefficient separation, the unresolved LED
    comfort coefficient, and the required downstream SAM_OpenStudio#33, SAM_Tas and SAM_LadybugTools changes;
  - §6.1 gated exclusions, the §8 D-LIGHT row and the recorded findings;
  - a SAM_LadybugTools evidence pin.
- **Decisions:** owner approval of 2026-10-21, as listed in the "Amendment L1" note at the top and in D-LIGHT.
- **Assumptions:**
  - s is a declared assumption, not SAM data;
  - the baseline is a midpoint-derived reference from cited historical ranges, read through a secondary source;
  - the Tas 0.30 / 0.49 values are labelled template and observed values, without proven factory provenance.
- **Files changed:** `documentation/SIMULATION_INPUT_MAPPING_S07.md` only. No code, fixture, roadmap or
  `PROJECT_PROGRESS.md` change.
- **Checks:** local documentation checks only; no simulation, sizing, Tas or EnergyPlus execution.
  - The validated content SHA is `eb7e3ff71df7762c0a3a4dc18877a8aa60ef6973`. A record-only commit follows it.
  - **Citations:** every new prefixed citation resolves at its pin, and its line ranges lie inside the file.
    Reproduce with `git -C <repo> show <sha>:<path> | sed -n '<a>,<b>p'`. Semantically checked:
    - `OS/InternalCondition.cs:125-136,314-327`;
    - SAM_OpenStudio `SAM_OpenStudio/SAM.Analytical.OpenStudio/Convert/ToSAM/InternalCondition.cs:236-237` at
      `7f216657`;
    - `MA/Modify/UpdateInternalCondition.cs:96-99` and `MA/Modify/UpdateInternalConditionTemplate.cs:78-81`;
    - `SA/Create/InternalCondition.cs:81-87`;
    - `LB/ToLadybugTools/ProgramType.cs:110-118` and `LB/ToSAM/InternalCondition.cs:59-60`.
  - **Pinned files:**
    - SAM_LadybugTools `0ffbaecf` equals `git ls-remote origin refs/heads/sow/2026-Q4` on 2026-10-21;
    - the NCM v6.1.b template blob at `e84aeff6` has sha256 `509fc55d5064…`;
    - neither SAM internal-condition library at `6836595d` contains a radiant proportion.
  - **Arithmetic:** the split values are recomputed at full precision (s = 0.4192708333…), for example
    `awk 'BEGIN{s=0.2415/0.576; print 0.37*s, 0.37*(1-s)}'`. The midpoints were recomputed from IEA Table 6.
  - **Links:**
    - relative links resolve;
    - the GitHub, EDSL, EnergyPlus 25.1 and IEA DOI links return HTTP 200;
    - the T&F DOI returns 403 to scripted requests (paywalled publisher).
  - `git diff --check` is clean. The diff against the base touches only this file.
  - **Machine-local, not needed elsewhere:** the IEA PDF read for Table 6, and the read-only decode of the NCM
    template.
  - **GitHub checks:** see the PR for the final head.
- **Not covered by this PR:**
  - the SAM_OpenStudio#33 revision, the SAM_Tas change and the SAM_LadybugTools change (§5.1). Until they land, the
    pinned writers keep the behaviour recorded in §2 item 2;
  - the LED view coefficient;
  - the original RP-1681 per-fitting values;
  - the separate findings: opaque single-sided optics (OS-MAT-SINGLE-SIDED, S09 record), reversed glazing, and the
    duplicate view-coefficient default.
- **Next step:**
  1. Owner review and merge decision.
  2. After merge: the known-SHA `PROJECT_PROGRESS.md` closeout and guarded branch cleanup.
  3. Then revise SAM_OpenStudio#33 to §5.1, with its own review.
  4. The SAM_Tas absent-value change needs a separate owner write approval.
