namespace AssetRipper.GUI.Web;

/// <summary>
/// Centralized fork identity used by the web shell and page titles.
/// Keeping these values in one place prevents the UI and release documents
/// from drifting apart.
/// </summary>
public static class AssetRipperBrand
{
	public const string ProductName = "AssetRipper DzGreen";
	public const string Maintainer = "dzgreeno";
	public const string UpstreamUrl = "https://github.com/AssetRipper/AssetRipper";
	public const string ForkUrl = "https://github.com/dzgreeno/AssetRipper-DzGreen";
	public const string SponsorUrl = "https://ko-fi.com/dzgreen";
	public const string VersionLine = "Advanced fork · v1.3.15-dzgreen.10";
	public static bool IsPremiumEdition => string.Equals(Environment.GetEnvironmentVariable("ASSET_RIPPER_DZGREEN_EDITION"), "Premium", StringComparison.OrdinalIgnoreCase);
}
