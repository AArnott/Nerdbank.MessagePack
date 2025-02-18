// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ShapeShift.MessagePack;

namespace ShapeShift.MessagePack;

/// <summary>
/// Primitive msgpack encoding/decoding methods.
/// </summary>
/// <devremarks>
/// This class makes aggressive use of `ref byte`-based APIs rather than the simpler and safer span-indexing based APIs.
/// The reason for this is it makes an appreciable reduction in assembly code size and improved performance.
/// We don't give up safety because we always check the buffer length ourselves first.
/// See <see href="https://sharplab.io/#v2:EYLgtghglgdgNAFxBAzmAPgAQEwEYCwAUJgAwAEmuAdAEICuAZgwKYBOKtsErAngNxFSFagCU6MBFDDMqAYQD2YAA5QANmwDKbAG5QAxsxQDi5SlTESpMgJIS28pVta6DRwQGYK2MrLIBvIjIgik9KADYyYHl5VTIAFV4AdVYoBGYAOTUACg0lCBgAHmAeNIA+MgATQ0kYCEl5eDJ5OgQyWFbitJRk1LSYAEoiAMIASE7DHoQ+sgBeMlxjEagGMiyqlBq6qAaqABlmGABzBAALMgLIkomUqYPB0eGRkcwAdjIGCFUUZkWAXyIiCMALLMMDyXhA7goE6fKgAcWYCBEzBYrAOBjW1S49QGszIJAAHgAOYzBMgAenJ602OIA2iQALp4wkkwGvMgIVh0H5Ef6EMmYTxRGJkXL5WxVAnMCo0VDSnJ5QrjcrUuBtCRkbT9fyBMnBZaral7A7HM4XAAs2uGeptFDeHy+PP5tr5trIuptKAA7qk9GcslqPXrrW7gno5ecyHRoeCEFRIQSAGqfbkgIOh4KTZg0KCHACiMAqUHymI2tOoVAZavEfuYegA1vKstGTrH+lr+qSM3rgGiIPW+G708FXd2ycOguzOdyu0FR5OhdFYsiGLLvhUFfkilcVdU1e1NVaJ20VqW4/sjqdI5adc6M+yHd9Z3r53rj2iVuNKtUV3iP2QQTBCEoRhVR4URFc2HRZgz07Y9vV9f1AzvYNjzJcNvkjFtY3jCAkxTZg0xQscsxzfNC2LGAsn/ABVGAUAgFgqAAQQqDd/2pFc1VwfpqxgWsGybbDWAQdt+jg4ju17Zh+0HW1j1fMdjynLknTJPkRiUFJtDqZhhAiTBzTIUjcwLIsSzFJUd2/DZsW2RoDx01RuXuJ4ZnKQDwR4SF2FAqgszPOyGjVHMEAUGBtDYNJWCoawUF2Xp1DMyiyAAfjIHNal4AAFFIwFSKBIo4ZFIvYZhkvyGBDBQAMCO1EBNTqxYtMK3T9IoIyTPI8yqP/L9qSChyNSclzARGdyyDohimKzOjPlzKr2JRGyaXskLUnC0roti+LEvKij8jSjKuByvKCqK8xmFK74KpgKqUBqkbmHqxrnOe4xfiAA=">this sample code</see>
/// to see how much smaller the code is by following these patterns.
/// </devremarks>
public static partial class MessagePackPrimitives
{
	/// <summary>
	/// Write a nil value to the specified buffer, if the buffer is large enough.
	/// </summary>
	/// <param name="destination">The buffer to write to. This should be at least 1 byte in length to ensure success.</param>
	/// <param name="bytesWritten">The number of bytes required to write the value, whether successful or not.</param>
	/// <returns>
	/// <see langword="true" /> if <paramref name="destination"/> was large enough and the value written; otherwise, <see langword="false" />.
	/// When <see langword="false"/>, the value of <paramref name="bytesWritten"/> is indicates how many bytes are required to write the value successfully.
	/// </returns>
	public static bool TryWriteNil(Span<byte> destination, out int bytesWritten)
	{
		bytesWritten = 1;
		if (destination.Length < bytesWritten)
		{
			return false;
		}

		MemoryMarshal.GetReference(destination) = MessagePackCode.Nil;
		return true;
	}

	/// <summary>
	/// Writes the array header to the specified buffer, if the buffer is large enough.
	/// </summary>
	/// <param name="destination">The buffer to write to. This should be at least 5 bytes in length to ensure success with any value for <paramref name="count"/>.</param>
	/// <param name="count">The number of elements in the array.</param>
	/// <param name="bytesWritten">The number of bytes required to write the array header, whether successful or not.</param>
	/// <returns>
	/// <see langword="true" /> if <paramref name="destination"/> was large enough and the array header written; otherwise, <see langword="false" />.
	/// When <see langword="false"/>, the value of <paramref name="bytesWritten"/> indicates how many bytes are required to write the array header successfully.
	/// </returns>
	/// <remarks>
	/// Writes the length of the next array to be written in the most compact form of
	/// <see cref="MessagePackCode.MinFixArray"/>,
	/// <see cref="MessagePackCode.Array16"/>, or
	/// <see cref="MessagePackCode.Array32"/>.
	/// </remarks>
	public static bool TryWriteArrayHeader(Span<byte> destination, uint count, out int bytesWritten)
	{
		ref byte destinationRef = ref MemoryMarshal.GetReference(destination);
		switch (count)
		{
			case <= MessagePackRange.MaxFixArrayCount:
				bytesWritten = 1;
				if (destination.Length < bytesWritten)
				{
					return false;
				}

				destinationRef = (byte)(MessagePackCode.MinFixArray | count);
				return true;
			case <= ushort.MaxValue:
				bytesWritten = 3;
				if (destination.Length < bytesWritten)
				{
					return false;
				}

				destinationRef = MessagePackCode.Array16;
				WriteBigEndian(ref Unsafe.Add(ref destinationRef, 1), (ushort)count);
				return true;
			default:
				bytesWritten = 5;
				if (destination.Length < bytesWritten)
				{
					return false;
				}

				destinationRef = MessagePackCode.Array32;
				WriteBigEndian(ref Unsafe.Add(ref destinationRef, 1), count);
				return true;
		}
	}

