using AssetRipper.Assets;
using AssetRipper.Assets.Bundles;
using AssetRipper.Export.Configuration;
using AssetRipper.Export.PrimaryContent;
using AssetRipper.Export.PrimaryContent.Models;
using AssetRipper.Export.UnityProjects;
using AssetRipper.GUI.Web;
using AssetRipper.IO.Files;
using AssetRipper.Processing;
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
using AssetRipper.SourceGenerated.Classes.ClassID_95;
using AssetRipper.SourceGenerated.Classes.ClassID_137;
using AssetRipper.SourceGenerated.Extensions;
using AssetRipper.SourceGenerated.Subclasses.PPtr_Material;
using AssetRipper.SourceGenerated.Subclasses.UnityTexEnv;
using AssetRipper.Yaml;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace AssetRipper.Tools.Common;

public sealed class AssetRipperToolService
{
	private const int DefaultAssetLimit = 2000;

	public bool IsLoaded => GameFileLoader.IsLoaded;

	public LoadSummary Load(IEnumerable<string> inputPaths, ModelExportFormat modelFormat = ModelExportFormat.Fbx)
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
		}

		GameFileLoader.ConfigureAutomation(modelFormat);
		GameFileLoader.LoadAndProcess(paths);
		return new LoadSummary(
			GameFileLoader.LoadedInputPaths.Select(Path.GetFileName).Where(name => name is not null).Cast<string>().ToArray(),
			GameFileLoader.GameBundle.FetchAssets().Count(),
			GameFileLoader.CurrentGameData.ProjectVersion.ToString());
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
			GameFileLoader.CurrentGameData.ProjectVersion.ToString());
	}

	public FbxExportResult ExportFbxWithAnimation(string? filter, string outputDirectory, bool includeAnimations = true)
	{
		EnsureLoaded();
		string directory = PrepareOutputDirectory(outputDirectory);
		IGameObject root = ResolveRoot(filter);
		FbxAsciiExporter exporter = new() { IncludeAnimations = includeAnimations };
		string safeName = SafeFileName(root.GetBestName(), $"character_{root.PathID}");
		string path = Path.Combine(directory, safeName + ".fbx");
		bool success = exporter.Export(exporter.GetCharacterAssets(root), path, LocalFileSystem.Instance);
		return new FbxExportResult(success, path, safeName, includeAnimations, File.Exists(path), CountFiles(directory));
	}

	public BatchProcessResult BatchProcess(string outputDirectory, string? filter, bool raw, bool fbx, bool includeAnimations = true)
	{
		EnsureLoaded();
		string directory = PrepareOutputDirectory(outputDirectory);
		List<string> files = [];
		if (raw)
		{
			string rawDirectory = Path.Combine(directory, "raw");
			Directory.CreateDirectory(rawDirectory);
			foreach (AssetSummary asset in ListAssets(filter, DefaultAssetLimit))
			{
				IUnityObjectBase? resolved = ResolveAsset(asset.PathId, asset.Collection);
				if (resolved is null)
				{
					continue;
				}
				string rawPath = Path.Combine(rawDirectory, $"{SafeFileName(asset.Name, asset.ClassName)}_{asset.PathId}.json");
				File.WriteAllText(rawPath, ToRawJson(resolved), new UTF8Encoding(false));
				files.Add(rawPath);
			}
		}
		if (fbx)
		{
			foreach (IGameObject root in FindCharacterRoots(filter))
			{
				FbxAsciiExporter exporter = new() { IncludeAnimations = includeAnimations };
				string safeName = SafeFileName(root.GetBestName(), $"character_{root.PathID}");
				string path = Path.Combine(directory, safeName + ".fbx");
				if (exporter.Export(exporter.GetCharacterAssets(root), path, LocalFileSystem.Instance))
				{
					files.Add(path);
				}
			}
		}
		string manifestPath = Path.Combine(directory, "assetripper-batch-manifest.json");
		File.WriteAllText(manifestPath, JsonSerializer.Serialize(new { generatedUtc = DateTimeOffset.UtcNow, raw, fbx, includeAnimations, files }, JsonOptions), new UTF8Encoding(false));
		return new BatchProcessResult(directory, files.ToArray(), manifestPath, raw, fbx, includeAnimations);
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
		IGameObject? root = FindCharacterRoots(filter).FirstOrDefault();
		if (root is not null)
		{
			return root;
		}
		throw new InvalidOperationException(string.IsNullOrWhiteSpace(filter) ? "No character or prefab root was found." : $"No character or prefab root matched '{filter}'.");
	}

	private IEnumerable<IGameObject> FindCharacterRoots(string? filter)
	{
		string query = filter?.Trim() ?? string.Empty;
		HashSet<IGameObject> roots = new(ReferenceComparer<IGameObject>.Instance);
		foreach (IGameObject gameObject in GameFileLoader.GameBundle.FetchAssets().OfType<IGameObject>())
		{
			if (!string.IsNullOrEmpty(query) && !Matches(gameObject, query) && !Matches(gameObject.GetRoot(), query))
			{
				continue;
			}
			IGameObject root = gameObject.GetRoot();
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
			return clip.FindRoots().Any(candidate => ReferenceEquals(candidate.GetRoot(), root));
		}
		catch
		{
			return string.Equals(clip.Collection, root.Collection);
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

	private static string SafeFileName(string? value, string fallback)
	{
		string candidate = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
		foreach (char invalid in Path.GetInvalidFileNameChars()) candidate = candidate.Replace(invalid, '_');
		return string.IsNullOrWhiteSpace(candidate) ? fallback : candidate;
	}

	private static int CountFiles(string directory) => Directory.Exists(directory) ? Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories).Count() : 0;
}

public sealed record LoadSummary(IReadOnlyList<string> InputFiles, int AssetCount, string UnityVersion);
public sealed record AssetSummary(string Name, string ClassName, long PathId, string Collection, bool IsComponent, bool IsGameObject);
public sealed record MeshInspection(string Name, long PathId, string Collection, int VertexCount, int SubMeshCount, int UvChannelCount, bool HasSkin, int BindPoseCount, bool HasTangents, bool HasNormals, int BlendShapeCount, int BlendShapeFrameCount);
public sealed record PrefabInspection(AssetSummary Root, IReadOnlyList<AssetSummary> Hierarchy, IReadOnlyList<AssetSummary> Components, IReadOnlyList<MeshInspection> Meshes, IReadOnlyList<AssetSummary> Materials, IReadOnlyList<AssetSummary> Textures, IReadOnlyList<AssetSummary> AnimationClips, int BoneCount, int WeightedMeshCount, int MissingWeightMeshCount, string UnityVersion);
public sealed record FbxExportResult(bool Success, string Path, string RootName, bool IncludeAnimations, bool FileExists, int FilesWritten);
public sealed record BatchProcessResult(string OutputDirectory, IReadOnlyList<string> Files, string ManifestPath, bool Raw, bool Fbx, bool IncludeAnimations);

internal sealed class ReferenceComparer<T> : IEqualityComparer<T> where T : class
{
	public static ReferenceComparer<T> Instance { get; } = new();
	public bool Equals(T? x, T? y) => ReferenceEquals(x, y);
	public int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
}
