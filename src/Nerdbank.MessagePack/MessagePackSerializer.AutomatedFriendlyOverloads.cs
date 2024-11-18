// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Pipelines;

namespace Nerdbank.MessagePack;

public partial record MessagePackSerializer
{
	/// <inheritdoc cref="Serialize{T}(in T, ITypeShape{T})" />
	public byte[] Serialize<T>(in T? value)
		where T : IShapeable<T> => this.Serialize(value, T.GetShape());

	/// <inheritdoc cref="Serialize{T}(IBufferWriter{byte}, in T, ITypeShape{T})" />
	public void Serialize<T>(IBufferWriter<byte> writer, in T? value)
		where T : IShapeable<T> => this.Serialize(writer, value, T.GetShape());

	/// <inheritdoc cref="Deserialize{T}(ReadOnlyMemory{byte}, ITypeShape{T})" />
	public T? Deserialize<T>(ReadOnlyMemory<byte> bytes)
		where T : IShapeable<T> => this.Deserialize(bytes, T.GetShape());

	/// <inheritdoc cref="Deserialize{T}(in ReadOnlySequence{byte}, ITypeShape{T})" />
	public T? Deserialize<T>(scoped in ReadOnlySequence<byte> bytes)
		where T : IShapeable<T> => this.Deserialize(bytes, T.GetShape());

	/// <inheritdoc cref="DeserializeAsync{T}(PipeReader, ITypeShape{T}, CancellationToken)" />
#pragma warning disable RS0026 // optional parameter on a method with overloads
#pragma warning disable RS0027 // optional parameter on a method with overloads
	public ValueTask<T?> DeserializeAsync<T>(PipeReader reader, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
#pragma warning restore RS0026 // optional parameter on a method with overloads
		where T : IShapeable<T> => this.DeserializeAsync(reader, T.GetShape(), cancellationToken);

	/// <inheritdoc cref="SerializeAsync{T}(PipeWriter, T, ITypeShape{T}, CancellationToken)" />
#pragma warning disable RS0027 // optional parameter on a method with overloads
#pragma warning disable RS0026 // optional parameter on a method with overloads
	public ValueTask SerializeAsync<T>(PipeWriter writer, in T? value, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
#pragma warning restore RS0026 // optional parameter on a method with overloads
		where T : IShapeable<T> => this.SerializeAsync(writer, value, T.GetShape(), cancellationToken);

	/// <inheritdoc cref="Serialize{T}(in T, ITypeShape{T})" />
	public byte[] Serialize<T, TProvider>(in T? value)
		where TProvider : IShapeable<T> => this.Serialize(value, TProvider.GetShape());

	/// <inheritdoc cref="Serialize{T}(IBufferWriter{byte}, in T, ITypeShape{T})" />
	public void Serialize<T, TProvider>(IBufferWriter<byte> writer, in T? value)
		where TProvider : IShapeable<T> => this.Serialize(writer, value, TProvider.GetShape());

	/// <inheritdoc cref="Deserialize{T}(ReadOnlyMemory{byte}, ITypeShape{T})" />
	public T? Deserialize<T, TProvider>(ReadOnlyMemory<byte> bytes)
		where TProvider : IShapeable<T> => this.Deserialize(bytes, TProvider.GetShape());

	/// <inheritdoc cref="Deserialize{T}(in ReadOnlySequence{byte}, ITypeShape{T})" />
	public T? Deserialize<T, TProvider>(scoped in ReadOnlySequence<byte> bytes)
		where TProvider : IShapeable<T> => this.Deserialize(bytes, TProvider.GetShape());

	/// <inheritdoc cref="DeserializeAsync{T}(PipeReader, ITypeShape{T}, CancellationToken)" />
#pragma warning disable RS0026 // optional parameter on a method with overloads
#pragma warning disable RS0027 // optional parameter on a method with overloads
	public ValueTask<T?> DeserializeAsync<T, TProvider>(PipeReader reader, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
#pragma warning restore RS0026 // optional parameter on a method with overloads
		where TProvider : IShapeable<T> => this.DeserializeAsync(reader, TProvider.GetShape(), cancellationToken);

	/// <inheritdoc cref="SerializeAsync{T}(PipeWriter, T, ITypeShape{T}, CancellationToken)" />
#pragma warning disable RS0027 // optional parameter on a method with overloads
#pragma warning disable RS0026 // optional parameter on a method with overloads
	public ValueTask SerializeAsync<T, TProvider>(PipeWriter writer, in T? value, CancellationToken cancellationToken = default)
#pragma warning restore RS0027 // optional parameter on a method with overloads
#pragma warning restore RS0026 // optional parameter on a method with overloads
		where TProvider : IShapeable<T> => this.SerializeAsync(writer, value, TProvider.GetShape(), cancellationToken);
}
