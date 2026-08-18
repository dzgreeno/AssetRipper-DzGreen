using AssetRipper.Assets;
using System.Numerics;

namespace AssetRipper.Export.Modules.Models;

/// <summary>
/// Selects a recovered component only when a single readable candidate satisfies every
/// structural constraint already declared by the source renderer. This resolver never
/// uses a display name as a sufficient condition and never fabricates a missing link.
/// </summary>
public static class RecoveredAssociationResolver
{
	public static RecoveredAssociationDecision SelectUniqueMesh(
		IEnumerable<RecoveredMeshCandidate> candidates,
		int declaredBoneCount,
		int declaredMaterialCount,
		bool requireRendererBoundsMatch = false)
	{
		ArgumentNullException.ThrowIfNull(candidates);
		if (declaredBoneCount <= 0)
		{
			return RecoveredAssociationDecision.Reject("renderer-no-bones", "The renderer does not declare a non-empty bone list.");
		}
		if (declaredMaterialCount <= 0)
		{
			return RecoveredAssociationDecision.Reject("renderer-no-materials", "The renderer does not declare a non-empty material list.");
		}

		List<RecoveredAssociationEvidence> evidence = [];
		List<RecoveredMeshCandidate> eligible = [];
		RecoveredAssociationRequirementFacts requirements = new(declaredBoneCount, declaredMaterialCount, requireRendererBoundsMatch);
		foreach (RecoveredMeshCandidate candidate in candidates.OrderBy(static candidate => candidate.PathID))
		{
			string? rejection = GetMeshRejection(candidate, declaredBoneCount, declaredMaterialCount, requireRendererBoundsMatch);
			if (rejection is null)
			{
				eligible.Add(candidate);
				evidence.Add(CreateEvidence(candidate, true, "candidate-eligible", "The candidate satisfies all declared mesh, skin, bone, submesh, and bounds constraints."));
			}
			else
			{
				evidence.Add(CreateEvidence(candidate, false, "candidate-rejected", rejection));
			}
		}

		return eligible.Count switch
		{
			0 => new(null, false, "no-unique-candidate", CreateNoCandidateMessage(evidence), evidence, requirements),
			1 => new(eligible[0], true, "recovered-association", "One and only one readable mesh satisfies all source-declared constraints.", evidence, requirements),
			_ => new(null, false, "ambiguous-candidates", $"{eligible.Count} readable meshes satisfy the constraints; automatic association is rejected.", evidence, requirements),
		};
	}

	private static string CreateNoCandidateMessage(IReadOnlyList<RecoveredAssociationEvidence> evidence)
	{
		if (evidence.Count == 0)
		{
			return "No readable Mesh or recovered TypeTree Mesh was available in the loaded input.";
		}
		string reasons = string.Join("; ", evidence
			.Where(static item => !item.Accepted)
			.GroupBy(static item => item.Message, StringComparer.Ordinal)
			.OrderByDescending(static group => group.Count())
			.ThenBy(static group => group.Key, StringComparer.Ordinal)
			.Take(4)
			.Select(static group => $"{group.Count()}× {group.Key}"));
		return $"No readable mesh satisfies all source-declared constraints across {evidence.Count} candidates. Dominant exclusions: {reasons}";
	}

