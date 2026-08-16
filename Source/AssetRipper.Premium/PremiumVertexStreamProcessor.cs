using AssetRipper.Assets.Bundles;
using AssetRipper.SourceGenerated.Classes.ClassID_43;
using AssetRipper.SourceGenerated.Extensions;
using AssetRipper.Primitives;
using AssetRipper.IO.Endian;
using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.InteropServices;

namespace AssetRipper.Premium;

/// <summary>
/// Decodes only schema-described IMesh vertex channels. The processor never infers a stride,
/// channel format, or byte range; unsupported layouts are returned as diagnostics.
/// </summary>
public static class PremiumVertexStreamProcessor
{
	private const int PositionChannel = 0;
	private const int NormalChannel = 1;
	private const int TangentChannel = 2;

	public static PremiumVertexStreamResult Process(IMesh mesh)
	{
		ArgumentNullException.ThrowIfNull(mesh);
		return Process(VertexDataBlob.Create(mesh));
	}

	public static PremiumVertexStreamResult Process(VertexDataBlob blob)
	{
		List<PremiumVertexStreamIssue> issues = new();
		Vector3[]? positions = DecodeVector3(blob, PositionChannel, PremiumVertexSemantic.Position, allowSnorm: false, issues);
		Vector3[]? normals = DecodeVector3(blob, NormalChannel, PremiumVertexSemantic.Normal, allowSnorm: true, issues);
		Vector4[]? tangents = DecodeVector4(blob, TangentChannel, PremiumVertexSemantic.Tangent, allowSnorm: true, issues);
		return new PremiumVertexStreamResult(blob.VertexCount, positions, normals, tangents, issues);
	}

	public static PremiumVertexStreamDiagnostics CreateDiagnostics(GameBundle gameBundle)
	{
		ArgumentNullException.ThrowIfNull(gameBundle);
		PremiumVertexStreamResult[] results = gameBundle.FetchAssets()
			.OfType<IMesh>()
			.Select(Process)
			.ToArray();
		PremiumVertexStreamIssueSummary[] issues = results
			.SelectMany(static result => result.Issues)
			.GroupBy(static issue => (issue.Semantic, issue.Code, issue.Message))
			.OrderBy(static group => group.Key.Semantic)
			.ThenBy(static group => group.Key.Code)
			.ThenBy(static group => group.Key.Message, StringComparer.Ordinal)
			.Select(static group => new PremiumVertexStreamIssueSummary(group.Key.Semantic, group.Key.Code, group.Key.Message, group.LongCount()))
			.ToArray();
		return new PremiumVertexStreamDiagnostics(
			results.LongLength,
			results.LongCount(static result => result.HasVerifiedPositions),
			results.LongCount(static result => result.Normals is not null),
			results.LongCount(static result => result.Tangents is not null),
			issues.Sum(static issue => issue.Count),
			issues);
	}

	/// <summary>
	/// Decodes an explicitly declared 10:10:10:2 packed normal channel. This method is not selected
	/// for an IMesh unless an importer supplies this exact layout; it prevents a format guess.
	/// </summary>
	public static bool TryDecodeExplicitSnorm1010102(ReadOnlySpan<byte> data, int vertexCount, int offset, int stride, bool bigEndian, out Vector3[]? normals, out PremiumVertexStreamIssue? issue)
	{
		normals = null;
		issue = null;
		if (!ValidateLayout(data, vertexCount, offset, stride, sizeof(uint), PremiumVertexSemantic.Normal, out string? reason))
		{
			issue = new(PremiumVertexSemantic.Normal, PremiumVertexIssueCode.InvalidLayout, reason!);
			return false;
		}

		normals = new Vector3[vertexCount];
		for (int index = 0; index < vertexCount; index++)
		{
			ReadOnlySpan<byte> source = data.Slice(offset + index * stride, sizeof(uint));
			uint packed = bigEndian
				? BinaryPrimitives.ReadUInt32BigEndian(source)
				: MemoryMarshal.Read<uint>(source);
			normals[index] = PremiumGeometryUnpackers.UnpackSnorm101010(packed);
		}
		return true;
	}

