using AssetRipper.Assets;
using AssetRipper.IO.Files.SerializedFiles;
using AssetRipper.Import.AssetCreation;
using AssetRipper.Import.Logging;
using AssetRipper.Processing.Prefabs;
using AssetRipper.SourceGenerated;
using AssetRipper.SourceGenerated.Classes.ClassID_1;
using AssetRipper.SourceGenerated.Classes.ClassID_1001;
using AssetRipper.SourceGenerated.Classes.ClassID_468431735;
using AssetRipper.SourceGenerated.Extensions;
using AssetRipper.SourceGenerated.MarkerInterfaces;

namespace AssetRipper.Export.UnityProjects.Project;

public class PrefabExportCollection : AssetsExportCollection<IPrefabInstance>
{
	private readonly List<IUnityObjectBase> recoveredSkinnedMeshRenderers = [];

	public PrefabExportCollection(IAssetExporter assetExporter, PrefabHierarchyObject prefabHierarchyObject)
		: base(assetExporter, prefabHierarchyObject.Prefab)
	{
		RootGameObject = prefabHierarchyObject.Root;
		Prefab = prefabHierarchyObject.Prefab;
		Hierarchy = prefabHierarchyObject;
		AddAssets(prefabHierarchyObject.Assets);
		AddRecoveredSkinnedMeshRenderers(prefabHierarchyObject);
		AddAsset(prefabHierarchyObject);
	}

	private void AddRecoveredSkinnedMeshRenderers(PrefabHierarchyObject prefabHierarchyObject)
	{
		// android.rar contains Unity 2020 SkinnedMeshRenderer records that were recovered
		// from embedded Type Trees. They do not implement IComponent and therefore cannot
		// enter PrefabHierarchyObject during processing, but their owning GameObjects retain
		// valid m_Component PPtrs. Add them at export time so the YAML document contains the
		// renderer alongside its GameObject, Mesh, Materials, and bone references.
		foreach (IGameObject gameObject in prefabHierarchyObject.GameObjects)
		{
			foreach (var componentPointer in gameObject.FetchComponents())
			{
				IUnityObjectBase? component = componentPointer.FileID == 0
					// TypeTreeObject derives from NullObject, so TryGetAsset deliberately hides it.
					// The original local component list remains authoritative for this recovery path.
					? gameObject.Collection.FirstOrDefault(asset => asset.PathID == componentPointer.PathID)
					: gameObject.Collection.TryGetAsset(componentPointer.FileID, componentPointer.PathID);
				if (component is TypeTreeObject { ClassID: (int)ClassIDType.SkinnedMeshRenderer })
				{
					if (AddAsset(component))
					{
						recoveredSkinnedMeshRenderers.Add(component);
						Logger.Info(LogCategory.Export, $"Added recovered SkinnedMeshRenderer '{component.PathID}' to Prefab '{prefabHierarchyObject.Name}'.");
					}
				}
			}
		}
	}

	protected override string GetExportExtension(IUnityObjectBase asset) => PrefabKeyword;

	public override TransferInstructionFlags Flags => base.Flags | TransferInstructionFlags.SerializeForPrefabSystem;
	public IGameObject RootGameObject { get; }
	public IPrefabInstance Prefab { get; }
	public PrefabHierarchyObject Hierarchy { get; }
	/// <summary>
	/// Prior to 2018.3, Prefab was an actual asset inside "*.prefab" files.
	/// After that, PrefabImporter and PrefabInstance were introduced as a replacement.
	/// </summary>
	public bool EmitPrefabAsset => Prefab is IPrefabMarker;
	public override string Name => RootGameObject.Name;

	protected override IUnityObjectBase CreateImporter(IExportContainer container)
	{
		if (EmitPrefabAsset)
		{
			return base.CreateImporter(container);
		}
		else
		{
			IPrefabImporter importer = PrefabImporter.Create(container.File, container.ExportVersion);
			if (RootGameObject.AssetBundleName is not null)
			{
				importer.AssetBundleName_R = RootGameObject.AssetBundleName;
			}
			return importer;
		}
	}

	public override IEnumerable<IUnityObjectBase> ExportableAssets
	{
		get
		{
				foreach (IUnityObjectBase asset in Hierarchy.ExportableAssets)
				{
					m_file = asset.Collection;
					yield return asset;
				}
				foreach (IUnityObjectBase renderer in recoveredSkinnedMeshRenderers)
				{
					m_file = renderer.Collection;
					yield return renderer;
				}
			}
		}

	/// <summary>
	/// Used for <see cref="IPrefabInstance.SourcePrefabP"/>
	/// </summary>
	/// <returns></returns>
	public MetaPtr GenerateMetaPtrForPrefab()
	{
		return new MetaPtr(
			ExportIdHandler.GetMainExportID((int)ClassIDType.PrefabInstance),
			GUID,
			EmitPrefabAsset ? AssetType.Serialized : AssetType.Meta);
	}

	public const string PrefabKeyword = "prefab";
}
