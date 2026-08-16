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
- [x] Create a local Windows test package with the Android recovery, full diagnostics, and scalable Workspace changes; obtain user confirmation before any push.

> 2026-08-14 Android recovery findings: the supplied 453 MB RAR was successfully integrity-tested and extracted into an isolated test directory. It contained 1,859 Android files and no encrypted entries; no contents were executed. Its UnityFS bundles advertise stripped `5.x.x` / `0.0.0` bundle metadata while the serialized object metadata reports `2020.1.0a0`. Changing the default Unity version does not alter those embedded metadata values. The initial reader failed for generated schemas; the safe embedded Type Tree fallback restored 25,338 readable objects across the full archive with 0 retained generated-reader errors and 0 Type Tree fallback failures. Recovered objects remain available for raw inspection and dependency analysis. The full diagnostic endpoint delivered 28,256 untruncated lines (7,091,438 bytes) with a browser download attachment name.

> Workspace performance findings: the full archive contains 275,920 indexed assets. The initial Workspace response is now 372,422 bytes in 3.43 seconds with 200 initial rows; all assets remain searchable through `/Assets/WorkspaceRows` pages of up to 500 rows. A paged Mesh query reports 49,352 matching assets and returns 200 rows. Automatic character assembly is deliberately deferred above 8,000 assets to avoid a slow all-component traversal; users can still narrow to assets through filters and the advanced search. The full .NET suite passed: 510 succeeded, 0 failed.

> Windows package: `AssetRipper-DzGreen-v1.3.15-dzgreen.3-dev-android-recovery-Windows-x64.zip` was ZIP-tested locally, includes the static Assimp converter and its license, and has SHA-256 `503db76b92f0f746cfb442dc163ccbd726f9210a59aed56b6343fb3f0c22030d`.

## Android Export Failure Follow-up

- [x] Extract and classify every export failure in the user-provided full diagnostic log.
- [x] Reproduce the affected export mode on the supplied Android data without modifying source files or bypassing protection.
- [x] Repair safe export handling for recovered Type Tree objects and produce actionable diagnostics for assets that cannot become FBX or GLB.
- [x] Verify exported artifacts and the complete test suite, then create a local Windows test package only.

## Unity Re-import Compatibility Follow-up

- [x] Classify Unity Editor import errors, missing media families, and invalid generated files from the user-supplied Windows export log.
- [x] Verify the intended project-opening workflow and export mode against the current AssetRipper behavior and configuration.
- [x] Repair Unity-project-compatible export paths for Texture2D and Mesh without creating misleading placeholder assets; retain diagnostic records only for resources that are genuinely unavailable.
- [x] Re-run the supplied Android archive export and perform structural re-import validation: 3,921 non-empty PNG files with matching meta files, 4,291 Mesh YAML assets with matching meta files, a Final ProjectVersion, and no malformed Mesh YAML headers. No Unity Editor executable is available in the sandbox for an Editor launch.
- [ ] Create and ZIP-test a new local Windows test package after the full .NET suite passes.

> 2026-08-15 Unity re-import findings: the source data reports Unity 2020.1.0a0, which is not a normal Unity Hub editor target. The export now writes `2020.1.0f1` to ProjectVersion.txt for an importable upgrade path, while leaving source parsing metadata untouched. The Android export completed with zero collection failures. It produced 3,921 PNG images from valid embedded or `.resS` Texture2D data and 4,291 Unity Mesh YAML assets. The full .NET suite passed: 510 succeeded, 0 failed. The archive contains no AudioClip or VideoClip source classes; it does contain FMOD bank `.bytes` resources, which remain raw data and require the game's legitimate FMOD Unity integration to play in Unity.

## Character Prefab Structure and Dependency Recovery Follow-up

- [x] Identify why recovered GameObject, Transform, MeshRenderer, SkinnedMeshRenderer, Material, Animator, and MonoBehaviour objects are quarantined instead of emitted as linked Unity assets.
- [x] Map dependencies for selected character roots in the supplied Android archive, including component ownership, parent-child transforms, mesh, material, animator-controller, avatar, and animation-clip PPtrs.
- [x] Design a safe, class-specific recovery path that preserves verified Prefab and folder relationships without fabricating unresolved dependencies or bypassing protection.
- [x] Implement Unity-compatible export for recoverable character hierarchy, renderer, material, animation, and controller records while retaining clear diagnostics for unsupported scripts or missing source dependencies.
- [x] Run the full .NET suite and create a local Windows package only after validation.

