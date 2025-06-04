// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Nerdbank.MessagePack;

/// <summary>
/// Represents the bits of a <see cref="Guid"/> in a way that allows for efficient access to its components.
/// Always encoded using the system's endianness.
/// </summary>
[StructLayout(LayoutKind.Explicit)]
internal struct GuidBits
{
#pragma warning disable SA1307, SA1600
	[FieldOffset(0)]
	internal Guid guid;

	[FieldOffset(0)]
	internal int a;
	[FieldOffset(4)]
	internal short b;
	[FieldOffset(6)]
	internal short c;
	[FieldOffset(8)]
	internal byte d;
	[FieldOffset(9)]
	internal byte e;
	[FieldOffset(10)]
	internal byte f;
	[FieldOffset(11)]
	internal byte g;
	[FieldOffset(12)]
	internal byte h;
	[FieldOffset(13)]
	internal byte i;
	[FieldOffset(14)]
	internal byte j;
	[FieldOffset(15)]
	internal byte k;
#pragma warning restore SA1307, SA1600

	/// <summary>
	/// Initializes a new instance of the <see cref="GuidBits"/> struct.
	/// </summary>
	/// <param name="value">The value to copy.</param>
	internal GuidBits(in Guid value)
	{
		this.guid = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="GuidBits"/> struct from a compact 16-byte binary representation.
	/// </summary>
	/// <param name="b">The binary representation.</param>
	/// <param name="bigEndian">A value indicating whether the binary was encoded with big endian (as opposed to little endian).</param>
	/// <exception cref="ArgumentException">Thrown if <paramref name="b"/> has a length other than 16.</exception>
	internal GuidBits(ReadOnlySpan<byte> b, bool bigEndian = false)
	{
		if (b.Length != 16)
		{
			throw new ArgumentException();
		}

		this = MemoryMarshal.Read<GuidBits>(b);

		if (bigEndian == BitConverter.IsLittleEndian)
		{
			this.a = BinaryPrimitives.ReverseEndianness(this.a);
			this.b = BinaryPrimitives.ReverseEndianness(this.b);
			this.c = BinaryPrimitives.ReverseEndianness(this.c);
		}
	}

	public static implicit operator Guid(GuidBits value) => Unsafe.As<GuidBits, Guid>(ref value);

	public static implicit operator GuidBits(Guid value) => Unsafe.As<Guid, GuidBits>(ref value);

	/// <summary>
	/// Attempt to parse a UTF-8 encoded string into a <see cref="Guid"/>.
	/// </summary>
	/// <param name="utf8string">The UTF-8 string.</param>
	/// <param name="guid">Receives the parsed <see cref="Guid"/> if successful.</param>
	/// <returns>A value indicating whether parsing was successful.</returns>
	internal static bool TryParseUtf8(ReadOnlySpan<byte> utf8string, out Guid guid)
	{
		guid = default;
		if (utf8string.Length < 32)
		{
			// The shortest encoding is 32 characters (N format), so if we have fewer than that, we cannot parse it.
			return false;
		}

		/*
		 * N: 32 digits, no hyphens. e.g. 69b942342c9e468b9bae77df7a288e45
		 * D: 8-4-4-4-12 digits, with hyphens. e.g. 69b94234-2c9e-468b-9bae-77df7a288e45
		 * B: 8-4-4-4-12 digits, with hyphens, enclosed in braces. e.g. {69b94234-2c9e-468b-9bae-77df7a288e45}
		 * P: 8-4-4-4-12 digits, with hyphens, enclosed in parentheses. e.g. (69b94234-2c9e-468b-9bae-77df7a288e45)
		 * X: 8-4-4-4-12 digits, with hyphens, enclosed in braces, with each group of digits in hexadecimal format. {0x69b94234,0x2c9e,0x468b,{0x9b,0xae,0x77,0xdf,0x7a,0x28,0x8e,0x45}}
		 */

		// Look for an opening parenthesis or brace.
		int bytesRead = 0;
		bool usesCurlyBraces = false, usesParentheses = false;
		switch (utf8string[0])
		{
			case (byte)'{':
				usesCurlyBraces = true;
				bytesRead++;
				break;
			case (byte)'(':
				usesParentheses = true;
				bytesRead++;
				break;
		}

		bool isXformat = usesCurlyBraces && utf8string[1..3] is [(byte)'0', (byte)'x'] or [(byte)'0', (byte)'X'];
		GuidBits bits = default;
		if (isXformat)
		{
			if (utf8string.Length < 68)
			{
				return false;
			}

			if (!TryRead0x(utf8string, ref bytesRead))
			{
				return false; // Expected "0x" after opening brace.
			}

			if (!TryParseUtf8Int(utf8string[bytesRead..], out bits.a))
			{
				return false;
			}

			bytesRead += 8; // 4 bytes read, each 2 bytes in length.

			static bool TryReadComma(ReadOnlySpan<byte> utf8Bytes, ref int bytesRead)
			{
				if (bytesRead < utf8Bytes.Length && utf8Bytes[bytesRead] == (byte)',' && bytesRead + 1 < utf8Bytes.Length)
				{
					bytesRead++;
					return true;
				}
				else
				{
					return false;
				}
			}

			if (!TryReadComma(utf8string, ref bytesRead) ||
				!TryRead0x(utf8string, ref bytesRead) ||
				!TryParseUtf8Short(utf8string[bytesRead..], out bits.b))
			{
				return false;
			}

			bytesRead += 4; // 2 bytes read, each 2 bytes in length.

			if (!TryReadComma(utf8string, ref bytesRead) ||
				!TryRead0x(utf8string, ref bytesRead) ||
				!TryParseUtf8Short(utf8string[bytesRead..], out bits.c))
			{
				return false;
			}

			bytesRead += 4; // 2 bytes read, each 2 bytes in length.

			if (!TryReadComma(utf8string, ref bytesRead))
			{
				return false;
			}

			if (utf8string[bytesRead++] != (byte)'{')
			{
				return false; // Expected an opening brace after the third short.
			}

			// Read the rest of the bytes as individually encoded hex bytes.
			for (int i = 0; i < 8; i++)
			{
				ref byte b = ref Unsafe.Add(ref bits.d, i);
				if (i > 0 && !TryReadComma(utf8string, ref bytesRead))
				{
					return false; // Expected a comma before the next byte.
				}

				if (!TryRead0x(utf8string, ref bytesRead) ||
					!TryParseUtf8Byte(utf8string[bytesRead..], out b))
				{
					return false;
				}

				bytesRead += 2; // 1 byte read, each 2 bytes in length.
			}

			// Read the closing brace after the last individual byte.
			if (utf8string[bytesRead++] != (byte)'}')
			{
				return false; // Expected an opening brace after the third short.
			}

			static bool TryRead0x(ReadOnlySpan<byte> utf8Bytes, ref int bytesRead)
			{
				if (bytesRead + 2 <= utf8Bytes.Length &&
					utf8Bytes[bytesRead] == (byte)'0' &&
					(utf8Bytes[bytesRead + 1] == (byte)'x' || utf8Bytes[bytesRead + 1] == (byte)'X'))
				{
					bytesRead += 2;
					return true;
				}
				else
				{
					return false;
				}
			}
		}
		else
		{
			if (!TryParseUtf8Int(utf8string[bytesRead..], out bits.a))
			{
				return false;
			}

			bytesRead += 8;
			bool usesHyphens = utf8string[bytesRead] == (byte)'-';

			if (usesHyphens)
			{
				bytesRead++;
			}

			if (!TryParseUtf8Short(utf8string[bytesRead..], out bits.b))
			{
				return false;
			}

			bytesRead += 4;
			if (usesHyphens && !TryReadHyphen(utf8string, ref bytesRead))
			{
				return false;
			}

			if (!TryParseUtf8Short(utf8string[bytesRead..], out bits.c))
			{
				return false;
			}

			bytesRead += 4;
			if (usesHyphens && !TryReadHyphen(utf8string, ref bytesRead))
			{
				return false;
			}

			if (!TryParseUtf8Byte(utf8string[bytesRead..], out bits.d))
			{
				return false;
			}

			bytesRead += 2;

			if (!TryParseUtf8Byte(utf8string[bytesRead..], out bits.e))
			{
				return false;
			}

			bytesRead += 2;

			if (usesHyphens && !TryReadHyphen(utf8string, ref bytesRead))
			{
				return false;
			}

			if (!(
				TryParseUtf8Byte(utf8string[bytesRead..], out bits.f) &&
				TryParseUtf8Byte(utf8string[(bytesRead + 2)..], out bits.g) &&
				TryParseUtf8Byte(utf8string[(bytesRead + 4)..], out bits.h) &&
				TryParseUtf8Byte(utf8string[(bytesRead + 6)..], out bits.i) &&
				TryParseUtf8Byte(utf8string[(bytesRead + 8)..], out bits.j) &&
				TryParseUtf8Byte(utf8string[(bytesRead + 10)..], out bits.k)))
			{
				return false;
			}

			bytesRead += 12; // 6 bytes read, each 2 bytes in length.

			bool TryReadHyphen(ReadOnlySpan<byte> utf8string, ref int bytesRead)
			{
				if (utf8string[bytesRead] != (byte)'-')
				{
					return false; // Expected a hyphen.
				}

				bytesRead++;
				return true;
			}
		}

		// Verify expected closure.
		if (usesCurlyBraces)
		{
			if (bytesRead == utf8string.Length || utf8string[bytesRead] != (byte)'}')
			{
				return false; // Expected a closing brace.
			}

			bytesRead++;
		}
		else if (usesParentheses)
		{
			if (bytesRead == utf8string.Length || utf8string[bytesRead] != (byte)')')
			{
				return false; // Expected a closing parenthesis.
			}

			bytesRead++;
		}

		if (bytesRead < utf8string.Length)
		{
			// extra bytes
			return false;
		}

		guid = bits.guid;
		return true;

		static bool TryParseUtf8Int(ReadOnlySpan<byte> utf8Bytes, out int value)
		{
			if (TryParseUtf8Short(utf8Bytes.Slice(0, 4), out short msb) &&
				TryParseUtf8Short(utf8Bytes.Slice(4, 4), out short lsb))
			{
				value = unchecked((msb << 16) | (ushort)lsb);
				return true;
			}
			else
			{
				value = 0;
				return false;
			}
		}

		static bool TryParseUtf8Short(ReadOnlySpan<byte> utf8Bytes, out short value)
		{
			if (TryParseUtf8Byte(utf8Bytes.Slice(0, 2), out byte msb) &&
				TryParseUtf8Byte(utf8Bytes.Slice(2, 2), out byte lsb))
			{
				value = unchecked((short)((msb << 8) | lsb));
				return true;
			}
			else
			{
				value = 0;
				return false;
			}
		}

		static bool TryParseUtf8Byte(ReadOnlySpan<byte> utf8Bytes, out byte value)
		{
			if (utf8Bytes.Length < 2)
			{
				value = 0;
				return false;
			}

			if (TryParseUtf8Char(utf8Bytes[0], out byte msb) &&
				TryParseUtf8Char(utf8Bytes[1], out byte lsb))
			{
				value = unchecked((byte)((msb << 4) | lsb));
				return true;
			}
			else
			{
				value = 0;
				return false;
			}
		}

		static bool TryParseUtf8Char(byte utf8Byte, out byte value)
		{
			unchecked
			{
				switch ((char)utf8Byte)
				{
					case >= '0' and <= '9':
						value = (byte)(utf8Byte - '0');
						return true;
					case >= 'a' and <= 'f':
						value = (byte)(10 + utf8Byte - 'a');
						return true;
					case >= 'A' and <= 'F':
						value = (byte)(10 + utf8Byte - 'A');
						return true;
					default:
						value = 0;
						return false;
				}
			}
		}
	}
}
