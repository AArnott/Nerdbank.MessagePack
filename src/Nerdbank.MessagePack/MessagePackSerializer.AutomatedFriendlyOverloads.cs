// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1601 // Partial elements should be documented
#pragma warning disable RS0026 // optional parameter on a method with overloads

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using Microsoft;

namespace Nerdbank.MessagePack;

#if NET

public partial record MessagePackSerializer
{
	/// <inheritdoc cref="Serialize{T}(ref MessagePackWriter, in T, ITypeShape{T}, CancellationToken)" />
	[ExcludeFromCodeCoverage]
	public void Serialize<T>(ref MessagePackWriter writer, in T? value, CancellationToken cancellationToken = default)
		where T : IShapeable<T> => this.Serialize(ref writer, value, T.GetTypeShape(), cancellationToken);

	/// <inheritdoc cref="Serialize{T}(in T, ITypeShape{T}, CancellationToken)" />
	[ExcludeFromCodeCoverage]
	public byte[] Serialize<T>(in T? value, CancellationToken cancellationToken = default)
		where T : IShapeable<T> => this.Serialize(value, T.GetTypeShape(), cancellationToken);

	/// <inheritdoc cref="Serialize{T}(IBufferWriter{byte}, in T, ITypeShape{T}, CancellationToken)" />
	[ExcludeFromCodeCoverage]
	public void Serialize<T>(IBufferWriter<byte> writer, in T? value, CancellationToken cancellationToken = default)
		where T : IShapeable<T> => this.Serialize(writer, value, T.GetTypeShape(), cancellationToken);

	/// <inheritdoc cref="Serialize{T}(Stream, in T, ITypeShape{T}, CancellationToken)" />
	[ExcludeFromCodeCoverage]
	public void Serialize<T>(Stream stream, in T? value, CancellationToken cancellationToken = default)
		where T : IShapeable<T> => this.Serialize(stream, value, T.GetTypeShape(), cancellationToken);

	/// <inheritdoc cref="Deserialize{T}(ref MessagePackReader, ITypeShape{T}, CancellationToken)" />
	[ExcludeFromCodeCoverage]
	public T? Deserialize<T>(ref MessagePackReader reader, CancellationToken cancellationToken = default)
		where T : IShapeable<T> => this.Deserialize(ref reader, T.GetTypeShape(), cancellationToken);

	/// <inheritdoc cref="Deserialize{T}(ReadOnlyMemory{byte}, ITypeShape{T}, CancellationToken)" />
	[ExcludeFromCodeCoverage]
	public T? Deserialize<T>(ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default)
		where T : IShapeable<T> => this.Deserialize(bytes, T.GetTypeShape(), cancellationToken);

	/// <inheritdoc cref="Deserialize{T}(in ReadOnlySequence{byte}, ITypeShape{T}, CancellationToken)" />
	[ExcludeFromCodeCoverage]
	public T? Deserialize<T>(scoped in ReadOnlySequence<byte> bytes, CancellationToken cancellationToken = default)
		where T : IShapeable<T> => this.Deserialize(bytes, T.GetTypeShape(), cancellationToken);

	/// <inheritdoc cref="Deserialize{T}(Stream, ITypeShape{T}, CancellationToken)" />
	[ExcludeFromCodeCoverage]
	public T? Deserialize<T>(Stream stream, CancellationToken cancellationToken = default)
		where T : IShapeable<T> => this.Deserialize(stream, T.GetTypeShape(), cancellationToken);