	/// <summary>
	/// Writes the map header to the specified buffer, if the buffer is large enough.
	/// </summary>
	/// <param name="destination">The buffer to write to. This should be at least 5 bytes in length to ensure success with any value for <paramref name="count"/>.</param>
	/// <param name="count">The number of key-value pairs in the map.</param>
	/// <param name="bytesWritten">The number of bytes required to write the map header, whether successful or not.</param>
	/// <returns>
	/// <see langword="true" /> if <paramref name="destination"/> was large enough and the map header written; otherwise, <see langword="false" />.
	/// When <see langword="false"/>, the value of <paramref name="bytesWritten"/> indicates how many bytes are required to write the map header successfully.
	/// </returns>
	/// <remarks>
	/// Writes the length of the next map to be written in the most compact form of
	/// <see cref="MessagePackCode.MinFixMap"/>,
	/// <see cref="MessagePackCode.Map16"/>, or
	/// <see cref="MessagePackCode.Map32"/>.
	/// </remarks>
	public static bool TryWriteMapHeader(Span<byte> destination, uint count, out int bytesWritten)
	{
		ref byte destinationRef = ref MemoryMarshal.GetReference(destination);
		switch (count)
		{
			case <= MessagePackRange.MaxFixMapCount:
				bytesWritten = 1;
				if (destination.Length < bytesWritten)
				{
					return false;
				}

				destinationRef = (byte)(MessagePackCode.MinFixMap | count);
				return true;
			case <= ushort.MaxValue:
				bytesWritten = 3;
				if (destination.Length < bytesWritten)
				{
					return false;
				}

				destinationRef = MessagePackCode.Map16;
				WriteBigEndian(ref Unsafe.Add(ref destinationRef, 1), (ushort)count);
				return true;
			default:
				bytesWritten = 5;
				if (destination.Length < bytesWritten)
				{
					return false;
				}

				destinationRef = MessagePackCode.Map32;
				WriteBigEndian(ref Unsafe.Add(ref destinationRef, 1), count);
				return true;
		}
	}

	/// <summary>
	/// Writes a signed integer value to the specified buffer, if the buffer is large enough.
	/// </summary>
	/// <param name="destination">The buffer to write to. This should be at least 2 bytes in length to ensure success with any value for <paramref name="value"/>.</param>
	/// <param name="value">The signed integer value to write.</param>
	/// <param name="bytesWritten">The number of bytes required to write the value, whether successful or not.</param>
	/// <returns>
	/// <see langword="true" /> if <paramref name="destination"/> was large enough and the value written; otherwise, <see langword="false" />.
	/// When <see langword="false"/>, the value of <paramref name="bytesWritten"/> indicates how many bytes are required to write the value successfully.
	/// </returns>
	/// <remarks>
	/// The smallest possible representation is used for the value, which may be as few as 1 byte.
	/// In addition to the built-in 1-byte code when within specific MessagePack-supported ranges,
	/// any of the following encodings may also be used:
	/// <see cref="MessagePackCode.UInt8"/>,
	/// <see cref="MessagePackCode.Int8"/>.
	/// </remarks>
	public static bool TryWrite(Span<byte> destination, sbyte value, out int bytesWritten)
	{
		if (value >= 0)
		{
			return TryWrite(destination, unchecked((byte)value), out bytesWritten);
		}

		switch (value)
		{
			case >= MessagePackRange.MinFixNegativeInt: return TryWriteNegativeFixIntUnsafe(destination, value, out bytesWritten);
			default: return TryWriteInt8(destination, value, out bytesWritten);
		}
	}

	/// <summary>
	/// Writes a signed integer value to the specified buffer, if the buffer is large enough.
	/// </summary>
	/// <param name="destination">The buffer to write to. This should be at least 3 bytes in length to ensure success with any value for <paramref name="value"/>.</param>
	/// <param name="value">The signed integer value to write.</param>
	/// <param name="bytesWritten">The number of bytes required to write the value, whether successful or not.</param>
	/// <returns>
	/// <see langword="true" /> if <paramref name="destination"/> was large enough and the value written; otherwise, <see langword="false" />.
	/// When <see langword="false"/>, the value of <paramref name="bytesWritten"/> indicates how many bytes are required to write the value successfully.
	/// </returns>
	/// <remarks>
	/// The smallest possible representation is used for the value, which may be as few as 1 byte.
	/// In addition to the built-in 1-byte code when within specific MessagePack-supported ranges,
	/// any of the following encodings may also be used:
	/// <see cref="MessagePackCode.UInt8"/>,
	/// <see cref="MessagePackCode.UInt16"/>,
	/// <see cref="MessagePackCode.Int8"/>,
	/// <see cref="MessagePackCode.Int16"/>.
	/// </remarks>
	public static bool TryWrite(Span<byte> destination, short value, out int bytesWritten)
	{
		if (value >= 0)
		{
			return TryWrite(destination, unchecked((ushort)value), out bytesWritten);
		}

		switch (value)
		{
			case >= MessagePackRange.MinFixNegativeInt: return TryWriteNegativeFixIntUnsafe(destination, unchecked((sbyte)value), out bytesWritten);
			case >= sbyte.MinValue: return TryWriteInt8(destination, unchecked((sbyte)value), out bytesWritten);
			default: return TryWriteInt16(destination, value, out bytesWritten);
		}
	}

	/// <summary>
	/// Writes a signed integer value to the specified buffer, if the buffer is large enough.
	/// </summary>
	/// <param name="destination">The buffer to write to. This should be at least 5 bytes in length to ensure success with any value for <paramref name="value"/>.</param>
	/// <param name="value">The signed integer value to write.</param>
	/// <param name="bytesWritten">The number of bytes required to write the value, whether successful or not.</param>
	/// <returns>
	/// <see langword="true" /> if <paramref name="destination"/> was large enough and the value written; otherwise, <see langword="false" />.
	/// When <see langword="false"/>, the value of <paramref name="bytesWritten"/> indicates how many bytes are required to write the value successfully.
	/// </returns>
	/// <remarks>
	/// The smallest possible representation is used for the value, which may be as few as 1 byte.
	/// In addition to the built-in 1-byte code when within specific MessagePack-supported ranges,
	/// any of the following encodings may also be used:
	/// <see cref="MessagePackCode.UInt8"/>,
	/// <see cref="MessagePackCode.UInt16"/>,
	/// <see cref="MessagePackCode.UInt32"/>,
	/// <see cref="MessagePackCode.Int8"/>,
	/// <see cref="MessagePackCode.Int16"/>,
	/// <see cref="MessagePackCode.Int32"/>.
	/// </remarks>
	public static bool TryWrite(Span<byte> destination, int value, out int bytesWritten)
	{
		if (value >= 0)
		{
			return TryWrite(destination, unchecked((uint)value), out bytesWritten);
		}

		switch (value)
		{
			case >= MessagePackRange.MinFixNegativeInt: return TryWriteNegativeFixIntUnsafe(destination, unchecked((sbyte)value), out bytesWritten);
			case >= sbyte.MinValue: return TryWriteInt8(destination, unchecked((sbyte)value), out bytesWritten);
			case >= short.MinValue: return TryWriteInt16(destination, unchecked((short)value), out bytesWritten);
			default: return TryWriteInt32(destination, value, out bytesWritten);
		}
	}

