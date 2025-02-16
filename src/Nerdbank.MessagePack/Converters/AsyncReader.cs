// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Pipelines;
using Microsoft;

namespace Nerdbank.PolySerializer.Converters;

public abstract class AsyncReader : IDisposable
{
	private protected bool readerReturned = true;
	private protected BufferRefresh? refresh;

	/// <inheritdoc cref="MessagePackStreamingReader.ExpectedRemainingStructures"/>
	protected uint expectedRemainingStructures;

	public AsyncReader(PipeReader pipeReader, Deformatter deformatter)
	{
		this.PipeReader = Requires.NotNull(pipeReader);
		this.Deformatter = Requires.NotNull(deformatter);
	}

	public PipeReader PipeReader { get; }

	public Deformatter Deformatter { get; }

	/// <summary>
	/// Gets a cancellation token to consider for calls into this object.
	/// </summary>
	public required CancellationToken CancellationToken { get; init; }

	private protected BufferRefresh Refresh => this.refresh ?? throw new InvalidOperationException($"Call {nameof(this.ReadAsync)} first.");

	/// <summary>
	/// Retrieves a <see cref="MessagePackStreamingReader"/>, which is suitable for
	/// decoding msgpack from a buffer without throwing any exceptions, even if the buffer is incomplete.
	/// </summary>
	/// <returns>A <see cref="MessagePackStreamingReader"/>.</returns>
	/// <remarks>
	/// The result must be returned with <see cref="ReturnReader(ref MessagePackStreamingReader)"/>
	/// before using this <see cref="MessagePackAsyncReader"/> again.
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
	/// Retrieves a <see cref="MessagePackReader"/>, which is suitable for
	/// decoding msgpack from a buffer that is known to have enough bytes for the decoding.
	/// </summary>
	/// <returns>A <see cref="MessagePackReader"/>.</returns>
	/// <remarks>
	/// The result must be returned with <see cref="ReturnReader(ref MessagePackReader)"/>
	/// before using this <see cref="MessagePackAsyncReader"/> again.
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

	/// <inheritdoc cref="ReturnReader(ref MessagePackStreamingReader)"/>
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

	public abstract ValueTask ReadAsync();

	public abstract ValueTask<int> BufferNextStructuresAsync(int minimumDesiredBufferedStructures, int countUpTo, SerializationContext context);

	public abstract ValueTask BufferNextStructureAsync(SerializationContext context);

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

	internal abstract int GetBufferedStructuresCount(int countUpTo, SerializationContext context, out bool reachedMaxCount);

	internal abstract ValueTask AdvanceToEndOfTopLevelStructureAsync();

	private protected void ThrowIfReaderNotReturned()
	{
		Verify.Operation(this.readerReturned, "The previous reader must be returned before creating a new one.");
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
	/// A non-<see langword="ref" /> structure that can be used to recreate a <see cref="MessagePackStreamingReader"/> after
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

		internal Deformatter Deformatter { get; init; }
	}
}
