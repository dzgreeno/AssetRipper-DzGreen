using AssetRipper.Assets;
using AssetRipper.SourceGenerated.Classes.ClassID_48;
using AssetRipper.SourceGenerated.Extensions;
using AssetRipper.SourceGenerated.Extensions.Enums.Shader.SerializedShader;
using AssetRipper.SourceGenerated.Subclasses.SerializedProperties;
using AssetRipper.SourceGenerated.Subclasses.SerializedProperty;

namespace AssetRipper.Export.UnityProjects.Shaders;

public sealed class DummyShaderTextExporter : ShaderExporterBase
{
	// Use a portable vertex-fragment program instead of serialized platform-specific
	// GPU disassembly. CGPROGRAM is accepted by the supported legacy Unity versions.
	private static string FallbackDummyShader { get; } = """

			SubShader
			{
				Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
				LOD 200
				Pass
				{
					CGPROGRAM
	#pragma vertex vert
	#pragma fragment frag
	#pragma target 3.0
	#include "UnityCG.cginc"

					struct appdata
					{
						float4 vertex : POSITION;
						float3 normal : NORMAL;
						float2 uv : TEXCOORD0;
					};

					struct v2f
					{
						float4 vertex : SV_POSITION;
						float2 uv : TEXCOORD0;
					};

					sampler2D _MainTex;
					float4 _MainTex_ST;
					fixed4 _Color;
					sampler2D _BumpMap;
					sampler2D _SpecGlossMap;
					sampler2D _OcclusionMap;

					v2f vert(appdata v)
					{
						v2f o;
						o.vertex = UnityObjectToClipPos(v.vertex);
						o.uv = TRANSFORM_TEX(v.uv, _MainTex);
						return o;
					}

					fixed4 frag(v2f i) : SV_Target
					{
						fixed4 albedo = tex2D(_MainTex, i.uv) * _Color;
						fixed occlusion = tex2D(_OcclusionMap, i.uv).r;
						return fixed4(albedo.rgb * max(occlusion, 0.25), albedo.a);
					}
					ENDCG
				}
			}

""".Replace("\r", "");

	public override bool Export(IExportContainer container, IUnityObjectBase asset, string path, FileSystem fileSystem)
	{
		return ExportShader((IShader)asset, path, fileSystem);
	}

	public static bool ExportShader(IShader shader, string path, FileSystem fileSystem)
	{
		using Stream fileStream = fileSystem.File.Create(path);
		using InvariantStreamWriter writer = new(fileStream);
		return ExportShader(shader, writer);
	}

	public static bool ExportShader(IShader shader, TextWriter writer)
	{
		// Technically, this outputs invalid shader code for Unity 5.5 because HLSLPROGRAM was not introduced until Unity 5.6.
		if (shader.Has_ParsedForm())
		{
			writer.Write($"Shader \"{shader.ParsedForm.Name}\" {{\n");
			Export(shader.ParsedForm.PropInfo, writer);

			writer.Write("\t//DummyShaderTextExporter\n");
			writer.WriteIndent(1);
			writer.Write(FallbackDummyShader);
			writer.Write('\n');

			if (shader.ParsedForm.FallbackName != string.Empty)
			{
				writer.WriteIndent(1);
				writer.Write($"Fallback \"{shader.ParsedForm.FallbackName}\"\n");
			}
			if (shader.ParsedForm.CustomEditorName != string.Empty)
			{
				writer.WriteIndent(1);
				writer.Write($"//CustomEditor \"{shader.ParsedForm.CustomEditorName}\"\n");
			}
			writer.Write('}');
		}
		else
		{
			string header = shader.Script.String;
			int subshaderIndex = header.IndexOf("SubShader");
			if (subshaderIndex < 0)
			{
				return false;
			}
			writer.WriteString(header, 0, subshaderIndex);

			writer.Write("\t//DummyShaderTextExporter\n");
			writer.WriteIndent(1);
			writer.Write(FallbackDummyShader);

			writer.Write('}');
		}
		return true;
	}

