# EPW writer: physically correct radiation and sky fields (F1)

PR record. PR https://github.com/SAM-BIM/SAM/pull/212, branch `fix/epw-weather-radiation-q4`, base `sow/2026-Q4`
(`618db819`). Tested content SHA: `a2cb16cf`. Production code is unchanged since `b721c5f4`: `a2cb16cf` adds one
test and evidence, and the commits after it change only this record. Not merged.

## Status
Final review complete (2026-10-19, see "Final review"). No blocking issues; recommended for owner approval and
merge. TAS was not re-run (see Risks).

## Defect (SAM_UI PR #239, finding F1)
`SAM.Weather.Query.DataString`, which `Convert.ToEPW` and SAM_OpenStudio `EmbeddedAnnualWeatherPath` use, wrote:
- fields 11-14 (extraterrestrial horizontal, extraterrestrial normal, horizontal IR, **global horizontal**) as `0`;
- field 15 (**direct normal**) as `DirectSolarRadiation`, i.e. global minus diffuse, which is the horizontal beam;
- field 23 (total sky cover, in tenths) as the SAM 0-1 fraction, and field 24 (opaque sky cover) as `0`;
- field 8 (dew point) as `0` when the source has none (TAS weather), and missing radiation as `0`.

Measured on the box model's embedded weather, which comes from the Budapest TMYx EPW: GHI 1276 -> 0 kWh/m2 and
DNI 1305 -> 735 kWh/m2 (the horizontal beam).

## Source-field semantics (verified in code)
- SAM `WeatherDataType.GlobalSolarRadiation`, `DiffuseSolarRadiation` and `DirectSolarRadiation` are all
  **horizontal**: `WeatherHour.CalculatedGlobalSolarRadiation` gives global = direct + diffuse. They are
  hourly values in W/m2, which equal Wh/m2 for one hour.
- EPW import (`Query.TryGetData`) reads GHI (field 14) and DHI (field 16) and does **not** read DNI. It stores
  the EPW dew point (field 8) under `WetBulbTemperature`, and total sky cover / 10 under `CloudCover`. SAM hour
  index `i` is EPW hour `i + 1`.
- TAS weather (SAM_Tas `Convert.ToSAM(TWD.WeatherYear)`) carries global, diffuse, cloud cover (0-1), dry bulb,
  RH and wind, with no dew point and no pressure. TBD hour `i` (1-based) maps to SAM index `i - 1`.
- `WeatherYear` has exactly 365 days. Time zone is the `WeatherDataParameter.TimeZone` string ("UTC+01:00").
- SAM has no solar-position code of its own. The one in SAM_SolarCalculator cannot be referenced, because that
  repository depends on SAM. The geometry is therefore implemented privately in SAM.Weather.

## Transformations (EnergyPlus EPW data dictionary conventions)
| EPW field | Written value |
|---|---|
| 8 dew point | stated value; else Magnus (Alduchov-Eskridge) from DBT and RH; else 99.9 |
| 11 / 12 extraterrestrial horizontal / normal | `I0 * meanCosZ` / `I0` (Spencer eccentricity, 1367 W/m2); 9999 without a location |
| 13 horizontal IR | 9999 (missing). SAM holds no IR; EnergyPlus derives it (Clark-Allen) from DBT, dew point and opaque sky cover |
| 14 global horizontal | stated value; else direct + diffuse; else 9999 |
| 15 direct normal | `beam / meanCosZ`, bounded by the Rayleigh-atmosphere limit; 0 if the sun is down all hour; 9999 without beam or location |
| 16 diffuse horizontal | stated value; else global - direct; else 9999 |
| 23 / 24 total / opaque sky cover | cloud cover x 10 (tenths) to both; 99 if missing |

- **Time base:** EPW hour `h` holds the radiation of the hour ending at `h:00` local standard time.
  `meanCosZ` is the mean of max(0, cos zenith) over `[h-1, h)` from 60 sub-steps. This makes the conversion
  energy-consistent, so `DNI * meanCosZ` returns the stated beam, and it handles the sunrise and sunset hours.
  Daylight saving does not apply: the writer declares none, and EPW is standard time.
