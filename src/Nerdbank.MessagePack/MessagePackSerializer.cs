// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable RS0026 // optional parameter on a method with overloads

using System.Collections.Immutable;
using System.ComponentModel;
using System.Globalization;
using System.IO.Pipelines;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Microsoft;

namespace Nerdbank.MessagePack;

/// <summary>
/// Serializes .NET objects using the MessagePack format.
/// </summary>
/// <remarks>
/// <para>
/// This type is immutable and thread-safe.
/// </para>
/// <para>
/// Changes to properties on this object should be performed with the record syntax <see langword="with" /> keyword.
/// </para>
/// </remarks>
public partial record MessagePackSerializer
{
#if NET
	private const string PreferTypeConstrainedOverloads = "Use an overload that does not take an ITypeShape<T> or ITypeShapeProvider, instead constraining T : IShapeable<T>.";
#endif

	private SerializerConfiguration configuration = SerializerConfiguration.Default;
	private int maxAsyncBuffer = 1 * 1024 * 1024;

#if NET

	/// <inheritdoc cref="SerializerConfiguration.MultiDimensionalArrayFormat"/>
	public MultiDimensionalArrayFormat MultiDimensionalArrayFormat
	{
		get => this.configuration.MultiDimensionalArrayFormat;
		init => this.configuration = this.configuration with { MultiDimensionalArrayFormat = value };
	}

#endif

	/// <inheritdoc cref="SerializerConfiguration.PropertyNamingPolicy"/>
	public MessagePackNamingPolicy? PropertyNamingPolicy
	{
		get => this.configuration.PropertyNamingPolicy;
		init => this.configuration = this.configuration with { PropertyNamingPolicy = value };
	}

	/// <inheritdoc cref="SerializerConfiguration.ComparerProvider"/>
	public IComparerProvider? ComparerProvider
	{
		get => this.configuration.ComparerProvider;
		init => this.configuration = this.configuration with { ComparerProvider = value };
	}

	/// <inheritdoc cref="SerializerConfiguration.PerfOverSchemaStability"/>
	public bool PerfOverSchemaStability
	{
		get => this.configuration.PerfOverSchemaStability;
		init => this.configuration = this.configuration with { PerfOverSchemaStability = value };
	}

	/// <inheritdoc cref="SerializerConfiguration.SerializeEnumValuesByName"/>
	public bool SerializeEnumValuesByName
	{
		get => this.configuration.SerializeEnumValuesByName;
		init => this.configuration = this.configuration with { SerializeEnumValuesByName = value };
	}

	/// <inheritdoc cref="SerializerConfiguration.SerializeDefaultValues"/>
	public SerializeDefaultValuesPolicy SerializeDefaultValues
	{
		get => this.configuration.SerializeDefaultValues;
		init => this.configuration = this.configuration with { SerializeDefaultValues = value };
	}

	/// <inheritdoc cref="SerializerConfiguration.DeserializeDefaultValues"/>
	public DeserializeDefaultValuesPolicy DeserializeDefaultValues
	{
		get => this.configuration.DeserializeDefaultValues;
		init => this.configuration = this.configuration with { DeserializeDefaultValues = value };
	}

	/// <inheritdoc cref="SerializerConfiguration.PreserveReferences"/>
	public ReferencePreservationMode PreserveReferences
	{
		get => this.configuration.PreserveReferences;
		init => this.configuration = this.configuration with { PreserveReferences = value };
	}

	/// <inheritdoc cref="SerializerConfiguration.InternStrings"/>
	public bool InternStrings
	{
		get => this.configuration.InternStrings;
		init => this.configuration = this.configuration with { InternStrings = value };
	}

	/// <summary>
	/// Gets the extension type codes to use for library-reserved extension types.
	/// </summary>
	/// <remarks>
	/// This property may be used to reassign the extension type codes for library-provided extension types
	/// in order to avoid conflicts with other libraries the application is using.
	/// </remarks>
	public LibraryReservedMessagePackExtensionTypeCode LibraryExtensionTypeCodes { get; init; } = LibraryReservedMessagePackExtensionTypeCode.Default;

	/// <inheritdoc cref="SerializerConfiguration.Converters"/>
	public ConverterCollection Converters
	{
		get => this.configuration.Converters;
		init => this.configuration = this.configuration with { Converters = value };
	}

	/// <inheritdoc cref="SerializerConfiguration.ConverterTypes"/>
	public ConverterTypeCollection ConverterTypes
	{
		get => this.configuration.ConverterTypes;
		init => this.configuration = this.configuration with { ConverterTypes = value };
	}

	/// <inheritdoc cref="SerializerConfiguration.ConverterFactories"/>
	public ImmutableArray<IMessagePackConverterFactory> ConverterFactories
	{
		get => this.configuration.ConverterFactories;
		init => this.configuration = this.configuration with { ConverterFactories = value };
	}

	/// <inheritdoc cref="SerializerConfiguration.DerivedTypeMappings"/>
	public DerivedTypeMappingCollection DerivedTypeMappings
	{
		get => this.configuration.DerivedTypeMappings;
		init => this.configuration = this.configuration with { DerivedTypeMappings = value };
	}

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
	internal bool DisableHardwareAcceleration
	{
		get => this.configuration.DisableHardwareAcceleration;
		init => this.configuration = this.configuration with { DisableHardwareAcceleration = value };
	}

