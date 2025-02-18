// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable SA1649 // File name should match first type name

using System.Drawing;
using System.Text.Json.Nodes;

namespace ShapeShift.Converters;

internal class SystemDrawingColorConverter : Converter<Color>
{
	public override Color Read(ref Reader reader, SerializationContext context)
		=> Color.FromArgb(reader.ReadInt32());

	public override void Write(ref Writer writer, in Color value, SerializationContext context)
		=> writer.Write(value.ToArgb());

	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
		=> new()
		{
			["type"] = "integer",
			["format"] = "int32",
			["description"] = "An ARGB color value.",
		};
}
