using AssetRipper.Assets;
using AssetRipper.Export.Configuration;
using AssetRipper.Export.Modules.Textures;
using AssetRipper.Import.Logging;
using AssetRipper.Processing.Textures;
using AssetRipper.SourceGenerated.Classes.ClassID_213;
using AssetRipper.SourceGenerated.Classes.ClassID_28;
using AssetRipper.SourceGenerated.Extensions;

namespace AssetRipper.Export.UnityProjects.Textures;

public class TextureAssetExporter : BinaryAssetExporter
{
	public ImageExportFormat ImageExportFormat { get; }
	private SpriteExportMode SpriteExportMode { get; }
	public bool PreferOriginalTextureExtension { get; }
	private bool ExportSprites => SpriteExportMode is not SpriteExportMode.Yaml;

	public TextureAssetExporter(FullConfiguration configuration)
	{
		ImageExportFormat = configuration.ExportSettings.ImageExportFormat;
		SpriteExportMode = configuration.ExportSettings.SpriteExportMode;
		PreferOriginalTextureExtension = configuration.ExportSettings.PreferOriginalTextureExtension;
	}

	public override bool TryCreateCollection(IUnityObjectBase asset, [NotNullWhen(true)] out IExportCollection? exportCollection)
	{
		if (asset.MainAsset is SpriteInformationObject spriteInformationObject && (ExportSprites || asset is not ISprite))
		{
			exportCollection = new TextureExportCollection(this, spriteInformationObject, ExportSprites);
			return true;
		}
		else
		{
			exportCollection = null;
			return false;
		}
	}

	public override bool Export(IExportContainer container, IUnityObjectBase asset, string path, FileSystem fileSystem)
	{
		ITexture2D texture = (ITexture2D)asset;
		if (!texture.CheckAssetIntegrity())
		{
			string reportPath = path + ".unavailable-texture.txt";
			string streamPath = texture.StreamData_C28?.Path.String ?? "<none>";
			string content = $"""
			AssetRipper DzGreen unavailable texture inspection record

			Name: {((IUnityObjectBase)texture).GetBestName()}
			Class: {texture.ClassName}
			PathID: {texture.PathID}
			Collection: {texture.Collection.Name}
			Streamed resource path length: {streamPath.Length}

			The texture payload could not be resolved or validated. No image was emitted because that would produce an invalid or misleading file. Inspect the original bundle and full diagnostics log for this asset.
			""";
			fileSystem.File.WriteAllText(reportPath, content);
			Logger.Log(LogType.Warning, LogCategory.Export, $"Texture PathID {texture.PathID} in '{texture.Collection.Name}' was not converted; saved '{fileSystem.Path.GetFileName(reportPath)}'.");
			return false;
		}

		if (TextureConverter.TryConvertToBitmap(texture, out DirectBitmap bitmap))
		{
			using Stream stream = fileSystem.File.Create(path);
			bitmap.Save(stream, texture.GetTextureExportFormat(PreferOriginalTextureExtension, ImageExportFormat));
			return true;
		}
		else
		{
			Logger.Log(LogType.Warning, LogCategory.Export, $"Unable to convert '{texture.Name}' to bitmap");
			return false;
		}
	}
}
