// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text;
using Microsoft;

namespace Nerdbank.PolySerializer.MessagePack;

public class MsgPackFormatter : Formatter
{
	public static readonly MsgPackFormatter Default = new();

	private MsgPackFormatter()
	{
	}

	public override string FormatName => "msgpack";

	public override Encoding Encoding => StringEncoding.UTF8;

	/// <summary>
	/// Gets or sets a value indicating whether to write in <see href="https://github.com/msgpack/msgpack/blob/master/spec-old.md">old spec</see> compatibility mode.
	/// </summary>
	public bool OldSpec { get; set; }

	public override bool ArrayLengthRequiredInHeader => true;

	public override void WriteArrayStart(ref Writer writer, int length)
	{
		Span<byte> span = writer.Buffer.GetSpan(5);
		Assumes.True(MessagePackPrimitives.TryWriteArrayHeader(span, checked((uint)length), out int written));
		writer.Buffer.Advance(written);
	}

	public override void WriteArrayElementSeparator(ref Writer writer)
	{
		// msgpack doesn't have one.
	}

	public override void WriteArrayEnd(ref Writer writer)
	{
		// msgpack has no array terminator.
	}

	public override void WriteMapStart(ref Writer writer, int length)
	{
		Span<byte> span = writer.Buffer.GetSpan(5);
		Assumes.True(MessagePackPrimitives.TryWriteMapHeader(span, checked((uint)length), out int written));
		writer.Buffer.Advance(written);
	}

	public override void WriteMapKeyValueSeparator(ref Writer writer)
	{
		// msgpack doesn't have one.
	}

	public override void WriteMapValueTrailer(ref Writer writer)
	{
		// msgpack doesn't have one.
	}

	public override void WriteMapEnd(ref Writer writer)
	{
		// msgpack has no map terminator.
	}

	public override void WriteNull(ref Writer writer)
	{
		Span<byte> span = writer.Buffer.GetSpan(1);
		Assumes.True(MessagePackPrimitives.TryWriteNil(span, out int written));
		writer.Buffer.Advance(written);
	}

	public override void Write(ref Writer writer, bool value)
	{
		Span<byte> span = writer.Buffer.GetSpan(1);
		Assumes.True(MessagePackPrimitives.TryWrite(span, value, out int written));
		writer.Buffer.Advance(written);
	}

	public override void Write(ref Writer writer, char value)
	{
		Span<byte> span = writer.Buffer.GetSpan(3);
		Assumes.True(MessagePackPrimitives.TryWrite(span, value, out int written));
		writer.Buffer.Advance(written);
	}

	public override void Write(ref Writer writer, byte value)
	{
		Span<byte> span = writer.Buffer.GetSpan(2);
		Assumes.True(MessagePackPrimitives.TryWrite(span, value, out int written));
		writer.Buffer.Advance(written);
	}

	public override void Write(ref Writer writer, sbyte value)
	{
		Span<byte> span = writer.Buffer.GetSpan(2);
		Assumes.True(MessagePackPrimitives.TryWrite(span, value, out int written));
		writer.Buffer.Advance(written);
	}

	public override void Write(ref Writer writer, ushort value)
	{
		Span<byte> span = writer.Buffer.GetSpan(3);
		Assumes.True(MessagePackPrimitives.TryWrite(span, value, out int written));
		writer.Buffer.Advance(written);
	}

	public override void Write(ref Writer writer, short value)
	{
		Span<byte> span = writer.Buffer.GetSpan(3);
		Assumes.True(MessagePackPrimitives.TryWrite(span, value, out int written));
		writer.Buffer.Advance(written);
	}

	public override void Write(ref Writer writer, uint value)
	{
		Span<byte> span = writer.Buffer.GetSpan(5);
		Assumes.True(MessagePackPrimitives.TryWrite(span, value, out int written));
		writer.Buffer.Advance(written);
	}

	public override void Write(ref Writer writer, int value)
	{
		Span<byte> span = writer.Buffer.GetSpan(5);
		Assumes.True(MessagePackPrimitives.TryWrite(span, value, out int written));
		writer.Buffer.Advance(written);
	}

	public override void Write(ref Writer writer, ulong value)
	{
		Span<byte> span = writer.Buffer.GetSpan(9);
		Assumes.True(MessagePackPrimitives.TryWrite(span, value, out int written));
		writer.Buffer.Advance(written);
	}

	public override void Write(ref Writer writer, long value)
	{
		Span<byte> span = writer.Buffer.GetSpan(9);
		Assumes.True(MessagePackPrimitives.TryWrite(span, value, out int written));
		writer.Buffer.Advance(written);
	}

	public override void Write(ref Writer writer, float value)
	{
		Span<byte> span = writer.Buffer.GetSpan(5);
		Assumes.True(MessagePackPrimitives.TryWrite(span, value, out int written));
		writer.Buffer.Advance(written);
	}

