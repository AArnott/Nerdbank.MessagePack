// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single class

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft;
using Strings = Microsoft.NET.StringTools.Strings;

namespace Nerdbank.PolySerializer.Converters;

/// <summary>
/// Serializes a <see cref="string"/>.
/// </summary>
[GeneralWithFormatterSpecialCasing]
internal class StringConverter : Converter<string>
{
#if NET
	/// <inheritdoc/>
	public override bool PreferAsyncSerialization => false; // async is slower, and incremental decoding isn't worth it.
#endif

	/// <inheritdoc/>
	public override string? Read(ref Reader reader, SerializationContext context) => reader.ReadString();

	/// <inheritdoc/>
	public override void Write(ref Writer writer, in string? value, SerializationContext context) => writer.Write(value);

#if NET
	/// <inheritdoc/>
	[Experimental("NBMsgPackAsync")]
	public override async ValueTask<string?> ReadAsync(AsyncReader reader, SerializationContext context)
	{
		const uint MinChunkSize = 2048;

		StreamingReader streamingReader = reader.CreateStreamingReader();
		bool wasNil;
		if (streamingReader.TryReadNull(out wasNil).NeedsMoreBytes())
		{
			streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
		}

		if (wasNil)
		{
			reader.ReturnReader(ref streamingReader);
			return null;
		}

		string result;
		if (reader.Deformatter.StreamingDeformatter is MessagePack.MsgPackStreamingDeformatter msgpackDeformatter)
		{
			uint length;
			while (msgpackDeformatter.TryReadStringHeader(ref streamingReader.Reader, out length).NeedsMoreBytes())
			{
				streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
			}

			if (streamingReader.TryReadRaw(length, out ReadOnlySequence<byte> utf8BytesSequence).NeedsMoreBytes())
			{
				uint remainingBytesToDecode = length;
				using SequencePool<char>.Rental sequenceRental = SequencePool<char>.Shared.Rent();
				Sequence<char> charSequence = sequenceRental.Value;

				Decoder decoder = reader.Deformatter.Encoding.GetDecoder();
				while (remainingBytesToDecode > 0)
				{
					// We'll always require at least a reasonable number of bytes to decode at once,
					// to keep overhead to a minimum.
					uint desiredBytesThisRound = Math.Min(remainingBytesToDecode, MinChunkSize);
					if (streamingReader.SequenceReader.Remaining < desiredBytesThisRound)
					{
						// We don't have enough bytes to decode this round. Fetch more.
						streamingReader = new(await streamingReader.FetchMoreBytesAsync(desiredBytesThisRound).ConfigureAwait(false));
					}

					int thisLoopLength = unchecked((int)Math.Min(int.MaxValue, Math.Min(checked((uint)streamingReader.SequenceReader.Remaining), remainingBytesToDecode)));
					Assumes.True(streamingReader.TryReadRaw(thisLoopLength, out utf8BytesSequence) == DecodeResult.Success);
					bool flush = utf8BytesSequence.Length == remainingBytesToDecode;
					decoder.Convert(utf8BytesSequence, charSequence, flush, out _, out _);
					remainingBytesToDecode -= checked((uint)utf8BytesSequence.Length);
				}

				result = string.Create(
					checked((int)charSequence.Length),
					charSequence,
					static (span, seq) => seq.AsReadOnlySequence.CopyTo(span));
			}
			else
			{
				// We happened to get all bytes at once. Decode now.
				result = reader.Deformatter.Encoding.GetString(utf8BytesSequence);
			}
		}
		else
		{
			bool contiguous;
			ReadOnlySpan<byte> bytesSpan;
			while (streamingReader.TryReadStringSpan(out contiguous, out bytesSpan).NeedsMoreBytes())
			{
				streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
			}

			if (contiguous)
			{
				result = reader.Deformatter.Encoding.GetString(bytesSpan);
			}
			else
			{
				Assumes.True(streamingReader.TryReadStringSequence(out ReadOnlySequence<byte> bytesSequence) == DecodeResult.Success);
				result = reader.Deformatter.Encoding.GetString(bytesSequence);
			}
		}

		reader.ReturnReader(ref streamingReader);
		return result;
	}

