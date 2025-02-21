// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Microsoft;

namespace ShapeShift.Converters;

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

		int? count = reader.ReadStartVector();
		if (count is not (2 or null))
		{
			ThrowWrongNumberOfElements(count.Value);
		}

		bool isFirstElement = true;
		if (count is null && !reader.TryAdvanceToNextElement(ref isFirstElement))
		{
			ThrowWrongNumberOfElements(0);
		}

		TBase? result;

		// The alias for the base type itself is simply nil.
		if (reader.TryReadNull())
		{
			if (count is null && !reader.TryAdvanceToNextElement(ref isFirstElement))
			{
				ThrowWrongNumberOfElements(1);
			}

			result = this.baseConverter.Read(ref reader, context);

			if (count is null && reader.TryAdvanceToNextElement(ref isFirstElement))
			{
				ThrowTooManyElements();
			}

			return result;
		}

		Converter? converter;
		if (reader.NextTypeCode == TokenType.Integer)
		{
			int alias = reader.ReadInt32();
			if (!this.subTypes.DeserializersByIntAlias.TryGetValue(alias, out converter))
			{
				throw new SerializationException($"Unknown alias {alias}.");
			}
		}
		else
		{
			if (reader.TryReadStringSpan(out ReadOnlySpan<byte> alias))
			{
				if (!this.subTypes.DeserializersByStringAlias.TryGetValue(alias, out converter))
				{
					throw new SerializationException($"Unknown alias \"{reader.Deformatter.StreamingDeformatter.Encoding.GetString(alias)}\".");
				}
			}
			else
			{
				reader.GetMaxStringLength(out _, out int maxBytes);
				byte[]? array = null;
				try
				{
					Span<byte> scratchBuffer = maxBytes > MaxStackStringCharLength ? array = ArrayPool<byte>.Shared.Rent(maxBytes) : stackalloc byte[maxBytes];
					int byteCount = reader.ReadString(scratchBuffer);
					if (!this.subTypes.DeserializersByStringAlias.TryGetValue(scratchBuffer[..byteCount], out converter))
					{
						throw new SerializationException($"Unknown alias \"{reader.Deformatter.StreamingDeformatter.Encoding.GetString(scratchBuffer[..byteCount])}\".");
					}
				}
				finally
				{
					if (array is not null)
					{
						ArrayPool<byte>.Shared.Return(array);
					}
				}
			}
		}

		if (count is null && !reader.TryAdvanceToNextElement(ref isFirstElement))
		{
			ThrowWrongNumberOfElements(1);
		}

		result = (TBase?)converter.ReadObject(ref reader, context);

		if (count is null && reader.TryAdvanceToNextElement(ref isFirstElement))
		{
			ThrowTooManyElements();
		}

		return result;
	}

	/// <inheritdoc/>
	public override void Write(ref Writer writer, in TBase? value, SerializationContext context)
	{
		if (value is null)
		{
			writer.WriteNull();
			return;
		}

		writer.WriteStartVector(2);

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

		int? count;
		while (streamingReader.TryReadArrayHeader(out count).NeedsMoreBytes())
		{
			streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
		}

		if (count is not (2 or null))
		{
			ThrowWrongNumberOfElements(count.Value);
		}

		TBase? result;

		// The alias for the base type itself is simply nil.
		bool isNull;
		while (streamingReader.TryReadNull(out isNull).NeedsMoreBytes())
		{
			streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
		}

		if (isNull)
		{
			if (count is null)
			{
				bool hasAnotherElement;
				bool isFirstElement = true;
				while (streamingReader.TryAdvanceToNextElement(ref isFirstElement, out hasAnotherElement).NeedsMoreBytes())
				{
					streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
				}

				if (!hasAnotherElement)
				{
					ThrowWrongNumberOfElements(1);
				}
			}

			reader.ReturnReader(ref streamingReader);
			result = await this.baseConverter.ReadAsync(reader, context).ConfigureAwait(false);

			if (count is null && await reader.TryAdvanceToNextElementAsync().ConfigureAwait(false))
			{
				ThrowTooManyElements();
			}

			return result;
		}

		TokenType nextType;
		if (streamingReader.TryPeekNextTypeCode(out nextType).NeedsMoreBytes())
		{
			streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
		}

		Converter? converter;
		if (nextType == TokenType.Integer)
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

			if (contiguous)
			{
				if (!this.subTypes.DeserializersByStringAlias.TryGetValue(alias, out converter))
				{
					throw new SerializationException($"Unknown alias \"{reader.Deformatter.StreamingDeformatter.Encoding.GetString(alias)}\".");
				}
			}
			else
			{
				byte[]? array = null;
				try
				{
					int maxBytes;
					while (streamingReader.TryGetMaxStringLength(out _, out maxBytes).NeedsMoreBytes())
					{
						streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
					}

					StreamingReader peekReader = streamingReader;
					if (peekReader.TryReadStringSpan(out _, out _).NeedsMoreBytes())
					{
						reader.ReturnReader(ref streamingReader);
						await reader.BufferNextStructureAsync(context).ConfigureAwait(false);
						streamingReader = reader.CreateStreamingReader();
					}

					Span<byte> scratchBuffer = maxBytes > MaxStackStringCharLength ? array = ArrayPool<byte>.Shared.Rent(maxBytes) : stackalloc byte[maxBytes];
					int bytesWritten;
					Assumes.False(streamingReader.TryReadString(scratchBuffer, out bytesWritten).NeedsMoreBytes());
					if (!this.subTypes.DeserializersByStringAlias.TryGetValue(scratchBuffer[..bytesWritten], out converter))
					{
						throw new SerializationException($"Unknown alias \"{reader.Deformatter.StreamingDeformatter.Encoding.GetString(scratchBuffer[..bytesWritten])}\".");
					}
				}
				finally
				{
					if (array is not null)
					{
						ArrayPool<byte>.Shared.Return(array);
					}
				}
			}
		}

		if (count is null)
		{
			bool hasAnotherElement;
			bool isFirstElement = true;
			while (streamingReader.TryAdvanceToNextElement(ref isFirstElement, out hasAnotherElement).NeedsMoreBytes())
			{
				streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
			}

			if (!hasAnotherElement)
			{
				ThrowWrongNumberOfElements(1);
			}
		}

		reader.ReturnReader(ref streamingReader);
		result = (TBase?)await converter.ReadObjectAsync(reader, context).ConfigureAwait(false);

		if (count is null && await reader.TryAdvanceToNextElementAsync().ConfigureAwait(false))
		{
			ThrowTooManyElements();
		}

		return result;
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
		syncWriter.WriteStartVector(2);

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

	[DoesNotReturn]
	private static void ThrowWrongNumberOfElements(int actual) => throw new SerializationException($"Expected an array of 2 elements, but found {actual}.");

	[DoesNotReturn]
	private static void ThrowTooManyElements() => throw new SerializationException("Expected an array of 2 elements, but found more.");
}
