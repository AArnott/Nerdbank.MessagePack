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
internal class ObjectArrayWithNonDefaultCtorConverter<TDeclaringType, TArgumentState>(
	PropertyAccessors<TDeclaringType>?[] properties,
	Func<TArgumentState> argStateCtor,
	Constructor<TArgumentState, TDeclaringType> ctor,
	DeserializableProperty<TArgumentState>?[] parameters) : ObjectArrayConverter<TDeclaringType>(properties, null)
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

		return ctor(ref argState);
	}

	/// <inheritdoc/>
	[Experimental("NBMsgPackAsync")]
	public override async ValueTask<TDeclaringType?> ReadAsync(MessagePackAsyncReader reader, SerializationContext context, CancellationToken cancellationToken)
	{
		if (await reader.TryReadNilAsync(cancellationToken).ConfigureAwait(false))
		{
			return default;
		}

		context.DepthStep();
		TArgumentState argState = argStateCtor();

		int arrayLength = await reader.ReadArrayHeaderAsync(cancellationToken).ConfigureAwait(false);
		int i = 0;
		while (i < arrayLength)
		{
			// Do a batch of all the consecutive properties that should be read synchronously.
			int syncBatchSize = NextSyncReadBatchSize();
			if (syncBatchSize > 0)
			{
				ReadOnlySequence<byte> buffer = await reader.ReadNextStructuresAsync(syncBatchSize, context, cancellationToken).ConfigureAwait(false);
				MessagePackReader syncReader = new(buffer);
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

				reader.AdvanceTo(syncReader.Position);
			}

			// Read any consecutive async parameters.
			for (; i < arrayLength && parameters.Length > i; i++)
			{
				if (parameters[i] is not DeserializableProperty<TArgumentState> { PreferAsyncSerialization: true, ReadAsync: { } deserializeAsync })
				{
					break;
				}

				argState = await deserializeAsync(argState, reader, context, cancellationToken).ConfigureAwait(false);
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

		return ctor(ref argState);
	}
}
