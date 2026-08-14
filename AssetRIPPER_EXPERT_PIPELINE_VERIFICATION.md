# AssetRipper Expert Pipeline — Verification

## Scope

This package extends the existing AssetRipper 1.3.14-custom tree. It is not a new project. The GUI, grouped FBX exporter, CLI, and MCP server share the same import/processing pipeline and preserve the no-DRM-bypass constraint.

## Delivered capabilities

The FBX pipeline now traverses the complete Transform hierarchy, emits local transforms and coordinate conversion, resolves static and skinned renderers, writes normalized vertex weights and bind poses as FBX skin clusters, preserves UV channels, normals, tangents, vertex colors, materials, texture sidecars, and emits blend-shape geometry and mapped blendShape animation curves when the Unity source exposes them. TRS AnimationClip curves and curve tangents are written when `--include-anim` is enabled.

`AssetRipper.CLI` supports repeatable `--input`, `--output`, `--filter`, `--include-anim`, `--raw`, `--fbx`, `--inspect-prefab`, and `--batch-process` options. `AssetRipper.MCP` implements the MCP JSON-RPC 2.0 stdio lifecycle and exposes `list_assets`, `inspect_prefab`, `export_fbx_with_anim`, and `batch_process`.

## Build and test results

| Check | Result |
|---|---|
| GUI.Free Release build | 0 warnings, 0 errors |
| Common tools build | 0 warnings, 0 errors |
| CLI build | 0 warnings, 0 errors |
| MCP build | 0 warnings, 0 errors |
| CLI help smoke test | Passed |
| MCP initialize/tools/list/ping smoke test | Passed |
| MCP tool error handling | Passed; errors returned as `isError: true` |
| MCP stdout/stderr separation | Passed; protocol JSON remained on stdout and diagnostics on stderr |
| GUI, CLI, MCP published binaries | Windows PE32+ console x86-64 |
| Win-x64 runtime config | `net10.0`, self-contained publish |

A full deformation test with the user's exact Unity sample still requires running the new Windows package against those files. The Linux sandbox cannot execute the Windows PE binaries, and no user sample is stored in the repository.

The final SHA256 is recorded in the external verification file delivered alongside the ZIP so the package hash remains independently verifiable.

## References

- https://modelcontextprotocol.io/specification/2026-07-28
- https://modelcontextprotocol.io/specification/2026-07-28/server/tools
- https://github.com/modelcontextprotocol/csharp-sdk
