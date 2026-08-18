using AssetRipper.Assets;
using AssetRipper.Assets.Collections;
using AssetRipper.Assets.Metadata;
using AssetRipper.Import.AssetCreation;
using AssetRipper.Import.Structure.Assembly.Serializable;
using AssetRipper.SourceGenerated.Classes.ClassID_21;
using AssetRipper.SourceGenerated.Classes.ClassID_43;
using AssetRipper.SourceGenerated.Classes.ClassID_4;
using AssetRipper.SourceGenerated.Extensions;
using AssetRipper.SourceGenerated.Subclasses.ChannelInfo;
using AssetRipper.SourceGenerated.Subclasses.StreamInfo;
using System.Numerics;

namespace AssetRipper.Export.Modules.Models;

/// <summary>
/// Reads only explicitly declared embedded vertex payloads from recovered Mesh TypeTrees.
/// It intentionally does not synthesize streams, components, indices, bind poses, or PPtrs.
/// </summary>
public static class TypeTreeMeshAdapter
{
	private const int MeshClassID = 43;
	private const int SkinnedMeshRendererClassID = 137;
	private const int VertexStreamAlign = 16;
	private const uint MaximumDeclaredVertexStreamSize = 512 * 1024 * 1024;

	public static bool TryReadEmbeddedVertexPayload(TypeTreeObject source, out TypeTreeMeshVertexPayload payload, out string? rejection)
	{
		payload = default;
		rejection = null;
		if (source.ClassID != MeshClassID)
		{
			rejection = "The recovered TypeTree object is not a Unity Mesh.";
			return false;
		}
		if (!source.ReleaseFields.TryGetField("m_VertexData", out SerializableValue vertexDataValue))
		{
			rejection = "The TypeTree does not declare m_VertexData.";
			return false;
		}

		SerializableStructure vertexData = vertexDataValue.AsStructure;
		if (!vertexData.TryGetField("m_VertexCount", out SerializableValue vertexCountValue) || vertexCountValue.AsUInt32 is 0 or > 5_000_000)
		{
			rejection = "m_VertexData.m_VertexCount is absent or outside the bounded recovery range.";
			return false;
		}
		if (!vertexData.TryGetField("m_Channels", out SerializableValue channelsValue)
			|| !TryGetStructureArray(channelsValue, out IReadOnlyList<SerializableStructure>? rawChannels)
			|| rawChannels.Count == 0)
		{
			rejection = "m_VertexData.m_Channels is not exposed as an explicit TypeTree array.";
			return false;
		}

		List<TypeTreeVertexChannel> channels = new(rawChannels.Count);
		foreach (SerializableStructure rawChannel in rawChannels)
		{
			if (!TryReadChannel(rawChannel, out TypeTreeVertexChannel parsed))
			{
				rejection = "A declared TypeTree vertex channel has an incomplete stream, offset, format, or dimension.";
				return false;
			}
			channels.Add(parsed);
		}

		List<ChannelInfo> declaredChannels = channels.Select(channel => new ChannelInfo { Stream = channel.Stream, Offset = channel.Offset, Format = channel.Format, Dimension = channel.Dimension }).ToList();
		BuildDeclaredStreams(declaredChannels, (int)vertexCountValue.AsUInt32, source, out int requiredByteCount);
		if (!TryReadDeclaredVertexBytes(source, vertexData, requiredByteCount, out byte[] vertexBytes, out rejection))
		{
			return false;
		}
		payload = new((int)vertexCountValue.AsUInt32, vertexBytes, channels);
		return true;
	}

	public static bool TryDecodeEmbeddedPositions(TypeTreeObject source, out System.Numerics.Vector3[]? positions, out string? rejection)
	{
		positions = null;
		if (!TryReadEmbeddedVertexPayload(source, out TypeTreeMeshVertexPayload payload, out rejection))
		{
			return false;
		}
		List<ChannelInfo> channels = payload.Channels.Select(channel => new ChannelInfo { Stream = channel.Stream, Offset = channel.Offset, Format = channel.Format, Dimension = channel.Dimension }).ToList();
		IStreamInfo[] streams = BuildDeclaredStreams(channels, payload.VertexCount, source, out _);
		VertexDataBlob blob = new(channels, streams, payload.EmbeddedData, payload.VertexCount, source.Collection.Version, source.Collection.EndianType);
		if (!blob.ReadData(out positions, out _, out _, out _, out _, out _, out _, out _, out _, out _, out _, out _, out _))
		{
			rejection = "The declared embedded vertex layout did not decode a POSITION channel.";
			return false;
		}
		return true;
	}

