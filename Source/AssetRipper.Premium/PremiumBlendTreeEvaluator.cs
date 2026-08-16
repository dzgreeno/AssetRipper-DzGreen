using System.Numerics;

namespace AssetRipper.Premium;

/// <summary>
/// Deterministically evaluates explicitly identified blend-tree inputs. This utility is intentionally
/// decoupled from opaque controller bytes: callers must provide a known blend mode and fully decoded
/// child thresholds or positions before weights are calculated.
/// </summary>
public static class PremiumBlendTreeEvaluator
{
	public static PremiumBlendTreeWeightResult Evaluate1D(IEnumerable<PremiumBlendTree1DChild> children, float parameter)
	{
		ArgumentNullException.ThrowIfNull(children);
		PremiumBlendTree1DChild[] ordered = children.OrderBy(static child => child.Threshold).ThenBy(static child => child.MotionId, StringComparer.Ordinal).ToArray();
		if (ordered.Length == 0 || !float.IsFinite(parameter) || ordered.Any(static child => !float.IsFinite(child.Threshold)))
		{
			return PremiumBlendTreeWeightResult.Rejected("1D evaluation requires finite parameter and at least one finite child threshold.");
		}
		if (ordered.Length == 1 || parameter <= ordered[0].Threshold)
		{
			return PremiumBlendTreeWeightResult.Accepted([new PremiumBlendTreeWeight(ordered[0].MotionId, 1)]);
		}
		if (parameter >= ordered[^1].Threshold)
		{
			return PremiumBlendTreeWeightResult.Accepted([new PremiumBlendTreeWeight(ordered[^1].MotionId, 1)]);
		}

		for (int index = 0; index < ordered.Length - 1; index++)
		{
			PremiumBlendTree1DChild left = ordered[index];
			PremiumBlendTree1DChild right = ordered[index + 1];
			if (parameter > right.Threshold)
			{
				continue;
			}
			float range = right.Threshold - left.Threshold;
			if (range <= 0)
			{
				return PremiumBlendTreeWeightResult.Rejected("1D blend tree contains duplicate or descending thresholds.");
			}
			float rightWeight = (parameter - left.Threshold) / range;
			return PremiumBlendTreeWeightResult.Accepted(
			[
				new PremiumBlendTreeWeight(left.MotionId, 1 - rightWeight),
				new PremiumBlendTreeWeight(right.MotionId, rightWeight),
			]);
		}
		return PremiumBlendTreeWeightResult.Rejected("No 1D blend interval could be determined.");
	}

	public static PremiumBlendTreeWeightResult EvaluateInverseDistance2D(IEnumerable<PremiumBlendTree2DChild> children, Vector2 parameter)
	{
		ArgumentNullException.ThrowIfNull(children);
		PremiumBlendTree2DChild[] ordered = children.OrderBy(static child => child.MotionId, StringComparer.Ordinal).ToArray();
		if (ordered.Length == 0 || !float.IsFinite(parameter.X) || !float.IsFinite(parameter.Y) || ordered.Any(static child => !float.IsFinite(child.Position.X) || !float.IsFinite(child.Position.Y)))
		{
			return PremiumBlendTreeWeightResult.Rejected("2D evaluation requires finite parameter coordinates and at least one finite child position.");
		}
		const float ExactPositionEpsilonSquared = 1e-12f;
		float[] inverseDistances = new float[ordered.Length];
		float sum = 0;
		for (int index = 0; index < ordered.Length; index++)
		{
			float distanceSquared = Vector2.DistanceSquared(parameter, ordered[index].Position);
			if (distanceSquared <= ExactPositionEpsilonSquared)
			{
				return PremiumBlendTreeWeightResult.Accepted([new PremiumBlendTreeWeight(ordered[index].MotionId, 1)]);
			}
			inverseDistances[index] = 1 / MathF.Sqrt(distanceSquared);
			sum += inverseDistances[index];
		}
		if (!float.IsFinite(sum) || sum <= 0)
		{
			return PremiumBlendTreeWeightResult.Rejected("2D inverse-distance normalization failed.");
		}
		return PremiumBlendTreeWeightResult.Accepted(ordered.Select((child, index) => new PremiumBlendTreeWeight(child.MotionId, inverseDistances[index] / sum)).ToArray());
	}
}

public sealed record PremiumBlendTree1DChild(string MotionId, float Threshold);
public sealed record PremiumBlendTree2DChild(string MotionId, Vector2 Position);
public sealed record PremiumBlendTreeWeight(string MotionId, float Weight);
public sealed record PremiumBlendTreeWeightResult(bool IsSuccess, IReadOnlyList<PremiumBlendTreeWeight> Weights, string? Message)
{
	public static PremiumBlendTreeWeightResult Accepted(IReadOnlyList<PremiumBlendTreeWeight> weights) => new(true, weights, null);
	public static PremiumBlendTreeWeightResult Rejected(string message) => new(false, [], message);
}
