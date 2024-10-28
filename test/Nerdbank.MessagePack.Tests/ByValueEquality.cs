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

	internal static bool Equal<T>(IEnumerable<T>? left, IEnumerable<T>? right, IEqualityComparer<T>? equalityComparer = null) => Equal(left?.ToArray(), right?.ToArray(), equalityComparer);

	internal static bool Equal<T>(IReadOnlyList<T>? left, IReadOnlyList<T>? right, IEqualityComparer<T>? equalityComparer = null)
	{
		equalityComparer ??= EqualityComparer<T>.Default;

		if (left is null || right is null)
		{
			return left is null == right is null;
		}

		if (left.Count != right.Count)
		{
			return false;
		}

		for (int i = 0; i < left.Count; i++)
		{
			if (!equalityComparer.Equals(left[i], right[i]))
			{
				return false;
			}
		}

		return true;
	}

	internal static bool Equal<T>(T[,]? left, T[,]? right, IEqualityComparer<T>? equalityComparer = null)
	{
		equalityComparer ??= EqualityComparer<T>.Default;

		if (left is null || right is null)
		{
			return left is null == right is null;
		}

		int rows = left.GetLength(0);
		int cols = left.GetLength(1);

		if (rows != right.GetLength(0) || cols != right.GetLength(1))
		{
			return false;
		}

		for (int i = 0; i < rows; i++)
		{
			for (int j = 0; j < cols; j++)
			{
				if (!equalityComparer.Equals(left[i, j], right[i, j]))
				{
					return false;
				}
			}
		}

		return true;
	}
}
