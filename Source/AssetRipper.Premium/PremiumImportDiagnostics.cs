using AssetRipper.Assets.Bundles;
using AssetRipper.Primitives;

namespace AssetRipper.Premium;

/// <summary>
/// Produces a deterministic post-import inventory for an authorized plaintext Unity input.
/// The report only summarizes material that the normal importer has already loaded; it does not
/// inspect process memory, acquire keys, or attempt to decode protected containers.
/// </summary>
public static class PremiumImportDiagnostics
{
	private const int MaxClassSummaries = 32;

	public static PremiumImportDiagnosticReport Create(GameBundle gameBundle, UnityVersion projectVersion, IReadOnlyList<string> inputPaths)
	{
		ArgumentNullException.ThrowIfNull(gameBundle);
		ArgumentNullException.ThrowIfNull(inputPaths);
		PremiumReferenceGraphReport referenceGraph = PremiumReferenceGraphAnalyzer.Create(gameBundle);
		PremiumTypeTreeCoverageReport typeTreeCoverage = PremiumTypeTreeCoverageAnalyzer.Create(gameBundle);
		PremiumMaterialBindingReport materialBindings = PremiumMaterialBindingAnalyzer.Create(gameBundle);
		PremiumVertexStreamDiagnostics vertexStreams = PremiumVertexStreamProcessor.CreateDiagnostics(gameBundle);
		PremiumHierarchyReport hierarchy = PremiumHierarchyReconstructor.Create(gameBundle);
		PremiumPrefabOverrideReport prefabOverrides = PremiumPrefabOverrideResolver.Create(gameBundle);
		PremiumMecanimReport mecanim = PremiumMecanimStateMachineAnalyzer.Create(gameBundle);
		PremiumMediaReport media = PremiumAudioMediaProcessor.CreateDiagnostics(gameBundle);
		PremiumTextureTranscodeReport textures = PremiumTextureTranscoder.CreateDiagnostics(gameBundle);
		PremiumShaderInjectionReport standardShaderPlan = PremiumShaderPropertyInjector.Create(materialBindings, PremiumShaderTarget.UrpLit);

		PremiumAssetClassSummary[] classes = gameBundle.FetchAssets()
			.GroupBy(static asset => asset.ClassName, StringComparer.Ordinal)
			.Select(static group => new PremiumAssetClassSummary(group.Key, group.LongCount(), IsRecoveryPriority(group.Key)))
			.OrderByDescending(static summary => summary.Count)
			.ThenBy(static summary => summary.ClassName, StringComparer.Ordinal)
			.Take(MaxClassSummaries)
			.ToArray();

		return new PremiumImportDiagnosticReport(
			projectVersion.ToString(),
			inputPaths.Select(CreateInputSummary).OrderBy(static summary => summary.Path, StringComparer.OrdinalIgnoreCase).ToArray(),
			gameBundle.FetchAssetCollections().LongCount(),
			gameBundle.FetchResourceFiles().LongCount(),
			CountFailedFiles(gameBundle),
			referenceGraph,
			typeTreeCoverage,
			materialBindings,
			vertexStreams,
			hierarchy,
			prefabOverrides,
			mecanim,
			media,
			textures,
			standardShaderPlan,
			classes.Sum(static summary => summary.Count),
			classes,
			gameBundle.AnyFailed ? "Some files were quarantined by the normal importer. Review failed-file diagnostics before export." : "No importer-quarantined files were recorded.");
	}

	private static PremiumInputPathSummary CreateInputSummary(string path)
	{
		string fullPath = TryGetFullPath(path);
		bool isDirectory = Directory.Exists(fullPath);
		return new PremiumInputPathSummary(fullPath, isDirectory, isDirectory || File.Exists(fullPath));
	}

	private static long CountFailedFiles(Bundle root)
	{
		long total = 0;
		Stack<Bundle> pending = new();
		pending.Push(root);
		while (pending.TryPop(out Bundle? bundle))
		{
			total += bundle.FailedFiles.Count;
			foreach (Bundle child in bundle.Bundles)
			{
				pending.Push(child);
			}
		}
		return total;
	}

	private static bool IsRecoveryPriority(string className) => className is "Mesh" or "SkinnedMeshRenderer" or "SpriteAtlas" or "Texture2D" or "AnimationClip" or "AudioClip";

	private static string TryGetFullPath(string path)
	{
		try
		{
			return Path.GetFullPath(path);
		}
		catch (Exception)
		{
			return path;
		}
	}
}

public sealed record PremiumImportDiagnosticReport(
	string UnityVersion,
	IReadOnlyList<PremiumInputPathSummary> Inputs,
	long AssetCollectionCount,
	long ResourceFileCount,
	long FailedFileCount,
	PremiumReferenceGraphReport ReferenceGraph,
	PremiumTypeTreeCoverageReport TypeTreeCoverage,
	PremiumMaterialBindingReport MaterialBindings,
	PremiumVertexStreamDiagnostics VertexStreams,
	PremiumHierarchyReport Hierarchy,
	PremiumPrefabOverrideReport PrefabOverrides,
	PremiumMecanimReport Mecanim,
	PremiumMediaReport Media,
	PremiumTextureTranscodeReport Textures,
	PremiumShaderInjectionReport StandardShaderPlan,
	long ClassifiedAssetCount,
	IReadOnlyList<PremiumAssetClassSummary> AssetClasses,
	string ImportStatus);

public sealed record PremiumInputPathSummary(string Path, bool IsDirectory, bool Exists);

public sealed record PremiumAssetClassSummary(string ClassName, long Count, bool IsRecoveryPriority);
