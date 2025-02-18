// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text;
using Microsoft;

namespace ShapeShift.MessagePack;

/// <summary>
/// A <see cref="Formatter"/> that can encode <see href="https://msgpack.org/">messagepack</see> data.
/// </summary>
public record MsgPackFormatter : Formatter
{
	/// <summary>
	/// The default configuration of <see cref="MsgPackFormatter"/>.
	/// </summary>
	public static readonly MsgPackFormatter Default = new();

	private MsgPackFormatter()
	{
	}

	/// <inheritdoc/>
	public override string FormatName => "msgpack";

	/// <inheritdoc/>
	public override Encoding Encoding => StringEncoding.UTF8;

	/// <summary>
	/// Gets a value indicating whether to write in <see href="https://github.com/msgpack/msgpack/blob/master/spec-old.md">old spec</see> compatibility mode.
	/// </summary>
	public bool OldSpec { get; init; }

	/// <inheritdoc/>
	public override bool VectorsMustHaveLengthPrefix => true;

	/// <summary>
	/// Writes a <see cref="MessagePackCode.Nil"/> value.
	/// </summary>
	/// <param name="writer">The writer to receive the formatted bytes.</param>
	public override void WriteNull(ref BufferWriter writer)
	{
		Span<byte> span = writer.GetSpan(1);
		Assumes.True(MessagePackPrimitives.TryWriteNil(span, out int written));
		writer.Advance(written);
	}

	/// <summary>
	/// Write the length of the next array to be written in the most compact form of
	/// <see cref="MessagePackCode.MinFixArray"/>,
	/// <see cref="MessagePackCode.Array16"/>, or
	/// <see cref="MessagePackCode.Array32"/>.
	/// </summary>
	/// <param name="writer"><inheritdoc cref="WriteNull(ref BufferWriter)" path="/param[@name='writer']"/></param>
	/// <param name="length">The number of elements that will be written in the array.</param>
	public override void WriteStartVector(ref BufferWriter writer, int length)
	{
		Span<byte> span = writer.GetSpan(5);
		Assumes.True(MessagePackPrimitives.TryWriteArrayHeader(span, checked((uint)length), out int written));
		writer.Advance(written);
	}

	/// <inheritdoc />
	public override void WriteVectorElementSeparator(ref BufferWriter writer)
	{
		// msgpack doesn't have one.
	}

	/// <inheritdoc />
	public override void WriteEndVector(ref BufferWriter writer)
	{
		// msgpack has no array terminator.
	}

	/// <summary>
	/// Write the length of the next map to be written in the most compact form of
	/// <see cref="MessagePackCode.MinFixMap"/>,
	/// <see cref="MessagePackCode.Map16"/>, or
	/// <see cref="MessagePackCode.Map32"/>.
	/// </summary>
	/// <param name="writer"><inheritdoc cref="WriteNull(ref BufferWriter)" path="/param[@name='writer']"/></param>
	/// <param name="length">The number of key=value pairs that will be written in the map.</param>
	public override void WriteStartMap(ref BufferWriter writer, int length)
	{
		Span<byte> span = writer.GetSpan(5);
		Assumes.True(MessagePackPrimitives.TryWriteMapHeader(span, checked((uint)length), out int written));
		writer.Advance(written);
	}

	/// <inheritdoc />
	public override void WriteMapPairSeparator(ref BufferWriter writer)
	{
		// msgpack doesn't have one.
	}

	/// <inheritdoc />
	public override void WriteMapKeyValueSeparator(ref BufferWriter writer)
	{
		// msgpack doesn't have one.
	}

	/// <inheritdoc />
	public override void WriteEndMap(ref BufferWriter writer)
	{
		// msgpack has no map terminator.
	}

	/// <summary>
	/// Writes a <see cref="bool"/> value using either <see cref="MessagePackCode.True"/> or <see cref="MessagePackCode.False"/>.
	/// </summary>
	/// <param name="writer"><inheritdoc cref="WriteNull(ref BufferWriter)" path="/param[@name='writer']"/></param>
	/// <param name="value">The value.</param>
	public override void Write(ref BufferWriter writer, bool value)
	{
		Span<byte> span = writer.GetSpan(1);
		Assumes.True(MessagePackPrimitives.TryWrite(span, value, out int written));
		writer.Advance(written);
	}

