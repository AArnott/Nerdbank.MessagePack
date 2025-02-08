// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;
using Nerdbank.PolySerializer.MessagePack;

namespace Nerdbank.PolySerializer.MessagePack.Converters;

/// <summary>
/// Serializes <see langword="enum" /> types as their underlying integral type.
/// </summary>
/// <typeparam name="TEnum">The enum type.</typeparam>
/// <typeparam name="TUnderlyingType">The underlying integer type.</typeparam>
internal class EnumAsOrdinalConverter<TEnum, TUnderlyingType>(MessagePackConverter<TUnderlyingType> primitiveConverter) : MessagePackConverter<TEnum>
	where TEnum : struct, Enum
{
	/// <inheritdoc/>
	public override TEnum Read(ref MessagePackReader reader, SerializationContext context) => (TEnum)(object)primitiveConverter.Read(ref reader, context)!;

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in TEnum value, SerializationContext context)
	{
		primitiveConverter.Write(ref writer, (TUnderlyingType)(object)value, context);
	}

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
	{
		var schema = new JsonObject { ["type"] = "integer" };

		StringBuilder description = new();
#if NET
		Array enumValuesUntyped = typeof(TEnum).GetEnumValuesAsUnderlyingType();
#else
		Array enumValuesUntyped = typeof(TEnum).GetEnumValues();
#endif
		var enumValueNodes = new JsonNode[enumValuesUntyped.Length];
		for (int i = 0; i < enumValueNodes.Length; i++)
		{
			var ordinalValue = (TUnderlyingType)enumValuesUntyped.GetValue(i)!;
			if (description.Length > 0)
			{
				description.Append(", ");
			}

			description.Append($"{ordinalValue} = {Enum.GetName(typeof(TEnum), ordinalValue)}");
			enumValueNodes[i] = CreateJsonValue(ordinalValue) ?? throw new NotSupportedException("Unrecognized ordinal value type.");
		}

		schema["enum"] = new JsonArray(enumValueNodes);
		schema["description"] = description.ToString();

		return schema;
	}
}
