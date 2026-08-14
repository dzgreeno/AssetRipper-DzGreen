using AssetRipper.GUI.Web.Documentation;
using AssetRipper.GUI.Web.Pages;
using AssetRipper.GUI.Web.Pages.Search;
using AssetRipper.GUI.Web.Paths;
using AssetRipper.Web.Content;

namespace AssetRipper.GUI.Web;
public abstract class DefaultPage : HtmlPage
{
	public sealed override void Write(TextWriter writer)
	{
		base.Write(writer);
		using (new Html(writer).WithLang(Localization.CurrentLanguageCode).End())
		{
			using (new Head(writer).End())
			{
				new Meta(writer).WithCharset("utf-8").Close();
					new Meta(writer).WithName("viewport").WithContent("width=device-width, initial-scale=1.0").Close();
					new Meta(writer).WithName("description").WithContent("AssetRipper DzGreen — advanced Unity asset analysis and export fork maintained by dzgreen.").Close();
						string pageTitle = GetTitle() ?? AssetRipperBrand.ProductName;
						new Title(writer).Close(pageTitle == AssetRipperBrand.ProductName ? pageTitle : $"{AssetRipperBrand.ProductName} · {pageTitle}");
					new Link(writer).WithRel("icon").WithType("image/x-icon").WithHref("/favicon.ico").Close();
					new Link(writer).WithRel("shortcut icon").WithType("image/x-icon").WithHref("/favicon.ico").Close();
					new Link(writer).WithRel("apple-touch-icon").WithHref("/favicon.ico").Close();
					OnlineDependencies.Bootstrap.WriteStyleSheetReference(writer);
				new Link(writer).WithRel("stylesheet").WithHref("/css/site.css").Close();
			}
				using (new Body(writer).WithCustomAttribute("data-bs-theme", "dark").End())
				{
					WriteHeader(writer);

				using (new Div(writer).WithClass("container").End())
				{
					using (new Main(writer).WithRole("main").WithId("app").WithClass("pb-3").End())
					{
						WriteInnerContent(writer);
					}
				}

					WriteFooter(writer);
					WriteStatusDock(writer);

					WriteScriptReferences(writer);
			}
		}
	}

	public abstract string? GetTitle();

	public abstract void WriteInnerContent(TextWriter writer);

		private static void WriteHeader(TextWriter writer)
		{
				using (new Header(writer).End())
				{
					using (new Div(writer).WithClass("dzgreen-brand").End())
					{
						new A(writer).WithHref("/").WithClass("dzgreen-brand__name").Close(AssetRipperBrand.ProductName);
						new Div(writer).WithClass("dzgreen-brand__status").Close(AssetRipperBrand.VersionLine);
					}
					using (new Div(writer).WithClass("top-navigation-controls").End())
				{
					new Button(writer).WithId("assetRipperNavigateBack").WithType("button").WithClass("btn btn-dark navigation-button").WithCustomAttribute("title", "Back").WithCustomAttribute("aria-label", "Back").Close("‹");
					new Button(writer).WithId("assetRipperNavigateForward").WithType("button").WithClass("btn btn-dark navigation-button").WithCustomAttribute("title", "Forward").WithCustomAttribute("aria-label", "Forward").Close("›");
				}
				using (new Div(writer).WithClass("btn-group").End())
				{
					WriteFileMenu(writer);
				WriteViewMenu(writer);
					WriteExportMenu(writer);
					WriteLanguageMenu(writer);
					WriteDevelopmentMenu(writer);
				}
					using (new Div(writer).WithClass("dzgreen-header-links").End())
					{
						new A(writer).WithHref(AssetRipperBrand.UpstreamUrl).WithNewTabAttributes().WithClass("dzgreen-header-link").Close("Upstream");
						new A(writer).WithHref(AssetRipperBrand.ForkUrl).WithNewTabAttributes().WithClass("dzgreen-header-link").Close("GitHub");
						new A(writer).WithHref(AssetRipperBrand.SponsorUrl).WithNewTabAttributes().WithClass("dzgreen-header-link dzgreen-header-link--support").Close("Support dzgreen");
					}
			}
	}

