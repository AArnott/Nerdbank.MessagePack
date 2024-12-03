// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Nerdbank.MessagePack;

/// <summary>
/// A non-generic, <see cref="object"/>-based interface for all message pack converters.
/// </summary>
internal interface IMessagePackConverter
{
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

	/// <summary>
	/// Wraps this converter with a reference preservation converter.
	/// </summary>
	/// <returns>A converter. Possibly <see langword="this"/> if this instance is already reference preserving.</returns>
	IMessagePackConverter WrapWithReferencePreservation();

	/// <summary>
	/// Removes the outer reference preserving converter, if present.
	/// </summary>
	/// <returns>The unwrapped converter.</returns>
	IMessagePackConverter UnwrapReferencePreservation();

	/// <inheritdoc cref="MessagePackConverter{T}.GetJsonSchema(JsonSchemaContext, ITypeShape)"/>
	JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape);
}