	/// <summary>
	/// Writes a <see cref="char"/> value using a 1-byte code when possible, otherwise as <see cref="MessagePackCode.UInt8"/> or <see cref="MessagePackCode.UInt16"/>.
	/// </summary>
	/// <param name="writer"><inheritdoc cref="WriteNull(ref BufferWriter)" path="/param[@name='writer']"/></param>
	/// <param name="value">The value.</param>
	public override void Write(ref BufferWriter writer, char value)
	{
		Span<byte> span = writer.GetSpan(3);
		Assumes.True(MessagePackPrimitives.TryWrite(span, value, out int written));
		writer.Advance(written);
	}

	/// <summary>
	/// Writes a <see cref="byte"/> value using a 1-byte code when possible, otherwise as <see cref="MessagePackCode.UInt8"/>.
	/// </summary>
	/// <param name="writer"><inheritdoc cref="WriteNull(ref BufferWriter)" path="/param[@name='writer']"/></param>
	/// <param name="value">The value.</param>
	public override void Write(ref BufferWriter writer, byte value)
	{
		Span<byte> span = writer.GetSpan(2);
		Assumes.True(MessagePackPrimitives.TryWrite(span, value, out int written));
		writer.Advance(written);
	}

	/// <summary>
	/// Writes a <see cref="byte"/> value using <see cref="MessagePackCode.UInt8"/>.
	/// </summary>
	/// <param name="writer"><inheritdoc cref="WriteNull(ref BufferWriter)" path="/param[@name='writer']"/></param>
	/// <param name="value">The value.</param>
	public void WriteUInt8(ref BufferWriter writer, byte value)
	{
		Span<byte> span = writer.GetSpan(2);
		Assumes.True(MessagePackPrimitives.TryWriteUInt8(span, value, out int written));
		writer.Advance(written);
	}

	/// <inheritdoc cref="WriteUInt8(ref BufferWriter, byte)"/>
	public void WriteByte(ref BufferWriter writer, byte value) => this.WriteUInt8(ref writer, value);

	/// <summary>
	/// Writes an 8-bit value using a 1-byte code when possible, otherwise as <see cref="MessagePackCode.Int8"/>.
	/// </summary>
	/// <param name="writer"><inheritdoc cref="WriteNull(ref BufferWriter)" path="/param[@name='writer']"/></param>
	/// <param name="value">The value.</param>
	public override void Write(ref BufferWriter writer, sbyte value)
	{
		Span<byte> span = writer.GetSpan(2);
		Assumes.True(MessagePackPrimitives.TryWrite(span, value, out int written));
		writer.Advance(written);
	}

	/// <summary>
	/// Writes an 8-bit value using <see cref="MessagePackCode.Int8"/>.
	/// </summary>
	/// <param name="writer"><inheritdoc cref="WriteNull(ref BufferWriter)" path="/param[@name='writer']"/></param>
	/// <param name="value">The value.</param>
	public void WriteInt8(ref BufferWriter writer, sbyte value)
	{
		Span<byte> span = writer.GetSpan(2);
		Assumes.True(MessagePackPrimitives.TryWriteInt8(span, value, out int written));
		writer.Advance(written);
	}

	/// <inheritdoc cref="WriteInt8(ref BufferWriter, sbyte)"/>
	public void WriteSByte(ref BufferWriter writer, sbyte value) => this.WriteInt8(ref writer, value);

	/// <summary>
	/// Writes a <see cref="ushort"/> value using a 1-byte code when possible, otherwise as <see cref="MessagePackCode.UInt8"/> or <see cref="MessagePackCode.UInt16"/>.
	/// </summary>
	/// <param name="writer"><inheritdoc cref="WriteNull(ref BufferWriter)" path="/param[@name='writer']"/></param>
	/// <param name="value">The value.</param>
	public override void Write(ref BufferWriter writer, ushort value)
	{
		Span<byte> span = writer.GetSpan(3);
		Assumes.True(MessagePackPrimitives.TryWrite(span, value, out int written));
		writer.Advance(written);
	}

