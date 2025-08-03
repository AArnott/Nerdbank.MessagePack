// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Originally taken from https://source.dot.net/#Microsoft.AspNetCore.SignalR.Protocols.MessagePack/src/SignalR/common/Shared/BinaryMessageFormatter.cs,eb01b8ec76038f4d
// and from https://source.dot.net/#Microsoft.AspNetCore.SignalR.Protocols.MessagePack/src/SignalR/common/Shared/BinaryMessageParser.cs,f835db938497c210
namespace Nerdbank.MessagePack.SignalR;

/// <summary>
/// Utility methods for reading and writing binary messages in the SignalR protocol.
/// </summary>
internal static class BinaryMessageFormatter
{
	/// <summary>
	/// The maximum number of bytes that can be used to encode a VarInt length prefix.
	/// This supports payloads up to 2GB (0x7FFFFFFF) which requires at most 5 bytes when encoded as VarInt.
	/// </summary>
	private const int MaxLengthPrefixSize = 5;

	/// <summary>
	/// Writes a length prefix as a VarInt to the specified buffer writer.
	/// </summary>
	/// <param name="length">The length value to encode as a VarInt prefix.</param>
	/// <param name="output">The buffer writer to write the encoded length prefix to.</param>
	internal static void WriteLengthPrefix(long length, IBufferWriter<byte> output)
	{
		Span<byte> lenBuffer = stackalloc byte[5];

		int lenNumBytes = WriteLengthPrefix(length, lenBuffer);

		output.Write(lenBuffer.Slice(0, lenNumBytes));
	}

	/// <summary>
	/// Writes a length prefix as a VarInt to the specified span and returns the number of bytes written.
	/// </summary>
	/// <param name="length">The length value to encode as a VarInt prefix.</param>
	/// <param name="output">The span to write the encoded length prefix to. Must be at least 5 bytes in length.</param>
	/// <returns>The number of bytes written to the output span.</returns>
	internal static int WriteLengthPrefix(long length, Span<byte> output)
	{
		// This code writes length prefix of the message as a VarInt. Read the comment in
		// the BinaryMessageParser.TryParseMessage for details.
		int lenNumBytes = 0;
		do
		{
			ref byte current = ref output[lenNumBytes];
			current = (byte)(length & 0x7f);
			length >>= 7;
			if (length > 0)
			{
				current |= 0x80;
			}

			lenNumBytes++;
		}
		while (length > 0);

		return lenNumBytes;
	}

	/// <summary>
	/// Calculates the number of bytes required to encode the specified length as a VarInt.
	/// </summary>
	/// <param name="length">The length value to calculate the encoding size for.</param>
	/// <returns>The number of bytes required to encode the length as a VarInt (1-5 bytes).</returns>
	internal static int LengthPrefixLength(long length)
	{
		int lenNumBytes = 0;
		do
		{
			length >>= 7;
			lenNumBytes++;
		}
		while (length > 0);

		return lenNumBytes;
	}

	/// <summary>
	/// Attempts to parse a complete message from the buffer, extracting the payload and advancing the buffer position.
	/// </summary>
	/// <param name="buffer">The buffer containing the message data. This will be advanced past the parsed message on success.</param>
	/// <param name="payload">When this method returns true, contains the extracted message payload.</param>
	/// <returns>
	/// <see langword="true"/> if a complete message was successfully parsed;
	/// <see langword="false"/> if the buffer does not contain enough data for a complete message.
	/// </returns>
	/// <exception cref="FormatException">Thrown when the message indicates a payload larger than 2GB.</exception>
	internal static bool TryParseMessage(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> payload)
	{
		if (buffer.IsEmpty)
		{
			payload = default;
			return false;
		}

		// The payload starts with a length prefix encoded as a VarInt. VarInts use the most significant bit
		// as a marker whether the byte is the last byte of the VarInt or if it spans to the next byte. Bytes
		// appear in the reverse order - i.e. the first byte contains the least significant bits of the value
		// Examples:
		// VarInt: 0x35 - %00110101 - the most significant bit is 0 so the value is %x0110101 i.e. 0x35 (53)
		// VarInt: 0x80 0x25 - %10000000 %00101001 - the most significant bit of the first byte is 1 so the
		// remaining bits (%x0000000) are the lowest bits of the value. The most significant bit of the second
		// byte is 0 meaning this is last byte of the VarInt. The actual value bits (%x0101001) need to be
		// prepended to the bits we already read so the values is %01010010000000 i.e. 0x1480 (5248)
		// We support payloads up to 2GB so the biggest number we support is 7fffffff which when encoded as
		// VarInt is 0xFF 0xFF 0xFF 0xFF 0x07 - hence the maximum length prefix is 5 bytes.
		uint length = 0U;
		int numBytes = 0;

		ReadOnlySequence<byte> lengthPrefixBuffer = buffer.Slice(0, Math.Min(MaxLengthPrefixSize, buffer.Length));
		ReadOnlySpan<byte> span = GetSpan(lengthPrefixBuffer);

		byte byteRead;
		do
		{
			byteRead = span[numBytes];
			length = length | (((uint)(byteRead & 0x7f)) << (numBytes * 7));
			numBytes++;
		}
		while (numBytes < lengthPrefixBuffer.Length && ((byteRead & 0x80) != 0));

		// size bytes are missing
		if ((byteRead & 0x80) != 0 && (numBytes < MaxLengthPrefixSize))
		{
			payload = default;
			return false;
		}

		if ((byteRead & 0x80) != 0 || (numBytes == MaxLengthPrefixSize && byteRead > 7))
		{
			throw new FormatException("Messages over 2GB in size are not supported.");
		}

		// We don't have enough data
		if (buffer.Length < length + numBytes)
		{
			payload = default;
			return false;
		}

		// Get the payload
		payload = buffer.Slice(numBytes, (int)length);

		// Skip the payload
		buffer = buffer.Slice(numBytes + (int)length);
		return true;
	}

	/// <summary>
	/// Gets a span from the specified sequence, handling both single-segment and multi-segment sequences.
	/// </summary>
	/// <param name="lengthPrefixBuffer">The sequence to get a span from.</param>
	/// <returns>A span containing the data from the sequence.</returns>
	private static ReadOnlySpan<byte> GetSpan(in ReadOnlySequence<byte> lengthPrefixBuffer)
	{
		if (lengthPrefixBuffer.IsSingleSegment)
		{
			return lengthPrefixBuffer.First.Span;
		}

		// Should be rare
		return lengthPrefixBuffer.ToArray();
	}
}
