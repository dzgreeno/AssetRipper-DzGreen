using AssetRipper.Export.Configuration;
using AssetRipper.Premium;
using AssetRipper.Tools.Common;
using System.Text.Json;

namespace AssetRipper.Tools.CLI;

internal static class Program
{
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

	public static int Main(string[] args)
	{
		try
		{
			CliOptions options = CliOptions.Parse(args);
			if (options.Help)
			{
				Console.WriteLine(CliOptions.HelpText);
				return 0;
			}
			if (options.Inputs.Count == 0)
			{
				Console.Error.WriteLine("No input path was supplied. Use --input <file-or-directory>.");
				Console.Error.WriteLine(CliOptions.HelpText);
				return 2;
			}

			AssetRipperToolService service = new();
			LoadSummary load = service.Load(options.Inputs, ModelExportFormat.Fbx, options.StrictProcessing);
			PremiumFallbackTextureCatalog? fallbackTextures = options.FallbackTexturesDirectory is null ? null : service.CreateFallbackTextureCatalog(options.FallbackTexturesDirectory);
			string? diagnosticsPath = options.DiagnosticsFormat is null ? null : service.ExportDiagnostics(options.OutputDirectory, options.DiagnosticsFormat.Value, fallbackTextures);
			object result;
			if (options.BatchProcess || options.Raw || options.ExportVerifiedOnly)
			{
				result = new { load, diagnosticsPath, batch = service.BatchProcess(options.OutputDirectory, options.Filter, options.Raw, options.Fbx, options.IncludeAnimations, options.ExportVerifiedOnly, fallbackTextures) };
			}
			else if (options.Fbx)
			{
				result = new { load, diagnosticsPath, fbx = service.ExportFbxWithAnimation(options.Filter, options.OutputDirectory, options.IncludeAnimations), fallbackTextures };
			}
			else if (options.InspectPrefab)
			{
				result = new { load, diagnosticsPath, prefab = service.InspectPrefab(options.Filter), fallbackTextures };
			}
			else
			{
				result = new { load, diagnosticsPath, verifiedOnly = options.ExportVerifiedOnly ? service.CreateVerifiedOnlyPlan() : null, fallbackTextures, assets = service.ListAssets(options.Filter, options.Limit) };
			}
			Console.WriteLine(JsonSerializer.Serialize(result, JsonOptions));
			if (load.Issues.Count > 0)
			{
				Console.Error.WriteLine($"AssetRipper CLI completed with {load.Issues.Count} recoverable processing issue(s). See the JSON issues array or batch manifest.");
				return 3;
			}
			return 0;
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"AssetRipper CLI failed: {ex.Message}");
			return 1;
		}
	}
}

internal sealed class CliOptions
{
	public List<string> Inputs { get; } = [];
	public string OutputDirectory { get; private set; } = Path.Combine(Environment.CurrentDirectory, "AssetRipperOutput");
	public string? Filter { get; private set; }
	public int Limit { get; private set; } = 2000;
	public bool IncludeAnimations { get; private set; } = true;
	public bool Raw { get; private set; }
	public bool Fbx { get; private set; }
	public bool InspectPrefab { get; private set; }
	public bool BatchProcess { get; private set; }
	public bool StrictProcessing { get; private set; }
	public bool ExportVerifiedOnly { get; private set; }
	public string? FallbackTexturesDirectory { get; private set; }
	public PremiumDiagnosticsFormat? DiagnosticsFormat { get; private set; }
	public bool Help { get; private set; }

