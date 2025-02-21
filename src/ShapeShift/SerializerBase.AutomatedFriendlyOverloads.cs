// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET

#pragma warning disable RS0026 // optional parameter on a method with overloads

using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;

namespace ShapeShift;

public partial record SerializerBase
{
	/// <inheritdoc cref="Serialize{T}(IBufferWriter{byte}, in T, ITypeShape{T}, CancellationToken)" />
	[ExcludeFromCodeCoverage]
	public void Serialize<T>(IBufferWriter<byte> writer, in T? value, CancellationToken cancellationToken = default)
		where T : IShapeable<T> => this.Serialize(writer, value, T.GetShape(), cancellationToken);

	/// <inheritdoc cref="Serialize{T}(Stream, in T, ITypeShape{T}, CancellationToken)" />
	[ExcludeFromCodeCoverage]
	public void Serialize<T>(Stream stream, in T? value, CancellationToken cancellationToken = default)
		where T : IShapeable<T> => this.Serialize(stream, value, T.GetShape(), cancellationToken);

	/// <inheritdoc cref="Deserialize{T}(ReadOnlyMemory{byte}, ITypeShape{T}, CancellationToken)" />
	[ExcludeFromCodeCoverage]
	public T? Deserialize<T>(ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default)
		where T : IShapeable<T> => this.Deserialize(bytes, T.GetShape(), cancellationToken);

	/// <inheritdoc cref="Deserialize{T}(in ReadOnlySequence{byte}, ITypeShape{T}, CancellationToken)" />
	[ExcludeFromCodeCoverage]
	public T? Deserialize<T>(scoped in ReadOnlySequence<byte> bytes, CancellationToken cancellationToken = default)
		where T : IShapeable<T> => this.Deserialize(bytes, T.GetShape(), cancellationToken);

	/// <inheritdoc cref="Deserialize{T}(Stream, ITypeShape{T}, CancellationToken)" />
	[ExcludeFromCodeCoverage]
	public T? Deserialize<T>(Stream stream, CancellationToken cancellationToken = default)
		where T : IShapeable<T> => this.Deserialize(stream, T.GetShape(), cancellationToken);

