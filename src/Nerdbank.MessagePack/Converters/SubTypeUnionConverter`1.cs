// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// A formatter for a type that may serve as an ancestor class for the actual runtime type of a value to be (de)serialized.
/// </summary>
/// <typeparam name="TBase">The type that serves as the runtime type or the ancestor type for any runtime value.</typeparam>
/// <param name="subTypes">Contains maps to assist with converting subtypes.</param>
/// <param name="baseConverter">The converter to use for values that are actual instances of the base type itself.</param>
internal class SubTypeUnionConverter<TBase>(SubTypes subTypes, MessagePackConverter<TBase> baseConverter) : MessagePackConverter<TBase>
{
	/// <inheritdoc/>
	public override TBase? Deserialize(ref MessagePackReader reader, SerializationContext context)
	{
		if (reader.TryReadNil())
		{
			return default;
		}

		int count = reader.ReadArrayHeader();
		if (count != 2)
		{
			throw new MessagePackSerializationException($"Expected an array of 2 elements, but found {count}.");
		}

		// The alias for the base type itself is simply nil.
		if (reader.TryReadNil())
		{
			return baseConverter.Deserialize(ref reader, context);
		}

		int alias = reader.ReadInt32();
		if (!subTypes.Deserializers.TryGetValue(alias, out IMessagePackConverter? converter))
		{
			throw new MessagePackSerializationException($"Unknown alias {alias}.");
		}

		return (TBase?)converter.Deserialize(ref reader, context);
	}

	/// <inheritdoc/>
	public override void Serialize(ref MessagePackWriter writer, in TBase? value, SerializationContext context)
	{
		if (value is null)
		{
			writer.WriteNil();
			return;
		}

		writer.WriteArrayHeader(2);

		Type valueType = value.GetType();
		if (valueType.IsEquivalentTo(typeof(TBase)))
		{
			// The runtime type of the value matches the base exactly. Use nil as the alias.
			writer.WriteNil();
			baseConverter.Serialize(ref writer, value, context);
		}
		else if (subTypes.Serializers.TryGetValue(valueType, out (int Alias, IMessagePackConverter Converter) result))
		{
			writer.Write(result.Alias);
			object? untypedValue = value;
			result.Converter.Serialize(ref writer, ref untypedValue, context);
		}
		else
		{
			throw new MessagePackSerializationException($"value is of type {valueType.FullName} which is not one of those listed via {nameof(KnownSubTypeAttribute)} on the declared base type {typeof(TBase).FullName}.");
		}
	}
}
