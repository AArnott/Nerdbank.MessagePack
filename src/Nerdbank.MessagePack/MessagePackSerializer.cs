// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

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
	/// <summary>
	/// A thread-local, recyclable array that may be used for short bursts of code.
	/// </summary>
	[ThreadStatic]
	private static byte[]? scratchArray;

	private static readonly FrozenDictionary<Type, object> PrimitiveConverters = new Dictionary<Type, object>()
	{
		{ typeof(char), new CharConverter() },
		{ typeof(byte), new ByteConverter() },
		{ typeof(ushort), new UInt16Converter() },
		{ typeof(uint), new UInt32Converter() },
		{ typeof(ulong), new UInt64Converter() },
		{ typeof(sbyte), new SByteConverter() },
		{ typeof(short), new Int16Converter() },
		{ typeof(int), new Int32Converter() },
		{ typeof(long), new Int64Converter() },
		{ typeof(BigInteger), new BigIntegerConverter() },
		{ typeof(Int128), new Int128Converter() },
		{ typeof(UInt128), new UInt128Converter() },
		{ typeof(string), new StringConverter() },
		{ typeof(bool), new BooleanConverter() },
		{ typeof(Version), new VersionConverter() },
		{ typeof(Uri), new UriConverter() },
		{ typeof(Half), new HalfConverter() },
		{ typeof(float), new SingleConverter() },
		{ typeof(double), new DoubleConverter() },
		{ typeof(decimal), new DecimalConverter() },
		{ typeof(TimeOnly), new TimeOnlyConverter() },
		{ typeof(DateOnly), new DateOnlyConverter() },
		{ typeof(DateTime), new DateTimeConverter() },
		{ typeof(DateTimeOffset), new DateTimeOffsetConverter() },
		{ typeof(TimeSpan), new TimeSpanConverter() },
		{ typeof(Guid), new GuidConverter() },
		{ typeof(byte[]), new ByteArrayConverter() },
	}.ToFrozenDictionary();

	private readonly ConcurrentDictionary<Type, object> cachedConverters = new();

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

	/// <summary>
	/// Gets a new <see cref="SerializationContext"/> for a new serialization job.
	/// </summary>
	protected SerializationContext StartingContext => new(this.MaxDepth);

	/// <summary>
	/// Serializes a given value to a byte array.
	/// </summary>
	/// <typeparam name="T">The type of value to be serialized. This must be able to disclose its own shape.</typeparam>
	/// <param name="value">The value to be serialized.</param>
	/// <returns>The byte array.</returns>
	public byte[] Serialize<T>(T? value)
		where T : IShapeable<T> => this.Serialize(value, T.GetShape());

	/// <summary>
	/// Serializes a given value to a byte array.
	/// </summary>
	/// <typeparam name="T">The type of value to be serialized.</typeparam>
	/// <param name="value">The value to be serialized.</param>
	/// <param name="shape">The shape of the type.</param>
	/// <returns>The byte array.</returns>
	public byte[] Serialize<T>(T? value, ITypeShape<T> shape)
	{
		byte[]? array = scratchArray;
		if (array == null)
		{
			scratchArray = array = new byte[65536];
		}

		MessagePackWriter writer = new(SequencePool.Shared, array);
		this.Serialize(ref writer, value, shape);
		return writer.FlushAndGetArray();
	}

	/// <inheritdoc cref="Serialize{T, TProvider}(IBufferWriter{byte}, T)"/>
	public void Serialize<T>(IBufferWriter<byte> writer, T? value)
		where T : IShapeable<T> => this.Serialize(writer, value, T.GetShape());

	/// <inheritdoc cref="Serialize{T}(ref MessagePackWriter, T)"/>
	/// <param name="writer">The buffer writer to serialize to.</param>
	/// <param name="value"><inheritdoc cref="Serialize{T}(ref MessagePackWriter, T)" path="/param[@name='value']"/></param>
	public void Serialize<T, TProvider>(IBufferWriter<byte> writer, T? value)
		where TProvider : IShapeable<T> => this.Serialize(writer, value, TProvider.GetShape());

	/// <inheritdoc cref="Serialize{T}(ref MessagePackWriter, T)"/>
	/// <param name="writer">The buffer writer to serialize to.</param>
	/// <param name="value"><inheritdoc cref="Serialize{T}(ref MessagePackWriter, T)" path="/param[@name='value']"/></param>
	/// <param name="shape">The shape of <typeparamref name="T"/>.</param>
	public void Serialize<T>(IBufferWriter<byte> writer, T? value, ITypeShape<T> shape)
	{
		MessagePackWriter msgpackWriter = new(writer);
		this.Serialize(ref msgpackWriter, value, shape);
		msgpackWriter.Flush();
	}

	/// <inheritdoc cref="Serialize{T, TProvider}(ref MessagePackWriter, T)"/>
	public void Serialize<T>(ref MessagePackWriter writer, T? value)
		where T : IShapeable<T> => this.Serialize(ref writer, value, T.GetShape());

	/// <summary>
	/// Serializes a value using the given <see cref="MessagePackWriter"/>.
	/// </summary>
	/// <typeparam name="T">The type to be serialized.</typeparam>
	/// <typeparam name="TProvider">The shape provider of <typeparamref name="T"/>. This may be the same as <typeparamref name="T"/> when the data type is attributed with <see cref="GenerateShapeAttribute"/>, or it may be another "witness" partial class that was annotated with <see cref="GenerateShapeAttribute{T}"/> where T for the attribute is the same as the <typeparamref name="T"/> used here.</typeparam>
	/// <param name="writer">The msgpack writer to use.</param>
	/// <param name="value">The value to serialize.</param>
	public void Serialize<T, TProvider>(ref MessagePackWriter writer, T? value)
		where TProvider : IShapeable<T> => this.Serialize<T>(ref writer, value, TProvider.GetShape());

	/// <summary>
	/// Serializes a value using the given <see cref="MessagePackWriter"/>.
	/// </summary>
	/// <typeparam name="T">The type to be serialized.</typeparam>
	/// <param name="writer">The msgpack writer to use.</param>
	/// <param name="value">The value to serialize.</param>
	/// <param name="shape">The shape of <typeparamref name="T"/>.</param>
	public void Serialize<T>(ref MessagePackWriter writer, T? value, ITypeShape<T> shape) => this.GetOrAddConverter(shape).Serialize(ref writer, ref value, this.StartingContext);

	/// <inheritdoc cref="Deserialize{T, TProvider}(ReadOnlySequence{byte})"/>
	public T? Deserialize<T>(byte[] buffer)
		where T : IShapeable<T> => this.Deserialize(new ReadOnlySequence<byte>(buffer), T.GetShape());

	/// <param name="buffer">The msgpack to deserialize from.</param>
	/// <inheritdoc cref="Deserialize{T}(ref MessagePackReader)"/>
	public T? Deserialize<T, TProvider>(byte[] buffer)
		where TProvider : IShapeable<T> => this.Deserialize(new ReadOnlySequence<byte>(buffer), TProvider.GetShape());

	/// <param name="buffer">The msgpack to deserialize from.</param>
	/// <param name="shape">The shape of the type to deserialize.</param>
	/// <inheritdoc cref="Deserialize{T}(ref MessagePackReader)"/>
	public T? Deserialize<T>(byte[] buffer, ITypeShape<T> shape)
		=> this.Deserialize(new ReadOnlySequence<byte>(buffer), shape);

	/// <inheritdoc cref="Deserialize{T, TProvider}(ReadOnlySequence{byte})"/>
	public T? Deserialize<T>(ReadOnlySequence<byte> buffer)
		where T : IShapeable<T> => this.Deserialize(buffer, T.GetShape());

	/// <param name="buffer">The msgpack to deserialize from.</param>
	/// <inheritdoc cref="Deserialize{T}(ref MessagePackReader)"/>
	public T? Deserialize<T, TProvider>(ReadOnlySequence<byte> buffer)
		where TProvider : IShapeable<T> => this.Deserialize(buffer, TProvider.GetShape());

	/// <inheritdoc cref="Deserialize{T}(ref MessagePackReader)"/>
	/// <param name="buffer">The msgpack to deserialize from.</param>
	/// <param name="shape">The shape of the type to deserialize.</param>
	public T? Deserialize<T>(ReadOnlySequence<byte> buffer, ITypeShape<T> shape)
	{
		MessagePackReader reader = new(buffer);
		return this.Deserialize(ref reader, shape);
	}

	/// <inheritdoc cref="Deserialize{T, TProvider}(ref MessagePackReader)"/>
	public T? Deserialize<T>(ref MessagePackReader reader)
		where T : IShapeable<T> => this.Deserialize(ref reader, T.GetShape());

	/// <summary>
	/// Deserializes a value from a <see cref="MessagePackReader"/>.
	/// </summary>
	/// <typeparam name="T">The type of value to deserialize.</typeparam>
	/// <typeparam name="TProvider"><inheritdoc cref="Serialize{T, TProvider}(ref MessagePackWriter, T)" path="/typeparam[@name='TProvider']"/></typeparam>
	/// <param name="reader">The msgpack reader to deserialize from.</param>
	/// <returns>The deserialized value.</returns>
	public T? Deserialize<T, TProvider>(ref MessagePackReader reader)
		where TProvider : IShapeable<T> => this.Deserialize(ref reader, TProvider.GetShape());

	/// <summary>
	/// Deserializes a value from a <see cref="MessagePackReader"/>.
	/// </summary>
	/// <typeparam name="T">The type of value to deserialize.</typeparam>
	/// <param name="reader">The msgpack reader to deserialize from.</param>
	/// <param name="shape">The shape of <typeparamref name="T"/>.</param>
	/// <returns>The deserialized value.</returns>
	public T? Deserialize<T>(ref MessagePackReader reader, ITypeShape<T> shape) => this.GetOrAddConverter(shape).Deserialize(ref reader, this.StartingContext);

	/// <summary>
	/// Gets a converter for the given type shape.
	/// An existing converter is reused if one is found in the cache.
	/// If a converter must be created, it is added to the cache for lookup next time.
	/// </summary>
	/// <typeparam name="T">The data type to convert.</typeparam>
	/// <param name="shape">The shape of the type to convert.</param>
	/// <returns>A msgpack converter.</returns>
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

	/// <summary>
	/// Gets a converter for a type that self-describes its shape.
	/// An existing converter is reused if one is found in the cache.
	/// If a converter must be created, it is added to the cache for lookup next time.
	/// </summary>
	/// <typeparam name="T">The data type to convert.</typeparam>
	/// <returns>A msgpack converter.</returns>
	internal MessagePackConverter<T> GetOrAddConverter<T>()
		where T : IShapeable<T> => this.GetOrAddConverter(T.GetShape());

	/// <summary>
	/// Searches our static and instance cached converters for a converter for the given type.
	/// </summary>
	/// <typeparam name="T">The data type to be converted.</typeparam>
	/// <param name="converter">Receives the converter instance if one exists.</param>
	/// <returns><see langword="true"/> if a converter was found to already exist; otherwise <see langword="false" />.</returns>
	internal bool TryGetConverter<T>([NotNullWhen(true)] out MessagePackConverter<T>? converter)
	{
		if (this.TryGetConverter(typeof(T), out object? candidate))
		{
			converter = (MessagePackConverter<T>)candidate;
			return true;
		}

		converter = null;
		return false;
	}

	/// <summary>
	/// Searches our static and instance cached converters for a converter for the given type.
	/// </summary>
	/// <param name="type">The data type to be converted.</param>
	/// <param name="converter">Receives the converter instance if one exists.</param>
	/// <returns><see langword="true"/> if a converter was found to already exist; otherwise <see langword="false" />.</returns>
	internal bool TryGetConverter(Type type, [NotNullWhen(true)] out object? converter)
	{
		// Query our cache before the static converters to allow overrides of the built-in converters.
		// For example this may allow for string interning or other optimizations.
		if (this.cachedConverters.TryGetValue(type, out converter))
		{
			return true;
		}

		if (PrimitiveConverters.TryGetValue(type, out converter))
		{
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
	internal void RegisterConverter<T>(MessagePackConverter<T> converter)
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
	internal void RegisterConverters(IEnumerable<KeyValuePair<Type, object>> converters)
	{
		foreach (KeyValuePair<Type, object> pair in converters)
		{
			this.cachedConverters.TryAdd(pair.Key, pair.Value);
		}
	}

	/// <summary>
	/// Synthesizes a <see cref="MessagePackConverter{T}"/> for a type with the given shape.
	/// </summary>
	/// <typeparam name="T">The data type that should be serializable.</typeparam>
	/// <param name="typeShape">The shape of the data type.</param>
	/// <returns>The msgpack converter.</returns>
	private MessagePackConverter<T> CreateConverter<T>(ITypeShape<T> typeShape)
	{
		StandardVisitor visitor = new(this);
		MessagePackConverter<T> result = (MessagePackConverter<T>)typeShape.Accept(visitor)!;

		// Cache all the converters that have been generated to support the one that our caller wants.
		this.RegisterConverters(visitor.GeneratedConverters.Where(kv => kv.Value is not null)!);

		return result;
	}
}
