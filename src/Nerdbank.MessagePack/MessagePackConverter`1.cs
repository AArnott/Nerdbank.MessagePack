// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Pipelines;
using Microsoft;

namespace Nerdbank.MessagePack;

/// <summary>
/// An interface for all message pack converters.
/// </summary>
/// <typeparam name="T">The data type that can be converted by this object.</typeparam>
public abstract class MessagePackConverter<T> : IMessagePackConverter
{
	/// <summary>
	/// Gets a value indicating whether callers should prefer the async methods on this object.
	/// </summary>
	/// <value>Unless overridden in a derived converter, this value is always <see langword="false"/>.</value>
	/// <remarks>
	/// Derived types that override the <see cref="SerializeAsync"/> and/or <see cref="DeserializeAsync"/> methods
	/// should also override this property and have it return <see langword="true" />.
	/// </remarks>
	public virtual bool PreferAsyncSerialization => false;

	/// <summary>
	/// Serializes an instance of <typeparamref name="T"/>.
	/// </summary>
	/// <param name="writer">The writer to use.</param>
	/// <param name="value">The value to serialize.</param>
	/// <param name="context">Context for the serialization.</param>
	public abstract void Serialize(ref MessagePackWriter writer, ref T? value, SerializationContext context);

	/// <summary>
	/// Deserializes an instance of <typeparamref name="T"/>.
	/// </summary>
	/// <param name="reader">The reader to use.</param>
	/// <param name="context">Context for the deserialization.</param>
	/// <returns>The deserialized value.</returns>
	public abstract T? Deserialize(ref MessagePackReader reader, SerializationContext context);

	/// <summary>
	/// Serializes an instance of <typeparamref name="T"/>.
	/// </summary>
	/// <param name="pipeWriter">The writer to use.</param>
	/// <param name="value">The value to serialize.</param>
	/// <param name="context">Context for the serialization.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>A task that tracks the async serialization.</returns>
	/// <remarks>
	/// <para>
	/// The default implementation delegates to <see cref="Serialize"/> and then flushes the data to the pipe
	/// if the buffers are getting relatively full.
	/// </para>
	/// <para>
	/// Derived classes should only override this method if they may write a lot of data.
	/// They should do so with the intent of writing fragments of data at a time and periodically call
	/// <see cref="FlushIfAppropriateAsync(PipeWriter, SerializationContext, CancellationToken)"/>
	/// in order to keep the size of memory buffers from growing too much.
	/// </para>
	/// </remarks>
	public virtual ValueTask SerializeAsync(PipeWriter pipeWriter, T? value, SerializationContext context, CancellationToken cancellationToken)
	{
		Requires.NotNull(pipeWriter);

		cancellationToken.ThrowIfCancellationRequested();
		MessagePackWriter writer = new(pipeWriter);
		this.Serialize(ref writer, ref value, context);
		writer.Flush();

		// On our way out, pause to flush the pipe if a lot of data has accumulated in the buffer.
		return FlushIfAppropriateAsync(pipeWriter, context, cancellationToken);
	}

	/// <summary>
	/// Deserializes an instance of <typeparamref name="T"/>.
	/// </summary>
	/// <param name="reader">The reader to use.</param>
	/// <param name="context">Context for the deserialization.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>The deserialized value.</returns>
	/// <remarks>
	/// <para>The default implementation delegates to <see cref="Deserialize"/> after ensuring there is sufficient buffer to read the next structure.</para>
	/// <para>
	/// Derived classes should only override this method if they may read a lot of data.
	/// They should do so with the intent to be able to read some data then asynchronously wait for data before reading more
	/// in order to reduce the amount of memory required to buffer.
	/// </para>
	/// </remarks>
	public virtual async ValueTask<T?> DeserializeAsync(PipeReader reader, SerializationContext context, CancellationToken cancellationToken)
	{
		Requires.NotNull(reader);
		cancellationToken.ThrowIfCancellationRequested();

		while (true)
		{
			ReadResult readBuffer = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
			if (readBuffer.IsCanceled)
			{
				throw new OperationCanceledException();
			}

			if (TryRead(readBuffer.Buffer, context, out SequencePosition newPosition, out T? result))
			{
				reader.AdvanceTo(newPosition);
				return result;
			}
			else if (readBuffer.IsCompleted)
			{
				throw new EndOfStreamException();
			}
			else
			{
				// Indicate that we haven't got enough buffer so that the next ReadAsync will guarantee us more.
				reader.AdvanceTo(newPosition, readBuffer.Buffer.End);
			}
		}

		bool TryRead(ReadOnlySequence<byte> buffer, SerializationContext context, out SequencePosition newPosition, out T? result)
		{
			MessagePackReader msgpackReader = new(buffer);
			if (!msgpackReader.CreatePeekReader().TrySkip())
			{
				result = default;
				newPosition = buffer.Start;
				return false;
			}

			result = this.Deserialize(ref msgpackReader, context);
			newPosition = msgpackReader.Position;
			return true;
		}
	}

	/// <inheritdoc/>
	void IMessagePackConverter.Serialize(ref MessagePackWriter writer, ref object? value, SerializationContext context)
	{
		T? typedValue = (T?)value;
		this.Serialize(ref writer, ref typedValue, context);
	}

	/// <inheritdoc/>
	object? IMessagePackConverter.Deserialize(ref MessagePackReader reader, SerializationContext context)
	{
		return this.Deserialize(ref reader, context);
	}

	protected static void WriteNil(PipeWriter pipeWriter)
	{
		MessagePackWriter writer = new(pipeWriter);
		writer.WriteNil();
		writer.Flush();
	}

	protected static ValueTask FlushIfAppropriateAsync(PipeWriter pipeWriter, SerializationContext context, CancellationToken cancellationToken)
	{
		Requires.NotNull(pipeWriter);

		return IsTimeToFlush(pipeWriter, context)
			? FlushAsync(pipeWriter, cancellationToken)
			: default;

		static async ValueTask FlushAsync(PipeWriter pipeWriter, CancellationToken cancellationToken)
		{
			await pipeWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
		}
	}

	protected static bool IsTimeToFlush(PipeWriter pipeWriter, SerializationContext context)
	{
		Requires.NotNull(pipeWriter);
		return pipeWriter.CanGetUnflushedBytes && pipeWriter.UnflushedBytes > context.UnflushedBytesThreshold;
	}
}
