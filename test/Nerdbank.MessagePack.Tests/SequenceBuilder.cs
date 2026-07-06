// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/// <summary>
/// Helper class for constructing artificial <see cref="ReadOnlySequence{T}"/> instances for testing purposes.
/// </summary>
internal static class SequenceBuilder
{
	/// <summary>
	/// Creates a <see cref="ReadOnlySequence{T}"/> from the provided segments.
	/// </summary>
	/// <typeparam name="T">The type of element in the sequence.</typeparam>
	/// <param name="segmentContents">The segments for the sequence.</param>
	/// <returns>A <see cref="ReadOnlySequence{T}"/> representing the concatenated segments.</returns>
	/// <remarks>
	/// This does not use <see cref="Nerdbank.Streams.Sequence{T}"/> to construct sequences because that
	/// type will not append <em>empty</em> segments, which for some tests is exactly what we need.
	/// </remarks>
	internal static ReadOnlySequence<T> Create<T>(params ReadOnlyMemory<T>[] segmentContents)
	{
		if (segmentContents.Length == 1)
		{
			return new ReadOnlySequence<T>(segmentContents[0]);
		}

		BufferSegment<T> bufferSegment = new(segmentContents[0]);
		BufferSegment<T>? last = bufferSegment;
		for (int i = 1; i < segmentContents.Length; i++)
		{
			last = last.Append(segmentContents[i]);
		}

		return new ReadOnlySequence<T>(bufferSegment, 0, last!, last!.Memory.Length);
	}

	/// <inheritdoc cref="Create{T}(ReadOnlyMemory{T}[])"/>
	internal static ReadOnlySequence<T> Create<T>(params T[][] segmentContents)
	{
		ReadOnlyMemory<T>[] memorySegments = new ReadOnlyMemory<T>[segmentContents.Length];
		for (int i = 0; i < segmentContents.Length; i++)
		{
			memorySegments[i] = segmentContents[i].AsMemory();
		}

		return Create(memorySegments);
	}

	private sealed class BufferSegment<T> : ReadOnlySequenceSegment<T>
	{
		internal BufferSegment(ReadOnlyMemory<T> memory)
		{
			this.Memory = memory;
		}

		internal BufferSegment<T> Append(ReadOnlyMemory<T> memory)
		{
			var segment = new BufferSegment<T>(memory)
			{
				RunningIndex = this.RunningIndex + this.Memory.Length,
			};
			this.Next = segment;
			return segment;
		}
	}
}
