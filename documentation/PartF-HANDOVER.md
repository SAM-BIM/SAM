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
| **Part O target architecture, iteration algorithms, what is and is not implemented** | [`PartO-ARCHITECTURE.md`](PartO-ARCHITECTURE.md) |
| Real TAS / Part O / TM59 validation evidence | [`PartO-TAS-VALIDATION.md`](PartO-TAS-VALIDATION.md) |
| Implementation history, closed findings, superseded designs | [`PartF-HANDOVER-ARCHIVE.md`](PartF-HANDOVER-ARCHIVE.md) — **history only, never state** |

**Working sequence, every checkpoint:** implement → test → commit → push → independent review/fixes → test
→ commit/push → **update this handover** → commit/push the handover → continue. A checkpoint is not
finished while this file is behind the repositories.

---

## 0. Current repository state

*Verified 2026-08-27. This table is the single authoritative record of repository state; nothing else in
this file or the archive supersedes it.*

**TWO repos are in flight.** The Part F terminal/transfer work merged everywhere; the live work is Part O.
**`sow/2026-Q3` is never committed to directly** and is untouched everywhere.

| Repo | Branch | Last CODE commit | Cut from | PR |
|---|---|---|---|---|
| `SAM` | `feature/parto-base-mvhr` | **`98d37adc`** | `sow/2026-Q3` @ `1db06e83` | not opened |
| `SAM_Tas` | `feature/parto-base-mvhr` | **`2caa2f05`** | `sow/2026-Q3` @ `1b3add6a` | not opened |

