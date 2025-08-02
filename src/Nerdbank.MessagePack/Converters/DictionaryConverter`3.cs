// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// Serializes a dictionary.
/// Deserialization is not supported.
/// </summary>
/// <typeparam name="TDictionary">The concrete dictionary type to be serialized.</typeparam>
/// <typeparam name="TKey">The type of key.</typeparam>
/// <typeparam name="TValue">The type of value.</typeparam>
/// <param name="getReadable">A delegate which converts the opaque dictionary type to a readable form.</param>
/// <param name="keyConverter">A converter for keys.</param>
/// <param name="valueConverter">A converter for values.</param>
/// <param name="disallowNullKeys">If <see langword="true"/>, deserialization should throw if a <see langword="null"/> key is encountered.</param>
/// <param name="disallowNullValues">If <see langword="true"/>, deserialization should throw if a <see langword="null"/> value is encountered.</param>
internal class DictionaryConverter<TDictionary, TKey, TValue>(Func<TDictionary, IReadOnlyDictionary<TKey, TValue>> getReadable, MessagePackConverter<TKey> keyConverter, MessagePackConverter<TValue> valueConverter, bool disallowNullKeys, bool disallowNullValues) : MessagePackConverter<TDictionary>
	where TKey : notnull
{
	/// <summary>
	/// Gets a value indicating whether the key or value converters prefer async serialization.
	/// </summary>
	protected bool ElementPrefersAsyncSerialization => keyConverter.PreferAsyncSerialization || valueConverter.PreferAsyncSerialization;

	/// <summary>
	/// Gets a value indicating whether deserialization should throw if a <see langword="null"/> key is encountered.
	/// </summary>
	protected bool DisallowNullKeys => disallowNullKeys;

	/// <summary>
	/// Gets a value indicating whether deserialization should throw if a <see langword="null"/> value is encountered.
	/// </summary>
	protected bool DisallowNullValues => disallowNullValues;

	/// <inheritdoc/>
	public override TDictionary? Read(ref MessagePackReader reader, SerializationContext context)
	{
		if (reader.TryReadNil())
		{
			return default;
		}

		throw new NotSupportedException();
	}

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in TDictionary? value, SerializationContext context)
	{
		if (value is null)
		{
			writer.WriteNil();
			return;
		}

		context.DepthStep();
		IReadOnlyDictionary<TKey, TValue> dictionary = getReadable(value);
		writer.WriteMapHeader(dictionary.Count);
		foreach (KeyValuePair<TKey, TValue> pair in dictionary)
		{
			TKey? entryKey = pair.Key;
			TValue? entryValue = pair.Value;

			keyConverter.Write(ref writer, entryKey, context);
			valueConverter.Write(ref writer, entryValue, context);
		}
	}

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
	{
		JsonObject schema = new()
		{
			["type"] = "object",
			["additionalProperties"] = context.GetJsonSchema(((IDictionaryTypeShape<TDictionary, TKey, TValue>)typeShape).ValueType),
		};

		if (typeof(TKey) != typeof(string))
		{
			schema["description"] = $"This object uses {typeof(TKey)} values as its keys instead of strings.";
		}

		return schema;
	}

	/// <inheritdoc/>
	public override async ValueTask<bool> SkipToIndexValueAsync(MessagePackAsyncReader reader, object? index, SerializationContext context)
	{
		if (index is null)
		{
			return false;
		}

		TKey desiredKey = (TKey)index;

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

		int count;
		while (streamingReader.TryReadMapHeader(out count).NeedsMoreBytes())
		{
			streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
		}

		for (int i = 0; i < count; i++)
		{
			reader.ReturnReader(ref streamingReader);
			TKey? key;
			if (keyConverter.PreferAsyncSerialization)
			{
				key = await keyConverter.ReadAsync(reader, context).ConfigureAwait(false);
			}
			else
			{
				await reader.BufferNextStructureAsync(context).ConfigureAwait(false);
				MessagePackReader syncReader = reader.CreateBufferedReader();
				key = keyConverter.Read(ref syncReader, context);
				reader.ReturnReader(ref syncReader);
			}

			if (EqualityComparer<TKey?>.Default.Equals(key, desiredKey))
			{
				return true;
			}

			// Skip the value since the key didn't match.
			streamingReader = reader.CreateStreamingReader();
			while (streamingReader.TrySkip(ref context).NeedsMoreBytes())
			{
				streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
			}
		}

		reader.ReturnReader(ref streamingReader);
		return false;
	}

	/// <summary>
	/// Reads a key and value pair.
	/// </summary>
	/// <param name="reader">The reader.</param>
	/// <param name="context"><inheritdoc cref="MessagePackConverter{T}.Read" path="/param[@name='context']"/></param>
	/// <param name="key">Receives the key.</param>
	/// <param name="value">Receives the value.</param>
	protected void ReadEntry(ref MessagePackReader reader, SerializationContext context, out TKey key, out TValue value)
	{
		key = keyConverter.Read(ref reader, context)!;
		if (key is null && disallowNullKeys)
		{
			ThrowDisallowedNullValue(true);
		}

		value = valueConverter.Read(ref reader, context)!;
		if (value is null && disallowNullValues)
		{
			ThrowDisallowedNullValue(false);
		}

		[DoesNotReturn]
		static void ThrowDisallowedNullValue(bool key)
		{
			string message = key ? "A disallowed null key was encountered in the dictionary." : "A disallowed null value was encountered in the dictionary.";
			throw new MessagePackSerializationException(message) { Code = MessagePackSerializationException.ErrorCode.DisallowedNullValue };
		}
	}

	/// <summary>
	/// Reads a key and value pair.
	/// </summary>
	/// <param name="reader">The reader.</param>
	/// <param name="context"><inheritdoc cref="MessagePackConverter{T}.Read" path="/param[@name='context']"/></param>
	/// <returns>The key=value pair.</returns>
	protected async ValueTask<KeyValuePair<TKey, TValue>> ReadEntryAsync(MessagePackAsyncReader reader, SerializationContext context)
	{
		TKey? key = await keyConverter.ReadAsync(reader, context).ConfigureAwait(false);
		TValue? value = await valueConverter.ReadAsync(reader, context).ConfigureAwait(false);
		return new(key!, value!);
	}
}

/// <summary>
/// Serializes and deserializes an mutable dictionary.
/// </summary>
/// <inheritdoc cref="DictionaryConverter{TDictionary, TKey, TValue}"/>
/// <param name="getReadable"><inheritdoc cref="DictionaryConverter{TDictionary, TKey, TValue}" path="/param[@name='getReadable']"/></param>
/// <param name="keyConverter"><inheritdoc cref="DictionaryConverter{TDictionary, TKey, TValue}" path="/param[@name='keyConverter']"/></param>
/// <param name="valueConverter"><inheritdoc cref="DictionaryConverter{TDictionary, TKey, TValue}" path="/param[@name='valueConverter']"/></param>
/// <param name="addEntry">The delegate that adds an entry to the dictionary.</param>
/// <param name="ctor">The constructor for the dictionary type.</param>
/// <param name="collectionConstructionOptions">A template for options to pass to the <paramref name="ctor"/>.</param>
/// <param name="disallowNullKeys"><inheritdoc cref="DictionaryConverter{TDictionary, TKey, TValue}" path="/param[@name='disallowNullKeys']"/></param>
/// <param name="disallowNullValues"><inheritdoc cref="DictionaryConverter{TDictionary, TKey, TValue}" path="/param[@name='disallowNullValues']"/></param>
internal class MutableDictionaryConverter<TDictionary, TKey, TValue>(
	Func<TDictionary, IReadOnlyDictionary<TKey, TValue>> getReadable,
	MessagePackConverter<TKey> keyConverter,
	MessagePackConverter<TValue> valueConverter,
	DictionaryInserter<TDictionary, TKey, TValue> addEntry,
	MutableCollectionConstructor<TKey, TDictionary> ctor,
	CollectionConstructionOptions<TKey> collectionConstructionOptions,
	bool disallowNullKeys,
	bool disallowNullValues) : DictionaryConverter<TDictionary, TKey, TValue>(getReadable, keyConverter, valueConverter, disallowNullKeys, disallowNullValues), IDeserializeInto<TDictionary>
	where TKey : notnull
{
	/// <inheritdoc/>
	public override bool PreferAsyncSerialization => this.ElementPrefersAsyncSerialization;

	/// <inheritdoc/>
#pragma warning disable NBMsgPack031 // Exactly one structure - analyzer cannot see through this.method calls.
	public override TDictionary? Read(ref MessagePackReader reader, SerializationContext context)
	{
		if (reader.TryReadNil())
		{
			return default;
		}

		return this.DeserializeInto(
			ref reader,
			static (s, size) => s.ctor(s.options.WithCapacity(size)),
			(ctor, options: collectionConstructionOptions),
			context);
	}
#pragma warning restore NBMsgPack03

	/// <inheritdoc/>
	public override async ValueTask<TDictionary?> ReadAsync(MessagePackAsyncReader reader, SerializationContext context)
	{
		MessagePackStreamingReader streamingReader = reader.CreateStreamingReader();
		bool success;
		while (streamingReader.TryReadNil(out success).NeedsMoreBytes())
		{
			streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
		}

		reader.ReturnReader(ref streamingReader);
		if (success)
		{
			return default;
		}

		return await this.DeserializeIntoAsync(
			reader,
			static (s, size) => s.ctor(s.options.WithCapacity(size)),
			(ctor, options: collectionConstructionOptions),
			context).ConfigureAwait(false);
	}

	/// <inheritdoc/>
	public void DeserializeInto(ref MessagePackReader reader, ref TDictionary collection, SerializationContext context)
		=> collection = this.DeserializeInto(ref reader, static (s, _) => s, collection, context);

	/// <inheritdoc/>
	public ValueTask<TDictionary> DeserializeIntoAsync(MessagePackAsyncReader reader, TDictionary collection, SerializationContext context)
		=> this.DeserializeIntoAsync(reader, static (s, _) => s, collection, context);

	private TDictionary DeserializeInto<TState>(ref MessagePackReader reader, Func<TState, int, TDictionary> getCollection, TState state, SerializationContext context)
	{
		context.DepthStep();
		int count = reader.ReadMapHeader();
		TDictionary collection = getCollection(state, count);
		for (int i = 0; i < count; i++)
		{
			this.ReadEntry(ref reader, context, out TKey key, out TValue value);
			addEntry(ref collection, key, value);
		}

		return collection;
	}

	private async ValueTask<TDictionary> DeserializeIntoAsync<TState>(MessagePackAsyncReader reader, Func<TState, int, TDictionary> getCollection, TState state, SerializationContext context)
	{
		context.DepthStep();

		TDictionary collection;
		if (this.ElementPrefersAsyncSerialization)
		{
			MessagePackStreamingReader streamingReader = reader.CreateStreamingReader();
			int count;
			while (streamingReader.TryReadMapHeader(out count).NeedsMoreBytes())
			{
				streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
			}

			collection = getCollection(state, count);
			reader.ReturnReader(ref streamingReader);
			for (int i = 0; i < count; i++)
			{
				KeyValuePair<TKey, TValue> keyValuePair = await this.ReadEntryAsync(reader, context).ConfigureAwait(false);
				addEntry(ref collection, keyValuePair.Key, keyValuePair.Value);
			}
		}
		else
		{
			await reader.BufferNextStructureAsync(context).ConfigureAwait(false);
			MessagePackReader syncReader = reader.CreateBufferedReader();
			int count = syncReader.ReadMapHeader();
			collection = getCollection(state, count);
			for (int i = 0; i < count; i++)
			{
				this.ReadEntry(ref syncReader, context, out TKey key, out TValue value);
				addEntry(ref collection, key, value);
			}

			reader.ReturnReader(ref syncReader);
		}

		return collection;
	}
}

/// <summary>
/// Serializes and deserializes an immutable dictionary.
/// </summary>
/// <inheritdoc cref="DictionaryConverter{TDictionary, TKey, TValue}"/>
/// <param name="getReadable"><inheritdoc cref="DictionaryConverter{TDictionary, TKey, TValue}" path="/param[@name='getReadable']"/></param>
/// <param name="keyConverter"><inheritdoc cref="DictionaryConverter{TDictionary, TKey, TValue}" path="/param[@name='keyConverter']"/></param>
/// <param name="valueConverter"><inheritdoc cref="DictionaryConverter{TDictionary, TKey, TValue}" path="/param[@name='valueConverter']"/></param>
/// <param name="ctor">A dictionary initializer that constructs from a span of entries.</param>
/// <param name="collectionConstructionOptions">A template for options to pass to the <paramref name="ctor"/>.</param>
/// <param name="disallowNullKeys"><inheritdoc cref="DictionaryConverter{TDictionary, TKey, TValue}" path="/param[@name='disallowNullKeys']"/></param>
/// <param name="disallowNullValues"><inheritdoc cref="DictionaryConverter{TDictionary, TKey, TValue}" path="/param[@name='disallowNullValues']"/></param>
internal class ImmutableDictionaryConverter<TDictionary, TKey, TValue>(
	Func<TDictionary, IReadOnlyDictionary<TKey, TValue>> getReadable,
	MessagePackConverter<TKey> keyConverter,
	MessagePackConverter<TValue> valueConverter,
	ParameterizedCollectionConstructor<TKey, KeyValuePair<TKey, TValue>, TDictionary> ctor,
	CollectionConstructionOptions<TKey> collectionConstructionOptions,
	bool disallowNullKeys,
	bool disallowNullValues) : DictionaryConverter<TDictionary, TKey, TValue>(getReadable, keyConverter, valueConverter, disallowNullKeys, disallowNullValues)
	where TKey : notnull
{
	/// <inheritdoc/>
	public override TDictionary? Read(ref MessagePackReader reader, SerializationContext context)
	{
		if (reader.TryReadNil())
		{
			return default;
		}

		context.DepthStep();
		int count = reader.ReadMapHeader();
		KeyValuePair<TKey, TValue>[] entries = ArrayPool<KeyValuePair<TKey, TValue>>.Shared.Rent(count);
		try
		{
			for (int i = 0; i < count; i++)
			{
				this.ReadEntry(ref reader, context, out TKey key, out TValue value);
				entries[i] = new(key, value);
			}

			return ctor(entries.AsSpan(0, count), collectionConstructionOptions);
		}
		finally
		{
			ArrayPool<KeyValuePair<TKey, TValue>>.Shared.Return(entries);
		}
	}
}
