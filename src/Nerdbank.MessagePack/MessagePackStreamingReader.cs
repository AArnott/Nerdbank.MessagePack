// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using DecodeResult = Nerdbank.MessagePack.MessagePackPrimitives.DecodeResult;

namespace Nerdbank.MessagePack;

public ref partial struct MessagePackStreamingReader
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

	public DecodeResult TryPeekNextCode(out byte code)
	{
		return this.reader.TryPeek(out code) ? DecodeResult.Success : this.InsufficientBytes;
	}

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

	public DecodeResult TryRead(out bool value)
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

	public DecodeResult TryRead(out float value)
	{
		DecodeResult readResult = MessagePackPrimitives.TryRead(this.reader.UnreadSpan, out value, out int tokenSize);
		if (readResult == MessagePackPrimitives.DecodeResult.Success)
		{
			this.reader.Advance(tokenSize);
			return DecodeResult.Success;
		}

		return SlowPath(ref this, readResult, ref value, ref tokenSize);

		static DecodeResult SlowPath(ref MessagePackStreamingReader self, DecodeResult readResult, ref float value, ref int tokenSize)
		{
			switch (readResult)
			{
				case MessagePackPrimitives.DecodeResult.Success:
					self.reader.Advance(tokenSize);
					return DecodeResult.Success;
				case MessagePackPrimitives.DecodeResult.TokenMismatch:
					return DecodeResult.TokenMismatch;
				case MessagePackPrimitives.DecodeResult.EmptyBuffer:
				case MessagePackPrimitives.DecodeResult.InsufficientBuffer:
					Span<byte> buffer = stackalloc byte[tokenSize];
					if (self.reader.TryCopyTo(buffer))
					{
						readResult = MessagePackPrimitives.TryRead(buffer, out value, out tokenSize);
						return SlowPath(ref self, readResult, ref value, ref tokenSize);
					}
					else
					{
						return self.InsufficientBytes;
					}

				default:
					return ThrowUnreachable();
			}
		}
	}

	public DecodeResult TryRead(out double value)
	{
		DecodeResult readResult = MessagePackPrimitives.TryRead(this.reader.UnreadSpan, out value, out int tokenSize);
		if (readResult == MessagePackPrimitives.DecodeResult.Success)
		{
			this.reader.Advance(tokenSize);
			return DecodeResult.Success;
		}

		return SlowPath(ref this, readResult, ref value, ref tokenSize);

		static DecodeResult SlowPath(ref MessagePackStreamingReader self, DecodeResult readResult, ref double value, ref int tokenSize)
		{
			switch (readResult)
			{
				case MessagePackPrimitives.DecodeResult.Success:
					self.reader.Advance(tokenSize);
					return DecodeResult.Success;
				case MessagePackPrimitives.DecodeResult.TokenMismatch:
					return DecodeResult.TokenMismatch;
				case MessagePackPrimitives.DecodeResult.EmptyBuffer:
				case MessagePackPrimitives.DecodeResult.InsufficientBuffer:
					Span<byte> buffer = stackalloc byte[tokenSize];
					if (self.reader.TryCopyTo(buffer))
					{
						readResult = MessagePackPrimitives.TryRead(buffer, out value, out tokenSize);
						return SlowPath(ref self, readResult, ref value, ref tokenSize);
					}
					else
					{
						return self.InsufficientBytes;
					}

				default:
					return ThrowUnreachable();
			}
		}
	}

	public DecodeResult TryRead(out string? value)
	{
		DecodeResult result = this.TryReadNil();
		if (result != DecodeResult.TokenMismatch)
		{
			value = null;
			return result;
		}

		result = this.TryGetStringLengthInBytes(out uint byteLength);
		if (result != DecodeResult.Success)
		{
			value = null;
			return result;
		}

		ReadOnlySpan<byte> unreadSpan = this.reader.UnreadSpan;
		if (unreadSpan.Length >= byteLength)
		{
			// Fast path: all bytes to decode appear in the same span.
			value = StringEncoding.UTF8.GetString(unreadSpan.Slice(0, checked((int)byteLength)));
			this.reader.Advance(byteLength);
			return DecodeResult.Success;
		}
		else
		{
			return this.ReadStringSlow(byteLength, out value);
		}
	}

	public DecodeResult TryRead(out DateTime value)
	{
		DecodeResult readResult = MessagePackPrimitives.TryRead(this.reader.UnreadSpan, out value, out int tokenSize);
		if (readResult == DecodeResult.Success)
		{
			this.reader.Advance(tokenSize);
			return DecodeResult.Success;
		}

		return SlowPath(ref this, readResult, ref value, ref tokenSize);

		static DecodeResult SlowPath(ref MessagePackStreamingReader self, DecodeResult readResult, ref DateTime value, ref int tokenSize)
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
						readResult = MessagePackPrimitives.TryRead(buffer, out value, out tokenSize);
						return SlowPath(ref self, readResult, ref value, ref tokenSize);
					}
					else
					{
						return self.InsufficientBytes;
					}

				default:
					return ThrowUnreachable();
			}
		}
	}

	public DecodeResult TryRead(ExtensionHeader extensionHeader, out DateTime value)
	{
		DecodeResult readResult = MessagePackPrimitives.TryRead(this.reader.UnreadSpan, extensionHeader, out value, out int tokenSize);
		if (readResult == DecodeResult.Success)
		{
			this.reader.Advance(tokenSize);
			return DecodeResult.Success;
		}

		return SlowPath(ref this, extensionHeader, readResult, ref value, ref tokenSize);

		static DecodeResult SlowPath(ref MessagePackStreamingReader self, ExtensionHeader header, DecodeResult readResult, ref DateTime value, ref int tokenSize)
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
						readResult = MessagePackPrimitives.TryRead(buffer, header, out value, out tokenSize);
						return SlowPath(ref self, header, readResult, ref value, ref tokenSize);
					}
					else
					{
						return self.InsufficientBytes;
					}

				default:
					return ThrowUnreachable();
			}
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

	public DecodeResult TryReadBinary(out ReadOnlySequence<byte> value)
	{
		DecodeResult result = this.TryGetBytesLength(out uint length);
		if (result != DecodeResult.Success)
		{
			value = default;
			return result;
		}

		if (this.reader.Remaining < length)
		{
			value = default;
			return this.InsufficientBytes;
		}

		value = this.reader.Sequence.Slice(this.reader.Position, length);
		this.reader.Advance(length);
		return DecodeResult.Success;
	}

	public DecodeResult TryReadStringSequence(out ReadOnlySequence<byte> value)
	{
		DecodeResult result = this.TryGetStringLengthInBytes(out uint length);
		if (result != DecodeResult.Success)
		{
			value = default;
			return result;
		}

		if (this.reader.Remaining < length)
		{
			value = default;
			return this.InsufficientBytes;
		}

		value = this.reader.Sequence.Slice(this.reader.Position, length);
		this.reader.Advance(length);
		return DecodeResult.Success;
	}

	public DecodeResult TryReadStringSpan(out bool contiguous, out ReadOnlySpan<byte> value)
	{
		SequenceReader<byte> oldReader = this.reader;
		DecodeResult result = this.TryGetStringLengthInBytes(out uint length);
		if (result != DecodeResult.Success)
		{
			value = default;
			contiguous = false;
			return result;
		}

		if (this.reader.Remaining < length)
		{
			value = default;
			contiguous = false;
			return this.InsufficientBytes;
		}

		if (this.reader.CurrentSpanIndex + length <= this.reader.CurrentSpan.Length)
		{
			value = this.reader.CurrentSpan.Slice(this.reader.CurrentSpanIndex, checked((int)length));
			this.reader.Advance(length);
			contiguous = true;
			return DecodeResult.Success;
		}
		else
		{
			this.reader = oldReader;
			value = default;
			contiguous = false;
			return DecodeResult.Success;
		}
	}

	public DecodeResult TryRead(out ExtensionHeader extensionHeader)
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

	public DecodeResult TryRead(out Extension extension)
	{
		DecodeResult result = this.TryRead(out ExtensionHeader header);
		if (result != DecodeResult.Success)
		{
			extension = default;
			return result;
		}

		if (this.reader.Remaining < header.Length)
		{
			extension = default;
			return this.InsufficientBytes;
		}

		ReadOnlySequence<byte> data = this.reader.Sequence.Slice(this.reader.Position, header.Length);
		this.reader.Advance(header.Length);
		extension = new Extension(header.TypeCode, data);
		return DecodeResult.Success;
	}

	public DecodeResult TrySkip(SerializationContext context)
	{
		DecodeResult result = this.TryPeekNextCode(out byte code);
		if (result != DecodeResult.Success)
		{
			return result;
		}

		switch (code)
		{
			case byte x when MessagePackCode.IsPositiveFixInt(x) || MessagePackCode.IsNegativeFixInt(x):
			case MessagePackCode.Nil:
			case MessagePackCode.True:
			case MessagePackCode.False:
				return this.reader.TryAdvance(1) ? DecodeResult.Success : this.InsufficientBytes;
			case MessagePackCode.Int8:
			case MessagePackCode.UInt8:
				return this.reader.TryAdvance(2) ? DecodeResult.Success : this.InsufficientBytes;
			case MessagePackCode.Int16:
			case MessagePackCode.UInt16:
				return this.reader.TryAdvance(3) ? DecodeResult.Success : this.InsufficientBytes;
			case MessagePackCode.Int32:
			case MessagePackCode.UInt32:
			case MessagePackCode.Float32:
				return this.reader.TryAdvance(5) ? DecodeResult.Success : this.InsufficientBytes;
			case MessagePackCode.Int64:
			case MessagePackCode.UInt64:
			case MessagePackCode.Float64:
				return this.reader.TryAdvance(9) ? DecodeResult.Success : this.InsufficientBytes;
			case byte x when MessagePackCode.IsFixMap(x):
			case MessagePackCode.Map16:
			case MessagePackCode.Map32:
				context.DepthStep();
				return TrySkipNextMap(ref this, context);
			case byte x when MessagePackCode.IsFixArray(x):
			case MessagePackCode.Array16:
			case MessagePackCode.Array32:
				context.DepthStep();
				return TrySkipNextArray(ref this, context);
			case byte x when MessagePackCode.IsFixStr(x):
			case MessagePackCode.Str8:
			case MessagePackCode.Str16:
			case MessagePackCode.Str32:
				result = this.TryGetStringLengthInBytes(out uint length);
				if (result != DecodeResult.Success)
				{
					return result;
				}

				return this.reader.TryAdvance(length) ? DecodeResult.Success : this.InsufficientBytes;
			case MessagePackCode.Bin8:
			case MessagePackCode.Bin16:
			case MessagePackCode.Bin32:
				result = this.TryGetBytesLength(out length);
				if (result != DecodeResult.Success)
				{
					return result;
				}

				return this.reader.TryAdvance(length) ? DecodeResult.Success : this.InsufficientBytes;
			case MessagePackCode.FixExt1:
			case MessagePackCode.FixExt2:
			case MessagePackCode.FixExt4:
			case MessagePackCode.FixExt8:
			case MessagePackCode.FixExt16:
			case MessagePackCode.Ext8:
			case MessagePackCode.Ext16:
			case MessagePackCode.Ext32:
				result = this.TryRead(out ExtensionHeader header);
				if (result != DecodeResult.Success)
				{
					return result;
				}

				return this.reader.TryAdvance(header.Length) ? DecodeResult.Success : this.InsufficientBytes;
			default:
				// We don't actually expect to ever hit this point, since every code is supported.
				Debug.Fail("Missing handler for code: " + code);
				throw MessagePackReader.ThrowInvalidCode(code);
		}

		DecodeResult TrySkipNextArray(ref MessagePackStreamingReader self, SerializationContext context)
		{
			DecodeResult result = self.TryReadArrayHeader(out int count);
			if (result != DecodeResult.Success)
			{
				return result;
			}

			return TrySkip(ref self, count, context);
		}

		DecodeResult TrySkipNextMap(ref MessagePackStreamingReader self, SerializationContext context)
		{
			DecodeResult result = self.TryReadMapHeader(out int count);
			if (result != DecodeResult.Success)
			{
				return result;
			}

			return TrySkip(ref self, count * 2, context);
		}

		DecodeResult TrySkip(ref MessagePackStreamingReader self, int count, SerializationContext context)
		{
			for (int i = 0; i < count; i++)
			{
				DecodeResult result = self.TrySkip(context);
				if (result != DecodeResult.Success)
				{
					return result;
				}
			}

			return DecodeResult.Success;
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
	private static DecodeResult ThrowUnreachable() => throw new UnreachableException();

	private DecodeResult TryGetBytesLength(out uint length)
	{
		bool usingBinaryHeader = true;
		MessagePackPrimitives.DecodeResult readResult = MessagePackPrimitives.TryReadBinHeader(this.reader.UnreadSpan, out length, out int tokenSize);
		if (readResult == MessagePackPrimitives.DecodeResult.Success)
		{
			this.reader.Advance(tokenSize);
			return DecodeResult.Success;
		}

		return SlowPath(ref this, readResult, usingBinaryHeader, ref length, ref tokenSize);

		static DecodeResult SlowPath(ref MessagePackStreamingReader self, MessagePackPrimitives.DecodeResult readResult, bool usingBinaryHeader, ref uint length, ref int tokenSize)
		{
			switch (readResult)
			{
				case MessagePackPrimitives.DecodeResult.Success:
					self.reader.Advance(tokenSize);
					return DecodeResult.Success;
				case MessagePackPrimitives.DecodeResult.TokenMismatch:
					if (usingBinaryHeader)
					{
						usingBinaryHeader = false;
						readResult = MessagePackPrimitives.TryReadStringHeader(self.reader.UnreadSpan, out length, out tokenSize);
						return SlowPath(ref self, readResult, usingBinaryHeader, ref length, ref tokenSize);
					}
					else
					{
						return DecodeResult.TokenMismatch;
					}

				case MessagePackPrimitives.DecodeResult.EmptyBuffer:
				case MessagePackPrimitives.DecodeResult.InsufficientBuffer:
					Span<byte> buffer = stackalloc byte[tokenSize];
					if (self.reader.TryCopyTo(buffer))
					{
						readResult = usingBinaryHeader
							? MessagePackPrimitives.TryReadBinHeader(buffer, out length, out tokenSize)
							: MessagePackPrimitives.TryReadStringHeader(buffer, out length, out tokenSize);
						return SlowPath(ref self, readResult, usingBinaryHeader, ref length, ref tokenSize);
					}
					else
					{
						length = default;
						return self.InsufficientBytes;
					}

				default:
					return ThrowUnreachable();
			}
		}
	}

	private DecodeResult TryGetStringLengthInBytes(out uint length)
	{
		DecodeResult readResult = MessagePackPrimitives.TryReadStringHeader(this.reader.UnreadSpan, out length, out int tokenSize);
		if (readResult == DecodeResult.Success)
		{
			this.reader.Advance(tokenSize);
			return DecodeResult.Success;
		}

		return SlowPath(ref this, readResult, ref length, ref tokenSize);

		static DecodeResult SlowPath(ref MessagePackStreamingReader self, DecodeResult readResult, ref uint length, ref int tokenSize)
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
						readResult = MessagePackPrimitives.TryReadStringHeader(buffer, out length, out tokenSize);
						return SlowPath(ref self, readResult, ref length, ref tokenSize);
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

	/// <summary>
	/// Reads a string assuming that it is spread across multiple spans in the <see cref="ReadOnlySequence{T}"/>.
	/// </summary>
	/// <param name="byteLength">The length of the string to be decoded, in bytes.</param>
	/// <param name="value">Receives the decoded string.</param>
	/// <returns>The result of the operation.</returns>
	private DecodeResult ReadStringSlow(uint byteLength, out string? value)
	{
		if (this.reader.Remaining < byteLength)
		{
			value = null;
			return this.InsufficientBytes;
		}

		// We need to decode bytes incrementally across multiple spans.
		int remainingByteLength = checked((int)byteLength);
		int maxCharLength = StringEncoding.UTF8.GetMaxCharCount(remainingByteLength);
		char[] charArray = ArrayPool<char>.Shared.Rent(maxCharLength);
		System.Text.Decoder decoder = StringEncoding.UTF8.GetDecoder();

		int initializedChars = 0;
		while (remainingByteLength > 0)
		{
			int bytesRead = Math.Min(remainingByteLength, this.reader.UnreadSpan.Length);
			remainingByteLength -= bytesRead;
			bool flush = remainingByteLength == 0;
			initializedChars += decoder.GetChars(this.reader.UnreadSpan.Slice(0, bytesRead), charArray.AsSpan(initializedChars), flush);
			this.reader.Advance(bytesRead);
		}

		value = new string(charArray, 0, initializedChars);
		ArrayPool<char>.Shared.Return(charArray);
		return DecodeResult.Success;
	}

	public struct BufferRefresh
	{
		internal CancellationToken CancellationToken { get; init; }

		internal ReadOnlySequence<byte> Buffer { get; init; }

		internal GetMoreBytesAsync? GetMoreBytes { get; init; }

		internal object? GetMoreBytesState { get; init; }

		internal bool EndOfStream { get; init; }
	}
}
