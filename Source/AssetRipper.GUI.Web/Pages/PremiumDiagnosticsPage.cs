using AssetRipper.Assets;
using AssetRipper.Premium;
using AssetRipper.SourceGenerated.Extensions;
using System.Net;

namespace AssetRipper.GUI.Web.Pages;

/// <summary>Read-only visualization of the already imported Premium diagnostic report.</summary>
public sealed class PremiumDiagnosticsPage : DefaultPage
{
	public static PremiumDiagnosticsPage Instance { get; } = new();

	public override string? GetTitle() => "Premium Diagnostics";

	public override void WriteInnerContent(TextWriter writer)
	{
		new H1(writer).Close("Premium Diagnostics");
		if (!GameFileLoader.Premium)
		{
			new P(writer).WithClass("alert alert-warning").Close("This read-only dashboard is available in AssetRipper DzGreen Premium.");
			return;
		}
		if (!GameFileLoader.IsLoaded)
		{
			new P(writer).WithClass("alert alert-info").Close("Load authorized plaintext Unity data to view diagnostics.");
			return;
		}

		PremiumImportDiagnosticReport report = PremiumImportDiagnostics.Create(GameFileLoader.GameBundle, GameFileLoader.CurrentGameData.ProjectVersion, GameFileLoader.LoadedInputPaths);
		PremiumVerifiedOnlyPlan verifiedOnly = PremiumExportOrchestrator.CreateVerifiedOnlyPlan(
			report.TypeTreeCoverage,
			GameFileLoader.GameBundle.FetchAssets().Select(asset => new PremiumExportCandidate(asset.Collection.FilePath, asset.Collection.Name, asset.PathID, asset.GetBestName(), asset.ClassName)));

		new P(writer).WithClass("text-muted").Close("The dashboard is read-only. It reports importer evidence and never alters assets, materials, or exported files.");
		WriteSummary(writer, report, verifiedOnly);
		writer.Write("<label for=\"premium-diagnostics-filter\" class=\"form-label mt-4\">Search unavailable, partial, unresolved, or skipped items</label><input id=\"premium-diagnostics-filter\" class=\"form-control\" type=\"search\" placeholder=\"Filter by path, material, property, or reason\">");
		WriteCoverage(writer, report);
		WriteMaterialBindings(writer, report);
		WriteVerifiedOnly(writer, verifiedOnly);
		writer.Write("<script>(function(){const input=document.getElementById('premium-diagnostics-filter');const rows=document.querySelectorAll('[data-premium-search]');input.addEventListener('input',function(){const q=input.value.toLowerCase();rows.forEach(r=>r.hidden=!r.dataset.premiumSearch.includes(q));});})();</script>");
	}

	private static void WriteSummary(TextWriter writer, PremiumImportDiagnosticReport report, PremiumVerifiedOnlyPlan verifiedOnly)
	{
		writer.Write("<div class=\"row row-cols-1 row-cols-md-4 g-3\">");
		WriteCard(writer, "TypeTree partial/unavailable", $"{report.TypeTreeCoverage.PartialCollectionCount} / {report.TypeTreeCoverage.UnavailableCollectionCount}");
		WriteCard(writer, "Reference graph cycles", report.ReferenceGraph.CycleComponentCount.ToString());
		WriteCard(writer, "Material unresolved/null", $"{report.MaterialBindings.UnresolvedTextureBindingCount} / {report.MaterialBindings.NullTextureBindingCount}");
		WriteCard(writer, "Verified-only eligible/skipped", $"{verifiedOnly.EligibleAssetCount} / {verifiedOnly.SkippedAssetCount}");
		writer.Write("</div>");
	}

	private static void WriteCard(TextWriter writer, string title, string value)
	{
		writer.Write($"<div class=\"col\"><div class=\"card h-100\"><div class=\"card-body\"><div class=\"text-muted small\">{WebUtility.HtmlEncode(title)}</div><strong class=\"fs-4\">{WebUtility.HtmlEncode(value)}</strong></div></div></div>");
	}

	private static void WriteCoverage(TextWriter writer, PremiumImportDiagnosticReport report)
	{
		writer.Write("<h2 class=\"h4 mt-4\">TypeTree coverage</h2><table class=\"table table-sm\"><thead><tr><th>Collection</th><th>State</th><th>Assets</th><th>Reason</th></tr></thead><tbody>");
		foreach (PremiumTypeTreeCollectionCoverage coverage in report.TypeTreeCoverage.Collections.Where(static item => item.State is PremiumTypeTreeCoverageState.Partial or PremiumTypeTreeCoverageState.Unavailable))
		{
			string reason = coverage.State == PremiumTypeTreeCoverageState.Partial ? "One or more serialized types are stripped." : "No embedded TypeTree or readable asset schema is available.";
			WriteRow(writer, $"{coverage.CollectionPath} {coverage.CollectionName} {coverage.State} {reason}", coverage.CollectionPath, coverage.State.ToString(), coverage.AssetCount.ToString(), reason);
		}
		writer.Write("</tbody></table>");
	}

	private static void WriteMaterialBindings(TextWriter writer, PremiumImportDiagnosticReport report)
	{
		writer.Write("<h2 class=\"h4 mt-4\">Material bindings requiring attention</h2><table class=\"table table-sm\"><thead><tr><th>Material</th><th>Property</th><th>Status</th><th>Texture</th></tr></thead><tbody>");
		foreach (PremiumMaterialBinding material in report.MaterialBindings.Materials)
		{
			foreach (PremiumTextureBinding texture in material.Textures.Where(static item => item.Status is PremiumTextureBindingStatus.Unresolved or PremiumTextureBindingStatus.Null))
			{
				WriteRow(writer, $"{material.MaterialName} {texture.PropertyName} {texture.Status} {texture.TextureName}", material.MaterialName, texture.PropertyName, texture.Status.ToString(), texture.TextureName ?? "—");
			}
		}
		writer.Write("</tbody></table>");
	}

	private static void WriteVerifiedOnly(TextWriter writer, PremiumVerifiedOnlyPlan plan)
	{
		writer.Write("<h2 class=\"h4 mt-4\">Verified-only export plan</h2><table class=\"table table-sm\"><thead><tr><th>Asset</th><th>Class</th><th>Decision</th><th>Reason</th></tr></thead><tbody>");
		foreach (PremiumVerifiedAssetDecision decision in plan.Decisions.Where(static item => !item.IsEligible))
		{
			WriteRow(writer, $"{decision.Candidate.Name} {decision.Candidate.ClassName} {decision.Reason}", decision.Candidate.Name, decision.Candidate.ClassName, "Skipped", decision.Reason ?? "—");
		}
		writer.Write("</tbody></table>");
	}

	private static void WriteRow(TextWriter writer, string search, params string[] cells)
	{
		writer.Write($"<tr data-premium-search=\"{WebUtility.HtmlEncode(search.ToLowerInvariant())}\">");
		foreach (string cell in cells)
		{
			writer.Write($"<td>{WebUtility.HtmlEncode(cell)}</td>");
		}
		writer.Write("</tr>");
	}
}
