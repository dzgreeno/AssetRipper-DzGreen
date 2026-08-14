# AssetRipper DzGreen — Upload Checklist

- [x] Verify the public Ko-fi page and confirm that the user wants project upload first.
- [x] Confirm the intended GitHub owner: `dzgreeno`.
- [x] Confirm the target repository name under `dzgreeno`: `AssetRipper-DzGreen`.
- [x] Review the source tree, GitHub Pages artifact, Windows package, and checksum files before upload.
- [x] Create or update the GitHub repository under `dzgreeno` without deleting unrelated existing content.
- [x] Upload the source tree, GitHub Pages artifact/source, release package, and checksum manifests.
- [x] Verify repository tree, release assets, GitHub Actions workflows, and Pages configuration after upload.
- [x] Verify the Ko-fi support destination and related support copy after upload.
- [x] Rewrite the reachable public history and normalize current ownership metadata to `dzgreeno` where appropriate.
- [x] Verify the current `main` tree, public branches, tags, documentation, Pages bundles, and release assets.
- [x] Inventory all Dependabot pull requests, close them without merging, and delete their remote branches.
- [x] Re-scan current public refs and document any GitHub-managed historical pull-request refs that remain outside public heads and tags.
- [x] Verify whether the branch-protection notice is GitHub interface metadata or repository content, and document its current state: it is a GitHub UI advisory, not repository content; the current public page HTML contains no copy of the notice.
- [x] Add a GitHub Pages badge/button beside the Build and Release, License, Support, and Upstream badges.
- [x] Audit the public README and repository text after the edit for prohibited legacy identity terms and non-`dzgreeno` branding.
- [x] Push the README and checklist update to `main`, then verify the public repository and Pages links.
- [x] Shorten the support and downloads badge labels so each badge stays compact.
- [x] Verify all README badges remain on one visual line at the target viewport and keep the canonical links intact.
- [x] Push the badge-label correction and re-run the public identity/link audit.

## Character Archive Validation (Local Only)

- [x] Inspect and safely extract the user-provided `character.rar` into an isolated test directory.
- [x] Inventory Unity asset files and identify the character root, meshes, textures, materials, and animation clips.
- [x] Run best-effort and strict CLI imports; record diagnostics without exporting protected or encrypted content.
- [ ] Complete one manual browser interaction pass for Asset Workspace visibility, collapse control, and Unicode filtering after the connected-browser bridge is available.
- [x] Test character assembly and strict FBX export with the supplied data, including independent Assimp import of all five generated files.
- [x] Test GLB export with the supplied data and independently inspect all five files through Assimp.
- [x] Validate artifact structure and document remaining source-data limitations without decrypting or bypassing any protection.

> 2026-08-14 local validation findings: archive integrity passed; 10 Unity bundles loaded under Unity 2018.4.18f1 with 2,458 assets and zero processing issues in both best-effort and strict modes. Strict FBX batch export produced five Assimp-readable files with geometry, skin clusters, bones, textures, and animation stacks. GLB batch export produced five Assimp-readable files with mesh and animations; hero20050 and hero20051 retain their meshes through a rigid fallback because the input exposes no resolvable bone PPtrs for their four weighted meshes. The GUI landing route is reachable. Direct browser interaction on the connected local browser failed after navigation, so the Workspace UI requires one final manual browser pass.

## Portable GUI User Test Package

- [x] Publish the Windows GUI test build into a self-contained portable directory.
- [x] Copy the validated character bundles into a clearly named `TestData` folder beside the application.
- [x] Create Arabic test instructions covering launch, folder loading, Workspace visibility/collapse, and Unicode filtering.
- [x] Archive the portable package and verify its file list and checksum before sharing it with the user.
- [ ] Receive the user's Workspace test result and address any reported issue before requesting permission to push.

## Workspace Direct FBX Export Follow-up

- [x] Trace the `Export FBX` action in the assembled-character workspace and confirm why it routes to the general project export page.
- [x] Add a direct, explicit FBX character-export action that uses the selected assembled root and includes associated animation clips.
- [x] Surface a clear completion or error notification with the saved output location in the Workspace.
- [x] Build and test the GUI action against `hero20053`: the direct endpoint produced an Assimp-readable FBX with 6 meshes, 113 bones, 94 animations, and texture sidecars.
- [x] Regenerate the Windows portable test package with the direct Workspace FBX export fix; ZIP integrity and SHA-256 were verified.
- [ ] Receive the user's direct-FBX package result before requesting approval to push.

## Workspace Download and Animation Clip Follow-up

- [x] Replace the direct-FBX text response with a downloadable ZIP containing the FBX and its texture sidecars, while preserving the local `Ripped/AssetWorkspace` export.
- [x] Add a clear post-export browser download action and an explicit status message that distinguishes local save from download.
- [x] Add an Animation Clip selector for the assembled character, with clip name/count and disabled state when none can be resolved.
- [x] Connect the selected clip to the GLB preview animation track instead of relying only on automatic playback.
- [ ] Build, test, and package the updated Windows GUI against the supplied character bundles.

