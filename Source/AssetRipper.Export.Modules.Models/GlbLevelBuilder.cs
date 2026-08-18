using AssetRipper.Assets;
using AssetRipper.Assets.Collections;
using AssetRipper.Assets.Generics;
using AssetRipper.Assets.Metadata;
using AssetRipper.Export.Modules.Textures;

using AssetRipper.Import.AssetCreation;

using AssetRipper.Import.Logging;
using AssetRipper.Numerics;
using AssetRipper.Primitives;
using AssetRipper.SourceGenerated.Classes.ClassID_1;
using AssetRipper.SourceGenerated.Classes.ClassID_18;
using AssetRipper.SourceGenerated.Classes.ClassID_2;
using AssetRipper.SourceGenerated.Classes.ClassID_21;
using AssetRipper.SourceGenerated.Classes.ClassID_25;
using AssetRipper.SourceGenerated.Classes.ClassID_28;
using AssetRipper.SourceGenerated.Classes.ClassID_33;
using AssetRipper.SourceGenerated.Classes.ClassID_4;
using AssetRipper.SourceGenerated.Classes.ClassID_43;
using AssetRipper.SourceGenerated.Classes.ClassID_74;
using AssetRipper.SourceGenerated.Classes.ClassID_91;
using AssetRipper.SourceGenerated.Classes.ClassID_95;
using AssetRipper.SourceGenerated.Classes.ClassID_137;
using AssetRipper.SourceGenerated.Extensions;
using AssetRipper.SourceGenerated.Subclasses.PPtr_Material;
using AssetRipper.SourceGenerated.Subclasses.PPtr_Component;
using AssetRipper.SourceGenerated.Subclasses.QuaternionCurve;
using AssetRipper.SourceGenerated.Subclasses.SubMesh;
using AssetRipper.SourceGenerated.Subclasses.UnityTexEnv;
using AssetRipper.SourceGenerated.Subclasses.Vector3Curve;
using AssetRipper.SourceGenerated.Subclasses.Keyframe_Quaternionf;
using AssetRipper.SourceGenerated.Subclasses.Keyframe_Vector3f;
using AssetRipper.TextureDecoder.Rgb.Formats;
using SharpGLTF.Geometry;
using SharpGLTF.Materials;
using SharpGLTF.Memory;
using SharpGLTF.Scenes;
using SharpGLTF.Schema2;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Reflection;
using UnityTextureWrapMode = AssetRipper.SourceGenerated.Enums.TextureWrapMode;

namespace AssetRipper.Export.Modules.Models;

public static class GlbLevelBuilder
{
	public static SceneBuilder Build(IEnumerable<IUnityObjectBase> assets, bool isScene, IEnumerable<IUnityObjectBase>? animationAssets = null, GlbFallbackTextureCatalog? fallbackTextures = null, ICollection<GlbTypeTreeFallbackDiagnostic>? typeTreeFallbackDiagnostics = null)
	{
		IUnityObjectBase[] sourceAssets = assets.ToArray();
		IUnityObjectBase[] animationCandidates = animationAssets?.ToArray() ?? sourceAssets;
		SceneBuilder sceneBuilder = new();
		BuildParameters parameters = new BuildParameters(isScene, fallbackTextures ?? GlbFallbackTextureCatalog.Empty, typeTreeFallbackDiagnostics ?? []);

		HashSet<IUnityObjectBase> exportedAssets = new();
		HashSet<IGameObject> roots = new(ReferenceEqualityComparer.Instance);

		foreach (IUnityObjectBase asset in sourceAssets)
		{
			if (!exportedAssets.Contains(asset) && asset is IGameObject or IComponent)
			{
				IGameObject root = GetRoot(asset);
				roots.Add(root);

				AddGameObjectToScene(sceneBuilder, parameters, null, Transformation.Identity, Transformation.Identity, root.GetTransform(), root.Name.String);

				foreach (IEditorExtension exportedAsset in root.FetchHierarchy())
				{
					exportedAssets.Add(exportedAsset);
				}
			}
		}
		AddAnimationClips(sceneBuilder, parameters, roots, animationCandidates);

		return sceneBuilder;
	}

