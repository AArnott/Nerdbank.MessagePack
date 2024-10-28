// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

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
internal class DictionaryConverter<TDictionary, TKey, TValue>(Func<TDictionary, IReadOnlyDictionary<TKey, TValue>> getReadable, IMessagePackConverter<TKey> keyConverter, IMessagePackConverter<TValue> valueConverter) : IMessagePackConverter<TDictionary>
{
	/// <inheritdoc/>
	public override TDictionary? Deserialize(ref MessagePackReader reader)
	{
		if (reader.TryReadNil())
		{
			return default;
		}

		throw new NotSupportedException();
	}

	/// <inheritdoc/>
	public override void Serialize(ref MessagePackWriter writer, ref TDictionary? value)
	{
		if (value is null)
		{
			writer.WriteNil();
			return;
		}

		IReadOnlyDictionary<TKey, TValue> dictionary = getReadable(value);
		writer.WriteMapHeader(dictionary.Count);
		foreach (KeyValuePair<TKey, TValue> pair in dictionary)
		{
			TKey? entryKey = pair.Key;
			TValue? entryValue = pair.Value;

			keyConverter.Serialize(ref writer, ref entryKey);
			valueConverter.Serialize(ref writer, ref entryValue);
		}
	}

	/// <summary>
	/// Reads a key and value pair.
	/// </summary>
	/// <param name="reader">The reader.</param>
	/// <param name="key">Receives the key.</param>
	/// <param name="value">Receives the value.</param>
	protected void ReadEntry(ref MessagePackReader reader, out TKey? key, out TValue? value)
	{
		key = keyConverter.Deserialize(ref reader);
		value = valueConverter.Deserialize(ref reader);
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
	IMessagePackConverter<TKey> keyConverter,
	IMessagePackConverter<TValue> valueConverter,
	Setter<TDictionary, KeyValuePair<TKey, TValue>> addEntry,
	Func<TDictionary> ctor) : DictionaryConverter<TDictionary, TKey, TValue>(getReadable, keyConverter, valueConverter)
{
	/// <inheritdoc/>
	public override TDictionary? Deserialize(ref MessagePackReader reader)
	{
		if (reader.TryReadNil())
		{
			return default;
		}

		TDictionary result = ctor();
		int count = reader.ReadMapHeader();
		for (int i = 0; i < count; i++)
		{
			this.ReadEntry(ref reader, out TKey? key, out TValue? value);
			addEntry(ref result, new KeyValuePair<TKey, TValue>(key!, value!));
		}

		return result;
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
	IMessagePackConverter<TKey> keyConverter,
	IMessagePackConverter<TValue> valueConverter,
	SpanConstructor<KeyValuePair<TKey, TValue>, TDictionary> ctor) : DictionaryConverter<TDictionary, TKey, TValue>(getReadable, keyConverter, valueConverter)
{
	/// <inheritdoc/>
	public override TDictionary? Deserialize(ref MessagePackReader reader)
	{
		if (reader.TryReadNil())
		{
			return default;
		}

		int count = reader.ReadMapHeader();
		KeyValuePair<TKey, TValue>[] entries = ArrayPool<KeyValuePair<TKey, TValue>>.Shared.Rent(count);
		try
		{
			for (int i = 0; i < count; i++)
			{
				this.ReadEntry(ref reader, out TKey? key, out TValue? value);
				entries[i] = new(key!, value!);
			}

			return ctor(entries.AsSpan(0, count));
		}
		finally
		{
			ArrayPool<KeyValuePair<TKey, TValue>>.Shared.Return(entries);
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
	IMessagePackConverter<TKey> keyConverter,
	IMessagePackConverter<TValue> valueConverter,
	Func<IEnumerable<KeyValuePair<TKey, TValue>>, TDictionary> ctor) : DictionaryConverter<TDictionary, TKey, TValue>(getReadable, keyConverter, valueConverter)
{
	/// <inheritdoc/>
	public override TDictionary? Deserialize(ref MessagePackReader reader)
	{
		if (reader.TryReadNil())
		{
			return default;
		}

		int count = reader.ReadMapHeader();
		KeyValuePair<TKey, TValue>[] entries = ArrayPool<KeyValuePair<TKey, TValue>>.Shared.Rent(count);
		try
		{
			for (int i = 0; i < count; i++)
			{
				this.ReadEntry(ref reader, out TKey? key, out TValue? value);
				entries[i] = new(key!, value!);
			}

			return ctor(entries.Take(count));
		}
		finally
		{
			ArrayPool<KeyValuePair<TKey, TValue>>.Shared.Return(entries);
		}
	}
}
