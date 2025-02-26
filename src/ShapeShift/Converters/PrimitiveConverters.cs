// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single class

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft;
using Strings = Microsoft.NET.StringTools.Strings;

namespace ShapeShift.Converters;

/// <summary>
/// Serializes a <see cref="string"/>.
/// </summary>
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
		StreamingReader streamingReader = reader.CreateStreamingReader();

		string? result;
		while (streamingReader.TryRead(out result).NeedsMoreBytes())
		{
			streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
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
	/// <inheritdoc/>
	public override string? Read(ref Reader reader, SerializationContext context)
	{
		if (reader.TryReadNull())
		{
			return null;
		}

		reader.GetMaxStringLength(out int maxCharLength, out _);
		char[]? charArray = maxCharLength > MaxStackStringCharLength ? ArrayPool<char>.Shared.Rent(maxCharLength) : null;
		try
		{
			Span<char> stackSpan = charArray ?? stackalloc char[maxCharLength];
			int characterCount = reader.ReadString(stackSpan);
			return Strings.WeakIntern(stackSpan[..characterCount]);
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
		reader.GetMaxStringLength(out int maxChars, out int maxBytes);
		switch (reader.Deformatter.Encoding)
		{
#if NET
			case UnicodeEncoding:
				char[]? charArray = null;
				try
				{
					Span<char> chars = maxBytes > MaxStackStringCharLength ? charArray = ArrayPool<char>.Shared.Rent(maxBytes) : stackalloc char[maxBytes];
					int byteCount = reader.ReadString(chars);
					if (decimal.TryParse(chars[..byteCount], CultureInfo.InvariantCulture, out decimal result))
					{
						return result;
					}
				}
				finally
				{
					if (charArray is not null)
					{
						ArrayPool<char>.Shared.Return(charArray);
					}
				}

				break;
#endif
			default:
				byte[]? byteArray = null;
				try
				{
					Span<byte> utf8Bytes = maxBytes > MaxStackStringCharLength ? byteArray = ArrayPool<byte>.Shared.Rent(maxBytes) : stackalloc byte[maxBytes];
					int byteCount = reader.ReadString(utf8Bytes);
					if (System.Buffers.Text.Utf8Parser.TryParse(utf8Bytes[..byteCount], out decimal result, out var bytesConsumed))
					{
						if (byteCount != bytesConsumed)
						{
							throw new SerializationException("Unexpected length of string.");
						}

						return result;
					}
				}
				finally
				{
					if (byteArray is not null)
					{
						ArrayPool<byte>.Shared.Return(byteArray);
					}
				}

				break;
		}

		throw new SerializationException("Can't parse to decimal, input string was not in a correct format.");
	}

	/// <inheritdoc/>
	public override void Write(ref Writer writer, in decimal value, SerializationContext context)
	{
		switch (writer.Formatter.Encoding)
		{
			case UTF8Encoding:
				Span<byte> utf8Bytes = stackalloc byte[MaxStackStringCharLength];
				if (System.Buffers.Text.Utf8Formatter.TryFormat(value, utf8Bytes, out int written))
				{
					writer.WriteEncodedString(utf8Bytes[..written]);
					return;
				}

				break;
#if NET
			default:
				Span<char> utf16Bytes = stackalloc char[MaxStackStringCharLength];
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
		reader.GetMaxStringLength(out int maxChars, out int maxBytes);
		switch (reader.Deformatter.Encoding)
		{
			case UnicodeEncoding:
				char[]? charArray = null;
				try
				{
					Span<char> chars = maxBytes > MaxStackStringCharLength ? charArray = ArrayPool<char>.Shared.Rent(maxBytes) : stackalloc char[maxBytes];
					int byteCount = reader.ReadString(chars);
					if (Int128.TryParse(chars[..byteCount], CultureInfo.InvariantCulture, out Int128 result))
					{
						return result;
					}
				}
				finally
				{
					if (charArray is not null)
					{
						ArrayPool<char>.Shared.Return(charArray);
					}
				}

				break;
			default:
				byte[]? byteArray = null;
				try
				{
					Span<byte> utf8Bytes = maxBytes > MaxStackStringCharLength ? byteArray = ArrayPool<byte>.Shared.Rent(maxBytes) : stackalloc byte[maxBytes];
					int byteCount = reader.ReadString(utf8Bytes);
					if (Int128.TryParse(utf8Bytes[..byteCount], CultureInfo.InvariantCulture, out Int128 result))
					{
						return result;
					}
				}
				finally
				{
					if (byteArray is not null)
					{
						ArrayPool<byte>.Shared.Return(byteArray);
					}
				}

				break;
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
		reader.GetMaxStringLength(out int maxChars, out int maxBytes);
		switch (reader.Deformatter.Encoding)
		{
			case UnicodeEncoding:
				char[]? charArray = null;
				try
				{
					Span<char> chars = maxBytes > MaxStackStringCharLength ? charArray = ArrayPool<char>.Shared.Rent(maxBytes) : stackalloc char[maxBytes];
					int byteCount = reader.ReadString(chars);
					if (UInt128.TryParse(chars[..byteCount], CultureInfo.InvariantCulture, out UInt128 result))
					{
						return result;
					}
				}
				finally
				{
					if (charArray is not null)
					{
						ArrayPool<char>.Shared.Return(charArray);
					}
				}

				break;
			default:
				byte[]? byteArray = null;
				try
				{
					Span<byte> utf8Bytes = maxBytes > MaxStackStringCharLength ? byteArray = ArrayPool<byte>.Shared.Rent(maxBytes) : stackalloc byte[maxBytes];
					int byteCount = reader.ReadString(utf8Bytes);
					if (UInt128.TryParse(utf8Bytes[..byteCount], CultureInfo.InvariantCulture, out UInt128 result))
					{
						return result;
					}
				}
				finally
				{
					if (byteArray is not null)
					{
						ArrayPool<byte>.Shared.Return(byteArray);
					}
				}

				break;
		}

		throw new SerializationException("Can't parse to UInt128, input string was not in a correct format.");
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
internal class BigIntegerBinaryConverter : Converter<BigInteger>
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
		if (writer.TryWriteStartBinary(byteCount))
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
/// Serializes a <see cref="BigInteger"/> value.
/// </summary>
internal class BigIntegerTextConverter : Converter<BigInteger>
{
	/// <inheritdoc/>
	public override BigInteger Read(ref Reader reader, SerializationContext context)
	{
#if NET
		reader.GetMaxStringLength(out int maxChars, out int maxBytes);
		char[]? charArray = null;
		try
		{
			Span<char> chars = maxBytes > MaxStackStringCharLength ? charArray = ArrayPool<char>.Shared.Rent(maxBytes) : stackalloc char[maxBytes];
			int byteCount = reader.ReadString(chars);
			return BigInteger.Parse(chars[..byteCount], NumberStyles.Integer, CultureInfo.InvariantCulture);
		}
		finally
		{
			if (charArray is not null)
			{
				ArrayPool<char>.Shared.Return(charArray);
			}
		}
#else
		return BigInteger.Parse(reader.ReadString() ?? throw SerializationException.ThrowUnexpectedNilWhileDeserializing<BigInteger>(), CultureInfo.InvariantCulture);
#endif
	}

	/// <inheritdoc/>
	public override void Write(ref Writer writer, in BigInteger value, SerializationContext context)
	{
#if NET
		Span<char> chars = stackalloc char[MaxStackStringCharLength];
		if (value.TryFormat(chars, out int charsWritten, provider: CultureInfo.InvariantCulture))
		{
			writer.Write(chars[..charsWritten]);
			return;
		}
#endif

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
			case TokenType.Null:
				reader.ReadNull();
				return null;
			case TokenType.Binary or TokenType.String:
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
internal class GuidBinaryConverter : Converter<Guid>
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
		if (writer.TryWriteStartBinary(GuidLength))
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
/// Serializes a <see cref="Guid"/> value.
/// </summary>
internal class GuidTextConverter : Converter<Guid>
{
	/// <inheritdoc/>
	public override Guid Read(ref Reader reader, SerializationContext context)
	{
		reader.GetMaxStringLength(out int maxChars, out _);
		if (maxChars > MaxStackStringCharLength)
		{
			throw new SerializationException("String exceeds reasonable guid length");
		}

		Span<char> guidValue = stackalloc char[maxChars];
		int charCount = reader.ReadString(guidValue);
#if NET
		return Guid.Parse(guidValue[..charCount]);
#else
		return Guid.Parse(guidValue[..charCount].ToString());
#endif
	}

	/// <inheritdoc/>
	public override void Write(ref Writer writer, in Guid value, SerializationContext context)
	{
		const string format = "D";
		switch (writer.Formatter.Encoding)
		{
#if NET
			case UTF8Encoding:
				Span<byte> utf8Bytes = stackalloc byte[100];
				if (!value.TryFormat(utf8Bytes, out int bytesWritten, format))
				{
					throw new SerializationException();
				}

				writer.WriteEncodedString(utf8Bytes[..bytesWritten]);
				break;
			case UnicodeEncoding:
				Span<char> utf16Chars = stackalloc char[100];
				if (!value.TryFormat(utf16Chars, out int charsWritten, format))
				{
					throw new SerializationException();
				}

				writer.WriteEncodedString(MemoryMarshal.Cast<char, byte>(utf16Chars[..charsWritten]));
				break;
#endif
			default:
				writer.Write(value.ToString(format, CultureInfo.InvariantCulture));
				break;
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