	private static void AddAnimationClips(SceneBuilder sceneBuilder, BuildParameters parameters, IEnumerable<IGameObject> roots, IEnumerable<IUnityObjectBase> animationCandidates)
	{
		HashSet<IAnimationClip> clips = new(ReferenceEqualityComparer.Instance);
		IAnimatorController[] controllers = animationCandidates.OfType<IAnimatorController>().ToArray();
		foreach (IGameObject root in roots)
		{
			foreach (IAnimationClip clip in animationCandidates.OfType<IAnimationClip>())
			{
				try
				{
					if (IsClipForRoot(clip, root, controllers))
					{
						clips.Add(clip);
					}
				}
				catch
				{
					// A malformed optional clip must not remove the character preview.
				}
			}
		}

			foreach (IAnimationClip clip in clips)
			{
			string track = $"{clip.GetBestName()}::{clip.PathID}";
			foreach (IGameObject root in roots)
			{
				AddVector3Tracks(parameters, root.GetTransform(), clip.PositionCurves_C74, track, isTranslation: true);
				AddVector3Tracks(parameters, root.GetTransform(), clip.ScaleCurves_C74, track, isTranslation: false);
				AddQuaternionTracks(parameters, root.GetTransform(), clip.RotationCurves_C74, track);
			}
			}
		}

	private static bool IsClipForRoot(IAnimationClip clip, IGameObject root, IReadOnlyCollection<IAnimatorController> controllers)
	{
			foreach (IAnimator animator in root.FetchHierarchy().OfType<IAnimator>())
			{
				if (animator.ContainsAnimationClip(clip))
				{
					return true;
			}
		}
		return controllers.Any(controller => string.Equals(controller.GetBestName(), root.GetBestName(), StringComparison.OrdinalIgnoreCase) && controller.ContainsAnimationClip(clip))
			|| clip.FindRoots().Any(candidate => ReferenceEquals(candidate.GetRoot(), root));
	}

		private static void AddVector3Tracks(BuildParameters parameters, ITransform root, IEnumerable<IVector3Curve> curves, string track, bool isTranslation)
	{
		foreach (IVector3Curve curve in curves)
		{
			ITransform? transform = FindTransform(root, curve.Path.String);
			if (transform is null || !parameters.NodeCache.TryGetValue(transform, out NodeBuilder? node))
			{
				continue;
			}
			Dictionary<float, Vector3> keyframes = [];
			foreach (IKeyframe_Vector3f key in curve.Curve.Curve)
			{
				Vector3 value = key.Value.CastToStruct();
				keyframes[key.Time] = isTranslation ? GlbCoordinateConversion.ToGltfVector3Convert(value) : value;
			}
			if (keyframes.Count == 0)
			{
				continue;
			}
			if (isTranslation)
			{
				node.WithLocalTranslation(track, keyframes);
			}
			else
			{
				node.WithLocalScale(track, keyframes);
			}
		}
	}

	private static void AddQuaternionTracks(BuildParameters parameters, ITransform root, IEnumerable<IQuaternionCurve> curves, string track)
	{
		foreach (IQuaternionCurve curve in curves)
		{
			ITransform? transform = FindTransform(root, curve.Path.String);
			if (transform is null || !parameters.NodeCache.TryGetValue(transform, out NodeBuilder? node))
			{
				continue;
			}
			Dictionary<float, Quaternion> keyframes = [];
			foreach (IKeyframe_Quaternionf key in curve.Curve.Curve)
			{
				keyframes[key.Time] = GlbCoordinateConversion.ToGltfQuaternionConvert(key.Value);
			}
			if (keyframes.Count > 0)
			{
				node.WithLocalRotation(track, keyframes);
			}
		}
	}

