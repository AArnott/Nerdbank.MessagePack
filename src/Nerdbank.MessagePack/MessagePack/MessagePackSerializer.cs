// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable RS0026 // optional parameter on a method with overloads

using System.Globalization;
using Microsoft;

namespace Nerdbank.PolySerializer.MessagePack;

/// <summary>
/// Serializes .NET objects using the MessagePack format.
/// </summary>
/// <devremarks>
/// <para>
/// This class may declare properties that customize how msgpack serialization is performed.
/// These properties must use <see langword="init"/> accessors to prevent modification after construction,
/// since there is no means to replace converters once they are created.
/// </para>
/// <para>
/// If the ability to add custom converters is exposed publicly, such a method should throw once generated converters have started being generated
/// because generated ones have already locked-in their dependencies.
/// </para>
/// </devremarks>
public partial record MessagePackSerializer : SerializerBase
{
	internal override ReusableObjectPool<IReferenceEqualityTracker>? ReferenceTrackingPool { get; } = new(() => new ReferenceEqualityTracker());

	public MessagePackSerializer()
		: base(new MsgPackConverterCache(MsgPackFormatter.Default, MsgPackDeformatter.Default))
	{
	}

	internal static MsgPackDeformatter MyDeformatter => MsgPackDeformatter.Default;

	internal new MsgPackConverterCache ConverterCache
	{
		get => (MsgPackConverterCache)base.ConverterCache;
		init => base.ConverterCache = value;
	}

	protected override Formatter Formatter => this.ConverterCache.Formatter;

	protected override Deformatter Deformatter => this.ConverterCache.Deformatter;

	internal static MsgPackStreamingDeformatter StreamingDeformatter => MsgPackStreamingDeformatter.Default;

	/// <inheritdoc cref="ConverterCache.PreserveReferences"/>
	public bool PreserveReferences
	{
		get => this.ConverterCache.PreserveReferences;
		init => this.ConverterCache = this.ConverterCache with { PreserveReferences = value };
	}

	/// <summary>
	/// Gets the extension type codes to use for library-reserved extension types.
	/// </summary>
	/// <remarks>
	/// This property may be used to reassign the extension type codes for library-provided extension types
	/// in order to avoid conflicts with other libraries the application is using.
	/// </remarks>
	public LibraryReservedMessagePackExtensionTypeCode LibraryExtensionTypeCodes { get; init; } = LibraryReservedMessagePackExtensionTypeCode.Default;

	/// <summary>
	/// Gets a value indicating whether hardware accelerated converters should be avoided.
	/// </summary>
	internal bool DisableHardwareAcceleration
	{
		get => this.ConverterCache.DisableHardwareAcceleration;
		init => this.ConverterCache = this.ConverterCache with { DisableHardwareAcceleration = value };
	}

	/// <inheritdoc cref="ConvertToJson(in ReadOnlySequence{byte}, JsonOptions?)"/>
	public static string ConvertToJson(ReadOnlyMemory<byte> msgpack, JsonOptions? options = null) => ConvertToJson(new ReadOnlySequence<byte>(msgpack), options);

	/// <summary>
	/// Converts a msgpack sequence into equivalent JSON.
	/// </summary>
	/// <param name="msgpack">The msgpack sequence.</param>
	/// <param name="options"><inheritdoc cref="ConvertToJson(ref MessagePackReader, TextWriter, JsonOptions?)" path="/param[@name='options']"/></param>
	/// <returns>The JSON.</returns>
	/// <remarks>
	/// <para>
	/// Not all valid msgpack can be converted to JSON. For example, msgpack maps with non-string keys cannot be represented in JSON.
	/// As such, this method is intended for debugging purposes rather than for production use.
	/// </para>
	/// </remarks>
	public static string ConvertToJson(in ReadOnlySequence<byte> msgpack, JsonOptions? options = null)
	{
		StringWriter jsonWriter = new();
		Reader reader = new(msgpack, MyDeformatter);
		while (!reader.End)
		{
			ConvertToJson(ref reader, jsonWriter, options);
		}

		return jsonWriter.ToString();
	}

	/// <summary>
	/// Converts one MessagePack structure to a JSON stream.
	/// </summary>
	/// <param name="reader">A reader of the msgpack stream.</param>
	/// <param name="jsonWriter">The writer that will receive JSON text.</param>
	/// <param name="options">Options to customize how the JSON is written.</param>
	public static void ConvertToJson(ref Reader reader, TextWriter jsonWriter, JsonOptions? options = null)
	{
		Requires.NotNull(jsonWriter);

		WriteOneElement(ref reader, jsonWriter, options ?? new(), 0);

		static void WriteOneElement(ref Reader reader, TextWriter jsonWriter, JsonOptions options, int indentationLevel)
		{
			switch (MyDeformatter.PeekNextMessagePackType(reader))
			{
				case MessagePackType.Nil:
					reader.ReadNull();
					jsonWriter.Write("null");
					break;
				case MessagePackType.Integer:
					if (MessagePackCode.IsSignedInteger(reader.NextCode))
					{
						jsonWriter.Write(reader.ReadInt64());
					}
					else
					{
						jsonWriter.Write(reader.ReadUInt64());
					}

					break;
				case MessagePackType.Boolean:
					jsonWriter.Write(reader.ReadBoolean() ? "true" : "false");
					break;
				case MessagePackType.Float:
					// Emit with only the precision inherent in the msgpack format.
					// Use "R" to preserve full precision in the string version so it isn't lossy.
					if (reader.NextCode == MessagePackCode.Float32)
					{
						jsonWriter.Write(reader.ReadSingle().ToString("R", CultureInfo.InvariantCulture));
					}
					else
					{
						jsonWriter.Write(reader.ReadDouble().ToString("R", CultureInfo.InvariantCulture));
					}

					break;
				case MessagePackType.String:
					WriteJsonString(reader.ReadString()!, jsonWriter);
					break;
				case MessagePackType.Array:
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
				case MessagePackType.Map:
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
				case MessagePackType.Binary:
					jsonWriter.Write("\"Binary as base64: ");
					jsonWriter.Write(Convert.ToBase64String(reader.ReadBytes()!.Value.ToArray()));
					jsonWriter.Write('\"');
					break;
				case MessagePackType.Extension:
					Extension extension = MyDeformatter.ReadExtension(ref reader);
					jsonWriter.Write($"\"msgpack extension {extension.Header.TypeCode} as base64: ");
					jsonWriter.Write(Convert.ToBase64String(extension.Data.ToArray()));
					jsonWriter.Write('\"');
					break;
				case MessagePackType.Unknown:
					throw new NotImplementedException($"{MyDeformatter.PeekNextMessagePackType(reader)} not yet implemented.");
			}

			static void NewLine(TextWriter writer, JsonOptions options, int indentationLevel)
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
		static void WriteJsonString(string value, TextWriter builder)
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
	/// A description of how JSON should be formatted when calling one of the <see cref="ConvertToJson(ref MessagePackReader, TextWriter, JsonOptions?)"/> overloads.
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
