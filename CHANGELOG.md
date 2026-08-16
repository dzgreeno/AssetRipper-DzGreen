# AssetRipper DzGreen — Development Changelog

## 1.3.15-dzgreen.3-dev

This unreleased development build hardens the processing pipeline while keeping the current public release unchanged. Import failures are isolated and reported through a concurrent processing-issue registry, with an opt-in strict mode for CI and automation. Unity file discovery now covers nested sibling files, while the Asset Workspace keeps the complete asset set available for filtering and warns when a dataset is large.

Character assembly now indexes both `SkinnedMeshRenderer` and `MeshFilter` components, resolves hierarchy-linked materials and textures, and records missing skin weights and animation links without rewriting source references. Sibling AnimatorController bundles are associated with a character only through resolved controller references or an exact controller/root-name match, allowing separated animation bundles to appear in Workspace inspection and character exports.

GLB export includes skinned meshes, cached joint nodes, corrected submesh/material mapping, and AnimationClip TRS tracks. If an input exposes weighted vertices but no resolvable bone references, GLB now preserves the mesh as a logged rigid fallback rather than silently dropping it. FBX ASCII export includes collision-safe texture sidecars, bind-pose-aware transform links, isolated malformed animation clips, document-root connections, standard object definitions, valid animation property quoting, mesh-node typing, and correct bone-to-cluster relationship direction for independent importer compatibility.

The assembled-character Workspace now exports the selected root directly from its `Export FBX` button. The action no longer redirects to the general project-export page: it writes the FBX and texture sidecars to `<ExportRootPath>/AssetWorkspace`, includes associated animation clips, and reports the resulting local path in the preview status area.

Workspace character export now starts a browser download of a Blender bundle while retaining the local export under `<ExportRootPath>/AssetWorkspace`. Each bundle includes the validated GLB scene with embedded textures, mesh skinning, and animation clips; on Windows it also includes a binary FBX converted from that GLB with the bundled Assimp 5.3.1 converter. A legacy ASCII FBX plus texture sidecars remains available only for tools that require it, with an in-bundle README explicitly noting that Blender must use the GLB or binary FBX.

The Workspace now uses the same Asset Atlas design language as the dzgreeno project site: graphite and forest surfaces, ivory content, DzGreen lime action states, copper annotations, atlas-style metadata, and a clearer character/export flow. The render area adds direct camera framing, perspective or orthographic projection, auto-rotation, PNG capture, camera-distance, lighting, backdrop, animation playback, and animation-speed controls. Blender bundle delivery now uses a direct browser-download route rather than a Blob fetch, exposes a visible `Direct download` link, and adds an `Open export folder` action so the completed local bundle remains accessible when a browser blocks automatic downloads.

Android and other Unity files that contain an embedded serialized Type Tree now get a safe best-effort fallback when the generated class reader cannot consume their schema. The fallback neither modifies input data nor attempts decryption or protection bypasses; it preserves the object for raw inspection and dependency analysis and records a specific recovery warning. Objects without a usable Type Tree remain quarantined with the original diagnostics instead of stopping the rest of the import.

The bottom status dock now provides `Copy full log` and `Save log`. These user-initiated actions expose every diagnostic line captured since application startup, including multiline exception details, without the old 500-character truncation or 120-line live-view limit. The live dock remains compact while the full artifact is available at `/Status/Full`.

Asset Workspace now sends only the first 200 rows for a large library and retrieves additional filtered pages on demand. Search, class, collection, and category filters run against the complete indexed asset set through the local endpoint. This keeps very large Android libraries responsive while preserving their complete list for browsing. To avoid expensive all-component traversal during first paint, automatic character reconstruction is deferred for libraries larger than 8,000 indexed assets; users can narrow to a root asset before inspection.

The CLI exposes strict processing diagnostics and returns a non-zero status when recoverable issues remain. The MCP stdio server documents the supported lifecycle versions, exposes processing issues as a read-only tool, validates the declared output contract, and propagates strict mode to controlled loads and exports. CI now follows the repository's `main` branch and performs restore, build, and test as separate steps.

Serialized-file parsing now derives a generation-appropriate Unity version for legacy formats and malformed signatures instead of using a fixed modern fallback. MCP input and output paths are normalized and can be confined with `ASSETRIPPER_MCP_ALLOWED_ROOTS`.

The public `main` branch and its published release are intentionally not changed by this development entry.
## 1.3.15-dzgreen.18 RC2 — Local Final Candidate

This local candidate adds deterministic CI artifacts, defensive input handling, and auditable RC2 evidence without changing the public release or attempting to process encrypted or protected Unity data. The CLI supports `--ci`, compact JSON output, stable exit codes (`0` success, `1` unexpected failure, `2` invalid arguments, `3` recoverable issues, `4` missing input), and omits generated timestamps from diagnostics and batch manifests in CI mode.

Fallback-texture catalogs are restricted to top-level regular image files, reject reparse points and zero-byte candidates, and impose a 64 MiB per-file ceiling. Output roots and output-inside-input locations remain rejected. Short or malformed direct-file inputs are rejected before the importer runs. Bundle headers with an invalid signature or a negative version now raise a normal `InvalidDataException` rather than terminating the CLI process.

The authorized F1 legacy-skinned and F2 Android multi-file fixture runs were repeated twice in CI mode. Their diagnostics JSON and batch manifests compared byte-for-byte. The four-sample corruption corpus completed its final run with no signal crash. This evidence does not claim universal compatibility, performance at 10k/50k real assets, real-game decoder acceptance across every compression family, or real Audio/Video coverage.

Response 2 continued the conservative GLB fallback policy: user fallback textures apply only to material bindings reported as `Unresolved`; `Resolved` bindings are not overwritten and `Null` bindings retain a neutral fallback. Mip and colour-space metadata are reported only when exposed by the readable schema, while audio/video handling remains container-preserving and decoder-gated.