The previous Part O branches merged: `SAM` [#76](https://github.com/SAM-BIM/SAM/pull/76) and `SAM_Tas`
[#43](https://github.com/SAM-BIM/SAM_Tas/pull/43), both `feature/parto-nv-workflow`, both Iteration 1b.

**Iteration 1a is committed, licensed-accepted, and has no PR open.** Suites at these commits: `SAM`
1470/1470, `SAM_Tas` 642/642.

Read [`PartO-TAS-VALIDATION.md`](PartO-TAS-VALIDATION.md) from § *Iteration 1a / Base MVHR — the block
resolved (2026-08-27)* onwards before touching any of it. The four things that section settles, none of
which was visible from the analytical side:

1. **TAS refuses a TBD in which any ONE zone's inter-zone air movements do not balance** — reported by the
   Building Simulator as a pressure error and by SAM as a bare `Simulation Failed`. Every room of a
   balanced heat-recovery dwelling is individually out of balance, which is why Iteration 1a's first
   licensed run produced nothing. Closed by routing each room's net through `PartFAirflowNetwork` — the
   same paragraph 1.25 network the Part F door schedule is solved over — and by writing the unit's exhaust.
2. **A TBD inter-zone air movement carries MASS flow in kg/s**; SAM states air volumetrically in m³/s and
   neither type says which. The unconverted number simulated a full year without complaint, about 21 %
   under design. One seam, `Modify.UpdateIZAMProfile`, one density.
3. **The transfer network is scoped to the dwelling zones, not to the served spaces**, so a zero-terminal
   hall can carry and divide transfer air without being claimed as served.
4. `Modify.AddAirMovementObjects(AnalyticalModel)` works on a *copy* of the adjacency cluster, so no SAM
   model had ever carried a `SpaceAirMovement` into a workflow — the whole IZAM path was unexercised.

Acceptance figure: `tsdcompare` 1a against 1b gives **78 835 of 78 840** hourly temperatures differing,
where the same comparison before this work gave **0**. The Iteration 1b OPEN/NIGHT A/B is unchanged at
**16 690**.

Still open and deliberately not this branch's: `Modify.Simulate` reports a refused simulation as success;
the legacy `Create.IZAM` / `UpdateIZAMsBySpaceParameter` route is not unit-converted; MVHR **unit
selection** against the derived duty is Iteration 2.

The other three are **idle on `sow/2026-Q3` with nothing in flight**, their Part F PRs merged:
`SAM_Systems` @ `208379d` (PR #14 merged), `SAM_UI` @ `43564e6` (PR #75 merged),
`SAM_Tas_Grasshopper` @ `01508b8` (PR #4 merged). Nothing this session touched them.

**Where to start reading for the Part O work:**
[`PartO-ARCHITECTURE.md`](PartO-ARCHITECTURE.md) for what is being built and what is deliberately not,
then `SAM/PROJECT_PROGRESS.md` for this checkpoint, then
[`PartO-TAS-VALIDATION.md`](PartO-TAS-VALIDATION.md) §"Iteration 1b" for the licensed evidence.

### Superseded row corrections, kept for the record

The 2026-08-20 checkpoint recorded two corrections against the then-live
`feature/partf-terminal-transfer-compliance` rows, which that table has since outlived. Both were verified
against the repositories at the time, not assumed:

- **`SAM_Tas_Grasshopper` was a stale pin.** The table said last code `32f2763`, HEAD `f3ee1ba`. `f3ee1ba`
  (the `sow/2026-Q3` base-sync merge) is an **ancestor** of the real HEAD `ebdf0bcd`, with three code
  commits after it — `2bee815` (TM59 verification report on the TSD query component), `29c31db` (Part O
  diagnostics component GUID baseline), `ebdf0bcd` (voluntary text-file export). Nothing was lost; the pin
  had been rewritten from an older view of that repo. Local and `origin` are level at `ebdf0bcd`.
- **A `git pull --ff` landed in `SAM` mid-checkpoint** (reflog `HEAD@{1}`), from the other laptop,
  fast-forwarding `3093a977` → `6f77dbf7` — `15f4bcdd`'s transfer-air-door corrections plus its two
  documentation commits. `3093a977` remains an ancestor, nothing was rewritten, and this checkpoint's `SAM`
  commit is a clean fast-forward on top. **Every build and both suites below were re-run on the merged
  state**, not carried over from the pre-pull run. Worth knowing for next time: a working tree here can move
  under you mid-session, so re-verify §0 before committing as well as before starting.

**Why a HEAD is not pinned to a SHA.** The commit that updates this file cannot contain its own hash. The
last **code** commit is pinned instead.

### The invariant — and a descendant check is NOT enough

"HEAD descends from the recorded SHA" also passes when somebody committed code and never recorded it —
exactly the state this file exists to prevent. Verify all three:

1. every tree **clean** and every branch **level with its remote**;
2. HEAD **descends from** the recorded last-code SHA;
3. **every change between that SHA and HEAD touches only documentation.**

```bash
for r in SAM SAM_Tas; do echo "=== $r ==="; git -C $r status --porcelain; git -C $r log --oneline -1; git -C $r log --oneline -1 origin/feature/parto-base-mvhr; done
```

```bash
while read -r r sha; do git -C "$r" merge-base --is-ancestor "$sha" HEAD && echo "$r: descends from $sha" || echo "$r: DOES NOT CONTAIN $sha - STOP"; git -C "$r" diff --name-only "$sha" HEAD | grep -vE '^(documentation/PartF-HANDOVER\.md|documentation/PartF-HANDOVER-ARCHIVE\.md|documentation/PartO-TAS-VALIDATION\.md|documentation/PartO-ARCHITECTURE\.md|AGENTS\.md|PROJECT_PROGRESS\.md)$' | sed "s|^|$r UNRECORDED CODE: |"; done <<'EOF'
SAM 98d37adc
SAM_Tas 2caa2f05
EOF
```

That must print exactly **two** `descends from` lines and nothing else. The three idle repos are checked by
confirming they are still on `sow/2026-Q3` and clean; there is nothing in flight in them to lose. **Any `UNRECORDED CODE:` or
`DOES NOT CONTAIN` line means stop and reconcile before changing any code.**

**Two reconciliation lessons worth keeping** (the incidents themselves are in the archive, where the check
is recorded as having earned its keep five times):

- **A stale pin and genuinely lost work look identical from here.** That is why the rule is *stop and
  reconcile*, not *assume*. Resolve by reading the prose, confirming the commit is accounted for, and
  correcting the pin — and bump this table in the **same commit** that lands code.
- **Check for a merge before assuming lost work.** `git log -1 --format='%P' <head>` showing two parents is
  the tell of a `sow/2026-Q3` base sync, not of unrecorded feature work. `SAM_Systems` and `SAM_UI` are in
  that state right now (`SAM_Tas_Grasshopper` no longer is — its base-sync merge `f3ee1ba` has three code
  commits on top); `AGENTS.md` / `PROJECT_PROGRESS.md` arriving that way is accepted as benign and is
  filtered by the command above.
- **Verify a "missing" pin in the RIGHT repository before rewriting the row.** This file briefly recorded
  that `SAM_Tas_Grasshopper`'s pin `ebdf0bcd` "could not be resolved in any of the five repos" and rewrote
  the row to `32f2763`/`f3ee1ba`. **That was wrong and is withdrawn.** `git cat-file -t ebdf0bcd` in
  `SAM_Tas_Grasshopper` resolves it as
  `ebdf0bcd490cf8c60d875cc7500fc84deb73a5f6` — "Add voluntary text-file export to
  `Tas.TSDQueryTM59Results`" — and `git diff 32f2763 HEAD` touches
  `Grasshopper/SAM.Analytical.Grasshopper.Tas/Component/TasTSDQueryTM59Results.cs` and
  `.github/scripts/guid-baseline.json` as well as the two documentation files, so `32f2763` was never the
  last code commit. No work was lost either way; the lesson is that an abbreviated SHA only resolves in the
  repo that owns it, and a failed lookup in the wrong repo is not evidence of a typo.

### CI / test state

All five PRs **OPEN**, all checks **green** as of 2026-08-19 — `SAM`: `build (Release)`, `test (Release)`,
`spdx`; the other four: `build`, `spdx`. `SAM`'s three checks ran **green again on 2026-08-20** on the
pushed head carrying `15f4bcdd` — `build (Release)` 1m56s, `test (Release)` 2m24s, `spdx` 5s — after the
same suite was run locally in Release first (below).

| Suite | Result | How |
|---|---|---|
| `SAM/SAM.Tests` | **1329 passed, 0 failed** (Release) | `dotnet test SAM/SAM/SAM.Tests/SAM.Tests.csproj -c Release` |
| `SAM_Tas/SAM.Analytical.Tas.TM59.Tests` | **123 passed, 0 failed**, Debug **and** Release | build `SAM` and `SAM_Tas.sln` with **VS Framework MSBuild first** — see §6 |
| `SAM_Systems/SAM.Analytical.Systems.Tests` | **40 passed, 0 failed** | in `SAM_Systems.sln`, executed by CI |
| `SAM_Systems/SAM.Analytical.Systems.Mollier.Tests` | **123 passed, 0 failed** | — |
| `SAM_UI/WPF/SAM.Analytical.UI.WPF.Tests` | **182 passed, 0 failed** | — |
| `SAM_Tas_Grasshopper` | no test project; builds with 0 errors under VS Framework MSBuild | equivalence pinned in `SAM.Tests` |

**Where the two changed counts come from.** `SAM.Tests` 1289 → **1329** is **+40 from this checkpoint**
(`DailyAvailabilityScheduleTests` 23, `ProfileOpeningPropertiesTests` 9, `ProfileDailyValuesTests` 6, and
`PartOOpeningPropertiesTests` 17 → 19). `SAM_Tas` 91 → **123** is +32, all in
`OpeningScheduleResolutionTests`. **Correction to `SAM` `2c7bb26f`'s own commit message**, which is
corrected here rather than rewritten: it says "1329 … up from 1275 – 33 new DailyAvailabilitySchedule
tests … three new [PartO] cases". The 1275 baseline was stale (it was already 1289 once `15f4bcdd` landed),
and the true per-class figures are 23 and +2, not 33 and +3. The 1329 total and the 0 failures are correct.
`SAM_Tas` `2ea7b43`'s and `5923812`'s messages are accurate as written.

**CI read and green for this checkpoint.** `SAM#73` — `build (Release)` 1m59s, `test (Release)` 2m23s,
`spdx` 5s. `SAM_Tas#29` — `build` 5m34s, `spdx` 4s. No new inline review comments on either PR. **`SAM_Tas`
`5923812` (post-review fixes) is pushed and its CI has not been read yet**; `SAM` is unchanged by it.

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
   as `Tas.TSDQueryTM59Results`'s `report` output (also voluntary `_saveReport_`/`reportFilePath_` text-file
   export, off by default). Evidence, and the defects this exposed:
   [`PartO-TAS-VALIDATION.md`](PartO-TAS-VALIDATION.md).

### Part F transfer-air doors (SAM `46a40cee`, review corrections `15f4bcdd`)

**Follow-up (new work, branch `feature/partf-transfer-door-panel-selection` off `sow/2026-Q3`, SAM repo only;
uncommitted at the time of writing, to be committed after review).** Where several shared wall panels could
take the door, the host panel is now resolved by the selection hierarchy below instead of being refused.

**Real-model acceptance run (2026-08-25, against the actual `SAM_zoningAM_v1.sam`): 5 doors created, 0
refusals.** Kitchen_4→Ensuite_5 selected `fe27dac4` (the horizontal partition, direct line crosses it;
`69de3fb5` stands 0.833 m off) and Kitchen_7→Ensuite_8 selected `ab1b0798` (same shape; `b154e0b7` 0.833 m
off) - both doors land on the horizontal partitions. Studio 1_0→Bathroom_2 resolved through the FULL
fallback chain: the two candidate panels (`7e09a798` vertical, `3e01ed80` horizontal) are the two legs of
the L-shaped partition meeting at (5,5), the centroid diagonal (4.1667, 4.1667)→(7.5, 7.5) passes exactly
through the corner (geometric tie), and both legs are the SAME length (5 m) - so the stable first candidate
`3e01ed80` was selected and door `b9517704` created there. That pair is a true geometric AND dimensional
tie, resolved only by the documented final fallback; do not describe it as a geometric choice.

`SAMAnalytical.AddTransferAirDoorsByPartF` (GH) / `Modify.AddTransferAirDoorsByPartF` (SAM.Analytical) now
close the gap the Part F assessment could only report: a dwelling transfer route that carries air but has
no modelled door gets ONE default internal door created in the shared internal wall - `SIM_INT_SLD`,
760 x 2100 mm (the paragraph 1.25 reference width and the documented default height, archive §6a), bottom
of the wall, centred on the clearest length. The requirement comes from re-running the SAME
`PartFCalculator`, not a second algorithm; SAM_UI was untouched because it already delegates to it.
Existing doors are never duplicated (a rerun is a no-op); a created door carries the paragraph 1.25
requirement but **no provided undercut**, so created is not compliant (CannotBeDetermined) until a person
records it. Unresolved topology (no shared wall, wall too small, no clear length) is refused and reported,
never guessed. The aperture construction library is now seeded into `ActiveSetting` for tests per the
documented pattern.

**Review corrections (`15f4bcdd`) — two places the operation manufactured a fact the model does not carry.**

- **Several shared walls are resolved by a fixed selection hierarchy, never refused merely for being
  equal.** Every wall panel related to both spaces by guid is tested — by reading it only — for whether it
  could take the 760 × 2100 mm door where it stands, via `Query.ApertureHost`, the same host check
  `Panel.AddApertures` applies. None can: the existing refusal. Exactly one can: the door is created there.
  **More than one can: host validity first (always), then geometric relevance — the panel the segment
  joining the two space locations passes through scores 0, the others score the distance from that segment
  — then the SHORTER valid shared wall for geometric ties within `Core.Tolerance.Distance`, then the stable
  first candidate from the guid-sorted list for equal lengths within `Core.Tolerance.Distance`.** The
  previous *largest shared wall, guid as tie-break* rule was deterministic but established nothing about
  where the door belonged — wall area, room name, panel name, enumeration order and guid VALUE are all
  arbitrary with respect to that question, and none of them is consulted before the final fallback; guid
  order is the absolute last arbiter only, and it is deterministic because candidates are guid-sorted
  before ranking. A route is refused only where the candidates cannot be ranked at all — one of the two
  spaces carries no valid location (`Space.IsPlaced()` false) — never merely because candidates tie. A
  selection among several candidates is reported in `notes` with the chosen panel and the reason.
- **A missing internal-door construction is a refusal.** `Query.DefaultApertureConstruction` returning null
  no longer causes a plain `Internal Door` `ApertureConstruction` to be manufactured and noted; that put a
  door build-up into the model that nothing established, purely so geometry could exist.

25 tests in `SAM.Tests/PartFTransferAirDoorTests.cs`, including the duplicate-room-name identity regression,
the two-candidate geometric-tie resolutions (shorter wall wins **and** equal-length stable-first wins, each
**run in both panel creation orders**), a split wall whose crossed panel takes the door **in both panel
creation orders**, a split wall where the direct line hits the joint (geometric tie, equal lengths -> stable
first candidate), a two-candidate case with a missing/NaN space location (refused cleanly, no winner
manufactured), the two Codex regressions (a wall beyond the bounded segment scores a finite distance and
loses instead of NaN-refusing the route; coincident locations score against the panel REGION, not the
panel's edges), a three-wall case where the only wall that fits is the **smallest** (so a geometrically
established sole candidate cannot be mistaken for the largest wall being picked), the three-flat
example-model reproduction (Studio 1_0→Bathroom_2, Kitchen_4→Ensuite_5, Kitchen_7→Ensuite_8 — each split
wall's door lands on the crossed panel), and the construction refusal. That last test swaps an
`ActiveSetting` default, so `PartFTransferAirDoorTests` and `QuadraticScanRegressionTests` — the suite's
only two readers of the default aperture construction library — share an xUnit collection and never run at
the same time.

### Iteration 1 — the remaining reviewer gate

Iteration 1 is **not** accepted yet. What stands between here and acceptance:

1. **DONE, code-complete — and the schedule foundation under it is now first-class. Still unverified
   against a real TAS run.** `PartOOpeningProperties` gained `OpeningRestriction`
   (`Unrestricted`/`NightClosed`/`AlwaysClosed`, SAM `8bf2cf61`); `Modify.SetApertureType` writes it into
   the TBD's aperture-control profile; and `SAMAnalytical.AddOpeningPropertiesByPartO` (SAM `53aabf2f`) exposes
   `restriction_` plus `openingHour_`/`closingHour_` so a modeller can author the restriction from
   Grasshopper. `profiles_` keeps precedence over `restriction_`, warning instead of silently dropping it.
   **The GH API is unchanged by the schedule foundation** — no new user-facing input was needed.

   **What the schedule foundation changed (SAM `2c7bb26f`, SAM_Tas `2ea7b43` + `5923812`).** A real GH → TAS run
   exposed a missing abstraction: SAM had an object for a `TBD.profile` but none for the `TBD.schedule` it
   points at, so a general `Profile` was standing in for a 24-hour availability mask. That is now
   `SAM.Analytical.DailyAvailabilitySchedule` — see §2 item 23 for the naming, binary-semantics, reuse and
   precedence rules, which are invariants, not implementation detail. The same run had produced a TBD
   schedule holding 24 zeros; **the cause was not the COM write API** (`schedule.values[i] = x` and
   `schedule.set_values(i, x)` are provably the same call — see §2 item 23) but two control-flow defects,
   both now fixed and both now diagnosable: a schedule created and named *before* its source was validated
   and then silently not written, and `profile.type` being set *after* `profile.schedule`. Every failure
   now returns an actionable refusal, and the write is verified by reading all 24 values back.

   **What is still NOT covered by an automated test**, and why: the COM write itself, its assignment to a
   `TBD.profile`, and repeated-export behaviour on a real TBD. The whole resolve/reuse/name/precedence
   algorithm *is* covered (32 tests) because it deliberately names no TAS COM type; what remains needs the
   TAS interop loaded inside a project built specifically to exclude it. Same precedent as
   `TsdZoneIdentityStampTests` (see `PartO-TAS-VALIDATION.md`). The GH component's own
   input-wiring/precedence/warning behaviour is likewise untested — `SAM.Tests` references no Grasshopper
   assembly — and is covered by the manual recipe in §5 instead.

   **Next: the manual TAS acceptance run in §5.** Until it passes, the schedule foundation is code-complete
   and locally green, and nothing more.
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
    assessment: PASS`, never "Part O compliant", and prints `Part O modelling assumptions: NOT VERIFIED BY
    THIS RESULT REPORT` near the header — passing temperatures do not establish that the Approved Document
    O modelling assumptions were applied. Equally, the calculation still routes a space to the full-year
    >28 °C check for having no TM59 application **or** for stating `UV`, with nothing in the domain
    recording which reason applied — but the report now positively identifies a real communal corridor by
    its restored/resolved `InternalCondition` (`TM59_Communal Corridor (including pipework gains)`), never
    by Space name, and reports it under `COMMUNAL CORRIDOR RISK`; everything else the same calculation
    produces is reported under `SUPPLEMENTARY >28 C CHECKS` instead, and never counted towards
    `CorridorRiskStatus`.
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
23. **A TAS schedule is a first-class SAM object, and its identity is its VALUES.** Six rules, all with
    tests:
    - **`DailyAvailabilitySchedule` is the SAM counterpart of a `TBD.schedule`** — one day, exactly 24
      hourly binary values, refused at construction otherwise, never handing out its internal array. A
      `Profile` is a general sparse curve and is **not** a schedule. Do **not** grow this into weekly,
      yearly or calendar schedules, or a schedule library.
    - **Not named `DailySchedule`, on purpose.** `SAM.Analytical.Systems.DailySchedule` already exists and
      means a named collection of `ScheduleDay`s. `SAM_Systems` is downstream of `SAM.Analytical` so it is
      not reusable and this is not a duplicate abstraction, but the homonym would mislead.
    - **Binary is a declared design decision, not a proven COM constraint.** `TBD.ISchedule`'s accessor is
      `int`-typed; what makes it binary is the mechanism (the schedule selects between the profile's
      value/function and its `setbackValue`) plus the fact that no shipped SAM resource sets an aperture
      schedule at all. A general-valued curve from a user-authored TAS model therefore stays a legacy
      `Profile` and is never coerced. **The TAS seam is `int[24]`** so the legacy route keeps its exact
      previous `Convert.ToInt32` rounding.
    - **Reuse by value, create by name.** A `TBD.schedule` is building-level and shared across zones, so
      openings with different reasons for the same window share one schedule whatever each calls it.
      Name-based matching is what produced `PartO_DayOpen_08_23 (1)`, `(2)`, … Collision naming is
      `<name>_<signature>` — deterministic, never a counter — and a third collision **refuses**.
      `Signature` is the 6-hex 24-bit mask (hour 0 most significant; `00FFFE` for 08:00–23:00), computed
      arithmetically because it reaches a persisted TBD name and `GetHashCode` is not build-stable.
    - **`ProfileOpeningProperties.Schedule` beats legacy `Profile`; they are never merged.** `Profile` alone
      is the state of every model saved before this existed, so those keep behaving identically. A
      different-valued schedule already on an aperture type is **refused, never overwritten**; an
      equal-valued one is retained.
    - **`schedule.values[i] = x` and `schedule.set_values(i, x)` are the SAME COM call.** C# supports
      indexed properties only for COM interop types and lowers both to
      `TBD.ISchedule::set_values(int32, int32)` — verified by disassembling `SAM.Analytical.Tas.dll`. Never
      describe one as correct and the other as defective. There is **one** writer
      (`Modify.SetScheduleValues`), it always verifies by read-back, and **nothing is created before the
      source is validated** because `TBD.Building` has no `RemoveSchedule` — an erroneous schedule can never
      be withdrawn. `profile.schedule` is assigned **last**, after `type`/`function`/`factor`/
      `setbackValue`. A failed read-back returns **null** from both `Create.GetOrCreateSchedule` and the
      legacy `Create.Schedule`, so no caller can assign a schedule whose values do not match the model; all
      four legacy call sites (`AssignApertureTypes`, `New.AssignOpeningTypes`, Day and Night each) already
      null-check.
    - **`Modify.SetApertureType` is NOT transactional — never document it as such.** What it guarantees is
      narrower and precise: *a failed or incompatible schedule is never assigned, and an existing
      different-valued schedule is never overwritten.* It can already have created the aperture type and
      written its `description` before a refusal, and the two assignment read-back refusals necessarily come
      after the profile was written. Schedule resolution is hoisted above every profile write — it needs none
      of them — so an unusable source, a naming collision or a failed COM write leaves the profile's mode,
      factor and function untouched. **Keep it hoisted**, and do not complicate the method further merely to
      make it look transaction-like.

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

**§1 item 1 is code-complete and the schedule foundation under it has landed (SAM `2c7bb26f`, SAM_Tas
`2ea7b43`, post-review fixes in `5923812`). The one thing standing between here and Iteration 1
acceptance is the real TAS run below.**

The traced seam: `SAMAnalytical.AddOpeningPropertiesByPartO`'s `restriction_`/`openingHour_`/`closingHour_`
(SAM `53aabf2f`) → `PartOOpeningProperties.OpeningRestriction` plus its derived
**`DailyAvailabilitySchedule`** named `PartO_DayOpen_HH_HH` for `NightClosed` (SAM `2c7bb26f` — this used to
be a `Profile` pretending to be a schedule) → `Modify.SetApertureType` (SAM_Tas, the method
`WorkflowCalculator`'s "Updating Aperture Types" step already calls for every opening) →
`Create.GetOrCreateSchedule`, which validates 24 values, reuses any **value-identical** building schedule
whatever its name, otherwise creates exactly one and verifies it by reading all 24 values back →
`TBD.ApertureType` profile + schedule, schedule assigned **last**.
**`PreparePartOIteration` does not rewrite authored opening availability to make it fit an iteration.** It
REPORTS how the model's opening behaviour compares with the stage's assumption, and changes nothing. That is
a contract, not an implementation detail. Concretely (`Query.PartOIterationOpeningCompatibility`, returning
`PartOOpeningCompatibility.Compatible` / `Incompatible` / `Unknown`):

| Model vs the stage's `Openings Restricted` | What happens |
|---|---|
| agrees | continue; scenarios stated |
| demonstrably disagrees (`Incompatible`) | model preserved untouched, **warning** naming the aperture(s) and what they state; scenarios still stated |
| cannot be classified (`Unknown`) | same treatment, with a message that says *unknown* rather than *wrong* |

**Why a warning and not a refusal.** Opening restriction is authored building behaviour and is **orthogonal
to the mitigation stage** — a base case may legitimately mix `NightClosed` and `Unrestricted` openings,
because a window shut for noise or security is a fact about the building, not a mitigation anybody added.
The defect is therefore that `Openings Restricted` is asserted by the STAGE at all; it belongs on the model.
Moving it is a separate change with scenario-identity consequences (it re-keys every scenario, so it needs
an `OverheatingScenario:v2` bump) and is **not** part of this change. Blocking on an assumption that is
itself wrong would refuse exactly the models this component exists to prepare.

It briefly did the opposite — enforcing `Openings Restricted = false` by calling
`Modify.ResetPartOOpeningRestrictions` (SAM `8bf2cf61`) — and that silently defeated the acceptance gate
below: `PartOOpeningProperties.Schedule` is **derived** from `OpeningRestriction`, so resetting the
restriction deleted the `PartO_DayOpen_HH_HH` schedule from the model that reached TAS. An
`OpeningRestriction` is authored building data — `AddOpeningPropertiesByPartO` records only *that* an
opening is restricted, never *why* — so SAM cannot know whether a `NightClosed` aperture is the Part O
mitigation a stage wants stripped, an acoustic restriction, a security one, or an internal door. Preserving
the model while still stating the scenario would only move the defect: a model TAS simulates with night
closure must not be reported as `Openings Restricted = false`. So it does neither, and refuses.

**Both authoring paths are classified the same way, and an unknown is never read as unrestricted**
(`Query.PartOOpeningRestricted`, which classifies off what the TAS write will be given, not off the
authoring vocabulary). A `PartOOpeningProperties` restriction and a `ProfileOpeningProperties` carrying a
`DailyAvailabilitySchedule` are both decidable — the schedule is binary and exactly 24 hours, so "available
every hour" is unrestricted and "any hour off" is restricted, with no reverse-engineering back into a
`NightClosed` window. The legacy general-valued `ProfileOpeningProperties.Profile` is **not** decidable: the
TAS write rounds it through `Convert.ToInt32`, but those values were never authored as an availability mask,
so reading a compliance assumption off them would be inference. It is reported `Unknown` and refused. An
`IOpeningProperties` stating no availability at all (a plain `OpeningProperties`, or the profile carrier
with neither field) is positively *unrestricted*, because `TryGetOpeningScheduleSource` finds no source and
the aperture control is written without a schedule — **a new opening-properties type that CAN carry a
schedule must be added to both that query and `PartOOpeningRestricted`.**

`Modify.ResetPartOOpeningRestrictions` remains as an explicit library operation for callers who do intend
to drop the restrictions *and the schedules derived from them*. The rules that must not be silently
revisited are §2 item 23; rationale is in the commit messages and the code's own doc comments.

**Not done, and the reason matters.** No automated test exercises the real TBD/COM write, its assignment to
a `TBD.profile`, or repeated export on a real TBD — this codebase's own precedent
(`TsdZoneIdentityStampTests`, see `PartO-TAS-VALIDATION.md`) is that a line this deep in COM interop is
verified by a manual acceptance run, because exercising it needs the whole TAS interop surface loaded inside
a project built specifically to exclude it. The same applies to the GH component's own
input-wiring/precedence/warning logic (`restriction_` parsing, the `profiles_` precedence warning, the
equal-hour warning): `SAM.Tests` references no Grasshopper assembly. Both are covered by the manual recipe
below instead.

What **is** covered automatically — 40 tests in `SAM.Tests`, 32 in `SAM_Tas` — is the whole domain model and
the whole resolve/reuse/name/precedence algorithm, the latter being testable precisely because it names no
TAS COM type: 24-values-or-refused, no mutation through a returned array, JSON round trip, value equality
per differing hour, build-stable signature; legacy JSON with a `Profile` key and no `Schedule` key; the
unchanged `Convert.ToInt32` legacy conversion; reuse by value across differently named sources; naming,
collision suffix and the refusal; `Schedule`-over-`Profile` precedence; and repeated export resolving to one
schedule. Plus `ProfileDailyValuesTests`, which pins `Profile.GetDailyValues()` itself so the old all-zero
failure can never be re-attributed to it.

### The manual TAS acceptance run — the Iteration 1 gate

In Grasshopper: `SAMAnalytical.AddOpeningPropertiesByPartO` with `restriction_ = NightClosed`,
`openingHour_ = 8`, `closingHour_ = 23`, then the normal Part O/TAS workflow. `restriction_` is a plain text
input (`Param_String`) — type `NightClosed` directly; no GH enum-picker exists or is needed.

**The BasePassive slice:** `AnalyticalModel` → `SAMAnalytical.AddVentilationPropertiesByPartF` (or
`CheckPartFCompliance`) → `SAMAnalytical.AddOpeningPropertiesByPartO` (`restriction_ = NightClosed`) →
`SAMAnalytical.PreparePartOIteration` (`BasePassive`) → inspect `supply m3/s` / `extract m3/s` /
`partF l/s` → `To gbXML` → `SAMAnalytical.WorkflowgbXML` (`Simulation=true`) →
`Tas.TSDQueryTM59Results` with `overheatingScenarios_` connected, `report` output read.

`PreparePartOIteration` raises a **warning** that the `NightClosed` openings disagree with `BasePassive`'s
`Openings Restricted = false`, names the apertures, and changes nothing — the restriction and its schedule
reach the TBD intact, which is what the table below checks. That warning is the stage-asserted assumption
showing through; see the contract above.

Open the resulting TBD. The aperture's opening profile must read **exactly**:

| Field | Required value |
|---|---|
| Type | `Function` |
| Factor | `1.000` |
| Setback | `0.000` |
| Function | `zdwno,0,19.00,21.00,99.00` |
| Schedule | `PartO_DayOpen_08_23`, **or** a deliberately reused existing schedule with exactly the same 24 values |

And the Schedule database must show `00-01` … `07-08` = **0**, `08-09` … `22-23` = **1**, `23-24` = **0**
(i.e. `000000001111111111111110`, signature `00FFFE` — the active interval is half-open `[08:00, 23:00)`).

**Then repeat the export.** There must still be exactly **one** schedule for those values — no
`PartO_DayOpen_08_23 (1)`.

**Iteration 1 is NOT accepted until that run is done and correct.** A `DailyAvailabilitySchedule` existing
in SAM proves nothing about what reached the TBD; that is the whole reason this gate exists. If it fails,
the refusal message says which stage failed — unusable source, invalid length, failed creation, failed
write, read-back mismatch, lost profile assignment, or a foreign different-valued schedule — which is the
diagnostic the original investigation did not have.

**One deliberate consequence to be aware of.** Re-exporting onto an **existing** TBD whose aperture type
already carries a *different-valued* schedule (e.g. after changing `openingHour_`) **refuses** rather than
updating it, and reports which schedule blocked it. That is the "never overwrite user-authored control"
policy (§2 item 23) and matches the previous name-based behaviour; it does not arise in the normal workflow,
where the TBD is generated fresh from gbXML each export.

**Also outstanding, in parallel, not blocking:** widening the validation beyond one flat (§1 item 2), §3
item 8's two open reviewer findings, and §3 items 1–2 (Michal's confirmations, not code).

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
