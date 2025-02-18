// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Pipelines;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Microsoft;

namespace Nerdbank.PolySerializer;

/// <summary>
/// A base class for serializers of all formats.
/// </summary>
/// <remarks>
/// <para>
/// Derived types may choose to expose <see cref="ConverterCache.PreserveReferences"/> as a public property
/// if the formatters support it.
/// </para>
/// </remarks>
public abstract partial record SerializerBase
{
	private int maxAsyncBuffer = 1 * 1024 * 1024;

	/// <summary>
	/// Initializes a new instance of the <see cref="SerializerBase"/> class.
	/// </summary>
	/// <param name="converterCache">A new converter cache.</param>
	internal SerializerBase(ConverterCache converterCache)
	{
		this.ConverterCache = converterCache;
	}

	/// <inheritdoc cref="ConverterCache.InternStrings"/>
	public bool InternStrings
	{
		get => this.ConverterCache.InternStrings;
		init => this.ConverterCache = this.ConverterCache with { InternStrings = value };
	}

#if NET

	/// <inheritdoc cref="ConverterCache.MultiDimensionalArrayFormat"/>
	public MultiDimensionalArrayFormat MultiDimensionalArrayFormat
	{
		get => this.ConverterCache.MultiDimensionalArrayFormat;
		init => this.ConverterCache = this.ConverterCache with { MultiDimensionalArrayFormat = value };
	}

#endif

	/// <inheritdoc cref="ConverterCache.PropertyNamingPolicy"/>
	public NamingPolicy? PropertyNamingPolicy
	{
		get => this.ConverterCache.PropertyNamingPolicy;
		init => this.ConverterCache = this.ConverterCache with { PropertyNamingPolicy = value };
	}

	/// <inheritdoc cref="ConverterCache.SerializeEnumValuesByName"/>
	public bool SerializeEnumValuesByName
	{
		get => this.ConverterCache.SerializeEnumValuesByName;
		init => this.ConverterCache = this.ConverterCache with { SerializeEnumValuesByName = value };
	}

	/// <inheritdoc cref="ConverterCache.SerializeDefaultValues"/>
	public SerializeDefaultValuesPolicy SerializeDefaultValues
	{
		get => this.ConverterCache.SerializeDefaultValues;
		init => this.ConverterCache = this.ConverterCache with { SerializeDefaultValues = value };
	}

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
	/// Gets the starting context to begin (de)serializations with.
	/// </summary>
	public SerializationContext StartingContext { get; init; } = new();

	/// <summary>
	/// Gets the converter cache for this serializer.
	/// </summary>
	internal ConverterCache ConverterCache { get; init; }

	/// <summary>
	/// Gets a pool of reference equality trackers, if the format supports reference preserving.
	/// </summary>
	internal virtual ReusableObjectPool<IReferenceEqualityTracker>? ReferenceTrackingPool { get; }

	/// <summary>
	/// Gets the formatter associated with this particular serializer.
	/// </summary>
	protected internal abstract Formatter Formatter { get; }

	/// <summary>
	/// Gets the deformatter associated with this particular serializer.
	/// </summary>
	protected internal abstract Deformatter Deformatter { get; }

	/// <inheritdoc cref="ConverterCache.RegisterConverter{T}(Converter{T})"/>
	public void RegisterConverter<T>(Converter<T> converter) => this.ConverterCache.RegisterConverter(converter);

	/// <inheritdoc cref="ConverterCache.RegisterKnownSubTypes{TBase}(KnownSubTypeMapping{TBase})"/>
	public void RegisterKnownSubTypes<TBase>(KnownSubTypeMapping<TBase> mapping) => this.ConverterCache.RegisterKnownSubTypes(mapping);

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
	public void SerializeObject(ref Writer writer, object? value, ITypeShape shape, CancellationToken cancellationToken = default)
	{
		Requires.NotNull(shape);

		using DisposableSerializationContext context = this.CreateSerializationContext(shape.Provider, cancellationToken);
		this.GetConverter(shape).WriteObject(ref writer, value, context.Value);
	}

