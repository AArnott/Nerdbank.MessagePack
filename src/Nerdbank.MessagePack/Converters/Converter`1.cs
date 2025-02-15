// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft;
using Nerdbank.PolySerializer.MessagePack;

namespace Nerdbank.PolySerializer.Converters;

public abstract class Converter<T>() : Converter(typeof(T))
{
	/// <summary>
	/// Serializes an instance of <typeparamref name="T"/>.
	/// </summary>
	/// <param name="writer">The writer to use.</param>
	/// <param name="value">The value to serialize.</param>
	/// <param name="context">Context for the serialization.</param>
	/// <remarks>
	/// Implementations of this method should not flush the writer.
	/// </remarks>
	public abstract void Write(ref Writer writer, in T? value, SerializationContext context);

	/// <summary>
	/// Deserializes an instance of <typeparamref name="T"/>.
	/// </summary>
	/// <param name="reader">The reader to use.</param>
	/// <param name="context">Context for the deserialization.</param>
	/// <returns>The deserialized value.</returns>
	public abstract T? Read(ref Reader reader, SerializationContext context);

	/// <summary>
	/// Serializes an instance of <typeparamref name="T"/>.
	/// </summary>
	/// <param name="writer">The writer to use.</param>
	/// <param name="value">The value to serialize.</param>
	/// <param name="context">Context for the serialization.</param>
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
	public virtual ValueTask WriteAsync(AsyncWriter writer, T? value, SerializationContext context)
	{
		Requires.NotNull(writer);
		context.CancellationToken.ThrowIfCancellationRequested();

		Writer syncWriter = writer.CreateWriter();
		this.Write(ref syncWriter, value, context);
		writer.ReturnWriter(ref syncWriter);

		// On our way out, pause to flush the pipe if a lot of data has accumulated in the buffer.
		return writer.FlushIfAppropriateAsync(context);
	}

	/// <summary>
	/// Deserializes an instance of <typeparamref name="T"/>.
	/// </summary>
	/// <param name="reader">The reader to use.</param>
	/// <param name="context">Context for the deserialization.</param>
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
	public virtual async ValueTask<T?> ReadAsync(AsyncReader reader, SerializationContext context)
	{
		Requires.NotNull(reader);
		context.CancellationToken.ThrowIfCancellationRequested();

		await reader.BufferNextStructureAsync(context).ConfigureAwait(false);
		Reader syncReader = reader.CreateBufferedReader();
		T? result = this.Read(ref syncReader, context);
		reader.ReturnReader(ref syncReader);
		return result;
	}

	/// <inheritdoc/>
	public override sealed void WriteObject(ref Writer writer, object? value, SerializationContext context) => this.Write(ref writer, (T?)value, context);

	/// <inheritdoc/>
	public override sealed object? ReadObject(ref Reader reader, SerializationContext context) => this.Read(ref reader, context);

	/// <inheritdoc/>
	[Experimental("NBMsgPackAsync")]
	[EditorBrowsable(EditorBrowsableState.Never)] // Use the generic methods instead.
	public override sealed ValueTask WriteObjectAsync(AsyncWriter writer, object? value, SerializationContext context) => this.WriteAsync(writer, (T?)value, context);

	/// <inheritdoc/>
	[Experimental("NBMsgPackAsync")]
	[EditorBrowsable(EditorBrowsableState.Never)] // Use the generic methods instead.
	public override sealed async ValueTask<object?> ReadObjectAsync(AsyncReader reader, SerializationContext context) => await this.ReadAsync(reader, context).ConfigureAwait(false);

	internal override Converter WrapWithReferencePreservationCore() => new ReferencePreservingConverter<T>(this);
}
