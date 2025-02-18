// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.PolySerializer.MessagePack;

/// <summary>
/// A <see cref="Deformatter"/>-derived type that adds msgpack-specific read functions.
/// </summary>
public class MsgPackDeformatter : Deformatter
{
	public static readonly MsgPackDeformatter Default = new(MsgPackStreamingDeformatter.Default);

	/// <summary>
	/// Initializes a new instance of the <see cref="MsgPackDeformatter"/> class.
	/// </summary>
	/// <param name="streamingDeformatter">The streaming deformatter to wrap.</param>
	public MsgPackDeformatter(MsgPackStreamingDeformatter streamingDeformatter)
		: base(streamingDeformatter)
	{
	}

	public new MsgPackStreamingDeformatter StreamingDeformatter => (MsgPackStreamingDeformatter)base.StreamingDeformatter;

	/// <summary>
	/// Gets the type of the next MessagePack block.
	/// </summary>
	/// <exception cref="EndOfStreamException">Thrown if the end of the sequence provided to the constructor is reached before the expected end of the data.</exception>
	/// <remarks>
	/// See <see cref="TypeCode"/> for valid codes and ranges.
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

	public MessagePackType PeekNextMessagePackType(in Reader reader) => MessagePackCode.ToMessagePackType(this.PeekNextCode(reader));

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
