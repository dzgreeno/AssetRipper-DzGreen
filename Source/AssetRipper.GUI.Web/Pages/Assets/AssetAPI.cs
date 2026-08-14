using AssetRipper.Assets;
using AssetRipper.Export.Configuration;
using AssetRipper.Export.Modules.Audio;
using AssetRipper.Export.Modules.Models;
using AssetRipper.Export.Modules.Textures;
using AssetRipper.Export.PrimaryContent;
using AssetRipper.Export.PrimaryContent.Models;
using AssetRipper.Export.UnityProjects;
using AssetRipper.Export.UnityProjects.Scripts;
using AssetRipper.Export.UnityProjects.Shaders;
using AssetRipper.GUI.Web.Documentation;
using AssetRipper.GUI.Web.Paths;
using AssetRipper.Import.AssetCreation;
using AssetRipper.Import.Logging;
using AssetRipper.Import.Structure.Assembly;
using AssetRipper.Import.Structure.Assembly.Managers;
using AssetRipper.IO.Files;
using AssetRipper.Processing.Textures;
using AssetRipper.SourceGenerated.Classes.ClassID_1;
using AssetRipper.SourceGenerated.Classes.ClassID_115;
using AssetRipper.SourceGenerated.Classes.ClassID_128;
using AssetRipper.SourceGenerated.Classes.ClassID_156;
using AssetRipper.SourceGenerated.Classes.ClassID_189;
using AssetRipper.SourceGenerated.Classes.ClassID_213;
using AssetRipper.SourceGenerated.Classes.ClassID_28;
using AssetRipper.SourceGenerated.Classes.ClassID_329;
using AssetRipper.SourceGenerated.Classes.ClassID_43;
using AssetRipper.SourceGenerated.Classes.ClassID_48;
using AssetRipper.SourceGenerated.Classes.ClassID_49;
using AssetRipper.SourceGenerated.Classes.ClassID_83;
using AssetRipper.SourceGenerated.Extensions;
using AssetRipper.Web.Extensions;
using AssetRipper.Yaml;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using SharpGLTF.Scenes;
using System.Globalization;
using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;

namespace AssetRipper.GUI.Web.Pages.Assets;

internal static class AssetAPI
{
	public static class Urls
	{
		public const string Base = "/Assets";
		public const string View = Base + "/View";
		public const string Image = Base + "/Image";
		public const string Audio = Base + "/Audio";
			public const string Model = Base + "/Model.glb";
			public const string CharacterModel = Base + "/Character.glb";
			public const string CharacterFbxExport = Base + "/Character.fbx";
			public const string CharacterExportFolder = Base + "/Character.ExportFolder";
			public const string WorkspaceRows = Base + "/WorkspaceRows";
			public const string Font = Base + "/Font";
		public const string Video = Base + "/Video";
		public const string Json = Base + "/Json";
		public const string Yaml = Base + "/Yaml";
		public const string Text = Base + "/Text";
		public const string Binary = Base + "/Binary";
	}
	private const string Extension = "Extension";
	private const string Path = "Path";

	#region View
	public static string GetViewUrl(AssetPath path) => $"{Urls.View}?{GetPathQuery(path)}";
	public static Task GetView(HttpContext context)
	{
		context.Response.DisableCaching();
		if (TryGetAssetFromQuery(context, out IUnityObjectBase? asset, out AssetPath path, out Task? failureTask))
		{
			return new ViewPage() { Asset = asset, Path = path }.WriteToResponse(context.Response);
		}
		else
		{
			return failureTask;
		}
	}
	#endregion

	#region Image
	public static string GetImageUrl(AssetPath path, string? extension = null)
	{
		return $"{Urls.Image}?{GetPathQuery(path)}{GetExtensionQuerySuffix(extension)}";
	}

