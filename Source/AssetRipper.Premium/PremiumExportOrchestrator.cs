namespace AssetRipper.Premium;

/// <summary>
/// Produces deterministic export eligibility and fallback-texture catalogs from the already loaded
/// diagnostic report. It validates file names and paths only; applying a replacement is left to an
/// exporter that explicitly supports the requested target format.
/// </summary>
public static class PremiumExportOrchestrator
{
	private static readonly HashSet<string> SupportedTextureExtensions = new(StringComparer.OrdinalIgnoreCase)
	{
		".bmp", ".gif", ".jpeg", ".jpg", ".png", ".tga", ".tif", ".tiff", ".webp",
	};

	public static PremiumVerifiedOnlyPlan CreateVerifiedOnlyPlan(PremiumTypeTreeCoverageReport coverage, IEnumerable<PremiumExportCandidate> candidates)
	{
		ArgumentNullException.ThrowIfNull(coverage);
		ArgumentNullException.ThrowIfNull(candidates);
		Dictionary<(string Path, string Name), PremiumTypeTreeCoverageState> states = coverage.Collections
			.GroupBy(static item => (item.CollectionPath, item.CollectionName))
			.ToDictionary(static group => group.Key, static group => group.First().State);
		PremiumVerifiedAssetDecision[] decisions = candidates
			.OrderBy(static candidate => candidate.CollectionPath, StringComparer.OrdinalIgnoreCase)
			.ThenBy(static candidate => candidate.CollectionName, StringComparer.Ordinal)
			.ThenBy(static candidate => candidate.PathId)
			.Select(candidate => CreateDecision(candidate, states))
			.ToArray();
		return new PremiumVerifiedOnlyPlan(
			decisions.LongCount(static item => item.IsEligible),
			decisions.LongCount(static item => !item.IsEligible),
			decisions);
	}

	public static PremiumFallbackTextureCatalog CreateFallbackTextureCatalog(string directory)
	{
		if (string.IsNullOrWhiteSpace(directory))
		{
			throw new ArgumentException("A fallback texture directory is required.", nameof(directory));
		}
		string fullPath = Path.GetFullPath(directory);
		if (!Directory.Exists(fullPath))
		{
			throw new DirectoryNotFoundException($"Fallback texture directory was not found: {fullPath}");
		}
		PremiumFallbackTexture[] textures = Directory.EnumerateFiles(fullPath, "*", SearchOption.TopDirectoryOnly)
			.Where(static path => SupportedTextureExtensions.Contains(Path.GetExtension(path)))
			.Select(path => new PremiumFallbackTexture(Path.GetFileNameWithoutExtension(path), Path.GetFullPath(path), Path.GetExtension(path)))
			.OrderBy(static texture => texture.Key, StringComparer.OrdinalIgnoreCase)
			.ThenBy(static texture => texture.Path, StringComparer.OrdinalIgnoreCase)
			.ToArray();
		return new PremiumFallbackTextureCatalog(fullPath, textures);
	}

	private static PremiumVerifiedAssetDecision CreateDecision(PremiumExportCandidate candidate, IReadOnlyDictionary<(string Path, string Name), PremiumTypeTreeCoverageState> states)
	{
		if (!states.TryGetValue((candidate.CollectionPath, candidate.CollectionName), out PremiumTypeTreeCoverageState state))
		{
			return new PremiumVerifiedAssetDecision(candidate, false, null, "The collection has no TypeTree coverage record.");
		}
		bool isEligible = state is PremiumTypeTreeCoverageState.Embedded or PremiumTypeTreeCoverageState.KnownEngineSchema;
		return new PremiumVerifiedAssetDecision(candidate, isEligible, state, isEligible ? null : $"The collection has {state} TypeTree coverage.");
	}
}

public sealed record PremiumExportCandidate(string CollectionPath, string CollectionName, long PathId, string Name, string ClassName);
public sealed record PremiumVerifiedAssetDecision(PremiumExportCandidate Candidate, bool IsEligible, PremiumTypeTreeCoverageState? CoverageState, string? Reason);
public sealed record PremiumVerifiedOnlyPlan(long EligibleAssetCount, long SkippedAssetCount, IReadOnlyList<PremiumVerifiedAssetDecision> Decisions);
public sealed record PremiumFallbackTexture(string Key, string Path, string Extension);
public sealed record PremiumFallbackTextureCatalog(string Directory, IReadOnlyList<PremiumFallbackTexture> Textures);
