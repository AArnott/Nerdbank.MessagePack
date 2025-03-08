// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

internal static class StructuralEquality
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

	internal static bool Equal<T>(IEnumerable<T>? left, IEnumerable<T>? right, IEqualityComparer<T>? equalityComparer = null) => Equal((Array?)left?.ToArray(), right?.ToArray(), equalityComparer);

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

	internal static bool Equal<T>(Array? left, Array? right, IEqualityComparer<T>? equalityComparer = null)
	{
		equalityComparer ??= EqualityComparer<T>.Default;

		if (left is null || right is null)
		{
			return left is null == right is null;
		}

		if (left.Rank != right.Rank)
		{
			return false;
		}

		for (int dimension = 0; dimension < left.Rank; dimension++)
		{
			if (left.GetLength(dimension) != right.GetLength(dimension))
			{
				return false;
			}
		}

		System.Collections.IEnumerator leftEnumerator = left.GetEnumerator();
		System.Collections.IEnumerator rightEnumerator = right.GetEnumerator();

		while (leftEnumerator.MoveNext() && rightEnumerator.MoveNext())
		{
			if (!equalityComparer.Equals((T)leftEnumerator.Current, (T)rightEnumerator.Current))
			{
				return false;
			}
		}

		return !leftEnumerator.MoveNext() && !rightEnumerator.MoveNext();
	}
}
