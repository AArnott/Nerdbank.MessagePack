// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Nerdbank.PolySerializer.MessagePack;

internal class MsgPackStreamingDeformatter : StreamingDeformatter
{
	internal static readonly Deformatter Deformatter = new(MsgPackStreamingDeformatter.Instance);

	internal static readonly MsgPackStreamingDeformatter Instance = new();

	private uint expectedRemainingStructures;

	private MsgPackStreamingDeformatter() { }

	public override Encoding Encoding => StringEncoding.UTF8;

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

	public override DecodeResult TryRead(ref Reader reader, out bool value)
	{
		throw new NotImplementedException();
	}

	public override DecodeResult TryRead(ref Reader reader, out float value)
	{
		throw new NotImplementedException();
	}

	public override DecodeResult TryRead(ref Reader reader, out string value)
	{
		throw new NotImplementedException();
	}

	/// <summary>
	/// Reads a byte sequence backing a UTF-8 encoded string with an appropriate msgpack header from the msgpack stream.
	/// </summary>
	/// <param name="value">The byte sequence if the read was successful.</param>
	/// <returns>The success or error code.</returns>
	public override DecodeResult TryReadStringSequence(ref Reader reader, out ReadOnlySequence<byte> value)
	{
		Reader originalPosition = reader;
		DecodeResult result = this.TryReadStringHeader(ref reader, out uint length);
		if (result != DecodeResult.Success)
		{
			value = default;
			return result;
		}

		if (reader.SequenceReader.Remaining < length)
		{
			// Rewind the header so we can try it again.
			reader = originalPosition;

			value = default;
			return this.InsufficientBytes(reader);
		}

		value = reader.SequenceReader.Sequence.Slice(reader.SequenceReader.Position, length);
		reader.SequenceReader.Advance(length);
		return DecodeResult.Success;
	}

	/// <summary>
	/// Reads a span backing a UTF-8 encoded string with an appropriate msgpack header from the msgpack stream,
	/// if the string is contiguous in memory.
	/// </summary>
	/// <param name="contiguous">Receives a value indicating whether the string was present and contiguous in memory.</param>
	/// <param name="value">The span of bytes if the read was successful.</param>
	/// <returns>The success or error code.</returns>
	public override DecodeResult TryReadStringSpan(scoped ref Reader reader, out bool contiguous, out ReadOnlySpan<byte> value)
	{
		Reader oldReader = reader;
		DecodeResult result = this.TryReadStringHeader(ref reader, out uint length);
		if (result != DecodeResult.Success)
		{
			value = default;
			contiguous = false;
			return result;
		}

		if (reader.SequenceReader.Remaining < length)
		{
			reader = oldReader;
			value = default;
			contiguous = false;
			return this.InsufficientBytes(reader);
		}

		if (reader.SequenceReader.CurrentSpanIndex + length <= reader.SequenceReader.CurrentSpan.Length)
		{
			value = reader.SequenceReader.CurrentSpan.Slice(reader.SequenceReader.CurrentSpanIndex, checked((int)length));
			reader.SequenceReader.Advance(length);
			contiguous = true;
			return DecodeResult.Success;
		}
		else
		{
			reader = oldReader;
			value = default;
			contiguous = false;
			return DecodeResult.Success;
		}
	}

