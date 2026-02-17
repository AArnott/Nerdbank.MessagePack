// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using Microsoft;

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// A formatter for a type that may serve as an ancestor class for the actual runtime type of a value to be (de)serialized.
/// </summary>
/// <typeparam name="TUnion">The type that serves as the declared type or the ancestor type for any runtime value.</typeparam>
internal class UnionConverter<TUnion> : MessagePackConverter<TUnion>
{
	private readonly MessagePackConverter<TUnion> baseConverter;
	private readonly SubTypes<TUnion> subTypes;
	private readonly bool useDiscriminatorObjects;

	/// <summary>
	/// Initializes a new instance of the <see cref="UnionConverter{TUnion}"/> class.
	/// </summary>
	/// <param name="baseConverter">The converter to use for the base type.</param>
	/// <param name="subTypes">The map of subtypes and their converters.</param>
	/// <param name="useDiscriminatorObjects">Indicates whether to serialize as objects instead of arrays.</param>
	public UnionConverter(MessagePackConverter<TUnion> baseConverter, SubTypes<TUnion> subTypes, bool useDiscriminatorObjects)
	{
		Requires.Argument(!subTypes.Disabled, nameof(subTypes), "This union is disabled.");

		this.baseConverter = baseConverter;
		this.subTypes = subTypes;
		this.useDiscriminatorObjects = useDiscriminatorObjects;
		this.PreferAsyncSerialization = baseConverter.PreferAsyncSerialization || subTypes.Serializers.Any(t => t.Converter.PreferAsyncSerialization);
	}

	/// <inheritdoc/>
	public override bool PreferAsyncSerialization { get; }

