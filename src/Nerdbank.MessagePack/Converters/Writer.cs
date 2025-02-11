// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Nerdbank.PolySerializer.Converters;

public ref struct Writer
{
	private BufferWriter inner;

	internal Writer(BufferWriter writer, Formatter formatter)
	{
		this.inner = writer;
		this.Formatter = formatter;
	}

	[UnscopedRef]
	public ref BufferWriter Buffer => ref this.inner;

	public Formatter Formatter { get; }

	public bool ArrayLengthRequiredInHeader => this.Formatter.ArrayLengthRequiredInHeader;

	public void WriteArrayHeader(int length) => this.Formatter.WriteArrayStart(ref this, length);

	public void WriteArrayElementSeparator() => this.Formatter.WriteArrayElementSeparator(ref this);

	public void WriteArrayEnd() => this.Formatter.WriteArrayEnd(ref this);

	public void WriteMapHeader(int length) => this.Formatter.WriteMapStart(ref this, length);

	public void WriteMapKeyValueSeparator() => this.Formatter.WriteMapKeyValueSeparator(ref this);

	public void WriteMapValueTrailer() => this.Formatter.WriteMapValueTrailer(ref this);

	public void WriteMapEnd() => this.Formatter.WriteMapEnd(ref this);

	public void WriteNull() => this.Formatter.WriteNull(ref this);

	public void Write(bool value) => this.Formatter.Write(ref this, value);

	public void Write(char value) => this.Formatter.Write(ref this, value);

	public void Write(byte value) => this.Formatter.Write(ref this, value);

	public void Write(sbyte value) => this.Formatter.Write(ref this, value);

	public void Write(ushort value) => this.Formatter.Write(ref this, value);

	public void Write(short value) => this.Formatter.Write(ref this, value);

	public void Write(uint value) => this.Formatter.Write(ref this, value);

	public void Write(int value) => this.Formatter.Write(ref this, value);

	public void Write(ulong value) => this.Formatter.Write(ref this, value);

	public void Write(long value) => this.Formatter.Write(ref this, value);

	public void Write(float value) => this.Formatter.Write(ref this, value);

	public void Write(double value) => this.Formatter.Write(ref this, value);

	public void Write(string? value) => this.Formatter.Write(ref this, value);

	public void Write(ReadOnlySpan<byte> value) => this.Formatter.Write(ref this, value);

	public void Write(ReadOnlySequence<byte> value) => this.Formatter.Write(ref this, value);

}
