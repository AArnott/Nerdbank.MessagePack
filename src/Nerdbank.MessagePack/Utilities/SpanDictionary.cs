// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// This file was originally derived from https://github.com/eiriktsarpalis/PolyType/
// with Eirik Tsarpalis getting credit for the original implementation.
#pragma warning disable SA1402 // File may only contain a single type

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Nerdbank.MessagePack.Utilities;

/// <summary>Defines factory methods for <see cref="SpanDictionary{TKey, TValue}"/>.</summary>
internal static class SpanDictionary
{
	/// <summary>Maps the specified enumerable using a dictionary using the provided transformers.</summary>
	/// <typeparam name="TSource">The type of element in the sequence to be converted to a dictionary.</typeparam>
	/// <typeparam name="TKey"><inheritdoc cref="SpanDictionary{TKey, TValue}" path="/typeparam[@name='TKey']"/></typeparam>
	/// <inheritdoc cref="ToSpanDictionary{TSource, TKey, TValue}(IEnumerable{TSource}, Func{TSource, ReadOnlyMemory{TKey}}, Func{TSource, TValue}, ISpanEqualityComparer{TKey})"/>
	/// <returns>The newly created <see cref="SpanDictionary{TKey, TValue}"/>.</returns>
	internal static SpanDictionary<TKey, TSource> ToSpanDictionary<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, ReadOnlyMemory<TKey>> keySelector, ISpanEqualityComparer<TKey> keyComparer)
		=> new(source.Select(t => new KeyValuePair<ReadOnlyMemory<TKey>, TSource>(keySelector(t), t)), keyComparer);

	/// <summary>Maps the specified enumerable using a dictionary using the provided transformers.</summary>
	/// <typeparam name="TSource">The type of element in the sequence to be converted to a dictionary.</typeparam>
	/// <typeparam name="TKey"><inheritdoc cref="SpanDictionary{TKey, TValue}" path="/typeparam[@name='TKey']"/></typeparam>
	/// <typeparam name="TValue"><inheritdoc cref="SpanDictionary{TKey, TValue}" path="/typeparam[@name='TValue']"/></typeparam>
	/// <param name="source">The sequence to be mapped into the dictionary.</param>
	/// <param name="keySelector">The function that transforms an element from <paramref name="source"/> into a key in the dictionary.</param>
	/// <param name="valueSelector">The function that transforms an element from <paramref name="source"/> into a value in the dictionary.</param>
	/// <param name="keyComparer">The equality comparer to use for matching keys.</param>
	/// <returns>The newly created <see cref="SpanDictionary{TKey, TValue}"/>.</returns>
	internal static SpanDictionary<TKey, TValue> ToSpanDictionary<TSource, TKey, TValue>(this IEnumerable<TSource> source, Func<TSource, ReadOnlyMemory<TKey>> keySelector, Func<TSource, TValue> valueSelector, ISpanEqualityComparer<TKey> keyComparer)
		=> new(source.Select(t => new KeyValuePair<ReadOnlyMemory<TKey>, TValue>(keySelector(t), valueSelector(t))), keyComparer);
}

