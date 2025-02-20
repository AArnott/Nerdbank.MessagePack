// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace ShapeShift.Converters;

/// <summary>
/// An abstract base class for streaming deformatters.
/// </summary>
public abstract record StreamingDeformatter
{
	/// <summary>
	/// A value indicating whether no more bytes can be expected once we reach the end of the current buffer.
	/// </summary>
	private bool eof;

	/// <inheritdoc cref="Formatter.FormatName" />
	public abstract string FormatName { get; }

	/// <inheritdoc cref="Formatter.Encoding" />
	public abstract Encoding Encoding { get; }

	/// <summary>
	/// Decodes the type of the next token in the data stream.
	/// </summary>
	/// <param name="reader"><inheritdoc cref="TryReadNull(ref Reader)" path="/param[@name='reader']" /></param>
	/// <param name="typeCode">Receives the type of the next token, if the read is successful.</param>
	/// <returns>
	/// <see cref="DecodeResult.Success"/> if the next token is of a type recognized by the deformatter.
	/// <see cref="DecodeResult.TokenMismatch"/> if the token is invalid or unrecognized by the deformatter.
	/// Other values for incomplete buffers.
	/// </returns>
	public abstract DecodeResult TryPeekNextTypeCode(in Reader reader, out TokenType typeCode);

	/// <summary>
	/// Advances the reader to the next element in a collection or beyond the termination marker.
	/// </summary>
	/// <param name="reader"><inheritdoc cref="TryReadNull(ref Reader)" path="/param[@name='reader']" /></param>
	/// <param name="hasAnotherElement">Receives a value indicating whether another element remains in the collection.</param>
	/// <returns>
	/// <see cref="DecodeResult.Success"/> if the next token was a separator or termination marker.
	/// <see cref="DecodeResult.TokenMismatch"/> for any other token.
	/// Other error codes if the buffer is incomplete.
	/// </returns>
	/// <remarks>
	/// This method must <em>not</em> be called when the collection has a prefixed length.
	/// When a collection does not have a prefixed length, this method must be called before each element read
	/// and necessarily after the last item (in order to discover that it is indeed the last element, and to
	/// advance the reader past the termination marker).
	/// </remarks>
	public abstract DecodeResult TryAdvanceToNextElement(ref Reader reader, out bool hasAnotherElement);

	/// <summary>
	/// Reads the next token if it is <see cref="TokenType.Null"/>.
	/// </summary>
	/// <param name="reader">The reader.</param>
	/// <returns>
	/// <see cref="DecodeResult.Success"/> if the token was nil and was read,
	/// <see cref="DecodeResult.TokenMismatch"/> if the token was not nil,
	/// or other error codes if the buffer is incomplete.
	/// </returns>
	public abstract DecodeResult TryReadNull(ref Reader reader);

	/// <summary>
	/// Reads the next token if it is <see cref="TokenType.Null"/>.
	/// </summary>
	/// <param name="reader"><inheritdoc cref="TryReadNull(ref Reader)" path="/param[@name='reader']" /></param>
	/// <param name="isNull">A value indicating whether the next token was nil.</param>
	/// <returns>
	/// <see cref="DecodeResult.Success"/> if the next token can be decoded whether or not it was <see langword="null" />,
	/// or other error codes if the buffer is incomplete.
	/// </returns>
	public abstract DecodeResult TryReadNull(ref Reader reader, out bool isNull);

	/// <summary>
	/// Reads an array header from the data stream.
	/// </summary>
	/// <param name="reader"><inheritdoc cref="TryReadNull(ref Reader)" path="/param[@name='reader']" /></param>
	/// <param name="length">The number of elements in the array, if the read was successful and the format prefixed the length.</param>
	/// <returns>The success or error code.</returns>
	public abstract DecodeResult TryReadStartVector(ref Reader reader, out int? length);

	/// <summary>
	/// Reads a map header from the data stream.
	/// </summary>
	/// <param name="reader"><inheritdoc cref="TryReadNull(ref Reader)" path="/param[@name='reader']" /></param>
	/// <param name="count">The number of elements in the map, if the read was successful and the format prefixed the size.</param>
	/// <returns>The success or error code.</returns>
	public abstract DecodeResult TryReadStartMap(ref Reader reader, out int? count);

	public abstract DecodeResult TryReadMapKeyValueSeparator(ref Reader reader);

	/// <summary>
	/// Reads a binary sequence (with its envelope) from the data stream.
	/// </summary>
	/// <param name="reader"><inheritdoc cref="TryReadNull(ref Reader)" path="/param[@name='reader']" /></param>
	/// <param name="value">The byte sequence if the read was successful.</param>
	/// <returns>The success or error code.</returns>
	public abstract DecodeResult TryReadBinary(ref Reader reader, out ReadOnlySequence<byte> value);

