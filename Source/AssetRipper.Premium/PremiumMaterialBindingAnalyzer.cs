using AssetRipper.Assets;
using AssetRipper.Assets.Bundles;
using AssetRipper.SourceGenerated.Classes.ClassID_21;
using AssetRipper.SourceGenerated.Classes.ClassID_28;
using AssetRipper.SourceGenerated.Extensions;
using AssetRipper.SourceGenerated.Subclasses.UnityTexEnv;
using System.Globalization;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;

namespace AssetRipper.Premium;

/// <summary>
/// Creates a read-only inventory of material texture properties exposed by imported Unity assets.
/// Shader bytecode is not inspected; the result contains only serialized property names, PPtr
/// resolution outcomes, and texture transform values already available to the standard exporter.
/// </summary>
public static class PremiumMaterialBindingAnalyzer
{
	public static PremiumMaterialBindingReport Create(GameBundle gameBundle)
	{
		ArgumentNullException.ThrowIfNull(gameBundle);
		IEnumerable<PremiumMaterialBinding> materials = gameBundle.FetchAssets()
			.OfType<IMaterial>()
			.Select(CreateBinding);
		return Analyze(materials);
	}

	public static PremiumMaterialBindingReport Analyze(IEnumerable<PremiumMaterialBinding> materials)
	{
		ArgumentNullException.ThrowIfNull(materials);
		PremiumMaterialBinding[] orderedMaterials = materials
			.OrderBy(static material => material.CollectionPath, StringComparer.OrdinalIgnoreCase)
			.ThenBy(static material => material.PathID)
			.ToArray();
		PremiumTextureBinding[] textures = orderedMaterials.SelectMany(static material => material.Textures).ToArray();
		return new PremiumMaterialBindingReport(
			orderedMaterials.LongLength,
			textures.LongLength,
			textures.LongCount(static texture => texture.Status == PremiumTextureBindingStatus.Resolved),
			textures.LongCount(static texture => texture.Status == PremiumTextureBindingStatus.Unresolved),
			textures.LongCount(static texture => texture.Status == PremiumTextureBindingStatus.Null),
			orderedMaterials);
	}

	private static PremiumMaterialBinding CreateBinding(IMaterial material)
	{
		PremiumTextureBinding[] textures = material.GetTextureProperties()
			.Select(pair => CreateTextureBinding(material, pair.Key.String, pair.Value))
			.OrderBy(static texture => texture.PropertyName, StringComparer.Ordinal)
			.ToArray();
		string collectionPath = string.IsNullOrWhiteSpace(material.Collection.FilePath) ? material.Collection.Name : material.Collection.FilePath;
		return new PremiumMaterialBinding(collectionPath, material.PathID, material.Name.String, textures);
	}

	private static PremiumTextureBinding CreateTextureBinding(IMaterial material, string propertyName, IUnityTexEnv environment)
	{
		(float scaleX, float scaleY) = ReadVector2(environment.Scale);
		(float offsetX, float offsetY) = ReadVector2(environment.Offset);
		IUnityObjectBase? texture = environment.Texture.TryGetAsset(material.Collection);
		if (texture is null)
		{
			return new PremiumTextureBinding(propertyName, null, null, scaleX, scaleY, offsetX, offsetY, PremiumTextureBindingStatus.Null);
		}
		if (texture is not ITexture2D texture2D)
		{
			return new PremiumTextureBinding(propertyName, texture.PathID, texture.ClassName, scaleX, scaleY, offsetX, offsetY, PremiumTextureBindingStatus.Unresolved);
		}
		return new PremiumTextureBinding(propertyName, texture2D.PathID, texture2D.Name.String, scaleX, scaleY, offsetX, offsetY, PremiumTextureBindingStatus.Resolved);
	}

	[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "UnityTexEnv uses generated public X/Y value members across supported source variants.")]
	private static (float X, float Y) ReadVector2(object value)
	{
		Type type = value.GetType();
		return (ReadComponent(type, value, "X"), ReadComponent(type, value, "Y"));
	}

	[UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "The generated UnityTexEnv vector types expose stable public X/Y value members.")]
	private static float ReadComponent([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] Type type, object value, string name)
	{
		object? component = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance)?.GetValue(value)
			?? type.GetField(name, BindingFlags.Public | BindingFlags.Instance)?.GetValue(value);
		return component switch
		{
			float single => single,
			double doubleValue => (float)doubleValue,
			IConvertible convertible => convertible.ToSingle(CultureInfo.InvariantCulture),
			_ => 0.0f,
		};
	}
}

public enum PremiumTextureBindingStatus
{
	Resolved,
	Unresolved,
	Null,
}

public sealed record PremiumTextureBinding(
	string PropertyName,
	long? TexturePathID,
	string? TextureName,
	float ScaleX,
	float ScaleY,
	float OffsetX,
	float OffsetY,
	PremiumTextureBindingStatus Status);

public sealed record PremiumMaterialBinding(
	string CollectionPath,
	long PathID,
	string MaterialName,
	IReadOnlyList<PremiumTextureBinding> Textures);

public sealed record PremiumMaterialBindingReport(
	long MaterialCount,
	long TextureBindingCount,
	long ResolvedTextureBindingCount,
	long UnresolvedTextureBindingCount,
	long NullTextureBindingCount,
	IReadOnlyList<PremiumMaterialBinding> Materials);
