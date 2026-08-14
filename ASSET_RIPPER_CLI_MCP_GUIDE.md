# AssetRipper CLI and MCP Integration

This extension adds two standalone entry points to the existing AssetRipper 1.3.14-custom tree: `AssetRipper.CLI` for deterministic command-line processing and `AssetRipper.MCP` for a local Model Context Protocol server over stdio.

## CLI

The CLI accepts Unity files or directories, expands the same sibling bundle/cab/resource companions used by the GUI, processes the files through AssetRipper, and emits machine-readable JSON on stdout. Its export path is guarded against filesystem roots and imported directories.

```text
AssetRipper.CLI --input <file-or-directory> [options]

--input, -i <path>       Repeatable Unity file or directory input.
--output, -o <dir>       Export directory.
--filter <query>         Name, class, collection, or Path ID filter.
--limit <n>              Asset list limit.
--inspect-prefab         Report hierarchy, components, meshes, bones, materials, textures, clips, skin weights, and blend shapes.
--fbx                    Export one resolved character/prefab as grouped FBX.
--include-anim[=bool]    Include AnimationClip TRS curves and mapped blendShape curves; default true.
--raw                    Write per-asset raw JSON files under output/raw.
--batch-process          Run a batch operation; combine with --raw and/or --fbx.
```

Examples:

```powershell
AssetRipper.CLI.exe --input "C:\Game\Game_Data" --inspect-prefab --filter hero
AssetRipper.CLI.exe --input "C:\Game\Game_Data" --output "C:\Exports\Hero" --fbx --filter hero --include-anim
AssetRipper.CLI.exe --input "C:\Game\Game_Data" --output "C:\Exports\Batch" --batch-process --raw --fbx
```

The grouped FBX path preserves the reconstructed Transform hierarchy, local transforms, static and skinned renderers, UV channels, normals, tangents, vertex colors, normalized bone weights, bind poses, material texture sidecars, blend-shape geometry when present, AnimationClip TRS curves, and mapped `blendShape.*` curves when the source clip and mesh expose them. Coordinate conversion is applied consistently by the existing exporter: Unity positions are converted to the FBX scene convention, rotations are converted before writing model properties, and texture UV orientation is handled by the mesh data pipeline.

## MCP stdio server

The server implements the MCP JSON-RPC 2.0 lifecycle over newline-delimited stdio. It supports `initialize`, `notifications/initialized`, `ping`, `tools/list`, and `tools/call`. The exposed tools are deterministic and ordered:

| Tool | Purpose |
|---|---|
| `list_assets` | List processed assets with optional filter and limit. It can load `inputPaths` on the same request. |
| `inspect_prefab` | Inspect a character or prefab root, including hierarchy, components, meshes, skin-weight diagnostics, blend-shape counts, materials, textures, animation clips, and bone count. |
| `export_fbx_with_anim` | Export one resolved character/prefab to a grouped FBX directory with optional animation curves. |
| `batch_process` | Run controlled raw JSON and/or grouped FBX processing and write a manifest. |

The MCP process writes protocol responses only to stdout. Diagnostics are written to stderr. The server does not execute arbitrary shell commands, does not decrypt or bypass DRM, and only writes to the explicitly supplied output directory. Hosts should present a human confirmation step before invoking export tools, consistent with MCP's tool safety guidance.

Example MCP client configuration:

```json
{
  "mcpServers": {
    "assetripper": {
      "command": "C:\\Tools\\AssetRipper\\tools\\AssetRipper.MCP.exe",
      "args": []
    }
  }
}
```

For a framework that launches .NET DLLs instead of Windows executables:

```json
{
  "mcpServers": {
    "assetripper": {
      "command": "dotnet",
      "args": ["C:\\Tools\\AssetRipper\\tools\\AssetRipper.MCP.dll"]
    }
  }
}
```

## Verification

The build and protocol smoke tests cover JSON syntax, `initialize`, tools capability discovery, deterministic tool ordering, `ping`, tool error results, stderr separation, and zero-warning compilation. A deformation test using the user's actual Unity files still needs to be run on Windows because the sandbox does not contain those game files.

## References

[1] [MCP Specification 2026-07-28](https://modelcontextprotocol.io/specification/2026-07-28)

[2] [MCP Server Tools Specification](https://modelcontextprotocol.io/specification/2026-07-28/server/tools)

[3] [Official Model Context Protocol C# SDK](https://github.com/modelcontextprotocol/csharp-sdk)