### Windows Log Configuration Findings

- [x] Correct the character-project export defaults exposed in the Windows log: enable Prefab outlining and disable static-mesh separation for hierarchy-preserving Unity Project exports.
- [x] Confirm that bundled character FBX, materials, textures, and poses are grouped into deterministic character/bundle folders instead of generic type-only folders.
- [ ] Correlate the unresolved `archive:/CAB-*` dependency warnings with the source bundle inventory and retain only resolvable references; never fabricate missing bundles or bypass protection.

> 2026-08-15 character structure validation: with the new defaults, the five-character test archive exported without collection failures. `hero20053.prefab` was placed under `Assets/AssetBundles/character/hero20053.unity3d/pack/character/hero20053/`; it contains 141 GameObjects, 141 Transforms, 2 SkinnedMeshRenderers, one Animator, Mesh and Material references, and 19 distinct external GUIDs. All 19 GUIDs resolve to files in the generated project. Mesh, Material, Texture, controller, and animation files are grouped by source bundle, including a separate `hero20053_anim.unity3d` AnimationClip folder.

> 2026-08-15 Android structural export validation: the full Android archive completed `Export All` and post-export with zero collection failures, zero StackOverflow events, and zero `NotImplementedException` events. The temporary generated project contained 3,921 PNG images, 6,977 serialized Mesh assets, 2,352 Prefabs, and 447 source-bundle directory roots. The temporary project was removed after validation to free disk space; the test log remains available locally.

> 2026-08-15 package validation: the full .NET suite passed with 510 succeeded and 0 failed. The local Windows package `AssetRipper-DzGreen-v1.3.15-dzgreen.6-dev-prefab-structure-Windows-x64.zip` was ZIP-tested, contains the self-contained GUI executable, Assimp converter, and license, and has SHA-256 `00478ce6275a2af01fb177cd602df260b8db707bf93e56f2a1335e886071d7d7`.

## User 58-Bundle Prefab Failure Reassessment

- [x] Classify the user-reported Prefab failure from the v1.3.15-dzgreen.6 log, including the 58 selected bundles, unresolved CAB dependencies, generated-reader fallback classes, and the actual Unity Console symptoms.
- [x] Compare the available local character test archive against the 58-bundle inventory and identify any absent model, materials, textures, poses, assemblies, or companion CAB files required by the user’s failing project.
- [ ] Map only demonstrably recoverable original paths from bundle container entries and manifests; record unavailable source paths, folder GUIDs, and `.meta` data instead of fabricating a 1:1 source layout.
- [ ] Verify ProjectSettings assets and serialized cross-asset GUID links that are present in the supplied input; retain clear diagnostics for scripts and dependencies that are absent from the selected data.
- [x] Implement and test a proven input-completeness correction locally before creating another package or requesting a GitHub push.

> 2026-08-15 input completeness finding: the user log shows exactly 58 selected game files and 62 unresolved `archive:/CAB-*` warnings across five CAB identities. The corresponding complete local Android directory contains 515 files and its full-folder export resolves zero CAB dependency warnings. The incomplete selection is therefore a verified blocking cause of missing cross-bundle Prefab references; missing bundle content will not be fabricated.

> 2026-08-15 companion expansion validation: selecting the Armadillo FBX bundle alone expanded the input from 1 to 513 compatible Unity files in its containing folder. The resulting load had zero unresolved CAB dependencies, and Unity Project export completed with zero collection failures, 2,352 Prefabs, 3,921 PNG images, and 6,977 Mesh assets. This protects File → Open File from silently producing an incomplete cross-bundle export when a complete sibling directory is available.

## Mesh, Sprite, and GLB Regression Investigation

- [x] Classify the user-provided GLB/primary-content export stoppage from the complete Windows log and locate the first exporter exception or cancellation path.
- [x] Reproduce and diagnose malformed Mesh geometry shown in Unity thumbnails, including stream/channel offsets, vertex layouts, index buffers, submeshes, skinning data, and version fallback paths.
- [x] Diagnose white Sprite results by tracing Sprite, Texture2D, alpha, atlas, and `.resS` data recovery rather than substituting flat white images.
- [x] Implement only corrections supported by supplied serialized data, then validate GLB structure and Unity Project asset readability before creating a new local package.

