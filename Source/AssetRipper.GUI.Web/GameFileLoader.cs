using AssetRipper.Assets.Bundles;
using AssetRipper.Export.Configuration;
using AssetRipper.Export.PrimaryContent;
using AssetRipper.Export.UnityProjects;
using AssetRipper.Import.Logging;
using AssetRipper.Import.Structure.Assembly.Managers;
using AssetRipper.IO.Files;
using AssetRipper.NativeDialogs;
using AssetRipper.Premium;
using AssetRipper.Processing;
using AssetRipper.GUI.Web.Pages;

namespace AssetRipper.GUI.Web;

public static class GameFileLoader
{
	private static GameData? GameData { get; set; }
	private static string[] LoadedPaths { get; set; } = [];
	[MemberNotNullWhen(true, nameof(GameData))]
	public static bool IsLoaded => GameData is not null;
	public static GameBundle GameBundle => GameData!.GameBundle;
	public static GameData CurrentGameData => GameData ?? throw new InvalidOperationException("No processed game data is loaded.");
	public static IReadOnlyList<string> LoadedInputPaths => LoadedPaths;
	public static IAssemblyManager AssemblyManager => GameData!.AssemblyManager;
	public static FullConfiguration Settings { get; } = LoadSettings();
	public static bool Headless { get; set; }
	public static bool StrictProcessing
	{
		get => ProcessingIssueRegistry.Strict;
		set => ProcessingIssueRegistry.Strict = value;
	}
	public static IReadOnlyList<ProcessingIssue> ProcessingIssues => ProcessingIssueRegistry.Snapshot();

	public static void ConfigureAutomation(ModelExportFormat modelExportFormat = ModelExportFormat.Fbx)
	{
		Headless = true;
		Settings.ExportSettings.ModelExportFormat = modelExportFormat;
	}

	public static ExportHandler ExportHandler
	{
		private get;
		set
		{
			ArgumentNullException.ThrowIfNull(value);
			value.ThrowIfSettingsDontMatch(Settings);
			field = value;
		}
	} = new(Settings);

	/// <summary>
	/// Is this the premium edition?
	/// </summary>
	/// <remarks>
	/// This is purely for UI functionality and has no direct effect on the presense of features.
	/// </remarks>
	public static bool Premium => AssetRipperBrand.IsPremiumEdition;

	public static void Reset()
	{
		ProcessingIssueRegistry.Clear();
		AssetBrowserPanel.ResetCache();
			if (GameData is not null)
			{
				GameData = null;
				LoadedPaths = [];
				GC.Collect();
			Logger.Info(LogCategory.General, "Data was reset.");
		}
	}

	public static void LoadAndProcess(IReadOnlyList<string> paths)
	{
		EnsurePremiumInputPolicy(paths);
		if (Premium)
		{
			PremiumRecoveryProfile.Apply(Settings);
			Logger.Info(LogCategory.General, "Applied the Premium high-fidelity recovery profile for authorized plaintext input.");
		}
		Reset();
		string[] expandedPaths = ExpandSiblingUnityFiles(paths);
		LoadedPaths = expandedPaths.Select(GetFullPathOrOriginal).ToArray();
		Settings.LogConfigurationValues();
		GameData = ExportHandler.LoadAndProcess(expandedPaths, LocalFileSystem.Instance);
	}

	private static void EnsurePremiumInputPolicy(IReadOnlyList<string> paths)
	{
		if (!Premium)
		{
			return;
		}

		bool hasAuthorizationAttestation = string.Equals(Environment.GetEnvironmentVariable("ASSET_RIPPER_DZGREEN_AUTHORIZED_INPUT"), "1", StringComparison.Ordinal);
		foreach (string path in paths)
		{
			PremiumInputDescriptor descriptor = CreatePremiumInputDescriptor(path, hasAuthorizationAttestation);
			PremiumInputAssessment assessment = PremiumInputPolicy.Assess(descriptor);
			if (!assessment.IsAccepted)
			{
				Logger.Error(LogCategory.Import, $"Premium input policy rejected '{path}' ({assessment.Code}): {assessment.Message}");
				throw new InvalidOperationException($"Premium input policy rejected '{path}' ({assessment.Code}). {assessment.Message}");
			}
			Logger.Info(LogCategory.Import, $"Premium input policy accepted '{path}' ({assessment.Code}).");
		}
	}

