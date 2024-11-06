// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// This file was originally derived from https://github.com/MessagePack-CSharp/MessagePack-CSharp/
// with Yoshifumi Kawai getting credit for the original implementation.
using Microsoft;

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
		int byteCount = StringEncoding.UTF8.GetByteCount(value);
		Memory<byte> bytes = new byte[byteCount + 5];
		Assumes.True(MessagePackPrimitives.TryWriteStringHeader(bytes.Span, (uint)byteCount, out int msgpackHeaderLength));
		StringEncoding.UTF8.GetBytes(value, bytes.Span[msgpackHeaderLength..]);
		utf8Bytes = bytes.Slice(msgpackHeaderLength, byteCount);
		msgpackEncoded = bytes.Slice(0, byteCount + msgpackHeaderLength);
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
}
