using AssetRipper.Assets;
using AssetRipper.SourceGenerated.Classes.ClassID_1;
using AssetRipper.SourceGenerated.Classes.ClassID_43;
using AssetRipper.Processing.Prefabs;
using AssetRipper.Export.PrimaryContent;

namespace AssetRipper.Export.PrimaryContent.Models;

internal sealed class FbxExportCollection : SingleExportCollection<IUnityObjectBase>
{
	public FbxExportCollection(IContentExtractor assetExporter, IUnityObjectBase asset) : base(assetExporter, asset) { }
	protected override string ExportExtension => "fbx";
}

internal sealed class FbxMeshExportCollection : SingleExportCollection<IMesh>
{
	public FbxMeshExportCollection(IContentExtractor assetExporter, IMesh asset) : base(assetExporter, asset) { }
	protected override string ExportExtension => "fbx";
}

internal sealed class FbxCharacterExportCollection : MultipleExportCollection<IGameObject>
{
	private readonly FbxAsciiExporter exporter;

	public FbxCharacterExportCollection(FbxAsciiExporter exporter, IGameObject root) : base(exporter, root)
	{
		this.exporter = exporter;
		AddAssets(exporter.GetCharacterAssets(root).Where(asset => !ReferenceEquals(asset, root)));
	}

	protected override string ExportExtension => "fbx";

	public override string Name => $"Character::{Asset.GetBestName()}";
}

internal sealed class FbxPrefabModelExportCollection : MultipleExportCollection<PrefabHierarchyObject>
{
	public FbxPrefabModelExportCollection(FbxAsciiExporter assetExporter, PrefabHierarchyObject asset) : base(assetExporter, asset) => AddAssets(asset.Assets);
	protected override string ExportExtension => "fbx";
}

internal sealed class FbxSceneModelExportCollection : MultipleExportCollection<SceneHierarchyObject>
{
	public FbxSceneModelExportCollection(FbxAsciiExporter assetExporter, SceneHierarchyObject asset) : base(assetExporter, asset) => AddAssets(asset.Assets);
	protected override string ExportExtension => "fbx";
}
