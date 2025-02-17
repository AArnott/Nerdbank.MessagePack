// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Microsoft;

namespace Nerdbank.PolySerializer;

public class JsonExporter
{
	private readonly SerializerBase serializerBase;

	public JsonExporter(SerializerBase serializerBase)
	{
		this.serializerBase = serializerBase;
	}

	/// <inheritdoc cref="ConvertToJson(in ReadOnlySequence{byte}, JsonOptions?)"/>
	public string ConvertToJson(ReadOnlyMemory<byte> buffer, JsonOptions? options = null) => this.ConvertToJson(new ReadOnlySequence<byte>(buffer), options);

	/// <summary>
	/// Converts a formatted byte sequence into equivalent JSON.
	/// </summary>
	/// <param name="buffer">The formatted byte sequence.</param>
	/// <param name="options"><inheritdoc cref="ConvertToJson(ref Reader, TextWriter, JsonOptions?)" path="/param[@name='options']"/></param>
	/// <returns>The JSON.</returns>
	/// <remarks>
	/// <para>
	/// Not all valid formatted structures can be converted to JSON. For example, msgpack maps with non-string keys cannot be represented in JSON.
	/// As such, this method is intended for debugging purposes rather than for production use.
	/// </para>
	/// </remarks>
	public string ConvertToJson(in ReadOnlySequence<byte> buffer, JsonOptions? options = null)
	{
		StringWriter jsonWriter = new();
		Reader reader = new(buffer, this.serializerBase.Deformatter);
		while (!reader.End)
		{
			this.ConvertToJson(ref reader, jsonWriter, options);
		}

		return jsonWriter.ToString();
	}

