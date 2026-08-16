AssetRipper DzGreen Premium — Enterprise Readable-Data Profile

1. Choose a six-character alphanumeric local token, for example A7x9Q2.
2. Before starting the GUI from Command Prompt, set it for that process:

   set ASSET_RIPPER_DZGREEN_RECOVERY_TOKEN=A7x9Q2
   AssetRipper.GUI.Premium.exe

   To set it persistently for the current Windows user, use:

   setx ASSET_RIPPER_DZGREEN_RECOVERY_TOKEN A7x9Q2

   Then start a new Command Prompt or restart the GUI.

3. For the CLI, optionally prove the configured local token:

   AssetRipper.CLI.exe --recovery-token A7x9Q2 --input "C:\Authorized\Game_Data" --glb --filter Hero

The GUI landing page reports whether the Diagnostic profile or Enterprise readable-data profile is active. The token is not embedded in the source or package. A missing, malformed, or mismatched token selects DiagnosticOnly mode.

The Enterprise readable-data profile is for user-authorized, readable Unity data supplied locally. It can use the existing readable TypeTree, mesh, hierarchy, material, animation, prefab, GLB, and FBX recovery paths already implemented in the program. It does not decrypt files, acquire or process keys, dump process memory, or bypass DRM/protection. Unavailable schema and unsupported byte layouts continue to be reported rather than guessed.

Token note: this is a local capability selector, not a replacement for enterprise identity management, OS access control, or a remote authorization service.
