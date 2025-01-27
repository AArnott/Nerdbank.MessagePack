// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// A <see cref="MessagePackConverter{T}"/> that writes objects as maps of property names to values.
/// Data types with constructors and/or <see langword="init" /> properties may be deserialized.
/// </summary>
/// <typeparam name="TDeclaringType">The type of objects that can be serialized or deserialized with this converter.</typeparam>
/// <typeparam name="TArgumentState">The state object that stores individual member values until the constructor delegate can be invoked.</typeparam>
/// <param name="properties">Property accessors, in array positions matching serialization indexes.</param>
/// <param name="argStateCtor">The constructor for the <typeparamref name="TArgumentState"/> that is later passed to the <typeparamref name="TDeclaringType"/> constructor.</param>
/// <param name="ctor">The data type's constructor helper.</param>
/// <param name="parameters">Constructor parameter initializers, in array positions matching serialization indexes.</param>
/// <param name="callShouldSerialize"><inheritdoc cref="ObjectArrayConverter{T}.ObjectArrayConverter(ReadOnlyMemory{PropertyAccessors{T}?}, Func{T}?, bool)" path="/param[@name='callShouldSerialize']"/></param>
internal class ObjectArrayWithNonDefaultCtorConverter<TDeclaringType, TArgumentState>(
	PropertyAccessors<TDeclaringType>?[] properties,
	Func<TArgumentState> argStateCtor,
	Constructor<TArgumentState, TDeclaringType> ctor,
	DeserializableProperty<TArgumentState>?[] parameters,
	bool callShouldSerialize) : ObjectArrayConverter<TDeclaringType>(properties, null, callShouldSerialize)
{
	/// <inheritdoc/>
	public override void Read(ref MessagePackReader reader, ref TDeclaringType? value, SerializationContext context)
	{
		if (reader.TryReadNil())
		{
			value = default;
			return;
		}

		context.DepthStep();
		TArgumentState argState = argStateCtor();

		if (reader.NextMessagePackType == MessagePackType.Map)
		{
			// The indexes we have are the keys in the map rather than indexes into the array.
			int count = reader.ReadMapHeader();
			for (int i = 0; i < count; i++)
			{
				int index = reader.ReadInt32();
				if (properties.Length > index && parameters[index] is { } deserialize)
				{
					deserialize.Read(ref argState, ref reader, context);
				}
				else
				{
					reader.Skip(context);
				}
			}
		}
		else
		{
			int count = reader.ReadArrayHeader();
			for (int i = 0; i < count; i++)
			{
				if (parameters.Length > i && parameters[i] is { } deserialize)
				{
					deserialize.Read(ref argState, ref reader, context);
				}
				else
				{
					reader.Skip(context);
				}
			}
		}

		value = ctor(ref argState);

		if (value is IMessagePackSerializationCallbacks callbacks)
		{
			callbacks.OnAfterDeserialize();
		}
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

		MessagePackType peekType;
		while (streamingReader.TryPeekNextMessagePackType(out peekType).NeedsMoreBytes())
		{
			streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
		}

		if (peekType == MessagePackType.Map)
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
					int propertyIndex = syncReader.ReadInt32();
					if (propertyIndex < parameters.Length && parameters[propertyIndex] is { Read: { } deserialize })
					{
						deserialize(ref argState, ref syncReader, context);
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
						if (propertyIndex < parameters.Length && parameters[propertyIndex] is { PreferAsyncSerialization: true, ReadAsync: { } deserializeAsync })
						{
							// The next property value is async, so turn in our sync reader and read it asynchronously.
							reader.ReturnReader(ref syncReader);
							argState = await deserializeAsync(argState, reader, context).ConfigureAwait(false);
							remainingEntries--;

							// Now loop around to see what else we can do with the next buffer.
							continue;
						}
					}
					else
					{
						// The property name isn't in the buffer, and thus whether it'll have an async reader.
						// Advance the reader so it knows we need more buffer than we got last time.
						reader.ReturnReader(ref syncReader);
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
					MessagePackReader syncReader = reader.CreateBufferedReader();
					for (int syncReadEndExclusive = i + syncBatchSize; i < syncReadEndExclusive; i++)
					{
						if (parameters.Length > i && parameters[i] is { Read: { } deserialize })
						{
							deserialize(ref argState, ref syncReader, context);
						}
						else
						{
							syncReader.Skip(context);
						}
					}

					reader.ReturnReader(ref syncReader);
				}

				// Read any consecutive async parameters.
				for (; i < arrayLength && parameters.Length > i; i++)
				{
					if (parameters[i] is not DeserializableProperty<TArgumentState> { PreferAsyncSerialization: true, ReadAsync: { } deserializeAsync })
					{
						break;
					}

					argState = await deserializeAsync(argState, reader, context).ConfigureAwait(false);
				}

				int NextSyncReadBatchSize()
				{
					// We want to count the number of array elements need to be read up to the next async property.
					for (int j = i; j < arrayLength; j++)
					{
						if (parameters.Length > j)
						{
							DeserializableProperty<TArgumentState>? property = parameters[j];
							if (property is { PreferAsyncSerialization: true })
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

		TDeclaringType value = ctor(ref argState);

		if (value is IMessagePackSerializationCallbacks callbacks)
		{
			callbacks.OnAfterDeserialize();
		}

		return value;
	}
}
