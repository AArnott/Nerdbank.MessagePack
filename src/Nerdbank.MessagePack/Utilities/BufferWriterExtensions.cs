// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack.Utilities;

internal static class BufferWriterExtensions
{
	internal static Memory<byte> GetMemoryCheckResult(this IBufferWriter<byte> bufferWriter, int size = 0)
	{
		var memory = bufferWriter.GetMemory(size);
		if (memory.IsEmpty)
		{
			throw new InvalidOperationException("The underlying IBufferWriter<byte>.GetMemory(int) method returned an empty memory block, which is not allowed. This is a bug in " + bufferWriter.GetType().FullName);
		}

		return memory;
	}
}
