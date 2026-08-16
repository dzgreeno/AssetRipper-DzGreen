using AssetRipper.Assets;
using AssetRipper.Assets.Bundles;
using AssetRipper.SourceGenerated.Classes.ClassID_1001;
using AssetRipper.SourceGenerated.Classes.ClassID_4;
using AssetRipper.SourceGenerated.MarkerInterfaces;
using System.Numerics;

namespace AssetRipper.Premium;

/// <summary>
/// Reconstructs only the Transform graph already exposed by the normal importer. The reconstructor
/// is read-only, sorts all canonical identifiers, and never invents a missing parent or child link.
/// </summary>
public static class PremiumHierarchyReconstructor
{
	public static PremiumHierarchyReport Create(GameBundle gameBundle)
	{
		ArgumentNullException.ThrowIfNull(gameBundle);
		PremiumHierarchyNode[] nodes = gameBundle.FetchAssets()
			.OfType<ITransform>()
			.Select(CreateNode)
			.OrderBy(static node => node.Id, StringComparer.Ordinal)
			.ToArray();
		return Analyze(nodes);
	}

	public static PremiumHierarchyReport Analyze(IEnumerable<PremiumHierarchyNode> sourceNodes)
	{
		ArgumentNullException.ThrowIfNull(sourceNodes);
		PremiumHierarchyNode[] nodes = sourceNodes
			.OrderBy(static node => node.Id, StringComparer.Ordinal)
			.ToArray();
		Dictionary<string, PremiumHierarchyNode> nodeById = new(StringComparer.Ordinal);
		long duplicateIdCount = 0;
		foreach (PremiumHierarchyNode node in nodes)
		{
			if (!nodeById.TryAdd(node.Id, node))
			{
				duplicateIdCount++;
			}
		}

		List<PremiumHierarchyNodeResult> results = new(nodeById.Count);
		long missingParentCount = 0;
		long missingChildCount = 0;
		long parentChildDisagreementCount = 0;
		long cyclicNodeCount = 0;
		long cycleComponentCount = 0;
		HashSet<string> cycleMembers = FindCycleMembers(nodeById, out cycleComponentCount);

		foreach (PremiumHierarchyNode node in nodeById.Values.OrderBy(static item => item.Id, StringComparer.Ordinal))
		{
			bool isCyclic = cycleMembers.Contains(node.Id);
			bool hasCyclicAncestor = isCyclic || HasCyclicAncestor(node, nodeById, cycleMembers);
			if (isCyclic)
			{
				cyclicNodeCount++;
			}

			bool parentIsMissing = node.ParentId is not null && !nodeById.ContainsKey(node.ParentId);
			if (parentIsMissing)
			{
				missingParentCount++;
			}
			bool isPresentInParentChildren = node.ParentId is null
				|| parentIsMissing
				|| nodeById[node.ParentId].ChildIds.Contains(node.Id, StringComparer.Ordinal);
			if (!isPresentInParentChildren)
			{
				parentChildDisagreementCount++;
			}

			foreach (string childId in node.ChildIds.Distinct(StringComparer.Ordinal).OrderBy(static id => id, StringComparer.Ordinal))
			{
				if (!nodeById.TryGetValue(childId, out PremiumHierarchyNode? child))
				{
					missingChildCount++;
				}
				else if (!string.Equals(child.ParentId, node.Id, StringComparison.Ordinal))
				{
					parentChildDisagreementCount++;
				}
			}

			Matrix4x4? worldMatrix = hasCyclicAncestor || parentIsMissing ? null : CalculateWorldMatrix(node, nodeById);
			results.Add(new PremiumHierarchyNodeResult(node.Id, node.ParentId, node.IsRectTransform, node.LocalMatrix, worldMatrix, isCyclic, hasCyclicAncestor, parentIsMissing, isPresentInParentChildren));
		}

		return new PremiumHierarchyReport(
			nodeById.Count,
			nodeById.Values.LongCount(static node => node.IsRectTransform),
			results.LongCount(static result => result.ParentId is null),
			missingParentCount,
			missingChildCount,
			parentChildDisagreementCount,
			cycleComponentCount,
			cyclicNodeCount,
			duplicateIdCount,
			results);
	}

