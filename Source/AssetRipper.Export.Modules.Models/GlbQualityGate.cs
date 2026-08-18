using System.Buffers.Binary;
using System.Text.Json;

namespace AssetRipper.Export.Modules.Models;

public static class GlbQualityGate
{
	public static bool TryValidate(string path, out string reason)
	{
		reason = string.Empty;
		try
		{
			FileInfo info = new(path);
			if (!info.Exists || info.Length <= 1024)
			{
				reason = "GLB is missing or smaller than the minimum geometry threshold.";
				return false;
			}
			using FileStream stream = File.OpenRead(path);
			Span<byte> header = stackalloc byte[20];
			if (stream.Read(header) != header.Length
				|| BinaryPrimitives.ReadUInt32LittleEndian(header) != 0x46546C67
				|| BinaryPrimitives.ReadUInt32LittleEndian(header[16..]) != 0x4E4F534A)
			{
				reason = "GLB header or JSON chunk is invalid.";
				return false;
			}
			int jsonLength = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(header[12..]));
			if (jsonLength <= 0 || jsonLength > stream.Length - stream.Position)
			{
				reason = "GLB JSON chunk length is invalid.";
				return false;
			}
			byte[] json = GC.AllocateUninitializedArray<byte>(jsonLength);
			stream.ReadExactly(json);
			using JsonDocument document = JsonDocument.Parse(json);
			JsonElement root = document.RootElement;
			if (!root.TryGetProperty("accessors", out JsonElement accessors) || accessors.ValueKind is not JsonValueKind.Array
				|| !root.TryGetProperty("meshes", out JsonElement meshes) || meshes.ValueKind is not JsonValueKind.Array)
			{
				reason = "GLB does not declare mesh and accessor arrays.";
				return false;
			}
			if (!ValidateMeshPrimitives(meshes, accessors, root, out reason)
				|| !ValidateSkinDeclarations(root, meshes, accessors, out reason)
				|| !ValidateAnimationDeclarations(root, accessors, out reason))
			{
				return false;
			}
			return true;
		}
		catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException or OverflowException)
		{
			reason = $"GLB validation could not safely parse the file: {exception.Message}";
			return false;
		}
	}

	private static bool ValidateMeshPrimitives(JsonElement meshes, JsonElement accessors, JsonElement root, out string reason)
	{
		reason = string.Empty;
		int primitiveCount = 0;
		int materialCount = root.TryGetProperty("materials", out JsonElement materials) && materials.ValueKind is JsonValueKind.Array ? materials.GetArrayLength() : 0;
		for (int meshIndex = 0; meshIndex < meshes.GetArrayLength(); meshIndex++)
		{
			JsonElement mesh = meshes[meshIndex];
			if (!mesh.TryGetProperty("primitives", out JsonElement primitives) || primitives.ValueKind is not JsonValueKind.Array || primitives.GetArrayLength() == 0)
			{
				reason = $"GLB mesh {meshIndex} has no primitives.";
				return false;
			}
			for (int primitiveIndex = 0; primitiveIndex < primitives.GetArrayLength(); primitiveIndex++)
			{
				primitiveCount++;
				JsonElement primitive = primitives[primitiveIndex];
				if (!TryGetAccessor(primitive, "attributes", "POSITION", accessors, out JsonElement position)
					|| !HasPositiveCountAndNonZeroBounds(position))
				{
					reason = $"GLB mesh {meshIndex} primitive {primitiveIndex} has no POSITION accessor with non-zero bounds.";
					return false;
				}
				bool hasJoints = TryGetAccessor(primitive, "attributes", "JOINTS_0", accessors, out _);
				bool hasWeights = TryGetAccessor(primitive, "attributes", "WEIGHTS_0", accessors, out _);
				if (hasJoints != hasWeights)
				{
					reason = $"GLB mesh {meshIndex} primitive {primitiveIndex} declares only one of JOINTS_0 and WEIGHTS_0.";
					return false;
				}
				if (primitive.TryGetProperty("indices", out JsonElement indices) && (!indices.TryGetInt32(out int indexAccessor) || !IsAccessorIndex(indexAccessor, accessors)))
				{
					reason = $"GLB mesh {meshIndex} primitive {primitiveIndex} has an invalid index accessor.";
					return false;
				}
				if (primitive.TryGetProperty("material", out JsonElement material) && (!material.TryGetInt32(out int materialIndex) || materialIndex < 0 || materialIndex >= materialCount))
				{
					reason = $"GLB mesh {meshIndex} primitive {primitiveIndex} has an invalid material reference.";
					return false;
				}
			}
		}
		if (primitiveCount == 0)
		{
			reason = "GLB contains no mesh primitives.";
			return false;
		}
		return true;
	}

	private static bool ValidateSkinDeclarations(JsonElement root, JsonElement meshes, JsonElement accessors, out string reason)
	{
		reason = string.Empty;
		if (!root.TryGetProperty("nodes", out JsonElement nodes) || nodes.ValueKind is not JsonValueKind.Array)
		{
			return true;
		}
		bool hasSkinNode = false;
		JsonElement skins = root.TryGetProperty("skins", out JsonElement declaredSkins) && declaredSkins.ValueKind is JsonValueKind.Array ? declaredSkins : default;
		for (int nodeIndex = 0; nodeIndex < nodes.GetArrayLength(); nodeIndex++)
		{
			JsonElement node = nodes[nodeIndex];
			if (!node.TryGetProperty("skin", out JsonElement skin) || !skin.TryGetInt32(out int skinIndex)) continue;
			hasSkinNode = true;
			if (skins.ValueKind is not JsonValueKind.Array || skinIndex < 0 || skinIndex >= skins.GetArrayLength())
			{
				reason = $"GLB node {nodeIndex} references an invalid skin.";
				return false;
			}
			JsonElement skinDefinition = skins[skinIndex];
			if (!skinDefinition.TryGetProperty("joints", out JsonElement joints) || joints.ValueKind is not JsonValueKind.Array || joints.GetArrayLength() == 0)
			{
				reason = $"GLB skin {skinIndex} declares no joints.";
				return false;
			}
			foreach (JsonElement joint in joints.EnumerateArray())
			{
				if (!joint.TryGetInt32(out int jointIndex) || jointIndex < 0 || jointIndex >= nodes.GetArrayLength())
				{
					reason = $"GLB skin {skinIndex} references an invalid joint node.";
					return false;
				}
			}
			if (skinDefinition.TryGetProperty("inverseBindMatrices", out JsonElement inverseBindMatrices)
				&& (!inverseBindMatrices.TryGetInt32(out int accessorIndex) || !IsAccessorIndex(accessorIndex, accessors)))
			{
				reason = $"GLB skin {skinIndex} references an invalid inverse bind matrix accessor.";
				return false;
			}
			if (node.TryGetProperty("mesh", out JsonElement mesh) && (!mesh.TryGetInt32(out int meshIndex) || meshIndex < 0 || meshIndex >= meshes.GetArrayLength() || !MeshHasCompleteSkinAttributes(meshes[meshIndex], accessors)))
			{
				reason = $"GLB skinned node {nodeIndex} does not reference primitives with JOINTS_0 and WEIGHTS_0.";
				return false;
			}
		}
		return !hasSkinNode || skins.ValueKind is JsonValueKind.Array;
	}

	private static bool ValidateAnimationDeclarations(JsonElement root, JsonElement accessors, out string reason)
	{
		reason = string.Empty;
		if (!root.TryGetProperty("animations", out JsonElement animations) || animations.ValueKind is not JsonValueKind.Array) return true;
		int nodeCount = root.TryGetProperty("nodes", out JsonElement nodes) && nodes.ValueKind is JsonValueKind.Array ? nodes.GetArrayLength() : 0;
		for (int animationIndex = 0; animationIndex < animations.GetArrayLength(); animationIndex++)
		{
			JsonElement animation = animations[animationIndex];
			if (!animation.TryGetProperty("samplers", out JsonElement samplers) || samplers.ValueKind is not JsonValueKind.Array
				|| !animation.TryGetProperty("channels", out JsonElement channels) || channels.ValueKind is not JsonValueKind.Array)
			{
				reason = $"GLB animation {animationIndex} has no sampler or channel array.";
				return false;
			}
			foreach (JsonElement sampler in samplers.EnumerateArray())
			{
				if (!sampler.TryGetProperty("input", out JsonElement input) || !input.TryGetInt32(out int inputIndex) || !IsAccessorIndex(inputIndex, accessors)
					|| !sampler.TryGetProperty("output", out JsonElement output) || !output.TryGetInt32(out int outputIndex) || !IsAccessorIndex(outputIndex, accessors))
				{
					reason = $"GLB animation {animationIndex} has an invalid sampler accessor.";
					return false;
				}
			}
			foreach (JsonElement channel in channels.EnumerateArray())
			{
				if (!channel.TryGetProperty("sampler", out JsonElement samplerIndex) || !samplerIndex.TryGetInt32(out int sampler) || sampler < 0 || sampler >= samplers.GetArrayLength()
					|| !channel.TryGetProperty("target", out JsonElement target) || !target.TryGetProperty("node", out JsonElement node) || !node.TryGetInt32(out int nodeIndex) || nodeIndex < 0 || nodeIndex >= nodeCount)
				{
					reason = $"GLB animation {animationIndex} has an invalid channel target.";
					return false;
				}
			}
		}
		return true;
	}

	private static bool MeshHasCompleteSkinAttributes(JsonElement mesh, JsonElement accessors)
	{
		if (!mesh.TryGetProperty("primitives", out JsonElement primitives) || primitives.ValueKind is not JsonValueKind.Array || primitives.GetArrayLength() == 0) return false;
		foreach (JsonElement primitive in primitives.EnumerateArray())
		{
			if (!TryGetAccessor(primitive, "attributes", "JOINTS_0", accessors, out _)
				|| !TryGetAccessor(primitive, "attributes", "WEIGHTS_0", accessors, out _)) return false;
		}
		return true;
	}

	private static bool TryGetAccessor(JsonElement primitive, string parentName, string attributeName, JsonElement accessors, out JsonElement accessor)
	{
		accessor = default;
		return primitive.TryGetProperty(parentName, out JsonElement parent)
			&& parent.ValueKind is JsonValueKind.Object
			&& parent.TryGetProperty(attributeName, out JsonElement property)
			&& property.TryGetInt32(out int accessorIndex)
			&& IsAccessorIndex(accessorIndex, accessors)
			&& (accessor = accessors[accessorIndex]).ValueKind is JsonValueKind.Object;
	}

	private static bool IsAccessorIndex(int accessorIndex, JsonElement accessors) => accessorIndex >= 0 && accessorIndex < accessors.GetArrayLength();

	private static bool HasPositiveCountAndNonZeroBounds(JsonElement accessor) => accessor.TryGetProperty("count", out JsonElement count) && count.TryGetInt32(out int value) && value > 0 && HasNonZeroBounds(accessor);

	private static bool HasNonZeroBounds(JsonElement accessor)
	{
		if (!accessor.TryGetProperty("min", out JsonElement min) || !accessor.TryGetProperty("max", out JsonElement max)
			|| min.GetArrayLength() < 3 || max.GetArrayLength() < 3) return false;
		for (int index = 0; index < 3; index++)
		{
			if (MathF.Abs(max[index].GetSingle() - min[index].GetSingle()) > 0.000001f) return true;
		}
		return false;
	}
}