	private static string? GetMeshRejection(RecoveredMeshCandidate candidate, int declaredBoneCount, int declaredMaterialCount, bool requireRendererBoundsMatch)
	{
		if (!candidate.HasPosition || candidate.VertexCount <= 0 || candidate.IndexCount <= 0 || candidate.IndexCount % 3 != 0)
		{
			return "The candidate does not expose valid triangle geometry with POSITION.";
		}
		if (!candidate.HasNonZeroBounds)
		{
			return "The candidate has zero or non-finite bounds.";
		}
		if (requireRendererBoundsMatch && !candidate.MatchesRendererBounds)
		{
			return $"The candidate mesh extents do not match the renderer AABB (extent distance={candidate.BoundsExtentDistance:R}; center distance={candidate.BoundsCenterDistance:R}).";
		}
		if (!candidate.HasSkin)
		{
			return "The candidate does not expose BlendWeight and BlendIndices skin data.";
		}
		if (candidate.BindPoseCount <= 0 || candidate.BindPoseCount > declaredBoneCount)
		{
			return $"The candidate bind-pose count ({candidate.BindPoseCount}) is not a usable prefix of the renderer bone count ({declaredBoneCount}).";
		}
		if (candidate.MaxReferencedBoneIndex < 0 || candidate.MaxReferencedBoneIndex >= candidate.BindPoseCount)
		{
			return "The candidate references a bone index outside its declared bind-pose prefix.";
		}
		if (candidate.SubMeshCount != declaredMaterialCount)
		{
			return $"The candidate submesh count ({candidate.SubMeshCount}) does not match the renderer material count ({declaredMaterialCount}).";
		}
		return null;
	}

	private static RecoveredAssociationEvidence CreateEvidence(RecoveredMeshCandidate candidate, bool accepted, string code, string message)
	{
		RecoveredAssociationCandidateFacts facts = new(
			candidate.CollectionPath,
			candidate.VertexCount,
			candidate.IndexCount,
			candidate.HasPosition,
			candidate.HasSkin,
			candidate.BindPoseCount,
			candidate.MaxReferencedBoneIndex,
			candidate.SubMeshCount,
			candidate.HasNonZeroBounds,
			candidate.MatchesRendererBounds,
			candidate.BoundsCenterDistance,
			candidate.BoundsExtentDistance);
		return new(candidate.PathID, candidate.Name, accepted, code, message, facts);
	}
}

public sealed record RecoveredMeshCandidate(
	long PathID,
	string Name,
	int VertexCount,
	int IndexCount,
	bool HasPosition,
	bool HasSkin,
	int BindPoseCount,
	int MaxReferencedBoneIndex,
	int SubMeshCount,
	bool HasNonZeroBounds,
	IUnityObjectBase? Asset = null,
	Vector3 BoundsCenter = default,
	Vector3 BoundsExtent = default,
	string CollectionPath = "",
	bool MatchesRendererBounds = true,
	float BoundsCenterDistance = 0.0f,
	float BoundsExtentDistance = 0.0f);

public sealed record RecoveredAssociationEvidence(
	long CandidatePathID,
	string CandidateName,
	bool Accepted,
	string Code,
	string Message,
	RecoveredAssociationCandidateFacts? Facts = null);

/// <summary>
/// Primitive-only candidate provenance kept in diagnostics so rejected associations can be audited
/// without serializing a Unity asset instance or exposing unverified recovered content as accepted.
/// </summary>
public sealed record RecoveredAssociationCandidateFacts(
	string CollectionPath,
	int VertexCount,
	int IndexCount,
	bool HasPosition,
	bool HasSkin,
	int BindPoseCount,
	int MaxReferencedBoneIndex,
	int SubMeshCount,
	bool HasNonZeroBounds,
	bool MatchesRendererBounds,
	float BoundsCenterDistance,
	float BoundsExtentDistance);

public sealed record RecoveredAssociationDecision(
	RecoveredMeshCandidate? Candidate,
	bool Accepted,
	string Code,
	string Message,
	IReadOnlyList<RecoveredAssociationEvidence> Evidence,
	RecoveredAssociationRequirementFacts? Requirements = null)
{
	public static RecoveredAssociationDecision Reject(string code, string message) => new(null, false, code, message, []);
}

/// <summary>
/// Constraints declared by the source renderer and applied identically to every candidate.
/// </summary>
public sealed record RecoveredAssociationRequirementFacts(
	int DeclaredBoneCount,
	int DeclaredMaterialCount,
	bool RendererBoundsRequired);