	/// <summary>
	/// Reads a <see cref="bool"/> value from the data stream.
	/// </summary>
	/// <param name="reader"><inheritdoc cref="TryReadNull(ref Reader)" path="/param[@name='reader']" /></param>
	/// <param name="value">The decoded value if the read was successful.</param>
	/// <returns>The success or error code.</returns>
	public abstract DecodeResult TryRead(ref Reader reader, out bool value);

	/// <summary>
	/// Reads a <see cref="char"/> value from the data stream.
	/// </summary>
	/// <param name="reader"><inheritdoc cref="TryReadNull(ref Reader)" path="/param[@name='reader']" /></param>
	/// <param name="value"><inheritdoc cref="TryRead(ref Reader, out bool)" path="/param[@name='value']" /></param>
	/// <returns><inheritdoc cref="TryRead(ref Reader, out bool)" path="/returns" /></returns>
	public abstract DecodeResult TryRead(ref Reader reader, out char value);

	/// <summary>
	/// Reads a <see cref="sbyte"/> value from the data stream.
	/// </summary>
	/// <param name="reader"><inheritdoc cref="TryReadNull(ref Reader)" path="/param[@name='reader']" /></param>
	/// <param name="value"><inheritdoc cref="TryRead(ref Reader, out bool)" path="/param[@name='value']" /></param>
	/// <returns><inheritdoc cref="TryRead(ref Reader, out bool)" path="/returns" /></returns>
	public abstract DecodeResult TryRead(ref Reader reader, out sbyte value);

	/// <summary>
	/// Reads a <see cref="short"/> value from the data stream.
	/// </summary>
	/// <param name="reader"><inheritdoc cref="TryReadNull(ref Reader)" path="/param[@name='reader']" /></param>
	/// <param name="value"><inheritdoc cref="TryRead(ref Reader, out bool)" path="/param[@name='value']" /></param>
	/// <returns><inheritdoc cref="TryRead(ref Reader, out bool)" path="/returns" /></returns>
	public abstract DecodeResult TryRead(ref Reader reader, out short value);

	/// <summary>
	/// Reads a <see cref="int"/> value from the data stream.
	/// </summary>
	/// <param name="reader"><inheritdoc cref="TryReadNull(ref Reader)" path="/param[@name='reader']" /></param>
	/// <param name="value"><inheritdoc cref="TryRead(ref Reader, out bool)" path="/param[@name='value']" /></param>
	/// <returns><inheritdoc cref="TryRead(ref Reader, out bool)" path="/returns" /></returns>
	public abstract DecodeResult TryRead(ref Reader reader, out int value);

	/// <summary>
	/// Reads a <see cref="long"/> value from the data stream.
	/// </summary>
	/// <param name="reader"><inheritdoc cref="TryReadNull(ref Reader)" path="/param[@name='reader']" /></param>
	/// <param name="value"><inheritdoc cref="TryRead(ref Reader, out bool)" path="/param[@name='value']" /></param>
	/// <returns><inheritdoc cref="TryRead(ref Reader, out bool)" path="/returns" /></returns>
	public abstract DecodeResult TryRead(ref Reader reader, out long value);

	/// <summary>
	/// Reads a <see cref="byte"/> value from the data stream.
	/// </summary>
	/// <param name="reader"><inheritdoc cref="TryReadNull(ref Reader)" path="/param[@name='reader']" /></param>
	/// <param name="value"><inheritdoc cref="TryRead(ref Reader, out bool)" path="/param[@name='value']" /></param>
	/// <returns><inheritdoc cref="TryRead(ref Reader, out bool)" path="/returns" /></returns>
	public abstract DecodeResult TryRead(ref Reader reader, out byte value);

	/// <summary>
	/// Reads a <see cref="ushort"/> value from the data stream.
	/// </summary>
	/// <param name="reader"><inheritdoc cref="TryReadNull(ref Reader)" path="/param[@name='reader']" /></param>
	/// <param name="value"><inheritdoc cref="TryRead(ref Reader, out bool)" path="/param[@name='value']" /></param>
	/// <returns><inheritdoc cref="TryRead(ref Reader, out bool)" path="/returns" /></returns>
	public abstract DecodeResult TryRead(ref Reader reader, out ushort value);

	/// <summary>
	/// Reads a <see cref="uint"/> value from the data stream.
	/// </summary>
	/// <param name="reader"><inheritdoc cref="TryReadNull(ref Reader)" path="/param[@name='reader']" /></param>
	/// <param name="value"><inheritdoc cref="TryRead(ref Reader, out bool)" path="/param[@name='value']" /></param>
	/// <returns><inheritdoc cref="TryRead(ref Reader, out bool)" path="/returns" /></returns>
	public abstract DecodeResult TryRead(ref Reader reader, out uint value);

