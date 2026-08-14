using AssetRipper.Assets;
using AssetRipper.Assets.Collections;
using AssetRipper.Export.Modules.Models;
using AssetRipper.Export.Modules.Textures;
using AssetRipper.Import.Logging;
using AssetRipper.IO.Files;
using AssetRipper.Numerics;
using AssetRipper.Processing.Prefabs;
using AssetRipper.SourceGenerated.Classes.ClassID_1;
using AssetRipper.SourceGenerated.Classes.ClassID_2;
using AssetRipper.SourceGenerated.Classes.ClassID_18;
using AssetRipper.SourceGenerated.Classes.ClassID_21;
using AssetRipper.SourceGenerated.Classes.ClassID_25;
using AssetRipper.SourceGenerated.Classes.ClassID_28;
using AssetRipper.SourceGenerated.Classes.ClassID_33;
using AssetRipper.SourceGenerated.Classes.ClassID_43;
using AssetRipper.SourceGenerated.Classes.ClassID_4;
using AssetRipper.SourceGenerated.Classes.ClassID_74;
using AssetRipper.SourceGenerated.Classes.ClassID_90;
using AssetRipper.SourceGenerated.Classes.ClassID_91;
using AssetRipper.SourceGenerated.Classes.ClassID_93;
using AssetRipper.SourceGenerated.Classes.ClassID_95;
using AssetRipper.SourceGenerated.Classes.ClassID_137;
using AssetRipper.SourceGenerated.Extensions;
using AssetRipper.SourceGenerated.Subclasses.BlendShapeData;
using AssetRipper.SourceGenerated.Subclasses.BlendShapeVertex;
using AssetRipper.SourceGenerated.Subclasses.FloatCurve;
using AssetRipper.SourceGenerated.Subclasses.Keyframe_Single;
using AssetRipper.SourceGenerated.Subclasses.MeshBlendShapeChannel;
using AssetRipper.SourceGenerated.Subclasses.SubMesh;
using AssetRipper.SourceGenerated.Subclasses.UnityTexEnv;
using AssetRipper.SourceGenerated.Subclasses.Vector3Curve;
using AssetRipper.SourceGenerated.Subclasses.QuaternionCurve;
using AssetRipper.SourceGenerated.Subclasses.PPtr_Material;
using AssetRipper.Primitives;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Text;

namespace AssetRipper.Export.PrimaryContent.Models;

/// <summary>
/// Standalone FBX ASCII exporter. It deliberately writes a documented, dependency-free
/// FBX 7.4 scene instead of requiring Autodesk native binaries.
/// </summary>
public sealed class FbxAsciiExporter : IContentExtractor
{
	/// <summary>
	/// Controls whether AnimationClip TRS curves are written to FBX animation stacks.
	/// </summary>
	public bool IncludeAnimations { get; set; } = true;

	public bool TryCreateCollection(IUnityObjectBase asset, [NotNullWhen(true)] out ExportCollectionBase? exportCollection)
	{
		switch (asset.MainAsset)
		{
			case SceneHierarchyObject scene:
				exportCollection = new FbxSceneModelExportCollection(this, scene);
				return true;
			case PrefabHierarchyObject prefab:
				exportCollection = new FbxPrefabModelExportCollection(this, prefab);
				return true;
			case IGameObject gameObject:
				exportCollection = new FbxCharacterExportCollection(this, gameObject.GetRoot());
				return true;
			case IComponent component when component.GameObject_C2P is IGameObject componentGameObject:
				exportCollection = new FbxCharacterExportCollection(this, componentGameObject.GetRoot());
				return true;
			case IComponent:
				exportCollection = new FbxExportCollection(this, asset);
				return true;
			case IMesh mesh:
				exportCollection = new FbxMeshExportCollection(this, mesh);
				return true;
			default:
				exportCollection = null;
				return false;
		}
	}

	public bool Export(IEnumerable<IUnityObjectBase> assets, string path, FileSystem fileSystem)
	{
		try
		{
			FbxSceneBuilder scene = FbxSceneBuilder.Build(assets, IncludeAnimations);
			if (scene.GeometryCount == 0)
			{
				Logger.Warning(LogCategory.Export, $"FBX scene '{path}' contains no geometry after hierarchy and mesh resolution.");
			}
			else
			{
				Logger.Info(LogCategory.Export, $"FBX scene assembled: {scene.GeometryCount} mesh(es), {scene.ModelCount} node(s), {scene.AnimationCount} animation stack(s), {scene.MaterialCount} material(s).");
			}
			return scene.Write(path, fileSystem);
		}
		catch (Exception ex)
		{
			Logger.Error(LogCategory.Export, $"FBX export failed: {ex.Message}");
			return false;
		}
	}

	public bool Export(IUnityObjectBase asset, string path, FileSystem fileSystem) => Export([asset], path, fileSystem);

	public IEnumerable<IUnityObjectBase> GetCharacterAssets(IGameObject inputRoot)
	{
		IGameObject root = inputRoot.GetRoot();
		HashSet<IUnityObjectBase> assets = new(ReferenceEqualityComparer.Instance);
		foreach (IEditorExtension hierarchyAsset in root.FetchHierarchy())
		{
			if (hierarchyAsset is IUnityObjectBase unityAsset)
			{
				assets.Add(unityAsset);
			}
		}

		foreach (IUnityObjectBase hierarchyAsset in assets.ToArray())
		{
			if (hierarchyAsset is ISkinnedMeshRenderer skinned && skinned.MeshP is IMesh mesh)
			{
				assets.Add(mesh);
			}
			if (hierarchyAsset is IRenderer renderer)
			{
				foreach (IPPtr_Material materialPointer in renderer.Materials_C25)
				{
					if (materialPointer.TryGetAsset(renderer.Collection) is IMaterial material)
					{
						assets.Add(material);
						foreach (IUnityTexEnv textureEnvironment in material.GetTextureProperties().Select(pair => pair.Value))
						{
							if (textureEnvironment.Texture.TryGetAsset(material.Collection) is ITexture2D texture)
							{
								assets.Add(texture);
							}
						}
					}
				}
			}
			if (hierarchyAsset is IAnimator animator)
			{
				if (animator.AvatarP is IAvatar avatar) assets.Add(avatar);
				if (animator.Controller_PPtr_AnimatorController_4P is IAnimatorController controller) assets.Add(controller);
				if (animator.Controller_PPtr_RuntimeAnimatorController_4_3P is IRuntimeAnimatorController runtimeController) assets.Add(runtimeController);
			}
		}

		foreach (IAnimationClip clip in root.Collection.Bundle.GetRoot().FetchAssets().OfType<IAnimationClip>())
		{
			try
			{
				if (clip.FindRoots().Any(candidate => ReferenceEquals(candidate.GetRoot(), root)))
				{
					assets.Add(clip);
				}
			}
			catch (Exception ex)
			{
				Logger.Warning(LogCategory.Export, $"Could not resolve AnimationClip '{clip.GetBestName()}' for character '{root.GetBestName()}': {ex.Message}");
			}
		}
		return assets;
	}
}