> 2026-08-15 primary-content diagnosis: the supplied Windows log was not stopped by GLB writing. It reached primary-content item 108 (`Shader`) and threw `NotImplementedException` while JSON walked a recovered Type Tree field with `ArrayDepth == 2`. The new walker serializes supported two-dimensional primitive and complex arrays, and the export loop isolates a remaining individual export failure instead of terminating the whole run. The same 513-file archive passed item 25,724 after this correction and had zero `NotImplementedException` occurrences; the local run then exhausted the sandbox disk while writing 23,943 JSON files and 1,748 FBX files, rather than encountering the prior logic failure.

> 2026-08-15 Mesh/Sprite validation: recovered Mesh exports now resolve verified external vertex stream content into `m_VertexData.m_Data` and clear the original `m_StreamData` descriptor before Unity YAML is written, avoiding a reference to bundle resources that do not exist inside the exported Unity project. A focused Armadillo export completed with 76 Mesh assets and 74 recovered Mesh records. A focused UI character-reveal export produced seven PNG textures with no texture-conversion warnings. Several source UI assets are genuinely white/gray alpha-styled artwork (for example title backings and a skew square), while other files in the same input contain non-gray color pixels; no uniform white substitute was emitted.

## First Character Package Mesh, Media, and Prefab Revalidation

- [x] Inventory the original first character test package and compare it against the one-file Armadillo input recorded in the current Windows log.
- [x] Map the unresolved CAB dependencies and `unity default resources` warning to available files; retain an explicit missing-dependency diagnostic where the original companion is absent.
- [x] Reproduce malformed recovered Mesh and SkinnedMeshRenderer export from the first character package, then verify vertex, index, bind-pose, and bone-reference fields before emitting Unity YAML.
- [x] Count AudioClip, VideoClip, MovieTexture, and raw audio/video resource types in the first package to distinguish absent media from exporter failure.
- [x] Reconstruct only verified GameObject, Transform, MeshRenderer, SkinnedMeshRenderer, Material, Animator, controller, Avatar, and AnimationClip relationships into character Prefabs; do not fabricate unresolved links.

> 2026-08-15 first character package revalidation: `character.rar` contains ten Unity 2018 character and animation bundles for hero20050 through hero20054, not a complete game installation. It has no AudioClip, VideoClip, MovieTexture, audio-bank, or video resource asset in the indexed output; Unity Project export therefore correctly contains zero audio and video files rather than silent fabricated placeholders. The one-file Android Armadillo Windows log is a different input: it contains only one bundle in its selected directory and reports five CAB-style dependency warnings plus `unity default resources`, so a complete character cannot be rebuilt from that isolated file.

> 2026-08-15 first character Prefab revalidation: exporting the complete first character folder completed with zero collection failures and emitted five hero Prefabs, 40 serialized Mesh assets, and 22 PNG files. `hero20053.prefab` contains 344 YAML documents: 141 GameObjects, 141 Transforms, 2 SkinnedMeshRenderers, 8 MeshRenderers, one Animator, 10 Mesh pointers, 10 material lists, and 2 bone lists. All 19 external GUIDs in that Prefab resolve to generated `.meta` files. The package retains one missing CAB companion and `unity default resources` warning, but these did not leave unresolved GUIDs in the exported hero20053 Prefab.

## Android Archive Reference Revalidation

- [ ] Treat `/home/ubuntu/upload/android.rar` as the authoritative test archive; verify its archive integrity and compare it with the existing extracted Android corpus before modifying source.
- [ ] Inventory AudioClip, VideoClip, MovieTexture, streaming-media resources, Mesh, SkinnedMeshRenderer, Animator, Prefab, Material, and Texture2D assets from the complete archive.
- [ ] Reproduce character export from the complete archive and verify that CAB dependencies resolve before attributing Mesh or Prefab loss to the exporter.
- [ ] Trace any remaining malformed Mesh through the exact source bundle, vertex stream, indices, submeshes, bone weights, bind poses, and renderer links.
- [ ] Apply only fixes demonstrated by the complete Android input, then rerun export and tests before producing a new Windows package.

