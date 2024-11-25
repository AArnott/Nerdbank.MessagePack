// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// A <see cref="MessagePackConverter{T}"/> that writes objects as maps of property names to values.
/// Only data types with default constructors may be deserialized.
/// </summary>
/// <typeparam name="T">The type of objects that can be serialized or deserialized with this converter.</typeparam>
/// <param name="serializable">Tools for serializing individual property values.</param>
/// <param name="deserializable">Tools for deserializing individual property values. May be omitted if the type will never be deserialized (i.e. there is no deserializing constructor).</param>
/// <param name="constructor">The default constructor, if present.</param>
/// <param name="callShouldSerialize"><see langword="true" /> if some of the properties should maybe be omitted; <see langword="false" /> to allow a fast path that assumes all properties are serialized.</param>
internal class ObjectMapConverter<T>(MapSerializableProperties<T> serializable, MapDeserializableProperties<T>? deserializable, Func<T>? constructor, bool callShouldSerialize) : MessagePackConverter<T>
{
	/// <inheritdoc/>
	public override bool PreferAsyncSerialization => true;

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in T? value, SerializationContext context)
	{
		if (value is null)
		{
			writer.WriteNil();
			return;
		}

		context.DepthStep();

		if (callShouldSerialize && serializable.Properties.Length > 0)
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

		static void WriteProperties(ref MessagePackWriter writer, in T value, ReadOnlySpan<SerializableProperty<T>> properties, SerializationContext context)
		{
			writer.WriteMapHeader(properties.Length);
			foreach (SerializableProperty<T> property in properties)
			{
				writer.WriteRaw(property.RawPropertyNameString.Span);
				property.Write(value, ref writer, context);
			}
		}
	}

	/// <inheritdoc/>
	[Experimental("NBMsgPackAsync")]
	public override async ValueTask WriteAsync(MessagePackAsyncWriter writer, T? value, SerializationContext context)
	{
		if (value is null)
		{
			writer.WriteNil();
			return;
		}

		context.DepthStep();
		ReadOnlyMemory<SerializableProperty<T>> propertiesToSerialize;
		SerializableProperty<T>[]? borrowedArray = null;
		try
		{
			if (callShouldSerialize && serializable.Properties.Length > 0)
			{
				borrowedArray = ArrayPool<SerializableProperty<T>>.Shared.Rent(serializable.Properties.Length);
				propertiesToSerialize = this.GetPropertiesToSerialize(value, borrowedArray.AsMemory());
			}
			else
			{
				propertiesToSerialize = serializable.Properties;
			}

			MessagePackWriter syncWriter = writer.CreateWriter();
			syncWriter.WriteMapHeader(propertiesToSerialize.Length);
			for (int i = 0; i < propertiesToSerialize.Length; i++)
			{
				SerializableProperty<T> property = propertiesToSerialize.Span[i];

				syncWriter.WriteRaw(property.RawPropertyNameString.Span);
				if (property.PreferAsyncSerialization)
				{
					syncWriter.Flush();
					await property.WriteAsync(value, writer, context).ConfigureAwait(false);
					syncWriter = writer.CreateWriter();
				}
				else
				{
					property.Write(value, ref syncWriter, context);
				}

				if (writer.IsTimeToFlush(context, syncWriter))
				{
					syncWriter.Flush();
					await writer.FlushIfAppropriateAsync(context).ConfigureAwait(false);
					syncWriter = writer.CreateWriter();
				}
			}

			syncWriter.Flush();
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
	public override T? Read(ref MessagePackReader reader, SerializationContext context)
	{
		if (reader.TryReadNil())
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
			int count = reader.ReadMapHeader();
			for (int i = 0; i < count; i++)
			{
				ReadOnlySpan<byte> propertyName = ReadStringSpan(ref reader);
				if (deserializable.Value.Readers.TryGetValue(propertyName, out DeserializableProperty<T> propertyReader))
				{
					propertyReader.Read(ref value, ref reader, context);
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

		return value;
	}

	/// <inheritdoc/>
	[Experimental("NBMsgPackAsync")]
	public override async ValueTask<T?> ReadAsync(MessagePackAsyncReader reader, SerializationContext context)
	{
		if (await reader.TryReadNilAsync().ConfigureAwait(false))
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
			int mapEntries = await reader.ReadMapHeaderAsync().ConfigureAwait(false);

			// We're going to read in bursts. Anything we happen to get in one buffer, we'll ready synchronously regardless of whether the property is async.
			// But when we run out of buffer, if the next thing to read is async, we'll read it async.
			int remainingEntries = mapEntries;
			while (remainingEntries > 0)
			{
				(ReadOnlySequence<byte> buffer, int bufferedStructures) = await reader.ReadNextStructuresAsync(1, remainingEntries * 2, context).ConfigureAwait(false);
				MessagePackReader syncReader = new(buffer);
				int bufferedEntries = bufferedStructures / 2;
				for (int i = 0; i < bufferedEntries; i++)
				{
					ReadOnlySpan<byte> propertyName = ReadStringSpan(ref syncReader);
					if (deserializable.Value.Readers.TryGetValue(propertyName, out DeserializableProperty<T> propertyReader))
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
						ReadOnlySpan<byte> propertyName = ReadStringSpan(ref syncReader);
						if (deserializable.Value.Readers.TryGetValue(propertyName, out DeserializableProperty<T> propertyReader) && propertyReader.PreferAsyncSerialization)
						{
							// The next property value is async, so turn in our sync reader and read it asynchronously.
							reader.AdvanceTo(syncReader.Position);
							value = await propertyReader.ReadAsync(value, reader, context).ConfigureAwait(false);
							remainingEntries--;

							// Now loop around to see what else we can do with the next buffer.
							continue;
						}
					}
					else
					{
						// The property name isn't in the buffer, and thus whether it'll have an async reader.
						// Advance the reader so it knows we need more buffer than we got last time.
						reader.AdvanceTo(syncReader.Position, buffer.End);
						continue;
					}
				}

				reader.AdvanceTo(syncReader.Position);
			}
		}
		else
		{
			// We have nothing to read into, so just skip any data in the object.
			await reader.SkipAsync(context).ConfigureAwait(false);
		}

		return value;
	}

	/// <summary>
	/// Reads a string as a contiguous span of UTF-8 encoded characters.
	/// An array may be allocated if the string is not already contiguous in memory.
	/// </summary>
	/// <param name="reader">The reader to use.</param>
	/// <returns>The span of UTF-8 encoded characters.</returns>
	protected static ReadOnlySpan<byte> ReadStringSpan(scoped ref MessagePackReader reader)
	{
		if (!reader.TryReadStringSpan(out ReadOnlySpan<byte> result))
		{
			ReadOnlySequence<byte>? sequence = reader.ReadStringSequence();
			if (sequence.HasValue)
			{
				if (sequence.Value.IsSingleSegment)
				{
					return sequence.Value.First.Span;
				}

				return sequence.Value.ToArray();
			}

			return default;
		}

		return result;
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