	public static CliOptions Parse(string[] args)
	{
		CliOptions options = new();
		for (int i = 0; i < args.Length; i++)
		{
			string argument = args[i];
			if (!argument.StartsWith("--", StringComparison.Ordinal))
			{
				options.Inputs.Add(argument);
				continue;
			}
			(string key, string? inlineValue) = Split(argument[2..]);
			switch (key.ToLowerInvariant())
			{
				case "help":
				case "h":
					options.Help = true;
					break;
				case "input":
				case "i":
					options.Inputs.Add(RequireValue(key, inlineValue, args, ref i));
					break;
				case "output":
				case "out":
				case "o":
					options.OutputDirectory = RequireValue(key, inlineValue, args, ref i);
					break;
				case "filter":
				case "asset-filter":
					options.Filter = RequireValue(key, inlineValue, args, ref i);
					break;
				case "limit":
					if (!int.TryParse(RequireValue(key, inlineValue, args, ref i), out int limit) || limit < 1)
					{
						throw new ArgumentException("--limit must be a positive integer.");
					}
					options.Limit = limit;
					break;
				case "include-anim":
					options.IncludeAnimations = ParseBoolean(key, inlineValue, args, ref i, true);
					break;
				case "raw":
					options.Raw = ParseBoolean(key, inlineValue, args, ref i, true);
					break;
				case "fbx":
					options.Fbx = ParseBoolean(key, inlineValue, args, ref i, true);
					break;
				case "inspect-prefab":
				case "inspect":
					options.InspectPrefab = true;
					break;
				case "batch-process":
				case "batch":
					options.BatchProcess = true;
					break;
					case "strict":
						options.StrictProcessing = ParseBoolean(key, inlineValue, args, ref i, true);
						break;
					case "export-verified-only":
						options.ExportVerifiedOnly = ParseBoolean(key, inlineValue, args, ref i, true);
						break;
					case "fallback-textures":
						options.FallbackTexturesDirectory = RequireValue(key, inlineValue, args, ref i);
						break;
					case "export-diagnostics":
						string requestedFormat = RequireValue(key, inlineValue, args, ref i);
						options.DiagnosticsFormat = requestedFormat.ToLowerInvariant() switch
						{
							"json" => PremiumDiagnosticsFormat.Json,
							"html" => PremiumDiagnosticsFormat.Html,
							_ => throw new ArgumentException("--export-diagnostics must be json or html."),
						};
						break;
				default:
					throw new ArgumentException($"Unknown option '--{key}'.");
			}
		}
		return options;
	}

	private static (string Key, string? Value) Split(string argument)
	{
		int equals = argument.IndexOf('=');
		return equals < 0 ? (argument, null) : (argument[..equals], argument[(equals + 1)..]);
	}

	private static string RequireValue(string key, string? inlineValue, string[] args, ref int index)
	{
		if (!string.IsNullOrWhiteSpace(inlineValue))
		{
			return inlineValue;
		}
		if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
		{
			throw new ArgumentException($"Option '--{key}' requires a value.");
		}
		return args[++index];
	}

	private static bool ParseBoolean(string key, string? inlineValue, string[] args, ref int index, bool defaultValue)
	{
		string? value = inlineValue;
		if (value is null && index + 1 < args.Length && !args[index + 1].StartsWith("--", StringComparison.Ordinal) && bool.TryParse(args[index + 1], out _))
		{
			value = args[++index];
		}
		return value is null ? defaultValue : bool.TryParse(value, out bool parsed) ? parsed : throw new ArgumentException($"Option '--{key}' expects true or false.");
	}

	public static string HelpText => """
AssetRipper CLI — Unity asset inspection and grouped FBX export

Usage:
  AssetRipper.CLI --input <file-or-directory> [options]

Core options:
  --input, -i <path>       Unity file or directory. Repeat for multiple roots.
  --output, -o <dir>       Output directory (default: ./AssetRipperOutput).
  --filter <query>         Filter by name, class, collection, or Path ID.
  --limit <n>              Maximum assets in list output (default: 2000).
  --inspect-prefab         Inspect the resolved prefab/character hierarchy.
  --fbx                   Export the selected character/prefab as grouped FBX.
  --include-anim[=bool]    Include AnimationClip TRS curves in FBX (default: true).
	  --raw                   Write raw JSON assets under output/raw.
	  --batch-process         Run batch mode; combine with --raw and/or --fbx.
	  --strict[=bool]         Fail on the first processing error instead of continuing with diagnostics.
	  --export-verified-only  In batch mode, export only assets from Embedded or KnownEngineSchema collections.
	  --fallback-textures <dir>  Deterministically catalog user-supplied replacement textures for the diagnostics/manifest.
	  --export-diagnostics <json|html>  Write the loaded Premium diagnostic report beside export output.
	  --help, -h              Show this help.

Examples:
  AssetRipper.CLI --input game_Data --inspect-prefab --filter hero
  AssetRipper.CLI --input game_Data --output export --fbx --filter hero --include-anim
	  AssetRipper.CLI --input game_Data --output export --batch-process --raw --fbx
	  AssetRipper.CLI --input game_Data --output export --batch --raw --export-verified-only --export-diagnostics html
""";
}
