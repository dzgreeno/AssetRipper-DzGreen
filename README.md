# AssetRipper DzGreen

[![Build and Release](https://github.com/dzgreeno/AssetRipper-DzGreen/actions/workflows/build-and-release.yml/badge.svg)](https://github.com/dzgreeno/AssetRipper-DzGreen/actions/workflows/build-and-release.yml)
[![License: GPL-3.0](https://img.shields.io/badge/license-GPL--3.0-9BE15D.svg)](LICENSE.md)
[![Support on Ko-fi](https://img.shields.io/badge/support-Ko--fi-B66D47.svg)](https://ko-fi.com/dzgreen)
[![Upstream](https://img.shields.io/badge/upstream-AssetRipper-343A35.svg)](https://github.com/AssetRipper/AssetRipper)
[![Downloads site](https://img.shields.io/badge/downloads-AssetRipper%20DzGreen-2E8B57.svg)](https://dzgreeno.github.io/AssetRipper-DzGreen/)

**AssetRipper DzGreen** is an independently maintained, advanced fork of the official [AssetRipper](https://github.com/AssetRipper/AssetRipper) project. It is maintained by **dzgreen** and continues the upstream `1.3.14` / `545f345` line with a focused workflow for Unity asset analysis, character assembly, FBX export, CLI automation, and MCP integration.

> This repository preserves upstream attribution and the GNU GPL-3.0 license. It is not sponsored by, affiliated with, or endorsed by Unity Technologies.

## Why this fork exists

AssetRipper is already a powerful tool for analyzing Unity game files. DzGreen extends the post-extraction workflow so that users can move from a large asset set to a useful, inspectable result without losing context between the file list, hierarchy, preview, dependencies, and export actions.

## Feature map

| Capability | AssetRipper upstream | AssetRipper DzGreen |
| --- | --- | --- |
| Unity asset analysis | Core import and inspection | Core import plus a unified Asset Workspace |
| Asset navigation | Asset and detail pages | Search, global filters, hierarchy, inspector, context tabs, and collapse controls |
| Character workflow | Asset-level export path | Grouped prefab assembly with meshes, bones, textures, Animator data, BlendShapes, and sibling clips |
| FBX output | Upstream export capabilities | Extended hierarchy, UV channels through UV7, texture transforms, bind matrices, skin clusters, BlendShapes, and animation curves |
| Automation | GUI-first workflow | `AssetRipper.CLI.exe` with JSON output and an MCP stdio server |
| Tool integration | Project-specific APIs | `list_assets`, `inspect_prefab`, `export_fbx_with_anim`, and `batch_process` MCP tools |
| Distribution | Upstream release channels | Fork-specific Windows package plus reproducible multi-platform GitHub Actions workflow |

## Current prepared package

The latest prepared Windows package contains the GUI, CLI, MCP server, and supporting documentation.

| Field | Value |
| --- | --- |
| Base line | AssetRipper `1.3.14` / commit `545f345` |
| Package | `AssetRipper-DzGreen-v1.3.14-win-x64.zip` |
| Platform | Windows x64, self-contained |
| Size | Approximately 248 MB |
| Files | 1,335 |
| SHA256 | `a3b8772b8a1c53c9517040f734142391d265e2a27dfab77e46d30682f6155ac0` |

The source tree also contains a GitHub Actions release workflow for Windows x64/ARM64, Linux x64/ARM64, and macOS x64/Apple Silicon. Release links become active at [`dzgreeno/AssetRipper-DzGreen`](https://github.com/dzgreeno/AssetRipper-DzGreen) after the first tag is published.

## Getting started

For the prepared Windows build, extract the ZIP and launch `AssetRipper.GUI.Free.exe`. The GUI terminal is intentionally visible so that diagnostics can be inspected and the process can be stopped cleanly. The Asset Workspace appears on the home page after data is loaded; its asset list, hierarchy, actions panel, and preview focus can be collapsed independently.

The CLI is available at `tools/CLI/AssetRipper.CLI.exe`. A minimal invocation is:

```powershell
tools\CLI\AssetRipper.CLI.exe --input "C:\path\to\unity-data" --output "C:\path\to\export" --fbx --include-anim
```

Useful options include `--filter`, `--raw`, `--inspect-prefab`, and `--batch-process`. Run `--help` for the full parser and JSON output examples.

The MCP stdio server is available at `tools/MCP/AssetRipper.MCP.exe`. Copy [`assetripper-mcp-config.example.json`](assetripper-mcp-config.example.json) into the configuration directory of the MCP client you use, then point its command to the extracted executable. Protocol responses are written to stdout; diagnostics are written to stderr.

## Building from source

The fork uses .NET 10 and the solution file `AssetRipper.slnx`.

```bash
dotnet restore AssetRipper.slnx
dotnet build Source/AssetRipper.GUI.Free/AssetRipper.GUI.Free.csproj -c Release -p:PublishAot=false
dotnet publish Source/AssetRipper.GUI.Free/AssetRipper.GUI.Free.csproj -c Release -r win-x64 --self-contained true -p:PublishAot=false
dotnet publish Source/AssetRipper.Tools.CLI/AssetRipper.Tools.CLI.csproj -c Release -r win-x64 --self-contained true -p:PublishAot=false
dotnet publish Source/AssetRipper.Tools.MCP/AssetRipper.Tools.MCP.csproj -c Release -r win-x64 --self-contained true -p:PublishAot=false
```

For cross-platform packages, use the checked-in workflow [`build-and-release.yml`](.github/workflows/build-and-release.yml). It builds the GUI, CLI, and MCP server for the six supported runtime targets, creates archives, writes SHA256 manifests, and publishes tagged releases.

## GitHub Pages downloads site

The prepared static site lives in [`docs-site`](docs-site). It contains the branded downloads and documentation landing page, while the source design project is maintained separately in the AssetRipper DzGreen web project. [`pages.yml`](.github/workflows/pages.yml) deploys the `docs-site` directory through GitHub Pages.

## Legal and attribution

The complete GPL-3.0 license text remains in [`LICENSE.md`](LICENSE.md). The short [`LICENSE`](LICENSE) pointer and [`NOTICE.md`](NOTICE.md) identify the fork maintainer and preserve upstream attribution without replacing the authoritative license text. Unity is a registered trademark of Unity Technologies or its affiliates; this project is not affiliated with Unity.

## Support and contributions

If this fork saves you time, you can support independent maintenance through [Ko-fi](https://ko-fi.com/dzgreen). Issues and pull requests are welcome once the fork repository is public. Please include the operating system, runtime target, input type, command or GUI action, and relevant log excerpt when reporting a problem.
