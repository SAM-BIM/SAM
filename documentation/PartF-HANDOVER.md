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

*Last updated at SAM `b23bef3b` — Iteration 0 step 5a (analytical half of system capability selection).*

### 0a. Repository state — verify this before touching anything

| Repo | Branch | Last CODE commit | HEAD should be | Tree | Cut from |
|---|---|---|---|---|---|
| `SAM` | `feature/partf-terminal-transfer-compliance` | **`b23bef3b`** | that, **plus the handover commit(s) on top** | clean, level | `sow/2026-Q3` @ `34dea440` |
| `SAM_UI` | `feature/partf-terminal-transfer-compliance` | **`ffd8e38`** | exactly `ffd8e38` | clean, level | `sow/2026-Q3` @ `074f3d9` |
| `SAM_Tas` | `feature/partf-terminal-transfer-compliance` | **`d56f679`** | exactly `d56f679` | clean, level | `sow/2026-Q3` @ `3d58bfe` |

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
for r in SAM SAM_UI SAM_Tas SAM_Systems; do echo "=== $r ==="; git -C $r status --porcelain; git -C $r log --oneline -1; git -C $r log --oneline -1 origin/feature/partf-terminal-transfer-compliance; done
```

```bash
for e in "SAM b23bef3b" "SAM_Systems PENDING"; do set -- $e; git -C $1 merge-base --is-ancestor $2 HEAD 2>/dev/null && { echo "$1: descends from $2"; git -C $1 diff --name-only $2 HEAD | grep -v '^documentation/PartF-HANDOVER.md$' | sed "s/^/$1 UNRECORDED CODE: /"; }; done
```

The second command must print only the `descends from` lines. **Any `UNRECORDED CODE:` line means stop.**

Not merged. **No PRs open.** `sow/2026-Q3` never committed to directly, untouched in all three.
`SAM_Systems` is **not in the workstream** — no branch, no changes, and step 5b will need one.

### 0b. Latest checkpoint — what it implemented

SAM `b23bef3b`, Iteration 0 **step 5a**: the analytical half of system capability selection.
`SystemCapability` (4 flags), `SystemCapabilityRequirement`, `SystemCapabilityDescriptor`,
`SystemCapabilitySelection`, `Query.PartFSystemCapabilityRequirement`,
`Query.SelectMinimumCapableSystem`. Detail in **11h**.

### 0c. Tests and builds run, with counts

| Suite | Result | How |
|---|---|---|
| `SAM/SAM.Tests` | **1124 passed, 0 failed** | `dotnet test SAM/SAM/SAM.Tests/SAM.Tests.csproj` |
| `SAM_UI`, `SAM_Tas`, `SAM_Systems`, `SAM_Mollier` | **unchanged since their last run** — see section 2 | not re-run this checkpoint; nothing in them changed |

Two mutation checks were run and both behaved correctly (the test failed, then passed on restore):
reverting the assumption-name normalisation fails `UnicodeNormalisation_DoesNotChangeCanonicalOrdering`
and nothing else; adding a `static List<SystemCapabilityDescriptor>` to `SAM.Analytical` fails
`NoDefaultCatalog_LivesInTheAnalyticalAssembly`.

### 0d. Architectural decisions made in this checkpoint

1. **`SAM.Analytical` owns the capability vocabulary + the Part F requirement rule + the selection rule.
   `SAM_Systems` owns the capability VALUES for its shipped templates.** Michal's decision, replacing an
   earlier recommendation of a declared catalog in `SAM.Analytical` — that would have put the names of
   ten `SAM_Systems` resource files inside the core library. Same cut as TAS: analytical states intent,
   the specialised assembly owns implementation.
2. **`HeatRecovery` is a capability, not an identity.** `MVRE` remains SAM's heat-recovery ventilation.
   It is in the vocabulary so `MVRE` is strictly more capable than `MV` and a requirement that does not
   ask for heat recovery returns the simpler system.
3. **Part F requires continuous ventilation and boost, and nothing else — never summer bypass or heat
   recovery.** Those are Part O mitigation a scenario states.
4. **Refuse, never approximate.** No capable system ⇒ an explicit refusal naming what was missing.

### 0e. Explicitly deferred

- **Step 5b — the `SAM_Systems` capability catalog.** A side-car index keyed on the existing
  `SystemTemplate` fields, beside the ten templates, plus resolution of a chosen `SystemTemplate` to a
  concrete `SystemEnergyCentre`, plus a **conformance test** that opens the real 1.8 MB templates (only
  in the test, never at runtime). **Concrete template selection is NOT enabled until this lands.**
- `SystemTemplate.FromJsonObject` whitespace asymmetry — 11f item 7.
- Everything in 11f items 1–5.

### 0f. The precise next task

**Step 5b**, then steps 6–10 in the order in 11g. Step 5b needs `SAM_Systems` brought into the
workstream on its own `feature/partf-terminal-transfer-compliance` branch off its `sow/2026-Q3`
— **ask Michal before creating it**, he has kept the workstream to three repos.

Files likely touched by step 5b:
- `SAM_Systems/SAM_Systems/files/resources/Analytical/Systems/SystemEnergyCentre/CapabilityIndex.JSON` (new)
- `SAM_Systems/SAM_Systems/SAM.Analytical.Systems/Query/SystemCapabilityDescriptors.cs` (new)
- `SAM_Systems/SAM_Systems/SAM.Analytical.Systems/Query/SystemEnergyCentre.cs` (existing loader, reused)
- a conformance test project under `SAM_Systems`

### 0g. Environment needed to continue

- **TAS-facing projects need VS Framework MSBuild**, not the dotnet CLI (MSB4803 / `ResolveComReference`):
  `C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe`.
  `SAM.Analytical` and `SAM.Tests` build fine under `dotnet`.
- Build **SAM before SAM_UI/SAM_Tas** — they reference its built DLLs.
- Section 9 has the rest of the gotchas.

---

## 1. Where the work is

**COMMITTED AND PUSHED in all three repos. Not merged, no PR opened.** All working trees are clean and
level with their remotes. Do not merge, squash, force-push, or open a PR unless Michal asks.

All three repos are on `feature/partf-terminal-transfer-compliance`. **SAM_Tas's branch was created in an
earlier session and needs a PR like the other two.** `sow/2026-Q3` was never committed to directly and is
untouched everywhere (SAM_Tas verified at `3d58bfe` local and remote).

**Current heads — SAM `b23bef3b`, SAM_UI `ffd8e38`, SAM_Tas `d56f679`. All pushed and verified.**

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
  - **HEAD = `b23bef3b`**, pushed
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
  - **HEAD = `d56f679`**, pushed

## 2. Validation state (all green at handover)

| Suite | Result |
|---|---|
| `SAM/SAM.Tests` | **1124 passed, 0 failed** (1046 + 16 Part O scope/identity + 5 TM extraction + 40 scenario identity + 17 capability selection) |
| `SAM_UI/WPF/SAM.Analytical.UI.WPF.Tests` | **180 passed, 0 failed** (123 + 21 placement + 7 identity + 6 whole-floor + 17 preset/scope + 6 assessment cache) |
| `SAM_Systems/SAM.Analytical.Systems.Mollier.Tests` | **123 passed, 0 failed** |
| `SAM_Mollier/SAM.Core.Mollier.Tests` | **22 passed, 0 failed** |
| `SAM.Core.Mollier.UI.WPF` | 0 `error CS` — builds unchanged against the hardened engine |
| `SAM.Analytical.Grasshopper` | 0 `error CS` under VS MSBuild |
| `SAM_Tas/SAM.Analytical.Tas.TM59.Tests` | **25 passed, 0 failed** (22 + 3 wrapper equivalence) |
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
| SAM_Tas | `5e38c94` | `OverheatingCalculator` → compatibility wrapper + 3 equivalence tests |
| SAM_Tas | `d56f679` | doc terminology |

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

### 11d. THE OPEN DEFECT — ventilation strategy has THREE conflicting derivations

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
   normalises at its own boundary (`Normalized`) so scenario identity is safe, but the shared
   serialisation path is still inconsistent for every other consumer. Fixing it means routing
   `SystemTemplate.FromJsonObject` through the setters, which changes a path `SAM_Systems` lookups use
   — **needs its own change and regression**, not a drive-by.
8. **`"R"` / `"G17"` are not runtime-stable** — both changed meaning in .NET Core 3.0, so a double
   formatted for identity differs between a .NET Framework host (Revit, gbXML) and .NET 8.
   `OverheatingOperatingAssumptions.Text(double)` is fixed to nine decimal places for that reason. Any
   *other* derived identity in SAM that formats a double has the same exposure.

### 11g. NEXT — Iteration 0 remaining steps, in this order

4. ~~**`OverheatingScenario`**~~ — **DONE**, `7faf964c` + `02e99582`. See 11c.
5. **SPLIT.** 5a **DONE** (`b23bef3b`, see 11h) — the analytical vocabulary, the Part F requirement rule
   and the pure selection rule. **5b OUTSTANDING** — the `SAM_Systems` capability catalog for the ten
   shipped templates and resolution to a concrete `SystemEnergyCentre`. **Concrete template selection is
   not enabled until 5b lands.**
6. Lift the `TasTSDQueryTM59Results` recipe into a testable service.
7. **Make the scenario authoritative** over ventilation strategy (11d).
8. **Result association** to design dwelling / common space **by identity, not name** — use
   `SimulationSpaceMap`. Three-flat isolation regression is mandatory.
9. Preserve the TSD-simple vs TPD-full routing boundary.
10. Thin headless TAS runner, **last**.

### 11h. Step 5a — system capability selection, analytical half (DONE, `b23bef3b`)

**The boundary, decided by Michal and replacing an earlier recommendation of mine.** A declared catalog
in `SAM.Analytical` would have put the names of ten `SAM_Systems` resource files inside the core
library. The split is instead the one already drawn for TAS:

```
SAM.Analytical                         │  SAM_Systems  (step 5b, NOT BUILT)
  SystemCapability (vocabulary)        │    CapabilityIndex.JSON beside the templates
  PartFSystemCapabilityRequirement     │    descriptors for the ten shipped templates
  SelectMinimumCapableSystem (rule)    │    chosen SystemTemplate → concrete SystemEnergyCentre
  — owns NO values                     │    conformance test opens the real 1.8 MB files
