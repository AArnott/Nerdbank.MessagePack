// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Pipelines;
using Microsoft;

namespace Nerdbank.MessagePack;

/// <summary>
/// A primitive types reader for the MessagePack format that reads from a <see cref="PipeReader"/>.
/// </summary>
/// <param name="pipeReader">The pipe reader to decode from. <see cref="PipeReader.Complete(Exception?)"/> is <em>not</em> called on this at the conclusion of deserialization, and the reader is left at the position after the last msgpack byte read.</param>
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
public class MessagePackAsyncReader(PipeReader pipeReader) : IDisposable
{
	private MessagePackStreamingReader.BufferRefresh? refresh;
	private bool readerReturned = true;

	/// <summary>
	/// Indicates whether this reader is a peek reader that should not advance the PipeReader on disposal.
	/// </summary>
	private bool isPeekReader;

	/// <inheritdoc cref="MessagePackStreamingReader.ExpectedRemainingStructures"/>
	private uint expectedRemainingStructures;

	/// <summary>
	/// Gets a cancellation token to consider for calls into this object.
	/// </summary>
	public required CancellationToken CancellationToken { get; init; }

	private MessagePackStreamingReader.BufferRefresh Refresh => this.refresh ?? throw new InvalidOperationException($"Call {nameof(this.ReadAsync)} first.");

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
	public async ValueTask BufferNextStructureAsync(SerializationContext context) => await this.BufferNextStructuresAsync(1, 1, context).ConfigureAwait(false);

	/// <summary>
	/// Fills the buffer with msgpack bytes to decode.
	/// If the buffer already has bytes, <em>more</em> will be retrieved and added.
	/// </summary>
	/// <returns>An async task.</returns>
	/// <exception cref="OperationCanceledException">Thrown if <see cref="CancellationToken"/> is canceled.</exception>
	public async ValueTask ReadAsync()
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
			ReadResult readResult = await pipeReader.ReadAsync(this.CancellationToken).ConfigureAwait(false);
			if (readResult.IsCanceled)
			{
				throw new OperationCanceledException();
			}

			reader = new(
				readResult.Buffer,
				readResult.IsCompleted ? null : FetchMoreBytesAsync,
				pipeReader);
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
	/// Retrieves a <see cref="MessagePackStreamingReader"/>, which is suitable for
	/// decoding msgpack from a buffer without throwing any exceptions, even if the buffer is incomplete.
	/// </summary>
	/// <returns>A <see cref="MessagePackStreamingReader"/>.</returns>
	/// <remarks>
	/// The result must be returned with <see cref="ReturnReader(ref MessagePackStreamingReader)"/>
	/// before using this <see cref="MessagePackAsyncReader"/> again.
	/// </remarks>
	public MessagePackStreamingReader CreateStreamingReader()
	{
		this.ThrowIfReaderNotReturned();
		this.readerReturned = false;
		return new(this.Refresh)
		{
			ExpectedRemainingStructures = this.expectedRemainingStructures,
		};
	}

	/// <summary>
	/// Retrieves a <see cref="MessagePackReader"/>, which is suitable for
	/// decoding msgpack from a buffer that is known to have enough bytes for the decoding.
	/// </summary>
	/// <returns>A <see cref="MessagePackReader"/>.</returns>
	/// <remarks>
	/// The result must be returned with <see cref="ReturnReader(ref MessagePackReader)"/>
	/// before using this <see cref="MessagePackAsyncReader"/> again.
	/// </remarks>
	public MessagePackReader CreateBufferedReader()
	{
		this.ThrowIfReaderNotReturned();
		this.readerReturned = false;
		return new(this.Refresh.Buffer)
		{
			ExpectedRemainingStructures = this.expectedRemainingStructures,
		};
	}

