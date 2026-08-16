using SharpGLTF.Memory;

namespace AssetRipper.Export.Modules.Models;

/// <summary>
/// Holds caller-supplied fallback image content for GLB export. Entries are keyed by serialized
/// material property name without a leading underscore and are consulted only for unresolved
/// non-texture bindings. Resolved source textures and explicit null bindings never query it.
/// </summary>
public sealed class GlbFallbackTextureCatalog
{
	private const long MaximumFallbackBytes = 64L * 1024 * 1024;
	private readonly IReadOnlyDictionary<string, MemoryImage> images;

	private GlbFallbackTextureCatalog(IReadOnlyDictionary<string, MemoryImage> images)
	{
		this.images = images;
	}

	public static GlbFallbackTextureCatalog Empty { get; } = new(new Dictionary<string, MemoryImage>(StringComparer.OrdinalIgnoreCase));

	public static GlbFallbackTextureCatalog Create(IEnumerable<GlbFallbackTextureSource> sources, out IReadOnlyList<GlbFallbackTextureRejection> rejections)
	{
		ArgumentNullException.ThrowIfNull(sources);
		Dictionary<string, MemoryImage> images = new(StringComparer.OrdinalIgnoreCase);
		List<GlbFallbackTextureRejection> failures = [];
		foreach (GlbFallbackTextureSource source in sources
			.OrderBy(static item => NormalizeKey(item.Key), StringComparer.OrdinalIgnoreCase)
			.ThenBy(static item => item.Path, StringComparer.OrdinalIgnoreCase))
		{
			string key = NormalizeKey(source.Key);
			if (key.Length == 0)
			{
				failures.Add(new(source.Key, source.Path, "A fallback texture key is required."));
				continue;
			}
			if (!File.Exists(source.Path))
			{
				failures.Add(new(key, source.Path, "The fallback texture file does not exist."));
				continue;
			}
			FileInfo info = new(source.Path);
			if (info.Length <= 0 || info.Length > MaximumFallbackBytes)
			{
				failures.Add(new(key, source.Path, "The fallback texture size is outside the accepted bounded range."));
				continue;
			}
			try
			{
				MemoryImage image = new(File.ReadAllBytes(source.Path));
				if (!image.IsValid)
				{
					failures.Add(new(key, source.Path, "The fallback file is not a supported image container."));
					continue;
				}
				if (!images.TryAdd(key, image))
				{
					failures.Add(new(key, source.Path, "A previous canonical fallback entry already owns this key."));
				}
			}
			catch (IOException exception)
			{
				failures.Add(new(key, source.Path, exception.Message));
			}
			catch (UnauthorizedAccessException exception)
			{
				failures.Add(new(key, source.Path, exception.Message));
			}
		}
		rejections = failures
			.OrderBy(static item => item.Key, StringComparer.OrdinalIgnoreCase)
			.ThenBy(static item => item.Path, StringComparer.OrdinalIgnoreCase)
			.ToArray();
		return new GlbFallbackTextureCatalog(images);
	}

	public bool TryGetUnresolvedImage(string propertyName, out MemoryImage image)
	{
		return images.TryGetValue(NormalizeKey(propertyName), out image!);
	}

	private static string NormalizeKey(string? key) => key?.Trim().TrimStart('_') ?? string.Empty;
}

public sealed record GlbFallbackTextureSource(string Key, string Path);
public sealed record GlbFallbackTextureRejection(string Key, string Path, string Reason);
