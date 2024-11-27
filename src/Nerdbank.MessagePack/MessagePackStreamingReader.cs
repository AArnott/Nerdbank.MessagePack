// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using DecodeResult = Nerdbank.MessagePack.MessagePackPrimitives.DecodeResult;

namespace Nerdbank.MessagePack;

public ref struct MessagePackStreamingReader
{
	private readonly GetMoreBytesAsync? getMoreBytesAsync;
	private readonly object? getMoreBytesState;
	private SequenceReader<byte> reader;

	/// <summary>
	/// A value indicating whether no more bytes can be expected once we reach the end of the current buffer.
	/// </summary>
	private bool eof;

	public MessagePackStreamingReader(scoped in ReadOnlySequence<byte> sequence)
		: this(sequence, null, null)
	{
	}

	public MessagePackStreamingReader(scoped in ReadOnlySequence<byte> sequence, GetMoreBytesAsync? additionalBytesSource, object? getMoreBytesState)
	{
		this.reader = new SequenceReader<byte>(sequence);
		this.getMoreBytesAsync = additionalBytesSource;
		this.getMoreBytesState = getMoreBytesState;
		;
	}

	public MessagePackStreamingReader(scoped in BufferRefresh refresh)
		: this(refresh.Buffer, refresh.GetMoreBytes, refresh.GetMoreBytesState)
	{
		this.CancellationToken = refresh.CancellationToken;
		this.eof = refresh.EndOfStream;
	}

	/// <summary>
	/// A delegate that can be used to get more bytes to complete the operation.
	/// </summary>
	/// <param name="state">A state object.</param>
	/// <param name="consumed">
	/// The position after the last consumed byte (i.e. the last byte from the original buffer that is not expected to be included to the new buffer).
	/// Any bytes at or following this position that were in the original buffer must be included to the buffer returned from this method.
	/// </param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>The available buffer, which must contain more bytes than remained after <paramref name="consumed"/> if there are any more bytes to be had.</returns>
	public delegate ValueTask<ReadResult> GetMoreBytesAsync(object? state, SequencePosition consumed, CancellationToken cancellationToken);

	public CancellationToken CancellationToken { get; init; }

	[UnscopedRef]
	internal ref SequenceReader<byte> SequenceReader => ref this.reader;

	private DecodeResult InsufficientBytes => this.eof ? DecodeResult.EmptyBuffer : DecodeResult.InsufficientBuffer;

	public DecodeResult TryReadNil()
	{
		if (this.reader.TryPeek(out byte next))
		{
			if (next == MessagePackCode.Nil)
			{
				this.reader.Advance(1);
				return DecodeResult.Success;
			}

			return DecodeResult.TokenMismatch;
		}
		else
		{
			return this.InsufficientBytes;
		}
	}

	public DecodeResult TryReadBoolean(out bool value)
	{
		if (this.reader.TryPeek(out byte next))
		{
			switch (next)
			{
				case MessagePackCode.True:
					value = true;
					break;
				case MessagePackCode.False:
					value = false;
					break;
				default:
					value = false;
					return DecodeResult.TokenMismatch;
			}

			this.reader.Advance(1);
			return DecodeResult.Success;
		}
		else
		{
			value = false;
			return this.InsufficientBytes;
		}
	}

	public DecodeResult TryReadArrayHeader(out int count)
	{
		DecodeResult readResult = MessagePackPrimitives.TryReadArrayHeader(this.reader.UnreadSpan, out uint uintCount, out int tokenSize);
		count = checked((int)uintCount);
		if (readResult == DecodeResult.Success)
		{
			this.reader.Advance(tokenSize);
			return DecodeResult.Success;
		}

		return SlowPath(ref this, readResult, ref count, ref tokenSize);

		static DecodeResult SlowPath(ref MessagePackStreamingReader self, DecodeResult readResult, ref int count, ref int tokenSize)
		{
			switch (readResult)
			{
				case DecodeResult.Success:
					self.reader.Advance(tokenSize);
					return DecodeResult.Success;
				case DecodeResult.TokenMismatch:
					return DecodeResult.TokenMismatch;
				case DecodeResult.EmptyBuffer:
				case DecodeResult.InsufficientBuffer:
					Span<byte> buffer = stackalloc byte[tokenSize];
					if (self.reader.TryCopyTo(buffer))
					{
						readResult = MessagePackPrimitives.TryReadArrayHeader(buffer, out uint uintCount, out tokenSize);
						count = checked((int)uintCount);
						return SlowPath(ref self, readResult, ref count, ref tokenSize);
					}
					else
					{
						count = 0;
						return self.InsufficientBytes;
					}

				default:
					return ThrowUnreachable();
			}
		}
	}

	public DecodeResult TryReadMapHeader(out int count)
	{
		DecodeResult readResult = MessagePackPrimitives.TryReadMapHeader(this.reader.UnreadSpan, out uint uintCount, out int tokenSize);
		count = checked((int)uintCount);
		if (readResult == DecodeResult.Success)
		{
			this.reader.Advance(tokenSize);
			return DecodeResult.Success;
		}

		return SlowPath(ref this, readResult, ref count, ref tokenSize);

		static DecodeResult SlowPath(ref MessagePackStreamingReader self, DecodeResult readResult, ref int count, ref int tokenSize)
		{
			switch (readResult)
			{
				case DecodeResult.Success:
					self.reader.Advance(tokenSize);
					return DecodeResult.Success;
				case DecodeResult.TokenMismatch:
					return DecodeResult.TokenMismatch;
				case DecodeResult.EmptyBuffer:
				case DecodeResult.InsufficientBuffer:
					Span<byte> buffer = stackalloc byte[tokenSize];
					if (self.reader.TryCopyTo(buffer))
					{
						readResult = MessagePackPrimitives.TryReadMapHeader(buffer, out uint uintCount, out tokenSize);
						count = checked((int)uintCount);
						return SlowPath(ref self, readResult, ref count, ref tokenSize);
					}
					else
					{
						count = 0;
						return self.InsufficientBytes;
					}

				default:
					return ThrowUnreachable();
			}
		}
	}

	public DecodeResult TryReadRaw(long length, out ReadOnlySequence<byte> rawMsgPack)
	{
		if (this.reader.Remaining >= length)
		{
			rawMsgPack = this.reader.Sequence.Slice(this.reader.Position, length);
			this.reader.Advance(length);
			return DecodeResult.Success;
		}

		rawMsgPack = default;
		return this.InsufficientBytes;
	}

	public DecodeResult TryReadExtensionHeader(out ExtensionHeader extensionHeader)
	{
		DecodeResult readResult = MessagePackPrimitives.TryReadExtensionHeader(this.reader.UnreadSpan, out extensionHeader, out int tokenSize);
		if (readResult == DecodeResult.Success)
		{
			this.reader.Advance(tokenSize);
			return DecodeResult.Success;
		}

		return SlowPath(ref this, readResult, ref extensionHeader, ref tokenSize);

		static DecodeResult SlowPath(ref MessagePackStreamingReader self, DecodeResult readResult, ref ExtensionHeader extensionHeader, ref int tokenSize)
		{
			switch (readResult)
			{
				case DecodeResult.Success:
					self.reader.Advance(tokenSize);
					return DecodeResult.Success;
				case DecodeResult.TokenMismatch:
					return DecodeResult.TokenMismatch;
				case DecodeResult.EmptyBuffer:
				case DecodeResult.InsufficientBuffer:
					Span<byte> buffer = stackalloc byte[tokenSize];
					if (self.reader.TryCopyTo(buffer))
					{
						readResult = MessagePackPrimitives.TryReadExtensionHeader(buffer, out extensionHeader, out tokenSize);
						return SlowPath(ref self, readResult, ref extensionHeader, ref tokenSize);
					}
					else
					{
						extensionHeader = default;
						return self.InsufficientBytes;
					}

				default:
					return ThrowUnreachable();
			}
		}
	}

	/// <summary>
	/// Gets the information to return from an async method that has been using this reader
	/// so that the caller knows how to resume reading.
	/// </summary>
	/// <returns>The value to pass to <see cref="MessagePackStreamingReader(in BufferRefresh)"/>.</returns>
	public readonly BufferRefresh GetExchangeInfo() => new()
	{
		CancellationToken = this.CancellationToken,
		Buffer = this.reader.UnreadSequence,
		GetMoreBytes = this.getMoreBytesAsync,
		GetMoreBytesState = this.getMoreBytesState,
		EndOfStream = this.eof,
	};

	/// <summary>
	/// Adds more bytes to the buffer being decoded, if they are available.
	/// </summary>
	/// <returns>The value to pass to <see cref="MessagePackStreamingReader(in BufferRefresh)"/>.</returns>
	/// <exception cref="EndOfStreamException">Thrown if no more bytes are available.</exception>
	/// <remarks>
	/// This is a destructive operation to this <see cref="MessagePackStreamingReader"/> value.
	/// It must not be used after calling this method.
	/// Instead, the result can use the result of this method to recreate a new <see cref="MessagePackStreamingReader"/> value.
	/// </remarks>
	public ValueTask<BufferRefresh> ReplenishBufferAsync()
	{
		if (this.getMoreBytesAsync is null || this.eof)
		{
			throw new EndOfStreamException($"Additional bytes are required to complete the operation and no means to get more was provided.");
		}

		this.CancellationToken.ThrowIfCancellationRequested();
		ValueTask<BufferRefresh> result = Helper(this.getMoreBytesAsync, this.getMoreBytesState, this.reader.Position, this.reader.Sequence.End, this.CancellationToken);

		// Having made the call to request more bytes, our caller can no longer use this struct because the buffers it had are assumed to have been recycled.
		this.reader = default;
		return result;

		static async ValueTask<BufferRefresh> Helper(GetMoreBytesAsync getMoreBytes, object? getMoreBytesState, SequencePosition consumed, SequencePosition examined, CancellationToken cancellationToken)
		{
			ReadResult moreBuffer = await getMoreBytes(getMoreBytesState, consumed, cancellationToken);
			return new BufferRefresh
			{
				CancellationToken = cancellationToken,
				Buffer = moreBuffer.Buffer,
				GetMoreBytes = getMoreBytes,
				GetMoreBytesState = getMoreBytesState,
				EndOfStream = moreBuffer.IsCompleted,
			};
		}
	}

	[DoesNotReturn]
	private static DecodeResult ThrowUnreachable() => throw new Exception("Presumed unreachable point in code reached.");

	public struct BufferRefresh
	{
		internal CancellationToken CancellationToken { get; init; }

		internal ReadOnlySequence<byte> Buffer { get; init; }

		internal GetMoreBytesAsync? GetMoreBytes { get; init; }

		internal object? GetMoreBytesState { get; init; }

		internal bool EndOfStream { get; init; }
	}
}