internal sealed class FbxSceneBuilder
{
	private const long KTimePerSecond = 46186158000L;
	private const double UnityToFbxScale = 100.0;
	private long nextId = 100000;
private readonly List<FbxGeometry> geometries = [];
		private readonly List<FbxShapeGeometry> shapeGeometries = [];
		private readonly List<FbxBlendShape> blendShapes = [];
private readonly List<FbxBlendShapeChannelNode> blendShapeChannels = [];
			private readonly Dictionary<(long ModelId, string ChannelName), FbxBlendShapeChannelNode> blendShapeChannelLookup = [];
			private readonly List<FbxModel> models = [];
	private readonly List<FbxMaterial> materials = [];
	private readonly List<FbxTexture> textures = [];
	private readonly List<FbxVideo> videos = [];
	private readonly List<FbxSkin> skins = [];
	private readonly List<FbxCluster> clusters = [];
	private readonly List<FbxAnimationStack> animationStacks = [];
	private readonly List<FbxAnimationLayer> animationLayers = [];
	private readonly List<FbxAnimationCurveNode> animationCurveNodes = [];
	private readonly List<FbxAnimationCurve> animationCurves = [];
	private readonly List<FbxConnection> connections = [];
	private readonly Dictionary<ITransform, FbxModel> transformModels = new(ReferenceEqualityComparer.Instance);
	private readonly Dictionary<IMaterial, FbxMaterial> materialCache = new(ReferenceEqualityComparer.Instance);
		private readonly Dictionary<ITexture2D, FbxTexture> textureCache = new(ReferenceEqualityComparer.Instance);
	private readonly HashSet<ITransform> buildingTransforms = new(ReferenceEqualityComparer.Instance);
	private readonly FbxModel sceneRoot;

		private FbxSceneBuilder()
		{
			sceneRoot = new FbxModel(NewId(), "SceneRoot", "Null", null, Matrix4x4.Identity);
			models.Add(sceneRoot);
		}

		public int GeometryCount => geometries.Count;
		public int ModelCount => models.Count;
		public int AnimationCount => animationStacks.Count;
		public int MaterialCount => materials.Count;

public static FbxSceneBuilder Build(IEnumerable<IUnityObjectBase> assets, bool includeAnimations = true)
		{
			FbxSceneBuilder builder = new();
		List<IGameObject> roots = GetRoots(assets).ToList();
		foreach (IGameObject root in roots)
		{
			ITransform? transform = root.GetTransform();
			if (transform is not null)
			{
				builder.AddTransformHierarchy(transform, builder.sceneRoot);
			}
		}

			foreach (IGameObject root in roots)
			{
				builder.AddRenderers(root.GetTransform());
			}

				// Character and prefab exports already attach every renderer mesh to its transform node.
				// Only emit loose mesh nodes when the requested collection contains meshes but no hierarchy.
				if (roots.Count == 0)
				{
					foreach (IMesh mesh in assets.OfType<IMesh>())
					{
						if (MeshData.TryMakeFromMesh(mesh, out MeshData meshData))
						{
							FbxModel meshModel = new(builder.NewId(), mesh.Name.String, "Mesh", builder.sceneRoot, Matrix4x4.Identity);
							builder.models.Add(meshModel);
							builder.connections.Add(new("OO", meshModel.Id, builder.sceneRoot.Id, null));
							builder.AddMesh(mesh, meshData, meshModel, null, []);
						}
						else
						{
							Logger.Warning(LogCategory.Export, $"Could not decode mesh '{mesh.GetBestName()}' ({mesh.PathID}) for FBX export.");
						}
					}
				}

			if (includeAnimations)
			{
				builder.AddAnimationClips(roots);
			}
			return builder;
	}

	private static IEnumerable<IGameObject> GetRoots(IEnumerable<IUnityObjectBase> assets)
	{
		HashSet<IGameObject> roots = new(ReferenceEqualityComparer.Instance);
		foreach (IUnityObjectBase asset in assets)
		{
			IGameObject? gameObject = asset switch
			{
				IGameObject go => go,
				IComponent component => component.GameObject_C2P,
				_ => null,
			};
			if (gameObject is not null)
			{
				roots.Add(gameObject.GetRoot());
			}
		}
		return roots;
	}

	private FbxModel? AddTransformHierarchy(ITransform transform, FbxModel? parent)
	{
		if (transformModels.TryGetValue(transform, out FbxModel? existing))
		{
			return existing;
		}
		if (!buildingTransforms.Add(transform))
		{
			return null;
		}

		IGameObject? gameObject = transform.GameObject_C4P;
		if (gameObject is null)
		{
			buildingTransforms.Remove(transform);
			return null;
		}

		Matrix4x4 local = CreateLocalMatrix(transform);
		FbxModel model = new(NewId(), gameObject.Name.String, "LimbNode", parent, local);
		models.Add(model);
		transformModels.Add(transform, model);
		if (parent is not null)
		{
			connections.Add(new("OO", model.Id, parent.Id, null));
		}

		foreach (ITransform child in transform.Children_C4P.WhereNotNull())
		{
			AddTransformHierarchy(child, model);
		}
		buildingTransforms.Remove(transform);
		return model;
	}

	private void AddRenderers(ITransform? transform)
	{
		if (transform is null || transform.GameObject_C4P is null)
		{
			return;
		}
		if (!transformModels.TryGetValue(transform, out FbxModel? model))
		{
			model = AddTransformHierarchy(transform, sceneRoot);
		}
			if (model is not null)
			{
				IGameObject gameObject = transform.GameObject_C4P;
				if (gameObject.TryGetComponent(out ISkinnedMeshRenderer? skinned)
					&& skinned.MeshP is IMesh skinnedMesh)
				{
					if (MeshData.TryMakeFromMesh(skinnedMesh, out MeshData skinnedData))
					{
						AddMesh(skinnedMesh, skinnedData, model, skinned, skinned.BonesP.WhereNotNull().ToArray());
					}
					else
					{
						Logger.Warning(LogCategory.Export, $"Could not decode skinned mesh '{skinnedMesh.GetBestName()}' ({skinnedMesh.PathID}) for FBX export.");
					}
				}
				else if (gameObject.TryGetComponent(out IMeshFilter? meshFilter)
				&& meshFilter.TryGetMesh(out IMesh? mesh)
				&& mesh is not null
					&& mesh is not null)
				{
					if (MeshData.TryMakeFromMesh(mesh, out MeshData meshData))
					{
						IRenderer? renderer = gameObject.GetComponent<IRenderer>();
						AddMesh(mesh, meshData, model, renderer, []);
					}
					else
					{
						Logger.Warning(LogCategory.Export, $"Could not decode mesh '{mesh.GetBestName()}' ({mesh.PathID}) for FBX export.");
					}
				}
		}
		foreach (ITransform child in transform.Children_C4P.WhereNotNull())
		{
			AddRenderers(child);
		}
	}

	private void AddMesh(IMesh mesh, MeshData data, FbxModel model, IRenderer? renderer, IReadOnlyList<ITransform> bones)
	{
		FbxGeometry geometry = FbxGeometry.Create(NewId(), mesh.Name.String, data, renderer, this);
			geometries.Add(geometry);
			connections.Add(new("OO", geometry.Id, model.Id, null));
			AddBlendShapes(mesh, geometry, model);

		if (renderer is not null)
		{
			foreach (IPPtr_Material materialPointer in renderer.Materials_C25)
			{
				IMaterial? material = materialPointer.TryGetAsset(renderer.Collection);
				if (material is null)
				{
					continue;
				}
				FbxMaterial fbxMaterial = GetOrCreateMaterial(material);
				connections.Add(new("OO", fbxMaterial.Id, model.Id, null));
			}
		}

		if (data.HasSkin && data.BindPose is { Length: > 0 })
		{
			FbxSkin skin = new(NewId(), $"Skin::{mesh.Name.String}");
			skins.Add(skin);
			connections.Add(new("OO", skin.Id, geometry.Id, null));
			for (int boneIndex = 0; boneIndex < data.BindPose.Length; boneIndex++)
			{
				FbxModel boneModel = GetBoneModel(bones, boneIndex, model);
				FbxCluster cluster = FbxCluster.Create(NewId(), boneModel, data, boneIndex, model.GlobalMatrix);
				clusters.Add(cluster);
				connections.Add(new("OO", cluster.Id, skin.Id, null));
				connections.Add(new("OO", cluster.Id, boneModel.Id, null));
			}
		}
	}

