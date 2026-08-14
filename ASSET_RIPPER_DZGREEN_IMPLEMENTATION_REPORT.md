# AssetRipper DzGreen — Implementation Report

## Scope

This fork is branded **AssetRipper DzGreen** and maintained by **dzgreeno**. It continues the official AssetRipper `1.3.14` / `545f345` line and keeps the upstream project link, GPL-3.0 license, Unity disclaimer, and attribution visible.

## Implemented

The repository contains a centralized fork identity used by the GUI Web shell, page titles, footer, project metadata, and Windows product metadata. The persistent GUI header exposes the upstream project, the fork repository, and Ko-fi support.

The repository includes `NOTICE.md`, a `LICENSE` pointer that leaves the authoritative `LICENSE.md` text intact, a dzgreen-specific `.github/FUNDING.yml`, a six-target build-and-release workflow, and a GitHub Pages deployment workflow for `docs-site`.

The standalone `docs-site` artifact contains the responsive Asset Atlas downloads page with direct GitHub Release naming conventions, source and checksum links, changelog, upstream comparison, support card, and local branded image assets. The public artifact contains no development-only runtime references.

## Verification

| Check | Result |
| --- | --- |
| GUI.Free build | Passed, 0 warnings, 0 errors |
| CLI build | Passed |
| MCP build | Passed |
| CLI `--help` | Passed; flags include `--fbx`, `--raw`, `--inspect-prefab`, `--batch-process`, and `--include-anim` |
| MCP initialize and tools/list | Passed with protocol `2026-07-28` |
| MCP stdout/stderr separation | Passed; smoke request produced no stderr diagnostics |
| Web TypeScript check | Passed |
| Web Vite build | Passed |
| Desktop visual check | Passed at 1440px viewport |
| Mobile visual check | Passed at 390px viewport |
| GitHub Pages artifact | Passed; local assets present and public paths verified |

## Prepared Windows package

The release package is `AssetRipper-DzGreen-v1.3.14-win-x64.zip`. It contains the self-contained GUI, CLI, MCP server, notices, quick-start material, and MCP configuration example.

The package SHA256 is:

```text
ed69bab919b79aba00b232dce195d9c4d1d0fe1b96bbd1fe3c08c04aa3d0969a
```

The package was built on Linux, so actual Windows PE launch, Windows native dialogs, Unity sample import, and deformation playback still require a Windows test pass. The package structure, CLI/MCP protocol behavior, and source build were verified.

## Public ownership and attribution

The current public repository is `https://github.com/dzgreeno/AssetRipper-DzGreen`. The current owner and maintainer shown in project branding are **dzgreeno**. AssetRipper upstream attribution, the Unity disclaimer, and GPL-3.0 licensing notices remain intact as required by the original project.

The six platform release assets are published under `v1.3.14-dzgreen.2`, with SHA256 manifests and the GitHub Pages downloads page available from the fork repository.
