# AssetRipper Auto-Fix Build

This working tree is based on [AssetRipper/AssetRipper](https://github.com/AssetRipper/AssetRipper) at master commit `545f34591`. The current upstream project uses the free GUI entry point `Source/AssetRipper.GUI.Free/AssetRipper.GUI.Free.csproj` and targets `net10.0` through shared build properties.

## Implemented changes

| Area | Modified location | Behavior |
|---|---|---|
| Prefixed bundle recovery | `Source/AssetRipper.IO.Files/BundleFiles/BundleHeaderNormalizer.cs` and `SchemeReader.cs` | Scans the first 128 bytes for null-terminated `UnityFS`, `UnityWeb`, `UnityRaw`, `RawWeb`, and `UnityArchive`. If a signature is found after a prefix, a suffix `SmartStream` is created before scheme detection. |
| Auto-Fix logging | `BundleHeaderNormalizer.AutoFixMessage` and `Source/AssetRipper.GUI.Web/WebApplicationLauncher.cs` | Emits `[Auto-Fix] Removed {X} junk header bytes from: {FileName} (Valid {Signature} signature recovered)` to the GUI import logger and trace output. |
| RawWeb compatibility | `Source/AssetRipper.IO.Files/BundleFiles/RawWeb/Raw/RawBundleHeader.cs` | Accepts the literal `RawWeb` signature through the existing raw bundle parser. |
| Direct bundle probes | `Source/AssetRipper.IO.Files/BundleFiles/BundleHeader.cs` and `Source/AssetRipper.Import/Platforms/PlatformGameStructure.cs` | Platform detection and bundle-version detection use the same prefix normalization. |
| Unity-version recovery | `SerializedFileMetadata.cs` and `PlatformGameStructure.cs` | Invalid or obfuscated version strings fall back to `2021.3.0f1`, or to a valid major/minor pair when recoverable. The fallback is reported as an Auto-Fix message. |
| Shader dummy export | `Source/AssetRipper.Export.UnityProjects/Shaders/DummyShaderTextExporter.cs` | Emits a clean ShaderLab `CGPROGRAM` vertex-fragment fallback with `_MainTex`, `_Color`, `_BumpMap`, and `_SpecGlossMap`. It no longer inserts a platform-specific serialized shader template that can contain invalid HLSL blocks. |

The rest of AssetRipper’s existing import, browsing, export, and GUI architecture remains intact. The normal raw-script and YAML/decompilation routes are not removed.

## Windows executable

The attached `AssetRipper-Updated-win-x64.zip` is a self-contained Windows x64 publish created from the corrected source tree using .NET 10.0 with `PublishAot=false`. It contains `AssetRipper.GUI.Free.exe` and its managed dependencies, so a separate .NET installation is not required on the target Windows x64 machine.

Extract the complete ZIP into a writable directory and launch `AssetRipper.GUI.Free.exe`. The program hides its console window by default, starts the local AssetRipper web GUI, and opens it through the normal startup flow. Use `--keep-console` when diagnostics are needed. Keep the DLLs and JSON files beside the EXE; do not copy only the EXE by itself.

The executable was produced and verified as a Windows PE32+ x86-64 file in the Linux sandbox. The sandbox cannot launch Windows GUI binaries, so final end-to-end testing with a real Unity bundle should still be performed on Windows.

## Rebuild commands

From a Windows PowerShell or a machine with the .NET 10 SDK installed:

```powershell
dotnet restore Source/AssetRipper.GUI.Free/AssetRipper.GUI.Free.csproj
dotnet build Source/AssetRipper.GUI.Free/AssetRipper.GUI.Free.csproj `
  --configuration Release `
  -p:PublishAot=false

dotnet publish Source/AssetRipper.GUI.Free/AssetRipper.GUI.Free.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  -p:PublishAot=false `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  --output publish\win-x64
```

The upstream project has `PublishAot=true` in the free GUI project. This delivery intentionally disables AOT for the first corrected build because a regular self-contained publish is more portable for verification and does not require the Windows native AOT toolchain. A Windows CI or Visual Studio build can re-enable AOT after testing the added stream and shader code.

## Verification performed

The complete free GUI project built successfully with .NET SDK `10.0.400`, `PublishAot=false`, and zero compiler errors in the final build. A temporary validation harness confirmed prefixed `UnityFS` recovery and Auto-Fix reporting. `node --check` and `git diff --check` also passed. The published file was identified as `PE32+ executable (console) x86-64, for MS Windows`.

The executable has not been run inside the Linux sandbox, and no real Unity 6000.5, URP, or WebGL fixture was available for a full extraction regression test. Therefore, the package is a buildable Windows x64 delivery with preliminary format-23 support, while final user-side smoke testing should load representative files and verify the log message and exported assets.

## References

[1]: https://github.com/AssetRipper/AssetRipper "AssetRipper repository"
[2]: https://github.com/AssetRipper/AssetRipper/releases "AssetRipper releases"
