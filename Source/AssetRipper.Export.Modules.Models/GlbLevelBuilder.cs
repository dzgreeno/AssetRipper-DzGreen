using AssetRipper.Assets;
using AssetRipper.Assets.Collections;
using AssetRipper.Assets.Generics;
using AssetRipper.Export.Modules.Textures;
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
using AssetRipper.SourceGenerated.Classes.ClassID_137;
using AssetRipper.SourceGenerated.Extensions;
using AssetRipper.SourceGenerated.Subclasses.PPtr_Material;
using AssetRipper.SourceGenerated.Subclasses.QuaternionCurve;
using AssetRipper.SourceGenerated.Subclasses.SubMesh;
using AssetRipper.SourceGenerated.Subclasses.UnityTexEnv;
using AssetRipper.SourceGenerated.Subclasses.Vector3Curve;
using AssetRipper.SourceGenerated.Subclasses.Keyframe_Quaternionf;
using AssetRipper.SourceGenerated.Subclasses.Keyframe_Vector3f;
using SharpGLTF.Geometry;
using SharpGLTF.Materials;
using SharpGLTF.Memory;
using SharpGLTF.Scenes;
using System.Buffers;
using System.Numerics;

namespace AssetRipper.Export.Modules.Models;

public static class GlbLevelBuilder
{
	public static SceneBuilder Build(IEnumerable<IUnityObjectBase> assets, bool isScene)
	{
		IUnityObjectBase[] sourceAssets = assets.ToArray();
		SceneBuilder sceneBuilder = new();
		BuildParameters parameters = new BuildParameters(isScene);

		HashSet<IUnityObjectBase> exportedAssets = new();
		HashSet<IGameObject> roots = new(ReferenceEqualityComparer.Instance);

		foreach (IUnityObjectBase asset in sourceAssets)
		{
			if (!exportedAssets.Contains(asset) && asset is IGameObject or IComponent)
			{
				IGameObject root = GetRoot(asset);
				roots.Add(root);

				AddGameObjectToScene(sceneBuilder, parameters, null, Transformation.Identity, Transformation.Identity, root.GetTransform());

				foreach (IEditorExtension exportedAsset in root.FetchHierarchy())
				{
					exportedAssets.Add(exportedAsset);
				}
			}
		}
		AddAnimationClips(sceneBuilder, parameters, roots);

		return sceneBuilder;
	}

	private static void AddAnimationClips(SceneBuilder sceneBuilder, BuildParameters parameters, IEnumerable<IGameObject> roots)
	{
		HashSet<IAnimationClip> clips = new(ReferenceEqualityComparer.Instance);
		foreach (IGameObject root in roots)
		{
			foreach (IAnimationClip clip in root.Collection.Bundle.GetRoot().FetchAssets().OfType<IAnimationClip>())
			{
				try
				{
					if (clip.FindRoots().Any(candidate => ReferenceEquals(candidate.GetRoot(), root)))
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

		private static void AddGameObjectToScene(SceneBuilder sceneBuilder, BuildParameters parameters, NodeBuilder? parentNode, Transformation parentGlobalTransform, Transformation parentGlobalInverseTransform, ITransform transform)
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
			AddGameObjectToScene(sceneBuilder, parameters, node, localTransform * parentGlobalTransform, parentGlobalInverseTransform * localInverseTransform, childTransform);
		}

		if (gameObject.TryGetComponent(out ISkinnedMeshRenderer? skinnedRenderer)
			&& skinnedRenderer.MeshP is IMesh skinnedMesh
			&& skinnedMesh.IsSet()
			&& parameters.TryGetOrMakeMeshData(skinnedMesh, out MeshData skinnedData)
			&& skinnedData.HasSkin)
		{
			ITransform[] bones = skinnedRenderer.BonesP.WhereNotNull().ToArray();
			if (bones.Length > 0 && bones.All(parameters.NodeCache.ContainsKey))
			{
				AddSkinnedMeshToScene(sceneBuilder, parameters, node, skinnedMesh, skinnedData, new MaterialList(skinnedRenderer), bones);
			}
		}
		else if (gameObject.TryGetComponent(out IMeshFilter? meshFilter)
			&& meshFilter.TryGetMesh(out IMesh? mesh)
			&& mesh.IsSet()
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
			bool IsScene)
		{
			public BuildParameters(bool isScene) : this(new MaterialBuilder("DefaultMaterial"), new(), new(), new(), new(ReferenceEqualityComparer.Instance), isScene) { }
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
			GetTextures(material, out ITexture2D? mainTexture, out ITexture2D? normalTexture);
			if (mainTexture is not null && TryGetOrMakeImage(mainTexture, out MemoryImage mainImage))
			{
				materialBuilder.WithBaseColor(mainImage);
			}
			if (normalTexture is not null && TryGetOrMakeImage(normalTexture, out MemoryImage normalImage))
			{
				materialBuilder.WithNormal(normalImage);
			}
			return materialBuilder;
		}

		private static void GetTextures(IMaterial material, out ITexture2D? mainTexture, out ITexture2D? normalTexture)
		{
			mainTexture = null;
			normalTexture = null;
			ITexture2D? mainReplacement = null;
			foreach ((Utf8String utf8Name, IUnityTexEnv textureParameter) in material.GetTextureProperties())
			{
				string name = utf8Name.String;
				if (IsMainTexture(name))
				{
					mainTexture ??= textureParameter.Texture.TryGetAsset(material.Collection) as ITexture2D;
				}
				else if (IsNormalTexture(name))
				{
					normalTexture ??= textureParameter.Texture.TryGetAsset(material.Collection) as ITexture2D;
				}
				else
				{
					mainReplacement ??= textureParameter.Texture.TryGetAsset(material.Collection) as ITexture2D;
				}
			}
			mainTexture ??= mainReplacement;
		}

		private static bool IsMainTexture(string textureName)
		{
			return textureName is "_MainTex" or "texture" or "Texture" or "_Texture";
		}

		private static bool IsNormalTexture(string textureName)
		{
			return textureName is "_Normal" or "Normal" or "normal";
		}
	}

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