	/// <inheritdoc cref="SerializerConfiguration.ConverterCache"/>
	internal ConverterCache ConverterCache => this.configuration.ConverterCache;

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
	/// <code source="../../samples/cs/SimpleSerialization.cs" region="NonGenericSerializeDeserialize" lang="C#" />
	/// </example>
	public void SerializeObject(ref MessagePackWriter writer, object? value, ITypeShape shape, CancellationToken cancellationToken = default)
	{
		Requires.NotNull(shape);

		try
		{
			using DisposableSerializationContext context = this.CreateSerializationContext(shape.Provider, cancellationToken);
			this.ConverterCache.GetOrAddConverter(shape).WriteObject(ref writer, value, context.Value);
		}
		catch (Exception ex) when (ShouldWrapSerializationException(ex, cancellationToken))
		{
			throw new MessagePackSerializationException("An error occurred during serialization.", ex);
		}
	}

	/// <summary>
	/// Serializes a value.
	/// </summary>
	/// <typeparam name="T">The type to be serialized.</typeparam>
	/// <param name="writer">The msgpack writer to use.</param>
	/// <param name="value">The value to serialize.</param>
	/// <param name="shape">The shape of <typeparamref name="T"/>.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
#if NET
	[PreferDotNetAlternativeApi(PreferTypeConstrainedOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public void Serialize<T>(ref MessagePackWriter writer, in T? value, ITypeShape<T> shape, CancellationToken cancellationToken = default)
	{
		Requires.NotNull(shape);

		try
		{
			using DisposableSerializationContext context = this.CreateSerializationContext(shape.Provider, cancellationToken);
			this.ConverterCache.GetOrAddConverter(shape).Write(ref writer, value, context.Value);
		}
		catch (Exception ex) when (ShouldWrapSerializationException(ex, cancellationToken))
		{
			throw new MessagePackSerializationException("An error occurred during serialization.", ex);
		}
	}

	/// <summary>
	/// Serializes a value.
	/// </summary>
	/// <typeparam name="T">The type to be serialized.</typeparam>
	/// <param name="writer">The msgpack writer to use.</param>
	/// <param name="value">The value to serialize.</param>
	/// <param name="provider"><inheritdoc cref="Deserialize{T}(ref MessagePackReader, ITypeShapeProvider, CancellationToken)" path="/param[@name='provider']"/></param>
	/// <param name="cancellationToken">A cancellation token.</param>
#if NET
	[PreferDotNetAlternativeApi(PreferTypeConstrainedOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public void Serialize<T>(ref MessagePackWriter writer, in T? value, ITypeShapeProvider provider, CancellationToken cancellationToken = default)
	{
		try
		{
			using DisposableSerializationContext context = this.CreateSerializationContext(provider, cancellationToken);
			this.ConverterCache.GetOrAddConverter<T>(provider).Write(ref writer, value, context.Value);
		}
		catch (Exception ex) when (ShouldWrapSerializationException(ex, cancellationToken))
		{
			throw new MessagePackSerializationException("An error occurred during serialization.", ex);
		}
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

		try
		{
			using DisposableSerializationContext context = this.CreateSerializationContext(shape.Provider, cancellationToken);
			return this.ConverterCache.GetOrAddConverter(shape).ReadObject(ref reader, context.Value);
		}
		catch (Exception ex) when (ShouldWrapSerializationException(ex, cancellationToken))
		{
			throw new MessagePackSerializationException("An error occurred during deserialization.", ex);
		}
	}

	/// <summary>
	/// Deserializes msgpack into primitive values, maps and arrays,
	/// where the maps are specially implemented to play nicely with the C# <c>dynamic</c> keyword.
	/// </summary>
	/// <param name="reader">The source of msgpack to decode.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <inheritdoc cref="PrimitivesAsObjectConverter.Read" path="/returns"/>
	/// <remarks>
	/// <inheritdoc cref="PrimitivesAsObjectConverter.Read" path="/remarks/para"/>
	/// <para>
	/// The resulting object is designed to work conveniently with the C# <c>dynamic</c> type.
	/// Maps become dynamic objects whose members are named after the keys in the maps.
	/// When maps do not use string keys, the values are still accessible by using the indexer on the object.
	/// Attempts to access values with keys that do not exist on the original msgpack object result in throwing a <see cref="KeyNotFoundException"/>.
	/// All members on maps are discoverable using <see langword="foreach" />, which enumerates the keys on the object.
	/// </para>
	/// </remarks>
	/// <example>
	/// <para>
	/// The following snippet demonstrates a way to use this method.
	/// </para>
	/// <code source="../../samples/cs/PrimitiveDeserialization.cs" region="DeserializeDynamicPrimitives" lang="C#" />
	/// </example>
	public dynamic? DeserializeDynamicPrimitives(ref MessagePackReader reader, CancellationToken cancellationToken = default)
	{
		using DisposableSerializationContext context = this.CreateSerializationContext(MsgPackPrimitivesWitness.ShapeProvider, cancellationToken);
		try
		{
			return PrimitivesAsDynamicConverter.Instance.Read(ref reader, context.Value);
		}
		catch (Exception ex) when (ShouldWrapSerializationException(ex, cancellationToken))
		{
			throw new MessagePackSerializationException("An error occurred during deserialization.", ex);
		}
	}

	/// <summary>
	/// Deserializes msgpack into primitive values, maps and arrays.
	/// </summary>
	/// <param name="reader">The source of msgpack to decode.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <inheritdoc cref="PrimitivesAsObjectConverter.Read" path="/remarks"/>
	/// <inheritdoc cref="PrimitivesAsObjectConverter.Read" path="/returns"/>
	/// <example>
	/// <para>
	/// The following snippet demonstrates a way to use this method.
	/// </para>
	/// <code source="../../samples/cs/PrimitiveDeserialization.cs" region="DeserializePrimitives" lang="C#" />
	/// </example>
	public object? DeserializePrimitives(ref MessagePackReader reader, CancellationToken cancellationToken = default)
	{
		using DisposableSerializationContext context = this.CreateSerializationContext(MsgPackPrimitivesWitness.ShapeProvider, cancellationToken);
		try
		{
			return PrimitivesAsObjectConverter.Instance.Read(ref reader, context.Value);
		}
		catch (Exception ex) when (ShouldWrapSerializationException(ex, cancellationToken))
		{
			throw new MessagePackSerializationException("An error occurred during deserialization.", ex);
		}
	}

	/// <summary>
	/// Deserializes a value.
	/// </summary>
	/// <typeparam name="T">The type of value to deserialize.</typeparam>
	/// <param name="reader">The msgpack reader to deserialize from.</param>
	/// <param name="shape">The shape of <typeparamref name="T"/>.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>The deserialized value.</returns>
#if NET
	[PreferDotNetAlternativeApi(PreferTypeConstrainedOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public T? Deserialize<T>(ref MessagePackReader reader, ITypeShape<T> shape, CancellationToken cancellationToken = default)
	{
		Requires.NotNull(shape);
		using DisposableSerializationContext context = this.CreateSerializationContext(shape.Provider, cancellationToken);
		try
		{
			return this.ConverterCache.GetOrAddConverter(shape).Read(ref reader, context.Value);
		}
		catch (Exception ex) when (ShouldWrapSerializationException(ex, cancellationToken))
		{
			throw new MessagePackSerializationException("An error occurred during deserialization.", ex);
		}
	}

	/// <summary>
	/// Deserializes a value.
	/// </summary>
	/// <typeparam name="T">The type of value to deserialize.</typeparam>
	/// <param name="reader">The msgpack reader to deserialize from.</param>
	/// <param name="provider">
	/// The shape provider of <typeparamref name="T"/>.
	/// This will typically be obtained by calling the <c>ShapeProvider</c> static property on a witness class
	/// (a class on which <see cref="GenerateShapeForAttribute{T}"/> has been applied).
	/// </param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>The deserialized value.</returns>
#if NET
	[PreferDotNetAlternativeApi(PreferTypeConstrainedOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public T? Deserialize<T>(ref MessagePackReader reader, ITypeShapeProvider provider, CancellationToken cancellationToken = default)
	{
		try
		{
			using DisposableSerializationContext context = this.CreateSerializationContext(provider, cancellationToken);
			return this.ConverterCache.GetOrAddConverter<T>(provider).Read(ref reader, context.Value);
		}
		catch (Exception ex) when (ShouldWrapSerializationException(ex, cancellationToken))
		{
			throw new MessagePackSerializationException("An error occurred during deserialization.", ex);
		}
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
#if NET
	[PreferDotNetAlternativeApi(PreferTypeConstrainedOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public async ValueTask SerializeAsync<T>(PipeWriter writer, T? value, ITypeShape<T> shape, CancellationToken cancellationToken = default)
	{
		Requires.NotNull(writer);
		Requires.NotNull(shape);

		try
		{
#pragma warning disable NBMsgPackAsync
			using DisposableSerializationContext context = this.CreateSerializationContext(shape.Provider, cancellationToken);
			MessagePackAsyncWriter asyncWriter = new(writer);
			await this.ConverterCache.GetOrAddConverter(shape).WriteAsync(asyncWriter, value, context.Value).ConfigureAwait(false);
			asyncWriter.Flush();
#pragma warning restore NBMsgPackAsync
		}
		catch (Exception ex) when (ShouldWrapSerializationException(ex, cancellationToken))
		{
			throw new MessagePackSerializationException("An error occurred during serialization.", ex);
		}
	}

	/// <inheritdoc cref="SerializeAsync{T}(PipeWriter, T, ITypeShape{T}, CancellationToken)"/>
	public async ValueTask SerializeObjectAsync(PipeWriter writer, object? value, ITypeShape shape, CancellationToken cancellationToken = default)
	{
		Requires.NotNull(writer);
		Requires.NotNull(shape);

		try
		{
#pragma warning disable NBMsgPackAsync
			using DisposableSerializationContext context = this.CreateSerializationContext(shape.Provider, cancellationToken);
			MessagePackAsyncWriter asyncWriter = new(writer);
			await this.ConverterCache.GetOrAddConverter(shape).WriteObjectAsync(asyncWriter, value, context.Value).ConfigureAwait(false);
			asyncWriter.Flush();
#pragma warning restore NBMsgPackAsync
		}
		catch (Exception ex) when (ShouldWrapSerializationException(ex, cancellationToken))
		{
			throw new MessagePackSerializationException("An error occurred during serialization.", ex);
		}
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
#if NET
	[PreferDotNetAlternativeApi(PreferTypeConstrainedOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public async ValueTask SerializeAsync<T>(PipeWriter writer, T? value, ITypeShapeProvider provider, CancellationToken cancellationToken = default)
	{
		Requires.NotNull(writer);

		try
		{
#pragma warning disable NBMsgPackAsync
			using DisposableSerializationContext context = this.CreateSerializationContext(provider, cancellationToken);
			MessagePackAsyncWriter asyncWriter = new(writer);
			await this.ConverterCache.GetOrAddConverter<T>(provider).WriteAsync(asyncWriter, value, context.Value).ConfigureAwait(false);
			asyncWriter.Flush();
#pragma warning restore NBMsgPackAsync
		}
		catch (Exception ex) when (ShouldWrapSerializationException(ex, cancellationToken))
		{
			throw new MessagePackSerializationException("An error occurred during serialization.", ex);
		}
	}

	/// <summary>
	/// Deserializes a value from a <see cref="PipeReader"/>.
	/// </summary>
	/// <typeparam name="T">The type of value to deserialize.</typeparam>
	/// <param name="reader">The reader to deserialize from. <see cref="PipeReader.Complete(Exception?)"/> is <em>not</em> called on this at the conclusion of deserialization, and the reader is left at the position after the last msgpack byte read.</param>
	/// <param name="shape">The shape of the type, as obtained from an <see cref="ITypeShapeProvider"/>.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>The deserialized value.</returns>
#if NET
	[PreferDotNetAlternativeApi(PreferTypeConstrainedOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public ValueTask<T?> DeserializeAsync<T>(PipeReader reader, ITypeShape<T> shape, CancellationToken cancellationToken = default)
		=> this.DeserializeAsync(Requires.NotNull(reader), Requires.NotNull(shape).Provider, this.ConverterCache.GetOrAddConverter(shape), cancellationToken);

	/// <inheritdoc cref="DeserializeAsync{T}(PipeReader, ITypeShape{T}, CancellationToken)"/>
	public ValueTask<object?> DeserializeObjectAsync(PipeReader reader, ITypeShape shape, CancellationToken cancellationToken = default)
		=> this.DeserializeObjectAsync(Requires.NotNull(reader), Requires.NotNull(shape).Provider, this.ConverterCache.GetOrAddConverter(shape), cancellationToken);

	/// <summary>
	/// Deserializes a value from a <see cref="PipeReader"/>.
	/// </summary>
	/// <typeparam name="T">The type of value to deserialize.</typeparam>
	/// <param name="reader">The reader to deserialize from.</param>
	/// <param name="provider"><inheritdoc cref="Deserialize{T}(ref MessagePackReader, ITypeShapeProvider, CancellationToken)" path="/param[@name='provider']"/></param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>The deserialized value.</returns>
#if NET
	[PreferDotNetAlternativeApi(PreferTypeConstrainedOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public ValueTask<T?> DeserializeAsync<T>(PipeReader reader, ITypeShapeProvider provider, CancellationToken cancellationToken = default)
		=> this.DeserializeAsync(Requires.NotNull(reader), Requires.NotNull(provider), this.ConverterCache.GetOrAddConverter<T>(provider), cancellationToken);

	/// <inheritdoc cref="ConvertToJson(in ReadOnlySequence{byte}, JsonOptions?)"/>
	public string ConvertToJson(ReadOnlyMemory<byte> msgpack, JsonOptions? options = null) => this.ConvertToJson(new ReadOnlySequence<byte>(msgpack), options);

	/// <summary>
	/// Converts a msgpack sequence into equivalent JSON.
	/// </summary>
	/// <param name="msgpack">The msgpack sequence.</param>
	/// <param name="options"><inheritdoc cref="ConvertToJson(ref MessagePackReader, TextWriter, JsonOptions?)" path="/param[@name='options']"/></param>
	/// <returns>The JSON.</returns>
	/// <remarks>
	/// <para>
	/// Not all valid msgpack can be converted to JSON. For example, msgpack maps with non-string keys cannot be represented in JSON.
	/// As such, this method is intended for debugging purposes rather than for production use.
	/// </para>
	/// </remarks>
	public string ConvertToJson(in ReadOnlySequence<byte> msgpack, JsonOptions? options = null)
	{
		using StringWriter jsonWriter = new();
		MessagePackReader reader = new(msgpack);
		while (!reader.End)
		{
			this.ConvertToJson(ref reader, jsonWriter, options);
		}

		return jsonWriter.ToString();
	}

	/// <summary>
	/// Converts one MessagePack structure to a JSON stream.
	/// </summary>
	/// <param name="reader">A reader of the msgpack stream.</param>
	/// <param name="jsonWriter">The writer that will receive JSON text.</param>
	/// <param name="options">Options to customize how the JSON is written.</param>
	public void ConvertToJson(ref MessagePackReader reader, TextWriter jsonWriter, JsonOptions? options = null)
	{
		Requires.NotNull(jsonWriter);

		WriteOneElement(ref reader, jsonWriter, options ?? new(), this.LibraryExtensionTypeCodes, 0);

		static void WriteOneElement(ref MessagePackReader reader, TextWriter jsonWriter, JsonOptions options, LibraryReservedMessagePackExtensionTypeCode extensionTypeCodes, int indentationLevel)
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
					// Emit with only the precision inherent in the msgpack format.
					// Use "R" to preserve full precision in the string version so it isn't lossy.
					if (reader.NextCode == MessagePackCode.Float32)
					{
						jsonWriter.Write(reader.ReadSingle().ToString("R", CultureInfo.InvariantCulture));
					}
					else
					{
						jsonWriter.Write(reader.ReadDouble().ToString("R", CultureInfo.InvariantCulture));
					}

					break;
				case MessagePackType.String:
					WriteJsonString(reader.ReadString()!, jsonWriter);
					break;
				case MessagePackType.Array:
					jsonWriter.Write('[');
					int count = reader.ReadArrayHeader();
					if (count > 0)
					{
						NewLine(jsonWriter, options, indentationLevel + 1);

						for (int i = 0; i < count; i++)
						{
							if (i > 0)
							{
								jsonWriter.Write(',');
								NewLine(jsonWriter, options, indentationLevel + 1);
							}

							WriteOneElement(ref reader, jsonWriter, options, extensionTypeCodes, indentationLevel + 1);
						}

						if (options.TrailingCommas && options.Indentation is not null && count > 0)
						{
							jsonWriter.Write(',');
						}

						NewLine(jsonWriter, options, indentationLevel);
					}

					jsonWriter.Write(']');
					break;
				case MessagePackType.Map:
					jsonWriter.Write('{');
					count = reader.ReadMapHeader();
					if (count > 0)
					{
						NewLine(jsonWriter, options, indentationLevel + 1);
						for (int i = 0; i < count; i++)
						{
							if (i > 0)
							{
								jsonWriter.Write(',');
								NewLine(jsonWriter, options, indentationLevel + 1);
							}

							WriteOneElement(ref reader, jsonWriter, options, extensionTypeCodes, indentationLevel + 1);
							if (options.Indentation is null)
							{
								jsonWriter.Write(':');
							}
							else
							{
								jsonWriter.Write(": ");
							}

							WriteOneElement(ref reader, jsonWriter, options, extensionTypeCodes, indentationLevel + 1);
						}

						if (options.TrailingCommas && options.Indentation is not null && count > 0)
						{
							jsonWriter.Write(',');
						}

						NewLine(jsonWriter, options, indentationLevel);
					}

					jsonWriter.Write('}');
					break;
				case MessagePackType.Binary:
					jsonWriter.Write("\"msgpack binary as base64: ");
					jsonWriter.Write(Convert.ToBase64String(reader.ReadBytes()!.Value.ToArray()));
					jsonWriter.Write('\"');
					break;
				case MessagePackType.Extension:
					SerializationContext context = new() { ExtensionTypeCodes = extensionTypeCodes };
					MessagePackReader peek = reader.CreatePeekReader();
					ExtensionHeader extensionHeader = peek.ReadExtensionHeader();
					if (!options.IgnoreKnownExtensions)
					{
						if (extensionHeader.TypeCode == extensionTypeCodes.GuidLittleEndian)
						{
							jsonWriter.Write('\"');
							jsonWriter.Write(GuidAsLittleEndianBinaryConverter.Instance.Read(ref reader, context).ToString("D"));
							jsonWriter.Write('\"');
							break;
						}

						if (extensionHeader.TypeCode == extensionTypeCodes.BigIntegerLittleEndian)
						{
							jsonWriter.Write(BigIntegerConverter.Instance.Read(ref reader, context).ToString());
							break;
						}

						if (extensionHeader.TypeCode == extensionTypeCodes.Decimal)
						{
							jsonWriter.Write(MessagePack.Converters.DecimalConverter.Instance.Read(ref reader, context).ToString(CultureInfo.InvariantCulture));
							break;
						}

#if NET
						if (extensionHeader.TypeCode == extensionTypeCodes.Int128)
						{
							jsonWriter.Write(MessagePack.Converters.Int128Converter.Instance.Read(ref reader, context).ToString(CultureInfo.InvariantCulture));
							break;
						}

						if (extensionHeader.TypeCode == extensionTypeCodes.UInt128)
						{
							jsonWriter.Write(MessagePack.Converters.UInt128Converter.Instance.Read(ref reader, context).ToString(CultureInfo.InvariantCulture));
							break;
						}
#endif
					}

					Extension extension = reader.ReadExtension();
					jsonWriter.Write($"\"msgpack extension {extension.Header.TypeCode} as base64: ");
					jsonWriter.Write(Convert.ToBase64String(extension.Data.ToArray()));
					jsonWriter.Write('\"');

					break;
				case MessagePackType.Unknown:
					throw new NotImplementedException($"{reader.NextMessagePackType} not yet implemented.");
			}

			static void NewLine(TextWriter writer, JsonOptions options, int indentationLevel)
			{
				if (options.Indentation is not null)
				{
					writer.Write(options.NewLine);
					for (int i = 0; i < indentationLevel; i++)
					{
						writer.Write(options.Indentation);
					}
				}
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

	/// <inheritdoc cref="DeserializeEnumerableAsync{T}(PipeReader, ITypeShapeProvider, MessagePackConverter{T}, CancellationToken)"/>
	/// <param name="shape"><inheritdoc cref="DeserializeAsync{T}(PipeReader, ITypeShape{T}, CancellationToken)" path="/param[@name='shape']"/></param>
#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
#if NET
	[PreferDotNetAlternativeApi(PreferTypeConstrainedOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public IAsyncEnumerable<T?> DeserializeEnumerableAsync<T>(PipeReader reader, ITypeShape<T> shape, CancellationToken cancellationToken = default)
#pragma warning restore CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
		=> this.DeserializeEnumerableAsync(Requires.NotNull(reader), Requires.NotNull(shape).Provider, this.ConverterCache.GetOrAddConverter(shape), cancellationToken);

	/// <inheritdoc cref="DeserializeEnumerableAsync{T}(PipeReader, ITypeShapeProvider, MessagePackConverter{T}, CancellationToken)"/>
#if NET
	[PreferDotNetAlternativeApi(PreferTypeConstrainedOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public IAsyncEnumerable<T?> DeserializeEnumerableAsync<T>(PipeReader reader, ITypeShapeProvider provider, CancellationToken cancellationToken = default)
		=> this.DeserializeEnumerableAsync(Requires.NotNull(reader), provider, this.ConverterCache.GetOrAddConverter<T>(provider), cancellationToken);

	/// <inheritdoc cref="DeserializeEnumerableAsync{T, TElement}(PipeReader, ITypeShapeProvider, StreamingEnumerationOptions{T, TElement}, MessagePackConverter{TElement}, CancellationToken)"/>
	/// <param name="shape"><inheritdoc cref="DeserializeAsync{T}(PipeReader, ITypeShape{T}, CancellationToken)" path="/param[@name='shape']"/></param>
#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
#if NET
	[PreferDotNetAlternativeApi(PreferTypeConstrainedOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public IAsyncEnumerable<TElement?> DeserializeEnumerableAsync<T, TElement>(PipeReader reader, ITypeShape<T> shape, StreamingEnumerationOptions<T, TElement> options, CancellationToken cancellationToken = default)
#pragma warning restore CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
		=> this.DeserializeEnumerableAsync(Requires.NotNull(reader), Requires.NotNull(shape).Provider, Requires.NotNull(options), this.ConverterCache.GetOrAddConverter(shape.Provider.Resolve<TElement>()), cancellationToken);

	/// <inheritdoc cref="DeserializeEnumerableAsync{T, TElement}(PipeReader, ITypeShapeProvider, StreamingEnumerationOptions{T, TElement}, MessagePackConverter{TElement}, CancellationToken)"/>
#if NET
	[PreferDotNetAlternativeApi(PreferTypeConstrainedOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public IAsyncEnumerable<TElement?> DeserializeEnumerableAsync<T, TElement>(PipeReader reader, ITypeShapeProvider provider, StreamingEnumerationOptions<T, TElement> options, CancellationToken cancellationToken = default)
		=> this.DeserializeEnumerableAsync(Requires.NotNull(reader), provider, Requires.NotNull(options), this.ConverterCache.GetOrAddConverter<TElement>(provider), cancellationToken);

	/// <summary>
	/// Gets a converter for a given type shape.
	/// </summary>
	/// <param name="typeShape">The type shape.</param>
	/// <returns>A converter.</returns>
	internal MessagePackConverter GetConverter(ITypeShape typeShape) => this.ConverterCache.GetOrAddConverter(typeShape);

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
		return new(this.StartingContext.Start(this, this.ConverterCache, provider, cancellationToken));
	}

	/// <summary>
	/// Deserializes a sequence of values such that each element is produced individually.
	/// </summary>
	/// <typeparam name="T">The type of value to be deserialized.</typeparam>
	/// <param name="reader">The reader to deserialize from. <see cref="PipeReader.CompleteAsync(Exception?)"/> will be called only at the conclusion of a successful enumeration.</param>
	/// <param name="provider"><inheritdoc cref="DeserializeAsync{T}(PipeReader, ITypeShapeProvider, CancellationToken)" path="/param[@name='provider']"/></param>
	/// <param name="converter">The msgpack converter for the root data type.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>An async enumerable, suitable for use with <c>await foreach</c>.</returns>
	/// <remarks>
	/// <para>
	/// The content read from <paramref name="reader"/> must be a sequence of msgpack-encoded values with no envelope (e.g. an array).
	/// After the <paramref name="reader"/> is exhausted, the sequence will end.
	/// </para>
	/// <para>
	/// See <see cref="DeserializeEnumerableAsync{T, TElement}(PipeReader, ITypeShapeProvider, StreamingEnumerationOptions{T, TElement}, CancellationToken)"/>
	/// or any other overload that takes a <see cref="StreamingEnumerationOptions{T, TElement}"/> parameter
	/// for streaming a sequence of values that is nested within a larger msgpack structure.
	/// </para>
	/// </remarks>
	/// <inheritdoc cref="ThrowIfPreservingReferencesDuringEnumeration" path="/exception"/>
	private async IAsyncEnumerable<T?> DeserializeEnumerableAsync<T>(PipeReader reader, ITypeShapeProvider provider, MessagePackConverter<T> converter, [EnumeratorCancellation] CancellationToken cancellationToken)
	{
		this.ThrowIfPreservingReferencesDuringEnumeration();

		using DisposableSerializationContext context = this.CreateSerializationContext(provider, cancellationToken);

#pragma warning disable NBMsgPackAsync
		MessagePackAsyncReader asyncReader = new(reader) { CancellationToken = cancellationToken };
		bool readMore = false;
		while (!await asyncReader.GetIsEndOfStreamAsync(readMore).ConfigureAwait(false))
		{
			// Use sync deserialization if we can because it's faster.
			int bufferedCount = asyncReader.GetBufferedStructuresCount(100, context.Value, out bool reachedMaxCount);
			if (bufferedCount > 0)
			{
				for (int i = 0; i < bufferedCount; i++)
				{
					MessagePackReader bufferedReader = asyncReader.CreateBufferedReader();
					T? element = converter.Read(ref bufferedReader, context.Value);
					asyncReader.ReturnReader(ref bufferedReader);
					yield return element;
				}

				// If we reached the max count, there may be more items in the buffer still that were not counted.
				// In which case we should NOT potentially wait for more bytes to come as that can hang deserialization
				// while there are still items to yield.
				// We've proven that items *can* fit in the buffer, and that we've read all we can.
				// Try to read more bytes to see if we can keep synchronously deserializing.
				readMore = !reachedMaxCount;
			}
			else
			{
				// We don't have a complete structure buffered, so use async streaming deserialization.
				yield return await converter.ReadAsync(asyncReader, context.Value).ConfigureAwait(false);

				// We don't know what's in the buffer.
				// There may be a whole element left in there.
				// Reading more at this point could cause us to hang for more bytes
				// instead of yielding the element we already have in the buffer.
				readMore = false;
			}
		}

		asyncReader.Dispose(); // Only dispose in non-exceptional paths, since it may throw again if an exception is already in progress.
#pragma warning restore NBMsgPackAsync

		await reader.CompleteAsync().ConfigureAwait(false);
	}

	/// <summary>
	/// Deserializes a sequence of values in a larger msgpack structure such that each element is produced individually.
	/// </summary>
	/// <typeparam name="T">The type that describes the top-level msgpack structure.</typeparam>
	/// <typeparam name="TElement">The type of element to be enumerated within the structure.</typeparam>
	/// <param name="reader">The reader to deserialize from. <see cref="PipeReader.CompleteAsync(Exception?)"/> will be called only at the conclusion of a successful enumeration.</param>
	/// <param name="provider"><inheritdoc cref="DeserializeAsync{T}(PipeReader, ITypeShapeProvider, CancellationToken)" path="/param[@name='provider']"/></param>
	/// <param name="options">Options to apply to the streaming enumeration.</param>
	/// <param name="converter">The msgpack converter for the root data type.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>An async enumerable, suitable for use with <c>await foreach</c>.</returns>
	/// <remarks>
	/// <para>
	/// The content read from <paramref name="reader"/> must be a sequence of msgpack-encoded values with no envelope (e.g. an array).
	/// After the <paramref name="reader"/> is exhausted, the sequence will end.
	/// </para>
	/// <para>
	/// See <see cref="DeserializeEnumerableAsync{T}(PipeReader, ITypeShapeProvider, CancellationToken)"/>
	/// or any other overload that does not take a <see cref="StreamingEnumerationOptions{T, TElement}"/> parameter
	/// for streaming a sequence of values that are each top-level structures in the stream (with no envelope).
	/// </para>
	/// </remarks>
	/// <inheritdoc cref="ThrowIfPreservingReferencesDuringEnumeration" path="/exception"/>
	private async IAsyncEnumerable<TElement?> DeserializeEnumerableAsync<T, TElement>(PipeReader reader, ITypeShapeProvider provider, StreamingEnumerationOptions<T, TElement> options, MessagePackConverter<TElement> converter, [EnumeratorCancellation] CancellationToken cancellationToken)
	{
		this.ThrowIfPreservingReferencesDuringEnumeration();

		using DisposableSerializationContext context = this.CreateSerializationContext(provider, cancellationToken);

#pragma warning disable NBMsgPackAsync
		MessagePackAsyncReader asyncReader = new(reader) { CancellationToken = cancellationToken };
		await asyncReader.ReadAsync().ConfigureAwait(false);

		StreamingDeserializer<TElement> helper = new(this, provider, asyncReader, context.Value);
		await foreach (TElement? element in helper.EnumerateArrayAsync(options.Path, throwOnUnreachableSequence: !options.EmptySequenceForUndiscoverablePath, converter, options.LeaveOpen).ConfigureAwait(false))
		{
			yield return element;
		}
#pragma warning restore NBMsgPackAsync

		asyncReader.Dispose(); // Only dispose in non-exceptional paths, since it may throw again if an exception is already in progress.
		if (!options.LeaveOpen)
		{
			await reader.CompleteAsync().ConfigureAwait(false);
		}
	}

	/// <exception cref="NotSupportedException">Thrown if <see cref="PreserveReferences"/> is not <see cref="ReferencePreservationMode.Off"/>.</exception>
	private void ThrowIfPreservingReferencesDuringEnumeration()
	{
		if (this.PreserveReferences != ReferencePreservationMode.Off)
		{
			// This was failing in less expected ways, so we just disable the scenario.
			// It may not make so much sense anyway, given async enumeration clients may not want to retain references to all the objects
			// over potentially long periods of time.
			// If we ever enable this, we should think about whether to use weak references, or only preserve references within a single enumerated item's graph.
			throw new NotSupportedException($"Enumeration is not supported when {nameof(this.PreserveReferences)} is enabled.");
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
		try
		{
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
			T? result2 = await converter.ReadAsync(asyncReader, context.Value).ConfigureAwait(false);
			asyncReader.Dispose(); // only dispose this on success paths, since on exception it may throw (again) and conceal the original exception.
			return result2;
#pragma warning restore NBMsgPackAsync
		}
		catch (Exception ex) when (ShouldWrapSerializationException(ex, cancellationToken))
		{
			throw new MessagePackSerializationException("An error occurred during deserialization.", ex);
		}
	}

	private async ValueTask<object?> DeserializeObjectAsync(PipeReader reader, ITypeShapeProvider provider, MessagePackConverter converter, CancellationToken cancellationToken = default)
	{
		try
		{
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
					object? result = converter.ReadObject(ref msgpackReader, context.Value);
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
			object? result2 = await converter.ReadObjectAsync(asyncReader, context.Value).ConfigureAwait(false);
			asyncReader.Dispose(); // only dispose this on success paths, since on exception it may throw (again) and conceal the original exception.
			return result2;
#pragma warning restore NBMsgPackAsync
		}
		catch (Exception ex) when (ShouldWrapSerializationException(ex, cancellationToken))
		{
			throw new MessagePackSerializationException("An error occurred during deserialization.", ex);
		}
	}

	/// <summary>
	/// Determines whether an exception thrown during (de)serialization should be wrapped.
	/// </summary>
	/// <param name="ex">The thrown exception.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns><see langword="true" /> if the exception should be wrapped; <see langword="false" /> otherwise.</returns>
	/// <remarks>
	/// We wrap all exceptions <em>except</em> <see cref="OperationCanceledException"/> if the cancellation token is cancelled.
	/// In other words, the only time we allow any exception to escape is when the operation was cancelled, because
	/// users expect to catch <see cref="OperationCanceledException"/> when they cancel an operation.
	/// </remarks>
	private static bool ShouldWrapSerializationException(Exception ex, CancellationToken cancellationToken)
		=> ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested;

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

	/// <summary>
	/// A description of how JSON should be formatted when calling one of the <see cref="ConvertToJson(ref MessagePackReader, TextWriter, JsonOptions?)"/> overloads.
	/// </summary>
	public record struct JsonOptions
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="JsonOptions"/> struct.
		/// </summary>
		public JsonOptions()
		{
		}

		/// <summary>
		/// Gets or sets the string used to indent the JSON (implies newlines are also used).
		/// </summary>
		/// <remarks>
		/// A <see langword="null" /> value indicates that no indentation should be used.
		/// </remarks>
		public string? Indentation { get; set; }

		/// <summary>
		/// Gets or sets the sequence of characters used to represent a newline.
		/// </summary>
		/// <value>The default is <see cref="Environment.NewLine"/>.</value>
		public string NewLine { get; set; } = Environment.NewLine;

		/// <summary>
		/// Gets or sets a value indicating whether the JSON may use trailing commas (e.g. after the last property or element in an array).
		/// </summary>
		/// <remarks>
		/// <para>
		/// Trailing commas are not allowed in JSON by default, but some parsers may accept them.
		/// JSON5 allows trailing commas.
		/// </para>
		/// <para>
		/// Trailing commas may only be emitted when <see cref="Indentation"/> is set to a non-<see langword="null" /> value.
		/// </para>
		/// </remarks>
		public bool TrailingCommas { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether to <em>not</em> interpret known msgpack extension types
		/// (e.g. <see cref="Guid"/>, <see cref="System.Numerics.BigInteger"/>, <see cref="decimal"/>)
		/// into JSON-friendly representations.
		/// </summary>
		public bool IgnoreKnownExtensions { get; set; }
	}

	/// <summary>
	/// Options for streaming a sequence of values from a msgpack stream.
	/// </summary>
	/// <typeparam name="T">The envelope type; i.e. the outer-most structure that contains the sequence.</typeparam>
	/// <typeparam name="TElement">The type of element to be enumerated.</typeparam>
	/// <param name="Path">The path leading from the envelope type <typeparamref name="T"/> to the sequence of <typeparamref name="TElement"/> values.</param>
	public record class StreamingEnumerationOptions<T, TElement>(Expression<Func<T, IEnumerable<TElement>>> Path)
	{
		/// <summary>
		/// Gets a value indicating whether the <see cref="PipeReader"/> should be left open after the enumeration completes.
		/// </summary>
		/// <remarks>
		/// When <see langword="false" />, <see cref="PipeReader.CompleteAsync(Exception?)"/> will be called at the conclusion of enumeration.
		/// </remarks>
		public bool LeaveOpen { get; init; }

		/// <summary>
		/// Gets a value indicating whether to produce an empty sequence if <see cref="Path"/> does not lead to a sequence (due to a missing property or null value) in the msgpack data.
		/// </summary>
		/// <remarks>
		/// When this value is <see langword="false"/>, a <see cref="MessagePackSerializationException"/> is thrown when <see cref="Path"/> does not lead to a sequence.
		/// </remarks>
		public bool EmptySequenceForUndiscoverablePath { get; init; }
	}
}
