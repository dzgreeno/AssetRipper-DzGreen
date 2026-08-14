using AssetRipper.Tools.Common;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AssetRipper.Tools.MCP;

internal static class Program
{
	public static async Task Main()
	{
		McpStdioServer server = new(Console.In, Console.Out, Console.Error, new AssetRipperToolService());
		await server.RunAsync();
	}
}

internal sealed class McpStdioServer(TextReader input, TextWriter output, TextWriter error, AssetRipperToolService service)
{
	private const string CurrentProtocolVersion = "2026-07-28";
	private const string PreviousProtocolVersion = "2025-11-25";
	private readonly SemaphoreSlim writeLock = new(1, 1);
	private readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = false };

	public async Task RunAsync()
	{
		while (await input.ReadLineAsync() is { } line)
		{
			if (string.IsNullOrWhiteSpace(line))
			{
				continue;
			}
			try
			{
				using JsonDocument document = JsonDocument.Parse(line);
				JsonElement request = document.RootElement;
				await HandleRequestAsync(request);
			}
			catch (JsonException exception)
			{
				await error.WriteLineAsync($"MCP parse error: {exception.Message}");
				await WriteErrorAsync(null, -32700, $"Parse error: {exception.Message}");
			}
			catch (Exception exception)
			{
				await error.WriteLineAsync($"MCP server error: {exception.Message}");
				await WriteErrorAsync(null, -32603, exception.Message);
			}
		}
	}

	private async Task HandleRequestAsync(JsonElement request)
	{
		if (request.ValueKind != JsonValueKind.Object || !request.TryGetProperty("method", out JsonElement methodElement))
		{
			await WriteErrorAsync(GetId(request), -32600, "Invalid Request");
			return;
		}
		string method = methodElement.GetString() ?? string.Empty;
		JsonElement? id = GetId(request);
		JsonElement arguments = request.TryGetProperty("params", out JsonElement parameters) && parameters.ValueKind == JsonValueKind.Object
			? parameters
			: default;

		switch (method)
		{
			case "initialize":
				await WriteResultAsync(id, new JsonObject
				{
					["protocolVersion"] = NegotiateProtocolVersion(arguments),
					["capabilities"] = new JsonObject { ["tools"] = new JsonObject { ["listChanged"] = false } },
					["serverInfo"] = new JsonObject { ["name"] = "assetripper", ["title"] = "AssetRipper Asset Processing Server", ["version"] = "1.3.14-custom" },
					["instructions"] = "Inspect Unity assets and export grouped FBX scenes. Export tools write only to the explicitly supplied output directory and do not decrypt or bypass protected content."
				});
				break;
			case "notifications/initialized":
				break;
			case "ping":
				await WriteResultAsync(id, new JsonObject());
				break;
			case "tools/list":
				await WriteResultAsync(id, new JsonObject { ["resultType"] = "complete", ["tools"] = ToolDefinitions() });
				break;
			case "tools/call":
				await HandleToolCallAsync(id, arguments);
				break;
			default:
				if (id is not null)
				{
					await WriteErrorAsync(id, -32601, $"Method not found: {method}");
				}
				break;
		}
	}

	private async Task HandleToolCallAsync(JsonElement? id, JsonElement parameters)
	{
		if (parameters.ValueKind != JsonValueKind.Object || !parameters.TryGetProperty("name", out JsonElement nameElement))
		{
			await WriteErrorAsync(id, -32602, "tools/call requires params.name and params.arguments.");
			return;
		}
		string name = nameElement.GetString() ?? string.Empty;
		JsonElement arguments = parameters.TryGetProperty("arguments", out JsonElement args) && args.ValueKind == JsonValueKind.Object ? args : default;
		try
		{
			EnsureLoadedIfRequested(arguments);
			object result = name switch
			{
				"list_assets" => new { assets = service.ListAssets(GetString(arguments, "filter"), GetInt(arguments, "limit", 2000)) },
				"inspect_prefab" => service.InspectPrefab(GetString(arguments, "filter")),
				"export_fbx_with_anim" => service.ExportFbxWithAnimation(GetString(arguments, "filter"), RequireString(arguments, "outputDirectory"), GetBool(arguments, "includeAnim", true)),
				"batch_process" => service.BatchProcess(RequireString(arguments, "outputDirectory"), GetString(arguments, "filter"), GetBool(arguments, "raw", false), GetBool(arguments, "fbx", true), GetBool(arguments, "includeAnim", true)),
				_ => throw new ToolNotFoundException(name)
			};
			await WriteToolResultAsync(id, result, false);
		}
		catch (ToolNotFoundException exception)
		{
			await WriteToolResultAsync(id, new { error = exception.Message }, true);
		}
		catch (Exception exception)
		{
			await WriteToolResultAsync(id, new { error = exception.Message, type = exception.GetType().Name }, true);
		}
	}

	private void EnsureLoadedIfRequested(JsonElement arguments)
	{
		if (arguments.ValueKind != JsonValueKind.Object || !arguments.TryGetProperty("inputPaths", out JsonElement inputPaths) || inputPaths.ValueKind != JsonValueKind.Array)
		{
			return;
		}
		string[] paths = inputPaths.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.String).Select(item => item.GetString()!).Where(path => !string.IsNullOrWhiteSpace(path)).ToArray();
		if (paths.Length > 0)
		{
			service.Load(paths);
		}
	}

	private async Task WriteToolResultAsync(JsonElement? id, object value, bool isError)
	{
		JsonNode payload = JsonSerializer.SerializeToNode(value, jsonOptions) ?? new JsonObject();
		string text = payload.ToJsonString(jsonOptions);
		JsonObject result = new()
		{
			["resultType"] = "complete",
			["content"] = new JsonArray(new JsonObject { ["type"] = "text", ["text"] = text }),
			["structuredContent"] = payload,
			["isError"] = isError
		};
		await WriteResultAsync(id, result);
	}

	private async Task WriteResultAsync(JsonElement? id, JsonNode result)
	{
		JsonObject response = new() { ["jsonrpc"] = "2.0", ["id"] = id is null ? null : JsonNode.Parse(id.Value.GetRawText()), ["result"] = result };
		await WriteJsonAsync(response);
	}

	private async Task WriteErrorAsync(JsonElement? id, int code, string message)
	{
		JsonObject response = new()
		{
			["jsonrpc"] = "2.0",
			["id"] = id is null ? null : JsonNode.Parse(id.Value.GetRawText()),
			["error"] = new JsonObject { ["code"] = code, ["message"] = message }
		};
		await WriteJsonAsync(response);
	}

	private async Task WriteJsonAsync(JsonObject response)
	{
		await writeLock.WaitAsync();
		try
		{
			await output.WriteLineAsync(response.ToJsonString(jsonOptions));
			await output.FlushAsync();
		}
		finally
		{
			writeLock.Release();
		}
	}

	private static JsonElement? GetId(JsonElement request)
	{
		return request.ValueKind == JsonValueKind.Object && request.TryGetProperty("id", out JsonElement id) ? id.Clone() : null;
	}

	private static string NegotiateProtocolVersion(JsonElement parameters)
	{
		if (parameters.ValueKind == JsonValueKind.Object && parameters.TryGetProperty("protocolVersion", out JsonElement requested) && requested.ValueKind == JsonValueKind.String)
		{
			string value = requested.GetString()!;
			if (value == CurrentProtocolVersion || value == PreviousProtocolVersion)
			{
				return value;
			}
		}
		return CurrentProtocolVersion;
	}

	private static string? GetString(JsonElement arguments, string name)
	{
		return arguments.ValueKind == JsonValueKind.Object && arguments.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
	}

	private static string RequireString(JsonElement arguments, string name) => GetString(arguments, name) is { Length: > 0 } value ? value : throw new ArgumentException($"Missing required argument: {name}");

	private static bool GetBool(JsonElement arguments, string name, bool fallback) => arguments.ValueKind == JsonValueKind.Object && arguments.TryGetProperty(name, out JsonElement value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False ? value.GetBoolean() : fallback;

	private static int GetInt(JsonElement arguments, string name, int fallback) => arguments.ValueKind == JsonValueKind.Object && arguments.TryGetProperty(name, out JsonElement value) && value.TryGetInt32(out int result) ? Math.Clamp(result, 1, 10000) : fallback;

	private static JsonArray ToolDefinitions() => new(
		Tool("list_assets", "List processed Unity assets with optional name/class/collection/Path ID filtering.", new JsonObject
		{
			["type"] = "object",
			["properties"] = new JsonObject
			{
				["inputPaths"] = new JsonObject { ["type"] = "array", ["items"] = new JsonObject { ["type"] = "string" } },
				["filter"] = new JsonObject { ["type"] = "string" },
				["limit"] = new JsonObject { ["type"] = "integer", ["minimum"] = 1, ["maximum"] = 10000 }
			}
		}),
		Tool("inspect_prefab", "Inspect a resolved Unity Prefab or character root, including hierarchy, meshes, materials, textures, clips, bones, and skin-weight diagnostics.", new JsonObject
		{
			["type"] = "object",
			["properties"] = new JsonObject
			{
				["inputPaths"] = new JsonObject { ["type"] = "array", ["items"] = new JsonObject { ["type"] = "string" } },
				["filter"] = new JsonObject { ["type"] = "string", ["description"] = "Character or prefab name, class, collection, or Path ID." }
			}
		}),
		Tool("export_fbx_with_anim", "Export one resolved character/prefab as grouped FBX with hierarchy, materials, texture sidecars, skin clusters, and optional AnimationClip TRS curves.", new JsonObject
		{
			["type"] = "object",
			["required"] = new JsonArray("outputDirectory"),
			["properties"] = new JsonObject
			{
				["inputPaths"] = new JsonObject { ["type"] = "array", ["items"] = new JsonObject { ["type"] = "string" } },
				["filter"] = new JsonObject { ["type"] = "string" },
				["outputDirectory"] = new JsonObject { ["type"] = "string" },
				["includeAnim"] = new JsonObject { ["type"] = "boolean", ["default"] = true }
			}
		}),
		Tool("batch_process", "Load Unity files and run a controlled batch export of raw JSON assets and/or grouped FBX character scenes.", new JsonObject
		{
			["type"] = "object",
			["required"] = new JsonArray("outputDirectory"),
			["properties"] = new JsonObject
			{
				["inputPaths"] = new JsonObject { ["type"] = "array", ["items"] = new JsonObject { ["type"] = "string" } },
				["filter"] = new JsonObject { ["type"] = "string" },
				["outputDirectory"] = new JsonObject { ["type"] = "string" },
				["raw"] = new JsonObject { ["type"] = "boolean", ["default"] = false },
				["fbx"] = new JsonObject { ["type"] = "boolean", ["default"] = true },
				["includeAnim"] = new JsonObject { ["type"] = "boolean", ["default"] = true }
			}
		}));

	private static JsonObject Tool(string name, string description, JsonObject schema) => new() { ["name"] = name, ["description"] = description, ["inputSchema"] = schema, ["annotations"] = new JsonObject { ["readOnlyHint"] = name is "list_assets" or "inspect_prefab", ["destructiveHint"] = false } };

	private sealed class ToolNotFoundException(string name) : Exception($"Unknown tool: {name}");
}
