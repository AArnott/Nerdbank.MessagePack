// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Microsoft;
using Nerdbank.PolySerializer.Converters;
using Nerdbank.PolySerializer.MessagePack;

namespace Nerdbank.PolySerializer.MessagePack.Converters;

/// <summary>
/// A formatter for a type that may serve as an ancestor class for the actual runtime type of a value to be (de)serialized.
/// </summary>
/// <typeparam name="TBase">The type that serves as the runtime type or the ancestor type for any runtime value.</typeparam>
internal class SubTypeUnionConverter<TBase> : MessagePackConverter<TBase>
{
	private readonly SubTypes subTypes;
	private readonly MessagePackConverter<TBase> baseConverter;

	/// <summary>
	/// Initializes a new instance of the <see cref="SubTypeUnionConverter{TBase}"/> class.
	/// </summary>
	/// <param name="subTypes">Contains maps to assist with converting subtypes.</param>
	/// <param name="baseConverter">The converter to use for values that are actual instances of the base type itself.</param>
	internal SubTypeUnionConverter(SubTypes subTypes, MessagePackConverter<TBase> baseConverter)
	{
		this.subTypes = subTypes;
		this.baseConverter = baseConverter;
		this.PreferAsyncSerialization = baseConverter.PreferAsyncSerialization || subTypes.Serializers.Values.Any(subTypes => subTypes.Converter.PreferAsyncSerialization);
	}

	/// <inheritdoc/>
	public override bool PreferAsyncSerialization { get; }

	/// <inheritdoc/>
	public override TBase? Read(ref MessagePackReader reader, SerializationContext context)
	{
		if (reader.TryReadNil())
		{
			return default;
		}

		int count = reader.ReadArrayHeader();
		if (count != 2)
		{
			throw new SerializationException($"Expected an array of 2 elements, but found {count}.");
		}

		// The alias for the base type itself is simply nil.
		if (reader.TryReadNil())
		{
			return this.baseConverter.Read(ref reader, context);
		}

		Converter? converter;
		if (reader.NextMessagePackType == MessagePackType.Integer)
		{
			int alias = reader.ReadInt32();
			if (!this.subTypes.DeserializersByIntAlias.TryGetValue(alias, out converter))
			{
				throw new SerializationException($"Unknown alias {alias}.");
			}
		}
		else
		{
			ReadOnlySpan<byte> alias = StringEncoding.ReadStringSpan(ref reader);
			if (!this.subTypes.DeserializersByStringAlias.TryGetValue(alias, out converter))
			{
				throw new SerializationException($"Unknown alias \"{StringEncoding.UTF8.GetString(alias)}\".");
			}
		}

		return (TBase?)((IMessagePackConverter)converter).ReadObject(ref reader, context);
	}

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in TBase? value, SerializationContext context)
	{
		if (value is null)
		{
			writer.WriteNil();
			return;
		}

		writer.WriteArrayHeader(2);

		Type valueType = value.GetType();
		if (valueType.IsEquivalentTo(typeof(TBase)))
		{
			// The runtime type of the value matches the base exactly. Use nil as the alias.
			writer.WriteNil();
			this.baseConverter.Write(ref writer, value, context);
		}
		else if (this.subTypes.Serializers.TryGetValue(valueType, out (SubTypeAlias Alias, Converter Converter, ITypeShape Shape) result))
		{
			writer.WriteRaw(result.Alias.MsgPackAlias.Span);
			((IMessagePackConverter)result.Converter).WriteObject(ref writer, value, context);
		}
		else
		{
			throw new SerializationException($"value is of type {valueType.FullName} which is not one of those listed via {KnownSubTypeAttribute.TypeName} on the declared base type {typeof(TBase).FullName}.");
		}
	}

	/// <inheritdoc/>
	[Experimental("NBMsgPackAsync")]
	public override async ValueTask<TBase?> ReadAsync(MessagePackAsyncReader reader, SerializationContext context)
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
		bool isNil;
		while (streamingReader.TryReadNil(out isNil).NeedsMoreBytes())
		{
			streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
		}

		if (isNil)
		{
			reader.ReturnReader(ref streamingReader);
			TBase? result = await this.baseConverter.ReadAsync(reader, context).ConfigureAwait(false);
			return result;
		}

		MessagePackType nextMessagePackType;
		if (streamingReader.TryPeekNextMessagePackType(out nextMessagePackType).NeedsMoreBytes())
		{
			streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
		}

		Converter? converter;
		if (nextMessagePackType == MessagePackType.Integer)
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
				throw new SerializationException($"Unknown alias \"{StringEncoding.UTF8.GetString(alias)}\".");
			}
		}

		reader.ReturnReader(ref streamingReader);
		return (TBase?)await ((IMessagePackConverter)converter).ReadObjectAsync(reader, context).ConfigureAwait(false);
	}

	/// <inheritdoc/>
	[Experimental("NBMsgPackAsync")]
	public override async ValueTask WriteAsync(MessagePackAsyncWriter writer, TBase? value, SerializationContext context)
	{
		if (value is null)
		{
			writer.WriteNil();
			return;
		}

		MessagePackWriter syncWriter = writer.CreateWriter();
		syncWriter.WriteArrayHeader(2);

		Type valueType = value.GetType();
		if (valueType.IsEquivalentTo(typeof(TBase)))
		{
			// The runtime type of the value matches the base exactly. Use nil as the alias.
			syncWriter.WriteNil();
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
		else if (this.subTypes.Serializers.TryGetValue(valueType, out (SubTypeAlias Alias, Converter Converter, ITypeShape Shape) result))
		{
			syncWriter.WriteRaw(result.Alias.MsgPackAlias.Span);
			if (result.Converter.PreferAsyncSerialization)
			{
				writer.ReturnWriter(ref syncWriter);
				await ((IMessagePackConverter)result.Converter).WriteObjectAsync(writer, value, context).ConfigureAwait(false);
			}
			else
			{
				((IMessagePackConverter)result.Converter).WriteObject(ref syncWriter, value, context);
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

		foreach ((SubTypeAlias alias, _, ITypeShape shape) in this.subTypes.Serializers.Values)
		{
			oneOfArray.Add((JsonNode)CreateOneOfElement(alias, context.GetJsonSchema(shape)));
		}

		return new()
		{
			["oneOf"] = oneOfArray,
		};

		JsonObject CreateOneOfElement(SubTypeAlias? alias, JsonObject schema)
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
