// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft;

namespace ShapeShift.Converters;

/// <summary>
/// A convenience API for reading formatted primitive values (e.g. MessagePack, JSON) from a complete buffer.
/// </summary>
/// <remarks>
/// <para>
/// This type is a wrapper around <see cref="Converters.StreamingDeformatter"/>, which is designed to support
/// reading from potentially incomplete buffers without throwing exceptions.
/// </para>
/// <para>
/// Particular formats may derive from this type in order to wrap unique APIs from their derived <see cref="Converters.StreamingDeformatter"/> type.
/// Derived types should avoid adding fields since the core functionality and options are expected to be on the wrapped <see cref="Converters.StreamingDeformatter"/>.
/// </para>
/// <para>
/// Derived types should be implemented in a thread-safe way, ideally by being immutable.
/// </para>
/// </remarks>
public partial class Deformatter
{
	/// <summary>
	/// Initializes a new instance of the <see cref="Deformatter"/> class.
	/// </summary>
	/// <param name="streamingDeformatter">The streaming deformatter to wrap.</param>
	public Deformatter(StreamingDeformatter streamingDeformatter)
	{
		Requires.NotNull(streamingDeformatter);
		this.StreamingDeformatter = streamingDeformatter;
	}

	/// <summary>
	/// Gets the streaming deformatter underlying this deformatter.
	/// </summary>
	public StreamingDeformatter StreamingDeformatter { get; }

	/// <inheritdoc cref="StreamingDeformatter.Encoding"/>
	public Encoding Encoding => this.StreamingDeformatter.Encoding;

	/// <summary><inheritdoc cref="StreamingDeformatter.TryAdvanceToNextElement(ref Reader, out bool)" path="/summary"/></summary>
	/// <param name="reader"><inheritdoc cref="StreamingDeformatter.TryReadNull(ref Reader)" path="/param[@name='reader']"/></param>
	/// <returns><see langword="true" /> if there is another element in the collection; otherwise <see langword="false" />.</returns>
	/// <inheritdoc cref="StreamingDeformatter.TryAdvanceToNextElement(ref Reader, out bool)" path="/remarks"/>
	/// <exception cref="EndOfStreamException">Thrown when there is insufficient buffer to decode the next token.</exception>
	public bool TryAdvanceToNextElement(ref Reader reader)
	{
		switch (this.StreamingDeformatter.TryAdvanceToNextElement(ref reader, out bool hasAnotherElement))
		{
			case DecodeResult.Success:
				return hasAnotherElement;
			case DecodeResult.TokenMismatch:
				throw this.StreamingDeformatter.ThrowInvalidCode(reader);
			case DecodeResult.InsufficientBuffer:
			case DecodeResult.EmptyBuffer:
				throw ThrowNotEnoughBytesException();
			default:
				throw ThrowUnreachable();
		}
	}

	/// <summary><inheritdoc cref="StreamingDeformatter.TryReadNull(ref Reader)" path="/summary"/></summary>
	/// <param name="reader"><inheritdoc cref="StreamingDeformatter.TryReadNull(ref Reader)" path="/param[@name='reader']"/></param>
	/// <returns><see langword="true" /> if the next token was <see cref="TokenType.Null"/>; otherwise <see langword="false" />.</returns>
	/// <remarks>
	/// The reader is only advanced if the token was <see cref="TokenType.Null"/>.
	/// </remarks>
	/// <exception cref="EndOfStreamException">Thrown when there is insufficient buffer to decode the next token.</exception>
	public bool TryReadNull(ref Reader reader)
	{
		switch (this.StreamingDeformatter.TryReadNull(ref reader))
		{
			case DecodeResult.Success:
				return true;
			case DecodeResult.InsufficientBuffer:
			case DecodeResult.EmptyBuffer:
				throw ThrowNotEnoughBytesException();
			default:
				return false;
		}
	}

