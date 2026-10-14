# About dialog SAM-BIM branding (Q4 PR record)

Status: open PR into `sow/2026-Q4`. Base: `sow/2026-Q4`.

## Work
- `SAM.Core` About text (`Query/AboutInfoTypeText.cs`, `Enums/AboutInfoType.cs`) now presents SAM-BIM, https://sambim.xyz and https://github.com/SAM-BIM. This single source feeds both the Grasshopper `SAM.About` component and the SAM_UI About button.
- Six Grasshopper `Kernel/AssemblyInfo.cs` files: author/contact now SAM-BIM; email addresses replaced by the website. `SAM.Core.Grasshopper` icon changed from `HL_Logo24` to `SAM_Small`.

## Decisions
- Enum member `AboutInfoType.HoareLea` renamed `SAMBIM` (value 0 unchanged): the Grasshopper menu lists member names, and the component saves the integer value, so existing files are unaffected.
- Original development at Hoare Lea remains as a clearly historical credit in the SAM info text.
- Retained: `HoareLea/ScriptsHydra` URLs in `ExportHydra.cs` (external repository needed by the feature); unused `HL_Logo24` resources.
- No GUID, assembly name, namespace or component identity changed.

## Validation (local)
- MSBuild Debug of SAM.Core.Grasshopper, Architectural, Geometry, Math, Weather: 0 compile errors. Analytical: compiles; only the post-build copy step fails locally (build run with RunPostBuildEvent=Never was ignored by that csproj).
- Not done: runtime check of the About component in Grasshopper.

## Next step
Owner review; merge only on approval and green CI, then post-merge closeout in `PROJECT_PROGRESS.md`.
