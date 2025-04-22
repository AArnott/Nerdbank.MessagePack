// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using Microsoft;

namespace Nerdbank.MessagePack;

/// <summary>
/// A primitive types writer for the MessagePack format that writes to a <see cref="PipeWriter"/>.
/// </summary>
/// <param name="pipeWriter">The pipe writer to encode to.</param>
/// <remarks>
/// <para>
/// This is an async capable and slower alternative to <see cref="MessagePackWriter"/> with fewer methods,
/// making the sync version more generally useful.
/// It is useful when implementing the async virtual methods on <see cref="MessagePackConverter{T}"/>.
/// </para>
/// <see href="https://github.com/msgpack/msgpack/blob/master/spec.md">The MessagePack spec.</see>.
/// </remarks>
[Experimental("NBMsgPackAsync")]
public class MessagePackAsyncWriter(PipeWriter pipeWriter)
{
	private BufferWriter bufferWriter = new(pipeWriter);

	/// <summary>
	/// The delegate type that may be provided to the <see cref="Write{TState}(SyncWriter{TState}, TState)"/> method.
	/// </summary>
	/// <typeparam name="T">The type of state that may be given to the writer.</typeparam>
	/// <param name="writer">The delegate to invoke to do the synchronous writing.</param>
	/// <param name="state">The state to pass to the <paramref name="writer"/>.</param>
	public delegate void SyncWriter<T>(ref MessagePackWriter writer, T state);

	/// <summary>
	/// Gets the fully-capable, synchronous writer.
	/// </summary>
	/// <returns>The writer.</returns>
	/// <remarks>
	/// The caller must take care to call <see cref="ReturnWriter(ref MessagePackWriter)"/> before discarding the writer.
	/// </remarks>
	public MessagePackWriter CreateWriter()
	{
		// ref fields are not supported on .NET Framework, so we have to prepare to copy the struct.
		this.bufferWriter.Commit();

		return new(this.bufferWriter);
	}

	/// <summary>
	/// Applies the bytes written with a writer previously obtained from <see cref="CreateWriter"/> back to this object.
	/// </summary>
	/// <param name="writer">The writer to return. It should not be used after this.</param>
	public void ReturnWriter(ref MessagePackWriter writer)
	{
		writer.Flush();

		this.bufferWriter = writer.Writer;

		// Help prevent misuse of the writer after it's been returned.
		writer = default;
	}

	/// <summary>
	/// Ensures everything previously written has been flushed to the underlying <see cref="IBufferWriter{T}"/>.
	/// </summary>
	public void Flush() => this.bufferWriter.Commit();

	/// <summary>
	/// Creates a sync writer for purposes of serializing a message.
	/// </summary>
	/// <typeparam name="TState">The type of state that may be given to the writer.</typeparam>
	/// <param name="writer">The delegate to invoke to do the synchronous writing.</param>
	/// <param name="state">State to pass to the writer.</param>
	public void Write<TState>(SyncWriter<TState> writer, TState state)
	{
		Requires.NotNull(writer);

		MessagePackWriter syncWriter = this.CreateWriter();
		writer(ref syncWriter, state);
		this.ReturnWriter(ref syncWriter);
	}

	/// <inheritdoc cref="MessagePackWriter.WriteNil"/>
	public void WriteNil()
	{
		MessagePackWriter writer = this.CreateWriter();
		writer.WriteNil();
		this.ReturnWriter(ref writer);
	}

	/// <inheritdoc cref="MessagePackWriter.WriteArrayHeader(int)"/>
	public void WriteArrayHeader(int count) => this.WriteArrayHeader(checked((uint)count));

	/// <inheritdoc cref="MessagePackWriter.WriteArrayHeader(uint)"/>
	public void WriteArrayHeader(uint count)
	{
		Span<byte> span = this.bufferWriter.GetSpan(5);
		Assumes.True(MessagePackPrimitives.TryWriteArrayHeader(span, count, out int written));
		this.bufferWriter.Advance(written);
	}

	/// <inheritdoc cref="MessagePackWriter.WriteMapHeader(int)"/>
	public void WriteMapHeader(int count) => this.WriteMapHeader(checked((uint)count));

	/// <inheritdoc cref="MessagePackWriter.WriteMapHeader(uint)"/>
	public void WriteMapHeader(uint count)
	{
		Span<byte> span = this.bufferWriter.GetSpan(5);
		Assumes.True(MessagePackPrimitives.TryWriteMapHeader(span, count, out int written));
		this.bufferWriter.Advance(written);
	}

	/// <inheritdoc cref="MessagePackWriter.WriteRaw(ReadOnlySpan{byte})"/>
	public void WriteRaw(ReadOnlySpan<byte> bytes)
	{
		MessagePackWriter writer = this.CreateWriter();
		writer.WriteRaw(bytes);
		this.ReturnWriter(ref writer);
	}

	/// <inheritdoc cref="MessagePackWriter.WriteRaw(in ReadOnlySequence{byte})"/>
	public void WriteRaw(ReadOnlySequence<byte> bytes)
	{
		MessagePackWriter writer = this.CreateWriter();
		writer.WriteRaw(bytes);
		this.ReturnWriter(ref writer);
	}

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
	public bool IsTimeToFlush(in SerializationContext context)
	{
		return pipeWriter.CanGetUnflushedBytes && pipeWriter.UnflushedBytes > context.UnflushedBytesThreshold;
	}

	/// <summary>
	/// Gets a value indicating whether it is time to flush the pipe.
	/// </summary>
	/// <param name="context">The serialization context.</param>
	/// <param name="syncWriter">The synchronous writer that may have unflushed bytes to consider as well.</param>
	/// <returns><see langword="true" /> if the pipe buffers are reaching their preferred capacity; <see langword="false" /> otherwise.</returns>
	public bool IsTimeToFlush(in SerializationContext context, in MessagePackWriter syncWriter)
	{
		return pipeWriter.CanGetUnflushedBytes && (pipeWriter.UnflushedBytes + syncWriter.UnflushedBytes) > context.UnflushedBytesThreshold;
	}
}