	/// <summary>
	/// Reads a <see cref="TokenType.Null"/> token from the reader.
	/// </summary>
	/// <param name="reader"><inheritdoc cref="StreamingDeformatter.TryReadNull(ref Reader)" path="/param[@name='reader']"/></param>
	/// <inheritdoc cref="TryReadNull(ref Reader)" path="/exception" />
	/// <exception cref="SerializationException">Thrown when the next token is not <see cref="TokenType.Null"/>.</exception>
	public void ReadNull(ref Reader reader)
	{
		if (!this.TryReadNull(ref reader))
		{
			throw this.StreamingDeformatter.ThrowInvalidCode(reader);
		}
	}

	/// <summary><inheritdoc cref="StreamingDeformatter.TryReadStartVector(ref Reader, out int?)" path="/summary"/></summary>
	/// <param name="reader"><inheritdoc cref="StreamingDeformatter.TryReadNull(ref Reader)" path="/param[@name='reader']"/></param>
	/// <returns>The number of elements in the vector, if the format prefixed the length.</returns>
	/// <exception cref="EndOfStreamException">
	/// Thrown when there is insufficient buffer to decode the next token or the buffer is obviously insufficient to store all the elements in the vector.
	/// Use <see cref="TryReadStartVector(ref Reader, out int?)"/> instead to avoid throwing due to insufficient buffer.
	/// </exception>
	/// <exception cref="SerializationException">Thrown when the next token is not <see cref="TokenType.Vector"/>.</exception>
	public int? ReadStartVector(ref Reader reader)
	{
		ThrowInsufficientBufferUnless(this.TryReadStartVector(ref reader, out int? count));

		if (count is not null)
		{
			// Protect against corrupted or mischievous data that may lead to allocating way too much memory.
			// We allow for each primitive to be the minimal 1 byte in size.
			// Formatters that know each element is larger can optionally add a stronger check.
			ThrowInsufficientBufferUnless(reader.SequenceReader.Remaining >= count);
		}

		return count;
	}

	/// <summary><inheritdoc cref="StreamingDeformatter.TryReadStartVector(ref Reader, out int?)" path="/summary"/></summary>
	/// <param name="reader"><inheritdoc cref="StreamingDeformatter.TryReadNull(ref Reader)" path="/param[@name='reader']"/></param>
	/// <param name="count">Receives the number of elements in the vector, if the read is successful.</param>
	/// <returns>
	/// <see langword="true"/> when the read operation is successful; <see langword="false"/> when the buffer is empty or insufficient.
	/// </returns>
	/// <exception cref="SerializationException">Thrown when the next token is not <see cref="TokenType.Vector"/>.</exception>
	public bool TryReadStartVector(ref Reader reader, out int? count)
	{
		switch (this.StreamingDeformatter.TryReadStartVector(ref reader, out count))
		{
			case DecodeResult.Success:
				return true;
			case DecodeResult.TokenMismatch:
				throw this.StreamingDeformatter.ThrowInvalidCode(reader);
			case DecodeResult.EmptyBuffer:
			case DecodeResult.InsufficientBuffer:
				return false;
			default:
				throw ThrowUnreachable();
		}
	}

	/// <summary><inheritdoc cref="StreamingDeformatter.TryReadStartMap(ref Reader, out int?)" path="/summary"/></summary>
	/// <param name="reader"><inheritdoc cref="StreamingDeformatter.TryReadNull(ref Reader)" path="/param[@name='reader']"/></param>
	/// <returns>The number of elements in the map, if the format prefixed the size.</returns>
	/// <exception cref="EndOfStreamException">
	/// Thrown when there is insufficient buffer to decode the next token or the buffer is obviously insufficient to store all the elements in the map.
	/// Use <see cref="TryReadStartMap(ref Reader, out int?)"/> instead to avoid throwing due to insufficient buffer.
	/// </exception>
	/// <exception cref="SerializationException">Thrown when the next token is not <see cref="TokenType.Map"/>.</exception>
	public int? ReadStartMap(ref Reader reader)
	{
		ThrowInsufficientBufferUnless(this.TryReadStartMap(ref reader, out int? count));

		if (count is not null)
		{
			// Protect against corrupted or mischievous data that may lead to allocating way too much memory.
			// We allow for each primitive to be the minimal 1 byte in size, and we have a key=value map, so that's 2 bytes.
			// Formatters that know each element is larger can optionally add a stronger check.
			ThrowInsufficientBufferUnless(reader.SequenceReader.Remaining >= count * 2);
		}

		return count;
	}