	private static void Export(ISerializedProperties _this, TextWriter writer)
	{
		writer.WriteIndent(1);
		writer.Write("Properties {\n");
			HashSet<string> propertyNames = new(StringComparer.Ordinal);
			foreach (ISerializedProperty prop in _this.Props)
			{
				propertyNames.Add(prop.Name.ToString());
				Export(prop, writer);
			}
			WriteFallbackProperty(writer, propertyNames, "_MainTex", "Albedo (RGB) and Alpha", "2D", "\"white\" {}");
			WriteFallbackProperty(writer, propertyNames, "_Color", "Tint", "Color", "(1,1,1,1)");
			WriteFallbackProperty(writer, propertyNames, "_BumpMap", "Normal Map", "2D", "\"bump\" {}");
			WriteFallbackProperty(writer, propertyNames, "_SpecGlossMap", "Specular / Smoothness", "2D", "\"white\" {}");
			WriteFallbackProperty(writer, propertyNames, "_OcclusionMap", "Occlusion", "2D", "\"white\" {}");
			writer.WriteIndent(1);
		writer.Write("}\n");
	}

	private static void WriteFallbackProperty(TextWriter writer, HashSet<string> existingNames, string name, string description, string type, string defaultValue)
	{
		if (existingNames.Contains(name))
		{
			return;
		}
		writer.WriteIndent(2);
		writer.Write($"{name} (\"{description}\", {type}) = {defaultValue}\\n");
	}

	private static void Export(ISerializedProperty _this, TextWriter writer)
	{
		writer.WriteIndent(2);
		foreach (Utf8String attribute in _this.Attributes)
		{
			writer.Write($"[{attribute}] ");
		}
		SerializedPropertyFlag flags = (SerializedPropertyFlag)_this.Flags;
		if (flags.IsHideInInspector())
		{
			writer.Write("[HideInInspector] ");
		}
		if (flags.IsPerRendererData())
		{
			writer.Write("[PerRendererData] ");
		}
		if (flags.IsNoScaleOffset())
		{
			writer.Write("[NoScaleOffset] ");
		}
		if (flags.IsNormal())
		{
			writer.Write("[Normal] ");
		}
		if (flags.IsHDR())
		{
			writer.Write("[HDR] ");
		}
		if (flags.IsGamma())
		{
			writer.Write("[Gamma] ");
		}

		writer.Write($"{_this.Name} (\"{_this.Description}\", ");

		switch (_this.GetType_())
		{
			case SerializedPropertyType.Color:
			case SerializedPropertyType.Vector:
				writer.Write("Vector");
				break;

			case SerializedPropertyType.Float:
				writer.Write("Float");
				break;

			case SerializedPropertyType.Range:
				writer.Write($"Range({_this.DefValue_1_.ToStringInvariant()}, {_this.DefValue_2_.ToStringInvariant()})");
				break;

			case SerializedPropertyType.Texture:
				switch (_this.DefTexture.TexDim)
				{
					case 1:
						writer.Write("any");
						break;
					case 2:
						writer.Write("2D");
						break;
					case 3:
						writer.Write("3D");
						break;
					case 4:
						writer.Write("Cube");
						break;
					case 5:
						writer.Write("2DArray");
						break;
					case 6:
						writer.Write("CubeArray");
						break;
					default:
						throw new NotSupportedException("Texture dimension isn't supported");

				}
				break;

			case SerializedPropertyType.Int:
				writer.Write("Int");
				break;

			default:
				throw new NotSupportedException($"Serialized property type {_this.Type} isn't supported");
		}
		writer.Write(") = ");

		switch (_this.GetType_())
		{
			case SerializedPropertyType.Color:
			case SerializedPropertyType.Vector:
				writer.Write($"({_this.DefValue_0_.ToStringInvariant()},{_this.DefValue_1_.ToStringInvariant()},{_this.DefValue_2_.ToStringInvariant()},{_this.DefValue_3_.ToStringInvariant()})");
				break;

			case SerializedPropertyType.Float:
			case SerializedPropertyType.Range:
			case SerializedPropertyType.Int:
				writer.Write(_this.DefValue_0_.ToStringInvariant());
				break;

			case SerializedPropertyType.Texture:
				writer.Write($"\"{_this.DefTexture.DefaultName}\" {{}}");
				break;

			default:
				throw new NotSupportedException($"Serialized property type {_this.Type} isn't supported");
		}
		writer.Write('\n');
	}
}
