// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft;

namespace Nerdbank.PolySerializer.MessagePack;

internal class MsgPackFormatter : Formatter
{
	internal static readonly MsgPackFormatter Instance = new();

	private MsgPackFormatter()
	{
	}

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

	public override void Write(ref Writer writer, ReadOnlySpan<byte> value)
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

	protected internal override void GetEncodedStringBytes(string value, out ReadOnlyMemory<byte> utf8Bytes, out ReadOnlyMemory<byte> msgpackEncoded)
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
}
