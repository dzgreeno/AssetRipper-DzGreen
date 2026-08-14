# Workspace Redesign Plan

The live audit shows that the requested home screen is not a replacement for asset pages; it must be a **master workspace** that combines their most useful functions while keeping the detailed asset page available.

## Home layout

The page will use three functional columns inside the existing dark shell. The left rail will contain workspace navigation, asset filters, and the selected character hierarchy. The center column will contain the assembled-character preview and a compact tab bar for Information, Model, Yaml, Json, Dependencies, and Development. The right column will contain the Inspector with type-specific facts, component links, skinning diagnostics, and quick links to the full asset page.

## Selection behavior

Selecting a row in the asset browser will update the central preview when the asset is a Mesh, update the Inspector with the same facts as InformationTab, and expose Model/Yaml/Json/asset-view links. Selecting a Character set will select its root, show a character preview based on the resolved hierarchy/mesh, and populate the left hierarchy and right component groups.

## Character set behavior

Each Character set will show a `Preview assembled` action, an `Export FBX` action that leads to Commands with Primary Content selected, and a hierarchy summary grouped into Root/GameObjects, SkinnedMeshRenderer/Mesh, Animator/Avatar/Controllers/Clips, Materials, and Textures. The UI will distinguish linked clips from global/unlinked clips so `0 clips` is explainable rather than silent.

## Raw data and diagnostics

The center tab strip will use the existing asset endpoints for Model GLB, Yaml, and Json. Dependencies will link to the existing asset page when available. Development will show the runtime C# type. The Inspector will show mesh vertex/submesh counts, texture dimensions/format, and skin bind-pose/weight diagnostics.

## Export boundary

The homepage will not invent a new exporter. It will call the existing Commands route and make the current export mode visible. The FBX exporter remains responsible for hierarchy, materials, textures, UV channels, bind matrices, skin clusters, and animation curves. If a file uses custom GPU skinning with no recoverable weights, the Workspace will report that limitation explicitly.
