// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Pipelines;

namespace Nerdbank.PolySerializer.Converters;

public class AsyncWriter(PipeWriter pipeWriter)
{
	internal BufferMemoryWriter bufferWriter = new(pipeWriter);

	/// <summary>
	/// Ensures everything previously written has been flushed to the underlying <see cref="IBufferWriter{T}"/>.
	/// </summary>
	public void Flush() => bufferWriter.Commit();

	/// <summary>
	/// Flushes the pipe if the buffer is getting full.
	/// </summary>
	/// <param name="context">The serialization context.</param>
	/// <returns>A task to await before writing further.</returns>
	public ValueTask FlushIfAppropriateAsync(SerializationContext context)
	{
		if (this.IsTimeToFlush(context))
		{
			// We need to commit our own writer first or the PipeWriter may discard the buffer we've written to.
			this.Flush();
			return FlushAsync(pipeWriter, context.CancellationToken);
		}
		else
		{
			return default;
		}

		static async ValueTask FlushAsync(PipeWriter pipeWriter, CancellationToken cancellationToken)
		{
			FlushResult flushResult = await pipeWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
			if (flushResult.IsCanceled)
			{
				throw new OperationCanceledException();
			}

			if (flushResult.IsCompleted)
			{
				throw new EndOfStreamException("The receiver has stopped listening.");
			}
		}
	}

	/// <summary>
	/// Gets a value indicating whether it is time to flush the pipe.
	/// </summary>
	/// <param name="context">The serialization context.</param>
	/// <returns><see langword="true" /> if the pipe buffers are reaching their preferred capacity; <see langword="false" /> otherwise.</returns>
	public bool IsTimeToFlush(SerializationContext context)
	{
		return pipeWriter.CanGetUnflushedBytes && pipeWriter.UnflushedBytes > context.UnflushedBytesThreshold;
	}

	/// <summary>
	/// Gets a value indicating whether it is time to flush the pipe.
	/// </summary>
	/// <param name="context">The serialization context.</param>
	/// <param name="syncWriter">The synchronous writer that may have unflushed bytes to consider as well.</param>
	/// <returns><see langword="true" /> if the pipe buffers are reaching their preferred capacity; <see langword="false" /> otherwise.</returns>
	public bool IsTimeToFlush(SerializationContext context, int unflushedBytes)
	{
		return pipeWriter.CanGetUnflushedBytes && (pipeWriter.UnflushedBytes + unflushedBytes) > context.UnflushedBytesThreshold;
	}
}
