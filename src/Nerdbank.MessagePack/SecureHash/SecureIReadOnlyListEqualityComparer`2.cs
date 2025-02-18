// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace ShapeShift.SecureHash;

/// <summary>
/// A secure equality comparer that performs deep by-value comparisons, and uses collision-resistant hashing.
/// </summary>
/// <typeparam name="TEnumerable">The type of the enumerable. Must be assignable to <see cref="IReadOnlyList{T}" />.</typeparam>
/// <typeparam name="TElement">The type of element in the enumerable.</typeparam>
/// <param name="equalityComparer">The equality comparer for individual elements.</param>
internal class SecureIReadOnlyListEqualityComparer<TEnumerable, TElement>(SecureEqualityComparer<TElement> equalityComparer) : SecureEqualityComparer<TEnumerable>
{
	/// <inheritdoc/>
	public override bool Equals(TEnumerable? x, TEnumerable? y)
	{
		if (x is null || y is null)
		{
			return ReferenceEquals(x, y);
		}

		IReadOnlyList<TElement> xList = (IReadOnlyList<TElement>)x;
		IReadOnlyList<TElement> yList = (IReadOnlyList<TElement>)y;

		if (xList.Count != yList.Count)
		{
			return false;
		}

		for (int i = 0; i < xList.Count; i++)
		{
			if (!equalityComparer.Equals(xList[i], yList[i]))
			{
				return false;
			}
		}

		return true;
	}

	/// <inheritdoc/>
	public override long GetSecureHashCode([DisallowNull] TEnumerable obj)
	{
		IReadOnlyList<TElement> list = (IReadOnlyList<TElement>)obj;

		// Ideally we could switch this to a SIP hash implementation that can process additional data in chunks with a constant amount of memory.
		long[] hashes = new long[list.Count];
		for (int i = 0; i < list.Count; i++)
		{
			hashes[i] = list[i] is TElement element ? equalityComparer.GetSecureHashCode(element) : 0;
		}

		return SipHash.Default.Compute(MemoryMarshal.Cast<long, byte>(hashes.AsSpan()));
	}
}
