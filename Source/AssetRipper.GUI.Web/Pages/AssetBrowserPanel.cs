using AssetRipper.Assets;
using AssetRipper.Assets.Bundles;
using AssetRipper.Assets.Collections;
using AssetRipper.GUI.Web.Paths;
using AssetRipper.GUI.Web.Pages.Assets;
using AssetRipper.SourceGenerated.Classes.ClassID_1;
using AssetRipper.SourceGenerated.Classes.ClassID_43;
using AssetRipper.SourceGenerated.Extensions;

namespace AssetRipper.GUI.Web.Pages;

internal static class AssetBrowserPanel
{
	public static void Write(TextWriter writer, GameBundle bundle)
	{
		AssetRow[] rows = bundle.FetchAssetCollections()
			.SelectMany(collection => collection.Select(asset => CreateRow(asset, collection)))
			.OrderBy(row => row.Name, StringComparer.OrdinalIgnoreCase)
			.ThenBy(row => row.ClassName, StringComparer.OrdinalIgnoreCase)
			.ToArray();

		HashSet<string> classes = rows.Select(row => row.ClassName).ToHashSet(StringComparer.OrdinalIgnoreCase);
		HashSet<string> collections = rows.Select(row => row.CollectionName).ToHashSet(StringComparer.OrdinalIgnoreCase);
		int gameObjects = rows.Count(row => row.Category == "GameObject");
		int meshes = rows.Count(row => row.Category == "Mesh");
		int animations = rows.Count(row => row.Category == "Animation");
		int textures = rows.Count(row => row.Category == "Texture");
		CharacterAssemblyIndex.CharacterAssembly[] characterAssemblies = CharacterAssemblyIndex.Build(bundle);

			using (new Section(writer).WithClass("asset-browser-shell").WithCustomAttribute("data-asset-browser", "true").WithCustomAttribute("data-asset-total", rows.Length.ToString()).End())
		{
			using (new Div(writer).WithClass("asset-browser-toolbar").End())
			{
				using (new Div(writer).WithClass("asset-browser-heading").End())
				{
new H1(writer).WithClass("asset-browser-title").Close("Asset Workspace");
						new P(writer).WithClass("asset-browser-subtitle").Close("Browse processed game data, inspect resolved components, and open any asset without leaving the main workspace.");
				}
				using (new Div(writer).WithClass("asset-browser-actions").End())
				{
					new A(writer).WithHref("/Commands").WithClass("btn btn-primary").Close("Open / export");
					new A(writer).WithHref("/Search/View").WithClass("btn btn-outline-secondary").Close("Advanced search");
				}
			}

			using (new Div(writer).WithClass("asset-browser-stats").End())
			{
				WriteStat(writer, "Assets", rows.Length.ToString());
				WriteStat(writer, "Collections", collections.Count.ToString());
				WriteStat(writer, "GameObjects", gameObjects.ToString());
				WriteStat(writer, "Meshes", meshes.ToString());
				WriteStat(writer, "Animations", animations.ToString());
					WriteStat(writer, "Textures", textures.ToString());
				}
				if (rows.Length > 5000)
				{
					new P(writer).WithClass("asset-browser-large-set-note").Close($"All {rows.Length} processed assets are available. Use search or filters to narrow the workspace.");
				}
				if (GameFileLoader.ProcessingIssues.Count > 0)
				{
					using (new Div(writer).WithClass("alert alert-warning asset-browser-processing-warning").End())
					{
						new Strong(writer).Close($"{GameFileLoader.ProcessingIssues.Count} optional processing issue(s) were recorded.");
						new P(writer).Close("The workspace remains usable. Review the processing log before exporting if a component is missing.");
					}
				}

					using (new Div(writer).WithClass("asset-browser-layout").End())
				{
				using (new Div(writer).WithClass("asset-browser-main").End())
				{
					WriteWorkspaceWorkbench(writer, rows, characterAssemblies);

					using (new Div(writer).WithId("assetBrowserFilesPanel").WithClass("asset-browser-files-panel").End())
					{
						using (new Div(writer).WithClass("asset-browser-files-panel-header").End())
						{
							using (new Div(writer).End())
							{
								new H3(writer).Close("Asset list");
								new P(writer).WithClass("asset-browser-files-panel-subtitle").Close("Search and browse all processed files without losing the selected asset.");
							}
							new Button(writer).WithId("assetBrowserFilesToggle").WithType("button").WithClass("btn btn-sm btn-outline-info").WithCustomAttribute("aria-expanded", "true").Close("Hide asset list");
						}
						using (new Div(writer).WithId("assetBrowserFilesContent").WithClass("asset-browser-files-content").End())
						{
							using (new Div(writer).WithClass("asset-browser-controls").End())
			{
				using (new Div(writer).WithClass("asset-browser-search-wrap").End())
				{
					new Label(writer).WithFor("assetBrowserSearch").WithClass("visually-hidden").Close("Search assets");
					new Input(writer).WithId("assetBrowserSearch").WithType("search").WithClass("form-control asset-browser-search")
						.WithPlaceholder("Search name, class, collection, or path…")
						.WithCustomAttribute("autocomplete", "off")
						.Close();
				}
				using (new Div(writer).WithClass("asset-browser-selects").End())
				{
					WriteSelect(writer, "assetBrowserCategory", "Category", new[] { "All", "GameObject", "Mesh", "Animation", "Texture", "Material", "Audio", "Video", "Shader", "Other" });
					WriteSelect(writer, "assetBrowserClass", "Class", classes.Order(StringComparer.OrdinalIgnoreCase));
					WriteSelect(writer, "assetBrowserCollection", "Collection", collections.Order(StringComparer.OrdinalIgnoreCase));
				}
				using (new Div(writer).WithClass("asset-browser-view-buttons").End())
				{
					new Button(writer).WithId("assetBrowserListView").WithType("button").WithClass("btn btn-sm btn-primary").Close("List");
					new Button(writer).WithId("assetBrowserGridView").WithType("button").WithClass("btn btn-sm btn-outline-secondary").Close("Grid");
				}
			}

							using (new Div(writer).WithClass("asset-browser-chip-row").End())
							{
								WriteChip(writer, "assetBrowserQuickAll", "All");
				WriteChip(writer, "assetBrowserQuickModel", "Model set", "Model");
				WriteChip(writer, "assetBrowserQuickAnimation", "Animation", "Animation");
				WriteChip(writer, "assetBrowserQuickTexture", "Textures", "Texture");
				WriteChip(writer, "assetBrowserQuickGameObject", "GameObjects", "GameObject");
				new Span(writer).WithId("assetBrowserResultCount").WithClass("asset-browser-result-count").Close();
			}

			using (new Div(writer).WithClass("asset-browser-table-wrap").End())
			{
				using (new Table(writer).WithId("assetBrowserTable").WithClass("table asset-browser-table").End())
				{
					using (new Thead(writer).End())
					{
						using (new Tr(writer).End())
						{
							new Th(writer).Close("Name");
							new Th(writer).Close("Class");
							new Th(writer).Close("Category");
														new Th(writer).Close("Collection");
														new Th(writer).Close("Components");
														new Th(writer).Close("Path ID");
						}
					}
					using (new Tbody(writer).End())
					{
						foreach (AssetRow row in rows)
						{
							using (new Tr(writer)
								.WithClass("asset-browser-row")
									.WithCustomAttribute("data-asset-name", row.Name)
									.WithCustomAttribute("data-asset-class", row.ClassName)
								.WithCustomAttribute("data-asset-category", row.Category)
									.WithCustomAttribute("data-asset-collection", row.CollectionName)
										.WithCustomAttribute("data-asset-search", row.SearchText)
											.WithCustomAttribute("data-asset-components", row.ComponentSummary)
.WithCustomAttribute("data-asset-model-url", row.ModelUrl ?? string.Empty)
											.WithCustomAttribute("data-asset-view-url", row.ViewUrl)
											.WithCustomAttribute("data-asset-yaml-url", row.YamlUrl)
											.WithCustomAttribute("data-asset-json-url", row.JsonUrl)
											.End())
							{
								using (new Td(writer).WithClass("asset-browser-name").End())
								{
									PathLinking.WriteLink(writer, row.Asset, row.Name, "asset-browser-link");
								}
								new Td(writer).WithClass("asset-browser-class").Close(row.ClassName);
								new Td(writer).WithClass("asset-browser-category").Close(row.Category);
															using (new Td(writer).WithClass("asset-browser-collection").End())
															{
																PathLinking.WriteLink(writer, row.Collection, row.CollectionName, "asset-browser-link asset-browser-link-muted");
															}
															new Td(writer).WithClass("asset-browser-components").WithCustomAttribute("title", row.ComponentSummary).Close(row.ComponentSummary);
															new Td(writer).WithClass("asset-browser-path-id").Close(row.Asset.PathID.ToString());
							}
						}
					}
											}
							}
						}
					}
				}
			}
		}
	}

