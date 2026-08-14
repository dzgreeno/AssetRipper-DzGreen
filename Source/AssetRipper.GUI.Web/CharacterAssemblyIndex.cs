using AssetRipper.Assets;
using AssetRipper.Assets.Bundles;
using AssetRipper.GUI.Web.Pages;
using AssetRipper.SourceGenerated.Classes.ClassID_1;
using AssetRipper.SourceGenerated.Classes.ClassID_18;
using AssetRipper.SourceGenerated.Classes.ClassID_21;
using AssetRipper.SourceGenerated.Classes.ClassID_25;
using AssetRipper.SourceGenerated.Classes.ClassID_28;
using AssetRipper.SourceGenerated.Classes.ClassID_43;
using AssetRipper.SourceGenerated.Classes.ClassID_74;
using AssetRipper.SourceGenerated.Classes.ClassID_90;
using AssetRipper.SourceGenerated.Classes.ClassID_111;
using AssetRipper.SourceGenerated.Classes.ClassID_91;
using AssetRipper.SourceGenerated.Classes.ClassID_93;
using AssetRipper.SourceGenerated.Classes.ClassID_95;
using AssetRipper.SourceGenerated.Classes.ClassID_137;
using AssetRipper.SourceGenerated.Extensions;
using AssetRipper.SourceGenerated.Subclasses.UnityTexEnv;
using AssetRipper.SourceGenerated.Subclasses.PPtr_Material;

namespace AssetRipper.GUI.Web;

/// <summary>
/// Builds a non-destructive, cross-collection view of character-related assets.
/// It never rewrites Unity references; it only indexes resolved links for browsing/export UI.
/// </summary>
internal static class CharacterAssemblyIndex
{
	public static CharacterAssembly[] Build(GameBundle bundle)
	{
		List<IUnityObjectBase> allAssets = bundle.FetchAssets().ToList();
		List<CharacterAssemblyBuilder> builders = [];
		Dictionary<IGameObject, CharacterAssemblyBuilder> byRoot = new(ReferenceEqualityComparer.Instance);

		foreach (IGameObject gameObject in allAssets.OfType<IGameObject>())
		{
			if (!IsCharacterCandidate(gameObject))
			{
				continue;
			}
			IGameObject root = gameObject.GetRoot();
			if (!byRoot.TryGetValue(root, out CharacterAssemblyBuilder? builder))
			{
				builder = new CharacterAssemblyBuilder(root);
				byRoot.Add(root, builder);
				builders.Add(builder);
			}
			builder.AddHierarchy(root.FetchHierarchy());
		}

		foreach (CharacterAssemblyBuilder builder in builders)
		{
			builder.ResolveDirectReferences();
		}

		foreach (IAnimationClip clip in allAssets.OfType<IAnimationClip>())
		{
			try
			{
				foreach (CharacterAssemblyBuilder builder in builders)
				{
					if (clip.FindRoots().Any(root => ReferenceEquals(root.GetRoot(), builder.Root)))
					{
						builder.AnimationClips.Add(clip);
					}
				}
			}
			catch (Exception ex)
			{
				foreach (CharacterAssemblyBuilder builder in builders.Where(builder => clip.Collection == builder.Root.Collection))
				{
					builder.AddMissingLink($"AnimationClip {clip.GetBestName()} could not be resolved: {ex.Message}");
				}
			}
		}

		return builders
			.Select(builder => builder.Build())
			.Where(assembly => assembly.HierarchyAssetCount > 0 || assembly.AnimationClips.Count > 0)
			.OrderBy(assembly => assembly.RootName, StringComparer.OrdinalIgnoreCase)
			.ToArray();
	}

	private static bool IsCharacterCandidate(IGameObject gameObject)
	{
		return gameObject.TryGetComponent<ISkinnedMeshRenderer>(out _)
			|| gameObject.TryGetComponent<IAnimator>(out _)
			|| gameObject.TryGetComponent<IAnimation>(out _);
	}

	internal sealed class CharacterAssembly
	{
		public required IGameObject Root { get; init; }
		public required string RootName { get; init; }
		public required IReadOnlyList<IUnityObjectBase> HierarchyAssets { get; init; }
		public required IReadOnlyList<IMesh> Meshes { get; init; }
		public required IReadOnlyList<IAvatar> Avatars { get; init; }
		public required IReadOnlyList<IAnimatorController> Controllers { get; init; }
		public required IReadOnlyList<IRuntimeAnimatorController> RuntimeControllers { get; init; }
		public required IReadOnlyList<IAnimationClip> AnimationClips { get; init; }
		public required IReadOnlyList<IMaterial> Materials { get; init; }
		public required IReadOnlyList<ITexture2D> Textures { get; init; }
			public required int HierarchyAssetCount { get; init; }
			public required int SkinnedMeshCount { get; init; }
			public required int WeightedSkinnedMeshCount { get; init; }
			public required int MissingSkinWeightsCount { get; init; }
			public required IReadOnlyList<string> MissingLinks { get; init; }
	}

