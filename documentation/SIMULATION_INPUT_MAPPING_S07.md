# S07 - Minimal Phase B physical-input mapping contract

Task S07 of the [simulation roadmap](SIMULATION_ROADMAP_Q4_2026.md). Documentation only: this document implements nothing and
changes no behaviour.

Phase A is met ([SIMULATION_ROADMAP_Q4_2026.md](SIMULATION_ROADMAP_Q4_2026.md)). S07 defines what S08 (Tas read-back) and S09
(OSM/IDF read-back) compare, against which reference, how tolerantly, and how outcomes aggregate.

**Owner approval.** Decisions D-ROUTE, D-F2 (V0/V1/V2), D-F8, D-F9 (A + C), D-FIX, the film convention and every tolerance
in this document are **approved by the owner, 2026-10-20 (S07 plan revision 3)**. The owner can amend any of them during
review of the pull request that introduces this document.

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
- **Contract-route facts from evidence:**
  - **Geometry:** with widths OFF, T3D = TBD = SAM shell area and volume. This was measured on a real model (`MA/DIRECT_T3D_ROUTE.md:258-275`). S08 re-verifies it per fixture.
  - **Floor-area flag:** `element.zoneFloorArea` (`MA/Convert/ToT3D/T3DDocument.cs:269`).
  - **Frame:** `window.frameWidth`, `isPercFrame = true`, `framePerc` (`MA/Convert/ToT3D/T3DDocument.cs:308-314`).
  - **Aperture identity:** `MA/Modify/UpdateIds.cs:12` stamps SAM identities on the TBD. Apertures are matched by `MA/Query/Match.cs`, `MA/Query/PaneStampMatch.cs` and `MA/Query/ApertureByElementName.cs`. S08 keys every comparison on these identities, not on names.
  - **Layer orientation:**
    - TAS chooses the reversed side of a partition, and `reverseElement` has no effect.
    - `MA/Modify/UpdateReversed.cs:25` sets it so that the space earlier in `AdjacencyCluster.GetSpaces()` sees the layers as listed. Horizontal surfaces are left to TAS (`MA/DIRECT_T3D_ROUTE.md:101-115`).
    - S08 compares layer order per zone side.

## 2. Findings carried forward (other engines and non-geometry quantities)
1. **U and g are engine-derived in both engines.** Both receive layers (`MA/Modify/UpdateMaterial.cs:167-240`, `MA/Modify/UpdateConstruction.cs:9-66`; `OS/Construction.cs`, `OS/Material.cs`, `OS/Aperture.cs:153-283`). Only the OS no-pane-layer fallback writes U and SHGC directly (`OS/Construction.cs:85-172`).
2. **Gain densities:**
   - Both engines compute SAM total W ÷ stored `SpaceParameter.Area` (`MA/Modify/UpdateInternalCondition.cs:94-271`; `OS/InternalCondition.cs:45-190`).
   - OS uses the authored radiant and visible fractions, falling back to 0.3 / 0.32 / 0.25 / 0 only when a value is absent or NaN (`OS/InternalCondition.cs:81-160`). The latent-equipment radiant fraction is 0 (L183).
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
- **Panel convention:** recorded per fixture (inner-face or mid-surface). On the contract route (`UseWidths=false`) both engines are expected to use the polygons as given, so the convention does not alter the engine comparison. It only interprets what the geometric volume represents.
- **The three volumes:**
  - **geometric** (the engine-comparison reference);
  - **stored** `SpaceParameter.Volume` (reported; used for Tas `ticV` ACH and `CalculatedInfiltrationAirFlow`);
  - **net-air** (no SAM use found; out of scope, UNVERIFIED).

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
| 4c | Radiant / visible fraction (–) | authored | `lightingRadProp`, `occupantRadProp`, `equipmentRadProp` | `setFractionRadiant` / `setFractionVisible` | identity | — | 0.001 | `OS/InternalCondition.cs:81-160` | absent → DOCUMENTED-DEFAULT |
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
  - Tas: `frameWidth`, `framePerc` from the import plan (`MA/Convert/ToT3D/T3DDocument.cs:308-314`). The derivation rule is to be written from `MA/Query/T3DImportPlan.cs`; until then T-FRAME is UNVERIFIED on the Tas side.
  - Tolerance: 0.5 % area; 1e-5 conductance.
