# Icon migration validation â€” 2026-10-11

Base: `sow/2026-Q4`. Scope: SAM-BIM only. Reference: [SAM#179](https://github.com/SAM-BIM/SAM/pull/179); [SAM#166](https://github.com/SAM-BIM/SAM/pull/166) is the closed historical design source.

## Changes and decisions

- Grasshopper manifest coverage: 522 concrete objects. GUIDs, APIs, namespaces, component metadata and behavior are preserved.
- Reuse the frozen SAM glyph/palette/badge grammar and frozen SAM-BIM extension v1. Preserve plugin identity, external integration logos, severity symbols and historical attribution.
- Source comparison against current base: 521 icon resource token swaps; 0 other production C# changes, apart from standard SPDX headers and additive resource properties.
- Validated 522 manifest resource references; 0 errors.

## Fresh validation

- VS MSBuild Restore,Build, Release: **PASS**. Scratch APPDATA/USERPROFILE; current sibling SAM-BIM dependencies, OCCT native build skipped.
- Regression suites: SAM-0.trx: passed 3414, failed 0, total 3414
- Re-parsed compiled component declarations: 522/522 manifest rows; 0 coverage/canonical 24x24 RGBA PNG errors.
- Built assembly resource presence and exact embedded PNG bytes checked.

Next step: verify final-head GitHub checks and merge only if green and safe under the owner authorization. No force-push or branch deletion. Hosted Rhino smoke is tracked separately; prior historical results are not fresh runs.

## Fresh CI blocker — do not merge

The owner approved enabling Actions. Repository Actions permission was set enabled (existing allowed_actions=all preserved), and workflow-enable endpoints succeeded; workflow definitions report active. Build/SPDX dispatch still fails HTTP422: Actions has been disabled for this repository. Reading SAM-BIM organisation Actions policy returns HTTP403 (org-admin scope unavailable). No broader privileges or upstream access requested. Fresh local build, 3414 tests and icon guards pass, but no fresh final-head CI is claimed. Next step: organisation/repository administrator resolves the effective Actions restriction, then dispatch build/SPDX at the current PR head and merge only after green checks.
