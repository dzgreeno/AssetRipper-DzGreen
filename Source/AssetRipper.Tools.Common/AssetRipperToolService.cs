using AssetRipper.Assets;
using AssetRipper.Assets.Bundles;
using AssetRipper.Export.Configuration;
using AssetRipper.Export.Modules.Models;
using GlbTypeTreeFallbackDiagnostic = AssetRipper.Export.Modules.Models.GlbLevelBuilder.GlbTypeTreeFallbackDiagnostic;
using AssetRipper.Export.PrimaryContent;
using AssetRipper.Export.PrimaryContent.Models;
using AssetRipper.Export.UnityProjects;
using AssetRipper.GUI.Web;
using AssetRipper.IO.Files;
using AssetRipper.Processing;
using AssetRipper.Premium;
using AssetRipper.SourceGenerated.Classes.ClassID_189;
using AssetRipper.SourceGenerated.Classes.ClassID_1;
using AssetRipper.SourceGenerated.Classes.ClassID_2;
using AssetRipper.SourceGenerated.Classes.ClassID_4;
using AssetRipper.SourceGenerated.Classes.ClassID_114;
using AssetRipper.SourceGenerated.Classes.ClassID_21;
using AssetRipper.SourceGenerated.Classes.ClassID_25;
using AssetRipper.SourceGenerated.Classes.ClassID_28;
using AssetRipper.SourceGenerated.Classes.ClassID_33;
using AssetRipper.SourceGenerated.Classes.ClassID_43;
using AssetRipper.SourceGenerated.Classes.ClassID_74;
using AssetRipper.SourceGenerated.Classes.ClassID_90;
using AssetRipper.SourceGenerated.Classes.ClassID_91;
using AssetRipper.SourceGenerated.Classes.ClassID_95;
using AssetRipper.SourceGenerated.Classes.ClassID_137;
using AssetRipper.SourceGenerated.Extensions;
using AssetRipper.SourceGenerated.Subclasses.PPtr_Material;
using AssetRipper.SourceGenerated.Subclasses.UnityTexEnv;
using AssetRipper.Yaml;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace AssetRipper.Tools.Common;

public sealed class AssetRipperToolService
{
	private const int DefaultAssetLimit = 2000;

	public bool IsLoaded => GameFileLoader.IsLoaded;
	public bool StrictProcessing
	{
		get => GameFileLoader.StrictProcessing;
		set => GameFileLoader.StrictProcessing = value;
	}
	public IReadOnlyList<ProcessingIssue> ProcessingIssues => GameFileLoader.ProcessingIssues;

	public PremiumImportDiagnosticReport GetPremiumDiagnostics()
	{
		EnsureLoaded();
		return PremiumImportDiagnostics.Create(GameFileLoader.GameBundle, GameFileLoader.CurrentGameData.ProjectVersion, GameFileLoader.LoadedInputPaths);
	}

	public PremiumVerifiedOnlyPlan CreateVerifiedOnlyPlan()
	{
		EnsureLoaded();
		PremiumImportDiagnosticReport diagnostics = GetPremiumDiagnostics();
		PremiumExportCandidate[] candidates = GameFileLoader.GameBundle.FetchAssets()
			.Select(asset => new PremiumExportCandidate(asset.Collection.FilePath, asset.Collection.Name, asset.PathID, asset.GetBestName(), asset.ClassName))
			.ToArray();
		return PremiumExportOrchestrator.CreateVerifiedOnlyPlan(diagnostics.TypeTreeCoverage, candidates);
	}

	public PremiumFallbackTextureCatalog CreateFallbackTextureCatalog(string directory) => PremiumExportOrchestrator.CreateFallbackTextureCatalog(directory);

