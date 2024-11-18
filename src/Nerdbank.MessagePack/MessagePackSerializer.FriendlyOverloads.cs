// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft;

namespace Nerdbank.MessagePack;

public partial record MessagePackSerializer
{
	/// <summary>
	/// A thread-local, recyclable array that may be used for short bursts of code.
	/// </summary>
	[ThreadStatic]
	private static byte[]? scratchArray;

	/// <inheritdoc cref="Serialize{T}(ref MessagePackWriter, in T, ITypeShape{T})" />
	/// <returns>A byte array containing the serialized msgpack.</returns>
	public byte[] Serialize<T>(in T? value, ITypeShape<T> shape)
	{
		Requires.NotNull(shape);

		// Although the static array is thread-local, we still want to null it out while using it
		// to avoid any potential issues with re-entrancy due to a converter that makes a (bad) top-level call to the serializer.
		(byte[] array, scratchArray) = (scratchArray ?? new byte[65536], null);
		try
		{
			MessagePackWriter writer = new(SequencePool.Shared, array);
			this.Serialize(ref writer, value, shape);
			return writer.FlushAndGetArray();
		}
		finally
		{
			scratchArray = array;
		}
	}

	/// <inheritdoc cref="Serialize{T}(ref MessagePackWriter, in T, ITypeShape{T})"/>
	public void Serialize<T>(IBufferWriter<byte> writer, in T? value, ITypeShape<T> shape)
	{
		MessagePackWriter msgpackWriter = new(writer);
		this.Serialize(ref msgpackWriter, value, shape);
		msgpackWriter.Flush();
	}

	/// <inheritdoc cref="Deserialize{T}(in ReadOnlySequence{byte}, ITypeShape{T})"/>
	public T? Deserialize<T>(ReadOnlyMemory<byte> buffer, ITypeShape<T> shape)
	{
		MessagePackReader reader = new(buffer);
		return this.Deserialize(ref reader, shape);
	}

	/// <inheritdoc cref="Deserialize{T}(ref MessagePackReader, ITypeShape{T})"/>
	/// <param name="buffer">The msgpack to deserialize from.</param>
#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
	public T? Deserialize<T>(scoped in ReadOnlySequence<byte> buffer, ITypeShape<T> shape)
#pragma warning restore CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
	{
		MessagePackReader reader = new(buffer);
		return this.Deserialize(ref reader, shape);
	}
}
