// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// A <see cref="MessagePackConverter{T}"/> that writes objects as arrays of property values.
/// Only data types with default constructors may be deserialized.
/// </summary>
/// <typeparam name="T">The type of objects that can be serialized or deserialized with this converter.</typeparam>
internal class ObjectArrayConverter<T>(PropertyAccessors<T>?[] properties, Func<T>? constructor) : MessagePackConverter<T>
{
	/// <inheritdoc/>
	public override bool PreferAsyncSerialization => true;

	/// <inheritdoc/>
	public override T? Read(ref MessagePackReader reader, SerializationContext context)
	{
		if (reader.TryReadNil())
		{
			return default;
		}

		if (constructor is null)
		{
			throw new NotSupportedException($"The {typeof(T).Name} type cannot be deserialized.");
		}

		context.DepthStep();
		T value = constructor();
		int count = reader.ReadArrayHeader();
		for (int i = 0; i < count; i++)
		{
			if (properties.Length > i && properties[i]?.MsgPackReaders is var (deserialize, _))
			{
				deserialize(ref value, ref reader, context);
			}
			else
			{
				reader.Skip(context);
			}
		}

		return value;
	}

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in T? value, SerializationContext context)
	{
		if (value is null)
		{
			writer.WriteNil();
			return;
		}

		context.DepthStep();

		writer.WriteArrayHeader(properties.Length);
		for (int i = 0; i < properties.Length; i++)
		{
			if (properties[i]?.MsgPackWriters is var (serialize, _))
			{
				serialize(value, ref writer, context);
			}
			else
			{
				writer.WriteNil();
			}
		}
	}

	/// <inheritdoc/>
	[Experimental("NBMsgPackAsync")]
	public override async ValueTask WriteAsync(MessagePackAsyncWriter writer, T? value, SerializationContext context, CancellationToken cancellationToken)
	{
		if (value is null)
		{
			writer.WriteNil();
			return;
		}

		context.DepthStep();

		writer.WriteArrayHeader(properties.Length);
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
				MessagePackWriter syncWriter = writer.CreateWriter();
				for (; i < syncWriteEndExclusive && !writer.IsTimeToFlush(context, syncWriter); i++)
				{
					if (properties[i] is { MsgPackWriters: var (serialize, _) })
					{
						serialize(value, ref syncWriter, context);
					}
					else
					{
						writer.WriteNil();
					}
				}

				syncWriter.Flush();
				await writer.FlushIfAppropriateAsync(context, cancellationToken).ConfigureAwait(false);
			}

			// Write all consecutive async properties.
			for (; i < properties.Length; i++)
			{
				if (properties[i] is not PropertyAccessors<T> { PreferAsyncSerialization: true, MsgPackWriters: var (_, serializeAsync) })
				{
					break;
				}

				await serializeAsync(value, writer, context, cancellationToken).ConfigureAwait(false);
			}

			int NextSyncBatchSize()
			{
				// We want to count the number of array elements need to be written up to the next async property.
				for (int j = i; j < properties.Length; j++)
				{
					if (properties.Length > j)
					{
						PropertyAccessors<T>? property = properties[j];
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

	/// <inheritdoc/>
	[Experimental("NBMsgPackAsync")]
	public override async ValueTask<T?> ReadAsync(MessagePackAsyncReader reader, SerializationContext context, CancellationToken cancellationToken)
	{
		if (await reader.TryReadNilAsync(cancellationToken).ConfigureAwait(false))
		{
			return default;
		}

		if (constructor is null)
		{
			throw new NotSupportedException($"The {typeof(T).Name} type cannot be deserialized.");
		}

		context.DepthStep();
		T value = constructor();
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
					if (properties.Length > i && properties[i]?.MsgPackReaders is var (deserialize, _))
					{
						deserialize(ref value, ref syncReader, context);
					}
					else
					{
						syncReader.Skip(context);
					}
				}

				reader.AdvanceTo(syncReader.Position);
			}

			// Read any consecutive async properties.
			for (; i < arrayLength && properties.Length > i; i++)
			{
				if (properties[i] is not PropertyAccessors<T> { PreferAsyncSerialization: true, MsgPackReaders: (_, { } deserializeAsync) })
				{
					break;
				}

				value = await deserializeAsync(value, reader, context, cancellationToken).ConfigureAwait(false);
			}

			int NextSyncReadBatchSize()
			{
				// We want to count the number of array elements need to be read up to the next async property.
				for (int j = i; j < arrayLength; j++)
				{
					if (properties.Length > j)
					{
						PropertyAccessors<T>? property = properties[j];
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

		return value;
	}
}
