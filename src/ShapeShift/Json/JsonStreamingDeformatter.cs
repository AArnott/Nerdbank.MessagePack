// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using Microsoft;

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
	public override DecodeResult TryAdvanceToNextElement(ref Reader reader, ref bool isFirstElement, out bool hasAnotherElement)
	{
		if (this.Encoding is not UTF8Encoding)
		{
			throw new NotImplementedException();
		}

		// Advance past all whitespace characters.
		// If the first non-whitespace character is a comma (that isn't followed by ] or }, if we want to support trailing commas),
		//     if !isFirstElement, return true.
		//     else throw for an unexpected token.
		// If the first non-whitespace character is a closing bracket or brace, return false.
		const byte comma = (byte)',';
		const byte closingBracket = (byte)']';
		const byte closingBrace = (byte)'}';
		long advance;
		for (int i = 0; i < reader.UnreadSpan.Length; i++)
		{
			if (char.IsWhiteSpace((char)reader.UnreadSpan[i]))
			{
				continue;
			}

			switch (reader.UnreadSpan[i])
			{
				case closingBrace:
				case closingBracket:
					hasAnotherElement = false;
					advance = i + 1;
					break;
				case comma:
					if (!isFirstElement)
					{
						hasAnotherElement = true;
						advance = i + 1;
					}
					else
					{
						hasAnotherElement = false;
						return DecodeResult.TokenMismatch;
					}

					break;
				default:
					isFirstElement = false;
					advance = i; // do NOT consume this character.
					hasAnotherElement = true;
					break;
			}

			reader.Advance(advance);
			return DecodeResult.Success;
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
		Utf8JsonReader utf8Reader = new(reader.UnreadSequence);
		if (!utf8Reader.Read())
		{
			typeCode = default;
			return this.InsufficientBytes(reader);
		}

		typeCode = utf8Reader.TokenType switch
		{
			JsonTokenType.StartObject => TokenType.Map,
			JsonTokenType.StartArray => TokenType.Vector,
			JsonTokenType.PropertyName or JsonTokenType.String => TokenType.String,
			JsonTokenType.Number => utf8Reader.TryGetInt64(out _) || utf8Reader.TryGetUInt64(out _) ? TokenType.Integer : TokenType.Float,
			JsonTokenType.True or JsonTokenType.False => TokenType.Boolean,
			JsonTokenType.Null => TokenType.Null,
			_ => TokenType.Unknown,
		};
		return DecodeResult.Success;
	}

	/// <inheritdoc/>
	public override DecodeResult TryRead(ref Reader reader, out bool value)
	{
		Utf8JsonReader utf8Reader = new(reader.UnreadSequence);
		if (!utf8Reader.Read())
		{
			value = false;
			return this.InsufficientBytes(reader);
		}

		switch (utf8Reader.TokenType)
		{
			case JsonTokenType.True:
				value = true;
				break;
			case JsonTokenType.False:
				value = false;
				break;
			default:
				value = false;
				return DecodeResult.TokenMismatch;
		}

		reader.Advance(utf8Reader.BytesConsumed);
		return DecodeResult.Success;
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
		Utf8JsonReader r = new(reader.UnreadSequence);
		if (!r.Read())
		{
			value = default;
			return this.InsufficientBytes(reader);
		}

		if (!r.TryGetSingle(out value))
		{
			return DecodeResult.TokenMismatch;
		}

		reader.Advance(r.BytesConsumed);
		return DecodeResult.Success;
	}

	/// <inheritdoc/>
	public override DecodeResult TryRead(ref Reader reader, out double value)
	{
		Utf8JsonReader r = new(reader.UnreadSequence);
		if (!r.Read())
		{
			value = default;
			return this.InsufficientBytes(reader);
		}

		if (!r.TryGetDouble(out value))
		{
			return DecodeResult.TokenMismatch;
		}

		reader.Advance(r.BytesConsumed);
		return DecodeResult.Success;
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
		// JSON does not prefix the size of the vector.
		length = null;

		if (this.Encoding is not UTF8Encoding)
		{
			throw new NotImplementedException();
		}

		Utf8JsonReader r = new(reader.UnreadSequence);
		if (r.Read())
		{
			if (r.TokenType == JsonTokenType.StartArray)
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
	public override DecodeResult TryGetMaxStringLength(in Reader reader, out int chars, out int bytes)
	{
		if (this.Encoding is not UTF8Encoding)
		{
			throw new NotImplementedException();
		}

		Utf8JsonReader utf8Reader = new(reader.UnreadSequence);
		if (!utf8Reader.Read())
		{
			chars = 0;
			bytes = 0;
			return this.InsufficientBytes(reader);
		}

		if (utf8Reader.TokenType != JsonTokenType.String)
		{
			chars = 0;
			bytes = 0;
			return DecodeResult.TokenMismatch;
		}

		bytes = utf8Reader.HasValueSequence ? checked((int)utf8Reader.ValueSequence.Length) : utf8Reader.ValueSpan.Length;
		chars = this.Encoding.GetMaxCharCount(bytes);

		return DecodeResult.Success;
	}

	/// <inheritdoc/>
	public override DecodeResult TryReadString(ref Reader reader, scoped Span<char> destination, out int charsWritten)
	{
		if (this.Encoding is not UTF8Encoding)
		{
			throw new NotImplementedException();
		}

		Utf8JsonReader utf8Reader = new(reader.UnreadSequence);
		if (!utf8Reader.Read())
		{
			charsWritten = 0;
			return this.InsufficientBytes(reader);
		}

		if (utf8Reader.TokenType != JsonTokenType.String)
		{
			charsWritten = 0;
			return DecodeResult.TokenMismatch;
		}

		charsWritten = utf8Reader.CopyString(destination);
		reader.Advance(utf8Reader.BytesConsumed);
		return DecodeResult.Success;
	}

	/// <inheritdoc/>
	public override DecodeResult TryReadString(ref Reader reader, scoped Span<byte> destination, out int bytesWritten)
	{
		if (this.Encoding is not UTF8Encoding)
		{
			throw new NotImplementedException();
		}

		Utf8JsonReader utf8Reader = new(reader.UnreadSequence);
		if (!utf8Reader.Read())
		{
			bytesWritten = 0;
			return this.InsufficientBytes(reader);
		}

		if (utf8Reader.TokenType != JsonTokenType.String)
		{
			bytesWritten = 0;
			return DecodeResult.TokenMismatch;
		}

		bytesWritten = utf8Reader.CopyString(destination);
		reader.Advance(utf8Reader.BytesConsumed);
		return DecodeResult.Success;
	}

	/// <inheritdoc/>
	public override DecodeResult TryReadStringSpan(scoped ref Reader reader, out ReadOnlySpan<byte> value, out bool success)
	{
		if (this.Encoding is not UTF8Encoding)
		{
			throw new NotImplementedException();
		}

		Utf8JsonReader utf8Reader = new(reader.UnreadSequence);
		if (!utf8Reader.Read())
		{
			success = false;
			value = default;
			return this.InsufficientBytes(reader);
		}

		if (utf8Reader.TokenType != JsonTokenType.String)
		{
			success = false;
			value = default;
			return DecodeResult.TokenMismatch;
		}

		if (utf8Reader.HasValueSequence || utf8Reader.ValueIsEscaped)
		{
			value = default;
			success = false;
		}
		else
		{
			value = reader.UnreadSpan;
			reader.Advance(utf8Reader.BytesConsumed);
			success = true;
		}

		return DecodeResult.Success;
	}

	/// <inheritdoc/>
	public override DecodeResult TrySkip(ref Reader reader, ref SerializationContext context)
	{
		uint originalCount = Math.Max(1, context.MidSkipRemainingCount);
		uint count = originalCount;
		Utf8JsonReader utf8Reader = new(reader.UnreadSequence);
		if (!utf8Reader.Read())
		{
			return this.InsufficientBytes(reader);
		}

		// Skip as many structures as we have already predicted we must skip to complete this or a previously suspended skip operation.
		for (uint i = 0; i < count; i++)
		{
			switch (TrySkipOne(ref utf8Reader, this, out uint skipMore))
			{
				case DecodeResult.Success:
					count += skipMore;
					break;
				case DecodeResult.InsufficientBuffer:
					context.MidSkipRemainingCount = count - i;
					this.DecrementRemainingStructures(ref reader, (int)originalCount - (int)context.MidSkipRemainingCount);
					return DecodeResult.InsufficientBuffer;
				case DecodeResult other:
					return other;
			}
		}

		this.DecrementRemainingStructures(ref reader, (int)originalCount);
		context.MidSkipRemainingCount = 0;
		reader.Advance(utf8Reader.BytesConsumed);
		return DecodeResult.Success;

		static DecodeResult TrySkipOne(ref Utf8JsonReader reader, JsonStreamingDeformatter self, out uint skipMore)
		{
			skipMore = 0;
			return reader.TrySkip() ? DecodeResult.Success : DecodeResult.InsufficientBuffer;
		}
	}

	/// <inheritdoc/>
	[DoesNotReturn]
	protected internal override Exception ThrowInvalidCode(in Reader reader)
	{
		Utf8JsonReader utf8Reader = new(reader.UnreadSequence);
		Verify.Operation(utf8Reader.Read(), "Expected to be able to peek the next code.");
		throw new SerializationException(string.Format("Unexpected code {0} encountered.", utf8Reader.TokenType));
	}

	private void DecrementRemainingStructures(ref Reader reader, int count)
	{
		uint expectedRemainingStructures = reader.ExpectedRemainingStructures;
		reader.ExpectedRemainingStructures = checked((uint)(expectedRemainingStructures > count ? expectedRemainingStructures - count : 0));
	}
}