	/// <summary>
	/// Writes a signed integer value to the specified buffer, if the buffer is large enough.
	/// </summary>
	/// <param name="destination">The buffer to write to. This should be at least 9 bytes in length to ensure success with any value for <paramref name="value"/>.</param>
	/// <param name="value">The signed integer value to write.</param>
	/// <param name="bytesWritten">The number of bytes required to write the value, whether successful or not.</param>
	/// <returns>
	/// <see langword="true" /> if <paramref name="destination"/> was large enough and the value written; otherwise, <see langword="false" />.
	/// When <see langword="false"/>, the value of <paramref name="bytesWritten"/> indicates how many bytes are required to write the value successfully.
	/// </returns>
	/// <remarks>
	/// The smallest possible representation is used for the value, which may be as few as 1 byte.
	/// In addition to the built-in 1-byte code when within specific MessagePack-supported ranges,
	/// any of the following encodings may also be used:
	/// <see cref="MessagePackCode.UInt8"/>,
	/// <see cref="MessagePackCode.UInt16"/>,
	/// <see cref="MessagePackCode.UInt32"/>,
	/// <see cref="MessagePackCode.UInt64"/>,
	/// <see cref="MessagePackCode.Int8"/>,
	/// <see cref="MessagePackCode.Int16"/>,
	/// <see cref="MessagePackCode.Int32"/>,
	/// <see cref="MessagePackCode.Int64"/>.
	/// </remarks>
	public static bool TryWrite(Span<byte> destination, long value, out int bytesWritten)
	{
		if (value >= 0)
		{
			return TryWrite(destination, unchecked((ulong)value), out bytesWritten);
		}

		return SlowPath(destination, value, out bytesWritten);
		static bool SlowPath(Span<byte> destination, long value, out int bytesWritten)
		{
			switch (value)
			{
				case >= 0: return TryWrite(destination, unchecked((ulong)value), out bytesWritten);
				case >= MessagePackRange.MinFixNegativeInt: return TryWriteNegativeFixIntUnsafe(destination, unchecked((sbyte)value), out bytesWritten);
				case >= sbyte.MinValue: return TryWriteInt8(destination, unchecked((sbyte)value), out bytesWritten);
				case >= short.MinValue: return TryWriteInt16(destination, unchecked((short)value), out bytesWritten);
				case >= int.MinValue: return TryWriteInt32(destination, unchecked((int)value), out bytesWritten);
				default: return TryWriteInt64(destination, value, out bytesWritten);
			}
		}
	}

	/// <summary>
	/// Writes an 8-bit signed integer value to the specified buffer, if the buffer is large enough.
	/// </summary>
	/// <param name="destination">The buffer to write to. This should be at least 2 bytes in length.</param>
	/// <param name="value">The 8-bit signed integer value to write.</param>
	/// <param name="bytesWritten">The number of bytes required to write the value, whether successful or not.</param>
	/// <returns>
	/// <see langword="true" /> if <paramref name="destination"/> was large enough and the value written; otherwise, <see langword="false" />.
	/// When <see langword="false"/>, the value of <paramref name="bytesWritten"/> indicates how many bytes are required to write the value successfully.
	/// </returns>
	/// <remarks>
	/// The <see cref="MessagePackCode.Int8"/> encoding is always used, even if the value could be encoded in fewer bytes.
	/// </remarks>
	public static bool TryWriteInt8(Span<byte> destination, sbyte value, out int bytesWritten)
	{
		ref byte destinationRef = ref MemoryMarshal.GetReference(destination);
		bytesWritten = 2;
		if (destination.Length < bytesWritten)
		{
			return false;
		}

		destinationRef = MessagePackCode.Int8;
		Unsafe.Add(ref destinationRef, 1) = unchecked((byte)value);
		return true;
	}

	/// <summary>
	/// Writes a 16-bit signed integer value to the specified buffer, if the buffer is large enough.
	/// </summary>
	/// <param name="destination">The buffer to write to. This should be at least 3 bytes in length to ensure success.</param>
	/// <param name="value">The 16-bit signed integer value to write.</param>
	/// <param name="bytesWritten">The number of bytes required to write the value, whether successful or not.</param>
	/// <returns>
	/// <see langword="true" /> if <paramref name="destination"/> was large enough and the value written; otherwise, <see langword="false" />.
	/// When <see langword="false"/>, the value of <paramref name="bytesWritten"/> indicates how many bytes are required to write the value successfully.
	/// </returns>
	/// <remarks>
	/// The <see cref="MessagePackCode.Int16"/> encoding is always used, even if the value could be encoded in fewer bytes.
	/// </remarks>
	public static bool TryWriteInt16(Span<byte> destination, short value, out int bytesWritten)
	{
		ref byte destinationRef = ref MemoryMarshal.GetReference(destination);
		bytesWritten = 3;
		if (destination.Length < bytesWritten)
		{
			return false;
		}

		destinationRef = MessagePackCode.Int16;
		WriteBigEndian(ref Unsafe.Add(ref destinationRef, 1), value);
		return true;
	}

	/// <summary>
	/// Writes a 32-bit signed integer value to the specified buffer, if the buffer is large enough.
	/// </summary>
	/// <param name="destination">The buffer to write to. This should be at least 5 bytes in length to ensure success.</param>
	/// <param name="value">The 32-bit signed integer value to write.</param>
	/// <param name="bytesWritten">The number of bytes required to write the value, whether successful or not.</param>
	/// <returns>
	/// <see langword="true" /> if <paramref name="destination"/> was large enough and the value written; otherwise, <see langword="false" />.
	/// When <see langword="false"/>, the value of <paramref name="bytesWritten"/> indicates how many bytes are required to write the value successfully.
	/// </returns>
	/// <remarks>
	/// The <see cref="MessagePackCode.Int32"/> encoding is always used, even if the value could be encoded in fewer bytes.
	/// </remarks>
	public static bool TryWriteInt32(Span<byte> destination, int value, out int bytesWritten)
	{
		ref byte destinationRef = ref MemoryMarshal.GetReference(destination);
		bytesWritten = 5;
		if (destination.Length < bytesWritten)
		{
			return false;
		}

		destinationRef = MessagePackCode.Int32;
		WriteBigEndian(ref Unsafe.Add(ref destinationRef, 1), value);
		return true;
	}

	/// <summary>
	/// Writes a 64-bit signed integer value to the specified buffer, if the buffer is large enough.
	/// </summary>
	/// <param name="destination">The buffer to write to. This should be at least 9 bytes in length to ensure success.</param>
	/// <param name="value">The 64-bit signed integer value to write.</param>
	/// <param name="bytesWritten">The number of bytes required to write the value, whether successful or not.</param>
	/// <returns>
	/// <see langword="true" /> if <paramref name="destination"/> was large enough and the value written; otherwise, <see langword="false" />.
	/// When <see langword="false"/>, the value of <paramref name="bytesWritten"/> indicates how many bytes are required to write the value successfully.
	/// </returns>
	/// <remarks>
	/// The <see cref="MessagePackCode.Int64"/> encoding is always used, even if the value could be encoded in fewer bytes.
	/// </remarks>
	public static bool TryWriteInt64(Span<byte> destination, long value, out int bytesWritten)
	{
		ref byte destinationRef = ref MemoryMarshal.GetReference(destination);
		bytesWritten = 9;
		if (destination.Length < bytesWritten)
		{
			return false;
		}

		destinationRef = MessagePackCode.Int64;
		WriteBigEndian(ref Unsafe.Add(ref destinationRef, 1), value);
		return true;
	}