- **Sun position:** NOAA general solar position (Spencer 1971 declination and equation of time), with the
  longitude and the time-zone meridian. No refraction.
- **Low-sun bound:** global - diffuse is ill-conditioned at grazing sun. A few W/m2 of beam divided by
  `meanCosZ` of about 0.02 gives 1000-1400 W/m2 of DNI. EnergyPlus then puts that DNI on east or west facades
  at near-normal incidence. The bound is the beam of a pure Rayleigh atmosphere: Kasten-Young (1989) air mass,
  pressure-corrected, with the Kasten (1996) integral Rayleigh optical thickness, averaged with cos-zenith
  weights. A real atmosphere only attenuates more, so this is a physical bound rather than a tuned clear-sky
  model. It does not bind at high sun: about 1210 W/m2 at air mass 1. The stated global and diffuse are always
  written unchanged.

## Decisions and assumptions
- **Missing is not zero.** A missing value is written with its EPW missing code. EnergyPlus sets missing DNI
  and DHI to 0 and reports the count, and it derives a missing IR.
- **IR is not computed in SAM.** Writing 9999 lets EnergyPlus apply its own documented algorithm. The previous
  0 was also treated by EnergyPlus as invalid and derived. Because opaque sky cover was 0 (clear sky), that
  gave a sky about 3 K too cold (see the box evidence). It was not a 0 W/m2 sky, as the PR #239 record says.
- **Opaque sky cover = SAM cloud cover.** SAM holds one cloud amount. TAS uses that amount for long-wave sky
  exchange, which is the role opaque cover has in EnergyPlus. For EPW-sourced data it is the total cover, and
  the opaque value of the original file cannot be recovered. This is a stated proxy.
- **No time zone means no DNI.** The time zone is not guessed from longitude. EPW and TAS imports both set it.
- **Public API is unchanged.** All four existing `DataString` overloads keep their signatures. Without a
  location they now write DNI as 9999 instead of the horizontal beam. New overloads take latitude, longitude
  and time zone. `DataString(WeatherData)` passes them.
- **Versioning:** none in the repository. `SAMVersion` is stamped by CI (`Directory.Build.props`, `build.yml`),
  and there is no Grasshopper component change.

## Independent verification
Five unmodified TMYx EPWs are committed under `SAM/SAM.Tests/Resources/Weather` (gzip, SHA-256 of the
originals, provenance README, attribution in `THIRD_PARTY.md`): Budapest (incident, ERA5-based), Fairbanks
(64.8 N), Sydney (southern hemisphere), Singapore (tropics), Phoenix (clear). Each is imported into SAM and
re-exported. The exported DNI is compared with the dataset's own DNI, using an independent geometry in the test:
the Michalsky (1988) Astronomical Almanac algorithm, which goes through sidereal time and has no equation of
time. Full numbers: `documentation/evidence/epw-weather-f1/reference-epw-statistics.txt`.

| Reference | Own closure (EPW time base / 30 min earlier) | Annual DNI exported vs reference | Sun-up median / P90 error | Facade beam N/E/S/W |
|---|---|---|---|---|
| Sydney | 0.40 % / 8.15 % | +0.22 % | +0.24 % / 0.59 % | within 0.3 % |
| Singapore | 0.54 % / 7.78 % | +0.28 % | +0.27 % / 0.57 % | within 0.4 % |
| Fairbanks | 1.32 % / 6.07 % | +0.23 % | +0.17 % / 0.61 % | within 0.4 % |
| Phoenix | **8.85 % / 2.34 %** | +6.7 % | -1.49 % / 20.0 % | E -10 %, W +27 % |
| Budapest | **9.31 % / 4.26 %** | +14.7 % | -1.36 % / 20.9 % | E -7 %, W +39 % |

