// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single type

using System.Drawing;
using System.Text.Json.Nodes;

namespace Nerdbank.MessagePack.Converters;

internal class SystemDrawingColorConverter : MessagePackConverter<Color>
{
	public override Color Read(ref MessagePackReader reader, SerializationContext context)
		=> Color.FromArgb(reader.ReadInt32());

	public override void Write(ref MessagePackWriter writer, in Color value, SerializationContext context)
		=> writer.Write(value.ToArgb());

	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
		=> new()
		{
			["type"] = "integer",
			["format"] = "int32",
			["description"] = "An ARGB color value.",
		};
}

internal class SystemDrawingPointConverter : MessagePackConverter<Point>
{
	public override Point Read(ref MessagePackReader reader, SerializationContext context)
	{
		int count = reader.ReadArrayHeader();
		if (count != 2)
		{
			throw new MessagePackSerializationException($"Expected an array of 2 integers, but got {count}.");
		}

		return new(reader.ReadInt32(), reader.ReadInt32());
	}

	public override void Write(ref MessagePackWriter writer, in Point value, SerializationContext context)
	{
		writer.WriteArrayHeader(2);
		writer.Write(value.X);
		writer.Write(value.Y);
	}

	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
		=> new()
		{
			["type"] = "array",
			["minItems"] = 2,
			["maxItems"] = 2,
			["items"] = new JsonArray(
				new JsonObject { ["type"] = "integer", ["format"] = "int32" },
				new JsonObject { ["type"] = "integer", ["format"] = "int32" }),
			["description"] = "A point represented by two integers.",
		};
}
