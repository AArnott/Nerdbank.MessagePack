// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;

namespace ShapeShift.Json;

/// <summary>
/// A JSON implementation of <see cref="StreamingDeformatter"/>.
/// </summary>
internal record JsonStreamingDeformatter : StreamingDeformatter
{
	/// <summary>
	/// The default instance.
	/// </summary>
	internal static readonly JsonStreamingDeformatter Default = new();

	private JsonStreamingDeformatter()
	{
	}

	/// <inheritdoc/>
	public override string FormatName => "JSON";

	/// <inheritdoc/>
	public override Encoding Encoding => JsonFormatter.DefaultUTF8Encoding;

	/// <inheritdoc/>
	public override DecodeResult TryAdvanceToNextElement(ref Reader reader, out bool hasAnotherElement)
	{
		if (this.Encoding is not UTF8Encoding)
		{
			throw new NotImplementedException();
		}

		// Advance past all whitespace characters.
		// If the first non-whitespace character is a comma (that isn't followed by ] or }, if we want to support trailing commas), return true.
		// If the first non-whitespace character is a closing bracket or brace, return false.
		// Anything else return true, since it may be the *first* element in the collection.
		const byte comma = (byte)',';
		const byte closingBracket = (byte)']';
		const byte closingBrace = (byte)'}';
		long advance;
		for (int i = 0; i < reader.UnreadSpan.Length; i++)
		{
			if (!char.IsWhiteSpace((char)reader.UnreadSpan[i]))
			{
				switch (reader.UnreadSpan[i])
				{
					case closingBrace:
					case closingBracket:
						hasAnotherElement = false;
						advance = i + 1;
						break;
					case comma:
						hasAnotherElement = true;
						advance = i + 1;
						break;
					default:
						advance = i; // do NOT consume this character.
						hasAnotherElement = true;
						break;
				}

				reader.Advance(advance);
				return DecodeResult.Success;
			}
		}

		if (reader.Remaining > reader.UnreadSpan.Length)
		{
			throw new NotImplementedException();
		}

		hasAnotherElement = false;
		return DecodeResult.InsufficientBuffer;
	}

	/// <inheritdoc/>
	public override DecodeResult TryPeekIsFloat32(in Reader reader, out bool float32)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override DecodeResult TryPeekIsSignedInteger(in Reader reader, out bool signed)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override DecodeResult TryPeekNextTypeCode(in Reader reader, out TokenType typeCode)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override DecodeResult TryRead(ref Reader reader, out bool value)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override DecodeResult TryRead(ref Reader reader, out char value)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override DecodeResult TryRead(ref Reader reader, out sbyte value)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override DecodeResult TryRead(ref Reader reader, out short value)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override DecodeResult TryRead(ref Reader reader, out int value)
	{
		if (this.Encoding is not UTF8Encoding)
		{
			throw new NotImplementedException();
		}

		Utf8JsonReader r = new(reader.UnreadSequence);
		if (r.Read())
		{
			if (r.TokenType == JsonTokenType.Number)
			{
				value = r.GetInt32();
				reader.Advance(r.BytesConsumed);
				return DecodeResult.Success;
			}
			else
			{
				value = default;
				return DecodeResult.TokenMismatch;
			}
		}

		value = default;
		return this.InsufficientBytes(reader);
	}

	/// <inheritdoc/>
	public override DecodeResult TryRead(ref Reader reader, out long value)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override DecodeResult TryRead(ref Reader reader, out byte value)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override DecodeResult TryRead(ref Reader reader, out ushort value)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override DecodeResult TryRead(ref Reader reader, out uint value)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override DecodeResult TryRead(ref Reader reader, out ulong value)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override DecodeResult TryRead(ref Reader reader, out float value)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override DecodeResult TryRead(ref Reader reader, out double value)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override DecodeResult TryRead(ref Reader reader, out string? value)
	{
		if (this.Encoding is not UTF8Encoding)
		{
			throw new NotImplementedException();
		}

		Utf8JsonReader r = new(reader.UnreadSequence);
		if (r.Read())
		{
			if (r.TokenType == JsonTokenType.String)
			{
				value = r.GetString();
				reader.Advance(r.BytesConsumed);
				return DecodeResult.Success;
			}
			else
			{
				value = null;
				return DecodeResult.TokenMismatch;
			}
		}

		value = null;
		return this.InsufficientBytes(reader);
	}

