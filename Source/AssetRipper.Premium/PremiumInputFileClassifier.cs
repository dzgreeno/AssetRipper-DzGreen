namespace AssetRipper.Premium;

/// <summary>
/// Classifies only recognizable plaintext Unity inputs and their companion files.
/// This is deliberately filename-based: it broadens local, user-authorized input discovery
/// without identifying arbitrary containers as Unity content.
/// </summary>
public static class PremiumInputFileClassifier
{
	public static PremiumInputKind Classify(string path)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(path);
		string fileName = Path.GetFileName(path);
		string lower = fileName.ToLowerInvariant();
		if (lower.StartsWith("cab-", StringComparison.Ordinal) || lower.Contains(".split", StringComparison.Ordinal))
		{
			return PremiumInputKind.UnityBundle;
		}

		if (lower is "globalgamemanagers" or "mainData")
		{
			return PremiumInputKind.SerializedFile;
		}

		if (lower.EndsWith(".unity3d", StringComparison.Ordinal)
			|| lower.Contains(".unity3d_", StringComparison.Ordinal)
			|| lower.EndsWith(".bundle", StringComparison.Ordinal)
			|| lower.Contains(".bundle_", StringComparison.Ordinal)
			|| lower.EndsWith(".manifest", StringComparison.Ordinal)
			|| lower.Contains(".manifest_", StringComparison.Ordinal))
		{
			return PremiumInputKind.UnityBundle;
		}

		if (lower.EndsWith(".assets", StringComparison.Ordinal) || lower.Contains(".assets_", StringComparison.Ordinal))
		{
			return PremiumInputKind.SerializedFile;
		}

		if (lower.EndsWith(".res", StringComparison.Ordinal)
			|| lower.EndsWith(".resource", StringComparison.Ordinal)
			|| lower.EndsWith(".ress", StringComparison.Ordinal)
			|| lower.EndsWith(".streaming", StringComparison.Ordinal))
		{
			return PremiumInputKind.ResourceStream;
		}

		return PremiumInputKind.Unknown;
	}

	public static bool IsRecognizedUnityCompanionFile(string fileName) => Classify(fileName) is not PremiumInputKind.Unknown;
}