	/// <summary>
	/// Writes an unsigned integer value to the specified buffer, if the buffer is large enough.
	/// </summary>
	/// <param name="destination">The buffer to write to. This should be at least 2 bytes in length to ensure success with any value for <paramref name="value"/>.</param>
	/// <param name="value">The unsigned integer value to write.</param>
	/// <param name="bytesWritten">The number of bytes required to write the value, whether successful or not.</param>
	/// <returns>
	/// <see langword="true" /> if <paramref name="destination"/> was large enough and the value written; otherwise, <see langword="false" />.
	/// When <see langword="false"/>, the value of <paramref name="bytesWritten"/> indicates how many bytes are required to write the value successfully.
	/// </returns>
	/// <remarks>
	/// The smallest possible representation is used for the value, which may be as few as 1 byte.
	/// In addition to the built-in 1-byte code when within specific MessagePack-supported ranges,
	/// any of the following encodings may also be used:
	/// <see cref="MessagePackCode.UInt8"/>.
	/// </remarks>
	public static bool TryWrite(Span<byte> destination, byte value, out int bytesWritten)
	{
		switch (value)
		{
			case <= MessagePackRange.MaxFixPositiveInt:
				return TryWriteFixIntUnsafe(destination, value, out bytesWritten);
			default:
				return TryWriteUInt8(destination, value, out bytesWritten);
		}
	}

	/// <summary>
	/// Writes an unsigned integer value to the specified buffer, if the buffer is large enough.
	/// </summary>
	/// <param name="destination">The buffer to write to. This should be at least 3 bytes in length to ensure success with any value for <paramref name="value"/>.</param>
	/// <param name="value">The unsigned integer value to write.</param>
	/// <param name="bytesWritten">The number of bytes required to write the value, whether successful or not.</param>
	/// <returns>
	/// <see langword="true" /> if <paramref name="destination"/> was large enough and the value written; otherwise, <see langword="false" />.
	/// When <see langword="false"/>, the value of <paramref name="bytesWritten"/> indicates how many bytes are required to write the value successfully.
	/// </returns>
	/// <remarks>
	/// The smallest possible representation is used for the value, which may be as few as 1 byte.
	/// In addition to the built-in 1-byte code when within specific MessagePack-supported ranges,
	/// any of the following encodings may also be used:
	/// <see cref="MessagePackCode.UInt8"/>,
	/// <see cref="MessagePackCode.UInt16"/>.
	/// </remarks>
	public static bool TryWrite(Span<byte> destination, ushort value, out int bytesWritten)
	{
		switch (value)
		{
			case <= MessagePackRange.MaxFixPositiveInt:
				return TryWriteFixIntUnsafe(destination, unchecked((byte)value), out bytesWritten);
			case <= byte.MaxValue:
				return TryWriteUInt8(destination, unchecked((byte)value), out bytesWritten);
			default:
				return TryWriteUInt16(destination, value, out bytesWritten);
		}
	}

	/// <summary>
	/// Writes an unsigned integer value to the specified buffer, if the buffer is large enough.
	/// </summary>
	/// <param name="destination">The buffer to write to. This should be at least 5 bytes in length to ensure success with any value for <paramref name="value"/>.</param>
	/// <param name="value">The unsigned integer value to write.</param>
	/// <param name="bytesWritten">The number of bytes required to write the value, whether successful or not.</param>
	/// <returns>
	/// <see langword="true" /> if <paramref name="destination"/> was large enough and the value written; otherwise, <see langword="false" />.
	/// When <see langword="false"/>, the value of <paramref name="bytesWritten"/> indicates how many bytes are required to write the value successfully.
	/// </returns>
	/// <remarks>
	/// The smallest possible representation is used for the value, which may be as few as 1 byte.
	/// In addition to the built-in 1-byte code when within specific MessagePack-supported ranges,
	/// any of the following encodings may also be used:
	/// <see cref="MessagePackCode.UInt8"/>,
	/// <see cref="MessagePackCode.UInt16"/>,
	/// <see cref="MessagePackCode.UInt32"/>.
	/// </remarks>
	public static bool TryWrite(Span<byte> destination, uint value, out int bytesWritten)
	{
		switch (value)
		{
			case <= MessagePackRange.MaxFixPositiveInt:
				return TryWriteFixIntUnsafe(destination, unchecked((byte)value), out bytesWritten);
			case <= byte.MaxValue:
				return TryWriteUInt8(destination, unchecked((byte)value), out bytesWritten);
			case <= ushort.MaxValue:
				return TryWriteUInt16(destination, unchecked((ushort)value), out bytesWritten);
			default:
				return TryWriteUInt32(destination, value, out bytesWritten);
		}
	}

	/// <summary>
	/// Writes an unsigned integer value to the specified buffer, if the buffer is large enough.
	/// </summary>
	/// <param name="destination">The buffer to write to. This should be at least 9 bytes in length to ensure success with any value for <paramref name="value"/>.</param>
	/// <param name="value">The unsigned integer value to write.</param>
	/// <param name="bytesWritten">The number of bytes required to write the value, whether successful or not.</param>
	/// <returns>
	/// <see langword="true" /> if <paramref name="destination"/> was large enough and the value written; otherwise, <see langword="false" />.
	/// When <see langword="false"/>, the value of <paramref name="bytesWritten"/> indicates how many bytes are required to write the value successfully.
	/// </returns>
	/// <remarks>
	/// The smallest possible representation is used for the value, which may be as few as 1 byte.
	/// In addition to the built-in 1-byte code when within specific MessagePack-supported ranges,
	/// any of the following encodings may also be used:
	/// <see cref="MessagePackCode.UInt8"/>,
	/// <see cref="MessagePackCode.UInt16"/>,
	/// <see cref="MessagePackCode.UInt32"/>,
	/// <see cref="MessagePackCode.UInt64"/>.
	/// </remarks>
	public static bool TryWrite(Span<byte> destination, ulong value, out int bytesWritten)
	{
		if (value <= MessagePackRange.MaxFixPositiveInt)
		{
			return TryWriteFixIntUnsafe(destination, unchecked((byte)value), out bytesWritten);
		}

		return SlowPath(destination, value, out bytesWritten);

		static bool SlowPath(Span<byte> destination, ulong value, out int bytesWritten)
		{
			switch (value)
			{
				case <= byte.MaxValue:
					return TryWriteUInt8(destination, unchecked((byte)value), out bytesWritten);
				case <= ushort.MaxValue:
					return TryWriteUInt16(destination, unchecked((ushort)value), out bytesWritten);
				case <= uint.MaxValue:
					return TryWriteUInt32(destination, unchecked((uint)value), out bytesWritten);
				default:
					return TryWriteUInt64(destination, value, out bytesWritten);
			}
		}
	}

