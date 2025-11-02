// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Nerdbank.MessagePack;

/// <summary>
/// A non-generic, <see cref="object"/>-based base class for all message pack converters.
/// </summary>
public abstract class MessagePackConverter
{
	/// <summary>
	/// Gets a value indicating whether callers should prefer the async methods on this object.
	/// </summary>
	/// <value>Unless overridden in a derived converter, this value is always <see langword="false"/>.</value>
	/// <remarks>
	/// Derived types that override the <see cref="WriteObjectAsync"/> and/or <see cref="ReadObjectAsync"/> methods
	/// should also override this property and have it return <see langword="true" />.
	/// </remarks>
	public abstract bool PreferAsyncSerialization { get; }

	/// <summary>
	/// Gets the data type that this converter can serialize and deserialize.
	/// </summary>
	internal abstract Type DataType { get; }

	/// <summary>
	/// Serializes an instance of an object.
	/// </summary>
	/// <param name="writer">The writer to use.</param>
	/// <param name="value">The value to serialize.</param>
	/// <param name="context">Context for the serialization.</param>
	/// <remarks>
	/// Implementations of this method should not flush the writer.
	/// </remarks>
	public abstract void WriteObject(ref MessagePackWriter writer, object? value, SerializationContext context);

	/// <summary>
	/// Deserializes an instance of an object.
	/// </summary>
	/// <param name="reader">The reader to use.</param>
	/// <param name="context">Context for the deserialization.</param>
	/// <returns>The deserialized value.</returns>
	public abstract object? ReadObject(ref MessagePackReader reader, SerializationContext context);

	/// <inheritdoc cref="WriteObject"/>
	/// <returns>A task that tracks the asynchronous operation.</returns>
	public abstract ValueTask WriteObjectAsync(MessagePackAsyncWriter writer, object? value, SerializationContext context);

	/// <inheritdoc cref="ReadObject"/>
	public abstract ValueTask<object?> ReadObjectAsync(MessagePackAsyncReader reader, SerializationContext context);

	/// <inheritdoc cref="MessagePackConverter{T}.GetJsonSchema(JsonSchemaContext, ITypeShape)"/>
	public abstract JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape);

	/// <summary>
	/// Skips ahead in the msgpack data to the point where the value of the specified property can be read.
	/// </summary>
	/// <param name="reader">The reader.</param>
	/// <param name="propertyShape">The shape of the property whose value is to be skipped to.</param>
	/// <param name="context">The serialization context.</param>
	/// <returns><see langword="true" /> if the specified property was found in the data and the value is ready to be read; <see langword="false" /> otherwise.</returns>
	/// <remarks><inheritdoc cref="SkipToIndexValueAsync(MessagePackAsyncReader, object?, SerializationContext)" path="/remarks"/></remarks>
	public abstract ValueTask<bool> SkipToPropertyValueAsync(MessagePackAsyncReader reader, IPropertyShape propertyShape, SerializationContext context);

	/// <summary>
	/// Skips ahead in the msgpack data to the point where the value of the specified property can be read.
	/// </summary>
	/// <param name="reader">The reader.</param>
	/// <param name="propertyShape">The shape of the property whose value is to be skipped to.</param>
	/// <param name="context">The serialization context.</param>
	/// <returns><see langword="true" /> if the specified property was found in the data and the value is ready to be read; <see langword="false" /> otherwise.</returns>
	/// <remarks><inheritdoc cref="SkipToIndexValue(ref MessagePackReader, object?, SerializationContext)" path="/remarks"/></remarks>
	public abstract bool SkipToPropertyValue(ref MessagePackReader reader, IPropertyShape propertyShape, SerializationContext context);

	/// <summary>
	/// Skips ahead in the msgpack data to the point where the value at the specified index can be read.
	/// </summary>
	/// <param name="reader">The reader.</param>
	/// <param name="index">The key or index of the value to be retrieved.</param>
	/// <param name="context">The serialization context.</param>
	/// <returns><see langword="true" /> if the specified index was found in the data and the value is ready to be read; <see langword="false" /> otherwise.</returns>
	/// <remarks>
	/// This method is used by <see cref="MessagePackSerializer.DeserializeEnumerableAsync{T, TElement}(System.IO.Pipelines.PipeReader, ITypeShape{T}, MessagePackSerializer.StreamingEnumerationOptions{T, TElement}, CancellationToken)"/>
	/// to skip to the starting position of a sequence that should be asynchronously enumerated.
	/// </remarks>
	public abstract ValueTask<bool> SkipToIndexValueAsync(MessagePackAsyncReader reader, object? index, SerializationContext context);

	/// <summary>
	/// Skips ahead in the msgpack data to the point where the value at the specified index can be read.
	/// </summary>
	/// <param name="reader">The reader.</param>
	/// <param name="index">The key or index of the value to be retrieved.</param>
	/// <param name="context">The serialization context.</param>
	/// <returns><see langword="true" /> if the specified index was found in the data and the value is ready to be read; <see langword="false" /> otherwise.</returns>
	/// <remarks>
	/// This method may be used by <see cref="MessagePackSerializer.DeserializePath{T, TElement}(ref MessagePackReader, ITypeShapeProvider, in MessagePackSerializer.DeserializePathOptions{T, TElement}, CancellationToken)"/>
	/// to skip to the starting position of the particular object to be deserialized.
	/// </remarks>
	public abstract bool SkipToIndexValue(ref MessagePackReader reader, object? index, SerializationContext context);

	/// <summary>
	/// Determines if a thrown exception should be wrapped with contextual information.
	/// </summary>
	/// <param name="ex">The thrown exception.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns><see langword="true" /> if the exception should be wrapped; <see langword="false" /> otherwise.</returns>
	/// <remarks>
	/// We wrap all exceptions <em>except</em> <see cref="OperationCanceledException"/> if the cancellation token is cancelled.
	/// In other words, the only time we allow any exception to escape is when the operation was cancelled, because
	/// that is the intended behavior of cancellation tokens.
	/// </remarks>
	internal static bool ShouldWrapSerializationException(Exception ex, CancellationToken cancellationToken)
		=> ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested;

	/// <summary>
	/// Creates a type-specific error message for deserialization failures.
	/// </summary>
	/// <param name="objectType">The type of object being deserialized.</param>
	/// <param name="index">The index within an array of the element being processed.</param>
	/// <returns>A formatted error message.</returns>
	internal static string CreateFailReadingValueAtIndex(Type objectType, int index)
		=> $"Failed to deserialize a '{objectType.FullName}' at index {index}.";

	/// <summary>
	/// Creates a type-specific error message for serialization failures.
	/// </summary>
	/// <param name="objectType">The type of object being serialized.</param>
	/// <param name="index">The index within an array of the element being processed.</param>
	/// <returns>A formatted error message.</returns>
	internal static string CreateFailWritingValueAtIndex(Type objectType, int index)
		=> $"Failed to serialize a '{objectType.FullName}' at index {index}.";

	/// <summary>
	/// Just insurance that no external assembly can derive a concrete type from this type, except through the generic <see cref="MessagePackConverter{T}"/>.
	/// </summary>
	internal abstract void DerivationGuard();
}