	public static Task GetImageData(HttpContext context)
	{
		context.Response.DisableCaching();
		if (!TryGetAssetFromQuery(context, out IUnityObjectBase? asset, out Task? failureTask))
		{
			return failureTask;
		}

		if (TryGetImageExtensionFromQuery(context, out string? extension, out ImageExportFormat format))
		{
			DirectBitmap bitmap = GetImageBitmap(asset);
			if (bitmap.IsEmpty)
			{
				return context.Response.NotFound("Image data could not be decoded.");
			}

			MemoryStream stream = new();
			bitmap.Save(stream, format);
			return Results.Bytes(stream.ToArray(), $"image/{extension}").ExecuteAsync(context);
		}
		else
		{
			return Results.Bytes(GetRawImageData(asset), "application/octet-stream").ExecuteAsync(context);
		}
	}

	public static bool HasImageData(IUnityObjectBase asset) => asset switch
	{
		IImageTexture texture => texture.CheckAssetIntegrity(),
		SpriteInformationObject spriteInformationObject => spriteInformationObject.Texture.CheckAssetIntegrity(),
		ISprite sprite => SpriteConverter.Supported(sprite),
		ITerrainData terrainData => terrainData.Heightmap.Heights.Count > 0,
		_ => false,
	};

	private static DirectBitmap GetImageBitmap(IUnityObjectBase asset)
	{
		return asset switch
		{
			IImageTexture texture => TextureToBitmap(texture),
			SpriteInformationObject spriteInformationObject => TextureToBitmap(spriteInformationObject.Texture),
			ISprite sprite => SpriteToBitmap(sprite),
			ITerrainData terrainData => TerrainHeatmap.GetBitmap(terrainData),
			_ => DirectBitmap.Empty,
		};

		static DirectBitmap TextureToBitmap(IImageTexture texture)
		{
			return TextureConverter.TryConvertToBitmap(texture, out DirectBitmap bitmap) ? bitmap : DirectBitmap.Empty;
		}

		static DirectBitmap SpriteToBitmap(ISprite sprite)
		{
			return SpriteConverter.TryConvertToBitmap(sprite, out DirectBitmap bitmap) ? bitmap : DirectBitmap.Empty;
		}
	}

	private static byte[] GetRawImageData(IUnityObjectBase asset)
	{
		return asset switch
		{
			ITexture2D texture => texture.GetImageData(),
			SpriteInformationObject spriteInformationObject => spriteInformationObject.Texture.GetImageData(),
			ISprite sprite => sprite.TryGetTexture()?.GetImageData() ?? [],
			ITerrainData terrainData => MemoryMarshal.AsBytes(terrainData.Heightmap.Heights.GetSpan()).ToArray(),
			_ => [],
		};
	}

	private static bool TryGetImageExtensionFromQuery(HttpContext context, [NotNullWhen(true)] out string? extension, out ImageExportFormat format)
	{
		if (context.Request.Query.TryGetValue(Extension, out extension))
		{
			return ImageExportFormat.TryGetFromExtension(extension, out format);
		}
		else
		{
			format = default;
			return false;
		}
	}
	#endregion

	#region Audio
	public static string GetAudioUrl(AssetPath path, string? extension = null)
	{
		return $"{Urls.Audio}?{GetPathQuery(path)}{GetExtensionQuerySuffix(extension)}";
	}

	public static Task GetAudioData(HttpContext context)
	{
		context.Response.DisableCaching();
		if (!TryGetAssetFromQuery(context, out IUnityObjectBase? asset, out Task? failureTask))
		{
			return failureTask;
		}

		if (asset is not IAudioClip clip)
		{
			return context.Response.NotFound("Asset was not an audio clip.");
		}
		else if (AudioClipDecoder.TryDecode(clip, out byte[]? decodedAudioData, out string? extension, out _))
		{
			if (context.Request.Query.TryGetValue(Extension, out string? desiredExtension))
			{
				if (extension is "ogg" && desiredExtension is "wav")
				{
					byte[] wavData = AudioConverter.OggToWav(decodedAudioData);
					if (wavData.Length > 0)
					{
						decodedAudioData = wavData;
						extension = "wav";
					}
				}
			}

			return Results.Bytes(decodedAudioData, $"audio/{extension}").ExecuteAsync(context);
		}
		else
		{
			return context.Response.NotFound("Audio data could not be decoded.");
		}
	}

	public static bool HasAudioData(IUnityObjectBase asset)
	{
		return asset is IAudioClip;
	}
	#endregion

