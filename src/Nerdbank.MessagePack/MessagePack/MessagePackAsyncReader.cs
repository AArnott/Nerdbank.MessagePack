// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using Microsoft;

namespace Nerdbank.PolySerializer.MessagePack;

/// <summary>
/// A primitive types reader for the MessagePack format that reads from a <see cref="PipeReader"/>.
/// </summary>
/// <remarks>
/// <para>
/// This is an async capable and slower alternative to <see cref="MessagePackReader"/> with fewer methods,
/// making the sync version more generally useful.
/// It is useful when implementing the async virtual methods on <see cref="MessagePackConverter{T}"/>.
/// </para>
/// <see href="https://github.com/msgpack/msgpack/blob/master/spec.md">The MessagePack spec.</see>.
/// </remarks>
/// <exception cref="SerializationException">Thrown when reading methods fail due to invalid data.</exception>
/// <exception cref="EndOfStreamException">Thrown by reading methods when there are not enough bytes to read the required value.</exception>
[Experimental("NBMsgPackAsync")]
public class MessagePackAsyncReader : AsyncReader
{
	/// <param name="pipeReader">The pipe reader to decode from. <see cref="PipeReader.Complete(Exception?)"/> is <em>not</em> called on this at the conclusion of deserialization, and the reader is left at the position after the last msgpack byte read.</param>
	public MessagePackAsyncReader(PipeReader pipeReader, MsgPackDeformatter deformatter)
		: base(pipeReader, deformatter)
	{
	}

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
	/// <exception cref="OperationCanceledException">Thrown if <see cref="SerializationContext.CancellationToken"/> is canceled or <see cref="PipeReader.ReadAsync(CancellationToken)"/> returns a result where <see cref="ReadResult.IsCanceled"/> is <see langword="true" />.</exception>
	/// <exception cref="EndOfStreamException">Thrown if <see cref="PipeReader.ReadAsync(CancellationToken)"/> returns a result where <see cref="ReadResult.IsCompleted"/> is <see langword="true" /> and yet the buffer is not sufficient to satisfy <paramref name="minimumDesiredBufferedStructures"/>.</exception>
	public override async ValueTask<int> BufferNextStructuresAsync(int minimumDesiredBufferedStructures, int countUpTo, SerializationContext context)
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

			skipCount = this.GetBufferedStructuresCount(countUpTo, context, out _);
			if (skipCount >= minimumDesiredBufferedStructures)
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
	public override async ValueTask BufferNextStructureAsync(SerializationContext context) => await this.BufferNextStructuresAsync(1, 1, context).ConfigureAwait(false);

	/// <summary>
	/// Fills the buffer with msgpack bytes to decode.
	/// If the buffer already has bytes, <em>more</em> will be retrieved and added.
	/// </summary>
	/// <returns>An async task.</returns>
	/// <exception cref="OperationCanceledException">Thrown if <see cref="CancellationToken"/> is canceled.</exception>
	public override async ValueTask ReadAsync()
	{
		this.ThrowIfReaderNotReturned();

		MessagePackStreamingReader reader;
		if (this.refresh.HasValue)
		{
			reader = new(this.refresh.Value);
			this.refresh = await reader.FetchMoreBytesAsync().ConfigureAwait(false);
		}
		else
		{
			ReadResult readResult = await this.PipeReader.ReadAsync(this.CancellationToken).ConfigureAwait(false);
			if (readResult.IsCanceled)
			{
				throw new OperationCanceledException();
			}

			reader = new(
				readResult.Buffer,
				readResult.IsCompleted ? null : FetchMoreBytesAsync,
				this.PipeReader);
			this.refresh = reader.GetExchangeInfo();

			static ValueTask<ReadResult> FetchMoreBytesAsync(object? state, SequencePosition consumed, SequencePosition examined, CancellationToken ct)
			{
				PipeReader pipeReader = (PipeReader)state!;
				pipeReader.AdvanceTo(consumed, examined);
				return pipeReader.ReadAsync(ct);
			}
		}
	}

	/// <summary>
	/// Counts how many structures are buffered, starting at the reader's current position.
	/// </summary>
	/// <param name="countUpTo">The max number of structures to count.</param>
	/// <param name="context">The serialization context.</param>
	/// <param name="reachedMaxCount">Receives a value indicating whether we reached <paramref name="countUpTo"/> before running out of buffer, so there could be even more full structures in the buffer left uncounted.</param>
	/// <returns>The number of structures in the buffer, up to <paramref name="countUpTo"/>.</returns>
	/// <remarks>
	/// If the reader is positioned at something other than the start of a stream, and somewhere deep in an object graph,
	/// the <paramref name="countUpTo"/> should be set to avoid walking *up* the graph to count shallower structures.
	/// Behavior is undefined if this is not followed.
	/// </remarks>
	internal override int GetBufferedStructuresCount(int countUpTo, SerializationContext context, out bool reachedMaxCount)
	{
		this.ThrowIfReaderNotReturned();

		reachedMaxCount = false;
		if (this.refresh is null)
		{
			return 0;
		}

		MessagePackStreamingReader reader = new(this.refresh.Value);
		int skipCount = 0;
		for (; skipCount < countUpTo; skipCount++)
		{
			// Present a copy of the context because we don't want TrySkip to retain state between each of our calls.
			SerializationContext contextCopy = context;
			if (reader.TrySkip(ref contextCopy) is not DecodeResult.Success)
			{
				return skipCount;
			}
		}

		reachedMaxCount = true;
		return skipCount;
	}

	/// <summary>
	/// Advances the reader to the end of the top-level structure that we started reading.
	/// </summary>
	/// <returns>An async task representing the advance operation.</returns>
	internal override async ValueTask AdvanceToEndOfTopLevelStructureAsync()
	{
		if (this.expectedRemainingStructures > 0)
		{
			StreamingReader reader = this.CreateStreamingReader();
			SerializationContext context = new()
			{
				MidSkipRemainingCount = this.expectedRemainingStructures,
			};
			while (reader.TrySkip(ref context).NeedsMoreBytes())
			{
				reader = new(await reader.FetchMoreBytesAsync().ConfigureAwait(false));
			}

			this.ReturnReader(ref reader);
		}
	}
}
