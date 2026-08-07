<!-- SPDX-License-Identifier: LGPL-3.0-or-later -->
<!-- Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors -->

# Part F — session handover

Paste the block at the bottom into a new session to continue. Everything above it is the detail that
block refers to.

---

## 1. Where the work is

**COMMITTED AND PUSHED as a review checkpoint** (2026-08-07). Not merged, no PR opened — Michal wants an
independent code review of the pushed branches first.

- **SAM**: `feature/partf-terminal-transfer-compliance`, on `sow/2026-Q3` @ `34dea440`.
  - `cd54a62b` shared `Solver2D` hardening + `Solver2DTests`
  - `ae921be4` the Part F analytical body (correction pass, new classes/enums/tests, 2 GH components, rule set, docs)
- **SAM_UI**: `feature/partf-terminal-transfer-compliance`, on `sow/2026-Q3` @ `074f3d9`.
  - `e787105` shared 2D-view infrastructure (`FloorPlan2DControl.Overlay`/`Plane`/`WorldToScreen`/`ViewChanged`,
    `AdjacencyCluster.SpaceSectionFace2Ds`, label-solver diagnostic reading `ResultType`)
  - `8782c40` Part F presentation checkpoint (window, overlay, view settings, placement adapter, annotation
    identity, all tests). One commit on purpose: the correction-pass fixes, the persistence types and the
    placement adapter are mutually dependent and the window is a single new file containing all three.

## 2. Validation state (all green at handover)

| Suite | Result |
|---|---|
| `SAM/SAM.Tests` | **1046 passed, 0 failed** (1031 + 15 `Solver2DTests`) |
| `SAM_UI/WPF/SAM.Analytical.UI.WPF.Tests` | **151 passed, 0 failed** (123 + 21 placement + 7 identity) |
| `SAM_Systems/SAM.Analytical.Systems.Mollier.Tests` | **123 passed, 0 failed** |
| `SAM_Mollier/SAM.Core.Mollier.Tests` | **22 passed, 0 failed** |
| `SAM.Core.Mollier.UI.WPF` | 0 `error CS` — builds unchanged against the hardened engine |
| `SAM.Analytical.Grasshopper` | 0 `error CS` under VS MSBuild |
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
- Terminal marks are a **point + direction** drawn as a short screen-space stub; only TRA marks span
  real distance.
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

## 5d. Superseded — original brief for task B

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

## 6. NEXT: Part F in the NORMAL saved 2D View (tasks 15–20, not started)

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

## 7. Screenshots still owed (blocked on Michal running the app)

Whole Level 0 with all flats; Flat 1 only; Flat 2 only; Continuous; Setback; colour scheme + overlay
together; before/after auto placement; one manually moved KEX tag with leader; view after
close/reopen proving persistence.

## 8. Environment gotchas

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

## 9. Standing instructions

- Do not commit, push or open PRs until Michal has reviewed. Then: SAM first (SAM_UI CI dep-clones
  it), PRs against `sow/2026-Q3` in both repos, CI green **and** the Codex inline comments read.
- Attribution line `Generated by Michal Dengusiak and CodeClaude` on every commit and PR body.
- SPDX header on every changed `.cs`.
- Use the SAM implementation-summary style for the final response.

---

## Paste this into the new session

Continue the Approved Document F work in the SAM-BIM workspace.

**First: read `SAM/documentation/PartF-HANDOVER.md` in full.** It holds the state, the decisions and
the gotchas. Then run `git status` in both SAM and SAM_UI and confirm the uncommitted changes on
`feature/partf-terminal-transfer-compliance` are still present in both.

The regulatory correction pass, the floor-plan overlay that replaced the old node diagram, the saved-view
persistence spike, **the `Solver2D` adoption and the Revit-style tag language (section 5)** are all DONE and
green: SAM 1046 tests, SAM_UI 151, SAM_Systems 123, SAM_Mollier 22, Grasshopper 0 `error CS`, SPDX clean.
Four commits are pushed on `feature/partf-terminal-transfer-compliance` in SAM and SAM_UI (section 1) as a
review checkpoint — **not merged, no PR open**. Do not merge or open one unless Michal asks.

**Your next task is section 6: Part F in the NORMAL saved 2D View.** Read section 5 first — it records the
placement engine, the annotation-scale rule and the annotation-identity rule you must build on, and 5e records
the agreed tag language. Note that a screenshot of the revised tags is owed to Michal before this starts.

The things section 6 has to respect, which are already built and tested:

- `PartFTagPlacement.Solve(items, overrides, obstacles)` is the ONLY placement path. Do not add a second
  one, and do not place anything in the renderer.
- A manual position is a `PartFAnnotationOverride` keyed on `PartFOverlayMark.AnnotationGuid` +
  `AnnotationType`, holding the label's **centre** as a world-plane `Point2D`. The adapter already turns
  those into solver obstacles; the window's `Overrides()` just needs to return the real list instead of an
  empty one, from the view settings.
- Placement must keep running on input change only. `FloorPlan_ViewChanged` re-solves on a scale change
  and never on a pan; a drag must re-solve once on release, not per mouse move.
- Whole-floor rendering as N independent overlays still needs its explicit regression test (see section 6).

Do not commit or push.