	/// <inheritdoc/>
	[Experimental("NBMsgPackAsync")]
	public override ValueTask WriteAsync(AsyncWriter writer, string? value, SerializationContext context)
	{
		// We *could* do incremental string encoding, flushing periodically based on the user's preferred flush threshold.
		return base.WriteAsync(writer, value, context);
	}
#endif

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape) => new() { ["type"] = "string" };
}

/// <summary>
/// Serializes a <see cref="string"/> and interns them during deserialization.
/// </summary>
internal class InterningStringConverter : Converter<string>
{
	// The actual stack space taken will be up to 2X this value, because we're converting UTF-8 to UTF-16.
	private const int MaxStackStringCharLength = 4096;

	/// <inheritdoc/>
	public override string? Read(ref Reader reader, SerializationContext context)
	{
		if (reader.TryReadNull())
		{
			return null;
		}

		ReadOnlySequence<byte> bytesSequence = default;
		bool spanMode;
		int byteLength;
		if (reader.TryReadStringSpan(out ReadOnlySpan<byte> byteSpan))
		{
			if (byteSpan.IsEmpty)
			{
				return string.Empty;
			}

			spanMode = true;
			byteLength = byteSpan.Length;
		}
		else
		{
			bytesSequence = reader.ReadStringSequence()!.Value;
			spanMode = false;
			byteLength = checked((int)bytesSequence.Length);
		}

		char[]? charArray = byteLength > MaxStackStringCharLength ? ArrayPool<char>.Shared.Rent(byteLength) : null;
		try
		{
			Span<char> stackSpan = charArray ?? stackalloc char[byteLength];
			if (spanMode)
			{
				int characterCount = reader.Deformatter.Encoding.GetChars(byteSpan, stackSpan);
				return Strings.WeakIntern(stackSpan[..characterCount]);
			}
			else
			{
				int characterCount = reader.Deformatter.Encoding.GetChars(bytesSequence, stackSpan);
				return Strings.WeakIntern(stackSpan[..characterCount]);
			}
		}
		finally
		{
			if (charArray is not null)
			{
				ArrayPool<char>.Shared.Return(charArray);
			}
		}
	}

	/// <inheritdoc/>
	public override void Write(ref Writer writer, in string? value, SerializationContext context) => writer.Write(value);

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape) => new() { ["type"] = "string" };
}

/// <summary>
/// Serializes a <see cref="bool"/>.
/// </summary>
internal class BooleanConverter : Converter<bool>
{
	/// <inheritdoc/>
	public override bool Read(ref Reader reader, SerializationContext context) => reader.ReadBoolean();

	/// <inheritdoc/>
	public override void Write(ref Writer writer, in bool value, SerializationContext context) => writer.Write(value);

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape) => new() { ["type"] = "boolean" };
}

/// <summary>
/// Serializes a <see cref="Version"/>.
/// </summary>
internal class VersionConverter : Converter<Version?>
{
	/// <inheritdoc/>
	public override Version? Read(ref Reader reader, SerializationContext context) => reader.ReadString() is string value ? new Version(value) : null;

	/// <inheritdoc/>
	public override void Write(ref Writer writer, in Version? value, SerializationContext context) => writer.Write(value?.ToString());

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
		=> new()
		{
			["type"] = "string",
			["pattern"] = @"^\d+(\.\d+){0,3}$",
		};
}

/// <summary>
/// Serializes a <see cref="Uri"/>.
/// </summary>
internal class UriConverter : Converter<Uri?>
{
	/// <inheritdoc/>
	public override Uri? Read(ref Reader reader, SerializationContext context) => reader.ReadString() is string value ? new Uri(value, UriKind.RelativeOrAbsolute) : null;

	/// <inheritdoc/>
	public override void Write(ref Writer writer, in Uri? value, SerializationContext context) => writer.Write(value?.OriginalString);

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
		=> new()
		{
			["type"] = "string",
			["format"] = "uri",
		};
}

#if NET

/// <summary>
/// Serializes a <see cref="Half"/>.
/// </summary>
internal class HalfConverter : Converter<Half>
{
	/// <inheritdoc/>
	public override Half Read(ref Reader reader, SerializationContext context) => (Half)reader.ReadSingle();

