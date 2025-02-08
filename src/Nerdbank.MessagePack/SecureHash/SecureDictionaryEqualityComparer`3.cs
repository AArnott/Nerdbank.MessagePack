// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Nerdbank.PolySerializer.SecureHash;

/// <summary>
/// A by-value, hash-collision resistant equality comparer for dictionary types.
/// </summary>
/// <typeparam name="TDictionary">The type of dictionary.</typeparam>
/// <typeparam name="TKey">The type of key.</typeparam>
/// <typeparam name="TValue">The type of value.</typeparam>
/// <param name="getDictionary">A function that can get a meaningful dictionary out of a <typeparamref name="TDictionary"/>.</param>
/// <param name="keyEqualityComparer">The equality comparer to use for the key.</param>
/// <param name="valueEqualityComparer">The equality comparer to use for the value.</param>
internal class SecureDictionaryEqualityComparer<TDictionary, TKey, TValue>(
	Func<TDictionary, IReadOnlyDictionary<TKey, TValue>> getDictionary,
	SecureEqualityComparer<TKey> keyEqualityComparer,
	SecureEqualityComparer<TValue> valueEqualityComparer) : SecureEqualityComparer<TDictionary>
{
	/// <inheritdoc/>
	public override bool Equals(TDictionary? x, TDictionary? y)
	{
		if (x is null || y is null)
		{
			return x is null && y is null;
		}

		if (ReferenceEquals(x, y))
		{
			return true;
		}

		IReadOnlyDictionary<TKey, TValue> xDict = getDictionary(x);
		IReadOnlyDictionary<TKey, TValue> yDict = getDictionary(y);

		if (xDict.Count != yDict.Count)
		{
			return false;
		}

		foreach (KeyValuePair<TKey, TValue> pair in xDict)
		{
			if (!yDict.TryGetValue(pair.Key, out TValue? yValue) || !valueEqualityComparer.Equals(pair.Value, yValue))
			{
				return false;
			}
		}

		return true;
	}

	/// <inheritdoc/>
	public override long GetSecureHashCode([DisallowNull] TDictionary obj)
	{
		IReadOnlyDictionary<TKey, TValue> dict = getDictionary(obj);

		// Ideally we could switch this to a SIP hash implementation that can process additional data in chunks with a constant amount of memory.
		long[] hashes = new long[dict.Count * 2];
		int index = 0;
		foreach (KeyValuePair<TKey, TValue> pair in dict)
		{
			hashes[index++] = pair.Key is null ? 0 : keyEqualityComparer.GetSecureHashCode(pair.Key);
			hashes[index++] = pair.Value is null ? 0 : valueEqualityComparer.GetSecureHashCode(pair.Value);
		}

		return SipHash.Default.Compute(MemoryMarshal.Cast<long, byte>(hashes));
	}
}