	/// <summary><inheritdoc cref="StreamingDeformatter.TryReadStartMap(ref Reader, out int?)" path="/summary"/></summary>
	/// <param name="reader"><inheritdoc cref="StreamingDeformatter.TryReadNull(ref Reader)" path="/param[@name='reader']"/></param>
	/// <param name="count">Receives the number of elements in the map, if the read is successful.</param>
	/// <returns>
	/// <see langword="true"/> when the read operation is successful; <see langword="false"/> when the buffer is empty or insufficient.
	/// </returns>
	/// <exception cref="SerializationException">Thrown when the next token is not <see cref="TokenType.Map"/>.</exception>
	public bool TryReadStartMap(ref Reader reader, out int? count)
	{
		switch (this.StreamingDeformatter.TryReadStartMap(ref reader, out count))
		{
			case DecodeResult.Success:
				return true;
			case DecodeResult.TokenMismatch:
				throw this.StreamingDeformatter.ThrowInvalidCode(reader);
			case DecodeResult.EmptyBuffer:
			case DecodeResult.InsufficientBuffer:
				return false;
			default:
				throw ThrowUnreachable();
		}
	}

	public void ReadMapKeyValueSeparator(ref Reader reader)
	{
		switch (this.StreamingDeformatter.TryReadMapKeyValueSeparator(ref reader))
		{
			case DecodeResult.Success:
				return;
			case DecodeResult.TokenMismatch:
				throw this.StreamingDeformatter.ThrowInvalidCode(reader);
			case DecodeResult.EmptyBuffer:
			case DecodeResult.InsufficientBuffer:
				throw ThrowNotEnoughBytesException();
			default:
				throw ThrowUnreachable();
		}
	}

	/// <summary><inheritdoc cref="StreamingDeformatter.TryRead(ref Reader, out bool)"/></summary>
	/// <param name="reader"><inheritdoc cref="StreamingDeformatter.TryReadNull(ref Reader)" path="/param[@name='reader']"/></param>
	/// <returns>The decoded value.</returns>
	/// <inheritdoc cref="TryReadNull(ref Reader)" path="/exception" />
	/// <exception cref="SerializationException">Thrown when the next token is not <see cref="TokenType.Boolean"/>.</exception>
	public bool ReadBoolean(ref Reader reader)
	{
		switch (this.StreamingDeformatter.TryRead(ref reader, out bool value))
		{
			case DecodeResult.Success:
				return value;
			case DecodeResult.TokenMismatch:
				throw this.StreamingDeformatter.ThrowInvalidCode(reader);
			case DecodeResult.EmptyBuffer:
			case DecodeResult.InsufficientBuffer:
				throw ThrowNotEnoughBytesException();
			default:
				throw ThrowUnreachable();
		}
	}

	/// <summary><inheritdoc cref="StreamingDeformatter.TryRead(ref Reader, out char)"/></summary>
	/// <param name="reader"><inheritdoc cref="StreamingDeformatter.TryReadNull(ref Reader)" path="/param[@name='reader']"/></param>
	/// <returns>The decoded value.</returns>
	/// <inheritdoc cref="TryReadNull(ref Reader)" path="/exception" />
	/// <exception cref="SerializationException">Thrown when the next token is not <see cref="TokenType.Integer"/>.</exception>
	public char ReadChar(ref Reader reader)
	{
		switch (this.StreamingDeformatter.TryRead(ref reader, out char value))
		{
			case DecodeResult.Success:
				return value;
			case DecodeResult.TokenMismatch:
				throw this.StreamingDeformatter.ThrowInvalidCode(reader);
			case DecodeResult.EmptyBuffer:
			case DecodeResult.InsufficientBuffer:
				throw ThrowNotEnoughBytesException();
			default:
				throw ThrowUnreachable();
		}
	}

