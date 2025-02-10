// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.PolySerializer.Converters;

internal abstract class Formatter
{
	protected internal abstract void GetEncodedStringBytes(string value, out ReadOnlyMemory<byte> utf8Bytes, out ReadOnlyMemory<byte> msgpackEncoded);

	public abstract bool ArrayLengthRequiredInHeader { get; }

	public abstract void WriteArrayStart(ref Writer writer, int length);

	public abstract void WriteArrayElementSeparator(ref Writer writer);

	public abstract void WriteArrayEnd(ref Writer writer);

	public abstract void WriteMapStart(ref Writer writer, int length);

	public abstract void WriteMapKeyValueSeparator(ref Writer writer);

	public abstract void WriteMapValueTrailer(ref Writer writer);

	public abstract void WriteMapEnd(ref Writer writer);

	public abstract void WriteNull(ref Writer writer);

	public abstract void Write(ref Writer writer, bool value);

	public abstract void Write(ref Writer writer, char value);

	public abstract void Write(ref Writer writer, byte value);

	public abstract void Write(ref Writer writer, sbyte value);

	public abstract void Write(ref Writer writer, ushort value);

	public abstract void Write(ref Writer writer, short value);

	public abstract void Write(ref Writer writer, uint value);

	public abstract void Write(ref Writer writer, int value);

	public abstract void Write(ref Writer writer, ulong value);

	public abstract void Write(ref Writer writer, long value);

	public abstract void Write(ref Writer writer, float value);

	public abstract void Write(ref Writer writer, double value);

	public abstract void Write(ref Writer writer, ReadOnlySpan<byte> value);

	public abstract void Write(ref Writer writer, ReadOnlySequence<byte> value);
}
