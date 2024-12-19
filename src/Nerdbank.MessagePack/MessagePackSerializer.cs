// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable RS0026 // optional parameter on a method with overloads

using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Reflection;
using Microsoft;
using PolyType.Utilities;

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
	private readonly object lazyInitCookie = new();

	private readonly ConcurrentDictionary<Type, object> userProvidedConverters = new();

	private readonly ConcurrentDictionary<Type, IKnownSubTypeMapping> userProvidedKnownSubTypes = new();

	private bool configurationLocked;

	private MultiProviderTypeCache? cachedConverters;
	private bool preserveReferences;
	private int maxAsyncBuffer = 1 * 1024 * 1024;

#if NET

	/// <summary>
	/// Gets the format to use when serializing multi-dimensional arrays.
	/// </summary>
	public MultiDimensionalArrayFormat MultiDimensionalArrayFormat { get; init; } = MultiDimensionalArrayFormat.Nested;

#endif

	/// <summary>
	/// Gets the transformation function to apply to property names before serializing them.
	/// </summary>
	/// <value>
	/// The default value is null, indicating that property names should be persisted exactly as they are declared in .NET.
	/// </value>
	public MessagePackNamingPolicy? PropertyNamingPolicy { get; init; }

	/// <summary>
	/// Gets a value indicating whether enum values will be serialized by name rather than by their numeric value.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Serializing by name is a best effort.
	/// Most enums do not define a name for every possible value, and flags enums may have complicated string representations when multiple named enum elements are combined to form a value.
	/// When a simple string cannot be constructed for a given value, the numeric form is used.
	/// </para>
	/// <para>
	/// When deserializing enums by name, name matching is case <em>insensitive</em> unless the enum type defines multiple values with names that are only distinguished by case.
	/// </para>
	/// </remarks>
	public bool SerializeEnumValuesByName { get; init; }

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
	public bool PreserveReferences
	{
		get => this.preserveReferences;
		init
		{
			if (this.preserveReferences != value)
			{
				this.preserveReferences = value;
				this.ReconfigureUserProvidedConverters();
			}
		}
	}

	/// <summary>
	/// Gets a value indicating whether to intern strings during deserialization.
	/// </summary>
	/// <remarks>
	/// <para>
	/// String interning means that a string that appears multiple times (within a single deserialization or across many)
	/// in the msgpack data will be deserialized as the same <see cref="string"/> instance, reducing GC pressure.
	/// </para>
	/// <para>
	/// When enabled, all deserialized are retained with a weak reference, allowing them to be garbage collected
	/// while also being reusable for future deserializations as long as they are in memory.
	/// </para>
	/// <para>
	/// This feature has a positive impact on memory usage but may have a negative impact on performance due to searching
	/// through previously deserialized strings to find a match.
	/// If your application is performance sensitive, you should measure the impact of this feature on your application.
	/// </para>
	/// <para>
	/// This feature is orthogonal and complementary to <see cref="PreserveReferences"/>.
	/// Preserving references impacts the serialized result and can hurt interoperability if the other party is not using the same feature.
	/// Preserving references also does not guarantee that equal strings will be reused because the original serialization may have had
	/// multiple string objects for the same value, so deserialization would produce the same result.
	/// Preserving references alone will never reuse strings across top-level deserialization operations either.
	/// Interning strings however, has no impact on the serialized result and is always safe to use.
	/// Interning strings will guarantee string objects are reused within and across deserialization operations so long as their values are equal.
	/// The combination of the two features will ensure the most compact msgpack, and will produce faster deserialization times than string interning alone.
	/// Combining the two features also activates special behavior to ensure that serialization only writes a string once
	/// and references that string later in that same serialization, even if the equal strings were unique objects.
	/// </para>
	/// </remarks>
	public bool InternStrings { get; init; }

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
	/// Gets the maximum length of msgpack to buffer before beginning deserialization.
	/// </summary>
	/// <value>
	/// May be set to any non-negative integer.
	/// The default value is 1MB and is subject to change.
	/// </value>
	/// <remarks>
	/// <para>
	/// Larger values are more likely to lead to async buffering followed by synchronous deserialization, which is significantly faster than <em>async</em> deserialization.
	/// Smaller values are useful for limiting memory usage since deserialization can happen while pulling in more msgpack bytes, allowing release of the buffers containing earlier bytes to make room for subsequent ones.
	/// </para>
	/// <para>
	/// This value has no impact once deserialization has begun.
	/// The msgpack structure to be deserialized and converters used during deserialization may result in buffering any amount of msgpack beyond this value.
	/// </para>
	/// </remarks>
	public int MaxAsyncBuffer
	{
		get => this.maxAsyncBuffer;
		init
		{
			Requires.Range(value >= 0, nameof(value));
			this.maxAsyncBuffer = value;
		}
	}

	/// <summary>
	/// Gets a value indicating whether hardware accelerated converters should be avoided.
	/// </summary>
	internal bool DisableHardwareAcceleration { get; init; }

	/// <summary>
	/// Gets all the converters this instance knows about so far.
	/// </summary>
	private MultiProviderTypeCache CachedConverters
	{
		get
		{
			if (this.cachedConverters is null)
			{
				lock (this.lazyInitCookie)
				{
					this.cachedConverters ??= new()
					{
						DelayedValueFactory = new DelayedConverterFactory(),
						ValueBuilderFactory = ctx =>
						{
							StandardVisitor standardVisitor = new StandardVisitor(this, ctx);
							if (!this.PreserveReferences)
							{
								return standardVisitor;
							}

							ReferencePreservingVisitor visitor = new(standardVisitor);
							standardVisitor.OutwardVisitor = visitor;
							return standardVisitor;
						},
					};
				}
			}

			return this.cachedConverters;
		}
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
	/// <exception cref="InvalidOperationException">Thrown if serialization has already occurred. All calls to this method should be made before anything is serialized.</exception>
	public void RegisterConverter<T>(MessagePackConverter<T> converter)
	{
		Requires.NotNull(converter);
		this.VerifyConfigurationIsNotLocked();
		this.userProvidedConverters[typeof(T)] = this.PreserveReferences
			? ((IMessagePackConverterInternal)converter).WrapWithReferencePreservation()
			: converter;
	}

	/// <summary>
	/// Registers a known sub-type mapping for a base type.
	/// </summary>
	/// <typeparam name="TBase"><inheritdoc cref="KnownSubTypeMapping{TBase}" path="/typeparam[@name='TBase']" /></typeparam>
	/// <param name="mapping">The mapping.</param>
	/// <remarks>
	/// <para>
	/// This method provides a runtime dynamic alternative to the otherwise simpler but static
	/// <see cref="KnownSubTypeAttribute"/>, enabling scenarios such as sub-types that are not known at compile time.
	/// </para>
	/// <para>
	/// This is also the only way to force the serialized schema to <em>support</em> sub-types in the future when
	/// no sub-types are defined yet, such that they can be added later without a schema-breaking change.
	/// </para>
	/// <para>
	/// A mapping provided for a given <typeparamref name="TBase"/> will completely replace any mapping from
	/// <see cref="KnownSubTypeAttribute"/> attributes that may be applied to that same <typeparamref name="TBase"/>.
	/// </para>
	/// </remarks>
	/// <exception cref="InvalidOperationException">Thrown if serialization has already occurred. All calls to this method should be made before anything is serialized.</exception>
	public void RegisterKnownSubTypes<TBase>(KnownSubTypeMapping<TBase> mapping)
	{
		Requires.NotNull(mapping);
		this.VerifyConfigurationIsNotLocked();
		this.userProvidedKnownSubTypes[typeof(TBase)] = mapping;
	}

	/// <summary>
	/// Serializes an untyped value.
	/// </summary>
	/// <param name="writer">The msgpack writer to use.</param>
	/// <param name="value">The value to serialize.</param>
	/// <param name="shape">The shape of the value to serialize.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <example>
	/// <para>
	/// The following snippet demonstrates a way to use this method.
	/// </para>
	/// <code source="../../samples/SimpleSerialization.cs" region="NonGenericSerializeDeserialize" lang="C#" />
	/// </example>
	public void SerializeObject(ref MessagePackWriter writer, object? value, ITypeShape shape, CancellationToken cancellationToken = default)
	{
		Requires.NotNull(shape);

		using DisposableSerializationContext context = this.CreateSerializationContext(shape.Provider, cancellationToken);
		this.GetOrAddConverter(shape).Write(ref writer, value, context.Value);
	}

	/// <summary>
	/// Serializes a value.
	/// </summary>
	/// <typeparam name="T">The type to be serialized.</typeparam>
	/// <param name="writer">The msgpack writer to use.</param>
	/// <param name="value">The value to serialize.</param>
	/// <param name="shape">The shape of <typeparamref name="T"/>.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	public void Serialize<T>(ref MessagePackWriter writer, in T? value, ITypeShape<T> shape, CancellationToken cancellationToken = default)
	{
		Requires.NotNull(shape);
		using DisposableSerializationContext context = this.CreateSerializationContext(shape.Provider, cancellationToken);
		this.GetOrAddConverter(shape).Write(ref writer, value, context.Value);
	}

	/// <summary>
	/// Serializes a value.
	/// </summary>
	/// <typeparam name="T">The type to be serialized.</typeparam>
	/// <param name="writer">The msgpack writer to use.</param>
	/// <param name="value">The value to serialize.</param>
	/// <param name="provider"><inheritdoc cref="Deserialize{T}(ref MessagePackReader, ITypeShapeProvider, CancellationToken)" path="/param[@name='provider']"/></param>
	/// <param name="cancellationToken">A cancellation token.</param>
	public void Serialize<T>(ref MessagePackWriter writer, in T? value, ITypeShapeProvider provider, CancellationToken cancellationToken = default)
	{
		using DisposableSerializationContext context = this.CreateSerializationContext(provider, cancellationToken);
		this.GetOrAddConverter<T>(provider).Write(ref writer, value, context.Value);
	}

	/// <summary>
	/// Deserializes an untyped value.
	/// </summary>
	/// <param name="reader">The msgpack reader to deserialize from.</param>
	/// <param name="shape">The shape of the value to deserialize.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>The deserialized value.</returns>
	/// <example>
	/// See the <see cref="SerializeObject(ref MessagePackWriter, object?, ITypeShape, CancellationToken)"/> method for an example of using this method.
	/// </example>
	public object? DeserializeObject(ref MessagePackReader reader, ITypeShape shape, CancellationToken cancellationToken = default)
	{
		Requires.NotNull(shape);

		using DisposableSerializationContext context = this.CreateSerializationContext(shape.Provider, cancellationToken);
		return this.GetOrAddConverter(shape).Read(ref reader, context.Value);
	}

	/// <summary>
	/// Deserializes a value.
	/// </summary>
	/// <typeparam name="T">The type of value to deserialize.</typeparam>
	/// <param name="reader">The msgpack reader to deserialize from.</param>
	/// <param name="shape">The shape of <typeparamref name="T"/>.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>The deserialized value.</returns>
	public T? Deserialize<T>(ref MessagePackReader reader, ITypeShape<T> shape, CancellationToken cancellationToken = default)
	{
		Requires.NotNull(shape);
		using DisposableSerializationContext context = this.CreateSerializationContext(shape.Provider, cancellationToken);
		return this.GetOrAddConverter(shape).Read(ref reader, context.Value);
	}

	/// <summary>
	/// Deserializes a value.
	/// </summary>
	/// <typeparam name="T">The type of value to deserialize.</typeparam>
	/// <param name="reader">The msgpack reader to deserialize from.</param>
	/// <param name="provider">
	/// The shape provider of <typeparamref name="T"/>.
	/// This will typically be obtained by calling the <c>ShapeProvider</c> static property on a witness class
	/// (a class on which <see cref="GenerateShapeAttribute{T}"/> has been applied).
	/// </param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>The deserialized value.</returns>
	public T? Deserialize<T>(ref MessagePackReader reader, ITypeShapeProvider provider, CancellationToken cancellationToken = default)
	{
		using DisposableSerializationContext context = this.CreateSerializationContext(provider, cancellationToken);
		return this.GetOrAddConverter<T>(provider).Read(ref reader, context.Value);
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
	public async ValueTask SerializeAsync<T>(PipeWriter writer, T? value, ITypeShape<T> shape, CancellationToken cancellationToken = default)
	{
		Requires.NotNull(writer);
		Requires.NotNull(shape);
		cancellationToken.ThrowIfCancellationRequested();

#pragma warning disable NBMsgPackAsync
		MessagePackAsyncWriter asyncWriter = new(writer);
		using DisposableSerializationContext context = this.CreateSerializationContext(shape.Provider, cancellationToken);
		await this.GetOrAddConverter(shape).WriteAsync(asyncWriter, value, context.Value).ConfigureAwait(false);
		asyncWriter.Flush();
#pragma warning restore NBMsgPackAsync
	}

	/// <summary>
	/// Serializes a value using the given <see cref="PipeWriter"/>.
	/// </summary>
	/// <typeparam name="T">The type to be serialized.</typeparam>
	/// <param name="writer">The writer to use.</param>
	/// <param name="value">The value to serialize.</param>
	/// <param name="provider"><inheritdoc cref="Deserialize{T}(ref MessagePackReader, ITypeShapeProvider, CancellationToken)" path="/param[@name='provider']"/></param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>A task that tracks the async serialization.</returns>
	public async ValueTask SerializeAsync<T>(PipeWriter writer, T? value, ITypeShapeProvider provider, CancellationToken cancellationToken = default)
	{
		Requires.NotNull(writer);
		cancellationToken.ThrowIfCancellationRequested();

#pragma warning disable NBMsgPackAsync
		MessagePackAsyncWriter asyncWriter = new(writer);
		using DisposableSerializationContext context = this.CreateSerializationContext(provider, cancellationToken);
		await this.GetOrAddConverter<T>(provider).WriteAsync(asyncWriter, value, context.Value).ConfigureAwait(false);
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
	public ValueTask<T?> DeserializeAsync<T>(PipeReader reader, ITypeShape<T> shape, CancellationToken cancellationToken = default)
		=> this.DeserializeAsync(Requires.NotNull(reader), Requires.NotNull(shape).Provider, this.GetOrAddConverter(shape), cancellationToken);

	/// <summary>
	/// Deserializes a value from a <see cref="PipeReader"/>.
	/// </summary>
	/// <typeparam name="T">The type of value to deserialize.</typeparam>
	/// <param name="reader">The reader to deserialize from.</param>
	/// <param name="provider"><inheritdoc cref="Deserialize{T}(ref MessagePackReader, ITypeShapeProvider, CancellationToken)" path="/param[@name='provider']"/></param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>The deserialized value.</returns>
	public ValueTask<T?> DeserializeAsync<T>(PipeReader reader, ITypeShapeProvider provider, CancellationToken cancellationToken = default)
		=> this.DeserializeAsync(Requires.NotNull(reader), Requires.NotNull(provider), this.GetOrAddConverter<T>(provider), cancellationToken);

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
		=> (MessagePackConverter<T>)this.CachedConverters.GetOrAdd(shape)!;

	/// <summary>
	/// Gets a converter for the given type shape.
	/// An existing converter is reused if one is found in the cache.
	/// If a converter must be created, it is added to the cache for lookup next time.
	/// </summary>
	/// <param name="shape">The shape of the type to convert.</param>
	/// <returns>A msgpack converter.</returns>
	internal IMessagePackConverterInternal GetOrAddConverter(ITypeShape shape)
		=> (IMessagePackConverterInternal)this.CachedConverters.GetOrAdd(shape)!;

	/// <summary>
	/// Gets a converter for the given type shape.
	/// An existing converter is reused if one is found in the cache.
	/// If a converter must be created, it is added to the cache for lookup next time.
	/// </summary>
	/// <typeparam name="T">The type to convert.</typeparam>
	/// <param name="provider">The type shape provider.</param>
	/// <returns>A msgpack converter.</returns>
	internal MessagePackConverter<T> GetOrAddConverter<T>(ITypeShapeProvider provider)
		=> (MessagePackConverter<T>)this.CachedConverters.GetOrAddOrThrow(typeof(T), provider);

	/// <summary>
	/// Gets a converter for the given type shape.
	/// An existing converter is reused if one is found in the cache.
	/// If a converter must be created, it is added to the cache for lookup next time.
	/// </summary>
	/// <param name="type">The type to convert.</param>
	/// <param name="provider">The type shape provider.</param>
	/// <returns>A msgpack converter.</returns>
	internal IMessagePackConverterInternal GetOrAddConverter(Type type, ITypeShapeProvider provider)
		=> (IMessagePackConverterInternal)this.CachedConverters.GetOrAddOrThrow(type, provider);

	/// <summary>
	/// Gets a user-defined converter for the specified type if one is available.
	/// </summary>
	/// <typeparam name="T">The data type for which a custom converter is desired.</typeparam>
	/// <param name="converter">Receives the converter, if the user provided one (e.g. via <see cref="RegisterConverter{T}(MessagePackConverter{T})"/>.</param>
	/// <returns>A value indicating whether a customer converter exists.</returns>
	internal bool TryGetUserDefinedConverter<T>([NotNullWhen(true)] out MessagePackConverter<T>? converter)
	{
		if (this.userProvidedConverters.TryGetValue(typeof(T), out object? value))
		{
			converter = (MessagePackConverter<T>)value;
			return true;
		}

		converter = default;
		return false;
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
	/// Gets the runtime registered sub-types for a given base type, if any.
	/// </summary>
	/// <param name="baseType">The base type.</param>
	/// <param name="subTypes">If sub-types are registered, receives the mapping of those sub-types to their aliases.</param>
	/// <returns><see langword="true" /> if sub-types are registered; <see langword="false" /> otherwise.</returns>
	internal bool TryGetDynamicSubTypes(Type baseType, [NotNullWhen(true)] out IReadOnlyDictionary<SubTypeAlias, ITypeShape>? subTypes)
	{
		if (this.userProvidedKnownSubTypes.TryGetValue(baseType, out IKnownSubTypeMapping? mapping))
		{
			subTypes = mapping.CreateSubTypesMapping();
			return true;
		}

		subTypes = null;
		return false;
	}

	/// <summary>
	/// Creates a new serialization context that is ready to process a serialization job.
	/// </summary>
	/// <param name="provider"><inheritdoc cref="Deserialize{T}(ref MessagePackReader, ITypeShapeProvider, CancellationToken)" path="/param[@name='provider']"/></param>
	/// <param name="cancellationToken">A cancellation token for the operation.</param>
	/// <returns>The serialization context.</returns>
	/// <remarks>
	/// Callers should be sure to always call <see cref="DisposableSerializationContext.Dispose"/> when done with the context.
	/// </remarks>
	protected DisposableSerializationContext CreateSerializationContext(ITypeShapeProvider provider, CancellationToken cancellationToken = default)
	{
		Requires.NotNull(provider);
		this.configurationLocked = true;
		return new(this.StartingContext.Start(this, provider, cancellationToken));
	}

	/// <summary>
	/// Throws <see cref="InvalidOperationException"/> if this object should not be mutated any more
	/// (because serializations have already happened, so mutating again can lead to unpredictable behavior).
	/// </summary>
	private void VerifyConfigurationIsNotLocked()
	{
		Verify.Operation(!this.configurationLocked, "This operation must be done before (de)serialization occurs.");
	}

	private void ReconfigureUserProvidedConverters()
	{
		foreach (KeyValuePair<Type, object> pair in this.userProvidedConverters)
		{
			IMessagePackConverterInternal converter = (IMessagePackConverterInternal)pair.Value;
			this.userProvidedConverters[pair.Key] = this.PreserveReferences ? converter.WrapWithReferencePreservation() : converter.UnwrapReferencePreservation();
		}
	}

	/// <summary>
	/// Deserializes a value from a <see cref="PipeReader"/>.
	/// </summary>
	/// <typeparam name="T">The type of value to be deserialized.</typeparam>
	/// <param name="reader">The <see cref="PipeReader"/> to read from.</param>
	/// <param name="provider"><inheritdoc cref="Deserialize{T}(ref MessagePackReader, ITypeShapeProvider, CancellationToken)" path="/param[@name='provider']"/></param>
	/// <param name="converter">The msgpack converter for the root data type.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>The deserialized value.</returns>
	private async ValueTask<T?> DeserializeAsync<T>(PipeReader reader, ITypeShapeProvider provider, MessagePackConverter<T> converter, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		using DisposableSerializationContext context = this.CreateSerializationContext(provider, cancellationToken);

		// Buffer up to some threshold before starting deserialization.
		// Only engage with the async code path (which is slower) if we reach our threshold
		// and more bytes are still to come.
		if (this.MaxAsyncBuffer > 0)
		{
			ReadResult readResult = await reader.ReadAtLeastAsync(this.MaxAsyncBuffer, cancellationToken).ConfigureAwait(false);
			if (readResult.IsCompleted)
			{
				MessagePackReader msgpackReader = new(readResult.Buffer);
				T? result = converter.Read(ref msgpackReader, context.Value);
				reader.AdvanceTo(msgpackReader.Position);
				return result;
			}
			else
			{
				reader.AdvanceTo(readResult.Buffer.Start);
			}
		}

#pragma warning disable NBMsgPackAsync
		MessagePackAsyncReader asyncReader = new(reader) { CancellationToken = cancellationToken };
		await asyncReader.ReadAsync().ConfigureAwait(false);
		return await converter.ReadAsync(asyncReader, context.Value).ConfigureAwait(false);
#pragma warning restore NBMsgPackAsync
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
