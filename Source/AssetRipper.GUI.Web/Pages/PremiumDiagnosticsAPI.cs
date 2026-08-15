using AssetRipper.Premium;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace AssetRipper.GUI.Web.Pages;

internal static class PremiumDiagnosticsAPI
{
	public static Task GetImportReport(HttpContext context)
	{
		context.Response.Headers.CacheControl = "no-store";
		if (!GameFileLoader.Premium)
		{
			return Results.NotFound().ExecuteAsync(context);
		}
		if (!GameFileLoader.IsLoaded)
		{
			return Results.Conflict("Load authorized plaintext Unity data before requesting the Premium import report.").ExecuteAsync(context);
		}

		PremiumImportDiagnosticReport report = PremiumImportDiagnostics.Create(
			GameFileLoader.GameBundle,
			GameFileLoader.CurrentGameData.ProjectVersion,
			GameFileLoader.LoadedInputPaths);
		return Results.Text(JsonSerializer.Serialize(report, PremiumDiagnosticsJsonContext.Default.PremiumImportDiagnosticReport), "application/json").ExecuteAsync(context);
	}
}
