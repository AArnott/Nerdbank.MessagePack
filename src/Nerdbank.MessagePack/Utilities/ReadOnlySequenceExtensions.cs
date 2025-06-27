// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// This is a copy of the Sequence<T> class from the Nerdbank.Streams library.
using Microsoft;

namespace Nerdbank.MessagePack.Utilities;

/// <summary>
/// Extension methods for the <see cref="ReadOnlySequence{T}"/> type.
/// </summary>
internal static partial class ReadOnlySequenceExtensions
{
	/// <summary>
	/// Copies the content of one <see cref="ReadOnlySequence{T}"/> to another that is backed by its own
	/// memory buffers.
	/// </summary>
	/// <typeparam name="T">The type of element in the sequence.</typeparam>
	/// <param name="template">The sequence to copy from.</param>
	/// <returns>A shallow copy of the sequence, backed by buffers which will never be recycled.</returns>
	/// <remarks>
	/// This method is useful for retaining data that is backed by buffers that will be reused later.
	/// </remarks>
	internal static ReadOnlySequence<T> Clone<T>(this ReadOnlySequence<T> template)
	{
		Sequence<T> sequence = new();
		sequence.Write(template);
		return sequence;
	}

	/// <summary>
	/// Compares two <see cref="ReadOnlySequence{T}"/> instances for structural equality.
	/// </summary>
	/// <typeparam name="T">The type of element.</typeparam>
	/// <param name="a">The first sequence.</param>
	/// <param name="b">The second sequence.</param>
	/// <returns>A boolean value indicating equality.</returns>
	internal static bool SequenceEqual<T>(this in ReadOnlySequence<T> a, in ReadOnlySequence<T> b)
#if !NET
		where T : IEquatable<T>
#endif
	{
		if (a.Length != b.Length)
		{
			return false;
		}

		if (a.IsSingleSegment && b.IsSingleSegment)
		{
#if NET
			return a.FirstSpan.SequenceEqual(b.FirstSpan);
#else
			return a.First.Span.SequenceEqual(b.First.Span);
#endif
		}

		ReadOnlySequence<T>.Enumerator aEnumerator = a.GetEnumerator();
		ReadOnlySequence<T>.Enumerator bEnumerator = b.GetEnumerator();

		ReadOnlySpan<T> aCurrent = default;
		ReadOnlySpan<T> bCurrent = default;
		while (true)
		{
			bool aNext = TryGetNonEmptySpan(ref aEnumerator, ref aCurrent);
			bool bNext = TryGetNonEmptySpan(ref bEnumerator, ref bCurrent);
			if (!aNext && !bNext)
			{
				// We've reached the end of both sequences at the same time.
				return true;
			}
			else if (aNext != bNext)
			{
				// One ran out of bytes before the other.
				// We don't anticipate this, because we already checked the lengths.
				throw Assumes.NotReachable();
			}

			int commonLength = Math.Min(aCurrent.Length, bCurrent.Length);
			if (!aCurrent[..commonLength].SequenceEqual(bCurrent[..commonLength]))
			{
				return false;
			}

			aCurrent = aCurrent.Slice(commonLength);
			bCurrent = bCurrent.Slice(commonLength);
		}

		static bool TryGetNonEmptySpan(ref ReadOnlySequence<T>.Enumerator enumerator, ref ReadOnlySpan<T> span)
		{
			while (span.Length == 0)
			{
				if (!enumerator.MoveNext())
				{
					return false;
				}

				span = enumerator.Current.Span;
			}

			return true;
		}
	}

	[GenerateShapeFor<ReadOnlySequence<byte>>]
	private partial class Witness;
}
