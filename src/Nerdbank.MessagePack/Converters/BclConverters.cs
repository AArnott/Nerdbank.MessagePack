// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single type

using System.Collections.Specialized;
using System.Drawing;
using System.Globalization;
using System.Text;
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
		reader.ReadArrayHeader(2);
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

internal class SystemGlobalizationCultureInfoConverter : MessagePackConverter<CultureInfo>
{
	public override CultureInfo? Read(ref MessagePackReader reader, SerializationContext context)
		=> reader.ReadString() is string name ? CultureInfo.GetCultureInfo(name) : null;

	public override void Write(ref MessagePackWriter writer, in CultureInfo? value, SerializationContext context)
		=> writer.Write(value?.Name);

	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
		=> new()
		{
			["type"] = "string",
			["description"] = "A culture name.",
		};
}

internal class SystemTextEncodingConverter : MessagePackConverter<Encoding>
{
	public override Encoding? Read(ref MessagePackReader reader, SerializationContext context)
		=> reader.ReadString() is string name ? Encoding.GetEncoding(name) : null;

	public override void Write(ref MessagePackWriter writer, in Encoding? value, SerializationContext context)
		=> writer.Write(value?.WebName);

	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
		=> new()
		{
			["type"] = "string",
			["description"] = "An encoding name.",
		};
}

/// <summary>
/// A converter for <see cref="NameValueCollection"/>.
/// </summary>
[GenerateShapeFor<string>]
internal partial class NameValueCollectionConverter : MessagePackConverter<NameValueCollection>
{
	private readonly MessagePackConverter<string> stringConverter;

	internal NameValueCollectionConverter(ConverterContext context)
	{
		this.stringConverter = context.GetConverter<string>(GeneratedTypeShapeProvider);
	}

	public override NameValueCollection? Read(ref MessagePackReader reader, SerializationContext context)
	{
		if (reader.TryReadNil())
		{
			return null;
		}

		context.DepthStep();
		int count = reader.ReadMapHeader();
		NameValueCollection result = new(count);
		for (int i = 0; i < count; i++)
		{
			string? key = this.stringConverter.Read(ref reader, context);
			if (reader.TryReadNil())
			{
				result.Add(key, null);
				continue;
			}

			if (reader.NextMessagePackType != MessagePackType.Array)
			{
				result.Add(key, this.stringConverter.Read(ref reader, context));
				continue;
			}

			SerializationContext valuesContext = context;
			valuesContext.DepthStep();
			int valueCount = reader.ReadArrayHeader();
			for (int j = 0; j < valueCount; j++)
			{
				result.Add(key, this.stringConverter.Read(ref reader, valuesContext));
			}
		}

		return result;
	}

	public override void Write(ref MessagePackWriter writer, in NameValueCollection? value, SerializationContext context)
	{
		if (value is null)
		{
			writer.WriteNil();
			return;
		}

		context.DepthStep();
		writer.WriteMapHeader(value.Count);
		for (int i = 0; i < value.Count; i++)
		{
			this.stringConverter.Write(ref writer, value.GetKey(i), context);
			string?[]? values = value.GetValues(i);
			if (values is null or [null])
			{
				writer.WriteNil();
				continue;
			}

			if (values is [string singleValue])
			{
				this.stringConverter.Write(ref writer, singleValue, context);
				continue;
			}

			SerializationContext valuesContext = context;
			valuesContext.DepthStep();
			writer.WriteArrayHeader(values.Length);
			foreach (string? item in values)
			{
				this.stringConverter.Write(ref writer, item, valuesContext);
			}
		}
	}

	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
		=> new()
		{
			["type"] = "object",
			["additionalProperties"] = new JsonObject
			{
				["anyOf"] = new JsonArray(
					new JsonObject { ["type"] = "null" },
					new JsonObject { ["type"] = "string" },
					new JsonObject
					{
						["type"] = "array",
						["items"] = new JsonObject { ["type"] = "string" },
					}),
			},
			["description"] = "A name/value collection represented as a map of strings or string arrays.",
		};
}

/// <summary>
/// Creates <see cref="NameValueCollectionConverter"/> instances when explicitly enabled.
/// </summary>
internal sealed class NameValueCollectionConverterFactory : IMessagePackConverterFactory
{
	/// <inheritdoc/>
	public MessagePackConverter? CreateConverter(Type type, ITypeShape? shape, in ConverterContext context)
		=> type == typeof(NameValueCollection) ? new NameValueCollectionConverter(context) : null;
}