	/// <summary>
	/// Creates a new <see cref="MessagePackAsyncReader"/> at this reader's current position.
	/// The two readers may then be used independently without impacting each other,
	/// while sharing the underlying buffer.
	/// </summary>
	/// <returns>A new <see cref="MessagePackAsyncReader"/>.</returns>
	/// <remarks>
	/// <para>
	/// The original reader and the peek reader share the underlying buffer.
	/// If the peek reader requests to increase the buffer size, the increased buffer
	/// will be available to the original reader as well.
	/// </para>
	/// <para>
	/// Reading from the peek reader will not advance the read position of the original reader.
	/// The original reader's state will not be disrupted by operations on the peek reader.
	/// </para>
	/// <para>
	/// The returned reader must be disposed when no longer needed.
	/// </para>
	/// </remarks>
	public MessagePackAsyncReader CreatePeekReader()
	{
		this.ThrowIfReaderNotReturned();

		// Create a new reader instance that shares the same buffer state
		return new MessagePackAsyncReader(pipeReader)
		{
			CancellationToken = this.CancellationToken,
			refresh = this.refresh, // Copy current state
			expectedRemainingStructures = this.expectedRemainingStructures,
			readerReturned = true, // The peek reader starts in a returned state
			isPeekReader = true, // Mark this as a peek reader
		};
	}

	/// <summary>
	/// Returns a previously obtained reader when the caller is done using it,
	/// and applies the given reader's position to <em>this</em> reader so that
	/// future reads move continuously forward in the msgpack stream.
	/// </summary>
	/// <param name="reader">The reader to return.</param>
	public void ReturnReader(ref MessagePackStreamingReader reader)
	{
		this.refresh = reader.GetExchangeInfo();
		this.expectedRemainingStructures = reader.ExpectedRemainingStructures;

		// Clear the reader to prevent accidental reuse by the caller.
		reader = default;

		this.readerReturned = true;
	}

	/// <inheritdoc cref="ReturnReader(ref MessagePackStreamingReader)"/>
	public void ReturnReader(ref MessagePackReader reader)
	{
		MessagePackStreamingReader.BufferRefresh refresh = this.Refresh;
		refresh = refresh with { Buffer = refresh.Buffer.Slice(reader.Position) };
		this.refresh = refresh;
		this.expectedRemainingStructures = reader.ExpectedRemainingStructures;

		// Clear the reader to prevent accidental reuse by the caller.
		reader = default;

		this.readerReturned = true;
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		if (!this.readerReturned)
		{
			throw new InvalidOperationException("A reader was not returned before disposing this object.");
		}

		// Only advance the PipeReader if this is not a peek reader
		if (this.refresh.HasValue && !this.isPeekReader)
		{
			// Update the PipeReader so it knows where we left off.
			pipeReader.AdvanceTo(this.refresh.Value.Buffer.Start);
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
	internal int GetBufferedStructuresCount(int countUpTo, SerializationContext context, out bool reachedMaxCount)
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
			if (reader.TrySkip(ref contextCopy) is not MessagePackPrimitives.DecodeResult.Success)
			{
				return skipCount;
			}
		}

		reachedMaxCount = true;
		return skipCount;
	}

	/// <summary>
	/// Gets a value indicating whether we've reached the end of the stream.
	/// </summary>
	/// <param name="forceRead"><see langword="true" /> to guarantee that we call <see cref="ReadAsync"/> unless we already hit the EOF marker, even if we know we're not at the end of the stream.</param>
	/// <returns>A boolean value.</returns>
	/// <remarks>
	/// This method <em>may</em> call <see cref="ReadAsync"/>.
	/// </remarks>
	internal async ValueTask<bool> GetIsEndOfStreamAsync(bool forceRead)
	{
		if (this.refresh is null or { Buffer.IsEmpty: true, EndOfStream: false } || (forceRead && this.refresh is not { EndOfStream: true }))
		{
			await this.ReadAsync().ConfigureAwait(false);
		}

		return this.Refresh.EndOfStream && this.Refresh.Buffer.IsEmpty;
	}

	/// <summary>
	/// Advances the reader to the end of the top-level structure that we started reading.
	/// </summary>
	/// <returns>An async task representing the advance operation.</returns>
	internal async ValueTask AdvanceToEndOfTopLevelStructureAsync()
	{
		if (this.expectedRemainingStructures > 0)
		{
			MessagePackStreamingReader reader = this.CreateStreamingReader();
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

	private void ThrowIfReaderNotReturned()
	{
		Verify.Operation(this.readerReturned, "The previous reader must be returned before creating a new one.");
	}
}
