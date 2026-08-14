using AssetRipper.IO.Files.Streams.Smart;

namespace AssetRipper.IO.Files.BundleFiles;

/// <summary>
/// Public entry point for bounded Unity bundle preprocessing.
/// This class only recovers a plainly visible Unity signature; it does not decrypt,
/// defeat DRM, or bypass anti-tamper mechanisms.
/// </summary>
public static class FilePreProcessor
{
	public static SmartStream NormalizeUnityBundle(SmartStream stream, string fileName)
	{
		return BundleHeaderNormalizer.Normalize(stream, fileName);
	}

	public static void ReportAutoFix(string message)
	{
		BundleHeaderNormalizer.ReportAutoFix(message);
	}
}