	/// <inheritdoc/>
	public override void Write(ref Writer writer, in Half value, SerializationContext context) => writer.Write((float)value);

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
		=> new()
		{
			["type"] = "number",
			["format"] = "float16",
		};
}

#endif

/// <summary>
/// Serializes a <see cref="float"/>.
/// </summary>
internal class SingleConverter : Converter<float>
{
	/// <inheritdoc/>
	public override float Read(ref Reader reader, SerializationContext context) => reader.ReadSingle();

	/// <inheritdoc/>
	public override void Write(ref Writer writer, in float value, SerializationContext context) => writer.Write(value);

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
		=> new()
		{
			["type"] = "number",
			["format"] = "float32",
		};
}

/// <summary>
/// Serializes a <see cref="double"/>.
/// </summary>
internal class DoubleConverter : Converter<double>
{
	/// <inheritdoc/>
	public override double Read(ref Reader reader, SerializationContext context) => reader.ReadDouble();

	/// <inheritdoc/>
	public override void Write(ref Writer writer, in double value, SerializationContext context) => writer.Write(value);

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
		=> new()
		{
			["type"] = "number",
			["format"] = "float64",
		};
}

/// <summary>
/// Serializes a <see cref="decimal"/>.
/// </summary>
internal class DecimalConverter : Converter<decimal>
{
	/// <inheritdoc/>
	public override decimal Read(ref Reader reader, SerializationContext context)
	{
		if (!(reader.ReadStringSequence() is ReadOnlySequence<byte> sequence))
		{
			throw new SerializationException("Unexpected null encountered.");
		}

		if (sequence.IsSingleSegment)
		{
			ReadOnlySpan<byte> span = sequence.First.Span;
			if (System.Buffers.Text.Utf8Parser.TryParse(span, out decimal result, out var bytesConsumed))
			{
				if (span.Length != bytesConsumed)
				{
					throw new SerializationException("Unexpected length of string.");
				}

				return result;
			}
		}
		else
		{
			// sequence.Length is not free
			var seqLen = (int)sequence.Length;
			if (seqLen < 128)
			{
				Span<byte> span = stackalloc byte[seqLen];
				sequence.CopyTo(span);
				if (System.Buffers.Text.Utf8Parser.TryParse(span, out decimal result, out var bytesConsumed))
				{
					if (seqLen != bytesConsumed)
					{
						throw new SerializationException("Unexpected length of string.");
					}

					return result;
				}
			}
			else
			{
				var rentArray = ArrayPool<byte>.Shared.Rent(seqLen);
				try
				{
					sequence.CopyTo(rentArray);
					if (System.Buffers.Text.Utf8Parser.TryParse(rentArray.AsSpan(0, seqLen), out decimal result, out var bytesConsumed))
					{
						if (seqLen != bytesConsumed)
						{
							throw new SerializationException("Unexpected length of string.");
						}

						return result;
					}
				}
				finally
				{
					ArrayPool<byte>.Shared.Return(rentArray);
				}
			}
		}

		throw new SerializationException("Can't parse to decimal, input string was not in a correct format.");
	}

	/// <inheritdoc/>
	public override void Write(ref Writer writer, in decimal value, SerializationContext context)
	{
		const int MaxLength = 100; // arbitrary but large enough for most decimal values.
		switch (writer.Formatter.Encoding)
		{
			case UTF8Encoding:
				Span<byte> utf8Bytes = stackalloc byte[MaxLength];
				if (System.Buffers.Text.Utf8Formatter.TryFormat(value, utf8Bytes, out int written))
				{
					writer.WriteEncodedString(utf8Bytes[..written]);
					return;
				}

				break;
#if NET
			default:
				Span<char> utf16Bytes = stackalloc char[MaxLength];
				if (value.TryFormat(utf16Bytes, out written, provider: CultureInfo.InvariantCulture))
				{
					writer.Write(utf16Bytes[..written]);
					return;
				}

				break;
#endif
		}

		writer.Write(value.ToString(CultureInfo.InvariantCulture));
	}

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
		=> new()
		{
			["type"] = "string",
			["pattern"] = @"^-?\d+(\.\d+)?$",
		};
}

#if NET