	/// <summary><inheritdoc cref="StreamingDeformatter.TryRead(ref Reader, out float)"/></summary>
	/// <param name="reader"><inheritdoc cref="StreamingDeformatter.TryReadNull(ref Reader)" path="/param[@name='reader']"/></param>
	/// <returns>The decoded value.</returns>
	/// <inheritdoc cref="TryReadNull(ref Reader)" path="/exception" />
	/// <exception cref="SerializationException">Thrown when the next token is not <see cref="TokenType.Float"/>.</exception>
	public unsafe float ReadSingle(ref Reader reader)
	{
		switch (this.StreamingDeformatter.TryRead(ref reader, out float value))
		{
			case DecodeResult.Success:
				return value;
				throw this.StreamingDeformatter.ThrowInvalidCode(reader);
			case DecodeResult.EmptyBuffer:
			case DecodeResult.InsufficientBuffer:
				throw ThrowNotEnoughBytesException();
			default:
				throw ThrowUnreachable();
		}
	}

	/// <summary><inheritdoc cref="StreamingDeformatter.TryRead(ref Reader, out double)"/></summary>
	/// <param name="reader"><inheritdoc cref="StreamingDeformatter.TryReadNull(ref Reader)" path="/param[@name='reader']"/></param>
	/// <returns>The decoded value.</returns>
	/// <inheritdoc cref="TryReadNull(ref Reader)" path="/exception" />
	/// <exception cref="SerializationException">Thrown when the next token is not <see cref="TokenType.Float"/>.</exception>
	public unsafe double ReadDouble(ref Reader reader)
	{
		switch (this.StreamingDeformatter.TryRead(ref reader, out double value))
		{
			case DecodeResult.Success:
				return value;
				throw this.StreamingDeformatter.ThrowInvalidCode(reader);
			case DecodeResult.EmptyBuffer:
			case DecodeResult.InsufficientBuffer:
				throw ThrowNotEnoughBytesException();
			default:
				throw ThrowUnreachable();
		}
	}

	/// <summary><inheritdoc cref="StreamingDeformatter.TryRead(ref Reader, out string)"/></summary>
	/// <param name="reader"><inheritdoc cref="StreamingDeformatter.TryReadNull(ref Reader)" path="/param[@name='reader']"/></param>
	/// <returns>The decoded value.</returns>
	/// <inheritdoc cref="TryReadNull(ref Reader)" path="/exception" />
	/// <exception cref="SerializationException">Thrown when the next token is not <see cref="TokenType.String"/> or <see cref="TokenType.Null"/>.</exception>
	public string? ReadString(ref Reader reader)
	{
		switch (this.StreamingDeformatter.TryRead(ref reader, out string? value))
		{
			case DecodeResult.Success:
				return value;
			case DecodeResult.TokenMismatch:
				throw this.StreamingDeformatter.ThrowInvalidCode(reader);
			case DecodeResult.EmptyBuffer:
			case DecodeResult.InsufficientBuffer:
				throw ThrowNotEnoughBytesException();
			default:
				throw ThrowUnreachable();
		}
	}

