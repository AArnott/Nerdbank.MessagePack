// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Pipelines;

namespace Nerdbank.PolySerializer.Converters;

public class AsyncWriter(PipeWriter pipeWriter, Formatter formatter)
{
	public Formatter Formatter => formatter;

	private BufferMemoryWriter bufferWriter = new(pipeWriter);

	internal ref BufferMemoryWriter Buffer => ref this.bufferWriter;

	/// <summary>
	/// Gets the fully-capable, synchronous writer.
	/// </summary>
	/// <returns>The writer.</returns>
	/// <remarks>
	/// The caller must take care to call <see cref="ReturnWriter(ref Writer)"/> before discarding the writer.
	/// </remarks>
	public Writer CreateWriter()
	{
#if !NET
		// ref fields are not supported on .NET Framework, so we have to prepare to copy the struct.
		this.bufferWriter.Commit();
#endif

		return new(new BufferWriter(ref this.bufferWriter), this.Formatter);
	}

	/// <summary>
	/// Applies the bytes written with a writer previously obtained from <see cref="CreateWriter"/> back to this object.
	/// </summary>
	/// <param name="writer">The writer to return. It should not be used after this.</param>
	public void ReturnWriter(ref Writer writer)
	{
		writer.Buffer.Commit();

#if !NET
		// ref fields are not supported on .NET Framework, so we have to copy the struct since it'll disappear.
		this.bufferWriter = writer.Buffer.BufferMemoryWriter;
#endif

		// Help prevent misuse of the writer after it's been returned.
		writer = default;
	}

	/// <summary>
	/// Ensures everything previously written has been flushed to the underlying <see cref="IBufferWriter{T}"/>.
	/// </summary>
	public void Flush() => this.bufferWriter.Commit();

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
	/// <param name="writer">The synchronous writer that may have unflushed bytes to consider as well.</param>
	/// <returns><see langword="true" /> if the pipe buffers are reaching their preferred capacity; <see langword="false" /> otherwise.</returns>
	public bool IsTimeToFlush(SerializationContext context, Writer writer)
	{
		return pipeWriter.CanGetUnflushedBytes && (pipeWriter.UnflushedBytes + writer.UnflushedBytes) > context.UnflushedBytesThreshold;
	}

	public void WriteNull()
	{
		Writer writer = this.CreateWriter();
		writer.WriteNull();
		this.ReturnWriter(ref writer);
	}

	/// <inheritdoc cref="Writer.WriteNull"/>
	public void WriteNil() => this.WriteNull();
}
