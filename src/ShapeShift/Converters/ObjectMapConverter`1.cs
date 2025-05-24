// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Microsoft;
using ShapeShift.MessagePack;

namespace ShapeShift.Converters;

/// <summary>
/// A <see cref="Converter{T}"/> that writes objects as maps of property names to values.
/// Only data types with default constructors may be deserialized.
/// </summary>
/// <typeparam name="T">The type of objects that can be serialized or deserialized with this converter.</typeparam>
/// <param name="serializable">Tools for serializing individual property values.</param>
/// <param name="deserializable">Tools for deserializing individual property values. May be omitted if the type will never be deserialized (i.e. there is no deserializing constructor).</param>
/// <param name="constructor">The default constructor, if present.</param>
/// <param name="defaultValuesPolicy">The policy for whether to serialize properties. When not <see cref="SerializeDefaultValuesPolicy.Always"/>, the <see cref="SerializableProperty{TDeclaringType}.ShouldSerialize"/> property will be consulted prior to serialization.</param>
internal class ObjectMapConverter<T>(MapSerializableProperties<T> serializable, MapDeserializableProperties<T>? deserializable, Func<T>? constructor, SerializeDefaultValuesPolicy defaultValuesPolicy) : Converter<T>
{
	/// <inheritdoc/>
	public override bool PreferAsyncSerialization => true;

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

		if (defaultValuesPolicy != SerializeDefaultValuesPolicy.Always && serializable.Properties.Length > 0)
		{
			SerializableProperty<T>[] include = ArrayPool<SerializableProperty<T>>.Shared.Rent(serializable.Properties.Length);
			try
			{
				WriteProperties(ref writer, value, this.GetPropertiesToSerialize(value, include.AsMemory()).Span, context);
			}
			finally
			{
				ArrayPool<SerializableProperty<T>>.Shared.Return(include);
			}
		}
		else
		{
			WriteProperties(ref writer, value, serializable.Properties.Span, context);
		}

		static void WriteProperties(ref Writer writer, in T value, ReadOnlySpan<SerializableProperty<T>> properties, SerializationContext context)
		{
			writer.WriteStartMap(properties.Length);
			bool first = true;
			foreach (SerializableProperty<T> property in properties)
			{
				if (!first)
				{
					writer.WriteMapPairSeparator();
				}

				first = false;
				writer.Buffer.Write(property.RawPropertyNameString.Span);
				writer.WriteMapKeyValueSeparator();
				property.Write(value, ref writer, context);
			}

			writer.WriteEndMap();
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
		ReadOnlyMemory<SerializableProperty<T>> propertiesToSerialize;
		SerializableProperty<T>[]? borrowedArray = null;
		try
		{
			if (defaultValuesPolicy != SerializeDefaultValuesPolicy.Always && serializable.Properties.Length > 0)
			{
				borrowedArray = ArrayPool<SerializableProperty<T>>.Shared.Rent(serializable.Properties.Length);
				propertiesToSerialize = this.GetPropertiesToSerialize(value, borrowedArray.AsMemory());
			}
			else
			{
				propertiesToSerialize = serializable.Properties;
			}

			Writer syncWriter = writer.CreateWriter();
			syncWriter.WriteStartMap(propertiesToSerialize.Length);
			for (int i = 0; i < propertiesToSerialize.Length; i++)
			{
				if (i > 0)
				{
					syncWriter.WriteMapPairSeparator();
				}

				SerializableProperty<T> property = propertiesToSerialize.Span[i];

				syncWriter.Buffer.Write(property.RawPropertyNameString.Span);
				syncWriter.WriteMapKeyValueSeparator();

				if (property.PreferAsyncSerialization)
				{
					writer.ReturnWriter(ref syncWriter);
					await property.WriteAsync(value, writer, context).ConfigureAwait(false);
					syncWriter = writer.CreateWriter();
				}
				else
				{
					property.Write(value, ref syncWriter, context);
				}

				if (writer.IsTimeToFlush(context, syncWriter))
				{
					writer.ReturnWriter(ref syncWriter);
					await writer.FlushIfAppropriateAsync(context).ConfigureAwait(false);
					syncWriter = writer.CreateWriter();
				}
			}

			syncWriter.WriteEndMap();
			writer.ReturnWriter(ref syncWriter);
		}
		finally
		{
			if (borrowedArray is not null)
			{
				ArrayPool<SerializableProperty<T>>.Shared.Return(borrowedArray);
			}
		}
	}

	/// <inheritdoc/>
	public override T? Read(ref Reader reader, SerializationContext context)
	{
		var deformatter = (MessagePackDeformatter)reader.Deformatter;
		if (deformatter.TryReadNull(ref reader))
		{
			return default;
		}

		if (constructor is null || deserializable is null)
		{
			throw new NotSupportedException($"The {typeof(T).Name} type cannot be deserialized.");
		}

		context.DepthStep();
		T value = constructor();

		if (deserializable.Value.Readers is not null)
		{
			int? count = deformatter.ReadStartMap(ref reader);
			bool isFirstElement = true;
			for (int i = 0; i < count /*|| (count is null && reader.TryAdvanceToNextElement(ref isFirstElement))*/; i++)
			{
				bool found = this.TryLookupProperty(ref reader, context, out DeserializableProperty<T> propertyReader);
				//reader.ReadMapKeyValueSeparator();

				if (found)
				{
					propertyReader.Read(ref value, ref reader, context);
				}
				else
				{
					deformatter.Skip(ref reader, context);
				}
			}
		}
		else
		{
			// We have nothing to read into, so just skip any data in the object.
			deformatter.Skip(ref reader, context);
		}

		if (value is ISerializationCallbacks callbacks)
		{
			callbacks.OnAfterDeserialize();
		}

		return value;
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

		if (constructor is null || deserializable is null)
		{
			throw new NotSupportedException($"The {typeof(T).Name} type cannot be deserialized.");
		}

		context.DepthStep();
		T value = constructor();

		if (deserializable.Value.Readers is not null)
		{
			int? mapEntries;
			while (streamingReader.TryReadMapHeader(out mapEntries).NeedsMoreBytes())
			{
				streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
			}

			// We're going to read in bursts. Anything we happen to get in one buffer, we'll ready synchronously regardless of whether the property is async.
			// But when we run out of buffer, if the next thing to read is async, we'll read it async.
			reader.ReturnReader(ref streamingReader);
			int? remainingEntries = mapEntries;
			bool isFirstElement = true;
			while (remainingEntries > 0 || (remainingEntries is null && await reader.TryAdvanceToNextElementAsync(isFirstElement).ConfigureAwait(false)))
			{
				isFirstElement = false;
				int bufferedStructures = await reader.BufferNextStructuresAsync(1, (remainingEntries ?? 1) * 2, context).ConfigureAwait(false);
				Reader syncReader = reader.CreateBufferedReader();
				int bufferedEntries = bufferedStructures / 2;
				for (int i = 0; i < bufferedEntries; i++)
				{
					if (this.TryLookupProperty(ref syncReader, context, out DeserializableProperty<T> propertyReader))
					{
						propertyReader.Read(ref value, ref syncReader, context);
					}
					else
					{
						syncReader.Skip(context);
					}

					remainingEntries--;
				}

				if (remainingEntries > 0)
				{
					// To know whether the next property is async, we need to know its name.
					// If its name isn't in the buffer, we'll just loop around and get it in the next buffer.
					if (bufferedStructures % 2 == 1)
					{
						// The property name has already been buffered.
						if (this.TryLookupProperty(ref syncReader, context, out DeserializableProperty<T> propertyReader))
						{
							if (propertyReader.PreferAsyncSerialization)
							{
								// The next property value is async, so turn in our sync reader and read it asynchronously.
								reader.ReturnReader(ref syncReader);
								value = await propertyReader.ReadAsync(value, reader, context).ConfigureAwait(false);
								remainingEntries--;
								continue;
							}
							else
							{
								// Deserialize the value synchronously.
								reader.ReturnReader(ref syncReader);
								await reader.BufferNextStructuresAsync(1, 1, context).ConfigureAwait(false);
								syncReader = reader.CreateBufferedReader();
								propertyReader.Read(ref value, ref syncReader, context);
								reader.ReturnReader(ref syncReader);
								remainingEntries--;
								continue;
							}
						}
						else
						{
							// We don't recognize the property name, so skip the value.
							reader.ReturnReader(ref syncReader);

							streamingReader = reader.CreateStreamingReader();
							while (streamingReader.TrySkip(ref context).NeedsMoreBytes())
							{
								streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
							}

							reader.ReturnReader(ref streamingReader);

							remainingEntries--;
							continue;
						}
					}
				}

				reader.ReturnReader(ref syncReader);
			}
		}
		else
		{
			// We have nothing to read into, so just skip any data in the object.
			while (streamingReader.TrySkip(ref context).NeedsMoreBytes())
			{
				streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
			}

			reader.ReturnReader(ref streamingReader);
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
			["type"] = "object",
		};

		if (objectShape.Properties.Count > 0)
		{
			Dictionary<string, IConstructorParameterShape>? ctorParams = ObjectConverterBase<T>.CreatePropertyAndParameterDictionary(objectShape);

			JsonObject properties = new();
			JsonArray? required = null;
			for (int i = 0; i < serializable.Properties.Length; i++)
			{
				SerializableProperty<T> property = serializable.Properties.Span[i];

				IConstructorParameterShape? associatedParameter = null;
				ctorParams?.TryGetValue(property.Name, out associatedParameter);

				JsonObject propertySchema = context.GetJsonSchema(property.Shape.PropertyType);
				ObjectConverterBase<T>.ApplyDescription(property.Shape.AttributeProvider, propertySchema);
				ObjectConverterBase<T>.ApplyDefaultValue(property.Shape.AttributeProvider, propertySchema, associatedParameter);

				if (!ObjectConverterBase<T>.IsNonNullable(property.Shape, associatedParameter))
				{
					propertySchema = ApplyJsonSchemaNullability(propertySchema);
				}

				properties.Add(property.Name, propertySchema);

				if (associatedParameter?.IsRequired is true)
				{
					(required ??= []).Add((JsonNode)property.Name);
				}
			}

			schema["properties"] = properties;

			// Only describe the properties as required if we guarantee that we'll write them.
			if ((defaultValuesPolicy & SerializeDefaultValuesPolicy.Required) == SerializeDefaultValuesPolicy.Required)
			{
				if (required is not null)
				{
					schema["required"] = required;
				}
			}
		}

		ObjectConverterBase<T>.ApplyDescription(objectShape.AttributeProvider, schema);

		return schema;
	}

	/// <inheritdoc/>
	[Experimental("NBMsgPackAsync")]
	public override async ValueTask<bool> SkipToPropertyValueAsync(AsyncReader reader, IPropertyShape propertyShape, SerializationContext context)
	{
		StreamingReader streamingReader = reader.CreateStreamingReader();
		bool isNull;
		while (streamingReader.TryReadNull(out isNull).NeedsMoreBytes())
		{
			streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
		}

		if (isNull)
		{
			reader.ReturnReader(ref streamingReader);
			return false;
		}

		context.DepthStep();

		int? mapEntries;
		while (streamingReader.TryReadMapHeader(out mapEntries).NeedsMoreBytes())
		{
			streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
		}

		if (mapEntries is null)
		{
			throw new NotImplementedException(); // TODO: review this for indefinite arrays.
		}

		for (int i = 0; i < mapEntries; i++)
		{
			ReadOnlySpan<byte> propertyName;
			bool contiguous;
			while (streamingReader.TryReadStringSpan(out contiguous, out propertyName).NeedsMoreBytes())
			{
				streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
			}

			bool match;
			if (contiguous)
			{
				match = this.TryMatchPropertyName(propertyName, propertyShape.Name);
			}
			else
			{
				StreamingReader peekReader = streamingReader;
				if (peekReader.TryReadStringSpan(out _, out _).NeedsMoreBytes())
				{
					reader.ReturnReader(ref streamingReader);
					await reader.BufferNextStructureAsync(context).ConfigureAwait(false);
					streamingReader = reader.CreateStreamingReader();
				}

				match = Helper(ref streamingReader);
			}

			if (match)
			{
				// Return before reading the value.
				reader.ReturnReader(ref streamingReader);
				return true;
			}
			else
			{
				// Skip over the value.
				while (streamingReader.TrySkip(ref context).NeedsMoreBytes())
				{
					streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
				}
			}
		}

		reader.ReturnReader(ref streamingReader);
		return false;

		bool Helper(ref StreamingReader streamingReader)
		{
			Assumes.False(streamingReader.TryGetMaxStringLength(out _, out int maxBytes).NeedsMoreBytes());
			byte[]? array = null;
			try
			{
				Span<byte> buffer = maxBytes > MaxStackStringCharLength ? array = ArrayPool<byte>.Shared.Rent(maxBytes) : stackalloc byte[maxBytes];
				Assumes.False(streamingReader.TryReadString(buffer, out int bytesWritten).NeedsMoreBytes());
				return this.TryMatchPropertyName(buffer[..bytesWritten], propertyShape.Name);
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

	/// <summary>
	/// Looks up the property deserializer given the property name at the current reader position.
	/// </summary>
	/// <param name="reader">The reader, positioned at the property name.</param>
	/// <param name="context">The serialization context.</param>
	/// <param name="propertyReader">Receives the property deserializer.</param>
	/// <returns>A value indicating whether a matching property deserializer was found.</returns>
	/// <remarks>
	/// Implementations of this method <em>must</em> advance the reader beyond the property name in every case.
	/// </remarks>
	protected virtual bool TryLookupProperty(ref Reader reader, SerializationContext context, out DeserializableProperty<T> propertyReader)
	{
		if (deserializable?.Readers is not null)
		{
			byte[]? array = null;
			reader.GetMaxStringLength(out _, out int maxBytes);
			try
			{
				Span<byte> span = maxBytes > MaxStackStringCharLength ? array = ArrayPool<byte>.Shared.Rent(maxBytes) : stackalloc byte[maxBytes];
				int bytesCount = reader.ReadString(span);
				return deserializable.Value.Readers.TryGetValue(span[..bytesCount], out propertyReader);
			}
			finally
			{
				if (array is not null)
				{
					ArrayPool<byte>.Shared.Return(array);
				}
			}
		}
		else
		{
			reader.Skip(context);
			propertyReader = default;
			return false;
		}
	}

	/// <summary>
	/// Searches for a property with a given UTF-8 encoded name, and checks to see if it matches the expected name expressed as a string.
	/// </summary>
	/// <param name="propertyName">The UTF-8 encoded string.</param>
	/// <param name="expectedName">The string.</param>
	/// <returns><see langword="true" /> iff the two arguments are equal to each other, and match a known property on the object.</returns>
	/// <remarks>
	/// This is a glorified way of avoiding the costs of encoding/decoding.
	/// Whether several dictionary lookups is faster than encoding/decoding is an open question.
	/// </remarks>
	private protected virtual bool TryMatchPropertyName(ReadOnlySpan<byte> propertyName, string expectedName)
	{
		return deserializable?.Readers?.TryGetValue(propertyName, out DeserializableProperty<T> propertyReader) is true && propertyReader.Name == expectedName;
	}

	private Memory<SerializableProperty<T>> GetPropertiesToSerialize(in T value, Memory<SerializableProperty<T>> include)
	{
		return include[..this.GetPropertiesToSerialize(value, include.Span)];
	}

	private int GetPropertiesToSerialize(in T value, Span<SerializableProperty<T>> include)
	{
		int propertyCount = 0;
		foreach (SerializableProperty<T> property in serializable.Properties.Span)
		{
			if (property.ShouldSerialize?.Invoke(value) is not false)
			{
				include[propertyCount++] = property;
			}
		}

		return propertyCount;
	}
}
