using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace AssetRipper.Premium;

/// <summary>
/// Selects the local Enterprise recovery profile for readable, user-provided plaintext data.
/// This is a capability selector, not a key-management, decryption, or DRM-bypass mechanism.
/// </summary>
public static partial class EnterpriseAccessGate
{
	public const string EnvironmentVariableName = "ASSET_RIPPER_DZGREEN_RECOVERY_TOKEN";

	public static EnterpriseAccessSession Resolve(string? presentedToken = null)
	{
		string? configuredToken = Environment.GetEnvironmentVariable(EnvironmentVariableName);
		if (!IsValidToken(configuredToken))
		{
			return new EnterpriseAccessSession(EnterpriseAccessMode.DiagnosticOnly, "recovery-token-unavailable", "Set a valid local recovery token and restart the application to select the advanced readable-data profile.");
		}
		if (presentedToken is not null && (!IsValidToken(presentedToken) || !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(configuredToken!), Encoding.UTF8.GetBytes(presentedToken))))
		{
			return new EnterpriseAccessSession(EnterpriseAccessMode.DiagnosticOnly, "recovery-token-mismatch", "The supplied recovery token does not match the configured local token.");
		}
		return new EnterpriseAccessSession(EnterpriseAccessMode.Tier1ReadableData, "recovery-token-accepted", "Advanced reconstruction is enabled for readable plaintext Unity data.");
	}

	public static bool IsValidToken(string? token) => token is not null && TokenPattern().IsMatch(token);

	public static void RequireTier1ReadableData(EnterpriseAccessSession session)
	{
		if (!session.IsTier1ReadableData)
		{
			throw new Tier1AuthorizationRequiredException(session.Message);
		}
	}

	[GeneratedRegex("^[A-Za-z0-9]{6}$", RegexOptions.CultureInvariant)]
	private static partial Regex TokenPattern();
}

public enum EnterpriseAccessMode
{
	DiagnosticOnly,
	Tier1ReadableData,
}

public sealed record EnterpriseAccessSession(EnterpriseAccessMode Mode, string Code, string Message)
{
	public bool IsTier1ReadableData => Mode == EnterpriseAccessMode.Tier1ReadableData;
}

public sealed class Tier1AuthorizationRequiredException : InvalidOperationException
{
	public Tier1AuthorizationRequiredException(string message) : base(message) { }
}
