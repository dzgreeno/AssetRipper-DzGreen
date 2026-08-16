using AssetRipper.Assets;
using AssetRipper.Assets.Bundles;
using AssetRipper.Export.Modules.Textures;
using AssetRipper.SourceGenerated.Classes.ClassID_189;
using AssetRipper.SourceGenerated.Extensions;

namespace AssetRipper.Premium;

/// <summary>
/// Exports only image textures which the existing importer can validate and decode. Compression
/// support is intentionally delegated to TextureConverter, which contains the established ASTC,
/// ETC/EAC, PVRTC, Crunch, BC, and DXT decoders. No opaque image stream is guessed or rewritten.
/// </summary>
public static class PremiumTextureTranscoder
{
	public static PremiumTextureTranscodeReport CreateDiagnostics(GameBundle gameBundle)
	{
		ArgumentNullException.ThrowIfNull(gameBundle);
		PremiumTextureTranscodeSummary[] textures = gameBundle.FetchAssets()
			.OfType<IImageTexture>()
			.OrderBy(static texture => GetId(texture), StringComparer.Ordinal)
			.Select(Probe)
			.ToArray();
		return new PremiumTextureTranscodeReport(
			textures.LongLength,
			textures.LongCount(static item => item.Status == PremiumTextureTranscodeStatus.Ready),
			textures.LongCount(static item => item.Status == PremiumTextureTranscodeStatus.Unavailable),
			textures.LongCount(static item => item.Status == PremiumTextureTranscodeStatus.Unsupported),
			textures);
	}

