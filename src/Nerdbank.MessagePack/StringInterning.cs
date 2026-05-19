// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Nerdbank.MessagePack.SecureHash;

namespace Nerdbank.MessagePack;

/// <summary>
/// A strong ref string interning collection.
/// </summary>
internal class StringInterning : IPoolableObject
{
	private const int InitialCapacity = 32;

	// The actual stack space taken will be up to 2X this value, because we're converting UTF-8 to UTF-16.
	private const int MaxStackStringByteLength = 4096;

	private const int StartOfFreeList = -3;

	private int[]? buckets;
	private Entry[]? entries;
	private int count;
	private int freeList;
	private int freeCount;

	/// <inheritdoc/>
	MessagePackSerializer? IPoolableObject.Owner { get; set; }

	/// <inheritdoc/>
	void IPoolableObject.Recycle() => this.Clear();

	/// <summary>
	/// Clears the cache of any strong references to interned strings.
	/// </summary>
	internal void Clear()
	{
		int count = this.count;
		if (count > 0)
		{
			Array.Clear(this.buckets!, 0, this.buckets!.Length);
			Array.Clear(this.entries!, 0, count);
			this.count = 0;
			this.freeList = -1;
			this.freeCount = 0;
		}
	}

	/// <summary>
	/// Returns an interned string for the given string.
	/// </summary>
	/// <param name="value">The string to be interned.</param>
	/// <returns>A reference to an equivalent, interned string. This will be <paramref name="value"/> itself if the string was not previously interned.</returns>
	internal string Intern(string value) => this.GetOrAdd(value.AsSpan(), value);

	/// <summary>
	/// Returns an interned string for a given character span.
	/// </summary>
	/// <param name="value">The characters for which an interned string is required.</param>
	/// <returns>The interned string.</returns>
	internal string Intern(ReadOnlySpan<char> value) => this.GetOrAdd(value, candidateValue: null);

	/// <summary>
	/// Returns an interned string for a given UTF-8 encoded byte span.
	/// </summary>
	/// <param name="value">The UTF-8 encoded bytes for which an interned string is required.</param>
	/// <returns>The interned string.</returns>
	internal string GetOrAddUtf8(ReadOnlySpan<byte> value)
	{
		if (value.IsEmpty)
		{
			return string.Empty;
		}

		char[]? charArray = value.Length > MaxStackStringByteLength ? ArrayPool<char>.Shared.Rent(value.Length) : null;
		try
		{
			Span<char> chars = charArray ?? stackalloc char[value.Length];
			int characterCount = StringEncoding.UTF8.GetChars(value, chars);
			return this.Intern(chars[..characterCount]);
		}
		finally
		{
			if (charArray is not null)
			{
				ArrayPool<char>.Shared.Return(charArray);
			}
		}
	}

