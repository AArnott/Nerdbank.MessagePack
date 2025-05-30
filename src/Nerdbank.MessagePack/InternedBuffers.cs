// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

/// <summary>
/// Interns byte arrays to reduce memory usage.
/// </summary>
internal class InternedBuffers : IDisposable
{
	private readonly WeakArrayCacheInterner interner = new();

	/// <summary>
	/// Interns a buffer with a weak reference.
	/// </summary>
	/// <param name="span">The buffer to be interned.</param>
	/// <returns>The interned copy of the buffer.</returns>
	public ReadOnlyMemory<byte> Intern(ReadOnlySpan<byte> span)
	{
		InternableArray internableArray = new(span);
		return this.interner.InternableToArray(ref internableArray);
	}

	/// <inheritdoc/>
	public void Dispose() => this.interner.Dispose();

	private ref struct InternableArray
	{
		private readonly ReadOnlySpan<byte> span;

		internal InternableArray(ReadOnlySpan<byte> span)
		{
			this.span = span;
			this.Length = span.Length;
		}

		internal int Length { get; }

		public readonly bool Equals(byte[] other)
		{
			if (this.Length != other.Length)
			{
				return false;
			}

			return this.span.SequenceEqual(other);
		}

		public override readonly int GetHashCode()
		{
			HashCode hc = default;
#if NET
			hc.AddBytes(this.span);
#else
			for (int i = 0; i < this.span.Length; i++)
			{
				hc.Add(this.span[i]);
			}
#endif
			return hc.ToHashCode();
		}

		internal readonly byte[] ExpensiveConvertToArray()
		{
			if (this.Length == 0)
			{
				return Array.Empty<byte>();
			}

			return this.span.ToArray();
		}
	}

	private sealed class WeakArrayCacheInterner : IDisposable
	{
		private readonly WeakArrayCache weakArrayCache = new();

		/// <summary>
		/// Enumerates the possible interning results.
		/// </summary>
		private enum InternResult
		{
			FoundInWeakArrayCache,
			AddedToWeakArrayCache,
		}

		public void Dispose() => this.weakArrayCache.Dispose();

		internal byte[] InternableToArray(ref InternableArray candidate)
		{
			if (candidate.Length == 0)
			{
				return Array.Empty<byte>();
			}

			InternResult resultForStatistics = this.Intern(ref candidate, out byte[] internedArray);
#if DEBUG
			byte[] expectedArray = candidate.ExpensiveConvertToArray();
			if (!expectedArray.SequenceEqual(internedArray))
			{
				throw new InvalidOperationException($"Interned array {internedArray} should have been {expectedArray}");
			}
#endif

			return internedArray;
		}

		/// <summary>
		/// Try to intern the array.
		/// The return value indicates the how the array was interned.
		/// </summary>
		private InternResult Intern(ref InternableArray candidate, out byte[] interned)
		{
			interned = this.weakArrayCache.GetOrCreateEntry(ref candidate, out bool cacheHit);
			return cacheHit ? InternResult.FoundInWeakArrayCache : InternResult.AddedToWeakArrayCache;
		}
	}

	private sealed class WeakArrayCache : IDisposable
	{
		/// <summary>
		/// Initial capacity of the underlying dictionary.
		/// </summary>
		private const int InitialCapacity = 503;

		private readonly Dictionary<int, ArrayWeakHandle> arraysByHashCode;

		/// <summary>
		/// The maximum size we let the collection grow before scavenging unused entries.
		/// </summary>
		private int scavengeThreshold = InitialCapacity;

		public WeakArrayCache()
		{
			this.arraysByHashCode = new Dictionary<int, ArrayWeakHandle>(InitialCapacity);
		}

		~WeakArrayCache()
		{
			this.Dispose(false);
		}

