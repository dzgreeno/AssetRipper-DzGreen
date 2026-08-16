namespace AssetRipper.Premium;

/// <summary>
/// Builds a reviewable Standard-Lit assignment plan from serialized material bindings. It never
/// decompiles a shader, edits a source material, or treats an unknown property as a standard one.
/// </summary>
public static class PremiumShaderPropertyInjector
{
	public static PremiumShaderInjectionReport Create(PremiumMaterialBindingReport materials, PremiumShaderTarget target, PremiumFallbackTextureCatalog? fallbackTextures = null)
	{
		ArgumentNullException.ThrowIfNull(materials);
		PremiumShaderMaterialPlan[] plans = materials.Materials
			.OrderBy(static material => material.CollectionPath, StringComparer.OrdinalIgnoreCase)
			.ThenBy(static material => material.PathID)
			.Select(material => CreatePlan(material, target, fallbackTextures))
			.ToArray();
		return new PremiumShaderInjectionReport(target, plans.LongLength, plans.Sum(static plan => plan.Assignments.Count), plans);
	}

	private static PremiumShaderMaterialPlan CreatePlan(PremiumMaterialBinding material, PremiumShaderTarget target, PremiumFallbackTextureCatalog? fallbackTextures)
	{
		PremiumShaderPropertyAssignment[] assignments = material.Textures
			.Select(binding => CreateAssignment(binding, fallbackTextures))
			.OrderBy(static assignment => assignment.SourceProperty, StringComparer.Ordinal)
			.ToArray();
		return new PremiumShaderMaterialPlan(material.CollectionPath, material.PathID, material.MaterialName, target, assignments);
	}

	private static PremiumShaderPropertyAssignment CreateAssignment(PremiumTextureBinding binding, PremiumFallbackTextureCatalog? fallbackTextures)
	{
		string? target = binding.PropertyName switch
		{
			"_MainTex" or "_BaseMap" => "_BaseMap",
			"_BumpMap" or "_NormalMap" => "_NormalMap",
			"_MetallicGlossMap" => "_MetallicGlossMap",
			"_OcclusionMap" => "_OcclusionMap",
			"_EmissionMap" => "_EmissionMap",
			_ => null,
		};
		if (target is null)
		{
			return new PremiumShaderPropertyAssignment(binding.PropertyName, null, PremiumShaderAssignmentStatus.NotMapped, null, "No safe Standard-Lit mapping is declared for this serialized property.");
		}
		if (binding.Status == PremiumTextureBindingStatus.Resolved)
		{
			return new PremiumShaderPropertyAssignment(binding.PropertyName, target, PremiumShaderAssignmentStatus.ResolvedSource, binding.TexturePathID, null);
		}
		if (binding.Status == PremiumTextureBindingStatus.Null)
		{
			return new PremiumShaderPropertyAssignment(binding.PropertyName, target, PremiumShaderAssignmentStatus.NeutralFallbackRequired, null, "The source binding is explicitly null; use the exporter neutral fallback, not a user replacement.");
		}
		PremiumFallbackTexture? fallback = fallbackTextures?.Textures.FirstOrDefault(texture => string.Equals(texture.Key, binding.PropertyName.TrimStart('_'), StringComparison.OrdinalIgnoreCase));
		return fallback is null
			? new PremiumShaderPropertyAssignment(binding.PropertyName, target, PremiumShaderAssignmentStatus.UnresolvedSource, null, "The source texture is unresolved and no matching user fallback was supplied.")
			: new PremiumShaderPropertyAssignment(binding.PropertyName, target, PremiumShaderAssignmentStatus.UserFallbackAvailable, null, fallback.Path);
	}
}

public enum PremiumShaderTarget { UrpLit, HdrpLit }
public enum PremiumShaderAssignmentStatus { ResolvedSource, NeutralFallbackRequired, UserFallbackAvailable, UnresolvedSource, NotMapped }
public sealed record PremiumShaderPropertyAssignment(string SourceProperty, string? TargetProperty, PremiumShaderAssignmentStatus Status, long? SourceTexturePathId, string? Detail);
public sealed record PremiumShaderMaterialPlan(string CollectionPath, long MaterialPathId, string MaterialName, PremiumShaderTarget Target, IReadOnlyList<PremiumShaderPropertyAssignment> Assignments);
public sealed record PremiumShaderInjectionReport(PremiumShaderTarget Target, long MaterialCount, int AssignmentCount, IReadOnlyList<PremiumShaderMaterialPlan> Materials);
