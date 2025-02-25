// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Pipelines;
using Microsoft;

namespace ShapeShift.Converters;

/// <summary>
/// An async, streaming reader for deserializing a data stream.
/// </summary>
public class AsyncReader : IDisposable
{
	private bool readerReturned = true;
	private BufferRefresh? refresh;

	/// <inheritdoc cref="StreamingReader.ExpectedRemainingStructures"/>
	private uint expectedRemainingStructures;

	/// <summary>
	/// Initializes a new instance of the <see cref="AsyncReader"/> class.
	/// </summary>
	/// <param name="pipeReader">The pipe to read data from.</param>
	/// <param name="deformatter">The deformatter that can decode the data.</param>
	public AsyncReader(PipeReader pipeReader, Deformatter deformatter)
	{
		this.PipeReader = Requires.NotNull(pipeReader);
		this.Deformatter = Requires.NotNull(deformatter);
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
	/// Gets the pipe reader to read data from.
	/// </summary>
	public PipeReader PipeReader { get; }

	/// <summary>
	/// Gets the deformatter that can decode the data.
	/// </summary>
	public Deformatter Deformatter { get; }

	/// <summary>
	/// Gets a cancellation token to consider for calls into this object.
	/// </summary>
	public required CancellationToken CancellationToken { get; init; }

	private BufferRefresh Refresh => this.refresh ?? throw new InvalidOperationException($"Call {nameof(this.ReadAsync)} first.");

	/// <summary>
	/// Retrieves a <see cref="StreamingReader"/>, which is suitable for
	/// decoding msgpack from a buffer without throwing any exceptions, even if the buffer is incomplete.
	/// </summary>
	/// <returns>A <see cref="StreamingReader"/>.</returns>
	/// <remarks>
	/// The result must be returned with <see cref="ReturnReader(ref StreamingReader)"/>
	/// before using this <see cref="AsyncReader"/> again.
	/// </remarks>
	public StreamingReader CreateStreamingReader()
	{
		this.ThrowIfReaderNotReturned();
		this.readerReturned = false;
		return new(this.Refresh)
		{
			ExpectedRemainingStructures = this.expectedRemainingStructures,
		};
	}

	/// <summary>
	/// Retrieves a <see cref="Reader"/>, which is suitable for
	/// decoding msgpack from a buffer that is known to have enough bytes for the decoding.
	/// </summary>
	/// <returns>A <see cref="Reader"/>.</returns>
	/// <remarks>
	/// The result must be returned with <see cref="ReturnReader(ref Reader)"/>
	/// before using this <see cref="AsyncReader"/> again.
	/// </remarks>
	public Reader CreateBufferedReader()
	{
		this.ThrowIfReaderNotReturned();
		this.readerReturned = false;
		return new(this.Refresh.Buffer, this.Deformatter)
		{
			ExpectedRemainingStructures = this.expectedRemainingStructures,
		};
	}

	/// <summary>
	/// Returns a previously obtained reader when the caller is done using it,
	/// and applies the given reader's position to <em>this</em> reader so that
	/// future reads move continuously forward in the msgpack stream.
	/// </summary>
	/// <param name="reader">The reader to return.</param>
	public void ReturnReader(ref StreamingReader reader)
	{
		this.refresh = reader.GetExchangeInfo();
		this.expectedRemainingStructures = reader.ExpectedRemainingStructures;

		// Clear the reader to prevent accidental reuse by the caller.
		reader = default;

		this.readerReturned = true;
	}

	/// <inheritdoc cref="ReturnReader(ref StreamingReader)"/>
	public void ReturnReader(ref Reader reader)
	{
		AsyncReader.BufferRefresh refresh = this.Refresh;
		refresh = refresh with { Buffer = refresh.Buffer.Slice(reader.SequenceReader.Position) };
		this.refresh = refresh;
		this.expectedRemainingStructures = reader.ExpectedRemainingStructures;

		// Clear the reader to prevent accidental reuse by the caller.
		reader = default;

		this.readerReturned = true;
	}

	/// <summary>
	/// Fills the buffer with bytes to decode.
	/// If the buffer already has bytes, <em>more</em> will be retrieved and added.
	/// </summary>
	/// <returns>An async task.</returns>
	/// <exception cref="OperationCanceledException">Thrown if <see cref="CancellationToken"/> is canceled.</exception>
	public virtual async ValueTask ReadAsync()
	{
		this.ThrowIfReaderNotReturned();

		StreamingReader reader;
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
				this.PipeReader,
				this.Deformatter);
			this.refresh = reader.GetExchangeInfo();

			static ValueTask<ReadResult> FetchMoreBytesAsync(object? state, SequencePosition consumed, SequencePosition examined, CancellationToken ct)
			{
				PipeReader pipeReader = (PipeReader)state!;
				pipeReader.AdvanceTo(consumed, examined);
				return pipeReader.ReadAsync(ct);
			}
		}
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		if (!this.readerReturned)
		{
			throw new InvalidOperationException("A reader was not returned before disposing this object.");
		}