	public static bool TryReadDeclaredTriangleIndices(TypeTreeObject source, int vertexCount, out uint[]? indices, out string? rejection)
	{
		indices = null;
		rejection = null;
		if (source.ClassID != MeshClassID)
		{
			rejection = "The recovered TypeTree object is not a Unity Mesh.";
			return false;
		}
		if (!source.ReleaseFields.TryGetField("m_IndexFormat", out SerializableValue formatValue)
			|| !source.ReleaseFields.TryGetField("m_IndexBuffer", out SerializableValue bufferValue))
		{
			rejection = "The TypeTree does not declare m_IndexFormat and m_IndexBuffer.";
			return false;
		}

		byte[] buffer = bufferValue.AsByteArray;
		int bytesPerIndex = formatValue.AsInt32 == 0 ? sizeof(ushort) : sizeof(uint);
		if (buffer.Length == 0 || buffer.Length % bytesPerIndex != 0)
		{
			rejection = "The declared index buffer is empty or not aligned to its declared index format.";
			return false;
		}
		int count = buffer.Length / bytesPerIndex;
		if (count % 3 != 0)
		{
			rejection = "The declared index buffer is not a triangle list.";
			return false;
		}

		uint[] decoded = new uint[count];
		for (int index = 0; index < count; index++)
		{
			uint value = bytesPerIndex == sizeof(ushort)
				? BitConverter.ToUInt16(buffer, index * bytesPerIndex)
				: BitConverter.ToUInt32(buffer, index * bytesPerIndex);
			if (value >= vertexCount)
			{
				rejection = "A declared index is outside the declared vertex range.";
				return false;
			}
			decoded[index] = value;
		}
		indices = decoded;
		return true;
	}

	public static bool TryReadDeclaredBindPoses(TypeTreeObject source, out System.Numerics.Matrix4x4[]? bindPoses, out string? rejection)
	{
		bindPoses = null;
		rejection = null;
		if (!source.ReleaseFields.TryGetField("m_BindPose", out SerializableValue bindPoseValue)
			|| bindPoseValue.CValue is not SerializableValue[] rawBindPoses
			|| rawBindPoses.Length == 0)
		{
			rejection = "The TypeTree does not expose a non-empty m_BindPose array.";
			return false;
		}

		System.Numerics.Matrix4x4[] decoded = new System.Numerics.Matrix4x4[rawBindPoses.Length];
		for (int index = 0; index < rawBindPoses.Length; index++)
		{
			SerializableStructure matrix = rawBindPoses[index].AsStructure;
			if (!TryReadMatrix(matrix, out System.Numerics.Matrix4x4 value) || !IsFinite(value))
			{
				rejection = "A declared m_BindPose matrix is incomplete or non-finite.";
				return false;
			}
			decoded[index] = value;
		}
		bindPoses = decoded;
		return true;
	}

	public static bool TryReadDeclaredSubMeshes(TypeTreeObject source, int vertexCount, out SubMeshData[]? subMeshes, out string? rejection)
	{
		subMeshes = null;
		rejection = null;
		if (!source.ReleaseFields.TryGetField("m_IndexFormat", out SerializableValue formatValue)
			|| !source.ReleaseFields.TryGetField("m_SubMeshes", out SerializableValue subMeshesValue)
			|| !TryGetStructureArray(subMeshesValue, out IReadOnlyList<SerializableStructure>? rawSubMeshes)
			|| rawSubMeshes.Count == 0)
		{
			rejection = "The TypeTree does not expose m_IndexFormat and a non-empty m_SubMeshes array.";
			return false;
		}

		int bytesPerIndex = formatValue.AsInt32 == 0 ? sizeof(ushort) : sizeof(uint);
		SubMeshData[] decoded = new SubMeshData[rawSubMeshes.Count];
		for (int index = 0; index < rawSubMeshes.Count; index++)
		{
			if (!TryReadSubMesh(rawSubMeshes[index], bytesPerIndex, vertexCount, out SubMeshData subMesh))
			{
				rejection = "A declared TypeTree submesh has incomplete bounds, invalid index alignment, or an invalid vertex range.";
				return false;
			}
			decoded[index] = subMesh;
		}
		subMeshes = decoded;
		return true;
	}

