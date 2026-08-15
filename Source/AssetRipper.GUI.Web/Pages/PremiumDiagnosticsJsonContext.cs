using AssetRipper.Premium;
using System.Text.Json.Serialization;

namespace AssetRipper.GUI.Web.Pages;

[JsonSerializable(typeof(PremiumImportDiagnosticReport))]
internal partial class PremiumDiagnosticsJsonContext : JsonSerializerContext
{
}