	/// <inheritdoc/>
	public override DecodeResult TryReadBinary(ref Reader reader, out ReadOnlySequence<byte> value)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override DecodeResult TryReadMapKeyValueSeparator(ref Reader reader)
	{
		if (this.Encoding is not UTF8Encoding)
		{
			throw new NotImplementedException();
		}

		int colonIndex = reader.UnreadSpan.IndexOf((byte)':');
		if (colonIndex != -1)
		{
			reader.Advance(colonIndex + 1);
			return DecodeResult.Success;
		}

		if (reader.Remaining > reader.UnreadSpan.Length)
		{
			SequencePosition? colonPosition = reader.UnreadSequence.Slice(reader.UnreadSpan.Length).PositionOf((byte)':');
			if (colonPosition.HasValue)
			{
				throw new NotImplementedException();
			}
		}

		return DecodeResult.TokenMismatch;
	}

	/// <inheritdoc/>
	public override DecodeResult TryReadNull(ref Reader reader)
	{
		if (this.Encoding is not UTF8Encoding)
		{
			throw new NotImplementedException();
		}

		Utf8JsonReader r = new(reader.UnreadSequence);
		if (r.Read())
		{
			if (r.TokenType == JsonTokenType.Null)
			{
				reader.Advance(r.BytesConsumed);
				return DecodeResult.Success;
			}
			else
			{
				return DecodeResult.TokenMismatch;
			}
		}

		return this.InsufficientBytes(reader);
	}

	/// <inheritdoc/>
	public override DecodeResult TryReadNull(ref Reader reader, out bool isNull)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override DecodeResult TryReadRaw(ref Reader reader, long length, out ReadOnlySequence<byte> rawBytes)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override DecodeResult TryReadStartMap(ref Reader reader, out int? count)
	{
		// JSON does not prefix the size of the map.
		count = null;

		if (this.Encoding is not UTF8Encoding)
		{
			throw new NotImplementedException();
		}

		Utf8JsonReader r = new(reader.UnreadSequence);
		if (r.Read())
		{
			if (r.TokenType == JsonTokenType.StartObject)
			{
				reader.Advance(r.BytesConsumed);
				return DecodeResult.Success;
			}
			else
			{
				return DecodeResult.TokenMismatch;
			}
		}

		return this.InsufficientBytes(reader);
	}

	/// <inheritdoc/>
	public override DecodeResult TryReadStartVector(ref Reader reader, out int? length)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override DecodeResult TryReadStringSequence(ref Reader reader, out ReadOnlySequence<byte> value)
	{
		// Utf8JsonReader doesn't support reading a string as a byte sequence.
		throw new NotSupportedException();
	}

	/// <inheritdoc/>
	public override DecodeResult TryReadStringSpan(scoped ref Reader reader, out bool contiguous, out ReadOnlySpan<byte> value)
	{
		if (this.Encoding is not UTF8Encoding)
		{
			throw new NotImplementedException();
		}

		// With the UTF8 reader, we never consider strings to be contiguous because the API doesn't give us the raw span.
		contiguous = false;
		value = default;

		Utf8JsonReader utf8Reader = new(reader.UnreadSequence);
		if (!utf8Reader.Read())
		{
			return this.InsufficientBytes(reader);
		}

		if (utf8Reader.TokenType != JsonTokenType.String)
		{
			return DecodeResult.TokenMismatch;
		}

		return DecodeResult.Success;
	}

	/// <inheritdoc/>
	public override DecodeResult TrySkip(ref Reader reader, ref SerializationContext context)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	[DoesNotReturn]
	protected internal override Exception ThrowInvalidCode(in Reader reader)
	{
		throw new NotImplementedException();
	}
}