	public static bool TryCreateDeclaredMeshData(TypeTreeObject source, out MeshData meshData, out string? rejection)
	{
		meshData = MeshData.Empty;
		rejection = null;
		if (!TryReadEmbeddedVertexPayload(source, out TypeTreeMeshVertexPayload payload, out rejection))
		{
			return false;
		}
		List<ChannelInfo> channels = payload.Channels.Select(channel => new ChannelInfo { Stream = channel.Stream, Offset = channel.Offset, Format = channel.Format, Dimension = channel.Dimension }).ToList();
		IStreamInfo[] streams = BuildDeclaredStreams(channels, payload.VertexCount, source, out _);
		MeshData vertexData = new VertexDataBlob(channels, streams, payload.EmbeddedData, payload.VertexCount, source.Collection.Version, source.Collection.EndianType).ToMeshData();
		if (vertexData.Vertices.Length != payload.VertexCount)
		{
			rejection = "The declared embedded vertex layout did not produce a complete POSITION array.";
			return false;
		}
		if (!TryReadDeclaredTriangleIndices(source, vertexData.Vertices.Length, out uint[]? indices, out rejection)
			|| !TryReadDeclaredSubMeshes(source, vertexData.Vertices.Length, out SubMeshData[]? subMeshes, out rejection))
		{
			return false;
		}

		System.Numerics.Matrix4x4[]? bindPoses = null;
		if (vertexData.HasSkin && !TryReadDeclaredBindPoses(source, out bindPoses, out rejection))
		{
			return false;
		}
		meshData = vertexData with { BindPose = bindPoses, ProcessedIndexBuffer = indices!, SubMeshes = subMeshes! };
		return true;
	}

	public static bool TryReadDeclaredSkinnedMeshRenderer(TypeTreeObject source, out TypeTreeSkinnedMeshRendererData data, out string? rejection)
	{
		data = default;
		if (!TryReadDeclaredSkinnedMeshRendererReferences(source, out TypeTreeSkinnedMeshRendererReferences references, out rejection))
		{
			return false;
		}
		if (references.Materials.Any(static target => target is not IMaterial))
		{
			rejection = $"m_Materials contains a non-IMaterial pointer ({string.Join(",", references.Materials.Select(static target => target.ClassName))}).";
			return false;
		}
		data = new TypeTreeSkinnedMeshRendererData(references.Mesh, references.Bones, references.Materials.Cast<IMaterial>().ToArray());
		return true;
	}

	public static bool TryReadDeclaredSkinnedMeshRendererReferences(TypeTreeObject source, out TypeTreeSkinnedMeshRendererReferences data, out string? rejection)
	{
		data = default;
		rejection = null;
		if (source.ClassID != SkinnedMeshRendererClassID)
		{
			rejection = "The recovered TypeTree object is not a Unity SkinnedMeshRenderer.";
			return false;
		}
		if (!source.ReleaseFields.TryGetField("m_Mesh", out SerializableValue meshValue))
		{
			rejection = "The TypeTree does not expose m_Mesh.";
			return false;
		}
		IPPtr meshPointer = meshValue.AsPPtr;
		if (!TryResolveDeclaredPointer(source, meshValue, out IUnityObjectBase? meshTarget))
		{
			rejection = $"m_Mesh PPtr(FileID={meshPointer.FileID}, PathID={meshPointer.PathID}) does not resolve to a loaded source object.";
			return false;
		}
		if (meshTarget is not IMesh && meshTarget is not TypeTreeObject { ClassID: MeshClassID })
		{
			rejection = $"m_Mesh PPtr(FileID={meshPointer.FileID}, PathID={meshPointer.PathID}) resolves to '{meshTarget.ClassName}' rather than a readable IMesh or recovered TypeTree Mesh.";
			return false;
		}
		if (!TryReadDeclaredPointers(source, "m_Bones", out IUnityObjectBase[]? boneTargets, out rejection)
			|| boneTargets.Any(static target => target is not ITransform))
		{
			rejection ??= "m_Bones contains an unresolved or non-Transform pointer.";
			return false;
		}
		if (!TryReadDeclaredPointers(source, "m_Materials", out IUnityObjectBase[]? materialTargets, out rejection))
		{
			return false;
		}
		Bounds rendererBounds = default;
		bool hasBounds = source.ReleaseFields.TryGetField("m_AABB", out SerializableValue boundsValue)
			&& TryReadBounds(boundsValue.AsStructure, out rendererBounds);
		data = new TypeTreeSkinnedMeshRendererReferences(meshTarget, boneTargets.Cast<ITransform>().ToArray(), materialTargets, hasBounds ? rendererBounds : default, hasBounds);
		return true;
	}

