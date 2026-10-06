# SAM-BIM runtime URLs (Q4) - SAM

Branch `fix/sam-bim-runtime-urls-q4` -> base `sow/2026-Q4`. Record date: 2026-10-06.

## Current status

PR open, **not merged**. Source-only, minimum change: product links that still pointed at the HoareLea
repositories now point at the equivalent SAM-BIM ones. No `.gitmodules`, gitlink, workflow, `master`,
`sow/2026-Q3` or icon-redesign change.

## Work completed

| File | Old | New | Effect |
|---|---|---|---|
| `SAM/SAM.Core/Query/LatestVersion.cs` | `api.github.com/repos/HoareLea/SAM_Deploy/releases/latest` | `api.github.com/repos/SAM-BIM/SAM_Deploy/releases/latest` | The "SAM Version" Grasshopper component compared the installed version with HoareLea's last release (`v20260630.1`), so every current SAM-BIM install was told an update was available. It now follows SAM-BIM's latest release (`v20261006.1`). |
| `Grasshopper/SAM.Analytical.Grasshopper/Component/SAMAnalyticalCreateAnalyticalModelByAdjacencyCluster.cs`, `...BySpaces.cs` | `Delivered by SAM https://github.com/HoareLea/SAM [date]` | `Delivered by SAM https://github.com/SAM-BIM/SAM [date]` | Default description text of a newly created analytical model. |
| `Grasshopper/SAM.Core.Grasshopper/Component/SAMCoreFilterByType.cs`, `SAMCoreInspect.cs` | `https://github.com/HoareLea/SAM` | `https://github.com/SAM-BIM/SAM` | "Source code" context-menu action (`OnSourceCodeClick`). Matches `AppendSourceCodeAdditionalMenuItem`, which already uses `github.com/SAM-BIM`. |

## Decisions and assumptions

- SAM-BIM is the authoritative ecosystem; HoareLea is no longer the synchronised operational source.
- Every new destination was probed first: `SAM-BIM/SAM_Deploy` latest release is public (`v20261006.1`,
  not a draft or prerelease); `SAM-BIM/SAM` is public with issues and wiki enabled. The version string
  comparison itself is unchanged (`CurrentVersion` is the `version` file written by the installer; `LatestVersion` is the release `tag_name`).
- Left unchanged on purpose (owner decisions): `ExportHydra.cs` clones `HoareLea/ScriptsHydra`, which is a
  **private** repository with no SAM-BIM equivalent (a replacement cannot be invented); author/contact strings in the
  `Kernel/AssemblyInfo.cs` files ("... at Hoare Lea", `@hoarelea.com`) and the unused `AboutInfoType.HoareLea` text are
  authorship/provenance metadata.
- No unit seam exists for `LatestVersion` (it performs the HTTP call directly); no refactor was made to add one.

## Files changed

The five `.cs` files above plus this record. Five product lines changed (one token each).

## Validation

- `git diff --check` clean; diff reviewed line by line (5 insertions, 5 deletions in product code).
- `msbuild SAM.sln -p:Configuration=Release` with `APPDATA`/`USERPROFILE` redirected: 0 errors.
- `SAM.Core.dll` contains `SAM-BIM/SAM_Deploy/releases/latest` and no longer contains `HoareLea/SAM_Deploy`.
- `dotnet test SAM/SAM.Tests/SAM.Tests.csproj -c Release`: first full run 2818 passed, 1 failed
  (`PartODwellingStrategyMaterialisationTests.SystemsScope_ScaffoldingIsTheAddMechanicalSystemsShape`: a
  pre-existing thread-safety race on a shared dictionary in `PartFData.GetPartFCategory`, unrelated to URLs); the class then
  passed 145/145 twice and a second full run passed 2819/2819. The race is tracked separately and not fixed here.
- The two Grasshopper components and the `Delivered by SAM` text were compiled but their strings were not byte-checked in the `.gha` outputs.
- Rhino-hosted Grasshopper tests were not run (they do not run under a redirected profile).
- PR CI (`build`, `test`, `spdx`) results are recorded in the PR description.

## Unresolved issues, risks

- Intermittent `PartFData` race noted above (pre-existing, separate task).

## Exact next step

Maintainer review and merge into `sow/2026-Q4` once CI is green (not merged automatically). After merge, the
`PROJECT_PROGRESS.md` closeout is a direct docs commit on `sow/2026-Q4` (never on this branch).
