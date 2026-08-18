using AssetRipper.Assets.Bundles;
using AssetRipper.Assets.IO;
using AssetRipper.Assets.Metadata;
using AssetRipper.IO.Files.SerializedFiles;
using AssetRipper.IO.Files.SerializedFiles.Parser;

namespace AssetRipper.Assets.Collections;

/// <summary>
/// A collection of assets read from a <see cref="SerializedFile"/>.
/// </summary>
public sealed class SerializedAssetCollection : AssetCollection
{
	private FileIdentifier[]? DependencyIdentifiers { get; set; }
	private string[] DependencyNames { get; set; } = [];
	/// <summary>Whether the source serialized file declared an embedded TypeTree.</summary>
	public bool HasEmbeddedTypeTree { get; private set; }
	/// <summary>The number of serialized types declared by the source file.</summary>
	public int SerializedTypeCount { get; private set; }
	/// <summary>The number of serialized types explicitly marked as stripped.</summary>
	public int StrippedSerializedTypeCount { get; private set; }
	/// <summary>The number of assembly-qualified reference types declared by the source file.</summary>
	public int ReferenceSerializedTypeCount { get; private set; }

	private SerializedAssetCollection(Bundle bundle) : base(bundle)
	{
	}

	internal void InitializeDependencyList(IDependencyProvider? dependencyProvider)
	{
		if (Dependencies.Count > 1)
		{
			throw new Exception("Dependency list has already been initialized.");
		}
		if (DependencyIdentifiers is not null)
		{
			for (int i = 0; i < DependencyIdentifiers.Length; i++)
			{
				FileIdentifier identifier = DependencyIdentifiers[i];
				AssetCollection? dependency = Bundle.ResolveCollection(identifier);
				if (dependency is null)
				{
					dependencyProvider?.ReportMissingDependency(identifier);
				}
				SetDependency(i + 1, dependency);
			}
			DependencyIdentifiers = null;
		}
	}

	/// <summary>Gets the source-declared external filename for a PPtr file ID, when available.</summary>
	public string? GetDependencyName(int fileID) => fileID is > 0 && fileID <= DependencyNames.Length ? DependencyNames[fileID - 1] : null;

	/// <summary>
	/// Creates a <see cref="SerializedAssetCollection"/> from a <see cref="SerializedFile"/>.
	/// </summary>
	/// <remarks>
	/// The new <see cref="SerializedAssetCollection"/> is automatically added to the <paramref name="bundle"/>.
	/// </remarks>
	/// <param name="bundle">The <see cref="Bundle"/> to add this collection to.</param>
	/// <param name="file">The <see cref="SerializedFile"/> from which to make this collection.</param>
	/// <param name="factory">A factory for creating assets.</param>
	/// <param name="defaultVersion">The default version to use if the file does not have a version, ie the version has been stripped.</param>
	/// <returns>The new collection.</returns>
	internal static SerializedAssetCollection FromSerializedFile(Bundle bundle, SerializedFile file, AssetFactoryBase factory, UnityVersion defaultVersion = default)
	{
		UnityVersion version = file.Version.Equals(0, 0, 0) ? defaultVersion : file.Version;
		SerializedAssetCollection collection = new SerializedAssetCollection(bundle)
		{
			Name = file.NameFixed,
			Version = version,
			OriginalVersion = version,
			Platform = file.Platform,
			Flags = file.Flags,
			EndianType = file.EndianType,
		};
		SerializedType[] serializedTypes = file.Types.ToArray();
		collection.HasEmbeddedTypeTree = file.HasTypeTree;
		collection.SerializedTypeCount = serializedTypes.Length;
		collection.StrippedSerializedTypeCount = serializedTypes.Count(static type => type.IsStrippedType);
		collection.ReferenceSerializedTypeCount = file.RefTypes.ToArray().Length;
		ReadOnlySpan<FileIdentifier> fileDependencies = file.Dependencies;
		if (fileDependencies.Length > 0)
		{
			collection.DependencyIdentifiers = fileDependencies.ToArray();
			collection.DependencyNames = fileDependencies.ToArray().Select(static identifier => identifier.GetFilePath()).ToArray();
		}
		ReadData(collection, file, factory);
		return collection;
	}

	private static void ReadData(SerializedAssetCollection collection, SerializedFile file, AssetFactoryBase factory)
	{
		foreach (ObjectInfo objectInfo in file.Objects)
		{
			int classID = objectInfo.TypeID < 0 ? 114 : objectInfo.TypeID;
			AssetInfo assetInfo = new AssetInfo(collection, objectInfo.FileID, classID);
			IUnityObjectBase? asset = factory.ReadAsset(assetInfo, objectInfo.ObjectData, objectInfo.Type);
			if (asset is not null)
			{
				collection.AddAsset(asset);
			}
		}
	}
}
