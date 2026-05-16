// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// Extension methods for PolyType types.
/// </summary>
internal static class PolyTypeExtensions
{
	/// <summary>
	/// Creates a copy of the specified <see cref="CollectionConstructionOptions{TKey}"/> with a new capacity.
	/// </summary>
	/// <typeparam name="TKey">The key used in comparers.</typeparam>
	/// <param name="options">The template options struct.</param>
	/// <param name="capacity">The capacity to set.</param>
	/// <returns>The new options.</returns>
	/// <remarks>
	/// This should be removed when <see href="https://github.com/eiriktsarpalis/PolyType/pull/198">this pull request</see> is merged and shipped.
	/// </remarks>
	internal static CollectionConstructionOptions<TKey> WithCapacity<TKey>(this in CollectionConstructionOptions<TKey> options, int capacity)
	{
		if (options.Capacity == capacity)
		{
			return options;
		}

		return new CollectionConstructionOptions<TKey>
		{
			Capacity = capacity,
			Comparer = options.Comparer,
			EqualityComparer = options.EqualityComparer,
		};
	}
}