	/// <summary>
	/// Writes a <see cref="ushort"/> value using <see cref="MessagePackCode.UInt16"/>.
	/// </summary>
	/// <param name="writer"><inheritdoc cref="WriteNull(ref BufferWriter)" path="/param[@name='writer']"/></param>
	/// <param name="value">The value.</param>
	public void WriteUInt16(ref BufferWriter writer, ushort value)
	{
		Span<byte> span = writer.GetSpan(3);
		Assumes.True(MessagePackPrimitives.TryWriteUInt16(span, value, out int written));
		writer.Advance(written);
	}

	/// <summary>
	/// Writes a <see cref="short"/> using a built-in 1-byte code when within specific MessagePack-supported ranges,
	/// or the most compact of
	/// <see cref="MessagePackCode.UInt8"/>,
	/// <see cref="MessagePackCode.UInt16"/>,
	/// <see cref="MessagePackCode.Int8"/>, or
	/// <see cref="MessagePackCode.Int16"/>.
	/// </summary>
	/// <param name="writer"><inheritdoc cref="WriteNull(ref BufferWriter)" path="/param[@name='writer']"/></param>
	/// <param name="value">The value to write.</param>
	public override void Write(ref BufferWriter writer, short value)
	{
		Span<byte> span = writer.GetSpan(3);
		Assumes.True(MessagePackPrimitives.TryWrite(span, value, out int written));
		writer.Advance(written);
	}

	/// <summary>
	/// Writes a <see cref="short"/> using <see cref="MessagePackCode.Int16"/>.
	/// </summary>
	/// <param name="writer"><inheritdoc cref="WriteNull(ref BufferWriter)" path="/param[@name='writer']"/></param>
	/// <param name="value">The value to write.</param>
	public void WriteInt16(ref BufferWriter writer, short value)
	{
		Span<byte> span = writer.GetSpan(3);
		Assumes.True(MessagePackPrimitives.TryWriteInt16(span, value, out int written));
		writer.Advance(written);
	}

	/// <summary>
	/// Writes an <see cref="uint"/> using <see cref="MessagePackCode.UInt32"/>.
	/// </summary>
	/// <param name="writer"><inheritdoc cref="WriteNull(ref BufferWriter)" path="/param[@name='writer']"/></param>
	/// <param name="value">The value to write.</param>
	public override void Write(ref BufferWriter writer, uint value)
	{
		Span<byte> span = writer.GetSpan(5);
		Assumes.True(MessagePackPrimitives.TryWrite(span, value, out int written));
		writer.Advance(written);
	}

	/// <summary>
	/// Writes an <see cref="uint"/> using <see cref="MessagePackCode.UInt32"/>.
	/// </summary>
	/// <param name="writer"><inheritdoc cref="WriteNull(ref BufferWriter)" path="/param[@name='writer']"/></param>
	/// <param name="value">The value to write.</param>
	public void WriteUInt32(ref BufferWriter writer, uint value)
	{
		Span<byte> span = writer.GetSpan(5);
		Assumes.True(MessagePackPrimitives.TryWriteUInt32(span, value, out int written));
		writer.Advance(written);
	}

	/// <summary>
	/// Writes an <see cref="int"/> using a built-in 1-byte code when within specific MessagePack-supported ranges,
	/// or the most compact of
	/// <see cref="MessagePackCode.UInt8"/>,
	/// <see cref="MessagePackCode.UInt16"/>,
	/// <see cref="MessagePackCode.UInt32"/>,
	/// <see cref="MessagePackCode.Int8"/>,
	/// <see cref="MessagePackCode.Int16"/>,
	/// <see cref="MessagePackCode.Int32"/>.
	/// </summary>
	/// <param name="writer"><inheritdoc cref="WriteNull(ref BufferWriter)" path="/param[@name='writer']"/></param>
	/// <param name="value">The value to write.</param>
	public override void Write(ref BufferWriter writer, int value)
	{
		Span<byte> span = writer.GetSpan(5);
		Assumes.True(MessagePackPrimitives.TryWrite(span, value, out int written));
		writer.Advance(written);
	}

