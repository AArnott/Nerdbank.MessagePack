// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETWASM
#pragma warning disable SA1402, SA1649, SA1600, SA1611, SA1615, SA1618, CS1591

// NetWasm (netwasm0.1) proof of concept: CoreLib has no System.Collections.Concurrent.
// NetWasm is single-threaded, so this minimal stand-in wraps Dictionary with a lock (for reentrancy safety only).
namespace System.Collections.Concurrent;

using System.Diagnostics.CodeAnalysis;

internal class ConcurrentDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
	where TKey : notnull
{
	private readonly Dictionary<TKey, TValue> map;

	public ConcurrentDictionary()
	{
		this.map = new();
	}

	public ConcurrentDictionary(IEqualityComparer<TKey>? comparer)
	{
		this.map = new(comparer);
	}

	public int Count
	{
		get
		{
			lock (this.map)
			{
				return this.map.Count;
			}
		}
	}

	public bool IsEmpty => this.Count == 0;

	public ICollection<TKey> Keys
	{
		get
		{
			lock (this.map)
			{
				return new List<TKey>(this.map.Keys);
			}
		}
	}

	public ICollection<TValue> Values
	{
		get
		{
			lock (this.map)
			{
				return new List<TValue>(this.map.Values);
			}
		}
	}

	public TValue this[TKey key]
	{
		get
		{
			lock (this.map)
			{
				return this.map[key];
			}
		}

		set
		{
			lock (this.map)
			{
				this.map[key] = value;
			}
		}
	}

	public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
	{
		lock (this.map)
		{
			return this.map.TryGetValue(key, out value);
		}
	}

	public bool ContainsKey(TKey key)
	{
		lock (this.map)
		{
			return this.map.ContainsKey(key);
		}
	}

	public bool TryAdd(TKey key, TValue value)
	{
		lock (this.map)
		{
			return this.map.TryAdd(key, value);
		}
	}

	public bool TryRemove(TKey key, [MaybeNullWhen(false)] out TValue value)
	{
		lock (this.map)
		{
			return this.map.Remove(key, out value);
		}
	}

	public bool TryUpdate(TKey key, TValue newValue, TValue comparisonValue)
	{
		lock (this.map)
		{
			if (this.map.TryGetValue(key, out TValue? existing) && EqualityComparer<TValue>.Default.Equals(existing, comparisonValue))
			{
				this.map[key] = newValue;
				return true;
			}

			return false;
		}
	}

	public TValue GetOrAdd(TKey key, TValue value)
	{
		lock (this.map)
		{
			if (!this.map.TryGetValue(key, out TValue? existing))
			{
				this.map.Add(key, existing = value);
			}

			return existing;
		}
	}

	public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
	{
		if (this.TryGetValue(key, out TValue? existing))
		{
			return existing;
		}

		return this.GetOrAdd(key, valueFactory(key));
	}

	public TValue GetOrAdd<TArg>(TKey key, Func<TKey, TArg, TValue> valueFactory, TArg factoryArgument)
	{
		if (this.TryGetValue(key, out TValue? existing))
		{
			return existing;
		}

		return this.GetOrAdd(key, valueFactory(key, factoryArgument));
	}

	public TValue AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
	{
		lock (this.map)
		{
			TValue result = this.map.TryGetValue(key, out TValue? existing) ? updateValueFactory(key, existing) : addValue;
			this.map[key] = result;
			return result;
		}
	}

	public TValue AddOrUpdate(TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
	{
		lock (this.map)
		{
			TValue result = this.map.TryGetValue(key, out TValue? existing) ? updateValueFactory(key, existing) : addValueFactory(key);
			this.map[key] = result;
			return result;
		}
	}

	public void Clear()
	{
		lock (this.map)
		{
			this.map.Clear();
		}
	}

	public KeyValuePair<TKey, TValue>[] ToArray()
	{
		lock (this.map)
		{
			var result = new KeyValuePair<TKey, TValue>[this.map.Count];
			int i = 0;
			foreach (KeyValuePair<TKey, TValue> pair in this.map)
			{
				result[i++] = pair;
			}

			return result;
		}
	}

	public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => ((IEnumerable<KeyValuePair<TKey, TValue>>)this.ToArray()).GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}
#endif