	/// <summary>
	/// Writes an 8-bit unsigned integer value to the specified buffer, if the buffer is large enough.
	/// </summary>
	/// <param name="destination">The buffer to write to. This should be at least 2 bytes in length.</param>
	/// <param name="value">The 8-bit unsigned integer value to write.</param>
	/// <param name="bytesWritten">The number of bytes required to write the value, whether successful or not.</param>
	/// <returns>
	/// <see langword="true" /> if <paramref name="destination"/> was large enough and the value written; otherwise, <see langword="false" />.
	/// When <see langword="false"/>, the value of <paramref name="bytesWritten"/> indicates how many bytes are required to write the value successfully.
	/// </returns>
	/// <remarks>
	/// The <see cref="MessagePackCode.UInt8"/> encoding is always used, even if the value could be encoded in fewer bytes.
	/// </remarks>
	public static bool TryWriteUInt8(Span<byte> destination, byte value, out int bytesWritten)
	{
		ref byte destinationRef = ref MemoryMarshal.GetReference(destination);
		bytesWritten = 2;
		if (destination.Length < bytesWritten)
		{
			return false;
		}

		destinationRef = MessagePackCode.UInt8;
		Unsafe.Add(ref destinationRef, 1) = value;
		return true;
	}

	/// <summary>
	/// Writes an 16-bit unsigned integer value to the specified buffer, if the buffer is large enough.
	/// </summary>
	/// <param name="destination">The buffer to write to. This should be at least 3 bytes in length.</param>
	/// <param name="value">The 16-bit unsigned integer value to write.</param>
	/// <param name="bytesWritten">The number of bytes required to write the value, whether successful or not.</param>
	/// <returns>
	/// <see langword="true" /> if <paramref name="destination"/> was large enough and the value written; otherwise, <see langword="false" />.
	/// When <see langword="false"/>, the value of <paramref name="bytesWritten"/> indicates how many bytes are required to write the value successfully.
	/// </returns>
	/// <remarks>
	/// The <see cref="MessagePackCode.UInt16"/> encoding is always used, even if the value could be encoded in fewer bytes.
	/// </remarks>
	public static bool TryWriteUInt16(Span<byte> destination, ushort value, out int bytesWritten)
	{
		ref byte destinationRef = ref MemoryMarshal.GetReference(destination);
		bytesWritten = 3;
		if (destination.Length < bytesWritten)
		{
			return false;
		}

		destinationRef = MessagePackCode.UInt16;
		WriteBigEndian(ref Unsafe.Add(ref destinationRef, 1), value);
		return true;
	}

	/// <summary>
	/// Writes a 32-bit unsigned integer value to the specified buffer, if the buffer is large enough.
	/// </summary>
	/// <param name="destination">The buffer to write to. This should be at least 5 bytes in length.</param>
	/// <param name="value">The 32-bit unsigned integer value to write.</param>
	/// <param name="bytesWritten">The number of bytes required to write the value, whether successful or not.</param>
	/// <returns>
	/// <see langword="true" /> if <paramref name="destination"/> was large enough and the value written; otherwise, <see langword="false" />.
	/// When <see langword="false"/>, the value of <paramref name="bytesWritten"/> indicates how many bytes are required to write the value successfully.
	/// </returns>
	/// <remarks>
	/// The <see cref="MessagePackCode.UInt32"/> encoding is always used, even if the value could be encoded in fewer bytes.
	/// </remarks>
	public static bool TryWriteUInt32(Span<byte> destination, uint value, out int bytesWritten)
	{
		ref byte destinationRef = ref MemoryMarshal.GetReference(destination);
		bytesWritten = 5;
		if (destination.Length < bytesWritten)
		{
			return false;
		}

		destinationRef = MessagePackCode.UInt32;
		WriteBigEndian(ref Unsafe.Add(ref destinationRef, 1), value);
		return true;
	}

	/// <summary>
	/// Writes a 64-bit unsigned integer value to the specified buffer, if the buffer is large enough.
	/// </summary>
	/// <param name="destination">The buffer to write to. This should be at least 9 bytes in length.</param>
	/// <param name="value">The 64-bit unsigned integer value to write.</param>
	/// <param name="bytesWritten">The number of bytes required to write the value, whether successful or not.</param>
	/// <returns>
	/// <see langword="true" /> if <paramref name="destination"/> was large enough and the value written; otherwise, <see langword="false" />.
	/// When <see langword="false"/>, the value of <paramref name="bytesWritten"/> indicates how many bytes are required to write the value successfully.
	/// </returns>
	/// <remarks>
	/// The <see cref="MessagePackCode.UInt64"/> encoding is always used, even if the value could be encoded in fewer bytes.
	/// </remarks>
	public static bool TryWriteUInt64(Span<byte> destination, ulong value, out int bytesWritten)
	{
		ref byte destinationRef = ref MemoryMarshal.GetReference(destination);
		bytesWritten = 9;
		if (destination.Length < bytesWritten)
		{
			return false;
		}

		destinationRef = MessagePackCode.UInt64;
		WriteBigEndian(ref Unsafe.Add(ref destinationRef, 1), value);
		return true;
	}

	/// <summary>
	/// Writes a single-precision floating-point value to the specified buffer, if the buffer is large enough.
	/// </summary>
	/// <param name="destination">The buffer to write to. This should be at least 5 bytes in length to ensure success.</param>
	/// <param name="value">The single-precision floating-point value to write.</param>
	/// <param name="bytesWritten">The number of bytes required to write the value, whether successful or not.</param>
	/// <returns>
	/// <see langword="true" /> if <paramref name="destination"/> was large enough and the value written; otherwise, <see langword="false" />.
	/// When <see langword="false"/>, the value of <paramref name="bytesWritten"/> indicates how many bytes are required to write the value successfully.
	/// </returns>
	/// <remarks>
	/// The <see cref="MessagePackCode.Float32"/> encoding is always used.
	/// </remarks>
	public static unsafe bool TryWrite(Span<byte> destination, float value, out int bytesWritten)
	{
		ref byte destinationRef = ref MemoryMarshal.GetReference(destination);
		bytesWritten = 5;
		if (destination.Length < bytesWritten)
		{
			return false;
		}

		destinationRef = MessagePackCode.Float32;
#if NET
		int valueAsInt = Unsafe.BitCast<float, int>(value);
#else
		int valueAsInt = *(int*)&value;
#endif
		WriteBigEndian(ref Unsafe.Add(ref destinationRef, 1), valueAsInt);
		return true;
	}

