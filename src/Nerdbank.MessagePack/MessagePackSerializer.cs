// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Numerics;
using System.Reflection;
using System.Text;
using Microsoft;

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
public partial record MessagePackSerializer
{
	private static readonly FrozenDictionary<Type, object> PrimitiveConverters = new Dictionary<Type, object>()
	{
		{ typeof(char), new CharConverter() },
		{ typeof(Rune), new RuneConverter() },
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

	private static readonly FrozenDictionary<Type, object> PrimitiveReferencePreservingConverters = PrimitiveConverters.ToFrozenDictionary(
		pair => pair.Key,
		pair => (object)((IMessagePackConverter)pair.Value).WrapWithReferencePreservation());

	private readonly object lazyInitCookie = new();

	private bool configurationLocked;

	private ConcurrentDictionary<Type, object>? cachedConverters;

	/// <summary>
	/// Gets the format to use when serializing multi-dimensional arrays.
	/// </summary>
	public MultiDimensionalArrayFormat MultiDimensionalArrayFormat { get; init; } = MultiDimensionalArrayFormat.Nested;

	/// <summary>
	/// Gets the transformation function to apply to property names before serializing them.
	/// </summary>
	/// <value>
	/// The default value is null, indicating that property names should be persisted exactly as they are declared in .NET.
	/// </value>
	public MessagePackNamingPolicy? PropertyNamingPolicy { get; init; }

	/// <summary>
	/// Gets a value indicating whether to serialize properties that are set to their default values.
	/// </summary>
	/// <value>The default value is <see langword="false" />.</value>
	/// <remarks>
	/// <para>
	/// By default, the serializer omits properties and fields that are set to their default values when serializing objects.
	/// This property can be used to override that behavior and serialize all properties and fields, regardless of their value.
	/// </para>
	/// <para>
	/// This property currently only impacts objects serialized as maps (i.e. types that are <em>not</em> using <see cref="KeyAttribute"/> on their members),
	/// but this could be expanded to truncate value arrays as well.
	/// </para>
	/// <para>
	/// Default values are assumed to be <c>default(TPropertyType)</c> except where overridden, as follows:
	/// <list type="bullet">
	///   <item><description>Primary constructor default parameter values. e.g. <c>record Person(int Age = 18)</c></description></item>
	///   <item><description>Properties or fields attributed with <see cref="System.ComponentModel.DefaultValueAttribute"/>. e.g. <c>[DefaultValue(18)] public int Age { get; set; }</c></description></item>
	/// </list>
	/// </para>
	/// </remarks>
	public bool SerializeDefaultValues { get; init; }

	/// <summary>
	/// Gets a value indicating whether to preserve reference equality when serializing objects.
	/// </summary>
	/// <value>The default value is <see langword="false" />.</value>
	/// <remarks>
	/// <para>
	/// When <see langword="false" />, if an object appears multiple times in a serialized object graph, it will be serialized at each location.
	/// This has two outcomes: redundant data leading to larger serialized payloads and the loss of reference equality when deserialized.
	/// This is the default behavior because it requires no msgpack extensions and is compatible with all msgpack readers.
	/// </para>
	/// <para>
	/// When <see langword="true"/>, every object is serialized normally the first time it appears in the object graph.
	/// Each subsequent type the object appears in the object graph, it is serialized as a reference to the first occurrence.
	/// This reference requires between 3-6 bytes of overhead per reference instead of whatever the object's by-value representation would have required.
	/// Upon deserialization, all objects that were shared across the object graph will also be shared across the deserialized object graph.
	/// Of course there will not be reference equality between the original and deserialized objects, but the deserialized objects will have reference equality with each other.
	/// This option utilizes a proprietary msgpack extension and can only be deserialized by libraries that understand this extension.
	/// There is a small perf penalty for this feature, but depending on the object graph it may turn out to improve performance due to avoiding redundant serializations.
	/// </para>
	/// <para>
	/// Reference cycles (where an object refers to itself or to another object that eventually refers back to it) are <em>not</em> supported in either mode.
	/// When this property is <see langword="true" />, an exception will be thrown when a cycle is detected.
	/// When this property is <see langword="false" />, a cycle will eventually result in a <see cref="StackOverflowException" /> being thrown.
	/// </para>
	/// </remarks>
	public bool PreserveReferences { get; init; }

	/// <summary>
	/// Gets the extension type codes to use for library-reserved extension types.
	/// </summary>
	/// <remarks>
	/// This property may be used to reassign the extension type codes for library-provided extension types
	/// in order to avoid conflicts with other libraries the application is using.
	/// </remarks>
	public LibraryReservedMessagePackExtensionTypeCode LibraryExtensionTypeCodes { get; init; } = LibraryReservedMessagePackExtensionTypeCode.Default;

	/// <summary>
	/// Gets the starting context to begin (de)serializations with.
	/// </summary>
	public SerializationContext StartingContext { get; init; } = new();

	/// <summary>
	/// Gets a value indicating whether hardware accelerated converters should be avoided.
	/// </summary>
	internal bool DisableHardwareAcceleration { get; init; }

	/// <summary>
	/// Gets all the converters this instance knows about so far.
	/// </summary>
	private ConcurrentDictionary<Type, object> CachedConverters
	{
		get
		{
			if (this.cachedConverters is null)
			{
				lock (this.lazyInitCookie)
				{
					this.cachedConverters ??= this.PreserveReferences ? new(PrimitiveReferencePreservingConverters) : new(PrimitiveConverters);
				}
			}

			return this.cachedConverters;
		}
	}

	/// <inheritdoc cref="RegisterConverterCore{T}(MessagePackConverter{T})"/>
	/// <exception cref="InvalidOperationException">Thrown if serialization has already occurred. All calls to this method should be made before anything is serialized.</exception>
	public void RegisterConverter<T>(MessagePackConverter<T> converter)
	{
		Requires.NotNull(converter);
		this.VerifyConfigurationIsNotLocked();
		this.RegisterConverterCore(converter);
	}

	/// <summary>
	/// Serializes a value.
	/// </summary>
	/// <typeparam name="T">The type to be serialized.</typeparam>
	/// <param name="writer">The msgpack writer to use.</param>
	/// <param name="value">The value to serialize.</param>
	/// <param name="shape">The shape of <typeparamref name="T"/>.</param>
	public void Serialize<T>(ref MessagePackWriter writer, in T? value, ITypeShape<T> shape)
	{
		using DisposableSerializationContext context = this.CreateSerializationContext();
		this.GetOrAddConverter(shape).Write(ref writer, value, context.Value);
	}

	/// <summary>
	/// Deserializes a value.
	/// </summary>
	/// <typeparam name="T">The type of value to deserialize.</typeparam>
	/// <param name="reader">The msgpack reader to deserialize from.</param>
	/// <param name="shape">The shape provider of <typeparamref name="T"/>. This may be the same as <typeparamref name="T"/> when the data type is attributed with <see cref="GenerateShapeAttribute"/>, or it may be another "witness" partial class that was annotated with <see cref="GenerateShapeAttribute{T}"/> where T for the attribute is the same as the <typeparamref name="T"/> used here.</param>
	/// <returns>The deserialized value.</returns>
	public T? Deserialize<T>(ref MessagePackReader reader, ITypeShape<T> shape)
	{
		using DisposableSerializationContext context = this.CreateSerializationContext();
		return this.GetOrAddConverter(shape).Read(ref reader, context.Value);
	}

	/// <summary>
	/// Serializes a value using the given <see cref="PipeWriter"/>.
	/// </summary>
	/// <typeparam name="T">The type to be serialized.</typeparam>
	/// <param name="writer">The writer to use.</param>
	/// <param name="value">The value to serialize.</param>
	/// <param name="shape">The shape of the type, as obtained from an <see cref="ITypeShapeProvider"/>.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>A task that tracks the async serialization.</returns>
	public async ValueTask SerializeAsync<T>(PipeWriter writer, T? value, ITypeShape<T> shape, CancellationToken cancellationToken)
	{
		Requires.NotNull(writer);
		cancellationToken.ThrowIfCancellationRequested();

#pragma warning disable NBMsgPackAsync
		MessagePackAsyncWriter asyncWriter = new(writer);
		using DisposableSerializationContext context = this.CreateSerializationContext();
		await this.GetOrAddConverter(shape).WriteAsync(asyncWriter, value, context.Value, cancellationToken).ConfigureAwait(false);
		asyncWriter.Flush();
#pragma warning restore NBMsgPackAsync
	}

	/// <summary>
	/// Deserializes a value from a <see cref="PipeReader"/>.
	/// </summary>
	/// <typeparam name="T">The type of value to deserialize.</typeparam>
	/// <param name="reader">The reader to deserialize from.</param>
	/// <param name="shape">The shape of the type, as obtained from an <see cref="ITypeShapeProvider"/>.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>The deserialized value.</returns>
	public ValueTask<T?> DeserializeAsync<T>(PipeReader reader, ITypeShape<T> shape, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		using DisposableSerializationContext context = this.CreateSerializationContext();
#pragma warning disable NBMsgPackAsync
		return this.GetOrAddConverter(shape).ReadAsync(new MessagePackAsyncReader(reader), context.Value, cancellationToken);
#pragma warning restore NBMsgPackAsync
	}

	/// <inheritdoc cref="ConvertToJson(in ReadOnlySequence{byte})"/>
	public static string ConvertToJson(ReadOnlyMemory<byte> msgpack) => ConvertToJson(new ReadOnlySequence<byte>(msgpack));

	/// <summary>
	/// Converts a msgpack sequence into equivalent JSON.
	/// </summary>
	/// <param name="msgpack">The msgpack sequence.</param>
	/// <returns>The JSON.</returns>
	/// <remarks>
	/// <para>
	/// Not all valid msgpack can be converted to JSON. For example, msgpack maps with non-string keys cannot be represented in JSON.
	/// As such, this method is intended for debugging purposes rather than for production use.
	/// </para>
	/// </remarks>
	public static string ConvertToJson(in ReadOnlySequence<byte> msgpack)
	{
		StringWriter jsonWriter = new();
		MessagePackReader reader = new(msgpack);
		while (!reader.End)
		{
			ConvertToJson(ref reader, jsonWriter);
		}

		return jsonWriter.ToString();
	}

	/// <summary>
	/// Converts one MessagePack structure to a JSON stream.
	/// </summary>
	/// <param name="reader">A reader of the msgpack stream.</param>
	/// <param name="jsonWriter">The writer that will receive JSON text.</param>
	public static void ConvertToJson(ref MessagePackReader reader, TextWriter jsonWriter)
	{
		Requires.NotNull(jsonWriter);

		WriteOneElement(ref reader, jsonWriter);

		static void WriteOneElement(ref MessagePackReader reader, TextWriter jsonWriter)
		{
			switch (reader.NextMessagePackType)
			{
				case MessagePackType.Nil:
					reader.ReadNil();
					jsonWriter.Write("null");
					break;
				case MessagePackType.Integer:
					if (MessagePackCode.IsSignedInteger(reader.NextCode))
					{
						jsonWriter.Write(reader.ReadInt64());
					}
					else
					{
						jsonWriter.Write(reader.ReadUInt64());
					}

					break;
				case MessagePackType.Boolean:
					jsonWriter.Write(reader.ReadBoolean() ? "true" : "false");
					break;
				case MessagePackType.Float:
					jsonWriter.Write(reader.ReadDouble());
					break;
				case MessagePackType.String:
					WriteJsonString(reader.ReadString()!, jsonWriter);
					break;
				case MessagePackType.Array:
					jsonWriter.Write('[');
					int count = reader.ReadArrayHeader();
					for (int i = 0; i < count; i++)
					{
						if (i > 0)
						{
							jsonWriter.Write(',');
						}

						WriteOneElement(ref reader, jsonWriter);
					}

					jsonWriter.Write(']');
					break;
				case MessagePackType.Map:
					jsonWriter.Write('{');
					count = reader.ReadMapHeader();
					for (int i = 0; i < count; i++)
					{
						if (i > 0)
						{
							jsonWriter.Write(',');
						}

						WriteOneElement(ref reader, jsonWriter);
						jsonWriter.Write(':');
						WriteOneElement(ref reader, jsonWriter);
					}

					jsonWriter.Write('}');
					break;
				case MessagePackType.Binary:
					jsonWriter.Write("\"msgpack binary as base64: ");
					jsonWriter.Write(Convert.ToBase64String(reader.ReadBytes()!.Value.ToArray()));
					jsonWriter.Write('\"');
					break;
				case MessagePackType.Extension:
					Extension extension = reader.ReadExtension();
					jsonWriter.Write($"\"msgpack extension {extension.Header.TypeCode} as base64: ");
					jsonWriter.Write(Convert.ToBase64String(extension.Data.ToArray()));
					jsonWriter.Write('\"');
					break;
				case MessagePackType.Unknown:
					throw new NotImplementedException($"{reader.NextMessagePackType} not yet implemented.");
			}
		}

		// escape string
		static void WriteJsonString(string value, TextWriter builder)
		{
			builder.Write('\"');

			var len = value.Length;
			for (int i = 0; i < len; i++)
			{
				var c = value[i];
				switch (c)
				{
					case '"':
						builder.Write("\\\"");
						break;
					case '\\':
						builder.Write("\\\\");
						break;
					case '\b':
						builder.Write("\\b");
						break;
					case '\f':
						builder.Write("\\f");
						break;
					case '\n':
						builder.Write("\\n");
						break;
					case '\r':
						builder.Write("\\r");
						break;
					case '\t':
						builder.Write("\\t");
						break;
					default:
						builder.Write(c);
						break;
				}
			}

			builder.Write('\"');
		}
	}

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
		if (this.TryGetConverter(out MessagePackConverter<T>? converter))
		{
			return converter;
		}

		converter = this.CreateConverter(shape);
		this.RegisterConverterCore(converter);

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
		if (this.CachedConverters.TryGetValue(typeof(T), out object? candidate))
		{
			converter = (MessagePackConverter<T>)candidate;
			return true;
		}

		converter = null;
		return false;
	}