	private static ITransform? FindTransform(ITransform root, string path)
	{
		if (string.IsNullOrEmpty(path))
		{
			return root;
		}
		string[] parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length > 0 && string.Equals(parts[0], root.GameObject_C4P?.Name.String, StringComparison.Ordinal))
		{
			parts = parts[1..];
		}
		ITransform current = root;
		foreach (string part in parts)
		{
			ITransform? next = current.Children_C4P.WhereNotNull().FirstOrDefault(child => string.Equals(child.GameObject_C4P?.Name.String, part, StringComparison.Ordinal));
			if (next is null)
			{
				return null;
			}
			current = next;
		}
		return current;
	}

	private static void AddGameObjectToScene(SceneBuilder sceneBuilder, BuildParameters parameters, NodeBuilder? parentNode, Transformation parentGlobalTransform, Transformation parentGlobalInverseTransform, ITransform transform, string recoveryIdentity)
	{
		IGameObject? gameObject = transform.GameObject_C4P;
		if (gameObject is null)
		{
			return;
		}

		Transformation localTransform = transform.ToTransformation();
		Transformation localInverseTransform = transform.ToInverseTransformation();
		Transformation globalTransform = localTransform * parentGlobalTransform;
		Transformation globalInverseTransform = parentGlobalInverseTransform * localInverseTransform;

		NodeBuilder node = parentNode is null ? new NodeBuilder(gameObject.Name) : parentNode.CreateNode(gameObject.Name);
		parameters.NodeCache[transform] = node;
		if (parentNode is not null || parameters.IsScene)
		{
			node.LocalTransform = new SharpGLTF.Transforms.AffineTransform(
				transform.LocalScale_C4.CastToStruct(),//Scaling is the same in both coordinate systems
				GlbCoordinateConversion.ToGltfQuaternionConvert(transform.LocalRotation_C4),
				GlbCoordinateConversion.ToGltfVector3Convert(transform.LocalPosition_C4));
		}
		sceneBuilder.AddNode(node);

		foreach (ITransform childTransform in transform.Children_C4P.WhereNotNull())
		{
			AddGameObjectToScene(sceneBuilder, parameters, node, localTransform * parentGlobalTransform, parentGlobalInverseTransform * localInverseTransform, childTransform, recoveryIdentity);
		}

		if (gameObject.TryGetComponent(out ISkinnedMeshRenderer? skinnedRenderer)
			&& skinnedRenderer.MeshP is IMesh skinnedMesh
			&& parameters.TryGetOrMakeMeshData(skinnedMesh, out MeshData skinnedData)
		)
		{
				ITransform[] sourceBones = skinnedRenderer.BonesP.WhereNotNull().ToArray();
				if (SkinnedMeshSanitizer.TrySanitize(skinnedData, sourceBones.Length, out SanitizedSkinData sanitized)
					&& sanitized.SurvivingSourceBoneIndices.Select(index => sourceBones[index]).All(parameters.NodeCache.ContainsKey))
				{
					ITransform[] bones = sanitized.SurvivingSourceBoneIndices.Select(index => sourceBones[index]).ToArray();
					if (sanitized.DroppedBindPoseCount > 0 || sanitized.RootFallbackVertexCount > 0)
					{
						Logger.Warning(LogCategory.Export, $"GLB sanitized skinned mesh '{skinnedMesh.GetBestName()}': droppedBindPoses={sanitized.DroppedBindPoseCount}, skinFallbackRootVertices={sanitized.RootFallbackVertexCount}.");
					}
					AddSkinnedMeshToScene(sceneBuilder, parameters, node, skinnedMesh, sanitized.Mesh, new MaterialList(skinnedRenderer), bones);
			}
				else
				{
					Logger.Warning(LogCategory.Export, $"GLB exported skinned mesh '{skinnedMesh.GetBestName()}' as a rigid mesh because its bone references could not be resolved.");
					AddDynamicMeshToScene(sceneBuilder, parameters, node, skinnedMesh, skinnedData, new MaterialList(skinnedRenderer));
				}
			}
			else if (TryAddRecoveredTypeTreeSkinnedMesh(sceneBuilder, parameters, node, gameObject, recoveryIdentity))
			{
				// The recovered path emitted either a fully validated skinned mesh or an explicit rejection diagnostic.
			}
			else if (gameObject.TryGetComponent(out IMeshFilter? meshFilter)
			&& meshFilter.TryGetMesh(out IMesh? mesh)
			&& parameters.TryGetOrMakeMeshData(mesh, out MeshData meshData)
			&& gameObject.TryGetComponent(out IRenderer? meshRenderer))
		{
			if (ReferencesDynamicMesh(meshRenderer))
			{
				AddDynamicMeshToScene(sceneBuilder, parameters, node, mesh, meshData, new MaterialList(meshRenderer));
			}
			else
			{
				int[] subsetIndices = GetSubsetIndices(meshRenderer, mesh.SubMeshes.Count);
				AddStaticMeshToScene(sceneBuilder, parameters, node, mesh, meshData, subsetIndices, new MaterialList(meshRenderer), globalTransform, globalInverseTransform);
			}
			}
		}

	private static bool TryAddRecoveredTypeTreeSkinnedMesh(SceneBuilder sceneBuilder, BuildParameters parameters, NodeBuilder node, IGameObject gameObject, string recoveryIdentity)
		{
			bool Reject(long rendererPathId, string code, string message, IReadOnlyList<RecoveredAssociationEvidence>? evidence = null, RecoveredAssociationRequirementFacts? requirements = null)
			{
				parameters.TypeTreeFallbackDiagnostics.Add(new GlbTypeTreeFallbackDiagnostic(rendererPathId, false, code, message, evidence ?? [], requirements));
				Logger.Warning(LogCategory.Export, $"GLB TypeTree fallback-rejected for SkinnedMeshRenderer '{rendererPathId}': {message}");
				return true;
			}

			TypeTreeSkinnedMeshRendererData source;
			long rendererPathId;
			string? rejection;
			if (gameObject.TryGetComponent(out ISkinnedMeshRenderer? knownRenderer)
				&& TryResolveKnownRendererTypeTreeMesh(knownRenderer, out TypeTreeObject? knownMesh))
			{
				ITransform[] bones = knownRenderer.BonesP.WhereNotNull().ToArray();
				IMaterial?[] materials = knownRenderer.Materials_C25.Select(pointer => pointer.TryGetAsset(knownRenderer.Collection)).ToArray();
				if (bones.Length == 0 || materials.Any(static material => material is null))
				{
					Logger.Warning(LogCategory.Export, $"GLB TypeTree fallback-rejected for SkinnedMeshRenderer '{knownRenderer.PathID}': the known renderer does not expose a complete bone or material pointer list.");
					return true;
				}
				source = new TypeTreeSkinnedMeshRendererData(knownMesh, bones, materials!);
				rendererPathId = knownRenderer.PathID;
			}
			else if (TryGetRecoveredTypeTreeSkinnedMeshRenderer(gameObject, out TypeTreeObject? renderer))
			{
				rendererPathId = renderer.PathID;
				if (!TypeTreeMeshAdapter.TryReadDeclaredSkinnedMeshRendererReferences(renderer, out TypeTreeSkinnedMeshRendererReferences references, out rejection))
				{
					return Reject(rendererPathId, "renderer-schema-or-pointer", rejection ?? "The recovered renderer does not expose a complete source mesh, bones, and materials set.");
				}
				RecoveredAssociationDecision recoveredMesh = TypeTreeMeshAdapter.RecoverUniqueMeshAssociation(renderer, references, recoveryIdentity);
					if (!recoveredMesh.Accepted || recoveredMesh.Candidate?.Asset is not IUnityObjectBase recoveredMeshAsset)
					{
						return Reject(rendererPathId, recoveredMesh.Code, recoveredMesh.Message, recoveredMesh.Evidence, recoveredMesh.Requirements);
				}
				if (references.Materials.Any(static material => material is not IMaterial))
				{
					return Reject(rendererPathId, "material-type-tree-unavailable", $"The recovered mesh association is unique (PathID={recoveredMeshAsset.PathID}), but its declared materials are not yet readable IMaterial instances ({string.Join(",", references.Materials.Select(static material => material.ClassName))}).");
				}
				// Unity skin indices address the bind-pose domain. A shorter domain is valid only because
				// the resolver proved every vertex index stays inside this declared prefix.
				source = new TypeTreeSkinnedMeshRendererData(recoveredMeshAsset, references.Bones.Take(recoveredMesh.Candidate.BindPoseCount).ToArray(), references.Materials.Cast<IMaterial>().ToArray());
			}
			else
			{
				return false;
			}
			MeshData meshData = default!;
			string? meshDataRejection = null;
			bool hasMeshData = source.Mesh switch
			{
				IMesh mesh => parameters.TryGetOrMakeMeshData(mesh, out meshData),
				TypeTreeObject mesh => TypeTreeMeshAdapter.TryCreateDeclaredMeshData(mesh, out meshData, out meshDataRejection),
				_ => false,
			};
			if (!hasMeshData)
			{
				return Reject(rendererPathId, "mesh-schema-or-payload", meshDataRejection ?? "The source m_Mesh does not expose readable mesh data.");
			}
			if (!meshData.HasSkin || meshData.BindPose is null || source.Bones.Length != meshData.BindPose.Length)
			{
				return Reject(rendererPathId, "bone-bind-pose-mismatch", "The declared bone count does not exactly match the declared bind-pose count.");
			}
			if (source.Materials.Length != meshData.SubMeshes.Length)
			{
				return Reject(rendererPathId, "material-submesh-mismatch", "The declared material count does not match the declared submesh count.");
			}
			if (!source.Bones.All(parameters.NodeCache.ContainsKey))
			{
				return Reject(rendererPathId, "bone-outside-hierarchy", "One or more declared bone Transforms are outside the exported hierarchy.");
			}
			if (!SkinnedMeshSanitizer.TrySanitize(meshData, source.Bones.Length, out SanitizedSkinData sanitized)
				|| sanitized.DroppedBindPoseCount != 0
				|| sanitized.RootFallbackVertexCount != 0)
			{
				return Reject(rendererPathId, "lossy-skin-sanitization", "The declared skin requires a lossy sanitization step.");
			}
			AddSkinnedMeshToScene(sceneBuilder, parameters, node, sanitized.Mesh, source.Materials, source.Bones);
				parameters.TypeTreeFallbackDiagnostics.Add(new GlbTypeTreeFallbackDiagnostic(rendererPathId, true, "accepted", $"vertices={sanitized.Mesh.Vertices.Length}; bones={source.Bones.Length}; submeshes={sanitized.Mesh.SubMeshes.Length}"));
			Logger.Info(LogCategory.Export, $"GLB TypeTree fallback accepted for SkinnedMeshRenderer '{rendererPathId}': vertices={sanitized.Mesh.Vertices.Length}, bones={source.Bones.Length}, submeshes={sanitized.Mesh.SubMeshes.Length}.");
			return true;
		}

		private static bool TryResolveKnownRendererTypeTreeMesh(ISkinnedMeshRenderer renderer, [NotNullWhen(true)] out TypeTreeObject? mesh)
		{
			mesh = null;
			IPPtr pointer = renderer.Mesh;
			if (pointer.PathID == 0 || pointer.FileID < 0)
			{
				return false;
			}
			AssetCollection? collection = pointer.FileID == 0
				? renderer.Collection
				: pointer.FileID < renderer.Collection.Dependencies.Count ? renderer.Collection.Dependencies[pointer.FileID] : null;
			mesh = collection is not null && collection.Assets.TryGetValue(pointer.PathID, out IUnityObjectBase? target)
				? target as TypeTreeObject
				: null;
			return mesh is { ClassID: 43 };
		}

		private static bool TryGetRecoveredTypeTreeSkinnedMeshRenderer(IGameObject gameObject, [NotNullWhen(true)] out TypeTreeObject? renderer)
		{
			foreach (IPPtr_Component componentPointer in gameObject.FetchComponents())
			{
				IUnityObjectBase? component = componentPointer.FileID == 0
					? gameObject.Collection.FirstOrDefault(asset => asset.PathID == componentPointer.PathID)
					: gameObject.Collection.TryGetAsset(componentPointer.FileID, componentPointer.PathID);
				if (component is TypeTreeObject { ClassID: 137 } typeTreeRenderer)
				{
					renderer = typeTreeRenderer;
					return true;
				}
			}
			renderer = null;
			return false;
		}

		private static void AddSkinnedMeshToScene(SceneBuilder sceneBuilder, BuildParameters parameters, NodeBuilder node, IMesh mesh, MeshData meshData, MaterialList materialList, IReadOnlyList<ITransform> bones)
	{
		(ISubMesh, MaterialBuilder)[] subMeshArray = ArrayPool<(ISubMesh, MaterialBuilder)>.Shared.Rent(mesh.SubMeshes.Count);
		for (int i = 0; i < mesh.SubMeshes.Count; i++)
		{
			subMeshArray[i] = (mesh.SubMeshes[i], parameters.GetOrMakeMaterial(materialList[i]));
		}
		ArraySegment<(ISubMesh, MaterialBuilder)> arraySegment = new(subMeshArray, 0, mesh.SubMeshes.Count);
		IMeshBuilder<MaterialBuilder> subMeshBuilder = GlbSubMeshBuilder.BuildSubMeshes(arraySegment, mesh.Is16BitIndices(), meshData, Transformation.Identity, Transformation.Identity);
		NodeBuilder[] joints = bones.Select(bone => parameters.NodeCache[bone]).ToArray();
		sceneBuilder.AddSkinnedMesh(subMeshBuilder, node.WorldMatrix, joints);
			ArrayPool<(ISubMesh, MaterialBuilder)>.Shared.Return(subMeshArray);
		}

		private static void AddSkinnedMeshToScene(SceneBuilder sceneBuilder, BuildParameters parameters, NodeBuilder node, MeshData meshData, IReadOnlyList<IMaterial> materials, IReadOnlyList<ITransform> bones)
		{
			if (materials.Count != meshData.SubMeshes.Length)
			{
				throw new InvalidOperationException("Validated TypeTree skinning requires one resolved material per declared submesh.");
			}
			(SubMeshData, MaterialBuilder)[] subMeshArray = ArrayPool<(SubMeshData, MaterialBuilder)>.Shared.Rent(meshData.SubMeshes.Length);
			try
			{
				for (int index = 0; index < meshData.SubMeshes.Length; index++)
				{
					subMeshArray[index] = (meshData.SubMeshes[index], parameters.GetOrMakeMaterial(materials[index]));
				}
				IMeshBuilder<MaterialBuilder> subMeshBuilder = GlbSubMeshBuilder.BuildSubMeshes(new ArraySegment<(SubMeshData, MaterialBuilder)>(subMeshArray, 0, meshData.SubMeshes.Length), meshData, Transformation.Identity, Transformation.Identity);
				NodeBuilder[] joints = bones.Select(bone => parameters.NodeCache[bone]).ToArray();
				sceneBuilder.AddSkinnedMesh(subMeshBuilder, node.WorldMatrix, joints);
			}
			finally
			{
				ArrayPool<(SubMeshData, MaterialBuilder)>.Shared.Return(subMeshArray);
			}
		}

		private static void AddDynamicMeshToScene(SceneBuilder sceneBuilder, BuildParameters parameters, NodeBuilder node, IMesh mesh, MeshData meshData, MaterialList materialList)
	{
		AccessListBase<ISubMesh> subMeshes = mesh.SubMeshes;
		(ISubMesh, MaterialBuilder)[] subMeshArray = ArrayPool<(ISubMesh, MaterialBuilder)>.Shared.Rent(subMeshes.Count);
		for (int i = 0; i < subMeshes.Count; i++)
		{
			MaterialBuilder materialBuilder = parameters.GetOrMakeMaterial(materialList[i]);
			subMeshArray[i] = (subMeshes[i], materialBuilder);
		}
		ArraySegment<(ISubMesh, MaterialBuilder)> arraySegment = new ArraySegment<(ISubMesh, MaterialBuilder)>(subMeshArray, 0, subMeshes.Count);
		IMeshBuilder<MaterialBuilder> subMeshBuilder = GlbSubMeshBuilder.BuildSubMeshes(arraySegment, mesh.Is16BitIndices(), meshData, Transformation.Identity, Transformation.Identity);
		sceneBuilder.AddRigidMesh(subMeshBuilder, node);
		ArrayPool<(ISubMesh, MaterialBuilder)>.Shared.Return(subMeshArray);
	}

	private static void AddStaticMeshToScene(SceneBuilder sceneBuilder, BuildParameters parameters, NodeBuilder node, IMesh mesh, MeshData meshData, int[] subsetIndices, MaterialList materialList, Transformation globalTransform, Transformation globalInverseTransform)
	{
		(ISubMesh, MaterialBuilder)[] subMeshArray = ArrayPool<(ISubMesh, MaterialBuilder)>.Shared.Rent(subsetIndices.Length);
		AccessListBase<ISubMesh> subMeshes = mesh.SubMeshes;
			for (int i = 0; i < subsetIndices.Length; i++)
			{
				ISubMesh subMesh = subMeshes[subsetIndices[i]];
				MaterialBuilder materialBuilder = parameters.GetOrMakeMaterial(materialList[subsetIndices[i]]);
			subMeshArray[i] = (subMesh, materialBuilder);
		}
		ArraySegment<(ISubMesh, MaterialBuilder)> arraySegment = new ArraySegment<(ISubMesh, MaterialBuilder)>(subMeshArray, 0, subsetIndices.Length);
		IMeshBuilder<MaterialBuilder> subMeshBuilder = GlbSubMeshBuilder.BuildSubMeshes(arraySegment, mesh.Is16BitIndices(), meshData, globalInverseTransform, globalTransform);
		sceneBuilder.AddRigidMesh(subMeshBuilder, node);
		ArrayPool<(ISubMesh, MaterialBuilder)>.Shared.Return(subMeshArray);
	}

	private static IGameObject GetRoot(IUnityObjectBase asset)
	{
		return asset switch
		{
			IGameObject gameObject => gameObject.GetRoot(),
			IComponent component => component.GameObject_C2P!.GetRoot(),
			_ => throw new InvalidOperationException()
		};
	}

	private static bool ReferencesDynamicMesh(IRenderer renderer)
	{
		return renderer.Has_StaticBatchInfo_C25() && renderer.StaticBatchInfo_C25.SubMeshCount == 0
			|| renderer.Has_SubsetIndices_C25() && renderer.SubsetIndices_C25.Count == 0;
	}

		private static int[] GetSubsetIndices(IRenderer renderer, int subMeshCount)
	{
		AccessListBase<IPPtr_Material> materials = renderer.Materials_C25;
		if (renderer.Has_SubsetIndices_C25())
		{
			return renderer.SubsetIndices_C25.Select(i => (int)i).ToArray();
		}
		else if (renderer.Has_StaticBatchInfo_C25())
		{
			return Enumerable.Range(renderer.StaticBatchInfo_C25.FirstSubMesh, renderer.StaticBatchInfo_C25.SubMeshCount).ToArray();
		}
		else
		{
				return Enumerable.Range(0, subMeshCount).ToArray();
		}
	}

	private readonly record struct BuildParameters(
		MaterialBuilder DefaultMaterial,
		Dictionary<ITexture2D, MemoryImage> ImageCache,
		Dictionary<IMaterial, MaterialBuilder> MaterialCache,
		Dictionary<IMesh, MeshData> MeshCache,
		Dictionary<ITransform, NodeBuilder> NodeCache,
		bool IsScene,
		GlbFallbackTextureCatalog FallbackTextures,
		ICollection<GlbTypeTreeFallbackDiagnostic> TypeTreeFallbackDiagnostics)
		{
			public BuildParameters(bool isScene, GlbFallbackTextureCatalog fallbackTextures, ICollection<GlbTypeTreeFallbackDiagnostic> typeTreeFallbackDiagnostics) : this(new MaterialBuilder("DefaultMaterial"), new(), new(), new(), new(ReferenceEqualityComparer.Instance), isScene, fallbackTextures, typeTreeFallbackDiagnostics) { }
		public bool TryGetOrMakeMeshData(IMesh mesh, out MeshData meshData)
		{
			if (MeshCache.TryGetValue(mesh, out meshData))
			{
				return true;
			}
			else if (MeshData.TryMakeFromMesh(mesh, out meshData))
			{
				MeshCache.Add(mesh, meshData);
				return true;
			}
			return false;
		}

		public MaterialBuilder GetOrMakeMaterial(IMaterial? material)
		{
			if (material is null)
			{
				return DefaultMaterial;
			}
			if (!MaterialCache.TryGetValue(material, out MaterialBuilder? materialBuilder))
			{
				materialBuilder = MakeMaterialBuilder(material);
				MaterialCache.Add(material, materialBuilder);
			}
			return materialBuilder;
		}

		public bool TryGetOrMakeImage(ITexture2D texture, out MemoryImage image)
		{
			if (!ImageCache.TryGetValue(texture, out image))
			{
				if (TextureConverter.TryConvertToBitmap(texture, out DirectBitmap bitmap))
				{
					using MemoryStream memoryStream = new();
					bitmap.SaveAsPng(memoryStream);
					image = new MemoryImage(memoryStream.ToArray());
					ImageCache.Add(texture, image);
					return true;
				}
				return false;
			}
			else
			{
				return true;
			}
		}

		private MaterialBuilder MakeMaterialBuilder(IMaterial material)
		{
			MaterialBuilder materialBuilder = new MaterialBuilder(material.Name);
			foreach ((Utf8String utf8Name, IUnityTexEnv textureEnvironment) in material.GetTextureProperties())
			{
				if (!TryMapGlbChannel(utf8Name.String, out KnownChannel channel))
				{
					continue;
				}
					IUnityObjectBase? target = textureEnvironment.Texture.TryGetAsset(material.Collection);
					if (target is ITexture2D texture)
					{
						// A resolved source texture is authoritative. A catalog entry must never overwrite it,
						// even if that source texture cannot be decoded for this export target.
						if (TryGetOrMakeImage(texture, out MemoryImage image))
						{
							BindTexture(materialBuilder, channel, image, textureEnvironment, texture);
						}
					}
					else if (target is null)
					{
						// An explicitly null source property gets only the neutral 1x1 fallback.
						BindTexture(materialBuilder, channel, GetNeutralFallback(channel), textureEnvironment, null);
					}
					else if (FallbackTextures.TryGetUnresolvedImage(utf8Name.String, out MemoryImage fallbackImage))
					{
						// Only a non-texture resolved object is an Unresolved binding eligible for user fallback.
						BindTexture(materialBuilder, channel, fallbackImage, textureEnvironment, null);
					}
					else
					{
						BindTexture(materialBuilder, channel, GetNeutralFallback(channel), textureEnvironment, null);
					}
			}
			return materialBuilder;
		}

		private static void BindTexture(MaterialBuilder materialBuilder, KnownChannel channel, MemoryImage image, IUnityTexEnv textureEnvironment, ITexture2D? sourceTexture)
		{
			TextureBuilder texture = materialBuilder.UseChannel(channel).UseTexture();
			texture.WithPrimaryImage(image);
			texture.WithTransform(ReadTextureVector2(textureEnvironment.Offset), ReadTextureVector2(textureEnvironment.Scale), 0f, null);
			texture.WrapS = GetWrapMode(sourceTexture, useU: true);
			texture.WrapT = GetWrapMode(sourceTexture, useU: false);
		}

		private static TextureWrapMode GetWrapMode(ITexture2D? texture, bool useU)
		{
			if (texture?.TextureSettings_C28 is not { } settings)
			{
				return TextureWrapMode.REPEAT;
			}
			int unityWrapMode = useU ? settings.WrapU : settings.WrapV;
			return (UnityTextureWrapMode)unityWrapMode switch
			{
				UnityTextureWrapMode.Clamp => TextureWrapMode.CLAMP_TO_EDGE,
				UnityTextureWrapMode.Mirror => TextureWrapMode.MIRRORED_REPEAT,
				_ => TextureWrapMode.REPEAT,
			};
		}

		[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "UnityTexEnv vector representations expose stable public X/Y value members.")]
		private static Vector2 ReadTextureVector2(object value)
		{
			Type type = value.GetType();
			return new Vector2(ReadTextureComponent(type, value, "X"), ReadTextureComponent(type, value, "Y"));
		}

		[UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "UnityTexEnv vector representations expose stable public X/Y value members.")]
		private static float ReadTextureComponent([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] Type type, object value, string name)
		{
			object? component = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance)?.GetValue(value)
				?? type.GetField(name, BindingFlags.Public | BindingFlags.Instance)?.GetValue(value);
			return component switch
			{
				float single => single,
				double doubleValue => (float)doubleValue,
				IConvertible convertible => convertible.ToSingle(System.Globalization.CultureInfo.InvariantCulture),
				_ => 0.0f,
			};
		}

		private static bool TryMapGlbChannel(string propertyName, out KnownChannel channel)
		{
			if (propertyName is "_MainTex" or "texture" or "Texture" or "_Texture" or "_BaseMap" or "_BaseColorMap")
			{
				channel = KnownChannel.BaseColor;
				return true;
			}
			if (propertyName.Contains("normal", StringComparison.OrdinalIgnoreCase) || propertyName.Contains("bump", StringComparison.OrdinalIgnoreCase))
			{
				channel = KnownChannel.Normal;
				return true;
			}
			if (propertyName is "_MetallicGlossMap" or "_MetallicRoughnessMap" or "_MaskMap")
			{
				channel = KnownChannel.MetallicRoughness;
				return true;
			}
			channel = default;
			return false;
		}

		private static MemoryImage GetNeutralFallback(KnownChannel channel)
		{
			byte[] rgba = channel == KnownChannel.Normal ? [128, 128, 255, 255] : [255, 255, 255, 255];
			DirectBitmap<ColorRGBA<byte>, byte> bitmap = new(1, 1, 1, rgba);
			using MemoryStream stream = new();
			bitmap.SaveAsPng(stream);
			return new MemoryImage(stream.ToArray());
		}
		}

	public sealed record GlbTypeTreeFallbackDiagnostic(long RendererPathId, bool Accepted, string Code, string Message, IReadOnlyList<RecoveredAssociationEvidence>? Evidence = null, RecoveredAssociationRequirementFacts? Requirements = null);

	private readonly struct MaterialList
	{
		private readonly AccessListBase<IPPtr_Material> materials;
		private readonly AssetCollection file;

		private MaterialList(AccessListBase<IPPtr_Material> materials, AssetCollection file)
		{
			this.materials = materials;
			this.file = file;
		}

		public MaterialList(IRenderer renderer) : this(renderer.Materials_C25, renderer.Collection) { }

		public int Count => materials.Count;

		public IMaterial? this[int index]
		{
			get
			{
				if (index >= materials.Count)
				{
					return null;
				}
				return materials[index].TryGetAsset(file);
			}
		}
	}
}
