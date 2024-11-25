// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using Microsoft;

namespace Nerdbank.MessagePack;

/// <summary>
/// A primitive types reader for the MessagePack format that reads from a <see cref="PipeReader"/>.
/// </summary>
/// <param name="pipeReader">The pipe reader to decode from.</param>
/// <remarks>
/// <para>
/// This is an async capable and slower alternative to <see cref="MessagePackReader"/> with fewer methods,
/// making the sync version more generally useful.
/// It is useful when implementing the async virtual methods on <see cref="MessagePackConverter{T}"/>.
/// </para>
/// <see href="https://github.com/msgpack/msgpack/blob/master/spec.md">The MessagePack spec.</see>.
/// </remarks>
/// <exception cref="MessagePackSerializationException">Thrown when reading methods fail due to invalid data.</exception>
/// <exception cref="EndOfStreamException">Thrown by reading methods when there are not enough bytes to read the required value.</exception>
[Experimental("NBMsgPackAsync")]
public class MessagePackAsyncReader(PipeReader pipeReader)
{
	private ReadResult? lastReadResult;
	private bool timeForAdvanceTo;

	/// <summary>
	/// Gets the fully-capable, synchronous reader.
	/// </summary>
	/// <param name="minimumDesiredBufferedStructures">The number of top-level structures expected by the caller that must be included in the returned buffer.</param>
	/// <param name="context">The serialization context.</param>
	/// <returns>The buffer, for use in creating a <see cref="MessagePackReader"/>. This buffer may be larger than needed to include <paramref name="minimumDesiredBufferedStructures"/>.</returns>
	/// <remarks>
	/// The caller must take care to call <see cref="AdvanceTo(SequencePosition)"/> with <see cref="MessagePackReader.Position"/> before any other methods on this class after this call.
	/// </remarks>
	/// <exception cref="OperationCanceledException">Thrown if <see cref="SerializationContext.CancellationToken"/> is canceled or <see cref="PipeReader.ReadAsync(CancellationToken)"/> returns a result where <see cref="ReadResult.IsCanceled"/> is <see langword="true" />.</exception>
	/// <exception cref="EndOfStreamException">Thrown if <see cref="PipeReader.ReadAsync(CancellationToken)"/> returns a result where <see cref="ReadResult.IsCompleted"/> is <see langword="true" /> and yet the buffer is not sufficient to satisfy <paramref name="minimumDesiredBufferedStructures"/>.</exception>
	public async ValueTask<ReadOnlySequence<byte>> ReadNextStructuresAsync(int minimumDesiredBufferedStructures, SerializationContext context)
		=> (await this.ReadNextStructuresAsync(minimumDesiredBufferedStructures, minimumDesiredBufferedStructures, context).ConfigureAwait(false)).Buffer;