```

- **`SystemCapability`** — `ContinuousVentilation`, `Boost`, `SummerBypass`, `HeatRecovery`.
  Capabilities, **not equipment**. `MVRE` stays the heat-recovery identity; `HeatRecovery` is a property
  of it. It earns its place by making `MVRE` strictly more capable than `MV`, so a requirement that does
  not ask for heat recovery returns the simpler system instead of being unable to tell them apart.
- **`Query.PartFSystemCapabilityRequirement(PartFDwellingResult)`** — continuous ventilation, plus boost
  exactly when `TotalHighExtract_Lps` exceeds `ContinuousDesignSystemRate_Lps` (intermittent extract
  excluded — it is not part of the balanced system). **It never asks for summer bypass or heat
  recovery**, swept over a grid of dwellings so that cannot pass on one convenient case. A dwelling
  credited with mitigation its design does not have would pass an assessment it should fail.
- **`Query.SelectMinimumCapableSystem`** — a pure function over descriptors the caller supplies.
  **Minimum = fewest capabilities, not first found.** Ties broken by `SystemTemplate` identity, field by
  field, ordinally — asserted over every rotation AND reversal, so a directory enumeration cannot decide
  an engineering answer. **Refuses and names what was missing**; where every capability exists but never
  together on one system it says that instead. No nearest match, no default.
- Two structural locks, both mutation-verified: no member of the selection types names a file, path,
  directory or energy centre; and no static member of `SAM.Analytical` hands out descriptors.
- 17 tests in `SAM.Tests/SystemCapabilitySelectionTests.cs`. The descriptors there are a **test fixture
  standing in for the `SAM_Systems` catalog, not a copy of it**.

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
- Work on `feature/partf-terminal-transfer-compliance` in **all three** repos — SAM_Tas's was created
  this session and needs a PR like the others. **Never commit to `sow/2026-Q3` directly.**
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

Then in **all three** repos - SAM, SAM_UI and **SAM_Tas** - run:

```
git status
git log --oneline -4
git log --oneline -1 origin/feature/partf-terminal-transfer-compliance
```

Confirm each is on `feature/partf-terminal-transfer-compliance`, the tree is **clean**, and local matches
remote at: **SAM `b23bef3b`, SAM_UI `ffd8e38`, SAM_Tas `d56f679`**. Nothing should be outstanding.

### State - all pushed, all green

SAM **1124**, SAM_UI **180**, SAM_Tas TM59.Tests **25**, SAM_Systems **123**, SAM_Mollier **22**;
Grasshopper and Mollier UI 0 `error CS`; SPDX clean. Not merged, no PRs open.

Done and reviewed: the Part F regulatory correction pass, the floor-plan overlay, saved-view persistence,
the dwelling-scope correctness fix and cache proof, and the first four Iteration 0 pieces: **Part O
assessment scope**, **`SimulationSpaceMap`**, the **engine-neutral `TMOverheatingCalculator`
extraction** with its TAS compatibility wrapper, and **`OverheatingScenario`** with its derived
deterministic key, reviewed and hardened (11c, and the two new entries in 11f).

**Read section 0 first and verify the repositories against it before changing any code.**

### Your next task: Iteration 0, step 5b onward (section 11g)

5b. The **`SAM_Systems` capability catalog** for the ten shipped `SystemEnergyCentre` templates, keyed on
   the existing `SystemTemplate` fields, beside the templates; resolution of a chosen `SystemTemplate` to
   a concrete `SystemEnergyCentre`; and a conformance test that opens the real 1.8 MB files - **only in
   the test, never at runtime**. 5a is done (`b23bef3b`, section 11h) and holds the vocabulary, the Part
   F requirement rule and the selection rule. **`SAM_Systems` is not yet in the workstream - ask before
   creating its branch.**
6. Lift the `TasTSDQueryTM59Results` recipe into a testable service. Reuse, do not rewrite.
7. **Make the scenario authoritative** over ventilation strategy - see 11d, three conflicting derivations.
8. **Result association** to design dwelling / common space **by identity, not name**, via
   `SimulationSpaceMap`. Three-flat isolation regression is mandatory.
9. Preserve the TSD-simple vs TPD-full-HVAC routing boundary.
10. Thin headless TAS runner, **last**.

**Do not implement Iteration 1, 2 or 3 behaviour.** Do not start CoolBreeze.

### Rules established under review. Each has a test; do not undo any

1. **`Query.PartFDwellingZones` is the single source of truth for what a dwelling is.** Part O scope
   delegates to it and only names the remainder as common space.
2. **The corridor is assessed, never attributed to a dwelling.** `UV` -> `TM59CorridorExtendedResult`.
3. **Never attribute a result by NAME.** `SimulationSpaceMap`: stable key, unique name, then refuse.
   Every flat has a "Bedroom 2".
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
8. **Behaviour preserved, not improved** in the extraction: the `"NV"` default, the zone-name lookup, the
   silent no-assessment and the no-weather throw are all pinned, not endorsed. Fixing them is later work.
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
