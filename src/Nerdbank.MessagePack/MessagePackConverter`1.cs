// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack;

/// <summary>
/// An interface for all message pack converters.
/// </summary>
/// <typeparam name="T">The data type that can be converted by this object.</typeparam>
public abstract class MessagePackConverter<T> : IMessagePackConverter
{
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
}
