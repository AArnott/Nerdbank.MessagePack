// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
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
	/// Derived types that override the <see cref="WriteAsync"/> and/or <see cref="ReadAsync"/> methods
	/// should also override this property and have it return <see langword="true" />.
	/// </remarks>
	public virtual bool PreferAsyncSerialization => false;

	/// <summary>
	/// Serializes an instance of <typeparamref name="T"/>.
	/// </summary>
	/// <param name="writer">The writer to use.</param>
	/// <param name="value">The value to serialize.</param>
	/// <param name="context">Context for the serialization.</param>
	public abstract void Write(ref MessagePackWriter writer, in T? value, SerializationContext context);

	/// <summary>
	/// Deserializes an instance of <typeparamref name="T"/>.
	/// </summary>
	/// <param name="reader">The reader to use.</param>
	/// <param name="context">Context for the deserialization.</param>
	/// <returns>The deserialized value.</returns>
	public abstract T? Read(ref MessagePackReader reader, SerializationContext context);

	/// <summary>
	/// Serializes an instance of <typeparamref name="T"/>.
	/// </summary>
	/// <param name="writer">The writer to use.</param>
	/// <param name="value">The value to serialize.</param>
	/// <param name="context">Context for the serialization.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>A task that tracks the async serialization.</returns>
	/// <remarks>
	/// <para>
	/// The default implementation delegates to <see cref="Write"/> and then flushes the data to the pipe
	/// if the buffers are getting relatively full.
	/// </para>
	/// <para>
	/// Derived classes should only override this method if they may write a lot of data.
	/// They should do so with the intent of writing fragments of data at a time and periodically call
	/// <see cref="MessagePackAsyncWriter.FlushIfAppropriateAsync"/>
	/// in order to keep the size of memory buffers from growing too much.
	/// </para>
	/// </remarks>
	[Experimental("NBMsgPackAsync")]
	public virtual ValueTask WriteAsync(MessagePackAsyncWriter writer, T? value, SerializationContext context, CancellationToken cancellationToken)
	{
		Requires.NotNull(writer);
		cancellationToken.ThrowIfCancellationRequested();

		MessagePackWriter syncWriter = writer.CreateWriter();
		this.Write(ref syncWriter, value, context);
		syncWriter.Flush();

		// On our way out, pause to flush the pipe if a lot of data has accumulated in the buffer.
		return writer.FlushIfAppropriateAsync(context, cancellationToken);
	}

	/// <summary>
	/// Deserializes an instance of <typeparamref name="T"/>.
	/// </summary>
	/// <param name="reader">The reader to use.</param>
	/// <param name="context">Context for the deserialization.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>The deserialized value.</returns>
	/// <remarks>
	/// <para>The default implementation delegates to <see cref="Read"/> after ensuring there is sufficient buffer to read the next structure.</para>
	/// <para>
	/// Derived classes should only override this method if they may read a lot of data.
	/// They should do so with the intent to be able to read some data then asynchronously wait for data before reading more
	/// in order to reduce the amount of memory required to buffer.
	/// </para>
	/// </remarks>
	[Experimental("NBMsgPackAsync")]
	public virtual async ValueTask<T?> ReadAsync(MessagePackAsyncReader reader, SerializationContext context, CancellationToken cancellationToken)
	{
		Requires.NotNull(reader);
		cancellationToken.ThrowIfCancellationRequested();

		ReadOnlySequence<byte> buffer = await reader.ReadNextStructureAsync(context, cancellationToken).ConfigureAwait(false);
		T? result = Deserialize(buffer, context);
		reader.AdvanceTo(buffer.End);
		return result;

		T? Deserialize(ReadOnlySequence<byte> buffer, SerializationContext context)
		{
			MessagePackReader msgpackReader = new(buffer);
			return this.Read(ref msgpackReader, context);
		}
	}

	/// <inheritdoc/>
	void IMessagePackConverter.Write(ref MessagePackWriter writer, ref object? value, SerializationContext context)
	{
		this.Write(ref writer, (T?)value, context);
	}

	/// <inheritdoc/>
	object? IMessagePackConverter.Read(ref MessagePackReader reader, SerializationContext context)
	{
		return this.Read(ref reader, context);
	}
}
