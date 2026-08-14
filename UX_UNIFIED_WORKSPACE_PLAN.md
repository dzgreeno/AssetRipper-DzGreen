# Unified Workspace UX Plan

## Current friction

The loaded home page is visually split into several layers: a large Character sets area, a separate Workbench, then filters and an asset table, followed by a second right-side Components inspector. This duplicates context and forces the user to understand two different work areas before selecting an asset. The main action is also visually detached from the selected asset because export and advanced search are top-level links rather than part of the current selection.

## Unified model

The page will be one continuous `Asset Workspace` shell with four coordinated regions:

1. A compact workspace header containing search, filter chips, view mode, and the active character-set selector.
2. A single work surface where the left rail contains the selected character hierarchy and character-set switcher, the center contains the preview and compact tabs, and the right rail contains the selected asset inspector and actions.
3. One asset browser table below the work surface, using the same selection state as the preview and inspector.
4. A sticky action bar with Open asset, Yaml, Json, Download GLB, Export FBX, and reset-selection actions.

## Interaction rules

The character-set selector changes the left hierarchy, the center preview URL, the preview title, the download filename, and the component facts together. A row click changes the selected asset and inspector; if it is a Mesh, it changes the center preview, and if it is a non-previewable asset, the existing character preview remains while the inspector switches to that asset. Search and category filters preserve the active selection when possible and automatically select the first visible row only when the current selection becomes hidden.

The user should not have to navigate to Search, Commands, or an individual asset page for common tasks. Those pages remain available as deep-detail routes, but the main Workspace exposes their high-value actions in context.

## Visual hierarchy

The former standalone Character sets panel will be removed from the vertical flow. Its data will become the left rail's character-set switcher and the selected character's hierarchy/facts. This creates one consistent mental model: choose a set, inspect its tree, select an asset, preview it, and export it.