	public string ExportDiagnostics(string outputDirectory, PremiumDiagnosticsFormat format, PremiumFallbackTextureCatalog? fallbackTextures = null, bool deterministic = false)
	{
		EnsureLoaded();
		string directory = PrepareOutputDirectory(outputDirectory);
		PremiumImportDiagnosticReport diagnostics = GetPremiumDiagnostics();
		PremiumVerifiedOnlyPlan verifiedOnly = CreateVerifiedOnlyPlan();
		string path = Path.Combine(directory, format == PremiumDiagnosticsFormat.Json ? "assetripper-premium-diagnostics.json" : "assetripper-premium-diagnostics.html");
		object report = deterministic
			? new { diagnostics, verifiedOnly, fallbackTextures }
			: new { generatedUtc = DateTimeOffset.UtcNow, diagnostics, verifiedOnly, fallbackTextures };
		if (format == PremiumDiagnosticsFormat.Json)
		{
			File.WriteAllText(path, JsonSerializer.Serialize(report, JsonOptions), new UTF8Encoding(false));
		}
		else
		{
			string payload = HtmlEncoder.Default.Encode(JsonSerializer.Serialize(report, JsonOptions));
			File.WriteAllText(path, $"<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><title>AssetRipper DzGreen Premium Diagnostics</title><style>body{{font-family:system-ui,sans-serif;margin:2rem;background:#101513;color:#eaf5ef}}pre{{white-space:pre-wrap;word-break:break-word;background:#18211d;padding:1rem;border-radius:.5rem}}</style></head><body><h1>AssetRipper DzGreen Premium Diagnostics</h1><p>Read-only report derived from already imported plaintext data.</p><pre>{payload}</pre></body></html>", new UTF8Encoding(false));
		}
		return path;
	}

	public PremiumTextureBatchResult ExportTextures(string outputDirectory, PremiumTextureOutputFormat format)
	{
		EnsureLoaded();
		string directory = PrepareOutputDirectory(Path.Combine(outputDirectory, "textures"));
		PremiumTextureBatchItem[] items = GameFileLoader.GameBundle.FetchAssets()
			.OfType<IImageTexture>()
			.OrderBy(texture => texture.Collection.FilePath, StringComparer.OrdinalIgnoreCase)
			.ThenBy(static texture => texture.PathID)
			.Select(texture =>
			{
				PremiumTextureExportResult result = PremiumTextureTranscoder.TryExport(texture, directory, format);
				return new PremiumTextureBatchItem(texture.PathID, texture.GetBestName(), result.IsSuccess, result.Path, result.Message);
			})
			.ToArray();
		string manifestPath = Path.Combine(directory, "assetripper-texture-transcode-manifest.json");
		File.WriteAllText(manifestPath, JsonSerializer.Serialize(new { generatedUtc = DateTimeOffset.UtcNow, format, items }, JsonOptions), new UTF8Encoding(false));
		return new PremiumTextureBatchResult(directory, format, items.LongCount(static item => item.IsSuccess), items.LongCount(static item => !item.IsSuccess), manifestPath, items);
	}

	public LoadSummary Load(IEnumerable<string> inputPaths, ModelExportFormat modelFormat = ModelExportFormat.Fbx, bool strict = false)
	{
		string[] paths = inputPaths
			.Where(path => !string.IsNullOrWhiteSpace(path))
			.Select(Path.GetFullPath)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToArray();
		if (paths.Length == 0)
		{
			throw new ArgumentException("At least one input file or directory is required.", nameof(inputPaths));
		}
		foreach (string path in paths)
		{
			if (!File.Exists(path) && !Directory.Exists(path))
			{
				throw new FileNotFoundException($"Input path was not found: {path}", path);
			}
			if (File.Exists(path))
			{
				ValidateInputFileHeader(path);
			}
		}

		GameFileLoader.ConfigureAutomation(modelFormat);
		GameFileLoader.StrictProcessing = strict;
		GameFileLoader.LoadAndProcess(paths);
		return new LoadSummary(
			GameFileLoader.LoadedInputPaths.Select(Path.GetFileName).Where(name => name is not null).Cast<string>().ToArray(),
			GameFileLoader.GameBundle.FetchAssets().Count(),
			GameFileLoader.CurrentGameData.ProjectVersion.ToString(),
			GameFileLoader.ProcessingIssues.ToArray());
	}

