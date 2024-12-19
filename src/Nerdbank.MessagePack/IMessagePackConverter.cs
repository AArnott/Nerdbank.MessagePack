// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace Nerdbank.MessagePack;

/// <summary>
/// A non-generic, <see cref="object"/>-based interface for all message pack converters.
/// </summary>
public interface IMessagePackConverter
{
	/// <summary>
	/// Gets a value indicating whether callers should prefer the async methods on this object.
	/// </summary>
	/// <value>Unless overridden in a derived converter, this value is always <see langword="false"/>.</value>
	/// <remarks>
	/// Derived types that override the <see cref="WriteAsync"/> and/or <see cref="ReadAsync"/> methods
	/// should also override this property and have it return <see langword="true" />.
	/// </remarks>
	bool PreferAsyncSerialization { get; }

	/// <summary>
	/// Serializes an instance of an object.
	/// </summary>
	/// <param name="writer">The writer to use.</param>
	/// <param name="value">The value to serialize.</param>
	/// <param name="context">Context for the serialization.</param>
	void Write(ref MessagePackWriter writer, object? value, SerializationContext context);

	/// <summary>
	/// Deserializes an instance of an object.
	/// </summary>
	/// <param name="reader">The reader to use.</param>
	/// <param name="context">Context for the deserialization.</param>
	/// <returns>The deserialized value.</returns>
	object? Read(ref MessagePackReader reader, SerializationContext context);

	/// <inheritdoc cref="Write"/>
	/// <returns>A task that tracks the asynchronous operation.</returns>
	[Experimental("NBMsgPackAsync")]
	ValueTask WriteAsync(MessagePackAsyncWriter writer, object? value, SerializationContext context);

	/// <inheritdoc cref="Read"/>
	[Experimental("NBMsgPackAsync")]
	ValueTask<object?> ReadAsync(MessagePackAsyncReader reader, SerializationContext context);

	/// <inheritdoc cref="MessagePackConverter{T}.GetJsonSchema(JsonSchemaContext, ITypeShape)"/>
	JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape);
}