	/// <summary>
	/// Gets the fully-capable, synchronous reader.
	/// </summary>
	/// <param name="minimumDesiredBufferedStructures">The number of top-level structures expected by the caller that must be included in the returned buffer.</param>
	/// <param name="countUpTo">The number of top-level structures to count and report on in the result.</param>
	/// <param name="context">The serialization context.</param>
	/// <returns>
	/// The buffer, for use in creating a <see cref="MessagePackReader"/>, which will contain at least <paramref name="minimumDesiredBufferedStructures"/> top-level structures and may include more.
	/// Also returns the number of top-level structures included in the buffer that were counted (up to <paramref name="countUpTo"/>).
	/// </returns>
	/// <remarks>
	/// The caller must take care to call <see cref="AdvanceTo(SequencePosition)"/> with <see cref="MessagePackReader.Position"/> before any other methods on this class after this call.
	/// </remarks>
	/// <exception cref="OperationCanceledException">Thrown if <see cref="SerializationContext.CancellationToken"/> is canceled or <see cref="PipeReader.ReadAsync(CancellationToken)"/> returns a result where <see cref="ReadResult.IsCanceled"/> is <see langword="true" />.</exception>
	/// <exception cref="EndOfStreamException">Thrown if <see cref="PipeReader.ReadAsync(CancellationToken)"/> returns a result where <see cref="ReadResult.IsCompleted"/> is <see langword="true" /> and yet the buffer is not sufficient to satisfy <paramref name="minimumDesiredBufferedStructures"/>.</exception>
	public async ValueTask<(ReadOnlySequence<byte> Buffer, int IncludedStructures)> ReadNextStructuresAsync(int minimumDesiredBufferedStructures, int countUpTo, SerializationContext context)
	{
		Requires.Argument(minimumDesiredBufferedStructures >= 0, nameof(minimumDesiredBufferedStructures), "A non-negative integer is required.");
		Requires.Argument(countUpTo >= minimumDesiredBufferedStructures, nameof(countUpTo), "Count must be at least as large as minimumDesiredBufferedStructures.");

		ReadResult readResult = default;
		int skipCount = 0;
		while (skipCount < minimumDesiredBufferedStructures)
		{
			readResult = await this.ReadAsync(context.CancellationToken).ConfigureAwait(false);
			MessagePackReader reader = new(readResult.Buffer);
			skipCount = 0;
			for (; skipCount < countUpTo; skipCount++)
			{
				if (!reader.TrySkip(context))
				{
					if (skipCount >= minimumDesiredBufferedStructures)
					{
						// We got what we needed.
						break;
					}
					else if (readResult.IsCompleted)
					{
						throw new EndOfStreamException();
					}
					else if (readResult.IsCanceled)
					{
						throw new OperationCanceledException();
					}

					// We need to read more data.
					this.AdvanceTo(readResult.Buffer.Start, readResult.Buffer.End);
					break;
				}
			}
		}

		return (readResult.Buffer, skipCount);
	}

	/// <inheritdoc cref="MessagePackReader.TryReadNil"/>
	/// <param name="cancellationToken">A cancellation token.</param>
	public async ValueTask<bool> TryReadNilAsync(CancellationToken cancellationToken)
	{
		ReadResult readResult = await this.ReadAsync(cancellationToken).ConfigureAwait(false);
		if (readResult.IsCanceled)
		{
			throw new OperationCanceledException();
		}

		MessagePackReader reader = new(readResult.Buffer);
		bool isNil = reader.TryReadNil();
		this.AdvanceTo(reader.Position);
		return isNil;
	}

	/// <inheritdoc cref="MessagePackReader.NextMessagePackType"/>
	/// <param name="cancellationToken">A cancellation token.</param>
	public async ValueTask<MessagePackType> TryPeekNextMessagePackTypeAsync(CancellationToken cancellationToken)
	{
		ReadResult readResult = await this.ReadAsync(cancellationToken).ConfigureAwait(false);
		if (readResult.IsCanceled)
		{
			throw new OperationCanceledException();
		}

		MessagePackReader reader = new(readResult.Buffer);
		MessagePackType result = reader.NextMessagePackType;
		this.AdvanceTo(readResult.Buffer.Start); // this was a peek. Don't consume anything.
		return result;
	}

	/// <inheritdoc cref="MessagePackReader.ReadMapHeader"/>
	/// <param name="cancellationToken">A cancellation token.</param>
	public async ValueTask<int> ReadMapHeaderAsync(CancellationToken cancellationToken)
	{
		ReadResult readResult = await this.ReadAsync(cancellationToken).ConfigureAwait(false);
		if (readResult.IsCanceled)
		{
			throw new OperationCanceledException();
		}

retry:
		MessagePackPrimitives.DecodeResult decodeResult = MessagePackPrimitives.TryReadMapHeader(readResult.Buffer, out uint count, out SequencePosition readTo);
		switch (decodeResult)
		{
			case MessagePackPrimitives.DecodeResult.Success:
				this.AdvanceTo(readTo);
				return (int)count;
			case MessagePackPrimitives.DecodeResult.TokenMismatch:
				throw MessagePackReader.ThrowInvalidCode(readResult.Buffer.FirstSpan[0]);
			case MessagePackPrimitives.DecodeResult.InsufficientBuffer when !readResult.IsCompleted:
				// Fetch more data.
				this.AdvanceTo(readResult.Buffer.Start, readResult.Buffer.End);
				readResult = await this.ReadAsync(cancellationToken).ConfigureAwait(false);
				goto retry;
			case MessagePackPrimitives.DecodeResult.EmptyBuffer or MessagePackPrimitives.DecodeResult.InsufficientBuffer:
				throw new EndOfStreamException();
			default: throw new UnreachableException();
		}
	}

