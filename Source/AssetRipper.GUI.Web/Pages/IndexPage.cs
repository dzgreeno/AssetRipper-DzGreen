using AssetRipper.GUI.Web.Paths;

using AssetRipper.Premium;

namespace AssetRipper.GUI.Web.Pages;

public sealed class IndexPage : DefaultPage
{
	public static IndexPage Instance { get; } = new();

	public override string? GetTitle() => AssetRipperBrand.ProductName;

			public override void WriteInnerContent(TextWriter writer)
		{
			if (GameFileLoader.IsLoaded)
			{
				AssetBrowserPanel.Write(writer, GameFileLoader.GameBundle);
				using (new Div(writer).WithClass("asset-browser-footer-actions").End())
					{
						PathLinking.WriteLink(writer, GameFileLoader.GameBundle, "Open loaded file tree", "btn btn-outline-secondary");
						if (GameFileLoader.Premium)
						{
							new A(writer).WithHref("/PremiumDiagnostics").WithClass("btn btn-outline-secondary").Close("Premium diagnostics");
						}
						new A(writer).WithHref("/Commands").WithClass("btn btn-primary").Close("Open / export");
				}
			}
		else
		{
			using (new Div(writer).WithClass("text-center container mt-5").End())
			{
				EnterpriseAccessSession access = EnterpriseAccessGate.Resolve();
				new P(writer).WithClass(access.IsTier1ReadableData ? "alert alert-success" : "alert alert-secondary").Close(access.IsTier1ReadableData ? "Enterprise readable-data profile is active for this local session." : "Diagnostic profile is active. Set ASSET_RIPPER_DZGREEN_RECOVERY_TOKEN to a six-character local token, then restart to enable the advanced readable-data profile.");
				new H1(writer).WithClass("display-4 mb-4").Close(Localization.Welcome);
					new P(writer).WithClass("mt-4").Close("Use File → Open file or Open folder to load Unity data. The processed asset workspace will appear here.");
					new Button(writer).WithType("button").WithClass("btn btn-secondary").WithDisabled().Close(Localization.NoFilesLoaded);
				}
			}
		}

		protected override void WriteScriptReferences(TextWriter writer)
		{
			base.WriteScriptReferences(writer);
			OnlineDependencies.Babylon.WriteScriptReference(writer);
			new Script(writer).WithSrc("/js/mesh_preview.js").Close();
			new Script(writer).WithSrc("/js/asset_browser.js").Close();
		}

}