	/// <summary><inheritdoc cref="StreamingDeformatter.TryReadBinary(ref Reader, out ReadOnlySequence{byte})"/></summary>
	/// <param name="reader"><inheritdoc cref="StreamingDeformatter.TryReadNull(ref Reader)" path="/param[@name='reader']"/></param>
	/// <returns>The decoded value.</returns>
	/// <inheritdoc cref="TryReadNull(ref Reader)" path="/exception" />
	/// <exception cref="SerializationException">Thrown when the next token is not <see cref="TokenType.Binary"/> or <see cref="TokenType.Null"/>.</exception>
	public ReadOnlySequence<byte>? ReadBytes(ref Reader reader)
	{
		switch (this.StreamingDeformatter.TryReadBinary(ref reader, out ReadOnlySequence<byte> value))
		{
			case DecodeResult.Success:
				return value;
			case DecodeResult.TokenMismatch:
				if (this.TryReadNull(ref reader))
				{
					return null;
				}

				throw this.StreamingDeformatter.ThrowInvalidCode(reader);
			case DecodeResult.EmptyBuffer:
			case DecodeResult.InsufficientBuffer:
				throw ThrowNotEnoughBytesException();
			default:
				throw ThrowUnreachable();
		}
	}

	/// <summary><inheritdoc cref="StreamingDeformatter.TryReadStringSequence(ref Reader, out ReadOnlySequence{byte})"/></summary>
	/// <param name="reader"><inheritdoc cref="StreamingDeformatter.TryReadNull(ref Reader)" path="/param[@name='reader']"/></param>
	/// <returns>The decoded value.</returns>
	/// <inheritdoc cref="TryReadNull(ref Reader)" path="/exception" />
	/// <exception cref="SerializationException">Thrown when the next token is not <see cref="TokenType.String"/> or <see cref="TokenType.Null"/>.</exception>
	public ReadOnlySequence<byte>? ReadStringSequence(ref Reader reader)
	{
		switch (this.StreamingDeformatter.TryReadStringSequence(ref reader, out ReadOnlySequence<byte> value))
		{
			case DecodeResult.Success:
				return value;
			case DecodeResult.TokenMismatch:
				if (this.TryReadNull(ref reader))
				{
					return null;
				}

				throw this.StreamingDeformatter.ThrowInvalidCode(reader);
			case DecodeResult.EmptyBuffer:
			case DecodeResult.InsufficientBuffer:
				throw ThrowNotEnoughBytesException();
			default:
				throw ThrowUnreachable();
		}
	}

	/// <summary>Reads a string from the data stream (with appropriate envelope) without decoding it.</summary>
	/// <param name="reader"><inheritdoc cref="StreamingDeformatter.TryReadNull(ref Reader)" path="/param[@name='reader']"/></param>
	/// <returns>The decoded value.</returns>
	/// <inheritdoc cref="TryReadNull(ref Reader)" path="/exception" />
	/// <exception cref="SerializationException">Thrown when the next token is not <see cref="TokenType.String"/>.</exception>
	/// <remarks>
	/// <para>
	/// This method will often be alloc-free if the string is contiguous in the buffer.
	/// If the string is <em>not</em> contiguous, then the result will be a copy of the string.
	/// Use <see cref="TryReadStringSpan(ref Reader, out ReadOnlySpan{byte})"/> to avoid allocating a copy of the string.
	/// </para>
	/// <para>
	/// <see cref="DecodeResult.TokenMismatch"/> is returned for <see cref="TokenType.Null"/> or any other non-<see cref="TokenType.String"/> token.
	/// </para>
	/// </remarks>
	public ReadOnlySpan<byte> ReadStringSpan(ref Reader reader)
	{
		switch (this.StreamingDeformatter.TryReadStringSpan(ref reader, out bool contiguous, out ReadOnlySpan<byte> span))
		{
			case DecodeResult.Success:
				return contiguous ? span : this.ReadStringSequence(ref reader)!.Value.ToArray();
			case DecodeResult.TokenMismatch:
				throw this.StreamingDeformatter.ThrowInvalidCode(reader);
			case DecodeResult.EmptyBuffer:
			case DecodeResult.InsufficientBuffer:
				throw ThrowNotEnoughBytesException();
			default:
				throw ThrowUnreachable();
		}
	}

