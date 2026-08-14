using AssetRipper.Assets.Bundles;
using AssetRipper.Export.Configuration;
using AssetRipper.Export.PrimaryContent;
using AssetRipper.Export.UnityProjects;
using AssetRipper.Import.Logging;
using AssetRipper.Import.Structure.Assembly.Managers;
using AssetRipper.IO.Files;
using AssetRipper.NativeDialogs;
using AssetRipper.Processing;

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
	public static bool Premium => ExportHandler.GetType() != typeof(ExportHandler);

	public static void Reset()
	{
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
		Reset();
		string[] expandedPaths = ExpandSiblingUnityFiles(paths);
		LoadedPaths = expandedPaths.Select(GetFullPathOrOriginal).ToArray();
		Settings.LogConfigurationValues();
		GameData = ExportHandler.LoadAndProcess(expandedPaths, LocalFileSystem.Instance);
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
		HashSet<string> familyPrefixes = paths
			.Select(GetFullPathOrOriginal)
			.Select(Path.GetFileName)
			.Where(name => !string.IsNullOrWhiteSpace(name))
			.Select(GetUnityFamilyPrefix)
			.Where(prefix => !string.IsNullOrWhiteSpace(prefix))
			.ToHashSet(StringComparer.OrdinalIgnoreCase);
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
				foreach (string sibling in Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly))
				{
					if (IsUnityCompanionFile(Path.GetFileName(sibling), familyPrefixes))
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
			Logger.Info(LogCategory.Import, $"Expanded selected Unity files from {paths.Count} to {expanded.Count} paths to include sibling bundles, manifests, and cab/resource companions.");
		}
		return expanded.ToArray();
	}

	private static bool IsUnityCompanionFile(string fileName, IReadOnlySet<string> familyPrefixes)
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
		return recognizedExtension && familyPrefixes.Any(prefix => lower.StartsWith(prefix, StringComparison.Ordinal));
	}

	private static string GetUnityFamilyPrefix(string? fileName)
	{
		if (string.IsNullOrWhiteSpace(fileName))
		{
			return string.Empty;
		}
		string lower = fileName.ToLowerInvariant();
		int marker = new[] { ".unity3d", ".bundle", ".assets" }
			.Select(value => lower.IndexOf(value, StringComparison.Ordinal))
			.Where(index => index >= 0)
			.DefaultIfEmpty(-1)
			.Min();
		return marker > 0 ? lower[..marker] : lower;
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
