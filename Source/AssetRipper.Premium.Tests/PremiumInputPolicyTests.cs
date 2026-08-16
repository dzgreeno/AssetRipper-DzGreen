using AssetRipper.Export.Configuration;
using AssetRipper.Processing.Configuration;
using NUnit.Framework;

namespace AssetRipper.Premium.Tests;

public sealed class PremiumInputPolicyTests
{
	[Test]
	public void HalfToSinglePreservesZeroSubnormalInfinityAndNaN()
	{
		Assert.Multiple(() =>
		{
			Assert.That(PremiumGeometryUnpackers.HalfToSingle(0x0000), Is.EqualTo(0.0f));
			Assert.That(PremiumGeometryUnpackers.HalfToSingle(0x3C00), Is.EqualTo(1.0f));
			Assert.That(PremiumGeometryUnpackers.HalfToSingle(0x0001), Is.EqualTo(5.9604645e-8f).Within(1e-14f));
			Assert.That(float.IsPositiveInfinity(PremiumGeometryUnpackers.HalfToSingle(0x7C00)), Is.True);
			Assert.That(float.IsNaN(PremiumGeometryUnpackers.HalfToSingle(0x7E00)), Is.True);
		});
	}

	[Test]
	public void UnpackSnorm101010RecoversSignedNormalizedChannels()
	{
		uint packed = PackSnorm10(511) | PackSnorm10(0) << 10 | PackSnorm10(-511) << 20;
		System.Numerics.Vector3 value = PremiumGeometryUnpackers.UnpackSnorm101010(packed);

		Assert.Multiple(() =>
		{
			Assert.That(value.X, Is.EqualTo(1.0f));
			Assert.That(value.Y, Is.EqualTo(0.0f));
			Assert.That(value.Z, Is.EqualTo(-1.0f));
		});
	}

	[Test]
	public void UnpackSmallestThreeQuaternionProducesCanonicalIdentity()
	{
		uint packed = 3u << 30;
		System.Numerics.Quaternion value = PremiumGeometryUnpackers.UnpackSmallestThreeQuaternion(packed);

		Assert.Multiple(() =>
		{
			Assert.That(value.X, Is.EqualTo(0.0f).Within(1e-6f));
			Assert.That(value.Y, Is.EqualTo(0.0f).Within(1e-6f));
			Assert.That(value.Z, Is.EqualTo(0.0f).Within(1e-6f));
			Assert.That(value.W, Is.EqualTo(1.0f).Within(1e-6f));
		});
	}

	[Test]
	public void ReferenceGraphReportsCyclesAndMissingTargets()
	{
		PremiumReferenceLink[] links =
		[
			new("A", "child", "B", PremiumReferenceResolution.Resolved),
			new("B", "parent", "A", PremiumReferenceResolution.Resolved),
			new("B", "material", null, PremiumReferenceResolution.MissingAsset),
			new("C", "optional", null, PremiumReferenceResolution.Null),
		];
		PremiumReferenceGraphReport report = PremiumReferenceGraphAnalyzer.Analyze(["A", "B", "C"], links);

		Assert.Multiple(() =>
		{
			Assert.That(report.NodeCount, Is.EqualTo(3));
			Assert.That(report.EdgeCount, Is.EqualTo(4));
			Assert.That(report.ResolvedEdgeCount, Is.EqualTo(2));
			Assert.That(report.MissingAssetCount, Is.EqualTo(1));
			Assert.That(report.NullReferenceCount, Is.EqualTo(1));
			Assert.That(report.CycleComponentCount, Is.EqualTo(1));
			Assert.That(report.CyclicNodeCount, Is.EqualTo(2));
			Assert.That(report.IsTruncated, Is.False);
		});
	}