## Blender-Compatible Character Export Follow-up

- [x] Evaluate a self-contained binary-FBX writer or Blender-ready alternative that preserves mesh, skin, animations, and texture sidecars without requiring Autodesk software.
- [x] Replace the Workspace download payload so Blender can import it directly; retain the existing local export and ZIP bundle behavior.
- [x] Validate the new artifact with an independent parser and a Blender-compatible import path.
- [ ] Regenerate the Windows test package and request a Blender import confirmation from the user.

> 2026-08-14 local Blender validation findings: Workspace export now writes a ZIP containing a validated GLB, an embedded-texture binary FBX generated by the bundled static Assimp 5.3.1 converter, the legacy ASCII FBX and its texture sidecars, plus `README-Blender.txt`. The endpoint export for `hero20053` was independently verified by Assimp as 6 meshes, 113 bones, 94 animations, 2 materials, and 1 embedded texture in both GLB and binary FBX. Blender 4.0.2 successfully imported both files; the binary FBX reported version 7500 and produced an armature with 113 bones, 10 mesh objects, 4 images, and 534 Blender actions. The GLB import produced an armature with 113 bones, 11 mesh objects, 3 images, and 534 Blender actions.

## Download Reliability and Workspace Design Follow-up

- [x] Reproduce and diagnose why the browser download did not appear even though the local Blender bundle was created.
- [x] Add an explicit local-folder action and direct download fallback so the exported bundle remains reachable without relying on automatic browser downloads.
- [x] Extract the approved dzgreeno GitHub Pages visual system and translate its palette, hierarchy, and visual language to Asset Workspace.
- [x] Replace the current Workspace presentation with a responsive modern shell and clearer character/export workflow.
- [x] Add direct renderer controls for camera framing, projection, lighting, background, animation speed, playback, and model rotation.
- [x] Validate download paths, renderer controls, and workspace interactions on the supplied character data.
- [ ] Build a new local Windows test package and obtain user confirmation before any push.

> 2026-08-14 local download and workspace validation findings: the user log confirmed that the original ZIP was created under `Ripped\AssetWorkspace`, but the browser-triggered Blob download did not surface reliably. The action now uses a direct user-initiated GET download with a visible `Direct download` link and an `Open export folder` fallback that opens the exact character directory in the operating-system file manager. The direct `hero20053` GET was verified to return `application/zip`, a browser `Content-Disposition` attachment filename, the `binary-fbx+glb` format header, and a ZIP with no integrity errors. The refreshed Asset Atlas workspace was rendered locally with character selection, hierarchy, preview, export actions, and direct controls for framing, perspective/orthographic projection, auto rotation, PNG capture, camera distance, lighting, backdrop, animation playback, and speed. All 510 .NET tests passed.

## Android Archive Recovery and Copyable Diagnostics Follow-up

- [x] Inspect the uploaded Android archive safely and inventory supported, missing, encrypted, or malformed inputs without bypassing any protection.
- [x] Reproduce the reported Unity 2020.1.0a0 Material, Mesh, and SkinnedMeshRenderer failures with the supplied data.
- [x] Add a complete, user-initiated terminal-log copy action that preserves every visible diagnostic line and records the exact source file used.
- [x] Improve safe best-effort handling so corrupt or unsupported objects are quarantined with actionable diagnostics while readable assets continue processing.
- [x] Verify the repaired behavior on the supplied Android archive and run the full test suite.
- [ ] Create a local Windows test package with the Android recovery, full diagnostics, and scalable Workspace changes; obtain user confirmation before any push.

> 2026-08-14 Android recovery findings: the supplied 453 MB RAR was successfully integrity-tested and extracted into an isolated test directory. It contained 1,859 Android files and no encrypted entries; no contents were executed. Its UnityFS bundles advertise stripped `5.x.x` / `0.0.0` bundle metadata while the serialized object metadata reports `2020.1.0a0`. Changing the default Unity version does not alter those embedded metadata values. The initial reader failed for generated schemas; the safe embedded Type Tree fallback restored 25,338 readable objects across the full archive with 0 retained generated-reader errors and 0 Type Tree fallback failures. Recovered objects remain available for raw inspection and dependency analysis. The full diagnostic endpoint delivered 28,256 untruncated lines (7,091,438 bytes) with a browser download attachment name.

> Workspace performance findings: the full archive contains 275,920 indexed assets. The initial Workspace response is now 372,422 bytes in 3.43 seconds with 200 initial rows; all assets remain searchable through `/Assets/WorkspaceRows` pages of up to 500 rows. A paged Mesh query reports 49,352 matching assets and returns 200 rows. Automatic character assembly is deliberately deferred above 8,000 assets to avoid a slow all-component traversal; users can still narrow to assets through filters and the advanced search. The full .NET suite passed: 510 succeeded, 0 failed.
