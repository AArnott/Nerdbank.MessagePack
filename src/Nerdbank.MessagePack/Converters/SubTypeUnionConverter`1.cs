﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

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
	public override TBase? Read(ref MessagePackReader reader, SerializationContext context)
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
			return baseConverter.Read(ref reader, context);
		}

		int alias = reader.ReadInt32();
		if (!subTypes.Deserializers.TryGetValue(alias, out IMessagePackConverter? converter))
		{
			throw new MessagePackSerializationException($"Unknown alias {alias}.");
		}

		return (TBase?)converter.Read(ref reader, context);
	}

#pragma warning disable NBMsgPack031 // Exactly one structure -- it can't see internal IMessagePackConverter.Write calls
	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in TBase? value, SerializationContext context)
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
			baseConverter.Write(ref writer, value, context);
		}
		else if (subTypes.Serializers.TryGetValue(valueType, out (int Alias, IMessagePackConverter Converter, ITypeShape Shape) result))
		{
			writer.Write(result.Alias);
			result.Converter.Write(ref writer, value, context);
		}
		else
		{
			throw new MessagePackSerializationException($"value is of type {valueType.FullName} which is not one of those listed via {KnownSubTypeAttribute.TypeName} on the declared base type {typeof(TBase).FullName}.");
		}
	}
#pragma warning restore NBMsgPack031

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
	{
		JsonArray oneOfArray = [CreateOneOfElement(null, baseConverter.GetJsonSchema(context, typeShape) ?? CreateUndocumentedSchema(baseConverter.GetType()))];

		foreach ((int alias, _, ITypeShape shape) in subTypes.Serializers.Values)
		{
			oneOfArray.Add((JsonNode)CreateOneOfElement(alias, context.GetJsonSchema(shape)));
		}

		return new()
		{
			["oneOf"] = oneOfArray,
		};

		JsonObject CreateOneOfElement(int? alias, JsonObject schema)
		{
			JsonObject aliasSchema = new()
			{
				["type"] = alias is null ? "null" : "integer",
			};
			if (alias is not null)
			{
				aliasSchema["enum"] = new JsonArray(alias.Value);
			}

			return new()
			{
				["type"] = "array",
				["minItems"] = 2,
				["maxItems"] = 2,
				["items"] = new JsonArray(aliasSchema, schema),
			};
		}
	}
}
