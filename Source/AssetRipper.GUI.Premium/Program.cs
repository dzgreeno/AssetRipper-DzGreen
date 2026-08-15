using AssetRipper.GUI.Web;

const string AuthorizationArgument = "--premium-authorized";
bool hasAuthorizationAttestation = args.Any(static argument => string.Equals(argument, AuthorizationArgument, StringComparison.OrdinalIgnoreCase));
string[] launchArguments = args.Where(static argument => !string.Equals(argument, AuthorizationArgument, StringComparison.OrdinalIgnoreCase)).ToArray();
Environment.SetEnvironmentVariable("ASSET_RIPPER_DZGREEN_EDITION", "Premium");
Environment.SetEnvironmentVariable("ASSET_RIPPER_DZGREEN_AUTHORIZED_INPUT", hasAuthorizationAttestation ? "1" : "0");
Console.WriteLine("AssetRipper DzGreen Premium accepts authorized plaintext Unity inputs only.");
Console.WriteLine("Pass --premium-authorized only when you are authorized to process the selected plaintext input.");
Console.WriteLine("Encrypted containers, runtime keys, memory dumps, and protection-bypass workflows are not supported.");
WebApplicationLauncher.Launch(launchArguments);
