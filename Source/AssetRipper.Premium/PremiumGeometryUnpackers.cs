using System.Numerics;

namespace AssetRipper.Premium;

/// <summary>
/// Deterministic numerical decoders used by Premium diagnostics to validate geometry data that
/// has already been made available by the normal Unity importer. These methods do not read files,
/// infer undocumented layouts, or manufacture geometry.
/// </summary>
public static class PremiumGeometryUnpackers
{
	private const float Snorm10Max = 511.0f;
	private const float SmallestThreeScale = 0.7071067811865475244f;

	/// <summary>
	/// Converts the IEEE 754 binary16 bit pattern to binary32 using integer operations.
	/// </summary>
	public static float HalfToSingle(ushort value)
	{
		uint sign = (uint)(value & 0x8000) << 16;
		uint exponent = (uint)(value >> 10) & 0x1F;
		uint mantissa = (uint)value & 0x03FF;

		uint bits;
		if (exponent == 0)
		{
			if (mantissa == 0)
			{
				bits = sign;
			}
			else
			{
				// Normalize the binary16 subnormal before translating its exponent to binary32.
				int adjustedExponent = -14;
				while ((mantissa & 0x0400) == 0)
				{
					mantissa <<= 1;
					adjustedExponent--;
				}
				mantissa &= 0x03FF;
				bits = sign | (uint)(adjustedExponent + 127) << 23 | mantissa << 13;
			}
		}
		else if (exponent == 0x1F)
		{
			// Preserve the payload for NaN and the exact infinity encoding otherwise.
			bits = sign | 0x7F80_0000 | mantissa << 13;
		}
		else
		{
			bits = sign | (exponent + 112) << 23 | mantissa << 13;
		}

		return BitConverter.Int32BitsToSingle(unchecked((int)bits));
	}

	/// <summary>
	/// Decodes three signed-normalized 10-bit channels stored in the low 30 bits of a uint.
	/// </summary>
	public static Vector3 UnpackSnorm101010(uint packed)
	{
		return new Vector3(
			DecodeSnorm10(packed),
			DecodeSnorm10(packed >> 10),
			DecodeSnorm10(packed >> 20));
	}

	/// <summary>
	/// Decodes a canonical smallest-three unit quaternion. Bits 30-31 select the omitted component;
	/// the remaining three signed-normalized values occupy bits 0-29.
	/// </summary>
	public static Quaternion UnpackSmallestThreeQuaternion(uint packed)
	{
		int omittedComponent = (int)(packed >> 30);
		float a = DecodeSnorm10(packed) * SmallestThreeScale;
		float b = DecodeSnorm10(packed >> 10) * SmallestThreeScale;
		float c = DecodeSnorm10(packed >> 20) * SmallestThreeScale;
		float omitted = MathF.Sqrt(MathF.Max(0.0f, 1.0f - a * a - b * b - c * c));

		Span<float> components = stackalloc float[4];
		int cursor = 0;
		for (int component = 0; component < components.Length; component++)
		{
			if (component == omittedComponent)
			{
				components[component] = omitted;
			}
			else
			{
				components[component] = cursor++ switch
				{
					0 => a,
					1 => b,
					_ => c,
				};
			}
		}

		Quaternion result = new(components[0], components[1], components[2], components[3]);
		return Quaternion.Normalize(result);
	}

	private static float DecodeSnorm10(uint packed)
	{
		int value = (int)(packed & 0x03FF);
		if ((value & 0x0200) != 0)
		{
			value |= unchecked((int)0xFFFF_FC00);
		}
		return Math.Clamp(value / Snorm10Max, -1.0f, 1.0f);
	}
}
