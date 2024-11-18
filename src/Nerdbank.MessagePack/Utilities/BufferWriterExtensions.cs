// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// This file was originally derived from https://github.com/MessagePack-CSharp/MessagePack-CSharp/
using Microsoft;

namespace Nerdbank.MessagePack.Utilities;

/// <summary>
/// Extensions for the <see cref="IBufferWriter{T}"/> type.
/// </summary>
internal static class BufferWriterExtensions
{
	/// <inheritdoc cref="IBufferWriter{T}.GetMemory(int)"/>
	/// <remarks>
	/// This method adds a runtime check that the result is not an empty memory block.
	/// </remarks>
	/// <exception cref="InvalidOperationException">Thrown if the <see cref="IBufferWriter{T}"/> implementation is faulty and returned an empty block.</exception>
	internal static Memory<byte> GetMemoryCheckResult(this IBufferWriter<byte> bufferWriter, int size = 0)
	{
		Memory<byte> memory = bufferWriter.GetMemory(size);
		if (memory.IsEmpty)
		{
			throw new InvalidOperationException("The underlying IBufferWriter<byte>.GetMemory(int) method returned an empty memory block, which is not allowed. This is a bug in " + bufferWriter.GetType().FullName);
		}

		return memory;
	}

	/// <summary>
	/// Copies the content of a <see cref="ReadOnlySequence{T}"/> into an <see cref="IBufferWriter{T}"/>.
	/// </summary>
	/// <typeparam name="T">The type of element to copy.</typeparam>
	/// <param name="writer">The <see cref="IBufferWriter{T}"/> to write to.</param>
	/// <param name="sequence">The sequence to read from.</param>
	internal static void Write<T>(this IBufferWriter<T> writer, ReadOnlySequence<T> sequence)
	{
		Requires.NotNull(writer, nameof(writer));

		foreach (ReadOnlyMemory<T> memory in sequence)
		{
			writer.Write(memory.Span);
		}
	}
}
