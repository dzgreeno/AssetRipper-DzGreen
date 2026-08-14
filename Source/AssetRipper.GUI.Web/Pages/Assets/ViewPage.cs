using AssetRipper.Assets;
using AssetRipper.GUI.Web.Paths;

namespace AssetRipper.GUI.Web.Pages.Assets;

public sealed class ViewPage : DefaultPage
{
	public required IUnityObjectBase Asset { get; init; }
	public required AssetPath Path { get; init; }

	public override string GetTitle() => Asset.GetBestName();

	public override void WriteInnerContent(TextWriter writer)
	{
		using (new Div(writer).WithClass("asset-workspace").End())
		{
			WriteSidebar(writer);

			using (new Div(writer).WithClass("workspace-panel").End())
			{
				using (new Div(writer).WithClass("d-flex align-items-center justify-content-between gap-2 flex-wrap").End())
				{
					new H1(writer).WithClass("h3 mb-3").Close(GetTitle());
					new Span(writer).WithClass("badge rounded-pill").Close(Asset.ClassName);
				}

				ReadOnlySpan<HtmlTab> tabs =
				[
					new InformationTab(Asset, Path),
					new AudioTab(Asset, Path),
					new ImageTab(Asset, Path),
					new ModelTab(Asset, Path),
					new TextTab(Asset, Path),
					new FontTab(Asset, Path),
					new VideoTab(Asset, Path),
					new YamlTab(Asset, Path),
					new JsonTab(Asset, Path),
					new HexTab(Asset, Path),
					new DependenciesTab(Asset),
					new DevelopmentTab(Asset),
				];

				HtmlTab.WriteNavigation(writer, tabs);
				using (new Div(writer).WithClass("asset-viewport").End())
				{
					HtmlTab.WriteContent(writer, tabs);
				}
			}

			WriteInspector(writer);
		}
	}

	private void WriteSidebar(TextWriter writer)
	{
		using (new Aside(writer).WithClass("asset-sidebar").End())
		{
			new H2(writer).Close("Workspace");
			new A(writer).WithHref("/").WithClass("btn btn-secondary btn-sm").Close("Game structure");
			new A(writer).WithHref("/Commands").WithClass("btn btn-secondary btn-sm").Close("Export modes");
			new H2(writer).WithClass("mt-4").Close("Asset filters");
			string[] filters = ["All", "Meshes", "Animations", "Textures", "Audio", "Video", "Shaders"];
			foreach (string filter in filters)
			{
				new Button(writer).WithType("button").WithClass("btn btn-dark btn-sm asset-filter").WithCustomAttribute("data-asset-filter", filter.ToLowerInvariant()).Close(filter);
			}
		}
	}

	private void WriteInspector(TextWriter writer)
	{
		using (new Aside(writer).WithClass("asset-inspector").End())
		{
			new H2(writer).Close("Inspector");
			using (new Table(writer).WithClass("table table-sm align-middle").End())
			{
				using (new Tbody(writer).End())
				{
					WriteInspectorRow(writer, "Class", Asset.ClassName);
					WriteInspectorRow(writer, "Class ID", Asset.ClassID.ToString());
					WriteInspectorRow(writer, "Path ID", Asset.PathID.ToString());
					WriteInspectorRow(writer, "Collection", Asset.Collection.Name);
					if (!string.IsNullOrEmpty(Asset.AssetBundleName))
					{
						WriteInspectorRow(writer, "Bundle", Asset.AssetBundleName);
					}
				}
			}
			using (new Div(writer).WithClass("small text-secondary mt-3").End())
			{
				new P(writer).Close("Preview and export actions remain available in the asset tabs and Commands page.");
			}
		}
	}

	private static void WriteInspectorRow(TextWriter writer, string label, string value)
	{
		using (new Tr(writer).End())
		{
			new Th(writer).Close(label);
			new Td(writer).Close(value);
		}
	}

	protected override void WriteScriptReferences(TextWriter writer)
	{
		base.WriteScriptReferences(writer);
		OnlineDependencies.Babylon.WriteScriptReference(writer);
		new Script(writer).WithSrc("/js/mesh_preview.js").Close();
	}
}
