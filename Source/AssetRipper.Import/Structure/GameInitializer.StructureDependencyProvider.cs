using AssetRipper.Assets.Bundles;
using AssetRipper.Import.Logging;
using AssetRipper.Import.Structure.Platforms;
using AssetRipper.IO.Files;
using AssetRipper.IO.Files.SerializedFiles.Parser;

namespace AssetRipper.Import.Structure;

internal sealed partial record class GameInitializer
{
	private sealed record class StructureDependencyProvider(
		PlatformGameStructure? PlatformStructure,
		PlatformGameStructure? MixedStructure,
		FileSystem FileSystem)
		: IDependencyProvider
	{
		public FileBase? FindDependency(FileIdentifier identifier)
		{
			string? requestedName = identifier.PathNameOrigin;
			string normalizedName = identifier.PathName;
			string? systemFilePath = RequestDependency(normalizedName);
			if (systemFilePath is not null)
			{
				Logger.Info(LogCategory.Import, $"Resolved dependency '{requestedName}' as '{normalizedName}' from '{systemFilePath}'.");
				return SchemeReader.LoadFile(systemFilePath, FileSystem);
			}

			string searched = string.Join("; ", new[] { PlatformStructure?.RootPath, PlatformStructure?.GameDataPath, MixedStructure?.RootPath, MixedStructure?.DataPaths.FirstOrDefault() }.Where(path => !string.IsNullOrWhiteSpace(path)));
			Logger.Warning(LogCategory.Import, $"Dependency '{requestedName}' was normalized to '{normalizedName}' but no matching file was found. Searched: {searched}. If this is a split/cab companion, select the complete containing folder or place the unmodified companion beside the selected Unity files.");
			return null;
		}

		/// <summary>
		/// Attempts to find the path for the dependency with that name.
		/// </summary>
		private string? RequestDependency(string dependency)
		{
			return PlatformStructure?.RequestDependency(dependency) ?? MixedStructure?.RequestDependency(dependency);
		}

		public void ReportMissingDependency(FileIdentifier identifier)
		{
			Logger.Log(LogType.Warning, LogCategory.Import, $"Dependency '{identifier.PathNameOrigin}' wasn't found after normalization to '{identifier.PathName}'. No file was fabricated and no protected content was bypassed.");
		}
	}
}