	/// <summary>
	/// Serializes a value.
	/// </summary>
	/// <typeparam name="T">The type to be serialized.</typeparam>
	/// <param name="writer">The msgpack writer to use.</param>
	/// <param name="value">The value to serialize.</param>
	/// <param name="shape">The shape of <typeparamref name="T"/>.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	public void Serialize<T>(ref Writer writer, in T? value, ITypeShape<T> shape, CancellationToken cancellationToken = default)
	{
		Requires.NotNull(shape);
		using DisposableSerializationContext context = this.CreateSerializationContext(shape.Provider, cancellationToken);
		this.GetConverter(shape).Write(ref writer, value, context.Value);
	}

	/// <summary>
	/// Serializes a value.
	/// </summary>
	/// <typeparam name="T">The type to be serialized.</typeparam>
	/// <param name="writer">The msgpack writer to use.</param>
	/// <param name="value">The value to serialize.</param>
	/// <param name="provider"><inheritdoc cref="Deserialize{T}(ref Reader, ITypeShapeProvider, CancellationToken)" path="/param[@name='provider']"/></param>
	/// <param name="cancellationToken">A cancellation token.</param>
	public void Serialize<T>(ref Writer writer, in T? value, ITypeShapeProvider provider, CancellationToken cancellationToken = default)
	{
		using DisposableSerializationContext context = this.CreateSerializationContext(provider, cancellationToken);
		this.GetConverter<T>(provider).Write(ref writer, value, context.Value);
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
		AsyncWriter asyncWriter = new(writer, this.Formatter);
		using DisposableSerializationContext context = this.CreateSerializationContext(shape.Provider, cancellationToken);
		await this.ConverterCache.GetOrAddConverter(shape).WriteAsync(asyncWriter, value, context.Value).ConfigureAwait(false);
		asyncWriter.Flush();
#pragma warning restore NBMsgPackAsync
	}

	/// <summary>
	/// Serializes a value using the given <see cref="PipeWriter"/>.
	/// </summary>
	/// <typeparam name="T">The type to be serialized.</typeparam>
	/// <param name="writer">The writer to use.</param>
	/// <param name="value">The value to serialize.</param>
	/// <param name="provider"><inheritdoc cref="Deserialize{T}(ref Reader, ITypeShapeProvider, CancellationToken)" path="/param[@name='provider']"/></param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>A task that tracks the async serialization.</returns>
	public async ValueTask SerializeAsync<T>(PipeWriter writer, T? value, ITypeShapeProvider provider, CancellationToken cancellationToken = default)
	{
		Requires.NotNull(writer);
		cancellationToken.ThrowIfCancellationRequested();

#pragma warning disable NBMsgPackAsync
		AsyncWriter asyncWriter = new(writer, this.Formatter);
		using DisposableSerializationContext context = this.CreateSerializationContext(provider, cancellationToken);
		await this.ConverterCache.GetOrAddConverter<T>(provider).WriteAsync(asyncWriter, value, context.Value).ConfigureAwait(false);
		asyncWriter.Flush();
#pragma warning restore NBMsgPackAsync
	}

	/// <summary>
	/// Deserializes an untyped value.
	/// </summary>
	/// <param name="reader">The msgpack reader to deserialize from.</param>
	/// <param name="shape">The shape of the value to deserialize.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>The deserialized value.</returns>
	/// <example>
	/// See the <see cref="SerializeObject(ref Writer, object?, ITypeShape, CancellationToken)"/> method for an example of using this method.
	/// </example>
	public object? DeserializeObject(ref Reader reader, ITypeShape shape, CancellationToken cancellationToken = default)
	{
		Requires.NotNull(shape);

		using DisposableSerializationContext context = this.CreateSerializationContext(shape.Provider, cancellationToken);
		object? result = this.GetConverter(shape).ReadObject(ref reader, context.Value);
		return result;
	}

