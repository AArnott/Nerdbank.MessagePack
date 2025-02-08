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
using Nerdbank.PolySerializer.MessagePack.Converters;
using Strings = Microsoft.NET.StringTools.Strings;

namespace Nerdbank.PolySerializer.MessagePack.Converters;

/// <summary>
/// Serializes a <see cref="string"/>.
/// </summary>
internal class StringConverter : MessagePackConverter<string>
{
#if NET
	/// <inheritdoc/>
	public override bool PreferAsyncSerialization => false; // async is slower, and incremental decoding isn't worth it.
#endif

	/// <inheritdoc/>
	public override string? Read(ref MessagePackReader reader, SerializationContext context) => reader.ReadString();

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in string? value, SerializationContext context) => writer.Write(value);

#if NET
	/// <inheritdoc/>
	[Experimental("NBMsgPackAsync")]
	public override async ValueTask<string?> ReadAsync(MessagePackAsyncReader reader, SerializationContext context)
	{
		const uint MinChunkSize = 2048;

		MessagePackStreamingReader streamingReader = reader.CreateStreamingReader();
		bool wasNil;
		if (streamingReader.TryReadNil(out wasNil).NeedsMoreBytes())
		{
			streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
		}

		if (wasNil)
		{
			reader.ReturnReader(ref streamingReader);
			return null;
		}

		uint length;
		while (streamingReader.TryReadStringHeader(out length).NeedsMoreBytes())
		{
			streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
		}

		string result;
		if (streamingReader.TryReadRaw(length, out ReadOnlySequence<byte> utf8BytesSequence).NeedsMoreBytes())
		{
			uint remainingBytesToDecode = length;
			using SequencePool<char>.Rental sequenceRental = SequencePool<char>.Shared.Rent();
			Sequence<char> charSequence = sequenceRental.Value;
			Decoder decoder = StringEncoding.UTF8.GetDecoder();
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
			result = StringEncoding.UTF8.GetString(utf8BytesSequence);
		}

		reader.ReturnReader(ref streamingReader);
		return result;
	}

	/// <inheritdoc/>
	[Experimental("NBMsgPackAsync")]
	public override ValueTask WriteAsync(MessagePackAsyncWriter writer, string? value, SerializationContext context)
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
internal class InterningStringConverter : MessagePackConverter<string>
{
	// The actual stack space taken will be up to 2X this value, because we're converting UTF-8 to UTF-16.
	private const int MaxStackStringCharLength = 4096;

	/// <inheritdoc/>
	public override string? Read(ref MessagePackReader reader, SerializationContext context)
	{
		if (reader.TryReadNil())
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
				int characterCount = StringEncoding.UTF8.GetChars(byteSpan, stackSpan);
				return Strings.WeakIntern(stackSpan[..characterCount]);
			}
			else
			{
				int characterCount = StringEncoding.UTF8.GetChars(bytesSequence, stackSpan);
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
	public override void Write(ref MessagePackWriter writer, in string? value, SerializationContext context) => writer.Write(value);

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape) => new() { ["type"] = "string" };
}

/// <summary>
/// Serializes a <see cref="bool"/>.
/// </summary>
internal class BooleanConverter : MessagePackConverter<bool>
{
	/// <inheritdoc/>
	public override bool Read(ref MessagePackReader reader, SerializationContext context) => reader.ReadBoolean();

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in bool value, SerializationContext context) => writer.Write(value);

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape) => new() { ["type"] = "boolean" };
}

