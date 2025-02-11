﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Nerdbank.PolySerializer.Converters;
using Nerdbank.PolySerializer.MessagePack;

namespace Nerdbank.PolySerializer.MessagePack.Converters;

/// <summary>
/// A <see cref="MessagePackConverter{T}"/> that writes objects as maps of property names to values.
/// Data types with constructors and/or <see langword="init" /> properties may be deserialized.
/// </summary>
/// <typeparam name="TDeclaringType">The type of objects that can be serialized or deserialized with this converter.</typeparam>
/// <typeparam name="TArgumentState">The state object that stores individual member values until the constructor delegate can be invoked.</typeparam>
/// <param name="serializable">Tools for serializing individual property values.</param>
/// <param name="argStateCtor">The constructor for the <typeparamref name="TArgumentState"/> that is later passed to the <typeparamref name="TDeclaringType"/> constructor.</param>
/// <param name="ctor">The data type's constructor helper.</param>
/// <param name="parameters">Tools for deserializing individual property values.</param>
/// <param name="defaultValuesPolicy"><inheritdoc cref="ObjectMapConverter{T}.ObjectMapConverter(MapSerializableProperties{T}, MapDeserializableProperties{T}?, Func{T}?, SerializeDefaultValuesPolicy)" path="/param[@name='defaultValuesPolicy']"/></param>
internal class ObjectMapWithNonDefaultCtorConverter<TDeclaringType, TArgumentState>(
	MapSerializableProperties<TDeclaringType> serializable,
	Func<TArgumentState> argStateCtor,
	Constructor<TArgumentState, TDeclaringType> ctor,
	MapDeserializableProperties<TArgumentState> parameters,
	SerializeDefaultValuesPolicy defaultValuesPolicy) : ObjectMapConverter<TDeclaringType>(serializable, null, null, defaultValuesPolicy)
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
		if (parameters.Readers is not null)
		{
			int count = reader.ReadMapHeader();
			for (int i = 0; i < count; i++)
			{
				ReadOnlySpan<byte> propertyName = StringEncoding.ReadStringSpan(ref reader);
				if (parameters.Readers.TryGetValue(propertyName, out DeserializableProperty<TArgumentState> deserializeArg))
				{
					Reader baseReader = reader.ToReader();
					deserializeArg.Read(ref argState, ref baseReader, context);
					reader = MessagePackReader.FromReader(baseReader);
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

		TDeclaringType value = ctor(ref argState);

		if (value is ISerializationCallbacks callbacks)
		{
			callbacks.OnAfterDeserialize();
		}

		return value;
	}

	/// <inheritdoc/>
	[Experimental("NBMsgPackAsync")]
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
					if (parameters.Readers.TryGetValue(propertyName, out DeserializableProperty<TArgumentState> propertyReader))
					{
						Reader baseReader = syncReader.ToReader();
						propertyReader.Read(ref argState, ref baseReader, context);
						syncReader = MessagePackReader.FromReader(baseReader);
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
						if (parameters.Readers.TryGetValue(propertyName, out DeserializableProperty<TArgumentState> propertyReader))
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
								Reader baseReader = syncReader.ToReader();
								propertyReader.Read(ref argState, ref baseReader, context);
								syncReader = MessagePackReader.FromReader(baseReader);
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

		TDeclaringType value = ctor(ref argState);

		if (value is ISerializationCallbacks callbacks)
		{
			callbacks.OnAfterDeserialize();
		}

		return value;
	}

	/// <inheritdoc/>
	private protected override bool TryMatchPropertyName(ReadOnlySpan<byte> propertyName, string expectedName)
	{
		return parameters.Readers?.TryGetValue(propertyName, out DeserializableProperty<TArgumentState> propertyReader) is true && propertyReader.Name == expectedName;
	}
}