/// <summary>
/// Read-only HashTable allowing lookup of <see cref="ReadOnlySpan{T}"/> keys.
/// </summary>
/// <typeparam name="TKey">The element type for the <see cref="ReadOnlySpan{T}"/> that serves as the key in the dictionary.</typeparam>
/// <typeparam name="TValue">The value type for the dictionary.</typeparam>
internal sealed class SpanDictionary<TKey, TValue>
#pragma warning restore SA1402 // File may only contain a single type
{
	private readonly int[] buckets;
	private readonly Entry[] entries;
	private readonly ulong fastModMultiplier;
	private readonly ISpanEqualityComparer<TKey> comparer;
	private readonly (EntryType Type, Entry Entry)[]? entryByLength;

	/// <summary>Initializes a new instance of the <see cref="SpanDictionary{TKey, TValue}"/> class.</summary>
	/// <param name="input">The sequence of keys and values to include in the dictionary.</param>
	/// <param name="comparer">The comparer to use to match keys.</param>
	internal SpanDictionary(IEnumerable<KeyValuePair<ReadOnlyMemory<TKey>, TValue>> input, ISpanEqualityComparer<TKey> comparer)
	{
		KeyValuePair<ReadOnlyMemory<TKey>, TValue>[] source = input.ToArray();
		int size = source.Length;
		this.comparer = comparer;

		this.buckets = new int[Math.Max(size, 1)];
		this.entries = new Entry[size];

		if (Environment.Is64BitProcess)
		{
			this.fastModMultiplier = GetFastModMultiplier((uint)this.buckets.Length);
		}

		int idx = 0;
		ulong lengthsObserved = 0;
		int uniqueLengths = 0;
		int count = 0;
		int maxAllowableLength = 0;
		foreach (KeyValuePair<ReadOnlyMemory<TKey>, TValue> kvp in source)
		{
			ReadOnlyMemory<TKey> key = kvp.Key;
			ReadOnlySpan<TKey> keySpan = key.Span;
			uint hashCode = this.GetHashCode(keySpan);
			ref int bucket = ref this.GetBucket(hashCode);

			while (bucket != 0)
			{
				ref Entry current = ref this.entries[bucket - 1];
				if (current.HashCode == hashCode && comparer.Equals(keySpan, current.Key.Span))
				{
					throw new ArgumentException("duplicate key found");
				}

				bucket = ref current.Next;
			}

			ref Entry entry = ref this.entries[idx];
			entry.HashCode = hashCode;
			entry.Key = key;
			entry.Value = kvp.Value;
			bucket = ++idx;

			count++;
			int len = kvp.Key.Length;
			if (len < 64)
			{
				maxAllowableLength = Math.Max(maxAllowableLength, len);
				if ((lengthsObserved & (1UL << len)) != 0)
				{
					// We've already seen a key with the same length.
					// TODO: camel vs Pascal casing screws us up here.
					uniqueLengths--;
				}
				else
				{
					lengthsObserved |= 1UL << len;
					uniqueLengths++;
				}
			}
		}

		// If there's a lot of unique lengths, we can optimize the lookup by length.
		if (count > 0 && uniqueLengths * 100 / count > 66)
		{
			this.entryByLength = new (EntryType, Entry)[maxAllowableLength + 1];
			foreach (Entry entry in this.entries)
			{
				ref (EntryType Type, Entry Entry) slot = ref this.entryByLength[entry.Key.Length];
				if (slot.Type == EntryType.None)
				{
					slot.Type = EntryType.Single;
					slot.Entry = entry;
				}
				else if (slot.Type == EntryType.Single)
				{
					slot.Type = EntryType.Multi;
					slot.Entry = default;
				}
			}
		}
	}

	private enum EntryType
	{
		None,
		Single,
		Multi,
	}

	/// <summary>Gets the numbers of entries on the dictionary.</summary>
	public int Count => this.entries.Length;

	/// <summary>Attempts to look up an entry by a span key.</summary>
	/// <param name="key">The key to look up.</param>
	/// <param name="value">Receives the value matching the <paramref name="key"/>.</param>
	/// <returns><see langword="true" /> if the <paramref name="key"/> matched an entry in the dictionary; otherwise <see langword="false" />.</returns>
	public bool TryGetValue(ReadOnlySpan<TKey> key, [MaybeNullWhen(false)] out TValue value)
	{
		if (this.entryByLength is not null)
		{
			ref (EntryType Type, Entry Entry) slot = ref this.entryByLength[key.Length];
			switch (slot.Type)
			{
				case EntryType.None:
					value = default;
					return false;
				case EntryType.Single:
					if (!this.comparer.Equals(key, slot.Entry.Key.Span))
					{
						value = default;
						return false;
					}

					value = slot.Entry.Value;
					return true;
				case EntryType.Multi:
					// Fallback to hashing.
					break;
			}
		}

		Entry[] entries = this.entries;
		uint hashCode = this.GetHashCode(key);
		int bucket = this.GetBucket(hashCode);
		ISpanEqualityComparer<TKey> comparer = this.comparer;

		while (bucket != 0)
		{
			ref Entry current = ref entries[bucket - 1];
			if (current.HashCode == hashCode && comparer.Equals(key, current.Key.Span))
			{
				value = current.Value;
				return true;
			}

			bucket = current.Next;
		}

		value = default;
		return false;
	}

	/// <summary>Returns approximate reciprocal of the divisor: ceil(2**64 / divisor).</summary>
	/// <remarks>This should only be used on 64-bit.</remarks>
	private static ulong GetFastModMultiplier(uint divisor) => unchecked((ulong.MaxValue / divisor) + 1);

	/// <summary>Performs a mod operation using the multiplier pre-computed with <see cref="GetFastModMultiplier"/>.</summary>
	/// <remarks>This should only be used on 64-bit.</remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static uint FastMod(uint value, uint divisor, ulong multiplier)
	{
		// We use modified Daniel Lemire's fastmod algorithm (https://github.com/dotnet/runtime/pull/406),
		// which allows to avoid the long multiplication if the divisor is less than 2**31.
		Debug.Assert(divisor <= int.MaxValue, $"{divisor} <= int.MaxValue");

		// This is equivalent of (uint)Math.BigMul(multiplier * value, divisor, out _). This version
		// is faster than BigMul currently because we only need the high bits.
		uint highbits = unchecked((uint)(((((multiplier * value) >> 32) + 1) * divisor) >> 32));

		Debug.Assert(highbits == value % divisor, $"{highbits} == {value} % {divisor}");
		return highbits;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private uint GetHashCode(ReadOnlySpan<TKey> key)
		=> unchecked((uint)this.comparer.GetHashCode(key));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private ref int GetBucket(uint hashCode)
	{
		int[] buckets = this.buckets;
		if (Environment.Is64BitProcess)
		{
			return ref buckets[FastMod(hashCode, (uint)buckets.Length, this.fastModMultiplier)];
		}
		else
		{
			return ref buckets[hashCode % (uint)buckets.Length];
		}
	}

	private struct Entry
	{
		public uint HashCode;
		public int Next; // 1-based index of next entry in chain: 0 means end of chain
		public ReadOnlyMemory<TKey> Key;
		public TValue Value;
	}
}
