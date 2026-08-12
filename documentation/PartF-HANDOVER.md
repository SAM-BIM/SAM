<!-- SPDX-License-Identifier: LGPL-3.0-or-later -->
<!-- Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors -->

# Part F — session handover

**This file is the authoritative cross-laptop continuation state.** Michal works interchangeably on two
laptops, so nothing important may live only in a chat, a terminal history or an unpushed local commit.

**Working sequence, every checkpoint:** implement → test → commit → push → independent review/fixes →
test → commit/push → **update this handover** → commit/push the handover → continue. A checkpoint is not
finished while this file is behind the repositories.

**Starting a session:** read section 0 first, then verify every repo against the branch/SHA/remote state
recorded there. **If the actual state differs, stop and reconcile before changing any code.**

---

## 0. Cross-laptop continuation state

*Last updated at SAM `f712fd9f` + SAM_Tas `7f3dfda` + SAM_Tas_Grasshopper `b8dae4b` — **Iteration 0 step 8
complete and independently reviewed**: simulation results, scenarios and design spaces are associated by
identity, and both existing TAS acceptance paths accept the scenario/identity architecture.*

### 0a. Repository state — verify this before touching anything

**FIVE repos are now in the workstream.** `SAM_Systems` joined at step 5b; **`SAM_Tas_Grasshopper` joined at
step 7a**, with Michal's approval, to repoint `Tas.TSDQueryTM59Results` at the step 6 service.

| Repo | Branch | Last CODE commit | HEAD should be | Tree | Cut from |
|---|---|---|---|---|---|
| `SAM` | `feature/partf-terminal-transfer-compliance` | **`f712fd9f`** | that, **plus the handover commit(s) on top** | clean, level | `sow/2026-Q3` @ `34dea440` |
| `SAM_Systems` | `feature/partf-terminal-transfer-compliance` | **`bff125f`** | exactly `bff125f` | clean, level | `sow/2026-Q3` @ `d7303c2` |
| `SAM_UI` | `feature/partf-terminal-transfer-compliance` | **`ffd8e38`** | exactly `ffd8e38` | clean, level | `sow/2026-Q3` @ `074f3d9` |
| `SAM_Tas` | `feature/partf-terminal-transfer-compliance` | **`7f3dfda`** | exactly `7f3dfda` | clean, level | `sow/2026-Q3` @ `3d58bfe` |
| `SAM_Tas_Grasshopper` | `feature/partf-terminal-transfer-compliance` | **`b8dae4b`** | exactly `b8dae4b` | clean, level | `sow/2026-Q3` @ `9555aa1` |

**One older wrinkle in `SAM`'s history, recorded rather than rewritten.** The step 7 review-fix commit
`d2b0f971` also carries a partial update of this file, because the handover edit was in progress when it
was staged. Nothing was force-pushed and nothing rewritten. It is historical now: the invariant below is
anchored at the later, code-only step 8 review commit `f712fd9f`.

**Why a HEAD is not pinned to a SHA.** The commit that updates this file cannot contain its own hash, so
a pinned HEAD would be wrong the moment it landed. The last **code** commit is pinned instead.

**The invariant, and a descendant check is NOT enough.** "HEAD descends from the recorded SHA" also
passes when somebody committed code and never recorded it — exactly the state this file exists to
prevent. So verify all three of:

1. every tree **clean** and every branch **level with its remote**;
2. HEAD **descends from** the recorded last-code SHA;
3. **every change between that SHA and HEAD touches only `documentation/PartF-HANDOVER.md`.**

Anything else — a dirty tree, an unpushed commit, a divergence, or unrecorded code between the
checkpoint and HEAD — means **stop and reconcile before changing any code**.

```bash
for r in SAM SAM_Systems SAM_UI SAM_Tas SAM_Tas_Grasshopper; do echo "=== $r ==="; git -C $r status --porcelain; git -C $r log --oneline -1; git -C $r log --oneline -1 origin/feature/partf-terminal-transfer-compliance; done
```

```bash
while read -r r sha; do git -C "$r" merge-base --is-ancestor "$sha" HEAD && echo "$r: descends from $sha" || echo "$r: DOES NOT CONTAIN $sha - STOP"; git -C "$r" diff --name-only "$sha" HEAD | grep -v '^documentation/PartF-HANDOVER\.md$' | sed "s|^|$r UNRECORDED CODE: |"; done <<'EOF'
SAM f712fd9f
SAM_Systems bff125f
SAM_UI ffd8e38
SAM_Tas 7f3dfda
SAM_Tas_Grasshopper b8dae4b
EOF
```

That must print exactly **five** `descends from` lines and nothing else. **Any `UNRECORDED CODE:` or
`DOES NOT CONTAIN` line means stop and reconcile.** (An earlier version of this check passed a literal
`PENDING` for a repo that had no SHA yet and swallowed the error, so it silently verified nothing —
the loop above fails loudly instead.)

Not merged. **No PRs open in any of the five.** `sow/2026-Q3` never committed to directly, untouched
everywhere. **`SAM_Tas`'s, `SAM_Systems`'s and now `SAM_Tas_Grasshopper`'s branches are new and need PRs
like the other two** — still waiting on Michal.

### 0b. Latest checkpoint — what it implemented

**Iteration 0 step 8 — identity-based association, complete and independently reviewed. Detail in 11n.**

- SAM `d7abd48a` + SAM_Tas `1aba5eec` + SAM_Tas_Grasshopper `f8ca646c` → the original step 8
  implementation: `SimulationSpaceMap` applied to restore, selection and result association; TAS stable
  keys; and the normal `Tas.TSDQueryTM59Results` path stopped matching by name.
- SAM `f712fd9f` + SAM_Tas `7f3dfda` + SAM_Tas_Grasshopper `b8dae4b` → independent-review fixes:
  fail-safe bidirectional collision handling, resolved-only whole-model/zone selection, transactional
  scenario ownership, both existing TAS workflows wired, and acceptance coverage for three flats each
  containing a room named exactly `Bedroom 2`, plus the communal corridor.
- The new scenario inputs are **optional and appended after existing Grasshopper inputs**, preserving saved
  component port indices and the two existing production workflows. When scenarios are supplied they are
  authoritative; when absent the documented legacy fallback remains available.

**Previous checkpoint — Iteration 0 step 7, scenario-authoritative ventilation strategy. Detail in 11l.**

- SAM `193968ff` + SAM_Tas `f6e32b4` + SAM_Tas_Grasshopper `047c583` → **7a**, the behaviour-preserving
  repoint. `TM59AssessmentCalculator.SourceFallback` so provenance survives it,
  `Create.TM59AssessmentCalculator` keeping TAS's two series keys and provenance in TAS's assembly, and
  `Tas.TSDQueryTM59Results` calling the service. **The TM59 recipe now exists once.**
- SAM `f6772519` → **7b**, `VentilationStrategyMap` + `VentilationStrategySelection`. The criterion is
  stated by the scenario, and refused where nothing states it.
- SAM_Tas `b03f02b` → **7c**, the TM59 **XML export** takes the strategy from the scenario too, through
  `Space.ToTM59`'s existing `systemType` seam.
- SAM `d2b0f971` + SAM_Tas `121035c` → **review fixes**, including two real defects: an unrecognised
  strategy silently assessed as mechanical, and the export still dropping unexportable rooms silently.
  See 0d.

**Previous checkpoint — Iteration 0 step 5 (5a, 5b, 5c) and step 6.**

- SAM `b23bef3b` → **5a**: the analytical half — `SystemCapability`, `SystemCapabilityRequirement`,
  `SystemCapabilityDescriptor`, `SystemCapabilitySelection`, `Query.PartFSystemCapabilityRequirement`,
  `Query.CapableSystems`, `Query.SelectPreferredCapableSystem`.