	public static PremiumTextureExportResult TryExport(IImageTexture texture, string outputDirectory, PremiumTextureOutputFormat outputFormat)
	{
		ArgumentNullException.ThrowIfNull(texture);
		if (!texture.CheckAssetIntegrity())
		{
			return new PremiumTextureExportResult(false, null, "Texture data failed normal importer integrity validation.");
		}
		if (!TextureConverter.TryConvertToBitmap(texture, out DirectBitmap bitmap) || bitmap.IsEmpty)
		{
			return new PremiumTextureExportResult(false, null, "The existing decoder does not support this readable texture stream.");
		}
		string directory = PrepareDirectory(outputDirectory);
		string path = Path.Combine(directory, MakeFileName(texture.GetBestName(), texture.PathID, outputFormat));
		using FileStream stream = File.Create(path);
		switch (outputFormat)
		{
			case PremiumTextureOutputFormat.Png:
				bitmap.SaveAsPng(stream);
				break;
			case PremiumTextureOutputFormat.Tga:
				bitmap.SaveAsTga(stream);
				break;
			case PremiumTextureOutputFormat.Exr:
				bitmap.SaveAsExr(stream);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(outputFormat));
		}
		return new PremiumTextureExportResult(true, path, null);
	}

	/// <summary>
	/// Saves only caller-supplied mip levels that an importer or embedded schema has already exposed.
	/// This method neither synthesizes a missing mip level nor infers one from dimensions.
	/// </summary>
	public static PremiumTextureMipExportResult TryExportExposedMipChain(IReadOnlyList<DirectBitmap>? exposedMips, string outputDirectory, string stem, PremiumTextureOutputFormat outputFormat)
	{
		if (exposedMips is null || exposedMips.Count == 0 || exposedMips.Any(static mip => mip is null || mip.IsEmpty))
		{
			return new PremiumTextureMipExportResult(PremiumTextureMipStatus.NotExposed, [], "No complete readable mip chain was exposed by the importer or schema.");
		}
		string directory = PrepareDirectory(outputDirectory);
		string safeStem = string.IsNullOrWhiteSpace(stem) ? "texture" : MakeFileName(stem, 0, outputFormat)[..^($"_0.{outputFormat.ToString().ToLowerInvariant()}".Length)];
		List<string> paths = new(exposedMips.Count);
		for (int index = 0; index < exposedMips.Count; index++)
		{
			string path = Path.Combine(directory, $"{safeStem}_mip{index:D2}.{outputFormat.ToString().ToLowerInvariant()}");
			using FileStream stream = File.Create(path);
			Save(exposedMips[index], stream, outputFormat);
			paths.Add(path);
		}
		return new PremiumTextureMipExportResult(PremiumTextureMipStatus.Exposed, paths, null);
	}

	/// <summary>
	/// Converts schema values already obtained by a reader into report states. Passing null means the
	/// source did not expose that datum; no color-space or mip value is guessed from file names or pixels.
	/// </summary>
	public static PremiumTextureSchemaMetadata FromExposedSchema(int? mipCount, bool? isSrgb)
	{
		PremiumTextureMipStatus mipStatus = mipCount is > 0 ? PremiumTextureMipStatus.Exposed : PremiumTextureMipStatus.NotExposed;
		PremiumTextureColorSpace colorSpace = isSrgb switch
		{
			true => PremiumTextureColorSpace.Srgb,
			false => PremiumTextureColorSpace.Linear,
			null => PremiumTextureColorSpace.Unknown,
		};
		return new PremiumTextureSchemaMetadata(mipStatus, mipCount is > 0 ? mipCount.Value : null, colorSpace);
	}

	private static PremiumTextureTranscodeSummary Probe(IImageTexture texture)
	{
		if (!texture.CheckAssetIntegrity())
		{
			return new PremiumTextureTranscodeSummary(GetId(texture), texture.GetBestName(), texture.GetType().Name, PremiumTextureColorSpace.Unknown, PremiumTextureMipStatus.NotExposed, PremiumTextureTranscodeStatus.Unavailable, "The texture stream is unavailable or incomplete.");
		}
		try
		{
			if (TextureConverter.TryConvertToBitmap(texture, out DirectBitmap bitmap) && !bitmap.IsEmpty)
			{
				return new PremiumTextureTranscodeSummary(GetId(texture), texture.GetBestName(), texture.GetType().Name, PremiumTextureColorSpace.Unknown, PremiumTextureMipStatus.NotExposed, PremiumTextureTranscodeStatus.Ready, null);
			}
		}
		catch (Exception exception)
		{
			return new PremiumTextureTranscodeSummary(GetId(texture), texture.GetBestName(), texture.GetType().Name, PremiumTextureColorSpace.Unknown, PremiumTextureMipStatus.NotExposed, PremiumTextureTranscodeStatus.Unsupported, exception.Message);
		}
		return new PremiumTextureTranscodeSummary(GetId(texture), texture.GetBestName(), texture.GetType().Name, PremiumTextureColorSpace.Unknown, PremiumTextureMipStatus.NotExposed, PremiumTextureTranscodeStatus.Unsupported, "No established decoder accepted this texture format.");
	}

	private static void Save(DirectBitmap bitmap, Stream stream, PremiumTextureOutputFormat outputFormat)
	{
		switch (outputFormat)
		{
			case PremiumTextureOutputFormat.Png:
				bitmap.SaveAsPng(stream);
				break;
			case PremiumTextureOutputFormat.Tga:
				bitmap.SaveAsTga(stream);
				break;
			case PremiumTextureOutputFormat.Exr:
				bitmap.SaveAsExr(stream);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(outputFormat));
		}
	}

	private static string PrepareDirectory(string outputDirectory)
	{
		if (string.IsNullOrWhiteSpace(outputDirectory))
		{
			throw new ArgumentException("An output directory is required.", nameof(outputDirectory));
		}
		string fullPath = Path.GetFullPath(outputDirectory);
		Directory.CreateDirectory(fullPath);
		return fullPath;
	}

	private static string MakeFileName(string? name, long pathId, PremiumTextureOutputFormat format)
	{
		string safeName = string.IsNullOrWhiteSpace(name) ? "texture" : name;
		foreach (char invalid in Path.GetInvalidFileNameChars())
		{
			safeName = safeName.Replace(invalid, '_');
		}
		return $"{safeName}_{pathId}.{format.ToString().ToLowerInvariant()}";
	}

	private static string GetId(IUnityObjectBase texture)
	{
		string collection = string.IsNullOrWhiteSpace(texture.Collection.FilePath) ? texture.Collection.Name : texture.Collection.FilePath;
		return $"{collection}:{texture.PathID}";
	}
}

public enum PremiumTextureOutputFormat { Png, Tga, Exr }
public enum PremiumTextureTranscodeStatus { Ready, Unavailable, Unsupported }
public enum PremiumTextureColorSpace { Unknown, Srgb, Linear }
public enum PremiumTextureMipStatus { NotExposed, Exposed }
public sealed record PremiumTextureSchemaMetadata(PremiumTextureMipStatus MipStatus, int? ExposedMipCount, PremiumTextureColorSpace ColorSpace);
public sealed record PremiumTextureTranscodeSummary(string Id, string Name, string TextureKind, PremiumTextureColorSpace ColorSpace, PremiumTextureMipStatus MipStatus, PremiumTextureTranscodeStatus Status, string? Message);
public sealed record PremiumTextureTranscodeReport(long TextureCount, long ReadyTextureCount, long UnavailableTextureCount, long UnsupportedTextureCount, IReadOnlyList<PremiumTextureTranscodeSummary> Textures);
public sealed record PremiumTextureExportResult(bool IsSuccess, string? Path, string? Message);
public sealed record PremiumTextureMipExportResult(PremiumTextureMipStatus Status, IReadOnlyList<string> Paths, string? Message);
