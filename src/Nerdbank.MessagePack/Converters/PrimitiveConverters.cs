﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single class

using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft;
using Strings = Microsoft.NET.StringTools.Strings;

namespace Nerdbank.MessagePack.Converters;

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
		if (streamingReader.TryReadRaw(length, out RawMessagePack utf8BytesSequence).NeedsMoreBytes())
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
				Assumes.True(streamingReader.TryReadRaw(thisLoopLength, out utf8BytesSequence) == MessagePackPrimitives.DecodeResult.Success);
				bool flush = utf8BytesSequence.MsgPack.Length == remainingBytesToDecode;
				decoder.Convert(utf8BytesSequence, charSequence, flush, out _, out _);
				remainingBytesToDecode -= checked((uint)utf8BytesSequence.MsgPack.Length);
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
	/// <summary>
	/// Gets the <see cref="DateTimeKind"/> the be used when serializing
	/// <see cref="DateTime"/> values whose <see cref="DateTime.Kind"/> value
	/// is left <see cref="DateTimeKind.Unspecified"/>.
	/// </summary>
	internal DateTimeKind? UnspecifiedKindAssumption { get; init; }

	/// <inheritdoc/>
	public override DateTime Read(ref MessagePackReader reader, SerializationContext context) => reader.ReadDateTime();

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in DateTime value, SerializationContext context)
	{
		if (value.Kind == DateTimeKind.Unspecified && this.UnspecifiedKindAssumption.HasValue)
		{
			// If the Kind is unspecified, use the user-supplied assumed Kind.
			writer.Write(DateTime.SpecifyKind(value, this.UnspecifiedKindAssumption.Value));
		}
		else
		{
			writer.Write(value);
		}
	}

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
[GenerateShapeFor<byte>]
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
/// Serializes a <see cref="Guid"/> value as a 16 byte binary blob, using little endian integer encoding.
/// </summary>
internal class GuidAsLittleEndianBinaryConverter : MessagePackConverter<Guid>
{
	/// <summary>
	/// A shared instance.
	/// </summary>
	internal static readonly GuidAsLittleEndianBinaryConverter Instance = new();

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
			return PolyfillExtensions.ParseGuidFromLittleEndianBytes(bytes.First.Span);
#endif
		}
		else
		{
			Span<byte> guidValue = stackalloc byte[GuidLength];
			bytes.CopyTo(guidValue);
#if NET
			return new Guid(guidValue);
#else
			return PolyfillExtensions.ParseGuidFromLittleEndianBytes(guidValue);
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
/// Serializes a <see cref="Guid"/> value as a string.
/// </summary>
internal class GuidAsStringConverter : MessagePackConverter<Guid>
{
	/// <summary>
	/// A shared instance.
	/// </summary>
	internal static readonly GuidAsStringConverter Instance = new();

	private const int LongestLength = 68; // Format "X" produces the longest length.
	private char format = 'D';

	/// <summary>
	/// Gets the format to use when formatting the GUID as a string.
	/// </summary>
	/// <value>
	/// Default is "D".
	/// Allowed values are "N", "D", "B", "P", or "X".
	/// </value>
	/// <remarks>
	/// While the deserializer may be optimized based on the value specified here,
	/// all formats will be allowed during deserialization.
	/// </remarks>
	internal char Format
	{
		get => this.format;
		init
		{
			Requires.Argument(value is 'N' or 'D' or 'B' or 'P' or 'X', nameof(value), "Format must be one of 'N', 'D', 'B', 'P', or 'X'.");
			this.format = value;
		}
	}

	/// <inheritdoc/>
	public override Guid Read(ref MessagePackReader reader, SerializationContext context)
	{
		ReadOnlySequence<byte> utf8Guid = reader.ReadStringSequence() ?? throw MessagePackSerializationException.ThrowUnexpectedNilWhileDeserializing<Guid>();
		if (utf8Guid.Length > LongestLength)
		{
			throw new MessagePackSerializationException($"The string representation of the GUID is longer than the max allowed ({LongestLength}).");
		}

		if (utf8Guid.IsSingleSegment)
		{
			if (GuidBits.TryParseUtf8(utf8Guid.First.Span, out Guid guid))
			{
				return guid;
			}
		}
		else
		{
			Span<byte> utf8GuidSpan = stackalloc byte[(int)utf8Guid.Length];
			utf8Guid.CopyTo(utf8GuidSpan);
			if (GuidBits.TryParseUtf8(utf8GuidSpan, out Guid guid))
			{
				return guid;
			}
		}

		throw new MessagePackSerializationException("Not a recognized GUID format.");
	}

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in Guid value, SerializationContext context)
	{
		Span<byte> buffer = stackalloc byte[LongestLength];
		Assumes.True(value.TryFormat(buffer, out int bytesWritten, [this.Format]));
		writer.WriteString(buffer[..bytesWritten]);
	}

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
		=> new()
		{
			["type"] = "string",
			["pattern"] = this.Format switch
			{
				'N' => @"^[0-9a-fA-F]{32}$", // 32 digits, no hyphens.
				'D' => @"^[0-9a-fA-F]{8}(?:-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12}$", // 8-4-4-4-12 digits, with hyphens.
				'B' => @"^\{[0-9a-fA-F]{8}(?:-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12}\}$", // 8-4-4-4-12 digits, with hyphens and braces.
				'P' => @"^\([0-9a-fA-F]{8}(?:-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12}\)$", // 8-4-4-4-12 digits, with hyphens and parentheses.
				'X' => @"^\{0x[0-9a-fA-F]{8}(?:,0x[0-9a-fA-F]{4}){2},\{0x[0-9a-fA-F]{2}(?:,0x[0-9a-fA-F]{2}){7}\}\}$", // Hexadecimal format with braces.
				_ => string.Empty, // unknown format.
			},
		};
}

