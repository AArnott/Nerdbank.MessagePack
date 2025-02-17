// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Microsoft;

namespace Nerdbank.PolySerializer.Converters;

/// <summary>
/// A formatter for a type that may serve as an ancestor class for the actual runtime type of a value to be (de)serialized.
/// </summary>
/// <typeparam name="TBase">The type that serves as the runtime type or the ancestor type for any runtime value.</typeparam>
internal class SubTypeUnionConverter<TBase> : Converter<TBase>
{
	private readonly SubTypes subTypes;
	private readonly Converter<TBase> baseConverter;

	/// <summary>
	/// Initializes a new instance of the <see cref="SubTypeUnionConverter{TBase}"/> class.
	/// </summary>
	/// <param name="subTypes">Contains maps to assist with converting subtypes.</param>
	/// <param name="baseConverter">The converter to use for values that are actual instances of the base type itself.</param>
	internal SubTypeUnionConverter(SubTypes subTypes, Converter<TBase> baseConverter)
	{
		this.subTypes = subTypes;
		this.baseConverter = baseConverter;
		this.PreferAsyncSerialization = baseConverter.PreferAsyncSerialization || subTypes.Serializers.Values.Any(subTypes => subTypes.Converter.PreferAsyncSerialization);
	}

	/// <inheritdoc/>
	public override bool PreferAsyncSerialization { get; }

	/// <inheritdoc/>
	public override TBase? Read(ref Reader reader, SerializationContext context)
	{
		if (reader.TryReadNull())
		{
			return default;
		}

		int count = reader.ReadArrayHeader();
		if (count != 2)
		{
			throw new SerializationException($"Expected an array of 2 elements, but found {count}.");
		}

		// The alias for the base type itself is simply nil.
		if (reader.TryReadNull())
		{
			return this.baseConverter.Read(ref reader, context);
		}

		Converter? converter;
		if (reader.NextTypeCode == TypeCode.Integer)
		{
			int alias = reader.ReadInt32();
			if (!this.subTypes.DeserializersByIntAlias.TryGetValue(alias, out converter))
			{
				throw new SerializationException($"Unknown alias {alias}.");
			}
		}
		else
		{
			ReadOnlySpan<byte> alias = reader.Deformatter.ReadStringSpan(ref reader);
			if (!this.subTypes.DeserializersByStringAlias.TryGetValue(alias, out converter))
			{
				throw new SerializationException($"Unknown alias \"{reader.Deformatter.StreamingDeformatter.Encoding.GetString(alias)}\".");
			}
		}

		return (TBase?)converter.ReadObject(ref reader, context);
	}

	/// <inheritdoc/>
	public override void Write(ref Writer writer, in TBase? value, SerializationContext context)
	{
		if (value is null)
		{
			writer.WriteNull();
			return;
		}

		writer.WriteArrayHeader(2);

		Type valueType = value.GetType();
		if (valueType.IsEquivalentTo(typeof(TBase)))
		{
			// The runtime type of the value matches the base exactly. Use nil as the alias.
			writer.WriteNull();
			this.baseConverter.Write(ref writer, value, context);
		}
		else if (this.subTypes.Serializers.TryGetValue(valueType, out (FormattedSubTypeAlias Alias, Converter Converter, ITypeShape Shape) result))
		{
			writer.Buffer.Write(result.Alias.FormattedAlias.Span);
			result.Converter.WriteObject(ref writer, value, context);
		}
		else
		{
			throw new SerializationException($"value is of type {valueType.FullName} which is not one of those listed via {KnownSubTypeAttribute.TypeName} on the declared base type {typeof(TBase).FullName}.");
		}
	}

