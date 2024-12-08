// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NET
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8767 // null ref annotations
#endif

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Nerdbank.MessagePack.SecureHash;

/// <summary>
/// An equality comparer that performs deep by-value comparisons.
/// </summary>
/// <typeparam name="TEnumerable">The type of the enumerable.</typeparam>
/// <typeparam name="TElement">The type of element in the enumerable.</typeparam>
/// <param name="equalityComparer">The equality comparer for individual elements.</param>
/// <param name="getEnumerable">The function that gets the enumerable of the collection.</param>
internal class ByValueEnumerableEqualityComparer<TEnumerable, TElement>(
	IEqualityComparer<TElement> equalityComparer,
	Func<TEnumerable, IEnumerable<TElement>> getEnumerable) : IEqualityComparer<TEnumerable>
{
	/// <inheritdoc/>
	public bool Equals(TEnumerable? x, TEnumerable? y)
	{
		if (x is null || y is null)
		{
			return ReferenceEquals(x, y);
		}

		IEnumerable<TElement> enumerableX = getEnumerable(x);
		IEnumerable<TElement> enumerableY = getEnumerable(y);

		if (PolyfillExtensions.TryGetNonEnumeratedCount(enumerableX, out int countX) &&
			PolyfillExtensions.TryGetNonEnumeratedCount(enumerableY, out int countY) &&
			countX != countY)
		{
			return false;
		}

		using IEnumerator<TElement> enumeratorX = enumerableX.GetEnumerator();
		using IEnumerator<TElement> enumeratorY = enumerableY.GetEnumerator();
		while (enumeratorX.MoveNext())
		{
			if (!enumeratorY.MoveNext() || !equalityComparer.Equals(enumeratorX.Current, enumeratorY.Current))
			{
				return false;
			}
		}

		return !enumeratorY.MoveNext();
	}

	/// <inheritdoc/>
	public int GetHashCode([DisallowNull] TEnumerable obj)
	{
		IEnumerable<TElement> enumerable = getEnumerable(obj);

		// Ideally we could switch this to a SIP hash implementation that can process additional data in chunks with a constant amount of memory.
		List<int> hashes = new();
		foreach (TElement element in enumerable)
		{
			hashes.Add(element is null ? 0 : equalityComparer.GetHashCode(element));
		}

#if NET
		Span<int> span = CollectionsMarshal.AsSpan(hashes);
#else
		Span<int> span = hashes.ToArray();
#endif
		return unchecked((int)SipHash.Default.Compute(MemoryMarshal.Cast<int, byte>(span)));
	}
}
