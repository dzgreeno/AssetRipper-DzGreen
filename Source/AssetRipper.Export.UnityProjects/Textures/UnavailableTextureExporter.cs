using AssetRipper.Assets;
using AssetRipper.Assets.Collections;
using AssetRipper.Assets.Metadata;
using AssetRipper.IO.Files.SerializedFiles;
using AssetRipper.SourceGenerated.Classes.ClassID_28;
using AssetRipper.SourceGenerated.Extensions;

namespace AssetRipper.Export.UnityProjects.Textures;

/// <summary>
/// Writes an inspection record for a texture whose streamed payload cannot be resolved.
/// This avoids labelling arbitrary or malformed bytes as a valid image export.
/// </summary>
internal sealed class UnavailableTextureExporter : BinaryAssetExporter
{
	public override bool TryCreateCollection(IUnityObjectBase asset, [NotNullWhen(true)] out IExportCollection? exportCollection)
	{
		if (asset is ITexture2D texture && (HasMalformedStreamPath(texture) || !texture.CheckAssetIntegrity()))
		{
			exportCollection = new UnavailableTextureExportCollection(this, texture);
			return true;
		}

		exportCollection = null;
		return false;
	}

	public override bool Export(IExportContainer container, IUnityObjectBase asset, string path, FileSystem fileSystem)
	{
		ITexture2D texture = (ITexture2D)asset;
		string streamPath = texture.StreamData_C28?.Path ?? "<none>";
		string content = $"""
		AssetRipper DzGreen unavailable texture inspection record

		Name: {((IUnityObjectBase)texture).GetBestName()}
		Class: {texture.ClassName}
		PathID: {texture.PathID}
		Collection: {texture.Collection.Name}
		Streamed resource path: {streamPath}

		The texture payload could not be resolved or validated. No image was emitted because that would produce an invalid or misleading texture file. The original bundle remains unchanged; use Asset Workspace and the full diagnostics log to inspect the dependency and the expected resource path.
		""";
		fileSystem.File.WriteAllText(path, content);
		return true;
	}

	private static bool HasMalformedStreamPath(ITexture2D texture)
	{
		string? path = texture.StreamData_C28?.Path.String;
		return path is { Length: > 0 }
			&& (path.Length > 512 || path.Any(char.IsControl));
	}

	private sealed class UnavailableTextureExportCollection : ExportCollection
	{
		private readonly ITexture2D asset;

		public UnavailableTextureExportCollection(IAssetExporter exporter, ITexture2D asset)
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
			string directory = fileSystem.Path.Join(projectDirectory, "AssetRipper", "UnavailableTextures");
			fileSystem.Directory.Create(directory);
			string assetName = SafePathSegment(((IUnityObjectBase)asset).GetBestName());
			string fileName = GetUniqueFileName(directory, $"{assetName}_{asset.PathID}.unavailable-texture.txt", fileSystem);
			return AssetExporter.Export(container, asset, fileSystem.Path.Join(directory, fileName), fileSystem);
		}

		public override MetaPtr CreateExportPointer(IExportContainer container, IUnityObjectBase asset, bool isLocal) => MetaPtr.NullPtr;
		public override long GetExportID(IExportContainer container, IUnityObjectBase asset) => throw new NotSupportedException();
		public override bool Contains(IUnityObjectBase other) => other.AssetInfo == asset.AssetInfo;

		private static string SafePathSegment(string value)
		{
			string result = FileSystem.FixInvalidPathCharacters(value);
			return string.IsNullOrWhiteSpace(result) ? "UnavailableTexture" : result.Length > 96 ? result[..96] : result;
		}
	}
}