/// <summary>
/// Serializes a <see cref="Int128"/> value.
/// </summary>
internal class Int128Converter : Converter<Int128>
{
	/// <inheritdoc/>
	public override Int128 Read(ref Reader reader, SerializationContext context)
	{
		ReadOnlySequence<byte> sequence = reader.ReadStringSequence() ?? throw SerializationException.ThrowUnexpectedNilWhileDeserializing<Int128>();
		if (sequence.IsSingleSegment)
		{
			ReadOnlySpan<byte> span = sequence.First.Span;
			if (Int128.TryParse(span, CultureInfo.InvariantCulture, out Int128 result))
			{
				return result;
			}
		}
		else
		{
			// sequence.Length is not free
			var seqLen = (int)sequence.Length;
			if (seqLen < 128)
			{
				Span<byte> span = stackalloc byte[seqLen];
				sequence.CopyTo(span);
				if (Int128.TryParse(span, CultureInfo.InvariantCulture, out Int128 result))
				{
					return result;
				}
			}
			else
			{
				var rentArray = ArrayPool<byte>.Shared.Rent(seqLen);
				try
				{
					sequence.CopyTo(rentArray);
					if (Int128.TryParse(rentArray.AsSpan(0, seqLen), CultureInfo.InvariantCulture, out Int128 result))
					{
						return result;
					}
				}
				finally
				{
					ArrayPool<byte>.Shared.Return(rentArray);
				}
			}
		}

		throw new SerializationException("Can't parse to Int128, input string was not in a correct format.");
	}

	/// <inheritdoc/>
	public override void Write(ref Writer writer, in Int128 value, SerializationContext context)
	{
		const int LongestInt128Value = 40; // Max(Int128.MinValue.ToString().Length, Int128.MaxValue.ToString().Length)
		switch (writer.Formatter.Encoding)
		{
			case UTF8Encoding:
				Span<byte> utf8Bytes = stackalloc byte[LongestInt128Value];
				if (value.TryFormat(utf8Bytes, out int bytesWritten, provider: CultureInfo.InvariantCulture))
				{
					writer.WriteEncodedString(utf8Bytes[..bytesWritten]);
					return;
				}

				break;
			default:
				Span<char> utf16Bytes = stackalloc char[LongestInt128Value];
				if (value.TryFormat(utf16Bytes, out int charsWritten, provider: CultureInfo.InvariantCulture))
				{
					writer.Write(utf16Bytes[..charsWritten]);
					return;
				}

				break;
		}

		writer.Write(value.ToString(CultureInfo.InvariantCulture));
	}

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
		=> new()
		{
			["type"] = "string",
			["pattern"] = @"^-?\d+$",
		};
}

/// <summary>
/// Serializes a <see cref="UInt128"/> value.
/// </summary>
internal class UInt128Converter : Converter<UInt128>
{
	/// <inheritdoc/>
	public override UInt128 Read(ref Reader reader, SerializationContext context)
	{
		ReadOnlySequence<byte> sequence = reader.ReadStringSequence() ?? throw SerializationException.ThrowUnexpectedNilWhileDeserializing<UInt128>();
		if (sequence.IsSingleSegment)
		{
			ReadOnlySpan<byte> span = sequence.First.Span;
			if (UInt128.TryParse(span, CultureInfo.InvariantCulture, out UInt128 result))
			{
				return result;
			}
		}
		else
		{
			// sequence.Length is not free
			var seqLen = (int)sequence.Length;
			if (seqLen < 128)
			{
				Span<byte> span = stackalloc byte[seqLen];
				sequence.CopyTo(span);
				if (UInt128.TryParse(span, CultureInfo.InvariantCulture, out UInt128 result))
				{
					return result;
				}
			}
			else
			{
				var rentArray = ArrayPool<byte>.Shared.Rent(seqLen);
				try
				{
					sequence.CopyTo(rentArray);
					if (UInt128.TryParse(rentArray.AsSpan(0, seqLen), CultureInfo.InvariantCulture, out UInt128 result))
					{
						return result;
					}
				}
				finally
				{
					ArrayPool<byte>.Shared.Return(rentArray);
				}
			}
		}

		throw new SerializationException("Can't parse to Int123, input string was not in a correct format.");
	}

