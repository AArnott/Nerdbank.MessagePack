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
	public override T? Deserialize(ref MessagePackReader reader, SerializationContext context)
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
	public override void Serialize(ref MessagePackWriter writer, in T? value, SerializationContext context)
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
	public override async ValueTask SerializeAsync(MessagePackAsyncWriter writer, T? value, SerializationContext context, CancellationToken cancellationToken)
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
			if (properties[i]?.MsgPackWriters is var (_, serializeAsync))
			{
				await serializeAsync(value, writer, context, cancellationToken).ConfigureAwait(false);
			}
			else
			{
				writer.WriteNil();
			}
		}
	}

	/// <inheritdoc/>
	[Experimental("NBMsgPackAsync")]
	public override async ValueTask<T?> DeserializeAsync(MessagePackAsyncReader reader, SerializationContext context, CancellationToken cancellationToken)
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
		int count = await reader.ReadArrayHeaderAsync(cancellationToken).ConfigureAwait(false);
		for (int i = 0; i < count; i++)
		{
			if (properties.Length > i && properties[i]?.MsgPackReaders is var (_, deserializeAsync))
			{
				value = await deserializeAsync(value, reader, context, cancellationToken).ConfigureAwait(false);
			}
			else
			{
				await reader.SkipAsync(context, cancellationToken).ConfigureAwait(false);
			}
		}

		return value;
	}
}
