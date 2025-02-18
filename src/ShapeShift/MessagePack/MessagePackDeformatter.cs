// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ShapeShift.MessagePack;

/// <summary>
/// A <see cref="Deformatter"/>-derived type that adds msgpack-specific read functions.
/// </summary>
public class MessagePackDeformatter : Deformatter
{
	/// <summary>
	/// Gets the default instance of <see cref="MessagePackDeformatter"/>, which wraps a <see cref="MessagePackStreamingDeformatter.Default"/> instance of <see cref="MessagePackStreamingDeformatter"/>.
	/// </summary>
	public static readonly MessagePackDeformatter Default = new(MessagePackStreamingDeformatter.Default);

	/// <summary>
	/// Initializes a new instance of the <see cref="MessagePackDeformatter"/> class.
	/// </summary>
	/// <param name="streamingDeformatter">The streaming deformatter to wrap.</param>
	public MessagePackDeformatter(MessagePackStreamingDeformatter streamingDeformatter)
		: base(streamingDeformatter)
	{
	}

	/// <inheritdoc cref="Deformatter.StreamingDeformatter"/>
	public new MessagePackStreamingDeformatter StreamingDeformatter => (MessagePackStreamingDeformatter)base.StreamingDeformatter;

	/// <summary>
	/// Gets the next byte in the buffer which, if the reader is properly positioned at the start of a token, can identify the next token type.
	/// </summary>
	/// <param name="reader"><inheritdoc cref="Deformatter.PeekNextTypeCode(in Reader)" path="/param[@name='reader']"/></param>
	/// <exception cref="EndOfStreamException">Thrown if the end of the sequence provided to the constructor is reached before the expected end of the data.</exception>
	/// <returns>The next byte in the buffer.</returns>
	/// <remarks>
	/// See <see cref="MessagePackCode"/> for valid codes and ranges.
	/// </remarks>
	public byte PeekNextCode(in Reader reader)
	{
		return this.StreamingDeformatter.TryPeekNextCode(reader, out byte code) switch
		{
			DecodeResult.Success => code,
			DecodeResult.EmptyBuffer or DecodeResult.InsufficientBuffer => throw ThrowNotEnoughBytesException(),
			_ => throw ThrowUnreachable(),
		};
	}

	/// <summary>
	/// Gets the type of the next MessagePack token.
	/// </summary>
	/// <param name="reader"><inheritdoc cref="Deformatter.PeekNextTypeCode(in Reader)" path="/param[@name='reader']"/></param>
	/// <returns>The classification of the next token.</returns>
	public MessagePackType PeekNextMessagePackType(in Reader reader) => MessagePackCode.ToMessagePackType(this.PeekNextCode(reader));

	/// <summary>
	/// Reads an <see cref="ExtensionHeader"/> from the stream.
	/// </summary>
	/// <param name="reader"><inheritdoc cref="Deformatter.PeekNextTypeCode(in Reader)" path="/param[@name='reader']"/></param>
	/// <returns>The extension header.</returns>
	/// <inheritdoc cref="Deformatter.TryReadNull(ref Reader)" path="/exception" />
	/// <exception cref="SerializationException">Thrown when the next token is not an extension.</exception>
	public ExtensionHeader ReadExtensionHeader(ref Reader reader)
	{
		switch (this.StreamingDeformatter.TryRead(ref reader, out ExtensionHeader value))
		{
			case DecodeResult.Success:
				// Protect against corrupted or mischievous data that may lead to allocating way too much memory.
				ThrowInsufficientBufferUnless(reader.Remaining >= value.Length);
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
	/// Reads an <see cref="Extension"/> (its header and data) from the stream.
	/// </summary>
	/// <param name="reader"><inheritdoc cref="Deformatter.PeekNextTypeCode(in Reader)" path="/param[@name='reader']"/></param>
	/// <returns>The extension.</returns>
	/// <inheritdoc cref="Deformatter.TryReadNull(ref Reader)" path="/exception" />
	/// <exception cref="SerializationException">Thrown when the next token is not an extension.</exception>
	public Extension ReadExtension(ref Reader reader)
	{
		switch (this.StreamingDeformatter.TryRead(ref reader, out Extension value))
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
	/// Reads a <see cref="DateTime"/> value from the stream.
	/// </summary>
	/// <param name="reader"><inheritdoc cref="Deformatter.PeekNextTypeCode(in Reader)" path="/param[@name='reader']"/></param>
	/// <returns>The decoded value.</returns>
	/// <inheritdoc cref="Deformatter.TryReadNull(ref Reader)" path="/exception" />
	/// <exception cref="SerializationException">Thrown when the next token is not a <see cref="DateTime"/>.</exception>
	public DateTime ReadDateTime(ref Reader reader)
	{
		switch (this.StreamingDeformatter.TryRead(ref reader, out DateTime value))
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
}
