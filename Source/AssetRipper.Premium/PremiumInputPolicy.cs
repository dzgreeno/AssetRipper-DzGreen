namespace AssetRipper.Premium;

/// <summary>
/// Defines the permitted inputs for Premium. The policy intentionally accepts only user-authorized,
/// plaintext Unity material that can be read by the ordinary importer.
/// </summary>
public static class PremiumInputPolicy
{
	public static PremiumInputAssessment Assess(PremiumInputDescriptor input)
	{
		ArgumentNullException.ThrowIfNull(input);
		if (!input.IsUserAuthorized)
		{
			return PremiumInputAssessment.Rejected("authorization-required", "Premium accepts only data the user is authorized to process.");
		}
		if (input.IsEncrypted)
		{
			return PremiumInputAssessment.Rejected("encrypted-input-not-supported", "Encrypted input is not supported. Supply only authorized plaintext Unity data.");
		}
		if (input.IsRuntimeMemoryDump)
		{
			return PremiumInputAssessment.Rejected("runtime-memory-not-supported", "Runtime memory input is not supported.");
		}
		if (input.UsesCustomVirtualContainer)
		{
			return PremiumInputAssessment.Rejected("custom-vfs-not-supported", "Custom virtual-file containers are not supported.");
		}
		if (input.Kind is PremiumInputKind.Unknown)
		{
			return PremiumInputAssessment.Rejected("unsupported-format", "The input is not a supported plaintext Unity asset or resource format.");
		}
		return PremiumInputAssessment.Accepted("plaintext-supported", "Authorized plaintext Unity input is supported.");
	}
}

public enum PremiumInputKind
{
	Unknown,
	UnityBundle,
	SerializedFile,
	ResourceStream,
	SpriteAtlasSource,
	AudioClipStream,
}

public sealed record PremiumInputDescriptor(
	string Name,
	PremiumInputKind Kind,
	bool IsUserAuthorized,
	bool IsEncrypted = false,
	bool IsRuntimeMemoryDump = false,
	bool UsesCustomVirtualContainer = false);

public sealed record PremiumInputAssessment(bool IsAccepted, string Code, string Message)
{
	public static PremiumInputAssessment Accepted(string code, string message) => new(true, code, message);
	public static PremiumInputAssessment Rejected(string code, string message) => new(false, code, message);
}
