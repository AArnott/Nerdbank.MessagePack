// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
}
