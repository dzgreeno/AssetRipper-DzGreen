# Asset View Live Audit

## URL examined

`/Assets/View?Path=...` for asset `m_20008`, class `Mesh`, Class ID 43, Path ID `-1453085627175344268`, collection `cab-4927f5bfcb7381d70207d706bf2a8b29`.

## Visible functions on the Information tab

The page has the left Workspace rail with Game structure and Export modes, Asset Filters for All/Meshes/Animations/Textures/Audio/Video/Shaders, a central asset view, and a right Inspector showing Class, Class ID, Path ID, and Collection. The central tabs include Information, Audio, Image, Model, Text, Font, Video, Yaml, Json, Hex, Dependencies, and Development.

The Information tab exposes Collection, Path ID, Class ID Type Number/Name, Vertex Count 6342, and Submesh Count 2.

## Visible functions on the Model tab

The Model tab exposes Download GLB, Lighting: on, Reset camera, and Animation: on, followed by a central Babylon canvas. This is the functionality the homepage currently lacks: a large model viewport with direct preview controls and export/preview actions next to the asset metadata.

## Required homepage transfer

The home Workspace should include a selected-asset viewport panel with the same Download/preview controls, a tab-like information strip, a Dependencies panel, and a Development/diagnostics panel. The existing homepage table/Inspector can remain as the browser, but it should feed the central viewport when a Mesh or assembled Character set is selected. Character sets should have an explicit open/preview action that selects the root and its resolved components rather than only displaying small text links.

## Yaml and Json findings

The Yaml tab exposes the full Unity serialized representation, including `m_SubMeshes`, vertex ranges, AABB values, `m_Shapes`, and a large `m_BindPose` matrix array. The Json tab exposes the same data in structured form, including `m_BindPose`, mesh fields, and collision fields. These tabs are not merely diagnostics: they are the source-level evidence needed to explain whether a mesh has bind poses, submeshes, and recoverable skin data.

The homepage Inspector should therefore include a compact “Raw data” section with links/buttons for Information, Model, Yaml, Json, and Hex, and a structured “Skinning” summary showing bind-pose count, vertex/weight availability, and submesh count. The full raw views should remain available through the asset page but must be one click away from the main Workspace.

## Commands and Settings findings

`/Commands` exposes Reset, a Create Subfolder checkbox, Export Unity Project, Export Primary Content, and Select Folder. The live status confirms that the user's eight input paths were discovered, two UnityFS headers were auto-fixed, and all files were processed. The homepage should expose the two export modes and folder selection without making the user leave the Workspace.

`/Settings/Edit` currently says settings can only be changed before loading files. After the user's files are loaded, the page shows no configuration controls. This makes it especially important to show the active model export format and a clear “settings must be changed before loading” note in the Workspace export panel. The main page should not imply that changing FBX/GLB after processing is possible.

## Search and Collection findings

`/Search/View` is only a single query field and Search button, with no visible advanced filters or grouped results until a query is submitted. The homepage should absorb this capability through its existing search field and filters rather than sending the user to a separate minimal page.

`/Collections/View` is the strongest source for the missing character workspace information. It lists Path ID, Class, and Name for all assets in a collection, and provides Filter name/class/Path ID, Class filter, Previous/Next paging, and Assets Per Page. The live collection for `hero20008.unity3d` exposes the actual chain: Avatar `hero20008Avatar`, mesh `m_20008`, materials `hero20008` and `hero20008Head`, textures `hero20008_DiffuseGlossiness` and `hero20008_Normal`, SkinnedMeshRenderer, Animator, GameObjects such as `hero20008` and `Bip001`, and the full Transform hierarchy.

This confirms that the homepage Character set must be a selectable hierarchy/workspace, not only counts and small link text. It should show a tree grouped as Root → GameObjects/Transforms → SkinnedMeshRenderer → Mesh/Materials/Textures and Root → Animator/Avatar/Clips, with each node opening the same asset view or raw/preview tab.