	/// <summary>
	/// Writes an <see cref="int"/> using <see cref="MessagePackCode.Int32"/>.
	/// </summary>
	/// <param name="writer"><inheritdoc cref="WriteNull(ref BufferWriter)" path="/param[@name='writer']"/></param>
	/// <param name="value">The value to write.</param>
	public void WriteInt32(ref BufferWriter writer, int value)
	{
		Span<byte> span = writer.GetSpan(5);
		Assumes.True(MessagePackPrimitives.TryWriteInt32(span, value, out int written));
		writer.Advance(written);
	}

	/// <summary>
	/// Writes an <see cref="ulong"/> using <see cref="MessagePackCode.Int32"/>.
	/// </summary>
	/// <param name="writer"><inheritdoc cref="WriteNull(ref BufferWriter)" path="/param[@name='writer']"/></param>
	/// <param name="value">The value to write.</param>
	public override void Write(ref BufferWriter writer, ulong value)
	{
		Span<byte> span = writer.GetSpan(9);
		Assumes.True(MessagePackPrimitives.TryWrite(span, value, out int written));
		writer.Advance(written);
	}

	/// <summary>
	/// Writes an <see cref="ulong"/> using <see cref="MessagePackCode.Int32"/>.
	/// </summary>
	/// <param name="writer"><inheritdoc cref="WriteNull(ref BufferWriter)" path="/param[@name='writer']"/></param>
	/// <param name="value">The value to write.</param>
	public void WriteUInt64(ref BufferWriter writer, ulong value)
	{
		Span<byte> span = writer.GetSpan(9);
		Assumes.True(MessagePackPrimitives.TryWriteUInt64(span, value, out int written));
		writer.Advance(written);
	}

	/// <summary>
	/// Writes an <see cref="long"/> using a built-in 1-byte code when within specific MessagePack-supported ranges,
	/// or the most compact of
	/// <see cref="MessagePackCode.UInt8"/>,
	/// <see cref="MessagePackCode.UInt16"/>,
	/// <see cref="MessagePackCode.UInt32"/>,
	/// <see cref="MessagePackCode.UInt64"/>,
	/// <see cref="MessagePackCode.Int8"/>,
	/// <see cref="MessagePackCode.Int16"/>,
	/// <see cref="MessagePackCode.Int32"/>,
	/// <see cref="MessagePackCode.Int64"/>.
	/// </summary>
	/// <param name="writer"><inheritdoc cref="WriteNull(ref BufferWriter)" path="/param[@name='writer']"/></param>
	/// <param name="value">The value to write.</param>
	public override void Write(ref BufferWriter writer, long value)
	{
		Span<byte> span = writer.GetSpan(9);
		Assumes.True(MessagePackPrimitives.TryWrite(span, value, out int written));
		writer.Advance(written);
	}

	/// <summary>
	/// Writes a <see cref="long"/> using <see cref="MessagePackCode.Int64"/>.
	/// </summary>
	/// <param name="writer"><inheritdoc cref="WriteNull(ref BufferWriter)" path="/param[@name='writer']"/></param>
	/// <param name="value">The value to write.</param>
	public void WriteInt64(ref BufferWriter writer, long value)
	{
		Span<byte> span = writer.GetSpan(9);
		Assumes.True(MessagePackPrimitives.TryWriteInt64(span, value, out int written));
		writer.Advance(written);
	}

	/// <summary>
	/// Writes a <see cref="MessagePackCode.Float32"/> value.
	/// </summary>
	/// <param name="writer"><inheritdoc cref="WriteNull(ref BufferWriter)" path="/param[@name='writer']"/></param>
	/// <param name="value">The value.</param>
	public override void Write(ref BufferWriter writer, float value)
	{
		Span<byte> span = writer.GetSpan(5);
		Assumes.True(MessagePackPrimitives.TryWrite(span, value, out int written));
		writer.Advance(written);
	}

