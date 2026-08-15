using AssetRipper.Assets.Collections;
using AssetRipper.Assets.Metadata;
using AssetRipper.Assets;
using AssetRipper.Import.AssetCreation;
using AssetRipper.IO.Files.SerializedFiles;
using AssetRipper.SourceGenerated.Extensions;

namespace AssetRipper.Export.UnityProjects.RawAssets;

/// <summary>
/// Exports assets recovered from an embedded Type Tree as a concise inspection record.
/// Their schema could not be validated by a generated reader, so emitting Unity YAML would
/// be misleading and can recurse indefinitely on malformed or self-referential structures.
/// </summary>
internal sealed class TypeTreeObjectExporter : BinaryAssetExporter
{
	public override bool TryCreateCollection(IUnityObjectBase asset, [NotNullWhen(true)] out IExportCollection? exportCollection)
	{
		if (asset is TypeTreeObject { IsPlayerSettings: false } typeTreeObject)
		{
			exportCollection = new TypeTreeExportCollection(this, typeTreeObject);
			return true;
		}

		exportCollection = null;
		return false;
	}

	public override bool Export(IExportContainer container, IUnityObjectBase asset, string path, FileSystem fileSystem)
	{
		TypeTreeObject typeTreeObject = (TypeTreeObject)asset;
		string content = $"""
		AssetRipper DzGreen recovered Type Tree inspection record

		Name: {((IUnityObjectBase)typeTreeObject).GetBestName()}
		Class: {typeTreeObject.ClassName}
		ClassID: {typeTreeObject.ClassID}
		PathID: {typeTreeObject.PathID}
		Collection: {typeTreeObject.Collection.Name}

		This object was recovered from an embedded serialized Type Tree because the generated class reader could not validate its schema. It is intentionally not emitted as Unity YAML or FBX/GLB: doing so could create invalid output or recurse through malformed field definitions. The original bundle remains unchanged and can be inspected through Asset Workspace, JSON, YAML view, and dependency analysis.
		""";
		fileSystem.File.WriteAllText(path, content);
		return true;
	}

	private sealed class TypeTreeExportCollection : ExportCollection
	{
		private readonly TypeTreeObject asset;

		public TypeTreeExportCollection(IAssetExporter exporter, TypeTreeObject asset)
		{
			AssetExporter = exporter;
			this.asset = asset;
		}

		public override IAssetExporter AssetExporter { get; }
		public override AssetCollection File => asset.Collection;
		public override TransferInstructionFlags Flags => asset.Collection.Flags;
		public override IEnumerable<IUnityObjectBase> Assets { get { yield return asset; } }
		public override string Name => ((IUnityObjectBase)asset).GetBestName();

		public override bool Export(IExportContainer container, string projectDirectory, FileSystem fileSystem)
		{
			string directory = fileSystem.Path.Join(projectDirectory, "AssetRipper", "RecoveredTypeTrees", SafePathSegment(asset.ClassName));
			fileSystem.Directory.Create(directory);
			string fileName = GetUniqueFileName(directory, $"{SafePathSegment(((IUnityObjectBase)asset).GetBestName())}_{asset.PathID}.typetree.txt", fileSystem);
			return AssetExporter.Export(container, asset, fileSystem.Path.Join(directory, fileName), fileSystem);
		}

		public override MetaPtr CreateExportPointer(IExportContainer container, IUnityObjectBase asset, bool isLocal) => MetaPtr.NullPtr;
		public override long GetExportID(IExportContainer container, IUnityObjectBase asset) => throw new NotSupportedException();
		public override bool Contains(IUnityObjectBase other) => other.AssetInfo == asset.AssetInfo;

		private static string SafePathSegment(string value)
		{
			string result = FileSystem.FixInvalidPathCharacters(value);
			if (string.IsNullOrWhiteSpace(result))
			{
				return "RecoveredAsset";
			}
			return result.Length > 96 ? result[..96] : result;
		}
	}
}