	private static Vector3[]? DecodeVector3(VertexDataBlob blob, int channelIndex, PremiumVertexSemantic semantic, bool allowSnorm, List<PremiumVertexStreamIssue> issues)
	{
		if (!TryGetChannelLayout(blob, channelIndex, semantic, 3, out PremiumVertexChannelLayout layout, out PremiumVertexStreamIssue? issue))
		{
			if (issue is not null)
			{
				issues.Add(issue);
			}
			return null;
		}
		if (!IsSupportedFormat(layout.Format, allowSnorm))
		{
			issues.Add(new(semantic, PremiumVertexIssueCode.UnsupportedFormat, $"{semantic} uses {layout.Format}, which is not a declared Premium decode path."));
			return null;
		}

		Vector3[] values = new Vector3[blob.VertexCount];
		ReadOnlySpan<byte> data = blob.Data;
		for (int index = 0; index < values.Length; index++)
		{
			ReadOnlySpan<byte> source = GetVertexSource(data, layout, index);
			values[index] = new Vector3(
				ReadComponent(source, 0, layout),
				ReadComponent(source, 1, layout),
				ReadComponent(source, 2, layout));
		}
		return values;
	}

	private static Vector4[]? DecodeVector4(VertexDataBlob blob, int channelIndex, PremiumVertexSemantic semantic, bool allowSnorm, List<PremiumVertexStreamIssue> issues)
	{
		if (!TryGetChannelLayout(blob, channelIndex, semantic, 4, out PremiumVertexChannelLayout layout, out PremiumVertexStreamIssue? issue))
		{
			if (issue is not null)
			{
				issues.Add(issue);
			}
			return null;
		}
		if (!IsSupportedFormat(layout.Format, allowSnorm))
		{
			issues.Add(new(semantic, PremiumVertexIssueCode.UnsupportedFormat, $"{semantic} uses {layout.Format}, which is not a declared Premium decode path."));
			return null;
		}

		Vector4[] values = new Vector4[blob.VertexCount];
		ReadOnlySpan<byte> data = blob.Data;
		for (int index = 0; index < values.Length; index++)
		{
			ReadOnlySpan<byte> source = GetVertexSource(data, layout, index);
			values[index] = new Vector4(
				ReadComponent(source, 0, layout),
				ReadComponent(source, 1, layout),
				ReadComponent(source, 2, layout),
				ReadComponent(source, 3, layout));
		}
		return values;
	}

	private static bool TryGetChannelLayout(VertexDataBlob blob, int channelIndex, PremiumVertexSemantic semantic, int requiredDimension, out PremiumVertexChannelLayout layout, out PremiumVertexStreamIssue? issue)
	{
		layout = default;
		issue = null;
		if (channelIndex >= blob.Channels.Count)
		{
			issue = new(semantic, PremiumVertexIssueCode.ChannelUnavailable, "The known vertex channel is absent from this mesh schema.");
			return false;
		}

		var channel = blob.Channels[channelIndex];
		int dimension = channel.GetDataDimension();
		if (dimension < requiredDimension)
		{
			issue = new(semantic, PremiumVertexIssueCode.InvalidDimension, $"{semantic} requires at least {requiredDimension} components but the schema declares {dimension}.");
			return false;
		}
		if (channel.Stream >= blob.Streams.Count)
		{
			issue = new(semantic, PremiumVertexIssueCode.InvalidLayout, $"{semantic} references missing stream {channel.Stream}.");
			return false;
		}

		var stream = blob.Streams[channel.Stream];
		if ((stream.ChannelMask & (1u << channelIndex)) == 0)
		{
			issue = new(semantic, PremiumVertexIssueCode.ChannelUnavailable, $"{semantic} is not enabled by the stream channel mask.");
			return false;
		}
		MeshHelper.VertexFormat format;
		try
		{
			format = MeshHelper.ToVertexFormat(channel.Format, blob.Version);
		}
		catch (ArgumentOutOfRangeException)
		{
			issue = new(semantic, PremiumVertexIssueCode.UnsupportedFormat, $"{semantic} uses an unknown vertex format code {channel.Format}.");
			return false;
		}

		int componentSize = MeshHelper.GetFormatSize(format);
		int offset = checked((int)stream.Offset + channel.Offset);
		int stride = checked((int)stream.GetStride());
		if (!ValidateLayout(blob.Data, blob.VertexCount, offset, stride, dimension * componentSize, semantic, out string? reason))
		{
			issue = new(semantic, PremiumVertexIssueCode.InvalidLayout, reason!);
			return false;
		}
		layout = new(offset, stride, componentSize, format, blob.EndianType == EndianType.BigEndian);
		return true;
	}