		/// <summary>
		/// Main entrypoint of this cache. Tries to look up a array that matches the given internable. If it succeeds, returns
		/// the array and sets cacheHit to true. If the array is not found, calls ExpensiveConvertToArray on the internable,
		/// adds the resulting array to the cache, and returns it, setting cacheHit to false.
		/// </summary>
		/// <param name="internable">The internable describing the array we're looking for.</param>
		/// <param name="cacheHit">Whether the entry was already in the cache.</param>
		/// <returns>A array matching the given internable.</returns>
		public byte[] GetOrCreateEntry(ref InternableArray internable, out bool cacheHit)
		{
			int hashCode = internable.GetHashCode();

			ArrayWeakHandle? handle;
			byte[]? result;
			bool addingNewHandle = false;

			lock (this.arraysByHashCode)
			{
				if (this.arraysByHashCode.TryGetValue(hashCode, out handle))
				{
					result = handle.GetArray(ref internable);
					if (result != null)
					{
						cacheHit = true;
						return result;
					}
				}
				else
				{
					handle = new ArrayWeakHandle();
					addingNewHandle = true;
				}

				// We don't have the array in the cache - create it.
				result = internable.ExpensiveConvertToArray();

				// Set the handle to reference the new array.
				handle.SetArray(result);

				if (addingNewHandle)
				{
					// Prevent the dictionary from growing forever with GC handles that don't reference live arrays anymore.
					if (this.arraysByHashCode.Count >= this.scavengeThreshold)
					{
						// Get rid of unused handles.
						this.ScavengeNoLock();

						// And do this again when the number of handles reaches double the current after-scavenge number.
						this.scavengeThreshold = this.arraysByHashCode.Count * 2;
					}
				}

				this.arraysByHashCode[hashCode] = handle;
			}

			cacheHit = false;
			return result;
		}

		/// <summary>
		/// Public version of ScavengeUnderLock() which takes the lock.
		/// </summary>
		public void Scavenge()
		{
			lock (this.arraysByHashCode)
			{
				this.ScavengeNoLock();
			}
		}

		/// <summary>
		/// Frees all GC handles and clears the cache.
		/// </summary>
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			foreach (KeyValuePair<int, ArrayWeakHandle> entry in this.arraysByHashCode)
			{
				entry.Value.Free();
			}

			this.arraysByHashCode.Clear();
		}

		/// <summary>
		/// Iterates over the cache and removes unused GC handles, i.e. handles that don't reference live arrays.
		/// This is expensive so try to call such that the cost is amortized to O(1) per GetOrCreateEntry() invocation.
		/// Assumes the lock is taken by the caller.
		/// </summary>
		private void ScavengeNoLock()
		{
			List<int>? keysToRemove = null;
			foreach (KeyValuePair<int, ArrayWeakHandle> entry in this.arraysByHashCode)
			{
				if (!entry.Value.IsUsed)
				{
					entry.Value.Free();
					keysToRemove ??= new List<int>();
					keysToRemove.Add(entry.Key);
				}
			}

			if (keysToRemove != null)
			{
				for (int i = 0; i < keysToRemove.Count; i++)
				{
					this.arraysByHashCode.Remove(keysToRemove[i]);
				}
			}
		}

		/// <summary>
		/// Holds a weak GC handle to a array. Shared by all arrays with the same hash code and referencing the last such array we've seen.
		/// </summary>
		private class ArrayWeakHandle
		{
			/// <summary>
			/// Weak GC handle to the last array of the given hashcode we've seen.
			/// </summary>
			private GCHandle weakHandle;

			/// <summary>
			/// Gets a value indicating whether the array referenced by the handle is still alive.
			/// </summary>
			internal bool IsUsed => this.weakHandle.Target != null;

			/// <summary>
			/// Returns the array referenced by this handle if it is equal to the given internable.
			/// </summary>
			/// <param name="internable">The internable describing the array we're looking for.</param>
			/// <returns>The array matching the internable or null if the handle is referencing a collected array or the array is different.</returns>
			internal byte[]? GetArray(ref InternableArray internable)
			{
				if (this.weakHandle.IsAllocated && this.weakHandle.Target is byte[] str)
				{
					if (internable.Equals(str))
					{
						return str;
					}
				}

				return null;
			}

			/// <summary>
			/// Sets the handle to the given array. If the handle is still referencing another live array, that array is effectively forgotten.
			/// </summary>
			/// <param name="str">The array to set.</param>
			internal void SetArray(byte[] str)
			{
				if (!this.weakHandle.IsAllocated)
				{
					// The handle is not allocated - allocate it.
					this.weakHandle = GCHandle.Alloc(str, GCHandleType.Weak);
				}
				else
				{
					this.weakHandle.Target = str;
				}
			}

			/// <summary>
			/// Frees the GC handle.
			/// </summary>
			internal void Free()
			{
				this.weakHandle.Free();
			}
		}
	}
}
