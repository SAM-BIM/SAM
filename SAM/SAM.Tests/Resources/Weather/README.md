# Reference EPW fixtures (EPW writer tests)

Five TMYx EPW files, gzip-compressed (`gzip -9 -n`) without modification. `SHA256SUMS.txt` holds the
SHA-256 of each **uncompressed** original. `WeatherEPWExportTests.ReferenceFixtures_MatchRecordedHashes`
fails if a fixture changes.

| File | Location | Lat / lon / time zone | Source | Why |
|---|---|---|---|---|
| `HUN_BU_Budapest.Met.Center.128400_TMYx.epw` | Budapest, HU | 47.517 / 19.033 / +1 | SRC-TMYx (ERA5-based) | The F1 incident file: the embedded weather of the SAM_UI PR #239 box model came from it |
| `USA_AK_Fairbanks.Intl.AP.702610_TMYx.2004-2018.epw` | Fairbanks, AK | 64.804 / -147.876 / -9 | ISD-TMYx | High latitude and low sun; 13 degrees west of the zone meridian |
| `AUS_NSW_Sydney.Intl.AP.947670_TMYx.2004-2018.epw` | Sydney, AU | -33.946 / 151.177 / +10 | ISD-TMYx | Southern hemisphere |
| `SGP_SG_Changi.Intl.AP.486980_TMYx.2004-2018.epw` | Singapore | 1.350 / 103.994 / +8 | ISD-TMYx | Tropics; 14 degrees east of the zone meridian |
| `USA_AZ_Phoenix-Sky.Harbor.Intl.AP.722780_TMYx.2004-2018.epw` | Phoenix, AZ | 33.433 / -112.017 / -7 | ISD-TMYx | Clear, high direct normal |

## Provenance

- Budapest: copied 2026-10-19 from the SAM_UI PR #239 validation inputs
  (`C:\TasOut\licensed-e2e-20261019\inputs-box\Budapest.epw`, machine-local, byte-identical to the
  `-EpwPath` file of that validation). Its `COMMENTS 2` header names climate.onebuilding.org as the source.
- The other four: copied 2026-10-19 from the local Ladybug Tools bundle
  (`C:\Program Files\ladybug_tools\resources\weather\<name>\<name>.epw`), which redistributes the TMYx
  files published at https://climate.onebuilding.org/.

## Licence

TMYx files from climate.onebuilding.org are free to use with attribution: "Climatic data provided by
climate.onebuilding.org".

## Known properties used by the tests

On the EPW time base (hour ending, local standard time), the Fairbanks, Sydney and Singapore direct
normal closes with its own global - diffuse horizontal within about 1.3 %. The Budapest and Phoenix
files do not (about 9 %). Their direct normal closes with global - diffuse when the solar geometry is
taken about 30 minutes earlier, so within each file the direct normal and the horizontal components
are on time bases about half an hour apart. Budapest's own extraterrestrial column is a further
30 minutes later than standard-time geometry. The tests measure this rather than assume it.
