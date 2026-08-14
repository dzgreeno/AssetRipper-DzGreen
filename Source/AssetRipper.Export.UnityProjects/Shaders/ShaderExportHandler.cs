using AssetRipper.SourceGenerated.Classes.ClassID_48;
namespace AssetRipper.Export.UnityProjects.Shaders;

/// <summary>
/// Stable shader export entry point. Platform-specific GPU blobs are not treated as
/// source code; the safe fallback is emitted by DummyShaderTextExporter.
/// </summary>
public static class ShaderExportHandler
{
	public static bool ExportShader(IShader shader, TextWriter writer)
	{
		return DummyShaderTextExporter.ExportShader(shader, writer);
	}
}
