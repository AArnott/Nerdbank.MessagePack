// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Nerdbank.PolySerializer.MessagePack;

internal class MsgPackStreamingDeformatter : StreamingDeformatter
{
	internal static readonly Deformatter Deformatter = new(MsgPackStreamingDeformatter.Instance);

	internal static readonly MsgPackStreamingDeformatter Instance = new();

	private uint expectedRemainingStructures;

	private MsgPackStreamingDeformatter() { }

	public override string ToFormatName(byte code) => MessagePackCode.ToFormatName(code);

	public override DecodeResult TryReadArrayHeader(ref Reader reader, out int count)
	{
		DecodeResult readResult = MessagePackPrimitives.TryReadArrayHeader(reader.UnreadSpan, out uint uintCount, out int tokenSize);
		count = checked((int)uintCount);
		if (readResult == DecodeResult.Success)
		{
			this.Advance(ref reader, tokenSize, added: uintCount);
			return DecodeResult.Success;
		}

		return SlowPath(ref reader, this, readResult, ref count, ref tokenSize);

		static DecodeResult SlowPath(ref Reader reader, MsgPackStreamingDeformatter self, DecodeResult readResult, ref int count, ref int tokenSize)
		{
			switch (readResult)
			{
				case DecodeResult.Success:
					self.Advance(ref reader, tokenSize, added: unchecked((uint)count));
					return DecodeResult.Success;
				case DecodeResult.TokenMismatch:
					return DecodeResult.TokenMismatch;
				case DecodeResult.EmptyBuffer:
				case DecodeResult.InsufficientBuffer:
					Span<byte> buffer = stackalloc byte[tokenSize];
					if (reader.SequenceReader.TryCopyTo(buffer))
					{
						readResult = MessagePackPrimitives.TryReadArrayHeader(buffer, out uint uintCount, out tokenSize);
						count = checked((int)uintCount);
						return SlowPath(ref reader, self, readResult, ref count, ref tokenSize);
					}
					else
					{
						count = 0;
						return self.InsufficientBytes(reader);
					}

				default:
					return ThrowUnreachable();
			}
		}
	}

	public override DecodeResult TryReadMapHeader(ref Reader reader, out int count)
	{
		DecodeResult readResult = MessagePackPrimitives.TryReadMapHeader(reader.UnreadSpan, out uint uintCount, out int tokenSize);
		count = checked((int)uintCount);
		if (readResult == DecodeResult.Success)
		{
			this.Advance(ref reader, tokenSize, added: uintCount * 2);
			return DecodeResult.Success;
		}

		return SlowPath(ref reader, this, readResult, ref count, ref tokenSize);

		static DecodeResult SlowPath(ref Reader reader, MsgPackStreamingDeformatter self, DecodeResult readResult, ref int count, ref int tokenSize)
		{
			switch (readResult)
			{
				case DecodeResult.Success:
					self.Advance(ref reader, tokenSize, added: unchecked((uint)count) * 2);
					return DecodeResult.Success;
				case DecodeResult.TokenMismatch:
					return DecodeResult.TokenMismatch;
				case DecodeResult.EmptyBuffer:
				case DecodeResult.InsufficientBuffer:
					Span<byte> buffer = stackalloc byte[tokenSize];
					if (reader.SequenceReader.TryCopyTo(buffer))
					{
						readResult = MessagePackPrimitives.TryReadMapHeader(buffer, out uint uintCount, out tokenSize);
						count = checked((int)uintCount);
						return SlowPath(ref reader, self, readResult, ref count, ref tokenSize);
					}
					else
					{
						count = 0;
						return self.InsufficientBytes(reader);
					}

				default:
					return ThrowUnreachable();
			}
		}
	}

	public override DecodeResult TryReadNull(ref Reader reader)
	{
		if (reader.SequenceReader.TryPeek(out byte next))
		{
			if (next == MessagePackCode.Nil)
			{
				this.Advance(ref reader, 1);
				return DecodeResult.Success;
			}

			return DecodeResult.TokenMismatch;
		}
		else
		{
			return this.InsufficientBytes(reader);
		}
	}

	public override DecodeResult TryReadNull(ref Reader reader, out bool isNull)
	{
		DecodeResult result = this.TryReadNull(ref reader);
		isNull = result == DecodeResult.Success;
		return result == DecodeResult.TokenMismatch ? DecodeResult.Success : result;
	}

	[DoesNotReturn]
	private static DecodeResult ThrowUnreachable() => throw new UnreachableException();

	/// <summary>
	/// Advances the reader past the specified number of bytes.
	/// </summary>
	/// <param name="bytes">The number of bytes to advance.</param>
	/// <param name="consumed">The number of msgpack structures that has been read. Typically 1, sometimes 0.</param>
	/// <param name="added">The number of msgpack structures added to the expected count. Typically 0, but for array/map headers will be non-zero.</param>
	private void Advance(ref Reader reader, long bytes, uint consumed = 1, uint added = 0)
	{
		reader.Advance(bytes);

		// Never let the expected remaining structures go negative.
		// If we're reading simple top-level values, we start at 0 and should remain there.
		uint expectedRemainingStructures = this.expectedRemainingStructures;
		if (consumed > expectedRemainingStructures)
		{
			expectedRemainingStructures = 0;
		}
		else
		{
			expectedRemainingStructures -= consumed;
		}

		this.expectedRemainingStructures = expectedRemainingStructures + added;
	}
}