	/// <inheritdoc cref="MessagePackReader.ReadArrayHeader"/>
	/// <param name="cancellationToken">A cancellation token.</param>
	public async ValueTask<int> ReadArrayHeaderAsync(CancellationToken cancellationToken)
	{
		ReadResult readResult = await this.ReadAsync(cancellationToken).ConfigureAwait(false);
		if (readResult.IsCanceled)
		{
			throw new OperationCanceledException();
		}

retry:
		MessagePackPrimitives.DecodeResult decodeResult = MessagePackPrimitives.TryReadArrayHeader(readResult.Buffer, out uint count, out SequencePosition readTo);
		switch (decodeResult)
		{
			case MessagePackPrimitives.DecodeResult.Success:
				this.AdvanceTo(readTo);
				return (int)count;
			case MessagePackPrimitives.DecodeResult.TokenMismatch:
				throw MessagePackReader.ThrowInvalidCode(readResult.Buffer.FirstSpan[0]);
			case MessagePackPrimitives.DecodeResult.InsufficientBuffer when !readResult.IsCompleted:
				// Fetch more data.
				this.AdvanceTo(readResult.Buffer.Start, readResult.Buffer.End);
				readResult = await this.ReadAsync(cancellationToken).ConfigureAwait(false);
				goto retry;
			case MessagePackPrimitives.DecodeResult.EmptyBuffer or MessagePackPrimitives.DecodeResult.InsufficientBuffer:
				throw new EndOfStreamException();
			default: throw new UnreachableException();
		}
	}

	/// <summary>
	/// Retrieves enough data from the pipe to read the next msgpack structure.
	/// </summary>
	/// <param name="context">The serialization context.</param>
	/// <returns>A task that completes when enough bytes have been retrieved into local buffers.</returns>
	/// <remarks>
	/// After awaiting this method, the next msgpack structure can be retrieved by a call to <see cref="PipeReader.ReadAsync(CancellationToken)"/>.
	/// </remarks>
	public async ValueTask BufferNextStructureAsync(SerializationContext context)
	{
		ReadOnlySequence<byte> buffer = await this.ReadNextStructureAsync(context).ConfigureAwait(false);
		this.AdvanceTo(buffer.Start);
	}

	/// <summary>
	/// Retrieves enough data from the pipe to read the next msgpack structure.
	/// </summary>
	/// <param name="context">The serialization context.</param>
	/// <returns>The buffer containing exactly the next structure.</returns>
	/// <remarks>
	/// After a successful call to this method, the caller *must* call <see cref="AdvanceTo(SequencePosition, SequencePosition)"/>,
	/// usually with <see cref="ReadOnlySequence{T}.End"/> to indicate that the next msgpack structure has been consumed.
	/// After that call, the caller must *not* read the buffer again as it will have been recycled.
	/// </remarks>
	public async ValueTask<ReadOnlySequence<byte>> ReadNextStructureAsync(SerializationContext context)
	{
		while (true)
		{
			ReadResult readBuffer = await this.ReadAsync(context.CancellationToken).ConfigureAwait(false);
			if (readBuffer.IsCanceled)
			{
				throw new OperationCanceledException();
			}

			MessagePackReader msgpackReader = new(readBuffer.Buffer);
			if (msgpackReader.TrySkip(context))
			{
				return readBuffer.Buffer.Slice(0, msgpackReader.Position);
			}
			else
			{
				// Indicate that we haven't got enough buffer so that the next ReadAsync will guarantee us more.
				this.AdvanceTo(readBuffer.Buffer.Start, readBuffer.Buffer.End);
				if (readBuffer.IsCompleted)
				{
					throw new EndOfStreamException();
				}
			}
		}
	}