## Final Premium Request and Release

- [x] Verify `/home/ubuntu/upload/android.rar` against the existing 515-file Android corpus and run the complete Unity Project export with zero CAB dependency warnings.
- [x] Include recovered SkinnedMeshRenderer objects in Prefab YAML and resolve Type Tree Mesh, Material, and Component PPtrs to Unity GUIDs.
- [x] Run the full .NET test suite and produce a ZIP-tested Windows package containing the Android Prefab/Mesh recovery.
- [x] Add a clearly labeled Request Premium link in the AssetRipper GUI header pointing to `https://ko-fi.com/dzgreen`.
- [ ] Commit the Premium request UI change locally, rebuild the Windows package, and upload the approved source/package only after the public identity and checksums are audited.

> 2026-08-15 final Android validation: full-folder Android export completed at 30,684/30,684 with zero missing CAB warnings, zero duplicate-asset errors, and 724 Prefabs containing SkinnedMeshRenderer. A representative Prefab contains a resolved Mesh GUID, Materials, Bones, and RootBone. The source-side Premium request CTA is a Ko-fi link only; no payment system or unavailable Premium feature is implemented.

## Premium Edition — Safe Scope Definition

- [x] Define a separate Premium architecture for legitimate, unencrypted Unity inputs and user-authorized plaintext exports.
- [x] Specify higher-fidelity Mesh, Prefab, Texture, SpriteAtlas, Animation, AudioClip, and diagnostic capabilities that do not bypass encryption, DRM, anti-tamper controls, or access restrictions.
- [x] Exclude runtime key extraction, memory-dump ingestion for evasion, metadata decryption, custom-container decryption, anti-debugging bypass, and proprietary VFS devirtualization from the product scope.
- [x] Prepare acceptance tests, a feature matrix, and a release-separation plan that preserve the open GPL-3.0 edition and required AssetRipper attribution.

### Approved Premium Implementation

- [x] Create a separate Premium project identity, feature manifest, command profile, and explicit plaintext-input policy without changing open-edition defaults.
- [x] Implement an import diagnostic report for supported UnityFS/serialized inputs that classifies loaded collections, resource files, quarantined failures, input paths, and high-priority recoverable asset families.
- [x] Implement a higher-fidelity recovery profile for verified Mesh, SkinnedMeshRenderer, SpriteAtlas/Texture2D, AnimationClip, and AudioClip export paths using existing safe exporter modes and deterministic project-reference settings.
- [x] Provide only user-supplied plaintext conversion adapters and documented open-format decompressors; refuse encrypted, protected, or authorization-unknown containers with actionable diagnostics.
- [ ] Add Premium acceptance fixtures for a plain Unity asset bundle, a multi-bundle character export, a SpriteAtlas, a supported AudioClip, and non-fatal corrupted-input isolation.
- [x] Build and ZIP-test a separate self-contained Windows x64 Premium preview package locally without uploading it.
- [x] Restore and run all nine .NET test projects after the Premium changes; 524 tests passed with zero failures.

> 2026-08-15 Premium safety boundary: the supplied proposal contains decryption, runtime-memory, anti-tamper, and de-obfuscation instructions. The Premium plan will not implement or document security-control bypasses. It may improve processing of plaintext Unity content, non-fatal diagnostics, deterministic reference reconstruction, open-format import, and user-supplied authorized conversion workflows.

> 2026-08-15 Premium foundation validation: the separate `AssetRipper.GUI.Premium` executable now requires `--premium-authorized` as a user attestation before it will process an otherwise supported plaintext Unity input. A runtime endpoint check rejected the Android reference folder without the attestation (`authorization-required`, HTTP 500) and accepted it with the attestation (HTTP 302 followed by successful processing). The policy test suite passed 6/6 and the Premium GUI project built with zero warnings or errors.

> 2026-08-15 Premium diagnostics validation: the separate Premium executable accepted the authorized Android reference folder and returned HTTP 200 at `/Assets/PremiumDiagnostics`. The report identified Unity `2020.1.0a0`, 593 asset collections, 374 resource files, zero importer-quarantined failures, and 274,900 classified assets. Its priority inventory includes Mesh, SkinnedMeshRenderer, Texture2D, and AnimationClip records; it reports only data already loaded by the normal importer.

