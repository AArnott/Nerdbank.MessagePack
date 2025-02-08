// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.PolySerializer.Utilities;

/// <summary>
/// Extension methods for the <see cref="ReadOnlySpan{T}"/> and <see cref="ReadOnlyMemory{T}"/> types.
/// </summary>
internal static class MemoryExtensions
{
	/// <summary>
	/// Allocates an array for a filtered result of a span.
	/// </summary>
	/// <typeparam name="T">The type of element in the span.</typeparam>
	/// <param name="span">The span.</param>
	/// <param name="predicate">The function to test each element.</param>
	/// <returns>The list.</returns>
	internal static ReadOnlyMemory<T> Where<T>(this ReadOnlySpan<T> span, Predicate<T> predicate)
	{
		List<T> list = new List<T>(span.Length);
		foreach (T item in span)
		{
			if (predicate(item))
			{
				list.Add(item);
			}
		}

		return list.ToArray();
	}

	/// <summary>
	/// Creates a list out of a span.
	/// </summary>
	/// <typeparam name="T">The type of element in the span.</typeparam>
	/// <param name="span">The span.</param>
	/// <returns>The list.</returns>
	internal static List<T> ToList<T>(this ReadOnlySpan<T> span)
	{
		List<T> list = new(span.Length);
		foreach (T item in span)
		{
			list.Add(item);
		}

		return list;
	}
}