	/// <summary>Reads a string from the data stream (with appropriate envelope) without decoding it.</summary>
	/// <param name="reader"><inheritdoc cref="StreamingDeformatter.TryReadNull(ref Reader)" path="/param[@name='reader']"/></param>
	/// <param name="span">Receives the encoded bytes of the string <em>if</em> they were contiguous in memory.</param>
	/// <returns><see langword="true" /> if the string was contiguous in memory; <see langword="false" /> if the string was not contiguous.</returns>
	/// <remarks>
	/// When <see langword="false"/>, the caller should use <see cref="ReadStringSequence(ref Reader)"/> to read the string
	/// without allocations or <see cref="ReadStringSpan(ref Reader)"/> with allocations.
	/// </remarks>
	/// <inheritdoc cref="TryReadNull(ref Reader)" path="/exception" />
	/// <exception cref="SerializationException">Thrown when the next token is not <see cref="TokenType.String"/>.</exception>
	public bool TryReadStringSpan(scoped ref Reader reader, out ReadOnlySpan<byte> span)
	{
		switch (this.StreamingDeformatter.TryReadStringSpan(ref reader, out bool contiguous, out span))
		{
			case DecodeResult.Success:
				return contiguous;
			case DecodeResult.TokenMismatch:
				throw this.StreamingDeformatter.ThrowInvalidCode(reader);
			case DecodeResult.EmptyBuffer:
			case DecodeResult.InsufficientBuffer:
				throw ThrowNotEnoughBytesException();
			default:
				throw ThrowUnreachable();
		}
	}

	/// <summary><inheritdoc cref="StreamingDeformatter.TryReadRaw(ref Reader, SerializationContext, out ReadOnlySequence{byte})" path="/summary"/></summary>
	/// <param name="reader"><inheritdoc cref="StreamingDeformatter.TryReadNull(ref Reader)" path="/param[@name='reader']"/></param>
	/// <param name="context"><inheritdoc cref="StreamingDeformatter.TryReadRaw(ref Reader, SerializationContext, out ReadOnlySequence{byte})" path="/param[@name='context']" /></param>
	/// <returns>The raw bytes of the next structure.</returns>
	/// <inheritdoc cref="TryReadNull(ref Reader)" path="/exception" />
	/// <exception cref="SerializationException">Thrown when the next structure contains unrecognized or invalid tokens.</exception>
	public ReadOnlySequence<byte> ReadRaw(ref Reader reader, SerializationContext context)
	{
		switch (this.StreamingDeformatter.TryReadRaw(ref reader, context, out ReadOnlySequence<byte> value))
		{
			case DecodeResult.Success:
				return value;
			case DecodeResult.TokenMismatch:
				throw this.StreamingDeformatter.ThrowInvalidCode(reader);
			case DecodeResult.EmptyBuffer:
			case DecodeResult.InsufficientBuffer:
				throw ThrowNotEnoughBytesException();
			default:
				throw ThrowUnreachable();
		}
	}

	/// <summary><inheritdoc cref="StreamingDeformatter.TryReadRaw(ref Reader, long, out ReadOnlySequence{byte})" path="/summary"/></summary>
	/// <param name="reader"><inheritdoc cref="StreamingDeformatter.TryReadNull(ref Reader)" path="/param[@name='reader']"/></param>
	/// <param name="length"><inheritdoc cref="StreamingDeformatter.TryReadRaw(ref Reader, long, out ReadOnlySequence{byte})" path="/param[@name='length']" /></param>
	/// <returns>The raw bytes of the next structure.</returns>
	/// <inheritdoc cref="TryReadNull(ref Reader)" path="/exception" />
	/// <exception cref="SerializationException">Thrown when the next structure contains unrecognized or invalid tokens.</exception>
	public ReadOnlySequence<byte> ReadRaw(ref Reader reader, long length)
	{
		switch (this.StreamingDeformatter.TryReadRaw(ref reader, length, out ReadOnlySequence<byte> value))
		{
			case DecodeResult.Success:
				return value;
			case DecodeResult.TokenMismatch:
				throw this.StreamingDeformatter.ThrowInvalidCode(reader);
			case DecodeResult.EmptyBuffer:
			case DecodeResult.InsufficientBuffer:
				throw ThrowNotEnoughBytesException();
			default:
				throw ThrowUnreachable();
		}
	}

