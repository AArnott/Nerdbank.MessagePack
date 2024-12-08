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
	private MessagePackStreamingReader.BufferRefresh? refresh;
	private bool readerReturned = true;

	/// <summary>
	/// Gets a cancellation token to consider for calls into this object.
	/// </summary>
	public required CancellationToken CancellationToken { get; init; }

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
	public async ValueTask<int> BufferNextStructuresAsync(int minimumDesiredBufferedStructures, int countUpTo, SerializationContext context)
	{
		Requires.Argument(minimumDesiredBufferedStructures >= 0, nameof(minimumDesiredBufferedStructures), "A non-negative integer is required.");
		Requires.Argument(countUpTo >= minimumDesiredBufferedStructures, nameof(countUpTo), "Count must be at least as large as minimumDesiredBufferedStructures.");
		this.ThrowIfReaderNotReturned();

		int skipCount = 0;
		while (skipCount < minimumDesiredBufferedStructures)
		{
			if (this.refresh is null)
			{
				await this.ReadAsync().ConfigureAwait(false);
			}

			MessagePackStreamingReader reader = new(this.Refresh);
			skipCount = 0;
			for (; skipCount < countUpTo; skipCount++)
			{
				if (reader.TrySkip(context) is MessagePackPrimitives.DecodeResult.InsufficientBuffer or MessagePackPrimitives.DecodeResult.EmptyBuffer)
				{
					if (skipCount >= minimumDesiredBufferedStructures)
					{
						return skipCount;
					}

					break;
				}
			}

			if (skipCount == countUpTo)
			{
				break;
			}

			await this.ReadAsync().ConfigureAwait(false);
		}

		return skipCount;
	}

	/// <summary>
	/// Retrieves enough data from the pipe to read the next msgpack structure.
	/// </summary>
	/// <param name="context">The serialization context.</param>
	/// <returns>A task that completes when enough bytes have been retrieved into local buffers.</returns>
	/// <remarks>
	/// After awaiting this method, the next msgpack structure can be retrieved by a call to <see cref="PipeReader.ReadAsync(CancellationToken)"/>.
	/// </remarks>
	public async ValueTask BufferNextStructureAsync(SerializationContext context) => await this.BufferNextStructuresAsync(1, 1, context).ConfigureAwait(false);

	public async ValueTask ReadAsync()
	{
		this.ThrowIfReaderNotReturned();

		MessagePackStreamingReader reader;
		if (this.refresh.HasValue)
		{
			reader = new(this.refresh.Value);
			this.refresh = await reader.ReplenishBufferAsync().ConfigureAwait(false);
		}
		else
		{
			ReadResult readResult = await pipeReader.ReadAsync(this.CancellationToken).ConfigureAwait(false);
			if (readResult.IsCanceled)
			{
				throw new OperationCanceledException();
			}

			reader = new(
				readResult.Buffer,
				static (state, consumed, examined, ct) =>
				{
					PipeReader pipeReader = (PipeReader)state!;
					pipeReader.AdvanceTo(consumed, examined);
					return pipeReader.ReadAsync(ct);
				},
				pipeReader);
			this.refresh = reader.GetExchangeInfo();
		}
	}

	public MessagePackStreamingReader CreateReader()
	{
		ThrowIfReaderNotReturned();
		this.readerReturned = false;
		return new(this.Refresh);
	}

	public MessagePackReader CreateReader2()
	{
		ThrowIfReaderNotReturned();
		this.readerReturned = false;
		return new(this.Refresh.Buffer);
	}

	public void ReturnReader(ref MessagePackStreamingReader reader)
	{
		this.refresh = reader.GetExchangeInfo();

		// Clear the reader to prevent accidental reuse by the caller.
		reader = default;

		readerReturned = true;
	}

	public void ReturnReader(ref MessagePackReader reader)
	{
		MessagePackStreamingReader.BufferRefresh refresh = this.Refresh;
		refresh = refresh with { Buffer = refresh.Buffer.Slice(reader.Position) };
		this.refresh = refresh;

		// Clear the reader to prevent accidental reuse by the caller.
		reader = default;

		readerReturned = true;
	}

	private MessagePackStreamingReader.BufferRefresh Refresh => this.refresh ?? throw new InvalidOperationException($"Call {nameof(this.ReadAsync)} first.");

	private void ThrowIfReaderNotReturned()
	{
		Verify.Operation(this.readerReturned, "The previous reader must be returned before creating a new one.");
	}
}
