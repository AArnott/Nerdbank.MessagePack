// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Microsoft;

namespace Nerdbank.MessagePack;

/// <summary>
/// An interface for all message pack converters.
/// </summary>
/// <typeparam name="T">The data type that can be converted by this object.</typeparam>
/// <remarks>
/// <para>
/// Authors of derived types should review <see href="https://aarnott.github.io/Nerdbank.MessagePack/docs/custom-converters.html">this documentation</see>
/// for important guidance on implementing a converter.
/// </para>
/// <para>
/// Key points to remember about each <see cref="Write"/> or <see cref="Read"/> method (or their async equivalents):
/// <list type="bullet">
/// <item>Read or write exactly one msgpack structure. Use an array or map header for multiple values.</item>
/// <item>Call <see cref="SerializationContext.DepthStep"/> before any significant work.</item>
/// <item>Delegate serialization of sub-values to a converter obtained using <see cref="SerializationContext.GetConverter{T}()"/> rather than making a top-level call back to <see cref="MessagePackSerializer"/>.</item>
/// </list>
/// </para>
/// <para>
/// Implementations are encouraged to implement <see cref="IMessagePackConverterJsonSchemaProvider"/> in order to support
/// <see cref="MessagePackSerializer.GetJsonSchema(ITypeShape)"/>.
/// </para>
/// </remarks>
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
	/// <remarks>
	/// Implementations of this method should not flush the writer.
	/// </remarks>
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
	public virtual ValueTask WriteAsync(MessagePackAsyncWriter writer, T? value, SerializationContext context)
	{
		Requires.NotNull(writer);
		context.CancellationToken.ThrowIfCancellationRequested();

		MessagePackWriter syncWriter = writer.CreateWriter();
		this.Write(ref syncWriter, value, context);
		syncWriter.Flush();

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
	public virtual async ValueTask<T?> ReadAsync(MessagePackAsyncReader reader, SerializationContext context)
	{
		Requires.NotNull(reader);
		context.CancellationToken.ThrowIfCancellationRequested();

		ReadOnlySequence<byte> buffer = await reader.ReadNextStructureAsync(context).ConfigureAwait(false);
		T? result = Deserialize(buffer, context);
		reader.AdvanceTo(buffer.End);
		return result;

		T? Deserialize(ReadOnlySequence<byte> buffer, SerializationContext context)
		{
			MessagePackReader msgpackReader = new(buffer);
			return this.Read(ref msgpackReader, context);
		}
	}

	/// <summary>
	/// Gets the <see href="https://json-schema.org/">JSON schema</see> that resembles the data structure that this converter can serialize and deserialize.
	/// </summary>
	/// <param name="context">A means to obtain schema fragments for inclusion when your converter delegates to other converters.</param>
	/// <returns>The fragment of JSON schema that describes the value written by this converter, or <see langword="null" /> if this method has not been overridden.</returns>
	/// <remarks>
	/// <para>
	/// Implementations should return a new instance of <see cref="JsonObject"/> that represents the JSON schema fragment for every caller.
	/// A shared instance <em>may</em> be used to call <see cref="JsonNode.DeepClone"/> and the result returned.
	/// </para>
	/// <para>
	/// Custom converters that do <em>not</em> override this method will lead to a JSON schema that does not describe the written data, and allows any data as input.
	/// </para>
	/// <para>
	/// If the converter delegates to other converters, the schemas for those sub-values can be obtained for inclusion in the returned schema
	/// by calling <see cref="JsonSchemaContext.GetJsonSchema{T}()"/> on the <paramref name="context"/>.
	/// </para>
	/// </remarks>
	public virtual JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape) => null;

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

	/// <inheritdoc/>
	IMessagePackConverter IMessagePackConverter.WrapWithReferencePreservation() => this.WrapWithReferencePreservation();

	/// <inheritdoc cref="IMessagePackConverter.WrapWithReferencePreservation" />
	internal virtual MessagePackConverter<T> WrapWithReferencePreservation() => typeof(T).IsValueType ? this : new ReferencePreservingConverter<T>(this);

	protected static JsonValue? CreateJsonValue(object? value)
	{
		return value switch
		{
			null => null,
			string v => JsonValue.Create(v),
			short v => JsonValue.Create(v),
			int v => JsonValue.Create(v),
			long v => JsonValue.Create(v),
			float v => JsonValue.Create(v),
			double v => JsonValue.Create(v),
			decimal v => JsonValue.Create(v),
			bool v => JsonValue.Create(v),
			byte v => JsonValue.Create(v),
			sbyte v => JsonValue.Create(v),
			ushort v => JsonValue.Create(v),
			uint v => JsonValue.Create(v),
			ulong v => JsonValue.Create(v),
			char v => JsonValue.Create(v),
			_ => throw new NotSupportedException($"Unsupported object type: {value.GetType().FullName}"),
		};
	}
}
