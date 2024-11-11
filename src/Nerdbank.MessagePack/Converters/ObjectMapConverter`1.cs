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
internal class ObjectMapConverter<T>(MapSerializableProperties<T> serializable, MapDeserializableProperties<T>? deserializable, Func<T>? constructor) : MessagePackConverter<T>
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
		writer.WriteMapHeader(serializable.Properties?.Count ?? 0);
		if (serializable.Properties is not null)
		{
			foreach (SerializableProperty<T> property in serializable.Properties)
			{
				writer.WriteRaw(property.RawPropertyNameString.Span);
				property.Write(value, ref writer, context);
			}
		}
	}

	/// <inheritdoc/>
	[Experimental("NBMsgPackAsync")]
	public override async ValueTask WriteAsync(MessagePackAsyncWriter writer, T? value, SerializationContext context, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		if (value is null)
		{
			writer.WriteNil();
			return;
		}

		context.DepthStep();
		writer.WriteMapHeader(serializable.Properties?.Count ?? 0);

		if (serializable.Properties is not null)
		{
			foreach (SerializableProperty<T> property in serializable.Properties)
			{
				writer.WriteRaw(property.RawPropertyNameString.Span);
				await property.WriteAsync(value, writer, context, cancellationToken).ConfigureAwait(false);
				await writer.FlushIfAppropriateAsync(context, cancellationToken).ConfigureAwait(false);
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
				ReadOnlySpan<byte> propertyName = CodeGenHelpers.ReadStringSpan(ref reader);
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
	public override async ValueTask<T?> ReadAsync(MessagePackAsyncReader reader, SerializationContext context, CancellationToken cancellationToken)
	{
		if (await reader.TryReadNilAsync(cancellationToken).ConfigureAwait(false))
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
			int count = await reader.ReadMapHeaderAsync(cancellationToken).ConfigureAwait(false);
			for (int i = 0; i < count; i++)
			{
				ReadOnlySequence<byte> buffer = await reader.ReadNextStructureAsync(context, cancellationToken).ConfigureAwait(false);
				bool matchedProperty = TryMatchProperty(buffer, out DeserializableProperty<T> propertyReader);
				reader.AdvanceTo(buffer.End);

				if (matchedProperty)
				{
					value = await propertyReader.ReadAsync(value, reader, context, cancellationToken).ConfigureAwait(false);
				}
				else
				{
					await reader.SkipAsync(context, cancellationToken).ConfigureAwait(false);
				}
			}
		}
		else
		{
			// We have nothing to read into, so just skip any data in the object.
			await reader.SkipAsync(context, cancellationToken).ConfigureAwait(false);
		}

		bool TryMatchProperty(ReadOnlySequence<byte> propertyName, out DeserializableProperty<T> propertyReader)
		{
			MessagePackReader reader = new(propertyName);
			ReadOnlySpan<byte> propertyNameSpan = CodeGenHelpers.ReadStringSpan(ref reader);
			return deserializable.Value.Readers.TryGetValue(propertyNameSpan, out propertyReader);
		}

		return value;
	}
}
