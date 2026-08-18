using System.Numerics;
using AssetRipper.Numerics;
using AssetRipper.SourceGenerated.Extensions;

namespace AssetRipper.Export.Modules.Models;

/// <summary>
/// Removes unusable source bone slots from readable mesh skin data before GLB construction.
/// It never creates a bind pose or a transform: callers must choose a rigid diagnostic fallback
/// when no source-valid bones survive.
/// </summary>
public static class SkinnedMeshSanitizer
{
	public static bool TrySanitize(MeshData source, int rendererBoneCount, out SanitizedSkinData sanitized)
	{
		sanitized = default;
		if (!source.HasSkin || source.BindPose is not { Length: > 0 } bindPoses || rendererBoneCount <= 0)
		{
			return false;
		}

		int sourceCount = Math.Min(rendererBoneCount, bindPoses.Length);
		int[] remap = Enumerable.Repeat(-1, sourceCount).ToArray();
		List<int> survivingSourceBones = [];
		List<Matrix4x4> survivingBindPoses = [];
		for (int sourceIndex = 0; sourceIndex < sourceCount; sourceIndex++)
		{
			Matrix4x4 bindPose = bindPoses[sourceIndex];
			if (!IsUsableBindPose(bindPose))
			{
				continue;
			}
			remap[sourceIndex] = survivingSourceBones.Count;
			survivingSourceBones.Add(sourceIndex);
			survivingBindPoses.Add(bindPose);
		}

		if (survivingSourceBones.Count == 0)
		{
			return false;
		}

		BoneWeight4[] skin = new BoneWeight4[source.Skin!.Length];
		int rootFallbackVertexCount = 0;
		for (int vertexIndex = 0; vertexIndex < skin.Length; vertexIndex++)
		{
			skin[vertexIndex] = RemapWeight(source.Skin[vertexIndex], remap, ref rootFallbackVertexCount);
		}

		MeshData mesh = source with { Skin = skin, BindPose = survivingBindPoses.ToArray() };
		sanitized = new SanitizedSkinData(mesh, survivingSourceBones.ToArray(), bindPoses.Length - survivingSourceBones.Count, rootFallbackVertexCount);
		return true;
	}

	public static bool IsUsableBindPose(Matrix4x4 value)
	{
		Span<float> components = stackalloc float[]
		{
			value.M11, value.M12, value.M13, value.M14,
			value.M21, value.M22, value.M23, value.M24,
			value.M31, value.M32, value.M33, value.M34,
			value.M41, value.M42, value.M43, value.M44,
		};
		bool anyNonZero = false;
		foreach (float component in components)
		{
			if (!float.IsFinite(component))
			{
				return false;
			}
			anyNonZero |= component != 0f;
		}
		return anyNonZero;
	}

	private static BoneWeight4 RemapWeight(BoneWeight4 source, IReadOnlyList<int> remap, ref int rootFallbackVertexCount)
	{
		Span<float> weights = stackalloc float[] { source.Weight0, source.Weight1, source.Weight2, source.Weight3 };
		Span<int> indices = stackalloc int[] { source.Index0, source.Index1, source.Index2, source.Index3 };
		Span<float> outputWeights = stackalloc float[BoneWeight4.Count];
		Span<int> outputIndices = stackalloc int[BoneWeight4.Count];
		int outputCount = 0;
		for (int i = 0; i < BoneWeight4.Count; i++)
		{
			int sourceIndex = indices[i];
			if (weights[i] <= 0f || sourceIndex < 0 || sourceIndex >= remap.Count || remap[sourceIndex] < 0)
			{
				continue;
			}
			outputWeights[outputCount] = weights[i];
			outputIndices[outputCount++] = remap[sourceIndex];
		}
		if (outputCount == 0)
		{
			rootFallbackVertexCount++;
			return new BoneWeight4(1f, 0f, 0f, 0f, 0, 0, 0, 0);
		}
		float sum = outputWeights[..outputCount].ToArray().Sum();
		for (int i = 0; i < outputCount; i++)
		{
			outputWeights[i] /= sum;
		}
		return new BoneWeight4(outputWeights[0], outputWeights[1], outputWeights[2], outputWeights[3], outputIndices[0], outputIndices[1], outputIndices[2], outputIndices[3]);
	}
}

public readonly record struct SanitizedSkinData(MeshData Mesh, int[] SurvivingSourceBoneIndices, int DroppedBindPoseCount, int RootFallbackVertexCount);