	private static void WriteWorkspaceWorkbench(TextWriter writer, AssetRow[] rows, CharacterAssemblyIndex.CharacterAssembly[] assemblies)
	{
		CharacterAssemblyIndex.CharacterAssembly? assembly = assemblies.FirstOrDefault();
		IUnityObjectBase? previewAsset = assembly?.Meshes.FirstOrDefault()
			?? rows.Select(row => row.Asset).FirstOrDefault(asset => asset is IMesh)
			?? assembly?.Root;
		string? previewUrl = assembly is not null
			? AssetAPI.GetCharacterModelUrl(assembly.Root.GetPath())
			: previewAsset is IMesh
				? AssetAPI.GetModelUrl(previewAsset.GetPath())
				: null;

		using (new Section(writer).WithClass("asset-browser-workbench").WithCustomAttribute("data-asset-workbench", "true").End())
		{
					using (new Div(writer).WithClass("asset-browser-workbench-header").End())
				{
					using (new Div(writer).End())
				{
					new H2(writer).WithId("assetBrowserWorkbenchTitle").Close(assembly is null ? "Selected asset preview" : $"Assembled character · {assembly.RootName}");
					new P(writer).WithClass("asset-browser-workbench-subtitle").Close("Preview, inspect, and export resolved components without leaving the main workspace.");
				}
					using (new Div(writer).WithClass("asset-browser-workbench-status").End())
					{
						new Span(writer).WithClass("asset-browser-status-dot").Close("●");
						new Span(writer).Close(assembly is null ? "No character set selected" : $"{assembly.HierarchyAssetCount} hierarchy assets resolved");
					}
					using (new Div(writer).WithClass("asset-browser-workbench-header-actions").End())
					{
						new Button(writer).WithId("assetBrowserFocusPreview").WithType("button").WithClass("btn btn-sm btn-outline-info").WithCustomAttribute("aria-pressed", "false").Close("Focus preview");
						new Button(writer).WithId("assetBrowserHierarchyToggle").WithType("button").WithClass("btn btn-sm btn-outline-secondary").WithCustomAttribute("aria-pressed", "true").Close("Hierarchy");
						new Button(writer).WithId("assetBrowserInspectorToggle").WithType("button").WithClass("btn btn-sm btn-outline-secondary").WithCustomAttribute("aria-pressed", "true").Close("Asset actions");
					}
				}

					WriteCharacterSwitcher(writer, assemblies);

					using (new Div(writer).WithClass("asset-browser-workbench-grid").End())
			{
				using (new Aside(writer).WithClass("asset-browser-hierarchy").End())
				{
					new H3(writer).Close("Hierarchy");
					if (assembly is null)
					{
						new P(writer).WithClass("asset-browser-empty-note").Close("Load a character root to see its resolved hierarchy.");
					}
					else
					{
						using (new Div(writer).WithClass("asset-browser-tree-group").End())
						{
							new Span(writer).WithClass("asset-browser-tree-label").Close("ROOT");
							PathLinking.WriteLink(writer, assembly.Root, assembly.RootName, "asset-browser-tree-link asset-browser-tree-root");
						}
						WriteHierarchyGroup(writer, "GAMEOBJECTS", assembly.HierarchyAssets.OfType<IGameObject>());
						WriteHierarchyGroup(writer, "COMPONENTS", assembly.HierarchyAssets.Where(asset => asset is not IGameObject));
					}
				}

				using (new Div(writer).WithClass("asset-browser-preview-panel").End())
				{
					using (new Div(writer).WithClass("asset-browser-preview-toolbar").End())
					{
						if (previewUrl is not null)
						{
							new A(writer).WithId("assetBrowserPreviewDownload").WithHref(previewUrl).WithClass("btn btn-sm btn-primary").WithCustomAttribute("download", "character.glb").Close("Download GLB");
						}
						new Button(writer).WithType("button").WithClass("btn btn-sm btn-secondary").WithId("toggleModelLighting").Close("Lighting: on");
						new Button(writer).WithType("button").WithClass("btn btn-sm btn-secondary").WithId("resetModelCamera").Close("Reset camera");
						new Button(writer).WithType("button").WithClass("btn btn-sm btn-secondary").WithId("toggleModelAnimation").Close("Animation: on");
						new A(writer).WithHref("/Commands").WithClass("btn btn-sm btn-success").Close("Export FBX");
					}
						WriteWorkspaceContextTabs(writer, previewAsset);
						if (previewUrl is not null)
						{
							using (new Div(writer).WithClass("asset-browser-preview-canvas-wrap").End())
						{
							new Canvas(writer).WithId("babylonRenderCanvas").WithClass("asset-browser-preview-canvas").WithCustomAttribute("glb-data-path", previewUrl).Close();
						}
					}
					else
					{
						using (new Div(writer).WithClass("asset-browser-preview-placeholder").End())
						{
							new Strong(writer).Close("No previewable mesh selected");
							new P(writer).Close("Select a Mesh row to open its GLB preview.");
						}
					}
					new P(writer).WithId("assetBrowserPreviewStatus").WithClass("asset-browser-preview-status").Close(assembly is null ? "Select a mesh or character set." : "Assembled hierarchy preview loaded from the resolved root.");
				}

				using (new Aside(writer).WithClass("asset-browser-workbench-inspector").End())
				{
					new H3(writer).Close("Asset actions");
						if (previewAsset is not null)
						{
							WriteWorkspaceActionLink(writer, "assetBrowserSelectedAssetOpen", AssetAPI.GetViewUrl(previewAsset.GetPath()), "Open selected asset", "btn btn-sm btn-outline-info w-100 mb-2");
							using (new Div(writer).WithClass("asset-browser-raw-links").End())
							{
								new Span(writer).WithClass("asset-browser-tree-label").Close("RAW DATA");
								WriteWorkspaceActionLink(writer, "assetBrowserSelectedAssetView", AssetAPI.GetViewUrl(previewAsset.GetPath()), "Information / tabs", "asset-browser-raw-link");
								WriteWorkspaceActionLink(writer, "assetBrowserSelectedAssetYaml", AssetAPI.GetYamlUrl(previewAsset.GetPath()), "Yaml", "asset-browser-raw-link");
								WriteWorkspaceActionLink(writer, "assetBrowserSelectedAssetJson", AssetAPI.GetJsonUrl(previewAsset.GetPath()), "Json", "asset-browser-raw-link");
								if (previewAsset is IMesh)
								{
									WriteWorkspaceActionLink(writer, "assetBrowserSelectedAssetModel", AssetAPI.GetModelUrl(previewAsset.GetPath()), "Mesh GLB", "asset-browser-raw-link");
								}
							}
						}
					if (assembly is not null)
					{
						using (new Div(writer).WithClass("asset-browser-assembly-facts").End())
						{
							new Span(writer).WithClass("asset-browser-tree-label").Close("RESOLVED COMPONENTS");
							WriteWorkbenchFact(writer, "Meshes", assembly.Meshes.Count);
							WriteWorkbenchFact(writer, "Materials", assembly.Materials.Count);
							WriteWorkbenchFact(writer, "Textures", assembly.Textures.Count);
							WriteWorkbenchFact(writer, "Clips", assembly.AnimationClips.Count);
					WriteWorkbenchFact(writer, "Weighted meshes", assembly.WeightedSkinnedMeshCount);
								if (assembly.MissingSkinWeightsCount > 0)
							{
								new P(writer).WithClass("character-assembly-missing").Close($"{assembly.MissingSkinWeightsCount} mesh(es) need skin-weight review.");
							}
						}
					}
				}
			}
		}
	}