	/// <inheritdoc/>
	public override void Write(ref Writer writer, in UInt128 value, SerializationContext context)
	{
		const int LongestUInt128Value = 39; // UInt128.MaxValue.ToString().Length
		switch (writer.Formatter.Encoding)
		{
			case UTF8Encoding:
				Span<byte> utf8Bytes = stackalloc byte[LongestUInt128Value];
				if (value.TryFormat(utf8Bytes, out int bytesWritten, provider: CultureInfo.InvariantCulture))
				{
					writer.WriteEncodedString(utf8Bytes[..bytesWritten]);
					return;
				}

				break;
			default:
				Span<char> utf16Bytes = stackalloc char[LongestUInt128Value];
				if (value.TryFormat(utf16Bytes, out int charsWritten, provider: CultureInfo.InvariantCulture))
				{
					writer.Write(utf16Bytes[..charsWritten]);
					return;
				}

				break;
		}

		writer.Write(value.ToString(CultureInfo.InvariantCulture));
	}

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
		=> new()
		{
			["type"] = "string",
			["pattern"] = @"^\d+$",
		};
}

#endif

/// <summary>
/// Serializes a <see cref="BigInteger"/> value.
/// </summary>
internal class BigIntegerConverter : Converter<BigInteger>
{
	/// <inheritdoc/>
	public override BigInteger Read(ref Reader reader, SerializationContext context)
	{
		ReadOnlySequence<byte> bytes = reader.ReadBytes() ?? throw SerializationException.ThrowUnexpectedNilWhileDeserializing<BigInteger>();
		if (bytes.IsSingleSegment)
		{
#if NET
			return new BigInteger(bytes.First.Span);
#else
			return new BigInteger(bytes.First.ToArray());
#endif
		}
		else
		{
#if NET
			byte[] bytesArray = ArrayPool<byte>.Shared.Rent((int)bytes.Length);
			try
			{
				bytes.CopyTo(bytesArray);
				return new BigInteger(bytesArray.AsSpan(0, (int)bytes.Length));
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(bytesArray);
			}
#else
			return new BigInteger(bytes.ToArray());
#endif
		}
	}

	/// <inheritdoc/>
	public override void Write(ref Writer writer, in BigInteger value, SerializationContext context)
	{
#if NET
		int byteCount = value.GetByteCount();
		if (writer.TryWriteBinHeader(byteCount))
		{
			Span<byte> span = writer.Buffer.GetSpan(byteCount);
			Assumes.True(value.TryWriteBytes(span, out int written));
			writer.Buffer.Advance(written);
		}
		else
		{
			Span<byte> span = stackalloc byte[value.GetByteCount()];
			Assumes.True(value.TryWriteBytes(span, out int written));
			writer.Write(span);
		}
#else
		writer.Write(value.ToByteArray());
#endif
	}

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
		=> CreateBase64EncodedBinarySchema("The binary representation of a BigInteger.");
}

/// <summary>
/// Serializes <see cref="DateTime"/> values.
/// </summary>
internal class DateTimeConverter : Converter<DateTime>
{
	/// <inheritdoc/>
	public override DateTime Read(ref Reader reader, SerializationContext context) => reader.ReadDateTime();

	/// <inheritdoc/>
	public override void Write(ref Writer writer, in DateTime value, SerializationContext context) => writer.Write(value);

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape) => MessagePack.MessagePackConverter.CreateMsgPackExtensionSchema(MessagePack.ReservedMessagePackExtensionTypeCode.DateTime);
}

/// <summary>
/// Serializes <see cref="DateTimeOffset"/> values.
/// </summary>
internal class DateTimeOffsetConverter : Converter<DateTimeOffset>
{
	/// <inheritdoc/>
	public override DateTimeOffset Read(ref Reader reader, SerializationContext context)
	{
		int count = reader.ReadArrayHeader();
		if (count != 2)
		{
			throw new SerializationException("Expected array of length 2.");
		}

		DateTime utcDateTime = reader.ReadDateTime();
		short offsetMinutes = reader.ReadInt16();
		return new DateTimeOffset(utcDateTime.Ticks, TimeSpan.FromMinutes(offsetMinutes));
	}

