// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Nerdbank.MessagePack.SecureHash;

/// <summary>
/// A secure equality comparer that performs deep by-value comparisons, and uses collision-resistant hashing.
/// </summary>
/// <typeparam name="TEnumerable">The type of the enumerable.</typeparam>
/// <typeparam name="TElement">The type of element in the enumerable.</typeparam>
/// <param name="equalityComparer">The equality comparer for individual elements.</param>
/// <param name="getEnumerable">The function that gets the enumerable of the collection.</param>
internal class SecureEnumerableEqualityComparer<TEnumerable, TElement>(
	SecureEqualityComparer<TElement> equalityComparer,
	Func<TEnumerable, IEnumerable<TElement>> getEnumerable) : SecureEqualityComparer<TEnumerable>
{
	/// <inheritdoc/>
	public override bool Equals(TEnumerable? x, TEnumerable? y)
	{
		if (x is null || y is null)
		{
			return ReferenceEquals(x, y);
		}

		IEnumerable<TElement> enumerableX = getEnumerable(x);
		IEnumerable<TElement> enumerableY = getEnumerable(y);

		if (Enumerable.TryGetNonEnumeratedCount(enumerableX, out int countX) &&
			Enumerable.TryGetNonEnumeratedCount(enumerableY, out int countY) &&
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
	public override long GetSecureHashCode([DisallowNull] TEnumerable obj)
	{
		IEnumerable<TElement> enumerable = getEnumerable(obj);

		// Ideally we could switch this to a SIP hash implementation that can process additional data in chunks with a constant amount of memory.
		List<long> hashes = Enumerable.TryGetNonEnumeratedCount(enumerable, out int count) ? new(count) : new();
		foreach (TElement element in enumerable)
		{
			hashes.Add(element is null ? 0 : equalityComparer.GetSecureHashCode(element));
		}

		return SipHash.Default.Compute(MemoryMarshal.Cast<long, byte>(CollectionsMarshal.AsSpan(hashes)));
	}
}
