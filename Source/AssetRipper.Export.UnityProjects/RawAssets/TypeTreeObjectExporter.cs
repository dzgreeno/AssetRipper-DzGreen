using AssetRipper.Assets.Collections;
using AssetRipper.Assets.Metadata;
using AssetRipper.Assets;
using AssetRipper.Import.AssetCreation;
using AssetRipper.Import.Structure.Assembly.Serializable;
using AssetRipper.IO.Files.SerializedFiles;
using AssetRipper.SourceGenerated;
using AssetRipper.SourceGenerated.Classes.ClassID_28;
using AssetRipper.SourceGenerated.Extensions;
using AssetRipper.SourceGenerated.Enums;
using AssetRipper.Export.UnityProjects.Textures;
using AssetRipper.Export.UnityProjects.Project;

namespace AssetRipper.Export.UnityProjects.RawAssets;

/// <summary>
/// Exports assets recovered from an embedded Type Tree as a concise inspection record.
/// Their schema could not be validated by a generated reader, so emitting Unity YAML would
/// be misleading and can recurse indefinitely on malformed or self-referential structures.
/// </summary>
internal sealed class TypeTreeObjectExporter : BinaryAssetExporter
{
	private readonly TextureAssetExporter textureExporter;

	public TypeTreeObjectExporter(TextureAssetExporter textureExporter)
	{
		this.textureExporter = textureExporter;
	}

	public override bool TryCreateCollection(IUnityObjectBase asset, [NotNullWhen(true)] out IExportCollection? exportCollection)
	{
		if (asset is TypeTreeObject textureSource && TryCreateTexture(textureSource, out ITexture2D? texture))
		{
			exportCollection = new TextureExportCollection(textureExporter, texture, textureSource);
			return true;
		}
		else if (asset is TypeTreeObject { ClassID: (int)ClassIDType.Mesh } mesh)
		{
			// The embedded Type Tree is authoritative for serialized Mesh fields. A Mesh has no
			// managed object graph, so it can be emitted as a regular Unity YAML asset while
			// other recovered types remain quarantine records until their layouts are verified.
			exportCollection = new AssetExportCollection<IUnityObjectBase>(new DefaultYamlExporter(), mesh);
			return true;
		}
		else if (asset is TypeTreeObject { IsPlayerSettings: false } inspectionAsset)
		{
			exportCollection = new TypeTreeExportCollection(this, inspectionAsset);
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

	private static bool TryCreateTexture(TypeTreeObject source, [NotNullWhen(true)] out ITexture2D? texture)
	{
		texture = null;
		if (source.ClassID != (int)ClassIDType.Texture2D)
		{
			return false;
		}

		SerializableStructure fields = source.ReleaseFields;
		if (!fields.TryGetField("m_ImageData", out SerializableValue imageData) && !fields.TryGetField("image data", out imageData))
		{
			return false;
		}
		byte[] embeddedData = imageData.AsByteArray;
		bool hasStreamData = TryReadStreamData(fields, out Utf8String streamPath, out ulong streamOffset, out uint streamSize);
		byte[] textureData = embeddedData;
		if (textureData.Length == 0 && hasStreamData)
		{
			textureData = TryReadStreamContent(source, streamPath, streamOffset, streamSize);
		}
		if (textureData.Length == 0)
		{
			return false;
		}

		UnityVersion releaseVersion = new UnityVersion(source.Collection.Version.Major, source.Collection.Version.Minor, source.Collection.Version.Build, UnityVersionType.Final, 1);
		if (AssetFactory.CreateSerialized(source.AssetInfo, releaseVersion) is not ITexture2D reconstructedTexture)
		{
			return false;
		}

		if (!TryGetInt32(fields, "m_Width", out int width) || !TryGetInt32(fields, "m_Height", out int height) || width <= 0 || height <= 0 || !TryGetInt32(fields, "m_TextureFormat", out int textureFormat))
		{
			return false;
		}

		reconstructedTexture.Name = TryGetString(fields, "m_Name", out string name) ? name : ((IUnityObjectBase)source).GetBestName();
		reconstructedTexture.Width_C28 = width;
		reconstructedTexture.Height_C28 = height;
		reconstructedTexture.Format_C28E = (TextureFormat)textureFormat;
		reconstructedTexture.ImageData_C28 = textureData;
		if (TryGetInt32(fields, "m_CompleteImageSize", out int completeImageSize) && completeImageSize > 0)
		{
			reconstructedTexture.CompleteImageSize_C28_UInt32 = (uint)completeImageSize;
		}
		if (TryGetInt32(fields, "m_MipCount", out int mipCount))
		{
			reconstructedTexture.MipCount_C28 = mipCount;
		}
		if (TryGetInt32(fields, "m_ImageCount", out int imageCount))
		{
			reconstructedTexture.ImageCount_C28 = imageCount;
		}
		texture = reconstructedTexture;
		return true;
	}

	private static byte[] TryReadStreamContent(TypeTreeObject source, Utf8String path, ulong offset, uint size)
	{
		if (offset > long.MaxValue || size == 0 || offset + size > long.MaxValue)
		{
			return [];
		}

		var resourceFile = source.Collection.Bundle.ResolveResource(path.String);
		if (resourceFile is null || resourceFile.Stream.Length < unchecked((long)(offset + size)))
		{
			return [];
		}

		byte[] content = new byte[size];
		resourceFile.Stream.Position = (long)offset;
		resourceFile.Stream.ReadExactly(content);
		return content;
	}

	private static bool TryReadStreamData(SerializableStructure fields, out Utf8String path, out ulong offset, out uint size)
	{
		path = Utf8String.Empty;
		offset = 0;
		size = 0;
		if (!fields.TryGetField("m_StreamData", out SerializableValue streamData))
		{
			return false;
		}

		SerializableStructure stream = streamData.AsStructure;
		if (!TryGetString(stream, "path", out string streamPath) || string.IsNullOrWhiteSpace(streamPath) || streamPath.Any(char.IsControl) || !TryGetUInt64(stream, "offset", out offset) || !TryGetUInt32(stream, "size", out size) || size == 0)
		{
			return false;
		}

		path = streamPath;
		return true;
	}

	private static bool TryGetString(SerializableStructure fields, string name, out string value)
	{
		if (fields.TryGetField(name, out SerializableValue field))
		{
			value = field.AsString;
			return true;
		}
		value = string.Empty;
		return false;
	}

	private static bool TryGetInt32(SerializableStructure fields, string name, out int value)
	{
		if (fields.TryGetField(name, out SerializableValue field))
		{
			value = field.AsInt32;
			return true;
		}
		value = 0;
		return false;
	}

	private static bool TryGetUInt64(SerializableStructure fields, string name, out ulong value)
	{
		if (fields.TryGetField(name, out SerializableValue field))
		{
			value = field.AsUInt64;
			return true;
		}
		value = 0;
		return false;
	}

	private static bool TryGetUInt32(SerializableStructure fields, string name, out uint value)
	{
		if (fields.TryGetField(name, out SerializableValue field))
		{
			value = field.AsUInt32;
			return true;
		}
		value = 0;
		return false;
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
