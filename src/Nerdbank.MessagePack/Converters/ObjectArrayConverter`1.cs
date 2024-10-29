// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// A <see cref="IMessagePackConverter{T}"/> that writes objects as arrays of property values.
/// Only data types with default constructors may be deserialized.
/// </summary>
/// <typeparam name="T">The type of objects that can be serialized or deserialized with this converter.</typeparam>
internal class ObjectArrayConverter<T>(PropertyAccessors<T>?[] properties, Func<T>? constructor) : IMessagePackConverter<T>
{
	/// <inheritdoc/>
	public virtual T? Deserialize(ref MessagePackReader reader, SerializationContext context)
	{
		if (reader.TryReadNil())
		{
			return default;
		}

		if (constructor is null)
		{
			throw new NotSupportedException($"The {typeof(T).Name} type cannot be deserialized.");
		}

		context.DepthStep();
		T value = constructor();
		int count = reader.ReadArrayHeader();
		for (int i = 0; i < count; i++)
		{
			if (properties.Length > i && properties[i]?.Deserialize is { } deserialize)
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

	/// <inheritdoc/>
	public void Serialize(ref MessagePackWriter writer, ref T? value, SerializationContext context)
	{
		if (value is null)
		{
			writer.WriteNil();
			return;
		}

		context.DepthStep();

		writer.WriteArrayHeader(properties.Length);
		for (int i = 0; i < properties.Length; i++)
		{
			if (properties[i]?.Serialize is { } serialize)
			{
				serialize(ref value, ref writer, context);
			}
			else
			{
				writer.WriteNil();
			}
		}
	}
}
