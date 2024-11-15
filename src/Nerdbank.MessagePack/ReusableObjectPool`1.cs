// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack;

/// <summary>
/// A means to pool objects for reuse.
/// </summary>
/// <typeparam name="T">The type of object to pool.</typeparam>
internal static class ReusableObjectPool<T>
	where T : IPoolableObject, new()
{
	/// <summary>
	/// A pool of objects that can be reused to reduce allocations.
	/// </summary>
	private static readonly ThreadLocal<Stack<T>> Pool = new(() => new Stack<T>());

	/// <summary>
	/// Retrieves an object from the pool, or creates a new one if the pool is empty.
	/// </summary>
	/// <returns>The object.</returns>
	internal static T Take() => Pool.Value!.TryPop(out T? result) ? result : new T();

	/// <summary>
	/// Clears an object's state and returns it to the pool for reuse.
	/// </summary>
	/// <param name="item">The object to disown.</param>
	internal static void Return(T item)
	{
		item.Recycle();
		Pool.Value!.Push(item);
	}
}
