// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers.Text;
using System.Globalization;
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

	private const byte EncodedQuoteCharacter = (byte)'"';

	private JsonFormatter()
	{
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
		Memory<byte> bytes = new byte[this.Encoding.GetByteCount(value) + 2];

		Memory<byte> encoded = bytes[1..^1];
		this.Encoding.GetBytes(value, encoded.Span);
		encodedBytes = encoded;

		bytes.Span[0] = EncodedQuoteCharacter;
		bytes.Span[^1] = EncodedQuoteCharacter;

		formattedBytes = bytes;
	}

	/// <inheritdoc/>
	public override void Write(ref BufferWriter writer, bool value) => writer.Write(value ? "true"u8 : "false"u8);

	/// <inheritdoc/>
	public override void Write(ref BufferWriter writer, char value)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override void Write(ref BufferWriter writer, byte value)
	{
		Span<byte> bytes = writer.GetSpan(3); // 3 is the max length of a byte in decimal
		Assumes.True(Utf8Formatter.TryFormat(value, bytes, out int bytesWritten));
		writer.Advance(bytesWritten);
	}

	/// <inheritdoc/>
	public override void Write(ref BufferWriter writer, sbyte value)
	{
		Span<byte> bytes = writer.GetSpan(4);
		Assumes.True(Utf8Formatter.TryFormat(value, bytes, out int bytesWritten));
		writer.Advance(bytesWritten);
	}

	/// <inheritdoc/>
	public override void Write(ref BufferWriter writer, ushort value)
	{
		Span<byte> bytes = writer.GetSpan(5); // 5 is the max length of a ushort in decimal
		Assumes.True(Utf8Formatter.TryFormat(value, bytes, out int bytesWritten));
		writer.Advance(bytesWritten);
	}

	/// <inheritdoc/>
	public override void Write(ref BufferWriter writer, short value)
	{
		Span<byte> bytes = writer.GetSpan(6); // 6 is the max length of a short in decimal
		Assumes.True(Utf8Formatter.TryFormat(value, bytes, out int bytesWritten));
		writer.Advance(bytesWritten);
	}

	/// <inheritdoc/>
	public override void Write(ref BufferWriter writer, uint value)
	{
		Span<byte> bytes = writer.GetSpan(10); // 10 is the max length of a uint in decimal
		Assumes.True(Utf8Formatter.TryFormat(value, bytes, out int bytesWritten));
		writer.Advance(bytesWritten);
	}

	/// <inheritdoc/>
	public override void Write(ref BufferWriter writer, int value)
	{
		Span<byte> bytes = writer.GetSpan(11); // 11 is the max length of an int in decimal
		Assumes.True(Utf8Formatter.TryFormat(value, bytes, out int bytesWritten));
		writer.Advance(bytesWritten);
	}

	/// <inheritdoc/>
	public override void Write(ref BufferWriter writer, ulong value)
	{
		Span<byte> bytes = writer.GetSpan(20); // 20 is the max length of a ulong in decimal
		Assumes.True(Utf8Formatter.TryFormat(value, bytes, out int bytesWritten));
		writer.Advance(bytesWritten);
	}

	/// <inheritdoc/>
	public override void Write(ref BufferWriter writer, long value)
	{
		Span<byte> bytes = writer.GetSpan(20); // 20 is the max length of a long in decimal
		Assumes.True(Utf8Formatter.TryFormat(value, bytes, out int bytesWritten));
		writer.Advance(bytesWritten);
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
		int spanLength = 2 + Base64.GetMaxEncodedToUtf8Length(value.Length);
		Span<byte> bytes = writer.GetSpan(spanLength);
		bytes[0] = EncodedQuoteCharacter;
		Assumes.True(Base64.EncodeToUtf8(value, bytes[1..], out _, out int bytesWritten) == OperationStatus.Done);
		bytes[1 + bytesWritten] = EncodedQuoteCharacter;
		writer.Advance(bytesWritten + 2);
	}

	/// <inheritdoc/>
	public override void Write(ref BufferWriter writer, in ReadOnlySequence<byte> value)
	{
		if (value.IsSingleSegment)
		{
			this.Write(ref writer, value.First.Span);
			return;
		}

		int spanLength = 2 + ((checked((int)value.Length) + 2) / 3 * 4);
		Span<byte> bytes = writer.GetSpan(spanLength);
		int totalBytesWritten = 0;
		bytes[totalBytesWritten++] = EncodedQuoteCharacter;

		int bytesWritten;
		foreach (ReadOnlyMemory<byte> segment in value)
		{
			Assumes.True(Base64.EncodeToUtf8(segment.Span, bytes[totalBytesWritten..], out _, out bytesWritten, isFinalBlock: false) == OperationStatus.Done);
			totalBytesWritten += bytesWritten;
		}

		Assumes.True(Base64.EncodeToUtf8(default, bytes[totalBytesWritten..], out _, out bytesWritten, isFinalBlock: true) == OperationStatus.Done);
		totalBytesWritten += bytesWritten;

		bytes[totalBytesWritten++] = EncodedQuoteCharacter;
		writer.Advance(totalBytesWritten);
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
	public override void WriteEndMap(ref BufferWriter writer) => writer.Write([(byte)'}']);

	/// <inheritdoc/>
	public override void WriteEndVector(ref BufferWriter writer) => writer.Write([(byte)']']);

	/// <inheritdoc/>
	public override void WriteMapKeyValueSeparator(ref BufferWriter writer) => writer.Write([(byte)':']);

	/// <inheritdoc/>
	public override void WriteMapPairSeparator(ref BufferWriter writer) => writer.Write([(byte)',']);

	/// <inheritdoc/>
	public override void WriteNull(ref BufferWriter writer) => writer.Write("null"u8);

	/// <inheritdoc/>
	public override void WriteStartMap(ref BufferWriter writer, int count) => writer.Write([(byte)'{']);

	/// <inheritdoc/>
	public override void WriteStartVector(ref BufferWriter writer, int length) => writer.Write([(byte)'[']);

	/// <inheritdoc/>
	public override void WriteVectorElementSeparator(ref BufferWriter writer) => writer.Write([(byte)',']);

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
}
