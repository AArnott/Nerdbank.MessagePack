// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// A converter for the <see cref="JsonElement"/> type.
/// </summary>
/// <remarks>
/// This converter only writes these objects. It throws <see cref="NotSupportedException"/> when reading them.
/// </remarks>
[GenerateShapeFor<JsonNode>]
internal partial class JsonElementConverter : MessagePackConverter<JsonElement>
{
	/// <inheritdoc/>
	public override JsonElement Read(ref MessagePackReader reader, SerializationContext context)
	{
		Sequence<byte> seq = new();
		Utf8JsonWriter writer = new(seq);
		JsonNode? node = context.GetConverter<JsonNode>(ShapeProvider).Read(ref reader, context);
		if (node is null)
		{
			throw new NotSupportedException("Null value cannot be made into a JsonElement.");
		}

		node.WriteTo(writer);
		writer.Flush();

		Utf8JsonReader utf8Reader = new(seq);
		return JsonElement.ParseValue(ref utf8Reader);
	}

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in JsonElement value, SerializationContext context)
	{
		switch (value.ValueKind)
		{
			case JsonValueKind.Object:
				context.DepthStep();
				int count = 0;
				foreach (JsonProperty property in value.EnumerateObject())
				{
					count++;
				}

				writer.WriteMapHeader(count);
				foreach (JsonProperty property in value.EnumerateObject())
				{
					writer.Write(property.Name);
					this.Write(ref writer, property.Value, context);
				}

				break;
			case JsonValueKind.Array:
				context.DepthStep();
				writer.WriteArrayHeader(value.GetArrayLength());
				foreach (JsonElement element in value.EnumerateArray())
				{
					this.Write(ref writer, element, context);
				}

				break;
			case JsonValueKind.String:
				writer.Write(value.GetString());
				break;
			case JsonValueKind.Number:
				if (value.TryGetUInt64(out ulong unsigned))
				{
					writer.Write(unsigned);
				}
				else if (value.TryGetInt64(out long signed))
				{
					writer.Write(signed);
				}
				else if (value.TryGetDouble(out double floatingPoint))
				{
					writer.Write(floatingPoint);
				}
				else
				{
					throw new NotSupportedException("Unsupported number.");
				}

				break;
			case JsonValueKind.True:
				writer.Write(true);
				break;
			case JsonValueKind.False:
				writer.Write(false);
				break;
			case JsonValueKind.Null:
				writer.WriteNil();
				break;
			default:
				throw new NotSupportedException($"Unsupported JSON value kind: {value.ValueKind}");
		}
	}

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape) => null;
}
