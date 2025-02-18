// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft;

namespace ShapeShift.Json;

/// <summary>
/// A JSON implementation of <see cref="Formatter"/>.
/// </summary>
internal record JsonFormatter : Formatter
{
	/// <summary>
	/// A UTF-8 encoding without a byte order mark.
	/// </summary>
	internal static readonly Encoding DefaultUTF8Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

	/// <summary>
	/// The default instance.
	/// </summary>
	internal static readonly JsonFormatter Default = new();

	private readonly object syncObject = new();

	private ReadOnlyMemory<byte> encodedQuoteCharacter;
	private ReadOnlyMemory<byte> encodedOpenCurlyCharacter;
	private ReadOnlyMemory<byte> encodedCloseCurlyCharacter;
	private ReadOnlyMemory<byte> encodedColonCharacter;
	private ReadOnlyMemory<byte> encodedCommaCharacter;

	private JsonFormatter()
	{
		this.PrepareEncodings();
	}

	/// <inheritdoc/>
	public override string FormatName => "JSON";

	/// <inheritdoc/>
	public override Encoding Encoding => DefaultUTF8Encoding;

	/// <inheritdoc/>
	public override bool VectorsMustHaveLengthPrefix => throw new NotImplementedException();

	/// <inheritdoc/>
	public override int GetEncodedLength(long value)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override int GetEncodedLength(ulong value)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override void GetEncodedStringBytes(ReadOnlySpan<char> value, out ReadOnlyMemory<byte> encodedBytes, out ReadOnlyMemory<byte> formattedBytes)
	{
		// TODO: apply any necessary escaping to the string value.
		ReadOnlySpan<byte> encodedQuoteCharacter = this.encodedQuoteCharacter.Span;
		Memory<byte> bytes = new byte[this.Encoding.GetByteCount(value) + (encodedQuoteCharacter.Length * 2)];

		Memory<byte> encoded = bytes[encodedQuoteCharacter.Length..^encodedQuoteCharacter.Length];
		this.Encoding.GetBytes(value, encoded.Span);
		encodedBytes = encoded;

		encodedQuoteCharacter.CopyTo(bytes.Span);
		encodedQuoteCharacter.CopyTo(bytes.Span[^encodedQuoteCharacter.Length..]);

		formattedBytes = bytes;
	}

	/// <inheritdoc/>
	public override void Write(ref BufferWriter writer, bool value)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override void Write(ref BufferWriter writer, char value)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override void Write(ref BufferWriter writer, byte value)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override void Write(ref BufferWriter writer, sbyte value)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override void Write(ref BufferWriter writer, ushort value)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override void Write(ref BufferWriter writer, short value)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override void Write(ref BufferWriter writer, uint value)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override void Write(ref BufferWriter writer, int value)
	{
		switch (this.Encoding)
		{
#if NET
			case UTF8Encoding:
				const int characterLength = 11; // max(int.MinValue.ToString().Length, int.MaxValue.ToString().Length)
				Span<byte> byteSpan = writer.GetSpan(characterLength);
				Assumes.True(value.TryFormat(byteSpan, out int bytesWritten, provider: CultureInfo.InvariantCulture));
				writer.Advance(bytesWritten);
				break;
			case UnicodeEncoding:
				byteSpan = writer.GetSpan(characterLength * 2);
				Span<char> charSpan = MemoryMarshal.Cast<byte, char>(byteSpan);
				Assumes.True(value.TryFormat(charSpan, out int charsWritten, provider: CultureInfo.InvariantCulture));
				writer.Advance(charsWritten * 2);
				break;
#endif
			default:
				writer.Write(this.Encoding.GetBytes(value.ToString(CultureInfo.InvariantCulture)));
				break;
		}
	}

	/// <inheritdoc/>
	public override void Write(ref BufferWriter writer, ulong value)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override void Write(ref BufferWriter writer, long value)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override void Write(ref BufferWriter writer, float value)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override void Write(ref BufferWriter writer, double value)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override void Write(ref BufferWriter writer, scoped ReadOnlySpan<char> value)
	{
		this.GetEncodedStringBytes(value, out _, out ReadOnlyMemory<byte> formattedBytes);
		writer.Write(formattedBytes.Span);
	}

	/// <inheritdoc/>
	public override void Write(ref BufferWriter writer, scoped ReadOnlySpan<byte> value)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override void Write(ref BufferWriter writer, in ReadOnlySequence<byte> value)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override void WriteEncodedString(ref BufferWriter writer, scoped ReadOnlySpan<byte> value)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override void WriteEncodedString(ref BufferWriter writer, in ReadOnlySequence<byte> value)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override void WriteEndMap(ref BufferWriter writer)
	{
		Span<byte> span = writer.GetSpan(this.encodedCloseCurlyCharacter.Length);
		this.encodedCloseCurlyCharacter.Span.CopyTo(span);
		writer.Advance(this.encodedCloseCurlyCharacter.Length);
	}

	/// <inheritdoc/>
	public override void WriteEndVector(ref BufferWriter writer)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override void WriteMapKeyValueSeparator(ref BufferWriter writer)
	{
		Span<byte> span = writer.GetSpan(this.encodedColonCharacter.Length);
		this.encodedColonCharacter.Span.CopyTo(span);
		writer.Advance(this.encodedColonCharacter.Length);
	}

	/// <inheritdoc/>
	public override void WriteMapPairSeparator(ref BufferWriter writer)
	{
		Span<byte> span = writer.GetSpan(this.encodedCommaCharacter.Length);
		this.encodedCommaCharacter.Span.CopyTo(span);
		writer.Advance(this.encodedCommaCharacter.Length);
	}

	/// <inheritdoc/>
	public override void WriteNull(ref BufferWriter writer)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override void WriteStartMap(ref BufferWriter writer, int count)
	{
		Span<byte> span = writer.GetSpan(this.encodedOpenCurlyCharacter.Length);
		this.encodedOpenCurlyCharacter.Span.CopyTo(span);
		writer.Advance(this.encodedOpenCurlyCharacter.Length);
	}

	/// <inheritdoc/>
	public override void WriteStartVector(ref BufferWriter writer, int length)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override void WriteVectorElementSeparator(ref BufferWriter writer)
	{
		throw new NotImplementedException();
	}

	private void PrepareEncodings()
	{
		lock (this.syncObject)
		{
			if (!this.encodedQuoteCharacter.IsEmpty)
			{
				return;
			}

			this.encodedQuoteCharacter = this.Encoding.GetBytes("\"");
			this.encodedOpenCurlyCharacter = this.Encoding.GetBytes("{");
			this.encodedCloseCurlyCharacter = this.Encoding.GetBytes("}");
			this.encodedColonCharacter = this.Encoding.GetBytes(":");
			this.encodedCommaCharacter = this.Encoding.GetBytes(",");
		}
	}
}