	/// <summary>
	/// Writes a <see cref="MessagePackCode.Float64"/> value.
	/// </summary>
	/// <param name="writer"><inheritdoc cref="WriteNull(ref BufferWriter)" path="/param[@name='writer']"/></param>
	/// <param name="value">The value.</param>
	public override void Write(ref BufferWriter writer, double value)
	{
		Span<byte> span = writer.GetSpan(9);
		Assumes.True(MessagePackPrimitives.TryWrite(span, value, out int written));
		writer.Advance(written);
	}

	/// <summary>
	/// Writes a <see cref="DateTime"/> using the message code <see cref="ReservedMessagePackExtensionTypeCode.DateTime"/>.
	/// </summary>
	/// <param name="writer"><inheritdoc cref="WriteNull(ref BufferWriter)" path="/param[@name='writer']"/></param>
	/// <param name="value">The value to write.</param>
	/// <exception cref="NotSupportedException">Thrown when <see cref="OldSpec"/> is true because the old spec does not define a <see cref="DateTime"/> format.</exception>
	public void Write(ref BufferWriter writer, DateTime value)
	{
		if (this.OldSpec)
		{
			throw new NotSupportedException($"The MsgPack spec does not define a format for {nameof(DateTime)} in {nameof(this.OldSpec)} mode. Turn off {nameof(this.OldSpec)} mode.");
		}
		else
		{
			Span<byte> span = writer.GetSpan(15);
			Assumes.True(MessagePackPrimitives.TryWrite(span, value, out int written));
			writer.Advance(written);
		}
	}

	/// <summary>
	/// Writes out a <see cref="string"/>, prefixed with the length using one of these message codes:
	/// <see cref="MessagePackCode.MinFixStr"/>,
	/// <see cref="MessagePackCode.Str8"/>,
	/// <see cref="MessagePackCode.Str16"/>,
	/// <see cref="MessagePackCode.Str32"/>,
	/// or <see cref="MessagePackCode.Nil"/> if the <paramref name="value"/> is <see langword="null"/>.
	/// </summary>
	/// <param name="writer"><inheritdoc cref="WriteNull(ref BufferWriter)" path="/param[@name='writer']"/></param>
	/// <param name="value">The value to write. May be null.</param>
	public override unsafe void Write(ref BufferWriter writer, string? value)
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

	/// <summary>
	/// Writes out a <see cref="string"/>, prefixed with the length using one of these message codes:
	/// <see cref="MessagePackCode.MinFixStr"/>,
	/// <see cref="MessagePackCode.Str8"/>,
	/// <see cref="MessagePackCode.Str16"/>,
	/// <see cref="MessagePackCode.Str32"/>,
	/// or <see cref="MessagePackCode.Nil"/> if the <paramref name="value"/> is <see langword="null"/>.
	/// </summary>
	/// <param name="writer"><inheritdoc cref="WriteNull(ref BufferWriter)" path="/param[@name='writer']"/></param>
	/// <param name="value">The value to write. May be null.</param>
	public override void Write(ref BufferWriter writer, scoped ReadOnlySpan<char> value)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override void WriteEncodedString(ref BufferWriter writer, scoped ReadOnlySpan<byte> value)
	{
		this.WriteStringHeader(ref writer, value.Length);
		writer.Write(value);
	}

	/// <inheritdoc/>
	public override void WriteEncodedString(ref BufferWriter writer, in ReadOnlySequence<byte> value)
	{
		this.WriteStringHeader(ref writer, checked((int)value.Length));
		writer.Write(value);
	}

