# FBX Reference Findings

## AssetStudio

Source: [Perfare/AssetStudio](https://github.com/Perfare/AssetStudio), archived repository, shallow clone commit `d158e86`.

The README documents that Animator assets can be exported to FBX with bound AnimationClips. It also documents selecting a model from Scene Hierarchy together with AnimationClips, or selecting an Animator plus AnimationClips, before exporting. The reference implementation uses a native Autodesk FBX SDK wrapper rather than a text-only writer.

The reference exporter pipeline first builds a unified imported representation containing a root frame hierarchy, meshes, materials, textures, animation list, and morphs. It derives bone paths and bind matrices from SkinnedMeshRenderer/mesh data, creates FBX joints from all required bone paths, attaches material/texture links, writes vertex weights into clusters, writes bind matrices, then exports animation tracks for each clip.

Important reference details found in `AssetStudioFBXWrapper/FbxExporterContext.cs`:

- Skinning is emitted per vertex with up to four bone indices and weights.
- Each skin cluster is linked to the corresponding bone node and receives the mesh bind matrix.
- Material texture slots are linked with texture offset and scale values.
- Animation export iterates each imported animation and each track path, then writes scaling, rotation, and translation keys.
- The hierarchy search retains ancestors of rendered meshes and all ancestors required by bone paths.

## Unity FBX Exporter documentation

Source: [Unity FBX Exporter 4.0.1 — Exporting FBX files](https://docs.unity3d.com/Packages/com.unity.formats.fbx@4.0/manual/exporting.html).

The official documentation states that a hierarchy is exported as one FBX with transforms, meshes, skinned mesh renderers, materials, textures, animation, and blendshapes. Supported mesh attributes include normals, binormals, tangents, vertex colors, all eight UV channels, quads, and triangles. The FBX Exporter supports Legacy and Generic Animation from Animation and Animator components and exports transform curves; animation curve tangents are included, while skinned-bone prerotation may require per-frame baking for faithful results.

The documentation also states that the exporter uses centimeter units while keeping mesh data at real-world meter scale. This confirms that the current exporter should preserve a consistent Unity-to-FBX scale conversion and should not merely dump separate mesh files.

## Gap analysis against current AssetRipper custom exporter

The current exporter already has hierarchy nodes, mesh geometry, normals, tangents, UV0, colors, materials, texture sidecars, skin clusters, bind pose matrices, and TRS animation curves. The primary gaps are:

1. The FBX material layer does not yet emit texture transform metadata from UnityTexEnv offset/scale.
2. The FBX hierarchy model writes translation and scale but does not emit local rotation properties, which can make rest poses and animation imports inaccurate.
3. The geometry writer only writes UV0 even though `MeshData` exposes UV0 through UV7.
4. Animation export only handles the normalized position/scale/quaternion lists and does not preserve curve tangents or robustly bake bone rotations with prerotation.
5. Character export should make the complete resolved hierarchy the canonical scene, not add loose mesh objects as independent scene-root nodes when those meshes are already represented by renderer nodes.
6. The web page head includes the favicon route/resource but does not emit a `<link rel="icon">`, so the browser tab icon is not guaranteed.

These gaps are the targets for the next implementation phase. The work remains limited to lawful asset analysis/export and does not bypass DRM or encryption.

## AssetStudio issue #560

Source: [Problem with exported animations — AssetStudio issue #560](https://github.com/Perfare/AssetStudio/issues/560).

The issue documents an important limitation: an FBX may contain the correct geometry, skeleton, and animation curves while the mesh does not deform because vertex weights are absent. The discussion identifies cases where a game stores bone information in a `GpuSkinning` MonoBehaviour instead of the Mesh data, so a general exporter cannot reconstruct weights from the Mesh alone. It also recommends loading all files in the folder before exporting the target hierarchy, because the Animator, clips, shader/material data, and companion assets may be split across bundles.

Implementation implication: the Workspace should distinguish `Resolved skin` from `Animation present but weights unavailable`, expose missing skin/dependency diagnostics in the Inspector and Character set card, and avoid claiming that every protected/custom GPU-skinning format can be reconstructed without source data. For ordinary SkinnedMeshRenderer assets, the exporter should write actual vertex weights and bind pose data.