	public override void Write(ref Writer writer, double value)
	{
		Span<byte> span = writer.Buffer.GetSpan(9);
		Assumes.True(MessagePackPrimitives.TryWrite(span, value, out int written));
		writer.Buffer.Advance(written);
	}

	public override void Write(ref Writer writer, DateTime value)
	{
		if (this.OldSpec)
		{
			throw new NotSupportedException($"The MsgPack spec does not define a format for {nameof(DateTime)} in {nameof(this.OldSpec)} mode. Turn off {nameof(this.OldSpec)} mode or use NativeDateTimeFormatter.");
		}
		else
		{
			Span<byte> span = writer.Buffer.GetSpan(15);
			Assumes.True(MessagePackPrimitives.TryWrite(span, value, out int written));
			writer.Buffer.Advance(written);
		}
	}

	public override unsafe void Write(ref Writer writer, string? value)
	{
		if (value == null)
		{
			this.WriteNull(ref writer);
			return;
		}

		ref byte buffer = ref this.WriteString_PrepareSpan(ref writer, value.Length, out int bufferSize, out int useOffset);
		fixed (char* pValue = value)
		{
			fixed (byte* pBuffer = &buffer)
			{
				int byteCount = StringEncoding.UTF8.GetBytes(pValue, value.Length, pBuffer + useOffset, bufferSize);
				this.WriteString_PostEncoding(ref writer, pBuffer, useOffset, byteCount);
			}
		}
	}

	public override void Write(ref Writer writer, scoped ReadOnlySpan<char> value)
	{
		throw new NotImplementedException();
	}

	public override void WriteEncodedString(ref Writer writer, scoped ReadOnlySpan<byte> value)
	{
		this.WriteStringHeader(ref writer, value.Length);
		writer.Buffer.Write(value);
	}

	public override void Write(ref Writer writer, scoped ReadOnlySpan<byte> value)
	{
		int length = value.Length;
		this.WriteBinHeader(ref writer, length);
		Span<byte> span = writer.Buffer.GetSpan(length);
		value.CopyTo(span);
		writer.Buffer.Advance(length);
	}

	public override void Write(ref Writer writer, ReadOnlySequence<byte> value)
	{
		int length = (int)value.Length;
		this.WriteBinHeader(ref writer, length);
		Span<byte> span = writer.Buffer.GetSpan(length);
		value.CopyTo(span);
		writer.Buffer.Advance(length);
	}

	public override bool TryWriteBinHeader(ref Writer writer, int length)
	{
		this.WriteBinHeader(ref writer, length);
		return true;
	}

	public override void GetEncodedStringBytes(string value, out ReadOnlyMemory<byte> utf8Bytes, out ReadOnlyMemory<byte> msgpackEncoded)
		=> StringEncoding.GetEncodedStringBytes(value, out utf8Bytes, out msgpackEncoded);

	public void WriteBinHeader(ref Writer writer, int length)
	{
		if (this.OldSpec)
		{
			this.WriteStringHeader(ref writer, length);
			return;
		}

		// When we write the header, we'll ask for all the space we need for the payload as well
		// as that may help ensure we only allocate a buffer once.
		Span<byte> span = writer.Buffer.GetSpan(length + 5);
		Assumes.True(MessagePackPrimitives.TryWriteBinHeader(span, (uint)length, out int written));
		writer.Buffer.Advance(written);
	}

	/// <summary>
	/// Writes out the header that may precede a UTF-8 encoded string, prefixed with the length using one of these message codes:
	/// <see cref="MessagePackCode.MinFixStr"/>,
	/// <see cref="MessagePackCode.Str8"/>,
	/// <see cref="MessagePackCode.Str16"/>, or
	/// <see cref="MessagePackCode.Str32"/>.
	/// </summary>
	/// <param name="byteCount">The number of bytes in the string that will follow this header.</param>
	/// <remarks>
	/// The caller should use <see cref="WriteRaw(in ReadOnlySequence{byte})"/> or <see cref="WriteRaw(ReadOnlySpan{byte})"/>
	/// after calling this method to actually write the content.
	/// Alternatively a single call to <see cref="WriteString(ReadOnlySpan{byte})"/> or <see cref="WriteString(in ReadOnlySequence{byte})"/> will take care of the header and content in one call.
	/// </remarks>
	public void WriteStringHeader(ref Writer writer, int byteCount)
	{
		// When we write the header, we'll ask for all the space we need for the payload as well
		// as that may help ensure we only allocate a buffer once.
		Span<byte> span = writer.Buffer.GetSpan(byteCount + 5);
		Assumes.True(MessagePackPrimitives.TryWriteStringHeader(span, (uint)byteCount, out int written));
		writer.Buffer.Advance(written);
	}