	#region Model
	public static string GetModelUrl(AssetPath path)
	{
		return $"{Urls.Model}?{GetPathQuery(path)}";
	}

	public static Task GetModelData(HttpContext context)
	{
		context.Response.DisableCaching();
		if (!TryGetAssetFromQuery(context, out IUnityObjectBase? asset, out Task? failureTask))
		{
			return failureTask;
		}

		if (asset is not IMesh mesh)
		{
			return context.Response.NotFound("Asset was not a mesh.");
		}
		else
		{
			MemoryStream stream = new();
			SceneBuilder sceneBuilder;
			try
			{
				sceneBuilder = GlbMeshBuilder.Build(mesh);
			}
			catch (Exception ex)
			{
				Logger.Error(ex);
				return context.Response.NotFound("Model data could not be decoded.");
			}

			if (GlbWriter.TryWrite(sceneBuilder, stream, out string? errorMessage))
			{
				return Results.Bytes(stream.ToArray(), "model/gltf-binary", "model.glb").ExecuteAsync(context);
			}
			else
			{
				Logger.Error(errorMessage);
				return context.Response.NotFound("Model data could not be decoded.");
			}
		}
	}

		public static bool HasModelData(IUnityObjectBase asset)
		{
			return asset is IMesh;
		}

		public static string GetCharacterModelUrl(AssetPath path)
		{
			return $"{Urls.CharacterModel}?{GetPathQuery(path)}";
		}

			public static string GetCharacterFbxExportUrl(AssetPath path)
			{
				return $"{Urls.CharacterFbxExport}?{GetPathQuery(path)}";
			}

			public static string GetCharacterExportFolderUrl(AssetPath path)
			{
				return $"{Urls.CharacterExportFolder}?{GetPathQuery(path)}";
			}

		public static Task GetCharacterModelData(HttpContext context)
		{
			context.Response.DisableCaching();
			if (!TryGetAssetFromQuery(context, out IUnityObjectBase? asset, out Task? failureTask))
			{
				return failureTask;
			}
			if (asset is not IGameObject root)
			{
				return context.Response.NotFound("Character preview requires a GameObject root.");
			}

			MemoryStream stream = new();
			try
			{
				SceneBuilder sceneBuilder = GlbLevelBuilder.Build(root.FetchHierarchy().OfType<IUnityObjectBase>(), false, GameFileLoader.GameBundle.FetchAssets());
				if (GlbWriter.TryWrite(sceneBuilder, stream, out string? errorMessage))
				{
					return Results.Bytes(stream.ToArray(), "model/gltf-binary", "character.glb").ExecuteAsync(context);
				}
				Logger.Error(errorMessage);
				return context.Response.NotFound("Character model could not be decoded.");
			}
			catch (Exception ex)
			{
				Logger.Error(ex);
				return context.Response.NotFound("Character model could not be decoded.");
			}
		}