	/// <inheritdoc/>
	public override void Write(ref Writer writer, in DateTimeOffset value, SerializationContext context)
	{
		writer.WriteArrayHeader(2);
		writer.Write(new DateTime(value.Ticks, DateTimeKind.Utc));
		writer.Write((short)value.Offset.TotalMinutes);
	}

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
		=> new()
		{
			["type"] = "array",
			["items"] = new JsonArray(
				MessagePack.MessagePackConverter.CreateMsgPackExtensionSchema(MessagePack.ReservedMessagePackExtensionTypeCode.DateTime),
				new JsonObject { ["type"] = "integer" }),
		};
}

#if NET

/// <summary>
/// Serializes <see cref="DateOnly"/> values.
/// </summary>
internal class DateOnlyConverter : Converter<DateOnly>
{
	/// <inheritdoc/>
	public override DateOnly Read(ref Reader reader, SerializationContext context) => DateOnly.FromDayNumber(reader.ReadInt32());

	/// <inheritdoc/>
	public override void Write(ref Writer writer, in DateOnly value, SerializationContext context) => writer.Write(value.DayNumber);

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
		=> new()
		{
			["type"] = "integer",
			["minimum"] = DateOnly.MinValue.DayNumber,
			["maximum"] = DateOnly.MaxValue.DayNumber,
		};
}

/// <summary>
/// Serializes <see cref="TimeOnly"/> values.
/// </summary>
internal class TimeOnlyConverter : Converter<TimeOnly>
{
	/// <inheritdoc/>
	public override TimeOnly Read(ref Reader reader, SerializationContext context) => new TimeOnly(reader.ReadInt64());

	/// <inheritdoc/>
	public override void Write(ref Writer writer, in TimeOnly value, SerializationContext context) => writer.Write(value.Ticks);

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
		=> new()
		{
			["type"] = "integer",
			["minimum"] = TimeOnly.MinValue.Ticks,
			["maximum"] = TimeOnly.MaxValue.Ticks,
		};
}

#endif

/// <summary>
/// Serializes <see cref="TimeSpan"/> values.
/// </summary>
internal class TimeSpanConverter : Converter<TimeSpan>
{
	/// <inheritdoc/>
	public override TimeSpan Read(ref Reader reader, SerializationContext context) => new TimeSpan(reader.ReadInt64());

	/// <inheritdoc/>
	public override void Write(ref Writer writer, in TimeSpan value, SerializationContext context) => writer.Write(value.Ticks);

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
		=> new()
		{
			["type"] = "integer",
			["minimum"] = TimeSpan.MinValue.Ticks,
			["maximum"] = TimeSpan.MaxValue.Ticks,
		};
}

#if NET

/// <summary>
/// Serializes <see cref="Rune"/> values.
/// </summary>
internal class RuneConverter : Converter<Rune>
{
	/// <inheritdoc/>
	public override Rune Read(ref Reader reader, SerializationContext context) => new Rune(reader.ReadInt32());

	/// <inheritdoc/>
	public override void Write(ref Writer writer, in Rune value, SerializationContext context) => writer.Write(value.Value);

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
		=> new()
		{
			["type"] = "integer",
		};
}

#endif

/// <summary>
/// Serializes <see cref="char"/> values.
/// </summary>
internal class CharConverter : Converter<char>
{
	/// <inheritdoc/>
	public override char Read(ref Reader reader, SerializationContext context) => reader.ReadChar();

	/// <inheritdoc/>
	public override void Write(ref Writer writer, in char value, SerializationContext context) => writer.Write(value);

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
		=> new()
		{
			["type"] = "integer",
			["minimum"] = 0,
			["maximum"] = ushort.MaxValue,
		};
}

/// <summary>
/// Serializes <see cref="byte"/> array values.
/// </summary>
[GenerateShape<byte>]
internal partial class ByteArrayConverter : Converter<byte[]?>
{
	/// <summary>
	/// A shareable instance of this converter.
	/// </summary>
	internal static readonly ByteArrayConverter Instance = new();

	private static readonly ArrayConverter<byte> Fallback = new(new ByteConverter());

	/// <inheritdoc/>
	public override byte[]? Read(ref Reader reader, SerializationContext context)
	{
		switch (reader.NextTypeCode)
		{
			case PolySerializer.Converters.TypeCode.Nil:
				reader.ReadNull();
				return null;
			case PolySerializer.Converters.TypeCode.Binary:
				return reader.ReadBytes()?.ToArray();
			default:
				return Fallback.Read(ref reader, context);
		}
	}

