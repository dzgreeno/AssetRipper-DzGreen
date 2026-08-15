using AssetRipper.Assets;
using AssetRipper.Assets.Metadata;
using AssetRipper.Assets.Collections;
using AssetRipper.Import.AssetCreation;
using AssetRipper.SourceGenerated.Classes.ClassID_2;
using AssetRipper.SourceGenerated.Subclasses.SceneObjectIdentifier;
using AssetRipper.Yaml;

namespace AssetRipper.Export.UnityProjects.Project;

public sealed class ProjectYamlWalker : YamlWalker
{
	private readonly IExportContainer container;

	public ProjectYamlWalker(IExportContainer container)
	{
		this.container = container;
		WithUnityVersion(container.ExportVersion);
	}

	public IUnityObjectBase CurrentAsset { get; set; } = null!;

	public YamlDocument ExportYamlDocument(IUnityObjectBase asset)
	{
		CurrentAsset = asset;
		return ExportYamlDocument(asset, container.GetExportID(asset));
	}

	public YamlNode ExportYamlNode(IUnityObjectBase asset)
	{
		CurrentAsset = asset;
		return base.ExportYamlNode(asset);
	}

	public override bool EnterAsset(IUnityAssetBase asset)
	{
		if (asset is SceneObjectIdentifier sceneObjectIdentifier)
		{
			long targetObject = sceneObjectIdentifier.TargetObjectReference is not null
				? container.CreateExportPointer(sceneObjectIdentifier.TargetObjectReference).FileID
				: sceneObjectIdentifier.TargetObject;
			long targetPrefab = sceneObjectIdentifier.TargetPrefabReference is not null
				? container.CreateExportPointer(sceneObjectIdentifier.TargetPrefabReference).FileID
				: sceneObjectIdentifier.TargetPrefab;
			YamlMappingNode yamlMappingNode = new()
			{
				{ YamlScalarNode.Create("targetObject"), targetObject },
				{ YamlScalarNode.Create("targetPrefab"), targetPrefab },
			};
			AddNode(yamlMappingNode);
			return false;
		}
		else
		{
			return base.EnterAsset(asset);
		}
	}

	public override YamlNode CreateYamlNodeForPPtr<TAsset>(PPtr<TAsset> pptr)
	{
		if (pptr.PathID == 0)
		{
			return MetaPtr.NullPtr.ExportYaml(container.ExportVersion);
		}
		else if (CurrentAsset.Collection.TryGetAsset(pptr, out TAsset? asset))
		{
			return container.CreateExportPointer(asset).ExportYaml(container.ExportVersion);
		}
		else if (TryResolveRecoveredTypeTreePPtr(pptr, out IUnityObjectBase? recoveredAsset))
		{
			return container.CreateExportPointer(recoveredAsset).ExportYaml(container.ExportVersion);
		}
		else
		{
			AssetType assetType = container.ToExportType(typeof(TAsset));
			MetaPtr pointer = MetaPtr.CreateMissingReference(GetClassID(typeof(TAsset)), assetType);
			return pointer.ExportYaml(container.ExportVersion);
		}
	}

	private bool TryResolveRecoveredTypeTreePPtr<TAsset>(PPtr<TAsset> pptr, [NotNullWhen(true)] out IUnityObjectBase? asset)
		where TAsset : IUnityObjectBase
	{
		asset = null;
		if (pptr.FileID < 0 || pptr.FileID >= CurrentAsset.Collection.Dependencies.Count)
		{
			return false;
		}

		AssetCollection? collection = CurrentAsset.Collection.Dependencies[pptr.FileID];
		if (collection is null || !collection.Assets.TryGetValue(pptr.PathID, out IUnityObjectBase? candidate))
		{
			return false;
		}

		if (candidate is not TypeTreeObject)
		{
			return false;
		}

		// A recovered Type Tree object is the exact serialized target selected by this
		// FileID/PathID pair. Its generated C# interface may be unavailable or may differ from
		// the pointer's nominal interface (for example PPtr<Component> ->
		// SkinnedMeshRenderer), so preserve the concrete serialized target.
		asset = candidate;
		return true;
	}
}