/// <summary>
/// Serializes a nullable value type.
/// </summary>
/// <typeparam name="TOptional">The optional wrapper around <typeparamref name="TElement"/>.</typeparam>
/// <typeparam name="TElement">The value type.</typeparam>
/// <param name="elementConverter">The converter to use when the value is not null.</param>
/// <param name="deconstructor">A function to unwrap an optional value.</param>
/// <param name="createNone">A function to create a deserialized "missing" value.</param>
/// <param name="createSome">A function to wrap a deserialized value.</param>
internal class OptionalConverter<TOptional, TElement>(
	MessagePackConverter<TElement> elementConverter,
	OptionDeconstructor<TOptional, TElement> deconstructor,
	Func<TOptional> createNone,
	Func<TElement, TOptional> createSome) : MessagePackConverter<TOptional>
{
	/// <inheritdoc/>
	public override bool PreferAsyncSerialization => elementConverter.PreferAsyncSerialization;

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in TOptional? value, SerializationContext context)
	{
		if (!deconstructor(value, out TElement? element))
		{
			writer.WriteNil();
			return;
		}

		elementConverter.Write(ref writer, element, context);
	}

	/// <inheritdoc/>
	public override ValueTask WriteAsync(MessagePackAsyncWriter writer, TOptional? value, SerializationContext context)
	{
		if (!deconstructor(value, out TElement? element))
		{
			writer.WriteNil();
		}

		return elementConverter.WriteAsync(writer, element, context);
	}

	/// <inheritdoc/>
	public override TOptional Read(ref MessagePackReader reader, SerializationContext context)
	{
		if (reader.TryReadNil())
		{
			return createNone();
		}

		return createSome(elementConverter.Read(ref reader, context)!);
	}

	/// <inheritdoc/>
	public override async ValueTask<TOptional?> ReadAsync(MessagePackAsyncReader reader, SerializationContext context)
	{
		MessagePackStreamingReader streamingReader = reader.CreateStreamingReader();
		bool wasNil;
		while (streamingReader.TryReadNil(out wasNil).NeedsMoreBytes())
		{
			streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
		}

		reader.ReturnReader(ref streamingReader);
		if (wasNil)
		{
			return createNone();
		}

		return createSome((await elementConverter.ReadAsync(reader, context).ConfigureAwait(false))!);
	}

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
		=> ApplyJsonSchemaNullability(context.GetJsonSchema(((IOptionalTypeShape)typeShape).ElementType));
}