		if (this.refresh.HasValue)
		{
			// Update the PipeReader so it knows where we left off.
			this.PipeReader.AdvanceTo(this.refresh.Value.Buffer.Start);
		}
	}

	/// <summary>
	/// Gets the fully-capable, synchronous reader.
	/// </summary>
	/// <param name="minimumDesiredBufferedStructures">The number of top-level structures expected by the caller that must be included in the returned buffer.</param>
	/// <param name="countUpTo">The number of top-level structures to count and report on in the result.</param>
	/// <param name="context">The serialization context.</param>
	/// <returns>
	/// The buffer, for use in creating a <see cref="Reader"/>, which will contain at least <paramref name="minimumDesiredBufferedStructures"/> top-level structures and may include more.
	/// Also returns the number of top-level structures included in the buffer that were counted (up to <paramref name="countUpTo"/>).
	/// </returns>
	/// <exception cref="OperationCanceledException">Thrown if <see cref="SerializationContext.CancellationToken"/> is canceled or <see cref="PipeReader.ReadAsync(CancellationToken)"/> returns a result where <see cref="ReadResult.IsCanceled"/> is <see langword="true" />.</exception>
	/// <exception cref="EndOfStreamException">Thrown if <see cref="PipeReader.ReadAsync(CancellationToken)"/> returns a result where <see cref="ReadResult.IsCompleted"/> is <see langword="true" /> and yet the buffer is not sufficient to satisfy <paramref name="minimumDesiredBufferedStructures"/>.</exception>
	public virtual async ValueTask<int> BufferNextStructuresAsync(int minimumDesiredBufferedStructures, int countUpTo, SerializationContext context)
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
	public virtual async ValueTask BufferNextStructureAsync(SerializationContext context) => await this.BufferNextStructuresAsync(1, 1, context).ConfigureAwait(false);

	/// <inheritdoc cref="Reader.TryAdvanceToNextElement"/>
	public async ValueTask<bool> TryAdvanceToNextElementAsync(bool isFirstElement)
	{
		Verify.Operation(this.readerReturned, "This cannot be done before returning the reader with ReturnReader.");
		StreamingReader streamingReader = this.CreateStreamingReader();
		bool hasAnotherElement;
		while (streamingReader.TryAdvanceToNextElement(ref isFirstElement, out hasAnotherElement).NeedsMoreBytes())
		{
			streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
		}

		this.ReturnReader(ref streamingReader);
		return hasAnotherElement;
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
	internal virtual int GetBufferedStructuresCount(int countUpTo, SerializationContext context, out bool reachedMaxCount)
	{
		this.ThrowIfReaderNotReturned();

		reachedMaxCount = false;
		if (this.refresh is null)
		{
			return 0;
		}

		StreamingReader reader = new(this.refresh.Value);
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
	internal virtual async ValueTask AdvanceToEndOfTopLevelStructureAsync()
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

	/// <summary>
	/// Gets a value indicating whether we've reached the end of the stream.
	/// </summary>
	/// <returns>A boolean value.</returns>
	internal async ValueTask<bool> GetIsEndOfStreamAsync()
	{
		if (this.refresh is null or { Buffer.IsEmpty: true, EndOfStream: false })
		{
			await this.ReadAsync().ConfigureAwait(false);
		}

		return this.Refresh.EndOfStream && this.Refresh.Buffer.IsEmpty;
	}

	private void ThrowIfReaderNotReturned()
	{
		Verify.Operation(this.readerReturned, "The previous reader must be returned before creating a new one.");
	}

	/// <summary>
	/// A non-<see langword="ref" /> structure that can be used to recreate a <see cref="StreamingReader"/> after
	/// an <see langword="await" /> expression.
	/// </summary>
	public struct BufferRefresh
	{
		/// <inheritdoc cref="AsyncReader.CancellationToken" />
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

		/// <summary>
		/// Gets the deformatter used by the reader.
		/// </summary>
		internal Deformatter Deformatter { get; init; }
	}
}
