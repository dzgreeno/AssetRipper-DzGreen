using AssetRipper.Export.Configuration;

namespace AssetRipper.GUI.Web.Pages.Settings.DropDown;

public sealed class ModelExportFormatDropDownSetting : DropDownSetting<ModelExportFormat>
{
	public static ModelExportFormatDropDownSetting Instance { get; } = new();

	public override string Title => "Model export format";

	protected override string GetDisplayName(ModelExportFormat value) => value switch
	{
		ModelExportFormat.Glb => "GLB (glTF binary)",
		ModelExportFormat.Fbx => "FBX ASCII (mesh + skin + animation)",
		_ => base.GetDisplayName(value),
	};

	protected override string? GetDescription(ModelExportFormat value) => value switch
	{
		ModelExportFormat.Glb => "Optional Babylon.js and glTF-compatible model export.",
		ModelExportFormat.Fbx => "The default grouped FBX 7.4 export with geometry, materials, texture sidecars, skeleton clusters, and TRS curves when source data is available.",
		_ => null,
	};
}
