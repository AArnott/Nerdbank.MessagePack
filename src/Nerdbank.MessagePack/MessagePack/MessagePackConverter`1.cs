// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Microsoft;

namespace Nerdbank.PolySerializer.MessagePack;

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
/// <item>Delegate serialization of sub-values to a converter obtained using <see cref="SerializationContext.GetConverter{T}(ITypeShapeProvider)"/> rather than making a top-level call back to <see cref="MessagePackSerializer"/>.</item>
/// </list>
/// </para>
/// <para>
/// Implementations are encouraged to override <see cref="GetJsonSchema(Nerdbank.MessagePack.JsonSchemaContext, ITypeShape)"/> in order to support
/// <see cref="MessagePackSerializer.GetJsonSchema(ITypeShape)"/>.
/// </para>
/// </remarks>
public abstract class MessagePackConverter<T> : Converter<T>, IMessagePackConverter
{
	object? IMessagePackConverter.ReadObject(ref MessagePackReader reader, SerializationContext context) => this.Read(ref reader, context);

	void IMessagePackConverter.WriteObject(ref MessagePackWriter writer, object? value, SerializationContext context) => this.Write(ref writer, (T?)value, context);

	[Experimental("NBMsgPackAsync")]
	async ValueTask<object?> IMessagePackConverter.ReadObjectAsync(MessagePackAsyncReader reader, SerializationContext context) => await this.ReadAsync(reader, context).ConfigureAwait(false);

	[Experimental("NBMsgPackAsync")]
	ValueTask IMessagePackConverter.WriteObjectAsync(MessagePackAsyncWriter writer, object? value, SerializationContext context) => this.WriteAsync(writer, (T?)value, context);

	[Experimental("NBMsgPackAsync")]
	Task<bool> IMessagePackConverter.SkipToIndexValueAsync(MessagePackAsyncReader reader, object? indexArg, SerializationContext context)
	{
		throw new NotImplementedException();
	}

	[Experimental("NBMsgPackAsync")]
	Task<bool> IMessagePackConverter.SkipToPropertyValueAsync(MessagePackAsyncReader reader, IPropertyShape propertyShape, SerializationContext context)
	{
		throw new NotImplementedException();
	}

	public sealed override T? Read(ref Reader reader, SerializationContext context)
	{
		MessagePackReader realReader = MessagePackReader.FromReader(reader);
		T? result = this.Read(ref realReader, context);
		reader = realReader.ToReader();
		return result;
	}

	public sealed override void Write(ref Writer writer, in T? value, SerializationContext context)
	{
		MessagePackWriter realWriter = MessagePackWriter.FromWriter(writer);
		this.Write(ref realWriter, value, context);
		writer = realWriter.ToWriter();
	}

	[Experimental("NBMsgPackAsync")]
	public sealed override ValueTask<T?> ReadAsync(AsyncReader reader, SerializationContext context)
		=> this.ReadAsync((MessagePackAsyncReader)reader, context);

	[Experimental("NBMsgPackAsync")]
	public sealed override ValueTask WriteAsync(AsyncWriter writer, T? value, SerializationContext context)
		=> this.WriteAsync((MessagePackAsyncWriter)writer, value, context);

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
	public virtual async ValueTask<T?> ReadAsync(MessagePackAsyncReader reader, SerializationContext context)
	{
		Requires.NotNull(reader);
		context.CancellationToken.ThrowIfCancellationRequested();

		await reader.BufferNextStructureAsync(context).ConfigureAwait(false);
		MessagePackReader syncReader = reader.CreateBufferedReader();
		T? result = this.Read(ref syncReader, context);
		reader.ReturnReader(ref syncReader);
		return result;
	}

	[Experimental("NBMsgPackAsync")]
	public sealed override ValueTask<bool> SkipToIndexValueAsync(AsyncReader reader, object? index, SerializationContext context)
		=> this.SkipToIndexValueAsync((MessagePackAsyncReader)reader, index, context);

	[Experimental("NBMsgPackAsync")]
	public sealed override ValueTask<bool> SkipToPropertyValueAsync(AsyncReader reader, IPropertyShape propertyShape, SerializationContext context)
		=> this.SkipToPropertyValueAsync((MessagePackAsyncReader)reader, propertyShape, context);

	[Experimental("NBMsgPackAsync")]
	public virtual ValueTask<bool> SkipToIndexValueAsync(MessagePackAsyncReader reader, object? index, SerializationContext context)
		=> throw new NotSupportedException($"The {this.GetType().FullName} converter does not support this operation.");

	[Experimental("NBMsgPackAsync")]
	public virtual ValueTask<bool> SkipToPropertyValueAsync(MessagePackAsyncReader reader, IPropertyShape propertyShape, SerializationContext context)
		=> throw new NotSupportedException($"The {this.GetType().FullName} converter does not support this operation.");

	/// <inheritdoc cref="WrapWithReferencePreservation" />
	internal override Converter WrapWithReferencePreservationCore() => new ReferencePreservingConverter<T>(this);

	/// <summary>
	/// Creates a JSON schema fragment that provides a cursory description of a MessagePack extension.
	/// </summary>
	/// <param name="extensionCode">The extension code used.</param>
	/// <returns>A JSON schema fragment.</returns>
	/// <remarks>
	/// This is provided as a helper function for <see cref="GetJsonSchema(JsonSchemaContext, ITypeShape)"/> implementations.
	/// </remarks>
	protected static JsonObject CreateMsgPackExtensionSchema(sbyte extensionCode) => new()
	{
		["type"] = "string",
		["pattern"] = FormattableString.Invariant($"^msgpack extension {extensionCode} as base64: "),
	};
}
