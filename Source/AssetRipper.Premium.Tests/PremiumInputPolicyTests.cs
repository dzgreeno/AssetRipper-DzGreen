using AssetRipper.Export.Configuration;
using AssetRipper.Export.Modules.Models;
using AssetRipper.IO.Endian;
using AssetRipper.IO.Files.ResourceFiles;
using AssetRipper.Processing.Configuration;
using AssetRipper.Primitives;
using AssetRipper.SourceGenerated.Extensions;
using AssetRipper.SourceGenerated.Subclasses.ChannelInfo;
using AssetRipper.SourceGenerated.Subclasses.StreamInfo;
using AssetRipper.Tools.Common;
using NUnit.Framework;
using System.Buffers.Binary;
using System.Numerics;
using System.Text;

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
	public void UserAuthorizedUnityBundleIsAcceptedWithoutEnvironmentAttestation()
	{
		PremiumInputDescriptor descriptor = new("android", PremiumInputKind.UnityBundle, IsUserAuthorized: true, IsEncrypted: false, IsRuntimeMemoryDump: false, UsesCustomVirtualContainer: false);
		PremiumInputAssessment assessment = PremiumInputPolicy.Assess(descriptor);
		Assert.Multiple(() =>
		{
			Assert.That(assessment.IsAccepted, Is.True);
			Assert.That(assessment.Code, Is.EqualTo("plaintext-supported"));
		});
	}

	[TestCase("A7x9Q2", true)]
	[TestCase("abc123", true)]
	[TestCase("abc12", false)]
	[TestCase("abc-12", false)]
	public void EnterpriseRecoveryTokenValidationRequiresSixAlphanumericCharacters(string token, bool expected)
	{
		Assert.That(EnterpriseAccessGate.IsValidToken(token), Is.EqualTo(expected));
	}

	[Test]
	public void DiagnosticSessionCannotUseTier1ReadableDataProfile()
	{
		EnterpriseAccessSession session = new(EnterpriseAccessMode.DiagnosticOnly, "recovery-token-unavailable", "test");
		Assert.That(() => EnterpriseAccessGate.RequireTier1ReadableData(session), Throws.TypeOf<Tier1AuthorizationRequiredException>());
	}

	[Test]
	public void SkinSanitizerDropsZeroBindPoseAndRemapsWeights()
	{
		MeshData source = new(
			[Vector3.Zero], null, null, null, null, null, null, null, null, null, null, null,
			[new AssetRipper.Numerics.BoneWeight4(0.25f, 0.75f, 0f, 0f, 0, 1, 0, 0)],
			[default, Matrix4x4.Identity], [], []);

		Assert.That(SkinnedMeshSanitizer.TrySanitize(source, 2, out SanitizedSkinData sanitized), Is.True);
		Assert.Multiple(() =>
		{
			Assert.That(sanitized.SurvivingSourceBoneIndices, Is.EqualTo(new[] { 1 }));
			Assert.That(sanitized.DroppedBindPoseCount, Is.EqualTo(1));
			Assert.That(sanitized.Mesh.BindPose, Has.Length.EqualTo(1));
			Assert.That(sanitized.Mesh.Skin![0].Index0, Is.EqualTo(0));
			Assert.That(sanitized.Mesh.Skin![0].Weight0, Is.EqualTo(1f));
		});
	}

	[Test]
	public void VertexStreamProcessorReadsFinalChannelPayloadWithoutRequiringTrailingStridePadding()
	{
		ChannelInfo[] channels =
		[
			new ChannelInfo { Stream = 0, Offset = 0, Format = 0, Dimension = 3 },
		];
		IStreamInfo[] streams =
		[
			new StreamInfo_4 { ChannelMask = 1, Offset = 0, Stride_Byte = 16 },
		];
		VertexDataBlob blob = new(channels, streams, new byte[28], 2, UnityVersion.Parse("2020.1.0f1"), EndianType.LittleEndian);

		PremiumVertexStreamResult result = PremiumVertexStreamProcessor.Process(blob);

		Assert.Multiple(() =>
		{
			Assert.That(result.Positions, Has.Length.EqualTo(2));
			Assert.That(result.Issues.Any(static issue => issue.Semantic == PremiumVertexSemantic.Position && issue.Code == PremiumVertexIssueCode.InvalidLayout), Is.False);
		});
	}

	[Test]
	public void RecoveredAssociationAcceptsExactlyOneFullyCompatibleMesh()
	{
		RecoveredAssociationDecision result = RecoveredAssociationResolver.SelectUniqueMesh(
		[
			new(11, "incomplete", 4356, 20193, true, false, 25, -1, 1, true),
			new(12, "candidate", 4356, 20193, true, true, 25, 24, 1, true),
		],
		declaredBoneCount: 25,
		declaredMaterialCount: 1);

		Assert.Multiple(() =>
		{
			Assert.That(result.Accepted, Is.True);
			Assert.That(result.Code, Is.EqualTo("recovered-association"));
			Assert.That(result.Candidate!.PathID, Is.EqualTo(12));
			Assert.That(result.Evidence, Has.Count.EqualTo(2));
		});
	}

	[Test]
	public void RecoveredAssociationRejectsAmbiguousOrSkinIncompatibleCandidates()
	{
		RecoveredAssociationDecision ambiguous = RecoveredAssociationResolver.SelectUniqueMesh(
		[
			new(10, "first", 32, 96, true, true, 2, 1, 1, true),
			new(20, "second", 32, 96, true, true, 2, 1, 1, true),
		],
		declaredBoneCount: 2,
		declaredMaterialCount: 1);
		RecoveredAssociationDecision incompatible = RecoveredAssociationResolver.SelectUniqueMesh(
		[
			new(30, "wrong-bind-pose", 32, 96, true, true, 3, 2, 1, true),
		],
		declaredBoneCount: 2,
		declaredMaterialCount: 1);

		Assert.Multiple(() =>
		{
			Assert.That(ambiguous.Accepted, Is.False);
			Assert.That(ambiguous.Code, Is.EqualTo("ambiguous-candidates"));
			Assert.That(incompatible.Accepted, Is.False);
			Assert.That(incompatible.Code, Is.EqualTo("no-unique-candidate"));
			Assert.That(incompatible.Evidence.Single().Message, Does.Contain("bind-pose count"));
		});
	}

	[Test]
	public void RecoveredAssociationAcceptsUsedBindPosePrefixAndRejectsOutOfPrefixWeights()
	{
		RecoveredAssociationDecision prefix = RecoveredAssociationResolver.SelectUniqueMesh(
		[
			new(40, "prefix", 32, 96, true, true, 35, 34, 1, true),
		],
		declaredBoneCount: 53,
		declaredMaterialCount: 1);
		RecoveredAssociationDecision outsidePrefix = RecoveredAssociationResolver.SelectUniqueMesh(
		[
			new(50, "outside", 32, 96, true, true, 35, 35, 1, true),
		],
		declaredBoneCount: 53,
		declaredMaterialCount: 1);

		Assert.Multiple(() =>
		{
			Assert.That(prefix.Accepted, Is.True);
			Assert.That(prefix.Candidate!.BindPoseCount, Is.EqualTo(35));
			Assert.That(outsidePrefix.Accepted, Is.False);
			Assert.That(outsidePrefix.Evidence.Single().Message, Does.Contain("bind-pose prefix"));
		});
	}

	[Test]
	public void RecoveredAssociationRequiresRendererAabbExtentMatchWhenDeclared()
	{
		RecoveredAssociationDecision result = RecoveredAssociationResolver.SelectUniqueMesh(
		[
			new(60, "wrong-aabb", 32, 96, true, true, 2, 1, 1, true, MatchesRendererBounds: false, BoundsCenterDistance: 0.41f, BoundsExtentDistance: 0.18f),
			new(61, "matching-aabb", 32, 96, true, true, 2, 1, 1, true, MatchesRendererBounds: true),
		],
		declaredBoneCount: 2,
		declaredMaterialCount: 1,
		requireRendererBoundsMatch: true);

		Assert.Multiple(() =>
		{
			Assert.That(result.Accepted, Is.True);
			Assert.That(result.Candidate!.PathID, Is.EqualTo(61));
			Assert.That(result.Evidence.Single(evidence => evidence.CandidatePathID == 60).Message, Does.Contain("renderer AABB"));
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
	public void HierarchyAnalyzerSortsAndQuarantinesCycleWorldMatrices()
	{
		PremiumHierarchyNode[] nodes =
		[
			new("Root", null, ["Child"], System.Numerics.Matrix4x4.CreateTranslation(1, 0, 0), false),
			new("Child", "Root", [], System.Numerics.Matrix4x4.CreateTranslation(0, 2, 0), false),
			new("CycleA", "CycleB", ["CycleB"], System.Numerics.Matrix4x4.Identity, false),
			new("CycleB", "CycleA", ["CycleA", "CycleChild"], System.Numerics.Matrix4x4.Identity, false),
			new("CycleChild", "CycleB", [], System.Numerics.Matrix4x4.Identity, true),
		];
		PremiumHierarchyReport report = PremiumHierarchyReconstructor.Analyze(nodes.Reverse());
		PremiumHierarchyNodeResult child = report.Nodes.Single(static node => node.Id == "Child");
		PremiumHierarchyNodeResult cycleChild = report.Nodes.Single(static node => node.Id == "CycleChild");

		Assert.Multiple(() =>
		{
			Assert.That(report.TransformCount, Is.EqualTo(5));
			Assert.That(report.RectTransformCount, Is.EqualTo(1));
			Assert.That(report.CycleComponentCount, Is.EqualTo(1));
			Assert.That(report.CyclicNodeCount, Is.EqualTo(2));
			Assert.That(child.WorldMatrix?.Translation, Is.EqualTo(new System.Numerics.Vector3(1, 2, 0)));
			Assert.That(cycleChild.IsCyclic, Is.False);
			Assert.That(cycleChild.HasCyclicAncestor, Is.True);
			Assert.That(cycleChild.WorldMatrix, Is.Null);
		});
	}

	[Test]
	public void PrefabOverrideResolverDoesNotInventUnknownProperties()
	{
		PremiumPrefabPropertyResolution resolution = PremiumPrefabOverrideResolver.Resolve(
			new Dictionary<string, string?> { ["m_LocalPosition.x"] = "1" },
			[
				new("A", "m_LocalPosition.x", "2"),
				new("A", "m_MissingScript.field", "value"),
			]);

		Assert.Multiple(() =>
		{
			Assert.That(resolution.EffectiveProperties["m_LocalPosition.x"], Is.EqualTo("2"));
			Assert.That(resolution.EffectiveProperties.ContainsKey("m_MissingScript.field"), Is.False);
			Assert.That(resolution.UnresolvedOverrides, Has.Count.EqualTo(1));
		});
	}

	[Test]
	public void VerifiedOnlyPlanAcceptsOnlyCompleteCoverageStates()
	{
		PremiumTypeTreeCoverageReport coverage = PremiumTypeTreeCoverageAnalyzer.Analyze(
		[
			new("A", "A", 1, true, 1, 0, 0),
			new("B", "B", 1, true, 1, 1, 0),
			new("C", "C", 0, false, 0, 0, 0),
		]);
		PremiumVerifiedOnlyPlan plan = PremiumExportOrchestrator.CreateVerifiedOnlyPlan(
			coverage,
			[
				new("A", "A", 1, "verified", "Mesh"),
				new("B", "B", 2, "partial", "Mesh"),
				new("C", "C", 3, "unavailable", "Mesh"),
				new("Missing", "Missing", 4, "missing", "Mesh"),
			]);

		Assert.Multiple(() =>
		{
			Assert.That(plan.EligibleAssetCount, Is.EqualTo(1));
			Assert.That(plan.SkippedAssetCount, Is.EqualTo(3));
			Assert.That(plan.Decisions.Single(static item => item.Candidate.Name == "partial").CoverageState, Is.EqualTo(PremiumTypeTreeCoverageState.Partial));
			Assert.That(plan.Decisions.Single(static item => item.Candidate.Name == "missing").Reason, Does.Contain("no TypeTree"));
		});
	}

	[Test]
	public void BlendTreeEvaluatorUsesExplicitFiniteInputsOnly()
	{
		PremiumBlendTreeWeightResult oneDimensional = PremiumBlendTreeEvaluator.Evaluate1D(
			[
				new("Idle", 0),
				new("Run", 10),
			],
			2.5f);
		PremiumBlendTreeWeightResult twoDimensional = PremiumBlendTreeEvaluator.EvaluateInverseDistance2D(
			[
				new("Forward", new System.Numerics.Vector2(0, 1)),
				new("Right", new System.Numerics.Vector2(1, 0)),
			],
			new System.Numerics.Vector2(0, 1));
		PremiumBlendTreeWeightResult rejected = PremiumBlendTreeEvaluator.Evaluate1D([], float.NaN);

		Assert.Multiple(() =>
		{
			Assert.That(oneDimensional.IsSuccess, Is.True);
			Assert.That(oneDimensional.Weights.Select(static weight => weight.Weight), Is.EqualTo(new[] { 0.75f, 0.25f }));
			Assert.That(twoDimensional.Weights, Is.EqualTo([new PremiumBlendTreeWeight("Forward", 1)]));
			Assert.That(rejected.IsSuccess, Is.False);
		});
	}

	[Test]
	public void ShaderPropertyInjectorUsesOnlyDeclaredStandardMappings()
	{
		PremiumMaterialBindingReport materials = PremiumMaterialBindingAnalyzer.Analyze(
		[
			new PremiumMaterialBinding("A", 1, "Hero", [
				new PremiumTextureBinding("_MainTex", 11, "Albedo", 1, 1, 0, 0, PremiumTextureBindingStatus.Resolved),
				new PremiumTextureBinding("_BumpMap", null, null, 1, 1, 0, 0, PremiumTextureBindingStatus.Null),
				new PremiumTextureBinding("_GameSpecificMap", 12, "Opaque", 1, 1, 0, 0, PremiumTextureBindingStatus.Resolved),
			]),
		]);
		PremiumShaderInjectionReport report = PremiumShaderPropertyInjector.Create(materials, PremiumShaderTarget.UrpLit);
		PremiumShaderPropertyAssignment[] assignments = report.Materials.Single().Assignments.ToArray();

		Assert.Multiple(() =>
		{
			Assert.That(assignments.Single(static assignment => assignment.SourceProperty == "_MainTex").TargetProperty, Is.EqualTo("_BaseMap"));
			Assert.That(assignments.Single(static assignment => assignment.SourceProperty == "_BumpMap").Status, Is.EqualTo(PremiumShaderAssignmentStatus.NeutralFallbackRequired));
			Assert.That(assignments.Single(static assignment => assignment.SourceProperty == "_GameSpecificMap").Status, Is.EqualTo(PremiumShaderAssignmentStatus.NotMapped));
		});
	}

	[Test]
	public void GlbFallbackCatalogAcceptsValidImageOnlyAndPreservesCanonicalFirstEntry()
	{
		string directory = Path.Combine(Path.GetTempPath(), $"assetripper-premium-glb-fallback-{Guid.NewGuid():N}");
		Directory.CreateDirectory(directory);
		try
		{
			string validPath = Path.Combine(directory, "valid.png");
			string invalidPath = Path.Combine(directory, "invalid.png");
			File.WriteAllBytes(validPath, Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVQIHWP4z8DwHwAFgAI/ScLZZwAAAABJRU5ErkJggg=="));
			File.WriteAllBytes(invalidPath, [1, 2, 3, 4]);

			GlbFallbackTextureCatalog catalog = GlbFallbackTextureCatalog.Create(
			[
				new("MainTex", validPath),
				new("_MainTex", invalidPath),
				new("BumpMap", invalidPath),
			],
			out IReadOnlyList<GlbFallbackTextureRejection> rejections);

			Assert.Multiple(() =>
			{
				Assert.That(catalog.TryGetUnresolvedImage("_MainTex", out _), Is.True);
				Assert.That(catalog.TryGetUnresolvedImage("_BumpMap", out _), Is.False);
				Assert.That(rejections, Has.Count.EqualTo(2));
				Assert.That(rejections.Select(static item => item.Key), Does.Contain("BumpMap"));
				Assert.That(rejections.Select(static item => item.Key), Does.Contain("MainTex"));
			});
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}

	[Test]
	public void TextureSchemaMetadataReportsExposedValuesOrUnknownWithoutGuessing()
	{
		PremiumTextureSchemaMetadata exposed = PremiumTextureTranscoder.FromExposedSchema(4, true);
		PremiumTextureSchemaMetadata notExposed = PremiumTextureTranscoder.FromExposedSchema(null, null);

		Assert.Multiple(() =>
		{
			Assert.That(exposed.MipStatus, Is.EqualTo(PremiumTextureMipStatus.Exposed));
			Assert.That(exposed.ExposedMipCount, Is.EqualTo(4));
			Assert.That(exposed.ColorSpace, Is.EqualTo(PremiumTextureColorSpace.Srgb));
			Assert.That(notExposed.MipStatus, Is.EqualTo(PremiumTextureMipStatus.NotExposed));
			Assert.That(notExposed.ExposedMipCount, Is.Null);
			Assert.That(notExposed.ColorSpace, Is.EqualTo(PremiumTextureColorSpace.Unknown));
		});
	}

	[Test]
	public void AudioNormalizationPreservesWavAndRejectsUnsupportedRelabeling()
	{
		PremiumAudioNormalizationResult wav = PremiumAudioMediaProcessor.TryNormalizeAudio([1, 2, 3], "wav", PremiumAudioOutputFormat.Wav);
		PremiumAudioNormalizationResult ogg = PremiumAudioMediaProcessor.TryNormalizeAudio([79, 103, 103, 83], "ogg", PremiumAudioOutputFormat.Ogg);
		PremiumAudioNormalizationResult rejected = PremiumAudioMediaProcessor.TryNormalizeAudio([1, 2, 3], "mp3", PremiumAudioOutputFormat.Wav);

		Assert.Multiple(() =>
		{
			Assert.That(wav.IsSuccess, Is.True);
			Assert.That(wav.Extension, Is.EqualTo("wav"));
			Assert.That(ogg.IsSuccess, Is.True);
			Assert.That(ogg.Extension, Is.EqualTo("ogg"));
			Assert.That(rejected.IsSuccess, Is.False);
			Assert.That(rejected.Message, Does.Contain("neither WAV nor OGG"));
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
			Assert.That(report.Hierarchy.TransformCount, Is.Zero);
			Assert.That(report.PrefabOverrides.ExposedModificationFieldCount, Is.Zero);
			Assert.That(report.Mecanim.ControllerCount, Is.Zero);
			Assert.That(report.Media.AudioClipCount, Is.Zero);
			Assert.That(report.Media.VideoClipCount, Is.Zero);
			Assert.That(report.Textures.TextureCount, Is.Zero);
			Assert.That(report.StandardShaderPlan.MaterialCount, Is.Zero);
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

	[TestCase("characters.resS", PremiumInputKind.ResourceStream)]
	[TestCase("characters.streaming", PremiumInputKind.ResourceStream)]
	[TestCase("resources.assets", PremiumInputKind.SerializedFile)]
	[TestCase("CAB-82e4aa35e2772775fa43714c41018c4f", PremiumInputKind.UnityBundle)]
	[TestCase("globalgamemanagers", PremiumInputKind.SerializedFile)]
	[TestCase("unknown-payload.bin", PremiumInputKind.Unknown)]
	public void ClassifiesRecognizedUnityAndStreamingCompanionFiles(string path, PremiumInputKind expectedKind)
	{
		Assert.That(PremiumInputFileClassifier.Classify(path), Is.EqualTo(expectedKind));
	}

	[Test]
	public void AuthorizedStreamingCompanionIsAcceptedByInputPolicy()
	{
		PremiumInputKind kind = PremiumInputFileClassifier.Classify("mesh.resS");
		PremiumInputAssessment assessment = PremiumInputPolicy.Assess(new("mesh.resS", kind, IsUserAuthorized: true));

		Assert.Multiple(() =>
		{
			Assert.That(kind, Is.EqualTo(PremiumInputKind.ResourceStream));
			Assert.That(assessment.IsAccepted, Is.True);
			Assert.That(assessment.Code, Is.EqualTo("plaintext-supported"));
		});
	}

	[Test]
	public void InputCompletenessSeparatesUnityAssetsStreamingCompanionsAndUnknownFiles()
	{
		using ResourceFile resource = new(new byte[] { 1 }, "/fixtures/characters.resS", "characters.resS");
		PremiumInputCompletenessReport report = PremiumInputCompletenessAnalyzer.Analyze(
		[
			"characters.bundle",
			"resources.assets",
			"characters.resS",
			"unknown-payload.bin",
		],
		[resource]);

		Assert.Multiple(() =>
		{
			Assert.That(report.InputPathCount, Is.EqualTo(4));
			Assert.That(report.UnityBundleCount, Is.EqualTo(1));
			Assert.That(report.SerializedFileCount, Is.EqualTo(1));
			Assert.That(report.ResourceStreamCount, Is.EqualTo(1));
			Assert.That(report.UnclassifiedFileCount, Is.EqualTo(1));
			Assert.That(report.ImporterConfirmedResourceFileCount, Is.EqualTo(1));
			Assert.That(report.ImporterConfirmedResourceNames, Is.EqualTo(new[] { "characters.resS" }));
			Assert.That(report.Entries.Select(static entry => entry.Kind), Does.Contain(PremiumInputKind.ResourceStream));
		});
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

	[Test]
	public void GlbQualityGateAcceptsStaticMeshWithVerifiedPositionBounds()
	{
		string path = WriteSyntheticGlb("""{"asset":{"version":"2.0"},"accessors":[{"count":3,"min":[0,0,0],"max":[1,1,1]}],"meshes":[{"primitives":[{"attributes":{"POSITION":0}}]}]}""");
		try
		{
			Assert.That(GlbQualityGate.TryValidate(path, out string reason), Is.True, reason);
		}
		finally
		{
			File.Delete(path);
		}
	}

	[Test]
	public void GlbQualityGateRejectsSkinnedPrimitiveWithMissingWeights()
	{
		string path = WriteSyntheticGlb("""{"asset":{"version":"2.0"},"accessors":[{"count":3,"min":[0,0,0],"max":[1,1,1]},{"count":3}],"meshes":[{"primitives":[{"attributes":{"POSITION":0,"JOINTS_0":1}}]}],"nodes":[{"mesh":0,"skin":0},{}],"skins":[{"joints":[1]}]}""");
		try
		{
			Assert.Multiple(() =>
			{
				Assert.That(GlbQualityGate.TryValidate(path, out string reason), Is.False);
				Assert.That(reason, Does.Contain("JOINTS_0 and WEIGHTS_0"));
			});
		}
		finally
		{
			File.Delete(path);
		}
	}

	[Test]
	public void GlbQualityGateAcceptsSkinnedPrimitiveWithCompleteSkinData()
	{
		string path = WriteSyntheticGlb("""{"asset":{"version":"2.0"},"accessors":[{"count":3,"min":[0,0,0],"max":[1,1,1]},{"count":3},{"count":3},{"count":1}],"meshes":[{"primitives":[{"attributes":{"POSITION":0,"JOINTS_0":1,"WEIGHTS_0":2}}]}],"nodes":[{"mesh":0,"skin":0},{}],"skins":[{"joints":[1],"inverseBindMatrices":3}]}""");
		try
		{
			Assert.That(GlbQualityGate.TryValidate(path, out string reason), Is.True, reason);
		}
		finally
		{
			File.Delete(path);
		}
	}

	[Test]
	public void RecoveredAssociationEvidenceIncludesCandidateProvenanceOnRejection()
	{
		RecoveredMeshCandidate candidate = new(
			PathID: 77,
			Name: "BodyCandidate",
			VertexCount: 120,
			IndexCount: 360,
			HasPosition: true,
			HasSkin: true,
			BindPoseCount: 3,
			MaxReferencedBoneIndex: 2,
			SubMeshCount: 1,
			HasNonZeroBounds: true,
			CollectionPath: "characters/body.bundle",
			MatchesRendererBounds: false,
			BoundsCenterDistance: 0.25f,
			BoundsExtentDistance: 0.5f);

		RecoveredAssociationDecision decision = RecoveredAssociationResolver.SelectUniqueMesh([candidate], declaredBoneCount: 3, declaredMaterialCount: 1, requireRendererBoundsMatch: true);

		Assert.Multiple(() =>
		{
			Assert.That(decision.Accepted, Is.False);
			Assert.That(decision.Evidence, Has.Count.EqualTo(1));
			Assert.That(decision.Evidence[0].Facts, Is.Not.Null);
			Assert.That(decision.Evidence[0].Facts!.CollectionPath, Is.EqualTo("characters/body.bundle"));
			Assert.That(decision.Evidence[0].Facts!.VertexCount, Is.EqualTo(120));
			Assert.That(decision.Evidence[0].Facts!.BoundsExtentDistance, Is.EqualTo(0.5f));
			Assert.That(decision.Evidence[0].Message, Does.Contain("renderer AABB"));
			Assert.That(decision.Requirements, Is.EqualTo(new RecoveredAssociationRequirementFacts(3, 1, true)));
		});
	}

	[Test]
	public void GlbFallbackDiagnosticPreservesAssociationRequirements()
	{
		RecoveredAssociationRequirementFacts requirements = new(53, 1, true);
		GlbLevelBuilder.GlbTypeTreeFallbackDiagnostic diagnostic = new(9, false, "no-unique-candidate", "Rejected", [], requirements);

		Assert.That(diagnostic.Requirements, Is.EqualTo(requirements));
	}

	[TestCase(1024L, 0UL, 1024U, true)]
	[TestCase(1024L, 1024UL, 0U, true)]
	[TestCase(1024L, 1024UL, 1U, false)]
	[TestCase(1024L, 900UL, 125U, false)]
	public void DeclaredResourceRangeValidationRejectsOutOfBoundsStreams(long length, ulong offset, uint size, bool expected)
	{
		bool actual = TypeTreeMeshAdapter.IsDeclaredResourceRangeValid(length, offset, size, out string? rejection);

		Assert.That(actual, Is.EqualTo(expected));
		Assert.That(rejection is null, Is.EqualTo(expected));
	}

	[TestCase((byte)0, true)]
	[TestCase((byte)4, true)]
	[TestCase((byte)5, false)]
	public void DeclaredTypeTreeChannelDimensionsAllowDisabledChannels(byte dimension, bool expected)
	{
		Assert.That(TypeTreeMeshAdapter.IsDeclaredChannelDimensionSupported(dimension), Is.EqualTo(expected));
	}

	[Test]
	public void RawAssetFileNamesRetainCollectionIdentity()
	{
		AssetSummary first = new("Mesh", "Mesh", 42, "cab-first", false, false);
		AssetSummary second = new("Mesh", "Mesh", 42, "cab-second", false, false);

		Assert.That(AssetRipperToolService.CreateRawAssetFileName(first), Is.EqualTo("cab-first__Mesh_42.json"));
		Assert.That(AssetRipperToolService.CreateRawAssetFileName(second), Is.EqualTo("cab-second__Mesh_42.json"));
		Assert.That(AssetRipperToolService.CreateRawAssetFileName(first), Is.Not.EqualTo(AssetRipperToolService.CreateRawAssetFileName(second)));
	}

	private static string WriteSyntheticGlb(string json)
	{
		byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
		int paddedLength = (jsonBytes.Length + 3) & ~3;
		if (paddedLength < 1030)
		{
			paddedLength = 1032;
		}
		byte[] file = new byte[12 + 8 + paddedLength];
		BinaryPrimitives.WriteUInt32LittleEndian(file, 0x46546C67);
		BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(4), 2);
		BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(8), (uint)file.Length);
		BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(12), (uint)paddedLength);
		BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(16), 0x4E4F534A);
		jsonBytes.CopyTo(file.AsSpan(20));
		file.AsSpan(20 + jsonBytes.Length, paddedLength - jsonBytes.Length).Fill((byte)' ');
		string path = Path.Combine(Path.GetTempPath(), $"assetripper-glb-gate-{Guid.NewGuid():N}.glb");
		File.WriteAllBytes(path, file);
		return path;
	}

	private static uint PackSnorm10(int value) => unchecked((uint)value) & 0x03FF;
}