	/// <inheritdoc/>
	public override void Write(ref Writer writer, in byte[]? value, SerializationContext context) => writer.Write(value);

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
		=> new()
		{
			["oneOf"] = new JsonArray(
				CreateBase64EncodedBinarySchema("The literal content of the buffer."),
				new JsonObject
				{
					["type"] = "array",
					["items"] = new JsonObject { ["type"] = "integer", ["minimum"] = 0, ["maximum"] = byte.MaxValue },
				}),
		};
}

/// <summary>
/// Serializes <see cref="byte"/> array values.
/// </summary>
internal class MemoryOfByteConverter : Converter<Memory<byte>>
{
	/// <inheritdoc/>
	public override Memory<byte> Read(ref Reader reader, SerializationContext context) => ByteArrayConverter.Instance.Read(ref reader, context);

	/// <inheritdoc/>
	public override void Write(ref Writer writer, in Memory<byte> value, SerializationContext context) => writer.Write(value.Span);

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
		=> ByteArrayConverter.Instance.GetJsonSchema(context, typeShape);
}

/// <summary>
/// Serializes <see cref="byte"/> array values.
/// </summary>
internal class ReadOnlyMemoryOfByteConverter : Converter<ReadOnlyMemory<byte>>
{
	/// <inheritdoc/>
	public override ReadOnlyMemory<byte> Read(ref Reader reader, SerializationContext context) => ByteArrayConverter.Instance.Read(ref reader, context);

	/// <inheritdoc/>
	public override void Write(ref Writer writer, in ReadOnlyMemory<byte> value, SerializationContext context) => writer.Write(value.Span);

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
		=> ByteArrayConverter.Instance.GetJsonSchema(context, typeShape);
}

/// <summary>
/// Serializes a <see cref="Guid"/> value.
/// </summary>
internal class GuidConverter : Converter<Guid>
{
	private const int GuidLength = 16;

	/// <inheritdoc/>
	public override Guid Read(ref Reader reader, SerializationContext context)
	{
		ReadOnlySequence<byte> bytes = reader.ReadBytes() ?? throw SerializationException.ThrowUnexpectedNilWhileDeserializing<Guid>();

		if (bytes.IsSingleSegment)
		{
#if NET
			return new Guid(bytes.FirstSpan);
#else
			return PolyfillExtensions.CreateGuid(bytes.First.Span);
#endif
		}
		else
		{
			Span<byte> guidValue = stackalloc byte[GuidLength];
			bytes.CopyTo(guidValue);
#if NET
			return new Guid(guidValue);
#else
			return PolyfillExtensions.CreateGuid(guidValue);
#endif
		}
	}

	/// <inheritdoc/>
	public override void Write(ref Writer writer, in Guid value, SerializationContext context)
	{
		if (writer.TryWriteBinHeader(GuidLength))
		{
			Assumes.True(value.TryWriteBytes(writer.Buffer.GetSpan(GuidLength)));
			writer.Buffer.Advance(GuidLength);
		}
		else
		{
			Span<byte> span = stackalloc byte[GuidLength];
			Assumes.True(value.TryWriteBytes(span));
			writer.Write(span);
		}
	}

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
		=> CreateBase64EncodedBinarySchema("The binary representation of the GUID.");
}

/// <summary>
/// Serializes a nullable value type.
/// </summary>
/// <typeparam name="T">The value type.</typeparam>
/// <param name="elementConverter">The converter to use when the value is not null.</param>
internal class NullableConverter<T>(Converter<T> elementConverter) : Converter<T?>
	where T : struct
{
	/// <inheritdoc/>
	public override void Write(ref Writer writer, in T? value, SerializationContext context)
	{
		if (value.HasValue)
		{
			elementConverter.Write(ref writer, value.Value, context);
		}
		else
		{
			writer.WriteNull();
		}
	}

	/// <inheritdoc/>
	public override T? Read(ref Reader reader, SerializationContext context)
	{
		if (reader.TryReadNull())
		{
			return null;
		}

		return elementConverter.Read(ref reader, context);
	}

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
		=> ApplyJsonSchemaNullability(context.GetJsonSchema(((INullableTypeShape<T>)typeShape).ElementType));
}
