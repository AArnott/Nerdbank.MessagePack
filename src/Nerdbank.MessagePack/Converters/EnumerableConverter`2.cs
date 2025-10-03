// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using System.Text.Json.Nodes;

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// Serializes an enumerable.
/// Deserialization is not supported.
/// </summary>
/// <typeparam name="TEnumerable">The concrete type of enumerable.</typeparam>
/// <typeparam name="TElement">The type of element in the enumerable.</typeparam>
internal class EnumerableConverter<TEnumerable, TElement>(Func<TEnumerable, IEnumerable<TElement>>? getEnumerable, MessagePackConverter<TElement> elementConverter) : MessagePackConverter<TEnumerable>
{
	/// <inheritdoc />
	public override bool PreferAsyncSerialization => getEnumerable is null || this.ElementPrefersAsyncSerialization;

	/// <summary>
	/// Gets a value indicating whether the element converter prefers async serialization.
	/// </summary>
	protected bool ElementPrefersAsyncSerialization => elementConverter.PreferAsyncSerialization;

	/// <inheritdoc/>
	public override TEnumerable? Read(ref MessagePackReader reader, SerializationContext context)
	{
		if (reader.TryReadNil())
		{
			return default;
		}

		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override async ValueTask WriteAsync(MessagePackAsyncWriter writer, TEnumerable? value, SerializationContext context)
	{
		if (value is null)
		{
			writer.WriteNil();
			return;
		}

		context.DepthStep();

		List<TElement> elements = [];
		if (getEnumerable is not null)
		{
			foreach (TElement element in getEnumerable(value))
			{
				elements.Add(element);
			}
		}
		else
		{
			IAsyncEnumerable<TElement> enumerable = (IAsyncEnumerable<TElement>)value;
			await foreach (TElement element in enumerable.WithCancellation(context.CancellationToken).ConfigureAwait(false))
			{
				elements.Add(element);
			}
		}

		if (elementConverter.PreferAsyncSerialization)
		{
			writer.WriteArrayHeader(elements.Count);
			int i = 0;
			try
			{
				for (; i < elements.Count; i++)
				{
					await elementConverter.WriteAsync(writer, elements[i], context).ConfigureAwait(false);
				}
			}
			catch (Exception ex) when (ShouldWrapSerializationException(ex, context.CancellationToken))
			{
				throw new MessagePackSerializationException(CreateFailWritingValueAtIndex(typeof(TElement), i), ex);
			}
		}
		else
		{
			MessagePackWriter syncWriter = writer.CreateWriter();
			syncWriter.WriteArrayHeader(elements.Count);
			int i = 0;
			try
			{
				for (; i < elements.Count; i++)
				{
					elementConverter.Write(ref syncWriter, elements[i], context);
				}

				if (writer.IsTimeToFlush(context, syncWriter))
				{
					writer.ReturnWriter(ref syncWriter);
					await writer.FlushIfAppropriateAsync(context).ConfigureAwait(false);
					syncWriter = writer.CreateWriter();
				}
			}
			catch (Exception ex) when (ShouldWrapSerializationException(ex, context.CancellationToken))
			{
				throw new MessagePackSerializationException(CreateFailWritingValueAtIndex(typeof(TElement), i), ex);
			}

			writer.ReturnWriter(ref syncWriter);
		}
	}

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in TEnumerable? value, SerializationContext context)
	{
		if (getEnumerable is null)
		{
			throw new NotSupportedException($"The type {typeof(TEnumerable)} does not support synchronous serialization because it implements IAsyncEnumerable<T>. Use async serialization instead.");
		}

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
			int index = 0;
			try
			{
				foreach (TElement element in enumerable)
				{
					elementConverter.Write(ref writer, element, context);
					index++;
				}
			}
			catch (Exception ex) when (ShouldWrapSerializationException(ex, context.CancellationToken))
			{
				throw new MessagePackSerializationException(CreateFailWritingValueAtIndex(typeof(TElement), index), ex);
			}
		}
		else
		{
			TElement[] array = enumerable.ToArray();
			writer.WriteArrayHeader(array.Length);
			int i = 0;
			try
			{
				for (; i < array.Length; i++)
				{
					elementConverter.Write(ref writer, array[i], context);
				}
			}
			catch (Exception ex) when (ShouldWrapSerializationException(ex, context.CancellationToken))
			{
				throw new MessagePackSerializationException(CreateFailWritingValueAtIndex(typeof(TElement), i), ex);
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
	protected TElement ReadElement(ref MessagePackReader reader, SerializationContext context) => elementConverter.Read(ref reader, context)!;

	/// <summary>
	/// Reads one element from the reader.
	/// </summary>
	/// <param name="reader">The reader.</param>
	/// <param name="context"><inheritdoc cref="MessagePackConverter{T}.Read" path="/param[@name='context']"/></param>
	/// <returns>The element.</returns>
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
/// <param name="ctor">The constructor for the enumerable type.</param>
/// <param name="collectionConstructionOptions">A template for options to pass to the <paramref name="ctor"/>.</param>
internal class MutableEnumerableConverter<TEnumerable, TElement>(
	Func<TEnumerable, IEnumerable<TElement>>? getEnumerable,
	MessagePackConverter<TElement> elementConverter,
	EnumerableAppender<TEnumerable, TElement> addElement,
	MutableCollectionConstructor<TElement, TEnumerable> ctor,
	Result<CollectionConstructionOptions<TElement>, VisitorError> collectionConstructionOptions) : EnumerableConverter<TEnumerable, TElement>(getEnumerable, elementConverter), IDeserializeInto<TEnumerable>
{
	/// <inheritdoc/>
#pragma warning disable NBMsgPack031 // Exactly one structure - analyzer cannot see through this.method calls.
	public override TEnumerable? Read(ref MessagePackReader reader, SerializationContext context)
	{
		CollectionConstructionOptions<TElement> options = collectionConstructionOptions.GetValueOrThrow();

		if (reader.TryReadNil())
		{
			return default;
		}

		return this.DeserializeInto(
			ref reader,
			static (s, size) => s.ctor(s.options.WithCapacity(size)),
			(ctor, options),
			context);
	}
#pragma warning restore NBMsgPack031

	/// <inheritdoc/>
	public override async ValueTask<TEnumerable?> ReadAsync(MessagePackAsyncReader reader, SerializationContext context)
	{
		CollectionConstructionOptions<TElement> options = collectionConstructionOptions.GetValueOrThrow();

		MessagePackStreamingReader streamingReader = reader.CreateStreamingReader();
		bool isNil;
		while (streamingReader.TryReadNil(out isNil).NeedsMoreBytes())
		{
			streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
		}

		reader.ReturnReader(ref streamingReader);
		if (isNil)
		{
			return default;
		}

		return await this.DeserializeIntoAsync(
			reader,
			static (s, size) => s.ctor(s.options.WithCapacity(size)),
			(ctor, options),
			context).ConfigureAwait(false);
	}

	/// <inheritdoc/>
	public void DeserializeInto(ref MessagePackReader reader, ref TEnumerable collection, SerializationContext context)
		=> collection = this.DeserializeInto(ref reader, static (s, _) => s, collection, context);

	/// <inheritdoc/>
	public ValueTask<TEnumerable> DeserializeIntoAsync(MessagePackAsyncReader reader, TEnumerable collection, SerializationContext context)
		=> this.DeserializeIntoAsync(reader, static (s, _) => s, collection, context);

	private TEnumerable DeserializeInto<TState>(ref MessagePackReader reader, Func<TState, int, TEnumerable> getCollection, TState state, SerializationContext context)
	{
		context.DepthStep();
		int count = reader.ReadArrayHeader();
		TEnumerable collection = getCollection(state, count);
		int i = 0;
		try
		{
			for (; i < count; i++)
			{
				addElement(ref collection, this.ReadElement(ref reader, context));
			}
		}
		catch (Exception ex) when (ShouldWrapSerializationException(ex, context.CancellationToken))
		{
			throw new MessagePackSerializationException(CreateFailReadingValueAtIndex(typeof(TElement), i), ex);
		}

		return collection;
	}

	private async ValueTask<TEnumerable> DeserializeIntoAsync<TState>(MessagePackAsyncReader reader, Func<TState, int, TEnumerable> getCollection, TState state, SerializationContext context)
	{
		context.DepthStep();

		TEnumerable collection;
		if (this.ElementPrefersAsyncSerialization)
		{
			MessagePackStreamingReader streamingReader = reader.CreateStreamingReader();
			int count;
			while (streamingReader.TryReadArrayHeader(out count).NeedsMoreBytes())
			{
				streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
			}

			reader.ReturnReader(ref streamingReader);

			collection = getCollection(state, count);
			int i = 0;
			try
			{
				for (; i < count; i++)
				{
					addElement(ref collection, await this.ReadElementAsync(reader, context).ConfigureAwait(false));
				}
			}
			catch (Exception ex) when (ShouldWrapSerializationException(ex, context.CancellationToken))
			{
				throw new MessagePackSerializationException(CreateFailReadingValueAtIndex(typeof(TElement), i), ex);
			}
		}
		else
		{
			await reader.BufferNextStructureAsync(context).ConfigureAwait(false);
			MessagePackReader syncReader = reader.CreateBufferedReader();
			int count = syncReader.ReadArrayHeader();
			collection = getCollection(state, count);
			int i = 0;
			try
			{
				for (; i < count; i++)
				{
					addElement(ref collection, this.ReadElement(ref syncReader, context));
				}
			}
			catch (Exception ex) when (ShouldWrapSerializationException(ex, context.CancellationToken))
			{
				throw new MessagePackSerializationException(CreateFailReadingValueAtIndex(typeof(TElement), i), ex);
			}

			reader.ReturnReader(ref syncReader);
		}

		return collection;
	}
}

/// <summary>
/// Serializes and deserializes an immutable enumerable.
/// </summary>
/// <inheritdoc cref="EnumerableConverter{TEnumerable, TElement}"/>
/// <param name="getEnumerable"><inheritdoc cref="EnumerableConverter{TEnumerable, TElement}" path="/param[@name='getEnumerable']"/></param>
/// <param name="elementConverter"><inheritdoc cref="EnumerableConverter{TEnumerable, TElement}" path="/param[@name='elementConverter']"/></param>
/// <param name="ctor">A enumerable initializer that constructs from a span of elements.</param>
/// <param name="collectionConstructionOptions">A template for options to pass to the <paramref name="ctor"/>.</param>
internal class SpanEnumerableConverter<TEnumerable, TElement>(
	Func<TEnumerable, IEnumerable<TElement>>? getEnumerable,
	MessagePackConverter<TElement> elementConverter,
	ParameterizedCollectionConstructor<TElement, TElement, TEnumerable> ctor,
	Result<CollectionConstructionOptions<TElement>, VisitorError> collectionConstructionOptions) : EnumerableConverter<TEnumerable, TElement>(getEnumerable, elementConverter)
{
	/// <inheritdoc/>
	public override TEnumerable? Read(ref MessagePackReader reader, SerializationContext context)
	{
		CollectionConstructionOptions<TElement> options = collectionConstructionOptions.GetValueOrThrow();

		if (reader.TryReadNil())
		{
			return default;
		}

		context.DepthStep();
		int count = reader.ReadArrayHeader();
		TElement[] elements = ArrayPool<TElement>.Shared.Rent(count);
		int? i = 0;
		try
		{
			for (; i < count; i++)
			{
				elements[i.Value] = this.ReadElement(ref reader, context);
			}

			i = null;
			return ctor(elements.AsSpan(0, count), options);
		}
		catch (Exception ex) when (i is not null && ShouldWrapSerializationException(ex, context.CancellationToken))
		{
			throw new MessagePackSerializationException(CreateFailReadingValueAtIndex(typeof(TElement), i.Value), ex);
		}
		finally
		{
			ArrayPool<TElement>.Shared.Return(elements);
		}
	}

	/// <inheritdoc/>
	public override async ValueTask<TEnumerable?> ReadAsync(MessagePackAsyncReader reader, SerializationContext context)
	{
		if (this.ElementPrefersAsyncSerialization)
		{
			MessagePackStreamingReader streamingReader = reader.CreateStreamingReader();
			bool isNil;
			while (streamingReader.TryReadNil(out isNil).NeedsMoreBytes())
			{
				streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
			}

			if (isNil)
			{
				reader.ReturnReader(ref streamingReader);
				return default;
			}

			context.DepthStep();

			int count;
			while (streamingReader.TryReadArrayHeader(out count).NeedsMoreBytes())
			{
				streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
			}

			reader.ReturnReader(ref streamingReader);
			TElement[] elements = ArrayPool<TElement>.Shared.Rent(count);
			int? i = 0;
			try
			{
				for (; i < count; i++)
				{
					elements[i.Value] = await this.ReadElementAsync(reader, context).ConfigureAwait(false);
				}

				i = null;
				return ctor(elements.AsSpan(0, count));
			}
			catch (Exception ex) when (i is not null && ShouldWrapSerializationException(ex, context.CancellationToken))
			{
				throw new MessagePackSerializationException(CreateFailReadingValueAtIndex(typeof(TElement), i.Value), ex);
			}
			finally
			{
				ArrayPool<TElement>.Shared.Return(elements);
			}
		}
		else
		{
			await reader.BufferNextStructureAsync(context).ConfigureAwait(false);
			MessagePackReader syncReader = reader.CreateBufferedReader();
			TEnumerable? result = this.Read(ref syncReader, context);
			reader.ReturnReader(ref syncReader);
			return result;
		}
	}
}
