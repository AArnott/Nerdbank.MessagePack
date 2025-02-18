// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json.Nodes;

namespace Nerdbank.PolySerializer.Converters;

/// <summary>
/// A <see cref="Converter{T}"/> that writes objects as arrays of property values.
/// Only data types with default constructors may be deserialized.
/// </summary>
/// <typeparam name="T">The type of objects that can be serialized or deserialized with this converter.</typeparam>
/// <param name="properties">The properties to be serialized.</param>
/// <param name="constructor">The constructor for the deserialized type.</param>
/// <param name="defaultValuesPolicy"><inheritdoc cref="ObjectMapConverter{T}.ObjectMapConverter(MapSerializableProperties{T}, MapDeserializableProperties{T}?, Func{T}?, SerializeDefaultValuesPolicy)" path="/param[@name='defaultValuesPolicy']"/></param>
internal class ObjectArrayConverter<T>(ReadOnlyMemory<PropertyAccessors<T>?> properties, Func<T>? constructor, SerializeDefaultValuesPolicy defaultValuesPolicy) : ObjectConverterBase<T>
{
	/// <inheritdoc/>
	public override bool PreferAsyncSerialization => true;

	/// <inheritdoc/>
	public override T? Read(ref Reader reader, SerializationContext context)
	{
		if (reader.TryReadNull())
		{
			return default;
		}

		if (constructor is null)
		{
			throw new NotSupportedException($"The {typeof(T).Name} type cannot be deserialized.");
		}

		context.DepthStep();
		T value = constructor();
		if (reader.NextTypeCode == PolySerializer.Converters.TokenType.Map)
		{
			// The indexes we have are the keys in the map rather than indexes into the array.
			int count = reader.ReadStartMap();
			for (int i = 0; i < count; i++)
			{
				int index = reader.ReadInt32();
				if (properties.Length > index && properties.Span[index]?.MsgPackReaders is var (deserialize, _))
				{
					deserialize(ref value, ref reader, context);
				}
				else
				{
					reader.Skip(context);
				}
			}
		}
		else
		{
			int count = reader.ReadStartVector();
			for (int i = 0; i < count; i++)
			{
				if (properties.Length > i && properties.Span[i]?.MsgPackReaders is var (deserialize, _))
				{
					deserialize(ref value, ref reader, context);
				}
				else
				{
					reader.Skip(context);
				}
			}
		}

		if (value is ISerializationCallbacks callbacks)
		{
			callbacks.OnAfterDeserialize();
		}

		return value;
	}

	/// <inheritdoc/>
#pragma warning disable NBMsgPack031 // Exactly one structure - this method is super complicated and beyond the analyzer
	public override void Write(ref Writer writer, in T? value, SerializationContext context)
#pragma warning restore NBMsgPack031
	{
		if (value is null)
		{
			writer.WriteNull();
			return;
		}

		if (value is ISerializationCallbacks callbacks)
		{
			callbacks.OnBeforeSerialize();
		}

		context.DepthStep();

		if (defaultValuesPolicy != SerializeDefaultValuesPolicy.Always && properties.Length > 0)
		{
			int[]? indexesToIncludeArray = null;
			try
			{
				if (this.ShouldUseMap(writer.Formatter, value, ref indexesToIncludeArray, out _, out ReadOnlySpan<int> indexesToInclude))
				{
					writer.WriteStartMap(indexesToInclude.Length);
					for (int i = 0; i < indexesToInclude.Length; i++)
					{
						int index = indexesToInclude[i];

						// In this case, we're serializing the *index* as the key rather than the property name.
						// It is faster and more compact that way, and we have the user-assigned indexes to use anyway.
						writer.Write(index);

						// The null forgiveness operators are safe because our filter would only have included
						// this index if these values are non-null.
						properties.Span[index]!.Value.MsgPackWriters!.Value.Serialize(value, ref writer, context);
					}
				}
				else if (indexesToInclude.Length == 0)
				{
					writer.WriteStartVector(0);
				}
				else
				{
					// Just serialize as an array, but truncate to the last index that *wanted* to be serialized.
					// We +1 to the last index because the slice has an exclusive end index.
					WriteArray(ref writer, value, properties.Span[..(indexesToInclude[^1] + 1)], context);
				}
			}
			finally
			{
				if (indexesToIncludeArray is not null)
				{
					ArrayPool<int>.Shared.Return(indexesToIncludeArray);
				}
			}
		}
		else
		{
			WriteArray(ref writer, value, properties.Span, context);
		}

		static void WriteArray(ref Writer writer, in T value, ReadOnlySpan<PropertyAccessors<T>?> properties, SerializationContext context)
		{
			writer.WriteStartVector(properties.Length);
			for (int i = 0; i < properties.Length; i++)
			{
				if (properties[i]?.MsgPackWriters is var (serialize, _))
				{
					serialize(value, ref writer, context);
				}
				else
				{
					writer.WriteNull();
				}
			}
		}
	}

