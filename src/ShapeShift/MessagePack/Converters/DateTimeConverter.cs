// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single class

using System.Text.Json.Nodes;

namespace ShapeShift.MessagePack.Converters;

/// <summary>
/// Serializes <see cref="DateTime"/> values using the message code <see cref="ReservedMessagePackExtensionTypeCode.DateTime"/>.
/// </summary>
internal class DateTimeConverter : MessagePackConverter<DateTime>
{
	/// <inheritdoc/>
	public override DateTime Read(ref Reader reader, SerializationContext context) => ((MessagePackDeformatter)reader.Deformatter).ReadDateTime(ref reader);

	/// <inheritdoc/>
	public override void Write(ref Writer writer, in DateTime value, SerializationContext context) => ((MessagePackFormatter)writer.Formatter).Write(ref writer.Buffer, value);

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape) => CreateMsgPackExtensionSchema(ReservedMessagePackExtensionTypeCode.DateTime);
}