	private static PremiumHierarchyNode CreateNode(ITransform transform)
	{
		Vector3 position = new(transform.LocalPosition_C4.X, transform.LocalPosition_C4.Y, transform.LocalPosition_C4.Z);
		Quaternion rotation = new(transform.LocalRotation_C4.X, transform.LocalRotation_C4.Y, transform.LocalRotation_C4.Z, transform.LocalRotation_C4.W);
		Vector3 scale = new(transform.LocalScale_C4.X, transform.LocalScale_C4.Y, transform.LocalScale_C4.Z);
		Matrix4x4 localMatrix = Matrix4x4.CreateScale(scale)
			* Matrix4x4.CreateFromQuaternion(rotation)
			* Matrix4x4.CreateTranslation(position);
		return new PremiumHierarchyNode(
			GetNodeId(transform),
			transform.Father_C4P is { } parent ? GetNodeId(parent) : null,
			transform.Children_C4P.WhereNotNull().Select(GetNodeId).OrderBy(static id => id, StringComparer.Ordinal).ToArray(),
			localMatrix,
			string.Equals(transform.ClassName, "RectTransform", StringComparison.Ordinal));
	}

	private static Matrix4x4 CalculateWorldMatrix(PremiumHierarchyNode node, IReadOnlyDictionary<string, PremiumHierarchyNode> nodeById)
	{
		Matrix4x4 matrix = node.LocalMatrix;
		string? parentId = node.ParentId;
		while (parentId is not null && nodeById.TryGetValue(parentId, out PremiumHierarchyNode? parent))
		{
			matrix *= parent.LocalMatrix;
			parentId = parent.ParentId;
		}
		return matrix;
	}

	private static bool HasCyclicAncestor(PremiumHierarchyNode node, IReadOnlyDictionary<string, PremiumHierarchyNode> nodeById, IReadOnlySet<string> cycleMembers)
	{
		string? parentId = node.ParentId;
		while (parentId is not null && nodeById.TryGetValue(parentId, out PremiumHierarchyNode? parent))
		{
			if (cycleMembers.Contains(parentId))
			{
				return true;
			}
			parentId = parent.ParentId;
		}
		return false;
	}

	private static HashSet<string> FindCycleMembers(IReadOnlyDictionary<string, PremiumHierarchyNode> nodeById, out long componentCount)
	{
		HashSet<string> members = new(StringComparer.Ordinal);
		HashSet<string> visited = new(StringComparer.Ordinal);
		componentCount = 0;
		foreach (string start in nodeById.Keys.OrderBy(static id => id, StringComparer.Ordinal))
		{
			if (!visited.Add(start))
			{
				continue;
			}

			Dictionary<string, int> positions = new(StringComparer.Ordinal);
			List<string> path = [];
			string? current = start;
			while (current is not null && nodeById.TryGetValue(current, out PremiumHierarchyNode? node))
			{
				if (positions.TryGetValue(current, out int cycleStart))
				{
					componentCount++;
					for (int i = cycleStart; i < path.Count; i++)
					{
						members.Add(path[i]);
					}
					break;
				}
				if (!visited.Add(current) && current != start)
				{
					break;
				}
				positions.Add(current, path.Count);
				path.Add(current);
				current = node.ParentId;
			}
		}
		return members;
	}

	private static string GetNodeId(IUnityObjectBase asset)
	{
		string collectionPath = string.IsNullOrWhiteSpace(asset.Collection.FilePath) ? asset.Collection.Name : asset.Collection.FilePath;
		return $"{collectionPath}:{asset.PathID}";
	}
}

/// <summary>
/// Inspects only Prefab dependency fields that the normal importer exposes. Serialized property
/// override values are deliberately not guessed when a generated schema has not exposed them.
/// </summary>
public static class PremiumPrefabOverrideResolver
{
	public static PremiumPrefabOverrideReport Create(GameBundle gameBundle)
	{
		ArgumentNullException.ThrowIfNull(gameBundle);
		PremiumPrefabSummary[] prefabs = gameBundle.FetchAssets()
			.OfType<IPrefabInstance>()
			.OrderBy(static prefab => GetNodeId(prefab), StringComparer.Ordinal)
			.Select(CreateSummary)
			.ToArray();
		return new PremiumPrefabOverrideReport(
			prefabs.LongCount(static prefab => prefab.Kind == PremiumPrefabKind.Definition),
			prefabs.LongCount(static prefab => prefab.Kind == PremiumPrefabKind.Instance),
			prefabs.Sum(static prefab => prefab.ExposedModificationFieldCount),
			prefabs.Sum(static prefab => prefab.NullModificationReferenceCount),
			prefabs);
	}