	/// <summary>
	/// Converts one formatted structure to a JSON stream.
	/// </summary>
	/// <param name="reader">A reader of the raw data stream.</param>
	/// <param name="jsonWriter">The writer that will receive JSON text.</param>
	/// <param name="options">Options to customize how the JSON is written.</param>
	public void ConvertToJson(ref Reader reader, TextWriter jsonWriter, JsonOptions? options = null)
	{
		Requires.NotNull(jsonWriter);

		WriteOneElement(ref reader, jsonWriter, options ?? new(), 0);

		void WriteOneElement(ref Reader reader, TextWriter jsonWriter, JsonOptions options, int indentationLevel)
		{
			switch (reader.NextTypeCode)
			{
				case Converters.TypeCode.Nil:
					reader.ReadNull();
					jsonWriter.Write("null");
					break;
				case Converters.TypeCode.Integer:
					if (reader.Deformatter.PeekIsSignedInteger(reader))
					{
						jsonWriter.Write(reader.ReadInt64());
					}
					else
					{
						jsonWriter.Write(reader.ReadUInt64());
					}

					break;
				case Converters.TypeCode.Boolean:
					jsonWriter.Write(reader.ReadBoolean() ? "true" : "false");
					break;
				case Converters.TypeCode.Float:
					// Emit with only the precision inherent in the format.
					// Use "R" to preserve full precision in the string version so it isn't lossy.
					if (reader.Deformatter.PeekIsFloat32(reader))
					{
						jsonWriter.Write(reader.ReadSingle().ToString("R", CultureInfo.InvariantCulture));
					}
					else
					{
						jsonWriter.Write(reader.ReadDouble().ToString("R", CultureInfo.InvariantCulture));
					}

					break;
				case Converters.TypeCode.String:
					WriteJsonString(reader.ReadString()!, jsonWriter);
					break;
				case Converters.TypeCode.Vector:
					jsonWriter.Write('[');
					int count = reader.ReadArrayHeader();
					if (count > 0)
					{
						NewLine(jsonWriter, options, indentationLevel + 1);

						for (int i = 0; i < count; i++)
						{
							if (i > 0)
							{
								jsonWriter.Write(',');
								NewLine(jsonWriter, options, indentationLevel + 1);
							}

							WriteOneElement(ref reader, jsonWriter, options, indentationLevel + 1);
						}

						if (options.TrailingCommas && options.Indentation is not null && count > 0)
						{
							jsonWriter.Write(',');
						}

						NewLine(jsonWriter, options, indentationLevel);
					}

					jsonWriter.Write(']');
					break;
				case Converters.TypeCode.Map:
					jsonWriter.Write('{');
					count = reader.ReadMapHeader();
					if (count > 0)
					{
						NewLine(jsonWriter, options, indentationLevel + 1);
						for (int i = 0; i < count; i++)
						{
							if (i > 0)
							{
								jsonWriter.Write(',');
								NewLine(jsonWriter, options, indentationLevel + 1);
							}

							WriteOneElement(ref reader, jsonWriter, options, indentationLevel + 1);
							if (options.Indentation is null)
							{
								jsonWriter.Write(':');
							}
							else
							{
								jsonWriter.Write(": ");
							}

							WriteOneElement(ref reader, jsonWriter, options, indentationLevel + 1);
						}

						if (options.TrailingCommas && options.Indentation is not null && count > 0)
						{
							jsonWriter.Write(',');
						}

						NewLine(jsonWriter, options, indentationLevel);
					}

					jsonWriter.Write('}');
					break;
				case Converters.TypeCode.Binary:
					jsonWriter.Write("\"Binary as base64: ");
					jsonWriter.Write(Convert.ToBase64String(reader.ReadBytes()!.Value.ToArray()));
					jsonWriter.Write('\"');
					break;
				case Converters.TypeCode.Unknown:
					this.serializerBase.RenderAsJson(ref reader, jsonWriter);
					break;
			}

			void NewLine(TextWriter writer, JsonOptions options, int indentationLevel)
			{
				if (options.Indentation is not null)
				{
					writer.Write(options.NewLine);
					for (int i = 0; i < indentationLevel; i++)
					{
						writer.Write(options.Indentation);
					}
				}
			}
		}

		// escape string
		void WriteJsonString(string value, TextWriter builder)
		{
			builder.Write('\"');

			var len = value.Length;
			for (int i = 0; i < len; i++)
			{
				var c = value[i];
				switch (c)
				{
					case '"':
						builder.Write("\\\"");
						break;
					case '\\':
						builder.Write("\\\\");
						break;
					case '\b':
						builder.Write("\\b");
						break;
					case '\f':
						builder.Write("\\f");
						break;
					case '\n':
						builder.Write("\\n");
						break;
					case '\r':
						builder.Write("\\r");
						break;
					case '\t':
						builder.Write("\\t");
						break;
					default:
						builder.Write(c);
						break;
				}
			}

			builder.Write('\"');
		}
	}

	/// <summary>
	/// A description of how JSON should be formatted when calling one of the <see cref="ConvertToJson(ref Reader, TextWriter, JsonOptions?)"/> overloads.
	/// </summary>
	public record struct JsonOptions
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="JsonOptions"/> struct.
		/// </summary>
		public JsonOptions()
		{
		}

		/// <summary>
		/// Gets or sets the string used to indent the JSON (implies newlines are also used).
		/// </summary>
		/// <remarks>
		/// A <see langword="null" /> value indicates that no indentation should be used.
		/// </remarks>
		public string? Indentation { get; set; }

		/// <summary>
		/// Gets or sets the sequence of characters used to represent a newline.
		/// </summary>
		/// <value>The default is <see cref="Environment.NewLine"/>.</value>
		public string NewLine { get; set; } = Environment.NewLine;

		/// <summary>
		/// Gets or sets a value indicating whether the JSON may use trailing commas (e.g. after the last property or element in an array).
		/// </summary>
		/// <remarks>
		/// <para>
		/// Trailing commas are not allowed in JSON by default, but some parsers may accept them.
		/// JSON5 allows trailing commas.
		/// </para>
		/// <para>
		/// Trailing commas may only be emitted when <see cref="Indentation"/> is set to a non-<see langword="null" /> value.
		/// </para>
		/// </remarks>
		public bool TrailingCommas { get; set; }
	}
}
