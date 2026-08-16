using AssetRipper.Assets.Bundles;
using AssetRipper.Assets.Collections;

namespace AssetRipper.Premium;

/// <summary>
/// Classifies TypeTree evidence that the ordinary importer retained for each serialized collection.
/// The classifier reports supported engine-schema coverage but never invents field layouts.
/// </summary>
public static class PremiumTypeTreeCoverageAnalyzer
{
	public static PremiumTypeTreeCoverageReport Create(GameBundle gameBundle)
	{
		ArgumentNullException.ThrowIfNull(gameBundle);
		IEnumerable<PremiumTypeTreeObservation> observations = gameBundle.FetchAssetCollections()
			.OfType<SerializedAssetCollection>()
			.Select(static collection => new PremiumTypeTreeObservation(
				collection.FilePath,
				collection.Name,
				collection.Count,
				collection.HasEmbeddedTypeTree,
				collection.SerializedTypeCount,
				collection.StrippedSerializedTypeCount,
				collection.ReferenceSerializedTypeCount));
		return Analyze(observations);
	}

	public static PremiumTypeTreeCoverageReport Analyze(IEnumerable<PremiumTypeTreeObservation> observations)
	{
		ArgumentNullException.ThrowIfNull(observations);
		PremiumTypeTreeCollectionCoverage[] collections = observations
			.Select(CreateCoverage)
			.OrderBy(static coverage => coverage.CollectionPath, StringComparer.OrdinalIgnoreCase)
			.ThenBy(static coverage => coverage.CollectionName, StringComparer.Ordinal)
			.ToArray();

		return new PremiumTypeTreeCoverageReport(
			collections.LongLength,
			collections.LongCount(static coverage => coverage.State == PremiumTypeTreeCoverageState.Embedded),
			collections.LongCount(static coverage => coverage.State == PremiumTypeTreeCoverageState.KnownEngineSchema),
			collections.LongCount(static coverage => coverage.State == PremiumTypeTreeCoverageState.Partial),
			collections.LongCount(static coverage => coverage.State == PremiumTypeTreeCoverageState.Unavailable),
			collections);
	}

	private static PremiumTypeTreeCollectionCoverage CreateCoverage(PremiumTypeTreeObservation observation)
	{
		PremiumTypeTreeCoverageState state = observation.HasEmbeddedTypeTree
			? observation.StrippedTypeCount > 0 ? PremiumTypeTreeCoverageState.Partial : PremiumTypeTreeCoverageState.Embedded
			: observation.AssetCount > 0 ? PremiumTypeTreeCoverageState.KnownEngineSchema : PremiumTypeTreeCoverageState.Unavailable;
		return new PremiumTypeTreeCollectionCoverage(
			observation.CollectionPath,
			observation.CollectionName,
			observation.AssetCount,
			observation.SerializedTypeCount,
			observation.StrippedTypeCount,
			observation.ReferenceTypeCount,
			state);
	}
}

public enum PremiumTypeTreeCoverageState
{
	Embedded,
	KnownEngineSchema,
	Partial,
	Unavailable,
}

public sealed record PremiumTypeTreeObservation(
	string CollectionPath,
	string CollectionName,
	int AssetCount,
	bool HasEmbeddedTypeTree,
	int SerializedTypeCount,
	int StrippedTypeCount,
	int ReferenceTypeCount);

public sealed record PremiumTypeTreeCollectionCoverage(
	string CollectionPath,
	string CollectionName,
	int AssetCount,
	int SerializedTypeCount,
	int StrippedTypeCount,
	int ReferenceTypeCount,
	PremiumTypeTreeCoverageState State);

public sealed record PremiumTypeTreeCoverageReport(
	long CollectionCount,
	long EmbeddedCollectionCount,
	long KnownEngineSchemaCollectionCount,
	long PartialCollectionCount,
	long UnavailableCollectionCount,
	IReadOnlyList<PremiumTypeTreeCollectionCoverage> Collections);