- **T-OMIT0:** OS omits a gain when the SAM total ≤ 0. Verified only when no object of that type exists for the zone.
- **T-OA-SYS:** OS Ideal-Loads outdoor air (system load) versus Tas `ticV` (zone-entering air). Only **flow-rate** equality is accepted.

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
| 3 | **BLOCKED** (owner decision named) | unaccepted UNSUPPORTED-MAPPING or MAPPING-DIFFERENCE; DOCUMENTED-DEFAULT on a gated quantity; MODEL-INTERPRETATION-REQUIRED not recorded |
| 4 | **NOT RUN** (quantities named) | UNVERIFIED that is readable but not yet resolved, including §7 schedule alignment |
| 5 | **Not PASS — accepted limitation** (items named) | only owner-accepted UNSUPPORTED-MAPPING / MAPPING-DIFFERENCE / DOCUMENTED-DEFAULT remain. Accepted per quantity and per fixture, as with S06. **Never PASS.** |
| 6 | **PASS** | all MATCH / WITHIN-TOLERANCE / verified ACCEPTED-TRANSFORMATION / NOT-APPLICABLE (proven SAM absence) |

"INCOMPLETE" in revision 2 is replaced by ranks 3–4.

## 7. Schedule time alignment
The Tas hour labels (`hourlyValues[1]`, yearly index 1), the first OS fixed-interval value and leap-year handling are UNVERIFIED. Until S08/S09 verify them by read-back:
- hourly-aligned quantities (5, 6, 7a profile, 8b) are only "equal under assumed alignment";
- the fixture is at most **NOT RUN** for alignment;
- an EFLH match is supporting evidence only.

---

## 8. Owner decisions

D-ROUTE, D-F2 (V0/V1/V2), D-F8, D-F9 (A + C) and D-FIX below: approved by the owner, 2026-10-20 (S07 plan revision 3). The Recommendation column records the approved choice.

| ID | Alternatives / content | Recommendation |
|---|---|---|
| **D-ROUTE** | Contract = Direct T3D with `UseWidths=false` (§1). Production (SAM_UI, the Both producer) stays gbXML until a separately approved integration. S08 either uses a harness selecting Direct, or waits for that integration. | Approve the contract route; production switch out of S07 |
| **D-F2 ventilation** | **Requirement and delivery are separate quantities.** Requirements are preserved for MEP sizing and later IZAM/TPD work, and never activate thermal ventilation by themselves. States:<br>**V0** no requirement.<br>**V1** requirement only, no delivery selected: Tas no `ticV` = MATCH with intent; OS unscheduled DSOA delivery = **MAPPING-DIFFERENCE** (reported; any OS change is a separate decision with compatibility consequences).<br>**V2** Building Simulator delivery **explicitly selected** (Ventilation profile): supported in both, compared on 8b with T-OA-SYS. The selection, not the building type, defines V2.<br>IZAM/TPD delivery is outside S07. Exhaust is UNSUPPORTED in both, with no balance or thermal assumption.<br>Rejected: P1 (missing profile = constant delivery; reverses the Tas design and test); P2 (missing profile = invalid; contradicts V1). | **V0/V1/V2 as stated.** Recorded, not fixed: `ticV` + AHU IZAM `fromOutside` on the same zone; space DSOA replacing the SpaceType DSOA; the Both producer's `AddIZAMs = true`. |
| **D-F8 window opening** | Tas read-back of 8d. OS: converter limitation (EnergyPlus supports opening airflow via AirflowNetwork; the SAM converter does not map `OpeningProperties`), reported. Common equivalence fixtures are closed-window (no `OpeningProperties`). No natural-ventilation implementation. | **As stated** |
| **D-F9 volume** | A: engine reference = §4 geometric; stored reported. C: fixtures have stored ≈ geometric within 0.5 %, or a recorded interpretation (convention, intended volume). The difference is MODEL-INTERPRETATION-REQUIRED (rank 3 until recorded), not a defect. A recorded interpretation **never waives** a native-vs-geometric FAIL; it only fixes how 7b and 8b are reported. | **A + C** |
| **D-FIX fixtures** | Roadmap box + multizone requirement **retained**. The committed two-cell model has non-zero supply and exhaust and no Ventilation profile: it is kept as **divergence evidence** (V1 MAPPING-DIFFERENCE; exhaust unsupported), not as an equivalence fixture. The box and HungaryHouse are machine-local. | The owner selects or clears both equivalence fixtures before the S08/S09 gate, against the criteria below. S07 creates none. |

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
- §6 completeness (found while checking this document; none of them can reach PASS, because PASS is an allow-list of
  resolved outcomes, but the resulting status is not stated): (1) S-UNSPEC with a native object present at zero or NaN;
  (2) an unaccepted DOCUMENTED-DEFAULT on a quantity that is not gated; (3) a native value that is present but NaN for an
  S-VALUE input. The approved gate text is unchanged; the owner can decide a rank for each during review.

