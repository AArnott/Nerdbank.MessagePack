// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable RS0026 // optional parameter on a method with overloads

using System.Globalization;
using System.IO.Pipelines;
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
	private ConverterCache converterCache = new();
	private int maxAsyncBuffer = 1 * 1024 * 1024;

#if NET

	/// <inheritdoc cref="ConverterCache.MultiDimensionalArrayFormat"/>
	public MultiDimensionalArrayFormat MultiDimensionalArrayFormat
	{
		get => this.converterCache.MultiDimensionalArrayFormat;
		init => this.converterCache = this.converterCache with { MultiDimensionalArrayFormat = value };
	}

#endif

	/// <inheritdoc cref="ConverterCache.PropertyNamingPolicy"/>
	public MessagePackNamingPolicy? PropertyNamingPolicy
	{
		get => this.converterCache.PropertyNamingPolicy;
		init => this.converterCache = this.converterCache with { PropertyNamingPolicy = value };
	}

	/// <inheritdoc cref="ConverterCache.SerializeEnumValuesByName"/>
	public bool SerializeEnumValuesByName
	{
		get => this.converterCache.SerializeEnumValuesByName;
		init => this.converterCache = this.converterCache with { SerializeEnumValuesByName = value };
	}

	/// <inheritdoc cref="ConverterCache.SerializeDefaultValues"/>
	public SerializeDefaultValuesPolicy SerializeDefaultValues
	{
		get => this.converterCache.SerializeDefaultValues;
		init => this.converterCache = this.converterCache with { SerializeDefaultValues = value };
	}

	/// <inheritdoc cref="ConverterCache.PreserveReferences"/>
	public bool PreserveReferences
	{
		get => this.converterCache.PreserveReferences;
		init => this.converterCache = this.converterCache with { PreserveReferences = value };
	}

	/// <inheritdoc cref="ConverterCache.InternStrings"/>
	public bool InternStrings
	{
		get => this.converterCache.InternStrings;
		init => this.converterCache = this.converterCache with { InternStrings = value };
	}

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
	internal bool DisableHardwareAcceleration
	{
		get => this.converterCache.DisableHardwareAcceleration;
		init => this.converterCache = this.converterCache with { DisableHardwareAcceleration = value };
	}

	/// <inheritdoc cref="ConverterCache.RegisterConverter{T}(MessagePackConverter{T})"/>
	public void RegisterConverter<T>(MessagePackConverter<T> converter) => this.converterCache.RegisterConverter(converter);

	/// <inheritdoc cref="ConverterCache.RegisterKnownSubTypes{TBase}(KnownSubTypeMapping{TBase})"/>
	public void RegisterKnownSubTypes<TBase>(KnownSubTypeMapping<TBase> mapping) => this.converterCache.RegisterKnownSubTypes(mapping);

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
		this.converterCache.GetOrAddConverter(shape).Write(ref writer, value, context.Value);
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
		this.converterCache.GetOrAddConverter(shape).Write(ref writer, value, context.Value);
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
		this.converterCache.GetOrAddConverter<T>(provider).Write(ref writer, value, context.Value);
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
		return this.converterCache.GetOrAddConverter(shape).Read(ref reader, context.Value);
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
		return this.converterCache.GetOrAddConverter(shape).Read(ref reader, context.Value);
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
		return this.converterCache.GetOrAddConverter<T>(provider).Read(ref reader, context.Value);
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
		await this.converterCache.GetOrAddConverter(shape).WriteAsync(asyncWriter, value, context.Value).ConfigureAwait(false);
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
		await this.converterCache.GetOrAddConverter<T>(provider).WriteAsync(asyncWriter, value, context.Value).ConfigureAwait(false);
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
		=> this.DeserializeAsync(Requires.NotNull(reader), Requires.NotNull(shape).Provider, this.converterCache.GetOrAddConverter(shape), cancellationToken);

	/// <summary>
	/// Deserializes a value from a <see cref="PipeReader"/>.
	/// </summary>
	/// <typeparam name="T">The type of value to deserialize.</typeparam>
	/// <param name="reader">The reader to deserialize from.</param>
	/// <param name="provider"><inheritdoc cref="Deserialize{T}(ref MessagePackReader, ITypeShapeProvider, CancellationToken)" path="/param[@name='provider']"/></param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>The deserialized value.</returns>
	public ValueTask<T?> DeserializeAsync<T>(PipeReader reader, ITypeShapeProvider provider, CancellationToken cancellationToken = default)
		=> this.DeserializeAsync(Requires.NotNull(reader), Requires.NotNull(provider), this.converterCache.GetOrAddConverter<T>(provider), cancellationToken);

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
		return new(this.StartingContext.Start(this, this.converterCache, provider, cancellationToken));
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
