using AssetRipper.Export.Configuration;
using AssetRipper.Processing.Configuration;

namespace AssetRipper.Premium;

/// <summary>
/// Applies established, relationship-preserving exporter modes to authorized plaintext input.
/// It does not fabricate unreadable content or bypass protected-content controls.
/// </summary>
public static class PremiumRecoveryProfile
{
	public static void Apply(FullConfiguration configuration)
	{
		ArgumentNullException.ThrowIfNull(configuration);
		ProcessingSettings processing = configuration.ProcessingSettings;
		processing.EnablePrefabOutlining = true;
		processing.EnableStaticMeshSeparation = false;
		processing.EnableAssetDeduplication = false;
		processing.BundledAssetsExportMode = BundledAssetsExportMode.DirectExport;

		ExportSettings export = configuration.ExportSettings;
		export.ModelExportFormat = ModelExportFormat.Fbx;
		export.ImageExportFormat = ImageExportFormat.Png;
		export.SpriteExportMode = SpriteExportMode.Yaml;
		export.AudioExportFormat = AudioExportFormat.Default;
		export.PreferOriginalTextureExtension = true;
		export.ExportUnreadableAssets = false;
	}

	public static PremiumRecoveryProfileSnapshot Capture(FullConfiguration configuration)
	{
		ArgumentNullException.ThrowIfNull(configuration);
		return new(
			configuration.ProcessingSettings.EnablePrefabOutlining,
			configuration.ProcessingSettings.EnableStaticMeshSeparation,
			configuration.ProcessingSettings.EnableAssetDeduplication,
			configuration.ProcessingSettings.BundledAssetsExportMode,
			configuration.ExportSettings.ModelExportFormat,
			configuration.ExportSettings.ImageExportFormat,
			configuration.ExportSettings.SpriteExportMode,
			configuration.ExportSettings.AudioExportFormat,
			configuration.ExportSettings.PreferOriginalTextureExtension,
			configuration.ExportSettings.ExportUnreadableAssets);
	}
}

public sealed record PremiumRecoveryProfileSnapshot(
	bool PrefabOutliningEnabled,
	bool StaticMeshSeparationEnabled,
	bool AssetDeduplicationEnabled,
	BundledAssetsExportMode BundledAssetsExportMode,
	ModelExportFormat ModelExportFormat,
	ImageExportFormat ImageExportFormat,
	SpriteExportMode SpriteExportMode,
	AudioExportFormat AudioExportFormat,
	bool PreferOriginalTextureExtension,
	bool ExportUnreadableAssets);