	private static void WriteFileMenu(TextWriter writer)
	{
		using (new Div(writer).WithClass("btn-group dropdown").End())
		{
			WriteDropdownButton(writer, Localization.MenuFile);
			using (new Ul(writer).WithClass("dropdown-menu").End())
			{
				using (new Li(writer).End())
				{
					WritePostLink(writer, "/LoadFile", Localization.MenuFileOpenFile, "dropdown-item");
				}
				using (new Li(writer).End())
				{
					WritePostLink(writer, "/LoadFolder", Localization.MenuFileOpenFolder, "dropdown-item");
				}
				using (new Li(writer).End())
				{
					WritePostLink(writer, "/Reset", Localization.MenuFileReset, "dropdown-item");
				}
				using (new Li(writer).End())
				{
					new Hr(writer).WithClass("dropdown-divider").Close();
				}
				using (new Li(writer).End())
				{
					new A(writer).WithClass("dropdown-item").WithHref("/Settings/Edit").Close(Localization.Settings);
				}
			}
		}
	}

	private static void WriteViewMenu(TextWriter writer)
	{
		using (new Div(writer).WithClass("btn-group dropdown").End())
		{
			WriteDropdownButton(writer, Localization.MenuView);
			using (new Ul(writer).WithClass("dropdown-menu").End())
			{
				using (new Li(writer).End())
				{
					new A(writer).WithClass("dropdown-item").WithHref("/").Close(Localization.Home);
				}
				if (GameFileLoader.IsLoaded)
				{
					using (new Li(writer).End())
					{
						new A(writer).WithClass("dropdown-item").WithHref("/Search/View").Close(Localization.Search);
					}
				}
				using (new Li(writer).End())
				{
					new A(writer).WithClass("dropdown-item").WithHref("/Settings/Edit").Close(Localization.Settings);
				}
				using (new Li(writer).End())
				{
					new A(writer).WithClass("dropdown-item").WithHref("/ConfigurationFiles").Close(Localization.ConfigurationFiles);
				}
				using (new Li(writer).End())
				{
					new A(writer).WithClass("dropdown-item").WithHref("/Commands").Close(Localization.Commands);
				}
				using (new Li(writer).End())
				{
					new A(writer).WithClass("dropdown-item").WithHref("/Privacy").Close(Localization.Privacy);
				}
				using (new Li(writer).End())
				{
					new A(writer).WithClass("dropdown-item").WithHref("/Licenses").Close(Localization.Licenses);
				}
				using (new Li(writer).End())
				{
					new A(writer).WithClass("dropdown-item").WithHref("/PremiumFeatures").Close(Localization.PremiumFeatures);
				}
			}
		}
	}

	private static void WriteExportMenu(TextWriter writer)
	{
		using (new Div(writer).WithClass("btn-group dropdown").End())
		{
			WriteDropdownButton(writer, Localization.MenuExport);
			using (new Ul(writer).WithClass("dropdown-menu").End())
			{
				if (GameFileLoader.IsLoaded)
				{
					using (new Li(writer).End())
					{
						new A(writer).WithClass("dropdown-item").WithHref("/Commands").Close(Localization.MenuExportAll);
					}
					string version = GameFileLoader.GameBundle.GetMaxUnityVersion().ToString();
					using (new Li(writer).End())
					{
						new A(writer).WithClass("dropdown-item").WithNewTabAttributes().WithHref($"unityhub://{version}").Close(version);
					}
				}
				else
				{
					using (new Li(writer).End())
					{
						new A(writer).WithClass("dropdown-item disabled").WithCustomAttribute("aria-diabled", "true").Close(Localization.MenuExportAll);
					}
				}
			}
		}
	}

	private static void WriteLanguageMenu(TextWriter writer)
	{
		using (new Div(writer).WithClass("btn-group dropdown").End())
		{
			WriteDropdownButton(writer, Localization.MenuLanguage);
			using (new Ul(writer).WithClass("dropdown-menu").End())
			{
				foreach ((string code, string name) in LanguageCodes.LanguageNameDictionary)
				{
					using (new Li(writer).End())
					{
						WritePostLink(writer, $"/Localization?code={code}", name, "dropdown-item");
					}
				}
			}
		}
	}

