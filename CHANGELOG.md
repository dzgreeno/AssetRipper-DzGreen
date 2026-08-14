# AssetRipper DzGreen — Development Changelog

## 1.3.15-dzgreen.3-dev

This unreleased development build hardens the processing pipeline while keeping the current public release unchanged. Import failures are isolated and reported through a concurrent processing-issue registry, with an opt-in strict mode for CI and automation. Unity file discovery now covers nested sibling files, while the Asset Workspace keeps the complete asset set available for filtering and warns when a dataset is large.

Character assembly now indexes both `SkinnedMeshRenderer` and `MeshFilter` components, resolves hierarchy-linked materials and textures, and records missing skin weights and animation links without rewriting source references. GLB export includes skinned meshes, cached joint nodes, corrected submesh/material mapping, and AnimationClip TRS tracks. FBX ASCII export includes collision-safe texture sidecars, bind-pose-aware transform links, and isolated malformed animation clips.

The CLI exposes strict processing diagnostics and returns a non-zero status when recoverable issues remain. The MCP stdio server documents the supported lifecycle versions, exposes processing issues as a read-only tool, validates the declared output contract, and propagates strict mode to controlled loads and exports. CI now follows the repository's `main` branch and performs restore, build, and test as separate steps.

Serialized-file parsing now derives a generation-appropriate Unity version for legacy formats and malformed signatures instead of using a fixed modern fallback. MCP input and output paths are normalized and can be confined with `ASSETRIPPER_MCP_ALLOWED_ROOTS`.

The public `main` branch and its published release are intentionally not changed by this development entry.
