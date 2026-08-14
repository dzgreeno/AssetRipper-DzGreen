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