	private static void WriteDevelopmentMenu(TextWriter writer)
	{
		using (new Div(writer).WithClass("btn-group dropdown").End())
		{
			WriteDropdownButton(writer, Localization.MenuDevelopment);
			using (new Ul(writer).WithClass("dropdown-menu").End())
			{
				using (new Li(writer).End())
				{
					new A(writer).WithClass("dropdown-item").WithHref(DocumentationPaths.OpenApi).Close(Localization.OpenApiJson);
				}
				using (new Li(writer).End())
				{
					new A(writer).WithClass("dropdown-item").WithHref(DocumentationPaths.Swagger).Close(Localization.SwaggerDocumentation);
				}
				using (new Li(writer).End())
				{
					new A(writer).WithClass("dropdown-item").WithNewTabAttributes().WithHref("https://unity.com/unity-hub").Close(Localization.InstallUnityHub);
				}
				if (GameFileLoader.IsLoaded)
				{
					string version = GameFileLoader.GameBundle.GetMaxUnityVersion().ToString();
					using (new Li(writer).End())
					{
						new A(writer).WithClass("dropdown-item").WithNewTabAttributes().WithHref($"unityhub://{version}").Close(Localization.InstallUnityEditor);
					}
				}
			}
		}
	}

	private static void WriteDropdownButton(TextWriter writer, string buttonText)
	{
		new Button(writer).WithClass("btn btn-dark dropdown-toggle mx-0")
			.WithType("button")
			.WithCustomAttribute("data-bs-toggle", "dropdown")
			.WithCustomAttribute("aria-expanded", "false")
			.Close(buttonText);
	}

	private static void WritePostLink(TextWriter writer, string url, string name, string? @class = null)
	{
		using (new Form(writer).WithAction(url).WithMethod("post").End())
		{
			new Input(writer).WithType("submit").WithClass(@class).WithValue(name.ToHtml()).Close();
		}
	}

	private static void WriteFooter(TextWriter writer)
	{
		using (new Footer(writer).WithClass("border-top footer text-muted").End())
		{
			using (new Div(writer).WithClass("container text-center").End())
			{
					writer.Write("&copy; 2026 - AssetRipper DzGreen · maintained by dzgreen - ");
					new A(writer).WithHref("/Privacy").Close(Localization.Privacy);
					writer.Write(" - ");
					new A(writer).WithHref("/Licenses").Close(Localization.Licenses);
					writer.Write(" - ");
					new A(writer).WithHref(AssetRipperBrand.UpstreamUrl).WithNewTabAttributes().Close("Upstream");
			}
		}
	}

	private static void WriteStatusDock(TextWriter writer)
	{
		using (new Div(writer).WithClass("status-dock").WithCustomAttribute("data-status-dock", "true").End())
		{
				using (new Div(writer).WithClass("status-dock__header").End())
				{
					new P(writer).WithClass("status-pill mb-0").Close("Live status");
					using (new Div(writer).WithClass("status-dock__actions").End())
					{
						new P(writer).WithClass("mb-0").Close("Auto-Fix · Import · Export");
						new Button(writer).WithId("assetRipperCopyFullLog").WithType("button").WithClass("btn btn-sm btn-outline-secondary").Close("Copy full log");
						new A(writer).WithHref("/Status/Full").WithClass("btn btn-sm btn-outline-secondary").WithCustomAttribute("download", "AssetRipper-DzGreen-diagnostics.log").Close("Save log");
					}
				}
			new Pre(writer).WithClass("status-dock__output").WithCustomAttribute("data-status-output", "true").Close("Ready.");
		}
	}

	protected virtual void WriteScriptReferences(TextWriter writer)
	{
		OnlineDependencies.Popper.WriteScriptReference(writer);
		OnlineDependencies.Bootstrap.WriteScriptReference(writer);
		new Script(writer).WithSrc("/js/site.js").Close();
	}
}
