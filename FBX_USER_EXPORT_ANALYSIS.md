# Analysis of the supplied post-export log

The supplied run processed two related input groups.

## Observed input groups

The first test loaded one file: `arena10003.unity3d`. AssetRipper detected a mixed game structure, removed 9 junk header bytes, reported one missing dependency, finished reading, and completed processing.

The second test loaded eight paths: `hero20007.unity3d`, its manifest and hashed companion files, `hero20007_anim.unity3d`, its manifest and hashed companion files. AssetRipper detected a mixed structure for all eight paths, auto-fixed the two main UnityFS headers by removing 9 junk bytes, then reported the same missing dependency identifier:

`archive:/cab-82e4aa35e2772775fa43714c41018c4f/cab-82e4aa35e2772775fa43714c41018c4f`

## Processing result

The importer completed `Processing loaded assets`, `Creating Scene Definitions`, `Main Asset Pairing`, `Reconstruct AnimatorController Assets`, `Reconstruct AudioMixer Assets`, `Editor Format Conversion`, `Lighting Data Assets`, and `Processing Scriptable Object Groups`.

The normal Unity project export selected Unity `2018.4.18f1`, exported 72 primary assets, and reached `Finished exporting assets`, `Saving game assemblies`, and `Finished post-export`.

The primary content export processed 279 entries. The log shows the relevant character/animation assets before the large group of built-in Unity shaders and editor resources:

- `hero20007_anim.unity3d`
- `hero20007`
- animation clips such as `walk`, `turnLeftHalf`, `skill10`, `stand`, `hitWeak`, `die`, `walkBack`, `walkLeft`, `walkRight`, `hitL`, `hitR`, `hitB`, `hitF`, `touchGround`, `hitExecute`, and related skill/recovery clips
- `hero20007Weapon`
- `hero20007Avatar`
- `m_20007`
- `weapon001`
- a set of attack-box assets

Primary content export completed successfully, but multiple built-in UI/material assets logged `Failed to export`. These failures are separate from the missing `cab` dependency and should not be treated as proof that the character assets failed.

## Implications for the implementation

1. The input files are being processed as separate serialized collections inside one `GameBundle`; the current hierarchy/prefab grouping is not a character package builder.
2. The presence of `hero20007Avatar`, animation clips, `hero20007`, `hero20007Weapon`, textures, and `m_20007` confirms that a useful character workspace should group by semantic/name family and dependency links rather than by one collection only.
3. The missing dependency is reported during dependency-list initialization after file loading. `SpecialFileNames.FixResourcePath` already strips the `archive:/` prefix and keeps the basename, so the diagnostic should show both the normalized name and the searched input roots. A resolver alias should only be added when an actual file or loaded collection can be proven to match; it must not fabricate or bypass encrypted content.
4. The UI should expose a broad asset browser immediately after processing, with category/class/collection/name filters and direct links to the original asset records. A character-group panel should show the proposed root, meshes, avatar, animator/controller, clips, materials, textures, and unresolved references, allowing the user to opt into a grouped export.
5. The log confirms that the current selected format during the run was `ModelExportFormat: Glb`, not FBX. Any FBX diagnosis must therefore distinguish the existing GLB/primary-content run from a later explicit FBX export run.
