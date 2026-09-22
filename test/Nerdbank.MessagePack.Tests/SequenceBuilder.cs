// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft;

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

	internal static ReadOnlySequence<T> InsertFragmentBreak<T>(ReadOnlySequence<T> sequence, ulong fragmentPosition)
	{
		Requires.Range(fragmentPosition <= (ulong)sequence.Length, nameof(fragmentPosition), "Must not be greater than the length of the sequence.");
		ulong priorBytes = 0;

		BufferSegment<T>? first = null, last = null;

		if (fragmentPosition == 0)
		{
			Append(default);
		}

		foreach (ReadOnlyMemory<T> segment in sequence)
		{
			// If the fragment position is within this segment, split it into two segments.
			if (fragmentPosition > priorBytes && fragmentPosition < priorBytes + (ulong)segment.Length)
			{
				int fragmentWithinSegment = (int)(fragmentPosition - priorBytes);
				Append(segment[..fragmentWithinSegment]);
				Append(segment[fragmentWithinSegment..]);
			}
			else
			{
				Append(segment);
			}

			priorBytes += (ulong)segment.Length;
		}

		if (fragmentPosition == (ulong)sequence.Length)
		{
			Append(default);
		}

		Assumes.NotNull(first);
		Assumes.NotNull(last);

		return new ReadOnlySequence<T>(first, 0, last, last.Memory.Length);

		void Append(ReadOnlyMemory<T> buffer) => last = last is null ? (first = new(buffer)) : last.Append(buffer);
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
