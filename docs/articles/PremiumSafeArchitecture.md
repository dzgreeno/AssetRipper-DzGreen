# Premium Safe Architecture

AssetRipper DzGreen Premium is a separate executable for **authorized, plaintext Unity content**. It reuses the normal importer and exporter, then applies relationship-preserving export settings and a post-import inventory. The open AssetRipper DzGreen edition remains separate and retains its GPL-3.0 licensing and upstream attribution.

The Premium boundary is intentionally narrow. The importer accepts standard Unity bundles, serialized data, and resource streams only after an explicit user authorization attestation. It rejects encrypted containers, runtime-memory dumps, custom virtual-file systems, access-key workflows, and any attempt to circumvent protection or access controls.

| Component | Verified behavior |
| --- | --- |
| Input policy | Requires `--premium-authorized`; rejects unauthorized, encrypted, memory, and custom-container inputs. |
| Recovery profile | Preserves prefab outlining, direct bundle topology, explicit mesh grouping, source-preferred textures, supported audio defaults, and readable-only export. |
| Import diagnostics | Counts loaded collections, resources, importer-quarantined files, input paths, and high-priority Mesh, SkinnedMeshRenderer, SpriteAtlas, Texture2D, AnimationClip, and AudioClip families. |
| JSON endpoint | Available after an authorized import at `/Assets/PremiumDiagnostics`. |

The profile does not create missing dependencies, textures, meshes, media, or scripts. Missing input content remains a diagnostic condition because replacement data would be misleading and could break Unity references.
