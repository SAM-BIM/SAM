<!-- SPDX-License-Identifier: LGPL-3.0-or-later -->
<!-- Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors -->

# Part F / Part O — session handover

**This file is the authoritative cross-laptop continuation state, and it is deliberately short.** Michal
works interchangeably on two laptops, so nothing important may live only in a chat, a terminal history or
an unpushed local commit. Read §0 first, reconcile, then §1 and §5.

| Looking for | Read |
|---|---|
| Repos, branches, SHAs, PRs, what to do next | **this file** |
| Approved Document F regulatory / calculation rules | [`PartF-ADF-Volume1-2021-Traceability.md`](PartF-ADF-Volume1-2021-Traceability.md) |
| Real TAS / Part O / TM59 validation evidence | [`PartO-TAS-VALIDATION.md`](PartO-TAS-VALIDATION.md) |
| Implementation history, closed findings, superseded designs | [`PartF-HANDOVER-ARCHIVE.md`](PartF-HANDOVER-ARCHIVE.md) — **history only, never state** |

**Working sequence, every checkpoint:** implement → test → commit → push → independent review/fixes → test
→ commit/push → **update this handover** → commit/push the handover → continue. A checkpoint is not
finished while this file is behind the repositories.

---

## 0. Current repository state

*Verified 2026-08-19. This table is the single authoritative record of repository state; nothing else in
this file or the archive supersedes it.*

**FIVE repos are in the workstream**, all on `feature/partf-terminal-transfer-compliance`, all with an open
PR against `sow/2026-Q3`. **`sow/2026-Q3` is never committed to directly** and is untouched everywhere.

