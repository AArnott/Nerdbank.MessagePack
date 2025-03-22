// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable NBMsgPack031 // Read exactly one value - analyzer is mis-firing.

using System.Text.Json.Nodes;

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// A converter for the <see cref="JsonNode"/> type.
/// </summary>
internal class JsonNodeConverter : MessagePackConverter<JsonNode>
{
	/// <inheritdoc/>
	public override JsonNode? Read(ref MessagePackReader reader, SerializationContext context)
	{
		return reader.NextMessagePackType switch
		{
			MessagePackType.Integer => MessagePackCode.IsSignedInteger(reader.NextCode) ? JsonValue.Create(reader.ReadInt64()) : JsonValue.Create(reader.ReadUInt64()),
			MessagePackType.Nil => JsonValue.Create((string?)null),
			MessagePackType.Boolean => JsonValue.Create(reader.ReadBoolean()),
			MessagePackType.Float => JsonValue.Create(reader.ReadDouble()),
			MessagePackType.String => JsonValue.Create(reader.ReadString()),
			MessagePackType.Binary => JsonValue.Create(Convert.ToBase64String(reader.ReadBytes()!.Value.ToArray())),
			MessagePackType.Array => ReadArray(ref reader, context),
			MessagePackType.Map => ReadMap(ref reader, context),
			MessagePackType.Extension => throw new NotSupportedException("msgpack extensions cannot be represented in JSON."),
			_ => throw new NotSupportedException("Unsupported msgpack token."),
		};

		JsonNode ReadArray(ref MessagePackReader reader, SerializationContext context)
		{
			context.DepthStep();
			JsonNode?[] array = new JsonNode[reader.ReadArrayHeader()];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = this.Read(ref reader, context);
			}

			return new JsonArray(array);
		}

		JsonNode ReadMap(ref MessagePackReader reader, SerializationContext context)
		{
			context.DepthStep();
			JsonObject obj = new();
			int count = reader.ReadMapHeader();
			for (int i = 0; i < count; i++)
			{
				string key = reader.ReadString() ?? throw new NotSupportedException("Map keys must be strings.");
				obj[key] = this.Read(ref reader, context);
			}

			return obj;
		}
	}

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in JsonNode? value, SerializationContext context)
	{
		if (value is null)
		{
			writer.WriteNil();
			return;
		}

		switch (value.GetValueKind())
		{
			case System.Text.Json.JsonValueKind.Object:
				context.DepthStep();
				JsonObject obj = value.AsObject();
				writer.WriteMapHeader(obj.Count);
				foreach (KeyValuePair<string, JsonNode?> item in obj)
				{
					writer.Write(item.Key);
					this.Write(ref writer, item.Value, context);
				}

				break;
			case System.Text.Json.JsonValueKind.Array:
				context.DepthStep();
				JsonArray arr = value.AsArray();
				writer.WriteArrayHeader(arr.Count);
				foreach (JsonNode? item in arr)
				{
					this.Write(ref writer, item, context);
				}

				break;
			case System.Text.Json.JsonValueKind.String:
				writer.Write(value.GetValue<string>());
				break;
			case System.Text.Json.JsonValueKind.Number:
				if (value.AsValue().TryGetValue<ulong>(out ulong unsigned))
				{
					writer.Write(unsigned);
				}
				else
				{
					writer.Write(value.GetValue<long>());
				}

				break;
			case System.Text.Json.JsonValueKind.True:
				writer.Write(true);
				break;
			case System.Text.Json.JsonValueKind.False:
				writer.Write(false);
				break;
			case System.Text.Json.JsonValueKind.Null:
				writer.WriteNil();
				break;
			default:
				throw new NotSupportedException($"Unrecognized JSON node kind {value.GetValueKind()}.");
		}
	}

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape) => null;
}