/// <summary>
/// Serializes a <see cref="Version"/>.
/// </summary>
internal class VersionConverter : MessagePackConverter<Version?>
{
	/// <inheritdoc/>
	public override Version? Read(ref MessagePackReader reader, SerializationContext context) => reader.ReadString() is string value ? new Version(value) : null;

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in Version? value, SerializationContext context) => writer.Write(value?.ToString());

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
internal class UriConverter : MessagePackConverter<Uri?>
{
	/// <inheritdoc/>
	public override Uri? Read(ref MessagePackReader reader, SerializationContext context) => reader.ReadString() is string value ? new Uri(value, UriKind.RelativeOrAbsolute) : null;

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in Uri? value, SerializationContext context) => writer.Write(value?.OriginalString);

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
internal class HalfConverter : MessagePackConverter<Half>
{
	/// <inheritdoc/>
	public override Half Read(ref MessagePackReader reader, SerializationContext context) => (Half)reader.ReadSingle();

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in Half value, SerializationContext context) => writer.Write((float)value);

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
internal class SingleConverter : MessagePackConverter<float>
{
	/// <inheritdoc/>
	public override float Read(ref MessagePackReader reader, SerializationContext context) => reader.ReadSingle();

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in float value, SerializationContext context) => writer.Write(value);

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
internal class DoubleConverter : MessagePackConverter<double>
{
	/// <inheritdoc/>
	public override double Read(ref MessagePackReader reader, SerializationContext context) => reader.ReadDouble();

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in double value, SerializationContext context) => writer.Write(value);

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
internal class DecimalConverter : MessagePackConverter<decimal>
{
	/// <inheritdoc/>
	public override decimal Read(ref MessagePackReader reader, SerializationContext context)
	{
		if (!(reader.ReadStringSequence() is ReadOnlySequence<byte> sequence))
		{
			throw new MessagePackSerializationException(string.Format("Unexpected msgpack code {0} ({1}) encountered.", MessagePackCode.Nil, MessagePackCode.ToFormatName(MessagePackCode.Nil)));
		}

		if (sequence.IsSingleSegment)
		{
			ReadOnlySpan<byte> span = sequence.First.Span;
			if (System.Buffers.Text.Utf8Parser.TryParse(span, out decimal result, out var bytesConsumed))
			{
				if (span.Length != bytesConsumed)
				{
					throw new MessagePackSerializationException("Unexpected length of string.");
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
						throw new MessagePackSerializationException("Unexpected length of string.");
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
							throw new MessagePackSerializationException("Unexpected length of string.");
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

		throw new MessagePackSerializationException("Can't parse to decimal, input string was not in a correct format.");
	}

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in decimal value, SerializationContext context)
	{
		Span<byte> dest = writer.GetSpan(1 + MessagePackRange.MaxFixStringLength);
		if (System.Buffers.Text.Utf8Formatter.TryFormat(value, dest.Slice(1, MessagePackRange.MaxFixStringLength), out var written))
		{
			// write header
			dest[0] = (byte)(MessagePackCode.MinFixStr | written);
			writer.Advance(written + 1);
		}
		else
		{
			// reset writer's span previously acquired that does not use
			writer.Advance(0);
			writer.Write(value.ToString(CultureInfo.InvariantCulture));
		}
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
internal class Int128Converter : MessagePackConverter<Int128>
{
	/// <inheritdoc/>
	public override Int128 Read(ref MessagePackReader reader, SerializationContext context)
	{
		ReadOnlySequence<byte> sequence = reader.ReadStringSequence() ?? throw MessagePackSerializationException.ThrowUnexpectedNilWhileDeserializing<Int128>();
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

		throw new MessagePackSerializationException("Can't parse to Int128, input string was not in a correct format.");
	}

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in Int128 value, SerializationContext context)
	{
		Span<byte> dest = writer.GetSpan(1 + MessagePackRange.MaxFixStringLength);
		if (value.TryFormat(dest.Slice(1, MessagePackRange.MaxFixStringLength), out var written, provider: CultureInfo.InvariantCulture))
		{
			// write header
			dest[0] = (byte)(MessagePackCode.MinFixStr | written);
			writer.Advance(written + 1);
		}
		else
		{
			// reset writer's span previously acquired that does not use
			writer.Advance(0);
			writer.Write(value.ToString(CultureInfo.InvariantCulture));
		}
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
internal class UInt128Converter : MessagePackConverter<UInt128>
{
	/// <inheritdoc/>
	public override UInt128 Read(ref MessagePackReader reader, SerializationContext context)
	{
		ReadOnlySequence<byte> sequence = reader.ReadStringSequence() ?? throw MessagePackSerializationException.ThrowUnexpectedNilWhileDeserializing<UInt128>();
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

		throw new MessagePackSerializationException("Can't parse to Int123, input string was not in a correct format.");
	}

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in UInt128 value, SerializationContext context)
	{
		Span<byte> dest = writer.GetSpan(1 + MessagePackRange.MaxFixStringLength);
		if (value.TryFormat(dest.Slice(1, MessagePackRange.MaxFixStringLength), out var written, provider: CultureInfo.InvariantCulture))
		{
			// write header
			dest[0] = (byte)(MessagePackCode.MinFixStr | written);
			writer.Advance(written + 1);
		}
		else
		{
			// reset writer's span previously acquired that does not use
			writer.Advance(0);
			writer.Write(value.ToString(CultureInfo.InvariantCulture));
		}
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
internal class BigIntegerConverter : MessagePackConverter<BigInteger>
{
	/// <inheritdoc/>
	public override BigInteger Read(ref MessagePackReader reader, SerializationContext context)
	{
		ReadOnlySequence<byte> bytes = reader.ReadBytes() ?? throw MessagePackSerializationException.ThrowUnexpectedNilWhileDeserializing<BigInteger>();
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
	public override void Write(ref MessagePackWriter writer, in BigInteger value, SerializationContext context)
	{
#if NET
		int byteCount = value.GetByteCount();
		writer.WriteBinHeader(byteCount);
		Span<byte> span = writer.GetSpan(byteCount);
		Assumes.True(value.TryWriteBytes(span, out int written));
		writer.Advance(written);
#else
		writer.Write(value.ToByteArray());
#endif
	}

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
		=> CreateMsgPackBinarySchema("The binary representation of a BigInteger.");
}

/// <summary>
/// Serializes <see cref="DateTime"/> values.
/// </summary>
internal class DateTimeConverter : MessagePackConverter<DateTime>
{
	/// <inheritdoc/>
	public override DateTime Read(ref MessagePackReader reader, SerializationContext context) => reader.ReadDateTime();

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in DateTime value, SerializationContext context) => writer.Write(value);

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape) => CreateMsgPackExtensionSchema(ReservedMessagePackExtensionTypeCode.DateTime);
}

/// <summary>
/// Serializes <see cref="DateTimeOffset"/> values.
/// </summary>
internal class DateTimeOffsetConverter : MessagePackConverter<DateTimeOffset>
{
	/// <inheritdoc/>
	public override DateTimeOffset Read(ref MessagePackReader reader, SerializationContext context)
	{
		int count = reader.ReadArrayHeader();
		if (count != 2)
		{
			throw new MessagePackSerializationException("Expected array of length 2.");
		}

		DateTime utcDateTime = reader.ReadDateTime();
		short offsetMinutes = reader.ReadInt16();
		return new DateTimeOffset(utcDateTime.Ticks, TimeSpan.FromMinutes(offsetMinutes));
	}

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in DateTimeOffset value, SerializationContext context)
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
				CreateMsgPackExtensionSchema(ReservedMessagePackExtensionTypeCode.DateTime),
				new JsonObject { ["type"] = "integer" }),
		};
}

#if NET

/// <summary>
/// Serializes <see cref="DateOnly"/> values.
/// </summary>
internal class DateOnlyConverter : MessagePackConverter<DateOnly>
{
	/// <inheritdoc/>
	public override DateOnly Read(ref MessagePackReader reader, SerializationContext context) => DateOnly.FromDayNumber(reader.ReadInt32());

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in DateOnly value, SerializationContext context) => writer.Write(value.DayNumber);

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
internal class TimeOnlyConverter : MessagePackConverter<TimeOnly>
{
	/// <inheritdoc/>
	public override TimeOnly Read(ref MessagePackReader reader, SerializationContext context) => new TimeOnly(reader.ReadInt64());

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in TimeOnly value, SerializationContext context) => writer.Write(value.Ticks);

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
internal class TimeSpanConverter : MessagePackConverter<TimeSpan>
{
	/// <inheritdoc/>
	public override TimeSpan Read(ref MessagePackReader reader, SerializationContext context) => new TimeSpan(reader.ReadInt64());

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in TimeSpan value, SerializationContext context) => writer.Write(value.Ticks);

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
internal class RuneConverter : MessagePackConverter<Rune>
{
	/// <inheritdoc/>
	public override Rune Read(ref MessagePackReader reader, SerializationContext context) => new Rune(reader.ReadInt32());

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in Rune value, SerializationContext context) => writer.Write(value.Value);

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
internal class CharConverter : MessagePackConverter<char>
{
	/// <inheritdoc/>
	public override char Read(ref MessagePackReader reader, SerializationContext context) => reader.ReadChar();

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in char value, SerializationContext context) => writer.Write(value);

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
internal partial class ByteArrayConverter : MessagePackConverter<byte[]?>
{
	/// <summary>
	/// A shareable instance of this converter.
	/// </summary>
	internal static readonly ByteArrayConverter Instance = new();

	private static readonly ArrayConverter<byte> Fallback = new(new ByteConverter());

	/// <inheritdoc/>
	public override byte[]? Read(ref MessagePackReader reader, SerializationContext context)
	{
		switch (reader.NextMessagePackType)
		{
			case MessagePackType.Nil:
				reader.ReadNil();
				return null;
			case MessagePackType.Binary:
				return reader.ReadBytes()?.ToArray();
			default:
				return Fallback.Read(ref reader, context);
		}
	}

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in byte[]? value, SerializationContext context) => writer.Write(value);

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
		=> new()
		{
			["oneOf"] = new JsonArray(
				CreateMsgPackBinarySchema("The literal content of the buffer."),
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
internal class MemoryOfByteConverter : MessagePackConverter<Memory<byte>>
{
	/// <inheritdoc/>
	public override Memory<byte> Read(ref MessagePackReader reader, SerializationContext context) => ByteArrayConverter.Instance.Read(ref reader, context);

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in Memory<byte> value, SerializationContext context) => writer.Write(value.Span);

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
		=> ByteArrayConverter.Instance.GetJsonSchema(context, typeShape);
}

/// <summary>
/// Serializes <see cref="byte"/> array values.
/// </summary>
internal class ReadOnlyMemoryOfByteConverter : MessagePackConverter<ReadOnlyMemory<byte>>
{
	/// <inheritdoc/>
	public override ReadOnlyMemory<byte> Read(ref MessagePackReader reader, SerializationContext context) => ByteArrayConverter.Instance.Read(ref reader, context);

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in ReadOnlyMemory<byte> value, SerializationContext context) => writer.Write(value.Span);

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
		=> ByteArrayConverter.Instance.GetJsonSchema(context, typeShape);
}

/// <summary>
/// Serializes a <see cref="Guid"/> value.
/// </summary>
internal class GuidConverter : MessagePackConverter<Guid>
{
	private const int GuidLength = 16;

	/// <inheritdoc/>
	public override Guid Read(ref MessagePackReader reader, SerializationContext context)
	{
		ReadOnlySequence<byte> bytes = reader.ReadBytes() ?? throw MessagePackSerializationException.ThrowUnexpectedNilWhileDeserializing<Guid>();

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
	public override void Write(ref MessagePackWriter writer, in Guid value, SerializationContext context)
	{
		writer.WriteBinHeader(GuidLength);
		Assumes.True(value.TryWriteBytes(writer.GetSpan(GuidLength)));
		writer.Advance(GuidLength);
	}

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
		=> CreateMsgPackBinarySchema("The binary representation of the GUID.");
}

/// <summary>
/// Serializes a nullable value type.
/// </summary>
/// <typeparam name="T">The value type.</typeparam>
/// <param name="elementConverter">The converter to use when the value is not null.</param>
internal class NullableConverter<T>(MessagePackConverter<T> elementConverter) : MessagePackConverter<T?>
	where T : struct
{
	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in T? value, SerializationContext context)
	{
		if (value.HasValue)
		{
			elementConverter.Write(ref writer, value.Value, context);
		}
		else
		{
			writer.WriteNil();
		}
	}

	/// <inheritdoc/>
	public override T? Read(ref MessagePackReader reader, SerializationContext context)
	{
		if (reader.TryReadNil())
		{
			return null;
		}

		return elementConverter.Read(ref reader, context);
	}

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
		=> ApplyJsonSchemaNullability(context.GetJsonSchema(((INullableTypeShape<T>)typeShape).ElementType));
}