	private sealed class CharacterAssemblyBuilder(IGameObject root)
	{
		private readonly HashSet<IUnityObjectBase> hierarchySet = new(ReferenceEqualityComparer.Instance);
			private readonly HashSet<IMesh> meshes = new(ReferenceEqualityComparer.Instance);
			private readonly HashSet<IMesh> skinnedMeshes = new(ReferenceEqualityComparer.Instance);
			private readonly HashSet<IMesh> weightedSkinnedMeshes = new(ReferenceEqualityComparer.Instance);
			private readonly HashSet<IAvatar> avatars = new(ReferenceEqualityComparer.Instance);
		private readonly HashSet<IAnimatorController> controllers = new(ReferenceEqualityComparer.Instance);
		private readonly HashSet<IRuntimeAnimatorController> runtimeControllers = new(ReferenceEqualityComparer.Instance);
		private readonly HashSet<IAnimationClip> animationClips = new(ReferenceEqualityComparer.Instance);
		private readonly HashSet<IMaterial> materials = new(ReferenceEqualityComparer.Instance);
		private readonly HashSet<ITexture2D> textures = new(ReferenceEqualityComparer.Instance);
		private readonly List<string> missingLinks = [];

		public IGameObject Root { get; } = root;
		public HashSet<IAnimationClip> AnimationClips => animationClips;

		public void AddHierarchy(IEnumerable<IEditorExtension> assets)
		{
			try
			{
				foreach (IEditorExtension editorAsset in assets)
				{
					if (editorAsset is IUnityObjectBase unityAsset)
					{
						hierarchySet.Add(unityAsset);
					}
				}
			}
			catch (Exception ex)
			{
				missingLinks.Add($"Hierarchy traversal stopped for {Root.GetBestName()}: {ex.Message}");
			}
		}

		public void ResolveDirectReferences()
		{
			foreach (IUnityObjectBase asset in hierarchySet)
			{
					if (asset is ISkinnedMeshRenderer skinned)
					{
						if (skinned.MeshP is IMesh mesh)
						{
							meshes.Add(mesh);
							skinnedMeshes.Add(mesh);
							if (MeshData.TryMakeFromMesh(mesh, out MeshData meshData) && meshData.HasSkin)
							{
								weightedSkinnedMeshes.Add(mesh);
							}
							else
							{
								missingLinks.Add($"Skin weights missing from {mesh.GetBestName()} ({mesh.PathID}); animation may move bones without deforming this mesh.");
							}
						}
						else
						{
							missingLinks.Add($"SkinnedMeshRenderer.Mesh missing from {skinned.GetBestName()} ({skinned.PathID})");
						}
						foreach (IMaterial material in skinned.MaterialsP.WhereNotNull())
					{
						AddMaterialAndTextures(material);
					}
				}
				else if (asset is IRenderer renderer)
				{
					foreach (IPPtr_Material materialPointer in renderer.Materials_C25)
					{
						if (materialPointer.TryGetAsset(renderer.Collection) is IMaterial material)
						{
							AddMaterialAndTextures(material);
						}
					}
				}

				if (asset is IAnimator animator)
				{
					AddOrReport(animator.AvatarP, avatars, "Animator.Avatar", animator);
					if (animator.Controller_PPtr_AnimatorController_4P is { } controller)
					{
						controllers.Add(controller);
					}
					else if (animator.Controller_PPtr_RuntimeAnimatorController_4_3P is { } runtimeController)
					{
						runtimeControllers.Add(runtimeController);
					}
				}
			}
		}

		public void AddMissingLink(string message) => missingLinks.Add(message);

		private void AddMaterialAndTextures(IMaterial material)
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

		public CharacterAssembly Build()
		{
			return new CharacterAssembly
			{
				Root = Root,
				RootName = Root.Name.String,
				HierarchyAssets = hierarchySet.ToArray(),
				Meshes = meshes.ToArray(),
				Avatars = avatars.ToArray(),
				Controllers = controllers.ToArray(),
				RuntimeControllers = runtimeControllers.ToArray(),
				AnimationClips = animationClips.ToArray(),
				Materials = materials.ToArray(),
				Textures = textures.ToArray(),
					HierarchyAssetCount = hierarchySet.Count,
					SkinnedMeshCount = skinnedMeshes.Count,
					WeightedSkinnedMeshCount = weightedSkinnedMeshes.Count,
					MissingSkinWeightsCount = skinnedMeshes.Count - weightedSkinnedMeshes.Count,
					MissingLinks = missingLinks.ToArray(),
			};
		}

		private void AddOrReport<T>(T? asset, ICollection<T> target, string label, IUnityObjectBase owner) where T : class, IUnityObjectBase
		{
			if (asset is not null)
			{
				target.Add(asset);
			}
			else
			{
				missingLinks.Add($"{label} missing from {owner.GetBestName()} ({owner.PathID})");
			}
		}
	}
}