- The writer's time base is the one the self-consistent datasets use. Its extraterrestrial-horizontal values
  match the Sydney and Phoenix files' own field hour by hour (for example 788.18 against 788).
- **Phoenix and Budapest are internally inconsistent.** Their DNI closes with their own GHI - DHI only when the
  geometry is taken about 30 minutes earlier, and Budapest's own extraterrestrial column is a further 30 minutes
  later. No conversion that follows the EPW convention can reproduce such a DNI from GHI - DHI. The tests
  measure this rather than assume it. For these files the tests require energy closure instead: the exported
  `DNI * cosZ` equals GHI - DHI within -0.4 %, which is the beam above the Rayleigh bound. This is a property of
  the source data, not of the writer.
- Synthetic hours: a known DNI at solar noon, at low sun, in winter and summer, in both hemispheres, 13-14
  degrees off the zone meridian and with a half-hour zone (New Delhi) is recovered within 1 %, or within 3 % in
  hours with the sun below about 5 degrees, where the two algorithms' few arc-minutes are a percent of the
  altitude. Night, sun-below-horizon beam, the low-sun bound, high sun untouched, missing data, partial
  components, TAS-style weather, leap year (8760 records, geometry of the written date) and export, import and
  export stability are also covered.
- **Fail-first:** with the original `DataString.cs` and the same tests, 34 of 38 fail. The 4 that pass are
  fixture hashes, the unchanged importer, night zeros and the leap-year row count.

## Box-model check (machine-local, EnergyPlus 25.2.0 from Ladybug Tools)
The producer's `in.idf` from PR #239 experiment e0 is used, with only `Version` changed to 25.2 and seven site
and surface output variables added. Three weather files: the old export, the corrected export (the box model's
own embedded `WeatherData` through the new writer) and the explicit Budapest EPW. Committed summaries:
`documentation/evidence/epw-weather-f1/` (`eplus-box-*.txt`, `weather-field-summary.txt`, `run-inputs.sha256`).
The old-export run reproduces the PR #239 E+ 25.1 values exactly (2810.0183 / 3832.5030 kWh), and the explicit run
reproduces e2.

| Annual (box) | Old export | Corrected export | Explicit EPW |
|---|---|---|---|
| Window transmitted solar [kWh] | 2592.6 (-28.5 %) | 3755.1 (+3.6 %) | 3623.7 |
| Site direct solar, mean [W/m2] | 83.6 | 162.1 (+8.9 %) | 148.8 |
| Site horizontal IR, mean [W/m2] | 296.4 | 319.6 (+2.9 %) | 310.5 |
| Site sky temperature, mean [C] | -4.83 | 0.28 | -1.87 |
| Ideal Loads heating [kWh] | 2810.0 (+29.3 %) | 2081.9 (-4.2 %) | 2173.9 |
| Ideal Loads sensible cooling [kWh] | 3432.6 (-17.1 %) | 4311.7 (+4.1 %) | 4140.8 |

The remaining embedded-vs-explicit difference comes from the Budapest file's internal half-hour inconsistency
(direct) and from Clark-Allen IR with opaque = total cover against the file's own IR column. It is not an
acceptance criterion, and engine agreement between TAS and EnergyPlus is not claimed.