	private static void WriteCharacterSwitcher(TextWriter writer, CharacterAssemblyIndex.CharacterAssembly[] assemblies)
	{
		using (new Div(writer).WithClass("asset-browser-character-switcher").End())
		{
			using (new Div(writer).WithClass("asset-browser-character-switcher-heading").End())
			{
				new Span(writer).WithClass("asset-browser-tree-label").Close("CHARACTER SET");
				new Span(writer).WithClass("asset-browser-switcher-hint").Close("Choose a resolved root");
			}
			if (assemblies.Length == 0)
			{
				new P(writer).WithClass("asset-browser-empty-note").Close("No resolved character roots were found.");
			}
			else
			{
				using (new Div(writer).WithClass("asset-browser-character-switcher-list").End())
				{
					foreach (CharacterAssemblyIndex.CharacterAssembly assembly in assemblies)
					{
						using (new Button(writer).WithType("button").WithClass("asset-browser-character-choice").WithCustomAttribute("data-character-preview-url", AssetAPI.GetCharacterModelUrl(assembly.Root.GetPath())).WithCustomAttribute("data-character-name", assembly.RootName).WithCustomAttribute("data-character-asset-url", AssetAPI.GetViewUrl(assembly.Root.GetPath())).WithCustomAttribute("data-character-yaml-url", AssetAPI.GetYamlUrl(assembly.Root.GetPath())).WithCustomAttribute("data-character-json-url", AssetAPI.GetJsonUrl(assembly.Root.GetPath())).WithCustomAttribute("data-character-collection", assembly.Root.Collection.Name).WithCustomAttribute("data-character-path-id", assembly.Root.PathID.ToString()).WithCustomAttribute("data-character-components", "GameObject · Transform · Animator hierarchy").End())
						{
							new Strong(writer).Close(assembly.RootName);
							new Span(writer).Close($"{assembly.Meshes.Count} meshes · {assembly.Textures.Count} textures · {assembly.AnimationClips.Count} clips");
						}
					}
				}
			}
		}
	}

