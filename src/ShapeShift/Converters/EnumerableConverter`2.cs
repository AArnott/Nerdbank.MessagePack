// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;

namespace ShapeShift.Converters;

/// <summary>
/// Serializes an enumerable.
/// Deserialization is not supported.
/// </summary>
/// <typeparam name="TEnumerable">The concrete type of enumerable.</typeparam>
/// <typeparam name="TElement">The type of element in the enumerable.</typeparam>
internal class EnumerableConverter<TEnumerable, TElement>(Func<TEnumerable, IEnumerable<TElement>> getEnumerable, Converter<TElement> elementConverter) : Converter<TEnumerable>
{
	/// <summary>
	/// Gets a value indicating whether the element converter prefers async serialization.
	/// </summary>
	protected bool ElementPrefersAsyncSerialization => elementConverter.PreferAsyncSerialization;

	/// <inheritdoc/>
	public override TEnumerable? Read(ref Reader reader, SerializationContext context)
	{
		if (reader.TryReadNull())
		{
			return default;
		}

		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override void Write(ref Writer writer, in TEnumerable? value, SerializationContext context)
	{
		if (value is null)
		{
			writer.WriteNull();
			return;
		}

		context.DepthStep();
		IEnumerable<TElement> enumerable = getEnumerable(value);
		if (enumerable.TryGetNonEnumeratedCount(out int count))
		{
			writer.WriteStartVector(count);
			foreach (TElement element in enumerable)
			{
				elementConverter.Write(ref writer, element, context);
			}
		}
		else
		{
			TElement[] array = enumerable.ToArray();
			writer.WriteStartVector(array.Length);
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
	public override async ValueTask<bool> SkipToIndexValueAsync(AsyncReader reader, object? index, SerializationContext context)
	{
		int skipCount = (int)index!;

		StreamingReader streamingReader = reader.CreateStreamingReader();

		bool isNull;
		while (streamingReader.TryReadNull(out isNull).NeedsMoreBytes())
		{
			streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
		}

		if (isNull)
		{
			reader.ReturnReader(ref streamingReader);
			return false;
		}

		int? length;
		while (streamingReader.TryReadArrayHeader(out length).NeedsMoreBytes())
		{
			streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
		}

		if (length is null)
		{
			throw new NotImplementedException(); // TODO: review this.
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
	/// <param name="context"><inheritdoc cref="Converter{T}.Read" path="/param[@name='context']"/></param>
	/// <returns>The element.</returns>
	protected TElement ReadElement(ref Reader reader, SerializationContext context) => elementConverter.Read(ref reader, context)!;

	/// <summary>
	/// Reads one element from the reader.
	/// </summary>
	/// <param name="reader">The reader.</param>
	/// <param name="context"><inheritdoc cref="Converter{T}.Read" path="/param[@name='context']"/></param>
	/// <returns>The element.</returns>
	[Experimental("NBMsgPackAsync")]
	protected ValueTask<TElement> ReadElementAsync(AsyncReader reader, SerializationContext context)
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
	Converter<TElement> elementConverter,
	Setter<TEnumerable, TElement> addElement,
	Func<TEnumerable> ctor) : EnumerableConverter<TEnumerable, TElement>(getEnumerable, elementConverter), IDeserializeInto<TEnumerable>
{
	/// <inheritdoc/>
#pragma warning disable NBMsgPack031 // Exactly one structure - analyzer cannot see through this.method calls.
	public override TEnumerable? Read(ref Reader reader, SerializationContext context)
	{
		if (reader.TryReadNull())
		{
			return default;
		}

		TEnumerable result = ctor();
		this.DeserializeInto(ref reader, ref result, context);
		return result;
	}
#pragma warning restore NBMsgPack03

	/// <inheritdoc/>
	public void DeserializeInto(ref Reader reader, ref TEnumerable collection, SerializationContext context)
	{
		context.DepthStep();
		int? count = reader.ReadStartVector();
		for (int i = 0; i < count || (count is null && reader.TryAdvanceToNextElement()); i++)
		{
			addElement(ref collection, this.ReadElement(ref reader, context));
		}
	}

	/// <inheritdoc/>
	[Experimental("NBMsgPackAsync")]
	public async ValueTask DeserializeIntoAsync(AsyncReader reader, TEnumerable collection, SerializationContext context)
	{
		context.DepthStep();

		if (this.ElementPrefersAsyncSerialization)
		{
			StreamingReader streamingReader = reader.CreateStreamingReader();
			int? count;
			while (streamingReader.TryReadArrayHeader(out count).NeedsMoreBytes())
			{
				streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
			}

			if (count is null)
			{
				throw new NotImplementedException();
			}

			for (int i = 0; i < count; i++)
			{
				addElement(ref collection, await this.ReadElementAsync(reader, context).ConfigureAwait(false));
			}
		}
		else
		{
			await reader.BufferNextStructureAsync(context).ConfigureAwait(false);
			Reader syncReader = reader.CreateBufferedReader();
			int? count = syncReader.ReadStartVector();
			for (int i = 0; i < count || (count is null && syncReader.TryAdvanceToNextElement()); i++)
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
	Converter<TElement> elementConverter,
	SpanConstructor<TElement, TEnumerable> ctor) : EnumerableConverter<TEnumerable, TElement>(getEnumerable, elementConverter)
{
	/// <inheritdoc/>
	public override TEnumerable? Read(ref Reader reader, SerializationContext context)
	{
		if (reader.TryReadNull())
		{
			return default;
		}

		context.DepthStep();
		int? count = reader.ReadStartVector();
		if (count.HasValue)
		{
			TElement[] elements = ArrayPool<TElement>.Shared.Rent(count.Value);
			try
			{
				for (int i = 0; i < count; i++)
				{
					elements[i] = this.ReadElement(ref reader, context);
				}

				return ctor(elements.AsSpan(0, count.Value));
			}
			finally
			{
				ArrayPool<TElement>.Shared.Return(elements);
			}
		}
		else
		{
			List<TElement> elements = new();
			while (reader.TryAdvanceToNextElement())
			{
				elements.Add(this.ReadElement(ref reader, context));
			}

#if NET
			return ctor(CollectionsMarshal.AsSpan(elements));
#else
			return ctor(elements.ToArray());
#endif
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
	Converter<TElement> elementConverter,
	Func<IEnumerable<TElement>, TEnumerable> ctor) : EnumerableConverter<TEnumerable, TElement>(getEnumerable, elementConverter)
{
	/// <inheritdoc/>
	public override TEnumerable? Read(ref Reader reader, SerializationContext context)
	{
		if (reader.TryReadNull())
		{
			return default;
		}

		context.DepthStep();
		int? count = reader.ReadStartVector();
		if (count.HasValue)
		{
			TElement[] elements = ArrayPool<TElement>.Shared.Rent(count.Value);
			try
			{
				for (int i = 0; i < count; i++)
				{
					elements[i] = this.ReadElement(ref reader, context);
				}

				return ctor(elements.Take(count.Value));
			}
			finally
			{
				ArrayPool<TElement>.Shared.Return(elements);
			}
		}
		else
		{
			List<TElement> elements = new();
			while (reader.TryAdvanceToNextElement())
			{
				elements.Add(this.ReadElement(ref reader, context));
			}

			return ctor(elements);
		}
	}
}
