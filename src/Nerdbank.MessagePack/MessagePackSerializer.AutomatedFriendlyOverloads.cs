// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1601 // Partial elements should be documented
#pragma warning disable RS0026 // optional parameter on a method with overloads
#pragma warning disable NBMsgPack051 // We deliberately forward the safe calls to the more general methods.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using Microsoft;

namespace Nerdbank.MessagePack;

#if NET

public partial record MessagePackSerializer
{
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

	/// <inheritdoc cref="DeserializeEnumerableAsync{T, TElement}(PipeReader, ITypeShape{T}, StreamingEnumerationOptions{T, TElement}, CancellationToken)" />
#pragma warning disable RS0027 // optional parameter on a method with overloads
	[ExcludeFromCodeCoverage]
	public IAsyncEnumerable<TElement?> DeserializeEnumerableAsync<T, TElement>(PipeReader reader, StreamingEnumerationOptions<T, TElement> options, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		where T : IShapeable<T> => this.DeserializeEnumerableAsync(reader, T.GetTypeShape(), options, cancellationToken);

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

	/// <inheritdoc cref="DeserializeEnumerableAsync{T, TElement}(Stream, ITypeShape{T}, StreamingEnumerationOptions{T, TElement}, CancellationToken)" />
#pragma warning disable RS0027 // optional parameter on a method with overloads
	[ExcludeFromCodeCoverage]
	public IAsyncEnumerable<TElement?> DeserializeEnumerableAsync<T, TElement>(Stream stream, StreamingEnumerationOptions<T, TElement> options, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		where T : IShapeable<T> => this.DeserializeEnumerableAsync(stream, T.GetTypeShape(), options, cancellationToken);

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

	/// <inheritdoc cref="DeserializeEnumerableAsync{T, TElement}(PipeReader, ITypeShape{T}, StreamingEnumerationOptions{T, TElement}, CancellationToken)" />
#pragma warning disable RS0027 // optional parameter on a method with overloads
	[ExcludeFromCodeCoverage]
	public IAsyncEnumerable<TElement?> DeserializeEnumerableAsync<T, TElement, TProvider>(PipeReader reader, StreamingEnumerationOptions<T, TElement> options, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		where TProvider : IShapeable<T> => this.DeserializeEnumerableAsync(reader, TProvider.GetTypeShape(), options, cancellationToken);

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

	/// <inheritdoc cref="DeserializeEnumerableAsync{T, TElement}(Stream, ITypeShape{T}, StreamingEnumerationOptions{T, TElement}, CancellationToken)" />
#pragma warning disable RS0027 // optional parameter on a method with overloads
	[ExcludeFromCodeCoverage]
	public IAsyncEnumerable<TElement?> DeserializeEnumerableAsync<T, TElement, TProvider>(Stream stream, StreamingEnumerationOptions<T, TElement> options, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		where TProvider : IShapeable<T> => this.DeserializeEnumerableAsync(stream, TProvider.GetTypeShape(), options, cancellationToken);

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
#if NET8_0
	// C.f. https://github.com/dotnet/runtime/issues/119440#issuecomment-3269894751
	private const string ResolveDynamicMessage =
		"Dynamic resolution of IShapeable<T> interface may require dynamic code generation in .NET 8 Native AOT. " +
		"It is recommended to switch to statically resolved IShapeable<T> APIs or upgrade your app to .NET 9 or later.";
#endif

	/// <inheritdoc cref="MessagePackSerializer.Serialize{T}(in T, ITypeShape{T}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="T"/> has no type shape created via the <see cref="GenerateShapeAttribute"/> source generator.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="T"/> is decorated with the <see cref="GenerateShapeAttribute"/>.
	/// For non-decorated types, apply <see cref="GenerateShapeForAttribute{T}"/> to a witness type and call <see cref="GetJsonSchema{T, TProvider}"/> instead,
	/// or use an overload that accepts a <see cref="ITypeShapeProvider"/> for an option that does not require source generation.
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
		=> Requires.NotNull(self).Serialize(value, TypeShapeResolver.ResolveDynamicOrThrow<T>(), cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.Serialize{T}(IBufferWriter{byte}, in T, ITypeShape{T}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="T"/> has no type shape created via the <see cref="GenerateShapeAttribute"/> source generator.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="T"/> is decorated with the <see cref="GenerateShapeAttribute"/>.
	/// For non-decorated types, apply <see cref="GenerateShapeForAttribute{T}"/> to a witness type and call <see cref="GetJsonSchema{T, TProvider}"/> instead,
	/// or use an overload that accepts a <see cref="ITypeShapeProvider"/> for an option that does not require source generation.
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
		=> Requires.NotNull(self).Serialize(writer, value, TypeShapeResolver.ResolveDynamicOrThrow<T>(), cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.Serialize{T}(Stream, in T, ITypeShape{T}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="T"/> has no type shape created via the <see cref="GenerateShapeAttribute"/> source generator.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="T"/> is decorated with the <see cref="GenerateShapeAttribute"/>.
	/// For non-decorated types, apply <see cref="GenerateShapeForAttribute{T}"/> to a witness type and call <see cref="GetJsonSchema{T, TProvider}"/> instead,
	/// or use an overload that accepts a <see cref="ITypeShapeProvider"/> for an option that does not require source generation.
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
		=> Requires.NotNull(self).Serialize(stream, value, TypeShapeResolver.ResolveDynamicOrThrow<T>(), cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.Deserialize{T}(ReadOnlyMemory{byte}, ITypeShape{T}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="T"/> has no type shape created via the <see cref="GenerateShapeAttribute"/> source generator.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="T"/> is decorated with the <see cref="GenerateShapeAttribute"/>.
	/// For non-decorated types, apply <see cref="GenerateShapeForAttribute{T}"/> to a witness type and call <see cref="GetJsonSchema{T, TProvider}"/> instead,
	/// or use an overload that accepts a <see cref="ITypeShapeProvider"/> for an option that does not require source generation.
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
		=> Requires.NotNull(self).Deserialize(bytes, TypeShapeResolver.ResolveDynamicOrThrow<T>(), cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.Deserialize{T}(in ReadOnlySequence{byte}, ITypeShape{T}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="T"/> has no type shape created via the <see cref="GenerateShapeAttribute"/> source generator.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="T"/> is decorated with the <see cref="GenerateShapeAttribute"/>.
	/// For non-decorated types, apply <see cref="GenerateShapeForAttribute{T}"/> to a witness type and call <see cref="GetJsonSchema{T, TProvider}"/> instead,
	/// or use an overload that accepts a <see cref="ITypeShapeProvider"/> for an option that does not require source generation.
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
		=> Requires.NotNull(self).Deserialize(bytes, TypeShapeResolver.ResolveDynamicOrThrow<T>(), cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.Deserialize{T}(Stream, ITypeShape{T}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="T"/> has no type shape created via the <see cref="GenerateShapeAttribute"/> source generator.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="T"/> is decorated with the <see cref="GenerateShapeAttribute"/>.
	/// For non-decorated types, apply <see cref="GenerateShapeForAttribute{T}"/> to a witness type and call <see cref="GetJsonSchema{T, TProvider}"/> instead,
	/// or use an overload that accepts a <see cref="ITypeShapeProvider"/> for an option that does not require source generation.
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
		=> Requires.NotNull(self).Deserialize(stream, TypeShapeResolver.ResolveDynamicOrThrow<T>(), cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.DeserializeAsync{T}(PipeReader, ITypeShape{T}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="T"/> has no type shape created via the <see cref="GenerateShapeAttribute"/> source generator.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="T"/> is decorated with the <see cref="GenerateShapeAttribute"/>.
	/// For non-decorated types, apply <see cref="GenerateShapeForAttribute{T}"/> to a witness type and call <see cref="GetJsonSchema{T, TProvider}"/> instead,
	/// or use an overload that accepts a <see cref="ITypeShapeProvider"/> for an option that does not require source generation.
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
		=> Requires.NotNull(self).DeserializeAsync(reader, TypeShapeResolver.ResolveDynamicOrThrow<T>(), cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.DeserializeEnumerableAsync{T}(PipeReader, ITypeShape{T}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="T"/> has no type shape created via the <see cref="GenerateShapeAttribute"/> source generator.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="T"/> is decorated with the <see cref="GenerateShapeAttribute"/>.
	/// For non-decorated types, apply <see cref="GenerateShapeForAttribute{T}"/> to a witness type and call <see cref="GetJsonSchema{T, TProvider}"/> instead,
	/// or use an overload that accepts a <see cref="ITypeShapeProvider"/> for an option that does not require source generation.
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
		=> Requires.NotNull(self).DeserializeEnumerableAsync(reader, TypeShapeResolver.ResolveDynamicOrThrow<T>(), cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.DeserializeEnumerableAsync{T, TElement}(PipeReader, ITypeShape{T}, MessagePackSerializer.StreamingEnumerationOptions{T, TElement}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="T"/> has no type shape created via the <see cref="GenerateShapeAttribute"/> source generator.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="T"/> is decorated with the <see cref="GenerateShapeAttribute"/>.
	/// For non-decorated types, apply <see cref="GenerateShapeForAttribute{T}"/> to a witness type and call <see cref="GetJsonSchema{T, TProvider}"/> instead,
	/// or use an overload that accepts a <see cref="ITypeShapeProvider"/> for an option that does not require source generation.
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
	public static IAsyncEnumerable<TElement?> DeserializeEnumerableAsync<T, TElement>(this MessagePackSerializer self, PipeReader reader, MessagePackSerializer.StreamingEnumerationOptions<T, TElement> options, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		=> Requires.NotNull(self).DeserializeEnumerableAsync(reader, TypeShapeResolver.ResolveDynamicOrThrow<T>(), options, cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.DeserializeAsync{T}(Stream, ITypeShape{T}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="T"/> has no type shape created via the <see cref="GenerateShapeAttribute"/> source generator.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="T"/> is decorated with the <see cref="GenerateShapeAttribute"/>.
	/// For non-decorated types, apply <see cref="GenerateShapeForAttribute{T}"/> to a witness type and call <see cref="GetJsonSchema{T, TProvider}"/> instead,
	/// or use an overload that accepts a <see cref="ITypeShapeProvider"/> for an option that does not require source generation.
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
		=> Requires.NotNull(self).DeserializeAsync(stream, TypeShapeResolver.ResolveDynamicOrThrow<T>(), cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.DeserializeEnumerableAsync{T}(Stream, ITypeShape{T}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="T"/> has no type shape created via the <see cref="GenerateShapeAttribute"/> source generator.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="T"/> is decorated with the <see cref="GenerateShapeAttribute"/>.
	/// For non-decorated types, apply <see cref="GenerateShapeForAttribute{T}"/> to a witness type and call <see cref="GetJsonSchema{T, TProvider}"/> instead,
	/// or use an overload that accepts a <see cref="ITypeShapeProvider"/> for an option that does not require source generation.
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
		=> Requires.NotNull(self).DeserializeEnumerableAsync(stream, TypeShapeResolver.ResolveDynamicOrThrow<T>(), cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.DeserializeEnumerableAsync{T, TElement}(Stream, ITypeShape{T}, MessagePackSerializer.StreamingEnumerationOptions{T, TElement}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="T"/> has no type shape created via the <see cref="GenerateShapeAttribute"/> source generator.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="T"/> is decorated with the <see cref="GenerateShapeAttribute"/>.
	/// For non-decorated types, apply <see cref="GenerateShapeForAttribute{T}"/> to a witness type and call <see cref="GetJsonSchema{T, TProvider}"/> instead,
	/// or use an overload that accepts a <see cref="ITypeShapeProvider"/> for an option that does not require source generation.
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
	public static IAsyncEnumerable<TElement?> DeserializeEnumerableAsync<T, TElement>(this MessagePackSerializer self, Stream stream, MessagePackSerializer.StreamingEnumerationOptions<T, TElement> options, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		=> Requires.NotNull(self).DeserializeEnumerableAsync(stream, TypeShapeResolver.ResolveDynamicOrThrow<T>(), options, cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.SerializeAsync{T}(PipeWriter, T, ITypeShape{T}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="T"/> has no type shape created via the <see cref="GenerateShapeAttribute"/> source generator.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="T"/> is decorated with the <see cref="GenerateShapeAttribute"/>.
	/// For non-decorated types, apply <see cref="GenerateShapeForAttribute{T}"/> to a witness type and call <see cref="GetJsonSchema{T, TProvider}"/> instead,
	/// or use an overload that accepts a <see cref="ITypeShapeProvider"/> for an option that does not require source generation.
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
		=> Requires.NotNull(self).SerializeAsync(writer, value, TypeShapeResolver.ResolveDynamicOrThrow<T>(), cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.SerializeAsync{T}(Stream, T, ITypeShape{T}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="T"/> has no type shape created via the <see cref="GenerateShapeAttribute"/> source generator.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="T"/> is decorated with the <see cref="GenerateShapeAttribute"/>.
	/// For non-decorated types, apply <see cref="GenerateShapeForAttribute{T}"/> to a witness type and call <see cref="GetJsonSchema{T, TProvider}"/> instead,
	/// or use an overload that accepts a <see cref="ITypeShapeProvider"/> for an option that does not require source generation.
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
		=> Requires.NotNull(self).SerializeAsync(stream, value, TypeShapeResolver.ResolveDynamicOrThrow<T>(), cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.Serialize{T}(in T, ITypeShape{T}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="TProvider"/> has no <see cref="GenerateShapeForAttribute{T}"/> source generator attribute for <typeparamref name="T"/>.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="TProvider"/> is decorated with a <see cref="GenerateShapeForAttribute{T}"/>.
	/// Use an overload that accepts a <see cref="ITypeShapeProvider"/> for an option that does not require source generation.
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
		=> Requires.NotNull(self).Serialize(value, TypeShapeResolver.ResolveDynamicOrThrow<T, TProvider>(), cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.Serialize{T}(IBufferWriter{byte}, in T, ITypeShape{T}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="TProvider"/> has no <see cref="GenerateShapeForAttribute{T}"/> source generator attribute for <typeparamref name="T"/>.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="TProvider"/> is decorated with a <see cref="GenerateShapeForAttribute{T}"/>.
	/// Use an overload that accepts a <see cref="ITypeShapeProvider"/> for an option that does not require source generation.
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
		=> Requires.NotNull(self).Serialize(writer, value, TypeShapeResolver.ResolveDynamicOrThrow<T, TProvider>(), cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.Serialize{T}(Stream, in T, ITypeShape{T}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="TProvider"/> has no <see cref="GenerateShapeForAttribute{T}"/> source generator attribute for <typeparamref name="T"/>.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="TProvider"/> is decorated with a <see cref="GenerateShapeForAttribute{T}"/>.
	/// Use an overload that accepts a <see cref="ITypeShapeProvider"/> for an option that does not require source generation.
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
		=> Requires.NotNull(self).Serialize(stream, value, TypeShapeResolver.ResolveDynamicOrThrow<T, TProvider>(), cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.Deserialize{T}(ReadOnlyMemory{byte}, ITypeShape{T}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="TProvider"/> has no <see cref="GenerateShapeForAttribute{T}"/> source generator attribute for <typeparamref name="T"/>.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="TProvider"/> is decorated with a <see cref="GenerateShapeForAttribute{T}"/>.
	/// Use an overload that accepts a <see cref="ITypeShapeProvider"/> for an option that does not require source generation.
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
		=> Requires.NotNull(self).Deserialize(bytes, TypeShapeResolver.ResolveDynamicOrThrow<T, TProvider>(), cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.Deserialize{T}(in ReadOnlySequence{byte}, ITypeShape{T}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="TProvider"/> has no <see cref="GenerateShapeForAttribute{T}"/> source generator attribute for <typeparamref name="T"/>.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="TProvider"/> is decorated with a <see cref="GenerateShapeForAttribute{T}"/>.
	/// Use an overload that accepts a <see cref="ITypeShapeProvider"/> for an option that does not require source generation.
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
		=> Requires.NotNull(self).Deserialize(bytes, TypeShapeResolver.ResolveDynamicOrThrow<T, TProvider>(), cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.Deserialize{T}(Stream, ITypeShape{T}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="TProvider"/> has no <see cref="GenerateShapeForAttribute{T}"/> source generator attribute for <typeparamref name="T"/>.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="TProvider"/> is decorated with a <see cref="GenerateShapeForAttribute{T}"/>.
	/// Use an overload that accepts a <see cref="ITypeShapeProvider"/> for an option that does not require source generation.
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
		=> Requires.NotNull(self).Deserialize(stream, TypeShapeResolver.ResolveDynamicOrThrow<T, TProvider>(), cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.DeserializeAsync{T}(PipeReader, ITypeShape{T}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="TProvider"/> has no <see cref="GenerateShapeForAttribute{T}"/> source generator attribute for <typeparamref name="T"/>.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="TProvider"/> is decorated with a <see cref="GenerateShapeForAttribute{T}"/>.
	/// Use an overload that accepts a <see cref="ITypeShapeProvider"/> for an option that does not require source generation.
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
		=> Requires.NotNull(self).DeserializeAsync(reader, TypeShapeResolver.ResolveDynamicOrThrow<T, TProvider>(), cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.DeserializeEnumerableAsync{T}(PipeReader, ITypeShape{T}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="TProvider"/> has no <see cref="GenerateShapeForAttribute{T}"/> source generator attribute for <typeparamref name="T"/>.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="TProvider"/> is decorated with a <see cref="GenerateShapeForAttribute{T}"/>.
	/// Use an overload that accepts a <see cref="ITypeShapeProvider"/> for an option that does not require source generation.
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
		=> Requires.NotNull(self).DeserializeEnumerableAsync(reader, TypeShapeResolver.ResolveDynamicOrThrow<T, TProvider>(), cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.DeserializeEnumerableAsync{T, TElement}(PipeReader, ITypeShape{T}, MessagePackSerializer.StreamingEnumerationOptions{T, TElement}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="TProvider"/> has no <see cref="GenerateShapeForAttribute{T}"/> source generator attribute for <typeparamref name="T"/>.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="TProvider"/> is decorated with a <see cref="GenerateShapeForAttribute{T}"/>.
	/// Use an overload that accepts a <see cref="ITypeShapeProvider"/> for an option that does not require source generation.
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
	public static IAsyncEnumerable<TElement?> DeserializeEnumerableAsync<T, TElement, TProvider>(this MessagePackSerializer self, PipeReader reader, MessagePackSerializer.StreamingEnumerationOptions<T, TElement> options, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		=> Requires.NotNull(self).DeserializeEnumerableAsync(reader, TypeShapeResolver.ResolveDynamicOrThrow<T, TProvider>(), options, cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.DeserializeAsync{T}(Stream, ITypeShape{T}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="TProvider"/> has no <see cref="GenerateShapeForAttribute{T}"/> source generator attribute for <typeparamref name="T"/>.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="TProvider"/> is decorated with a <see cref="GenerateShapeForAttribute{T}"/>.
	/// Use an overload that accepts a <see cref="ITypeShapeProvider"/> for an option that does not require source generation.
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
		=> Requires.NotNull(self).DeserializeAsync(stream, TypeShapeResolver.ResolveDynamicOrThrow<T, TProvider>(), cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.DeserializeEnumerableAsync{T}(Stream, ITypeShape{T}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="TProvider"/> has no <see cref="GenerateShapeForAttribute{T}"/> source generator attribute for <typeparamref name="T"/>.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="TProvider"/> is decorated with a <see cref="GenerateShapeForAttribute{T}"/>.
	/// Use an overload that accepts a <see cref="ITypeShapeProvider"/> for an option that does not require source generation.
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
		=> Requires.NotNull(self).DeserializeEnumerableAsync(stream, TypeShapeResolver.ResolveDynamicOrThrow<T, TProvider>(), cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.DeserializeEnumerableAsync{T, TElement}(Stream, ITypeShape{T}, MessagePackSerializer.StreamingEnumerationOptions{T, TElement}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="TProvider"/> has no <see cref="GenerateShapeForAttribute{T}"/> source generator attribute for <typeparamref name="T"/>.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="TProvider"/> is decorated with a <see cref="GenerateShapeForAttribute{T}"/>.
	/// Use an overload that accepts a <see cref="ITypeShapeProvider"/> for an option that does not require source generation.
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
	public static IAsyncEnumerable<TElement?> DeserializeEnumerableAsync<T, TElement, TProvider>(this MessagePackSerializer self, Stream stream, MessagePackSerializer.StreamingEnumerationOptions<T, TElement> options, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
		=> Requires.NotNull(self).DeserializeEnumerableAsync(stream, TypeShapeResolver.ResolveDynamicOrThrow<T, TProvider>(), options, cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.SerializeAsync{T}(PipeWriter, T, ITypeShape{T}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="TProvider"/> has no <see cref="GenerateShapeForAttribute{T}"/> source generator attribute for <typeparamref name="T"/>.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="TProvider"/> is decorated with a <see cref="GenerateShapeForAttribute{T}"/>.
	/// Use an overload that accepts a <see cref="ITypeShapeProvider"/> for an option that does not require source generation.
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
		=> Requires.NotNull(self).SerializeAsync(writer, value, TypeShapeResolver.ResolveDynamicOrThrow<T, TProvider>(), cancellationToken);

	/// <inheritdoc cref="MessagePackSerializer.SerializeAsync{T}(Stream, T, ITypeShape{T}, CancellationToken)" />
	/// <exception cref="NotSupportedException">Thrown if <typeparamref name="TProvider"/> has no <see cref="GenerateShapeForAttribute{T}"/> source generator attribute for <typeparamref name="T"/>.</exception>
	/// <remarks>
	/// This overload should only be used when <typeparamref name="TProvider"/> is decorated with a <see cref="GenerateShapeForAttribute{T}"/>.
	/// Use an overload that accepts a <see cref="ITypeShapeProvider"/> for an option that does not require source generation.
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
		=> Requires.NotNull(self).SerializeAsync(stream, value, TypeShapeResolver.ResolveDynamicOrThrow<T, TProvider>(), cancellationToken);
}