	/// <summary>
	/// Skips the next msgpack structure in the pipe.
	/// </summary>
	/// <param name="context">The serialization context.</param>
	/// <returns>A task that completes when done reading past the next msgpack structure.</returns>
	public async ValueTask SkipAsync(SerializationContext context)
	{
		while (true)
		{
			ReadResult readBuffer = await this.ReadAsync(context.CancellationToken).ConfigureAwait(false);
			if (readBuffer.IsCanceled)
			{
				throw new OperationCanceledException();
			}

			if (TrySkip(readBuffer.Buffer, context, out SequencePosition newPosition))
			{
				this.AdvanceTo(newPosition);
				return;
			}
			else
			{
				// Indicate that we haven't got enough buffer so that the next ReadAsync will guarantee us more.
				this.AdvanceTo(readBuffer.Buffer.Start, readBuffer.Buffer.End);
				if (readBuffer.IsCompleted)
				{
					throw new EndOfStreamException();
				}
			}
		}

		bool TrySkip(ReadOnlySequence<byte> buffer, SerializationContext context, out SequencePosition newPosition)
		{
			MessagePackReader msgpackReader = new(buffer);
			if (msgpackReader.TrySkip(context))
			{
				newPosition = msgpackReader.Position;
				return true;
			}
			else
			{
				newPosition = buffer.Start;
				return false;
			}
		}
	}

	/// <summary>
	/// Follows up on a prior call to <see cref="ReadNextStructureAsync"/> to report some subset of the sequence as consumed.
	/// </summary>
	/// <param name="consumed">The position that was consumed. Should be <see cref="ReadOnlySequence{T}.End"/> on the value returned from <see cref="ReadNextStructureAsync"/> if the whole sequence was deserialize.</param>
	public void AdvanceTo(SequencePosition consumed) => this.AdvanceTo(consumed, consumed);

	/// <summary>
	/// Follows up on a prior call to <see cref="ReadNextStructureAsync"/> to report some subset of the sequence as consumed.
	/// </summary>
	/// <param name="consumed">The position that was consumed. Should be <see cref="ReadOnlySequence{T}.End"/> on the value returned from <see cref="ReadNextStructureAsync"/> if the whole sequence was deserialize.</param>
	/// <param name="examined">The position that was examined up to. Should always be no earlier in the sequence than <paramref name="consumed" />.</param>
	public void AdvanceTo(SequencePosition consumed, SequencePosition examined)
	{
		Verify.Operation(this.timeForAdvanceTo && this.lastReadResult.HasValue, "Call ReadAsync first.");
		ReadResult lastReadResult = this.lastReadResult.Value;

		if (lastReadResult.Buffer.End.Equals(examined))
		{
			// The caller has examined everything we have.
			pipeReader.AdvanceTo(consumed, examined);
			this.lastReadResult = null;
		}
		else
		{
			// We still have data left to read in our local buffer.
			this.lastReadResult = new(lastReadResult.Buffer.Slice(consumed), lastReadResult.IsCanceled, lastReadResult.IsCompleted);
		}

		this.timeForAdvanceTo = false;
	}

	/// <summary>
	/// Immediately returns the last read result if non-empty, or pulls on the <see cref="PipeReader"/> for more data and returns that.
	/// </summary>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>The current or next buffer to read.</returns>
	private async ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken)
	{
		Verify.Operation(!this.timeForAdvanceTo, "Calls out of order. Call AdvanceTo first.");

		// PipeReader.ReadAsync is *slow*.
		// Only pull on the pipeReader if we don't already have a buffer to read from.
		if (this.lastReadResult is not { Buffer.IsEmpty: false })
		{
			this.lastReadResult = await pipeReader.ReadAsync(cancellationToken).ConfigureAwait(false);
		}

		this.timeForAdvanceTo = true;
		return this.lastReadResult.Value;
	}
}