	/// <inheritdoc/>
	[Experimental("NBMsgPackAsync")]
	public override async ValueTask WriteAsync(AsyncWriter writer, T? value, SerializationContext context)
	{
		if (value is null)
		{
			writer.WriteNull();
			return;
		}

		if (value is ISerializationCallbacks callbacks)
		{
			callbacks.OnBeforeSerialize();
		}

		context.DepthStep();

		if (defaultValuesPolicy != SerializeDefaultValuesPolicy.Always && properties.Length > 0)
		{
			int[]? indexesToIncludeArray = null;
			try
			{
				if (this.ShouldUseMap(writer.Formatter, value, ref indexesToIncludeArray, out ReadOnlyMemory<int> indexesToInclude, out _))
				{
					await WriteAsMapAsync(writer, value, indexesToInclude, properties, context).ConfigureAwait(false);
				}
				else if (indexesToInclude.Length == 0)
				{
					Writer syncWriter = writer.CreateWriter();
					syncWriter.WriteStartVector(0);
					writer.ReturnWriter(ref syncWriter);
					await writer.FlushIfAppropriateAsync(context).ConfigureAwait(false);
				}
				else
				{
					// Just serialize as an array, but truncate to the last index that *wanted* to be serialized.
					// We +1 to the last index because the slice has an exclusive end index.
					await WriteAsArrayAsync(writer, value, properties[..(indexesToInclude.Span[^1] + 1)], context).ConfigureAwait(false);
				}
			}
			finally
			{
				if (indexesToIncludeArray is not null)
				{
					ArrayPool<int>.Shared.Return(indexesToIncludeArray);
				}
			}
		}
		else
		{
			await WriteAsArrayAsync(writer, value, properties, context).ConfigureAwait(false);
		}

		static async ValueTask WriteAsMapAsync(AsyncWriter writer, T value, ReadOnlyMemory<int> properties, ReadOnlyMemory<PropertyAccessors<T>?> allProperties, SerializationContext context)
		{
			Writer syncWriter = writer.CreateWriter();
			syncWriter.WriteStartMap(properties.Length);
			writer.ReturnWriter(ref syncWriter);
			int i = 0;
			while (i < properties.Length)
			{
				// Do a batch of all the consecutive properties that should be written synchronously.
				int syncBatchSize = NextSyncBatchSize();
				int syncWriteEndExclusive = i + syncBatchSize;
				while (i < syncWriteEndExclusive)
				{
					// We use a nested loop here because even during synchronous writing, we may need to occasionally yield to
					// flush what we've written so far, but then we want to come right back to synchronous writing.
					syncWriter = writer.CreateWriter();
					for (; i < syncWriteEndExclusive && !writer.IsTimeToFlush(context, syncWriter); i++)
					{
						syncWriter.Write(properties.Span[i]);

						// The null forgiveness operators are safe because our filter would only have included
						// this index if these values are non-null.
						allProperties.Span[properties.Span[i]]!.Value.MsgPackWriters!.Value.Serialize(value, ref syncWriter, context);
					}

					writer.ReturnWriter(ref syncWriter);
					await writer.FlushIfAppropriateAsync(context).ConfigureAwait(false);
				}

				// Write all consecutive async properties.
				for (; i < properties.Length; i++)
				{
					if (allProperties.Span[properties.Span[i]] is not PropertyAccessors<T> { PreferAsyncSerialization: true, MsgPackWriters: var (_, serializeAsync) })
					{
						break;
					}

					syncWriter = writer.CreateWriter();
					syncWriter.Write(properties.Span[i]);
					writer.ReturnWriter(ref syncWriter);

					await serializeAsync(value, writer, context).ConfigureAwait(false);
				}

				int NextSyncBatchSize()
				{
					// We want to count the number of array elements need to be written up to the next async property.
					for (int j = i; j < properties.Length; j++)
					{
						if (properties.Length > j)
						{
							PropertyAccessors<T>? property = allProperties.Span[properties.Span[j]];
							if (property?.PreferAsyncSerialization is true && property.Value.MsgPackWriters is not null)
							{
								return j - i;
							}
						}
					}

					// We didn't encounter any more async property readers.
					return properties.Length - i;
				}
			}
		}

		static async ValueTask WriteAsArrayAsync(AsyncWriter writer, T value, ReadOnlyMemory<PropertyAccessors<T>?> properties, SerializationContext context)
		{
			Writer syncWriter = writer.CreateWriter();
			syncWriter.WriteStartVector(properties.Length);
			writer.ReturnWriter(ref syncWriter);
			int i = 0;
			while (i < properties.Length)
			{
				// Do a batch of all the consecutive properties that should be written synchronously.
				int syncBatchSize = NextSyncBatchSize();
				int syncWriteEndExclusive = i + syncBatchSize;
				while (i < syncWriteEndExclusive)
				{
					// We use a nested loop here because even during synchronous writing, we may need to occasionally yield to
					// flush what we've written so far, but then we want to come right back to synchronous writing.
					syncWriter = writer.CreateWriter();
					for (; i < syncWriteEndExclusive && !writer.IsTimeToFlush(context, syncWriter); i++)
					{
						if (properties.Span[i] is { MsgPackWriters: var (serialize, _) })
						{
							serialize(value, ref syncWriter, context);
						}
						else
						{
							syncWriter.WriteNull();
						}
					}

					writer.ReturnWriter(ref syncWriter);
					await writer.FlushIfAppropriateAsync(context).ConfigureAwait(false);
				}

				// Write all consecutive async properties.
				for (; i < properties.Length; i++)
				{
					if (properties.Span[i] is not PropertyAccessors<T> { PreferAsyncSerialization: true, MsgPackWriters: var (_, serializeAsync) })
					{
						break;
					}

					await serializeAsync(value, writer, context).ConfigureAwait(false);
				}

				int NextSyncBatchSize()
				{
					// We want to count the number of array elements need to be written up to the next async property.
					for (int j = i; j < properties.Length; j++)
					{
						if (properties.Length > j)
						{
							PropertyAccessors<T>? property = properties.Span[j];
							if (property?.PreferAsyncSerialization is true && property.Value.MsgPackWriters is not null)
							{
								return j - i;
							}
						}
					}

					// We didn't encounter any more async property readers.
					return properties.Length - i;
				}
			}
		}
	}