	public static PremiumPrefabPropertyResolution Resolve(
		IReadOnlyDictionary<string, string?> baseProperties,
		IEnumerable<PremiumPrefabPropertyOverride> overrides)
	{
		ArgumentNullException.ThrowIfNull(baseProperties);
		ArgumentNullException.ThrowIfNull(overrides);
		SortedDictionary<string, string?> resolved = new(baseProperties.ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal), StringComparer.Ordinal);
		List<PremiumPrefabPropertyOverride> unresolved = [];
		foreach (PremiumPrefabPropertyOverride modification in overrides
			.OrderBy(static item => item.TargetId, StringComparer.Ordinal)
			.ThenBy(static item => item.PropertyPath, StringComparer.Ordinal))
		{
			if (!resolved.ContainsKey(modification.PropertyPath))
			{
				unresolved.Add(modification);
				continue;
			}
			resolved[modification.PropertyPath] = modification.SerializedValue;
		}
		return new PremiumPrefabPropertyResolution(resolved, unresolved);
	}

	private static PremiumPrefabSummary CreateSummary(IPrefabInstance prefab)
	{
		var modifications = prefab.FetchDependencies()
			.Where(static dependency => dependency.Item1.Contains("Modification", StringComparison.OrdinalIgnoreCase))
			.OrderBy(static dependency => dependency.Item1, StringComparer.Ordinal)
			.ToArray();
		return new PremiumPrefabSummary(
			GetNodeId(prefab),
			prefab is IPrefabMarker ? PremiumPrefabKind.Definition : PremiumPrefabKind.Instance,
			modifications.LongLength,
			modifications.LongCount(static modification => modification.Item2.IsNull),
			modifications.Select(static modification => modification.Item1).ToArray());
	}

	private static string GetNodeId(IUnityObjectBase asset)
	{
		string collectionPath = string.IsNullOrWhiteSpace(asset.Collection.FilePath) ? asset.Collection.Name : asset.Collection.FilePath;
		return $"{collectionPath}:{asset.PathID}";
	}
}

public sealed record PremiumHierarchyNode(string Id, string? ParentId, IReadOnlyList<string> ChildIds, Matrix4x4 LocalMatrix, bool IsRectTransform);
public sealed record PremiumHierarchyNodeResult(string Id, string? ParentId, bool IsRectTransform, Matrix4x4 LocalMatrix, Matrix4x4? WorldMatrix, bool IsCyclic, bool HasCyclicAncestor, bool ParentIsMissing, bool IsPresentInParentChildren);
public sealed record PremiumHierarchyReport(long TransformCount, long RectTransformCount, long RootCount, long MissingParentCount, long MissingChildCount, long ParentChildDisagreementCount, long CycleComponentCount, long CyclicNodeCount, long DuplicateIdCount, IReadOnlyList<PremiumHierarchyNodeResult> Nodes);

public enum PremiumPrefabKind
{
	Definition,
	Instance,
}

public sealed record PremiumPrefabSummary(string Id, PremiumPrefabKind Kind, long ExposedModificationFieldCount, long NullModificationReferenceCount, IReadOnlyList<string> ModificationFields);
public sealed record PremiumPrefabOverrideReport(long DefinitionCount, long InstanceCount, long ExposedModificationFieldCount, long NullModificationReferenceCount, IReadOnlyList<PremiumPrefabSummary> Prefabs);
public sealed record PremiumPrefabPropertyOverride(string TargetId, string PropertyPath, string? SerializedValue);
public sealed record PremiumPrefabPropertyResolution(IReadOnlyDictionary<string, string?> EffectiveProperties, IReadOnlyList<PremiumPrefabPropertyOverride> UnresolvedOverrides);
