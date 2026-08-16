using AssetRipper.Assets;
using AssetRipper.Assets.Bundles;
using AssetRipper.Assets.Collections;
using AssetRipper.Assets.Metadata;

namespace AssetRipper.Premium;

/// <summary>
/// Builds a bounded, read-only dependency summary from PPtr values that the ordinary importer
/// already exposed. It does not attempt to synthesize unavailable collections or targets.
/// </summary>
public static class PremiumReferenceGraphAnalyzer
{
	private const int DefaultMaximumEdgeCount = 250_000;

	public static PremiumReferenceGraphReport Create(GameBundle gameBundle, int maximumEdgeCount = DefaultMaximumEdgeCount)
	{
		ArgumentNullException.ThrowIfNull(gameBundle);
		if (maximumEdgeCount < 1)
		{
			throw new ArgumentOutOfRangeException(nameof(maximumEdgeCount));
		}

		IUnityObjectBase[] assets = gameBundle.FetchAssets()
			.OrderBy(static asset => GetNodeId(asset), StringComparer.Ordinal)
			.ToArray();
		List<PremiumReferenceLink> links = new();
		bool truncated = false;

		foreach (IUnityObjectBase asset in assets)
		{
			foreach ((string fieldName, PPtr pointer) in asset.FetchDependencies())
			{
				if (links.Count == maximumEdgeCount)
				{
					truncated = true;
					break;
				}
				links.Add(CreateLink(asset, fieldName, pointer));
			}
			if (truncated)
			{
				break;
			}
		}

		return Analyze(assets.Select(GetNodeId), links, truncated);
	}

	public static PremiumReferenceGraphReport Analyze(IEnumerable<string> nodeIds, IEnumerable<PremiumReferenceLink> links, bool truncated = false)
	{
		ArgumentNullException.ThrowIfNull(nodeIds);
		ArgumentNullException.ThrowIfNull(links);

		string[] nodes = nodeIds.Distinct(StringComparer.Ordinal).OrderBy(static id => id, StringComparer.Ordinal).ToArray();
		PremiumReferenceLink[] orderedLinks = links
			.OrderBy(static link => link.SourceId, StringComparer.Ordinal)
			.ThenBy(static link => link.FieldName, StringComparer.Ordinal)
			.ThenBy(static link => link.TargetId, StringComparer.Ordinal)
			.ToArray();
		Dictionary<string, List<string>> adjacency = nodes.ToDictionary(static id => id, static _ => new List<string>(), StringComparer.Ordinal);

		long resolved = 0;
		long nullReferences = 0;
		long missingCollections = 0;
		long missingAssets = 0;
		foreach (PremiumReferenceLink link in orderedLinks)
		{
			switch (link.Resolution)
			{
				case PremiumReferenceResolution.Resolved when link.TargetId is not null && adjacency.TryGetValue(link.SourceId, out List<string>? targets):
					resolved++;
					targets.Add(link.TargetId);
					break;
				case PremiumReferenceResolution.Null:
					nullReferences++;
					break;
				case PremiumReferenceResolution.MissingCollection:
					missingCollections++;
					break;
				case PremiumReferenceResolution.MissingAsset:
					missingAssets++;
					break;
			}
		}

		foreach (List<string> targets in adjacency.Values)
		{
			targets.Sort(StringComparer.Ordinal);
		}

		return new PremiumReferenceGraphReport(
			nodes.LongLength,
			orderedLinks.LongLength,
			resolved,
			nullReferences,
			missingCollections,
			missingAssets,
			CountBackEdges(nodes, adjacency),
			truncated);
	}

	private static PremiumReferenceLink CreateLink(IUnityObjectBase source, string fieldName, PPtr pointer)
	{
		string sourceId = GetNodeId(source);
		if (pointer.IsNull)
		{
			return new PremiumReferenceLink(sourceId, fieldName, null, PremiumReferenceResolution.Null);
		}

		IReadOnlyList<AssetCollection?> collections = source.Collection.Dependencies;
		if (pointer.FileID < 0 || pointer.FileID >= collections.Count || collections[pointer.FileID] is not AssetCollection targetCollection)
		{
			return new PremiumReferenceLink(sourceId, fieldName, null, PremiumReferenceResolution.MissingCollection);
		}

		if (!targetCollection.Assets.TryGetValue(pointer.PathID, out IUnityObjectBase? target))
		{
			return new PremiumReferenceLink(sourceId, fieldName, null, PremiumReferenceResolution.MissingAsset);
		}

		return new PremiumReferenceLink(sourceId, fieldName, GetNodeId(target), PremiumReferenceResolution.Resolved);
	}

	private static long CountBackEdges(IEnumerable<string> nodes, IReadOnlyDictionary<string, List<string>> adjacency)
	{
		Dictionary<string, byte> state = new(StringComparer.Ordinal);
		long backEdges = 0;
		foreach (string start in nodes)
		{
			if (state.ContainsKey(start))
			{
				continue;
			}

			state[start] = 1;
			Stack<(string Node, int NextIndex)> pending = new();
			pending.Push((start, 0));
			while (pending.TryPop(out (string node, int nextIndex) frame))
			{
				List<string> targets = adjacency[frame.node];
				if (frame.nextIndex >= targets.Count)
				{
					state[frame.node] = 2;
					continue;
				}

				pending.Push((frame.node, frame.nextIndex + 1));
				string target = targets[frame.nextIndex];
				if (!state.TryGetValue(target, out byte targetState))
				{
					state[target] = 1;
					pending.Push((target, 0));
				}
				else if (targetState == 1)
				{
					backEdges++;
				}
			}
		}
		return backEdges;
	}

	private static string GetNodeId(IUnityObjectBase asset)
	{
		string collectionPath = string.IsNullOrWhiteSpace(asset.Collection.FilePath) ? asset.Collection.Name : asset.Collection.FilePath;
		return $"{collectionPath}:{asset.PathID}";
	}
}

public enum PremiumReferenceResolution
{
	Resolved,
	Null,
	MissingCollection,
	MissingAsset,
}

public sealed record PremiumReferenceLink(string SourceId, string FieldName, string? TargetId, PremiumReferenceResolution Resolution);

public sealed record PremiumReferenceGraphReport(
	long NodeCount,
	long EdgeCount,
	long ResolvedEdgeCount,
	long NullReferenceCount,
	long MissingCollectionCount,
	long MissingAssetCount,
	long BackEdgeCount,
	bool IsTruncated);