	/// <summary>
	/// Reads a <see cref="ulong"/> value from the data stream.
	/// </summary>
	/// <param name="reader"><inheritdoc cref="TryReadNull(ref Reader)" path="/param[@name='reader']" /></param>
	/// <param name="value"><inheritdoc cref="TryRead(ref Reader, out bool)" path="/param[@name='value']" /></param>
	/// <returns><inheritdoc cref="TryRead(ref Reader, out bool)" path="/returns" /></returns>
	public abstract DecodeResult TryRead(ref Reader reader, out ulong value);

	/// <summary>
	/// Reads a <see cref="float"/> value from the data stream.
	/// </summary>
	/// <param name="reader"><inheritdoc cref="TryReadNull(ref Reader)" path="/param[@name='reader']" /></param>
	/// <param name="value"><inheritdoc cref="TryRead(ref Reader, out bool)" path="/param[@name='value']" /></param>
	/// <returns><inheritdoc cref="TryRead(ref Reader, out bool)" path="/returns" /></returns>
	public abstract DecodeResult TryRead(ref Reader reader, out float value);

	/// <summary>
	/// Reads a <see cref="double"/> value from the data stream.
	/// </summary>
	/// <param name="reader"><inheritdoc cref="TryReadNull(ref Reader)" path="/param[@name='reader']" /></param>
	/// <param name="value"><inheritdoc cref="TryRead(ref Reader, out bool)" path="/param[@name='value']" /></param>
	/// <returns><inheritdoc cref="TryRead(ref Reader, out bool)" path="/returns" /></returns>
	public abstract DecodeResult TryRead(ref Reader reader, out double value);

	/// <summary>
	/// Reads a <see cref="string"/> or <see langword="null" /> value from the data stream.
	/// </summary>
	/// <param name="reader"><inheritdoc cref="TryReadNull(ref Reader)" path="/param[@name='reader']" /></param>
	/// <param name="value"><inheritdoc cref="TryRead(ref Reader, out bool)" path="/param[@name='value']" /></param>
	/// <returns><inheritdoc cref="TryRead(ref Reader, out bool)" path="/returns" /></returns>
	public abstract DecodeResult TryRead(ref Reader reader, out string? value);

	/// <summary>
	/// Reads a string from the data stream (with appropriate envelope) without decoding it.
	/// </summary>
	/// <param name="reader"><inheritdoc cref="TryReadNull(ref Reader)" path="/param[@name='reader']" /></param>
	/// <param name="value"><inheritdoc cref="TryRead(ref Reader, out bool)" path="/param[@name='value']" /></param>
	/// <returns><inheritdoc cref="TryRead(ref Reader, out bool)" path="/returns" /></returns>
	/// <remarks>
	/// <see cref="DecodeResult.TokenMismatch"/> is returned for <see cref="TokenType.Null"/> or any other non-<see cref="TokenType.String"/> token.
	/// </remarks>
	public abstract DecodeResult TryReadStringSequence(ref Reader reader, out ReadOnlySequence<byte> value);

	/// <summary>
	/// Reads a string from the data stream (with appropriate envelope) without decoding it,
	/// if the string is contiguous in memory.
	/// </summary>
	/// <param name="reader"><inheritdoc cref="TryReadNull(ref Reader)" path="/param[@name='reader']" /></param>
	/// <param name="contiguous">
	/// Receives a value indicating whether the string was present and contiguous in memory.
	/// When the result is <see cref="DecodeResult.Success"/> and this parameter is <see langword="false"/>,
	/// use <see cref="TryReadStringSequence(ref Reader, out ReadOnlySequence{byte})"/> instead to get the non-contiguous string.
	/// </param>
	/// <param name="value">
	/// The encoded bytes of the string <em>if</em> it is contiguous in memory.
	/// This condition is indicated by <paramref name="contiguous"/> being <see langword="true"/>.
	/// </param>
	/// <returns>
	/// <see cref="DecodeResult.Success"/> if the next token is a string and fully included in the buffer,
	/// whether or not it is contiguous in memory and <paramref name="value"/> was successfully initalized.
	/// Other error codes also apply.
	/// </returns>
	/// <remarks>
	/// <see cref="DecodeResult.TokenMismatch"/> is returned for <see cref="TokenType.Null"/> or any other non-<see cref="TokenType.String"/> token.
	/// </remarks>
	public abstract DecodeResult TryReadStringSpan(scoped ref Reader reader, out bool contiguous, out ReadOnlySpan<byte> value);