	private FbxModel GetBoneModel(IReadOnlyList<ITransform> bones, int index, FbxModel meshModel)
	{
		if (index < bones.Count && bones[index] is ITransform bone)
		{
			if (!transformModels.TryGetValue(bone, out FbxModel? boneModel))
			{
				boneModel = AddTransformHierarchy(bone, sceneRoot);
			}
			if (boneModel is not null)
			{
				return boneModel;
			}
		}
		FbxModel fallback = new(NewId(), $"Bone_{index}", "LimbNode", meshModel, Matrix4x4.Identity);
		models.Add(fallback);
		connections.Add(new("OO", fallback.Id, meshModel.Id, null));
		return fallback;
	}

	private FbxMaterial GetOrCreateMaterial(IMaterial material)
	{
		if (materialCache.TryGetValue(material, out FbxMaterial? cached))
		{
			return cached;
		}
		FbxMaterial fbxMaterial = new(NewId(), string.IsNullOrWhiteSpace(material.Name.String) ? "Material" : material.Name.String);
		materials.Add(fbxMaterial);
		materialCache.Add(material, fbxMaterial);
		foreach ((Utf8String propertyName, IUnityTexEnv textureEnvironment) in material.GetTextureProperties())
		{
			if (textureEnvironment.Texture.TryGetAsset(material.Collection) is not ITexture2D texture)
			{
				continue;
			}
				FbxTexture fbxTexture = GetOrCreateTexture(texture, ReadUnityVector2(textureEnvironment.Offset), ReadUnityVector2(textureEnvironment.Scale));
				string property = propertyName.String;
				string fbxProperty = IsNormalProperty(property) ? "NormalMap" : "DiffuseColor";
			connections.Add(new("OP", fbxTexture.Id, fbxMaterial.Id, fbxProperty));
		}
		return fbxMaterial;
	}

		private FbxTexture GetOrCreateTexture(ITexture2D texture, Vector2 offset, Vector2 scale)
		{
			if (textureCache.TryGetValue(texture, out FbxTexture? cached))
			{
				return cached;
			}
			string safeName = FileSystem.FixInvalidFileNameCharacters(string.IsNullOrWhiteSpace(texture.Name.String) ? $"Texture_{texture.PathID}" : texture.Name.String);
			FbxTexture fbxTexture = new(NewId(), $"Texture::{safeName}", safeName, texture, offset, scale);
		textures.Add(fbxTexture);
		textureCache.Add(texture, fbxTexture);
		FbxVideo video = new(NewId(), $"Video::{safeName}", safeName);
		videos.Add(video);
		connections.Add(new("OO", video.Id, fbxTexture.Id, null));
		return fbxTexture;
	}

	private void AddBlendShapes(IMesh mesh, FbxGeometry geometry, FbxModel model)
		{
			if (!mesh.Has_Shapes() || mesh.Shapes is not IBlendShapeData shapeData || shapeData.Channels.Count == 0 || shapeData.Shapes.Count == 0)
			{
				return;
			}

			foreach (IMeshBlendShapeChannel channel in shapeData.Channels)
			{
				if (channel.FrameCount <= 0)
				{
					continue;
				}
				string channelName = string.IsNullOrWhiteSpace(channel.Name_R.String) ? $"BlendShape_{channel.NameHash}" : channel.Name_R.String;
				FbxBlendShape blendShape = new(NewId(), $"BlendShape::{channelName}");
				FbxBlendShapeChannelNode blendShapeChannel = new(NewId(), $"BlendShapeChannel::{channelName}", channel.FrameCount > 0 && channel.FrameIndex < shapeData.FullWeights.Count ? shapeData.FullWeights[channel.FrameIndex] : 100f);
				blendShapes.Add(blendShape);
				blendShapeChannels.Add(blendShapeChannel);
				blendShapeChannelLookup[(model.Id, channelName)] = blendShapeChannel;
				connections.Add(new("OO", blendShape.Id, geometry.Id, null));
				connections.Add(new("OO", blendShapeChannel.Id, blendShape.Id, null));

				for (int frame = 0; frame < channel.FrameCount; frame++)
				{
					int shapeIndex = channel.FrameIndex + frame;
					if (shapeIndex < 0 || shapeIndex >= shapeData.Shapes.Count)
					{
						continue;
					}
					var shape = shapeData.Shapes[shapeIndex];
					int firstVertex = checked((int)shape.FirstVertex);
					int vertexCount = checked((int)shape.VertexCount);
					if (vertexCount <= 0 || firstVertex < 0 || firstVertex + vertexCount > shapeData.Vertices.Count)
					{
						continue;
					}
					List<(int Index, Vector3 Vertex, Vector3 Normal, Vector3 Tangent)> vertices = [];
					for (int index = firstVertex; index < firstVertex + vertexCount; index++)
					{
						IBlendShapeVertex vertex = shapeData.Vertices[index];
						vertices.Add((checked((int)vertex.Index), vertex.Vertex.CastToStruct(), vertex.Normal.CastToStruct(), vertex.Tangent.CastToStruct()));
					}
					if (vertices.Count == 0)
					{
						continue;
					}
					string frameName = $"{channelName}_{frame}";
					FbxShapeGeometry shapeGeometry = new(NewId(), $"Shape::{frameName}", vertices, shape.HasNormals, shape.HasTangents);
					shapeGeometries.Add(shapeGeometry);
					connections.Add(new("OO", shapeGeometry.Id, blendShapeChannel.Id, null));
				}
			}
		}

		private void AddAnimationClips(IReadOnlyList<IGameObject> roots)
	{
		HashSet<IAnimationClip> clips = new(ReferenceEqualityComparer.Instance);
		foreach (IGameObject root in roots)
		{
			foreach (IAnimationClip clip in root.Collection.Bundle.GetRoot().FetchAssets().OfType<IAnimationClip>())
			{
				if (clip.FindRoots().Any(candidate => ReferenceEquals(candidate.GetRoot(), root)))
				{
					clips.Add(clip);
				}
			}
		}
		foreach (IAnimationClip clip in clips)
		{
			AddAnimationClip(clip, roots);
		}
	}

	private void AddAnimationClip(IAnimationClip clip, IReadOnlyList<IGameObject> roots)
	{
		FbxAnimationStack stack = new(NewId(), string.IsNullOrWhiteSpace(clip.Name_C74.String) ? "Animation" : clip.Name_C74.String, clip.SampleRate_C74 > 0 ? clip.SampleRate_C74 : 30f);
		animationStacks.Add(stack);
		FbxAnimationLayer layer = new(NewId(), $"BaseLayer::{stack.Name}");
		animationLayers.Add(layer);
		connections.Add(new("OO", layer.Id, stack.Id, null));

		float maxTime = 0;
			foreach (IGameObject root in roots)
			{
				maxTime = Math.Max(maxTime, AddBlendShapeCurves(clip, root, layer));
				foreach (IVector3Curve curve in clip.PositionCurves_C74)
			{
				FbxModel? node = FindModel(root.GetTransform(), curve.Path.String);
				if (node is not null)
				{
					maxTime = Math.Max(maxTime, AddVectorCurve(curve.Curve.Curve, node, layer, "T", "Lcl Translation", true));
				}
			}
			foreach (IVector3Curve curve in clip.ScaleCurves_C74)
			{
				FbxModel? node = FindModel(root.GetTransform(), curve.Path.String);
				if (node is not null)
				{
					maxTime = Math.Max(maxTime, AddVectorCurve(curve.Curve.Curve, node, layer, "S", "Lcl Scaling", false));
				}
			}
			foreach (IQuaternionCurve curve in clip.RotationCurves_C74)
			{
				FbxModel? node = FindModel(root.GetTransform(), curve.Path.String);
				if (node is not null)
				{
					maxTime = Math.Max(maxTime, AddQuaternionCurve(curve.Curve.Curve, node, layer));
				}
			}
		}
		stack.StopTime = maxTime;
	}

