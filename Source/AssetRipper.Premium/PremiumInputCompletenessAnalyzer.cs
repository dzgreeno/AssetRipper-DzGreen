namespace AssetRipper.Premium;

using AssetRipper.IO.Files.ResourceFiles;

/// <summary>
/// Summarizes the already selected local input set so operators can distinguish Unity data,
/// streaming companions, and unclassified files. It never probes for missing files or treats
/// an unclassified file as a Unity payload.
/// </summary>
public static class PremiumInputCompletenessAnalyzer
{
	public static PremiumInputCompletenessReport Analyze(IEnumerable<string> inputPaths, IEnumerable<ResourceFile> resourceFiles)
	{
		ArgumentNullException.ThrowIfNull(inputPaths);
		ArgumentNullException.ThrowIfNull(resourceFiles);
		PremiumInputCompletenessEntry[] entries = inputPaths
			.Select(CreateEntry)
			.OrderBy(static entry => entry.Path, StringComparer.OrdinalIgnoreCase)
			.ToArray();
		string[] loadedResourceNames = resourceFiles
			.Select(static resource => resource.Name)
			.Where(static name => !string.IsNullOrWhiteSpace(name))
			.Order(StringComparer.OrdinalIgnoreCase)
			.ToArray();
		return new PremiumInputCompletenessReport(
			entries.Length,
			entries.LongCount(static entry => entry.IsDirectory),
			entries.LongCount(static entry => !entry.IsDirectory && entry.Kind is PremiumInputKind.UnityBundle),
			entries.LongCount(static entry => !entry.IsDirectory && entry.Kind is PremiumInputKind.SerializedFile),
			entries.LongCount(static entry => !entry.IsDirectory && entry.Kind is PremiumInputKind.ResourceStream),
			entries.LongCount(static entry => !entry.IsDirectory && entry.Kind is PremiumInputKind.Unknown),
			loadedResourceNames.LongLength,
			loadedResourceNames,
			entries);
	}

	private static PremiumInputCompletenessEntry CreateEntry(string path)
	{
		string fullPath = TryGetFullPath(path);
		bool isDirectory = Directory.Exists(fullPath);
		return new PremiumInputCompletenessEntry(fullPath, isDirectory, isDirectory ? PremiumInputKind.UnityBundle : PremiumInputFileClassifier.Classify(fullPath));
	}

	private static string TryGetFullPath(string path)
	{
		try
		{
			return Path.GetFullPath(path);
		}
		catch (Exception)
		{
			return path;
		}
	}
}

public sealed record PremiumInputCompletenessReport(
	long InputPathCount,
	long DirectoryCount,
	long UnityBundleCount,
	long SerializedFileCount,
	long ResourceStreamCount,
	long UnclassifiedFileCount,
	long ImporterConfirmedResourceFileCount,
	IReadOnlyList<string> ImporterConfirmedResourceNames,
	IReadOnlyList<PremiumInputCompletenessEntry> Entries);

public sealed record PremiumInputCompletenessEntry(string Path, bool IsDirectory, PremiumInputKind Kind);
