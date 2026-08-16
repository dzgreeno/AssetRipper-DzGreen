# AssetRipper DzGreen Premium — RC2 User Guide

This guide applies only to Unity data you are authorized to inspect and that is readable without decryption, key acquisition, memory scraping, or protection bypassing.

| Need | Command |
|---|---|
| Deterministic CI batch | `AssetRipper.CLI --ci --input <F1-or-F2-directory> --output <output> --batch --export-verified-only --export-diagnostics json` |
| Grouped GLB | `AssetRipper.CLI --input <directory> --output <output> --glb --filter <character>` |
| GLB with replacement catalog | `AssetRipper.CLI --input <directory> --output <output> --glb --filter <character> --fallback-textures <catalog>` |
| Verified-only batch | `AssetRipper.CLI --input <directory> --output <output> --batch --raw --export-verified-only` |

## CI and exit codes

`--ci` writes compact command output to stdout and a single JSON summary event to stderr. It also removes variable timestamps from diagnostics and batch manifests, which allows byte-level deterministic comparison for the same fixture and command. Exit codes are `0` for success, `1` for unexpected failure, `2` for invalid arguments, `3` for recoverable processing issues, and `4` for a missing input path.

The local F1 and F2 runs used the first command in the table, twice per fixture. Both `assetripper-premium-diagnostics.json` and `assetripper-batch-manifest.json` compared byte-identically between paired runs.

## GLB, fallback textures, and verified-only exports

`--glb` exports a selected readable character or prefab hierarchy. `--fallback-textures` is intentionally narrow: its catalog can substitute only an explicitly **Unresolved** material texture binding. It never replaces a **Resolved** texture. A **Null** binding remains associated with the built-in neutral fallback. The catalog accepts top-level supported image files only; symlinks/reparse points, empty files, and files larger than 64 MiB are ignored.

`--export-verified-only` restricts batch export decisions to collections whose TypeTree coverage is recorded as Embedded or KnownEngineSchema. It is a conservative filter, not a claim that every eligible asset will have a complete mesh, material, animation, or dependency graph.

## Safe execution boundaries

Use complete sibling bundles and resource files for a given authorized fixture. Never provide keys, memory dumps, encrypted containers, or requests to bypass DRM or encryption. If a dependency is not present or readable, the tool reports that limitation rather than fabricating a replacement.

