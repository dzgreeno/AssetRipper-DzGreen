# RC2 Response 3 — Hardening Evidence

**Scope.** This evidence covers local, authorized fixtures only. The importer processes readable plaintext data and does not acquire keys, bypass DRM, or decode encrypted containers.

| Control | Evidence | Result |
|---|---|---|
| Missing input | `artifacts/rc2-response3/ci-missing-input` | Pass: exit code `4` and one JSON CI summary |
| Empty, 64-byte, and malformed bundle headers | `artifacts/rc2-response3/fuzz/*after*` | Pass: preflight rejects before importer assertion |
| Modified internal bundle bytes | `fuzz/inconsistent-pointer-like.after.stderr` | **Open**: process aborts in upstream low-level `BundleHeader.Read` assertion; blocks Pre-Final |
| F1 deterministic diagnostics and manifest | `determinism/F1-result.json` | Pass: byte-identical in CI mode |
| F2 deterministic diagnostics and manifest | `determinism/F2-result.json` | Pass: byte-identical in CI mode |

The CLI now emits compact JSON to stdout in `--ci` mode and one JSON summary event to stderr. Its public exit-code contract is: `0` success, `1` unexpected failure, `2` invalid arguments, `3` recoverable processing issues, and `4` missing input. Input and fallback texture paths are normalized. Output roots and output-inside-input locations are rejected. Fallback enumeration stays top-level, rejects reparse points, rejects zero-byte files, and caps candidate fallback texture files at 64 MiB.

> The remaining internal-byte mutation abort is deliberately recorded as a release blocker. It is not classified as quarantined, and RC2-Pre-Final packaging must not be claimed ready until it is isolated at the file-reader boundary.

## CI integration

```sh
dotnet AssetRipper.CLI.dll --ci --input game_Data --output export --batch --export-verified-only --export-diagnostics json
status=$?
```

In CI mode only, variable `generatedUtc` fields are omitted from diagnostics and batch manifests, enabling byte-level comparisons for the same input and command.
