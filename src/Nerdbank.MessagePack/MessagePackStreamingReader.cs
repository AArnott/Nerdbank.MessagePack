// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using Microsoft;
using DecodeResult = Nerdbank.MessagePack.MessagePackPrimitives.DecodeResult;

namespace Nerdbank.MessagePack;

/// <summary>
/// A msgpack decoder that returns error codes rather than throws exceptions
/// when the buffer is incomplete or the token type does not match expectations.
/// </summary>
/// <remarks>
/// All decoding methods on this struct return <see cref="DecodeResult"/>.
/// Callers <em>must</em> take care to observe this value and take appropriate action.
/// A common calling pattern is to call the decoding method within a <see langword="while"/> loop's expression
/// and use the <see cref="DecodeResultExtensions.NeedsMoreBytes(DecodeResult)"/>
/// extension method on the result.
/// The content of the loop should be a call to <see cref="FetchMoreBytesAsync"/> and to reconstruct
/// the reader using <see cref="MessagePackStreamingReader(in BufferRefresh)"/>.
/// </remarks>
/// <example>
/// <para>
/// The following snippet demonstrates a common pattern for properly reading with this type.
/// </para>
/// <code source="../../samples/CustomConverters.cs" region="GetMoreBytesPattern" lang="C#" />
/// </example>
[Experimental("NBMsgPackAsync")]
public ref partial struct MessagePackStreamingReader
{
	private readonly GetMoreBytesAsync? getMoreBytesAsync;
	private readonly object? getMoreBytesState;
	private SequenceReader<byte> reader;

	/// <summary>
	/// A value indicating whether no more bytes can be expected once we reach the end of the current buffer.
	/// </summary>
	private bool eof;

	/// <summary>
	/// Initializes a new instance of the <see cref="MessagePackStreamingReader"/> struct
	/// that decodes from a complete buffer.
	/// </summary>
	/// <param name="sequence">The buffer to decode msgpack from. This buffer should be complete.</param>
	public MessagePackStreamingReader(scoped in ReadOnlySequence<byte> sequence)
		: this(sequence, null, null)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MessagePackStreamingReader"/> struct
	/// that decodes from a buffer that may be initially incomplete.
	/// </summary>
	/// <param name="sequence">The buffer we have so far.</param>
	/// <param name="additionalBytesSource">A means to obtain more msgpack bytes when necessary.</param>
	/// <param name="getMoreBytesState">
	/// A value to provide to the <paramref name="getMoreBytesState"/> delegate.
	/// This facilitates reuse of a particular delegate across deserialization operations.
	/// </param>
	public MessagePackStreamingReader(scoped in ReadOnlySequence<byte> sequence, GetMoreBytesAsync? additionalBytesSource, object? getMoreBytesState)
	{
		this.reader = new SequenceReader<byte>(sequence);
		this.getMoreBytesAsync = additionalBytesSource;
		this.getMoreBytesState = getMoreBytesState;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MessagePackStreamingReader"/> struct
	/// that resumes after an <see langword="await" /> operation.
	/// </summary>
	/// <param name="refresh">The data to reinitialize this ref struct.</param>
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
	/// <param name="examined">
	/// The position of the last examined byte.
	/// This should be passed to <see cref="PipeReader.AdvanceTo(SequencePosition, SequencePosition)"/>
	/// when applicable to ensure that the request to get more bytes is filled with actual more bytes rather than the existing buffer.
	/// </param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>The available buffer, which must contain more bytes than remained after <paramref name="consumed"/> if there are any more bytes to be had.</returns>
	public delegate ValueTask<ReadResult> GetMoreBytesAsync(object? state, SequencePosition consumed, SequencePosition examined, CancellationToken cancellationToken);

	/// <summary>
	/// Gets a token that may cancel deserialization.
	/// </summary>
	public CancellationToken CancellationToken { get; init; }

	/// <summary>
	/// Gets the reader's position within the current buffer.
	/// </summary>
	public SequencePosition Position => this.SequenceReader.Position;

	/// <summary>
	/// Gets the underlying <see cref="SequenceReader{T}"/>.
	/// </summary>
	[UnscopedRef]
	internal ref SequenceReader<byte> SequenceReader => ref this.reader;

	/// <summary>
	/// Gets the error code to return when the buffer has insufficient bytes to finish a decode request.
	/// </summary>
	private DecodeResult InsufficientBytes => this.eof ? DecodeResult.EmptyBuffer : DecodeResult.InsufficientBuffer;

	/// <summary>
	/// Peeks at the next msgpack byte without advancing the reader.
	/// </summary>
	/// <param name="code">When successful, receives the next msgpack byte.</param>
	/// <returns>The success or error code.</returns>
	public DecodeResult TryPeekNextCode(out byte code)
	{
		return this.reader.TryPeek(out code) ? DecodeResult.Success : this.InsufficientBytes;
	}

	/// <summary>
	/// Peeks at the next msgpack token type without advancing the reader.
	/// </summary>
	/// <param name="type">When successful, receives the next msgpack token type.</param>
	/// <returns>The success or error code.</returns>
	public DecodeResult TryPeekNextMessagePackType(out MessagePackType type)
	{
		if (this.reader.TryPeek(out byte code))
		{
			type = MessagePackCode.ToMessagePackType(code);
			return DecodeResult.Success;
		}

		type = default;
		return this.InsufficientBytes;
	}

	/// <summary>
	/// Reads the next token if it is <see cref="MessagePackType.Nil"/>.
	/// </summary>
	/// <returns>
	/// <see cref="DecodeResult.Success"/> if the token was nil and was read,
	/// <see cref="DecodeResult.TokenMismatch"/> if the token was not nil,
	/// or other error codes if the buffer is incomplete.
	/// </returns>
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

	/// <summary>
	/// Reads the next token if it is <see cref="MessagePackType.Nil"/>.
	/// </summary>
	/// <param name="isNil">A value indicating whether the next token was nil.</param>
	/// <returns>
	/// <see cref="DecodeResult.Success"/> if the next token can be decoded whether or not it was nil,
	/// or other error codes if the buffer is incomplete.
	/// </returns>
	public DecodeResult TryReadNil(out bool isNil)
	{
		DecodeResult result = this.TryReadNil();
		isNil = result == DecodeResult.Success;
		return result == DecodeResult.TokenMismatch ? DecodeResult.Success : result;
	}

	/// <summary>
	/// Reads a <see cref="bool"/> value from the msgpack stream.
	/// </summary>
	/// <param name="value">The decoded value if the read was successful.</param>
	/// <returns>The success or error code.</returns>
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

	/// <summary>
	/// Reads a <see cref="float"/> value from the msgpack stream.
	/// </summary>
	/// <param name="value">The decoded value if the read was successful.</param>
	/// <returns>The success or error code.</returns>
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

	/// <summary>
	/// Reads a <see cref="double"/> value from the msgpack stream.
	/// </summary>
	/// <param name="value">The decoded value if the read was successful.</param>
	/// <returns>The success or error code.</returns>
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

	/// <summary>
	/// Reads a <see cref="string"/> or <see langword="null" /> value from the msgpack stream.
	/// </summary>
	/// <param name="value">The decoded value if the read was successful.</param>
	/// <returns>The success or error code.</returns>
	public DecodeResult TryRead(out string? value)
	{
		DecodeResult result = this.TryReadNil();
		if (result != DecodeResult.TokenMismatch)
		{
			value = null;
			return result;
		}

		result = this.TryReadStringHeader(out uint byteLength);
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

	/// <summary>
	/// Reads a <see cref="DateTime"/> value from the msgpack stream.
	/// </summary>
	/// <param name="value">The decoded value if the read was successful.</param>
	/// <returns>The success or error code.</returns>
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

	/// <summary>
	/// Reads a <see cref="DateTime"/> value from the msgpack stream,
	/// assuming the given <see cref="ExtensionHeader"/>.
	/// </summary>
	/// <param name="extensionHeader">The extension header that was previously read.</param>
	/// <param name="value">The decoded value if the read was successful.</param>
	/// <returns>The success or error code.</returns>
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

	/// <summary>
	/// Reads an array header from the msgpack stream.
	/// </summary>
	/// <param name="count">The number of elements in the array, if the read was successful.</param>
	/// <returns>The success or error code.</returns>
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

	/// <summary>
	/// Reads a map header from the msgpack stream.
	/// </summary>
	/// <param name="count">The number of elements in the map, if the read was successful.</param>
	/// <returns>The success or error code.</returns>
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

	/// <summary>
	/// Reads a given number of bytes from the msgpack stream without decoding them.
	/// </summary>
	/// <param name="length">The number of bytes to read.</param>
	/// <param name="rawMsgPack">The bytes if the read was successful.</param>
	/// <returns>The success or error code.</returns>
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

	/// <summary>
	/// Reads a binary sequence with an appropriate msgpack header from the msgpack stream.
	/// </summary>
	/// <param name="value">The byte sequence if the read was successful.</param>
	/// <returns>The success or error code.</returns>
	public DecodeResult TryReadBinary(out ReadOnlySequence<byte> value)
	{
		DecodeResult result = this.TryReadBinHeader(out uint length);
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

	/// <summary>
	/// Reads a byte sequence backing a UTF-8 encoded string with an appropriate msgpack header from the msgpack stream.
	/// </summary>
	/// <param name="value">The byte sequence if the read was successful.</param>
	/// <returns>The success or error code.</returns>
	public DecodeResult TryReadStringSequence(out ReadOnlySequence<byte> value)
	{
		DecodeResult result = this.TryReadStringHeader(out uint length);
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

	/// <summary>
	/// Reads a span backing a UTF-8 encoded string with an appropriate msgpack header from the msgpack stream,
	/// if the string is contiguous in memory.
	/// </summary>
	/// <param name="contiguous">Receives a value indicating whether the string was present and contiguous in memory.</param>
	/// <param name="value">The span of bytes if the read was successful.</param>
	/// <returns>The success or error code.</returns>
	public DecodeResult TryReadStringSpan(out bool contiguous, out ReadOnlySpan<byte> value)
	{
		SequenceReader<byte> oldReader = this.reader;
		DecodeResult result = this.TryReadStringHeader(out uint length);
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

	/// <summary>
	/// Reads an extension header from the msgpack stream.
	/// </summary>
	/// <param name="extensionHeader">Receives the extension header if the read was successful.</param>
	/// <returns>The success or error code.</returns>
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

	/// <summary>
	/// Reads an extension from the msgpack stream.
	/// </summary>
	/// <param name="extension">Receives the extension if the read was successful.</param>
	/// <returns>The success or error code.</returns>
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

	/// <summary>
	/// Advances the reader past the next msgpack structure.
	/// </summary>
	/// <param name="context">The context of the deserialization operation.</param>
	/// <returns>The success or error code.</returns>
	/// <remarks>
	/// The reader position is changed when the return value is <see cref="DecodeResult.Success"/>.
	/// The reader position and the <paramref name="context"/> may also be changed when the return value is <see cref="DecodeResult.InsufficientBuffer"/>,
	/// such that after fetching more bytes, a follow-up call to this method can resume skipping.
	/// </remarks>
	public DecodeResult TrySkip(ref SerializationContext context)
	{
		int count = Math.Max(1, context.MidSkipRemainingCount);

		// Skip as many structures as we have already predicted we must skip to complete this or a previously suspended skip operation.
		for (int i = 0; i < count; i++)
		{
			switch (TrySkipOne(ref this, out int skipMore))
			{
				case DecodeResult.Success:
					count += skipMore;
					break;
				case DecodeResult.InsufficientBuffer:
					context.MidSkipRemainingCount = count - i;
					return DecodeResult.InsufficientBuffer;
				case DecodeResult other:
					return other;
			}
		}

		context.MidSkipRemainingCount = 0;
		return DecodeResult.Success;

		static DecodeResult TrySkipOne(ref MessagePackStreamingReader self, out int skipMore)
		{
			skipMore = 0;
			DecodeResult result = self.TryPeekNextCode(out byte code);
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
					return self.reader.TryAdvance(1) ? DecodeResult.Success : self.InsufficientBytes;
				case MessagePackCode.Int8:
				case MessagePackCode.UInt8:
					return self.reader.TryAdvance(2) ? DecodeResult.Success : self.InsufficientBytes;
				case MessagePackCode.Int16:
				case MessagePackCode.UInt16:
					return self.reader.TryAdvance(3) ? DecodeResult.Success : self.InsufficientBytes;
				case MessagePackCode.Int32:
				case MessagePackCode.UInt32:
				case MessagePackCode.Float32:
					return self.reader.TryAdvance(5) ? DecodeResult.Success : self.InsufficientBytes;
				case MessagePackCode.Int64:
				case MessagePackCode.UInt64:
				case MessagePackCode.Float64:
					return self.reader.TryAdvance(9) ? DecodeResult.Success : self.InsufficientBytes;
				case byte x when MessagePackCode.IsFixMap(x):
				case MessagePackCode.Map16:
				case MessagePackCode.Map32:
					result = self.TryReadMapHeader(out int count);
					if (result == DecodeResult.Success)
					{
						skipMore = count * 2;
					}

					return result;
				case byte x when MessagePackCode.IsFixArray(x):
				case MessagePackCode.Array16:
				case MessagePackCode.Array32:
					result = self.TryReadArrayHeader(out count);
					if (result == DecodeResult.Success)
					{
						skipMore = count;
					}

					return result;
				case byte x when MessagePackCode.IsFixStr(x):
				case MessagePackCode.Str8:
				case MessagePackCode.Str16:
				case MessagePackCode.Str32:
					SequenceReader<byte> peekBackup = self.SequenceReader;
					result = self.TryReadStringHeader(out uint length);
					if (result != DecodeResult.Success)
					{
						return result;
					}

					if (self.reader.TryAdvance(length))
					{
						return DecodeResult.Success;
					}
					else
					{
						// Rewind so we can read the string header again next time.
						self.reader = peekBackup;
						return self.InsufficientBytes;
					}

				case MessagePackCode.Bin8:
				case MessagePackCode.Bin16:
				case MessagePackCode.Bin32:
					peekBackup = self.SequenceReader;
					result = self.TryReadBinHeader(out length);
					if (result != DecodeResult.Success)
					{
						return result;
					}

					if (self.reader.TryAdvance(length))
					{
						return DecodeResult.Success;
					}
					else
					{
						// Rewind so we can read the string header again next time.
						self.reader = peekBackup;
						return self.InsufficientBytes;
					}

				case MessagePackCode.FixExt1:
				case MessagePackCode.FixExt2:
				case MessagePackCode.FixExt4:
				case MessagePackCode.FixExt8:
				case MessagePackCode.FixExt16:
				case MessagePackCode.Ext8:
				case MessagePackCode.Ext16:
				case MessagePackCode.Ext32:
					peekBackup = self.SequenceReader;
					result = self.TryRead(out ExtensionHeader header);
					if (result != DecodeResult.Success)
					{
						return result;
					}

					if (self.reader.TryAdvance(header.Length))
					{
						return DecodeResult.Success;
					}
					else
					{
						// Rewind so we can read the string header again next time.
						self.reader = peekBackup;
						return self.InsufficientBytes;
					}

				default:
					// We don't actually expect to ever hit this point, since every code is supported.
					Debug.Fail("Missing handler for code: " + code);
					throw MessagePackReader.ThrowInvalidCode(code);
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
	/// <param name="minimumLength">The minimum number of bytes to fetch before returning.</param>
	/// <returns>The value to pass to <see cref="MessagePackStreamingReader(in BufferRefresh)"/>.</returns>
	/// <exception cref="EndOfStreamException">Thrown if no more bytes are available.</exception>
	/// <remarks>
	/// This is a destructive operation to this <see cref="MessagePackStreamingReader"/> value.
	/// It must not be used after calling this method.
	/// Instead, the result can use the result of this method to recreate a new <see cref="MessagePackStreamingReader"/> value.
	/// </remarks>
	public ValueTask<BufferRefresh> FetchMoreBytesAsync(uint minimumLength = 1)
	{
		if (this.getMoreBytesAsync is null || this.eof)
		{
			throw new EndOfStreamException($"Additional bytes are required to complete the operation and no means to get more was provided.");
		}

		this.CancellationToken.ThrowIfCancellationRequested();
		ValueTask<BufferRefresh> result = HelperAsync(this.getMoreBytesAsync, this.getMoreBytesState, this.reader.Position, this.reader.Sequence.End, minimumLength, this.CancellationToken);

		// Having made the call to request more bytes, our caller can no longer use this struct because the buffers it had are assumed to have been recycled.
		this.reader = default;
		return result;

		static async ValueTask<BufferRefresh> HelperAsync(GetMoreBytesAsync getMoreBytes, object? getMoreBytesState, SequencePosition consumed, SequencePosition examined, uint minimumLength, CancellationToken cancellationToken)
		{
			ReadResult moreBuffer = await getMoreBytes(getMoreBytesState, consumed, examined, cancellationToken).ConfigureAwait(false);
			while (moreBuffer.Buffer.Length < minimumLength && !(moreBuffer.IsCompleted || moreBuffer.IsCanceled))
			{
				// We haven't got enough bytes. Try again.
				moreBuffer = await getMoreBytes(getMoreBytesState, consumed, moreBuffer.Buffer.End, cancellationToken).ConfigureAwait(false);
			}

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

	/// <summary>
	/// Tries to read the header of binary data.
	/// </summary>
	/// <param name="length">Receives the length of the binary data, when successful.</param>
	/// <returns>The result classification of the read operation.</returns>
	/// <inheritdoc cref="MessagePackPrimitives.TryReadBinHeader(ReadOnlySpan{byte}, out uint, out int)" path="/remarks" />
	public DecodeResult TryReadBinHeader(out uint length)
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

	/// <summary>
	/// Tries to read the header of a string.
	/// </summary>
	/// <param name="length">Receives the length of the next string, when successful.</param>
	/// <returns>The result classification of the read operation.</returns>
	/// <inheritdoc cref="MessagePackPrimitives.TryReadStringHeader(ReadOnlySpan{byte}, out uint, out int)" path="/remarks" />
	public DecodeResult TryReadStringHeader(out uint length)
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

	[DoesNotReturn]
	private static DecodeResult ThrowUnreachable() => throw new UnreachableException();

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
#if NET
			initializedChars += decoder.GetChars(this.reader.UnreadSpan.Slice(0, bytesRead), charArray.AsSpan(initializedChars), flush);
#else
			unsafe
			{
				fixed (byte* pUnreadSpan = this.reader.UnreadSpan)
				{
					fixed (char* pCharArray = &charArray[initializedChars])
					{
						initializedChars += decoder.GetChars(pUnreadSpan, bytesRead, pCharArray, charArray.Length - initializedChars, flush);
					}
				}
			}
#endif
			this.reader.Advance(bytesRead);
		}

		value = new string(charArray, 0, initializedChars);
		ArrayPool<char>.Shared.Return(charArray);
		return DecodeResult.Success;
	}

	/// <summary>
	/// A non-<see langword="ref" /> structure that can be used to recreate a <see cref="MessagePackStreamingReader"/> after
	/// an <see langword="await" /> expression.
	/// </summary>
	public struct BufferRefresh
	{
		/// <inheritdoc cref="MessagePackStreamingReader.CancellationToken" />
		internal CancellationToken CancellationToken { get; init; }

		/// <summary>
		/// Gets the buffer of msgpack already obtained.
		/// </summary>
		internal ReadOnlySequence<byte> Buffer { get; init; }

		/// <summary>
		/// Gets the delegate that can obtain more bytes.
		/// </summary>
		internal GetMoreBytesAsync? GetMoreBytes { get; init; }

		/// <summary>
		/// Gets the state object to supply to the <see cref="GetMoreBytes"/> delegate.
		/// </summary>
		internal object? GetMoreBytesState { get; init; }

		/// <summary>
		/// Gets a value indicating whether the <see cref="Buffer"/> contains all remaining bytes and <see cref="GetMoreBytes"/> will not provide more.
		/// </summary>
		internal bool EndOfStream { get; init; }
	}
}
