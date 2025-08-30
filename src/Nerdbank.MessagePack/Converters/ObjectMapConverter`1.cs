// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// A <see cref="MessagePackConverter{T}"/> that writes objects as maps of property names to values.
/// Only data types with default constructors may be deserialized.
/// </summary>
/// <typeparam name="T">The type of objects that can be serialized or deserialized with this converter.</typeparam>
/// <param name="serializable">Tools for serializing individual property values.</param>
/// <param name="deserializable">Tools for deserializing individual property values. May be omitted if the type will never be deserialized (i.e. there is no deserializing constructor).</param>
/// <param name="unusedDataProperty">The special <see cref="UnusedDataPacket"/> property, if declared.</param>
/// <param name="constructor">The default constructor, if present.</param>
/// <param name="propertyShapes">The property shapes.</param>
/// <param name="defaultValuesPolicy">The policy for whether to serialize properties. When not <see cref="SerializeDefaultValuesPolicy.Always"/>, the <see cref="SerializableProperty{TDeclaringType}.ShouldSerialize"/> property will be consulted prior to serialization.</param>
internal class ObjectMapConverter<T>(
	MapSerializableProperties<T> serializable,
	MapDeserializableProperties<T>? deserializable,
	DirectPropertyAccess<T, UnusedDataPacket>? unusedDataProperty,
	Func<T>? constructor,
	IReadOnlyList<IPropertyShape> propertyShapes,
	SerializeDefaultValuesPolicy defaultValuesPolicy) : ObjectConverterBase<T>
{
	/// <inheritdoc/>
	public override bool PreferAsyncSerialization => true;

	/// <summary>
	/// Gets the special <see cref="UnusedDataPacket"/> property, if declared.
	/// </summary>
	protected DirectPropertyAccess<T, UnusedDataPacket>? UnusedDataProperty => unusedDataProperty;

	/// <inheritdoc/>
#pragma warning disable NBMsgPack031 // Exactly one structure - this method is super complicated and beyond the analyzer
	public override void Write(ref MessagePackWriter writer, in T? value, SerializationContext context)
#pragma warning restore NBMsgPack031
	{
		try
		{
			if (value is null)
			{
				writer.WriteNil();
				return;
			}

			IMessagePackSerializationCallbacks? callbacks = value as IMessagePackSerializationCallbacks;
			callbacks?.OnBeforeSerialize();

			context.DepthStep();

			if (defaultValuesPolicy != SerializeDefaultValuesPolicy.Always && serializable.Properties.Length > 0)
			{
				SerializableProperty<T>[] include = ArrayPool<SerializableProperty<T>>.Shared.Rent(serializable.Properties.Length);
				try
				{
					WriteProperties(ref writer, value, this.GetPropertiesToSerialize(value, include.AsMemory()).Span, unusedDataProperty, context);
				}
				finally
				{
					ArrayPool<SerializableProperty<T>>.Shared.Return(include);
				}
			}
			else
			{
				WriteProperties(ref writer, value, serializable.Properties.Span, unusedDataProperty, context);
			}

			callbacks?.OnAfterSerialize();
		}
		catch (Exception ex) when (ShouldWrapSerializationException(ex, context.CancellationToken))
		{
			throw new MessagePackSerializationException(CreateTypeErrorMessage("serializing", typeof(T)), ex);
		}

		static void WriteProperties(ref MessagePackWriter writer, in T value, ReadOnlySpan<SerializableProperty<T>> properties, DirectPropertyAccess<T, UnusedDataPacket>? unusedDataProperty, SerializationContext context)
		{
			UnusedDataPacket.Map? unused = unusedDataProperty?.Getter?.Invoke(ref Unsafe.AsRef(in value)) as UnusedDataPacket.Map;
			writer.WriteMapHeader(properties.Length + (unused?.Count ?? 0));
			foreach (SerializableProperty<T> property in properties)
			{
				writer.WriteRaw(property.RawPropertyNameString.Span);
				property.Write(value, ref writer, context);
			}

			unused?.WriteTo(ref writer);
		}
	}

	/// <inheritdoc/>
	public override async ValueTask WriteAsync(MessagePackAsyncWriter writer, T? value, SerializationContext context)
	{
		try
		{
			if (value is null)
			{
				writer.WriteNil();
				return;
			}

			IMessagePackSerializationCallbacks? callbacks = value as IMessagePackSerializationCallbacks;
			callbacks?.OnBeforeSerialize();

			context.DepthStep();
			UnusedDataPacket.Map? unused = unusedDataProperty?.Getter?.Invoke(ref Unsafe.AsRef(in value)) as UnusedDataPacket.Map;
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

				MessagePackWriter syncWriter = writer.CreateWriter();
				syncWriter.WriteMapHeader(propertiesToSerialize.Length + (unused?.Count ?? 0));
				for (int i = 0; i < propertiesToSerialize.Length; i++)
				{
					SerializableProperty<T> property = propertiesToSerialize.Span[i];

					syncWriter.WriteRaw(property.RawPropertyNameString.Span);
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

				if (unused is not null)
				{
					unused?.WriteTo(ref syncWriter);
					writer.ReturnWriter(ref syncWriter);
					await writer.FlushIfAppropriateAsync(context).ConfigureAwait(false);
				}
				else
				{
					writer.ReturnWriter(ref syncWriter);
				}
			}
			finally
			{
				if (borrowedArray is not null)
				{
					ArrayPool<SerializableProperty<T>>.Shared.Return(borrowedArray);
				}
			}

			callbacks?.OnAfterSerialize();
		}
		catch (Exception ex) when (ShouldWrapSerializationException(ex, context.CancellationToken))
		{
			throw new MessagePackSerializationException(CreateTypeErrorMessage("serializing", typeof(T)), ex);
		}
	}

	/// <inheritdoc/>
	public override T? Read(ref MessagePackReader reader, SerializationContext context)
	{
		try
		{
			if (reader.TryReadNil())
			{
				return default;
			}

			if (constructor is null || deserializable is null)
			{
				throw new NotSupportedException($"The {typeof(T).FullName} type cannot be deserialized.");
			}

			context.DepthStep();
			T value = constructor();
			IMessagePackSerializationCallbacks? callbacks = value as IMessagePackSerializationCallbacks;
			callbacks?.OnBeforeDeserialize();
			PropertyCollisionDetection collisionDetection = new(propertyShapes);
			UnusedDataPacket.Map? unused = null;

			if (!typeof(T).IsValueType)
			{
				context.ReportObjectConstructed(value);
			}

			if (deserializable.Readers is not null)
			{
				int count = reader.ReadMapHeader();
				for (int i = 0; i < count; i++)
				{
					ReadOnlySpan<byte> propertyName = StringEncoding.ReadStringSpan(ref reader);
					if (deserializable.Readers.TryGetValue(propertyName, out DeserializableProperty<T>? propertyReader))
					{
						collisionDetection.MarkAsRead(propertyReader.ShapePosition);
						propertyReader.Read(ref value, ref reader, context);
					}
					else if (unusedDataProperty?.Setter is not null)
					{
						unused ??= new();
						unused.Add(propertyName, reader.ReadRaw(context));
					}
					else
					{
						reader.Skip(context);
					}
				}
			}
			else
			{
				// We have nothing to read into, so just skip any data in the object.
				reader.Skip(context);
			}

			if (unused is not null && value is not null && unusedDataProperty?.Setter is not null)
			{
				unusedDataProperty.Setter(ref value, unused);
			}

			callbacks?.OnAfterDeserialize();

			return value;
		}
		catch (Exception ex) when (ShouldWrapSerializationException(ex, context.CancellationToken))
		{
			throw new MessagePackSerializationException(CreateTypeErrorMessage("deserializing", typeof(T)), ex);
		}
	}

	/// <inheritdoc/>
	public override async ValueTask<T?> ReadAsync(MessagePackAsyncReader reader, SerializationContext context)
	{
		try
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

			if (constructor is null || deserializable is null)
			{
				throw new NotSupportedException($"The {typeof(T).FullName} type cannot be deserialized.");
			}

			context.DepthStep();
			T value = constructor();
			IMessagePackSerializationCallbacks? callbacks = value as IMessagePackSerializationCallbacks;
			callbacks?.OnBeforeDeserialize();
			PropertyCollisionDetection collisionDetection = new(propertyShapes);
			UnusedDataPacket.Map? unused = null;

			if (!typeof(T).IsValueType)
			{
				context.ReportObjectConstructed(value);
			}

			if (deserializable.Readers is not null)
			{
				int mapEntries;
				while (streamingReader.TryReadMapHeader(out mapEntries).NeedsMoreBytes())
				{
					streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
				}

				// We're going to read in bursts. Anything we happen to get in one buffer, we'll ready synchronously regardless of whether the property is async.
				// But when we run out of buffer, if the next thing to read is async, we'll read it async.
				reader.ReturnReader(ref streamingReader);
				int remainingEntries = mapEntries;
				while (remainingEntries > 0)
				{
					int bufferedStructures = await reader.BufferNextStructuresAsync(1, remainingEntries * 2, context).ConfigureAwait(false);
					MessagePackReader syncReader = reader.CreateBufferedReader();
					int bufferedEntries = bufferedStructures / 2;
					for (int i = 0; i < bufferedEntries; i++)
					{
						ReadOnlySpan<byte> propertyName = StringEncoding.ReadStringSpan(ref syncReader);
						if (deserializable.Readers.TryGetValue(propertyName, out DeserializableProperty<T>? propertyReader))
						{
							collisionDetection.MarkAsRead(propertyReader.ShapePosition);
							propertyReader.Read(ref value, ref syncReader, context);
						}
						else if (unusedDataProperty?.Setter is not null)
						{
							unused ??= new();
							unused.Add(propertyName, syncReader.ReadRaw(context));
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
							ReadOnlySpan<byte> propertyName = StringEncoding.ReadStringSpan(ref syncReader);
							if (deserializable.Readers.TryGetValue(propertyName, out DeserializableProperty<T>? propertyReader))
							{
								collisionDetection.MarkAsRead(propertyReader.ShapePosition);
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

								if (unusedDataProperty?.Setter is not null)
								{
									unused ??= new();
									RawMessagePack msgpack;
									ReadOnlyMemory<byte> propertyNameMemory = UnusedDataPacket.Map.GetPropertyNameMemory(propertyName);
									while (streamingReader.TryReadRaw(ref context, out msgpack).NeedsMoreBytes())
									{
										streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
									}

									unused.Add(propertyNameMemory, msgpack);
								}
								else
								{
									while (streamingReader.TrySkip(ref context).NeedsMoreBytes())
									{
										streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
									}
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

			if (unused is not null && value is not null && unusedDataProperty?.Setter is not null)
			{
				unusedDataProperty.Setter(ref value, unused);
			}

			callbacks?.OnAfterDeserialize();

			return value;
		}
		catch (Exception ex) when (ShouldWrapSerializationException(ex, context.CancellationToken))
		{
			throw new MessagePackSerializationException(CreateTypeErrorMessage("deserializing", typeof(T)), ex);
		}
	}

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
	{
		IObjectTypeShape<T> objectShape = (IObjectTypeShape<T>)typeShape;

		JsonObject schema = new()
		{
			["type"] = "object",
		};

		if (objectShape.Properties.Count > 0)
		{
			Dictionary<string, IParameterShape>? ctorParams = CreatePropertyAndParameterDictionary(objectShape);

			JsonObject properties = new();
			JsonArray? required = null;
			for (int i = 0; i < serializable.Properties.Length; i++)
			{
				SerializableProperty<T> property = serializable.Properties.Span[i];

				IParameterShape? associatedParameter = null;
				ctorParams?.TryGetValue(property.Name, out associatedParameter);

				JsonObject propertySchema = context.GetJsonSchema(property.Shape.PropertyType);
				ApplyDescription(property.Shape.AttributeProvider, propertySchema);
				ApplyDefaultValue(property.Shape.AttributeProvider, propertySchema, associatedParameter);

				if (!IsNonNullable(property.Shape, associatedParameter))
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

		ApplyDescription(objectShape.AttributeProvider, schema);

		return schema;
	}

	/// <inheritdoc/>
	public override async ValueTask<bool> SkipToPropertyValueAsync(MessagePackAsyncReader reader, IPropertyShape propertyShape, SerializationContext context)
	{
		MessagePackStreamingReader streamingReader = reader.CreateStreamingReader();
		bool isNil;
		while (streamingReader.TryReadNil(out isNil).NeedsMoreBytes())
		{
			streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
		}

		if (isNil)
		{
			reader.ReturnReader(ref streamingReader);
			return false;
		}

		context.DepthStep();

		int mapEntries;
		while (streamingReader.TryReadMapHeader(out mapEntries).NeedsMoreBytes())
		{
			streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
		}

		for (int i = 0; i < mapEntries; i++)
		{
			ReadOnlySpan<byte> propertyName;
			bool contiguous;
			while (streamingReader.TryReadStringSpan(out contiguous, out propertyName).NeedsMoreBytes())
			{
				streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
			}

			if (!contiguous)
			{
				ReadOnlySequence<byte> propertyNameSequence;
				while (streamingReader.TryReadStringSequence(out propertyNameSequence).NeedsMoreBytes())
				{
					streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
				}

				propertyName = propertyNameSequence.ToArray();
			}

			if (this.TryMatchPropertyName(propertyName, propertyShape.Name))
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
		return deserializable?.Readers?.TryGetValue(propertyName, out DeserializableProperty<T>? propertyReader) is true && propertyReader.Name == expectedName;
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
