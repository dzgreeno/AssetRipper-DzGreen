using AssetRipper.GUI.Web;
using System.Runtime.InteropServices;

const string KeepConsoleArgument = "--keep-console";
const string HideConsoleArgument = "--hide-console";
const int ShowWindowHide = 0;

bool hideConsole = args.Any(static argument => string.Equals(argument, HideConsoleArgument, StringComparison.OrdinalIgnoreCase));
string[] launchArguments = args
	.Where(static argument => !string.Equals(argument, KeepConsoleArgument, StringComparison.OrdinalIgnoreCase))
	.Where(static argument => !string.Equals(argument, HideConsoleArgument, StringComparison.OrdinalIgnoreCase))
	.ToArray();

if (OperatingSystem.IsWindows() && hideConsole)
{
	HideConsoleWindow();
}

WebApplicationLauncher.Launch(launchArguments);

static void HideConsoleWindow()
{
	nint consoleWindow = GetConsoleWindow();
	if (consoleWindow != 0)
	{
		ShowWindow(consoleWindow, ShowWindowHide);
	}
}

[DllImport("kernel32.dll")]
static extern nint GetConsoleWindow();

[DllImport("user32.dll")]
[return: MarshalAs(UnmanagedType.Bool)]
static extern bool ShowWindow(nint hWnd, int nCmdShow);