	private static void WriteHierarchyGroup(TextWriter writer, string label, IEnumerable<IUnityObjectBase> assets)
	{
		IUnityObjectBase[] resolved = assets.OrderBy(asset => asset.GetBestName(), StringComparer.OrdinalIgnoreCase).Take(40).ToArray();
		if (resolved.Length == 0)
		{
			return;
		}
		using (new Div(writer).WithClass("asset-browser-tree-group").End())
		{
			new Span(writer).WithClass("asset-browser-tree-label").Close(label);
			foreach (IUnityObjectBase asset in resolved)
			{
				PathLinking.WriteLink(writer, asset, string.IsNullOrWhiteSpace(asset.GetBestName()) ? asset.ClassName : asset.GetBestName(), "asset-browser-tree-link");
			}
		}
	}

	private static void WriteWorkspaceContextTabs(TextWriter writer, IUnityObjectBase? asset)
	{
		using (new Nav(writer).WithClass("asset-browser-context-tabs").End())
		{
			new A(writer).WithId("assetBrowserContextAsset").WithHref(asset is null ? "#" : AssetAPI.GetViewUrl(asset.GetPath())).WithClass("asset-browser-context-tab active").Close("Information");
			new A(writer).WithId("assetBrowserContextYaml").WithHref(asset is null ? "#" : AssetAPI.GetYamlUrl(asset.GetPath())).WithClass("asset-browser-context-tab").Close("Yaml");
			new A(writer).WithId("assetBrowserContextJson").WithHref(asset is null ? "#" : AssetAPI.GetJsonUrl(asset.GetPath())).WithClass("asset-browser-context-tab").Close("Json");
			new A(writer).WithId("assetBrowserContextDependencies").WithHref(asset is null ? "#" : AssetAPI.GetViewUrl(asset.GetPath())).WithClass("asset-browser-context-tab").Close("Dependencies");
		}
	}