	/// <summary>
	/// Advances the reader to the next structure in the data stream.
	/// </summary>
	/// <param name="reader"><inheritdoc cref="StreamingDeformatter.TryReadNull(ref Reader)" path="/param[@name='reader']"/></param>
	/// <param name="context">The serialization context. Used for the stack guard.</param>
	/// <remarks>
	/// <para>
	/// The entire structure is skipped, including content of maps or vectors, or any other type with payloads.
	/// To get the raw data sequence that was skipped, use <see cref="ReadRaw(ref Reader, SerializationContext)"/> instead.
	/// </para>
	/// </remarks>
	/// <exception cref="EndOfStreamException">Thrown when the next structure is not wholly contained in the buffer. The position of the reader at this point is undefined.</exception>
	public void Skip(ref Reader reader, SerializationContext context) => ThrowInsufficientBufferUnless(this.TrySkip(ref reader, ref context));

	/// <summary>
	/// Retrieves the type of the next token without advancing the reader.
	/// </summary>
	/// <param name="reader"><inheritdoc cref="StreamingDeformatter.TryReadNull(ref Reader)" path="/param[@name='reader']"/></param>
	/// <returns>The type of the next token.</returns>
	/// <inheritdoc cref="TryReadNull(ref Reader)" path="/exception" />
	/// <exception cref="SerializationException">Thrown when the next token is unrecognized.</exception>
	public TokenType PeekNextTypeCode(in Reader reader)
	{
		switch (this.StreamingDeformatter.TryPeekNextTypeCode(reader, out TokenType typeCode))
		{
			case DecodeResult.Success:
				return typeCode;
			case DecodeResult.TokenMismatch:
				throw this.StreamingDeformatter.ThrowInvalidCode(reader);
			case DecodeResult.EmptyBuffer:
			case DecodeResult.InsufficientBuffer:
				throw ThrowNotEnoughBytesException();
			default:
				throw ThrowUnreachable();
		}
	}

	/// <summary>
	/// Checks whether the next token (which must be an <see cref="TokenType.Integer"/>) is an integer which <em>may</em> be negative without advancing the reader.
	/// </summary>
	/// <param name="reader"><inheritdoc cref="StreamingDeformatter.TryReadNull(ref Reader)" path="/param[@name='reader']"/></param>
	/// <returns><see langword="true" /> when the integer is encoded as a signed integer; <see langword="false" /> otherwise.</returns>
	/// <remarks>
	/// This is useful for readers that wish to know whether they should use <see cref="ReadInt64(ref Reader)"/>
	/// instead of <see cref="ReadUInt64(ref Reader)"/> to read a token typed as <see cref="TokenType.Integer"/>.
	/// </remarks>
	/// <inheritdoc cref="TryReadNull(ref Reader)" path="/exception" />
	/// <exception cref="SerializationException">Thrown when the next token is not an <see cref="TokenType.Integer"/>.</exception>
	public bool PeekIsSignedInteger(in Reader reader)
	{
		switch (this.StreamingDeformatter.TryPeekIsSignedInteger(reader, out bool signed))
		{
			case DecodeResult.Success:
				return signed;
			case DecodeResult.TokenMismatch:
				throw this.StreamingDeformatter.ThrowInvalidCode(reader);
			case DecodeResult.EmptyBuffer:
			case DecodeResult.InsufficientBuffer:
				throw ThrowNotEnoughBytesException();
			default:
				throw ThrowUnreachable();
		}
	}

