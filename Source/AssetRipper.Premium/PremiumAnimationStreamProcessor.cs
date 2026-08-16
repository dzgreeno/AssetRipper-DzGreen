using AssetRipper.SourceGenerated.Subclasses.QuaternionCurve;
using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.InteropServices;

namespace AssetRipper.Premium;

/// <summary>
/// Adapts readable quaternion curves into bounded sampled tracks. Low-level packed input is only
/// accepted through an explicit 32-bit Smallest-Three layout supplied by a known schema.
/// </summary>
public static class PremiumAnimationStreamProcessor
{
	private const int MaximumSamples = 1_000_000;

	public static PremiumQuaternionTrack FromReadableCurve(IQuaternionCurve curve)
	{
		ArgumentNullException.ThrowIfNull(curve);
		PremiumQuaternionKey[] keys = curve.Curve.Curve
			.Select(static key => new PremiumQuaternionKey(key.Time, key.Value))
			.OrderBy(static key => key.Time)
			.ToArray();
		return new PremiumQuaternionTrack(curve.Path.String, keys);
	}

	public static bool TryDecodeExplicitSmallestThree(ReadOnlySpan<byte> data, int keyCount, int offset, int stride, bool bigEndian, out PremiumQuaternionKey[]? keys, out PremiumAnimationStreamIssue? issue)
	{
		keys = null;
		issue = null;
		if (keyCount < 0 || offset < 0 || stride < sizeof(uint))
		{
			issue = new(PremiumAnimationIssueCode.InvalidLayout, "The packed quaternion layout declares an invalid key count, offset, or stride.");
			return false;
		}
		if (keyCount > 0 && (long)offset + (long)(keyCount - 1) * stride + sizeof(uint) > data.Length)
		{
			issue = new(PremiumAnimationIssueCode.InvalidLayout, "The packed quaternion stream exceeds the readable buffer length.");
			return false;
		}

		keys = new PremiumQuaternionKey[keyCount];
		for (int index = 0; index < keyCount; index++)
		{
			ReadOnlySpan<byte> source = data.Slice(offset + index * stride, sizeof(uint));
			uint packed = bigEndian
				? BinaryPrimitives.ReadUInt32BigEndian(source)
				: MemoryMarshal.Read<uint>(source);
			keys[index] = new PremiumQuaternionKey(index, PremiumGeometryUnpackers.UnpackSmallestThreeQuaternion(packed));
		}
		return true;
	}

	public static PremiumQuaternionSampleResult Sample(PremiumQuaternionTrack track, int targetFramesPerSecond = 60)
	{
		ArgumentNullException.ThrowIfNull(track);
		if (targetFramesPerSecond is < 1 or > 240)
		{
			return PremiumQuaternionSampleResult.Failed(new(PremiumAnimationIssueCode.InvalidSampleRate, "Target frames per second must be between 1 and 240."));
		}
		if (track.Keys.Count == 0)
		{
			return new([], null);
		}
		if (!IsValidKeySequence(track.Keys, out PremiumAnimationStreamIssue? issue))
		{
			return PremiumQuaternionSampleResult.Failed(issue!);
		}

		float duration = track.Keys[^1].Time;
		long sampleCount = (long)MathF.Floor(duration * targetFramesPerSecond) + 1;
		if (sampleCount > MaximumSamples)
		{
			return PremiumQuaternionSampleResult.Failed(new(PremiumAnimationIssueCode.SampleLimitExceeded, $"Sampling requires {sampleCount} keys, beyond the bounded limit of {MaximumSamples}."));
		}

		PremiumQuaternionKey[] samples = new PremiumQuaternionKey[sampleCount];
		int segment = 0;
		for (int index = 0; index < samples.Length; index++)
		{
			float time = index / (float)targetFramesPerSecond;
			while (segment < track.Keys.Count - 2 && time > track.Keys[segment + 1].Time)
			{
				segment++;
			}
			samples[index] = new(time, Interpolate(track.Keys[segment], track.Keys[Math.Min(segment + 1, track.Keys.Count - 1)], time));
		}
		return new(samples, null);
	}

	private static bool IsValidKeySequence(IReadOnlyList<PremiumQuaternionKey> keys, out PremiumAnimationStreamIssue? issue)
	{
		float lastTime = float.NegativeInfinity;
		for (int index = 0; index < keys.Count; index++)
		{
			PremiumQuaternionKey key = keys[index];
			if (!float.IsFinite(key.Time) || key.Time < lastTime || !IsFinite(key.Value))
			{
				issue = new(PremiumAnimationIssueCode.InvalidKeySequence, "Quaternion keys must have finite nondecreasing times and finite values.");
				return false;
			}
			lastTime = key.Time;
		}
		issue = null;
		return true;
	}

	private static Quaternion Interpolate(PremiumQuaternionKey left, PremiumQuaternionKey right, float time)
	{
		if (right.Time <= left.Time)
		{
			return Quaternion.Normalize(left.Value);
		}
		float amount = Math.Clamp((time - left.Time) / (right.Time - left.Time), 0.0f, 1.0f);
		return Quaternion.Normalize(Quaternion.Slerp(left.Value, right.Value, amount));
	}

	private static bool IsFinite(Quaternion value)
	{
		return float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z) && float.IsFinite(value.W);
	}
}

public sealed record PremiumQuaternionKey(float Time, Quaternion Value);

public sealed record PremiumQuaternionTrack(string Path, IReadOnlyList<PremiumQuaternionKey> Keys);

public enum PremiumAnimationIssueCode
{
	InvalidLayout,
	InvalidSampleRate,
	InvalidKeySequence,
	SampleLimitExceeded,
}

public sealed record PremiumAnimationStreamIssue(PremiumAnimationIssueCode Code, string Message);

public sealed record PremiumQuaternionSampleResult(IReadOnlyList<PremiumQuaternionKey> Keys, PremiumAnimationStreamIssue? Issue)
{
	public bool IsSuccess => Issue is null;

	public static PremiumQuaternionSampleResult Failed(PremiumAnimationStreamIssue issue) => new([], issue);
}
