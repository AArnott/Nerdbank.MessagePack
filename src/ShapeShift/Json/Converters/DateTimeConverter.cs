﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single class

using System.Buffers.Text;
using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft;

namespace ShapeShift.Json.Converters;

/// <summary>
/// Serializes a <see cref="DateTime"/> value as a string.
/// </summary>
internal class DateTimeConverter : Converter<DateTime>
{
	/// <summary>
	/// The standard format used to serialize <see cref="DateTime"/> and <see cref="DateTimeOffsetConverter"/> values.
	/// </summary>
	internal static readonly StandardFormat DateTimeStandardFormat = new StandardFormat('O');

	private const int MaximumFormatDateTimeOffsetLength = 33;  // StandardFormat 'O', e.g. 2017-06-12T05:30:45.7680000-07:00

	/// <inheritdoc/>
	public override DateTime Read(ref Reader reader, SerializationContext context)
	{
		// PERF: this could be highly tuned to avoid allocations.
		return DateTime.Parse(reader.ReadString() ?? throw new SerializationException("Unexpected null."));
	}

	/// <inheritdoc/>
	public override void Write(ref Writer writer, in DateTime value, SerializationContext context)
	{
		switch (writer.Formatter.Encoding)
		{
			case UTF8Encoding:
				Span<byte> span = stackalloc byte[MaximumFormatDateTimeOffsetLength];
				Assumes.True(Utf8Formatter.TryFormat(value, span, out int bytesWritten, DateTimeStandardFormat));

				// TODO: add trimming, like STJ has.
				writer.WriteEncodedString(span[..bytesWritten]);
				break;
			default:
				writer.Write(value.ToString("O", CultureInfo.InvariantCulture));
				break;
		}
	}

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
		=> new()
		{
			["type"] = "string",
			["format"] = "date-time",
		};
}
