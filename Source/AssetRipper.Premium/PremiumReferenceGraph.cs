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
		List<PremiumMissingCollectionReference> recoveryPriorityMissingCollections = new();
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
					PremiumReferenceLink link = CreateLink(asset, fieldName, pointer);
					links.Add(link);
					if (link.Resolution == PremiumReferenceResolution.MissingCollection && IsRecoveryPriority(asset.ClassID))
					{
						recoveryPriorityMissingCollections.Add(new(GetNodeId(asset), asset.ClassID, fieldName, link.RequestedCollectionName));
					}
			}
			if (truncated)
			{
				break;
			}
		}

		return Analyze(assets.Select(GetNodeId), links, truncated) with
		{
			RecoveryPriorityMissingCollections = recoveryPriorityMissingCollections
				.OrderBy(static reference => reference.ClassID)
				.ThenBy(static reference => reference.SourceId, StringComparer.Ordinal)
				.ThenBy(static reference => reference.FieldName, StringComparer.Ordinal)
				.ToArray(),
		};
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

		PremiumReferenceCycleSummary cycles = AnalyzeCycles(nodes, adjacency);
		return new PremiumReferenceGraphReport(
			nodes.LongLength,
			orderedLinks.LongLength,
			resolved,
			nullReferences,
			missingCollections,
			missingAssets,
			cycles.CycleComponentCount,
			cycles.CyclicNodeCount,
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
			string? requestedCollectionName = source.Collection is SerializedAssetCollection collection
				? collection.GetDependencyName(pointer.FileID)
				: null;
			return new PremiumReferenceLink(sourceId, fieldName, null, PremiumReferenceResolution.MissingCollection, requestedCollectionName);
		}

		if (!targetCollection.Assets.TryGetValue(pointer.PathID, out IUnityObjectBase? target))
		{
			return new PremiumReferenceLink(sourceId, fieldName, null, PremiumReferenceResolution.MissingAsset);
		}

		return new PremiumReferenceLink(sourceId, fieldName, GetNodeId(target), PremiumReferenceResolution.Resolved);
	}

	private const int MeshClassID = 43;
	private const int AvatarClassID = 90;
	private const int SkinnedMeshRendererClassID = 137;
	private static bool IsRecoveryPriority(int classID) => classID is MeshClassID or SkinnedMeshRendererClassID or AvatarClassID;

	private static PremiumReferenceCycleSummary AnalyzeCycles(IReadOnlyList<string> nodes, IReadOnlyDictionary<string, List<string>> adjacency)
	{
		Dictionary<string, byte> state = new(StringComparer.Ordinal);
		List<string> finishOrder = new(nodes.Count);
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
					finishOrder.Add(frame.node);
					continue;
				}

				pending.Push((frame.node, frame.nextIndex + 1));
				string target = targets[frame.nextIndex];
				if (!state.ContainsKey(target))
				{
					state[target] = 1;
					pending.Push((target, 0));
				}
			}
		}

		Dictionary<string, List<string>> reverse = nodes.ToDictionary(static id => id, static _ => new List<string>(), StringComparer.Ordinal);
		foreach ((string source, List<string> targets) in adjacency)
		{
			foreach (string target in targets)
			{
				reverse[target].Add(source);
			}
		}
		foreach (List<string> sources in reverse.Values)
		{
			sources.Sort(StringComparer.Ordinal);
		}

		HashSet<string> assigned = new(StringComparer.Ordinal);
		long cycleComponents = 0;
		long cyclicNodes = 0;
		for (int i = finishOrder.Count - 1; i >= 0; i--)
		{
			string start = finishOrder[i];
			if (!assigned.Add(start))
			{
				continue;
			}

			List<string> component = new();
			Stack<string> pending = new();
			pending.Push(start);
			while (pending.Count > 0)
			{
				string node = pending.Pop();
				component.Add(node);
				foreach (string source in reverse[node])
				{
					if (assigned.Add(source))
					{
						pending.Push(source);
					}
				}
			}

			bool isSelfCycle = component.Count == 1 && adjacency[component[0]].Contains(component[0], StringComparer.Ordinal);
			if (component.Count > 1 || isSelfCycle)
			{
				cycleComponents++;
				cyclicNodes += component.Count;
			}
		}
		return new PremiumReferenceCycleSummary(cycleComponents, cyclicNodes);
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

public sealed record PremiumReferenceLink(string SourceId, string FieldName, string? TargetId, PremiumReferenceResolution Resolution, string? RequestedCollectionName = null);

public sealed record PremiumMissingCollectionReference(string SourceId, int ClassID, string FieldName, string? RequestedCollectionName);

public sealed record PremiumReferenceGraphReport(
	long NodeCount,
	long EdgeCount,
	long ResolvedEdgeCount,
	long NullReferenceCount,
	long MissingCollectionCount,
	long MissingAssetCount,
	long CycleComponentCount,
	long CyclicNodeCount,
	bool IsTruncated)
{
	public IReadOnlyList<PremiumMissingCollectionReference> RecoveryPriorityMissingCollections { get; init; } = [];
}

internal readonly record struct PremiumReferenceCycleSummary(long CycleComponentCount, long CyclicNodeCount);
