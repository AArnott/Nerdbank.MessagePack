﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;

namespace ShapeShift.Converters;

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
internal class DictionaryConverter<TDictionary, TKey, TValue>(Func<TDictionary, IReadOnlyDictionary<TKey, TValue>> getReadable, Converter<TKey> keyConverter, Converter<TValue> valueConverter) : Converter<TDictionary>
	where TKey : notnull
{
	/// <summary>
	/// Gets a value indicating whether the key or value converters prefer async serialization.
	/// </summary>
	protected bool ElementPrefersAsyncSerialization => keyConverter.PreferAsyncSerialization || valueConverter.PreferAsyncSerialization;

	/// <inheritdoc/>
	public override TDictionary? Read(ref Reader reader, SerializationContext context)
	{
		if (reader.TryReadNull())
		{
			return default;
		}

		throw new NotSupportedException();
	}

	/// <inheritdoc/>
	public override void Write(ref Writer writer, in TDictionary? value, SerializationContext context)
	{
		if (value is null)
		{
			writer.WriteNull();
			return;
		}

		context.DepthStep();
		IReadOnlyDictionary<TKey, TValue> dictionary = getReadable(value);
		writer.WriteStartMap(dictionary.Count);
		bool isFirstElement = true;
		foreach (KeyValuePair<TKey, TValue> pair in dictionary)
		{
			if (isFirstElement)
			{
				isFirstElement = false;
			}
			else
			{
				writer.WriteMapPairSeparator();
			}

			TKey? entryKey = pair.Key;
			TValue? entryValue = pair.Value;

			keyConverter.Write(ref writer, entryKey, context);
			writer.WriteMapKeyValueSeparator();
			valueConverter.Write(ref writer, entryValue, context);
		}

		writer.WriteEndMap();
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
	[Experimental("NBMsgPackAsync")]
	public override async ValueTask<bool> SkipToIndexValueAsync(AsyncReader reader, object? index, SerializationContext context)
	{
		if (index is null)
		{
			return false;
		}

		var desiredKey = (TKey)index;

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

		int? count;
		while (streamingReader.TryReadMapHeader(out count).NeedsMoreBytes())
		{
			streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
		}

		if (count is null)
		{
			throw new NotImplementedException(); // TODO: review this.
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
				Reader syncReader = reader.CreateBufferedReader();
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
	/// <param name="context"><inheritdoc cref="Converter{T}.Read" path="/param[@name='context']"/></param>
	/// <param name="key">Receives the key.</param>
	/// <param name="value">Receives the value.</param>
	protected void ReadEntry(ref Reader reader, SerializationContext context, out TKey key, out TValue value)
	{
		key = keyConverter.Read(ref reader, context)!;
		reader.ReadMapKeyValueSeparator();
		value = valueConverter.Read(ref reader, context)!;
	}

	/// <summary>
	/// Reads a key and value pair.
	/// </summary>
	/// <param name="reader">The reader.</param>
	/// <param name="context"><inheritdoc cref="Converter{T}.Read" path="/param[@name='context']"/></param>
	/// <returns>The key=value pair.</returns>
	[Experimental("NBMsgPackAsync")]
	protected async ValueTask<KeyValuePair<TKey, TValue>> ReadEntryAsync(AsyncReader reader, SerializationContext context)
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
/// <param name="ctor">The default constructor for the dictionary type.</param>
internal class MutableDictionaryConverter<TDictionary, TKey, TValue>(
	Func<TDictionary, IReadOnlyDictionary<TKey, TValue>> getReadable,
	Converter<TKey> keyConverter,
	Converter<TValue> valueConverter,
	Setter<TDictionary, KeyValuePair<TKey, TValue>> addEntry,
	Func<TDictionary> ctor) : DictionaryConverter<TDictionary, TKey, TValue>(getReadable, keyConverter, valueConverter), IDeserializeInto<TDictionary>
	where TKey : notnull
{
	/// <inheritdoc/>
	public override bool PreferAsyncSerialization => this.ElementPrefersAsyncSerialization;

	/// <inheritdoc/>
#pragma warning disable NBMsgPack031 // Exactly one structure - analyzer cannot see through this.method calls.
	public override TDictionary? Read(ref Reader reader, SerializationContext context)
	{
		if (reader.TryReadNull())
		{
			return default;
		}

		TDictionary result = ctor();
		this.DeserializeInto(ref reader, ref result, context);
		return result;
	}
#pragma warning restore NBMsgPack03

	/// <inheritdoc/>
	[Experimental("NBMsgPackAsync")]
	public override async ValueTask<TDictionary?> ReadAsync(AsyncReader reader, SerializationContext context)
	{
		StreamingReader streamingReader = reader.CreateStreamingReader();
		bool success;
		while (streamingReader.TryReadNull(out success).NeedsMoreBytes())
		{
			streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
		}

		reader.ReturnReader(ref streamingReader);
		if (success)
		{
			return default;
		}

		TDictionary result = ctor();
		await this.DeserializeIntoAsync(reader, result, context).ConfigureAwait(false);
		return result;
	}

	/// <inheritdoc/>
	public void DeserializeInto(ref Reader reader, ref TDictionary collection, SerializationContext context)
	{
		context.DepthStep();
		int? count = reader.ReadStartMap();
		bool isFirstElement = true;
		for (int i = 0; i < count || (count is null && reader.TryAdvanceToNextElement(ref isFirstElement)); i++)
		{
			this.ReadEntry(ref reader, context, out TKey key, out TValue value);
			addEntry(ref collection, new KeyValuePair<TKey, TValue>(key, value));
		}
	}

	/// <inheritdoc/>
	[Experimental("NBMsgPackAsync")]
	public async ValueTask DeserializeIntoAsync(AsyncReader reader, TDictionary collection, SerializationContext context)
	{
		context.DepthStep();

		if (this.ElementPrefersAsyncSerialization)
		{
			StreamingReader streamingReader = reader.CreateStreamingReader();
			int? count;
			while (streamingReader.TryReadMapHeader(out count).NeedsMoreBytes())
			{
				streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
			}

			reader.ReturnReader(ref streamingReader);
			bool isFirstElement = true;
			for (int i = 0; i < count || (count is null && await reader.TryAdvanceToNextElementAsync(isFirstElement).ConfigureAwait(false)); i++)
			{
				isFirstElement = false;
				addEntry(ref collection, await this.ReadEntryAsync(reader, context).ConfigureAwait(false));
			}
		}
		else
		{
			await reader.BufferNextStructureAsync(context).ConfigureAwait(false);
			Reader syncReader = reader.CreateBufferedReader();
			int? count = syncReader.ReadStartMap();
			bool isFirstElement = true;
			for (int i = 0; i < count || (count is null && syncReader.TryAdvanceToNextElement(ref isFirstElement)); i++)
			{
				this.ReadEntry(ref syncReader, context, out TKey key, out TValue value);
				addEntry(ref collection, new KeyValuePair<TKey, TValue>(key, value));
			}

			reader.ReturnReader(ref syncReader);
		}
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
internal class ImmutableDictionaryConverter<TDictionary, TKey, TValue>(
	Func<TDictionary, IReadOnlyDictionary<TKey, TValue>> getReadable,
	Converter<TKey> keyConverter,
	Converter<TValue> valueConverter,
	SpanConstructor<KeyValuePair<TKey, TValue>, TDictionary> ctor) : DictionaryConverter<TDictionary, TKey, TValue>(getReadable, keyConverter, valueConverter)
	where TKey : notnull
{
	/// <inheritdoc/>
	public override TDictionary? Read(ref Reader reader, SerializationContext context)
	{
		if (reader.TryReadNull())
		{
			return default;
		}

		context.DepthStep();
		int? count = reader.ReadStartMap();
		if (count.HasValue)
		{
			KeyValuePair<TKey, TValue>[] entries = ArrayPool<KeyValuePair<TKey, TValue>>.Shared.Rent(count.Value);
			try
			{
				for (int i = 0; i < count; i++)
				{
					this.ReadEntry(ref reader, context, out TKey key, out TValue value);
					entries[i] = new(key, value);
				}

				return ctor(entries.AsSpan(0, count.Value));
			}
			finally
			{
				ArrayPool<KeyValuePair<TKey, TValue>>.Shared.Return(entries);
			}
		}
		else
		{
			List<KeyValuePair<TKey, TValue>> entries = new();
			bool isFirstElement = true;
			while (reader.TryAdvanceToNextElement(ref isFirstElement))
			{
				this.ReadEntry(ref reader, context, out TKey key, out TValue value);
				entries.Add(new(key, value));
			}

#if NET
			return ctor(CollectionsMarshal.AsSpan(entries));
#else
			return ctor([.. entries]);
#endif
		}
	}
}

/// <summary>
/// Serializes and deserializes a dictionary that initializes from an enumerable of entries.
/// </summary>
/// <inheritdoc cref="DictionaryConverter{TDictionary, TKey, TValue}"/>
/// <param name="getReadable"><inheritdoc cref="DictionaryConverter{TDictionary, TKey, TValue}" path="/param[@name='getReadable']"/></param>
/// <param name="keyConverter"><inheritdoc cref="DictionaryConverter{TDictionary, TKey, TValue}" path="/param[@name='keyConverter']"/></param>
/// <param name="valueConverter"><inheritdoc cref="DictionaryConverter{TDictionary, TKey, TValue}" path="/param[@name='valueConverter']"/></param>
/// <param name="ctor">A dictionary initializer that constructs from an enumerable of entries.</param>
internal class EnumerableDictionaryConverter<TDictionary, TKey, TValue>(
	Func<TDictionary, IReadOnlyDictionary<TKey, TValue>> getReadable,
	Converter<TKey> keyConverter,
	Converter<TValue> valueConverter,
	Func<IEnumerable<KeyValuePair<TKey, TValue>>, TDictionary> ctor) : DictionaryConverter<TDictionary, TKey, TValue>(getReadable, keyConverter, valueConverter)
	where TKey : notnull
{
	/// <inheritdoc/>
	public override TDictionary? Read(ref Reader reader, SerializationContext context)
	{
		if (reader.TryReadNull())
		{
			return default;
		}

		context.DepthStep();
		int? count = reader.ReadStartMap();
		if (count.HasValue)
		{
			KeyValuePair<TKey, TValue>[] entries = ArrayPool<KeyValuePair<TKey, TValue>>.Shared.Rent(count.Value);
			try
			{
				for (int i = 0; i < count; i++)
				{
					this.ReadEntry(ref reader, context, out TKey key, out TValue value);
					entries[i] = new(key, value);
				}

				return ctor(entries.Take(count.Value));
			}
			finally
			{
				ArrayPool<KeyValuePair<TKey, TValue>>.Shared.Return(entries);
			}
		}
		else
		{
			List<KeyValuePair<TKey, TValue>> entries = new();
			bool isFirstElement = true;
			while (reader.TryAdvanceToNextElement(ref isFirstElement))
			{
				this.ReadEntry(ref reader, context, out TKey key, out TValue value);
				entries.Add(new(key, value));
			}

			return ctor(entries);
		}
	}
}