	public IReadOnlyList<AssetSummary> ListAssets(string? filter = null, int limit = DefaultAssetLimit)
	{
		EnsureLoaded();
		limit = Math.Clamp(limit, 1, 10000);
		string query = filter?.Trim() ?? string.Empty;
		return GameFileLoader.GameBundle.FetchAssets()
			.Where(asset => Matches(asset, query))
			.OrderBy(asset => asset.GetBestName(), StringComparer.OrdinalIgnoreCase)
			.ThenBy(asset => asset.ClassName, StringComparer.OrdinalIgnoreCase)
			.Take(limit)
			.Select(ToAssetSummary)
			.ToArray();
	}

	public IReadOnlyList<AssetSummary> ListAllAssets(string? filter = null)
	{
		EnsureLoaded();
		string query = filter?.Trim() ?? string.Empty;
		return GameFileLoader.GameBundle.FetchAssets()
			.Where(asset => Matches(asset, query))
			.OrderBy(asset => asset.GetBestName(), StringComparer.OrdinalIgnoreCase)
			.ThenBy(asset => asset.ClassName, StringComparer.OrdinalIgnoreCase)
			.Select(ToAssetSummary)
			.ToArray();
	}

	public PrefabInspection InspectPrefab(string? filter = null)
	{
		EnsureLoaded();
		IGameObject root = ResolveRoot(filter);
		IUnityObjectBase[] hierarchy = root.FetchHierarchy().OfType<IUnityObjectBase>().ToArray();
		IComponent[] components = hierarchy.OfType<IComponent>().ToArray();
		List<MeshInspection> meshes = [];
		HashSet<ITransform> bones = new(ReferenceComparer<ITransform>.Instance);
		int weightedMeshes = 0;
		int missingWeights = 0;

		foreach (ISkinnedMeshRenderer renderer in components.OfType<ISkinnedMeshRenderer>())
		{
			if (renderer.MeshP is not IMesh mesh)
			{
				continue;
			}
			foreach (ITransform bone in renderer.BonesP.WhereNotNull())
			{
				bones.Add(bone);
			}
			MeshData.TryMakeFromMesh(mesh, out MeshData meshData);
			if (meshData.HasSkin)
			{
				weightedMeshes++;
			}
			else
			{
				missingWeights++;
			}
			meshes.Add(ToMeshInspection(mesh, meshData));
		}
		foreach (IMeshFilter filterComponent in components.OfType<IMeshFilter>())
		{
			if (filterComponent.TryGetMesh(out IMesh? mesh) && mesh is not null && meshes.All(item => item.PathId != mesh.PathID))
			{
				meshes.Add(ToMeshInspection(mesh, MeshData.TryMakeFromMesh(mesh, out MeshData meshData) ? meshData : default));
			}
		}

		IAnimationClip[] clips = GameFileLoader.GameBundle.FetchAssets().OfType<IAnimationClip>()
			.Where(clip => IsClipForRoot(clip, root))
			.Distinct(ReferenceComparer<IAnimationClip>.Instance)
			.OrderBy(clip => clip.GetBestName(), StringComparer.OrdinalIgnoreCase)
			.ToArray();
		HashSet<IMaterial> materials = new(ReferenceComparer<IMaterial>.Instance);
		HashSet<ITexture2D> textures = new(ReferenceComparer<ITexture2D>.Instance);
		foreach (IRenderer renderer in components.OfType<IRenderer>())
		{
			foreach (IPPtr_Material materialPointer in renderer.Materials_C25)
			{
				if (materialPointer.TryGetAsset(renderer.Collection) is IMaterial material)
				{
					materials.Add(material);
					foreach (IUnityTexEnv textureEnvironment in material.GetTextureProperties().Select(pair => pair.Value))
					{
						if (textureEnvironment.Texture.TryGetAsset(material.Collection) is ITexture2D texture)
						{
							textures.Add(texture);
						}
					}
				}
			}
		}

		return new PrefabInspection(
			ToAssetSummary(root),
			hierarchy.Select(ToAssetSummary).ToArray(),
			components.Select(ToAssetSummary).ToArray(),
			meshes,
			materials.Select(item => ToAssetSummary(item)).ToArray(),
			textures.Select(item => ToAssetSummary(item)).ToArray(),
				clips.Select(item => ToAssetSummary(item)).ToArray(),
				bones.Count,
				weightedMeshes,
				missingWeights,
				GameFileLoader.CurrentGameData.ProjectVersion.ToString(),
				GameFileLoader.ProcessingIssues.ToArray());
	}