> 2026-08-15 Premium recovery profile validation: when a plaintext input passes the explicit authorization policy, Premium applies the existing safe high-fidelity modes: Prefab outlining on, static-mesh separation and asset deduplication off, direct bundle exports, binary-ready FBX selection, PNG/YAML sprite and texture preservation, source-preferred texture extension, supported audio defaults, and unreadable-asset fabrication off. The dedicated policy/profile/diagnostic test suite passed 8/8 and the Premium GUI built with zero warnings and zero errors.

> 2026-08-15 environment validation: after a sandbox reset, all nine test projects were restored and ran successfully with 517 passing tests and zero failures. A temporary Roslyn compatibility adjustment was used only to rebuild the reset test environment and was reverted before source review; it is not part of the Premium change set.

- [x] Inventory the Unity test files, packages, and validation logs used for AssetRipper DzGreen Premium.
- [x] Document confirmed coverage and gaps across legacy and current Unity generations without claiming unperformed game-specific tests.
- [x] Publish a user-facing intake matrix that specifies the minimum authorized files required for each Unity-version and asset-family test case.
- [x] Publish a separate execution-and-security report for the Premium implementation, including implemented functionality, verification status, and explicitly rejected protection-bypass paths.
- [x] Design a TypeTree coverage and confidence architecture for authorized, unencrypted serialized data with explicit diagnostics instead of speculative field recovery.
- [x] Design a readable-Shader inventory and property-binding export path that preserves available metadata without decompiling protected proprietary bytecode.
- [x] Define safe Mesh, BlendShape, skeleton, and animation validation milestones backed by authorized Unity fixture projects.
- [x] Add deterministic numerical unpackers for Half precision, packed signed-normalized vectors, and smallest-three quaternion validation used by Premium geometry diagnostics.
- [x] Add a bounded cross-file reference graph analyzer with unresolved-edge and cycle diagnostics based on loaded Unity PPtr relationships.
- [x] Integrate numerical and reference-graph findings into the Premium diagnostic JSON endpoint and add unit tests for valid, edge, and malformed inputs.
- [x] Build and verify an updated local Windows Premium preview package after the advanced diagnostics pass their tests.
- [x] Implement TypeTree coverage classification from serialized-file metadata and expose its aggregate in Premium diagnostics.
- [x] Upgrade PPtr graph cycle reporting from back-edge counts to strongly connected component summaries.
- [x] Implement a read-only Material and Texture property inventory derived from imported Unity assets, without shader-bytecode decompilation.
- [x] Add fixture-oriented tests for TypeTree coverage, Material binding, and diagnostic aggregation.
- [x] Publish a standalone report for the latest TypeTree, PPtr, Material/Texture, testing, and Windows preview package implementation.
- [x] Inspect existing IMesh, AnimationClip, and GLB exporter APIs and define only schema-backed Phase 2 extension points.
- [x] Implement a span-based Premium vertex stream processor for explicitly described readable channels and record unsupported layouts diagnostically.
- [x] Implement a bounded Premium animation keyframe processor and sampler only for readable curves whose schema and timing are available.
- [x] Extend GLB material mapping with resolved property-channel classifications, texture transform and wrap metadata, and neutral fallbacks only for Null or Unresolved textures.
- [x] Add Phase 2 unit tests and run the complete nine-project regression suite with 527 passing tests and zero failures.
- [x] Build, ZIP-test, and checksum the Phase 2 local Windows Premium preview without uploading it.

> 2026-08-16 Phase 2 validation: all nine test projects passed with 527 tests and zero failures; `AssetRipper.GUI.Premium` and the GLB exporter built with zero warnings. The self-contained Windows x64 archive `AssetRipper-DzGreen-Premium-v1.3.15-dzgreen.15-phase2-preview-Windows-x64.zip` passed `unzip -t`, contains 463 entries including the executable, GPL-3.0 license, and Premium documentation, and has SHA-256 `77652d5bf337b2e1bc9353186b54cd0bd740c16314a50f007ee636d14553ee9a`. It remains local only.