| Repo | Last CODE commit | HEAD | Cut from | PR |
|---|---|---|---|---|
| `SAM` | **`3fcfb880`** | that, plus documentation commit(s) on top | `sow/2026-Q3` @ `34dea440` | [SAM#73](https://github.com/SAM-BIM/SAM/pull/73) |
| `SAM_Systems` | **`4446bb8`** | `618fc74` — base-sync merge, first parent is the pin | `sow/2026-Q3` @ `d7303c2` | [SAM_Systems#14](https://github.com/SAM-BIM/SAM_Systems/pull/14) |
| `SAM_UI` | **`fcf0ec8`** | `2a0d480` — base-sync merge, first parent is the pin | `sow/2026-Q3` @ `074f3d9` | [SAM_UI#75](https://github.com/SAM-BIM/SAM_UI/pull/75) |
| `SAM_Tas` | **`2750a21`** | exactly `2750a21` | `sow/2026-Q3` @ `3d58bfe` | [SAM_Tas#29](https://github.com/SAM-BIM/SAM_Tas/pull/29) |
| `SAM_Tas_Grasshopper` | **`29c31db`** | exactly `29c31db` | `sow/2026-Q3` @ `9555aa1` | [SAM_Tas_Grasshopper#4](https://github.com/SAM-BIM/SAM_Tas_Grasshopper/pull/4) |

**Why a HEAD is not pinned to a SHA.** The commit that updates this file cannot contain its own hash. The
last **code** commit is pinned instead.

### The invariant — and a descendant check is NOT enough

"HEAD descends from the recorded SHA" also passes when somebody committed code and never recorded it —
exactly the state this file exists to prevent. Verify all three:

1. every tree **clean** and every branch **level with its remote**;
2. HEAD **descends from** the recorded last-code SHA;
3. **every change between that SHA and HEAD touches only documentation.**

```bash
for r in SAM SAM_Systems SAM_UI SAM_Tas SAM_Tas_Grasshopper; do echo "=== $r ==="; git -C $r status --porcelain; git -C $r log --oneline -1; git -C $r log --oneline -1 origin/feature/partf-terminal-transfer-compliance; done
```

```bash
while read -r r sha; do git -C "$r" merge-base --is-ancestor "$sha" HEAD && echo "$r: descends from $sha" || echo "$r: DOES NOT CONTAIN $sha - STOP"; git -C "$r" diff --name-only "$sha" HEAD | grep -vE '^(documentation/PartF-HANDOVER\.md|documentation/PartF-HANDOVER-ARCHIVE\.md|documentation/PartO-TAS-VALIDATION\.md|AGENTS\.md|PROJECT_PROGRESS\.md)$' | sed "s|^|$r UNRECORDED CODE: |"; done <<'EOF'
SAM 3fcfb880
SAM_Systems 4446bb8
SAM_UI fcf0ec8
SAM_Tas 2750a21
SAM_Tas_Grasshopper 29c31db
EOF
```

That must print exactly **five** `descends from` lines and nothing else. **Any `UNRECORDED CODE:` or
`DOES NOT CONTAIN` line means stop and reconcile before changing any code.**

**Two reconciliation lessons worth keeping** (the incidents themselves are in the archive, where the check
is recorded as having earned its keep five times):

- **A stale pin and genuinely lost work look identical from here.** That is why the rule is *stop and
  reconcile*, not *assume*. Resolve by reading the prose, confirming the commit is accounted for, and
  correcting the pin — and bump this table in the **same commit** that lands code.
- **Check for a merge before assuming lost work.** `git log -1 --format='%P' <head>` showing two parents is
  the tell of a `sow/2026-Q3` base sync, not of unrecorded feature work. `SAM_Systems` and `SAM_UI` are in
  that state right now; `AGENTS.md` / `PROJECT_PROGRESS.md` arriving that way is accepted as benign and is
  filtered by the command above.

### CI / test state

All five PRs **OPEN**, all checks **green** as of 2026-08-19 — `SAM`: `build (Release)`, `test (Release)`,
`spdx`; the other four: `build`, `spdx`.

| Suite | Result | How |
|---|---|---|
| `SAM/SAM.Tests` | **1230 passed, 0 failed** (Release, matching CI) | `dotnet test SAM/SAM/SAM.Tests/SAM.Tests.csproj -c Release` |
| `SAM_Tas/SAM.Analytical.Tas.TM59.Tests` | **91 passed, 0 failed**, Debug **and** Release | build `SAM` and `SAM_Tas.sln` with **VS Framework MSBuild first** — see §6 |
| `SAM_Systems/SAM.Analytical.Systems.Tests` | **40 passed, 0 failed** | in `SAM_Systems.sln`, executed by CI |
| `SAM_Systems/SAM.Analytical.Systems.Mollier.Tests` | **123 passed, 0 failed** | — |
| `SAM_UI/WPF/SAM.Analytical.UI.WPF.Tests` | **180 passed, 0 failed** | — |
| `SAM_Tas_Grasshopper` | no test project; builds with 0 errors under VS Framework MSBuild | equivalence pinned in `SAM.Tests` |

---

## 1. Current programme state

| Stage | State |
|---|---|
| **Iteration 0** — foundation: dwelling scope → Part F → system selection → scenario → TM59 → identity-based result association | **COMPLETE** (steps 4–9; step 10, the thin headless TAS runner, is deferred and not on the critical path) |
| **Iteration 1** — BasePassive / unrestricted openings, MVRE at Part F continuous, TSD route | **IN PROGRESS** |
| **Iteration 2** — AcousticRestricted: acoustic restriction + boost + summer bypass, TSD route | **DO NOT START** until Iteration 1 is explicitly accepted |
| **Iteration 3** — CoolBreeze-class active trim cooling, full `SystemEnergyCentre` → TAS HVAC → TPD route | **DO NOT START** |

### Iteration 1 BasePassive — completed

1. **Scenarios can be created and driven.** `Query.PartOOperatingAssumptions`,
   `Create.OverheatingScenarios` and the `SAMAnalytical.CreateOverheatingScenarios` component. Before this,
   three components accepted `overheatingScenarios_` and nothing produced one, so the
   scenario-authoritative path could not be driven from Grasshopper at all.
2. **Part F airflows reach the simulation.** `Modify.ApplyPartFVentilationRates` carries the Part F sized
   airflows onto the internal conditions the simulation actually reads — `Query.CalculatedSupplyAirFlow`
   had no knowledge of `PartFSpaceData` before, so Part F numbers were reporting only. Part F becomes
   authoritative (the other supply/exhaust bases are cleared and every displacement reported); rates are
   written to a **per-space clone** of the internal condition; wet rooms get extract and an explicit zero
   supply; `MeasuredCommissioning` is refused. `SAMAnalytical.PreparePartOIteration` drives the slice and
   reads the applied supply back through the query the **simulation** uses.
3. **Identity resolves on the real run.** `SimulationSpaceMap` resolves by TAS zone guid on all 9 spaces
   of the Flat1 model, `unassociatedCount = 0`.
4. **TM59 numbers validated against native TAS**, plus a `TM59AssessmentReport` verification layer exposed
   as `Tas.TSDQueryTM59Results`'s `report` output. Evidence, and the defects this exposed:
   [`PartO-TAS-VALIDATION.md`](PartO-TAS-VALIDATION.md).

### Iteration 1 — the remaining reviewer gate

Iteration 1 is **not** accepted yet. What stands between here and acceptance:

1. **The BasePassive opening assumption does not reach the simulation input.** `Openings Restricted` is
   *stated* on the scenario and nothing writes aperture control into the TBD, so a BasePassive scenario
   asserts the model was **assessed** as base provision, not that it was **built** that way. This is §5,
   the next task.
2. **Widen the validation beyond one flat**, in particular to a plain non-bedroom `natural` criterion. See
   `PartO-TAS-VALIDATION.md` → *Remaining validation work*.
3. **Michal's confirmations** in §3 items 1 and 2 — neither is code.
4. **Two repos' reviewer findings are still open** — §3 item 8.

**Reading the completed list as "Iteration 1 is done" would be wrong.** Stating a stage does not make a
simulation obey it, and modelling the stage you state remains the modeller's job today.

---

## 2. Engineering invariants — do not silently revisit

Each of these has a test. Do not undo any without Michal's agreement.

1. **Association is identity-first at every boundary.** `SimulationSpaceMap` controls design-condition
   restore, requested-space/zone selection, scenario projection and result attribution. Stable engine key
   first, unique name only as the map's own fallback, then **refuse**. No assessment code performs a second
   name join, and **no result is ever matched by room name outside that fallback**. Every flat has a
   `Bedroom 2`.
2. **Ambiguity invalidates both directions.** Two simulated spaces claiming one design identity ⇒ neither
   forward mapping survives and the reverse mapping is refused.
3. **Whole model means every *resolvable* simulated space.** Unresolved and ambiguous spaces are excluded
   and reported; a whole-model request cannot undo an earlier identity refusal. Whole-model means **both**
   `spaces` and `zones` are null — a zones-only request stays scoped to those zones.
4. **The scenario is authoritative when supplied, and a refusal NEVER falls back.** Four distinct refusals
   with four distinct sentences: no scenario covers the space; the scenario states no strategy; two
   scenarios disagree; the strategy is outside the closed vocabulary. Falling through on refusal would
   restore the defect invisibly at the one input where nothing was said.
5. **Refuse rather than silently approximate or guess.** Refusals are **reported, not thrown**, so one
   unstated dwelling does not cost every other dwelling its assessment, and they are copies on the way out
   so a reporting layer cannot erase the record of what went unassessed.
6. **No silent disappearance of spaces or results.** The assessment drops a refused space **and reports
   it**; the official TM59 XML export **refuses the whole document**, because the external TAS TM59 tool
   cannot be told a room is missing and would produce a complete-looking answer for an incomplete building.
   "Cannot be exported" counts as a refusal too, and a null return always carries a reason.
7. **Scenario ownership is transactional.** A second scenario claiming an owned design space invalidates
   both and rolls back the first scenario's whole stored selection; the live `VentilationStrategyMap` is
   built only after ownership settles. Two scenarios deriving the **same key** are one answer said twice,
   not a collision.
8. **`MVRE` is SAM's MVHR** — verified from the template (one air-side exchanger, 0.7 sensible / 0 latent,
   no recirculation; `MV` is the same system without it). **Never add an `MVHR` identity.**
9. **Boost and summer bypass are operating states of `MVRE`, not new system identities.** `HeatRecovery` is
   likewise a capability, not an identity.
10. **The recognised ventilation vocabulary is CLOSED** — `NV MV MVRE UV EOL EOC CAV VAV DISP`. `UV` reads
    as corridor, `NV` as natural, everything else as mechanical. An open default in the authoritative path
    is the same defect pointing the other way: `"Natural"`, `"N-V"` or `"MVHR"` were once assessed
    mechanically and reported as results. Declared policy; a custom `SAM_SystemTypeLibrary` needs it
    extended. **Not** read from `Query.DefaultSystemTypeLibrary()`, which comes from `ActiveSetting` and can
    be absent.
11. **Simulation preparation works on a COPY; the original design model is unchanged.** The TPD-full route
    always modifies a copy of the TBD, and a refusal copies nothing and leaves the design TBD
    byte-identical. A `Modify` method claiming to return a copy must write onto `new Space(space)` —
    `AdjacencyCluster`'s clone **shares its `Space` objects**, which was a real aliasing bug.
12. **`SignificantRisk` is not an occupied-space TM59 failure.** `TM59ComplianceStatus` (Pass / Fail /
    NotApplicable) and `TM59RiskStatus` (Acceptable / SignificantRisk) are separate concepts on purpose. A
    full-year >28 °C result at `SignificantRisk` does not fail the occupied-space assessment.
13. **A TM59 result is never described as full Part O compliance.** The report states `TM59 occupied-space
    assessment: PASS`, never "Part O compliant" — passing temperatures do not establish that the Approved
    Document O modelling assumptions were applied. Equally, the full-year bucket is named
    `FULL-YEAR >28 C / CORRIDOR-STYLE RESULTS`, never "COMMUNAL CORRIDORS": nothing in the domain records
    whether a space landed there for having no TM59 application or for stating `UV`.
14. **The communal corridor is assessed with its own scenario and never attributed to a dwelling.**
15. **`Query.PartFDwellingZones` is the single source of truth for what a dwelling is.** Part O scope
    delegates to it and only names the remainder as common space. One stricter rule on top: an **unmarked**
    zone beside marked ones is **refused**, never called a common space.
16. **`SAM.Analytical` never references `SAM.Analytical.Tas`**, and `TM59AssessmentCalculator` must never
    learn the words TSD or TPD. Preparation differs; assessment does not. TAS owns conversion, series keys
    and provenance; `Source` is provenance only and takes no part in any identity.
17. **A scenario key is derived, never generated, never stored.** UTF-8, length-prefixed, NFC-normalised at
    the boundary, enums by name, behind the marker `OverheatingScenario:v1`. `Key_IsStableAcrossBuilds`
    pins the exact guid — regenerate **only** on a deliberate schema bump. There is no `Iteration0` member;
    the foundation stage states `Undefined`. **The four Part O assumption NAMES are permanent** — they
    participate in the key, so renaming one re-keys every scenario and orphans every attributed result.
18. **The two-pass TPD route is a required TAS workaround, not duplication.** Never remove, simplify or
    "consolidate" it into the TSD path. It is expensive on purpose.
19. **`SAM.Analytical` owns the capability vocabulary, the Part F requirement rule and suitability;
    `SAM_Systems` owns the capability VALUES, the rank and eligibility.** `SAM.Analytical` must not infer
    preference from capabilities and names no file, path or resource. `Application` is an **eligibility**
    constraint, not documentation — a Domestic request is never offered a commercial template, and rank is
    not what keeps one out.
20. **An unverified capability is never credited**, and a malformed capability index **disables selection
    rather than reordering it**.
21. **Model owns engineering data; view owns presentation only.** No flow rate, status or terminal in view
    settings; one renderer and one placement path for Part F presentation; a reflection test asserts it.
22. **A calculated failure can never be overridden into a pass.** Absence of evidence is never compliance;
    the output is an assessment, never a certificate. (Full rule in the traceability document.)

---

## 3. Open decisions and deferred work

Only genuinely unresolved items. **Items 1–7 need Michal, not code — do not decide them yourself.**

1. **The four BasePassive / AcousticRestricted assumption VALUES** — `Openings Restricted`,
   `Mechanical Ventilation At Design Rate`, `Boost Available`, `Summer Bypass Available`. Declared policy
   read off `PartOIteration`'s own stage definitions; nothing in Approved Document O was parsed. The
   **names** are permanent (§2 item 17); the values are not confirmed. **Gates Iteration 2.**
2. **Which Approved Document F operating condition `AcousticRestricted` simulates at.** Its assumptions say
   boost is *available*, not continuous, and running a whole cooling season at the Table 1.2 high rate is a
   materially **more favourable** claim. `Query.PartOIterationOperatingMode` therefore **refuses**
   `AcousticRestricted` today rather than guessing — that refusal firing is correct behaviour, not a bug.
   **This is the direct unblock for Iteration 2.** (`ActiveTrimCooling` refuses for the parallel reason:
   what it assumes about a cooling provision is unsettled.)
3. **The TPD-full transfer limitation.** The intended supply-temperature/airflow transfer is impossible in
   the TBD object model (evidence in `PartO-TAS-VALIDATION.md`). Options: accept the
   achieved-zone-temperature transfer as the documented approximation (what the code does now), raise it
   with EDSL, or change the route's shape.
4. **How boost availability is represented, and summer bypass capability.** `ContinuousVentilation`,
   `MechanicalSupply` and `Boost` are **declared** from each system type's meaning, not read from the
   templates — nothing in the files marks any of them, and each index entry says so in
   `EvidenceFromTemplate`. Confirm the values, particularly `CAV`/`DISP` boost = false. Separately, **no
   shipped template models a summer bypass** (verified against all ten), so `AcousticRestricted` states a
   bypass nothing can satisfy and Iteration 2 needs one; until then a bypass requirement is refused. The
   commercial `Boost` declarations are all `false` so they refuse rather than mis-assess.
5. **The provisional domestic `Rank` order** — `NV 10, EOL 20, EOC 30, MV 40, MVRE 50`. Recorded in the
   index as PROVISIONAL AND NOT CONFIRMED: it makes selection deterministic and claims nothing about
   engineering preference.
6. **Whether `Application` should also gate the older unguarded path.**
   `Query.DefaultSystemEnergyCentres` → `Create.SystemEnergyCentre` derives a template from a **file name**
   and matches a space's `VentilationSystemTypeName`, consulting neither the index nor `Application` — so a
   space carrying `"VAV"` still resolves `VAV.json` in a dwelling model. **Four Grasshopper components
   reach it.** Pinned by a test, not fixed; needs its own change and regression.
7. **The design-side `ZoneGuid` provenance** — preserve the guid through `Modify.UpdateIds`'s strip, or
   refuse an ambiguous name match. The simulated side is repaired; the design side is still name-assigned.
8. **Two repos' Codex/Copilot review findings are still open**, transcribed verbatim with file, line and
   rationale in the archive (archived §11s) so nothing needs re-querying. Not blocking Iteration 1.
   - `SAM_Tas_Grasshopper` **#4** (1 finding, P2) — a refused scenario map leaves stale TM59 companion XML
     beside a rewritten TBD.
   - `SAM_UI` **#75** (3 findings, two P1) — switching dwellings discards unsaved edits; a withdrawn
     confirmation is reinstated by `PersistConfirmations`; the dwelling list ignores the assessment scope so
     a saved view draws nothing.

### Still-open infrastructure defects that can affect this programme

- **`SAM.Core.Create.ParameterSet` / `TypeMap` is broken for every TAS-imported parameter whose SAM-side
  name differs from its TAS-side property name.** It reads the source property by the **SAM-side** name and
  stores under the **TAS-side** name — inverted both ways, so any registration whose two names differ
  silently produces nothing. `SpaceParameter.ZoneGuid` is now stamped explicitly instead; `ZoneNumber`,
  `Description`, `Volume` and `FloorArea` are in the same position and are **not** fixed. **The trap for
  whoever takes it:** correcting the direction starts populating parameters that are absent today, so
  anything downstream currently reading a default because the value never arrived will change answer.
  Separate SAM infrastructure task.
- **The TPD-approx identity risk** — `SystemSpaceResult.Reference` has never been shown to equal the
  TBD/TSD zone guid. Detail and the probable (unapplied) fix in `PartO-TAS-VALIDATION.md`.
- **`Modify.CalculateResultantTemperature` zero-fills** an unbounded 8760-hour read, so a part-year TPD
  puts a 0 °C setpoint into both thermostat limit profiles outside the simulated period. Pre-existing.
- **`PartFAirflowNetwork.Solve`** — a reviewer finding that is real in the abstract but whose obvious fix
  (refusing a component whose net flow is not ~zero) **broke an intentional passing regression test**,
  because supply and extract are allocated dwelling-wide while a "component" here is a physically
  disconnected sub-graph. **Do not require per-component balance**; re-derive from scratch. Reverted;
  archived §11s.
- **`PartFSchematic.AppendBranches`** — confirmed real, not fixed: at `HighBoost` a transfer record's
  direction can reverse and the one-directional branch filter drops it, so the route vanishes from the
  high-rate schematic. Visualisation only, no PASS/FAIL effect.
- **`SystemTemplate.FromJsonObject` whitespace asymmetry** — `"MV RE"` means `MVRE` through the constructor
  and `MV RE` through JSON. Worked around at both boundaries that matter; the shared path is still
  inconsistent for other consumers.
- **Anything reached through `ActiveSetting` is null on a machine with no populated `%APPDATA%\SAM`** — and
  every developer machine has one, so two defects hid behind it. `SAM.Tests` now seeds `ActiveSetting` from
  the repository's own resources through `AppContext.BaseDirectory`. **A new test needing another default
  must copy its resource in `SAM.Tests.csproj` *and* seed it in `SAMResourcesModuleInitializer`.** To
  verify properly, point the whole profile at an empty directory — moving `%APPDATA%\SAM\settings` aside is
  **not** enough, because `resources` is still found (archived §11r has the PowerShell).
- **Step 10, the thin headless TAS runner**, remains deferred and is not a replacement for either
  production acceptance path.
- Smaller pinned-not-fixed items (`"Occupant"` vs `"Occupancy"` sensible-gain spellings, the `MVRE`
  description saying "Recirculation", `MVRE`'s `Air Supply Method`, the no-weather-data
  `NullReferenceException`, the missing-series silent no-assessment, `Core.Query.ComputeHash` being ASCII,
  `"R"`/`"G17"` not being runtime-stable) are in archived §11f.

---

## 4. Validation evidence — summary

**Full record: [`PartO-TAS-VALIDATION.md`](PartO-TAS-VALIDATION.md).** What matters for ongoing engineering
decisions:

- **Flat1 BasePassive identity is validated.** `identityMode = zoneGuid` on all 9 spaces,
  `designZoneGuidRaw` equal to `simulatedZoneGuidRaw` including braces and casing, both maps complete,
  **`unassociatedCount = 0`**, `refusalCount = 0`. Read per-space from the real diagnostic JSONL, not from
  the aggregate record.
- **SAM's TM59 numbers match native TAS exactly on every applicable occupied-space criterion** —
  `Studio 1_0` Criterion 1 (37/110) and Criterion 2 (11/32), the two `Bedroom 2`s and the two kitchens on
  >26 °C. 8 of 9 spaces agree on every number that governs pass/fail.
- **The 9th space disagrees by design, not by defect.** SAM's full-year >28 °C / corridor-style check is
  occupancy-independent and TAS's canned report has no equivalent criterion — it buckets such a space
  "Other" and reports 0/0/0 as *not run*. `Corridor_1` (337/262) is therefore `SignificantRisk` in SAM and
  Pass in TAS, and **does not fail** the occupied-space assessment. `Bathroom_2`'s 2 hours against TAS's 0
  is the same difference, investigated and confirmed not a defect.
- **Validation found five real defects the test suites did not** — most importantly Criterion 1's report
  `Limit` reading the **annual** `MaxExceedableHours` instead of `MaxExceedableSummerHours` (262 shown
  where TAS's real figure is 110), invisible because every existing fixture used the same number for both
  bases; and a `Query.Simplify` argument-order bug in the non-bedroom natural-ventilation branch.
- **Current test counts are in §0.** Do not quote counts from the archive.

**Explicitly NOT validated:** a second real model; the plain non-bedroom `natural` criterion; either TPD
route; `AcousticRestricted` / `ActiveTrimCooling` (both refuse today by design); the design-side identity
provenance; and the unresolved-space refusal path on live data. **TM59 validation is not a claim of Part O
compliance.**

---

## 5. The precise next task

**Trace the existing aperture / `PartOOpeningProperties` / TAS opening-control-and-profile path, and
determine the smallest correct seam that makes the BasePassive unrestricted-opening assumption actually
affect the TAS simulation input.**

This is the counterpart, on the opening side, of what `Modify.ApplyPartFVentilationRates` already did on
the airflow side: `Openings Restricted` is stated on the scenario and reaches nothing the simulation reads.
It is also what would make `AcousticRestricted` mean anything on the input side — today that iteration is
key-distinct but behaviourally identical to BasePassive there.

**Trace, do not design.** The deliverable is a trace and a named seam, not an implementation.

**Do NOT invent a parallel opening-control framework.** The pieces already exist; the job is to find where
they already meet.

- `SAM/SAM.Analytical/Classes/OpeningProperties/PartOOpeningProperties.cs` — width, height, opening angle
  and `Factor`; an `ISingleOpeningProperties`. Referenced from `SAM.Analytical/Create/AnalyticalModel.cs`.
  **The real model's 20 apertures already carry it.**
- `SAM_Tas/SAM.Analytical.Tas/Convert/ToSAM/OpeningProperties.cs` — the read direction, already working.
- `SAM_Tas/SAM.Analytical.Tas/Modify/UpdateApertureControl.cs` — three overloads (`string path_TBD`,
  `SAMTBDDocument`, `TBDDocument`), each taking `IEnumerable<ApertureConstruction>`. The existing TBD-side
  aperture-control write path.
- `SAM_Tas/SAM.Analytical.Tas/Convert/ToTBD/Building.cs:552-571` — **a commented-out block that already
  reaches for exactly this**: `ApertureParameter.OpeningProperties`, `apertureType.GetProfile()`,
  `TBD.ProfileTypes.ticFunctionProfile`. Start by establishing why it is commented out; it is either the
  seam or the record of why that seam was rejected.
- `Modify.AssignApertureTypes` / `SetApertureType(s)` / `Modify/New/AssignOpeningTypes.cs` — the
  aperture-type assignment neighbourhood any seam has to sit consistently with.

**Constraints carried in from §2.** Whatever the seam is: it writes to a **copy**, never the design model;
it **refuses** rather than silently modelling an unstated opening regime; and an unrepresentable assumption
is reported, not approximated. `Panel.Apertures` returns **clones** — go through the `AdjacencyCluster`
setter.

**The BasePassive slice as it stands, for manual testing:** `AnalyticalModel` →
`SAMAnalytical.AddVentilationPropertiesByPartF` (or `CheckPartFCompliance`) →
`SAMAnalytical.PreparePartOIteration` (`BasePassive`) → inspect `supply m3/s` / `extract m3/s` /
`partF l/s` → `To gbXML` → `SAMAnalytical.WorkflowgbXML` (`Simulation=true`) → `Tas.TSDQueryTM59Results`
with `overheatingScenarios_` connected, `report` output read.

**Also outstanding, in parallel, not blocking:** §3 item 8 (the two repos' reviewer findings), and widening
the validation comparison per `PartO-TAS-VALIDATION.md`.

---

## 6. Working and environment rules

**Build order and toolchain**

- **Build `SAM` before `SAM_Systems` / `SAM_UI` / `SAM_Tas`** — they reference its built DLLs from
  `SAM\build\`.
- **TAS-facing projects need VS Framework MSBuild**, not the dotnet CLI (`MSB4803` on
  `ResolveComReference`):
  `C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe`.
  `SAM.Analytical`, `SAM.Tests`, `SAM.Analytical.Systems` and its test project build fine under `dotnet`.
- **`SAM.Analytical.Tas.TM59.Tests` references prebuilt DLLs from `build/`**, deliberately, because a
  `ProjectReference` would drag in the TAS COM interop. **`dotnet test` alone therefore silently tests the
  OLD code** — build `SAM.Analytical.Tas.TPD` and `SAM.Analytical.Tas.TM59` with VS MSBuild first. This
  cost a full wrong test run once, and it also means **a mutation left in `SAM.Analytical` poisons the next
  `SAM_Tas` run**.
- **Run Release, not just Debug.** CI runs Release only.
- A Grasshopper project's `dotnet build` exits non-zero on the post-build `%APPDATA%\SAM` copy when Rhino
  is absent — **check for `error CS` only**, or pass `-p:PostBuildEvent=`. Note the flip side: a live
  canvas exercises the *old* assembly until a Debug build refreshes `%APPDATA%\SAM`.
- `SAM.Analytical.Systems` has **no `<LangVersion>`**, so it compiles as **C# 7.3** — no collection
  expressions, no target-typed `new`. Its test project is C# 12.

**Language and API gotchas**

- `Panel.Apertures` returns **clones**; go through `AdjacencyCluster.SetPartFDoorTransferData(...)`.
- Use `System.Math.*` — bare `Math` collides with the `SAM.Math` namespace. `SAM.Analytical` multi-targets
  below `Dictionary.TryAdd`; use `ContainsKey` + indexer.
- After a compile error `dotnet test` can run a **stale** assembly: `dotnet build --no-incremental` then
  `dotnet test --no-build`.
- The remaining gotchas (WPF `Window` name collision, `CS1566 g.resources`, heredoc backslash mangling,
  reading the ADF PDF with `pdftotext -layout`) are in archived §9.

**Branch and PR discipline**

- Work on `feature/partf-terminal-transfer-compliance` in **all five** repos. **Never commit to
  `sow/2026-Q3` directly.**
- **Never force-push; never squash or rebase published commits.** A commit message that turns out to be
  wrong is corrected *here*, not rewritten — this file is the correcting record.
- **Do not open new PRs.** The five in §0 are open; pushing to the same branch updates them. Do not merge
  or squash unless Michal asks.
- SAM first (SAM_UI and SAM_Tas CI dep-clone it). CI green **and** the Codex inline comments read.
- **Address every valid reviewer finding regardless of authorship** — never skip one as
  pre-existing/not-my-work. Verify each against the current code rather than trusting the claim, add a
  regression test where a test project exists, and **run the full affected suite in Release before
  considering a fix done**.
- SPDX header on every changed `.cs`. Attribution line `Generated by Michal Dengusiak and CodeClaude` plus
  the `Co-Authored-By` trailer on every commit and PR body. Use the SAM implementation-summary style for
  the final response.

**Handover discipline**

- **Update this file in the same working session as the checkpoint**, then commit and push it — never
  finish a checkpoint with the handover behind the repositories, and never leave important reasoning only
  in a chat or an unpushed commit. **Bump §0's table in the same commit that lands code.**
- **Keep this file short.** New history belongs in
  [`PartF-HANDOVER-ARCHIVE.md`](PartF-HANDOVER-ARCHIVE.md), new validation evidence in
  [`PartO-TAS-VALIDATION.md`](PartO-TAS-VALIDATION.md), regulatory rules in
  [`PartF-ADF-Volume1-2021-Traceability.md`](PartF-ADF-Volume1-2021-Traceability.md). This file holds
  current state, active invariants, open questions and the next task — nothing else.