	public FbxExportResult ExportFbxWithAnimation(string? filter, string outputDirectory, bool includeAnimations = true)
	{
		EnsureLoaded();
		string directory = PrepareOutputDirectory(outputDirectory);
		IGameObject root = ResolveRoot(filter);
		FbxAsciiExporter exporter = new() { IncludeAnimations = includeAnimations };
		string safeName = SafeFileName($"{root.GetBestName()}_{root.PathID}", $"character_{root.PathID}");
		string path = Path.Combine(directory, safeName + ".fbx");
		bool success = exporter.Export(exporter.GetCharacterAssets(root, GameFileLoader.GameBundle.FetchAssets()), path, LocalFileSystem.Instance);
		return new FbxExportResult(success, path, safeName, includeAnimations, File.Exists(path), CountFiles(directory), GameFileLoader.ProcessingIssues.ToArray());
	}

	public GlbExportResult ExportGlb(string? filter, string outputDirectory, PremiumFallbackTextureCatalog? fallbackTextures = null)
	{
		EnsureLoaded();
		string directory = PrepareOutputDirectory(outputDirectory);
		IGameObject root = ResolveRoot(filter);
		IUnityObjectBase[] hierarchy = root.FetchHierarchy().OfType<IUnityObjectBase>().ToArray();
		GlbFallbackTextureCatalog catalog;
		IReadOnlyList<GlbFallbackTextureRejection> rejections;
		if (fallbackTextures is null)
		{
			catalog = GlbFallbackTextureCatalog.Empty;
			rejections = [];
		}
		else
		{
			catalog = GlbFallbackTextureCatalog.Create(
				fallbackTextures.Textures.Select(static item => new GlbFallbackTextureSource(item.Key, item.Path)),
				out rejections);
		}
		string safeName = SafeFileName($"{root.GetBestName()}_{root.PathID}", $"character_{root.PathID}");
		string path = Path.Combine(directory, safeName + ".glb");
		List<GlbTypeTreeFallbackDiagnostic> typeTreeFallbackDiagnostics = [];
		bool writerSucceeded;
		string? errorMessage;
		using (FileStream stream = File.Create(path))
		{
			writerSucceeded = GlbWriter.TryWrite(
				GlbLevelBuilder.Build(hierarchy, isScene: false, GameFileLoader.GameBundle.FetchAssets(), catalog, typeTreeFallbackDiagnostics),
				stream,
				out errorMessage);
		}
		if (!writerSucceeded)
		{
			return new GlbExportResult(false, path, safeName, File.Exists(path), rejections, typeTreeFallbackDiagnostics, errorMessage, GameFileLoader.ProcessingIssues.ToArray());
		}
		bool sourceAccepted = !typeTreeFallbackDiagnostics.Any(static item => !item.Accepted);
		string qualityReason = string.Empty;
		bool qualityAccepted = sourceAccepted && GlbQualityGate.TryValidate(path, out qualityReason);
		if (!qualityAccepted)
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
			errorMessage = sourceAccepted ? qualityReason : "A TypeTree renderer was rejected by the source fidelity gate.";
			return new GlbExportResult(false, path, safeName, false, rejections, typeTreeFallbackDiagnostics, errorMessage, GameFileLoader.ProcessingIssues.ToArray());
		}
		return new GlbExportResult(true, path, safeName, true, rejections, typeTreeFallbackDiagnostics, null, GameFileLoader.ProcessingIssues.ToArray());
	}

	public BatchProcessResult BatchProcess(string outputDirectory, string? filter, bool raw, bool fbx, bool includeAnimations = true, bool verifiedOnly = false, PremiumFallbackTextureCatalog? fallbackTextures = null, bool deterministic = false, bool glb = false)
	{
		EnsureLoaded();
		string directory = PrepareOutputDirectory(outputDirectory);
		List<string> files = [];
		List<GlbCharacterDecision> glbDecisions = [];
		HashSet<(long PathId, string Collection)> eligibleAssets = verifiedOnly
			? CreateVerifiedOnlyPlan().Decisions.Where(static decision => decision.IsEligible).Select(static decision => (decision.Candidate.PathId, decision.Candidate.CollectionName)).ToHashSet()
			: [];
		int skippedAssetCount = 0;
		if (raw)
		{
			string rawDirectory = Path.Combine(directory, "raw");
			Directory.CreateDirectory(rawDirectory);
				foreach (AssetSummary asset in ListAllAssets(filter))
			{
				IUnityObjectBase? resolved = ResolveAsset(asset.PathId, asset.Collection);
				if (resolved is null)
				{
					continue;
				}
				if (verifiedOnly && !eligibleAssets.Contains((resolved.PathID, resolved.Collection.Name)))
				{
					skippedAssetCount++;
					continue;
				}
				string rawPath = Path.Combine(rawDirectory, CreateRawAssetFileName(asset));
				File.WriteAllText(rawPath, ToRawJson(resolved), new UTF8Encoding(false));
				files.Add(rawPath);
			}
		}
		if (fbx)
		{
			foreach (IGameObject root in FindCharacterRoots(filter))
			{
				if (verifiedOnly && root.FetchHierarchy().OfType<IUnityObjectBase>().Any(asset => !eligibleAssets.Contains((asset.PathID, asset.Collection.Name))))
				{
					skippedAssetCount++;
					continue;
				}
				FbxAsciiExporter exporter = new() { IncludeAnimations = includeAnimations };
					string safeName = SafeFileName($"{root.GetBestName()}_{root.PathID}", $"character_{root.PathID}");
				string path = Path.Combine(directory, safeName + ".fbx");
				if (exporter.Export(exporter.GetCharacterAssets(root, GameFileLoader.GameBundle.FetchAssets()), path, LocalFileSystem.Instance))
				{
					files.Add(path);
				}
			}
		}
		if (glb)
		{
			IUnityObjectBase[] animationCandidates = GameFileLoader.GameBundle.FetchAssets()
				.Where(static asset => asset is IAnimationClip or IAnimatorController)
				.ToArray();
			foreach (IGameObject root in FindCharacterRoots(filter))
			{
				string safeName = SafeFileName($"{root.GetBestName()}_{root.PathID}", $"character_{root.PathID}");
				string path = Path.Combine(directory, safeName + ".glb");
				List<GlbTypeTreeFallbackDiagnostic> typeTreeDiagnostics = [];
				using (FileStream stream = File.Create(path))
				{
					if (!GlbWriter.TryWrite(GlbLevelBuilder.Build(root.FetchHierarchy().OfType<IUnityObjectBase>(), false, animationCandidates, GlbFallbackTextureCatalog.Empty, typeTreeDiagnostics), stream, out string? error))
					{
						glbDecisions.Add(new GlbCharacterDecision(root.GetBestName(), root.PathID, false, null, error ?? "GLB writer failed.", typeTreeDiagnostics));
						continue;
					}
				}
				bool sourceAccepted = !typeTreeDiagnostics.Any(static item => !item.Accepted);
				string qualityReason = string.Empty;
				bool qualityAccepted = sourceAccepted && GlbQualityGate.TryValidate(path, out qualityReason);
				string reason = qualityAccepted
					? "accepted"
					: sourceAccepted
						? qualityReason
						: "A TypeTree renderer was rejected by the source fidelity gate.";
				if (reason != "accepted")
				{
					File.Delete(path);
						glbDecisions.Add(new GlbCharacterDecision(root.GetBestName(), root.PathID, false, null, reason, typeTreeDiagnostics));
					continue;
				}
				files.Add(path);
					glbDecisions.Add(new GlbCharacterDecision(root.GetBestName(), root.PathID, true, path, "accepted", typeTreeDiagnostics));
			}
		}
		string manifestPath = Path.Combine(directory, "assetripper-batch-manifest.json");
		ProcessingIssue[] issues = GameFileLoader.ProcessingIssues.ToArray();
		object manifest = deterministic
			? new { raw, fbx, glb, includeAnimations, verifiedOnly, skippedAssetCount, fallbackTextures, files, glbDecisions, issues }
			: new { generatedUtc = DateTimeOffset.UtcNow, raw, fbx, glb, includeAnimations, verifiedOnly, skippedAssetCount, fallbackTextures, files, glbDecisions, issues };
		File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, JsonOptions), new UTF8Encoding(false));
		return new BatchProcessResult(directory, files.ToArray(), manifestPath, raw, fbx, glb, includeAnimations, verifiedOnly, skippedAssetCount, fallbackTextures, glbDecisions, issues);
	}

	public string ToRawJson(IUnityObjectBase asset)
	{
		StringWriter writer = new(CultureInfo.InvariantCulture) { NewLine = "\n" };
		asset.WalkStandard(new DefaultJsonWalker(writer));
		return writer.ToString();
	}

	public string ToRawYaml(IUnityObjectBase asset)
	{
		StringWriter writer = new(CultureInfo.InvariantCulture) { NewLine = "\n" };
		YamlWriter yamlWriter = new();
		yamlWriter.WriteHead(writer);
		yamlWriter.WriteDocument(new YamlWalker().ExportYamlDocument(asset, ExportIdHandler.GetMainExportID(asset)));
		yamlWriter.WriteTail(writer);
		return writer.ToString();
	}

	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

	private void EnsureLoaded()
	{
		if (!GameFileLoader.IsLoaded)
		{
			throw new InvalidOperationException("No Unity data is loaded. Provide input paths first.");
		}
	}

	private IUnityObjectBase? ResolveAsset(long pathId, string collectionName)
	{
		return GameFileLoader.GameBundle.FetchAssets().FirstOrDefault(asset => asset.PathID == pathId && string.Equals(asset.Collection.Name, collectionName, StringComparison.OrdinalIgnoreCase));
	}

	private IGameObject ResolveRoot(string? filter)
	{
		IGameObject[] roots = FindCharacterRoots(filter).ToArray();
		if (roots.Length == 1)
		{
			return roots[0];
		}
		if (roots.Length > 1)
		{
			string candidates = string.Join(", ", roots.Take(8).Select(root => $"{root.GetBestName()} ({root.PathID})"));
			throw new InvalidOperationException($"The filter matched multiple character roots. Choose one explicitly: {candidates}");
		}
		throw new InvalidOperationException(string.IsNullOrWhiteSpace(filter) ? "No character or prefab root was found." : $"No character or prefab root matched '{filter}'.");
	}

	private IEnumerable<IGameObject> FindCharacterRoots(string? filter)
	{
		string[] queries = (filter ?? string.Empty)
			.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
		HashSet<IGameObject> roots = new(ReferenceComparer<IGameObject>.Instance);
		foreach (IGameObject gameObject in GameFileLoader.GameBundle.FetchAssets().OfType<IGameObject>())
		{
			IGameObject root = gameObject.GetRoot();
			if (queries.Length > 0 && !queries.Any(query => Matches(root, query)))
			{
				continue;
			}
			if (root.TryGetComponent<ISkinnedMeshRenderer>(out _) || root.TryGetComponent<IAnimator>(out _) || root.FetchHierarchy().OfType<IComponent>().Any(component => component is ISkinnedMeshRenderer or IMeshFilter or IRenderer))
			{
				roots.Add(root);
			}
		}
		return roots.OrderBy(root => root.GetBestName(), StringComparer.OrdinalIgnoreCase);
	}

	private static bool IsClipForRoot(IAnimationClip clip, IGameObject root)
	{
		try
		{
			foreach (IAnimator animator in root.FetchHierarchy().OfType<IAnimator>())
			{
				if (animator.ContainsAnimationClip(clip))
				{
					return true;
				}
			}
			string rootName = root.GetBestName();
			if (GameFileLoader.GameBundle.FetchAssets().OfType<IAnimatorController>().Any(controller => string.Equals(controller.GetBestName(), rootName, StringComparison.OrdinalIgnoreCase) && controller.ContainsAnimationClip(clip)))
			{
				return true;
			}
			return clip.FindRoots().Any(candidate => ReferenceEquals(candidate.GetRoot(), root));
		}
		catch
		{
			return false;
		}
	}

	private static bool Matches(IUnityObjectBase asset, string query)
	{
		return string.IsNullOrWhiteSpace(query)
			|| asset.GetBestName().Contains(query, StringComparison.OrdinalIgnoreCase)
			|| asset.ClassName.Contains(query, StringComparison.OrdinalIgnoreCase)
			|| asset.PathID.ToString(CultureInfo.InvariantCulture).Equals(query, StringComparison.OrdinalIgnoreCase)
			|| asset.Collection.Name.Contains(query, StringComparison.OrdinalIgnoreCase);
	}

	private static AssetSummary ToAssetSummary(IUnityObjectBase asset) => new(asset.GetBestName(), asset.ClassName, asset.PathID, asset.Collection.Name, asset is IComponent, asset is IGameObject);

	private static MeshInspection ToMeshInspection(IMesh mesh, MeshData data)
	{
		int blendShapeCount = mesh.Has_Shapes() && mesh.Shapes is { } shapes ? shapes.Channels.Count : 0;
		int blendShapeFrameCount = mesh.Has_Shapes() && mesh.Shapes is { } shapeData ? shapeData.Shapes.Count : 0;
		return new(mesh.GetBestName(), mesh.PathID, mesh.Collection.Name, data.Vertices.Length, data.SubMeshes.Length, data.UVCount, data.HasSkin, data.BindPose?.Length ?? 0, data.HasTangents, data.HasNormals, blendShapeCount, blendShapeFrameCount);
	}

	private string PrepareOutputDirectory(string outputDirectory)
	{
		if (string.IsNullOrWhiteSpace(outputDirectory))
		{
			throw new ArgumentException("Output directory is required.", nameof(outputDirectory));
		}
		string fullPath = Path.GetFullPath(outputDirectory);
		if (Path.GetPathRoot(fullPath)?.TrimEnd(Path.DirectorySeparatorChar) == fullPath.TrimEnd(Path.DirectorySeparatorChar))
		{
			throw new InvalidOperationException("Refusing to write exports to a filesystem root.");
		}
		foreach (string input in GameFileLoader.LoadedInputPaths)
		{
			string inputBase = Directory.Exists(input) ? input : Path.GetDirectoryName(input) ?? input;
			if (IsSameOrInside(fullPath, inputBase))
			{
				throw new InvalidOperationException($"Output directory must not be inside an imported path: {inputBase}");
			}
		}
		Directory.CreateDirectory(fullPath);
		return fullPath;
	}

	private static bool IsSameOrInside(string candidate, string basePath)
	{
		string relative = Path.GetRelativePath(Path.GetFullPath(basePath), Path.GetFullPath(candidate));
		return relative == "." || (!Path.IsPathRooted(relative) && relative != ".." && !relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
	}

	private static void ValidateInputFileHeader(string path)
	{
		FileInfo info = new(path);
		if (info.Length < 128)
		{
			throw new InvalidDataException($"Input file is too short to contain a supported Unity header: {path}");
		}
		using FileStream stream = File.OpenRead(path);
		Span<byte> header = stackalloc byte[12];
		if (stream.Read(header) != header.Length)
		{
			throw new InvalidDataException($"Unable to read the Unity header: {path}");
		}
		if (header[..8].SequenceEqual("UnityFS\0"u8) || header[..8].SequenceEqual("UnityRaw"u8) || header[..8].SequenceEqual("UnityWeb"u8))
		{
			int version = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(header[8..12]);
			if (version is < 1 or > 10)
			{
				throw new InvalidDataException($"Unity bundle header version is outside the supported safety range: {path}");
			}
		}
	}

	private static string SafeFileName(string? value, string fallback)
	{
		string candidate = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
		foreach (char invalid in Path.GetInvalidFileNameChars()) candidate = candidate.Replace(invalid, '_');
		return string.IsNullOrWhiteSpace(candidate) ? fallback : candidate;
	}

	public static string CreateRawAssetFileName(AssetSummary asset)
	{
		string collection = SafeFileName(asset.Collection, "collection");
		string assetName = SafeFileName(asset.Name, asset.ClassName);
		return $"{collection}__{assetName}_{asset.PathId}.json";
	}

	private static int CountFiles(string directory) => Directory.Exists(directory) ? Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories).Count() : 0;
}

