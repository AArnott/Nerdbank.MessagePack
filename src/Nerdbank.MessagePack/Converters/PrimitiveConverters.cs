// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single class

using System.Globalization;
using System.Numerics;
using System.Text;
using Microsoft;

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// Serializes a <see cref="string"/>.
/// </summary>
internal class StringConverter : MessagePackConverter<string>
{
	/// <inheritdoc/>
	public override string? Read(ref MessagePackReader reader, SerializationContext context) => reader.ReadString();

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in string? value, SerializationContext context) => writer.Write(value);
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
}

/// <summary>
/// Serializes a <see cref="Half"/>.
/// </summary>
internal class HalfConverter : MessagePackConverter<Half>
{
	/// <inheritdoc/>
	public override Half Read(ref MessagePackReader reader, SerializationContext context) => (Half)reader.ReadSingle();

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in Half value, SerializationContext context) => writer.Write((float)value);
}

/// <summary>
/// Serializes a <see cref="float"/>.
/// </summary>
internal class SingleConverter : MessagePackConverter<float>
{
	/// <inheritdoc/>
	public override float Read(ref MessagePackReader reader, SerializationContext context) => reader.ReadSingle();

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in float value, SerializationContext context) => writer.Write(value);
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
}

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
}

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
			return new BigInteger(bytes.First.Span);
		}
		else
		{
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
		}
	}

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in BigInteger value, SerializationContext context)
	{
		int byteCount = value.GetByteCount();
		writer.WriteBinHeader(byteCount);
		Span<byte> span = writer.GetSpan(byteCount);
		Assumes.True(value.TryWriteBytes(span, out int written));
		writer.Advance(written);
	}
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
}

/// <summary>
/// Serializes <see cref="DateOnly"/> values.
/// </summary>
internal class DateOnlyConverter : MessagePackConverter<DateOnly>
{
	/// <inheritdoc/>
	public override DateOnly Read(ref MessagePackReader reader, SerializationContext context) => DateOnly.FromDayNumber(reader.ReadInt32());

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in DateOnly value, SerializationContext context) => writer.Write(value.DayNumber);
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
}

/// <summary>
/// Serializes <see cref="TimeSpan"/> values.
/// </summary>
internal class TimeSpanConverter : MessagePackConverter<TimeSpan>
{
	/// <inheritdoc/>
	public override TimeSpan Read(ref MessagePackReader reader, SerializationContext context) => new TimeSpan(reader.ReadInt64());

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in TimeSpan value, SerializationContext context) => writer.Write(value.Ticks);
}

/// <summary>
/// Serializes <see cref="Rune"/> values.
/// </summary>
internal class RuneConverter : MessagePackConverter<Rune>
{
	/// <inheritdoc/>
	public override Rune Read(ref MessagePackReader reader, SerializationContext context) => new Rune(reader.ReadInt32());

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in Rune value, SerializationContext context) => writer.Write(value.Value);
}

/// <summary>
/// Serializes <see cref="char"/> values.
/// </summary>
internal class CharConverter : MessagePackConverter<char>
{
	/// <inheritdoc/>
	public override char Read(ref MessagePackReader reader, SerializationContext context) => reader.ReadChar();

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in char value, SerializationContext context) => writer.Write(value);
}

/// <summary>
/// Serializes <see cref="byte"/> array values.
/// </summary>
internal class ByteArrayConverter : MessagePackConverter<byte[]?>
{
	/// <inheritdoc/>
	public override byte[]? Read(ref MessagePackReader reader, SerializationContext context) => reader.ReadBytes()?.ToArray();

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in byte[]? value, SerializationContext context) => writer.Write(value);
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
			return new Guid(bytes.FirstSpan);
		}
		else
		{
			Span<byte> guidValue = stackalloc byte[GuidLength];
			bytes.CopyTo(guidValue);
			return new Guid(guidValue);
		}
	}

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in Guid value, SerializationContext context)
	{
		writer.WriteBinHeader(GuidLength);
		Assumes.True(value.TryWriteBytes(writer.GetSpan(GuidLength)));
		writer.Advance(GuidLength);
	}
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
}