	public override DecodeResult TryRead(ref Reader reader, out int value)
	{
		DecodeResult readResult = MessagePackPrimitives.TryRead(reader.UnreadSpan, out value, out int tokenSize);
		if (readResult == DecodeResult.Success)
		{
			this.Advance(ref reader, tokenSize);
			return DecodeResult.Success;
		}

		return SlowPath(ref reader, this, readResult, ref value, ref tokenSize);

		static DecodeResult SlowPath(ref Reader reader, MsgPackStreamingDeformatter self, DecodeResult readResult, ref Int32 value, ref int tokenSize)
		{
			switch (readResult)
			{
				case DecodeResult.Success:
					self.Advance(ref reader,tokenSize);
					return DecodeResult.Success;
				case DecodeResult.TokenMismatch:
					return DecodeResult.TokenMismatch;
				case DecodeResult.EmptyBuffer:
				case DecodeResult.InsufficientBuffer:
					Span<byte> buffer = stackalloc byte[tokenSize];
					if (reader.SequenceReader.TryCopyTo(buffer))
					{
						readResult = MessagePackPrimitives.TryRead(buffer, out value, out tokenSize);
						return SlowPath(ref reader, self, readResult, ref value, ref tokenSize);
					}
					else
					{
						return self.InsufficientBytes(reader);
					}

				default:
					return ThrowUnreachable();
			}
		}
	}

	public override DecodeResult TrySkip(ref Reader reader, ref SerializationContext context)
	{
		throw new NotImplementedException();
	}

	public override PolySerializer.Converters.TypeCode ToTypeCode(byte code) => MessagePackCode.ToMessagePackType(code) switch
	{
		MessagePackType.Integer => PolySerializer.Converters.TypeCode.Integer,
		MessagePackType.Boolean => PolySerializer.Converters.TypeCode.Boolean,
		MessagePackType.Float => PolySerializer.Converters.TypeCode.Float,
		MessagePackType.String => PolySerializer.Converters.TypeCode.String,
		MessagePackType.Binary => PolySerializer.Converters.TypeCode.Binary,
		MessagePackType.Array => PolySerializer.Converters.TypeCode.Vector,
		MessagePackType.Map => PolySerializer.Converters.TypeCode.Map,
		MessagePackType.Nil => PolySerializer.Converters.TypeCode.Nil,
		_ => PolySerializer.Converters.TypeCode.Unknown,
	};

	/// <summary>
	/// Tries to read the header of a string.
	/// </summary>
	/// <param name="length">Receives the length of the next string, when successful.</param>
	/// <returns>The result classification of the read operation.</returns>
	/// <remarks>
	/// A successful call should always be followed by a successful call to <see cref="TryReadRaw(long, out ReadOnlySequence{byte})"/>,
	/// with the length of bytes specified by the extension (even if zero), so that the overall structure can be recorded as read.
	/// </remarks>
	/// <inheritdoc cref="MessagePackPrimitives.TryReadStringHeader(ReadOnlySpan{byte}, out uint, out int)" path="/remarks" />
	public DecodeResult TryReadStringHeader(ref Reader reader, out uint length)
	{
		DecodeResult readResult = MessagePackPrimitives.TryReadStringHeader(reader.UnreadSpan, out length, out int tokenSize);
		if (readResult == DecodeResult.Success)
		{
			this.Advance(ref reader, tokenSize, 0);
			return DecodeResult.Success;
		}

		return SlowPath(ref reader, this, readResult, ref length, ref tokenSize);

		static DecodeResult SlowPath(ref Reader reader, MsgPackStreamingDeformatter self, DecodeResult readResult, ref uint length, ref int tokenSize)
		{
			switch (readResult)
			{
				case DecodeResult.Success:
					self.Advance(ref reader, tokenSize, 0);
					return DecodeResult.Success;
				case DecodeResult.TokenMismatch:
					return DecodeResult.TokenMismatch;
				case DecodeResult.EmptyBuffer:
				case DecodeResult.InsufficientBuffer:
					Span<byte> buffer = stackalloc byte[tokenSize];
					if (reader.SequenceReader.TryCopyTo(buffer))
					{
						readResult = MessagePackPrimitives.TryReadStringHeader(buffer, out length, out tokenSize);
						return SlowPath(ref reader, self, readResult, ref length, ref tokenSize);
					}
					else
					{
						length = default;
						return DecodeResult.InsufficientBuffer;
					}

				default:
					return ThrowUnreachable();
			}
		}
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