	/// <summary>
	/// Writes out a <see cref="string"/>, prefixed with the length using one of these message codes:
	/// <see cref="MessagePackCode.MinFixStr"/>,
	/// <see cref="MessagePackCode.Str8"/>,
	/// <see cref="MessagePackCode.Str16"/>,
	/// <see cref="MessagePackCode.Str32"/>.
	/// </summary>
	/// <param name="writer"><inheritdoc cref="WriteNull(ref BufferWriter)" path="/param[@name='writer']"/></param>
	/// <param name="value">The value to write.</param>
	public override void Write(ref BufferWriter writer, scoped ReadOnlySpan<byte> value)
	{
		int length = value.Length;
		this.WriteBinHeader(ref writer, length);
		Span<byte> span = writer.GetSpan(length);
		value.CopyTo(span);
		writer.Advance(length);
	}

	/// <summary>
	/// Writes out a <see cref="string"/>, prefixed with the length using one of these message codes:
	/// <see cref="MessagePackCode.MinFixStr"/>,
	/// <see cref="MessagePackCode.Str8"/>,
	/// <see cref="MessagePackCode.Str16"/>,
	/// <see cref="MessagePackCode.Str32"/>.
	/// </summary>
	/// <param name="writer"><inheritdoc cref="WriteNull(ref BufferWriter)" path="/param[@name='writer']"/></param>
	/// <param name="value">The value to write.</param>
	public override void Write(ref BufferWriter writer, in ReadOnlySequence<byte> value)
	{
		int length = (int)value.Length;
		this.WriteBinHeader(ref writer, length);
		Span<byte> span = writer.GetSpan(length);
		value.CopyTo(span);
		writer.Advance(length);
	}

	/// <inheritdoc/>
	public override bool TryWriteStartBinary(ref BufferWriter writer, int length)
	{
		this.WriteBinHeader(ref writer, length);
		return true;
	}

	/// <inheritdoc/>
	public override void GetEncodedStringBytes(string value, out ReadOnlyMemory<byte> utf8Bytes, out ReadOnlyMemory<byte> msgpackEncoded)
		=> StringEncoding.GetEncodedStringBytes(value, out utf8Bytes, out msgpackEncoded);

	/// <summary>
	/// Writes the header that precedes a raw binary sequence with a length encoded as the smallest fitting from:
	/// <see cref="MessagePackCode.Bin8"/>,
	/// <see cref="MessagePackCode.Bin16"/>, or
	/// <see cref="MessagePackCode.Bin32"/>.
	/// </summary>
	/// <param name="writer"><inheritdoc cref="WriteNull(ref BufferWriter)" path="/param[@name='writer']"/></param>
	/// <param name="length">The length of bytes that will be written next.</param>
	/// <remarks>
	/// <para>
	/// The caller should use <see cref="BufferWriter.Write(in ReadOnlySequence{byte})"/> or <see cref="BufferWriter.Write(ReadOnlySpan{byte})"/>
	/// on <see cref="Writer.Buffer"/>
	/// after calling this method to actually write the content.
	/// Alternatively a single call to <see cref="Write(ref BufferWriter, ReadOnlySpan{byte})"/> or <see cref="Write(ref BufferWriter, in ReadOnlySequence{byte})"/> will take care of the header and content in one call.
	/// </para>
	/// <para>
	/// When <see cref="OldSpec"/> is <see langword="true"/>, the msgpack code used is <see cref="MessagePackCode.Str8"/>, <see cref="MessagePackCode.Str16"/> or <see cref="MessagePackCode.Str32"/> instead.
	/// </para>
	/// </remarks>
	public void WriteBinHeader(ref BufferWriter writer, int length)
	{
		if (this.OldSpec)
		{
			this.WriteStringHeader(ref writer, length);
			return;
		}

		// When we write the header, we'll ask for all the space we need for the payload as well
		// as that may help ensure we only allocate a buffer once.
		Span<byte> span = writer.GetSpan(length + 5);
		Assumes.True(MessagePackPrimitives.TryWriteBinHeader(span, (uint)length, out int written));
		writer.Advance(written);
	}

