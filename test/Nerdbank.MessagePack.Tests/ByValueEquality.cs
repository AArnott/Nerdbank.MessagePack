// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

internal static class ByValueEquality
{
	internal static bool Equal<TKey, TValue>(IReadOnlyDictionary<TKey, TValue>? left, IReadOnlyDictionary<TKey, TValue>? right, IEqualityComparer<TValue>? valueComparer = null)
	{
		if (left is null || right is null)
		{
			return left is null == right is null;
		}

		if (left.Count != right.Count)
		{
			return false;
		}

		valueComparer ??= EqualityComparer<TValue>.Default;
		foreach (KeyValuePair<TKey, TValue> item in left)
		{
			if (!right.TryGetValue(item.Key, out TValue? otherValue) || !valueComparer.Equals(item.Value, otherValue))
			{
				return false;
			}
		}

		return true;
	}
}
