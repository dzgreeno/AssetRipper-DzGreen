using System.Buffers.Binary;
using System.Text.Json;

if (!Arguments.TryParse(args, out Arguments? options, out string? error))
{
	Console.Error.WriteLine(error);
	return 2;
}

try
{
	Arguments verifiedOptions = options!;
	using JsonDocument inspection = JsonDocument.Parse(File.ReadAllText(verifiedOptions.InspectionPath));
	using JsonDocument glb = ParseGlb(verifiedOptions.GlbPath);
	object report = new
	{
		fixture = verifiedOptions.Fixture,
		provenance = verifiedOptions.Provenance,
		glb = verifiedOptions.GlbPath,
		inspection = ReadInspectionCounts(inspection.RootElement),
		export = ReadGlbCounts(glb.RootElement),
		comparisonPolicy = "Counts are reported side by side. Differences are observations, not fabricated equivalence claims: GLB may split or merge primitives and omit unsupported Unity data.",
	};
	JsonSerializerOptions serializerOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
	File.WriteAllText(verifiedOptions.OutputPath, JsonSerializer.Serialize(report, serializerOptions));
	Console.WriteLine(verifiedOptions.OutputPath);
	return 0;
}
catch (Exception exception)
{
	Console.Error.WriteLine($"RC2 verifier failed: {exception.Message}");
	return 1;
}

static JsonDocument ParseGlb(string path)
{
	byte[] bytes = File.ReadAllBytes(path);
	if (bytes.Length < 20 || BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(0, 4)) != 0x46546C67)
	{
		throw new InvalidDataException("The export is not a valid GLB header.");
	}
	int offset = 12;
	while (offset + 8 <= bytes.Length)
	{
		uint length = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset, 4));
		uint type = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset + 4, 4));
		offset += 8;
		if (length > bytes.Length - offset)
		{
			throw new InvalidDataException("A GLB chunk exceeds the file boundary.");
		}
		if (type == 0x4E4F534A)
		{
			return JsonDocument.Parse(bytes.AsMemory(offset, checked((int)length)));
		}
		offset += checked((int)length);
	}
	throw new InvalidDataException("The GLB JSON chunk was not found.");
}

static object ReadInspectionCounts(JsonElement root)
{
	JsonElement prefab = root.GetProperty("prefab");
	JsonElement meshes = prefab.GetProperty("meshes");
	return new
	{
		meshes = meshes.GetArrayLength(),
		vertices = meshes.EnumerateArray().Sum(static mesh => mesh.GetProperty("vertexCount").GetInt32()),
		bones = prefab.GetProperty("boneCount").GetInt32(),
		bindPoses = meshes.EnumerateArray().Sum(static mesh => mesh.GetProperty("bindPoseCount").GetInt32()),
		blendShapes = meshes.EnumerateArray().Sum(static mesh => mesh.GetProperty("blendShapeCount").GetInt32()),
		clips = prefab.GetProperty("animationClips").GetArrayLength(),
		materials = prefab.GetProperty("materials").GetArrayLength(),
		resolvedTextures = prefab.GetProperty("textures").GetArrayLength(),
	};
}

static object ReadGlbCounts(JsonElement root)
{
	JsonElement accessors = root.TryGetProperty("accessors", out JsonElement accessorElement) ? accessorElement : default;
	int vertices = 0;
	int blendShapes = 0;
	if (root.TryGetProperty("meshes", out JsonElement meshes))
	{
		foreach (JsonElement mesh in meshes.EnumerateArray())
		{
			foreach (JsonElement primitive in mesh.GetProperty("primitives").EnumerateArray())
			{
				if (primitive.TryGetProperty("attributes", out JsonElement attributes) && attributes.TryGetProperty("POSITION", out JsonElement position))
				{
					vertices += AccessorCount(accessors, position.GetInt32());
				}
				if (primitive.TryGetProperty("targets", out JsonElement targets))
				{
					blendShapes += targets.GetArrayLength();
				}
			}
		}
	}
	int bones = 0;
	int bindPoses = 0;
	if (root.TryGetProperty("skins", out JsonElement skins))
	{
		foreach (JsonElement skin in skins.EnumerateArray())
		{
			bones += skin.GetProperty("joints").GetArrayLength();
			if (skin.TryGetProperty("inverseBindMatrices", out JsonElement inverseBindMatrices))
			{
				bindPoses += AccessorCount(accessors, inverseBindMatrices.GetInt32());
			}
		}
	}
	return new
	{
		meshes = Count(root, "meshes"),
		vertices,
		bones,
		bindPoses,
		blendShapes,
		clips = Count(root, "animations"),
		materials = Count(root, "materials"),
		resolvedTextures = Count(root, "textures"),
	};
}

static int Count(JsonElement root, string property) => root.TryGetProperty(property, out JsonElement value) ? value.GetArrayLength() : 0;
static int AccessorCount(JsonElement accessors, int index) => accessors.ValueKind == JsonValueKind.Array && index >= 0 && index < accessors.GetArrayLength() ? accessors[index].GetProperty("count").GetInt32() : 0;

sealed record Arguments(string Fixture, string Provenance, string InspectionPath, string GlbPath, string OutputPath)
{
	public static bool TryParse(string[] args, out Arguments? options, out string? error)
	{
		Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);
		for (int index = 0; index < args.Length; index += 2)
		{
			if (index + 1 >= args.Length || !args[index].StartsWith("--", StringComparison.Ordinal))
			{
				options = null;
				error = "Usage: --fixture <id> --provenance <real|synthetic-verified> --inspection <json> --glb <file> --output <json>";
				return false;
			}
			values[args[index][2..]] = args[index + 1];
		}
		string[] required = ["fixture", "provenance", "inspection", "glb", "output"];
		if (required.Any(key => !values.TryGetValue(key, out string? value) || string.IsNullOrWhiteSpace(value)))
		{
			options = null;
			error = "All verifier options are required.";
			return false;
		}
		options = new(values["fixture"], values["provenance"], values["inspection"], values["glb"], values["output"]);
		error = null;
		return true;
	}
}