- SAM `c5112e4f` → preference taken out of `SAM.Analytical` (Michal's correction), handover invariant
  strengthened.
- SAM_Systems `c6a441d` → **5b**: `CapabilityIndex.JSON` beside the ten templates,
  `Query.SystemCapabilityDescriptors`, `Query.SystemEnergyCentreResource`,
  `Query.SystemEnergyCentre(SystemTemplate)`, and the conformance test project.
- SAM `b52aff65` + SAM_Systems `e99f311` → review fixes, including a **real misselection** (see 0d).
- SAM_Systems `895a86d` → **5c**: `Application` becomes an eligibility constraint, rank recorded as
  provisional declared policy, commercial `Boost` recorded as unverified, CI executes the suite.
- SAM_Systems `bff125f` → 5c review fixes, including a **CI assembly-version restamp** and an unverified
  capability being credited (see 11j).

- SAM `d327496a` → **step 6**: `TM59AssessmentCalculator` / `TM59AssessmentResult` — the
  `Tas.TSDQueryTM59Results` recipe lifted into `SAM.Analytical`, behaviour unchanged.

Detail in **11h** (5a), **11i** (5b), **11j** (5c) and **11k** (6).

### 0c. Tests and builds run, with counts

| Suite | Result | How |
|---|---|---|
| `SAM/SAM.Tests` | **1183 passed, 0 failed** | `dotnet test SAM/SAM/SAM.Tests/SAM.Tests.csproj` |
| `SAM_Tas/SAM.Analytical.Tas.TM59.Tests` | **46 passed, 0 failed** | `dotnet test SAM_Tas/SAM_Tas/SAM.Analytical.Tas.TM59.Tests/…csproj` — **build SAM and the TM59 library first** |
| `SAM_Tas_Grasshopper/SAM.Analytical.Grasshopper.Tas` | 0 `error CS` under **VS Framework MSBuild** | no test project in that repo; equivalence is pinned in `SAM.Tests` |
| `SAM_Systems/SAM.Analytical.Systems.Tests` | **34 passed, 0 failed** | unchanged by step 7 |
| `SAM_Systems/SAM.Analytical.Systems.Mollier.Tests` | **123 passed, 0 failed** | unchanged by step 7 |
| `SAM_Systems.sln` | 0 `error` under VS MSBuild | the test project is in the solution AND executed by CI |
| `SAM_UI`, `SAM_Mollier` | **unchanged since their last run** — see section 2 | not re-run; nothing in them changed during step 8 |

**Step 8 mutation checks all behaved correctly** (the named test failed under the mutation, then passed
after restore):

1. restoring simulated/design conditions by name instead of identity → the mandatory duplicate-`Bedroom 2`
   regressions fail;
2. bypassing the whole-model resolved-identity filter →
   `TwoCandidateMappingsForOneDesignIdentity_RefuseExplicitly` fails;
3. treating `spaces == null` as whole-model even when zones were supplied →
   `Zones_ContributeTheirSpacesByIdentityWithoutDuplicating` fails;
4. publishing ventilation claims while scenario ownership was still being accumulated →
   `TwoScenariosOverOneSpace_RefuseExplicitly` fails.

**The earlier five step 7 mutation checks also remain recorded and green after restore:**

1. making a refusal fall back to `SystemTypeName` → fails **4** tests;
2. removing the strategy normalisation → fails `UV_StillRoutesToTheCorridorCriterion`;
3. returning the `Building` regardless of refusals → fails `AnUnsettledStrategy_RefusesTheWholeExport`;
4. disabling the closed ventilation vocabulary → fails **5** tests;
5. disabling the export's dropped-zone guard → fails `ASpaceThatCannotBeExported_RefusesRatherThanVanishing`
   and `ANullManager_RefusesRatherThanExportingNothing`.

**A trap worth remembering, hit during mutation 5.** The `SAM_Tas` test project references
`SAM/build/SAM.Analytical.dll`, so a mutation left in `SAM.Analytical` and not rebuilt away silently
poisons the *next* suite you run. It looked like a third test failing for the export guard; it was the
previous mutation still in the built DLL. **Rebuild SAM before every SAM_Tas run**, mutation testing
included.

### 0d. Architectural decisions made in this checkpoint (step 8)

1. **Association is identity-first at every boundary.** `SimulationSpaceMap` now controls design-condition
   restore, requested-space/zone selection, scenario projection and result attribution. Names are only the
   map's unique fallback; no assessment code independently joins by name.
2. **Ambiguity invalidates both directions.** If two simulated spaces claim one design identity, neither
   forward mapping survives and the reverse mapping is refused. Keeping either candidate usable would let
   the ambiguous pair receive a design internal condition or scenario.
3. **Whole model means every resolvable simulated space, not every simulated space.** Ambiguous or unresolved
   spaces are excluded and reported. A whole-model request cannot undo an earlier identity refusal.
4. **A zones-only request is scoped to those zones.** `spaces == null` is not whole-model when `zones` is
   supplied; zone-contributed spaces are resolved and de-duplicated by identity.
5. **Scenario ownership is transactional.** A second scenario claiming an already-owned design space
   invalidates both scenarios for that space and rolls back the first scenario's complete stored selection.
   The live `VentilationStrategyMap` is built only after ownership is settled, so a refused scenario leaves
   no stale strategy claim behind.
6. **The normal TAS route uses TAS zone identity.** `Tas.TSDQueryTM59Results` builds a
   `SimulationSpaceMap` from each TAS zone guid. Unique-name fallback remains only where a stable key cannot
   be recovered, and duplicate names refuse rather than cross flats.
7. **The official TAS print route uses design identity.** `SAMAnalytical.CreateTBDByTM59` starts from the
   design spaces themselves, so its map is identity. The resulting XML/TBD/TSD continues into the existing
   TAS TM59 tabs and print workflow; no substitute user workflow was introduced.
8. **Both scenario inputs are optional and appended.** Existing Grasshopper input indices and saved
   definitions are preserved. Supplied scenarios are authoritative and use the new identity architecture;
   absent scenarios retain the previously documented fallback path.
9. **Assessment and export keep different refusal contracts.** Assessment returns the valid partial result
   set plus refusals. Official XML export refuses the whole document when any requested scenario association
   is incomplete, because TAS cannot display the omitted room as a refusal.
10. **The communal corridor remains common space.** It is mapped and assessed with its own scenario; it is
    never attributed to one of the three dwellings.

### 0d-prev7. Architectural decisions from the step 7 checkpoint

1. **The scenario is authoritative, and a refusal never falls back.** Where a `VentilationStrategyMap` is
   supplied, none of the three derivations in 11d is consulted — not as a seed, not as a tie-break. Falling
   through on refusal would restore the defect invisibly, at the one input where nothing was said.
2. **Three refusals, three sentences.** "No scenario covers this space", "the scenario states no strategy",
   "two scenarios disagree" and now "the strategy stated is not one I have a criterion for" are different
   mistakes with different fixes. Collapsing them would send somebody looking in the wrong place.
3. **The recognised ventilation vocabulary is CLOSED** — `NV MV MVRE UV EOL EOC CAV VAV DISP`. The criterion
   selection reads `UV` as corridor, `NV` as natural and *everything else* as mechanical, and an open default
   in the authoritative path is the step 7 defect pointing the other way: a scenario stating `"Natural"`, or
   `"N-V"`, or `"MVHR"` — **a name that does not exist, because `MVRE` is SAM's heat-recovery ventilation** —
   was assessed mechanically and reported as a result. Found by review. **The set is declared policy, like
   `SAM_Systems`' rank, and a project with a custom system-type library needs it extended.**
4. **The vocabulary is NOT read from `Query.DefaultSystemTypeLibrary()`.** That comes from `ActiveSetting` and
   can be absent, and making the authoritative path depend on the same installed resource the *defective*
   derivation used would trade one silent failure for another. A list of names is vocabulary and belongs in
   `SAM.Analytical`, which already names `UV` and `NV` in `Query.IsMechanicalVentilation`.
5. **The assessment drops a refused space; the export refuses the whole document.** Asymmetric on purpose. A
   result list is naturally partial and `TM59AssessmentResult` reports the gaps. A TM59 XML is configuration
   for the external TAS TM59 tool, which cannot be told a room is missing — it would assess what it was given
   and produce a complete-looking answer for an incomplete building.
6. **"Cannot be exported" counts as a refusal too.** `Space.ToTM59` returns null for a space with no internal
   condition, and for a null `TM59Manager`. Found by review: those were dropped silently and the completeness
   gate still passed, so a three-space building shipped two zones as a success.
7. **A null return always carries a reason.** Found by review: with a map supplied and nothing to export, the
   export returned null with an *empty* refusal list, so the documented contract lied.
8. **Provenance and routing never touch the criterion.** Not `Source`, not TSD-versus-TPD, not which engine
   wrote the numbers. Tested on both routes.
9. **The map is live and held by reference; the scenario is copied.** A map is built up scenario by scenario
   and is not an identity, so it is not copied in — the opposite of `OverheatingScenario`, deliberately, and
   now stated and tested rather than left to be discovered.
10. **Refusals are reported, not thrown**, so one unstated dwelling does not cost every other dwelling in the
    building its assessment. And they are a **copy** on both types — a reporting layer that de-duplicates in
    place was able to erase the record of which dwellings went unassessed.
11. **`Calculate_TM52` deliberately does not clear the TM59 refusals.** TM52 selects no criterion, so
    clearing them would let a TM52 run erase a TM59 run's record.

### 0d-prev5. Architectural decisions from the step 5 checkpoint

1. **`SAM.Analytical` owns the capability vocabulary + the Part F requirement rule + suitability +
   preference-by-supplied-rank. `SAM_Systems` owns the capability VALUES and the rank.** Michal's
   decision. Same cut as TAS: analytical states intent, the specialised assembly owns implementation.
2. **`SAM.Analytical` must NOT infer preference from capabilities.** An earlier revision chose the
   system with the fewest capabilities. That is a policy about a particular library, not something that
   follows from Part F — a capability a system happens to have may cost nothing to specify. Preference
   is now `SystemCapabilityDescriptor.Rank`, supplied by the catalogue and never derived.
   `Query.CapableSystems` (suitability) is separate from `Query.SelectPreferredCapableSystem` (choice).
3. **`SystemCapability.MechanicalSupply` exists because of a real misselection the review found.** A
   balanced dwelling — paragraph 1.67, a supply terminal in every habitable room — was met by
   `Local Extract Only`, because extract-only does run continuously and can boost. The overheating
   simulation would have run a system with no supply and no heat recovery against a building with both.
   `TotalSupply_Lps` is the indicator: those terminals carry `IsInBalancedFlow`.
4. **`HeatRecovery` is a capability, not an identity.** `MVRE` remains SAM's heat-recovery ventilation.
5. **Part F requires continuous ventilation, mechanical supply where supply was sized, and boost —
   never summer bypass or heat recovery.** Those are Part O mitigation a scenario states.
6. **Refuse, never approximate, and refuse rather than guess.** No capable system ⇒ refusal naming what
   was missing. Two *different* suitable systems at the same rank ⇒ refusal (the catalogue has not said
   which is preferred). One system listed twice ⇒ not ambiguous, it is selected.
7. **A malformed capability index disables selection rather than reordering it.** A missing or mistyped
   `Rank`, or a present-but-unusable ventilation identity, refuses the whole index.
8. **`Application` is an ELIGIBILITY constraint owned by `SAM_Systems`, not documentation.** A Domestic
   request is never offered a commercial template, and rank is no longer what keeps one out — see 11j.
   The constraint parameter is **not optional**, so the commercial-inclusive call cannot be written by
   omission.
9. **The eligibility guarantee is scoped to capability selection.** The older
   `DefaultSystemEnergyCentres`/`Create.SystemEnergyCentre` path bypasses the index entirely and is
   unguarded — recorded, pinned by a test, not changed.
10. **An unverified capability is never credited.** Every capability an index entry lists under
   `UnverifiedDeclarations` must be `false`, asserted.
11. **`SystemCapabilityDescriptor` is not an `IJSAMObject`.** It had a second, incompatible JSON shape
   from the one the catalogue actually uses, and its own reader turned a real index entry into a
   confident empty descriptor. The index is the wire format.

### 0e. Explicitly deferred

- ~~CI runs no tests in `SAM_Systems`~~ — **CLOSED in 5c.** `build.yml` now executes
  `SAM.Analytical.Systems.Tests` after the ordered Rebuild. **Not yet demonstrated on a real run**: the
  workflow triggers on `master`/`main`/`sow/**` only, so it will first fire when a PR to `sow/2026-Q3`
  is opened. Other SAM repos still have the original gap.
- **The domestic `Rank` order is PROVISIONAL and not confirmed** — `NV 10, EOL 20, EOC 30, MV 40,
  MVRE 50`. It makes selection deterministic and claims nothing about engineering preference. Michal to
  confirm or replace. Commercial ranks now order commercial-to-commercial selection only.
- **`ContinuousVentilation`, `MechanicalSupply` and `Boost` are DECLARED** from each system type's
  meaning, not read from the templates — nothing in the files marks any of them. Each index entry says
  so in `EvidenceFromTemplate`. Michal to confirm the values, particularly `CAV`/`DISP` boost = false.
- ~~`Application` is documentation only~~ — **CLOSED in 5c**, it is now an eligibility constraint (11j).
- ~~The `Tas.TSDQueryTM59Results` Grasshopper component still holds its own copy of the step 6 recipe.~~
  **CLOSED in step 7a.** `SAM_Tas_Grasshopper` is the fifth repo and the component calls the service.
- **`TasTPDQueryTM59Results` still holds a THIRD inline copy of the recipe**, and repointing it is **step 9,
  not a drive-by** — its middle stage is the two-pass TAS workaround that must be preserved. See **11m**,
  which is authoritative on that boundary.
- ~~No production caller can supply a `VentilationStrategyMap`.~~ **CLOSED in step 8.** Both existing
  Grasshopper entry points accept optional `OverheatingScenario` inputs and construct the identity-aware
  scenario/ventilation maps. Supplying scenarios takes the authoritative path; omitting them preserves the
  existing workflow and its documented legacy derivations. The thin headless runner remains step 10; it is
  not a replacement for either production acceptance path.
- **The closed ventilation vocabulary is declared policy and needs Michal's confirmation** — the nine
  identities in `VentilationStrategyMap`. A project shipping a custom `SAM_SystemTypeLibrary` would have its
  extra identities refused.
- **The `Create.SystemEnergyCentre` / `DefaultSystemEnergyCentres` path is UNGUARDED** and is the one with
  production callers (four Grasshopper components). It derives a template from a file NAME and matches a
  space's `VentilationSystemTypeName`, so a space carrying `"VAV"` still resolves `VAV.json` in a dwelling
  model. Pinned by a test, not fixed — **needs its own change and regression**.
- **The commercial `Boost` values are unverified declarations**, all `false` so they refuse rather than
  mis-assess. Not to be relied on until confirmed; nothing in Part F work turns on them, since commercial
  templates are ineligible for a dwelling.
- **No shipped template models a summer bypass**, so Iteration 2 needs one; until then a bypass
  requirement is refused. Verified against all ten files.
- **No template self-identifies its ventilation type.** The identity↔file link is only the index's
  `Resource` string, so swapping two templates' contents is undetectable.
- `SystemTemplate.FromJsonObject` whitespace asymmetry — 11f item 7. Worked around at both boundaries
  that matter; the shared path is still inconsistent for other consumers.
- Everything in 11f items 1–5.

### 0f. The precise next task

**Step 9 — preserve and make explicit the TSD-simple vs TPD-full preparation boundary. Read 11m in full
before touching either implementation.** `TM59AssessmentCalculator` must remain engine-neutral and must not
learn the words TSD or TPD.

Step 9 cannot be implemented safely until Michal resolves the three discrepancies in 11m:

1. whether the intended second pass evolves mechanism A or describes a different supply-temperature and
   airflow injection;
2. which of mechanism A and mechanism B is the authoritative TPD-full path;
3. where B's preparation boundary settles before its inline TM59 recipe is repointed.

Do not guess. Do not redesign `CalculateResultantTemperature.cs` or `TasTPDQueryTM59Results` while waiting.
After step 9: step 10, the thin headless TAS runner, last.

### 0g. Environment needed to continue

- **TAS-facing projects need VS Framework MSBuild**, not the dotnet CLI (MSB4803 / `ResolveComReference`):
  `C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe`.
  `SAM.Analytical`, `SAM.Tests`, `SAM.Analytical.Systems` and its test project build fine under `dotnet`.
- Build **SAM before SAM_Systems / SAM_UI / SAM_Tas** — they reference its built DLLs from `SAM\build\`.
- `SAM.Analytical.Systems` has **no `<LangVersion>`**, so it compiles as **C# 7.3** — no collection
  expressions, no target-typed `new`. Its test project is C# 12.
- The conformance test reads the repository's own resources by walking up for the ancestor holding both
  `files/resources/…/SystemEnergyCentre` **and** `SAM_Systems/SAM.Analytical.Systems`. It therefore only
  runs from a source checkout, and it deliberately does **not** accept `build_tests/net8.0/`, where the
  Mollier tests copy two templates.
- Section 9 has the rest of the gotchas.

---

## 1. Where the work is

**COMMITTED AND PUSHED in all FIVE repos. Not merged, no PR opened.** All working trees are clean and
level with their remotes. Do not merge, squash, force-push, or open a PR unless Michal asks.

All five repos are on `feature/partf-terminal-transfer-compliance`. **SAM_Tas's, SAM_Systems's and
SAM_Tas_Grasshopper's branches are new and need PRs like the other two.** `sow/2026-Q3` was never committed to directly and is
untouched everywhere (SAM_Tas verified at `3d58bfe` local and remote).

**Current code heads — SAM `f712fd9f`+handover, SAM_Systems `bff125f`, SAM_UI `ffd8e38`, SAM_Tas
`7f3dfda`, SAM_Tas_Grasshopper `b8dae4b`. All pushed and verified. See section 0a, which is
authoritative.**

Note for review: `ffd8e38` (SAM_UI) is the Part F dwelling-scope/cache work and is a *different review
topic* from the two Iteration 0 commits in SAM/SAM_Tas — worth looking at separately.

- **SAM**: `feature/partf-terminal-transfer-compliance`, on `sow/2026-Q3` @ `34dea440`.
  - `cd54a62b` shared `Solver2D` hardening + `Solver2DTests`
  - `ae921be4` the Part F analytical body (correction pass, new classes/enums/tests, 2 GH components, rule set, docs)
  - `92c685e0`, `6cefcc08`, `0959b256`, `6148b554` handover
  - `b0e72a21` `AdjacencyCluster.PartFDwellingZoneCategories()`
  - `c5ca006e` the dwelling-selection policy given a single home (see 6b)
  - `cdd36e36` record the pushed checkpoint SHAs in this handover
  - `17a881e8` handover after the saved-view correctness fixes
  - `060feeda` **Part O scope + `SimulationSpaceMap`** (see 11c)
  - `9cbf308a` **`TMOverheatingCalculator` extraction** (see 11c)
  - `2e2362f7` doc terminology
  - `52698867` record the Iteration 0 state in this handover
  - `7faf964c` **`OverheatingScenario` + derived deterministic key** (step 4, see 11c)
  - `02e99582` **step 4 hardened after an independent review** (see 11c)
  - `5d4274a5` handover after step 4
  - `884a2f54` **step 4.1 canonicalisation** — assumption names normalised before sorting, JSON primitives through the typed canonicaliser (see 11c)
  - `342dfb26` handover after step 4.1
  - `b23bef3b` **step 5a — analytical half of system capability selection** (see 11h)
  - `b4d5aaa4`, `27ef1730` handover: section 0 and the repo-state invariant
  - `c5112e4f` **preference taken out of `SAM.Analytical`** + the stronger invariant (Michal's review)
  - `b52aff65` **`MechanicalSupply` + selection hardening** (review of 5a/5b, see 11h)
  - `f712fd9f` **step 8 independent-review fixes** — last CODE commit, pushed; HEAD is the handover commit on top
- **SAM_UI**: `feature/partf-terminal-transfer-compliance`, on `sow/2026-Q3` @ `074f3d9`.
  - `e787105` shared 2D-view infrastructure (`FloorPlan2DControl.Overlay`/`Plane`/`WorldToScreen`/`ViewChanged`,
    `AdjacencyCluster.SpaceSectionFace2Ds`, label-solver diagnostic reading `ResultType`)
  - `8782c40` Part F presentation checkpoint (window, overlay, view settings, placement adapter, annotation
    identity, all tests). One commit on purpose: the correction-pass fixes, the persistence types and the
    placement adapter are mutually dependent and the window is a single new file containing all three.
  - `efaed83` Part F airflow on the normal saved 2D views (the one renderer)
  - `150ea63` the new-view preset
  - `31d96cc` test-doc follow-up — last pushed commit
  - `ffd8e38` **dwelling scope + cache safety** (sections 6b, 6c) — `PartFDwellingScope`,
    `PartFAssessmentCache`, `PartFAssessmentCacheTests` new; the preset, the view settings, the dialog, the
    scope-gate in `AnalyticalWindow.PartF.cs` and the button text changed
  - **HEAD = `ffd8e38`**, pushed
- **SAM_Tas**: `feature/partf-terminal-transfer-compliance` (**new this session**), off `sow/2026-Q3` @ `3d58bfe`.
  - `5e38c94` `OverheatingCalculator` reduced to a compatibility wrapper + 3 equivalence tests (see 11c)
  - `d56f679` doc terminology
  - `1aba5eec` original step 8 TAS identity mapping
  - `7f3dfda` step 8 review acceptance coverage for both TAS paths
  - **HEAD = `7f3dfda`**, pushed
- **SAM_Systems**: **HEAD = `bff125f`**, pushed; unchanged by step 8.
- **SAM_Tas_Grasshopper**: `f8ca646c` original step 8 result association; `b8dae4b` review wiring for
  both existing workflows; **HEAD = `b8dae4b`**, pushed.

## 2. Validation state (all green at handover)

| Suite | Result |
|---|---|
| `SAM/SAM.Tests` | **1183 passed, 0 failed** |
| `SAM_UI/WPF/SAM.Analytical.UI.WPF.Tests` | **180 passed, 0 failed** (123 + 21 placement + 7 identity + 6 whole-floor + 17 preset/scope + 6 assessment cache) |
| `SAM_Systems/SAM.Analytical.Systems.Mollier.Tests` | **123 passed, 0 failed** |
| `SAM_Systems/SAM.Analytical.Systems.Tests` | **34 passed, 0 failed** — capability index conformance + eligibility |
| `SAM_Mollier/SAM.Core.Mollier.Tests` | **22 passed, 0 failed** |
| `SAM.Core.Mollier.UI.WPF` | 0 `error CS` — builds unchanged against the hardened engine |
| `SAM.Analytical.Grasshopper` | 0 `error CS` under VS MSBuild |
| `SAM_Tas/SAM.Analytical.Tas.TM59.Tests` | **46 passed, 0 failed** |
| `SAM_Tas_Grasshopper/SAM.Analytical.Grasshopper.Tas` | 0 `error CS` under VS Framework MSBuild — no test project in that repo |
| SPDX | present on every changed `.cs` |

`dotnet build` on the Grasshopper project exits non-zero on a post-build `%APPDATA%` copy (no Rhino
installed). **Check for `error CS` only.** Same for MSB4803 on SAM_Tas_Grasshopper.

## 3. What is DONE

### 3a. Regulatory correction pass (reviewed and approved, 8/8 items)

1. **The continuous design rate no longer includes the sum of Table 1.2 high rates.**
   `ContinuousDesignSystemRate_Lps = max(BedroomOrHabitableRate, 0.3 × InternalFloorArea)` and nothing
   else. New `AllocateContinuousExtractBelowMinimumTotal` handles the case where the per-room minima
   total *more* than that: share the whole-dwelling rate pro-rata, each room boosts to its own figure.
2. Per-room check renamed **"Each extract room reaches its Table 1.2 minimum high rate"**; the
   whole-dwelling check states the per-room sum does not raise it.
3. **Required / proposed / provided split** on the terminal: `RequiredHighFlowRate_Lps`,
   `ProposedExtractMethod`, `ProvidedExtractMethod` (new `PartFExtractMethod.NotSpecified`),
   `ProvisionStatus`. An unrecorded kitchen terminal is sized but never counted as provided.
4. **Calculated failures cannot be overridden.** `CalculatedStatus` + `FinalAssessmentStatus` +
   `UserEvidence` / `AlternativeComplianceMethod` / `OverrideReason` / `ConfirmedBy` /
   `ConfirmationDate`, and `ApplyUserResolution` redirects a claimed pass to
   `AlternativeSolutionPendingApproval` or `EngineeringReviewRequired`. `IsCalculatedFailureOverstated`
   catches a `Status` assigned from outside that method; `Resolve()` keeps the dwelling at **Fail**
   unless an alternative method was recorded.
5. Door l/s relabelled **calculated routing** everywhere (schedule "INTERNAL TRANSFER AIR ROUTING
   (CALCULATED)", columns "Calculated …").
6. Report + `PartF-ADF-Volume1-2021-Traceability.md` corrected; the old third governing term is
   recorded as removed.
7. Tests updated/renamed where they asserted the removed rule.
8. Six follow-up fixes: floor-plan selection bug, schematic/plan obeying `OpeningStatus`, supply
   high/boost wording, "Sizing method" rename, allocation-note rewrite, label placement.

### 3b. Floor-plan overlay (replaces the old node diagram, which is DELETED)

- `FloorPlan2DControl` gained the one minimal shared hook: screen-space `Overlay`, `Plane`,
  `WorldToScreen`, `ViewChanged`, and hit-test exclusion.
- `AdjacencyCluster.SpaceSectionFace2Ds` exposes the existing cached section.
- `PartFFloorPlanOverlay` + `PartFOverlayMark` (WPF-free, tested): real coordinates, space marks at
  the section outline's **internal point** (not centroid — that falls outside an L-shaped room),
  transfer marks on the modelled door or the longest cut segment of the shared partition.
- Terminal marks are a **point + direction** in the model. ~~drawn as a short screen-space stub~~ —
  **superseded by 5e**: a terminal is now drawn as a white tag and nothing else. The `Direction` is still on
  the mark and unused by the renderer; only TRA marks span real distance.
- `PartFTransferOpeningStatus` (derived, separate from `PartFTransferRouteStatus`): a
  `MissingTransferOpening` route gets no span — a dashed cross on the partition, a `?` in the label
  text, caption "No modelled transfer opening identified".
- `PartFSelectionResolver` — guid-only, searches the whole selection.

### 3c. Persistence spike (proven, section A of the last addendum)

A second parameter enum from the analytical layer **can** be associated with `ViewSettings`, and the
whole chain round-trips through `UIGeometrySettings` → `AnalyticalModel` → JSON.

New in `SAM.Analytical.UI`: `AnalyticalViewSettingsParameter`, `PartFAirflowViewSettings`,
`PartFAnnotationOverride`, `PartFAnnotationType`, `PartFDwellingFilter`, `PartFViewJson`.

**The correction that spike produced, and it was not theoretical:** naming the enum
`ViewSettingsParameter` (mirroring the `AnalyticalModelParameter` precedent) **shadows**
`SAM.Geometry.UI.ViewSettingsParameter` in every file importing both namespaces and broke **seven**
existing `.Group` / `.UseDefaultName` call sites. Hence `AnalyticalViewSettingsParameter`. The reason
is recorded on the type — do not "tidy" it back.

## 4. Decisions — do NOT silently revisit

1. Minimum-first cooking-priority is the default extract allocation; `VolumeWeighted` retained.
2. Table 1.2 is **two** requirements: total continuous ≥ whole-dwelling rate, and each room ≥ its own
   **high**-rate minimum. The per-room sum never raises the continuous rate.
3. The transfer network is built from internal separating **panels**, not doors.
4. A confirmation can resolve an undecidable check; it can **never** overturn a calculated failure.
5. Absence of evidence is never compliance. The output is an assessment, never a certificate.
6. Model owns engineering values; **view owns presentation only**. No flow rate, status or terminal
   may ever be stored in view settings — there is a reflection test asserting this.
7. Manual label positions are **world-plane `Point2D`**, never pixels; `ViewGuid` implicit; **not** keyed by
   operating mode. ~~Keyed on the annotated object's own guid~~ — **superseded, see 5d**: the object's own
   guid is regenerated by every calculation, so the key is DERIVED via `PartFAnnotationKey`.
8. A view with no Part F parameter reopens with the overlay **OFF**.
9. Stale overrides are **ignored**, never pruned on load.

## 5. DONE: Solver2D adopted as the Part F tag engine (task B)

Delivered as two separate changes, in this order, so a Mollier regression is attributable.

### 5a. Shared `Solver2D` hardening (`SAM.Geometry`, additive)

- **Both null paths guarded.** `obstacles2D` null was a real `NullReferenceException` on the first
  candidate — `new Solver2D(area, null)` crashed. The other one **could not actually throw**: `InRange`
  is a null-guarded extension method, so an earlier unplaced item was silently *skipped*, which is what
  the new explicit `continue` does. Both are now explicit and tested. Michal's review said "appears
  capable of dereferencing"; the audit says it skips instead. No behaviour change from that one.
- **`Solver2DResultType { Undefined, Solved, Fallback, Unplaced }`** on `Solver2DResult.ResultType`.
  `Undefined` was added to the three approved members so a defaulted value cannot read as `Solved`;
  `Solve()` never returns it. The old 2-arg `Solver2DResult` ctor is kept and derives the type from the
  geometry — `Solve()` deliberately does not use it, because a fallback rectangle would come out `Solved`.
- **Ordering is total**: priority, then insertion index, by sorting an index list. `List.Sort` stability
  is no longer relied on, and `Solve()` no longer re-sorts the field, so a second solve of one instance
  matches the first.
- **The 10 s wall clock is GONE.** Replaced by a deterministic work budget — `Solver2D.WorkBudget`,
  default `DefaultWorkBudget = 500000` geometric comparisons, with `Solver2D.WorkUnits` exposing the last
  solve's cost. Calibrated by measurement, not guessed: healthy 5 000 space labels = **9 900** units /
  0.4 s; 2 000 = 3 960 (linear); 400 labels collapsed onto one anchor at `IterationCount` 100 = **620 263**
  units / 14.6 s. So it sits >10× above the healthy case and bites into the degenerate one at roughly the
  point in time the stopwatch used to. Time per unit is not constant, so the equivalence is approximate by
  construction — that is the price of a machine-independent layout.
- `LimitArea` semantics **unchanged**, documented at length instead: centroid-only, `Inside` not
  `InRange`, and why all three consumers need the loose meaning.
- 15 tests in `SAM/SAM.Tests/Solver2DTests.cs`, including a Mollier-shaped case (point labels at default
  priority + circle obstacles + polyline-anchored curve labels at priorities 2–4) — the only cover the
  `Polyline2D` branch has, since Mollier's own adapter sits above OxyPlot.

**Mollier behaviour difference observed, and it is real.** Mollier leaves `Priority` at `int.MinValue`
for every point/process/zone label and sets 1–6 for curve labels, so it has large equal-priority groups.
Above 16 items `List.Sort` was reordering them, so those labels **were** nondeterministic between
redraws; they now follow chart-series order. Positions may therefore differ from before — consistently.
Charts with ≤16 in every tie group are unaffected (insertion sort is stable in practice).
Also noted, not changed: `Solver2DDatas_CurveNames` iterates a `Dictionary`, which is deterministic for
identical input but is the kind of incidental order the adapter layer should not lean on.

### 5b. `PartFTagPlacement` adapter (`SAM.Analytical.UI`, user-interface-free)

- `PartFTagPriority` — the named policy: TRA(1) → KEX(2) → EX(3) → SUP(4) → net(5) → diagnostics(6),
  applied only in `PartFTagPlacement.Priority(mark)`. **An unresolved transfer keeps TRA**, asserted.
- `PartFTagPlacementItem` (anchor, size in **metres**, `LimitArea`, priority, key) →
  `PartFTagPlacementResult` (rectangle, `ResultType` passed straight through, `IsUserPositioned`,
  `IsOverlapPossible`, `Leader2D()`).
- Placement order inside the adapter is priority → annotation type → object guid → supplied index, so a
  set handed over through a `HashSet` still lays out identically. Tested.
- **Manual tags in as obstacles**, stale overrides ignored not pruned. `PartFAnnotationOverride.Position2D`
  is now documented as the label's **centre** (a corner would slide when the text changes).
- `Rectangle2D` on a result is **never null**: an `Unplaced` Part F tag is drawn at its anchor and flagged,
  because a rate that vanishes off a compliance drawing is worse than one that overlaps. The floor-plan
  space labels take the opposite decision on the same engine (they blank the text) — that is defensible
  for a room name and not for a rate.
- Leader built at the consumer, `Segment2D` from anchor → nearest point on the tag; the anchor is cloned
  so a renderer cannot move a terminal by drawing a line. `Point2D` is mutable — tested.
- New `PartFOverlayMark.AnnotationGuid` / `.AnnotationType`: the **terminal**'s guid for a terminal mark
  and the **route**'s for a transfer mark (not the aperture — the common case has no modelled door).
- Window: the temporary `Place(Rect, List<Rect>)` pixel-nudging loop and `SpaceNameRects` are **deleted**;
  a reflection test asserts the window declares exactly one `Place`, parameterless. Solve runs on
  dwelling/level/mode/toggle/scale change only — `FloorPlan_ViewChanged` re-solves on a **scale** change
  and a pan merely re-transforms; `loading_FloorPlan` suppresses the solve while the plan is rebuilt.
- Space-name obstacles are now **read from the loaded `GeometryObjectModel`'s `Text3DObject` positions**
  rather than assumed to sit at the space anchor — those labels are themselves solved and can be metres
  away. Only the band's size is still assumed (120×20 px).
- 15 tests in `SAM_UI/WPF/SAM.Analytical.UI.WPF.Tests/PartFTagPlacementTests.cs`, including one end-to-end
  on a real three-room flat with the shipped rule set.
- Also fixed, enabled by the enum: `GeometryObjectModel`'s `FloorPlan.LabelSolver.Placement` diagnostic
  counted a budget fallback as "placed". It now reports solved / over budget / unplaced and work units.

### 5c. Annotation scale, NOT viewport zoom (reviewed and required)

Michal rejected an earlier revision where a zoom re-solved the layout: *"I do not want ordinary navigation
to keep changing an engineering drawing's annotation layout."* The distinction is now explicit.

- `PartFAirflowViewSettings.AnnotationScale` is a **real drawing scale denominator** (1:50 by default), saved
  with the view, machine-independent. It replaced a never-consumed text-size multiplier of the same name.
- `PartFTagPlacement.PixelsPerMetre(scale)` = 96 / 0.0254 / scale is the ONLY conversion from measured text
  to plane units. The viewport transform appears nowhere in the placement path.
- Pan **and zoom** transform and redraw only. Re-solve on: model/dwelling/level, visible annotation types,
  dwelling filter, operating mode, manual position, `AnnotationScale`, reset/auto-arrange.
- Tags are DRAWN at the size the annotation scale implies at the current zoom (text scales with the view), so
  the drawn box always equals the solved box. Fixing the text at its measured pixel size would let tags
  visibly collide at every zoom except one. Consequence to accept: zoomed far out, Part F text is small.
- Proved structurally: `Place_NeverReadsTheViewTransform` walks the compiled IL of `Place()` and everything it
  reaches, with a **positive control** (it must find `PartFTagPlacement.Solve` and `FloorPlan2DControl.Plane`)
  so it cannot pass vacuously, then asserts `WorldToScreen` is unreachable. Plus
  `Place_IsCalledOnlyByTheAgreedInputChanges` locks the caller set to exactly five methods.

### 5d. Stable annotation identity (reviewed and required)

`PartFVentilationTerminalRequirement` and `PartFDoorTransferData` are `SAMObject`s the calculator builds with
`Guid.NewGuid()`, so **their own guids last exactly one calculation**. Keyed on those — which is what handover
decision 7 originally said — every label an engineer had tidied would jump back on the next recalculation.
`RouteAndTerminalOwnGuids_AreNotStableAcrossRecalculation` documents it.

`PartFAnnotationKey` derives the key from persistent model identities (RFC-4122-style name-based guid, SHA-256,
version 8): terminal = space + role; transfer = **aperture** where the model has one, else the two space guids
**canonically ordered** so the key survives the route being reported the other way round. Nothing is persisted
— the key is recomputed. `ManualTransferPosition_SurvivesSaveReopenAndRecalculation` runs the real calculator
twice over one model, for a door-backed route AND a partition-only route.

**This supersedes decision 7 below**: the key is derived, not the terminal's own guid.

### 5e. Tag language (reviewed 2026-08-07 from the screenshot)

Michal asked for Revit-style tags rather than airflow markers, and the screenshot showed why.

- A terminal is a **tag and nothing else** — the directional arrow is gone. SAM knows the room requires
  extract or supply; it does not know where the grille is, so an arrow at a point SAM chose itself asserted a
  direction of air movement at an unestablished location.
- White (`0xF2`) fill, neutral grey hairline border, 3 px padding, text in the air type's colour, no `▶`.
  Reads "SUP 63.0 l/s ✓".
- **No leader for a terminal tag**, automatic or manual: nothing real to lead to. `HasPhysicalAnchor(mark)` is
  the single predicate suppressing it, so a future terminal object with a coordinate gets its leader back. A
  transfer tag DOES get one — a door or partition is a real location.
- The tag is drawn unconditionally now; "Values" chooses between `SUP 63.0 l/s` and `SUP`. It used to be gated
  behind the value toggles, which was safe only while an arrow remained underneath.
- Room names and door labels are protected obstacles, from the **measured** bounds of the text the plan drew
  (`TextObstacle2Ds`, using the plan's own `Query.Width` and `TextAppearance.Height`), not a 120×20 px band
  around the space anchor as before — those labels are themselves solved and can sit metres from the anchor.
- Fixed: `TRA 63.0 l/s ? ?` — the route's own trailing `?` and the cannot-be-determined symbol were both
  appended.
- Checkbox labels are now "Terminal tags" / "Transfer routes".

**Screenshot still owed** (blocked on Michal running the app) before the main 2D View work starts.

### 5f. Known limits, deliberately left

- Tag sizes are fixed on screen, so a zoom re-solves. At a very zoomed-out scale a tag is large in plane
  units, may not fit its room, and comes back `Unplaced` → drawn at its anchor. That is CAD behaviour and
  it is reported, not hidden.
- `PartFTagPlacement` is a pure function and the window owns the "when"; there is no cache keyed on a
  signature yet. Add one if a large plan shows a lag on a zoom — `GeometryObjectModel.labelSolveCache` is
  the precedent.
- `Overrides()` in the window returns an empty list until the drag-to-move UI (section 6) exists. The
  obstacle path is built and tested, so it works the day the UI writes an override.

## 5g. Superseded — original brief for task B

**There is nothing to extract.** The shared engine already exists at
`SAM/SAM/SAM.Geometry/Geometry/Planar/Classes/Solver2D/`. SAM_Mollier is a **consumer**
(`SAM.Core.Mollier.UI.WPF/Create/Solver2DData.cs`, `Modify/AddLabels.cs`). Do **not** create
`AnnotationPlacementEngine`.

Michal's `Solver2D-review.md` was verified line by line against the code — **every claim accurate**:

- `solver2DDatas.Sort(...)` on `Priority` is **unstable** → equal-priority tags reorder between
  redraws. A saved drawing must redraw identically. Fix: priority, then insertion order.
- A 10 s wall-clock budget silently places remaining tags **at their anchor, overlapping**, and the
  caller cannot tell. Fix: `Solver2DResultType { Solved, Fallback, Unplaced }` on `Solver2DResult`.
- `LimitArea` tests the **centroid** only. Keep that (option A) for floor-plan tags — a small ensuite
  cannot contain the whole text box.
- **Latent bug**: `solver2DResults.Find(x => x.Closed2D<Rectangle2D>().InRange(...))` has no null
  guard; an earlier unplaceable item throws. Only on the **≤256-item non-grid path** — i.e. exactly
  Mollier charts and small floor plans.
- **Latent bug**: `obstacles2D.Find(...)` assumes the list is non-null.

**Do NOT add `LeaderLine` to `Solver2DResult`** — this supersedes an earlier proposal of mine that was
wrong. A leader is `anchor + solved rectangle → nearest point → Segment2D`, derivable at the consumer.

**Manual tags become obstacles, not omissions.** Mollier only *excludes* a user-positioned label from
the solve, so an automatic tag can land on top of one somebody placed deliberately:

```
automatic tag   -> Solver2DData
manual tag      -> obstacle
model geometry  -> obstacle
```

`IsUserPositioned` stays in the view layer (`PartFAnnotationOverride`), never in `SAM.Geometry`.

**Order of work** — steps 1–2 touch shared `SAM.Geometry` used by Mollier, so do them as their own
small change with the Mollier chart re-verified BEFORE the Part F adapter, so a regression is
attributable:

1. Harden the two null paths in `Solver2D`.
2. Add `Solver2DResultType`; make ordering stable. Additive only; Mollier regressions must stay green.
3. Thin `PartFTagPlacement` adapter in `SAM.Analytical.UI`: `Solver2DData` per tag with priority
   (TRA → KEX → EX → SUP → net → diagnostics), manual overrides + space outlines as obstacles, leader
   built at the consumer.
4. **Delete the temporary `Place(...)` loop** in `PartFAssessmentWindow.FloorPlan.cs`.
5. Tests: automatic separation, a manual tag never overlapped by an automatic one, deterministic
   repeat solve, `Fallback` surfaced.

Caching: `Solver2D` must NOT run on every pan/zoom `ViewChanged`. Solve on model/mode/filter/toggle/
scale/manual-position change and on auto-arrange; pan and zoom only re-transform and redraw.

## 6. DONE: Part F in the NORMAL saved 2D View

SAM_UI `efaed83`, on the remote.

- **One renderer.** Everything the drawing consists of is now `PartFAirflowRenderer`
  (`WPF/SAM.Analytical.UI.WPF/Controls/`), used by the assessment window AND the saved view, driven entirely
  by `PartFAirflowViewSettings`. `PartFAssessmentWindow` has **no placement and no drawing code at all** — a
  test asserts it declares neither `Place` nor `DrawMarks`.
- The structural zoom tests now target the renderer, so they cover both consumers: callers of
  `PartFAirflowRenderer.Place` are pinned to `{Load, Refresh, set_ViewSettings}`.
- `ViewportControl.FloorPlan2D` exposes the 2D plan (null in 3D or on the legacy orthographic path).
- `AnalyticalWindow.PartF.cs` attaches a renderer per view tab from the view's `PartFAirflow` parameter, and
  re-reads the assessment through the same `PartFCalculator` the command uses, cached per model instance +
  scope. A view with no parameter is left without one — never given a disabled setting. ~~The window holds
  the cache~~ — **superseded by 6b**: the cache and the scope gate are `PartFAssessmentCache`.
- `PartFAirflowViewSettings.ZoneCategoryName` scopes which dwellings a drawing reports on, so reopening
  reproduces the assessment instead of asking again. ~~Empty means the whole model is one dwelling~~ —
  **superseded by 6b**: empty is UNDECIDED, and whole-model is `PartFDwellingScope.WholeModel`.
- **Part F Airflow dialog** on the 2D view settings (`PartFAirflowViewSettingsWindow`) — how it is turned on.
- **Whole-floor isolation is now tested** (`PartFWholeFloorTests`, 6 tests) on two flats separated by a
  communal corridor with every partition real and shared: every dwelling assessed, no TRA between dwellings,
  every mark inside its own dwelling, the corridor annotated with nothing, the floor exactly the sum of its
  dwellings with distinct annotation keys, and no tag overlap ACROSS dwellings (one solve, separate
  assessments).

### 6b. New-view preset (SAM `b0e72a21` + `c5ca006e`, SAM_UI `150ea63`)

Choosing `Color Scheme → Element → PartF Data` on a **new** view now initialises a usable Part F drawing:
annotation on, all layers, 1:50, continuous design, all dwellings on the level. Before this, the obvious way
to ask for a Part F drawing produced coloured fills and no airflow, with no hint that nine more options sat
behind another dialog.

- **The dwelling category is resolved, never hard-coded.** `AdjacencyCluster.PartFDwellingZoneCategories()`
  (SAM). One category ⇒ auto-selected. Several ⇒ **left unset for the user** (which flats a drawing reports on
  is an engineering decision). None ⇒ whole-house mode.
- **The dwelling-selection policy exists exactly ONCE**, in `Query.PartFDwellingZones(IEnumerable<Zone>)`:
  where no zone carries `IsDwelling` every zone counts (the legacy behaviour, because the parameter postdates
  the models); otherwise only an explicit `true` does. `PartFCalculator.SelectDwellingZones` now **decides**
  through that rule and keeps only its own reporting, taking its warning/remark lists from
  `Query.PartFClassifyDwellingZones`. Its warning texts are byte-identical and all 1046 SAM tests pass
  unchanged. **There is no duplicated rule and no technical debt here** - an earlier revision restated the
  policy in the categories query, and Michal was right to object.
  `DwellingCategories_AgreeWithTheCalculator` is kept as the end-to-end integration lock.
- **The four scope cases are now distinguished (review correction, 2026-08-07).** A null `ZoneCategoryName`
  used to mean three different things, and `PartFCalculator.Calculate(string)` reads null as whole-model
  single-house mode - so a new view of a block with several possible categories was enabled, unscoped, and
  drew the whole building as ONE dwelling while waiting to be told which flats it was about. Fixed at the
  **view/preset scope only; the calculator is untouched.**
  - New `PartFDwellingScope { Undefined, WholeModel, ZoneCategory }` on `PartFAirflowViewSettings`, and the
    single predicate `HasDwellingScope`. **A blank category is no longer a decision.**
  - **`Enabled` is intent; `HasDwellingScope` is the safety mechanism.** `Enabled` says this view wants Part
    F annotation; `HasDwellingScope` says whether SAM knows enough to calculate any. The preset therefore
    leaves the annotation **ON in every case** and only the scope undecided, so selecting the dwellings is
    the single remaining action and the drawing appears without a second trip back to re-enable anything.
    An earlier revision switched it off instead; Michal was right that the presentation toggle is the wrong
    place for a correctness gate. `ScopeSelection_IsTheOnlyRemainingAction` pins the behaviour.
  - Preset scope: one category ⇒ that category; no zones at all ⇒ `WholeModel`; several categories ⇒
    `Undefined`; zones but no dwelling among them ⇒ `Undefined` (**never** a silent fall back to
    whole-house - that would report a block as one dwelling because `IsDwelling` was never set).
  - **`PartFAssessmentCache`** (new, in `SAM.Analytical.UI`) is the extracted saved-view assessment AND the
    gate: an undecided scope is not calculated at all, and `null` reaches the calculator only from an
    explicit `WholeModel`. `AnalyticalWindow.PartF.cs` now holds one of these instead of three fields.
  - Dialog: the editable category combo became an explicit list - "Not chosen", "Whole model as one
    dwelling", then each **dwelling** category the model actually holds - with a message saying why nothing
    is drawn. The button reads `Part F Airflow: choose the dwellings...`.
  - A view saved before the scope existed is read from its category: named ⇒ `ZoneCategory`, blank ⇒
    `Undefined`. The safe direction.
- **`IsNewViewSettings`** on `AnalyticalTwoDimensionalViewSettingsControl` is the only gate. Set by the two
  creation paths only — `AnalyticalWindow`'s New Section View and `BatchCreateViewsControl`.
- Never applied to an existing view, never re-applied where settings exist, never on duplicate, never resets a
  manual position. Four tests, one per promise. The preset does NOT switch the annotation off again if the
  colour scheme is later changed — the two are independent by design.
- 17 tests in `PartFAirflowPresetTests` (one per scope case, both round trips, and
  `TwoCategories_NeverProduceAWholeModelAssessment` driving the real cache), plus
  `PartFPlanModel.ZoneWithoutIsDwelling` for the legacy case.

### 6c. The saved-view cache invariant, proved (review correction, 2026-08-07)

`PartFAssessmentCache` is the one place an engineering value is held between draws, so the claim "an edit
cannot be answered from a stale assessment" is now a test rather than a comment. **No revision framework was
added** — the invariant was already there and only needed asserting:

- `UIAnalyticalModel_HandsOutANewModelOnEveryRead` — `UIJSAMObject.JSAMObject` deep-clones on every read, so
  an instance key cannot survive an edit. One line, and it is the whole argument.
- `ModelEdit_CannotReuseTheCachedAssessment` — end to end through the real `UIAnalyticalModel`, cache and
  calculator: two dwellings assessed and the cache proved WARM, a dwelling zone removed, one dwelling after.
- `SameModelAndScope_IsAnsweredFromTheCache` first, so none of the above passes vacuously.
- Verified by mutation: reverting the gate and re-keying the cache on `AnalyticalModel.Guid` (which the clone
  preserves) fails 5 of these tests. Keying on the guid is the tempting wrong answer — it is now covered.
- 6 tests in `PartFAssessmentCacheTests`.

### 6a. Backlog, explicitly NOT to be started yet

Michal's optional productivity command, after the 2D view work settles:
`Create proposed Part F transfer door...` — a **user-invoked** model edit, never something the assessment
creates silently. For a simple unique shared partition it would propose a 760 × 2100 mm internal door,
`SIM_INT_SLD`, a 10 mm finished-floor undercut and 7 600 mm² transfer free area, with a preview and
confirmation before the model changes. **Creating the aperture must not by itself make paragraph 1.25 pass**:
transfer compliance comes from the recorded undercut/free area, so accepting the proposal has to record the
design provision explicitly.

## 7. NEXT: the remaining saved-view work (tasks 15–20)

**This is the real target, and Michal has said so twice.** The assessment window's Airflow tab is for checking
and debugging Part F and must not become the final drawing interface. The goal is:

> normal saved 2D Section/Floor Plan View + `PartF Data` colour scheme + optional Part F annotation overlay

so that a view called e.g. `Level 0 [0m] Part F` can be created and reopened with its Part F presentation
intact. Everything in section 5 was built to be reused there unchanged: `PartFFloorPlanOverlay` and
`PartFTagPlacement` are user-interface-free, and the shared control already carries the overlay hook.

Part F Airflow section in View Settings (separate from the existing `PartF Data` colour scheme, which
is a `ValueAppearanceSettings` and stays untouched); whole-floor rendering as N independent overlays,
one per dwelling result; drag-to-move labels with leaders and reset/auto-arrange; door properties in
the normal property workflow; plan selection resolving to the underlying object.

**Whole-floor isolation needs an explicit regression test** — Michal does not want it resting on the
per-dwelling architecture as an inference: all dwellings render, no TRA between dwelling results,
communal corridor gets nothing, filtering one dwelling removes the others' marks, and per-dwelling
rendering matches the combined render.

## 8. Screenshots still owed (blocked on Michal running the app)

Whole Level 0 with all flats; Flat 1 only; Flat 2 only; Continuous; Setback; colour scheme + overlay
together; before/after auto placement; one manually moved KEX tag with leader; view after
close/reopen proving persistence.

## 9. Environment gotchas

- `Panel.Apertures` returns **clones**; go through `AdjacencyCluster.SetPartFDoorTransferData(...)`.
- Inside `SAM.Analytical.UI.WPF`, unqualified `Window` binds to `SAM.Analytical.Window` — WPF windows
  must declare `: System.Windows.Window`.
- `CS1566 ... g.resources` → delete `WPF/SAM.Analytical.UI.WPF/obj` and rebuild.
- After a compile error, `dotnet test` can run a **stale** test assembly. Use
  `dotnet build --no-incremental` then `dotnet test --no-build` when a fix "does nothing".
- Use `System.Math.*` — bare `Math` collides with the `SAM.Math` namespace.
- `SAM.Analytical` multi-targets below `Dictionary.TryAdd`; use `ContainsKey` + indexer.
- Heredocs through the Bash tool mangle backslashes — use the Edit tool for C# string literals
  containing `\r\n` or `\n`.
- The ADF PDF is read with `pdftotext -layout` (Git for Windows).

## 11. Part O / Iteration programme — Iteration 0 IN PROGRESS

The work has been reframed as iterations. **Only Iteration 0 is being built.**

| | |
|---|---|
| **Iteration 0** | Foundation: dwelling scope → Part F → system selection → scenario → TM59 → result association. **CURRENT** |
| Iteration 1 | Base passive / unrestricted openings, MVRE at Part F continuous. TSD route. NOT STARTED |
| Iteration 2 | Acoustic restriction + boost + summer bypass. TSD route. NOT STARTED |
| Iteration 3 | CoolBreeze-class active trim cooling. Full `SystemEnergyCentre` → TAS HVAC → **TPD** route. NOT STARTED |

Iteration 0 must not make that routing impossible; it must not implement any of it.

### 11a. Architecture reconnaissance — what already exists (REUSE, do not rebuild)

The real TAS run proved execution and result association already work. **Do not build a new simulation
pipeline.**

- **Execution**: `WorkflowCalculator` / `WorkflowSettings` (SAM_Tas), driven from `Modify.Simulate`
  (SAM_UI) — the "Convert to Tas TBD" dialog. Works today.
- **Overheating engine**: `SAM.Analytical.TMOverheatingCalculator` (extracted this session — see 11c).
- **The TSD → TM59 recipe already exists**, in the Grasshopper component
  `TasTSDQueryTM59Results`: TSD → `Convert.ToSAM(path, TSDConversionSettings{ResultantTemperature,
  OccupantSensibleGain, ConvertZones})` → restore design `InternalCondition`s → **select spaces from a
  dwelling Zone via `GetRelatedObjects<Space>(zone)`** → `Calculate_TM59` → split Mechanical / Natural /
  Corridor. **Lift this, do not rewrite it.**
- **Results**: `Result` base + `Space/Zone/AdjacencyCluster/AnalyticalModelSimulationResult`, associated
  through `AdjacencyCluster` relations. The real run wrote 18 space + 8 zone + 440 surface results.
- **System templates**: `SAM_Systems/files/resources/Analytical/Systems/SystemEnergyCentre/*.json`
  (1–1.8 MB each: plant rooms, energy sources, ~100 `SystemType`s, 2D schematic), loaded by
  `Analytical.Systems.Query.SystemEnergyCentre(path)` / `DefaultSystemEnergyCentreDirectory()`.
- **`PartOOpeningProperties`** already exists, and the real model's 20 apertures already carry it.

### 11b. The vocabulary — settled, do not re-litigate

`SAM_SystemTypeLibrary.JSON`'s `VentilationSystemType` ids are **the same identifiers as the
SystemEnergyCentre template filenames**, and `SystemTemplate` (Ventilation/Heating/Cooling/PlantRoom/
Controls/Version) is the existing engine-neutral identity matching names like `NV 1:RAD 1:UC 1`.

```
NV    Natural ventilation
MV    Mechanical ventilation, NO heat recovery   (Air Supply Method = Outside)
MVRE  Mechanical ventilation WITH heat recovery  ← SAM's MVHR
UV    Unconditioned  → routes to TM59CorridorExtendedResult
```

**`MVRE` is MVHR — verified, not assumed.** Its template holds one air-side exchanger:
`SensibleEfficiency 0.7`, `LatentEfficiency 0`, and **no mixing or recirculation component anywhere**.
`MV.json` has no exchanger at all. So the difference between them is exactly heat recovery, and `RE`
behaves as REcovery despite the library description saying "Recirculation".
**Do NOT add an `MVHR` identity** — it would split an established concept. Summer bypass and boost are
**operating states of MVRE**, not new system types.

### 11c. DONE this session (all pushed)

| Repo | SHA | What |
|---|---|---|
| SAM | `060feeda` | `Query.PartOClassifyAssessmentZones` + `SimulationSpaceMap` + 16 tests |
| SAM | `9cbf308a` | `TMOverheatingCalculator` extraction + 5 tests |
| SAM | `2e2362f7` | doc terminology |
| SAM | `7faf964c` | **`OverheatingScenario` + derived key** (step 4) + 24 tests |
| SAM | `02e99582` | **step 4 hardened after independent review** → 35 tests |
| SAM | `884a2f54` | **step 4.1 canonicalisation** (Michal's review) → 40 tests |
| SAM | `b23bef3b` | **step 5a analytical capability selection** → 17 tests |
| SAM | `c5112e4f` | preference out of `SAM.Analytical`; stronger handover invariant |
| SAM | `b52aff65` | **`MechanicalSupply`** + selection hardening → 21 tests |
| SAM_Systems | `c6a441d` | **step 5b `CapabilityIndex.JSON`** + conformance test → 9 tests |
| SAM_Systems | `e99f311` | malformed-index refusal + structural no-template proof → 20 tests |
| SAM_Tas | `5e38c94` | `OverheatingCalculator` → compatibility wrapper + 3 equivalence tests |
| SAM_Tas | `d56f679` | doc terminology |
| SAM | `193968ff` | **step 7a** `TM59AssessmentCalculator.SourceFallback` — provenance survives the repoint |
| SAM_Tas | `f6e32b4` | **step 7a** `Create.TM59AssessmentCalculator` — TAS's series keys + provenance stay in SAM_Tas |
| SAM_Tas_Grasshopper | `047c583` | **step 7a** `Tas.TSDQueryTM59Results` repointed; the recipe now exists once |
| SAM | `f6772519` | **step 7b** `VentilationStrategyMap` + `VentilationStrategySelection` → 16 tests |
| SAM_Tas | `b03f02b` | **step 7c** scenario-authoritative TM59 XML export → 9 tests |
| SAM | `d2b0f971` | step 7 review fixes — closed vocabulary, refusal copies, falsifiable controls → 1173 |
| SAM_Tas | `121035c` | step 7 review fixes — no silently dropped rooms, null returns carry a reason → 42 |
| SAM | `d7abd48a` | **step 8** identity association through `SimulationSpaceMap` |
| SAM_Tas | `1aba5eec` | **step 8** TAS stable-key mapping and print-route scenario seam |
| SAM_Tas_Grasshopper | `f8ca646c` | **step 8** `Tas.TSDQueryTM59Results` stops matching by name |
| SAM | `f712fd9f` | step 8 review fixes — fail-safe mapping, scoped selection, transactional scenarios → 1183 |
| SAM_Tas | `7f3dfda` | step 8 review acceptance coverage for both TAS production paths → 46 |
| SAM_Tas_Grasshopper | `b8dae4b` | step 8 review wiring in both existing Grasshopper workflows |

- **Part O scope** (`PartOClassifyAssessmentZones`): dwellings vs common space, dwelling half
  **delegated to `Query.PartFDwellingZones`** (single source of truth). The corridor is *returned* as
  common space, never dropped, never attributed to a dwelling.
- **`SimulationSpaceMap`**: resolves a simulation-derived space back to the design space. Stable
  engine key first, unique name as fallback, **refuses on ambiguity**. The key is a
  `Func<Space,string>` so `SAM.Analytical` stays engine-free. Verified on the real run: **9 spaces → 9
  distinct TAS zone guids**, matching the DomOv XML — unique per SPACE, not per zone.
- **`OverheatingScenario` (step 4, DONE)**: a Part O assessment stated as engineering intent - scope,
  design zone guid, iteration, the existing `SystemTemplate` identity, and an
  `OverheatingOperatingAssumptions` bag - with a **derived** key. SHA-256 over a namespace and the
  components, first 16 bytes stamped version 8 + RFC 4122 variant, mirroring `PartFAnnotationKey`.
  UTF-8, **length-prefixed** (concatenation is ambiguous), NFC-normalised, enums hashed **by name**,
  behind the marker `OverheatingScenario:v1`. **Not** `Core.Query.ComputeHash` - it is ASCII and
  collides. Not in the key: name, `Source`, anything engine-shaped, simulation output, view settings,
  creation time. `Key` is `Guid.Empty` where `IsValid` is false, held after first derivation and
  dropped by `FromJsonObject` - the only path that writes identity-defining state.
  `Key_IsStableAcrossBuilds` pins the exact guid; regenerate it **only** with a schema bump.
  - **No `Iteration0` member.** `PartOIteration { Undefined, BasePassive, AcousticRestricted,
    ActiveTrimCooling }` - the foundation stage is a stage of this codebase, not an operating scenario
    of a building, and a scenario built during it states `Undefined`, which is true.
  - `VentilationStrategy` / `HasVentilationStrategy` read the existing `SystemTemplate.Ventilation`
    (`NV`/`MV`/`MVRE`/`UV`). **No second vocabulary, no `MVHR`.** Step 7's consumer must **refuse**
    where `HasVentilationStrategy` is false, never fall back to the zone-name lookup it replaces.
  - **Canonicalisation is at the boundary, not at the hash** (step 4.1). An assumption **name** is
    NFC-normalised *before* it enters the ordinal `SortedDictionary`, because the assumptions are hashed
    in name order and ordinal order runs over raw code units — composed `é` (U+00E9) sorts **after**
    `f`, the decomposed form sorts **before** it. Normalising only inside `Append` left canonically
    identical assumptions hashed in different **orders**. Proved by mutation.
  - **One canonicaliser per type, whichever door a value came through.** A JSON primitive goes through
    the same path as the typed setter that would have written it: `{"SummerBypass": false}` stores
    `False`, not `false`; a JSON number goes through `Text(double)`. A JSON **string** stays text (so
    `"21.0"` is not `21`). An object or array is **refused**, not flattened — there is no canonical form
    for arbitrary JSON and property order alone would decide the key.
  - An **invalid** scenario is equal only to itself. Its key is empty, and treating "no identity" as a
    shared identity collapsed every half-filled scenario in a user interface into one `HashSet` entry.
- **TM overheating extraction**: `SAM.Analytical.TMOverheatingCalculator` owns TM52 + TM59 + comfort
  helpers; `SAM.Analytical.Tas.OverheatingCalculator` is a delegating wrapper with its public API
  intact (no Grasshopper/UI migration). The two series keys are **instance** properties
  (`ResultantTemperatureSeriesKey`, `OccupancySensibleGainSeriesKey`) defaulted to the analytical
  vocabulary; the wrapper supplies TAS's. Equivalence tests run under `dotnet test` **without TAS COM**.

### 11d. ~~THE OPEN DEFECT~~ — CLOSED in step 7. Ventilation strategy had THREE conflicting derivations

**Fixed in step 7 (11l).** Where a `VentilationStrategyMap` is supplied, the scenario states the strategy and
**all three derivations below are bypassed**; where nothing states it, the space is **refused**. All three
remain reachable for callers that supply no scenario, documented as **superseded**, and derivation #3's
`"NV"` default is the one that made an MVRE dwelling assess as naturally ventilated. Kept below because it
is the record of what the defect was and which code still contains it.



The real run exposed `Nat Vent` / `Mech Vent` mixed across three flats of one building. Root cause:

| # | Where | Decides by | Feeds |
|---|---|---|---|
| 1 | `Convert/ToTM59/Zone.cs:27` | space's `InternalCondition.VentilationSystemTypeName` | TM59 XML |
| 2 | `Convert/ToTM59/Building.cs:26` | any related `VentilationSystem` that `IsMechanicalVentilation()`; overrides #1 | TM59 XML |
| 3 | `TMOverheatingCalculator.SystemTypeName` | internal condition → **else the zone's NAME looked up in `DefaultSystemTypeLibrary()`** → else `"NV"` | **the engineering result** |

**#3 picks the TM59 criterion**, and its middle step is a string match of a zone name (`"Flat 1"`)
against a library — which misses, defaulting an MVHR dwelling to the natural-ventilation criterion.
`Space.ToTM59` already accepts a `systemType` override, so the seam exists. **The scenario must become
authoritative.** All three were ported verbatim and commented; fixing them is Step 7.

Also found: **`Zone Category` does nothing for Domestic Overheating.** In `Modify.Simulate` it is
consumed only by the `createSAP` branch, so the TM59 export covers the whole model — which is why the
communal corridor appeared in the run's DomOv XML as an ordinary room.

### 11e. Real TAS validation assets (on this machine)

- TAS **9.5.7.0** at `C:\Program Files\Environmental Design Solutions Ltd\Tas`.
- Michal's run output: `…\OneDrive - Tetra Tech, Inc\Documents\SAM_daily\2027-08-03-HVAC\` —
  `000000_SAM_AnalyticalModel.{tbd,tsd,json}`, `.timing.csv` (78 s total), and
  `Report XMLs\…DomOv.xml`.
- Model: `SAM_zoningAM_v2zonesisDomestic.sam` — **Zone Category `Flats` → Flat 1 / Flat 2 / Flat 3
  (`IsDwelling` true) + Corridor (false)**, 9 spaces. The canonical Iteration 0 fixture shape.
- Weather: DSY from `C:\Users\Public\Documents\Tas Data\Databases\CIBSE Weather 2021.twd`, readable via
  `SAM.Weather.Tas.Convert.ToSAM_WeatherDatas(path)`.
- **The post-run model carries NO `TM59*Result`** — the overheating output is only the DomOv XML, which
  is *configuration for the external TAS TM59 tool*, not an assessment. That gap is Iteration 0's job.

### 11f. Follow-ups deliberately NOT fixed (each needs its own change + regression)

1. **`"Occupant Sensible Gain"` (TAS) vs `"Occupancy Sensible Gain"` (analytical)** — same quantity,
   two spellings. Reading the wrong one is silent. Do not add a second enum member; do not migrate data.
2. **`MVRE` description** says "with Recirculation" but the template is heat recovery. Descriptions
   participate in lookups, so this is behavioural.
3. **`MVRE` `Air Supply Method = Total`** vs `MV`'s `Outside` — verify before Iteration 2 relies on it.
4. **No weather data + valid series → `NullReferenceException`**, thrown inside
   `SAM.Weather.Query.RunningMeanDryBulbTemperatures`. Characterized by
   `NoWeatherData_ThrowsToday_PreExistingBehaviourNotAContract`. The fragility is in the weather layer.
5. **Missing series → silent no-assessment**, pinned not endorsed. Needs a diagnostic.
6. **`Core.Query.ComputeHash` uses `Encoding.ASCII`** — non-ASCII names collide. Fine as a checksum,
   **unsafe for derived identity**. Use UTF-8 for the scenario key. *(Done for the scenario key in
   `7faf964c`; `ComputeHash` itself is untouched and still ASCII.)*
7. **`SystemTemplate`'s setters strip spaces; its copy and JSON constructors do not.** `"MV RE"` means
   `MVRE` through `new SystemTemplate(...)` and `MV RE` through `FromJsonObject`. `OverheatingScenario`
   normalises at its own boundary (`Normalized`), and `SAM_Systems`' capability-index reader rebuilds
   identities through the setters for the same reason - so both places that matter are safe, but the
   shared serialisation path is still inconsistent for every other consumer. Fixing it means routing
   `SystemTemplate.FromJsonObject` through the setters, which changes a path `SAM_Systems` lookups use
   — **needs its own change and regression**, not a drive-by.
8. **`"R"` / `"G17"` are not runtime-stable** — both changed meaning in .NET Core 3.0, so a double
   formatted for identity differs between a .NET Framework host (Revit, gbXML) and .NET 8.
   `OverheatingOperatingAssumptions.Text(double)` is fixed to nine decimal places for that reason. Any
   *other* derived identity in SAM that formats a double has the same exposure.

### 11g. NEXT — Iteration 0 remaining steps, in this order

4. ~~**`OverheatingScenario`**~~ — **DONE**, `7faf964c` + `02e99582`. See 11c.
5. **DONE — 5a, 5b, 5c.** 5a in `SAM.Analytical` (11h), 5b the `SAM_Systems` catalogue (11i), 5c
   eligibility and CI closure (11j). Concrete template selection is enabled: a Part F assessment resolves
   to a real `SystemEnergyCentre`, and a commercial template is never offered for a dwelling.
6. ~~Lift the `TasTSDQueryTM59Results` recipe into a testable service.~~ **DONE** (11k), and **fully
   closed in 7a** — the component now calls it, so the recipe exists once.
7. ~~**Make the scenario authoritative** over ventilation strategy (11d).~~ **DONE and reviewed** — see
   **11l**. 11d is closed.
8. ~~Result association to design dwelling / common space **by identity, not name**.~~ **DONE and
   independently reviewed** — see **11n**. The mandatory three-flat duplicate-`Bedroom 2` regression covers
   both existing TAS production paths.
9. **NEXT — preserve the TSD-simple vs TPD-full routing boundary. Read 11m first.** The two-pass TAS
   workaround is deliberate and must not be removed as duplication. Implementation is waiting on Michal's
   three decisions in 11m; do not guess.
10. Thin headless TAS runner, **last**.

### 11h. Step 5a — system capability selection, analytical half (DONE, `b23bef3b` + `c5112e4f` + `b52aff65`)

**The boundary, decided by Michal and replacing an earlier recommendation of mine.** A declared catalog
in `SAM.Analytical` would have put the names of ten `SAM_Systems` resource files inside the core
library. The split is instead the one already drawn for TAS:

```
SAM.Analytical                              │  SAM_Systems  (11i)
  SystemCapability (vocabulary)             │    CapabilityIndex.JSON beside the templates
  PartFSystemCapabilityRequirement (rule)   │    capability VALUES + Rank for the ten templates
  CapableSystems (suitability)              │    chosen SystemTemplate -> concrete SystemEnergyCentre
  SelectPreferredCapableSystem (choice)     │    conformance test opens the real 1.8 MB files
  — owns NO values and NO preference        │
```

- **`SystemCapability`** — `ContinuousVentilation`, `MechanicalSupply`, `Boost`, `SummerBypass`,
  `HeatRecovery`. Capabilities, **not equipment**. `MVRE` stays the heat-recovery identity.
- **`MechanicalSupply` was added after a review found a real misselection.** A balanced dwelling —
  paragraph 1.67, a supply terminal in every habitable room — was met by `Local Extract Only`, because
  extract-only does run continuously and can boost. The overheating simulation would have run a system
  with no supply and no heat recovery against a building that has both.
- **`Query.PartFSystemCapabilityRequirement(PartFDwellingResult)`** — continuous ventilation; mechanical
  supply when `TotalSupply_Lps`/`TotalHighSupply_Lps` is non-zero (those terminals carry
  `IsInBalancedFlow`); boost when `TotalHighExtract_Lps` exceeds `ContinuousDesignSystemRate_Lps`
  (intermittent extract excluded — it is not part of the balanced system). **It never asks for summer
  bypass or heat recovery**, swept over a grid of dwellings.
- **`Query.CapableSystems`** — suitability only, which is all this assembly can honestly decide.
  Returned in the supplied order so a caller with a different policy has the whole suitable set.
- **`Query.SelectPreferredCapableSystem`** — takes the first. **Preference is
  `SystemCapabilityDescriptor.Rank`, supplied by the catalogue and never derived.** An earlier revision
  ranked by fewest capabilities; Michal rejected that as a policy about a particular library rather than
  something following from Part F. Ranks are compared with `CompareTo`, not subtracted (overflow put the
  lowest-ranked system last). The sort breaks a final tie on insertion order, which is the only thing
  that makes sorting an index list less unstable than sorting the list.
- **Refusals**: nothing capable ⇒ names what was missing; every capability present but never together on
  one system ⇒ says that instead; two **different** suitable systems at the lowest rank ⇒ refused, the
  catalogue has not said which is preferred; one system listed **twice** ⇒ not ambiguous, selected.
- **`SystemCapabilityDescriptor` is not an `IJSAMObject`** — it had a second JSON shape incompatible with
  the catalogue's, and its own reader turned a real index entry into a confident empty descriptor.
- Two structural locks, both mutation-verified: no member of the selection types names a file, path,
  directory or energy centre; no static member of `SAM.Analytical` hands out descriptors.
- 21 tests in `SAM.Tests/SystemCapabilitySelectionTests.cs`. The descriptors there are a **test fixture
  standing in for the `SAM_Systems` catalogue, not a copy of it**.

### 11i. Step 5b — the SAM_Systems capability catalogue (DONE, `c6a441d` + `e99f311`)

`files/resources/Analytical/Systems/SystemEnergyCentre/CapabilityIndex.JSON`, beside the ten shipped
templates. Read at runtime; **the templates are not**. Keyed on the existing `SystemTemplate` identity —
ventilation part only, because these are ventilation templates and heating/cooling/plant/controls are
chosen elsewhere. No new system or equipment identity, no `MVHR`.

| | `CV` | `Supply` | `Boost` | `Bypass` | `HR` | Rank | Application |
|---|---|---|---|---|---|---|---|
| `NV` Natural Ventilation | yes | | | | | 10 | Domestic |
| `EOL` Local Extract Only | yes | | yes | | | 20 | Domestic |
| `EOC` Central Extract Only | yes | | yes | | | 30 | Domestic |
| `MV` Supply And Extract | yes | yes | yes | | | 40 | Domestic |
| `MVRE` — SAM's MVHR | yes | yes | yes | | yes | 50 | Domestic |
| `CAV` Constant Air Volume | yes | yes | | | yes | 60 | Commercial |
| `VAV` Variable Air Volume | yes | yes | yes | | yes | 70 | Commercial |
| `DISP` Displacement | yes | yes | | | yes | 80 | Commercial |
| `UV` Unconditioned | | | | | | 90 | Any |

`Plantroom-Only.json` is listed under `NonVentilationResources` — not a candidate, recorded so every
shipped resource is accounted for.

- **`Query.SystemCapabilityDescriptors(directory)`** reads the index and nothing else. **A malformed
  index refuses entirely**: a missing or mistyped `Rank`, or a ventilation identity that is present but
  unusable. The `Rank` case is why — the review changed `"Rank"` to `"rank"` on VAV and every dwelling
  requirement then silently selected a commercial VAV unit in place of a dwelling extract fan, because
  a defaulted 0 sorts first. An **absent** ventilation key still means "not a ventilation template".
- Identities are rebuilt through `SystemTemplate`'s property setters, so an index entry reading `"M V"`
  matches a caller's constructor-built `"MV"` (11f item 7).
- **`Query.SystemEnergyCentreResource`** matches on the ventilation identity and refuses on ambiguity, a
  blank resource, or any resource containing a separator or `..`.
- **`Query.SystemEnergyCentre(SystemTemplate)`** is **the one point at which a template is opened at
  runtime**, once, after the choice, for the system chosen.
- **What the conformance test can and cannot prove.** `HeatRecovery` is checked against every shipped
  file — an exchanger with a sensible efficiency **above zero** — and re-establishes the MVRE fact every
  Part O heat-recovery decision rests on: one exchanger, 0.7 sensible, 0 latent, no recirculation, and
  `MV` is the same system without it. `SummerBypass` is checked as **absent from all ten** (ignoring
  `BypassFactor`, a cooling-coil parameter that false-positives a naive search).
  `ContinuousVentilation`, `MechanicalSupply` and `Boost` **cannot be read off a template at all** and
  are declared from the system type's meaning; each entry's `EvidenceFromTemplate` says which values are
  evidence, and a test asserts it says so honestly rather than presenting declaration as measurement.
- **"No template is opened" is proved structurally**, not by a stopwatch: the index is copied alone into
  an empty directory and every requirement Part F can produce still gives the same answer — and
  resolution is asserted to fail there, so the test cannot pass by reading nothing.
- 34 tests in `SAM_Systems/SAM.Analytical.Systems.Tests`, in `SAM_Systems.sln`, **and executed by CI** —
  see 11j.

### 11j. Step 5c — eligibility and CI closure (DONE, SAM_Systems `895a86d` + `bff125f`)

**`Application` is an eligibility constraint, not documentation.** It was a comment, and `Rank` was doing
its job — so `CapableSystems` reported `CAV`, `VAV` and `DISP` as **suitable** for an Approved Document F
dwelling requirement and only their ranks kept them out of the answer. One edited number could have put a
variable-air-volume unit in a flat.

- `SystemApplication { Undefined, Domestic, Commercial, Any }` lives in **`SAM_Systems`**. Which of *these*
  templates is a dwelling system is a fact about this repository's resources, on the same footing as their
  capabilities. `SAM.Analytical` contains no reference to it and never learns the words.
- `Query.SystemCapabilityDescriptors(directory, systemApplication)` filters **before** an entry becomes a
  descriptor. **The parameter is not optional** — defaulting it made the commercial-inclusive call the one
  you get by writing nothing.
- **Two roles, asymmetrical.** As a *classification* on an entry, `Any` means "suits either". As a
  *request*, `Any` and `Undefined` both mean **no constraint** — before that, asking for `Any` returned only
  `UV`, the one template that can never be selected.
- `CommercialTemplates_AreExcludedByEligibilityAndNotByRank` **inverts every rank in the index** so
  commercial sorts first, asserts that premise, and asserts a dwelling still never sees one.
- A missing, unrecognised, numeric or comma-listed `Application` **refuses the whole index**. The last two
  matter: `Enum.TryParse` accepts `"3"` and `"Domestic,Commercial"`, and since the members number
  `Domestic = 1, Commercial = 2, Any = 3` both OR to `Any` — the most permissive value — with
  `Enum.IsDefined` unable to object. Applications are matched against the three legal names, ordinally.

**The guarantee is scoped, and the limit is recorded rather than left to be discovered.** Eligibility holds
at the descriptor boundary. It does **not** hold across the repository, because an older, live path —
`Query.DefaultSystemEnergyCentres` → `Create.SystemEnergyCentre` — derives a `SystemTemplate` from the
**file name** and matches it against each space's `VentilationSystemTypeName`, consulting neither the index
nor `Application`. **Four Grasshopper components reach it**, and a space carrying `"VAV"` still resolves
`VAV.json` there. Nothing in step 5 changed it; `ResolutionIsIdentityDriven_AndDoesNotApplyEligibility`
pins the behaviour so a future fix has something to change. **Guarding that path is separate work.**

**Rank is declared library policy and nothing else** — not derived, not a claim of engineering minimality.
The domestic order (`NV 10, EOL 20, EOC 30, MV 40, MVRE 50`) is recorded in the index as **PROVISIONAL AND
NOT CONFIRMED**: it makes selection deterministic, nothing more. Ranks need only be distinct **within** an
application, which is the only set they order.

**Unverified declarations are recorded and fail-safe.** `CAV`, `VAV` and `DISP` carry
`"UnverifiedDeclarations": [ "Boost" ]`, and a test asserts **every capability named there is false** — a
review caught `VAV` declaring `Boost: true` while listing it as unverified, which is crediting a capability
nobody confirmed. The name-derived justifications are gone: `"constant volume"`, `"displacement regime"`
and `"Variable Air Volume → can vary"` are all inference from a label. A non-commercial entry carrying an
unverified declaration is now **asserted** impossible, not merely commented.

**CI executes the suite.** `build.yml` runs `SAM.Analytical.Systems.Tests` after the ordered Rebuild, with
`/p:SAMVersion` — **which matters**: three SAM_Systems projects write to the same `build\` folder the
artifact step publishes, and a test build without it recompiles them at the `1.0.0.0` local-dev fallback
and overwrites the CI-stamped DLLs, shipping version-inconsistent assemblies. Verified: the test build now
leaves `2026.3.999.0` in place where it previously left `1.0.0.0`. A failing test no longer costs the build
artifacts either. **Not yet demonstrated on a real run** — the workflow triggers on `master`/`main`/`sow/**`
only, so it first fires when a PR to `sow/2026-Q3` is opened.

### 11k. Step 6 — the TM59 assessment recipe, extracted (DONE, SAM `d327496a`)

`SAM.Analytical.TM59AssessmentCalculator` + `TM59AssessmentResult`. **Moved, not redesigned.**

The `Tas.TSDQueryTM59Results` Grasshopper component held the only working statement of the sequence,
inside a `SolveInstance` interleaved with parameter plumbing — so nothing could call it, nothing could
test it, and the step 10 headless runner would have had to restate it and drift.

```
TSD file ──(TAS)──> analyticalModel_TSD          <- stays in SAM.Analytical.Tas
                          │
                          ├─ RestoreDesignInternalConditions(design)   <- engine-free
                          ├─ Spaces(spaces, zones)                     <- engine-free
                          └─ Calculate(spaces, extended)               <- engine-free
                                 └─> TM59AssessmentResult
                                       Spaces, MechanicalVentilationResults,
                                       NaturalVentilationResults, CorridorResults,
                                       Max/MinIndoorComfortTemperatures
```

Only the read needs TAS, so the recipe runs under plain `dotnet test` — the same split
`TMOverheatingCalculator` already makes.

**Two things deliberately preserved during step 6 so the extraction was falsifiable; both are now closed:**

1. ~~Spaces are matched by NAME in restore and selection.~~ Closed by `SimulationSpaceMap` in **step 8**
   (11n), after step 6 had pinned the old behaviour. Every flat's `Bedroom 2` regression proves it.
2. ~~The criterion comes from `TMOverheatingCalculator`'s zone-name/default derivation.~~ Closed for
   scenario-aware calls in **step 7** (11l); legacy calls without scenarios deliberately retain it.

One incidental asymmetry removed with no behaviour change: the component null-guarded two of its three
result splits and not the third. `FindAll` never returns null, so the guard never did anything; the three
are now one generic `Split<T>`.

**9 tests**, and the one that makes this an extraction rather than a rewrite is
`TheService_MatchesTheComponentsOwnSequence` — the component's sequence is **inlined verbatim** from its
`SolveInstance` and its output compared with the service's: spaces, all three criterion lists, and both
comfort limit series. Deliberately not factored; it is a transcript. Also pinned: the silent
no-assessment when the series key is the analytical spelling against a TAS-written model, and that
**no spaces and no zones means the whole model** — which is why the real run exported a communal corridor
into a domestic overheating assessment as an ordinary room.

~~**NOT DONE** — the Grasshopper component still holds its own copy.~~ **CLOSED in step 7a** — see 11l.
`SAM_Tas_Grasshopper` is now the **fifth repo** in the workstream and `TasTSDQueryTM59Results` calls the
service.

### 11l. Step 7 — the scenario is authoritative over ventilation strategy (DONE)

Three commits, in this order, because 7a had to be behaviour-preserving **before** the semantics changed:

| Repo | SHA | What |
|---|---|---|
| SAM | `193968ff` | **7a** — `TM59AssessmentCalculator.SourceFallback`, so the repoint keeps TAS's provenance |
| SAM_Tas | `f6e32b4` | **7a** — `Create.TM59AssessmentCalculator`, TAS's two series keys + provenance in TAS's assembly |
| SAM_Tas_Grasshopper | `047c583` | **7a** — `TasTSDQueryTM59Results` repointed; ~90 lines of `SolveInstance` become four calls |
| SAM | `f6772519` | **7b** — `VentilationStrategyMap` + `VentilationStrategySelection`; the criterion is stated, not derived |
| SAM_Tas | `b03f02b` | **7c** — the TM59 **XML export** takes the strategy from the scenario too |

**7a, and why `SourceFallback` had to exist first.** The component reached the recipe through
`SAM.Analytical.Tas.OverheatingCalculator`, which supplies three values that are TAS's and not the
assessment's: the two series keys the TSD conversion writes, and the assembly name a result reports as its
`Source` when the model is unnamed. The service exposed the keys but not the fallback, so repointing would
have silently changed a published result's provenance from `SAM.Analytical.Tas` to `SAM.Analytical` as a
side effect of a refactor. `Create.TM59AssessmentCalculator` keeps all three in `SAM_Tas` rather than
restating them at the call site — a second place to keep them right is the drift the extraction existed to
stop. **One behaviour difference, and it is a refusal replacing a crash:** where the TSD read produced
nothing the old code dereferenced null, and the component now reports `"Invalid data"`.

**7b — what the map replaces.** All three derivations in 11d, and it replaces them rather than seeding them.
`VentilationStrategyMap` is keyed on `Space.Guid`, **never on a name**, and it does no matching of its own,
so it cannot quietly reintroduce matching by name. At the step 7 checkpoint, resolving a simulation space
to its design space remained step 8's job; it is now closed in 11n.

- **Refusal never falls through.** A space no scenario covers, a scenario that states no strategy, and two
  scenarios stating different strategies are three refusals with **three different sentences**, because they
  are three different mistakes with three different fixes. A refused space produces **no result** and its
  reason lands in `TM59AssessmentResult.VentilationStrategyRefusals`. Falling back on refusal would restore
  the exact defect the map removes, invisibly, at the one input where nothing was said.
- **Reported, not thrown**, so one unstated dwelling does not cost every other dwelling its assessment.
- **Nothing is inferred from provenance** — not `Source`, not TSD-versus-TPD, not which engine wrote the
  numbers.
- **What a strategy MEANS is unchanged**: `UV` → corridor, `NV` → natural, anything else → mechanical, the
  vocabulary the criterion selection already had. Step 7 changed *which* strategy applies.
- **Scenario identity untouched.** `VentilationStrategy` / `HasVentilationStrategy` already existed and are
  only read. `Key_IsStableAcrossBuilds` still pins the same guid.
- **Left unsupplied, nothing changes.** `SystemTypeName` is documented as **superseded**, not deleted — the
  Grasshopper and user-interface callers have no scenario to state yet, and removing their behaviour without
  giving them a way to state the right one would be a regression.

**7c — the export refuses the WHOLE document, which is deliberately different from the assessment.** The
assessment drops a refused space and reports it; a TM59 XML is configuration for the external TAS TM59 tool,
which has no way of being told a room is missing — it would assess what it was given and produce a
complete-looking answer for an incomplete building. So `ToTM59(…, VentilationStrategyMap, out refusals)`
returns **null** if any space is refused, having visited every space first so one unstated dwelling does not
hide the others' reasons. The strategy goes in through `Space.ToTM59`'s existing `systemType` parameter —
the seam that was already there — and a non-`Undefined` value means that method's internal-condition
fallback is never reached. The two-argument overload is untouched and a null map delegates to it.

**Tests: SAM 1173 (was 1137), SAM_Tas TM59.Tests 42 (was 25).** Every override test carries a **control**
that runs the same model without the map and shows the old derivation reaching the opposite answer —
without those, a passing test would prove only that the map agreed with a derivation that was already
right. Five mutations run, all caught and restored — listed in 0c, along with the build-order trap one of
them exposed.

**What the independent review found, and it was not cosmetic** (fixed in SAM `d2b0f971` + SAM_Tas
`121035c`):

1. **An unrecognised strategy was silently assessed as MECHANICAL** — the step 7 defect pointing the other
   way, reachable from the *authoritative* path. `"Natural"`, `"Mixed Mode"`, `"N-V"`, or `"MVHR"` (a name
   that does not exist, because `MVRE` is SAM's heat-recovery ventilation) all became mechanical results with
   no refusal. The vocabulary is now **closed**; see 0d items 3 and 4.
2. **The export still dropped rooms silently.** `Space.ToTM59` returns null for a space with no internal
   condition and for a null `TM59Manager`; those were omitted, no refusal was recorded, and the completeness
   gate still passed — a three-space building shipped two zones as a success, which is the one failure the
   external TAS tool could never notice.
3. **A null export return could carry an empty reason list**, so the documented contract lied.
4. **`TM59AssessmentResult.VentilationStrategyRefusals` was caller-mutable** while the property it is copied
   from was not — a reporting layer de-duplicating in place could erase the record of which dwellings went
   unassessed.
5. **Two tests were not falsifiable.** The zone-name control used zones named `"NV"`/`"NV Wing"` against an
   MVRE scenario and asserted `"Natural"` — which the `"NV"` default says anyway, so it would have passed
   with the entire zone-name lookup deleted. It now uses `MVRE` and `MVR` against an `NV` scenario, where the
   control reads *mechanical* and the assertion flips. **And the direction of the `StartsWith` step was
   documented backwards**: the comparison asks whether the *library entry's* name starts with the zone name,
   so a zone must be a **prefix** of an entry — `"NV Wing"` matches nothing and went through the default,
   while `"MVR"` genuinely exercises it. Separately, the test claiming to pin derivation #2 in the assessment
   could not: `TMOverheatingCalculator` never consults a `VentilationSystem`, so its control came from
   derivation #1. Renamed to what it proves — that an attached mechanical system is **inert** there —
   with derivation #2 pinned on the export side, where it lives.
6. **The normalisation comment was wrong and `.Trim()` was dead code.** `OverheatingScenario.Normalized`
   already rebuilds the `SystemTemplate` through setters that strip **every** space, so `"MV RE"` and
   `" uv "` cannot reach the map; only `.ToUpper()` does work, and the test claiming to pin whitespace
   normalisation was vacuous.

The review confirmed **no behaviour regression in the 7a repoint**, having read the old `SolveInstance`
against the new one line by line, and confirmed `OverheatingScenario`'s identity is untouched.

**The step 6 equivalence test still matters and its doc comment now says why.** The component calls the
service, so the two agree by construction and the component can no longer disagree with itself. What
`TheService_MatchesTheComponentsOwnSequence` pins is the only falsifiable thing left: that the service
still does what the component *used* to do. Delete it and the last statement of the original behaviour goes
with it.

**Historical step 7 stopping point, closed by step 8.** At this checkpoint no production caller supplied a
map: `Convert.ToXml(AnalyticalModel, …)` — reached from `ToTBD`, which wrote the real run's DomOv XML — still
called the two-argument `ToTM59`. Step 8 now adds an optional scenario input to both existing Grasshopper
entry points and routes supplied scenarios through identity-aware maps; omission deliberately retains this
legacy path. See 11n.

### 11m. Step 9 — the TSD-simple vs TPD-full boundary (Michal's clarification, AUTHORITATIVE)

**The two-pass TPD route is a deliberate TAS compatibility workaround. It is NOT duplication. Do not remove
it, simplify it, or "consolidate" it into the TSD path.** It is expensive on purpose.

**Why it exists.** The TPD route does not give the `ResultantTemperature` series TM59 needs, so the workflow
deliberately runs **two** simulations.

**The intended TPD-full sequence, as stated by Michal:**

1. Simulate the actual system.
2. Read the resulting **supply air temperature** and **supply airflow**.
3. Inject those values into a **copy** of the TBD.
4. Simulate that modified TBD again.
5. Read the resulting TSD.
6. Take the TM59 `ResultantTemperature` from that second simulation.
7. Pass the prepared analytical result model into the common `TM59AssessmentCalculator`.

**Constraints, all of them binding:**

- The first simulation's supply temperature and airflow are **engineering inputs** to the second run, **not
  provenance**. They are therefore not something a scenario states and not something step 7's
  `VentilationStrategyMap` touches.
- **Always modify a COPY of the TBD**, never the original design model.
- **Preserve current TPD-full behaviour exactly** until TAS exposes an equivalent native resultant
  temperature through the TPD workflow.
- **Documentation and regression coverage must explain why the two-pass route exists**, so a future refactor
  cannot delete it as duplicate work. That coverage is owed and does not exist yet.
- When TAS does expose it natively, the workaround may be retired **behind the preparation boundary** without
  changing `TM59AssessmentCalculator`.

**The boundary step 9 must draw.** Preparation differs; assessment does not.

```
TSD-simple:  TSD ───────────────────────────────────────────► analytical result model ─┐
                                                                                       ├─► TM59AssessmentCalculator
TPD-full:    TPD ─► pass 1 ─► TBD COPY ─► pass 2 ─► TSD ────► analytical result model ─┘
```

`TM59AssessmentCalculator` **must not care** which side prepared the model, and must not learn the words
TSD or TPD.

**What the code does TODAY — read this before touching anything, because it is NOT the sequence above.**
Two separate TPD-side mechanisms exist and **only one of them is the two-pass route**:

| | Where | What it actually does |
|---|---|---|
| **A** | `SAM_Tas/…/SAM.Analytical.Tas.TPD/Modify/CalculateResultantTemperature.cs`, driven by the component `Tas.CalculateResultantTemperatureFromTPD` (`SAM_Tas_Grasshopper/…/SAM.Analytical.Grasshopper.Tas.TPD/`) | **The two-pass route.** Reads an already-simulated TPD (`Simulate = false`, `IncludeComponentResults = true`), takes each space's `SpaceDataType.ZoneTemperature`, **copies** the TBD to `<name>_TPDThermostat.tbd`, writes that series into every zone internal condition's thermostat **upper- and lower-limit profiles** as yearly profiles (`factor 1`), **simulates again** to `<name>_TPDThermostat.tsd`, and returns both paths |
| **B** | `SAM_Tas_Grasshopper/…/SAM.Analytical.Grasshopper.Tas.TPD/Component/TasTPDQueryTM59Results.cs` | **Not the two-pass route.** Reads the TSD beside the TPD via `ToSAM_SpaceSystemResults(path_TPD, out path_TSD)` and synthesises `ResultantTemperature` as the arithmetic **mean of the TSD's `MeanRadiantTemperature` and the TPD's `ZoneTemperature`**, then runs its own inline copy of the TM59 recipe |

**Three discrepancies step 9 must reconcile with Michal — do not guess at any of them:**

1. **A injects zone temperature into thermostat setpoint limits**, not supply air temperature and supply
   airflow. Airflow injection exists as separate, uncalled modifiers (`Modify.UpdateSpaceAirflows`,
   `Modify.UpdateFanAirflows`). Whether the intended sequence describes A's evolution or a different
   mechanism is **Michal's call**.
2. **B is a one-pass approximation and does not use A at all.** Two different TPD answers to the same
   question are shipping side by side, and which one is the TPD-full path is not settled in the code.
3. **B still holds a third inline copy of the TM59 recipe.** Repointing it at `TM59AssessmentCalculator` is
   step 9, **not** a step 7 drive-by, precisely because its middle stage is the thing being preserved.

Steps 7 and 8 changed **nothing** in either mechanism, by design.

### 11n. Step 8 — identity-based result association (DONE and independently reviewed)

**Goal.** Associate simulation-derived spaces, design spaces, scenarios and results by identity through
`SimulationSpaceMap`, never by a direct name join. The mandatory regression is the real failure shape:
three flats each containing a room named exactly `Bedroom 2`, plus a communal corridor.

**Original implementation:** SAM `d7abd48a`, SAM_Tas `1aba5eec`, SAM_Tas_Grasshopper `f8ca646c`.
It applied the existing stable-key/unique-name/refuse map to internal-condition restore, requested-space and
zone selection, TAS zone identity and the normal TSD result component. The original mutation replacing the
identity restore with name matching failed three tests including the mandatory duplicate-bedroom case.

**Both existing production acceptance paths are preserved and now expose the same architecture:**

1. **Normal Grasshopper results:** `To gbXML` → `SAMAnalytical.WorkflowgbXML` (`Simulation=true`) → TSD →
   `Tas.TSDQueryTM59Results`. Version 1.0.8 appends optional `overheatingScenarios_`, constructs the
   `OverheatingScenarioMap` with the calculator's TAS-keyed `SimulationSpaceMap`, installs its authoritative
   `VentilationStrategyMap`, and reports identity/scenario/criterion refusals as Grasshopper warnings.
2. **Official TAS print:** `SAMAnalytical.CreateTBDByTM59` → TM59 XML/TBD/TSD → TAS UI, where spaces remain
   preconfigured and the user moves through the existing TAS TM59 tabs and prints. Version 1.0.3 appends the
   same optional scenario input, uses a design-identity `SimulationSpaceMap`, and calls the scenario-aware
   XML overload. An incomplete association refuses the whole XML. No replacement print workflow was added.

Appending the optional inputs, rather than inserting them among existing ports, preserves saved Grasshopper
definitions. With no scenarios both components retain their previous acceptance path and fallback behaviour;
with scenarios, the identity/scenario path is authoritative.

**Independent review findings and fixes:** SAM `f712fd9f`, SAM_Tas `7f3dfda`,
SAM_Tas_Grasshopper `b8dae4b`.

1. A whole-model request included unresolved/ambiguous simulated spaces after restore had correctly refused
   them, so those spaces could still produce results. Whole-model selection now includes only resolved map
   entries and reports exclusions.
2. A two-candidates-to-one-design collision removed the reverse mapping but left both forward mappings
   usable, allowing both candidates to receive design state. The collision now invalidates both directions.
3. `Spaces(null, zones)` selected the whole model instead of the supplied zones. Whole-model now means both
   `spaces` and `zones` are null.
4. A scenario ownership collision marked the shared lookup ambiguous but retained the first scenario's full
   stored selection and live ventilation claim. The first scenario is rolled back as a whole, and the
   ventilation map is built only after all ownership conflicts settle.
5. The official print component had only the legacy XML call and the normal results component had no
   scenario input. Both were wired at their existing seams as described above.
6. The new optional inputs were initially inserted mid-list, which would have shifted saved component port
   indices; review moved both to the end. Review also prevented a successful SAP conversion from overwriting
   a failed/refused TM59 status while still allowing the SAP attempt to run.

**Final validation:** SAM **1183 passed, 0 failed**; SAM_Tas TM59.Tests **46 passed, 0 failed**; the
SAM_Tas_Grasshopper project built under VS Framework MSBuild with **0 errors**. The TAS suite covers both
paths with three exact duplicate `Bedroom 2` names plus the corridor. Three additional review mutations
proved the resolved-only whole-model filter, zones-only scope and transactional scenario ownership; all were
restored before the final full runs.

**Step 9 boundary preserved.** Neither
`SAM.Analytical.Tas.TPD/Modify/CalculateResultantTemperature.cs` nor
`SAM.Analytical.Grasshopper.Tas.TPD/Component/TasTPDQueryTM59Results.cs` changed. Mechanism A remains the
two-pass TBD/TSD thermostat-profile workaround; mechanism B remains the separate one-pass MRT + TPD zone
temperature synthesis with its inline recipe. The discrepancies in 11m are recorded accurately and require
Michal's decisions before step 9 begins.

## 10. Standing instructions

- **Two-laptop continuity rule, mandatory.** This file is the authoritative continuation state. After
  every completed checkpoint, update it **in the same working session**, then commit and push it. Never
  finish a checkpoint with the handover behind the repositories, and never leave important reasoning
  only in a chat, a terminal history or an unpushed local commit. Before starting work in a new session,
  read section 0 and verify every repo against the recorded branch/SHA/remote state; **if the actual
  state differs, stop and reconcile before changing code.**
- **Working sequence:** implement → test → commit → push → independent review/fixes → test →
  commit/push → update this handover → commit/push handover → continue. Never force-push; never squash
  or rebase published commits; do not open a PR unless asked.
- Work on `feature/partf-terminal-transfer-compliance` in **all five** repos — SAM_Tas's,
  SAM_Systems's and SAM_Tas_Grasshopper's are new and need PRs like the others. **Never commit to
  `sow/2026-Q3` directly.**
- SAM first (SAM_UI and SAM_Tas CI dep-clone it), PRs against `sow/2026-Q3`, CI green **and** the Codex
  inline comments read.
- Attribution line `Generated by Michal Dengusiak and CodeClaude` on every commit and PR body.
- SPDX header on every changed `.cs`.
- Use the SAM implementation-summary style for the final response.

---

## Paste this into the new session

Continue the Approved Document F / Part O work in the SAM-BIM workspace. We are building **Iteration 0**
only.

**First: read `SAM/documentation/PartF-HANDOVER.md` in full** - especially **section 11**, which is the
Iteration 0 state, the architecture reconnaissance, the open defect and the follow-ups deliberately not
fixed. It also holds the decisions that must not be silently revisited and the environment gotchas.

Then in **all five** repos - SAM, **SAM_Systems**, SAM_UI, SAM_Tas and **SAM_Tas_Grasshopper** - run the
two verification commands in **section 0a**, which check clean/level, descent from the recorded last-code
SHA, and that nothing but the handover changed since it. The second must print exactly **five**
`descends from` lines and nothing else.

Confirm each is on `feature/partf-terminal-transfer-compliance`, the tree is **clean**, and local matches
remote at the SHAs in **section 0a**, which is authoritative. Nothing should be outstanding.

### State - all pushed, all green

SAM **1183**, SAM_Tas TM59.Tests **46**, SAM_UI **180**, SAM_Systems **123** + **34**, SAM_Mollier **22**;
Grasshopper (including `SAM.Analytical.Grasshopper.Tas`) and Mollier UI 0 `error CS`; SPDX clean.
Not merged, **no PRs open in any of the five**.

Done and reviewed: the Part F regulatory correction pass, the floor-plan overlay, saved-view persistence,
the dwelling-scope correctness fix and cache proof, and **Iteration 0 steps 1-8**: Part O assessment scope,
`SimulationSpaceMap`, the engine-neutral `TMOverheatingCalculator` extraction with its TAS compatibility
wrapper, `OverheatingScenario` with its derived deterministic key (11c), system capability selection across
`SAM.Analytical` and `SAM_Systems` (11h-11j), the `TM59AssessmentCalculator` extraction (11k) **now called
by its Grasshopper component**, **the scenario made authoritative over ventilation strategy (11l)**, and
**identity-based result/scenario association wired into both existing TAS acceptance paths (11n)**.
Every checkpoint independently reviewed and hardened.

**Read section 0 first and verify the repositories against it before changing any code.**

### Your next task: Iteration 0, step 9 onward (section 11g)
7. **DONE and reviewed** (see 11l) — the scenario is authoritative over the TM59 criterion and over the XML
   export, and refuses where nothing states a strategy. 11d is closed.
8. **DONE and independently reviewed** (see 11n) — association is through `SimulationSpaceMap`; both
   existing TAS paths carry optional scenarios; the three-flat duplicate-`Bedroom 2` plus corridor
   regression covers both.
9. **NEXT — preserve the TSD-simple vs TPD-full-HVAC routing boundary.** Read 11m before touching either
   implementation. The two-pass TAS workaround is deliberate and must not be removed or simplified as
   apparent duplication. The three discrepancies in 11m require Michal's answers first; do not guess.
10. Thin headless TAS runner, **last**.

**Do not implement Iteration 1, 2 or 3 behaviour.** Do not start CoolBreeze.

### Decisions still waiting on Michal - do not decide these yourself, ask

1. The provisional domestic `Rank` order (`NV 10, EOL 20, EOC 30, MV 40, MVRE 50`).
2. The unverified commercial `Boost` declarations (all `false`, `CAV`/`VAV`/`DISP`).
3. Whether `Application` should also gate the older unguarded `Create.SystemEnergyCentre` /
   `DefaultSystemEnergyCentres` path - the one that HAS production callers (four Grasshopper components),
   where a space carrying `"VAV"` still resolves `VAV.json`.
4. PRs for the three new branches: `SAM_Tas`, `SAM_Systems` and `SAM_Tas_Grasshopper`.
5. The `SAM_Systems` CI test step is implemented and locally verified but has **never run** - the workflow
   triggers on `master`/`main`/`sow` only, so it first fires on a PR to `sow/2026-Q3`.
6. **NEW - the closed ventilation vocabulary** in `VentilationStrategyMap`
   (`NV MV MVRE UV EOL EOC CAV VAV DISP`). Declared policy; a custom `SAM_SystemTypeLibrary` with extra
   identities would be refused.
7. **NEW - the three TPD discrepancies in 11m**, which must not be guessed at.

### Rules established under review. Each has a test; do not undo any

1. **`Query.PartFDwellingZones` is the single source of truth for what a dwelling is.** Part O scope
   delegates to it and only names the remainder as common space.
2. **The corridor is assessed, never attributed to a dwelling.** `UV` -> `TM59CorridorExtendedResult`.
3. **Never attribute a result by NAME.** `SimulationSpaceMap`: stable key, unique name, then refuse.
   The unique-name fallback exists only inside that map; callers never perform a second name join. Every flat
   has a `Bedroom 2`. A collision invalidates both mapping directions, whole-model selection excludes every
   unresolved space, and a zones-only request remains scoped to those zones.
3a. **Scenario ownership is transactional.** A collision rolls back both scenarios and publishes no stale
    ventilation claim. Assessment reports partial results; official TM59 XML refuses the whole incomplete
    document.
4. **`MVRE` is SAM's MVHR** - verified from the template (0.7 sensible / 0 latent exchanger, no
   recirculation component). Never add an `MVHR` identity. Boost and summer bypass are **operating
   states**, not system types.
5. **`SAM.Analytical` never references `SAM.Analytical.Tas`.** The engineering calculation is
   engine-free; TAS owns conversion, series keys and provenance.
5a. **A scenario key is derived, never generated, never stored.** UTF-8, length-prefixed, NFC-normalised,
   enums by name, behind the marker `OverheatingScenario:v1`. `Key_IsStableAcrossBuilds` pins the exact
   guid - regenerate it ONLY with a deliberate schema bump. There is **no `Iteration0` enum member**: the
   foundation stage states `Undefined`.
6. **`Source` is provenance only** - no part in scenario, equipment, dwelling or result identity.
7. **Series keys are instance state**, defaulted to the analytical vocabulary. Do not reconcile
   `Occupant`/`Occupancy` here.
8. **Behaviour preserved, not improved** in the extraction: the silent no-assessment on a missing series and
   the no-weather throw are still pinned, not endorsed. The `"NV"` default and the zone-name lookup are
   **superseded as of step 7** — bypassed whenever a scenario states a strategy, reachable only for callers
   that state none.
8a. **The scenario is authoritative over ventilation strategy, and a refusal NEVER falls back** (11l). Three
   distinct refusals — uncovered space, scenario states nothing, scenarios disagree — plus a fourth for a
   strategy outside the **closed** vocabulary, because "anything that is not NV or UV is mechanical" is the
   same defect pointing the other way. The assessment drops a refused space and reports it; the **export
   refuses the whole document**, because the external TAS TM59 tool cannot be told a room is missing.
8b. **The two-pass TPD route is a required TAS workaround, not duplication** (11m). Never remove or simplify
   it; always modify a **copy** of the TBD; both routes converge on `TM59AssessmentCalculator`, which must
   not learn the words TSD or TPD.
9. **ONE renderer, ONE placement path** for Part F presentation (sections 5-6).
10. **Model owns engineering data; view owns presentation only.** Reflection test asserts it.

### Working practice

- **test -> commit -> push -> independent review -> continue**, at every architectural checkpoint. Do not
  leave a completed green checkpoint local-only. Never force-push, never squash/rebase published commits,
  never commit to `sow/2026-Q3`, no PRs unless asked.
- Attribution line `Generated by Michal Dengusiak and CodeClaude` plus the `Co-Authored-By` trailer.
- SPDX header on every changed `.cs`.
- **TAS-facing projects need VS Framework MSBuild**, not the dotnet CLI (MSB4803 / `ResolveComReference`):
  `C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe`.
  Build SAM before SAM_UI/SAM_Tas - they reference its built DLLs.
- Section 9 has the rest of the gotchas. Use the SAM implementation-summary style for the final response.
