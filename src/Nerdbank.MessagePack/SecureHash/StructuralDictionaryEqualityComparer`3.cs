// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NET
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8767 // null ref annotations
#endif

using System.Diagnostics.CodeAnalysis;

namespace Nerdbank.PolySerializer.SecureHash;

/// <summary>
/// A by-value equality comparer for dictionary types.
/// </summary>
/// <typeparam name="TDictionary">The type of dictionary.</typeparam>
/// <typeparam name="TKey">The type of key.</typeparam>
/// <typeparam name="TValue">The type of value.</typeparam>
/// <param name="getDictionary">A function that can get a meaningful dictionary out of a <typeparamref name="TDictionary"/>.</param>
/// <param name="keyEqualityComparer">The equality comparer to use for the key.</param>
/// <param name="valueEqualityComparer">The equality comparer to use for the value.</param>
internal class StructuralDictionaryEqualityComparer<TDictionary, TKey, TValue>(
	Func<TDictionary, IReadOnlyDictionary<TKey, TValue>> getDictionary,
	IEqualityComparer<TKey> keyEqualityComparer,
	IEqualityComparer<TValue> valueEqualityComparer) : IEqualityComparer<TDictionary>
{
	/// <inheritdoc/>
	public bool Equals(TDictionary? x, TDictionary? y)
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
	public int GetHashCode([DisallowNull] TDictionary obj)
	{
		HashCode hashCode = default;
		IReadOnlyDictionary<TKey, TValue> dict = getDictionary(obj);
		foreach (KeyValuePair<TKey, TValue> pair in dict)
		{
			hashCode.Add(pair.Key is null ? 0 : keyEqualityComparer.GetHashCode(pair.Key));
			hashCode.Add(pair.Value is null ? 0 : valueEqualityComparer.GetHashCode(pair.Value));
		}

		return hashCode.ToHashCode();
	}
}
