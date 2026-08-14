# GitHub repository state

The connected GitHub owner session shows `dzgreeno`, and the repository visibility is **Public**.

Repository:

`https://github.com/dzgreeno/AssetRipper-DzGreen`

Description:

> AssetRipper DzGreen — advanced fork with unified Asset Workspace, grouped FBX export, CLI, MCP, and GitHub Pages downloads.

The public repository contains the full source tree, `.github`, `docs-site`, documentation, and project metadata. The project is intended to remain under the sole ownership and administration of `dzgreeno`; no additional maintainer is part of the project branding.

## Continuous integration

The release workflow restores each GUI, CLI, and MCP project for its target runtime before publishing. Run `31803525319` completed successfully for all six platform jobs and produced non-expired artifacts for Windows x64/ARM64, Linux x64/ARM64, and macOS x64/ARM64.

The corrected workflow uses the actual publish directories `AssetRipper.GUI.Free`, `AssetRipper.CLI`, and `AssetRipper.MCP` under `Source/0Bins`.

Release tag `v1.3.14-dzgreen.2` is based on the corrected main commit and is intended to publish the six platform packages and SHA256 manifests through GitHub Actions.
