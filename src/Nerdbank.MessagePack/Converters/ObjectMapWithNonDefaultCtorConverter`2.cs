// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// A <see cref="MessagePackConverter{T}"/> that writes objects as maps of property names to values.
/// Data types with constructors and/or <see langword="init" /> properties may be deserialized.
/// </summary>
/// <typeparam name="TDeclaringType">The type of objects that can be serialized or deserialized with this converter.</typeparam>
/// <typeparam name="TArgumentState">The state object that stores individual member values until the constructor delegate can be invoked.</typeparam>
/// <param name="serializable">Tools for serializing individual property values.</param>
/// <param name="unusedDataProperty">The special <see cref="UnusedDataPacket"/> property, if declared.</param>
/// <param name="argStateCtor">The constructor for the <typeparamref name="TArgumentState"/> that is later passed to the <typeparamref name="TDeclaringType"/> constructor.</param>
/// <param name="ctor">The data type's constructor helper.</param>
/// <param name="parameters">Tools for deserializing individual property values.</param>
/// <param name="parameterShapes">The parameter shapes.</param>
/// <param name="serializeDefaultValuesPolicy"><inheritdoc cref="ObjectMapConverter{T}.ObjectMapConverter" path="/param[@name='defaultValuesPolicy']"/></param>
/// <param name="deserializeDefaultValuesPolicy">The policy to apply when deserializing properties.</param>
internal class ObjectMapWithNonDefaultCtorConverter<TDeclaringType, TArgumentState>(
	MapSerializableProperties<TDeclaringType> serializable,
	Func<TArgumentState> argStateCtor,
	DirectPropertyAccess<TDeclaringType, UnusedDataPacket>? unusedDataProperty,
	Constructor<TArgumentState, TDeclaringType> ctor,
	MapDeserializableProperties<TArgumentState> parameters,
	IReadOnlyList<IParameterShape> parameterShapes,
	SerializeDefaultValuesPolicy serializeDefaultValuesPolicy,
	DeserializeDefaultValuesPolicy deserializeDefaultValuesPolicy) : ObjectMapConverter<TDeclaringType>(serializable, null, unusedDataProperty, null, [], serializeDefaultValuesPolicy)
	where TArgumentState : IArgumentState
{
	/// <inheritdoc/>
	public override TDeclaringType? Read(ref MessagePackReader reader, SerializationContext context)
	{
		if (reader.TryReadNil())
		{
			return default;
		}

		context.DepthStep();
		TArgumentState argState = argStateCtor();
		UnusedDataPacket.Map? unused = null;

		if (parameters.Readers is not null)
		{
			int count = reader.ReadMapHeader();
			for (int i = 0; i < count; i++)
			{
				ReadOnlySpan<byte> propertyName = StringEncoding.ReadStringSpan(ref reader);
				if (parameters.Readers.TryGetValue(propertyName, out DeserializableProperty<TArgumentState>? propertyReader))
				{
					propertyReader.Read(ref argState, ref reader, context);
				}
				else if (this.UnusedDataProperty?.Setter is not null)
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

		ThrowIfMissingRequiredProperties(argState, parameterShapes, deserializeDefaultValuesPolicy);
		TDeclaringType value = ctor(ref argState);

		if (unused is not null && value is not null && this.UnusedDataProperty?.Setter is not null)
		{
			this.UnusedDataProperty.Setter(ref value, unused);
		}

		if (value is IMessagePackSerializationCallbacks callbacks)
		{
			callbacks.OnAfterDeserialize();
		}

		return value;
	}

	/// <inheritdoc/>
	public override async ValueTask<TDeclaringType?> ReadAsync(MessagePackAsyncReader reader, SerializationContext context)
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

		context.DepthStep();
		TArgumentState argState = argStateCtor();
		UnusedDataPacket.Map? unused = null;

		if (parameters.Readers is not null)
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
				MessagePackReader syncReader = reader.CreateBufferedReader();
				int bufferedEntries = bufferedStructures / 2;
				for (int i = 0; i < bufferedEntries; i++)
				{
					ReadOnlySpan<byte> propertyName = StringEncoding.ReadStringSpan(ref syncReader);
					if (parameters.Readers.TryGetValue(propertyName, out DeserializableProperty<TArgumentState>? propertyReader))
					{
						propertyReader.Read(ref argState, ref syncReader, context);
					}
					else if (this.UnusedDataProperty?.Setter is not null)
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
						if (parameters.Readers.TryGetValue(propertyName, out DeserializableProperty<TArgumentState>? propertyReader))
						{
							if (propertyReader.PreferAsyncSerialization)
							{
								// The next property value is async, so turn in our sync reader and read it asynchronously.
								reader.ReturnReader(ref syncReader);
								argState = await propertyReader.ReadAsync(argState, reader, context).ConfigureAwait(false);
								remainingEntries--;
								continue;
							}
							else
							{
								// Deserialize the value synchronously.
								reader.ReturnReader(ref syncReader);
								await reader.BufferNextStructuresAsync(1, 1, context).ConfigureAwait(false);
								syncReader = reader.CreateBufferedReader();
								propertyReader.Read(ref argState, ref syncReader, context);
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
							if (this.UnusedDataProperty?.Setter is not null)
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

		ThrowIfMissingRequiredProperties(argState, parameterShapes, deserializeDefaultValuesPolicy);
		TDeclaringType value = ctor(ref argState);

		if (unused is not null && value is not null && this.UnusedDataProperty?.Setter is not null)
		{
			this.UnusedDataProperty.Setter(ref value, unused);
		}

		if (value is IMessagePackSerializationCallbacks callbacks)
		{
			callbacks.OnAfterDeserialize();
		}

		return value;
	}

	/// <inheritdoc/>
	private protected override bool TryMatchPropertyName(ReadOnlySpan<byte> propertyName, string expectedName)
	{
		return parameters.Readers?.TryGetValue(propertyName, out DeserializableProperty<TArgumentState>? propertyReader) is true && propertyReader.Name == expectedName;
	}
}