	/// <summary>
	/// Deserializes a value.
	/// </summary>
	/// <typeparam name="T">The type of value to deserialize.</typeparam>
	/// <param name="reader">The msgpack reader to deserialize from.</param>
	/// <param name="shape">The shape of <typeparamref name="T"/>.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>The deserialized value.</returns>
	public T? Deserialize<T>(ref Reader reader, ITypeShape<T> shape, CancellationToken cancellationToken = default)
	{
		Requires.NotNull(shape);
		using DisposableSerializationContext context = this.CreateSerializationContext(shape.Provider, cancellationToken);
		Converter<T> converter = this.GetConverter(shape);
		T? result = converter.Read(ref reader, context.Value);
		return result;
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
	public T? Deserialize<T>(ref Reader reader, ITypeShapeProvider provider, CancellationToken cancellationToken = default)
	{
		using DisposableSerializationContext context = this.CreateSerializationContext(provider, cancellationToken);
		T? result = this.ConverterCache.GetOrAddConverter<T>(provider).Read(ref reader, context.Value);
		return result;
	}

	/// <summary>
	/// Deserializes a value from a <see cref="PipeReader"/>.
	/// </summary>
	/// <typeparam name="T">The type of value to deserialize.</typeparam>
	/// <param name="reader">The reader to deserialize from. <see cref="PipeReader.Complete(Exception?)"/> is <em>not</em> called on this at the conclusion of deserialization, and the reader is left at the position after the last msgpack byte read.</param>
	/// <param name="shape">The shape of the type, as obtained from an <see cref="ITypeShapeProvider"/>.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>The deserialized value.</returns>
	public ValueTask<T?> DeserializeAsync<T>(PipeReader reader, ITypeShape<T> shape, CancellationToken cancellationToken = default)
		=> this.DeserializeAsync(Requires.NotNull(reader), Requires.NotNull(shape).Provider, this.GetConverter(shape), cancellationToken);

	/// <summary>
	/// Deserializes a value from a <see cref="PipeReader"/>.
	/// </summary>
	/// <typeparam name="T">The type of value to deserialize.</typeparam>
	/// <param name="reader">The reader to deserialize from.</param>
	/// <param name="provider"><inheritdoc cref="Deserialize{T}(ref Reader, ITypeShapeProvider, CancellationToken)" path="/param[@name='provider']"/></param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>The deserialized value.</returns>
	public ValueTask<T?> DeserializeAsync<T>(PipeReader reader, ITypeShapeProvider provider, CancellationToken cancellationToken = default)
		=> this.DeserializeAsync(Requires.NotNull(reader), Requires.NotNull(provider), this.GetConverter<T>(provider), cancellationToken);

	/// <inheritdoc cref="DeserializeEnumerableAsync{T}(PipeReader, ITypeShapeProvider, Converter{T}, CancellationToken)"/>
	/// <param name="shape"><inheritdoc cref="DeserializeAsync{T}(PipeReader, ITypeShape{T}, CancellationToken)" path="/param[@name='shape']"/></param>
#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
	public IAsyncEnumerable<T?> DeserializeEnumerableAsync<T>(PipeReader reader, ITypeShape<T> shape, CancellationToken cancellationToken = default)
#pragma warning restore CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
		=> this.DeserializeEnumerableAsync(Requires.NotNull(reader), Requires.NotNull(shape).Provider, this.GetConverter(shape), cancellationToken);

	/// <inheritdoc cref="DeserializeEnumerableAsync{T}(PipeReader, ITypeShapeProvider, Converter{T}, CancellationToken)"/>
	public IAsyncEnumerable<T?> DeserializeEnumerableAsync<T>(PipeReader reader, ITypeShapeProvider provider, CancellationToken cancellationToken = default)
		=> this.DeserializeEnumerableAsync(Requires.NotNull(reader), provider, this.GetConverter<T>(provider), cancellationToken);

	/// <inheritdoc cref="DeserializeEnumerableAsync{T, TElement}(PipeReader, ITypeShapeProvider, StreamingEnumerationOptions{T, TElement}, Converter{TElement}, CancellationToken)"/>
	/// <param name="shape"><inheritdoc cref="DeserializeAsync{T}(PipeReader, ITypeShape{T}, CancellationToken)" path="/param[@name='shape']"/></param>
#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
	public IAsyncEnumerable<TElement?> DeserializeEnumerableAsync<T, TElement>(PipeReader reader, ITypeShape<T> shape, StreamingEnumerationOptions<T, TElement> options, CancellationToken cancellationToken = default)
#pragma warning restore CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
		=> this.DeserializeEnumerableAsync(Requires.NotNull(reader), Requires.NotNull(shape).Provider, Requires.NotNull(options), this.GetConverter(shape.Provider.Resolve<TElement>()), cancellationToken);

	/// <inheritdoc cref="DeserializeEnumerableAsync{T, TElement}(PipeReader, ITypeShapeProvider, StreamingEnumerationOptions{T, TElement}, Converter{TElement}, CancellationToken)"/>
	public IAsyncEnumerable<TElement?> DeserializeEnumerableAsync<T, TElement>(PipeReader reader, ITypeShapeProvider provider, StreamingEnumerationOptions<T, TElement> options, CancellationToken cancellationToken = default)
		=> this.DeserializeEnumerableAsync(Requires.NotNull(reader), provider, Requires.NotNull(options), this.GetConverter<TElement>(provider), cancellationToken);

	/// <summary>
	/// Gets a converter for a given type shape.
	/// </summary>
	/// <param name="typeShape">The type shape.</param>
	/// <returns>A converter.</returns>
	internal Converter GetConverter(ITypeShape typeShape) => this.ConverterCache.GetOrAddConverter(typeShape);

	/// <summary>
	/// Gets a converter for a given type shape.
	/// </summary>
	/// <typeparam name="T">The type whose shape is given by <paramref name="typeShape"/>.</typeparam>
	/// <param name="typeShape">The type shape.</param>
	/// <returns>A converter.</returns>
	internal Converter<T> GetConverter<T>(ITypeShape<T> typeShape) => this.ConverterCache.GetOrAddConverter(typeShape);

	/// <summary>
	/// Gets a converter for a type, given a type provider.
	/// </summary>
	/// <typeparam name="T">The type whose shape is given by <paramref name="shapeProvider"/>.</typeparam>
	/// <param name="shapeProvider">The type shape provider.</param>
	/// <returns>A converter.</returns>
	internal Converter<T> GetConverter<T>(ITypeShapeProvider shapeProvider) => this.ConverterCache.GetOrAddConverter<T>(shapeProvider);

	/// <summary>
	/// A best-effort (possibly lossy or un-parseable) attempt to translate the structure
	/// at the current reader position into a JSON token.
	/// </summary>
	/// <param name="reader">The reader. This should only be advanced by one structure.</param>
	/// <param name="writer">The writer to emit JSON to.</param>
	/// <remarks>
	/// <para>
	/// Overridding methods need only handle tokens that would be <see cref="Converters.TokenType.Unknown"/>,
	/// since format-agnostic token types are already handled by the caller.
	/// </para>
	/// <para>
	/// When writing a JSON string, implementations should take care to apply proper JSON escaping as required.
	/// </para>
	/// </remarks>
	protected internal virtual void RenderAsJson(ref Reader reader, TextWriter writer)
	{
		Requires.NotNull(writer);

		writer.Write('"');
		writer.Write(Convert.ToBase64String(reader.ReadRaw(this.StartingContext).ToArray()));
		writer.Write('"');
	}

	/// <summary>
	/// Creates a new serialization context that is ready to process a serialization job.
	/// </summary>
	/// <param name="provider"><inheritdoc cref="Deserialize{T}(ref Reader, ITypeShapeProvider, CancellationToken)" path="/param[@name='provider']"/></param>
	/// <param name="cancellationToken">A cancellation token for the operation.</param>
	/// <returns>The serialization context.</returns>
	/// <remarks>
	/// Callers should be sure to always call <see cref="DisposableSerializationContext.Dispose"/> when done with the context.
	/// </remarks>
	protected DisposableSerializationContext CreateSerializationContext(ITypeShapeProvider provider, CancellationToken cancellationToken = default)
	{
		Requires.NotNull(provider);
		return new(this.StartingContext.Start(this, this.ConverterCache, this.ReferenceTrackingPool, provider, cancellationToken));
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
	private async IAsyncEnumerable<T?> DeserializeEnumerableAsync<T>(PipeReader reader, ITypeShapeProvider provider, Converter<T> converter, [EnumeratorCancellation] CancellationToken cancellationToken)
	{
		this.ThrowIfPreservingReferencesDuringEnumeration();

		cancellationToken.ThrowIfCancellationRequested();
		using DisposableSerializationContext context = this.CreateSerializationContext(provider, cancellationToken);

#pragma warning disable NBMsgPackAsync
		AsyncReader asyncReader = new(reader, this.Deformatter) { CancellationToken = cancellationToken };
		bool readMore = false;
		while (!await asyncReader.GetIsEndOfStreamAsync().ConfigureAwait(false))
		{
			if (readMore)
			{
				// We've proven that items *can* fit in the buffer, and that we've read all we can.
				// Try once to read more bytes to see if we can keep synchronously deserializing.
				await asyncReader.ReadAsync().ConfigureAwait(false);
				readMore = false;
			}

			// Use sync deserialization if we can because it's faster.
			int bufferedCount = asyncReader.GetBufferedStructuresCount(100, context.Value, out bool reachedMaxCount);
			if (bufferedCount > 0)
			{
				for (int i = 0; i < bufferedCount; i++)
				{
					Reader bufferedReader = ((AsyncReader)asyncReader).CreateBufferedReader();
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
	private async IAsyncEnumerable<TElement?> DeserializeEnumerableAsync<T, TElement>(PipeReader reader, ITypeShapeProvider provider, StreamingEnumerationOptions<T, TElement> options, Converter<TElement> converter, [EnumeratorCancellation] CancellationToken cancellationToken)
	{
		this.ThrowIfPreservingReferencesDuringEnumeration();

		cancellationToken.ThrowIfCancellationRequested();
		using DisposableSerializationContext context = this.CreateSerializationContext(provider, cancellationToken);

#pragma warning disable NBMsgPackAsync
		AsyncReader asyncReader = new(reader, this.Deformatter) { CancellationToken = cancellationToken };
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

	private void ThrowIfPreservingReferencesDuringEnumeration()
	{
		if (this.ConverterCache.PreserveReferences)
		{
			// This was failing in less expected ways, so we just disable the scenario.
			// It may not make so much sense anyway, given async enumeration clients may not want to retain references to all the objects
			// over potentially long periods of time.
			// If we ever enable this, we should think about whether to use weak references, or only preserve references within a single enumerated item's graph.
			throw new NotSupportedException($"Enumeration is not supported when {nameof(this.ConverterCache.PreserveReferences)} is enabled.");
		}
	}

	/// <summary>
	/// Deserializes a value from a <see cref="PipeReader"/>.
	/// </summary>
	/// <typeparam name="T">The type of value to be deserialized.</typeparam>
	/// <param name="reader">The <see cref="PipeReader"/> to read from.</param>
	/// <param name="provider"><inheritdoc cref="Deserialize{T}(ref Reader, ITypeShapeProvider, CancellationToken)" path="/param[@name='provider']"/></param>
	/// <param name="converter">The msgpack converter for the root data type.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>The deserialized value.</returns>
	private async ValueTask<T?> DeserializeAsync<T>(PipeReader reader, ITypeShapeProvider provider, Converter<T> converter, CancellationToken cancellationToken)
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
				Reader msgpackReader = new(readResult.Buffer, this.Deformatter);
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
		AsyncReader asyncReader = new(reader, this.Deformatter) { CancellationToken = cancellationToken };
		await asyncReader.ReadAsync().ConfigureAwait(false);
		T? result2 = await converter.ReadAsync(asyncReader, context.Value).ConfigureAwait(false);
		asyncReader.Dispose(); // only dispose this on success paths, since on exception it may throw (again) and conceal the original exception.
		return result2;
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
		/// When this value is <see langword="false"/>, a <see cref="SerializationException"/> is thrown when <see cref="Path"/> does not lead to a sequence.
		/// </remarks>
		public bool EmptySequenceForUndiscoverablePath { get; init; }
	}
}
