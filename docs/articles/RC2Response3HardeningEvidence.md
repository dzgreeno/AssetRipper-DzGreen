# RC2 Response 3 — Hardening Evidence

**Scope.** This evidence covers local, authorized fixtures only. The importer processes readable plaintext data and does not acquire keys, bypass DRM, or decode encrypted containers.

| Control | Evidence | Result |
|---|---|---|
| Missing input | `artifacts/rc2-response3/ci-missing-input` | Pass: exit code `4` and one JSON CI summary |
| Empty, 64-byte, and malformed bundle headers | `artifacts/rc2-response3/fuzz/*after*` | Pass: preflight rejects before importer assertion |
| Modified internal bundle bytes | `fuzz/fuzz-results-final.ndjson` | Pass: no signal crash; the readable fixture still completes with exit `0` |
| F1 deterministic diagnostics and manifest | `determinism/F1-result.json` | Pass: byte-identical in CI mode |
| F2 deterministic diagnostics and manifest | `determinism/F2-result.json` | Pass: byte-identical in CI mode |

The CLI now emits compact JSON to stdout in `--ci` mode and one JSON summary event to stderr. Its public exit-code contract is: `0` success, `1` unexpected failure, `2` invalid arguments, `3` recoverable processing issues, and `4` missing input. Input and fallback texture paths are normalized. Output roots and output-inside-input locations are rejected. Fallback enumeration stays top-level, rejects reparse points, rejects zero-byte files, and caps candidate fallback texture files at 64 MiB.

The original low-level assertion in `BundleHeader.Read` has been replaced by a normal `InvalidDataException` for an invalid signature or negative version. The final corpus run has no signal crash. The short and malformed inputs are rejected by preflight, while the modified readable fixture completes without termination.

## CI integration

```sh
dotnet AssetRipper.CLI.dll --ci --input game_Data --output export --batch --export-verified-only --export-diagnostics json
status=$?
```

In CI mode only, variable `generatedUtc` fields are omitted from diagnostics and batch manifests, enabling byte-level comparisons for the same input and command.