		public static Task ExportCharacterFbx(HttpContext context)
		{
			context.Response.DisableCaching();
			if (!TryGetAssetFromQuery(context, out IUnityObjectBase? asset, out Task? failureTask))
			{
				return failureTask;
			}
			if (asset is not IGameObject gameObject)
			{
				return Results.BadRequest("Character FBX export requires a GameObject root.").ExecuteAsync(context);
			}

			IGameObject root = gameObject.GetRoot();
			try
			{
				string directory = System.IO.Path.Combine(GameFileLoader.Settings.ExportRootPath, "AssetWorkspace");
				Directory.CreateDirectory(directory);
				string safeName = CreateSafeFileName($"{root.GetBestName()}_{root.PathID}", $"character_{root.PathID}");
					string characterDirectory = GetCharacterExportDirectory(root);
				if (Directory.Exists(characterDirectory))
				{
					Directory.Delete(characterDirectory, recursive: true);
				}
					Directory.CreateDirectory(characterDirectory);
					string glbOutputPath = System.IO.Path.Combine(characterDirectory, safeName + ".glb");
					if (!TryWriteCharacterGlb(root, glbOutputPath, out string? glbErrorMessage))
					{
						Logger.Error(LogCategory.Export, $"Workspace GLB export failed for '{root.GetBestName()}': {glbErrorMessage}");
						return Results.InternalServerError("Blender-ready GLB export failed. See the local status log for details.").ExecuteAsync(context);
					}

					string outputPath = System.IO.Path.Combine(characterDirectory, safeName + ".fbx");
					bool binaryFbxSuccess = FbxBinaryConverter.TryConvertGlbToBinaryFbx(glbOutputPath, outputPath, out string? binaryFbxErrorMessage);
					if (!binaryFbxSuccess)
					{
						Logger.Warning(LogCategory.Export, $"Workspace binary FBX conversion was unavailable for '{root.GetBestName()}': {binaryFbxErrorMessage}");
					}

					string legacyAsciiOutputPath = System.IO.Path.Combine(characterDirectory, safeName + "_legacy_ascii.fbx");
					FbxAsciiExporter exporter = new() { IncludeAnimations = true };
					bool asciiFbxSuccess = exporter.Export(exporter.GetCharacterAssets(root, GameFileLoader.GameBundle.FetchAssets()), legacyAsciiOutputPath, LocalFileSystem.Instance);
					if (!asciiFbxSuccess || !File.Exists(legacyAsciiOutputPath))
					{
						Logger.Warning(LogCategory.Export, $"Workspace legacy ASCII FBX export failed for '{root.GetBestName()}'.");
					}

					string manifestPath = System.IO.Path.Combine(characterDirectory, "README-Blender.txt");
					File.WriteAllText(manifestPath, CreateCharacterExportReadme(safeName, binaryFbxSuccess, binaryFbxErrorMessage, asciiFbxSuccess), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

					string archivePath = System.IO.Path.Combine(directory, safeName + "_blender_bundle.zip");
					if (File.Exists(archivePath))
					{
						File.Delete(archivePath);
					}
					ZipFile.CreateFromDirectory(characterDirectory, archivePath, CompressionLevel.Fastest, includeBaseDirectory: true);
					string primaryOutputPath = binaryFbxSuccess ? outputPath : glbOutputPath;
					context.Response.Headers.Append("X-AssetRipper-Export-Path", primaryOutputPath);
					context.Response.Headers.Append("X-AssetRipper-Export-Format", binaryFbxSuccess ? "binary-fbx+glb" : "glb+legacy-ascii-fbx");
					Logger.Info(LogCategory.Export, $"Workspace Blender export completed: {primaryOutputPath}; download bundle: {archivePath}");
					return Results.File(archivePath, "application/zip", System.IO.Path.GetFileName(archivePath)).ExecuteAsync(context);
			}
			catch (Exception ex)
			{
				Logger.Error(ex);
				return Results.InternalServerError($"FBX export failed: {ex.Message}").ExecuteAsync(context);
				}
			}

			public static Task OpenCharacterExportFolder(HttpContext context)
			{
				context.Response.DisableCaching();
				if (!TryGetAssetFromQuery(context, out IUnityObjectBase? asset, out Task? failureTask))
				{
					return failureTask;
				}
				if (asset is not IGameObject gameObject)
				{
					return Results.BadRequest("Character export folder requires a GameObject root.").ExecuteAsync(context);
				}

				try
				{
					string characterDirectory = GetCharacterExportDirectory(gameObject.GetRoot());
					Directory.CreateDirectory(characterDirectory);
					Process.Start(new ProcessStartInfo
					{
						FileName = characterDirectory,
						UseShellExecute = true,
					});
					Logger.Info(LogCategory.Export, $"Opened Workspace export folder: {characterDirectory}");
					return Results.Ok().ExecuteAsync(context);
				}
				catch (Exception ex)
				{
					Logger.Error(ex);
					return Results.InternalServerError($"Could not open the Workspace export folder: {ex.Message}").ExecuteAsync(context);
				}
			}

			private static bool TryWriteCharacterGlb(IGameObject root, string outputPath, [NotNullWhen(false)] out string? errorMessage)
			{
				try
				{
					SceneBuilder sceneBuilder = GlbLevelBuilder.Build(root.FetchHierarchy().OfType<IUnityObjectBase>(), false, GameFileLoader.GameBundle.FetchAssets());
					using FileStream stream = File.Create(outputPath);
					return GlbWriter.TryWrite(sceneBuilder, stream, out errorMessage);
				}
				catch (Exception ex)
				{
					errorMessage = ex.Message;
					return false;
				}
			}

			private static string GetCharacterExportDirectory(IGameObject root)
			{
				string workspaceDirectory = System.IO.Path.Combine(GameFileLoader.Settings.ExportRootPath, "AssetWorkspace");
				string safeName = CreateSafeFileName($"{root.GetBestName()}_{root.PathID}", $"character_{root.PathID}");
				return System.IO.Path.Combine(workspaceDirectory, safeName);
			}

			private static string CreateCharacterExportReadme(string safeName, bool hasBinaryFbx, string? binaryFbxErrorMessage, bool hasLegacyAsciiFbx)
			{
				StringBuilder builder = new();
				builder.AppendLine("AssetRipper DzGreen character export");
				builder.AppendLine();
				builder.AppendLine($"{safeName}.glb");
				builder.AppendLine("  Blender-ready glTF binary. Use this file in Blender for the most direct import of meshes, skinning, embedded textures, and animation clips.");
				builder.AppendLine();
				if (hasBinaryFbx)
				{
					builder.AppendLine($"{safeName}.fbx");
					builder.AppendLine("  Binary FBX created from the verified GLB scene. Blender and standard FBX tools can import this file.");
					builder.AppendLine();
				}
				else
				{
					builder.AppendLine("Binary FBX was not created on this platform.");
					builder.AppendLine($"  Reason: {binaryFbxErrorMessage ?? "The converter was unavailable."}");
					builder.AppendLine("  Import the included GLB instead.");
					builder.AppendLine();
				}

				if (hasLegacyAsciiFbx)
				{
					builder.AppendLine($"{safeName}_legacy_ascii.fbx");
					builder.AppendLine("  Legacy ASCII FBX with texture sidecars. Blender does not support ASCII FBX; use this only with tools that require it.");
				}
				return builder.ToString();
			}

			private static string CreateSafeFileName(string value, string fallback)
		{
			string candidate = string.IsNullOrWhiteSpace(value) ? fallback : value;
			foreach (char invalid in System.IO.Path.GetInvalidFileNameChars())
			{
				candidate = candidate.Replace(invalid, '_');
			}
			return string.IsNullOrWhiteSpace(candidate) ? fallback : candidate;
		}
		#endregion

	#region Font
	public static string GetFontUrl(AssetPath path)
	{
		return $"{Urls.Font}?{GetPathQuery(path)}";
	}

	public static Task GetFontData(HttpContext context)
	{
		//Only accept Path in the query.
		context.Response.DisableCaching();
		if (!TryGetAssetFromQuery(context, out IUnityObjectBase? asset, out Task? failureTask))
		{
			return failureTask;
		}

		if (asset is not IFont font)
		{
			return context.Response.NotFound("Asset was not a font.");
		}
		else if (TryGetFontData(font, out byte[] data, out string? extension, out string? mimeType))
		{
			return Results.Bytes(data, mimeType, $"{font.GetBestName()}.{extension}").ExecuteAsync(context);
		}
		else
		{
			return context.Response.NotFound("Font data could not be decoded.");
		}
	}

	public static bool HasFontData(IUnityObjectBase asset)
	{
		return asset is IFont font && TryGetFontData(font, out _, out _, out _);
	}

	private static bool TryGetFontData(IFont font, out byte[] data, [NotNullWhen(true)] out string? extension, [NotNullWhen(true)] out string? mimeType)
	{
		data = font.FontData;

		if (data is { Length: >= 4 })
		{
			(extension, mimeType) = (data[0], data[1], data[2], data[3]) switch
			{
				(0x4F, 0x54, 0x54, 0x4F) => ("otf", "font/otf"),
				(0x00, 0x01, 0x00, 0x00) => ("ttf", "font/ttf"),
				(0x74, 0x74, 0x63, 0x66) => ("ttc", "font/collection"),
				_ => ("dat", "application/octet-stream"),
			};

			return true;
		}
		else
		{
			extension = null;
			mimeType = null;

			return false;
		}
	}
	#endregion

	#region Video
	public static string GetVideoUrl(AssetPath path)
	{
		return $"{Urls.Video}?{GetPathQuery(path)}";
	}

	public static Task GetVideoData(HttpContext context)
	{
		//Only accept Path in the query.
		context.Response.DisableCaching();
		if (!TryGetAssetFromQuery(context, out IUnityObjectBase? asset, out Task? failureTask))
		{
			return failureTask;
		}

		if (asset is not IVideoClip videoClip)
		{
			return context.Response.NotFound("Asset was not a video clip.");
		}
		else if (videoClip.TryGetExtensionFromPath(out string? extension) && videoClip.TryGetContent(out byte[]? content))
		{
			return Results.Bytes(content, $"video/{extension}", $"{videoClip.GetBestName()}.{extension}").ExecuteAsync(context);
		}
		else
		{
			return context.Response.NotFound("Video data could not be decoded.");
		}
	}

	public static bool HasVideoData(IUnityObjectBase asset)
	{
		return asset is IVideoClip clip && clip.CheckIntegrity();
	}
	#endregion

	#region Json
	public static string GetJsonUrl(AssetPath path)
	{
		return $"{Urls.Json}?{GetPathQuery(path)}";
	}
	public static Task GetJson(HttpContext context)
	{
		context.Response.DisableCaching();
		if (!TryGetAssetFromQuery(context, out IUnityObjectBase? asset, out Task? failureTask))
		{
			return failureTask;
		}

		try
		{
			StringWriter stringWriter = new(CultureInfo.InvariantCulture) { NewLine = "\n" };
			asset.WalkStandard(new DefaultJsonWalker(stringWriter));
			string text = stringWriter.ToString();
			return Results.Text(text, "application/json").ExecuteAsync(context);
		}
		catch (Exception ex)
		{
			return Results.Text(ex.ToString()).ExecuteAsync(context);
		}
	}
	#endregion

	#region Yaml
	public static string GetYamlUrl(AssetPath path)
	{
		return $"{Urls.Yaml}?{GetPathQuery(path)}";
	}
	public static Task GetYaml(HttpContext context)
	{
		context.Response.DisableCaching();
		if (!TryGetAssetFromQuery(context, out IUnityObjectBase? asset, out Task? failureTask))
		{
			return failureTask;
		}

		try
		{
			string text;
			using (StringWriter stringWriter = new(CultureInfo.InvariantCulture) { NewLine = "\n" })
			{
				YamlWriter writer = new();
				writer.WriteHead(stringWriter);
				YamlDocument document = new YamlWalker().ExportYamlDocument(asset, ExportIdHandler.GetMainExportID(asset));
				writer.WriteDocument(document);
				writer.WriteTail(stringWriter);
				text = stringWriter.ToString();
			}
			return Results.Text(text, "application/yaml").ExecuteAsync(context);
		}
		catch (Exception ex)
		{
			return Results.Text(ex.ToString()).ExecuteAsync(context);
		}
	}
	#endregion

	#region Text
	public static string GetTextUrl(AssetPath path)
	{
		return $"{Urls.Text}?{GetPathQuery(path)}";
	}

	public static Task GetText(HttpContext context)
	{
		//Only accept Path in the query. It sensibly determines the file extension.
		context.Response.DisableCaching();
		if (!TryGetAssetFromQuery(context, out IUnityObjectBase? asset, out Task? failureTask))
		{
			return failureTask;
		}

		return Results.Text(TryGetText(asset), "text/plain").ExecuteAsync(context);
	}

	public static bool HasText(IUnityObjectBase asset)
	{
		return asset is IShader or IMonoScript or ITextAsset { Script_C49.IsEmpty: false };
	}

	public static string TryGetText(IUnityObjectBase asset)
	{
		return asset switch
		{
			IShader shader => shader.Has_Script() && !shader.Script.IsEmpty ? shader.Script : DumpShaderDataAsText(shader),
			IMonoScript monoScript => DecompileMonoScript(monoScript),
			ITextAsset textAsset => textAsset.Script_C49,
			_ => "",
		};
	}

	public static string GetTextFileName(IUnityObjectBase asset)
	{
		return asset switch
		{
			IShader => $"{asset.GetBestName()}.shader",
			IMonoScript monoScript => $"{monoScript.ClassName_R}.cs",
			ITextAsset textAsset => $"{asset.GetBestName()}.{GetTextAssetExtension(textAsset)}",
			_ => $"{asset.GetBestName()}.txt",
		};

		static string GetTextAssetExtension(ITextAsset textAsset)
		{
			return textAsset.GetBestExtension() ?? "txt";
		}
	}

	private static string DumpShaderDataAsText(IShader shader)
	{
		InvariantStringWriter writer = new();
		ShaderExportHandler.ExportShader(shader, writer);
		return writer.ToString();
	}

	private static string DecompileMonoScript(IMonoScript monoScript)
	{
		IAssemblyManager assemblyManager = GameFileLoader.AssemblyManager;
		if (!monoScript.IsScriptPresents(assemblyManager))
		{
			return EmptyScript.GetContent(monoScript);
		}
		else
		{
			try
			{
				_ = monoScript.GetTypeDefinition(assemblyManager);
				return EmptyScript.GetContent(monoScript); // Todo: replace with ILSpy
			}
			catch (Exception ex)
			{
				return $"{Localization.AnErrorOccuredDuringDecompilation}\n\n{ex}";
			}
		}
	}
	#endregion

	#region Binary Data
	public static string GetBinaryUrl(AssetPath path)
	{
		return $"{Urls.Binary}?{GetPathQuery(path)}";
	}

	public static Task GetBinaryData(HttpContext context)
	{
		//Only for RawDataObject. This should not call any of the IUnityAssetBase Write methods.
		context.Response.DisableCaching();
		if (!TryGetAssetFromQuery(context, out IUnityObjectBase? asset, out Task? failureTask))
		{
			return failureTask;
		}

		byte[] data = (asset as RawDataObject)?.RawData ?? [];
		return Results.Bytes(data, "application/octet-stream").ExecuteAsync(context);
	}

	public static bool HasBinaryData(IUnityObjectBase asset)
	{
		return asset is RawDataObject { RawData.Length: > 0 };
	}
	#endregion

	private static string GetPathQuery(AssetPath path) => $"{Path}={path.ToJson().ToUrl()}";

	private static string? GetExtensionQuerySuffix(string? extension) => string.IsNullOrEmpty(extension) ? null : $"&{Extension}={extension}";

	private static bool TryGetAssetFromQuery(HttpContext context, [NotNullWhen(true)] out IUnityObjectBase? asset, [NotNullWhen(false)] out Task? failureTask)
	{
		return TryGetAssetFromQuery(context, out asset, out _, out failureTask);
	}

	private static bool TryGetAssetFromQuery(HttpContext context, [NotNullWhen(true)] out IUnityObjectBase? asset, out AssetPath path, [NotNullWhen(false)] out Task? failureTask)
	{
		if (!context.Request.Query.TryGetValue(Path, out string? json) || string.IsNullOrEmpty(json))
		{
			asset = null;
			path = default;
			failureTask = context.Response.NotFound("The path must be included in the request.");
			return false;
		}

		try
		{
			path = AssetPath.FromJson(json);
		}
		catch (Exception ex)
		{
			asset = null;
			path = default;
			failureTask = context.Response.NotFound(ex.ToString());
			return false;
		}

		if (!GameFileLoader.IsLoaded)
		{
			asset = null;
			failureTask = context.Response.NotFound("No files loaded.");
			return false;
		}
		else if (!GameFileLoader.GameBundle.TryGetAsset(path, out asset))
		{
			failureTask = context.Response.NotFound($"Asset could not be resolved: {path}");
			return false;
		}
		else
		{
			failureTask = null;
			return true;
		}
	}

	public static RouteHandlerBuilder WithAssetPathParameter(this RouteHandlerBuilder builder)
	{
		return builder.WithQueryStringParameter(Path, "Path to the asset", true);
	}

	public static RouteHandlerBuilder WithImageExtensionParameter(this RouteHandlerBuilder builder)
	{
		return builder.WithQueryStringParameter(Extension, "Extension for decoding the image.", true);
	}

	private sealed class InvariantStringWriter : StringWriter
	{
		public override IFormatProvider FormatProvider => CultureInfo.InvariantCulture;
	}
}
