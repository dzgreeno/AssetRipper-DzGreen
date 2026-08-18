# Validated Model Export Acceptance

## Scope

This gate applies only to locally supplied, readable Unity data. It does not decrypt data, acquire keys, bypass DRM, or create missing project data.

## GLB / FBX acceptance requirements

| Area | Required source-backed evidence | Rejection condition |
|---|---|---|
| Geometry | Non-empty position data decoded from declared vertex channels and embedded or resolved stream bytes | Missing, zero-length, non-finite, or undeclared POSITION layout |
| Indices and submeshes | Triangle indices within the decoded vertex range; each emitted primitive maps to a declared submesh | Out-of-range index, non-triangle remainder, or unverified range |
| Skinning | Declared blend indices/weights normalize to a resolved joint range | No resolved joint influence after sanitization |
| Bind poses | One finite, non-zero bind matrix for each emitted joint | Missing, non-finite, zero, or count mismatch |
| Hierarchy | Source PPtrs resolve GameObject, Transform, renderer, mesh, and required bones | Required PPtr cannot be resolved |
| Materials | Each declared slot resolves a material or has an explicit source `Null` binding | Unresolved material slot cannot be represented honestly |
| Textures | Resolved binding is preserved; only source `Null` uses the neutral fallback; `Unresolved` may use a user catalog | A resolved binding is overwritten or decoder output fails validation |
| Animation | Export only clips with source-backed channels and target paths | No source-backed target binding or an unknown stream format |

## Output decision

The exporter records a structured QA decision per requested root:

* `Accepted` means every required row for the requested representation passed.
* `Partial` means optional data, such as an unresolved optional texture, was retained as an explicit diagnostic without changing required geometry correctness.
* `Rejected` means the output is not emitted as a validated model. A diagnostic report identifies the first failed requirement and all additional observed failures.

The existing stable `IMesh` path remains unchanged. The experimental TypeTree/raw-cab bridge must satisfy this exact gate independently before it can be considered a valid export path.

## Automatic TypeTree fallback policy

The exporter may consider a recovered TypeTree/raw-cab object only when the requested root has no valid typed `IMesh` export path. The recovered path remains rejected unless it proves declared POSITION data, triangle indices, declared submeshes, source-backed renderer linkage, and every required skin, bind-pose, bone, material, and texture condition for the requested representation.

When the normal typed path is valid, it is authoritative. When the recovered path is considered but rejected, diagnostics must identify `fallback-considered`, `fallback-rejected`, and the concrete source-backed rejection reason. The exporter must never merge partial recovered geometry into an otherwise typed mesh.
