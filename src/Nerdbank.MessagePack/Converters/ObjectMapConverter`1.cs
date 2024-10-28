// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// A <see cref="IMessagePackConverter{T}"/> that writes objects as maps of property names to values.
/// Only data types with default constructors may be deserialized.
/// </summary>
/// <typeparam name="T">The type of objects that can be serialized or deserialized with this converter.</typeparam>
/// <param name="serializable">Tools for serializing individual property values.</param>
/// <param name="deserializable">Tools for deserializing individual property values. May be omitted if the type will never be deserialized (i.e. there is no deserializing constructor).</param>
/// <param name="constructor">The default constructor, if present.</param>
internal class ObjectMapConverter<T>(MapSerializableProperties<T> serializable, MapDeserializableProperties<T>? deserializable, Func<T>? constructor) : IMessagePackConverter<T>
{
	/// <inheritdoc/>
	public override void Serialize(ref MessagePackWriter writer, ref T? value, SerializationContext context)
	{
		if (value is null)
		{
			writer.WriteNil();
			return;
		}

		context.DepthStep();
		writer.WriteMapHeader(serializable.Properties.Count);
		foreach ((ReadOnlyMemory<byte> RawPropertyNameString, SerializeProperty<T> Write) property in serializable.Properties)
		{
			writer.WriteRaw(property.RawPropertyNameString.Span);
			property.Write(ref value, ref writer, context);
		}
	}

	/// <inheritdoc/>
	public override T? Deserialize(ref MessagePackReader reader, SerializationContext context)
	{
		if (reader.TryReadNil())
		{
			return default;
		}

		if (constructor is null || deserializable is null)
		{
			throw new NotSupportedException($"The {typeof(T).Name} type cannot be deserialized.");
		}

		context.DepthStep();
		T value = constructor();
		int count = reader.ReadMapHeader();
		for (int i = 0; i < count; i++)
		{
			ReadOnlySpan<byte> propertyName = CodeGenHelpers.ReadStringSpan(ref reader);
			if (deserializable.Value.Readers.TryGetValue(propertyName, out DeserializeProperty<T>? deserialize))
			{
				deserialize(ref value, ref reader, context);
			}
			else
			{
				reader.Skip();
			}
		}

		return value;
	}
}