	/// <inheritdoc/>
	[Experimental("NBMsgPackAsync")]
	public override async ValueTask<T?> ReadAsync(AsyncReader reader, SerializationContext context)
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

		if (constructor is null)
		{
			throw new NotSupportedException($"The {typeof(T).Name} type cannot be deserialized.");
		}

		context.DepthStep();
		T value = constructor();

		PolySerializer.Converters.TokenType peekType;
		while (streamingReader.TryPeekNextTypeCode(out peekType).NeedsMoreBytes())
		{
			streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
		}

		if (peekType == PolySerializer.Converters.TokenType.Map)
		{
			int mapEntries;
			while (streamingReader.TryReadMapHeader(out mapEntries).NeedsMoreBytes())
			{
				streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
			}

			// We're going to read in bursts. Anything we happen to get in one buffer, we'll read synchronously regardless of whether the property is async.
			// But when we run out of buffer, if the next thing to read is async, we'll read it async.
			reader.ReturnReader(ref streamingReader);
			int remainingEntries = mapEntries;
			while (remainingEntries > 0)
			{
				int bufferedStructures = await reader.BufferNextStructuresAsync(1, remainingEntries * 2, context).ConfigureAwait(false);
				Reader syncReader = reader.CreateBufferedReader();
				int bufferedEntries = bufferedStructures / 2;
				for (int i = 0; i < bufferedEntries; i++)
				{
					int propertyIndex = syncReader.ReadInt32();
					if (propertyIndex < properties.Length && properties.Span[propertyIndex] is { MsgPackReaders: { Deserialize: { } deserialize } })
					{
						deserialize(ref value, ref syncReader, context);
					}
					else
					{
						syncReader.Skip(context);
					}

					remainingEntries--;
				}

				if (remainingEntries > 0)
				{
					// To know whether the next property is async, we need to know its index.
					// If its index isn't in the buffer, we'll just loop around and get it in the next buffer.
					if (bufferedStructures % 2 == 1)
					{
						// The property name has already been buffered.
						int propertyIndex = syncReader.ReadInt32();
						if (propertyIndex < properties.Length && properties.Span[propertyIndex] is { PreferAsyncSerialization: true, MsgPackReaders: { } propertyReader })
						{
							// The next property value is async, so turn in our sync reader and read it asynchronously.
							reader.ReturnReader(ref syncReader);
							value = await propertyReader.DeserializeAsync(value, reader, context).ConfigureAwait(false);
							remainingEntries--;

							// Now loop around to see what else we can do with the next buffer.
							continue;
						}
					}
					else
					{
						// The property name isn't in the buffer, and thus whether it'll have an async reader.
						reader.ReturnReader(ref syncReader);
						await reader.ReadAsync().ConfigureAwait(false);

						continue;
					}
				}

				reader.ReturnReader(ref syncReader);
			}
		}
		else
		{
			int arrayLength;
			while (streamingReader.TryReadArrayHeader(out arrayLength).NeedsMoreBytes())
			{
				streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
			}

			reader.ReturnReader(ref streamingReader);
			int i = 0;
			while (i < arrayLength)
			{
				// Do a batch of all the consecutive properties that should be read synchronously.
				int syncBatchSize = NextSyncReadBatchSize();
				if (syncBatchSize > 0)
				{
					await reader.BufferNextStructuresAsync(syncBatchSize, syncBatchSize, context).ConfigureAwait(false);
					Reader syncReader = reader.CreateBufferedReader();
					for (int syncReadEndExclusive = i + syncBatchSize; i < syncReadEndExclusive; i++)
					{
						if (properties.Length > i && properties.Span[i]?.MsgPackReaders is var (deserialize, _))
						{
							deserialize(ref value, ref syncReader, context);
						}
						else
						{
							syncReader.Skip(context);
						}
					}

					reader.ReturnReader(ref syncReader);
				}

				// Read any consecutive async properties.
				for (; i < arrayLength && properties.Length > i; i++)
				{
					if (properties.Span[i] is not PropertyAccessors<T> { PreferAsyncSerialization: true, MsgPackReaders: (_, { } deserializeAsync) })
					{
						break;
					}

					value = await deserializeAsync(value, reader, context).ConfigureAwait(false);
				}

				int NextSyncReadBatchSize()
				{
					// We want to count the number of array elements need to be read up to the next async property.
					for (int j = i; j < arrayLength; j++)
					{
						if (properties.Length > j)
						{
							PropertyAccessors<T>? property = properties.Span[j];
							if (property?.PreferAsyncSerialization is true && property.Value.MsgPackReaders is not null)
							{
								return j - i;
							}
						}
					}

					// We didn't encounter any more async property readers.
					return arrayLength - i;
				}
			}
		}

