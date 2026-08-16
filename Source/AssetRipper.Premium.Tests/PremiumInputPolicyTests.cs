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
			Assert.That(report.BackEdgeCount, Is.GreaterThanOrEqualTo(1));
			Assert.That(report.IsTruncated, Is.False);
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