	/// <summary>
	/// Checks whether the next token (which must be an <see cref="TokenType.Float"/>) is as encoded as a 32-bit float without advancing the reader.
	/// </summary>
	/// <param name="reader"><inheritdoc cref="StreamingDeformatter.TryReadNull(ref Reader)" path="/param[@name='reader']"/></param>
	/// <returns><see langword="true" /> when the integer is encoded as a signed integer; <see langword="false" /> otherwise.</returns>
	/// <remarks>
	/// This is useful for readers that wish to know whether they should use <see cref="ReadSingle(ref Reader)"/>
	/// instead of <see cref="ReadDouble(ref Reader)"/> to read a token typed as <see cref="TokenType.Float"/>.
	/// </remarks>
	/// <inheritdoc cref="TryReadNull(ref Reader)" path="/exception" />
	/// <exception cref="SerializationException">Thrown when the next token is not an <see cref="TokenType.Float"/>.</exception>
	public bool PeekIsFloat32(in Reader reader)
	{
		switch (this.StreamingDeformatter.TryPeekIsFloat32(reader, out bool float32))
		{
			case DecodeResult.Success:
				return float32;
			case DecodeResult.TokenMismatch:
				throw this.StreamingDeformatter.ThrowInvalidCode(reader);
			case DecodeResult.EmptyBuffer:
			case DecodeResult.InsufficientBuffer:
				throw ThrowNotEnoughBytesException();
			default:
				throw ThrowUnreachable();
		}
	}

	/// <summary>
	/// Throws <see cref="UnreachableException"/>.
	/// </summary>
	/// <returns>Nothing. This method always throws.</returns>
	[DoesNotReturn]
	internal static Exception ThrowUnreachable() => throw new UnreachableException();

	/// <summary>
	/// Advances the reader to the next structure in the data stream.
	/// </summary>
	/// <param name="reader"><inheritdoc cref="StreamingDeformatter.TryReadNull(ref Reader)" path="/param[@name='reader']"/></param>
	/// <param name="context">The serialization context. Used for the stack guard.</param>
	/// <returns>
	/// <see langword="true"/> if the entire structure beginning at the current <see cref="Reader.Position"/> is found in the <see cref="Reader.Sequence"/>;
	/// <see langword="false"/> otherwise.
	/// </returns>
	/// <remarks>
	/// <para>
	/// The entire structure is skipped, including content of maps or vectors, or any other type with payloads.
	/// To get the raw data sequence that was skipped, use <see cref="ReadRaw(ref Reader, SerializationContext)"/> instead.
	/// </para>
	/// <para>
	/// The reader position is changed when the return value is <see langword="true"/>.
	/// The reader position and the <paramref name="context"/> may also be changed when the return value is <see langword="false"/>,
	/// such that after fetching more bytes, a follow-up call to this method can resume skipping.
	/// </para>
	/// </remarks>
	internal bool TrySkip(ref Reader reader, ref SerializationContext context)
	{
		switch (this.StreamingDeformatter.TrySkip(ref reader, ref context))
		{
			case DecodeResult.Success:
				return true;
			case DecodeResult.TokenMismatch:
				throw this.StreamingDeformatter.ThrowInvalidCode(reader);
			case DecodeResult.EmptyBuffer:
			case DecodeResult.InsufficientBuffer:
				return false;
			default:
				throw ThrowUnreachable();
		}
	}

	/// <summary>
	/// Throws <see cref="EndOfStreamException"/> if a condition is false.
	/// </summary>
	/// <param name="condition">A boolean value.</param>
	/// <exception cref="EndOfStreamException">Thrown if <paramref name="condition"/> is <see langword="false"/>.</exception>
	protected static void ThrowInsufficientBufferUnless(bool condition)
	{
		if (!condition)
		{
			ThrowNotEnoughBytesException();
		}
	}

	/// <summary>
	/// Throws <see cref="EndOfStreamException"/> indicating that there aren't enough bytes remaining in the buffer to read the next token.
	/// </summary>
	/// <returns>Nothing. This method always throws.</returns>
	[DoesNotReturn]
	protected static EndOfStreamException ThrowNotEnoughBytesException() => throw new EndOfStreamException();
}
