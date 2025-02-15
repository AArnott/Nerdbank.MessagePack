// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.PolySerializer.MessagePack;

public class MsgPackDeformatter : Deformatter
{
	public static readonly MsgPackDeformatter Default = new();

	private MsgPackDeformatter()
		: base(MsgPackStreamingDeformatter.Default)
	{
	}

	public new MsgPackStreamingDeformatter StreamingDeformatter => (MsgPackStreamingDeformatter)base.StreamingDeformatter;

	public MessagePackType PeekNextMessagePackType(in Reader reader) => MessagePackCode.ToMessagePackType(this.PeekNextCode(reader));

	public ExtensionHeader ReadExtensionHeader(ref Reader reader)
	{
		switch (this.StreamingDeformatter.TryRead(ref reader, out ExtensionHeader value))
		{
			case DecodeResult.Success:
				return value;
			case DecodeResult.TokenMismatch:
				throw this.ThrowInvalidCode(reader);
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
				throw this.ThrowInvalidCode(reader);
			case DecodeResult.EmptyBuffer:
			case DecodeResult.InsufficientBuffer:
				throw ThrowNotEnoughBytesException();
			default:
				throw ThrowUnreachable();
		}
	}
}