	/// <summary>
	/// Writes out the header that may precede a UTF-8 encoded string, prefixed with the length using one of these message codes:
	/// <see cref="MessagePackCode.MinFixStr"/>,
	/// <see cref="MessagePackCode.Str8"/>,
	/// <see cref="MessagePackCode.Str16"/>, or
	/// <see cref="MessagePackCode.Str32"/>.
	/// </summary>
	/// <param name="writer"><inheritdoc cref="WriteNull(ref BufferWriter)" path="/param[@name='writer']"/></param>
	/// <param name="byteCount">The number of bytes in the string that will follow this header.</param>
	/// <remarks>
	/// The caller should use <see cref="BufferWriter.Write(in ReadOnlySequence{byte})"/> or <see cref="BufferWriter.Write(ReadOnlySpan{byte})"/>
	/// on <see cref="Writer.Buffer"/>
	/// after calling this method to actually write the content.
	/// Alternatively a single call to <see cref="WriteEncodedString(ref BufferWriter, ReadOnlySpan{byte})"/> or <see cref="WriteEncodedString(ref BufferWriter, in ReadOnlySequence{byte})"/> will take care of the header and content in one call.
	/// </remarks>
	public void WriteStringHeader(ref BufferWriter writer, int byteCount)
	{
		// When we write the header, we'll ask for all the space we need for the payload as well
		// as that may help ensure we only allocate a buffer once.
		Span<byte> span = writer.GetSpan(byteCount + 5);
		Assumes.True(MessagePackPrimitives.TryWriteStringHeader(span, (uint)byteCount, out int written));
		writer.Advance(written);
	}

	/// <summary>
	/// Writes the extension format header, using the smallest one of these codes:
	/// <see cref="MessagePackCode.FixExt1"/>,
	/// <see cref="MessagePackCode.FixExt2"/>,
	/// <see cref="MessagePackCode.FixExt4"/>,
	/// <see cref="MessagePackCode.FixExt8"/>,
	/// <see cref="MessagePackCode.FixExt16"/>,
	/// <see cref="MessagePackCode.Ext8"/>,
	/// <see cref="MessagePackCode.Ext16"/>, or
	/// <see cref="MessagePackCode.Ext32"/>.
	/// </summary>
	/// <param name="writer"><inheritdoc cref="WriteNull(ref BufferWriter)" path="/param[@name='writer']"/></param>
	/// <param name="value">The extension header.</param>
	public void Write(ref BufferWriter writer, ExtensionHeader value)
	{
		// When we write the header, we'll ask for all the space we need for the payload as well
		// as that may help ensure we only allocate a buffer once.
		Span<byte> span = writer.GetSpan((int)(value.Length + 6));
		Assumes.True(MessagePackPrimitives.TryWriteExtensionHeader(span, value, out int written));
		writer.Advance(written);
	}

	/// <summary>
	/// Writes the extension format header, using the smallest one of these codes:
	/// <see cref="MessagePackCode.FixExt1"/>,
	/// <see cref="MessagePackCode.FixExt2"/>,
	/// <see cref="MessagePackCode.FixExt4"/>,
	/// <see cref="MessagePackCode.FixExt8"/>,
	/// <see cref="MessagePackCode.FixExt16"/>,
	/// <see cref="MessagePackCode.Ext8"/>,
	/// <see cref="MessagePackCode.Ext16"/>, or
	/// <see cref="MessagePackCode.Ext32"/>.
	/// </summary>
	/// <param name="writer"><inheritdoc cref="WriteNull(ref BufferWriter)" path="/param[@name='writer']"/></param>
	/// <param name="value">The extension header.</param>
	public void Write(ref BufferWriter writer, Extension value)
	{
		this.Write(ref writer, value.Header);
		writer.Write(value.Data);
	}

	/// <inheritdoc path="/summary"/>
	/// <inheritdoc path="/param"/>
	/// <returns>The byte length; One of 1, 2, 3, 5 or 9 bytes.</returns>
	public override int GetEncodedLength(long value)
	{
		return value switch
		{
			>= 0 => value switch
			{
				<= MessagePackRange.MaxFixPositiveInt => 1,
				<= byte.MaxValue => 2,
				<= ushort.MaxValue => 3,
				<= uint.MaxValue => 5,
				_ => 9,
			},
			_ => value switch
			{
				>= MessagePackRange.MinFixNegativeInt => 1,
				>= sbyte.MinValue => 2,
				>= short.MinValue => 3,
				>= int.MinValue => 5,
				_ => 9,
			},
		};
	}

