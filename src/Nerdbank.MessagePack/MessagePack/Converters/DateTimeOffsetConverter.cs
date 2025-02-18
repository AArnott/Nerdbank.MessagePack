// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single class

using System.Text.Json.Nodes;

namespace ShapeShift.MessagePack.Converters;

/// <summary>
/// Serializes <see cref="DateTimeOffset"/> values.
/// </summary>
internal class DateTimeOffsetConverter : MessagePackConverter<DateTimeOffset>
{
	/// <inheritdoc/>
	public override DateTimeOffset Read(ref Reader reader, SerializationContext context)
	{
		int count = reader.ReadStartVector();
		if (count != 2)
		{
			throw new SerializationException("Expected array of length 2.");
		}

		DateTime utcDateTime = ((MsgPackDeformatter)reader.Deformatter).ReadDateTime(ref reader);
		short offsetMinutes = reader.ReadInt16();
		return new DateTimeOffset(utcDateTime.Ticks, TimeSpan.FromMinutes(offsetMinutes));
	}

	/// <inheritdoc/>
	public override void Write(ref Writer writer, in DateTimeOffset value, SerializationContext context)
	{
		writer.WriteStartVector(2);
		((MsgPackFormatter)writer.Formatter).Write(ref writer, new DateTime(value.Ticks, DateTimeKind.Utc));
		writer.Write((short)value.Offset.TotalMinutes);
	}

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
		=> new()
		{
			["type"] = "array",
			["items"] = new JsonArray(
				CreateMsgPackExtensionSchema(ReservedMessagePackExtensionTypeCode.DateTime),
				new JsonObject { ["type"] = "integer" }),
		};
}