	/// <summary>
	/// Writes a double-precision floating-point value to the specified buffer, if the buffer is large enough.
	/// </summary>
	/// <param name="destination">The buffer to write to. This should be at least 9 bytes in length to ensure success.</param>
	/// <param name="value">The double-precision floating-point value to write.</param>
	/// <param name="bytesWritten">The number of bytes required to write the value, whether successful or not.</param>
	/// <returns>
	/// <see langword="true" /> if <paramref name="destination"/> was large enough and the value written; otherwise, <see langword="false" />.
	/// When <see langword="false"/>, the value of <paramref name="bytesWritten"/> indicates how many bytes are required to write the value successfully.
	/// </returns>
	/// <remarks>
	/// The <see cref="MessagePackCode.Float64"/> encoding is always used.
	/// </remarks>
	public static unsafe bool TryWrite(Span<byte> destination, double value, out int bytesWritten)
	{
		ref byte destinationRef = ref MemoryMarshal.GetReference(destination);
		bytesWritten = 9;
		if (destination.Length < bytesWritten)
		{
			return false;
		}

		destinationRef = MessagePackCode.Float64;
#if NET
		long valueAsLong = Unsafe.BitCast<double, long>(value);
#else
		long valueAsLong = *(long*)&value;
#endif
		WriteBigEndian(ref Unsafe.Add(ref destinationRef, 1), valueAsLong);
		return true;
	}

	/// <summary>
	/// Writes a boolean value to the specified buffer, if the buffer is large enough.
	/// </summary>
	/// <param name="destination">The buffer to write to. This should be at least 1 byte in length to ensure success.</param>
	/// <param name="value">The boolean value to write.</param>
	/// <param name="bytesWritten">The number of bytes required to write the value, whether successful or not.</param>
	/// <returns>
	/// <see langword="true" /> if <paramref name="destination"/> was large enough and the value written; otherwise, <see langword="false" />.
	/// When <see langword="false"/>, the value of <paramref name="bytesWritten"/> indicates how many bytes are required to write the value successfully.
	/// </returns>
	public static bool TryWrite(Span<byte> destination, bool value, out int bytesWritten)
	{
		ref byte destinationRef = ref MemoryMarshal.GetReference(destination);
		bytesWritten = 1;
		if (destination.Length < bytesWritten)
		{
			return false;
		}

		destinationRef = value ? MessagePackCode.True : MessagePackCode.False;
		return true;
	}

	/// <summary>
	/// Writes a character value to the specified buffer, if the buffer is large enough.
	/// </summary>
	/// <param name="destination">The buffer to write to. This should be at least 3 bytes in length to ensure success.</param>
	/// <param name="value">The character value to write.</param>
	/// <param name="bytesWritten">The number of bytes required to write the value, whether successful or not.</param>
	/// <returns>
	/// <see langword="true" /> if <paramref name="destination"/> was large enough and the value written; otherwise, <see langword="false" />.
	/// When <see langword="false"/>, the value of <paramref name="bytesWritten"/> indicates how many bytes are required to write the value successfully.
	/// </returns>
	/// <remarks>
	/// A character is encoded as a 16-bit unsigned integer, in its most compact form.
	/// </remarks>
	public static bool TryWrite(Span<byte> destination, char value, out int bytesWritten) => TryWrite(destination, (ushort)value, out bytesWritten);

	/// <summary>
	/// Writes a <see cref="DateTime"/> value to the specified buffer, if the buffer is large enough.
	/// </summary>
	/// <param name="destination">The buffer to write to. This should be at least 315 bytes in length to ensure success.</param>
	/// <param name="value">The <see cref="DateTime"/> value to write.</param>
	/// <param name="bytesWritten">The number of bytes required to write the value, whether successful or not.</param>
	/// <returns>
	/// <see langword="true" /> if <paramref name="destination"/> was large enough and the value written; otherwise, <see langword="false" />.
	/// When <see langword="false"/>, the value of <paramref name="bytesWritten"/> indicates how many bytes are required to write the value successfully.
	/// </returns>
	/// <remarks>
	/// The value is encoded as an extension type with a type code of <see cref="ReservedMessagePackExtensionTypeCode.DateTime"/>.
	/// </remarks>
	public static bool TryWrite(Span<byte> destination, DateTime value, out int bytesWritten)
	{
		ref byte destinationRef = ref MemoryMarshal.GetReference(destination);

		// Timestamp spec
		// https://github.com/msgpack/msgpack/pull/209
		// FixExt4(-1) => seconds |  [1970-01-01 00:00:00 UTC, 2106-02-07 06:28:16 UTC) range
		// FixExt8(-1) => nanoseconds + seconds | [1970-01-01 00:00:00.000000000 UTC, 2514-05-30 01:53:04.000000000 UTC) range
		// Ext8(12,-1) => nanoseconds + seconds | [-584554047284-02-23 16:59:44 UTC, 584554051223-11-09 07:00:16.000000000 UTC) range

		// The spec requires UTC. Convert to UTC if we're sure the value was expressed as Local time.
		// If it's Unspecified, we want to leave it alone since .NET will change the value when we convert
		// and we simply don't know, so we should leave it as-is.
		if (value.Kind == DateTimeKind.Local)
		{
			value = value.ToUniversalTime();
		}

		long secondsSinceBclEpoch = value.Ticks / TimeSpan.TicksPerSecond;
		long seconds = secondsSinceBclEpoch - DateTimeConstants.BclSecondsAtUnixEpoch;
		long nanoseconds = (value.Ticks % TimeSpan.TicksPerSecond) * DateTimeConstants.NanosecondsPerTick;

		// reference pseudo code.
		/*
		struct timespec {
			long tv_sec;  // seconds
			long tv_nsec; // nanoseconds
		} time;
		if ((time.tv_sec >> 34) == 0)
		{
			uint64_t data64 = (time.tv_nsec << 34) | time.tv_sec;
			if (data & 0xffffffff00000000L == 0)
			{
				// timestamp 32
				uint32_t data32 = data64;
				serialize(0xd6, -1, data32)
			}
			else
			{
				// timestamp 64
				serialize(0xd7, -1, data64)
			}
		}
		else
		{
			// timestamp 96
			serialize(0xc7, 12, -1, time.tv_nsec, time.tv_sec)
		}
		*/

		if ((seconds >> 34) == 0)
		{
			ulong data64 = unchecked((ulong)((nanoseconds << 34) | seconds));
			if ((data64 & 0xffffffff00000000L) == 0)
			{
				bytesWritten = 6;
				if (destination.Length < bytesWritten)
				{
					return false;
				}

				// timestamp 32(seconds in 32-bit unsigned int)
				uint data32 = (uint)data64;
				destinationRef = MessagePackCode.FixExt4;
				Unsafe.Add(ref destinationRef, 1) = unchecked((byte)ReservedMessagePackExtensionTypeCode.DateTime);
				WriteBigEndian(ref Unsafe.Add(ref destinationRef, 2), data32);
			}
			else
			{
				bytesWritten = 10;
				if (destination.Length < bytesWritten)
				{
					return false;
				}

				// timestamp 64(nanoseconds in 30-bit unsigned int | seconds in 34-bit unsigned int)
				destinationRef = MessagePackCode.FixExt8;
				Unsafe.Add(ref destinationRef, 1) = unchecked((byte)ReservedMessagePackExtensionTypeCode.DateTime);
				WriteBigEndian(ref Unsafe.Add(ref destinationRef, 2), data64);
			}
		}
		else
		{
			bytesWritten = 15;
			if (destination.Length < bytesWritten)
			{
				return false;
			}

			// timestamp 96( nanoseconds in 32-bit unsigned int | seconds in 64-bit signed int )
			destinationRef = MessagePackCode.Ext8;
			Unsafe.Add(ref destinationRef, 1) = 12;
			Unsafe.Add(ref destinationRef, 2) = unchecked((byte)ReservedMessagePackExtensionTypeCode.DateTime);
			WriteBigEndian(ref Unsafe.Add(ref destinationRef, 3), (uint)nanoseconds);
			WriteBigEndian(ref Unsafe.Add(ref destinationRef, 7), seconds);
		}

		return true;
	}