	/// <inheritdoc path="/summary"/>
	/// <inheritdoc path="/param"/>
	/// <returns>The byte length; One of 1, 2, 3, 5 or 9 bytes.</returns>
	public override int GetEncodedLength(ulong value)
	{
		return value switch
		{
			> long.MaxValue => 9,
			_ => this.GetEncodedLength((long)value),
		};
	}

	/// <summary>
	/// Estimates the length of the header required for a given string.
	/// </summary>
	/// <param name="writer"><inheritdoc cref="WriteNull(ref BufferWriter)" path="/param[@name='writer']"/></param>
	/// <param name="characterLength">The length of the string to be written, in characters.</param>
	/// <param name="bufferSize">Receives the guaranteed length of the returned buffer.</param>
	/// <param name="encodedBytesOffset">Receives the offset within the returned buffer to write the encoded string to.</param>
	/// <returns>
	/// A reference to the first byte in the buffer.
	/// </returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private ref byte WriteString_PrepareSpan(ref BufferWriter writer, int characterLength, out int bufferSize, out int encodedBytesOffset)
	{
		// MaxByteCount -> WritePrefix -> GetBytes has some overheads of `MaxByteCount`
		// solves heuristic length check

		// ensure buffer by MaxByteCount(faster than GetByteCount)
		bufferSize = StringEncoding.UTF8.GetMaxByteCount(characterLength) + 5;
		ref byte buffer = ref writer.GetPointer(bufferSize);

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
	/// <param name="writer"><inheritdoc cref="WriteNull(ref BufferWriter)" path="/param[@name='writer']"/></param>
	/// <param name="pBuffer">A pointer obtained from a prior call to <see cref="WriteString_PrepareSpan"/>.</param>
	/// <param name="estimatedOffset">The offset obtained from a prior call to <see cref="WriteString_PrepareSpan"/>.</param>
	/// <param name="byteCount">The number of bytes used to actually encode the string.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe void WriteString_PostEncoding(ref BufferWriter writer, byte* pBuffer, int estimatedOffset, int byteCount)
	{
		// move body and write prefix
		if (byteCount <= MessagePackRange.MaxFixStringLength)
		{
			if (estimatedOffset != 1)
			{
				Buffer.MemoryCopy(pBuffer + estimatedOffset, pBuffer + 1, byteCount, byteCount);
			}

			pBuffer[0] = (byte)(MessagePackCode.MinFixStr | byteCount);
			writer.Advance(byteCount + 1);
		}
		else if (byteCount <= byte.MaxValue && !this.OldSpec)
		{
			if (estimatedOffset != 2)
			{
				Buffer.MemoryCopy(pBuffer + estimatedOffset, pBuffer + 2, byteCount, byteCount);
			}

			pBuffer[0] = MessagePackCode.Str8;
			pBuffer[1] = unchecked((byte)byteCount);
			writer.Advance(byteCount + 2);
		}
		else if (byteCount <= ushort.MaxValue)
		{
			if (estimatedOffset != 3)
			{
				Buffer.MemoryCopy(pBuffer + estimatedOffset, pBuffer + 3, byteCount, byteCount);
			}

			pBuffer[0] = MessagePackCode.Str16;
			WriteBigEndian((ushort)byteCount, pBuffer + 1);
			writer.Advance(byteCount + 3);
		}
		else
		{
			if (estimatedOffset != 5)
			{
				Buffer.MemoryCopy(pBuffer + estimatedOffset, pBuffer + 5, byteCount, byteCount);
			}

			pBuffer[0] = MessagePackCode.Str32;
			WriteBigEndian((uint)byteCount, pBuffer + 1);
			writer.Advance(byteCount + 5);
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
}
