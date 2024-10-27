// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack.Utilities;

/// <summary>
/// Helpers for converter generation.
/// </summary>
internal static class CodeGenHelpers
{
	/// <summary>
	/// Gets the messagepack encoding for a given string.
	/// </summary>
	/// <param name="value">The string to encode.</param>
	/// <param name="utf8Bytes">The UTF-8 encoded string.</param>
	/// <param name="msgpackEncoded">The msgpack-encoded string.</param>
	/// <remarks>
	/// Because msgpack encodes with UTF-8 bytes, the two output parameter share most of the memory.
	/// </remarks>
	internal static void GetEncodedStringBytes(string value, out ReadOnlyMemory<byte> utf8Bytes, out ReadOnlyMemory<byte> msgpackEncoded)
	{
		var byteCount = StringEncoding.UTF8.GetByteCount(value);
		if (byteCount <= MessagePackRange.MaxFixStringLength)
		{
			var bytes = new byte[byteCount + 1];
			bytes[0] = (byte)(MessagePackCode.MinFixStr | byteCount);
			StringEncoding.UTF8.GetBytes(value, bytes.AsSpan(1));
			utf8Bytes = bytes[1..];
			msgpackEncoded = bytes;
		}
		else if (byteCount <= byte.MaxValue)
		{
			var bytes = new byte[byteCount + 2];
			bytes[0] = MessagePackCode.Str8;
			bytes[1] = unchecked((byte)byteCount);
			StringEncoding.UTF8.GetBytes(value, bytes.AsSpan(2));
			utf8Bytes = bytes[2..];
			msgpackEncoded = bytes;
		}
		else if (byteCount <= ushort.MaxValue)
		{
			var bytes = new byte[byteCount + 3];
			bytes[0] = MessagePackCode.Str16;
			bytes[1] = unchecked((byte)(byteCount >> 8));
			bytes[2] = unchecked((byte)byteCount);
			StringEncoding.UTF8.GetBytes(value, bytes.AsSpan(3));
			utf8Bytes = bytes[3..];
			msgpackEncoded = bytes;
		}
		else
		{
			var bytes = new byte[byteCount + 5];
			bytes[0] = MessagePackCode.Str32;
			bytes[1] = unchecked((byte)(byteCount >> 24));
			bytes[2] = unchecked((byte)(byteCount >> 16));
			bytes[3] = unchecked((byte)(byteCount >> 8));
			bytes[4] = unchecked((byte)byteCount);
			StringEncoding.UTF8.GetBytes(value, bytes.AsSpan(5));
			utf8Bytes = bytes[5..];
			msgpackEncoded = bytes;
		}
	}

	/// <summary>
	/// Gets a single <see cref="ReadOnlySpan{T}"/> containing all bytes in a given <see cref="ReadOnlySequence{T}"/>.
	/// An array may be allocated if the bytes are not already contiguous in memory.
	/// </summary>
	/// <param name="sequence">The sequence to get a span for.</param>
	/// <returns>The span.</returns>
	internal static ReadOnlySpan<byte> GetSpanFromSequence(scoped in ReadOnlySequence<byte> sequence)
	{
		if (sequence.IsSingleSegment)
		{
			return sequence.First.Span;
		}

		return sequence.ToArray();
	}

	/// <summary>
	/// Reads a string as a contiguous span of UTF-8 encoded characters.
	/// An array may be allocated if the string is not already contiguous in memory.
	/// </summary>
	/// <param name="reader">The reader to use.</param>
	/// <returns>The span of UTF-8 encoded characters.</returns>
	internal static ReadOnlySpan<byte> ReadStringSpan(scoped ref MessagePackReader reader)
	{
		if (!reader.TryReadStringSpan(out ReadOnlySpan<byte> result))
		{
			ReadOnlySequence<byte>? sequence = reader.ReadStringSequence();
			if (sequence.HasValue)
			{
				if (sequence.Value.IsSingleSegment)
				{
					return sequence.Value.First.Span;
				}

				return sequence.Value.ToArray();
			}

			return default;
		}

		return result;
	}

	/// <summary>
	/// Creates a <see cref="byte"/> array for a given sequence, or <see langword="null" /> if the optional sequence is itself <see langword="null" />.
	/// </summary>
	/// <param name="sequence">The sequence.</param>
	/// <returns>The byte array or <see langword="null" /> .</returns>
	internal static byte[]? GetArrayFromNullableSequence(in ReadOnlySequence<byte>? sequence) => sequence?.ToArray();
}
