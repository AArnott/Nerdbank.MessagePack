// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using TypeShape;

namespace Nerdbank.MessagePack;

/// <summary>
/// Serializes .NET objects using the MessagePack format.
/// </summary>
/// <devremarks>
/// <para>
/// This class may declare properties that customize how msgpack serialization is performed.
/// These properties must use <see langword="init"/> accessors to prevent modification after construction,
/// since there is no means to replace converters once they are created.
/// </para>
/// <para>
/// If the ability to add custom converters is exposed publicly, such a method should throw once generated converters have started being generated
/// because generated ones have already locked-in their dependencies.
/// </para>
/// </devremarks>
public record MessagePackSerializer
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
		{ typeof(string), new StringConverter() },
		{ typeof(bool), new BooleanConverter() },
		{ typeof(float), new SingleConverter() },
		{ typeof(double), new DoubleConverter() },
	}.ToFrozenDictionary();

	private readonly ConcurrentDictionary<Type, IMessagePackConverter> cachedConverters = new();

	/// <summary>
	/// Gets the format to use when serializing multi-dimensional arrays.
	/// </summary>
	public MultiDimensionalArrayFormat MultiDimensionalArrayFormat { get; init; } = MultiDimensionalArrayFormat.Nested;

	/// <summary>
	/// Gets the maximum depth of the object graph to serialize or deserialize.
	/// </summary>
	/// <remarks>
	/// Exceeding this depth will result in a <see cref="MessagePackSerializationException"/> being thrown.
	/// </remarks>
	public int MaxDepth { get; init; } = 64;

	/// <inheritdoc cref="Serialize{T}(ref MessagePackWriter, T)"/>
	/// <param name="writer">The buffer writer to serialize to.</param>
	/// <param name="value"><inheritdoc cref="Serialize{T}(ref MessagePackWriter, T)" path="/param[@name='value']"/></param>
	public void Serialize<T>(IBufferWriter<byte> writer, T? value)
		where T : IShapeable<T>
	{
		MessagePackWriter msgpackWriter = new(writer);
		this.Serialize(ref msgpackWriter, value);
		msgpackWriter.Flush();
	}

	/// <summary>
	/// Serializes a value using the given <see cref="MessagePackWriter"/>.
	/// </summary>
	/// <typeparam name="T">The type to be serialized.</typeparam>
	/// <param name="writer">The msgpack writer to use.</param>
	/// <param name="value">The value to serialize.</param>
	public void Serialize<T>(ref MessagePackWriter writer, T? value)
		where T : IShapeable<T>
	{
		this.GetOrAddConverter<T>().Serialize(ref writer, ref value, new SerializationContext(this.MaxDepth));
	}

	/// <param name="buffer">The msgpack to deserialize from.</param>
	/// <inheritdoc cref="Deserialize{T}(MessagePackReader)"/>
	public T? Deserialize<T>(ReadOnlySequence<byte> buffer)
		where T : IShapeable<T>
	{
		MessagePackReader reader = new(buffer);
		return this.Deserialize<T>(reader);
	}

	/// <summary>
	/// Deserializes a value from a <see cref="MessagePackReader"/>.
	/// </summary>
	/// <typeparam name="T">The type of value to deserialize.</typeparam>
	/// <param name="reader">The msgpack reader to deserialize from.</param>
	/// <returns>The deserialized value.</returns>
	public T? Deserialize<T>(MessagePackReader reader)
		where T : IShapeable<T>
	{
		return this.GetOrAddConverter<T>().Deserialize(ref reader, new SerializationContext(this.MaxDepth));
	}

	/// <summary>
	/// Gets a converter for the given type shape.
	/// An existing converter is reused if one is found in the cache.
	/// If a converter must be created, it is added to the cache for lookup next time.
	/// </summary>
	/// <typeparam name="T">The data type to convert.</typeparam>
	/// <param name="shape">The shape of the type to convert.</param>
	/// <returns>A msgpack converter.</returns>
	internal IMessagePackConverter<T> GetOrAddConverter<T>(ITypeShape<T> shape)
	{
		if (this.TryGetConverter<T>(out IMessagePackConverter<T>? converter))
		{
			return converter;
		}

		converter = this.CreateConverter(shape);
		this.RegisterConverter(converter);

		return converter;
	}

	/// <summary>
	/// Gets a converter for a type that self-describes its shape.
	/// An existing converter is reused if one is found in the cache.
	/// If a converter must be created, it is added to the cache for lookup next time.
	/// </summary>
	/// <typeparam name="T">The data type to convert.</typeparam>
	/// <returns>A msgpack converter.</returns>
	internal IMessagePackConverter<T> GetOrAddConverter<T>()
		where T : IShapeable<T> => this.GetOrAddConverter(T.GetShape());

	/// <summary>
	/// Searches our static and instance cached converters for a converter for the given type.
	/// </summary>
	/// <typeparam name="T">The data type to be converted.</typeparam>
	/// <param name="converter">Receives the converter instance if one exists.</param>
	/// <returns><see langword="true"/> if a converter was found to already exist; otherwise <see langword="false" />.</returns>
	internal bool TryGetConverter<T>([NotNullWhen(true)] out IMessagePackConverter<T>? converter)
	{
		// Query our cache before the static converters to allow overrides of the built-in converters.
		// For example this may allow for string interning or other optimizations.
		if (this.cachedConverters.TryGetValue(typeof(T), out IMessagePackConverter? candidate))
		{
			converter = (IMessagePackConverter<T>)candidate;
			return true;
		}

		if (PrimitiveConverters.TryGetValue(typeof(T), out candidate))
		{
			converter = (IMessagePackConverter<T>)candidate;
			return true;
		}

		converter = null;
		return false;
	}

	/// <summary>
	/// Stores a converter in the cache for later reuse.
	/// </summary>
	/// <typeparam name="T">The convertible type.</typeparam>
	/// <param name="converter">The converter.</param>
	/// <remarks>
	/// If a converter for the data type has already been cached, this method does nothing.
	/// </remarks>
	internal void RegisterConverter<T>(IMessagePackConverter<T> converter)
	{
		this.cachedConverters.TryAdd(typeof(T), converter);
	}

	/// <summary>
	/// Stores a set of converters in the cache for later reuse.
	/// </summary>
	/// <param name="converters">The converters to store.</param>
	/// <remarks>
	/// Any collisions with existing converters are resolved in favor of the original converters.
	/// </remarks>
	internal void RegisterConverters(IEnumerable<KeyValuePair<Type, IMessagePackConverter>> converters)
	{
		foreach (KeyValuePair<Type, IMessagePackConverter> pair in converters)
		{
			this.cachedConverters.TryAdd(pair.Key, pair.Value);
		}
	}

	/// <summary>
	/// Synthesizes a <see cref="IMessagePackConverter{T}"/> for a type with the given shape.
	/// </summary>
	/// <typeparam name="T">The data type that should be serializable.</typeparam>
	/// <param name="typeShape">The shape of the data type.</param>
	/// <returns>The msgpack converter.</returns>
	private IMessagePackConverter<T> CreateConverter<T>(ITypeShape<T> typeShape)
		=> (IMessagePackConverter<T>)typeShape.Accept(new StandardVisitor(this))!;
}
