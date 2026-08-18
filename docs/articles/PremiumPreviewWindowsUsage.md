# AssetRipper DzGreen Premium Preview — Windows Usage

This preview is a **separate executable** for high-fidelity work on Unity data that is already plaintext and that you are authorized to process. It preserves the existing AssetRipper DzGreen open-edition workflow and GPL-3.0 attribution; it does not replace the open edition.

To start the Premium preview, run `AssetRipper.GUI.Premium.exe` from the extracted folder. Before loading an input, explicitly confirm that you are authorized to process it by launching the application with the following argument:

```text
AssetRipper.GUI.Premium.exe --premium-authorized
```

When started without this user attestation, the Premium preview rejects imports before processing. This explicit confirmation does not relax the safety boundary: encrypted containers, runtime-memory dumps, custom virtual-file containers, runtime-key workflows, and protection-bypass workflows remain unsupported.

| Input condition | Premium preview behavior |
| --- | --- |
| Authorized plaintext Unity bundle or serialized data | Accepted for normal import and export processing. |
| No authorization attestation | Rejected with the `authorization-required` diagnostic. |
| File marked as encrypted | Rejected with the `encrypted-input-not-supported` diagnostic. |
| Runtime memory dump | Rejected with the `runtime-memory-not-supported` diagnostic. |
| Custom virtual-file container | Rejected with the `custom-vfs-not-supported` diagnostic. |
| Unknown plaintext format | Rejected with the `unsupported-format` diagnostic. |

The preview uses the same safe companion-file discovery, prefab-reference recovery, and Unity-project export pipeline as the current DzGreen build. Keep the full set of legitimate companion files together in their original directory where possible, because incomplete source selections cannot be reconstructed by inventing missing bundles or dependencies.

For the architecture and high-fidelity improvements, see `PremiumSafeArchitecture.md`. DzGreen Premium Early Access is available for USD 10 through [Ko-fi](https://ko-fi.com/dzgreen); it supports the DzGreen development and verification additions, not an official upstream AssetRipper Premium edition. The GPL-3.0 source remains publicly available in the dzgreeno repository.
