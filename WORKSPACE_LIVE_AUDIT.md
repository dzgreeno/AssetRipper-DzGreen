# Live Workspace Audit — 127.0.0.1:42884

## Observed in the live browser

The current page is already rendering the custom Asset Workspace, but it is still primarily a statistics panel plus a long asset table. The visible flow is: header navigation, Asset Workspace title/actions, six stats cards, one Character sets card, filters, table, a narrow Components inspector, and a floating Live Status dock.

The live DOM exposed:

- 325 assets, 9 collections, 11 GameObjects, 16 meshes, 2 animations, 61 textures.
- Character set root `hero20008` with 24 hierarchy items, 1 mesh, 1 Avatar, 0 controllers, 0 clips, 2 materials, 2 textures, 1 skinned mesh, 1 weighted mesh.
- Character links shown: mesh `m_20008`, Avatar `hero20008Avatar`, materials `hero20008` and `hero20008Head`, textures `hero20008_DiffuseGlossiness` and `hero20008_Normal`.
- The first selected row is `Animator`, and the right inspector shows only class/collection/path/components for that selected asset.
- The page table exposes Name, Class, Category, Collection, Components, and Path ID.
- The top actions are `Open / export` and `Advanced search`; Back/Forward exist in the header.

## Main gaps against the requested reference workspace

1. The main page still does not show a central character/model preview. The user expects a workspace with a visible scene/viewport, component hierarchy, and export controls, not only a list.
2. Character sets are a single wide metrics card; components are rendered as small text links and do not provide a selectable hierarchy/tree or a one-click “open assembled character” action.
3. The Inspector is a narrow metadata card and does not show a structured component tree, mesh/material/texture/animation groups, or per-component actions.
4. There is no visible “Build/preview assembled character” control in the Character set card, and no explicit animation selector/play control tied to the assembled set.
5. The live page still uses the generic old navigation labels and does not present the reference-style left Workspace/Asset Filters rail.
6. The live asset list starts below the Character sets card, but the main visual focus remains the table; the requested design needs the assembled character workspace to be primary and the asset table secondary.
7. The live sample shows `0 CLIPS` while the overall workspace has `2 ANIMATIONS`, so animation-to-character association is not visible or explainable to the user. The UI needs explicit “linked clips / unlinked clips” diagnostics and an action to review/attach candidates rather than silently showing zero.