	/// <inheritdoc cref="DeserializeAsync{T}(PipeReader, ITypeShape{T}, CancellationToken)" />
#pragma warning disable RS0027 // optional parameter on a method with overloads
	[ExcludeFromCodeCoverage]
	public ValueTask<T?> DeserializeAsync<T>(PipeReader reader, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		where T : IShapeable<T> => this.DeserializeAsync(reader, T.GetTypeShape(), cancellationToken);

	/// <inheritdoc cref="DeserializeEnumerableAsync{T}(PipeReader, ITypeShape{T}, CancellationToken)" />
#pragma warning disable RS0027 // optional parameter on a method with overloads
	[ExcludeFromCodeCoverage]
	public IAsyncEnumerable<T?> DeserializeEnumerableAsync<T>(PipeReader reader, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		where T : IShapeable<T> => this.DeserializeEnumerableAsync(reader, T.GetTypeShape(), cancellationToken);

	/// <inheritdoc cref="DeserializePathEnumerableAsync{T, TElement}(PipeReader, ITypeShape{T}, StreamingEnumerationOptions{T, TElement}, CancellationToken)" />
#pragma warning disable RS0027 // optional parameter on a method with overloads
	[ExcludeFromCodeCoverage]
	public IAsyncEnumerable<TElement?> DeserializePathEnumerableAsync<T, TElement>(PipeReader reader, StreamingEnumerationOptions<T, TElement> options, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		where T : IShapeable<T> => this.DeserializePathEnumerableAsync(reader, T.GetTypeShape(), options, cancellationToken);

	/// <inheritdoc cref="DeserializeAsync{T}(Stream, ITypeShape{T}, CancellationToken)" />
#pragma warning disable RS0027 // optional parameter on a method with overloads
	[ExcludeFromCodeCoverage]
	public ValueTask<T?> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		where T : IShapeable<T> => this.DeserializeAsync(stream, T.GetTypeShape(), cancellationToken);

	/// <inheritdoc cref="DeserializeEnumerableAsync{T}(Stream, ITypeShape{T}, CancellationToken)" />
#pragma warning disable RS0027 // optional parameter on a method with overloads
	[ExcludeFromCodeCoverage]
	public IAsyncEnumerable<T?> DeserializeEnumerableAsync<T>(Stream stream, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		where T : IShapeable<T> => this.DeserializeEnumerableAsync(stream, T.GetTypeShape(), cancellationToken);

	/// <inheritdoc cref="DeserializePathEnumerableAsync{T, TElement}(Stream, ITypeShape{T}, StreamingEnumerationOptions{T, TElement}, CancellationToken)" />
#pragma warning disable RS0027 // optional parameter on a method with overloads
	[ExcludeFromCodeCoverage]
	public IAsyncEnumerable<TElement?> DeserializePathEnumerableAsync<T, TElement>(Stream stream, StreamingEnumerationOptions<T, TElement> options, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		where T : IShapeable<T> => this.DeserializePathEnumerableAsync(stream, T.GetTypeShape(), options, cancellationToken);

	/// <inheritdoc cref="DeserializePath{T, TElement}(ref MessagePackReader, ITypeShape{T}, in DeserializePathOptions{T, TElement}, CancellationToken)" />
	[ExcludeFromCodeCoverage]
	public TElement? DeserializePath<T, TElement>(ref MessagePackReader reader, in DeserializePathOptions<T, TElement> options, CancellationToken cancellationToken = default)
		where T : IShapeable<T> => this.DeserializePath(ref reader, T.GetTypeShape(), options, cancellationToken);

	/// <inheritdoc cref="DeserializePath{T, TElement}(ReadOnlyMemory{byte}, ITypeShape{T}, in DeserializePathOptions{T, TElement}, CancellationToken)" />
	[ExcludeFromCodeCoverage]
	public TElement? DeserializePath<T, TElement>(ReadOnlyMemory<byte> bytes, in DeserializePathOptions<T, TElement> options, CancellationToken cancellationToken = default)
		where T : IShapeable<T> => this.DeserializePath(bytes, T.GetTypeShape(), options, cancellationToken);

	/// <inheritdoc cref="DeserializePath{T, TElement}(in ReadOnlySequence{byte}, ITypeShape{T}, in DeserializePathOptions{T, TElement}, CancellationToken)" />
	[ExcludeFromCodeCoverage]
	public TElement? DeserializePath<T, TElement>(scoped in ReadOnlySequence<byte> bytes, in DeserializePathOptions<T, TElement> options, CancellationToken cancellationToken = default)
		where T : IShapeable<T> => this.DeserializePath(bytes, T.GetTypeShape(), options, cancellationToken);

	/// <inheritdoc cref="DeserializePath{T, TElement}(Stream, ITypeShape{T}, in DeserializePathOptions{T, TElement}, CancellationToken)" />
	[ExcludeFromCodeCoverage]
	public TElement? DeserializePath<T, TElement>(Stream stream, in DeserializePathOptions<T, TElement> options, CancellationToken cancellationToken = default)
		where T : IShapeable<T> => this.DeserializePath(stream, T.GetTypeShape(), options, cancellationToken);

	/// <inheritdoc cref="SerializeAsync{T}(PipeWriter, T, ITypeShape{T}, CancellationToken)" />
#pragma warning disable RS0027 // optional parameter on a method with overloads
	[ExcludeFromCodeCoverage]
	public ValueTask SerializeAsync<T>(PipeWriter writer, in T? value, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		where T : IShapeable<T> => this.SerializeAsync(writer, value, T.GetTypeShape(), cancellationToken);

	/// <inheritdoc cref="SerializeAsync{T}(Stream, T, ITypeShape{T}, CancellationToken)" />
#pragma warning disable RS0027 // optional parameter on a method with overloads
	[ExcludeFromCodeCoverage]
	public ValueTask SerializeAsync<T>(Stream stream, in T? value, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		where T : IShapeable<T> => this.SerializeAsync(stream, value, T.GetTypeShape(), cancellationToken);

	/// <inheritdoc cref="Serialize{T}(ref MessagePackWriter, in T, ITypeShape{T}, CancellationToken)" />
	[ExcludeFromCodeCoverage]
	public void Serialize<T, TProvider>(ref MessagePackWriter writer, in T? value, CancellationToken cancellationToken = default)
		where TProvider : IShapeable<T> => this.Serialize(ref writer, value, TProvider.GetTypeShape(), cancellationToken);

	/// <inheritdoc cref="Serialize{T}(in T, ITypeShape{T}, CancellationToken)" />
	[ExcludeFromCodeCoverage]
	public byte[] Serialize<T, TProvider>(in T? value, CancellationToken cancellationToken = default)
		where TProvider : IShapeable<T> => this.Serialize(value, TProvider.GetTypeShape(), cancellationToken);

	/// <inheritdoc cref="Serialize{T}(IBufferWriter{byte}, in T, ITypeShape{T}, CancellationToken)" />
	[ExcludeFromCodeCoverage]
	public void Serialize<T, TProvider>(IBufferWriter<byte> writer, in T? value, CancellationToken cancellationToken = default)
		where TProvider : IShapeable<T> => this.Serialize(writer, value, TProvider.GetTypeShape(), cancellationToken);

	/// <inheritdoc cref="Serialize{T}(Stream, in T, ITypeShape{T}, CancellationToken)" />
	[ExcludeFromCodeCoverage]
	public void Serialize<T, TProvider>(Stream stream, in T? value, CancellationToken cancellationToken = default)
		where TProvider : IShapeable<T> => this.Serialize(stream, value, TProvider.GetTypeShape(), cancellationToken);

	/// <inheritdoc cref="Deserialize{T}(ref MessagePackReader, ITypeShape{T}, CancellationToken)" />
	[ExcludeFromCodeCoverage]
	public T? Deserialize<T, TProvider>(ref MessagePackReader reader, CancellationToken cancellationToken = default)
		where TProvider : IShapeable<T> => this.Deserialize(ref reader, TProvider.GetTypeShape(), cancellationToken);

	/// <inheritdoc cref="Deserialize{T}(ReadOnlyMemory{byte}, ITypeShape{T}, CancellationToken)" />
	[ExcludeFromCodeCoverage]
	public T? Deserialize<T, TProvider>(ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default)
		where TProvider : IShapeable<T> => this.Deserialize(bytes, TProvider.GetTypeShape(), cancellationToken);

	/// <inheritdoc cref="Deserialize{T}(in ReadOnlySequence{byte}, ITypeShape{T}, CancellationToken)" />
	[ExcludeFromCodeCoverage]
	public T? Deserialize<T, TProvider>(scoped in ReadOnlySequence<byte> bytes, CancellationToken cancellationToken = default)
		where TProvider : IShapeable<T> => this.Deserialize(bytes, TProvider.GetTypeShape(), cancellationToken);

	/// <inheritdoc cref="Deserialize{T}(Stream, ITypeShape{T}, CancellationToken)" />
	[ExcludeFromCodeCoverage]
	public T? Deserialize<T, TProvider>(Stream stream, CancellationToken cancellationToken = default)
		where TProvider : IShapeable<T> => this.Deserialize(stream, TProvider.GetTypeShape(), cancellationToken);

	/// <inheritdoc cref="DeserializeAsync{T}(PipeReader, ITypeShape{T}, CancellationToken)" />
#pragma warning disable RS0027 // optional parameter on a method with overloads
	[ExcludeFromCodeCoverage]
	public ValueTask<T?> DeserializeAsync<T, TProvider>(PipeReader reader, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		where TProvider : IShapeable<T> => this.DeserializeAsync(reader, TProvider.GetTypeShape(), cancellationToken);

	/// <inheritdoc cref="DeserializeEnumerableAsync{T}(PipeReader, ITypeShape{T}, CancellationToken)" />
#pragma warning disable RS0027 // optional parameter on a method with overloads
	[ExcludeFromCodeCoverage]
	public IAsyncEnumerable<T?> DeserializeEnumerableAsync<T, TProvider>(PipeReader reader, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		where TProvider : IShapeable<T> => this.DeserializeEnumerableAsync(reader, TProvider.GetTypeShape(), cancellationToken);

	/// <inheritdoc cref="DeserializePathEnumerableAsync{T, TElement}(PipeReader, ITypeShape{T}, StreamingEnumerationOptions{T, TElement}, CancellationToken)" />
#pragma warning disable RS0027 // optional parameter on a method with overloads
	[ExcludeFromCodeCoverage]
	public IAsyncEnumerable<TElement?> DeserializePathEnumerableAsync<T, TElement, TProvider>(PipeReader reader, StreamingEnumerationOptions<T, TElement> options, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		where TProvider : IShapeable<T> => this.DeserializePathEnumerableAsync(reader, TProvider.GetTypeShape(), options, cancellationToken);

	/// <inheritdoc cref="DeserializeAsync{T}(Stream, ITypeShape{T}, CancellationToken)" />
#pragma warning disable RS0027 // optional parameter on a method with overloads
	[ExcludeFromCodeCoverage]
	public ValueTask<T?> DeserializeAsync<T, TProvider>(Stream stream, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		where TProvider : IShapeable<T> => this.DeserializeAsync(stream, TProvider.GetTypeShape(), cancellationToken);

	/// <inheritdoc cref="DeserializeEnumerableAsync{T}(Stream, ITypeShape{T}, CancellationToken)" />
#pragma warning disable RS0027 // optional parameter on a method with overloads
	[ExcludeFromCodeCoverage]
	public IAsyncEnumerable<T?> DeserializeEnumerableAsync<T, TProvider>(Stream stream, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		where TProvider : IShapeable<T> => this.DeserializeEnumerableAsync(stream, TProvider.GetTypeShape(), cancellationToken);

	/// <inheritdoc cref="DeserializePathEnumerableAsync{T, TElement}(Stream, ITypeShape{T}, StreamingEnumerationOptions{T, TElement}, CancellationToken)" />
#pragma warning disable RS0027 // optional parameter on a method with overloads
	[ExcludeFromCodeCoverage]
	public IAsyncEnumerable<TElement?> DeserializePathEnumerableAsync<T, TElement, TProvider>(Stream stream, StreamingEnumerationOptions<T, TElement> options, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		where TProvider : IShapeable<T> => this.DeserializePathEnumerableAsync(stream, TProvider.GetTypeShape(), options, cancellationToken);

	/// <inheritdoc cref="DeserializePath{T, TElement}(ref MessagePackReader, ITypeShape{T}, in DeserializePathOptions{T, TElement}, CancellationToken)" />
	[ExcludeFromCodeCoverage]
	public TElement? DeserializePath<T, TElement, TProvider>(ref MessagePackReader reader, in DeserializePathOptions<T, TElement> options, CancellationToken cancellationToken = default)
		where TProvider : IShapeable<T> => this.DeserializePath(ref reader, TProvider.GetTypeShape(), options, cancellationToken);

	/// <inheritdoc cref="DeserializePath{T, TElement}(ReadOnlyMemory{byte}, ITypeShape{T}, in DeserializePathOptions{T, TElement}, CancellationToken)" />
	[ExcludeFromCodeCoverage]
	public TElement? DeserializePath<T, TElement, TProvider>(ReadOnlyMemory<byte> bytes, in DeserializePathOptions<T, TElement> options, CancellationToken cancellationToken = default)
		where TProvider : IShapeable<T> => this.DeserializePath(bytes, TProvider.GetTypeShape(), options, cancellationToken);

	/// <inheritdoc cref="DeserializePath{T, TElement}(in ReadOnlySequence{byte}, ITypeShape{T}, in DeserializePathOptions{T, TElement}, CancellationToken)" />
	[ExcludeFromCodeCoverage]
	public TElement? DeserializePath<T, TElement, TProvider>(scoped in ReadOnlySequence<byte> bytes, in DeserializePathOptions<T, TElement> options, CancellationToken cancellationToken = default)
		where TProvider : IShapeable<T> => this.DeserializePath(bytes, TProvider.GetTypeShape(), options, cancellationToken);

	/// <inheritdoc cref="DeserializePath{T, TElement}(Stream, ITypeShape{T}, in DeserializePathOptions{T, TElement}, CancellationToken)" />
	[ExcludeFromCodeCoverage]
	public TElement? DeserializePath<T, TElement, TProvider>(Stream stream, in DeserializePathOptions<T, TElement> options, CancellationToken cancellationToken = default)
		where TProvider : IShapeable<T> => this.DeserializePath(stream, TProvider.GetTypeShape(), options, cancellationToken);

	/// <inheritdoc cref="SerializeAsync{T}(PipeWriter, T, ITypeShape{T}, CancellationToken)" />
#pragma warning disable RS0027 // optional parameter on a method with overloads
	[ExcludeFromCodeCoverage]
	public ValueTask SerializeAsync<T, TProvider>(PipeWriter writer, in T? value, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		where TProvider : IShapeable<T> => this.SerializeAsync(writer, value, TProvider.GetTypeShape(), cancellationToken);

	/// <inheritdoc cref="SerializeAsync{T}(Stream, T, ITypeShape{T}, CancellationToken)" />
#pragma warning disable RS0027 // optional parameter on a method with overloads
	[ExcludeFromCodeCoverage]
	public ValueTask SerializeAsync<T, TProvider>(Stream stream, in T? value, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		where TProvider : IShapeable<T> => this.SerializeAsync(stream, value, TProvider.GetTypeShape(), cancellationToken);
}

#endif

public static partial class MessagePackSerializerExtensions
{
	/// <inheritdoc cref="MessagePackSerializer.Serialize{T}(ref MessagePackWriter, in T, ITypeShape{T}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="T"/> has no type shape created via the <see cref="GenerateShapeAttribute"/> source generator.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="T"/> is decorated with the <see cref="GenerateShapeAttribute"/>.
	/// For non-decorated types, apply <see cref="GenerateShapeForAttribute{T}"/> to a witness type and call <see cref="GetJsonSchema{T, TProvider}"/> instead,
	/// or use an overload that accepts a <see cref="ITypeShape{T}"/> for an option that does not require source generation.
	/// </remarks>
	[ExcludeFromCodeCoverage]
#if NET8_0
	[RequiresDynamicCode(ResolveDynamicMessage)]
#endif
#if NET
	[PreferDotNetAlternativeApi(MessagePackSerializer.PreferTypeConstrainedInstanceOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public static void Serialize<T>(this MessagePackSerializer self, ref MessagePackWriter writer, in T? value, CancellationToken cancellationToken = default)
		=> Requires.NotNull(self).Serialize(ref writer, value, ResolveTypeShapeOrThrow<T>(), cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.Serialize{T}(in T, ITypeShape{T}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="T"/> has no type shape created via the <see cref="GenerateShapeAttribute"/> source generator.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="T"/> is decorated with the <see cref="GenerateShapeAttribute"/>.
	/// For non-decorated types, apply <see cref="GenerateShapeForAttribute{T}"/> to a witness type and call <see cref="GetJsonSchema{T, TProvider}"/> instead,
	/// or use an overload that accepts a <see cref="ITypeShape{T}"/> for an option that does not require source generation.
	/// </remarks>
	[ExcludeFromCodeCoverage]
#if NET8_0
	[RequiresDynamicCode(ResolveDynamicMessage)]
#endif
#if NET
	[PreferDotNetAlternativeApi(MessagePackSerializer.PreferTypeConstrainedInstanceOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public static byte[] Serialize<T>(this MessagePackSerializer self, in T? value, CancellationToken cancellationToken = default)
		=> Requires.NotNull(self).Serialize(value, ResolveTypeShapeOrThrow<T>(), cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.Serialize{T}(IBufferWriter{byte}, in T, ITypeShape{T}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="T"/> has no type shape created via the <see cref="GenerateShapeAttribute"/> source generator.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="T"/> is decorated with the <see cref="GenerateShapeAttribute"/>.
	/// For non-decorated types, apply <see cref="GenerateShapeForAttribute{T}"/> to a witness type and call <see cref="GetJsonSchema{T, TProvider}"/> instead,
	/// or use an overload that accepts a <see cref="ITypeShape{T}"/> for an option that does not require source generation.
	/// </remarks>
	[ExcludeFromCodeCoverage]
#if NET8_0
	[RequiresDynamicCode(ResolveDynamicMessage)]
#endif
#if NET
	[PreferDotNetAlternativeApi(MessagePackSerializer.PreferTypeConstrainedInstanceOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public static void Serialize<T>(this MessagePackSerializer self, IBufferWriter<byte> writer, in T? value, CancellationToken cancellationToken = default)
		=> Requires.NotNull(self).Serialize(writer, value, ResolveTypeShapeOrThrow<T>(), cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.Serialize{T}(Stream, in T, ITypeShape{T}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="T"/> has no type shape created via the <see cref="GenerateShapeAttribute"/> source generator.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="T"/> is decorated with the <see cref="GenerateShapeAttribute"/>.
	/// For non-decorated types, apply <see cref="GenerateShapeForAttribute{T}"/> to a witness type and call <see cref="GetJsonSchema{T, TProvider}"/> instead,
	/// or use an overload that accepts a <see cref="ITypeShape{T}"/> for an option that does not require source generation.
	/// </remarks>
	[ExcludeFromCodeCoverage]
#if NET8_0
	[RequiresDynamicCode(ResolveDynamicMessage)]
#endif
#if NET
	[PreferDotNetAlternativeApi(MessagePackSerializer.PreferTypeConstrainedInstanceOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public static void Serialize<T>(this MessagePackSerializer self, Stream stream, in T? value, CancellationToken cancellationToken = default)
		=> Requires.NotNull(self).Serialize(stream, value, ResolveTypeShapeOrThrow<T>(), cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.Deserialize{T}(ref MessagePackReader, ITypeShape{T}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="T"/> has no type shape created via the <see cref="GenerateShapeAttribute"/> source generator.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="T"/> is decorated with the <see cref="GenerateShapeAttribute"/>.
	/// For non-decorated types, apply <see cref="GenerateShapeForAttribute{T}"/> to a witness type and call <see cref="GetJsonSchema{T, TProvider}"/> instead,
	/// or use an overload that accepts a <see cref="ITypeShape{T}"/> for an option that does not require source generation.
	/// </remarks>
	[ExcludeFromCodeCoverage]
#if NET8_0
	[RequiresDynamicCode(ResolveDynamicMessage)]
#endif
#if NET
	[PreferDotNetAlternativeApi(MessagePackSerializer.PreferTypeConstrainedInstanceOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public static T? Deserialize<T>(this MessagePackSerializer self, ref MessagePackReader reader, CancellationToken cancellationToken = default)
		=> Requires.NotNull(self).Deserialize(ref reader, ResolveTypeShapeOrThrow<T>(), cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.Deserialize{T}(ReadOnlyMemory{byte}, ITypeShape{T}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="T"/> has no type shape created via the <see cref="GenerateShapeAttribute"/> source generator.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="T"/> is decorated with the <see cref="GenerateShapeAttribute"/>.
	/// For non-decorated types, apply <see cref="GenerateShapeForAttribute{T}"/> to a witness type and call <see cref="GetJsonSchema{T, TProvider}"/> instead,
	/// or use an overload that accepts a <see cref="ITypeShape{T}"/> for an option that does not require source generation.
	/// </remarks>
	[ExcludeFromCodeCoverage]
#if NET8_0
	[RequiresDynamicCode(ResolveDynamicMessage)]
#endif
#if NET
	[PreferDotNetAlternativeApi(MessagePackSerializer.PreferTypeConstrainedInstanceOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public static T? Deserialize<T>(this MessagePackSerializer self, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default)
		=> Requires.NotNull(self).Deserialize(bytes, ResolveTypeShapeOrThrow<T>(), cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.Deserialize{T}(in ReadOnlySequence{byte}, ITypeShape{T}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="T"/> has no type shape created via the <see cref="GenerateShapeAttribute"/> source generator.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="T"/> is decorated with the <see cref="GenerateShapeAttribute"/>.
	/// For non-decorated types, apply <see cref="GenerateShapeForAttribute{T}"/> to a witness type and call <see cref="GetJsonSchema{T, TProvider}"/> instead,
	/// or use an overload that accepts a <see cref="ITypeShape{T}"/> for an option that does not require source generation.
	/// </remarks>
	[ExcludeFromCodeCoverage]
#if NET8_0
	[RequiresDynamicCode(ResolveDynamicMessage)]
#endif
#if NET
	[PreferDotNetAlternativeApi(MessagePackSerializer.PreferTypeConstrainedInstanceOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public static T? Deserialize<T>(this MessagePackSerializer self, scoped in ReadOnlySequence<byte> bytes, CancellationToken cancellationToken = default)
		=> Requires.NotNull(self).Deserialize(bytes, ResolveTypeShapeOrThrow<T>(), cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.Deserialize{T}(Stream, ITypeShape{T}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="T"/> has no type shape created via the <see cref="GenerateShapeAttribute"/> source generator.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="T"/> is decorated with the <see cref="GenerateShapeAttribute"/>.
	/// For non-decorated types, apply <see cref="GenerateShapeForAttribute{T}"/> to a witness type and call <see cref="GetJsonSchema{T, TProvider}"/> instead,
	/// or use an overload that accepts a <see cref="ITypeShape{T}"/> for an option that does not require source generation.
	/// </remarks>
	[ExcludeFromCodeCoverage]
#if NET8_0
	[RequiresDynamicCode(ResolveDynamicMessage)]
#endif
#if NET
	[PreferDotNetAlternativeApi(MessagePackSerializer.PreferTypeConstrainedInstanceOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public static T? Deserialize<T>(this MessagePackSerializer self, Stream stream, CancellationToken cancellationToken = default)
		=> Requires.NotNull(self).Deserialize(stream, ResolveTypeShapeOrThrow<T>(), cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.DeserializeAsync{T}(PipeReader, ITypeShape{T}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="T"/> has no type shape created via the <see cref="GenerateShapeAttribute"/> source generator.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="T"/> is decorated with the <see cref="GenerateShapeAttribute"/>.
	/// For non-decorated types, apply <see cref="GenerateShapeForAttribute{T}"/> to a witness type and call <see cref="GetJsonSchema{T, TProvider}"/> instead,
	/// or use an overload that accepts a <see cref="ITypeShape{T}"/> for an option that does not require source generation.
	/// </remarks>
#pragma warning disable RS0027 // optional parameter on a method with overloads
	[ExcludeFromCodeCoverage]
#if NET8_0
	[RequiresDynamicCode(ResolveDynamicMessage)]
#endif
#if NET
	[PreferDotNetAlternativeApi(MessagePackSerializer.PreferTypeConstrainedInstanceOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public static ValueTask<T?> DeserializeAsync<T>(this MessagePackSerializer self, PipeReader reader, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		=> Requires.NotNull(self).DeserializeAsync(reader, ResolveTypeShapeOrThrow<T>(), cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.DeserializeEnumerableAsync{T}(PipeReader, ITypeShape{T}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="T"/> has no type shape created via the <see cref="GenerateShapeAttribute"/> source generator.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="T"/> is decorated with the <see cref="GenerateShapeAttribute"/>.
	/// For non-decorated types, apply <see cref="GenerateShapeForAttribute{T}"/> to a witness type and call <see cref="GetJsonSchema{T, TProvider}"/> instead,
	/// or use an overload that accepts a <see cref="ITypeShape{T}"/> for an option that does not require source generation.
	/// </remarks>
#pragma warning disable RS0027 // optional parameter on a method with overloads
	[ExcludeFromCodeCoverage]
#if NET8_0
	[RequiresDynamicCode(ResolveDynamicMessage)]
#endif
#if NET
	[PreferDotNetAlternativeApi(MessagePackSerializer.PreferTypeConstrainedInstanceOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public static IAsyncEnumerable<T?> DeserializeEnumerableAsync<T>(this MessagePackSerializer self, PipeReader reader, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		=> Requires.NotNull(self).DeserializeEnumerableAsync(reader, ResolveTypeShapeOrThrow<T>(), cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.DeserializePathEnumerableAsync{T, TElement}(PipeReader, ITypeShape{T}, MessagePackSerializer.StreamingEnumerationOptions{T, TElement}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="T"/> has no type shape created via the <see cref="GenerateShapeAttribute"/> source generator.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="T"/> is decorated with the <see cref="GenerateShapeAttribute"/>.
	/// For non-decorated types, apply <see cref="GenerateShapeForAttribute{T}"/> to a witness type and call <see cref="GetJsonSchema{T, TProvider}"/> instead,
	/// or use an overload that accepts a <see cref="ITypeShape{T}"/> for an option that does not require source generation.
	/// </remarks>
#pragma warning disable RS0027 // optional parameter on a method with overloads
	[ExcludeFromCodeCoverage]
#if NET8_0
	[RequiresDynamicCode(ResolveDynamicMessage)]
#endif
#if NET
	[PreferDotNetAlternativeApi(MessagePackSerializer.PreferTypeConstrainedInstanceOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public static IAsyncEnumerable<TElement?> DeserializePathEnumerableAsync<T, TElement>(this MessagePackSerializer self, PipeReader reader, MessagePackSerializer.StreamingEnumerationOptions<T, TElement> options, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		=> Requires.NotNull(self).DeserializePathEnumerableAsync(reader, ResolveTypeShapeOrThrow<T>(), options, cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.DeserializeAsync{T}(Stream, ITypeShape{T}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="T"/> has no type shape created via the <see cref="GenerateShapeAttribute"/> source generator.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="T"/> is decorated with the <see cref="GenerateShapeAttribute"/>.
	/// For non-decorated types, apply <see cref="GenerateShapeForAttribute{T}"/> to a witness type and call <see cref="GetJsonSchema{T, TProvider}"/> instead,
	/// or use an overload that accepts a <see cref="ITypeShape{T}"/> for an option that does not require source generation.
	/// </remarks>
#pragma warning disable RS0027 // optional parameter on a method with overloads
	[ExcludeFromCodeCoverage]
#if NET8_0
	[RequiresDynamicCode(ResolveDynamicMessage)]
#endif
#if NET
	[PreferDotNetAlternativeApi(MessagePackSerializer.PreferTypeConstrainedInstanceOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public static ValueTask<T?> DeserializeAsync<T>(this MessagePackSerializer self, Stream stream, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		=> Requires.NotNull(self).DeserializeAsync(stream, ResolveTypeShapeOrThrow<T>(), cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.DeserializeEnumerableAsync{T}(Stream, ITypeShape{T}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="T"/> has no type shape created via the <see cref="GenerateShapeAttribute"/> source generator.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="T"/> is decorated with the <see cref="GenerateShapeAttribute"/>.
	/// For non-decorated types, apply <see cref="GenerateShapeForAttribute{T}"/> to a witness type and call <see cref="GetJsonSchema{T, TProvider}"/> instead,
	/// or use an overload that accepts a <see cref="ITypeShape{T}"/> for an option that does not require source generation.
	/// </remarks>
#pragma warning disable RS0027 // optional parameter on a method with overloads
	[ExcludeFromCodeCoverage]
#if NET8_0
	[RequiresDynamicCode(ResolveDynamicMessage)]
#endif
#if NET
	[PreferDotNetAlternativeApi(MessagePackSerializer.PreferTypeConstrainedInstanceOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public static IAsyncEnumerable<T?> DeserializeEnumerableAsync<T>(this MessagePackSerializer self, Stream stream, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		=> Requires.NotNull(self).DeserializeEnumerableAsync(stream, ResolveTypeShapeOrThrow<T>(), cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.DeserializePathEnumerableAsync{T, TElement}(Stream, ITypeShape{T}, MessagePackSerializer.StreamingEnumerationOptions{T, TElement}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="T"/> has no type shape created via the <see cref="GenerateShapeAttribute"/> source generator.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="T"/> is decorated with the <see cref="GenerateShapeAttribute"/>.
	/// For non-decorated types, apply <see cref="GenerateShapeForAttribute{T}"/> to a witness type and call <see cref="GetJsonSchema{T, TProvider}"/> instead,
	/// or use an overload that accepts a <see cref="ITypeShape{T}"/> for an option that does not require source generation.
	/// </remarks>
#pragma warning disable RS0027 // optional parameter on a method with overloads
	[ExcludeFromCodeCoverage]
#if NET8_0
	[RequiresDynamicCode(ResolveDynamicMessage)]
#endif
#if NET
	[PreferDotNetAlternativeApi(MessagePackSerializer.PreferTypeConstrainedInstanceOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public static IAsyncEnumerable<TElement?> DeserializePathEnumerableAsync<T, TElement>(this MessagePackSerializer self, Stream stream, MessagePackSerializer.StreamingEnumerationOptions<T, TElement> options, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		=> Requires.NotNull(self).DeserializePathEnumerableAsync(stream, ResolveTypeShapeOrThrow<T>(), options, cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.DeserializePath{T, TElement}(ref MessagePackReader, ITypeShape{T}, in MessagePackSerializer.DeserializePathOptions{T, TElement}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="T"/> has no type shape created via the <see cref="GenerateShapeAttribute"/> source generator.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="T"/> is decorated with the <see cref="GenerateShapeAttribute"/>.
	/// For non-decorated types, apply <see cref="GenerateShapeForAttribute{T}"/> to a witness type and call <see cref="GetJsonSchema{T, TProvider}"/> instead,
	/// or use an overload that accepts a <see cref="ITypeShape{T}"/> for an option that does not require source generation.
	/// </remarks>
	[ExcludeFromCodeCoverage]
#if NET8_0
	[RequiresDynamicCode(ResolveDynamicMessage)]
#endif
#if NET
	[PreferDotNetAlternativeApi(MessagePackSerializer.PreferTypeConstrainedInstanceOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public static TElement? DeserializePath<T, TElement>(this MessagePackSerializer self, ref MessagePackReader reader, in MessagePackSerializer.DeserializePathOptions<T, TElement> options, CancellationToken cancellationToken = default)
		=> Requires.NotNull(self).DeserializePath(ref reader, ResolveTypeShapeOrThrow<T>(), options, cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.DeserializePath{T, TElement}(ReadOnlyMemory{byte}, ITypeShape{T}, in MessagePackSerializer.DeserializePathOptions{T, TElement}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="T"/> has no type shape created via the <see cref="GenerateShapeAttribute"/> source generator.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="T"/> is decorated with the <see cref="GenerateShapeAttribute"/>.
	/// For non-decorated types, apply <see cref="GenerateShapeForAttribute{T}"/> to a witness type and call <see cref="GetJsonSchema{T, TProvider}"/> instead,
	/// or use an overload that accepts a <see cref="ITypeShape{T}"/> for an option that does not require source generation.
	/// </remarks>
	[ExcludeFromCodeCoverage]
#if NET8_0
	[RequiresDynamicCode(ResolveDynamicMessage)]
#endif
#if NET
	[PreferDotNetAlternativeApi(MessagePackSerializer.PreferTypeConstrainedInstanceOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public static TElement? DeserializePath<T, TElement>(this MessagePackSerializer self, ReadOnlyMemory<byte> bytes, in MessagePackSerializer.DeserializePathOptions<T, TElement> options, CancellationToken cancellationToken = default)
		=> Requires.NotNull(self).DeserializePath(bytes, ResolveTypeShapeOrThrow<T>(), options, cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.DeserializePath{T, TElement}(in ReadOnlySequence{byte}, ITypeShape{T}, in MessagePackSerializer.DeserializePathOptions{T, TElement}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="T"/> has no type shape created via the <see cref="GenerateShapeAttribute"/> source generator.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="T"/> is decorated with the <see cref="GenerateShapeAttribute"/>.
	/// For non-decorated types, apply <see cref="GenerateShapeForAttribute{T}"/> to a witness type and call <see cref="GetJsonSchema{T, TProvider}"/> instead,
	/// or use an overload that accepts a <see cref="ITypeShape{T}"/> for an option that does not require source generation.
	/// </remarks>
	[ExcludeFromCodeCoverage]
#if NET8_0
	[RequiresDynamicCode(ResolveDynamicMessage)]
#endif
#if NET
	[PreferDotNetAlternativeApi(MessagePackSerializer.PreferTypeConstrainedInstanceOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public static TElement? DeserializePath<T, TElement>(this MessagePackSerializer self, scoped in ReadOnlySequence<byte> bytes, in MessagePackSerializer.DeserializePathOptions<T, TElement> options, CancellationToken cancellationToken = default)
		=> Requires.NotNull(self).DeserializePath(bytes, ResolveTypeShapeOrThrow<T>(), options, cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.DeserializePath{T, TElement}(Stream, ITypeShape{T}, in MessagePackSerializer.DeserializePathOptions{T, TElement}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="T"/> has no type shape created via the <see cref="GenerateShapeAttribute"/> source generator.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="T"/> is decorated with the <see cref="GenerateShapeAttribute"/>.
	/// For non-decorated types, apply <see cref="GenerateShapeForAttribute{T}"/> to a witness type and call <see cref="GetJsonSchema{T, TProvider}"/> instead,
	/// or use an overload that accepts a <see cref="ITypeShape{T}"/> for an option that does not require source generation.
	/// </remarks>
	[ExcludeFromCodeCoverage]
#if NET8_0
	[RequiresDynamicCode(ResolveDynamicMessage)]
#endif
#if NET
	[PreferDotNetAlternativeApi(MessagePackSerializer.PreferTypeConstrainedInstanceOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public static TElement? DeserializePath<T, TElement>(this MessagePackSerializer self, Stream stream, in MessagePackSerializer.DeserializePathOptions<T, TElement> options, CancellationToken cancellationToken = default)
		=> Requires.NotNull(self).DeserializePath(stream, ResolveTypeShapeOrThrow<T>(), options, cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.SerializeAsync{T}(PipeWriter, T, ITypeShape{T}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="T"/> has no type shape created via the <see cref="GenerateShapeAttribute"/> source generator.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="T"/> is decorated with the <see cref="GenerateShapeAttribute"/>.
	/// For non-decorated types, apply <see cref="GenerateShapeForAttribute{T}"/> to a witness type and call <see cref="GetJsonSchema{T, TProvider}"/> instead,
	/// or use an overload that accepts a <see cref="ITypeShape{T}"/> for an option that does not require source generation.
	/// </remarks>
#pragma warning disable RS0027 // optional parameter on a method with overloads
	[ExcludeFromCodeCoverage]
#if NET8_0
	[RequiresDynamicCode(ResolveDynamicMessage)]
#endif
#if NET
	[PreferDotNetAlternativeApi(MessagePackSerializer.PreferTypeConstrainedInstanceOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public static ValueTask SerializeAsync<T>(this MessagePackSerializer self, PipeWriter writer, in T? value, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		=> Requires.NotNull(self).SerializeAsync(writer, value, ResolveTypeShapeOrThrow<T>(), cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.SerializeAsync{T}(Stream, T, ITypeShape{T}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="T"/> has no type shape created via the <see cref="GenerateShapeAttribute"/> source generator.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="T"/> is decorated with the <see cref="GenerateShapeAttribute"/>.
	/// For non-decorated types, apply <see cref="GenerateShapeForAttribute{T}"/> to a witness type and call <see cref="GetJsonSchema{T, TProvider}"/> instead,
	/// or use an overload that accepts a <see cref="ITypeShape{T}"/> for an option that does not require source generation.
	/// </remarks>
#pragma warning disable RS0027 // optional parameter on a method with overloads
	[ExcludeFromCodeCoverage]
#if NET8_0
	[RequiresDynamicCode(ResolveDynamicMessage)]
#endif
#if NET
	[PreferDotNetAlternativeApi(MessagePackSerializer.PreferTypeConstrainedInstanceOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public static ValueTask SerializeAsync<T>(this MessagePackSerializer self, Stream stream, in T? value, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		=> Requires.NotNull(self).SerializeAsync(stream, value, ResolveTypeShapeOrThrow<T>(), cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.Serialize{T}(ref MessagePackWriter, in T, ITypeShape{T}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="TProvider"/> has no <see cref="GenerateShapeForAttribute{T}"/> source generator attribute for <typeparamref name="T"/>.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="TProvider"/> is decorated with a <see cref="GenerateShapeForAttribute{T}"/>.
	/// Use an overload that accepts a <see cref="ITypeShape{T}"/> for an option that does not require source generation.
	/// </remarks>
	[ExcludeFromCodeCoverage]
#if NET8_0
	[RequiresDynamicCode(ResolveDynamicMessage)]
#endif
#if NET
	[PreferDotNetAlternativeApi(MessagePackSerializer.PreferTypeConstrainedInstanceOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public static void Serialize<T, TProvider>(this MessagePackSerializer self, ref MessagePackWriter writer, in T? value, CancellationToken cancellationToken = default)
		=> Requires.NotNull(self).Serialize(ref writer, value, ResolveTypeShapeOrThrow<T, TProvider>(), cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.Serialize{T}(in T, ITypeShape{T}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="TProvider"/> has no <see cref="GenerateShapeForAttribute{T}"/> source generator attribute for <typeparamref name="T"/>.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="TProvider"/> is decorated with a <see cref="GenerateShapeForAttribute{T}"/>.
	/// Use an overload that accepts a <see cref="ITypeShape{T}"/> for an option that does not require source generation.
	/// </remarks>
	[ExcludeFromCodeCoverage]
#if NET8_0
	[RequiresDynamicCode(ResolveDynamicMessage)]
#endif
#if NET
	[PreferDotNetAlternativeApi(MessagePackSerializer.PreferTypeConstrainedInstanceOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public static byte[] Serialize<T, TProvider>(this MessagePackSerializer self, in T? value, CancellationToken cancellationToken = default)
		=> Requires.NotNull(self).Serialize(value, ResolveTypeShapeOrThrow<T, TProvider>(), cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.Serialize{T}(IBufferWriter{byte}, in T, ITypeShape{T}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="TProvider"/> has no <see cref="GenerateShapeForAttribute{T}"/> source generator attribute for <typeparamref name="T"/>.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="TProvider"/> is decorated with a <see cref="GenerateShapeForAttribute{T}"/>.
	/// Use an overload that accepts a <see cref="ITypeShape{T}"/> for an option that does not require source generation.
	/// </remarks>
	[ExcludeFromCodeCoverage]
#if NET8_0
	[RequiresDynamicCode(ResolveDynamicMessage)]
#endif
#if NET
	[PreferDotNetAlternativeApi(MessagePackSerializer.PreferTypeConstrainedInstanceOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public static void Serialize<T, TProvider>(this MessagePackSerializer self, IBufferWriter<byte> writer, in T? value, CancellationToken cancellationToken = default)
		=> Requires.NotNull(self).Serialize(writer, value, ResolveTypeShapeOrThrow<T, TProvider>(), cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.Serialize{T}(Stream, in T, ITypeShape{T}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="TProvider"/> has no <see cref="GenerateShapeForAttribute{T}"/> source generator attribute for <typeparamref name="T"/>.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="TProvider"/> is decorated with a <see cref="GenerateShapeForAttribute{T}"/>.
	/// Use an overload that accepts a <see cref="ITypeShape{T}"/> for an option that does not require source generation.
	/// </remarks>
	[ExcludeFromCodeCoverage]
#if NET8_0
	[RequiresDynamicCode(ResolveDynamicMessage)]
#endif
#if NET
	[PreferDotNetAlternativeApi(MessagePackSerializer.PreferTypeConstrainedInstanceOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public static void Serialize<T, TProvider>(this MessagePackSerializer self, Stream stream, in T? value, CancellationToken cancellationToken = default)
		=> Requires.NotNull(self).Serialize(stream, value, ResolveTypeShapeOrThrow<T, TProvider>(), cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.Deserialize{T}(ref MessagePackReader, ITypeShape{T}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="TProvider"/> has no <see cref="GenerateShapeForAttribute{T}"/> source generator attribute for <typeparamref name="T"/>.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="TProvider"/> is decorated with a <see cref="GenerateShapeForAttribute{T}"/>.
	/// Use an overload that accepts a <see cref="ITypeShape{T}"/> for an option that does not require source generation.
	/// </remarks>
	[ExcludeFromCodeCoverage]
#if NET8_0
	[RequiresDynamicCode(ResolveDynamicMessage)]
#endif
#if NET
	[PreferDotNetAlternativeApi(MessagePackSerializer.PreferTypeConstrainedInstanceOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public static T? Deserialize<T, TProvider>(this MessagePackSerializer self, ref MessagePackReader reader, CancellationToken cancellationToken = default)
		=> Requires.NotNull(self).Deserialize(ref reader, ResolveTypeShapeOrThrow<T, TProvider>(), cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.Deserialize{T}(ReadOnlyMemory{byte}, ITypeShape{T}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="TProvider"/> has no <see cref="GenerateShapeForAttribute{T}"/> source generator attribute for <typeparamref name="T"/>.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="TProvider"/> is decorated with a <see cref="GenerateShapeForAttribute{T}"/>.
	/// Use an overload that accepts a <see cref="ITypeShape{T}"/> for an option that does not require source generation.
	/// </remarks>
	[ExcludeFromCodeCoverage]
#if NET8_0
	[RequiresDynamicCode(ResolveDynamicMessage)]
#endif
#if NET
	[PreferDotNetAlternativeApi(MessagePackSerializer.PreferTypeConstrainedInstanceOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public static T? Deserialize<T, TProvider>(this MessagePackSerializer self, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default)
		=> Requires.NotNull(self).Deserialize(bytes, ResolveTypeShapeOrThrow<T, TProvider>(), cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.Deserialize{T}(in ReadOnlySequence{byte}, ITypeShape{T}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="TProvider"/> has no <see cref="GenerateShapeForAttribute{T}"/> source generator attribute for <typeparamref name="T"/>.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="TProvider"/> is decorated with a <see cref="GenerateShapeForAttribute{T}"/>.
	/// Use an overload that accepts a <see cref="ITypeShape{T}"/> for an option that does not require source generation.
	/// </remarks>
	[ExcludeFromCodeCoverage]
#if NET8_0
	[RequiresDynamicCode(ResolveDynamicMessage)]
#endif
#if NET
	[PreferDotNetAlternativeApi(MessagePackSerializer.PreferTypeConstrainedInstanceOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public static T? Deserialize<T, TProvider>(this MessagePackSerializer self, scoped in ReadOnlySequence<byte> bytes, CancellationToken cancellationToken = default)
		=> Requires.NotNull(self).Deserialize(bytes, ResolveTypeShapeOrThrow<T, TProvider>(), cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.Deserialize{T}(Stream, ITypeShape{T}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="TProvider"/> has no <see cref="GenerateShapeForAttribute{T}"/> source generator attribute for <typeparamref name="T"/>.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="TProvider"/> is decorated with a <see cref="GenerateShapeForAttribute{T}"/>.
	/// Use an overload that accepts a <see cref="ITypeShape{T}"/> for an option that does not require source generation.
	/// </remarks>
	[ExcludeFromCodeCoverage]
#if NET8_0
	[RequiresDynamicCode(ResolveDynamicMessage)]
#endif
#if NET
	[PreferDotNetAlternativeApi(MessagePackSerializer.PreferTypeConstrainedInstanceOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public static T? Deserialize<T, TProvider>(this MessagePackSerializer self, Stream stream, CancellationToken cancellationToken = default)
		=> Requires.NotNull(self).Deserialize(stream, ResolveTypeShapeOrThrow<T, TProvider>(), cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.DeserializeAsync{T}(PipeReader, ITypeShape{T}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="TProvider"/> has no <see cref="GenerateShapeForAttribute{T}"/> source generator attribute for <typeparamref name="T"/>.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="TProvider"/> is decorated with a <see cref="GenerateShapeForAttribute{T}"/>.
	/// Use an overload that accepts a <see cref="ITypeShape{T}"/> for an option that does not require source generation.
	/// </remarks>
#pragma warning disable RS0027 // optional parameter on a method with overloads
	[ExcludeFromCodeCoverage]
#if NET8_0
	[RequiresDynamicCode(ResolveDynamicMessage)]
#endif
#if NET
	[PreferDotNetAlternativeApi(MessagePackSerializer.PreferTypeConstrainedInstanceOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public static ValueTask<T?> DeserializeAsync<T, TProvider>(this MessagePackSerializer self, PipeReader reader, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		=> Requires.NotNull(self).DeserializeAsync(reader, ResolveTypeShapeOrThrow<T, TProvider>(), cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.DeserializeEnumerableAsync{T}(PipeReader, ITypeShape{T}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="TProvider"/> has no <see cref="GenerateShapeForAttribute{T}"/> source generator attribute for <typeparamref name="T"/>.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="TProvider"/> is decorated with a <see cref="GenerateShapeForAttribute{T}"/>.
	/// Use an overload that accepts a <see cref="ITypeShape{T}"/> for an option that does not require source generation.
	/// </remarks>
#pragma warning disable RS0027 // optional parameter on a method with overloads
	[ExcludeFromCodeCoverage]
#if NET8_0
	[RequiresDynamicCode(ResolveDynamicMessage)]
#endif
#if NET
	[PreferDotNetAlternativeApi(MessagePackSerializer.PreferTypeConstrainedInstanceOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public static IAsyncEnumerable<T?> DeserializeEnumerableAsync<T, TProvider>(this MessagePackSerializer self, PipeReader reader, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		=> Requires.NotNull(self).DeserializeEnumerableAsync(reader, ResolveTypeShapeOrThrow<T, TProvider>(), cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.DeserializePathEnumerableAsync{T, TElement}(PipeReader, ITypeShape{T}, MessagePackSerializer.StreamingEnumerationOptions{T, TElement}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="TProvider"/> has no <see cref="GenerateShapeForAttribute{T}"/> source generator attribute for <typeparamref name="T"/>.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="TProvider"/> is decorated with a <see cref="GenerateShapeForAttribute{T}"/>.
	/// Use an overload that accepts a <see cref="ITypeShape{T}"/> for an option that does not require source generation.
	/// </remarks>
#pragma warning disable RS0027 // optional parameter on a method with overloads
	[ExcludeFromCodeCoverage]
#if NET8_0
	[RequiresDynamicCode(ResolveDynamicMessage)]
#endif
#if NET
	[PreferDotNetAlternativeApi(MessagePackSerializer.PreferTypeConstrainedInstanceOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public static IAsyncEnumerable<TElement?> DeserializePathEnumerableAsync<T, TElement, TProvider>(this MessagePackSerializer self, PipeReader reader, MessagePackSerializer.StreamingEnumerationOptions<T, TElement> options, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		=> Requires.NotNull(self).DeserializePathEnumerableAsync(reader, ResolveTypeShapeOrThrow<T, TProvider>(), options, cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.DeserializeAsync{T}(Stream, ITypeShape{T}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="TProvider"/> has no <see cref="GenerateShapeForAttribute{T}"/> source generator attribute for <typeparamref name="T"/>.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="TProvider"/> is decorated with a <see cref="GenerateShapeForAttribute{T}"/>.
	/// Use an overload that accepts a <see cref="ITypeShape{T}"/> for an option that does not require source generation.
	/// </remarks>
#pragma warning disable RS0027 // optional parameter on a method with overloads
	[ExcludeFromCodeCoverage]
#if NET8_0
	[RequiresDynamicCode(ResolveDynamicMessage)]
#endif
#if NET
	[PreferDotNetAlternativeApi(MessagePackSerializer.PreferTypeConstrainedInstanceOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public static ValueTask<T?> DeserializeAsync<T, TProvider>(this MessagePackSerializer self, Stream stream, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		=> Requires.NotNull(self).DeserializeAsync(stream, ResolveTypeShapeOrThrow<T, TProvider>(), cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.DeserializeEnumerableAsync{T}(Stream, ITypeShape{T}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="TProvider"/> has no <see cref="GenerateShapeForAttribute{T}"/> source generator attribute for <typeparamref name="T"/>.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="TProvider"/> is decorated with a <see cref="GenerateShapeForAttribute{T}"/>.
	/// Use an overload that accepts a <see cref="ITypeShape{T}"/> for an option that does not require source generation.
	/// </remarks>
#pragma warning disable RS0027 // optional parameter on a method with overloads
	[ExcludeFromCodeCoverage]
#if NET8_0
	[RequiresDynamicCode(ResolveDynamicMessage)]
#endif
#if NET
	[PreferDotNetAlternativeApi(MessagePackSerializer.PreferTypeConstrainedInstanceOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public static IAsyncEnumerable<T?> DeserializeEnumerableAsync<T, TProvider>(this MessagePackSerializer self, Stream stream, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		=> Requires.NotNull(self).DeserializeEnumerableAsync(stream, ResolveTypeShapeOrThrow<T, TProvider>(), cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.DeserializePathEnumerableAsync{T, TElement}(Stream, ITypeShape{T}, MessagePackSerializer.StreamingEnumerationOptions{T, TElement}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="TProvider"/> has no <see cref="GenerateShapeForAttribute{T}"/> source generator attribute for <typeparamref name="T"/>.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="TProvider"/> is decorated with a <see cref="GenerateShapeForAttribute{T}"/>.
	/// Use an overload that accepts a <see cref="ITypeShape{T}"/> for an option that does not require source generation.
	/// </remarks>
#pragma warning disable RS0027 // optional parameter on a method with overloads
	[ExcludeFromCodeCoverage]
#if NET8_0
	[RequiresDynamicCode(ResolveDynamicMessage)]
#endif
#if NET
	[PreferDotNetAlternativeApi(MessagePackSerializer.PreferTypeConstrainedInstanceOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public static IAsyncEnumerable<TElement?> DeserializePathEnumerableAsync<T, TElement, TProvider>(this MessagePackSerializer self, Stream stream, MessagePackSerializer.StreamingEnumerationOptions<T, TElement> options, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		=> Requires.NotNull(self).DeserializePathEnumerableAsync(stream, ResolveTypeShapeOrThrow<T, TProvider>(), options, cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.DeserializePath{T, TElement}(ref MessagePackReader, ITypeShape{T}, in MessagePackSerializer.DeserializePathOptions{T, TElement}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="TProvider"/> has no <see cref="GenerateShapeForAttribute{T}"/> source generator attribute for <typeparamref name="T"/>.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="TProvider"/> is decorated with a <see cref="GenerateShapeForAttribute{T}"/>.
	/// Use an overload that accepts a <see cref="ITypeShape{T}"/> for an option that does not require source generation.
	/// </remarks>
	[ExcludeFromCodeCoverage]
#if NET8_0
	[RequiresDynamicCode(ResolveDynamicMessage)]
#endif
#if NET
	[PreferDotNetAlternativeApi(MessagePackSerializer.PreferTypeConstrainedInstanceOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public static TElement? DeserializePath<T, TElement, TProvider>(this MessagePackSerializer self, ref MessagePackReader reader, in MessagePackSerializer.DeserializePathOptions<T, TElement> options, CancellationToken cancellationToken = default)
		=> Requires.NotNull(self).DeserializePath(ref reader, ResolveTypeShapeOrThrow<T, TProvider>(), options, cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.DeserializePath{T, TElement}(ReadOnlyMemory{byte}, ITypeShape{T}, in MessagePackSerializer.DeserializePathOptions{T, TElement}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="TProvider"/> has no <see cref="GenerateShapeForAttribute{T}"/> source generator attribute for <typeparamref name="T"/>.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="TProvider"/> is decorated with a <see cref="GenerateShapeForAttribute{T}"/>.
	/// Use an overload that accepts a <see cref="ITypeShape{T}"/> for an option that does not require source generation.
	/// </remarks>
	[ExcludeFromCodeCoverage]
#if NET8_0
	[RequiresDynamicCode(ResolveDynamicMessage)]
#endif
#if NET
	[PreferDotNetAlternativeApi(MessagePackSerializer.PreferTypeConstrainedInstanceOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public static TElement? DeserializePath<T, TElement, TProvider>(this MessagePackSerializer self, ReadOnlyMemory<byte> bytes, in MessagePackSerializer.DeserializePathOptions<T, TElement> options, CancellationToken cancellationToken = default)
		=> Requires.NotNull(self).DeserializePath(bytes, ResolveTypeShapeOrThrow<T, TProvider>(), options, cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.DeserializePath{T, TElement}(in ReadOnlySequence{byte}, ITypeShape{T}, in MessagePackSerializer.DeserializePathOptions{T, TElement}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="TProvider"/> has no <see cref="GenerateShapeForAttribute{T}"/> source generator attribute for <typeparamref name="T"/>.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="TProvider"/> is decorated with a <see cref="GenerateShapeForAttribute{T}"/>.
	/// Use an overload that accepts a <see cref="ITypeShape{T}"/> for an option that does not require source generation.
	/// </remarks>
	[ExcludeFromCodeCoverage]
#if NET8_0
	[RequiresDynamicCode(ResolveDynamicMessage)]
#endif
#if NET
	[PreferDotNetAlternativeApi(MessagePackSerializer.PreferTypeConstrainedInstanceOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public static TElement? DeserializePath<T, TElement, TProvider>(this MessagePackSerializer self, scoped in ReadOnlySequence<byte> bytes, in MessagePackSerializer.DeserializePathOptions<T, TElement> options, CancellationToken cancellationToken = default)
		=> Requires.NotNull(self).DeserializePath(bytes, ResolveTypeShapeOrThrow<T, TProvider>(), options, cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.DeserializePath{T, TElement}(Stream, ITypeShape{T}, in MessagePackSerializer.DeserializePathOptions{T, TElement}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="TProvider"/> has no <see cref="GenerateShapeForAttribute{T}"/> source generator attribute for <typeparamref name="T"/>.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="TProvider"/> is decorated with a <see cref="GenerateShapeForAttribute{T}"/>.
	/// Use an overload that accepts a <see cref="ITypeShape{T}"/> for an option that does not require source generation.
	/// </remarks>
	[ExcludeFromCodeCoverage]
#if NET8_0
	[RequiresDynamicCode(ResolveDynamicMessage)]
#endif
#if NET
	[PreferDotNetAlternativeApi(MessagePackSerializer.PreferTypeConstrainedInstanceOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public static TElement? DeserializePath<T, TElement, TProvider>(this MessagePackSerializer self, Stream stream, in MessagePackSerializer.DeserializePathOptions<T, TElement> options, CancellationToken cancellationToken = default)
		=> Requires.NotNull(self).DeserializePath(stream, ResolveTypeShapeOrThrow<T, TProvider>(), options, cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.SerializeAsync{T}(PipeWriter, T, ITypeShape{T}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="TProvider"/> has no <see cref="GenerateShapeForAttribute{T}"/> source generator attribute for <typeparamref name="T"/>.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="TProvider"/> is decorated with a <see cref="GenerateShapeForAttribute{T}"/>.
	/// Use an overload that accepts a <see cref="ITypeShape{T}"/> for an option that does not require source generation.
	/// </remarks>
#pragma warning disable RS0027 // optional parameter on a method with overloads
	[ExcludeFromCodeCoverage]
#if NET8_0
	[RequiresDynamicCode(ResolveDynamicMessage)]
#endif
#if NET
	[PreferDotNetAlternativeApi(MessagePackSerializer.PreferTypeConstrainedInstanceOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public static ValueTask SerializeAsync<T, TProvider>(this MessagePackSerializer self, PipeWriter writer, in T? value, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		=> Requires.NotNull(self).SerializeAsync(writer, value, ResolveTypeShapeOrThrow<T, TProvider>(), cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.SerializeAsync{T}(Stream, T, ITypeShape{T}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="TProvider"/> has no <see cref="GenerateShapeForAttribute{T}"/> source generator attribute for <typeparamref name="T"/>.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="TProvider"/> is decorated with a <see cref="GenerateShapeForAttribute{T}"/>.
	/// Use an overload that accepts a <see cref="ITypeShape{T}"/> for an option that does not require source generation.
	/// </remarks>
#pragma warning disable RS0027 // optional parameter on a method with overloads
	[ExcludeFromCodeCoverage]
#if NET8_0
	[RequiresDynamicCode(ResolveDynamicMessage)]
#endif
#if NET
	[PreferDotNetAlternativeApi(MessagePackSerializer.PreferTypeConstrainedInstanceOverloads)]
	[EditorBrowsable(EditorBrowsableState.Never)]
#endif
	public static ValueTask SerializeAsync<T, TProvider>(this MessagePackSerializer self, Stream stream, in T? value, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		=> Requires.NotNull(self).SerializeAsync(stream, value, ResolveTypeShapeOrThrow<T, TProvider>(), cancellationToken);

	/// <summary>
	/// Resolves a type shape, providing enhanced error messages for array types.
	/// </summary>
	/// <typeparam name="T">The type to resolve a shape for.</typeparam>
	/// <returns>The type shape.</returns>
	/// <exception cref="NotSupportedException">
	/// Thrown if <typeparamref name="T"/> has no type shape created via the <see cref="GenerateShapeAttribute"/> source generator.
	/// For array types, provides specific guidance on using witness types.
	/// </exception>
	private static ITypeShape<T> ResolveTypeShapeOrThrow<T>()
	{
		try
		{
			return TypeShapeResolver.ResolveDynamicOrThrow<T>();
		}
		catch (NotSupportedException ex) when (typeof(T).IsArray)
		{
			Type elementType = typeof(T).GetElementType()!;
			throw new NotSupportedException(
				$"The type '{typeof(T).FullName}' does not have a generated shape. " +
				$"To deserialize an array as a top-level type, use a witness type with the [GenerateShapeFor<{typeof(T).Name}>] attribute. " +
				$"For example:\n\n" +
				$"[GenerateShapeFor<{typeof(T).Name}>]\n" +
				$"partial class Witness;\n\n" +
				$"var result = serializer.Deserialize<{typeof(T).Name}, Witness>(stream);\n\n" +
				$"See https://aarnott.github.io/Nerdbank.MessagePack/docs/type-shapes.html for more information.",
				ex);
		}
	}

	/// <summary>
	/// Resolves a type shape for a witness type, providing enhanced error messages for array types.
	/// </summary>
	/// <typeparam name="T">The type to resolve a shape for.</typeparam>
	/// <typeparam name="TProvider">The witness type that provides the shape for <typeparamref name="T"/>.</typeparam>
	/// <returns>The type shape.</returns>
	/// <exception cref="NotSupportedException">
	/// Thrown if <typeparamref name="TProvider"/> has no <see cref="GenerateShapeForAttribute{T}"/> source generator attribute for <typeparamref name="T"/>.
	/// For array types, provides specific guidance on using witness types.
	/// </exception>
	private static ITypeShape<T> ResolveTypeShapeOrThrow<T, TProvider>()
	{
		try
		{
			return TypeShapeResolver.ResolveDynamicOrThrow<T, TProvider>();
		}
		catch (NotSupportedException ex) when (typeof(T).IsArray)
		{
			Type elementType = typeof(T).GetElementType()!;
			throw new NotSupportedException(
				$"The type '{typeof(T).FullName}' does not have a generated shape on the witness type '{typeof(TProvider).FullName}'. " +
				$"To deserialize an array as a top-level type, ensure the witness type has a [GenerateShapeFor<{typeof(T).Name}>] attribute. " +
				$"For example:\n\n" +
				$"[GenerateShapeFor<{typeof(T).Name}>]\n" +
				$"partial class {typeof(TProvider).Name};\n\n" +
				$"See https://aarnott.github.io/Nerdbank.MessagePack/docs/type-shapes.html for more information.",
				ex);
		}
	}
}