	/// <summary>
	/// Stores a set of converters in the cache for later reuse.
	/// </summary>
	/// <param name="converters">The converters to store.</param>
	/// <remarks>
	/// Any collisions with existing converters are resolved in favor of the new converters.
	/// </remarks>
	internal void RegisterConverters(IEnumerable<KeyValuePair<Type, object>> converters)
	{
		foreach (KeyValuePair<Type, object> pair in converters)
		{
			IMessagePackConverter converter = (IMessagePackConverter)pair.Value;
			if (this.PreserveReferences)
			{
				converter = converter.WrapWithReferencePreservation();
			}

			this.CachedConverters.TryAdd(pair.Key, converter);
		}
	}

	/// <summary>
	/// Gets the property name that should be used when serializing a property.
	/// </summary>
	/// <param name="name">The original property name as given by <see cref="IPropertyShape"/>.</param>
	/// <param name="attributeProvider">The attribute provider for the property.</param>
	/// <returns>The serialized property name to use.</returns>
	internal string GetSerializedPropertyName(string name, ICustomAttributeProvider? attributeProvider)
	{
		if (this.PropertyNamingPolicy is null)
		{
			return name;
		}

		// If the property was decorated with [PropertyShape(Name = "...")], do *not* meddle with the property name.
		if (attributeProvider?.GetCustomAttributes(typeof(PropertyShapeAttribute), false).FirstOrDefault() is PropertyShapeAttribute { Name: not null })
		{
			return name;
		}

		return this.PropertyNamingPolicy.ConvertName(name);
	}

