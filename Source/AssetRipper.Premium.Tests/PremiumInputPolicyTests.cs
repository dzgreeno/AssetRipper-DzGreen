using AssetRipper.Export.Configuration;
using AssetRipper.Processing.Configuration;
using NUnit.Framework;

namespace AssetRipper.Premium.Tests;

public sealed class PremiumInputPolicyTests
{
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
}
