// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
	void Write(ref MessagePackWriter writer, ref object? value, SerializationContext context);

	/// <summary>
	/// Deserializes an instance of an object.
	/// </summary>
	/// <param name="reader">The reader to use.</param>
	/// <param name="context">Context for the deserialization.</param>
	/// <returns>The deserialized value.</returns>
	object? Read(ref MessagePackReader reader, SerializationContext context);
}
