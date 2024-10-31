// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// Serializes an enumerable.
/// Deserialization is not supported.
/// </summary>
/// <typeparam name="TEnumerable">The concrete type of enumerable.</typeparam>
/// <typeparam name="TElement">The type of element in the enumerable.</typeparam>
internal class EnumerableConverter<TEnumerable, TElement>(Func<TEnumerable, IEnumerable<TElement>> getEnumerable, MessagePackConverter<TElement> elementConverter) : MessagePackConverter<TEnumerable>
{
	/// <inheritdoc/>
	public override TEnumerable? Deserialize(ref MessagePackReader reader, SerializationContext context)
	{
		if (reader.TryReadNil())
		{
			return default;
		}

		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override void Serialize(ref MessagePackWriter writer, ref TEnumerable? value, SerializationContext context)
	{
		if (value is null)
		{
			writer.WriteNil();
			return;
		}

		context.DepthStep();
		IEnumerable<TElement> enumerable = getEnumerable(value);
		if (Enumerable.TryGetNonEnumeratedCount(enumerable, out int count))
		{
			writer.WriteArrayHeader(count);
			foreach (TElement element in enumerable)
			{
				TElement? el = element;
				elementConverter.Serialize(ref writer, ref el, context);
			}
		}
		else
		{
			TElement?[] array = enumerable.ToArray();
			writer.WriteArrayHeader(array.Length);
			for (int i = 0; i < array.Length; i++)
			{
				elementConverter.Serialize(ref writer, ref array[i], context);
			}
		}
	}

	/// <summary>
	/// Reads one element from the reader.
	/// </summary>
	/// <param name="reader">The reader.</param>
	/// <param name="context"><inheritdoc cref="MessagePackConverter{T}.Deserialize" path="/param[@name='context']"/></param>
	/// <returns>The element.</returns>
	protected TElement ReadElement(ref MessagePackReader reader, SerializationContext context) => elementConverter.Deserialize(ref reader, context)!;
}

/// <summary>
/// Serializes and deserializes a mutable enumerable.
/// </summary>
/// <inheritdoc cref="EnumerableConverter{TEnumerable, TElement}"/>
/// <param name="getEnumerable"><inheritdoc cref="EnumerableConverter{TEnumerable, TElement}" path="/param[@name='getEnumerable']"/></param>
/// <param name="elementConverter"><inheritdoc cref="EnumerableConverter{TEnumerable, TElement}" path="/param[@name='elementConverter']"/></param>
/// <param name="addElement">The delegate that adds an element to the enumerable.</param>
/// <param name="ctor">The default constructor for the enumerable type.</param>
internal class MutableEnumerableConverter<TEnumerable, TElement>(
	Func<TEnumerable, IEnumerable<TElement>> getEnumerable,
	MessagePackConverter<TElement> elementConverter,
	Setter<TEnumerable, TElement> addElement,
	Func<TEnumerable> ctor) : EnumerableConverter<TEnumerable, TElement>(getEnumerable, elementConverter)
{
	/// <inheritdoc/>
	public override TEnumerable? Deserialize(ref MessagePackReader reader, SerializationContext context)
	{
		if (reader.TryReadNil())
		{
			return default;
		}

		context.DepthStep();
		TEnumerable result = ctor();
		int count = reader.ReadArrayHeader();
		for (int i = 0; i < count; i++)
		{
			addElement(ref result, this.ReadElement(ref reader, context));
		}

		return result;
	}
}

/// <summary>
/// Serializes and deserializes an immutable enumerable.
/// </summary>
/// <inheritdoc cref="EnumerableConverter{TEnumerable, TElement}"/>
/// <param name="getEnumerable"><inheritdoc cref="EnumerableConverter{TEnumerable, TElement}" path="/param[@name='getEnumerable']"/></param>
/// <param name="elementConverter"><inheritdoc cref="EnumerableConverter{TEnumerable, TElement}" path="/param[@name='elementConverter']"/></param>
/// <param name="ctor">A enumerable initializer that constructs from a span of elements.</param>
internal class SpanEnumerableConverter<TEnumerable, TElement>(
	Func<TEnumerable, IEnumerable<TElement>> getEnumerable,
	MessagePackConverter<TElement> elementConverter,
	SpanConstructor<TElement, TEnumerable> ctor) : EnumerableConverter<TEnumerable, TElement>(getEnumerable, elementConverter)
{
	/// <inheritdoc/>
	public override TEnumerable? Deserialize(ref MessagePackReader reader, SerializationContext context)
	{
		if (reader.TryReadNil())
		{
			return default;
		}

		context.DepthStep();
		int count = reader.ReadArrayHeader();
		TElement[] elements = ArrayPool<TElement>.Shared.Rent(count);
		try
		{
			for (int i = 0; i < count; i++)
			{
				elements[i] = this.ReadElement(ref reader, context);
			}

			return ctor(elements.AsSpan(0, count));
		}
		finally
		{
			ArrayPool<TElement>.Shared.Return(elements);
		}
	}
}

/// <summary>
/// Serializes and deserializes an enumerable that initializes from an enumerable of elements.
/// </summary>
/// <inheritdoc cref="EnumerableConverter{TEnumerable, TElement}"/>
/// <param name="getEnumerable"><inheritdoc cref="EnumerableConverter{TEnumerable, TElement}" path="/param[@name='getEnumerable']"/></param>
/// <param name="elementConverter"><inheritdoc cref="EnumerableConverter{TEnumerable, TElement}" path="/param[@name='elementConverter']"/></param>
/// <param name="ctor">A enumerable initializer that constructs from an enumerable of elements.</param>
internal class EnumerableEnumerableConverter<TEnumerable, TElement>(
	Func<TEnumerable, IEnumerable<TElement>> getEnumerable,
	MessagePackConverter<TElement> elementConverter,
	Func<IEnumerable<TElement>, TEnumerable> ctor) : EnumerableConverter<TEnumerable, TElement>(getEnumerable, elementConverter)
{
	/// <inheritdoc/>
	public override TEnumerable? Deserialize(ref MessagePackReader reader, SerializationContext context)
	{
		if (reader.TryReadNil())
		{
			return default;
		}

		context.DepthStep();
		int count = reader.ReadArrayHeader();
		TElement[] elements = ArrayPool<TElement>.Shared.Rent(count);
		try
		{
			for (int i = 0; i < count; i++)
			{
				elements[i] = this.ReadElement(ref reader, context);
			}

			return ctor(elements.Take(count));
		}
		finally
		{
			ArrayPool<TElement>.Shared.Return(elements);
		}
	}
}