	private static void WriteWorkspaceActionLink(TextWriter writer, string id, string href, string text, string className)
	{
		new A(writer).WithId(id).WithHref(href).WithClass(className).Close(text);
	}

	private static void WriteWorkbenchFact(TextWriter writer, string label, int value)
	{
		using (new Div(writer).WithClass("asset-browser-fact").End())
		{
			new Strong(writer).Close(value.ToString());
			new Span(writer).Close(label);
		}
	}

	private static void WriteInspector(TextWriter writer)
	{
		using (new Aside(writer).WithId("assetBrowserInspector").WithClass("asset-browser-inspector").End())
		{
			new H2(writer).Close("Components");
			new P(writer).WithId("assetBrowserInspectorEmpty").WithClass("asset-browser-inspector-empty").Close("Select an asset row to inspect its class, collection, and resolved components.");
			using (new Div(writer).WithId("assetBrowserInspectorDetails").WithClass("asset-browser-inspector-details").End())
			{
				new H3(writer).WithId("assetBrowserInspectorName").Close("-");
				new P(writer).WithId("assetBrowserInspectorClass").WithClass("asset-browser-inspector-line").Close("Class: -");
				new P(writer).WithId("assetBrowserInspectorCollection").WithClass("asset-browser-inspector-line").Close("Collection: -");
				new P(writer).WithId("assetBrowserInspectorPathId").WithClass("asset-browser-inspector-line").Close("Path ID: -");
				new P(writer).WithId("assetBrowserInspectorComponents").WithClass("asset-browser-inspector-components").Close("Components: -");
				new A(writer).WithId("assetBrowserInspectorOpen").WithHref("#").WithClass("btn btn-sm btn-primary").Close("Open asset");
			}
		}
	}

