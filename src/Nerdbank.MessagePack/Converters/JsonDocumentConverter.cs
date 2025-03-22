// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// A converter for the <see cref="JsonDocument"/> type.
/// </summary>
/// <remarks>
/// This converter only writes these objects. It throws <see cref="NotSupportedException"/> when reading them.
/// </remarks>
[GenerateShape<JsonElement>]
internal partial class JsonDocumentConverter : MessagePackConverter<JsonDocument>
{
	/// <inheritdoc/>
	public override JsonDocument? Read(ref MessagePackReader reader, SerializationContext context)
	{
		if (reader.TryReadNil())
		{
			return null;
		}

		Sequence<byte> seq = new();
		Utf8JsonWriter writer = new(seq);
		JsonNode? node = context.GetConverter<JsonNode>(ShapeProvider).Read(ref reader, context);
		if (node is null)
		{
			throw new NotSupportedException("Null value cannot be made into a JsonElement.");
		}

		node.WriteTo(writer);
		writer.Flush();

		return JsonDocument.Parse(seq);
	}

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in JsonDocument? value, SerializationContext context)
	{
		if (value is null)
		{
			writer.WriteNil();
			return;
		}

		context.GetConverter<JsonElement>(ShapeProvider).Write(ref writer, value.RootElement, context);
	}

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape) => null;
}