		if (value is ISerializationCallbacks callbacks)
		{
			callbacks.OnAfterDeserialize();
		}

		return value;
	}

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
	{
		var objectShape = (IObjectTypeShape<T>)typeShape;

		JsonObject schema = new()
		{
			["type"] = new JsonArray(["object", "array"]),
		};

		if (objectShape.Properties.Count > 0)
		{
			Dictionary<string, IConstructorParameterShape>? ctorParams = CreatePropertyAndParameterDictionary(objectShape);

			JsonObject propertiesObject = [];
			JsonArray? items = [];
			JsonArray? required = null;
			int minItems = 0;
			for (int i = 0; i < properties.Length; i++)
			{
				if (properties.Span[i] is not PropertyAccessors<T> property)
				{
					continue;
				}

				IConstructorParameterShape? associatedParameter = null;
				ctorParams?.TryGetValue(property.Shape.Name, out associatedParameter);

				JsonObject propertySchema = context.GetJsonSchema(property.Shape.PropertyType);
				ApplyDescription(property.Shape.AttributeProvider, propertySchema, property.Shape.Name);
				ApplyDefaultValue(property.Shape.AttributeProvider, propertySchema, associatedParameter);
				if (!IsNonNullable(property.Shape, associatedParameter))
				{
					propertySchema = ApplyJsonSchemaNullability(propertySchema);
				}

				string objectPropertyName = i.ToString(CultureInfo.InvariantCulture);
				propertiesObject.Add(objectPropertyName, propertySchema);

				while (items.Count < i)
				{
					items.Add((JsonNode)new JsonObject
					{
						["type"] = new JsonArray("number", "integer", "string", "boolean", "object", "array", "null"),
						["description"] = "This is an undocumented element that is ignored by the deserializer and always serialized as null.",
					});
				}

				items.Add(propertySchema.DeepClone());

				if (associatedParameter?.IsRequired is true)
				{
					(required ??= []).Add((JsonNode)objectPropertyName);

					// In the case of an array, a required element means the array must be at least this long.
					minItems = i + 1;
				}
			}

			schema["properties"] = propertiesObject;
			schema["items"] = items;

			// Only describe the properties as required if we guarantee that we'll write them.
			if ((defaultValuesPolicy & SerializeDefaultValuesPolicy.Required) == SerializeDefaultValuesPolicy.Required)
			{
				if (required is not null)
				{
					schema["required"] = required;
				}

				if (minItems > 0)
				{
					schema["minItems"] = minItems;
				}
			}
		}

		return schema;
	}

	/// <inheritdoc/>
	[Experimental("NBMsgPackAsync")]
	public override async ValueTask<bool> SkipToPropertyValueAsync(AsyncReader reader, IPropertyShape propertyShape, SerializationContext context)
	{
		int index = -1;
		for (int i = 0; i < properties.Length; i++)
		{
			PropertyAccessors<T>? property = properties.Span[i];
			if (propertyShape.Equals(property?.Shape))
			{
				index = i;
				break;
			}
		}

		if (index == -1)
		{
			return false;
		}

		StreamingReader streamingReader = reader.CreateStreamingReader();
		bool isNil;
		while (streamingReader.TryReadNull(out isNil).NeedsMoreBytes())
		{
			streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
		}

		if (isNil)
		{
			reader.ReturnReader(ref streamingReader);
			return false;
		}

		context.DepthStep();

		PolySerializer.Converters.TokenType peekType;
		while (streamingReader.TryPeekNextTypeCode(out peekType).NeedsMoreBytes())
		{
			streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
		}

		if (peekType == PolySerializer.Converters.TokenType.Map)
		{
			int count;
			while (streamingReader.TryReadMapHeader(out count).NeedsMoreBytes())
			{
				streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
			}

			for (int i = 0; i < count; i++)
			{
				if (streamingReader.TryRead(out int key).NeedsMoreBytes())
				{
					streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
				}

				if (key == index)
				{
					reader.ReturnReader(ref streamingReader);
					return true;
				}

				// Skip over the value.
				if (streamingReader.TrySkip(ref context).NeedsMoreBytes())
				{
					streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
				}
			}

			reader.ReturnReader(ref streamingReader);
			return false;
		}
		else
		{
			int count;
			while (streamingReader.TryReadArrayHeader(out count).NeedsMoreBytes())
			{
				streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
			}

			if (count < index + 1)
			{
				reader.ReturnReader(ref streamingReader);
				return false;
			}

			// Skip over the preceding elements.
			for (int i = 0; i < index; i++)
			{
				while (streamingReader.TrySkip(ref context).NeedsMoreBytes())
				{
					streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
				}
			}

			reader.ReturnReader(ref streamingReader);
			return true;
		}
	}

	private Memory<int> GetPropertiesToSerialize(in T value, Memory<int> include)
	{
		return include[..this.GetPropertiesToSerialize(value, include.Span)];
	}

	private int GetPropertiesToSerialize(in T value, Span<int> include)
	{
		ReadOnlySpan<PropertyAccessors<T>?> propertiesSpan = properties.Span;
		int propertyCount = 0;
		for (int i = 0; i < propertiesSpan.Length; i++)
		{
			if (propertiesSpan[i] is { MsgPackWriters: not null } property && property.ShouldSerialize?.Invoke(value) is not false)
			{
				include[propertyCount++] = i;
			}
		}

		return propertyCount;
	}

	private bool ShouldUseMap(Formatter formatter, in T value, ref int[]? indexesToIncludeArray, out ReadOnlyMemory<int> indexesToIncludeMemory, out ReadOnlySpan<int> indexesToIncludeSpan)
	{
		indexesToIncludeArray = ArrayPool<int>.Shared.Rent(properties.Length);

		indexesToIncludeMemory = this.GetPropertiesToSerialize(value, indexesToIncludeArray.AsMemory());
		indexesToIncludeSpan = indexesToIncludeMemory.Span;
		if (indexesToIncludeMemory.Length == 0)
		{
			return false;
		}

		// Determine whether an array or a map would be more efficient.
		// A map will incur a penalty for writing the indexes as the key, which we'll estimate based on the size of the largest index's msgpack representation.
		// There's no way in an array to represent a "missing" value (since Nil is in fact a valid value), so ShouldSerialize is only useful for
		// array representations when we can truncate the array.
		// We can't cheaply predict how large a value that didn't need to be serialized would be, but since they are 'default' values for their type,
		// we'll assume each is just 1 byte.
		int maxKeyLength = formatter.GetEncodedLength(indexesToIncludeSpan[^1]);
		int mapOverhead = maxKeyLength * indexesToIncludeSpan.Length;
		int arrayOverhead = indexesToIncludeSpan[^1] + 1 - indexesToIncludeSpan.Length; // number of indexes that are required - number of indexes that are useful.

		// Go with whichever representation will be most compact, by estimate.
		return mapOverhead < arrayOverhead;
	}
}