	[Test]
	public void TypeTreeCoverageClassifiesOnlySupportedEvidence()
	{
		PremiumTypeTreeObservation[] observations =
		[
			new("A.assets", "A", 10, true, 3, 0, 0),
			new("B.assets", "B", 10, true, 3, 1, 0),
			new("C.assets", "C", 10, false, 0, 0, 0),
			new("D.assets", "D", 0, false, 0, 0, 0),
		];
		PremiumTypeTreeCoverageReport report = PremiumTypeTreeCoverageAnalyzer.Analyze(observations);

		Assert.Multiple(() =>
		{
			Assert.That(report.CollectionCount, Is.EqualTo(4));
			Assert.That(report.EmbeddedCollectionCount, Is.EqualTo(1));
			Assert.That(report.PartialCollectionCount, Is.EqualTo(1));
			Assert.That(report.KnownEngineSchemaCollectionCount, Is.EqualTo(1));
			Assert.That(report.UnavailableCollectionCount, Is.EqualTo(1));
			Assert.That(report.Collections.Select(static coverage => coverage.State), Is.EqualTo(new[]
			{
				PremiumTypeTreeCoverageState.Embedded,
				PremiumTypeTreeCoverageState.Partial,
				PremiumTypeTreeCoverageState.KnownEngineSchema,
				PremiumTypeTreeCoverageState.Unavailable,
			}));
		});
	}

	[Test]
	public void MaterialBindingInventoryTotalsReadableProperties()
	{
		PremiumMaterialBinding[] materials =
		[
			new("sharedassets0.assets", 1, "Hero", [
				new("_MainTex", 10, "Hero_Diffuse", 1, 1, 0, 0, PremiumTextureBindingStatus.Resolved),
				new("_BumpMap", null, null, 1, 1, 0, 0, PremiumTextureBindingStatus.Null),
			]),
			new("sharedassets0.assets", 2, "Prop", [
				new("_Mask", 20, "Texture3D", 1, 1, 0, 0, PremiumTextureBindingStatus.Unresolved),
			]),
		];
		PremiumMaterialBindingReport report = PremiumMaterialBindingAnalyzer.Analyze(materials);

		Assert.Multiple(() =>
		{
			Assert.That(report.MaterialCount, Is.EqualTo(2));
			Assert.That(report.TextureBindingCount, Is.EqualTo(3));
			Assert.That(report.ResolvedTextureBindingCount, Is.EqualTo(1));
			Assert.That(report.UnresolvedTextureBindingCount, Is.EqualTo(1));
			Assert.That(report.NullTextureBindingCount, Is.EqualTo(1));
		});
	}

	[Test]
	public void ExplicitPackedNormalDecoderUsesVerifiedSnormUnpacker()
	{
		uint packed = PackSnorm10(511) | PackSnorm10(0) << 10 | PackSnorm10(-511) << 20;
		byte[] bytes = BitConverter.GetBytes(packed);

		bool decoded = PremiumVertexStreamProcessor.TryDecodeExplicitSnorm1010102(bytes, 1, 0, 4, false, out System.Numerics.Vector3[]? normals, out PremiumVertexStreamIssue? issue);

		Assert.Multiple(() =>
		{
			Assert.That(decoded, Is.True);
			Assert.That(issue, Is.Null);
			Assert.That(normals, Is.Not.Null);
			Assert.That(normals![0], Is.EqualTo(new System.Numerics.Vector3(1, 0, -1)));
		});
	}

	[Test]
	public void ExplicitPackedNormalDecoderRejectsTruncatedStride()
	{
		bool decoded = PremiumVertexStreamProcessor.TryDecodeExplicitSnorm1010102(new byte[3], 1, 0, 4, false, out _, out PremiumVertexStreamIssue? issue);

		Assert.Multiple(() =>
		{
			Assert.That(decoded, Is.False);
			Assert.That(issue?.Code, Is.EqualTo(PremiumVertexIssueCode.InvalidLayout));
		});
	}

	[Test]
	public void AnimationSamplerUsesSmallestThreeAndInterpolatesAtRequestedRate()
	{
		uint identity = 3u << 30;
		bool decoded = PremiumAnimationStreamProcessor.TryDecodeExplicitSmallestThree(BitConverter.GetBytes(identity), 1, 0, 4, false, out PremiumQuaternionKey[]? decodedKeys, out PremiumAnimationStreamIssue? decodeIssue);
		PremiumQuaternionTrack track = new("Root", [
			new(0, System.Numerics.Quaternion.Identity),
			new(1, System.Numerics.Quaternion.CreateFromAxisAngle(System.Numerics.Vector3.UnitY, MathF.PI)),
		]);
		PremiumQuaternionSampleResult samples = PremiumAnimationStreamProcessor.Sample(track, 2);

		Assert.Multiple(() =>
		{
			Assert.That(decoded, Is.True);
			Assert.That(decodeIssue, Is.Null);
			Assert.That(decodedKeys![0].Value, Is.EqualTo(System.Numerics.Quaternion.Identity));
			Assert.That(samples.IsSuccess, Is.True);
			Assert.That(samples.Keys.Select(static key => key.Time), Is.EqualTo(new[] { 0f, 0.5f, 1f }));
			Assert.That(samples.Keys[1].Value.Length(), Is.EqualTo(1f).Within(1e-5f));
		});
	}

