using AssetRipper.Assets;
using AssetRipper.GUI.Web.Paths;

namespace AssetRipper.GUI.Web.Pages.Assets;

internal sealed class ModelTab : AssetHtmlTab
{
	public string Source { get; }

	public override string DisplayName => Localization.AssetTabModel;
	public override string HtmlName => "model";
	public override bool Enabled => AssetAPI.HasModelData(Asset);

	public ModelTab(IUnityObjectBase asset, AssetPath path) : base(asset)
	{
		Source = AssetAPI.GetModelUrl(path);
	}

	public override void Write(TextWriter writer)
	{
		using (new Div(writer).WithClass("d-flex flex-wrap gap-2 mb-2").End())
		{
			SaveButton.Write(writer, Source, $"{Asset.GetBestName()}.glb", "Download GLB");
			new Button(writer).WithType("button").WithClass("btn btn-sm btn-secondary").WithId("toggleModelLighting").Close("Lighting: on");
			new Button(writer).WithType("button").WithClass("btn btn-sm btn-secondary").WithId("resetModelCamera").Close("Reset camera");
			new Button(writer).WithType("button").WithClass("btn btn-sm btn-secondary").WithId("toggleModelAnimation").Close("Animation: on");
		}
		using (new Div(writer).WithClass("asset-viewport").End())
		{
			new Canvas(writer)
				.WithId("babylonRenderCanvas")
				.WithCustomAttribute("glb-data-path", Source)
				.Close();
		}
	}
}
