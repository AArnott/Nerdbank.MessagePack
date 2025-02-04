// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// Serializes an enumerable.
/// Deserialization is not supported.
/// </summary>
/// <typeparam name="TEnumerable">The concrete type of enumerable.</typeparam>
/// <typeparam name="TElement">The type of element in the enumerable.</typeparam>
internal class EnumerableConverter<TEnumerable, TElement>(Func<TEnumerable, IEnumerable<TElement>> getEnumerable, MessagePackConverter<TElement> elementConverter) : MessagePackConverter<TEnumerable>
{
	/// <summary>
	/// Gets a value indicating whether the element converter prefers async serialization.
	/// </summary>
	protected bool ElementPrefersAsyncSerialization => elementConverter.PreferAsyncSerialization;

	/// <inheritdoc/>
	public override void Read(ref MessagePackReader reader, ref TEnumerable? value, SerializationContext context)
	{
		if (reader.TryReadNil())
		{
			value = default;
			return;
		}

		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in TEnumerable? value, SerializationContext context)
	{
		if (value is null)
		{
			writer.WriteNil();
			return;
		}

		context.DepthStep();
		IEnumerable<TElement> enumerable = getEnumerable(value);
		if (PolyfillExtensions.TryGetNonEnumeratedCount(enumerable, out int count))
		{
			writer.WriteArrayHeader(count);
			foreach (TElement element in enumerable)
			{
				elementConverter.Write(ref writer, element, context);
			}
		}
		else
		{
			TElement[] array = enumerable.ToArray();
			writer.WriteArrayHeader(array.Length);
			for (int i = 0; i < array.Length; i++)
			{
				elementConverter.Write(ref writer, array[i], context);
			}
		}
	}

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
	{
		return new JsonObject
		{
			["type"] = "array",
			["items"] = context.GetJsonSchema(((IEnumerableTypeShape<TEnumerable, TElement>)typeShape).ElementType),
		};
	}

	/// <inheritdoc/>
	[Experimental("NBMsgPackAsync")]
	public override async ValueTask<bool> SkipToIndexValueAsync(MessagePackAsyncReader reader, object? index, SerializationContext context)
	{
		int skipCount = (int)index!;

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

		int length;
		while (streamingReader.TryReadArrayHeader(out length).NeedsMoreBytes())
		{
			streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
		}

		if (length < skipCount + 1)
		{
			reader.ReturnReader(ref streamingReader);
			return false;
		}

		for (int i = 0; i < skipCount; i++)
		{
			while (streamingReader.TrySkip(ref context).NeedsMoreBytes())
			{
				streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
			}
		}

		reader.ReturnReader(ref streamingReader);
		return true;
	}

	/// <summary>
	/// Reads one element from the reader.
	/// </summary>
	/// <param name="reader">The reader.</param>
	/// <param name="context"><inheritdoc cref="MessagePackConverter{T}.Read" path="/param[@name='context']"/></param>
	/// <returns>The element.</returns>
	protected TElement ReadElement(ref MessagePackReader reader, SerializationContext context)
	{
		TElement? element = default;
		elementConverter.Read(ref reader, ref element, context);
		return element;
	}

	/// <summary>
	/// Reads one element from the reader.
	/// </summary>
	/// <param name="reader">The reader.</param>
	/// <param name="context"><inheritdoc cref="MessagePackConverter{T}.Read" path="/param[@name='context']"/></param>
	/// <returns>The element.</returns>
	[Experimental("NBMsgPackAsync")]
	protected ValueTask<TElement> ReadElementAsync(MessagePackAsyncReader reader, SerializationContext context)
		=> elementConverter.ReadAsync(reader, context)!;
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
	Func<TEnumerable> ctor) : EnumerableConverter<TEnumerable, TElement>(getEnumerable, elementConverter), IDeserializeInto<TEnumerable>
{
	/// <inheritdoc/>
#pragma warning disable NBMsgPack031 // Exactly one structure - analyzer cannot see through this.method calls.
	public override void Read(ref MessagePackReader reader, ref TEnumerable? value, SerializationContext context)
	{
		if (reader.TryReadNil())
		{
			value = default;
			return;
		}

		TEnumerable result = ctor();
		this.DeserializeInto(ref reader, ref result, context);
		value = result;
	}
#pragma warning restore NBMsgPack03

	/// <inheritdoc/>
	public void DeserializeInto(ref MessagePackReader reader, ref TEnumerable collection, SerializationContext context)
	{
		context.DepthStep();
		int count = reader.ReadArrayHeader();
		for (int i = 0; i < count; i++)
		{
			addElement(ref collection, this.ReadElement(ref reader, context));
		}
	}

	/// <inheritdoc/>
	[Experimental("NBMsgPackAsync")]
	public async ValueTask DeserializeIntoAsync(MessagePackAsyncReader reader, TEnumerable collection, SerializationContext context)
	{
		context.DepthStep();

		if (this.ElementPrefersAsyncSerialization)
		{
			MessagePackStreamingReader streamingReader = reader.CreateStreamingReader();
			int count;
			while (streamingReader.TryReadArrayHeader(out count).NeedsMoreBytes())
			{
				streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
			}

			for (int i = 0; i < count; i++)
			{
				addElement(ref collection, await this.ReadElementAsync(reader, context).ConfigureAwait(false));
			}
		}
		else
		{
			await reader.BufferNextStructureAsync(context).ConfigureAwait(false);
			MessagePackReader syncReader = reader.CreateBufferedReader();
			int count = syncReader.ReadArrayHeader();
			for (int i = 0; i < count; i++)
			{
				addElement(ref collection, this.ReadElement(ref syncReader, context));
			}

			reader.ReturnReader(ref syncReader);
		}
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
	public override void Read(ref MessagePackReader reader, ref TEnumerable? value, SerializationContext context)
	{
		if (reader.TryReadNil())
		{
			value = default;
			return;
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

			value = ctor(elements.AsSpan(0, count));
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
	public override void Read(ref MessagePackReader reader, ref TEnumerable? value, SerializationContext context)
	{
		if (reader.TryReadNil())
		{
			value = default;
			return;
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

			value = ctor(elements.Take(count));
		}
		finally
		{
			ArrayPool<TElement>.Shared.Return(elements);
		}
	}
}
