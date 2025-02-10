// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Pipelines;
using Microsoft;

namespace Nerdbank.PolySerializer.Converters;

public abstract class AsyncReader(PipeReader pipeReader) : IDisposable
{
	private protected bool readerReturned = true;
	private protected BufferRefresh? refresh;

	public PipeReader PipeReader => pipeReader;

	/// <summary>
	/// Gets a cancellation token to consider for calls into this object.
	/// </summary>
	public required CancellationToken CancellationToken { get; init; }

	private protected BufferRefresh Refresh => this.refresh ?? throw new InvalidOperationException($"Call {nameof(this.ReadAsync)} first.");

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
	}
}