	private static AssetRow CreateRow(IUnityObjectBase asset, AssetCollection collection)
	{
		string name = string.IsNullOrWhiteSpace(asset.GetBestName()) ? $"{asset.ClassName} ({asset.PathID})" : asset.GetBestName();
		string className = asset.ClassName;
		string category = GetCategory(className);
		string collectionName = collection.Name;
			string componentSummary = GetComponentSummary(asset);
AssetPath assetPath = asset.GetPath();
				string? modelUrl = asset is IMesh ? AssetAPI.GetModelUrl(assetPath) : null;
				return new AssetRow(asset, collection, name, className, category, collectionName, componentSummary, $"{name} {className} {category} {collectionName} {componentSummary} {asset.PathID}", modelUrl, AssetAPI.GetViewUrl(assetPath), AssetAPI.GetYamlUrl(assetPath), AssetAPI.GetJsonUrl(assetPath));
	}

	private static string GetComponentSummary(IUnityObjectBase asset)
	{
		if (asset is not IGameObject gameObject)
		{
			return asset.ClassName;
		}
		try
		{
			string[] components = gameObject.GetComponentAccessList().WhereNotNull().Select(component => component.ClassName).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
			return components.Length == 0 ? "GameObject (no resolved components)" : string.Join(" · ", components);
		}
		catch (Exception ex)
		{
			return $"GameObject (component read failed: {ex.Message})";
		}
	}

