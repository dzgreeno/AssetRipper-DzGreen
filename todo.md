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
