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
	private ReadOnlyMemory<byte> encodedOpenBracketCharacter;
	private ReadOnlyMemory<byte> encodedCloseBracketCharacter;
	private ReadOnlyMemory<byte> encodedColonCharacter;
	private ReadOnlyMemory<byte> encodedCommaCharacter;
	private ReadOnlyMemory<byte> nullLiteral;
	private ReadOnlyMemory<byte> trueLiteral;
	private ReadOnlyMemory<byte> falseLiteral;

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
		if (value >= 0)
		{
			return this.GetEncodedLength(unchecked((ulong)value));
		}
		else
		{
			// Convert the negative value to a positive value, and add 1 for the minus sign.
			return 1 + this.GetEncodedLength(unchecked((ulong)-value));
		}
	}

	/// <inheritdoc/>
	public override int GetEncodedLength(ulong value)
	{
		int length = 1;
		const int Base = 10;
		while (value >= Base)
		{
			value /= Base;
			length++;
		}

		return length;
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
		WriteLiteral(ref writer, value ? this.trueLiteral.Span : this.falseLiteral.Span);
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
		if (!IsFinite(value))
		{
			throw new NotSupportedException($"The value {value} is not supported.");
		}

#if NET
		const int LongestValueInCharacters = 15;
		Span<byte> byteSpan = writer.GetSpan(LongestValueInCharacters);
		Assumes.True(value.TryFormat(byteSpan, out int bytesWritten, provider: CultureInfo.InvariantCulture));
		writer.Advance(bytesWritten);
#else
		string valueAsString = value.ToString(CultureInfo.InvariantCulture);
		Span<byte> byteSpan = writer.GetSpan(this.Encoding.GetMaxByteCount(valueAsString.Length));
		int byteCount = this.Encoding.GetBytes(valueAsString.AsSpan(), byteSpan);
		writer.Write(byteSpan[..byteCount]);
#endif
	}

	/// <inheritdoc/>
	public override void Write(ref BufferWriter writer, double value)
	{
		if (!IsFinite(value))
		{
			throw new NotSupportedException($"The value {value} is not supported.");
		}

#if NET
		const int LongestValueInCharacters = 24;
		Span<byte> byteSpan = writer.GetSpan(LongestValueInCharacters);
		Assumes.True(value.TryFormat(byteSpan, out int bytesWritten, provider: CultureInfo.InvariantCulture));
		writer.Advance(bytesWritten);
#else
		string valueAsString = value.ToString(CultureInfo.InvariantCulture);
		Span<byte> byteSpan = writer.GetSpan(this.Encoding.GetMaxByteCount(valueAsString.Length));
		int byteCount = this.Encoding.GetBytes(valueAsString.AsSpan(), byteSpan);
		writer.Write(byteSpan[..byteCount]);
#endif
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
		Span<byte> span = writer.GetSpan(this.encodedCloseBracketCharacter.Length);
		this.encodedCloseBracketCharacter.Span.CopyTo(span);
		writer.Advance(this.encodedCloseBracketCharacter.Length);
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
		WriteLiteral(ref writer, this.nullLiteral.Span);
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
		Span<byte> span = writer.GetSpan(this.encodedOpenBracketCharacter.Length);
		this.encodedOpenBracketCharacter.Span.CopyTo(span);
		writer.Advance(this.encodedOpenBracketCharacter.Length);
	}

	/// <inheritdoc/>
	public override void WriteVectorElementSeparator(ref BufferWriter writer)
	{
		Span<byte> span = writer.GetSpan(this.encodedCommaCharacter.Length);
		this.encodedCommaCharacter.Span.CopyTo(span);
		writer.Advance(this.encodedCommaCharacter.Length);
	}

	private static void WriteLiteral(ref BufferWriter writer, ReadOnlySpan<byte> literal)
	{
		Span<byte> span = writer.GetSpan(literal.Length);
		literal.CopyTo(span);
		writer.Advance(literal.Length);
	}

	private static bool IsFinite(float value)
	{
#if NET
		return float.IsFinite(value);
#else
		return !(float.IsNaN(value) || float.IsInfinity(value));
#endif
	}

	private static bool IsFinite(double value)
	{
#if NET
		return double.IsFinite(value);
#else
		return !(double.IsNaN(value) || double.IsInfinity(value));
#endif
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
			this.encodedOpenBracketCharacter = this.Encoding.GetBytes("[");
			this.encodedCloseBracketCharacter = this.Encoding.GetBytes("]");
			this.encodedColonCharacter = this.Encoding.GetBytes(":");
			this.encodedCommaCharacter = this.Encoding.GetBytes(",");
			this.nullLiteral = this.Encoding.GetBytes("null");
			this.trueLiteral = this.Encoding.GetBytes("true");
			this.falseLiteral = this.Encoding.GetBytes("false");
		}
	}
}