	private static string GetCategory(string className)
	{
		if (className is "GameObject" or "PrefabInstance") return "GameObject";
		if (className.Contains("Mesh", StringComparison.OrdinalIgnoreCase) || className is "Avatar") return "Mesh";
		if (className.Contains("Animation", StringComparison.OrdinalIgnoreCase) || className is "AnimatorController" or "AnimatorOverrideController") return "Animation";
		if (className.Contains("Texture", StringComparison.OrdinalIgnoreCase) || className is "Sprite" or "Cubemap") return "Texture";
		if (className.Contains("Material", StringComparison.OrdinalIgnoreCase) || className is "Shader") return className == "Shader" ? "Shader" : "Material";
		if (className.Contains("Audio", StringComparison.OrdinalIgnoreCase)) return "Audio";
		if (className.Contains("Video", StringComparison.OrdinalIgnoreCase)) return "Video";
		return "Other";
	}

	private static void WriteAssemblyLinks(TextWriter writer, string label, IEnumerable<IUnityObjectBase> assets)
	{
		IUnityObjectBase[] resolved = assets.Where(asset => asset is not null).OrderBy(asset => asset.GetBestName(), StringComparer.OrdinalIgnoreCase).Take(12).ToArray();
		if (resolved.Length == 0)
		{
			return;
		}
		using (new Div(writer).WithClass("character-assembly-component-group").End())
		{
			new Span(writer).WithClass("character-assembly-component-label").Close(label);
			foreach (IUnityObjectBase asset in resolved)
			{
				PathLinking.WriteLink(writer, asset, string.IsNullOrWhiteSpace(asset.GetBestName()) ? $"{asset.ClassName} ({asset.PathID})" : asset.GetBestName(), "asset-browser-link asset-browser-link-muted");
			}
		}
	}

	private static void WriteAssemblyMetric(TextWriter writer, string label, int value)
	{
		using (new Div(writer).WithClass("character-assembly-metric").End())
		{
			new Span(writer).WithClass("character-assembly-metric-value").Close(value.ToString());
			new Span(writer).WithClass("character-assembly-metric-label").Close(label);
		}
	}

	private static void WriteStat(TextWriter writer, string label, string value)
	{
		using (new Div(writer).WithClass("asset-browser-stat").End())
		{
			new Span(writer).WithClass("asset-browser-stat-value").Close(value);
			new Span(writer).WithClass("asset-browser-stat-label").Close(label);
		}
	}

	private static void WriteSelect(TextWriter writer, string id, string label, IEnumerable<string> values)
	{
		using (new Div(writer).WithClass("asset-browser-select-wrap").End())
		{
			new Label(writer).WithFor(id).WithClass("asset-browser-control-label").Close(label);
			using (new Select(writer).WithId(id).WithClass("form-select form-select-sm").End())
			{
				new Option(writer).WithValue(string.Empty).Close($"All {label}s");
				foreach (string value in values.Where(value => !string.Equals(value, "All", StringComparison.OrdinalIgnoreCase)))
				{
					new Option(writer).WithValue(value.ToHtml()).Close(value);
				}
			}
		}
	}

	private static void WriteChip(TextWriter writer, string id, string label, string? category = null)
	{
		Button button = new Button(writer).WithId(id).WithType("button").WithClass("asset-browser-chip");
		if (category is not null)
		{
			button.WithCustomAttribute("data-category", category);
		}
		button.Close(label);
	}

	private readonly record struct AssetRow(
		IUnityObjectBase Asset,
		AssetCollection Collection,
		string Name,
		string ClassName,
		string Category,
		string CollectionName,
			string ComponentSummary,
			string SearchText,
			string? ModelUrl,
			string ViewUrl,
			string YamlUrl,
			string JsonUrl);
}