	/// <summary>
	/// Advances the reader past the next structure.
	/// </summary>
	/// <param name="reader"><inheritdoc cref="TryReadNull(ref Reader)" path="/param[@name='reader']" /></param>
	/// <param name="context">The context of the deserialization operation.</param>
	/// <returns>The success or error code.</returns>
	/// <remarks>
	/// The reader position is changed when the return value is <see cref="DecodeResult.Success"/>.
	/// The reader position and the <paramref name="context"/> may also be changed when the return value is <see cref="DecodeResult.InsufficientBuffer"/>,
	/// such that after fetching more bytes, a follow-up call to this method can resume skipping.
	/// </remarks>
	public abstract DecodeResult TrySkip(ref Reader reader, ref SerializationContext context);

	/// <summary>
	/// Reads the raw bytes of one structure from the stream without deformatting them.
	/// </summary>
	/// <param name="reader"><inheritdoc cref="TryReadNull(ref Reader)" path="/param[@name='reader']" /></param>
	/// <param name="context"><inheritdoc cref="TrySkip(ref Reader, ref SerializationContext)" path="/param[@name='context']"/></param>
	/// <param name="bytes">Receives the bytes of the next structure.</param>
	/// <returns><inheritdoc cref="TrySkip(ref Reader, ref SerializationContext)" path="/returns" /></returns>
	public virtual DecodeResult TryReadRaw(ref Reader reader, SerializationContext context, out ReadOnlySequence<byte> bytes)
	{
		SequencePosition initialPosition = reader.Position;
		DecodeResult result = this.TrySkip(ref reader, ref context);
		bytes = result == DecodeResult.Success ? reader.Sequence.Slice(initialPosition, reader.Position) : default;
		return result;
	}

	/// <summary>
	/// Reads a given number of bytes from the stream without deformatting them.
	/// </summary>
	/// <param name="reader"><inheritdoc cref="TryReadNull(ref Reader)" path="/param[@name='reader']" /></param>
	/// <param name="length">The number of bytes to read. This should always be the length of exactly one structure (e.g. scalar value, whole vector or map).</param>
	/// <param name="rawBytes">The bytes if the read was successful.</param>
	/// <returns>The success or error code.</returns>
	public abstract DecodeResult TryReadRaw(ref Reader reader, long length, out ReadOnlySequence<byte> rawBytes);

	/// <summary>
	/// Tests whether the next token is an integer which <em>may</em> be negative.
	/// </summary>
	/// <param name="reader"><inheritdoc cref="TryReadNull(ref Reader)" path="/param[@name='reader']" /></param>
	/// <param name="signed">When decoding is successful, receives a value indicating whether the integer may be negative.</param>
	/// <returns>Result of the decoding operation. <see cref="DecodeResult.TokenMismatch"/> if the next token is not an integer at all.</returns>
	/// <remarks>
	/// This is useful for readers that wish to know whether they should use <see cref="TryRead(ref Reader, out long)"/>
	/// instead of <see cref="TryRead(ref Reader, out ulong)"/> to read a token typed as <see cref="TokenType.Integer"/>.
	/// </remarks>
	public abstract DecodeResult TryPeekIsSignedInteger(in Reader reader, out bool signed);

	/// <summary>
	/// Tests whether the next token is encoded as a <see cref="float"/> (as opposed to a <see cref="double"/>).
	/// </summary>
	/// <param name="reader"><inheritdoc cref="TryReadNull(ref Reader)" path="/param[@name='reader']" /></param>
	/// <param name="float32">When decoding is successful, receives a value indicating whether the floating point number is encoded with only 32-bits.</param>
	/// <returns>Result of the decoding operation. <see cref="DecodeResult.TokenMismatch"/> if the next token is not a floating point number.</returns>
	/// <remarks>
	/// This is useful for readers that wish to know whether they should use <see cref="TryRead(ref Reader, out float)"/>
	/// instead of <see cref="TryRead(ref Reader, out double)"/> to read a token typed as <see cref="TokenType.Float"/>.
	/// </remarks>
	public abstract DecodeResult TryPeekIsFloat32(in Reader reader, out bool float32);

	/// <summary>
	/// Throws an <see cref="SerializationException"/> explaining an unexpected code was encountered.
	/// </summary>
	/// <param name="reader">The reader positioned at the unexpected token.</param>
	/// <returns>Nothing. This method always throws.</returns>
	[DoesNotReturn]
	protected internal abstract Exception ThrowInvalidCode(in Reader reader);

	/// <summary>
	/// Gets the error code to return when the buffer has insufficient bytes to finish a decode request.
	/// </summary>
	/// <param name="reader"><inheritdoc cref="TryReadNull(ref Reader)" path="/param[@name='reader']" /></param>
	/// <returns>Either <see cref="DecodeResult.EmptyBuffer"/> or <see cref="DecodeResult.InsufficientBuffer"/>.</returns>
	protected DecodeResult InsufficientBytes(in Reader reader) => this.eof && reader.SequenceReader.Sequence.IsEmpty ? DecodeResult.EmptyBuffer : DecodeResult.InsufficientBuffer;
}
