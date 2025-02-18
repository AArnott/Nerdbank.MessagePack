// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ShapeShift;

/// <summary>
/// A means to pool objects for reuse.
/// </summary>
/// <typeparam name="T">The type of object to pool.</typeparam>
internal class ReusableObjectPool<T>(Func<T> factory)
	where T : IPoolableObject
{
	/// <summary>
	/// A pool of objects that can be reused to reduce allocations.
	/// </summary>
	private readonly ThreadLocal<Stack<T>> Pool = new(() => new Stack<T>());

	/// <summary>
	/// Retrieves an object from the pool, or creates a new one if the pool is empty.
	/// </summary>
	/// <param name="serializer">The owner for this user.</param>
	/// <returns>The object.</returns>
	internal T Take(SerializerBase? serializer)
	{
		if (!this.Pool.Value!.TryPop(out T? result))
		{
			result = factory();
		}

		result.Owner = serializer;
		return result;
	}

	/// <summary>
	/// Clears an object's state and returns it to the pool for reuse.
	/// </summary>
	/// <param name="item">The object to disown.</param>
	internal void Return(T item)
	{
		item.Recycle();
		item.Owner = null;
		this.Pool.Value!.Push(item);
	}
}