	/// <summary>
	/// Writes the binary header to the specified buffer, if the buffer is large enough.
	/// </summary>
	/// <param name="destination">The buffer to write to. This should be at least 5 bytes in length to ensure success.</param>
	/// <param name="length">The length of the binary data.</param>
	/// <param name="bytesWritten">The number of bytes required to write the binary header, whether successful or not.</param>
	/// <returns>
	/// <see langword="true" /> if <paramref name="destination"/> was large enough and the binary header written; otherwise, <see langword="false" />.
	/// When <see langword="false"/>, the value of <paramref name="bytesWritten"/> indicates how many bytes are required to write the binary header successfully.
	/// </returns>
	/// <remarks>
	/// Writes the length of the binary data in the most compact form of
	/// <see cref="MessagePackCode.Bin8"/>,
	/// <see cref="MessagePackCode.Bin16"/>, or
	/// <see cref="MessagePackCode.Bin32"/>.
	/// </remarks>
	public static bool TryWriteBinHeader(Span<byte> destination, uint length, out int bytesWritten)
	{
		ref byte destinationRef = ref MemoryMarshal.GetReference(destination);
		switch (length)
		{
			case <= byte.MaxValue:
				bytesWritten = 2;
				if (destination.Length < bytesWritten)
				{
					return false;
				}

				destinationRef = MessagePackCode.Bin8;
				Unsafe.Add(ref destinationRef, 1) = (byte)length;
				return true;
			case <= ushort.MaxValue:
				bytesWritten = 3;
				if (destination.Length < bytesWritten)
				{
					return false;
				}

				destinationRef = MessagePackCode.Bin16;
				WriteBigEndian(ref Unsafe.Add(ref destinationRef, 1), (ushort)length);
				return true;
			default:
				bytesWritten = 5;
				if (destination.Length < bytesWritten)
				{
					return false;
				}

				destinationRef = MessagePackCode.Bin32;
				WriteBigEndian(ref Unsafe.Add(ref destinationRef, 1), length);
				return true;
		}
	}

	/// <summary>
	/// Writes the header for a UTF-8 encoded string to the specified buffer, if the buffer is large enough.
	/// </summary>
	/// <param name="destination">The buffer to write to. This should be at least 5 bytes in length to ensure success.</param>
	/// <param name="byteCount">The number of UTF-8 encoded bytes in the string.</param>
	/// <param name="bytesWritten">The number of bytes required to write the string header, whether successful or not.</param>
	/// <returns>
	/// <see langword="true" /> if <paramref name="destination"/> was large enough and the string header written; otherwise, <see langword="false" />.
	/// When <see langword="false"/>, the value of <paramref name="bytesWritten"/> indicates how many bytes are required to write the string header successfully.
	/// </returns>
	/// <remarks>
	/// Writes the length of the string in the most compact form of
	/// <see cref="MessagePackCode.MinFixStr"/>,
	/// <see cref="MessagePackCode.Str8"/>,
	/// <see cref="MessagePackCode.Str16"/>, or
	/// <see cref="MessagePackCode.Str32"/>.
	/// </remarks>
	public static bool TryWriteStringHeader(Span<byte> destination, uint byteCount, out int bytesWritten)
	{
		ref byte destinationRef = ref MemoryMarshal.GetReference(destination);
		switch (byteCount)
		{
			case <= MessagePackRange.MaxFixStringLength:
				bytesWritten = 1;
				if (destination.Length < bytesWritten)
				{
					return false;
				}

				destinationRef = (byte)(MessagePackCode.MinFixStr | byteCount);
				return true;
			case <= byte.MaxValue:
				bytesWritten = 2;
				if (destination.Length < bytesWritten)
				{
					return false;
				}

				destinationRef = MessagePackCode.Str8;
				Unsafe.Add(ref destinationRef, 1) = unchecked((byte)byteCount);
				return true;
			case <= ushort.MaxValue:
				bytesWritten = 3;
				if (destination.Length < bytesWritten)
				{
					return false;
				}

				destinationRef = MessagePackCode.Str16;
				WriteBigEndian(ref Unsafe.Add(ref destinationRef, 1), unchecked((ushort)byteCount));
				return true;
			default:
				bytesWritten = 5;
				if (destination.Length < bytesWritten)
				{
					return false;
				}

				destinationRef = MessagePackCode.Str32;
				WriteBigEndian(ref Unsafe.Add(ref destinationRef, 1), byteCount);
				return true;
		}
	}