	private static PremiumInputDescriptor CreatePremiumInputDescriptor(string path, bool isUserAuthorized)
	{
		string extension = Path.GetExtension(path).ToLowerInvariant();
		bool isDirectory = Directory.Exists(path);
		bool isEncrypted = extension is ".enc" or ".encrypted" or ".crypt";
		bool isRuntimeMemoryDump = extension is ".dmp" or ".core";
		bool usesCustomVirtualContainer = extension is ".dat" or ".pkg" or ".pck";
		PremiumInputKind kind = isDirectory || extension is ".bundle" or ".unity3d" or ".assets" or ".manifest"
			? PremiumInputKind.UnityBundle
			: extension is ".res" or ".resource" or ".ress"
				? PremiumInputKind.ResourceStream
				: PremiumInputKind.Unknown;
		return new(Path.GetFileName(path), kind, isUserAuthorized, isEncrypted, IsRuntimeMemoryDump: isRuntimeMemoryDump, UsesCustomVirtualContainer: usesCustomVirtualContainer);
	}

	/// <summary>
	/// When the user selects individual Unity files, include recognizable sibling files from the same
	/// directory. Unity serialized files commonly refer to cab/resource companions that are not visible
	/// as a direct dependency of the selected file. This only broadens the input set; it does not decrypt,
	/// patch, or bypass protected content.
	/// </summary>
	private static string[] ExpandSiblingUnityFiles(IReadOnlyList<string> paths)
	{
		HashSet<string> expanded = new(paths.Select(GetFullPathOrOriginal), StringComparer.OrdinalIgnoreCase);
		foreach (string inputPath in paths)
		{
			string fullPath = GetFullPathOrOriginal(inputPath);
			if (Directory.Exists(fullPath))
			{
				continue;
			}

			string? directory = Path.GetDirectoryName(fullPath);
			if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
			{
				continue;
			}

			try
			{
					foreach (string sibling in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
				{
						if (IsUnityCompanionFile(Path.GetFileName(sibling)))
					{
						expanded.Add(Path.GetFullPath(sibling));
					}
				}
			}
			catch (IOException ex)
			{
				Logger.Warning(LogCategory.Import, $"Could not enumerate sibling Unity files in '{directory}': {ex.Message}");
			}
			catch (UnauthorizedAccessException ex)
			{
				Logger.Warning(LogCategory.Import, $"Access denied while enumerating sibling Unity files in '{directory}': {ex.Message}");
			}
		}

		if (expanded.Count > paths.Count)
		{
			Logger.Info(LogCategory.Import, $"Expanded selected Unity files from {paths.Count} to {expanded.Count} paths by including compatible Unity data, bundles, manifests, and cab/resource companions from the containing folders.");
		}
		return expanded.ToArray();
	}

	private static bool IsUnityCompanionFile(string fileName)
	{
		string lower = fileName.ToLowerInvariant();
		if (lower.StartsWith("cab-", StringComparison.Ordinal) || lower.Contains(".split", StringComparison.Ordinal))
		{
			return true;
		}

		bool recognizedExtension = lower.EndsWith(".unity3d", StringComparison.Ordinal)
			|| lower.Contains(".unity3d_", StringComparison.Ordinal)
			|| lower.EndsWith(".bundle", StringComparison.Ordinal)
			|| lower.Contains(".bundle_", StringComparison.Ordinal)
			|| lower.EndsWith(".assets", StringComparison.Ordinal)
			|| lower.Contains(".assets_", StringComparison.Ordinal)
			|| lower.EndsWith(".res", StringComparison.Ordinal)
			|| lower.EndsWith(".resource", StringComparison.Ordinal)
			|| lower.EndsWith(".ress", StringComparison.Ordinal)
			|| lower.EndsWith(".manifest", StringComparison.Ordinal)
			|| lower.Contains(".manifest_", StringComparison.Ordinal);
		return recognizedExtension;
	}

	public static async Task ExportUnityProject(string path)
	{
		if (IsLoaded && IsValidExportDirectory(path))
		{
			if (IsNonEmptyDirectory(path))
			{
				if (!await UserConsentsToDeletion())
				{
					Logger.Info(LogCategory.Export, "User declined to delete existing export directory. Aborting export.");
					return;
				}
				Directory.Delete(path, true);
			}

			Directory.CreateDirectory(path);
			ExportHandler.Export(GameData, path, LocalFileSystem.Instance);
		}
	}

	public static async Task ExportPrimaryContent(string path)
	{
		if (IsLoaded && IsValidExportDirectory(path))
		{
			if (IsNonEmptyDirectory(path))
			{
				if (!await UserConsentsToDeletion())
				{
					Logger.Info(LogCategory.Export, "User declined to delete existing export directory. Aborting export.");
					return;
				}
				Directory.Delete(path, true);
			}

			Directory.CreateDirectory(path);
			Logger.Info(LogCategory.Export, "Starting primary content export");
			Logger.Info(LogCategory.Export, $"Attempting to export assets to {path}...");
			Settings.ExportRootPath = path;
			PrimaryContentExporter.CreateDefault(GameData, Settings).Export(GameBundle, Settings, LocalFileSystem.Instance);
			Logger.Info(LogCategory.Export, "Finished exporting primary content.");
		}
	}

	private static FullConfiguration LoadSettings()
	{
		FullConfiguration settings = new();
		settings.LoadFromDefaultPath();
		return settings;
	}

	private static bool IsValidExportDirectory(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			Logger.Error(LogCategory.Export, "Export path is empty");
			return false;
		}

		string fullPath;
		try
		{
			fullPath = Path.GetFullPath(path);
		}
		catch (Exception ex)
		{
			Logger.Error(LogCategory.Export, $"Export path '{path}' is invalid: {ex.Message}");
			return false;
		}

		string? root = Path.GetPathRoot(fullPath);
		if (root is not null && string.Equals(fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
		{
			Logger.Error(LogCategory.Export, $"Refusing to export to the filesystem root '{fullPath}'.");
			return false;
		}

		string directoryName = Path.GetFileName(fullPath);
		if (directoryName is "Desktop" or "Documents" or "Downloads")
		{
			Logger.Error(LogCategory.Export, $"Export path '{fullPath}' is a system directory");
			return false;
		}

		foreach (string loadedPath in LoadedPaths)
		{
			if (IsSameOrInside(fullPath, loadedPath))
			{
				Logger.Error(LogCategory.Export, $"Refusing to export inside an imported path '{loadedPath}'. Choose a separate output directory.");
				return false;
			}
		}
		return true;
	}

	private static string GetFullPathOrOriginal(string path)
	{
		try
		{
			return Path.GetFullPath(path);
		}
		catch
		{
			return path;
		}
	}

	private static bool IsSameOrInside(string candidate, string basePath)
	{
		if (string.Equals(candidate, basePath, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		if (!Directory.Exists(basePath))
		{
			return false;
		}
		string relative = Path.GetRelativePath(basePath, candidate);
		return relative != "."
			&& !Path.IsPathRooted(relative)
			&& relative != ".."
			&& !relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
			&& !relative.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal);
	}

	private static bool IsNonEmptyDirectory(string path)
	{
		return Directory.Exists(path) && Directory.EnumerateFileSystemEntries(path).Any();
	}

	private static async Task<bool> UserConsentsToDeletion()
	{
		if (Headless)
		{
			return true;
		}
		ConfirmationDialog.Options options = new()
		{
			Message = Localization.ExportDirectoryDeleteUserConfirmation,
			Type = ConfirmationDialog.Type.YesNo,
		};
		bool? result = await ConfirmationDialog.Confirm(options);
		return result ?? false;
	}
}