	private static bool IsSupportedFormat(MeshHelper.VertexFormat format, bool allowSnorm)
	{
		return format is MeshHelper.VertexFormat.kVertexFormatFloat or MeshHelper.VertexFormat.kVertexFormatFloat16
			|| allowSnorm && format is MeshHelper.VertexFormat.kVertexFormatSNorm8 or MeshHelper.VertexFormat.kVertexFormatSNorm16;
	}

	private static ReadOnlySpan<byte> GetVertexSource(ReadOnlySpan<byte> data, PremiumVertexChannelLayout layout, int vertexIndex)
	{
		return data.Slice(layout.Offset + vertexIndex * layout.Stride, layout.Stride);
	}

	private static float ReadComponent(ReadOnlySpan<byte> vertex, int componentIndex, PremiumVertexChannelLayout layout)
	{
		ReadOnlySpan<byte> source = vertex.Slice(componentIndex * layout.ComponentSize, layout.ComponentSize);
		return layout.Format switch
		{
			MeshHelper.VertexFormat.kVertexFormatFloat => layout.BigEndian
				? BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32BigEndian(source))
				: MemoryMarshal.Read<float>(source),
			MeshHelper.VertexFormat.kVertexFormatFloat16 => PremiumGeometryUnpackers.HalfToSingle(layout.BigEndian
				? BinaryPrimitives.ReadUInt16BigEndian(source)
				: MemoryMarshal.Read<ushort>(source)),
			MeshHelper.VertexFormat.kVertexFormatSNorm8 => Math.Max((int)sbyte.MinValue + 1, unchecked((int)(sbyte)source[0])) / (float)sbyte.MaxValue,
			MeshHelper.VertexFormat.kVertexFormatSNorm16 => Math.Max((int)short.MinValue + 1, layout.BigEndian
				? (int)BinaryPrimitives.ReadInt16BigEndian(source)
				: (int)MemoryMarshal.Read<short>(source)) / (float)short.MaxValue,
			_ => throw new InvalidOperationException($"Unsupported verified vertex format {layout.Format}."),
		};
	}

	private static bool ValidateLayout(ReadOnlySpan<byte> data, int vertexCount, int offset, int stride, int channelSize, PremiumVertexSemantic semantic, out string? reason)
	{
		reason = null;
		if (vertexCount < 0 || offset < 0 || stride < channelSize || channelSize <= 0)
		{
			reason = $"{semantic} declares an invalid vertex count, offset, stride, or channel size.";
			return false;
		}
		if (vertexCount == 0)
		{
			return true;
		}
		long finalOffset = (long)offset + (long)(vertexCount - 1) * stride + channelSize;
		if (finalOffset > data.Length)
		{
			reason = $"{semantic} extends to byte {finalOffset}, beyond the readable stream length {data.Length}.";
			return false;
		}
		return true;
	}

	private readonly record struct PremiumVertexChannelLayout(int Offset, int Stride, int ComponentSize, MeshHelper.VertexFormat Format, bool BigEndian);
}

public enum PremiumVertexSemantic
{
	Position,
	Normal,
	Tangent,
}

public enum PremiumVertexIssueCode
{
	ChannelUnavailable,
	InvalidDimension,
	InvalidLayout,
	UnsupportedFormat,
}

public sealed record PremiumVertexStreamIssue(PremiumVertexSemantic Semantic, PremiumVertexIssueCode Code, string Message);

public sealed record PremiumVertexStreamResult(
	int VertexCount,
	Vector3[]? Positions,
	Vector3[]? Normals,
	Vector4[]? Tangents,
	IReadOnlyList<PremiumVertexStreamIssue> Issues)
{
	public bool HasVerifiedPositions => Positions is not null && Positions.Length == VertexCount;
}

public sealed record PremiumVertexStreamIssueSummary(PremiumVertexSemantic Semantic, PremiumVertexIssueCode Code, string Message, long Count);

public sealed record PremiumVertexStreamDiagnostics(
	long MeshCount,
	long PositionVerifiedMeshCount,
	long NormalVerifiedMeshCount,
	long TangentVerifiedMeshCount,
	long IssueCount,
	IReadOnlyList<PremiumVertexStreamIssueSummary> Issues);