Reproduce: run `energyplus.exe -w <weather>.epw -d . in.idf` in each `C:\TasOut\f1-epw-weather-20261019\run-*`
folder, then `eso-summary.ps1 -Eso eplusout.eso` (PR #239 tool). These are machine-local. Copy that folder and
`C:\TasOut\licensed-e2e-20261019` to continue elsewhere.

## Final review (2026-10-19)

### 1. Rayleigh bound: how often it binds and what it changes
Test `RayleighBound_BindsOnlyAtGrazingSun_AndRemovesLittleEnergy`, evidence
`documentation/evidence/epw-weather-f1/rayleigh-bound-activation.txt`. It reads the writer's own hour geometry back
from the export (ETR horizontal / ETR normal), so "binds" counts exactly the hours the bound changed (more than 1 %).
It compares against the same conversion without the bound. Facade beam uses the independent incidence geometry.

| Reference | Hours bound / beam hours | Highest sun when bound | Annual DNI | Horizontal beam | Facade beam N / E / S / W |
|---|---|---|---|---|---|
| Sydney, Singapore, Fairbanks | **0** | - | 0.00 % | 0.00 % | 0.00 % on all four |
| Budapest | 170 / 4217 (4.0 %) | ~4.7 deg | -51.9 % | -0.18 % | -23.1 / 0.0 / -4.5 / -15.5 % |
| Phoenix | 251 / 4217 (6.0 %) | ~20.7 deg | -3.6 % | -0.38 % | -11.9 / 0.0 / -1.4 / -6.3 % |

- For the three self-consistent datasets the bound never binds, so the exported DNI is the unbounded energy-consistent
  value. The test asserts this.
- It binds only for the two files whose DNI and GHI/DHI are half an hour apart. There, the stated beam at low sun
  implies impossible DNI: up to 2450 W/m2 at ~21 deg in Phoenix, where a pure Rayleigh atmosphere allows about
  1080 W/m2, and up to 4.7e5 W/m2 in Budapest in the minutes after sunrise. Without the bound, Budapest's annual DNI
  would be 3109 instead of 1495 kWh/m2, and its west facade beam 567 instead of 479 kWh/m2 (the dataset's own DNI
  gives 345). The bound removes at most 0.38 % of the stated horizontal beam.
- East facades are unaffected (0.00 %): the binding hours are afternoon and sunset hours.
- Separately, by rule, beam stated while the sun is below the horizon all hour becomes DNI 0. That is 163 hours and
  0.51 kWh/m2 in Budapest, and none elsewhere.
- **Conclusion:** the bound is inert for physically consistent data and removes only physically impossible beam
  from inconsistent data. Accepted.

### 2. Opaque-sky-cover stand-in
The stand-in is documented in the code (`DataString` remarks), in "Decisions and assumptions" above and in the PR
description. It was isolated with two more EnergyPlus box runs (`eplus-box-corrected-refopaque.txt`,
`eplus-box-corrected-refIR.txt`): the corrected export with the reference file's real opaque cover, and with the
reference file's own IR column.

| Box, annual | Stand-in (opaque = total) | Real opaque cover | Dataset IR | Explicit EPW |
|---|---|---|---|---|
| Horizontal IR, mean [W/m2] | 319.6 | 322.2 | 310.5 | 310.5 |
| Sky temperature, mean [C] | 0.28 | 0.88 | -1.87 | -1.87 |
| Heating [kWh] | 2081.9 | 2066.9 (-0.7 %) | 2101.1 (+0.9 %) | 2173.9 |
| Sensible cooling [kWh] | 4311.7 | 4327.9 (+0.4 %) | 4238.4 (-1.7 %) | 4140.8 |

- Using the stand-in instead of the real opaque cover changes IR by 2.6 W/m2 and loads by less than 1 %. In this
  ERA5-based file the opaque cover is on average larger than the total (6.56 against 5.80 tenths), so the "real"
  value is itself unreliable.
- EnergyPlus's Clark-Allen IR against the dataset's IR column is a larger effect (9 W/m2, up to 1.7 % of loads), and
  it is EnergyPlus's documented behaviour for a missing IR.
- With the dataset's IR the residual against the explicit EPW is -3.4 % heating and +2.4 % cooling. That is the
  solar part, from the file's half-hour DNI inconsistency.
- **Conclusion:** documented, small, and better than the alternatives (opaque 0 means a clear sky; 99 means an
  unknown EnergyPlus substitute). Accepted.

### 3. CI, tests and mergeability
- PR #212 at `90822fac`: GitHub `build (Release)`, `test (Release)` and `spdx` pass. Mergeable, state CLEAN, base
  `618db819` unchanged, no reviews or comments, ready for review. The final head's checks are in the PR description.
- Local at `a2cb16cf`: `SAM.Tests` 3726/3726 in Release and in Debug, which includes the 43 `WeatherEPWExportTests`
  cases. Production code is identical to `b721c5f4`, whose sandboxed `SAM.sln` Release rebuild had 0 errors.

**Blocking issues: none.** Recommended for owner approval and merge.

## Files
- `SAM/SAM.Weather/Query/DataString.cs`: field mapping, missing codes and location overloads.
- `SAM/SAM.Weather/Query/EPWSolarGeometry.cs` (new): hour-mean geometry, Rayleigh bound, dew point and time zone.
- `SAM/SAM.Tests/WeatherEPWExportTests.cs` (new, 43 test cases) and `SAM/SAM.Tests/SAM.Tests.csproj` (copies fixtures).
- `SAM/SAM.Tests/Resources/Weather/` (new): 5 `.epw.gz`, `SHA256SUMS.txt` and `README.md`.
- `THIRD_PARTY.md`: TMYx attribution.
- `documentation/evidence/epw-weather-f1/` (new) and this record.

## Validation (local, Windows, .NET 8 SDK)
- At `a2cb16cf`: `dotnet test SAM/SAM.Tests/SAM.Tests.csproj -c Release`: **3726/3726** passed. With `-c Debug`:
  3726/3726.
- At `b721c5f4` (same production code): `dotnet test` gave 3721/3721 in Release and Debug, before the bound test was
  added. `MSBuild SAM.sln /t:Restore;Rebuild /p:Configuration=Release /m:1 /p:UseSharedCompilation=false`: 0 errors.
  It ran with `APPDATA`/`USERPROFILE` redirected to a sandbox so the Grasshopper post-build copies could not touch
  the installed SAM.
- `git diff --check` is clean, and the new `.cs` files carry the SPDX header.
- GitHub checks: green at `90822fac`; see the PR description for the final head.

## Unresolved issues and risks
- **Consumers see changed weather.** Embedded-weather OpenStudio and Both runs get more solar and a warmer sky.
  Results from embedded weather (including PR #239 B1a) change and are not comparable with earlier runs. The
  explicit-EPW route is unchanged: the file is passed through, and the SAM importer is untouched (test
  `ReferenceEpw_Import_IsUnchanged`).
- **TAS embedded path not re-run.** No TAS licence run was done here. Whether the TAS route reads this EPW
  (PR #239: "Tas then simulates with no solar gain") is to be confirmed in the next licensed run.
- **Internally inconsistent sources** (above): DNI cannot be reconstructed better than the source's own
  consistency allows, and east/west facade beam can differ by tens of percent from the dataset's own DNI.
  Mitigation: use the explicit EPW when the original file is available, since SAM does not retain DNI.
- **Leap years:** `WeatherYear` holds 365 days. In a leap year, day index 59 is written as 29 February and
  31 December is not written (existing labelling, unchanged). The header declares no leap day.
- **Out of scope, existing behaviour:** DBT, RH, wind and pressure use the current culture, so a decimal-comma
  locale breaks the row. Missing DBT, RH and wind are still written as 0. The importer does not recognise EPW
  missing codes (a 9999 GHI is imported as 9999). Illuminance, visibility and present-weather fields are 0.
- The SAM_OpenStudio `R7/WeatherImportTests` and `C4/EmbeddedWeatherDesignDaysTests` export through this writer
  but assert no radiation fields. They were not run here, because they need the OpenStudio SDK.

## Recommended next step
1. Owner approval and merge of PR #212. Then the `PROJECT_PROGRESS.md` closeout with the merge SHA, and cleanup
   of the task branch.
2. F2 (EPW culture and missing-code correctness for the other fields and the importer), in the same session, on a
   new branch from the merged `sow/2026-Q4`.
3. Later: re-run the PR #239 box and HungaryHouse embedded-weather comparisons with a licensed TAS, and record
   the new embedded-vs-explicit residuals. Suggested settings: Opus 5.5, High effort, a new session in Build mode,
   because it is a licensed validation run.