public sealed record LoadSummary(IReadOnlyList<string> InputFiles, int AssetCount, string UnityVersion, IReadOnlyList<ProcessingIssue> Issues);
public sealed record AssetSummary(string Name, string ClassName, long PathId, string Collection, bool IsComponent, bool IsGameObject);
public sealed record MeshInspection(string Name, long PathId, string Collection, int VertexCount, int SubMeshCount, int UvChannelCount, bool HasSkin, int BindPoseCount, bool HasTangents, bool HasNormals, int BlendShapeCount, int BlendShapeFrameCount);
public sealed record PrefabInspection(AssetSummary Root, IReadOnlyList<AssetSummary> Hierarchy, IReadOnlyList<AssetSummary> Components, IReadOnlyList<MeshInspection> Meshes, IReadOnlyList<AssetSummary> Materials, IReadOnlyList<AssetSummary> Textures, IReadOnlyList<AssetSummary> AnimationClips, int BoneCount, int WeightedMeshCount, int MissingWeightMeshCount, string UnityVersion, IReadOnlyList<ProcessingIssue> Issues);
public sealed record FbxExportResult(bool Success, string Path, string RootName, bool IncludeAnimations, bool FileExists, int FilesWritten, IReadOnlyList<ProcessingIssue> Issues);
public sealed record GlbExportResult(bool Success, string Path, string RootName, bool FileExists, IReadOnlyList<GlbFallbackTextureRejection> FallbackRejections, IReadOnlyList<GlbTypeTreeFallbackDiagnostic> TypeTreeFallbackDiagnostics, string? ErrorMessage, IReadOnlyList<ProcessingIssue> Issues);
public enum PremiumDiagnosticsFormat
{
	Json,
	Html,
}

public sealed record BatchProcessResult(string OutputDirectory, IReadOnlyList<string> Files, string ManifestPath, bool Raw, bool Fbx, bool Glb, bool IncludeAnimations, bool VerifiedOnly, int SkippedAssetCount, PremiumFallbackTextureCatalog? FallbackTextures, IReadOnlyList<GlbCharacterDecision> GlbDecisions, IReadOnlyList<ProcessingIssue> Issues);
public sealed record GlbCharacterDecision(string RootName, long RootPathId, bool Accepted, string? Path, string Reason, IReadOnlyList<GlbTypeTreeFallbackDiagnostic>? TypeTreeFallbackDiagnostics = null);
public sealed record PremiumTextureBatchItem(long PathId, string Name, bool IsSuccess, string? Path, string? Message);
public sealed record PremiumTextureBatchResult(string OutputDirectory, PremiumTextureOutputFormat Format, long SucceededCount, long FailedCount, string ManifestPath, IReadOnlyList<PremiumTextureBatchItem> Items);

internal sealed class ReferenceComparer<T> : IEqualityComparer<T> where T : class
{
	public static ReferenceComparer<T> Instance { get; } = new();
	public bool Equals(T? x, T? y) => ReferenceEquals(x, y);
	public int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
}