	[Test]
	public void EmptyBundleDiagnosticIsDeterministic()
	{
		AssetRipper.Assets.Bundles.GameBundle gameBundle = new();
		PremiumImportDiagnosticReport report = PremiumImportDiagnostics.Create(gameBundle, gameBundle.GetMaxUnityVersion(), ["missing.bundle"]);

		Assert.Multiple(() =>
		{
			Assert.That(report.AssetCollectionCount, Is.Zero);
			Assert.That(report.ResourceFileCount, Is.Zero);
			Assert.That(report.FailedFileCount, Is.Zero);
			Assert.That(report.VertexStreams.MeshCount, Is.Zero);
			Assert.That(report.VertexStreams.IssueCount, Is.Zero);
			Assert.That(report.Inputs, Has.Count.EqualTo(1));
			Assert.That(report.ImportStatus, Is.EqualTo("No importer-quarantined files were recorded."));
		});
	}

	[Test]
	public void HighFidelityProfilePreservesResolvedSourceRelationships()
	{
		FullConfiguration configuration = new();
		configuration.ProcessingSettings.EnablePrefabOutlining = false;
		configuration.ProcessingSettings.EnableStaticMeshSeparation = true;
		configuration.ProcessingSettings.EnableAssetDeduplication = true;
		configuration.ProcessingSettings.BundledAssetsExportMode = BundledAssetsExportMode.GroupByAssetType;
		configuration.ExportSettings.PreferOriginalTextureExtension = false;
		configuration.ExportSettings.ExportUnreadableAssets = true;

		PremiumRecoveryProfile.Apply(configuration);
		PremiumRecoveryProfileSnapshot profile = PremiumRecoveryProfile.Capture(configuration);

		Assert.Multiple(() =>
		{
			Assert.That(profile.PrefabOutliningEnabled, Is.True);
			Assert.That(profile.StaticMeshSeparationEnabled, Is.False);
			Assert.That(profile.AssetDeduplicationEnabled, Is.False);
			Assert.That(profile.BundledAssetsExportMode, Is.EqualTo(BundledAssetsExportMode.DirectExport));
			Assert.That(profile.PreferOriginalTextureExtension, Is.True);
			Assert.That(profile.ExportUnreadableAssets, Is.False);
		});
	}

	[Test]
	public void AcceptsAuthorizedPlaintextUnityBundle()
	{
		PremiumInputAssessment assessment = PremiumInputPolicy.Assess(new("character.bundle", PremiumInputKind.UnityBundle, IsUserAuthorized: true));

		Assert.That(assessment.IsAccepted, Is.True);
		Assert.That(assessment.Code, Is.EqualTo("plaintext-supported"));
	}

	[Test]
	public void RejectsUnrecognizedPlaintextFormat()
	{
		PremiumInputAssessment assessment = PremiumInputPolicy.Assess(new("payload.bin", PremiumInputKind.Unknown, IsUserAuthorized: true));

		Assert.That(assessment.IsAccepted, Is.False);
		Assert.That(assessment.Code, Is.EqualTo("unsupported-format"));
	}

	[TestCase(false, true, false, false, "authorization-required")]
	[TestCase(true, true, false, false, "encrypted-input-not-supported")]
	[TestCase(true, false, true, false, "runtime-memory-not-supported")]
	[TestCase(true, false, false, true, "custom-vfs-not-supported")]
	public void RejectsOutOfPolicyInputs(bool authorized, bool encrypted, bool memoryDump, bool customContainer, string expectedCode)
	{
		PremiumInputAssessment assessment = PremiumInputPolicy.Assess(new("input.bin", PremiumInputKind.UnityBundle, authorized, encrypted, IsRuntimeMemoryDump: memoryDump, UsesCustomVirtualContainer: customContainer));

		Assert.That(assessment.IsAccepted, Is.False);
		Assert.That(assessment.Code, Is.EqualTo(expectedCode));
	}

	private static uint PackSnorm10(int value) => unchecked((uint)value) & 0x03FF;
}