	/// <summary>
	/// Registers a converter for use with this serializer.
	/// </summary>
	/// <typeparam name="T">The convertible type.</typeparam>
	/// <param name="converter">The converter.</param>
	/// <remarks>
	/// If a converter for the data type has already been cached, the new value takes its place.
	/// Custom converters should be registered before serializing anything on this
	/// instance of <see cref="MessagePackSerializer" />.
	/// </remarks>
	internal void RegisterConverterCore<T>(MessagePackConverter<T> converter)
	{
		this.CachedConverters[typeof(T)] = this.PreserveReferences ? ((IMessagePackConverter)converter).WrapWithReferencePreservation() : converter;
	}

	/// <summary>
	/// Creates a new serialization context that is ready to process a serialization job.
	/// </summary>
	/// <returns>The serialization context.</returns>
	/// <remarks>
	/// Callers should be sure to always call <see cref="DisposableSerializationContext.Dispose"/> when done with the context.
	/// </remarks>
	protected DisposableSerializationContext CreateSerializationContext()
	{
		this.configurationLocked = true;
		return new(this.StartingContext.Start(this));
	}

	/// <summary>
	/// Synthesizes a <see cref="MessagePackConverter{T}"/> for a type with the given shape.
	/// </summary>
	/// <typeparam name="T">The data type that should be serializable.</typeparam>
	/// <param name="typeShape">The shape of the data type.</param>
	/// <returns>The msgpack converter.</returns>
	private MessagePackConverter<T> CreateConverter<T>(ITypeShape<T> typeShape)
	{
		StandardVisitor standardVisitor = new(this);
		ITypeShapeVisitor visitor;
		if (this.PreserveReferences)
		{
			visitor = new ReferencePreservingVisitor(standardVisitor);
			standardVisitor.OutwardVisitor = visitor;
		}
		else
		{
			visitor = standardVisitor;
		}

		MessagePackConverter<T> result = (MessagePackConverter<T>)typeShape.Accept(visitor)!;

		// Cache all the converters that have been generated to support the one that our caller wants.
		this.RegisterConverters(standardVisitor.GeneratedConverters.Where(kv => kv.Value is not null)!);

		return result;
	}

	/// <summary>
	/// Throws <see cref="InvalidOperationException"/> if this object should not be mutated any more
	/// (because serializations have already happened, so mutating again can lead to unpredictable behavior).
	/// </summary>
	private void VerifyConfigurationIsNotLocked()
	{
		Verify.Operation(!this.configurationLocked, "This operation must be done before (de)serialization occurs.");
	}

	/// <summary>
	/// A wrapper around <see cref="SerializationContext"/> that makes disposal easier.
	/// </summary>
	/// <param name="context">The <see cref="SerializationContext"/> to wrap.</param>
	protected struct DisposableSerializationContext(SerializationContext context) : IDisposable
	{
		/// <summary>
		/// Gets the actual <see cref="SerializationContext"/>.
		/// </summary>
		public SerializationContext Value => context;

		/// <summary>
		/// Disposes of any resources held by the serialization context.
		/// </summary>
		public void Dispose() => context.End();
	}
}