	private FbxModel? FindModel(ITransform? root, string path)
	{
		if (root is null)
		{
			return null;
		}
		if (string.IsNullOrEmpty(path))
		{
			return transformModels.GetValueOrDefault(root);
		}
			ITransform current = root;
			string[] parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length > 0 && string.Equals(parts[0], root.GameObject_C4P?.Name.String, StringComparison.Ordinal))
			{
				parts = parts[1..];
			}
			foreach (string part in parts)
		{
			ITransform? next = current.Children_C4P.WhereNotNull().FirstOrDefault(child => child.GameObject_C4P?.Name.String == part);
			if (next is null)
			{
				return null;
			}
			current = next;
		}
		return transformModels.GetValueOrDefault(current);
	}

private float AddVectorCurve<T>(IEnumerable<T> keys, FbxModel node, FbxAnimationLayer layer, string suffix, string property, bool translation)
			where T : class
		{
			List<(float Time, Vector3 Value, Vector3 InSlope, Vector3 OutSlope)> values = [];
		foreach (T key in keys)
		{
			switch (key)
			{
					case AssetRipper.SourceGenerated.Subclasses.Keyframe_Vector3f.IKeyframe_Vector3f vectorKey:
						Vector3 value = vectorKey.Value.CastToStruct();
						Vector3 inSlope = vectorKey.InSlope.CastToStruct();
						Vector3 outSlope = vectorKey.OutSlope.CastToStruct();
						values.Add((vectorKey.Time, translation ? ToFbxVector(value) : value, translation ? ToFbxVector(inSlope) : inSlope, translation ? ToFbxVector(outSlope) : outSlope));
					break;
			}
		}
		if (values.Count == 0)
		{
			return 0;
		}
FbxAnimationCurveNode nodeData = new(NewId(), $"AnimCurveNode::{suffix}");
			animationCurveNodes.Add(nodeData);
			connections.Add(new("OO", nodeData.Id, layer.Id, null));
			connections.Add(new("OP", nodeData.Id, node.Id, property));
		for (int component = 0; component < 3; component++)
		{
				FbxAnimationCurve curve = new(NewId(), $"AnimCurve::{suffix}.{component}", values.Select(v => (v.Time, component switch { 0 => v.Value.X, 1 => v.Value.Y, _ => v.Value.Z }, component switch { 0 => v.InSlope.X, 1 => v.InSlope.Y, _ => v.InSlope.Z }, component switch { 0 => v.OutSlope.X, 1 => v.OutSlope.Y, _ => v.OutSlope.Z })).ToList());
			animationCurves.Add(curve);
			connections.Add(new("OP", curve.Id, nodeData.Id, component switch { 0 => "d|X", 1 => "d|Y", _ => "d|Z" }));
		}
		return values.Max(v => v.Time);
	}

		private float AddBlendShapeCurves(IAnimationClip clip, IGameObject root, FbxAnimationLayer layer)
		{
			float maxTime = 0;
			foreach (IFloatCurve curve in clip.FloatCurves_C74)
			{
				string attribute = curve.Attribute.String;
				if (!attribute.StartsWith("blendShape.", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				FbxModel? model = FindModel(root.GetTransform(), curve.Path.String);
				if (model is null || !blendShapeChannelLookup.TryGetValue((model.Id, attribute["blendShape.".Length..]), out FbxBlendShapeChannelNode? channel))
				{
					continue;
				}
				List<(float Time, float Value, float InSlope, float OutSlope)> values = [];
				foreach (IKeyframe_Single key in curve.Curve.Curve)
				{
					values.Add((key.Time, key.Value, key.InSlope, key.OutSlope));
				}
				maxTime = Math.Max(maxTime, AddScalarCurve(values, channel, layer, "DeformPercent"));
			}
			return maxTime;
		}

		private float AddScalarCurve(IReadOnlyList<(float Time, float Value, float InSlope, float OutSlope)> values, FbxBlendShapeChannelNode target, FbxAnimationLayer layer, string property)
		{
			if (values.Count == 0)
			{
				return 0;
			}
			FbxAnimationCurveNode nodeData = new(NewId(), $"AnimCurveNode::{property}");
			animationCurveNodes.Add(nodeData);
			connections.Add(new("OO", nodeData.Id, layer.Id, null));
			connections.Add(new("OP", nodeData.Id, target.Id, property));
			FbxAnimationCurve curve = new(NewId(), $"AnimCurve::{property}", values);
			animationCurves.Add(curve);
			connections.Add(new("OP", curve.Id, nodeData.Id, "d|X"));
			return values.Max(value => value.Time);
		}

		private float AddQuaternionCurve<T>(IEnumerable<T> keys, FbxModel node, FbxAnimationLayer layer)
		where T : class
	{
		List<(float Time, Vector3 Value)> values = [];
		foreach (T key in keys)
		{
			if (key is AssetRipper.SourceGenerated.Subclasses.Keyframe_Quaternionf.IKeyframe_Quaternionf quaternionKey)
			{
				Quaternion value = quaternionKey.Value.CastToStruct();
				values.Add((quaternionKey.Time, ToFbxEulerDegrees(value)));
			}
		}
		if (values.Count == 0)
		{
			return 0;
		}
FbxAnimationCurveNode nodeData = new(NewId(), "AnimCurveNode::R");
			animationCurveNodes.Add(nodeData);
			connections.Add(new("OO", nodeData.Id, layer.Id, null));
			connections.Add(new("OP", nodeData.Id, node.Id, "Lcl Rotation"));
		for (int component = 0; component < 3; component++)
		{
				FbxAnimationCurve curve = new(NewId(), $"AnimCurve::R.{component}", values.Select(v => (v.Time, component switch { 0 => v.Value.X, 1 => v.Value.Y, _ => v.Value.Z }, 0f, 0f)).ToList());
			animationCurves.Add(curve);
			connections.Add(new("OP", curve.Id, nodeData.Id, component switch { 0 => "d|X", 1 => "d|Y", _ => "d|Z" }));
		}
		return values.Max(v => v.Time);
	}

	public bool Write(string path, FileSystem fileSystem)
	{
		string directory = Path.GetDirectoryName(path) ?? ".";
		fileSystem.Directory.Create(directory);
		string textureDirectory = Path.Combine(directory, "Textures");
		if (textures.Count > 0)
		{
			fileSystem.Directory.Create(textureDirectory);
		}
		foreach (FbxTexture texture in textures)
		{
			texture.WriteSidecar(textureDirectory, fileSystem);
		}
		using Stream stream = fileSystem.File.Create(path);
		using StreamWriter writer = new(stream, new UTF8Encoding(false), 1024, leaveOpen: false) { NewLine = "\n" };
		FbxAsciiDocument document = new(this);
		document.Write(writer);
		return true;
	}

	private long NewId() => nextId++;
	private static Matrix4x4 CreateLocalMatrix(ITransform transform)
	{
		Vector3 position = ToFbxVector(transform.LocalPosition_C4.CastToStruct() * (float)UnityToFbxScale);
		Quaternion rotation = ToFbxQuaternion(transform.LocalRotation_C4.CastToStruct());
		Vector3 scale = transform.LocalScale_C4.CastToStruct();
		return Matrix4x4.CreateScale(scale) * Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateTranslation(position);
	}
	internal static Vector3 ToFbxVector(Vector3 value) => new(-value.X, value.Y, value.Z);
	internal static Vector4 ToFbxTangent(Vector4 value) => new(-value.X, value.Y, value.Z, -value.W);
	internal static Quaternion ToFbxQuaternion(Quaternion value) => new(value.X, -value.Y, -value.Z, value.W);
	private static Vector3 ToFbxEulerDegrees(Quaternion value)
	{
		Quaternion q = ToFbxQuaternion(value);
		float sinr = 2 * (q.W * q.X + q.Y * q.Z);
		float cosr = 1 - 2 * (q.X * q.X + q.Y * q.Y);
		float roll = MathF.Atan2(sinr, cosr);
		float sinp = 2 * (q.W * q.Y - q.Z * q.X);
		float pitch = MathF.Abs(sinp) >= 1 ? MathF.CopySign(MathF.PI / 2, sinp) : MathF.Asin(sinp);
		float siny = 2 * (q.W * q.Z + q.X * q.Y);
		float cosy = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
		float yaw = MathF.Atan2(siny, cosy);
		const float radToDeg = 57.29577951308232f;
		return new Vector3(roll, pitch, yaw) * radToDeg;
	}

	private static bool IsNormalProperty(string property) => property.Contains("normal", StringComparison.OrdinalIgnoreCase) || property.Contains("bump", StringComparison.OrdinalIgnoreCase);

	[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "UnityTexEnv Vector2f is a generated public value type with stable X/Y members.")]
	private static Vector2 ReadUnityVector2(object value)
	{
		Type type = value.GetType();
		return new Vector2(ReadUnityVectorComponent(type, value, "X"), ReadUnityVectorComponent(type, value, "Y"));
	}

	[UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "UnityTexEnv Vector2f is a generated public value type with stable X/Y members.")]
	private static float ReadUnityVectorComponent([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] Type type, object value, string name)
	{
		object? component = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance)?.GetValue(value)
			?? type.GetField(name, BindingFlags.Public | BindingFlags.Instance)?.GetValue(value);
		return component switch
		{
			float single => single,
			double doubleValue => (float)doubleValue,
			IConvertible convertible => convertible.ToSingle(CultureInfo.InvariantCulture),
			_ => 0f,
		};
	}

	private sealed class FbxAsciiDocument(FbxSceneBuilder scene)
	{
		public void Write(TextWriter writer)
		{
			writer.WriteLine("; FBX 7.4.0 project file");
			writer.WriteLine("; Generated by AssetRipper FbxAsciiExporter");
			writer.WriteLine("FBXHeaderExtension:  {");
			writer.WriteLine(" FBXHeaderVersion: 1003");
			writer.WriteLine(" FBXVersion: 7400");
			writer.WriteLine(" Creator: \"AssetRipper FbxAsciiExporter\"");
			writer.WriteLine("}");
			writer.WriteLine("GlobalSettings:  {");
			writer.WriteLine(" Version: 1000");
			writer.WriteLine(" Properties70:  {");
			writer.WriteLine("  P: \"UpAxis\",\"int\",\"Integer\",\"\",1");
			writer.WriteLine("  P: \"UpAxisSign\",\"int\",\"Integer\",\"\",1");
			writer.WriteLine("  P: \"FrontAxis\",\"int\",\"Integer\",\"\",2");
			writer.WriteLine("  P: \"FrontAxisSign\",\"int\",\"Integer\",\"\",1");
			writer.WriteLine("  P: \"CoordAxis\",\"int\",\"Integer\",\"\",0");
			writer.WriteLine("  P: \"CoordAxisSign\",\"int\",\"Integer\",\"\",1");
			writer.WriteLine("  P: \"UnitScaleFactor\",\"double\",\"Number\",\"\",1");
			writer.WriteLine("  P: \"OriginalUnitScaleFactor\",\"double\",\"Number\",\"\",100");
			writer.WriteLine("  P: \"TimeMode\",\"enum\",\"Integer\",\"\",6");
			writer.WriteLine(" }");
			writer.WriteLine("}");
			writer.WriteLine("Definitions:  {");
			writer.WriteLine(" Version: 100");
			writer.WriteLine($" Count: {scene.ObjectCount}");
			writer.WriteLine("}");
			writer.WriteLine("Objects:  {");
			foreach (FbxModel model in scene.models) model.Write(writer);
				foreach (FbxGeometry geometry in scene.geometries) geometry.Write(writer);
				foreach (FbxShapeGeometry shapeGeometry in scene.shapeGeometries) shapeGeometry.Write(writer);
				foreach (FbxBlendShape blendShape in scene.blendShapes) blendShape.Write(writer);
				foreach (FbxBlendShapeChannelNode channel in scene.blendShapeChannels) channel.Write(writer);
				foreach (FbxMaterial material in scene.materials) material.Write(writer);
			foreach (FbxVideo video in scene.videos) video.Write(writer);
			foreach (FbxTexture texture in scene.textures) texture.Write(writer);
			foreach (FbxSkin skin in scene.skins) skin.Write(writer);
			foreach (FbxCluster cluster in scene.clusters) cluster.Write(writer);
			foreach (FbxAnimationStack stack in scene.animationStacks) stack.Write(writer);
			foreach (FbxAnimationLayer layer in scene.animationLayers) layer.Write(writer);
			foreach (FbxAnimationCurveNode node in scene.animationCurveNodes) node.Write(writer);
			foreach (FbxAnimationCurve curve in scene.animationCurves) curve.Write(writer);
			writer.WriteLine("}");
			writer.WriteLine("Connections:  {");
			foreach (FbxConnection connection in scene.connections)
			{
				writer.Write(" C: \"");
				writer.Write(connection.Kind);
				writer.Write("\", ");
				writer.Write(connection.Child);
				writer.Write(", ");
				writer.Write(connection.Parent);
				if (connection.Property is not null)
				{
					writer.Write(", \"");
					writer.Write(Escape(connection.Property));
					writer.Write('\"');
				}
				writer.WriteLine();
			}
			writer.WriteLine("}");
		}

		private int ObjectCount => scene.models.Count + scene.geometries.Count + scene.shapeGeometries.Count + scene.blendShapes.Count + scene.blendShapeChannels.Count + scene.materials.Count + scene.videos.Count + scene.textures.Count + scene.skins.Count + scene.clusters.Count + scene.animationStacks.Count + scene.animationLayers.Count + scene.animationCurveNodes.Count + scene.animationCurves.Count;
	}

	private sealed class FbxGeometry(long id, string name, MeshData data, IReadOnlyList<int> materialIndices) : FbxObject(id, $"Geometry::{name}")
	{
		public MeshData Data { get; } = data;
		public IReadOnlyList<int> MaterialIndices { get; } = materialIndices;

		public static FbxGeometry Create(long id, string name, MeshData data, IRenderer? renderer, FbxSceneBuilder scene)
		{
			List<int> materialIndices = [];
			for (int i = 0; i < data.SubMeshes.Length; i++)
			{
				materialIndices.Add(i);
			}
			return new FbxGeometry(id, string.IsNullOrWhiteSpace(name) ? "Mesh" : name, data, materialIndices);
		}

		public override void Write(TextWriter writer)
		{
			writer.WriteLine($" Geometry: {Id}, \"{Escape(Name)}\", \"Mesh\" {{");
			writer.WriteLine("  GeometryVersion: 124");
			writer.WriteLine($"  Vertices: *{Data.Vertices.Length * 3} {{");
			writer.WriteLine("   a: " + string.Join(",", Data.Vertices.SelectMany(v => new[] { F(v.X * -UnityToFbxScale), F(v.Y * UnityToFbxScale), F(v.Z * UnityToFbxScale) })));
			writer.WriteLine("  }");
				List<int> polygonVertexIndices = [];
				List<int> materialByPolygon = [];
				SubMeshData[] subMeshes = Data.SubMeshes.Length > 0
					? Data.SubMeshes
					: Data.ProcessedIndexBuffer.Length >= 3
						? [new SubMeshData(0, 0, 0, Data.ProcessedIndexBuffer.Length, Data.ProcessedIndexBuffer.Length / 3, Data.Vertices.Length, AssetRipper.SourceGenerated.Enums.MeshTopology.Triangles, default)]
						: [];
				foreach ((SubMeshData subMesh, int subMeshIndex) in subMeshes.Select((value, index) => (value, index)))
			{
				foreach (int[] polygon in GetPolygons(subMesh, Data.ProcessedIndexBuffer))
				{
					for (int i = 0; i < polygon.Length; i++)
					{
						int value = (int)polygon[i];
						polygonVertexIndices.Add(i == polygon.Length - 1 ? -value - 1 : value);
					}
					materialByPolygon.Add(subMeshIndex);
				}
			}
			writer.WriteLine($"  PolygonVertexIndex: *{polygonVertexIndices.Count} {{");
			writer.WriteLine("   a: " + string.Join(",", polygonVertexIndices));
			writer.WriteLine("  }");
				WriteLayerElementNormal(writer);
				for (int channel = 0; channel < Data.UVCount; channel++)
				{
					WriteLayerElementUV(writer, channel);
				}
				WriteLayerElementTangent(writer);
			WriteLayerElementColor(writer);
			writer.WriteLine("  LayerElementMaterial:  {");
			writer.WriteLine("   Version: 101");
			writer.WriteLine("   Name: \"MaterialLayer\"");
			writer.WriteLine("   MappingInformationType: \"ByPolygon\"");
			writer.WriteLine("   ReferenceInformationType: \"IndexToDirect\"");
			writer.WriteLine($"   Materials: *{materialByPolygon.Count} {{");
			writer.WriteLine("    a: " + string.Join(",", materialByPolygon));
			writer.WriteLine("   }");
			writer.WriteLine("  }");
			writer.WriteLine("  Layer: 0 { Version: 100 }");
			writer.WriteLine(" }");
		}

		private void WriteLayerElementNormal(TextWriter writer)
		{
			if (!Data.HasNormals) return;
			writer.WriteLine("  LayerElementNormal:  {");
			writer.WriteLine("   Version: 101");
			writer.WriteLine("   Name: \"Normals\"");
			writer.WriteLine("   MappingInformationType: \"ByVertice\"");
			writer.WriteLine("   ReferenceInformationType: \"Direct\"");
			writer.WriteLine($"   Normals: *{Data.Normals!.Length * 3} {{");
			writer.WriteLine("    a: " + string.Join(",", Data.Normals.SelectMany(v => new[] { F(-v.X), F(v.Y), F(v.Z) })));
			writer.WriteLine("   }");
			writer.WriteLine("  }");
		}

			private void WriteLayerElementUV(TextWriter writer, int channel)
			{
				Vector2[]? uv = channel switch
				{
					0 => Data.UV0,
					1 => Data.UV1,
					2 => Data.UV2,
					3 => Data.UV3,
					4 => Data.UV4,
					5 => Data.UV5,
					6 => Data.UV6,
					7 => Data.UV7,
					_ => null,
				};
				if (uv is null || uv.Length != Data.Vertices.Length) return;
				writer.WriteLine($"  LayerElementUV: {channel} {{ ");
				writer.WriteLine("   Version: 101");
				writer.WriteLine($"   Name: \"UVChannel_{channel + 1}\"");
			writer.WriteLine("   MappingInformationType: \"ByPolygonVertex\"");
			writer.WriteLine("   ReferenceInformationType: \"IndexToDirect\"");
			writer.WriteLine($"   UV: *{uv.Length * 2} {{");
			writer.WriteLine("    a: " + string.Join(",", uv.SelectMany(v => new[] { F(v.X), F(v.Y) })));
			writer.WriteLine("   }");
				List<int> indices = [];
				SubMeshData[] subMeshes = Data.SubMeshes.Length > 0
					? Data.SubMeshes
					: Data.ProcessedIndexBuffer.Length >= 3
						? [new SubMeshData(0, 0, 0, Data.ProcessedIndexBuffer.Length, Data.ProcessedIndexBuffer.Length / 3, Data.Vertices.Length, AssetRipper.SourceGenerated.Enums.MeshTopology.Triangles, default)]
						: [];
				foreach (SubMeshData subMesh in subMeshes)
			{
				foreach (int[] polygon in GetPolygons(subMesh, Data.ProcessedIndexBuffer)) indices.AddRange(polygon);
			}
			writer.WriteLine($"   UVIndex: *{indices.Count} {{");
			writer.WriteLine("    a: " + string.Join(",", indices));
			writer.WriteLine("   }");
			writer.WriteLine("  }");
		}

		private void WriteLayerElementTangent(TextWriter writer)
		{
			if (!Data.HasTangents) return;
			writer.WriteLine("  LayerElementTangent:  {");
			writer.WriteLine("   Version: 101");
			writer.WriteLine("   Name: \"Tangents\"");
			writer.WriteLine("   MappingInformationType: \"ByVertice\"");
			writer.WriteLine("   ReferenceInformationType: \"Direct\"");
			writer.WriteLine($"   Tangents: *{Data.Tangents!.Length * 3} {{");
			writer.WriteLine("    a: " + string.Join(",", Data.Tangents.Select(v => FbxSceneBuilder.ToFbxTangent(v)).SelectMany(v => new[] { F(v.X), F(v.Y), F(v.Z) })));
			writer.WriteLine("   }");
			writer.WriteLine("  }");
		}

		private void WriteLayerElementColor(TextWriter writer)
		{
			if (!Data.HasColors) return;
			writer.WriteLine("  LayerElementColor:  {");
			writer.WriteLine("   Version: 101");
			writer.WriteLine("   Name: \"Colors\"");
			writer.WriteLine("   MappingInformationType: \"ByVertice\"");
			writer.WriteLine("   ReferenceInformationType: \"Direct\"");
			writer.WriteLine($"   Colors: *{Data.Colors!.Length * 4} {{");
			writer.WriteLine("    a: " + string.Join(",", Data.Colors.SelectMany(v => new[] { F(v.R), F(v.G), F(v.B), F(v.A) })));
			writer.WriteLine("   }");
			writer.WriteLine("  }");
		}

		private static IEnumerable<int[]> GetPolygons(SubMeshData subMesh, uint[] indexBuffer)
		{
			int start = subMesh.FirstIndex;
			int count = Math.Min(subMesh.IndexCount, indexBuffer.Length - start);
			if (start < 0 || count <= 0) yield break;
			switch (subMesh.Topology)
			{
				case AssetRipper.SourceGenerated.Enums.MeshTopology.Triangles:
					for (int i = 0; i + 2 < count; i += 3) yield return [ (int)indexBuffer[start + i], (int)indexBuffer[start + i + 1], (int)indexBuffer[start + i + 2] ];
					break;
				case AssetRipper.SourceGenerated.Enums.MeshTopology.Quads:
					for (int i = 0; i + 3 < count; i += 4) yield return [ (int)indexBuffer[start + i], (int)indexBuffer[start + i + 1], (int)indexBuffer[start + i + 2], (int)indexBuffer[start + i + 3] ];
					break;
				case AssetRipper.SourceGenerated.Enums.MeshTopology.TriangleStrip:
					for (int i = 0; i + 2 < count; i++)
					{
						int a = (int)indexBuffer[start + i];
						int b = (int)indexBuffer[start + i + 1];
						int c = (int)indexBuffer[start + i + 2];
						if (a == b || a == c || b == c) continue;
						yield return (i & 1) == 0 ? [a, b, c] : [b, a, c];
					}
					break;
			}
		}
	}

	private sealed class FbxShapeGeometry(long id, string name, IReadOnlyList<(int Index, Vector3 Vertex, Vector3 Normal, Vector3 Tangent)> vertices, bool hasNormals, bool hasTangents) : FbxObject(id, name)
	{
		public override void Write(TextWriter writer)
		{
			writer.WriteLine($" Geometry: {Id}, \"{Escape(Name)}\", \"Shape\" {{");
			writer.WriteLine("  GeometryVersion: 100");
			writer.WriteLine($"  Indexes: *{vertices.Count} {{ a: {string.Join(",", vertices.Select(vertex => vertex.Index))} }}");
			writer.WriteLine($"  Vertices: *{vertices.Count * 3} {{ a: {string.Join(",", vertices.SelectMany(vertex => { Vector3 value = FbxSceneBuilder.ToFbxVector(vertex.Vertex); return new[] { F(value.X), F(value.Y), F(value.Z) }; }))} }}");
			if (hasNormals)
			{
				writer.WriteLine($"  Normals: *{vertices.Count * 3} {{ a: {string.Join(",", vertices.SelectMany(vertex => { Vector3 value = FbxSceneBuilder.ToFbxVector(vertex.Normal); return new[] { F(value.X), F(value.Y), F(value.Z) }; }))} }}");
			}
			if (hasTangents)
			{
				writer.WriteLine($"  Tangents: *{vertices.Count * 3} {{ a: {string.Join(",", vertices.SelectMany(vertex => { Vector3 value = FbxSceneBuilder.ToFbxVector(vertex.Tangent); return new[] { F(value.X), F(value.Y), F(value.Z) }; }))} }}");
			}
			writer.WriteLine(" }");
		}
	}

	private sealed class FbxBlendShape(long id, string name) : FbxObject(id, name)
	{
		public override void Write(TextWriter writer) => writer.WriteLine($" Deformer: {Id}, \"{Escape(Name)}\", \"BlendShape\" {{ }}");
	}

	private sealed class FbxBlendShapeChannelNode(long id, string name, float fullWeight) : FbxObject(id, name)
	{
		public override void Write(TextWriter writer)
		{
			writer.WriteLine($" Deformer: {Id}, \"{Escape(Name)}\", \"BlendShapeChannel\" {{");
			writer.WriteLine("  DeformPercent: 0");
			writer.WriteLine($"  FullWeights: *1 {{ a: {F(fullWeight)} }}");
			writer.WriteLine(" }");
		}
	}

	private sealed class FbxModel(long id, string name, string type, FbxModel? parent, Matrix4x4 local) : FbxObject(id, $"Model::{name}")
	{
		public string Type { get; } = type;
		public Matrix4x4 LocalMatrix { get; } = local;
		public Matrix4x4 GlobalMatrix => Parent is null ? LocalMatrix : LocalMatrix * Parent.GlobalMatrix;
		public FbxModel? Parent { get; } = parent;
		public Vector3 Translation => new(LocalMatrix.M41, LocalMatrix.M42, LocalMatrix.M43);
		public Vector3 Scaling => new(LocalMatrix.GetScale().X, LocalMatrix.GetScale().Y, LocalMatrix.GetScale().Z);
			public Vector3 Rotation
			{
				get
				{
					return Matrix4x4.Decompose(LocalMatrix, out _, out Quaternion rotation, out _)
						? ToFbxEulerDegrees(rotation)
						: Vector3.Zero;
				}
			}

		public override void Write(TextWriter writer)
		{
			writer.WriteLine($" Model: {Id}, \"{Escape(Name)}\", \"{Type}\" {{");
			writer.WriteLine("  Version: 232");
			writer.WriteLine("  Properties70:  {");
				writer.WriteLine($"   P: \"Lcl Translation\",\"Lcl Translation\",\"\",\"A\",{F(Translation.X)},{F(Translation.Y)},{F(Translation.Z)}");
				writer.WriteLine($"   P: \"Lcl Rotation\",\"Lcl Rotation\",\"\",\"A\",{F(Rotation.X)},{F(Rotation.Y)},{F(Rotation.Z)}");
				writer.WriteLine($"   P: \"Lcl Scaling\",\"Lcl Scaling\",\"\",\"A\",{F(Scaling.X)},{F(Scaling.Y)},{F(Scaling.Z)}");
			writer.WriteLine("  }");
			writer.WriteLine("  Shading: T");
			writer.WriteLine("  Culling: \"CullingOff\"");
			writer.WriteLine(" }");
		}
	}

	private sealed class FbxMaterial(long id, string name) : FbxObject(id, $"Material::{name}")
	{
		public override void Write(TextWriter writer)
		{
			writer.WriteLine($" Material: {Id}, \"{Escape(Name)}\", \"\" {{");
			writer.WriteLine("  Version: 102");
			writer.WriteLine("  ShadingModel: \"Phong\"");
			writer.WriteLine("  MultiLayer: 0");
			writer.WriteLine("  Properties70:  {");
			writer.WriteLine("   P: \"DiffuseColor\",\"Color\",\"\",\"A\",0.8,0.8,0.8");
			writer.WriteLine("   P: \"DiffuseFactor\",\"double\",\"Number\",\"A\",1");
			writer.WriteLine("   P: \"SpecularColor\",\"Color\",\"\",\"A\",0.2,0.2,0.2");
			writer.WriteLine("   P: \"Shininess\",\"double\",\"Number\",\"A\",20");
			writer.WriteLine("  }");
			writer.WriteLine(" }");
		}
	}

		private sealed class FbxTexture(long id, string name, string fileName, ITexture2D source, Vector2 offset, Vector2 scale) : FbxObject(id, name)
		{
			public string FileName { get; } = fileName;
			public ITexture2D Source { get; } = source;
			public Vector2 Offset { get; } = offset;
			public Vector2 Scale { get; } = scale;
		public string RelativeFileName => $"Textures/{FileName}.png";

		public void WriteSidecar(string directory, FileSystem fileSystem)
		{
			if (!TextureConverter.TryConvertToBitmap(Source, out DirectBitmap bitmap)) return;
			using MemoryStream memory = new();
			bitmap.SaveAsPng(memory);
			string path = Path.Combine(directory, FileName + ".png");
			using Stream stream = fileSystem.File.Create(path);
			memory.Position = 0;
			memory.CopyTo(stream);
		}

		public override void Write(TextWriter writer)
		{
			writer.WriteLine($" Texture: {Id}, \"{Escape(Name)}\", \"TextureVideoClip\" {{");
			writer.WriteLine("  Type: \"TextureVideoClip\"");
			writer.WriteLine($"  FileName: \"{Escape(RelativeFileName)}\"");
			writer.WriteLine($"  RelativeFileName: \"{Escape(RelativeFileName)}\"");
				writer.WriteLine("  UVSet: \"UVChannel_1\"");
				writer.WriteLine("  UseMaterial: 1");
				writer.WriteLine("  Properties70:  {");
				writer.WriteLine($"   P: \"Translation\",\"Vector\",\"\",\"A\",{F(Offset.X)},{F(Offset.Y)},0");
				writer.WriteLine($"   P: \"Scaling\",\"Vector\",\"\",\"A\",{F(Scale.X)},{F(Scale.Y)},1");
				writer.WriteLine("  }");
			writer.WriteLine(" }");
		}
	}

	private sealed class FbxVideo(long id, string name, string fileName) : FbxObject(id, name)
	{
		public string FileName { get; } = fileName;
		public override void Write(TextWriter writer)
		{
			writer.WriteLine($" Video: {Id}, \"{Escape(Name)}\", \"Clip\" {{");
			writer.WriteLine("  Type: \"Clip\"");
			writer.WriteLine($"  FileName: \"Textures/{Escape(FileName)}.png\"");
			writer.WriteLine($"  RelativeFilename: \"Textures/{Escape(FileName)}.png\"");
			writer.WriteLine(" }");
		}
	}

	private sealed class FbxSkin(long id, string name) : FbxObject(id, name)
	{
		public override void Write(TextWriter writer)
		{
			writer.WriteLine($" Deformer: {Id}, \"{Escape(Name)}\", \"Skin\" {{");
			writer.WriteLine("  Version: 101");
			writer.WriteLine("  Link_DeformAcuracy: 50");
			writer.WriteLine(" }");
		}
	}

	private sealed class FbxCluster(long id, FbxModel bone, MeshData data, int boneIndex, Matrix4x4 geometryGlobal) : FbxObject(id, $"SubDeformer::{bone.Name}")
	{
		private readonly FbxModel bone = bone;
		private readonly MeshData data = data;
		private readonly int boneIndex = boneIndex;
		private readonly Matrix4x4 geometryGlobal = geometryGlobal;

		public static FbxCluster Create(long id, FbxModel bone, MeshData data, int boneIndex, Matrix4x4 geometryGlobal) => new(id, bone, data, boneIndex, geometryGlobal);

		public override void Write(TextWriter writer)
		{
			List<(int Index, float Weight)> weights = [];
			if (data.HasSkin)
			{
				for (int vertex = 0; vertex < data.Vertices.Length; vertex++)
				{
					BoneWeight4 weight = data.TryGetSkinAtIndex((uint)vertex);
					int[] indices = [weight.Index0, weight.Index1, weight.Index2, weight.Index3];
					float[] values = [weight.Weight0, weight.Weight1, weight.Weight2, weight.Weight3];
					for (int i = 0; i < indices.Length; i++)
					{
						if (indices[i] == boneIndex && values[i] > 0) weights.Add((vertex, values[i]));
					}
				}
			}
			writer.WriteLine($" Deformer: {Id}, \"{Escape(Name)}\", \"Cluster\" {{");
			writer.WriteLine("  Version: 100");
			writer.WriteLine("  UserData: \"\", \"\"");
			writer.WriteLine($"  Indexes: *{weights.Count} {{");
			writer.WriteLine("   a: " + string.Join(",", weights.Select(w => w.Index)));
			writer.WriteLine("  }");
			writer.WriteLine($"  Weights: *{weights.Count} {{");
			writer.WriteLine("   a: " + string.Join(",", weights.Select(w => F(w.Weight))));
			writer.WriteLine("  }");
			WriteMatrix(writer, "Transform", geometryGlobal);
			Matrix4x4 link = bone.GlobalMatrix;
			WriteMatrix(writer, "TransformLink", link);
			writer.WriteLine(" }");
		}
	}

	private sealed class FbxAnimationStack(long id, string name, float frameRate) : FbxObject(id, $"AnimStack::{name}")
	{
		public string NameOnly { get; } = name;
		public float FrameRate { get; } = frameRate;
		public float StopTime { get; set; }
		public override void Write(TextWriter writer)
		{
			long stop = (long)(StopTime * KTimePerSecond);
			writer.WriteLine($" AnimationStack: {Id}, \"{Escape(Name)}\", \"\" {{");
			writer.WriteLine("  Properties70:  {");
			writer.WriteLine("   P: \"LocalStart\",\"KTime\",\"Time\",\"\",0");
			writer.WriteLine($"   P: \"LocalStop\",\"KTime\",\"Time\",\"\",{stop}");
			writer.WriteLine("   P: \"ReferenceStart\",\"KTime\",\"Time\",\"\",0");
			writer.WriteLine($"   P: \"ReferenceStop\",\"KTime\",\"Time\",\"\",{stop}");
			writer.WriteLine("  }");
			writer.WriteLine(" }");
		}
	}

	private sealed class FbxAnimationLayer(long id, string name) : FbxObject(id, name)
	{
		public override void Write(TextWriter writer) => writer.WriteLine($" AnimationLayer: {Id}, \"{Escape(Name)}\", \"\" {{ }}");
	}

	private sealed class FbxAnimationCurveNode(long id, string name) : FbxObject(id, name)
	{
		public override void Write(TextWriter writer)
		{
			writer.WriteLine($" AnimationCurveNode: {Id}, \"{Escape(Name)}\", \"\" {{");
			writer.WriteLine("  Properties70:  {");
			writer.WriteLine("   P: \\\"d|X\\\",\\\"Number\\\",\\\"\\\",\\\"A\\\",0");
			writer.WriteLine("   P: \\\"d|Y\\\",\\\"Number\\\",\\\"\\\",\\\"A\\\",0");
			writer.WriteLine("   P: \\\"d|Z\\\",\\\"Number\\\",\\\"\\\",\\\"A\\\",0");
			writer.WriteLine("  }");
			writer.WriteLine(" }");
		}
	}

		private sealed class FbxAnimationCurve(long id, string name, IReadOnlyList<(float Time, float Value, float InSlope, float OutSlope)> keys) : FbxObject(id, name)
	{
		public override void Write(TextWriter writer)
		{
			long[] times = keys.Select(k => (long)(k.Time * KTimePerSecond)).ToArray();
			writer.WriteLine($" AnimationCurve: {Id}, \"{Escape(Name)}\", \"\" {{");
			writer.WriteLine("  Default: 0");
			writer.WriteLine("  KeyVer: 4008");
			writer.WriteLine($"  KeyTime: *{times.Length} {{ a: {string.Join(",", times)} }}");
			writer.WriteLine($"  KeyValueFloat: *{keys.Count} {{ a: {string.Join(",", keys.Select(k => F(k.Value)))} }}");
			writer.WriteLine($"  KeyAttrFlags: *{keys.Count} {{ a: {string.Join(",", Enumerable.Repeat("24840", keys.Count))} }}");
				writer.WriteLine($"  KeyAttrDataFloat: *{keys.Count * 4} {{ a: {string.Join(",", keys.SelectMany(k => new[] { F(k.InSlope), F(k.OutSlope), "0", "0" }))} }}");
			writer.WriteLine($"  KeyAttrRefCount: *{keys.Count} {{ a: {string.Join(",", Enumerable.Repeat("1", keys.Count))} }}");
			writer.WriteLine(" }");
		}
	}

	private abstract class FbxObject(long id, string name)
	{
		public long Id { get; } = id;
		public string Name { get; } = name;
		public abstract void Write(TextWriter writer);
	}

	private int ObjectCount => models.Count + geometries.Count + shapeGeometries.Count + blendShapes.Count + blendShapeChannels.Count + materials.Count + videos.Count + textures.Count + skins.Count + clusters.Count + animationStacks.Count + animationLayers.Count + animationCurveNodes.Count + animationCurves.Count;

	private readonly record struct FbxConnection(string Kind, long Child, long Parent, string? Property);
	private static string F(float value) => value.ToString("R", CultureInfo.InvariantCulture);
	private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);
	private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");
	private static void WriteMatrix(TextWriter writer, string name, Matrix4x4 matrix)
	{
		writer.WriteLine($"  {name}: *16 {{");
		writer.WriteLine($"   a: {F(matrix.M11)},{F(matrix.M12)},{F(matrix.M13)},{F(matrix.M14)},{F(matrix.M21)},{F(matrix.M22)},{F(matrix.M23)},{F(matrix.M24)},{F(matrix.M31)},{F(matrix.M32)},{F(matrix.M33)},{F(matrix.M34)},{F(matrix.M41)},{F(matrix.M42)},{F(matrix.M43)},{F(matrix.M44)}");
		writer.WriteLine("  }");
	}
}

internal static class FbxMatrixExtensions
{
	public static Vector3 GetScale(this Matrix4x4 matrix)
	{
		return new(
			new Vector3(matrix.M11, matrix.M12, matrix.M13).Length(),
			new Vector3(matrix.M21, matrix.M22, matrix.M23).Length(),
			new Vector3(matrix.M31, matrix.M32, matrix.M33).Length());
	}
}