	public void Write(ref Writer writer, ExtensionHeader value)
	{
		// When we write the header, we'll ask for all the space we need for the payload as well
		// as that may help ensure we only allocate a buffer once.
		Span<byte> span = writer.Buffer.GetSpan((int)(value.Length + 6));
		Assumes.True(MessagePackPrimitives.TryWriteExtensionHeader(span, value, out int written));
		writer.Buffer.Advance(written);
	}

	/// <summary>
	/// Estimates the length of the header required for a given string.
	/// </summary>
	/// <param name="characterLength">The length of the string to be written, in characters.</param>
	/// <param name="bufferSize">Receives the guaranteed length of the returned buffer.</param>
	/// <param name="encodedBytesOffset">Receives the offset within the returned buffer to write the encoded string to.</param>
	/// <returns>
	/// A reference to the first byte in the buffer.
	/// </returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private ref byte WriteString_PrepareSpan(ref Writer writer, int characterLength, out int bufferSize, out int encodedBytesOffset)
	{
		// MaxByteCount -> WritePrefix -> GetBytes has some overheads of `MaxByteCount`
		// solves heuristic length check

		// ensure buffer by MaxByteCount(faster than GetByteCount)
		bufferSize = StringEncoding.UTF8.GetMaxByteCount(characterLength) + 5;
		ref byte buffer = ref writer.Buffer.GetPointer(bufferSize);

		int useOffset;
		if (characterLength <= MessagePackRange.MaxFixStringLength)
		{
			useOffset = 1;
		}
		else if (characterLength <= byte.MaxValue && !this.OldSpec)
		{
			useOffset = 2;
		}
		else if (characterLength <= ushort.MaxValue)
		{
			useOffset = 3;
		}
		else
		{
			useOffset = 5;
		}

		encodedBytesOffset = useOffset;
		return ref buffer;
	}

	/// <summary>
	/// Finalizes an encoding of a string.
	/// </summary>
	/// <param name="pBuffer">A pointer obtained from a prior call to <see cref="WriteString_PrepareSpan"/>.</param>
	/// <param name="estimatedOffset">The offset obtained from a prior call to <see cref="WriteString_PrepareSpan"/>.</param>
	/// <param name="byteCount">The number of bytes used to actually encode the string.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe void WriteString_PostEncoding(ref Writer writer, byte* pBuffer, int estimatedOffset, int byteCount)
	{
		// move body and write prefix
		if (byteCount <= MessagePackRange.MaxFixStringLength)
		{
			if (estimatedOffset != 1)
			{
				Buffer.MemoryCopy(pBuffer + estimatedOffset, pBuffer + 1, byteCount, byteCount);
			}

			pBuffer[0] = (byte)(MessagePackCode.MinFixStr | byteCount);
			writer.Buffer.Advance(byteCount + 1);
		}
		else if (byteCount <= byte.MaxValue && !this.OldSpec)
		{
			if (estimatedOffset != 2)
			{
				Buffer.MemoryCopy(pBuffer + estimatedOffset, pBuffer + 2, byteCount, byteCount);
			}

			pBuffer[0] = MessagePackCode.Str8;
			pBuffer[1] = unchecked((byte)byteCount);
			writer.Buffer.Advance(byteCount + 2);
		}
		else if (byteCount <= ushort.MaxValue)
		{
			if (estimatedOffset != 3)
			{
				Buffer.MemoryCopy(pBuffer + estimatedOffset, pBuffer + 3, byteCount, byteCount);
			}

			pBuffer[0] = MessagePackCode.Str16;
			WriteBigEndian((ushort)byteCount, pBuffer + 1);
			writer.Buffer.Advance(byteCount + 3);
		}
		else
		{
			if (estimatedOffset != 5)
			{
				Buffer.MemoryCopy(pBuffer + estimatedOffset, pBuffer + 5, byteCount, byteCount);
			}

			pBuffer[0] = MessagePackCode.Str32;
			WriteBigEndian((uint)byteCount, pBuffer + 1);
			writer.Buffer.Advance(byteCount + 5);
		}
	}

	private static unsafe void WriteBigEndian(ushort value, byte* span)
	{
		// TODO: test perf of this alternative.
		////BinaryPrimitives.WriteUInt16BigEndian(new Span<byte>(span, 2), value);
		unchecked
		{
			span[0] = (byte)(value >> 8);
			span[1] = (byte)value;
		}
	}

	private static unsafe void WriteBigEndian(uint value, byte* span)
	{
		// TODO: test perf of this alternative.
		////BinaryPrimitives.WriteUInt32BigEndian(new Span<byte>(span, 4), value);
		unchecked
		{
			span[0] = (byte)(value >> 24);
			span[1] = (byte)(value >> 16);
			span[2] = (byte)(value >> 8);
			span[3] = (byte)value;
		}
	}

	public override int GetEncodedLength(long value) => MessagePackWriter.GetEncodedLength(value);

	public override int GetEncodedLength(ulong value) => MessagePackWriter.GetEncodedLength(value);
}
