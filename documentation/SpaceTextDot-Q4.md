# Rhino Text Dot support for Analytical Spaces — Q4 (2026-10-14)

Status: implemented on `feature/gh-space-textdot-q4`; draft PR #197, owner review required. Not merged.
Base: `sow/2026-Q4` at `5708c037370bc1c78b864ca6c12e2b0c7d91eeff`.
PR: SAM-BIM/SAM#197 (draft). The validated content head is `ab8a12d3`; the record-only commit that adds this
line follows it. The PR description holds the final head and hosted checks.

## Why (root cause)

`SAMAnalytical.CreateSpace` (1.0.2) failed with

```
Data conversion failed from Model Text Dot to ISAMGeometry
```

when a Rhino Text Dot was supplied through `Query Model Objects → Model Object`. The Rhino 8 Model Object is a
`Grasshopper.Rhinoceros.Model.ModelObject` (an `IGH_Goo`, not a `GH_GeometricGoo`). `GooSAMGeometry.CastFrom` handled
the Rhino geometry types and generic `IGH_Goo.Value`, but not a Text Dot and not the Model Object (it has no public
`Value`), so the cast to `ISAMGeometry` failed.

A Text Dot also carries the value that makes it a natural space seed: its **text** (name) and its **position**
(location). A location-point-only conversion would drop the name, so `GooSpace` accepts it directly.

## Change

- New `SAM.Geometry.Grasshopper.Query.TryGetTextDot(object, out Rhino.Geometry.TextDot)`:
  resolves a Text Dot from a Rhino `TextDot`, a `GH_TextDot`, a `RhinoObject`/`ObjRef`, or any Grasshopper goo whose
  standard `IGH_Goo.CastTo<TextDot>` succeeds — which includes the Rhino 8 Model Object (`ModelObject.CastTo` handles
  `TextDot` and `GH_TextDot`). Null, deleted or unrelated values resolve to `false`.
- `GooSAMGeometry.CastFrom`: a resolved Text Dot becomes its location `Point3D`. This fixes the reported conversion
  error when a Model Object is wired to `locationPoint_`.
- `GooSpace.CastFrom` (new override): a resolved Text Dot becomes a `Space` with the Text Dot text as the name
  (blank → `Cell`, the existing fallback) and its position as the location. Wiring a Model Object or Text Dot to the
  `space_` input of `SAMAnalytical.CreateSpace` therefore yields the name and the location. Other sources (a SAM
  `ISpace`, `GH_*` geometry) are unchanged via `GooJSAMObject<ISpace>.CastFrom`.
- `GooSpaceParam` (the `Space` parameter): `Prompt_Singular`/`Prompt_Plural` now filter
  `ObjectType.Point | ObjectType.TextDot` and convert a selected Text Dot through `GooSpace.CastFrom`. Selection is
  the standard Grasshopper reference "Set one / Set several" workflow; the resulting Space persists like any other
  `GooSpace` (JSON). A null or unexpected object is skipped instead of cancelling the whole pick.
- `SAMAnalytical.CreateSpace`: version 1.0.2 → 1.0.3, input/component descriptions mention the Text Dot, and a
  non-metre document now raises the existing `Geometry.Rhino.Query.ModelUnitsWarning` as a Warning when a Space
  source or a location point is supplied (SAM reads Rhino geometry as metres without scaling). No input, GUID or
  default changed; all existing inputs behave as before.

No Rhino dependency was added to `SAM.Core` or `SAM.Analytical`; the Rhino/Grasshopper types stay in the
`SAM.Geometry.Grasshopper` / `SAM.Analytical.Grasshopper` integration assemblies.

## Validation (local, this machine)

```powershell
dotnet build SAM.sln -c Release                                              # 0 errors
dotnet test Grasshopper/SAM.Core.Grasshopper.Tests/SAM.Core.Grasshopper.Tests.csproj -c Release `
  -p:SolutionDir="<repo>\"
```

- Full `SAM.sln` Release build: **0 errors**.
- `SAM.Core.Grasshopper.Tests` (headless Rhino 8 + Grasshopper via Rhino.Testing): **56 passed / 0 failed**, of which
  11 are new:
  - `TextDotSpaceTests`: resolve from `TextDot`, `GH_TextDot` (the `IGH_Goo.CastTo` path), and null/unsupported;
    `GooSAMGeometry.CastFrom` → `Point3D`.
  - `SpaceTextDotTests`: `GooSpace.CastFrom` → `Space` name/location from `TextDot` and `GH_TextDot`; a **real**
    `Grasshopper.Rhinoceros.Model.ModelObject` built from a headless `RhinoDoc` Text Dot gives the expected Space and
    the expected `Point3D` (the two reported workflows); blank text → `Cell`; a SAM `ISpace` is unchanged.
- `SAM.Core.Grasshopper.Tests` was added as a reference to `SAM.Analytical.Grasshopper`, and its setup fixture is now
  `global::Rhino.Testing.Fixtures.RhinoSetupFixture` (that reference brings the `SAM.Core.Rhino` namespace into scope).
- **Not run:** hosted CI; the interactive Rhino/Grasshopper UI test (see below).

## Manual test (Rhino 8/9 + Grasshopper)

1. In Rhino, add a Text Dot (`TextDot` command) with text `Living Room` at `(5, 3, 1)`; set the document units to
   Meters.
2. `Query Model Objects` → `Model Object` (the Text Dot) → `SAMAnalytical.CreateSpace.space_`.
3. Expected: the output Space has Name `Living Room` and location `(5, 3, 1)`; no conversion error.
4. Alternative: right-click the `Space` parameter → *Set One Space* (or *Set Several Spaces*) and pick the Text Dot
   from the viewport.
5. Regression: the same component with a SAM Space, a `Point`/`Point3D`, and a default (no input) still produces the
   previous results.
6. In a non-metre document, step 2 shows the metres Warning and still creates the Space.

## Remaining / next

- Owner review; no merge without approval.
- Locate end-to-end use (room labels on `Ground Floor::Labels` / `Upper Floor::Labels` in the house model,
  `documentation/PanelsBySolid-Q4.md`) is a manual follow-up.