	/// <summary>
	/// Returns an interned string for a given UTF-8 encoded byte sequence.
	/// </summary>
	/// <param name="value">The UTF-8 encoded bytes for which an interned string is required.</param>
	/// <returns>The interned string.</returns>
	internal string GetOrAddUtf8(ReadOnlySequence<byte> value)
	{
		if (value.IsEmpty)
		{
			return string.Empty;
		}

		if (value.IsSingleSegment)
		{
			return this.GetOrAddUtf8(value.First.Span);
		}

		int byteLength = checked((int)value.Length);
		char[]? charArray = byteLength > MaxStackStringByteLength ? ArrayPool<char>.Shared.Rent(byteLength) : null;
		try
		{
			Span<char> chars = charArray ?? stackalloc char[byteLength];
			int characterCount = StringEncoding.UTF8.GetChars(value, chars);
			return this.Intern(chars[..characterCount]);
		}
		finally
		{
			if (charArray is not null)
			{
				ArrayPool<char>.Shared.Return(charArray);
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static uint CalculateHashCode(ReadOnlySpan<char> value)
	{
#if NET
		return unchecked((uint)string.GetHashCode(value, StringComparison.Ordinal));
#else
		return unchecked((uint)SipHash.Default.Compute(MemoryMarshal.AsBytes(value)));
#endif
	}

	private string GetOrAdd(ReadOnlySpan<char> value, string? candidateValue)
	{
		unchecked
		{
			if (this.buckets is null)
			{
				this.Initialize(InitialCapacity);
			}

			Entry[] entries = this.entries!;
			uint hashCode = CalculateHashCode(value);
			ref int bucket = ref this.GetBucket(hashCode);
			uint collisionCount = 0;

			for (int probeIndex = bucket - 1; (uint)probeIndex < (uint)entries.Length;)
			{
				ref Entry entry = ref entries[probeIndex];
				if (entry.HashCode == hashCode && entry.Value.AsSpan().SequenceEqual(value))
				{
					return entry.Value;
				}

				probeIndex = entry.Next;

				collisionCount++;
				if (collisionCount > (uint)entries.Length)
				{
					throw new InvalidOperationException();
				}
			}

			int index;
			if (this.freeCount > 0)
			{
				index = this.freeList;
				this.freeList = StartOfFreeList - entries[this.freeList].Next;
				--this.freeCount;
			}
			else
			{
				int count = this.count;
				if (count == entries.Length)
				{
					this.Resize(StringHashHelpers.ExpandPrime(count));
					entries = this.entries!;
					bucket = ref this.GetBucket(hashCode);
				}

				index = count;
				this.count = count + 1;
			}

			string interned = candidateValue ?? value.ToString();
			ref Entry newEntry = ref entries[index];
			newEntry.HashCode = hashCode;
			newEntry.Next = bucket - 1;
			newEntry.Value = interned;
			bucket = index + 1;
			return interned;
		}
	}

	private int Initialize(int capacity)
	{
		int size = StringHashHelpers.GetPrimeGreaterThan(capacity);
		int[] buckets = new int[size];
		Entry[] entries = new Entry[size];

		this.freeList = -1;
		this.buckets = buckets;
		this.entries = entries;

		return size;
	}

	private void Resize(int newSize)
	{
		unchecked
		{
			if (newSize <= this.count)
			{
				throw new OverflowException();
			}

			Entry[] newEntries = new Entry[newSize];
			Array.Copy(this.entries!, 0, newEntries, 0, this.count);

			int[] newBuckets = new int[newSize];
			for (int i = 0; i < this.count; i++)
			{
				ref Entry entry = ref newEntries[i];
				if (entry.Next >= -1)
				{
					uint bucket = (uint)(entry.HashCode % newSize);
					entry.Next = newBuckets[bucket] - 1;
					newBuckets[bucket] = i + 1;
				}
			}

			this.entries = newEntries;
			this.buckets = newBuckets;
		}
	}

	private ref int GetBucket(uint hashCode)
	{
		unchecked
		{
			int[] buckets = this.buckets!;
			return ref buckets[(uint)(hashCode % buckets.Length)];
		}
	}

	private struct Entry
	{
		internal uint HashCode;
		internal int Next;
		internal string Value;
	}

	private static class StringHashHelpers
	{
		private const int MaxPrimeArrayLength = 0x7FFFFFC3;

		private const int HashPrime = 101;

		private static readonly int[] Primes = new int[]
		{
			3, 7, 11, 17, 23, 29, 37, 47, 59, 71,
			89, 107, 131, 163, 197, 239, 293, 353, 431, 521,
			631, 761, 919, 1103, 1327, 1597, 1931, 2333, 2801, 3371,
			4049, 4861, 5839, 7013, 8419, 10103, 12143, 14591, 17519, 21023,
			25229, 30293, 36353, 43627, 52361, 62851, 75431, 90523, 108631, 130363,
			156437, 187751, 225307, 270371, 324449, 389357, 467237, 560689, 672827, 807403,
			968897, 1162687, 1395263, 1674319, 2009191, 2411033, 2893249, 3471899, 4166287, 4999559,
			5999471, 7199369,
		};

		internal static int ExpandPrime(int oldSize)
		{
			unchecked
			{
				int num = 2 * oldSize;

				if ((uint)num > MaxPrimeArrayLength && oldSize < MaxPrimeArrayLength)
				{
					return MaxPrimeArrayLength;
				}

				return GetPrimeGreaterThan(num);
			}
		}

		internal static int GetPrimeGreaterThan(int min)
		{
			unchecked
			{
				if (min < 0)
				{
					throw new OverflowException();
				}

				foreach (int prime in Primes)
				{
					if (prime >= min)
					{
						return prime;
					}
				}

				for (int i = min | 1; i < int.MaxValue; i += 2)
				{
					if (IsPrime(i) && ((i - 1) % HashPrime != 0))
					{
						return i;
					}
				}

				return min;

				static bool IsPrime(int candidate)
				{
					unchecked
					{
						if ((candidate & 1) != 0)
						{
							int limit = (int)Math.Sqrt(candidate);
							for (int divisor = 3; divisor <= limit; divisor += 2)
							{
								if ((candidate % divisor) == 0)
								{
									return false;
								}
							}

							return true;
						}

						return candidate == 2;
					}
				}
			}
		}
	}
}