	public static RecoveredAssociationDecision RecoverUniqueMeshAssociation(TypeTreeObject renderer, TypeTreeSkinnedMeshRendererReferences references, string characterIdentity)
	{
		ArgumentNullException.ThrowIfNull(renderer);
		string normalizedIdentity = characterIdentity.Split('@', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
		List<RecoveredMeshCandidate> allCandidates = [];
		List<RecoveredAssociationEvidence> unreadableEvidence = [];
		foreach (IUnityObjectBase asset in renderer.Collection.Bundle.GetRoot().FetchAssets()
			.Where(static asset => asset is IMesh or TypeTreeObject { ClassID: MeshClassID })
			.OrderBy(static asset => asset.PathID))
		{
			RecoveredMeshCandidate? candidate = ToRecoveredMeshCandidate(asset, references.RendererBounds, references.HasRendererBounds, out string? unreadableReason);
			if (candidate is not null)
			{
				allCandidates.Add(candidate);
			}
			else
			{
				string collectionPath = string.IsNullOrWhiteSpace(asset.Collection.FilePath) ? asset.Collection.Name : asset.Collection.FilePath;
				RecoveredAssociationCandidateFacts facts = new(collectionPath, 0, 0, false, false, 0, -1, 0, false, false, 0.0f, 0.0f);
				unreadableEvidence.Add(new(asset.PathID, asset.GetBestName(), false, "candidate-unreadable", unreadableReason ?? "The source Mesh does not expose readable geometry through an established schema path.", facts));
			}
		}
		List<RecoveredMeshCandidate> identityCandidates = allCandidates
			.Where(candidate => !string.IsNullOrEmpty(normalizedIdentity)
				&& (candidate.CollectionPath.Contains(normalizedIdentity, StringComparison.OrdinalIgnoreCase)
				|| candidate.Name.Contains(normalizedIdentity, StringComparison.OrdinalIgnoreCase))
				)
			.ToList();
		IReadOnlyList<RecoveredMeshCandidate> selectedScope = identityCandidates.Count > 0 ? identityCandidates : allCandidates;
		RecoveredAssociationDecision decision = RecoveredAssociationResolver.SelectUniqueMesh(selectedScope, references.Bones.Length, references.Materials.Length, references.HasRendererBounds);
		string scopeMessage = identityCandidates.Count > 0
			? $" Character identity '{normalizedIdentity}' narrowed {allCandidates.Count} readable meshes to {identityCandidates.Count} source-local candidates."
			: $" Character identity '{normalizedIdentity}' did not occur in readable mesh names or collection paths, so all {allCandidates.Count} readable meshes remained eligible for structural evaluation.";
		if (unreadableEvidence.Count == 0)
		{
			return decision with { Message = decision.Message + scopeMessage };
		}
		string unavailableSummary = string.Join("; ", unreadableEvidence
			.GroupBy(static evidence => evidence.Message, StringComparer.Ordinal)
			.OrderByDescending(static group => group.Count())
			.ThenBy(static group => group.Key, StringComparer.Ordinal)
			.Take(3)
			.Select(static group => $"{group.Count()}× {group.Key}"));
		return decision with
		{
			Evidence = decision.Evidence.Concat(unreadableEvidence).OrderBy(static evidence => evidence.CandidatePathID).ToArray(),
			Message = decision.Message + scopeMessage + $" {unreadableEvidence.Count} Mesh candidate(s) were unreadable and retained as diagnostics: {unavailableSummary}",
		};
	}

	private static bool BoundsMatch(Bounds rendererBounds, Bounds meshBounds)
	{
		// Renderer AABBs can be represented after a local Transform, whereas a Mesh AABB is local to its mesh.
		// Center is diagnostic only; extents remain structural and are compared with an explicit relative tolerance.
		const float absoluteTolerance = 0.002f;
		const float relativeTolerance = 0.02f;
		Vector3 delta = Vector3.Abs(rendererBounds.Extent - meshBounds.Extent);
		Vector3 allowance = new(
			absoluteTolerance + MathF.Abs(rendererBounds.Extent.X) * relativeTolerance,
			absoluteTolerance + MathF.Abs(rendererBounds.Extent.Y) * relativeTolerance,
			absoluteTolerance + MathF.Abs(rendererBounds.Extent.Z) * relativeTolerance);
		return delta.X <= allowance.X && delta.Y <= allowance.Y && delta.Z <= allowance.Z;
	}

	private static RecoveredMeshCandidate? ToRecoveredMeshCandidate(IUnityObjectBase asset, Bounds rendererBounds, bool hasRendererBounds, out string? rejection)
	{
		rejection = null;
		MeshData meshData = MeshData.Empty;
		bool readable;
		switch (asset)
		{
			case IMesh mesh:
				readable = MeshData.TryMakeFromMesh(mesh, out meshData);
				if (!readable)
				{
					rejection = "The readable IMesh did not produce MeshData through the established importer path.";
				}
				break;
			case TypeTreeObject typeTreeMesh:
				readable = TryCreateDeclaredMeshData(typeTreeMesh, out meshData, out rejection);
				break;
			default:
				readable = false;
				rejection = "The candidate is not a supported Mesh source.";
				break;
		}
		if (!readable)
		{
			return null;
		}
		int maxBoneIndex = meshData.Skin is null || meshData.Skin.Length == 0
			? -1
			: meshData.Skin.Max(static weight => Math.Max(Math.Max(weight.Index0, weight.Index1), Math.Max(weight.Index2, weight.Index3)));
		bool nonZeroBounds = meshData.Vertices.Length > 0
			&& meshData.Vertices.All(static vertex => float.IsFinite(vertex.X) && float.IsFinite(vertex.Y) && float.IsFinite(vertex.Z))
			&& meshData.Vertices.Aggregate(System.Numerics.Vector3.Min) != meshData.Vertices.Aggregate(System.Numerics.Vector3.Max);
		Bounds bounds = Bounds.CalculateFromVertexArray(meshData.Vertices);
		string collectionPath = string.IsNullOrWhiteSpace(asset.Collection.FilePath) ? asset.Collection.Name : asset.Collection.FilePath;
		float centerDistance = hasRendererBounds ? Vector3.Distance(rendererBounds.Center, bounds.Center) : 0.0f;
		float extentDistance = hasRendererBounds ? Vector3.Distance(rendererBounds.Extent, bounds.Extent) : 0.0f;
		bool matchesRendererBounds = !hasRendererBounds || BoundsMatch(rendererBounds, bounds);
		return new RecoveredMeshCandidate(asset.PathID, asset.GetBestName(), meshData.Vertices.Length, meshData.ProcessedIndexBuffer.Length, meshData.Vertices.Length > 0, meshData.HasSkin, meshData.BindPose?.Length ?? 0, maxBoneIndex, meshData.SubMeshes.Length, nonZeroBounds, asset, bounds.Center, bounds.Extent, collectionPath, matchesRendererBounds, centerDistance, extentDistance);
	}

	public static bool TryResolveDeclaredPointer(TypeTreeObject source, SerializableValue pointerValue, [NotNullWhen(true)] out IUnityObjectBase? target)
	{
		IPPtr pointer = pointerValue.AsPPtr;
		return TryResolveDeclaredPointer(source, pointer.FileID, pointer.PathID, out target);
	}

	private static bool TryResolveDeclaredPointer(TypeTreeObject source, int fileID, long pathID, [NotNullWhen(true)] out IUnityObjectBase? target)
	{
		target = null;
		if (pathID == 0 || fileID < 0)
		{
			return false;
		}
		if (fileID == 0)
		{
			target = source.Collection.FirstOrDefault(asset => asset.PathID == pathID);
			return target is not null;
		}
		if (fileID > source.Collection.Dependencies.Count)
		{
			return false;
		}
		AssetCollection? collection = source.Collection.Dependencies[fileID - 1];
		return collection is not null && collection.Assets.TryGetValue(pathID, out target);
	}

	private static bool TryReadDeclaredPointers(TypeTreeObject source, string fieldName, [NotNullWhen(true)] out IUnityObjectBase[]? targets, out string? rejection)
	{
		targets = null;
		rejection = null;
		if (!source.ReleaseFields.TryGetField(fieldName, out SerializableValue pointersValue))
		{
			rejection = $"The TypeTree does not expose {fieldName}.";
			return false;
		}
		if (pointersValue.CValue is IPPtr[] pointerArray && pointerArray.Length > 0)
		{
			IUnityObjectBase[] pointerResolved = new IUnityObjectBase[pointerArray.Length];
			for (int index = 0; index < pointerArray.Length; index++)
			{
				if (!TryResolveDeclaredPointer(source, new SerializableValue(0, pointerArray[index]), out IUnityObjectBase? target))
				{
					rejection = $"{fieldName}[{index}] does not resolve to a loaded source object.";
					return false;
				}
				pointerResolved[index] = target;
			}
			targets = pointerResolved;
			return true;
		}
		if (pointersValue.CValue is IUnityAssetBase[] structureArray && structureArray.Length > 0)
		{
			IUnityObjectBase[] structureResolved = new IUnityObjectBase[structureArray.Length];
			for (int index = 0; index < structureArray.Length; index++)
			{
				if (!TryReadDeclaredPointer(structureArray[index], out int fileID, out long pathID))
				{
					string shape = structureArray[index] is SerializableStructure structure
						? $"{structure.Type.FullName} ({string.Join(",", structure.Type.Fields.Select(static field => field.Name))})"
						: structureArray[index].GetType().FullName ?? structureArray[index].GetType().Name;
					rejection = $"{fieldName}[{index}] does not expose m_FileID and m_PathID pointer values (representation: {shape}).";
					return false;
				}
				if (!TryResolveDeclaredPointer(source, fileID, pathID, out IUnityObjectBase? target))
				{
					rejection = $"{fieldName}[{index}] PPtr(FileID={fileID}, PathID={pathID}) does not resolve to a loaded source object.";
					return false;
				}
				structureResolved[index] = target;
			}
			targets = structureResolved;
			return true;
		}
		if (pointersValue.CValue is not SerializableValue[] pointers || pointers.Length == 0)
		{
			rejection = $"The TypeTree does not expose a non-empty {fieldName} array (representation: {pointersValue.CValue.GetType().FullName}).";
			return false;
		}
		IUnityObjectBase[] valueResolved = new IUnityObjectBase[pointers.Length];
		for (int index = 0; index < pointers.Length; index++)
		{
			if (!TryResolveDeclaredPointer(source, pointers[index], out IUnityObjectBase? target))
			{
				rejection = $"{fieldName}[{index}] does not resolve to a loaded source object.";
				return false;
			}
		valueResolved[index] = target;
		}
		targets = valueResolved;
		return true;
	}

	private static bool TryReadDeclaredPointer(IUnityAssetBase candidate, out int fileID, out long pathID)
	{
		switch (candidate)
		{
			case IPPtr pointer:
				fileID = pointer.FileID;
				pathID = pointer.PathID;
				return true;
			case SerializableStructure pointerStructure
				when pointerStructure.TryGetField("m_FileID", out SerializableValue fileIDValue)
					&& pointerStructure.TryGetField("m_PathID", out SerializableValue pathIDValue):
				fileID = fileIDValue.AsInt32;
				pathID = pathIDValue.AsInt64;
				return true;
			default:
				fileID = default;
				pathID = default;
				return false;
		}
	}

	private static bool TryReadChannel(SerializableStructure source, out TypeTreeVertexChannel channel)
	{
		channel = default;
		if (!source.TryGetField("stream", out SerializableValue stream)
			|| !source.TryGetField("offset", out SerializableValue offset)
			|| !source.TryGetField("format", out SerializableValue format)
			|| !source.TryGetField("dimension", out SerializableValue dimension)
			|| !IsDeclaredChannelDimensionSupported(dimension.AsByte))
		{
			return false;
		}
		channel = new(stream.AsByte, offset.AsByte, format.AsByte, dimension.AsByte);
		return true;
	}

	public static bool IsDeclaredChannelDimensionSupported(byte dimension) => dimension <= 4;

	private static bool TryReadMatrix(SerializableStructure source, out System.Numerics.Matrix4x4 matrix)
	{
		matrix = default;
		return source.TryGetField("e00", out SerializableValue e00)
			&& source.TryGetField("e01", out SerializableValue e01)
			&& source.TryGetField("e02", out SerializableValue e02)
			&& source.TryGetField("e03", out SerializableValue e03)
			&& source.TryGetField("e10", out SerializableValue e10)
			&& source.TryGetField("e11", out SerializableValue e11)
			&& source.TryGetField("e12", out SerializableValue e12)
			&& source.TryGetField("e13", out SerializableValue e13)
			&& source.TryGetField("e20", out SerializableValue e20)
			&& source.TryGetField("e21", out SerializableValue e21)
			&& source.TryGetField("e22", out SerializableValue e22)
			&& source.TryGetField("e23", out SerializableValue e23)
			&& source.TryGetField("e30", out SerializableValue e30)
			&& source.TryGetField("e31", out SerializableValue e31)
			&& source.TryGetField("e32", out SerializableValue e32)
			&& source.TryGetField("e33", out SerializableValue e33)
			&& AssignMatrix(out matrix, e00.AsSingle, e01.AsSingle, e02.AsSingle, e03.AsSingle, e10.AsSingle, e11.AsSingle, e12.AsSingle, e13.AsSingle, e20.AsSingle, e21.AsSingle, e22.AsSingle, e23.AsSingle, e30.AsSingle, e31.AsSingle, e32.AsSingle, e33.AsSingle);
	}

	private static bool AssignMatrix(out System.Numerics.Matrix4x4 matrix, float e00, float e01, float e02, float e03, float e10, float e11, float e12, float e13, float e20, float e21, float e22, float e23, float e30, float e31, float e32, float e33)
	{
		matrix = new(e00, e01, e02, e03, e10, e11, e12, e13, e20, e21, e22, e23, e30, e31, e32, e33);
		return true;
	}

	private static bool TryReadSubMesh(SerializableStructure source, int bytesPerIndex, int totalVertexCount, out SubMeshData subMesh)
	{
		subMesh = default;
		if (!source.TryGetField("firstByte", out SerializableValue firstByte)
			|| !source.TryGetField("indexCount", out SerializableValue indexCount)
			|| !source.TryGetField("topology", out SerializableValue topology)
			|| !source.TryGetField("baseVertex", out SerializableValue baseVertex)
			|| !source.TryGetField("firstVertex", out SerializableValue firstVertex)
			|| !source.TryGetField("vertexCount", out SerializableValue vertexCount)
			|| !source.TryGetField("localAABB", out SerializableValue boundsValue)
			|| !TryReadBounds(boundsValue.AsStructure, out Bounds bounds))
		{
			return false;
		}
		uint firstByteValue = firstByte.AsUInt32;
		uint indexCountValue = indexCount.AsUInt32;
		uint firstVertexValue = firstVertex.AsUInt32;
		uint vertexCountValue = vertexCount.AsUInt32;
		if (firstByteValue % bytesPerIndex != 0 || indexCountValue == 0 || firstVertexValue + vertexCountValue > totalVertexCount)
		{
			return false;
		}
		subMesh = new(baseVertex.AsUInt32, checked((int)(firstByteValue / bytesPerIndex)), checked((int)firstVertexValue), checked((int)indexCountValue), checked((int)(indexCountValue / 3)), checked((int)vertexCountValue), (AssetRipper.SourceGenerated.Enums.MeshTopology)topology.AsInt32, bounds);
		return true;
	}

	private static bool TryReadBounds(SerializableStructure source, out Bounds bounds)
	{
		bounds = default;
		if (!source.TryGetField("m_Center", out SerializableValue centerValue)
			|| !source.TryGetField("m_Extent", out SerializableValue extentValue)
			|| !TryReadVector3(centerValue.AsStructure, out System.Numerics.Vector3 center)
			|| !TryReadVector3(extentValue.AsStructure, out System.Numerics.Vector3 extent))
		{
			return false;
		}
		bounds = new(center, extent);
		return true;
	}

	private static bool TryReadVector3(SerializableStructure source, out System.Numerics.Vector3 vector)
	{
		vector = default;
		if (!source.TryGetField("x", out SerializableValue x)
			|| !source.TryGetField("y", out SerializableValue y)
			|| !source.TryGetField("z", out SerializableValue z))
		{
			return false;
		}
		vector = new(x.AsSingle, y.AsSingle, z.AsSingle);
		return float.IsFinite(vector.X) && float.IsFinite(vector.Y) && float.IsFinite(vector.Z);
	}

	private static bool IsFinite(System.Numerics.Matrix4x4 value)
	{
		return float.IsFinite(value.M11) && float.IsFinite(value.M12) && float.IsFinite(value.M13) && float.IsFinite(value.M14)
			&& float.IsFinite(value.M21) && float.IsFinite(value.M22) && float.IsFinite(value.M23) && float.IsFinite(value.M24)
			&& float.IsFinite(value.M31) && float.IsFinite(value.M32) && float.IsFinite(value.M33) && float.IsFinite(value.M34)
			&& float.IsFinite(value.M41) && float.IsFinite(value.M42) && float.IsFinite(value.M43) && float.IsFinite(value.M44);
	}

	private static bool TryReadDeclaredVertexBytes(TypeTreeObject source, SerializableStructure vertexData, int requiredByteCount, out byte[] bytes, out string? rejection)
	{
		bytes = [];
		rejection = null;
		if (TryGetNonEmptyByteArray(vertexData, "m_Data", out byte[]? embedded)
			|| TryGetNonEmptyByteArray(vertexData, "m_DataSize", out embedded))
		{
			if (embedded.Length < requiredByteCount)
			{
				rejection = $"The declared embedded vertex bytes ({embedded.Length}) are shorter than the declared channel layout ({requiredByteCount}).";
				return false;
			}
			bytes = embedded;
			return true;
		}
		if (!TryReadStreamData(source.ReleaseFields, out string streamPath, out ulong streamOffset, out uint streamSize, out rejection))
		{
			return false;
		}
		if (streamSize < requiredByteCount)
		{
			rejection = $"m_StreamData.size ({streamSize}) is shorter than the declared vertex channel layout ({requiredByteCount}).";
			return false;
		}
		if (streamSize > MaximumDeclaredVertexStreamSize)
		{
			rejection = $"m_StreamData.size ({streamSize}) exceeds the bounded recovery limit ({MaximumDeclaredVertexStreamSize}).";
			return false;
		}
		var resourceFile = source.Collection.Bundle.ResolveResource(streamPath);
		if (resourceFile is null)
		{
			rejection = $"m_StreamData.path '{streamPath}' does not resolve to a loaded ResourceFile.";
			return false;
		}
		if (!IsDeclaredResourceRangeValid(resourceFile.Stream.Length, streamOffset, streamSize, out rejection))
		{
			return false;
		}
		bytes = new byte[streamSize];
		lock (resourceFile.Stream)
		{
			resourceFile.Stream.Position = checked((long)streamOffset);
			resourceFile.Stream.ReadExactly(bytes);
		}
		return true;
	}

	private static bool TryGetStructureArray(SerializableValue value, [NotNullWhen(true)] out IReadOnlyList<SerializableStructure>? structures)
	{
		structures = value.CValue switch
		{
			SerializableValue[] values => values.Select(static item => item.AsStructure).ToArray(),
			IUnityAssetBase[] values when values.All(static item => item is SerializableStructure) => values.Cast<SerializableStructure>().ToArray(),
			_ => null,
		};
		return structures is not null;
	}

	public static bool IsDeclaredResourceRangeValid(long resourceLength, ulong offset, uint size, [NotNullWhen(false)] out string? rejection)
	{
		rejection = null;
		if (resourceLength < 0 || offset > long.MaxValue)
		{
			rejection = "The declared resource stream length or offset is outside the supported signed range.";
			return false;
		}
		ulong length = (ulong)resourceLength;
		if (offset > length || size > length - offset)
		{
			rejection = $"m_StreamData range offset={offset} size={size} is outside the loaded ResourceFile length={resourceLength}.";
			return false;
		}
		return true;
	}

	private static bool TryGetNonEmptyByteArray(SerializableStructure fields, string fieldName, [NotNullWhen(true)] out byte[]? bytes)
	{
		bytes = null;
		if (!fields.TryGetField(fieldName, out SerializableValue value) || value.AsByteArray.Length == 0)
		{
			return false;
		}
		bytes = value.AsByteArray;
		return true;
	}

	private static bool TryReadStreamData(SerializableStructure fields, out string path, out ulong offset, out uint size, out string? rejection)
	{
		path = string.Empty;
		offset = 0;
		size = 0;
		rejection = null;
		if (!fields.TryGetField("m_StreamData", out SerializableValue streamData))
		{
			rejection = "The TypeTree exposes neither embedded vertex bytes nor m_StreamData.";
			return false;
		}
		SerializableStructure stream = streamData.AsStructure;
		if (!stream.TryGetField("path", out SerializableValue pathValue)
			|| string.IsNullOrWhiteSpace(pathValue.AsString)
			|| pathValue.AsString.Any(char.IsControl)
			|| !stream.TryGetField("offset", out SerializableValue offsetValue)
			|| !stream.TryGetField("size", out SerializableValue sizeValue)
			|| sizeValue.AsUInt32 == 0)
		{
			rejection = "m_StreamData does not expose a non-empty path and positive offset/size declaration.";
			return false;
		}
		path = pathValue.AsString;
		offset = offsetValue.AsUInt64;
		size = sizeValue.AsUInt32;
		return true;
	}

	private static IStreamInfo[] BuildDeclaredStreams(IReadOnlyList<ChannelInfo> channels, int vertexCount, TypeTreeObject source, out int requiredByteCount)
	{
		int streamCount = channels.Count == 0 ? 0 : channels.Max(static channel => channel.Stream) + 1;
		IStreamInfo[] streams = new IStreamInfo[streamCount];
		long offset = 0;
		long dataEnd = 0;
		for (int stream = 0; stream < streamCount; stream++)
		{
			uint channelMask = 0;
			uint stride = 0;
			for (int index = 0; index < channels.Count; index++)
			{
				ChannelInfo channel = channels[index];
				if (channel.Stream != stream || channel.GetDataDimension() == 0)
				{
					continue;
				}
				channelMask |= 1u << index;
				stride += channel.GetDataDimension() * (uint)MeshHelper.GetFormatSize(MeshHelper.ToVertexFormat(channel.Format, source.Collection.Version));
			}
			streams[stream] = new StreamInfo_4 { ChannelMask = channelMask, Offset = checked((uint)offset), Stride_Byte = checked((byte)stride) };
			offset = checked(offset + (long)vertexCount * stride);
			dataEnd = offset;
			offset = (offset + VertexStreamAlign - 1) & ~(VertexStreamAlign - 1);
		}
		requiredByteCount = checked((int)dataEnd);
		return streams;
	}
}

	public readonly record struct TypeTreeMeshVertexPayload(int VertexCount, byte[] EmbeddedData, IReadOnlyList<TypeTreeVertexChannel> Channels);
	public readonly record struct TypeTreeVertexChannel(byte Stream, byte Offset, byte Format, byte Dimension);
	public readonly record struct TypeTreeSkinnedMeshRendererData(IUnityObjectBase Mesh, ITransform[] Bones, IMaterial[] Materials);
	public readonly record struct TypeTreeSkinnedMeshRendererReferences(IUnityObjectBase Mesh, ITransform[] Bones, IUnityObjectBase[] Materials, Bounds RendererBounds, bool HasRendererBounds);
