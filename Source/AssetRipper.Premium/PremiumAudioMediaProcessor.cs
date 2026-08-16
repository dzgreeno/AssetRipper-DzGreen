using AssetRipper.Assets;
using AssetRipper.Assets.Bundles;
using AssetRipper.Export.Modules.Audio;
using AssetRipper.SourceGenerated.Classes.ClassID_329;
using AssetRipper.SourceGenerated.Classes.ClassID_83;
using AssetRipper.SourceGenerated.Extensions;

namespace AssetRipper.Premium;

/// <summary>
/// Inventories and exports media solely through the regular importer and existing standard-format
/// decoders. It never decrypts a resource stream, reconstructs a proprietary container, or writes
/// bytes when the type or integrity cannot be verified.
/// </summary>
public static class PremiumAudioMediaProcessor
{
	private const int MaximumProbeBytes = 256 * 1024 * 1024;

	public static PremiumMediaReport CreateDiagnostics(GameBundle gameBundle)
	{
		ArgumentNullException.ThrowIfNull(gameBundle);
		PremiumAudioMediaSummary[] audio = gameBundle.FetchAssets()
			.OfType<IAudioClip>()
			.OrderBy(static clip => GetNodeId(clip), StringComparer.Ordinal)
			.Select(ProbeAudio)
			.ToArray();
		PremiumVideoMediaSummary[] video = gameBundle.FetchAssets()
			.OfType<IVideoClip>()
			.OrderBy(static clip => GetNodeId(clip), StringComparer.Ordinal)
			.Select(ProbeVideo)
			.ToArray();
		return new PremiumMediaReport(
			audio.LongLength,
			audio.LongCount(static item => item.Status == PremiumMediaStatus.Ready),
			audio.LongCount(static item => item.Status == PremiumMediaStatus.Unavailable),
			audio.LongCount(static item => item.Status == PremiumMediaStatus.Unsupported),
			video.LongLength,
			video.LongCount(static item => item.Status == PremiumMediaStatus.Ready),
			video.LongCount(static item => item.Status == PremiumMediaStatus.Unavailable),
			audio,
			video);
	}

	public static PremiumMediaExportResult TryExportAudio(IAudioClip clip, string outputDirectory)
	{
		ArgumentNullException.ThrowIfNull(clip);
		string directory = PrepareDirectory(outputDirectory);
		if (!AudioClipDecoder.TryDecode(clip, out byte[]? content, out string? extension, out string? message))
		{
			return new PremiumMediaExportResult(false, null, message ?? "Audio data could not be decoded by the supported media pipeline.");
		}
		string path = Path.Combine(directory, MakeFileName(clip.GetBestName(), clip.PathID, extension));
		File.WriteAllBytes(path, content);
		return new PremiumMediaExportResult(true, path, null);
	}

	public static PremiumMediaExportResult TryExportVideo(IVideoClip clip, string outputDirectory)
	{
		ArgumentNullException.ThrowIfNull(clip);
		string directory = PrepareDirectory(outputDirectory);
		if (!clip.CheckIntegrity() || !clip.TryGetExtensionFromPath(out string? extension) || !clip.TryGetContent(out byte[]? content))
		{
			return new PremiumMediaExportResult(false, null, "Video data is unavailable, fails integrity validation, or exposes no supported source extension.");
		}
		string path = Path.Combine(directory, MakeFileName(clip.GetBestName(), clip.PathID, extension));
		File.WriteAllBytes(path, content);
		return new PremiumMediaExportResult(true, path, null);
	}

	private static PremiumAudioMediaSummary ProbeAudio(IAudioClip clip)
	{
		byte[] rawData;
		try
		{
			rawData = clip.GetAudioData();
		}
		catch (Exception exception)
		{
			return new PremiumAudioMediaSummary(GetNodeId(clip), null, 0, PremiumMediaStatus.Unavailable, exception.Message);
		}
		if (rawData.Length == 0)
		{
			return new PremiumAudioMediaSummary(GetNodeId(clip), null, 0, PremiumMediaStatus.Unavailable, "No readable audio bytes were supplied by the normal importer.");
		}
		if (rawData.Length > MaximumProbeBytes)
		{
			return new PremiumAudioMediaSummary(GetNodeId(clip), null, rawData.LongLength, PremiumMediaStatus.Unsupported, "Audio stream exceeds the bounded diagnostic probe limit.");
		}
		if (AudioClipDecoder.TryDecode(clip, out _, out string? extension, out string? message))
		{
			return new PremiumAudioMediaSummary(GetNodeId(clip), extension, rawData.LongLength, PremiumMediaStatus.Ready, null);
		}
		return new PremiumAudioMediaSummary(GetNodeId(clip), null, rawData.LongLength, PremiumMediaStatus.Unsupported, message);
	}

	private static PremiumVideoMediaSummary ProbeVideo(IVideoClip clip)
	{
		bool isIntegrityValid;
		try
		{
			isIntegrityValid = clip.CheckIntegrity();
		}
		catch (Exception exception)
		{
			return new PremiumVideoMediaSummary(GetNodeId(clip), null, PremiumMediaStatus.Unavailable, exception.Message);
		}
		if (!isIntegrityValid)
		{
			return new PremiumVideoMediaSummary(GetNodeId(clip), null, PremiumMediaStatus.Unavailable, "Video integrity validation failed in the normal importer.");
		}
		if (!clip.TryGetExtensionFromPath(out string? extension))
		{
			return new PremiumVideoMediaSummary(GetNodeId(clip), null, PremiumMediaStatus.Unsupported, "Video container extension is unavailable.");
		}
		return new PremiumVideoMediaSummary(GetNodeId(clip), extension, PremiumMediaStatus.Ready, null);
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

	private static string MakeFileName(string? name, long pathId, string extension)
	{
		string value = string.IsNullOrWhiteSpace(name) ? "media" : name;
		foreach (char invalid in Path.GetInvalidFileNameChars())
		{
			value = value.Replace(invalid, '_');
		}
		return $"{value}_{pathId}.{extension.TrimStart('.')}";
	}

	private static string GetNodeId(IUnityObjectBase asset)
	{
		string collectionPath = string.IsNullOrWhiteSpace(asset.Collection.FilePath) ? asset.Collection.Name : asset.Collection.FilePath;
		return $"{collectionPath}:{asset.PathID}";
	}
}

public enum PremiumMediaStatus
{
	Ready,
	Unavailable,
	Unsupported,
}

public sealed record PremiumAudioMediaSummary(string Id, string? Extension, long ByteLength, PremiumMediaStatus Status, string? Message);
public sealed record PremiumVideoMediaSummary(string Id, string? Extension, PremiumMediaStatus Status, string? Message);
public sealed record PremiumMediaReport(long AudioClipCount, long ReadyAudioClipCount, long UnavailableAudioClipCount, long UnsupportedAudioClipCount, long VideoClipCount, long ReadyVideoClipCount, long UnavailableVideoClipCount, IReadOnlyList<PremiumAudioMediaSummary> Audio, IReadOnlyList<PremiumVideoMediaSummary> Video);
public sealed record PremiumMediaExportResult(bool IsSuccess, string? Path, string? Message);
