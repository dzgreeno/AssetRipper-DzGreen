using AssetRipper.IO.Files.Streams.Smart;
using System.Diagnostics;
using System.Text;

namespace AssetRipper.IO.Files.BundleFiles;

/// <summary>
/// Detects and removes bounded, non-encrypted prefixes placed before a Unity bundle signature.
/// The original stream is not mutated when no repair is required. This does not decrypt or bypass protected content.
/// </summary>
public static class BundleHeaderNormalizer
{
	private const int MaxScanLength = 256;
	private static readonly string[] BundleSignatures =
	[
		"UnityFS",
		"UnityWeb",
		"UnityRaw",
		"RawWeb",
		"UnityArchive",
	];

	/// <summary>
	/// Raised when a prefix is removed. GUI layers can bridge this event to their logger
	/// without adding a dependency from the IO assembly to the Import assembly.
	/// </summary>
	public static event Action<string>? AutoFixMessage;

	public static SmartStream Normalize(SmartStream stream, string fileName)
	{
		ArgumentNullException.ThrowIfNull(stream);
		if (!stream.CanSeek || stream.Length == 0)
		{
			return stream;
		}

		long originalPosition = stream.Position;
		try
		{
			stream.Position = 0;
			int scanLength = (int)Math.Min(stream.Length, MaxScanLength);
			byte[] buffer = new byte[scanLength];
			int bytesRead = 0;
			while (bytesRead < buffer.Length)
			{
				int read = stream.Read(buffer, bytesRead, buffer.Length - bytesRead);
				if (read == 0)
				{
					break;
				}
				bytesRead += read;
			}

			int offset = FindSignature(buffer, bytesRead, out string? signature);
			if (offset <= 0)
			{
				stream.Position = originalPosition;
				return stream;
			}

			SmartStream normalized = stream.CreatePartial(offset, stream.Length - offset);
			stream.Dispose();

			ReportAutoFix($"[Auto-Fix] Removed {offset} junk header bytes from: {Path.GetFileName(fileName)} (Valid {signature} signature recovered)");
			return normalized;
		}
		catch
		{
			if (!stream.IsNull)
			{
				stream.Position = originalPosition;
			}
			return stream;
		}
	}

	public static void ReportAutoFix(string message)
	{
		ArgumentNullException.ThrowIfNull(message);
		Trace.WriteLine(message);
		AutoFixMessage?.Invoke(message);
	}

	private static int FindSignature(byte[] buffer, int length, out string? signature)
	{
		signature = null;
		foreach (string candidate in BundleSignatures)
		{
			byte[] candidateBytes = Encoding.ASCII.GetBytes(candidate);
			int maxOffset = length - candidateBytes.Length;
			for (int offset = 0; offset <= maxOffset; offset++)
			{
				bool matches = true;
				for (int i = 0; i < candidateBytes.Length; i++)
				{
					if (buffer[offset + i] != candidateBytes[i])
					{
						matches = false;
						break;
					}
				}

				bool hasNullTerminator = offset + candidateBytes.Length < length
					&& buffer[offset + candidateBytes.Length] == 0;
				if (matches && hasNullTerminator)
				{
					signature = candidate;
					return offset;
				}
			}
		}
		return -1;
	}
}
