// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// Extension methods for PolyType types.
/// </summary>
internal static class PolyTypeExtensions
{
	/// <summary>
	/// The maximum collection capacity to allocate before reading elements from a streaming source.
	/// </summary>
	internal const int MaxStreamingCollectionPreallocation = 4096;

	/// <summary>
	/// Gets the initial capacity to allocate before reading elements from a streaming source.
	/// </summary>
	/// <param name="count">The element count declared by the messagepack header.</param>
	/// <returns>A capacity that does not exceed <see cref="MaxStreamingCollectionPreallocation" />.</returns>
	internal static int GetStreamingCollectionInitialCapacity(int count) => Math.Min(count, MaxStreamingCollectionPreallocation);

	/// <summary>
	/// Ensures a pooled buffer can store the required number of elements.
	/// </summary>
	/// <typeparam name="T">The element type.</typeparam>
	/// <param name="buffer">The buffer to grow, if required.</param>
	/// <param name="initializedLength">The number of elements in <paramref name="buffer" /> that have been initialized.</param>
	/// <param name="requiredLength">The minimum length required.</param>
	/// <param name="maxLength">The maximum useful length.</param>
	/// <returns>A buffer with at least <paramref name="requiredLength" /> elements.</returns>
	internal static T[] EnsurePooledBufferSize<T>(T[] buffer, int initializedLength, int requiredLength, int maxLength)
	{
		if (buffer.Length >= requiredLength)
		{
			return buffer;
		}

		int doubledLength = buffer.Length <= maxLength / 2 ? buffer.Length * 2 : maxLength;
		int newLength = Math.Max(requiredLength, doubledLength);
		T[] newBuffer = ArrayPool<T>.Shared.Rent(newLength);
		buffer.AsSpan(0, initializedLength).CopyTo(newBuffer);
		ArrayPool<T>.Shared.Return(buffer);
		return newBuffer;
	}

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