## PR record

Task S07 of [SIMULATION_ROADMAP_Q4_2026.md](SIMULATION_ROADMAP_Q4_2026.md). Documentation only; no behaviour change.

- **Status:** In review: SAM#(number pending), base `sow/2026-Q4` (`6836595d`), branch `docs/s07-physical-input-mapping`.
  Not merged; merge needs explicit owner approval. Validated content SHA: (pending). Final PR head and checks: in the PR
  description.
- **Work completed:** this document (plan revision 3, sections 1-8, owner choices recorded, evidence pins re-checked); the
  roadmap updated for the S07 row, decision items 3 and 5, and the contract-route note.
- **Decisions:** D-ROUTE, D-F2 (V0/V1/V2), D-F8, D-F9 (A + C), D-FIX, the film convention and every tolerance are
  approved by the owner, 2026-10-20 (S07 plan revision 3); the owner can amend any of them during review. Production
  stays gbXML. The two equivalence fixtures are not yet selected (D-FIX: the owner selects or clears them before the
  S08/S09 gate).
- **Files changed:** `documentation/SIMULATION_INPUT_MAPPING_S07.md` (new), `documentation/SIMULATION_ROADMAP_Q4_2026.md`.
- **Checks (local, no GitHub result implied):**
  - the seven `sow/2026-Q4` tips were re-read with `git ls-remote` on 2026-10-20 and equal the pins above;
  - every file citation in this document (83 distinct) resolves at its pinned SHA, line ranges lie inside the file, and
    each native name used was found in the pinned code or is marked UNVERIFIED / *(translator)*. Reproduce with
    `git -C <repo> show <sha>:<path> | sed -n '<a>,<b>p'`;
  - evidence corrections made while checking: the OpenStudio, Tas and SAM path prefixes; the benchmark producer, SAM_UI
    and `PROJECT_PROGRESS.md` paths; `freshAirRate` is written at `MA/Modify/UpdateInternalCondition.cs:141`; the older
    `ToTBD(AnalyticalModel, …)` reaches its `zone.volume` / `zone.floorArea` writes through
    `MA/Convert/ToTBD/Building.cs:47` and `MA/Modify/Update.cs:612,623`; `GeometricalFloorPanels` is used at
    `SA/Query/FloorArea.cs:123`; bare file names were expanded to prefixed paths;
  - §5 has all ten categories, each with SAM, Tas and OS fields, a rule, per-unit tolerances (or a stated reason) and
    evidence;
  - the §6 gate was checked as an allow-list: no outcome other than MATCH, WITHIN-TOLERANCE, verified
    ACCEPTED-TRANSFORMATION or proven NOT-APPLICABLE can give PASS (completeness gaps listed above);
  - relative links resolve and no link targets a repository outside `SAM-BIM`; S08-S12 still read "Not started" in the
    roadmap.
- **GitHub checks:** (pending: build, test, spdx; see the PR).
- **Risks and open items:** T-FRAME on the Tas side stays UNVERIFIED (its inputs are built at
  `MA/Query/T3DImportPlan.cs:720-847`, but the derivation rule is not yet written); schedule hour labels and leap-year
  handling stay UNVERIFIED until S08/S09 read them back (§7); the Tas design-day reader may need a raw-field read (§3); the
  §6 completeness gaps above; the roadmap's closing line "No task is active. S07 is the next candidate" was left unchanged
  by instruction and is stale once this merges.
- **Next step:** owner start of S08 (Tas independent native-input read-back). Switching the production route from gbXML to
  Direct is separate integration work and needs its own approval.