	/// <summary>
	/// Writes an extension header to the specified buffer, if the buffer is large enough.
	/// </summary>
	/// <param name="destination">The buffer to write to. This should be at least 6 bytes in length to ensure success.</param>
	/// <param name="extensionHeader">The extension header to write.</param>
	/// <param name="bytesWritten">The number of bytes required to write the binary header, whether successful or not.</param>
	/// <returns>
	/// <see langword="true" /> if <paramref name="destination"/> was large enough and the binary header written; otherwise, <see langword="false" />.
	/// When <see langword="false"/>, the value of <paramref name="bytesWritten"/> indicates how many bytes are required to write the binary header successfully.
	/// </returns>
	/// <remarks>
	/// Writes the header of the extension data using the most compact form of
	/// <see cref="MessagePackCode.FixExt1"/>,
	/// <see cref="MessagePackCode.FixExt2"/>,
	/// <see cref="MessagePackCode.FixExt4"/>,
	/// <see cref="MessagePackCode.FixExt8"/>,
	/// <see cref="MessagePackCode.Ext8"/>,
	/// <see cref="MessagePackCode.Ext16"/>, or
	/// <see cref="MessagePackCode.Ext32"/>.
	/// </remarks>
	public static bool TryWriteExtensionHeader(Span<byte> destination, ExtensionHeader extensionHeader, out int bytesWritten)
	{
		ref byte destinationRef = ref MemoryMarshal.GetReference(destination);
		int dataLength = (int)extensionHeader.Length;
		byte typeCode = unchecked((byte)extensionHeader.TypeCode);
		switch (dataLength)
		{
			case 1 or 2 or 4 or 8 or 16:
				bytesWritten = 2;
				if (destination.Length < bytesWritten)
				{
					return false;
				}

				destinationRef = dataLength switch
				{
					1 => MessagePackCode.FixExt1,
					2 => MessagePackCode.FixExt2,
					4 => MessagePackCode.FixExt4,
					8 => MessagePackCode.FixExt8,
					16 => MessagePackCode.FixExt16,
					_ => throw ThrowUnreachable(),
				};
				Unsafe.Add(ref destinationRef, 1) = typeCode;
				return true;
			case <= byte.MaxValue:
				bytesWritten = 3;
				if (destination.Length < bytesWritten)
				{
					return false;
				}

				destinationRef = MessagePackCode.Ext8;
				Unsafe.Add(ref destinationRef, 1) = unchecked((byte)dataLength);
				Unsafe.Add(ref destinationRef, 2) = typeCode;
				return true;
			case <= ushort.MaxValue:
				bytesWritten = 4;
				if (destination.Length < bytesWritten)
				{
					return false;
				}

				destinationRef = MessagePackCode.Ext16;
				WriteBigEndian(ref Unsafe.Add(ref destinationRef, 1), unchecked((ushort)dataLength));
				Unsafe.Add(ref destinationRef, 3) = typeCode;
				return true;
			default:
				bytesWritten = 6;
				if (destination.Length < bytesWritten)
				{
					return false;
				}

				destinationRef = MessagePackCode.Ext32;
				WriteBigEndian(ref Unsafe.Add(ref destinationRef, 1), dataLength);
				Unsafe.Add(ref destinationRef, 5) = typeCode;
				return true;
		}
	}

	/// <summary>
	/// Writes a very small integer into just one byte of msgpack data.
	/// This method does *not* ensure that the value is within the range of a fixint.
	/// The caller must ensure that the value is less than or equal to <see cref="MessagePackCode.MaxFixInt"/>.
	/// </summary>
	/// <param name="destination">The buffer to write to. This should be at least 5 bytes in length to ensure success.</param>
	/// <param name="value">The single-precision floating-point value to write.</param>
	/// <param name="bytesWritten">The number of bytes required to write the value, whether successful or not.</param>
	/// <returns>
	/// <see langword="true" /> if <paramref name="destination"/> was large enough and the value written; otherwise, <see langword="false" />.
	/// When <see langword="false"/>, the value of <paramref name="bytesWritten"/> indicates how many bytes are required to write the value successfully.
	/// </returns>
	private static bool TryWriteFixIntUnsafe(Span<byte> destination, byte value, out int bytesWritten)
	{
		ref byte destinationRef = ref MemoryMarshal.GetReference(destination);
		bytesWritten = 1;
		if (destination.Length < bytesWritten)
		{
			return false;
		}

		destinationRef = unchecked(value);
		return true;
	}

	/// <summary>
	/// Writes a very small magnitude negative integer into just one byte of msgpack data.
	/// This method does *not* ensure that the value is within the range of a fixint.
	/// The caller must ensure that the value is between <see cref="MessagePackCode.MinNegativeFixInt"/>
	/// and <see cref="MessagePackCode.MaxNegativeFixInt"/>, inclusive.
	/// </summary>
	/// <param name="destination">The buffer to write to. This should be at least 5 bytes in length to ensure success.</param>
	/// <param name="value">The single-precision floating-point value to write.</param>
	/// <param name="bytesWritten">The number of bytes required to write the value, whether successful or not.</param>
	/// <returns>
	/// <see langword="true" /> if <paramref name="destination"/> was large enough and the value written; otherwise, <see langword="false" />.
	/// When <see langword="false"/>, the value of <paramref name="bytesWritten"/> indicates how many bytes are required to write the value successfully.
	/// </returns>
	private static bool TryWriteNegativeFixIntUnsafe(Span<byte> destination, sbyte value, out int bytesWritten)
	{
		ref byte destinationRef = ref MemoryMarshal.GetReference(destination);
		bytesWritten = 1;
		if (destination.Length < bytesWritten)
		{
			return false;
		}

		destinationRef = unchecked((byte)value);
		return true;
	}

	[DoesNotReturn]
	private static Exception ThrowUnreachable() => throw new UnreachableException();

	/// <summary>
	/// Writes an integer value in big-endian order to the specified buffer.
	/// </summary>
	/// <param name="destination">The buffer to write to.</param>
	/// <param name="value">The value to write.</param>
	private static void WriteBigEndian(ref byte destination, ushort value)
		=> Unsafe.WriteUnaligned(ref destination, BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value);

	/// <summary>
	/// Writes an integer value in big-endian order to the specified buffer.
	/// </summary>
	/// <param name="destination">The buffer to write to.</param>
	/// <param name="value">The value to write.</param>
	private static void WriteBigEndian(ref byte destination, uint value)
		=> Unsafe.WriteUnaligned(ref destination, BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value);

	/// <summary>
	/// Writes an integer value in big-endian order to the specified buffer.
	/// </summary>
	/// <param name="destination">The buffer to write to.</param>
	/// <param name="value">The value to write.</param>
	private static void WriteBigEndian(ref byte destination, ulong value)
		=> Unsafe.WriteUnaligned(ref destination, BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value);

	/// <summary>
	/// Writes an integer value in big-endian order to the specified buffer.
	/// </summary>
	/// <param name="destination">The buffer to write to.</param>
	/// <param name="value">The value to write.</param>
	private static void WriteBigEndian(ref byte destination, short value)
		=> Unsafe.WriteUnaligned(ref destination, BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value);

	/// <summary>
	/// Writes an integer value in big-endian order to the specified buffer.
	/// </summary>
	/// <param name="destination">The buffer to write to.</param>
	/// <param name="value">The value to write.</param>
	private static void WriteBigEndian(ref byte destination, int value)
		=> Unsafe.WriteUnaligned(ref destination, BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value);

	/// <summary>
	/// Writes an integer value in big-endian order to the specified buffer.
	/// </summary>
	/// <param name="destination">The buffer to write to.</param>
	/// <param name="value">The value to write.</param>
	private static void WriteBigEndian(ref byte destination, long value)
		=> Unsafe.WriteUnaligned(ref destination, BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value);
}