	/// <inheritdoc/>
	public override TUnion? Read(ref MessagePackReader reader, SerializationContext context)
	{
		if (reader.TryReadNil())
		{
			return default;
		}

		// Read header based on format (object vs array)
		if (this.useDiscriminatorObjects)
		{
			// Object format: {"TypeName": {...}}
			int count = reader.ReadMapHeader();
			if (count != 1)
			{
				throw new MessagePackSerializationException($"Expected a map with 1 property, but found {count}.");
			}
		}
		else
		{
			// Array format: ["TypeName", {...}]
			int count = reader.ReadArrayHeader();
			if (count != 2)
			{
				throw new MessagePackSerializationException($"Expected an array of 2 elements, but found {count}.");
			}
		}

		// The alias for the base type itself is simply nil.
		if (reader.TryReadNil())
		{
			return this.baseConverter.Read(ref reader, context);
		}

		// Read the discriminator and find the converter (same for both formats after header)
		MessagePackConverter? converter;
		if (reader.NextMessagePackType == MessagePackType.Integer)
		{
			int alias = reader.ReadInt32();
			if (!this.subTypes.DeserializersByIntAlias.TryGetValue(alias, out converter))
			{
				throw new MessagePackSerializationException($"Unspecified alias {alias}.");
			}
		}
		else
		{
			ReadOnlySpan<byte> alias = StringEncoding.ReadStringSpan(ref reader);
			if (!this.subTypes.DeserializersByStringAlias.TryGetValue(alias, out converter))
			{
				throw new MessagePackSerializationException($"Unspecified alias \"{StringEncoding.UTF8.GetString(alias)}\".");
			}
		}

		return (TUnion?)converter.ReadObject(ref reader, context);
	}

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in TUnion? value, SerializationContext context)
	{
		if (value is null)
		{
			writer.WriteNil();
			return;
		}

		// Write header based on format (object vs array)
		if (this.useDiscriminatorObjects)
		{
			// Object format: {"TypeName": {...}}
			writer.WriteMapHeader(1);
		}
		else
		{
			// Array format: ["TypeName", {...}]
			writer.WriteArrayHeader(2);
		}

		// Write discriminator and value (same for both formats after header)
		MessagePackConverter converter;
		if (this.subTypes.TryGetSerializer(ref Unsafe.AsRef(in value)) is { } subtype)
		{
			writer.WriteRaw(subtype.Alias.MsgPackAlias.Span);
			converter = subtype.Converter;
		}
		else
		{
			writer.WriteNil();
			converter = this.baseConverter;
		}

		converter.WriteObject(ref writer, value, context);
	}

	/// <inheritdoc/>
	public override async ValueTask<TUnion?> ReadAsync(MessagePackAsyncReader reader, SerializationContext context)
	{
		MessagePackStreamingReader streamingReader = reader.CreateStreamingReader();
		bool success;
		while (streamingReader.TryReadNil(out success).NeedsMoreBytes())
		{
			streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
		}

		if (success)
		{
			reader.ReturnReader(ref streamingReader);
			return default;
		}

		// Read header based on format (object vs array)
		int count;
		if (this.useDiscriminatorObjects)
		{
			// Object format: {"TypeName": {...}}
			while (streamingReader.TryReadMapHeader(out count).NeedsMoreBytes())
			{
				streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
			}

			if (count != 1)
			{
				throw new MessagePackSerializationException($"Expected a map with 1 property, but found {count}.");
			}
		}
		else
		{
			// Array format: ["TypeName", {...}]
			while (streamingReader.TryReadArrayHeader(out count).NeedsMoreBytes())
			{
				streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
			}

			if (count != 2)
			{
				throw new MessagePackSerializationException($"Expected an array of 2 elements, but found {count}.");
			}
		}

		// Read discriminator and find converter (same for both formats after header)
		// The alias for the base type itself is simply nil.
		bool isNil;
		while (streamingReader.TryReadNil(out isNil).NeedsMoreBytes())
		{
			streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
		}

		if (isNil)
		{
			reader.ReturnReader(ref streamingReader);
			TUnion? result = await this.baseConverter.ReadAsync(reader, context).ConfigureAwait(false);
			return result;
		}

		MessagePackType nextMessagePackType;
		if (streamingReader.TryPeekNextMessagePackType(out nextMessagePackType).NeedsMoreBytes())
		{
			streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
		}

		MessagePackConverter? converter;
		if (nextMessagePackType == MessagePackType.Integer)
		{
			int alias;
			while (streamingReader.TryRead(out alias).NeedsMoreBytes())
			{
				streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
			}

			if (!this.subTypes.DeserializersByIntAlias.TryGetValue(alias, out converter))
			{
				throw new MessagePackSerializationException($"Unspecified alias {alias}.");
			}
		}
		else
		{
			ReadOnlySpan<byte> alias;
			bool contiguous;
			while (streamingReader.TryReadStringSpan(out contiguous, out alias).NeedsMoreBytes())
			{
				streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
			}

			if (!contiguous)
			{
				Assumes.True(streamingReader.TryReadStringSequence(out ReadOnlySequence<byte> utf8Sequence) == MessagePackPrimitives.DecodeResult.Success);
				alias = utf8Sequence.ToArray();
			}

			if (!this.subTypes.DeserializersByStringAlias.TryGetValue(alias, out converter))
			{
				throw new MessagePackSerializationException($"Unspecified alias \"{StringEncoding.UTF8.GetString(alias)}\".");
			}
		}

		reader.ReturnReader(ref streamingReader);
		return (TUnion?)await converter.ReadObjectAsync(reader, context).ConfigureAwait(false);
	}

	/// <inheritdoc/>
	public override async ValueTask WriteAsync(MessagePackAsyncWriter writer, TUnion? value, SerializationContext context)
	{
		if (value is null)
		{
			writer.WriteNil();
			return;
		}

		MessagePackWriter syncWriter = writer.CreateWriter();

		if (this.useDiscriminatorObjects)
		{
			// Object format: {"TypeName": {...}}
			syncWriter.WriteMapHeader(1);
		}
		else
		{
			// Array format: ["TypeName", {...}]
			syncWriter.WriteArrayHeader(2);
		}

		MessagePackConverter converter;
		if (this.subTypes.TryGetSerializer(ref Unsafe.AsRef(in value)) is { } subtype)
		{
			syncWriter.WriteRaw(subtype.Alias.MsgPackAlias.Span);
			converter = subtype.Converter;
		}
		else
		{
			syncWriter.WriteNil();
			converter = this.baseConverter;
		}

		if (converter.PreferAsyncSerialization)
		{
			writer.ReturnWriter(ref syncWriter);
			await converter.WriteObjectAsync(writer, value, context).ConfigureAwait(false);
		}
		else
		{
			converter.WriteObject(ref syncWriter, value, context);
			writer.ReturnWriter(ref syncWriter);
		}

		await writer.FlushIfAppropriateAsync(context).ConfigureAwait(false);
	}

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
	{
		var unionTypeShape = (IUnionTypeShape)typeShape;
		JsonArray oneOfArray = new(CreateOneOfElement(null, this.baseConverter.GetJsonSchema(context, unionTypeShape.BaseType) ?? CreateUndocumentedSchema(this.baseConverter.GetType())));

		foreach ((DerivedTypeIdentifier alias, _, ITypeShape shape) in this.subTypes.Serializers)
		{
			oneOfArray.Add((JsonNode)CreateOneOfElement(alias, context.GetJsonSchema(shape)));
		}

		return new()
		{
			["oneOf"] = oneOfArray,
		};

		JsonObject CreateOneOfElement(DerivedTypeIdentifier? alias, JsonObject schema)
		{
			if (this.useDiscriminatorObjects)
			{
				// Object format schema: {"TypeName": {...}}
				string propertyName;
				JsonObject schemaObject = new()
				{
					["type"] = "object",
				};

				if (alias is null)
				{
					propertyName = "null";

					// The actual MessagePack representation uses a nil value as the map key, not the string "null".
					// JSON Schema does not support nil keys, so we use the string "null" as a placeholder.
					schemaObject["description"] = "The discriminator key is a MessagePack nil value, represented here as the string 'null' for JSON Schema compatibility.";
				}
				else
				{
					propertyName = alias.Value.Type switch
					{
						DerivedTypeIdentifier.AliasType.String => alias.Value.StringAlias,
						DerivedTypeIdentifier.AliasType.Integer => alias.Value.IntAlias.ToString(System.Globalization.CultureInfo.InvariantCulture),
						_ => throw new NotImplementedException(),
					};
				}

				schemaObject["properties"] = new JsonObject
				{
					[propertyName] = schema,
				};
				schemaObject["required"] = new JsonArray((JsonNode)propertyName);
				schemaObject["additionalProperties"] = false;

				return schemaObject;
			}
			else
			{
				// Array format schema: ["TypeName", {...}]
				JsonObject aliasSchema = new()
				{
					["type"] = alias switch
					{
						null => "null",
						{ Type: DerivedTypeIdentifier.AliasType.Integer } => "integer",
						{ Type: DerivedTypeIdentifier.AliasType.String } => "string",
						_ => throw new NotImplementedException(),
					},
				};
				if (alias is not null)
				{
					JsonNode enumValue = alias.Value.Type switch
					{
						DerivedTypeIdentifier.AliasType.String => (JsonNode)alias.Value.StringAlias,
						DerivedTypeIdentifier.AliasType.Integer => (JsonNode)alias.Value.IntAlias,
						_ => throw new NotImplementedException(),
					};
					aliasSchema["enum"] = new JsonArray(enumValue);
				}

				return new()
				{
					["type"] = "array",
					["minItems"] = 2,
					["maxItems"] = 2,
					["items"] = new JsonArray(aliasSchema, schema),
				};
			}
		}
	}
}
