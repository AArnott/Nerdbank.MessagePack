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
	private readonly bool canPreserveUnknownUnionCase;
	private readonly UntaggedUnionCases? untaggedCases;

	/// <summary>
	/// Initializes a new instance of the <see cref="UnionConverter{TUnion}"/> class.
	/// </summary>
	/// <param name="baseConverter">The converter to use for the base type.</param>
	/// <param name="subTypes">The map of subtypes and their converters.</param>
	/// <param name="useDiscriminatorObjects">Indicates whether to serialize as objects instead of arrays.</param>
	/// <param name="untaggedCases">Native encodings for C# union cases, if applicable.</param>
	public UnionConverter(MessagePackConverter<TUnion> baseConverter, SubTypes<TUnion> subTypes, bool useDiscriminatorObjects, UntaggedUnionCases? untaggedCases = null)
	{
		Requires.Argument(!subTypes.Disabled, nameof(subTypes), "This union is disabled.");

		this.baseConverter = baseConverter;
		this.subTypes = subTypes;
		this.useDiscriminatorObjects = useDiscriminatorObjects;
		this.untaggedCases = untaggedCases;
		this.PreferAsyncSerialization = baseConverter.PreferAsyncSerialization || subTypes.Serializers.Any(t => t.Converter.PreferAsyncSerialization);
		this.canPreserveUnknownUnionCase = this.baseConverter is IUnknownUnionCaseFallback<TUnion> { CanPreserveUnknownUnionCase: true };
	}

	/// <inheritdoc/>
	public override bool PreferAsyncSerialization { get; }

	/// <inheritdoc/>
	public override TUnion? Read(ref MessagePackReader reader, SerializationContext context)
	{
		if (this.untaggedCases?.Select(reader, typeof(TUnion).IsValueType, context) is { } nativeConverter)
		{
			return (TUnion?)nativeConverter.ReadObject(ref reader, context);
		}

		if (reader.TryReadNil())
		{
			return default;
		}

		// Read header based on format (object vs array)
		if (this.useDiscriminatorObjects)
		{
			// Object format: {"TypeName": {...}}
			reader.ReadMapHeader(1);
		}
		else
		{
			// Array format: ["TypeName", {...}]
			reader.ReadArrayHeader(2);
		}

		RawMessagePack discriminator = reader.ReadRaw(context);
		MessagePackReader discriminatorReader = new(discriminator.MsgPack);

		// The alias for the base type itself is simply nil.
		if (discriminatorReader.TryReadNil())
		{
			return this.baseConverter.Read(ref reader, context);
		}

		// Read the discriminator and find the converter (same for both formats after header)
		MessagePackConverter? converter;
		if (discriminatorReader.NextMessagePackType == MessagePackType.Integer)
		{
			int alias = discriminatorReader.ReadInt32();
			if (!this.subTypes.DeserializersByIntAlias.TryGetValue(alias, out converter))
			{
				if (this.TryReadUnknownUnionCase(ref reader, discriminator, context, out TUnion? result))
				{
					return result;
				}

				throw new MessagePackSerializationException($"Unspecified alias {alias}.");
			}
		}
		else
		{
			ReadOnlySpan<byte> alias = StringEncoding.ReadStringSpan(ref discriminatorReader);
			if (!this.subTypes.DeserializersByStringAlias.TryGetValue(alias, out converter))
			{
				if (this.TryReadUnknownUnionCase(ref reader, discriminator, context, out TUnion? result))
				{
					return result;
				}

				throw new MessagePackSerializationException($"Unspecified alias \"{StringEncoding.UTF8.GetString(alias)}\".");
			}
		}

		return (TUnion?)converter.ReadObject(ref reader, context);
	}

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in TUnion? value, SerializationContext context)
	{
		bool tagNullPayload = context.TagCSharpUnionNullPayload;
		context.TagCSharpUnionNullPayload = false;
		if (!typeof(TUnion).IsValueType && value is null)
		{
			writer.WriteNil();
			return;
		}

		ref TUnion nonNullValue = ref Unsafe.AsRef(in value!);
		(DerivedTypeIdentifier Alias, MessagePackConverter Converter)? subtype = this.subTypes.TryGetSerializer(ref nonNullValue);
		if (subtype is { } nativeCase && this.untaggedCases?.Contains(nativeCase.Converter) == true)
		{
			bool nullPayload = this.untaggedCases.HasNullPayload(nativeCase.Converter, nonNullValue!);
			if (!nullPayload || (!tagNullPayload && typeof(TUnion).IsValueType))
			{
				nativeCase.Converter.WriteObject(ref writer, value, context);
				return;
			}
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
		if (subtype is { } taggedCase)
		{
			writer.WriteRaw(taggedCase.Alias.MsgPackAlias.Span);
			converter = taggedCase.Converter;
		}
		else if (this.baseConverter is IUnknownUnionCaseFallback<TUnion> { CanPreserveUnknownUnionCase: true } fallback && fallback.TryGetUnknownUnionDiscriminator(nonNullValue, out RawMessagePack discriminator))
		{
			writer.WriteRaw(discriminator);
			converter = this.baseConverter;
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
		if (this.untaggedCases is not null)
		{
			// Integer fallback and reference extension selection need more than the leading code.
			MessagePackStreamingReader peek = reader.CreateStreamingReader();
			byte code;
			while (peek.TryPeekNextCode(out code).NeedsMoreBytes())
			{
				peek = new(await peek.FetchMoreBytesAsync().ConfigureAwait(false));
			}

			reader.ReturnReader(ref peek);
			if (MessagePackCode.ToMessagePackType(code) is MessagePackType.Integer or MessagePackType.Extension)
			{
				await reader.BufferNextStructureAsync(context).ConfigureAwait(false);
			}

			MessagePackReader buffered = reader.CreateBufferedReader();
			MessagePackConverter? nativeConverter = this.untaggedCases.Select(buffered, typeof(TUnion).IsValueType, context);
			reader.ReturnReader(ref buffered);
			if (nativeConverter is not null)
			{
				return (TUnion?)await nativeConverter.ReadObjectAsync(reader, context).ConfigureAwait(false);
			}
		}

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

		// Read discriminator and find converter (same for both formats after header).
		RawMessagePack discriminator;
		while (streamingReader.TryReadRaw(ref context, out discriminator).NeedsMoreBytes())
		{
			streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
		}

		MessagePackReader discriminatorReader = new(discriminator.MsgPack);
		if (discriminatorReader.TryReadNil())
		{
			reader.ReturnReader(ref streamingReader);
			TUnion? result = await this.baseConverter.ReadAsync(reader, context).ConfigureAwait(false);
			return result;
		}

		MessagePackConverter? converter;
		if (discriminatorReader.NextMessagePackType == MessagePackType.Integer)
		{
			int alias = discriminatorReader.ReadInt32();
			if (!this.subTypes.DeserializersByIntAlias.TryGetValue(alias, out converter))
			{
				reader.ReturnReader(ref streamingReader);
				if (this.canPreserveUnknownUnionCase)
				{
					return await this.ReadUnknownUnionCaseAsync(reader, discriminator, context).ConfigureAwait(false);
				}

				throw new MessagePackSerializationException($"Unspecified alias {alias}.");
			}
		}
		else
		{
			ReadOnlySpan<byte> alias = StringEncoding.ReadStringSpan(ref discriminatorReader);
			if (!this.subTypes.DeserializersByStringAlias.TryGetValue(alias, out converter))
			{
				reader.ReturnReader(ref streamingReader);
				if (this.canPreserveUnknownUnionCase)
				{
					return await this.ReadUnknownUnionCaseAsync(reader, discriminator, context).ConfigureAwait(false);
				}

				throw new MessagePackSerializationException($"Unspecified alias \"{StringEncoding.UTF8.GetString(alias)}\".");
			}
		}

		reader.ReturnReader(ref streamingReader);
		return (TUnion?)await converter.ReadObjectAsync(reader, context).ConfigureAwait(false);
	}

	/// <inheritdoc/>
	public override async ValueTask WriteAsync(MessagePackAsyncWriter writer, TUnion? value, SerializationContext context)
	{
		bool tagNullPayload = context.TagCSharpUnionNullPayload;
		context.TagCSharpUnionNullPayload = false;
		if (!typeof(TUnion).IsValueType && value is null)
		{
			writer.WriteNil();
			return;
		}

		ref TUnion nonNullValue = ref Unsafe.AsRef(in value!);
		(DerivedTypeIdentifier Alias, MessagePackConverter Converter)? subtype = this.subTypes.TryGetSerializer(ref nonNullValue);
		if (subtype is { } nativeCase && this.untaggedCases?.Contains(nativeCase.Converter) == true)
		{
			bool nullPayload = this.untaggedCases.HasNullPayload(nativeCase.Converter, nonNullValue!);
			if (!nullPayload || (!tagNullPayload && typeof(TUnion).IsValueType))
			{
				await nativeCase.Converter.WriteObjectAsync(writer, value, context).ConfigureAwait(false);
				return;
			}
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
		if (subtype is { } taggedCase)
		{
			syncWriter.WriteRaw(taggedCase.Alias.MsgPackAlias.Span);
			converter = taggedCase.Converter;
		}
		else if (this.baseConverter is IUnknownUnionCaseFallback<TUnion> { CanPreserveUnknownUnionCase: true } fallback && fallback.TryGetUnknownUnionDiscriminator(nonNullValue, out RawMessagePack discriminator))
		{
			syncWriter.WriteRaw(discriminator);
			converter = this.baseConverter;
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
		JsonArray oneOfArray = [];
		if (this.untaggedCases is null)
		{
			oneOfArray.Add((JsonNode)CreateOneOfElement(null, this.baseConverter.GetJsonSchema(context, unionTypeShape.BaseType) ?? CreateUndocumentedSchema(this.baseConverter.GetType())));
		}

		foreach ((DerivedTypeIdentifier alias, MessagePackConverter converter, ITypeShape shape) in this.subTypes.Serializers)
		{
			JsonObject schema = context.GetJsonSchema(shape);
			bool isUntagged = this.untaggedCases?.Contains(converter) == true;
			if (this.untaggedCases is not null && !shape.Type.IsValueType && (!isUntagged || this.untaggedCases.CanSelectNil(converter)))
			{
				schema = ApplyJsonSchemaNullability(schema);
			}

			oneOfArray.Add(isUntagged ? schema : (JsonNode)CreateOneOfElement(alias, schema));
			if (isUntagged && !shape.Type.IsValueType)
			{
				oneOfArray.Add((JsonNode)CreateOneOfElement(alias, new() { ["type"] = "null" }));
			}
		}

		JsonObject unionSchema = new()
		{
			[this.untaggedCases is null ? "oneOf" : "anyOf"] = oneOfArray,
		};
		return this.untaggedCases is not null && !typeof(TUnion).IsValueType ? ApplyJsonSchemaNullability(unionSchema) : unionSchema;

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

				JsonObject tupleSchema = new()
				{
					["type"] = "array",
					["minItems"] = 2,
					["maxItems"] = 2,
				};
				context.ApplyTupleSchema(tupleSchema, new JsonArray(aliasSchema, schema));
				return tupleSchema;
			}
		}
	}

	private bool TryReadUnknownUnionCase(ref MessagePackReader reader, in RawMessagePack discriminator, SerializationContext context, out TUnion? result)
	{
		if (this.baseConverter is not IUnknownUnionCaseFallback<TUnion> { CanPreserveUnknownUnionCase: true } fallback)
		{
			result = default;
			return false;
		}

		result = this.baseConverter.Read(ref reader, context);
		if (result is not null)
		{
			fallback.SetUnknownUnionDiscriminator(ref result, discriminator);
		}

		return true;
	}

	private async ValueTask<TUnion?> ReadUnknownUnionCaseAsync(MessagePackAsyncReader reader, RawMessagePack discriminator, SerializationContext context)
	{
		var fallback = (IUnknownUnionCaseFallback<TUnion>)this.baseConverter;
		Assumes.True(fallback.CanPreserveUnknownUnionCase);

		TUnion? result = await this.baseConverter.ReadAsync(reader, context).ConfigureAwait(false);
		if (result is not null)
		{
			fallback.SetUnknownUnionDiscriminator(ref result, discriminator);
		}

		return result;
	}
}
