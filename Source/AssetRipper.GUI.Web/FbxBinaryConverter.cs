using System.Diagnostics;
using System.Text;

namespace AssetRipper.GUI.Web;

/// <summary>
/// Converts an already validated GLB export into a binary FBX using the bundled
/// Assimp command-line tool on Windows. GLB remains in the export bundle as the
/// direct Blender-ready source when a platform cannot launch the converter.
/// </summary>
internal static class FbxBinaryConverter
{
	private const int TimeoutMilliseconds = 10 * 60 * 1000;
	private const string BinaryFbxSignature = "Kaydara FBX Binary";

	public static bool TryConvertGlbToBinaryFbx(string inputGlbPath, string outputFbxPath, [NotNullWhen(false)] out string? errorMessage)
	{
		errorMessage = null;
		if (!File.Exists(inputGlbPath))
		{
			errorMessage = "The intermediate GLB file was not created.";
			return false;
		}

		string? converterPath = ResolveConverterPath();
		if (converterPath is null)
		{
			errorMessage = "The binary FBX converter is unavailable on this platform. The export bundle still contains the Blender-ready GLB.";
			return false;
		}

		Directory.CreateDirectory(Path.GetDirectoryName(outputFbxPath)!);
		if (File.Exists(outputFbxPath))
		{
			File.Delete(outputFbxPath);
		}

		try
		{
			ProcessStartInfo startInfo = new()
			{
				FileName = converterPath,
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardError = true,
			};
			startInfo.ArgumentList.Add("export");
			startInfo.ArgumentList.Add(inputGlbPath);
			startInfo.ArgumentList.Add(outputFbxPath);
			startInfo.ArgumentList.Add("-f");
			startInfo.ArgumentList.Add("fbx");

			using Process? process = Process.Start(startInfo);
			if (process is null)
			{
				errorMessage = "The binary FBX converter could not be started.";
				return false;
			}

			Task<string> standardErrorTask = process.StandardError.ReadToEndAsync();
			if (!process.WaitForExit(TimeoutMilliseconds))
			{
				process.Kill(entireProcessTree: true);
				errorMessage = "The binary FBX converter timed out after ten minutes.";
				return false;
			}

			string standardError = standardErrorTask.GetAwaiter().GetResult().Trim();
			if (process.ExitCode != 0)
			{
				errorMessage = string.IsNullOrWhiteSpace(standardError)
					? $"The binary FBX converter exited with code {process.ExitCode}."
					: standardError;
				return false;
			}

			if (!HasBinaryFbxHeader(outputFbxPath))
			{
				errorMessage = "The converter did not produce a valid binary FBX header.";
				return false;
			}

			return true;
		}
		catch (Exception ex)
		{
			errorMessage = ex.Message;
			return false;
		}
	}

	private static string? ResolveConverterPath()
	{
		string? configuredPath = Environment.GetEnvironmentVariable("ASSETRIPPER_ASSIMP_CONVERTER");
		if (!string.IsNullOrWhiteSpace(configuredPath) && File.Exists(configuredPath))
		{
			return configuredPath;
		}

		if (OperatingSystem.IsWindows())
		{
			string bundledPath = Path.Combine(AppContext.BaseDirectory, "NativeTools", "win-x64", "assimp.exe");
			return File.Exists(bundledPath) ? bundledPath : null;
		}

		// Development and Linux validation can use the system command-line tool.
		return "assimp";
	}

	private static bool HasBinaryFbxHeader(string path)
	{
		if (!File.Exists(path) || new FileInfo(path).Length < BinaryFbxSignature.Length)
		{
			return false;
		}

		Span<byte> buffer = stackalloc byte[BinaryFbxSignature.Length];
		using FileStream stream = File.OpenRead(path);
		int read = stream.Read(buffer);
		return read == buffer.Length && Encoding.ASCII.GetString(buffer) == BinaryFbxSignature;
	}
}
