// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Microsoft;
using TypeShape;

namespace Nerdbank.MessagePack;

/// <summary>
/// Serializes .NET objects using the MessagePack format.
/// </summary>
public class MessagePackSerializer
{
	private static readonly FrozenDictionary<Type, IMessagePackConverter> PrimitiveConverters = new Dictionary<Type, IMessagePackConverter>()
	{
		{ typeof(byte), new ByteConverter() },
		{ typeof(ushort), new UInt16Converter() },
		{ typeof(uint), new UInt32Converter() },
		{ typeof(ulong), new UInt64Converter() },
		{ typeof(sbyte), new SByteConverter() },
		{ typeof(short), new Int16Converter() },
		{ typeof(int), new Int32Converter() },
		{ typeof(long), new Int64Converter() },
	}.ToFrozenDictionary();

	private readonly ConcurrentDictionary<Type, IMessagePackConverter> cachedConverters = new();

	public MessagePackConverter<T> CreateConverter<T>(ITypeShape<T> typeShape)
		=> (MessagePackConverter<T>)Requires.NotNull(typeShape).Accept(new StandardVisitor(this))!;

	public void Serialize<T>(IBufferWriter<byte> writer, T? value)
		where T : IShapeable<T>
	{
		MessagePackWriter msgpackWriter = new(writer);
		this.Serialize(ref msgpackWriter, value);
		msgpackWriter.Flush();
	}

	public void Serialize<T>(ref MessagePackWriter writer, T? value)
		where T : IShapeable<T>
	{
		this.GetOrAddConverter<T>().Serialize(ref writer, ref value);
	}

	public T? Deserialize<T>(ReadOnlySequence<byte> buffer)
		where T : IShapeable<T>
	{
		MessagePackReader reader = new(buffer);
		return this.Deserialize<T>(reader);
	}

	public T? Deserialize<T>(MessagePackReader reader)
		where T : IShapeable<T>
	{
		return this.GetOrAddConverter<T>().Deserialize(ref reader);
	}

	internal MessagePackConverter<T> GetOrAddConverter<T>(ITypeShape<T> shape)
	{
		if (this.TryGetConverter<T>(out MessagePackConverter<T>? converter))
		{
			return converter;
		}

		converter = this.CreateConverter(shape);
		this.RegisterConverter(converter);

		return converter;
	}

	internal MessagePackConverter<T> GetOrAddConverter<T>()
		where T : IShapeable<T> => this.GetOrAddConverter(T.GetShape());

	internal bool TryGetConverter<T>([NotNullWhen(true)] out MessagePackConverter<T>? converter)
	{
		if (PrimitiveConverters.TryGetValue(typeof(T), out IMessagePackConverter? candidate))
		{
			converter = (MessagePackConverter<T>)candidate;
			return true;
		}

		if (this.cachedConverters.TryGetValue(typeof(T), out candidate))
		{
			converter = (MessagePackConverter<T>)candidate;
			return true;
		}

		converter = null;
		return false;
	}

	internal void RegisterConverter<T>(MessagePackConverter<T> converter)
	{
		this.cachedConverters[typeof(T)] = converter;
	}

	internal void RegisterConverters(IEnumerable<KeyValuePair<Type, IMessagePackConverter>> converters)
	{
		foreach (KeyValuePair<Type, IMessagePackConverter> pair in converters)
		{
			this.cachedConverters.TryAdd(pair.Key, pair.Value);
		}
	}
}