	/// <inheritdoc cref="DeserializeAsync{T}(PipeReader, ITypeShape{T}, CancellationToken)" />
#pragma warning disable RS0027 // optional parameter on a method with overloads
	[ExcludeFromCodeCoverage]
	public ValueTask<T?> DeserializeAsync<T>(PipeReader reader, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		where T : IShapeable<T> => this.DeserializeAsync(reader, T.GetShape(), cancellationToken);

	/// <inheritdoc cref="DeserializeEnumerableAsync{T}(PipeReader, ITypeShape{T}, CancellationToken)" />
#pragma warning disable RS0027 // optional parameter on a method with overloads
	[ExcludeFromCodeCoverage]
	public IAsyncEnumerable<T?> DeserializeEnumerableAsync<T>(PipeReader reader, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		where T : IShapeable<T> => this.DeserializeEnumerableAsync(reader, T.GetShape(), cancellationToken);

	/// <inheritdoc cref="DeserializeEnumerableAsync{T, TElement}(PipeReader, ITypeShape{T}, StreamingEnumerationOptions{T, TElement}, CancellationToken)" />
#pragma warning disable RS0027 // optional parameter on a method with overloads
	[ExcludeFromCodeCoverage]
	public IAsyncEnumerable<TElement?> DeserializeEnumerableAsync<T, TElement>(PipeReader reader, StreamingEnumerationOptions<T, TElement> options, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		where T : IShapeable<T> => this.DeserializeEnumerableAsync(reader, T.GetShape(), options, cancellationToken);

	/// <inheritdoc cref="DeserializeAsync{T}(Stream, ITypeShape{T}, CancellationToken)" />
#pragma warning disable RS0027 // optional parameter on a method with overloads
	[ExcludeFromCodeCoverage]
	public ValueTask<T?> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		where T : IShapeable<T> => this.DeserializeAsync(stream, T.GetShape(), cancellationToken);

	/// <inheritdoc cref="DeserializeEnumerableAsync{T}(Stream, ITypeShape{T}, CancellationToken)" />
#pragma warning disable RS0027 // optional parameter on a method with overloads
	[ExcludeFromCodeCoverage]
	public IAsyncEnumerable<T?> DeserializeEnumerableAsync<T>(Stream stream, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		where T : IShapeable<T> => this.DeserializeEnumerableAsync(stream, T.GetShape(), cancellationToken);

	/// <inheritdoc cref="DeserializeEnumerableAsync{T, TElement}(Stream, ITypeShape{T}, StreamingEnumerationOptions{T, TElement}, CancellationToken)" />
#pragma warning disable RS0027 // optional parameter on a method with overloads
	[ExcludeFromCodeCoverage]
	public IAsyncEnumerable<TElement?> DeserializeEnumerableAsync<T, TElement>(Stream stream, StreamingEnumerationOptions<T, TElement> options, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		where T : IShapeable<T> => this.DeserializeEnumerableAsync(stream, T.GetShape(), options, cancellationToken);

	/// <inheritdoc cref="SerializeAsync{T}(PipeWriter, T, ITypeShape{T}, CancellationToken)" />
#pragma warning disable RS0027 // optional parameter on a method with overloads
	[ExcludeFromCodeCoverage]
	public ValueTask SerializeAsync<T>(PipeWriter writer, in T? value, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		where T : IShapeable<T> => this.SerializeAsync(writer, value, T.GetShape(), cancellationToken);

	/// <inheritdoc cref="SerializeAsync{T}(Stream, T, ITypeShape{T}, CancellationToken)" />
#pragma warning disable RS0027 // optional parameter on a method with overloads
	[ExcludeFromCodeCoverage]
	public ValueTask SerializeAsync<T>(Stream stream, in T? value, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		where T : IShapeable<T> => this.SerializeAsync(stream, value, T.GetShape(), cancellationToken);

	/// <inheritdoc cref="Serialize{T}(IBufferWriter{byte}, in T, ITypeShape{T}, CancellationToken)" />
	[ExcludeFromCodeCoverage]
	public void Serialize<T, TProvider>(IBufferWriter<byte> writer, in T? value, CancellationToken cancellationToken = default)
		where TProvider : IShapeable<T> => this.Serialize(writer, value, TProvider.GetShape(), cancellationToken);

	/// <inheritdoc cref="Serialize{T}(Stream, in T, ITypeShape{T}, CancellationToken)" />
	[ExcludeFromCodeCoverage]
	public void Serialize<T, TProvider>(Stream stream, in T? value, CancellationToken cancellationToken = default)
		where TProvider : IShapeable<T> => this.Serialize(stream, value, TProvider.GetShape(), cancellationToken);

	/// <inheritdoc cref="Deserialize{T}(ReadOnlyMemory{byte}, ITypeShape{T}, CancellationToken)" />
	[ExcludeFromCodeCoverage]
	public T? Deserialize<T, TProvider>(ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default)
		where TProvider : IShapeable<T> => this.Deserialize(bytes, TProvider.GetShape(), cancellationToken);

	/// <inheritdoc cref="Deserialize{T}(in ReadOnlySequence{byte}, ITypeShape{T}, CancellationToken)" />
	[ExcludeFromCodeCoverage]
	public T? Deserialize<T, TProvider>(scoped in ReadOnlySequence<byte> bytes, CancellationToken cancellationToken = default)
		where TProvider : IShapeable<T> => this.Deserialize(bytes, TProvider.GetShape(), cancellationToken);

	/// <inheritdoc cref="Deserialize{T}(Stream, ITypeShape{T}, CancellationToken)" />
	[ExcludeFromCodeCoverage]
	public T? Deserialize<T, TProvider>(Stream stream, CancellationToken cancellationToken = default)
		where TProvider : IShapeable<T> => this.Deserialize(stream, TProvider.GetShape(), cancellationToken);

	/// <inheritdoc cref="DeserializeAsync{T}(PipeReader, ITypeShape{T}, CancellationToken)" />
#pragma warning disable RS0027 // optional parameter on a method with overloads
	[ExcludeFromCodeCoverage]
	public ValueTask<T?> DeserializeAsync<T, TProvider>(PipeReader reader, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		where TProvider : IShapeable<T> => this.DeserializeAsync(reader, TProvider.GetShape(), cancellationToken);

	/// <inheritdoc cref="DeserializeEnumerableAsync{T}(PipeReader, ITypeShape{T}, CancellationToken)" />
#pragma warning disable RS0027 // optional parameter on a method with overloads
	[ExcludeFromCodeCoverage]
	public IAsyncEnumerable<T?> DeserializeEnumerableAsync<T, TProvider>(PipeReader reader, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		where TProvider : IShapeable<T> => this.DeserializeEnumerableAsync(reader, TProvider.GetShape(), cancellationToken);

	/// <inheritdoc cref="DeserializeEnumerableAsync{T, TElement}(PipeReader, ITypeShape{T}, StreamingEnumerationOptions{T, TElement}, CancellationToken)" />
#pragma warning disable RS0027 // optional parameter on a method with overloads
	[ExcludeFromCodeCoverage]
	public IAsyncEnumerable<TElement?> DeserializeEnumerableAsync<T, TElement, TProvider>(PipeReader reader, StreamingEnumerationOptions<T, TElement> options, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		where TProvider : IShapeable<T> => this.DeserializeEnumerableAsync(reader, TProvider.GetShape(), options, cancellationToken);

	/// <inheritdoc cref="DeserializeAsync{T}(Stream, ITypeShape{T}, CancellationToken)" />
#pragma warning disable RS0027 // optional parameter on a method with overloads
	[ExcludeFromCodeCoverage]
	public ValueTask<T?> DeserializeAsync<T, TProvider>(Stream stream, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		where TProvider : IShapeable<T> => this.DeserializeAsync(stream, TProvider.GetShape(), cancellationToken);

	/// <inheritdoc cref="DeserializeEnumerableAsync{T}(Stream, ITypeShape{T}, CancellationToken)" />
#pragma warning disable RS0027 // optional parameter on a method with overloads
	[ExcludeFromCodeCoverage]
	public IAsyncEnumerable<T?> DeserializeEnumerableAsync<T, TProvider>(Stream stream, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		where TProvider : IShapeable<T> => this.DeserializeEnumerableAsync(stream, TProvider.GetShape(), cancellationToken);

	/// <inheritdoc cref="DeserializeEnumerableAsync{T, TElement}(Stream, ITypeShape{T}, StreamingEnumerationOptions{T, TElement}, CancellationToken)" />
#pragma warning disable RS0027 // optional parameter on a method with overloads
	[ExcludeFromCodeCoverage]
	public IAsyncEnumerable<TElement?> DeserializeEnumerableAsync<T, TElement, TProvider>(Stream stream, StreamingEnumerationOptions<T, TElement> options, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		where TProvider : IShapeable<T> => this.DeserializeEnumerableAsync(stream, TProvider.GetShape(), options, cancellationToken);

	/// <inheritdoc cref="SerializeAsync{T}(PipeWriter, T, ITypeShape{T}, CancellationToken)" />
#pragma warning disable RS0027 // optional parameter on a method with overloads
	[ExcludeFromCodeCoverage]
	public ValueTask SerializeAsync<T, TProvider>(PipeWriter writer, in T? value, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		where TProvider : IShapeable<T> => this.SerializeAsync(writer, value, TProvider.GetShape(), cancellationToken);

	/// <inheritdoc cref="SerializeAsync{T}(Stream, T, ITypeShape{T}, CancellationToken)" />
#pragma warning disable RS0027 // optional parameter on a method with overloads
	[ExcludeFromCodeCoverage]
	public ValueTask SerializeAsync<T, TProvider>(Stream stream, in T? value, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		where TProvider : IShapeable<T> => this.SerializeAsync(stream, value, TProvider.GetShape(), cancellationToken);
}

#endif