	/// <inheritdoc/>
	[Experimental("NBMsgPackAsync")]
	public override async ValueTask<TBase?> ReadAsync(AsyncReader reader, SerializationContext context)
	{
		StreamingReader streamingReader = reader.CreateStreamingReader();
		bool success;
		while (streamingReader.TryReadNull(out success).NeedsMoreBytes())
		{
			streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
		}

		if (success)
		{
			reader.ReturnReader(ref streamingReader);
			return default;
		}

		int count;
		while (streamingReader.TryReadArrayHeader(out count).NeedsMoreBytes())
		{
			streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
		}

		if (count != 2)
		{
			throw new SerializationException($"Expected an array of 2 elements, but found {count}.");
		}

		// The alias for the base type itself is simply nil.
		bool isNull;
		while (streamingReader.TryReadNull(out isNull).NeedsMoreBytes())
		{
			streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
		}

		if (isNull)
		{
			reader.ReturnReader(ref streamingReader);
			TBase? result = await this.baseConverter.ReadAsync(reader, context).ConfigureAwait(false);
			return result;
		}

		TypeCode nextType;
		if (streamingReader.TryPeekNextCode(out nextType).NeedsMoreBytes())
		{
			streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
		}

		Converter? converter;
		if (nextType == TypeCode.Integer)
		{
			int alias;
			while (streamingReader.TryRead(out alias).NeedsMoreBytes())
			{
				streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
			}

			if (!this.subTypes.DeserializersByIntAlias.TryGetValue(alias, out converter))
			{
				throw new SerializationException($"Unknown alias {alias}.");
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
				Assumes.True(streamingReader.TryReadStringSequence(out ReadOnlySequence<byte> utf8Sequence) == DecodeResult.Success);
				alias = utf8Sequence.ToArray();
			}

			if (!this.subTypes.DeserializersByStringAlias.TryGetValue(alias, out converter))
			{
				throw new SerializationException($"Unknown alias \"{reader.Deformatter.StreamingDeformatter.Encoding.GetString(alias)}\".");
			}
		}

		reader.ReturnReader(ref streamingReader);
		return (TBase?)await converter.ReadObjectAsync(reader, context).ConfigureAwait(false);
	}

	/// <inheritdoc/>
	[Experimental("NBMsgPackAsync")]
	public override async ValueTask WriteAsync(AsyncWriter writer, TBase? value, SerializationContext context)
	{
		if (value is null)
		{
			writer.WriteNull();
			return;
		}

		Writer syncWriter = writer.CreateWriter();
		syncWriter.WriteArrayHeader(2);

		Type valueType = value.GetType();
		if (valueType.IsEquivalentTo(typeof(TBase)))
		{
			// The runtime type of the value matches the base exactly. Use nil as the alias.
			syncWriter.WriteNull();
			if (this.baseConverter.PreferAsyncSerialization)
			{
				writer.ReturnWriter(ref syncWriter);
				await this.baseConverter.WriteAsync(writer, value, context).ConfigureAwait(false);
			}
			else
			{
				this.baseConverter.Write(ref syncWriter, value, context);
				writer.ReturnWriter(ref syncWriter);
			}
		}
		else if (this.subTypes.Serializers.TryGetValue(valueType, out (FormattedSubTypeAlias Alias, Converter Converter, ITypeShape Shape) result))
		{
			syncWriter.Buffer.Write(result.Alias.FormattedAlias.Span);
			if (result.Converter.PreferAsyncSerialization)
			{
				writer.ReturnWriter(ref syncWriter);
				await result.Converter.WriteObjectAsync(writer, value, context).ConfigureAwait(false);
			}
			else
			{
				result.Converter.WriteObject(ref syncWriter, value, context);
				writer.ReturnWriter(ref syncWriter);
			}
		}
		else
		{
			throw new SerializationException($"value is of type {valueType.FullName} which is not one of those listed via {KnownSubTypeAttribute.TypeName} on the declared base type {typeof(TBase).FullName}.");
		}

		await writer.FlushIfAppropriateAsync(context).ConfigureAwait(false);
	}

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
	{
		JsonArray oneOfArray = [CreateOneOfElement(null, this.baseConverter.GetJsonSchema(context, typeShape) ?? CreateUndocumentedSchema(this.baseConverter.GetType()))];

		foreach ((FormattedSubTypeAlias alias, _, ITypeShape shape) in this.subTypes.Serializers.Values)
		{
			oneOfArray.Add((JsonNode)CreateOneOfElement(alias, context.GetJsonSchema(shape)));
		}

		return new()
		{
			["oneOf"] = oneOfArray,
		};

		JsonObject CreateOneOfElement(FormattedSubTypeAlias? alias, JsonObject schema)
		{
			JsonObject aliasSchema = new()
			{
				["type"] = alias switch
				{
					null => "null",
					{ Type: SubTypeAlias.AliasType.Integer } => "integer",
					{ Type: SubTypeAlias.AliasType.String } => "string",
					_ => throw new NotImplementedException(),
				},
			};
			if (alias is not null)
			{
				JsonNode enumValue = alias.Value.Type switch
				{
					SubTypeAlias.AliasType.String => (JsonNode)alias.Value.StringAlias,
					SubTypeAlias.AliasType.Integer => (JsonNode)alias.Value.IntAlias,
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
