// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using Microsoft;

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// A formatter for a type that may serve as an ancestor class for the actual runtime type of a value to be (de)serialized.
/// </summary>
/// <typeparam name="TUnion">The type that serves as the declared type or the ancestor type for any runtime value.</typeparam>
internal class UnionConverter<TUnion>(MessagePackConverter<TUnion> baseConverter, SubTypes<TUnion> subTypes) : MessagePackConverter<TUnion>
{
	/// <inheritdoc/>
	public override bool PreferAsyncSerialization { get; } = baseConverter.PreferAsyncSerialization || subTypes.Serializers.Any(t => t.Converter.PreferAsyncSerialization);

	/// <inheritdoc/>
	public override void Read(ref MessagePackReader reader, ref TUnion? value, SerializationContext context)
	{
		if (reader.TryReadNil())
		{
			value = default;
			return;
		}

		int count = reader.ReadArrayHeader();
		if (count != 2)
		{
			throw new MessagePackSerializationException($"Expected an array of 2 elements, but found {count}.");
		}

		// The alias for the base type itself is simply nil.
		if (reader.TryReadNil())
		{
			baseConverter.Read(ref reader, ref value, context);
			return;
		}

		MessagePackConverter? converter;
		if (reader.NextMessagePackType == MessagePackType.Integer)
		{
			int alias = reader.ReadInt32();
			if (!subTypes.DeserializersByIntAlias.TryGetValue(alias, out converter))
			{
				throw new MessagePackSerializationException($"Unknown alias {alias}.");
			}
		}
		else
		{
			ReadOnlySpan<byte> alias = StringEncoding.ReadStringSpan(ref reader);
			if (!subTypes.DeserializersByStringAlias.TryGetValue(alias, out converter))
			{
				throw new MessagePackSerializationException($"Unknown alias \"{StringEncoding.UTF8.GetString(alias)}\".");
			}
		}

		value = (TUnion?)converter.ReadObject(ref reader, context);
	}

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in TUnion? value, SerializationContext context)
	{
		if (value is null)
		{
			writer.WriteNil();
			return;
		}

		writer.WriteArrayHeader(2);

		MessagePackConverter converter;
		if (subTypes.TryGetSerializer(ref Unsafe.AsRef(in value)) is { } subtype)
		{
			writer.WriteRaw(subtype.Alias.MsgPackAlias.Span);
			converter = subtype.Converter;
		}
		else
		{
			writer.WriteNil();
			converter = baseConverter;
		}

		converter.WriteObject(ref writer, value, context);
	}

	/// <inheritdoc/>
	[Experimental("NBMsgPackAsync")]
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

		int count;
		while (streamingReader.TryReadArrayHeader(out count).NeedsMoreBytes())
		{
			streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
		}

		if (count != 2)
			{
			throw new MessagePackSerializationException($"Expected an array of 2 elements, but found {count}.");
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
			TUnion? result = await baseConverter.ReadAsync(reader, context).ConfigureAwait(false);
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

			if (!subTypes.DeserializersByIntAlias.TryGetValue(alias, out converter))
			{
				throw new MessagePackSerializationException($"Unknown alias {alias}.");
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

			if (!subTypes.DeserializersByStringAlias.TryGetValue(alias, out converter))
			{
				throw new MessagePackSerializationException($"Unknown alias \"{StringEncoding.UTF8.GetString(alias)}\".");
			}
		}

		reader.ReturnReader(ref streamingReader);
		return (TUnion?)await converter.ReadObjectAsync(reader, context).ConfigureAwait(false);
	}

	/// <inheritdoc/>
	[Experimental("NBMsgPackAsync")]
	public override async ValueTask WriteAsync(MessagePackAsyncWriter writer, TUnion? value, SerializationContext context)
	{
		if (value is null)
		{
			writer.WriteNil();
			return;
		}

		MessagePackWriter syncWriter = writer.CreateWriter();
		syncWriter.WriteArrayHeader(2);

		MessagePackConverter converter;
		if (subTypes.TryGetSerializer(ref Unsafe.AsRef(in value)) is { } subtype)
		{
			syncWriter.WriteRaw(subtype.Alias.MsgPackAlias.Span);
			converter = subtype.Converter;
		}
		else
		{
			syncWriter.WriteNil();
			converter = baseConverter;
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
		JsonArray oneOfArray = [CreateOneOfElement(null, baseConverter.GetJsonSchema(context, unionTypeShape.BaseType) ?? CreateUndocumentedSchema(baseConverter.GetType()))];

		foreach ((DerivedTypeIdentifier alias, _, ITypeShape shape) in subTypes.Serializers)
		{
			oneOfArray.Add((JsonNode)CreateOneOfElement(alias, context.GetJsonSchema(shape)));
		}

		return new()
		{
			["oneOf"] = oneOfArray,
		};

		JsonObject CreateOneOfElement(DerivedTypeIdentifier? alias, JsonObject schema)
		{
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
