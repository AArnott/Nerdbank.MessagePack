// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Nerdbank.MessagePack;

/// <summary>
/// Dictionary class that can accept multiple string-like keys.
/// </summary>
/// <typeparam name="TValue">The type of the values that are stored in the dictionary.</typeparam>
internal class StringlyKeyedDictionary<TValue> : IDictionary<string, TValue>, IReadOnlyDictionary<string, TValue>
{
	private const int StartOfFreeList = -3;

	private readonly StringComparison stringComparison;
	private int[]? buckets;
	private Entry[]? entries;
	private int count;
	private int freeList;
	private int freeCount;
	private KeyCollection? keys;
	private ValueCollection? values;

	/// <summary>
	/// Initializes a new instance of the <see cref="StringlyKeyedDictionary{TValue}"/> class.
	/// </summary>
	public StringlyKeyedDictionary()
		: this(isCaseSensitive: true)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="StringlyKeyedDictionary{TValue}"/> class.
	/// </summary>
	/// <param name="capacity">The initial capacity for the collection.</param>
	public StringlyKeyedDictionary(int capacity)
		: this(capacity, isCaseSensitive: true)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="StringlyKeyedDictionary{TValue}"/> class.
	/// </summary>
	/// <param name="capacity">The initial capacity for the collection.</param>
	/// <param name="isCaseSensitive">If the dictionary keys should be case sensitive.</param>
	public StringlyKeyedDictionary(int capacity, bool isCaseSensitive)
	{
		if (capacity < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(capacity));
		}

		if (capacity > 0)
		{
			this.Initialize(capacity);
		}

		this.stringComparison = isCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="StringlyKeyedDictionary{TValue}"/> class.
	/// </summary>
	/// <param name="isCaseSensitive">If the dictionary keys should be case sensitive.</param>
	public StringlyKeyedDictionary(bool isCaseSensitive)
	{
		this.stringComparison = isCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="StringlyKeyedDictionary{TValue}"/> class.
	/// </summary>
	/// <param name="isCaseSensitive">If the dictionary keys should be case sensitive.</param>
	/// <param name="initialSize">The initial size for the collection.</param>
	public StringlyKeyedDictionary(bool isCaseSensitive, int initialSize)
	{
		int primeInitialSize = StringlyHashHelpers.GetPrimeGreaterThan(initialSize);
		this.buckets = new int[primeInitialSize];
		this.entries = new Entry[primeInitialSize];

		this.stringComparison = isCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
		this.freeList = -1;
	}

	/// <summary>
	/// Gets a struct based enumerator for enumerating the dictionary keys.
	/// </summary>
	public KeyCollection Keys => this.keys ??= new KeyCollection(this);

	/// <summary>
	/// Gets a struct based enumerator for enumerating the dictionary values.
	/// </summary>
	public ValueCollection Values => this.values ??= new ValueCollection(this);

	/// <inheritdoc/>
	ICollection<string> IDictionary<string, TValue>.Keys => this.Keys;

	/// <inheritdoc/>
	ICollection<TValue> IDictionary<string, TValue>.Values => this.Values;

	/// <inheritdoc/>
	public int Count => unchecked(this.count - this.freeCount);

	/// <summary>
	/// Gets the total numbers of elements the internal data structure can hold without resizing.
	/// </summary>
	public int Capacity => this.entries?.Length ?? 0;

	/// <inheritdoc/>
	bool ICollection<KeyValuePair<string, TValue>>.IsReadOnly => false;

	/// <inheritdoc/>
	IEnumerable<string> IReadOnlyDictionary<string, TValue>.Keys => this.Keys;

	/// <inheritdoc/>
	IEnumerable<TValue> IReadOnlyDictionary<string, TValue>.Values => this.Values;

	/// <inheritdoc/>
	public TValue this[string key]
	{
		get
		{
			if (this.TryGetValue(key.AsSpan(), out TValue? value))
			{
				return value;
			}

			throw new KeyNotFoundException();
		}
		set => this.AddInternal(key.AsSpan(), value, overwriteDuplicate: true);
	}

	/// <summary>
	/// Gets or sets the element with the specified key.
	/// </summary>
	/// <param name="key">The given key.</param>
	/// <returns>The element with the specified key.</returns>
	/// <exception cref="KeyNotFoundException">Throws if the key isn't found.</exception>
	public TValue this[ReadOnlySpan<char> key]
	{
		get
		{
			ref TValue value = ref this.FindValue(key);
			if (!Unsafe.IsNullRef(ref value))
			{
				return value;
			}

			throw new KeyNotFoundException(nameof(key));
		}
		set => this.AddInternal(key, value, overwriteDuplicate: true);
	}

	/// <inheritdoc/>
	public void Add(string key, TValue value)
	{
		this.AddInternal(key.AsSpan(), value, overwriteDuplicate: false);
	}

	/// <summary>
	/// Adds an element with the provided key and value to the <see cref="StringlyKeyedDictionary{TValue}"/>.
	/// </summary>
	/// <param name="key">The object to use as the key of the element to add.</param>
	/// <param name="value">The object to use as the value of the element to add.</param>
	public void Add(ReadOnlySpan<char> key, TValue value)
	{
		this.AddInternal(key, value, overwriteDuplicate: false);
	}

	/// <inheritdoc/>
	public bool ContainsKey(string key) => !Unsafe.IsNullRef(ref this.FindValue(key.AsSpan()));

	/// <summary>
	/// Determines whether the <see cref="StringlyKeyedDictionary{TValue}"/> contains an element with the specified key.
	/// </summary>
	/// <param name="key">The key to locate in the <see cref="StringlyKeyedDictionary{TValue}"/>.</param>
	/// <returns><see langword="true"/> if the <see cref="StringlyKeyedDictionary{TValue}"/> contains an element with the key; otherwise, <see langword="false"/>.</returns>
	public bool ContainsKey(ReadOnlySpan<char> key) => !Unsafe.IsNullRef(ref this.FindValue(key));

	/// <summary>
	/// Determines whether the <see cref="StringlyKeyedDictionary{TValue}"/> contains an element with the specified value.
	/// </summary>
	/// <param name="value">The value to locate in the <see cref="StringlyKeyedDictionary{TValue}"/>.</param>
	/// <returns><see langword="true"/> if the <see cref="StringlyKeyedDictionary{TValue}"/> contains an element with the value; otherwise, <see langword="false"/>.</returns>
	public bool ContainsValue(TValue value)
	{
		unchecked
		{
			Entry[]? entries = this.entries;
			if (value is null)
			{
				for (int i = 0; i < this.count; i++)
				{
					if (entries![i].Next >= -1 && entries[i].Value is null)
					{
						return true;
					}
				}
			}
			else if (typeof(TValue).IsValueType)
			{
				// ValueType: Devirtualize with EqualityComparer<TValue>.Default intrinsic
				for (int i = 0; i < this.count; i++)
				{
					if (entries![i].Next >= -1 && EqualityComparer<TValue>.Default.Equals(entries[i].Value, value))
					{
						return true;
					}
				}
			}
			else
			{
				// Object type: Shared Generic, EqualityComparer<TValue>.Default won't devirtualize
				// https://github.com/dotnet/runtime/issues/10050
				// So cache in a local rather than get EqualityComparer per loop iteration
				EqualityComparer<TValue> defaultComparer = EqualityComparer<TValue>.Default;
				for (int i = 0; i < this.count; i++)
				{
					if (entries![i].Next >= -1 && defaultComparer.Equals(entries[i].Value, value))
					{
						return true;
					}
				}
			}

			return false;
		}
	}

	/// <inheritdoc/>
	public bool Remove(string key)
	{
		return this.RemoveInternal(key.AsSpan());
	}

	/// <summary>
	/// Removes the element with the specified key from the <see cref="StringlyKeyedDictionary{TValue}"/>.
	/// </summary>
	/// <param name="key">The key of the element to remove.</param>
	/// <returns><see langword="true"/> if the element is successfully removed; otherwise, false. This method also
	/// returns <see langword="false"/> if key was not found in the original <see cref="StringlyKeyedDictionary{TValue}"/>.</returns>
	public bool Remove(ReadOnlySpan<char> key)
	{
		return this.RemoveInternal(key);
	}

	/// <inheritdoc/>
	bool IDictionary<string, TValue>.TryGetValue(string key, out TValue value)
	{
		return this.TryGetValue(key, out value!);
	}

	/// <inheritdoc/>
	bool IReadOnlyDictionary<string, TValue>.TryGetValue(string key, out TValue value)
	{
		return this.TryGetValue(key, out value!);
	}

	/// <summary>
	/// Gets the value that is associated with the specified key.
	/// </summary>
	/// <param name="key">The key to locate.</param>
	/// <param name="value">
	/// When this method returns, the value associated with the specified key, if the
	/// key is found; otherwise, the default value for the type of the value parameter.
	/// This parameter is passed uninitialized.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if the <see cref="StringlyKeyedDictionary{TValue}" /> contains
	/// an element that has the specified key; otherwise, <see langword="false"/>.
	/// </returns>
	public bool TryGetValue(string key, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out TValue value)
	{
		ref TValue valueRef = ref this.FindValue(key.AsSpan());
		if (!Unsafe.IsNullRef(ref valueRef))
		{
			value = valueRef;
			return true;
		}

		value = default;
		return false;
	}

	/// <summary>
	/// Gets the value associated with the specified key.
	/// </summary>
	/// <param name="key">The key whose value to get.</param>
	/// <param name="value">When this method returns, the value associated with the specified key, if the key is found; otherwise, the default value for the type of the value parameter.
	/// This parameter is passed uninitialized.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if <see cref="StringlyKeyedDictionary{TValue}"/> contains the an element with the specified key; otherwise, false.
	/// </returns>
	public bool TryGetValue(ReadOnlySpan<char> key, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out TValue value)
	{
		ref TValue valueRef = ref this.FindValue(key);
		if (!Unsafe.IsNullRef(ref valueRef))
		{
			value = valueRef;
			return true;
		}

		value = default;
		return false;
	}

	/// <inheritdoc/>
	public void Add(KeyValuePair<string, TValue> item)
	{
		this.AddInternal(item.Key.AsSpan(), item.Value, overwriteDuplicate: false);
	}

	/// <inheritdoc/>
	public void Clear()
	{
		int count = this.count;
		if (count > 0)
		{
			Array.Clear(this.buckets!, 0, this.buckets!.Length);
			this.count = 0;
			this.freeList = -1;
			this.freeCount = 0;
			Array.Clear(this.entries!, 0, count);
		}
	}

	/// <inheritdoc/>
	public bool Contains(KeyValuePair<string, TValue> item)
	{
		ref TValue value = ref this.FindValue(item.Key.AsSpan());
		if (!Unsafe.IsNullRef(ref value) && EqualityComparer<TValue>.Default.Equals(value, item.Value))
		{
			return true;
		}

		return false;
	}

	/// <inheritdoc/>
	public void CopyTo(KeyValuePair<string, TValue>[] array, int arrayIndex)
	{
		unchecked
		{
			if (array is null)
			{
				throw new ArgumentNullException(nameof(array));
			}

			if ((uint)arrayIndex > (uint)array.Length)
			{
				throw new ArgumentOutOfRangeException(nameof(arrayIndex));
			}

			if (array.Length - arrayIndex < this.Count)
			{
				throw new ArgumentException(nameof(array));
			}

			int count = this.count;
			Entry[] entries = this.entries!;
			for (int i = 0; i < count; i++)
			{
				ref Entry entry = ref entries[i];
				if (entry.Next >= -1)
				{
					array[arrayIndex++] = new KeyValuePair<string, TValue>(entry.Key, entry.Value);
				}
			}
		}
	}

	/// <summary>
	/// Ensures that the dictionary can hold up to a specified number of entries without any further expansion of its backing storage.
	/// </summary>
	/// <param name="capacity">The number of entries.</param>
	/// <returns>The current capacity of <see cref="StringlyKeyedDictionary{TValue}"/>.</returns>
	public int EnsureCapacity(int capacity)
	{
		if (capacity < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(capacity));
		}

		int currentCapacity = this.entries is null ? 0 : this.entries.Length;
		if (currentCapacity >= capacity)
		{
			return currentCapacity;
		}

		if (this.buckets is null)
		{
			return this.Initialize(capacity);
		}

		int newSize = StringlyHashHelpers.GetPrimeGreaterThan(capacity);
		this.Resize(newSize);
		return newSize;
	}

	/// <inheritdoc/>
	public bool Remove(KeyValuePair<string, TValue> item)
	{
		ref TValue value = ref this.FindValue(item.Key.AsSpan());
		if (!Unsafe.IsNullRef(ref value) && EqualityComparer<TValue>.Default.Equals(value, item.Value))
		{
			this.RemoveInternal(item.Key.AsSpan());
			return true;
		}

		return false;
	}

	/// <summary>
	/// Returns an enumerator that iterates through the collection.
	/// </summary>
	/// <returns>An enumerator that can be used to iterate through the collection.</returns>
	public Enumerator GetEnumerator() => new(this);

	/// <inheritdoc/>
	IEnumerator<KeyValuePair<string, TValue>> IEnumerable<KeyValuePair<string, TValue>>.GetEnumerator()
	{
		return this.GetEnumerator();
	}

	/// <inheritdoc/>
	IEnumerator IEnumerable.GetEnumerator()
	{
		return this.GetEnumerator();
	}

	/// <summary>
	/// Calculate a case insensitive hash code.
	/// </summary>
	/// <param name="value">The input span.</param>
	/// <returns>The hash code.</returns>
	internal static int GetHashCodeOrdinalIgnoreCase(ReadOnlySpan<char> value)
	{
#if NET
		return string.GetHashCode(value, StringComparison.OrdinalIgnoreCase);
#else
		return Marvin.GetHashCodeOrdinalIgnoreCase(value);
#endif
	}

	/// <summary>
	/// Calculate a case insensitive hash code.
	/// </summary>
	/// <param name="value">The input span.</param>
	/// <returns>The hash code.</returns>
	internal static int GetHashCodeOrdinal(ReadOnlySpan<char> value)
	{
#if NET
		return string.GetHashCode(value, StringComparison.Ordinal);
#else
		return Marvin.ComputeHash32(value);
#endif
	}

	private int Initialize(int capacity)
	{
		int size = StringlyHashHelpers.GetPrimeGreaterThan(capacity);
		int[] buckets = new int[size];
		Entry[] entries = new Entry[size];

		// Assign member variables after both arrays allocated to guard against corruption from OOM if second fails
		this.freeList = -1;

		this.buckets = buckets;
		this.entries = entries;

		return size;
	}

	private ref TValue FindValue(ReadOnlySpan<char> key)
	{
		unchecked
		{
			if (this.buckets is null || this.buckets.Length == 0)
			{
				return ref Unsafe.NullRef<TValue>();
			}

			Entry[] entries = this.entries!;

			uint hashCode = this.CalculateHashCode(key);
			ref int bucket = ref this.GetBucket(hashCode);
			uint collisionCount = 0;

			// Search for an existing entry.
			for (int probeIndex = bucket - 1; probeIndex >= 0;)
			{
				ref Entry entry = ref entries[probeIndex];

				if (entry.HashCode == hashCode && entry.Key.AsSpan().Equals(key, this.stringComparison))
				{
					return ref entry.Value;
				}

				// Follow the chain to find the next item.
				probeIndex = entry.Next;

				collisionCount++;
				if (collisionCount > entries.Length)
				{
					throw new InvalidOperationException();
				}
			}

			return ref Unsafe.NullRef<TValue>();
		}
	}

	private void AddInternal(ReadOnlySpan<char> key, TValue value, bool overwriteDuplicate)
	{
		unchecked
		{
			if (this.buckets is null)
			{
				this.Initialize(0);
			}

			Entry[] entries = this.entries!;

			uint hashCode = this.CalculateHashCode(key);
			ref int bucket = ref this.GetBucket(hashCode);
			int probeIndex = bucket - 1;
			uint collisionCount = 0;

			// Search for an existing entry.
			while ((uint)probeIndex < (uint)entries.Length)
			{
				ref Entry currentEntry = ref entries[probeIndex];

				if (currentEntry.HashCode == hashCode && currentEntry.Key.AsSpan().Equals(key, this.stringComparison))
				{
					if (overwriteDuplicate)
					{
						currentEntry.Value = value;
						return;
					}
					else
					{
						throw new ArgumentException();
					}
				}

				// Follow the chain to find the next item.
				probeIndex = currentEntry.Next;

				collisionCount++;
				if (collisionCount > (uint)entries.Length)
				{
					// The chain of entries forms a loop; which means a concurrent update has happened.
					// Break out of the loop and throw, rather than looping forever.
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
				// We will add a new entry.
				// Resize our storage if needed.
				int count = this.count;
				if (count == entries.Length)
				{
					this.Resize(StringlyHashHelpers.ExpandPrime(this.count));
					bucket = ref this.GetBucket(hashCode);
				}

				index = count;
				this.count = count + 1;
				entries = this.entries!;
			}

			// Materialize a string for the span.
			string str = key.ToString();

			// Store the value.
			ref Entry entry = ref entries[index];
			entry.HashCode = hashCode;
			entry.Next = bucket - 1;
			entry.Key = str;
			entry.Value = value;
			bucket = index + 1;
		}
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

			if (this.entries is not null)
			{
				Array.Copy(this.entries, 0, newEntries, 0, this.count);
			}

			int[] newBuckets = new int[newSize];

			for (int i = 0; i < this.count; i++)
			{
				uint num = (uint)(newEntries[i].HashCode % newSize);
				newEntries[i].Next = newBuckets[num] - 1;
				newBuckets[num] = i + 1;
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

	private bool RemoveInternal(ReadOnlySpan<char> key)
	{
		unchecked
		{
			if (this.buckets is null || this.buckets.Length == 0)
			{
				return false;
			}

			uint collisionCount = 0;
			uint hashCode = this.CalculateHashCode(key);

			// Search for an existing entry.
			int last = -1;
			ref int bucket = ref this.GetBucket(hashCode);
			Entry[] entries = this.entries!;
			for (int probeIndex = bucket - 1; probeIndex >= 0;)
			{
				ref Entry entry = ref entries[probeIndex];

				if (entry.HashCode == hashCode && entry.Key.AsSpan().Equals(key, this.stringComparison))
				{
					if (last < 0)
					{
						bucket = entry.Next + 1;
					}
					else
					{
						entries[last].Next = entry.Next;
					}

					entry.Next = StartOfFreeList - this.freeList;
					entry.Key = default!;
					entry.Value = default!;

					this.freeList = probeIndex;
					this.freeCount++;
					return true;
				}

				last = probeIndex;

				// Follow the chain to find the next item.
				probeIndex = entry.Next;

				collisionCount++;
				if (collisionCount > (uint)entries.Length)
				{
					// The chain of entries forms a loop; which means a concurrent update has happened.
					// Break out of the loop and throw, rather than looping forever.
					throw new InvalidOperationException();
				}
			}

			return false;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private uint CalculateHashCode(ReadOnlySpan<char> key)
	{
		return unchecked((uint)(this.stringComparison == StringComparison.Ordinal ? GetHashCodeOrdinal(key) : GetHashCodeOrdinalIgnoreCase(key)));
	}

	/// <summary>
	/// Struct enumerator for <see cref="StringlyKeyedDictionary{TValue}"/>.
	/// </summary>
	internal struct Enumerator : IEnumerator<KeyValuePair<string, TValue>>
	{
		private readonly StringlyKeyedDictionary<TValue> dictionary;
		private int index;
		private KeyValuePair<string, TValue> current;

		/// <summary>
		/// Initializes a new instance of the <see cref="Enumerator"/> struct.
		/// </summary>
		/// <param name="dictionary">The <see cref="StringlyKeyedDictionary{TValue}"/> to enumerate.</param>
		internal Enumerator(StringlyKeyedDictionary<TValue> dictionary)
		{
			this.dictionary = dictionary;
			this.index = 0;
			this.current = default;
		}

		/// <inheritdoc/>
		public readonly KeyValuePair<string, TValue> Current => this.current;

		/// <inheritdoc/>
		readonly object IEnumerator.Current => this.Current;

		/// <inheritdoc/>
		public readonly void Dispose()
		{
		}

		/// <inheritdoc/>
		public bool MoveNext()
		{
			unchecked
			{
				// Use unsigned comparison since we set index to dictionary.count+1 when the enumeration ends.
				// dictionary.count+1 could be negative if dictionary.count is int.MaxValue
				while ((uint)this.index < (uint)this.dictionary.count)
				{
					ref Entry entry = ref this.dictionary.entries![this.index++];

					if (entry.Next >= -1)
					{
						this.current = new KeyValuePair<string, TValue>(entry.Key, entry.Value);
						return true;
					}
				}

				this.index = this.dictionary.count + 1;
				this.current = default;
				return false;
			}
		}

		/// <inheritdoc/>
		public void Reset()
		{
			this.index = 0;
			this.current = default;
		}
	}

	/// <summary>Models an entry in our hash table.</summary>
	private struct Entry
	{
		public uint HashCode;
		public int Next;
		public string Key;
		public TValue Value;
	}

	/// <summary>
	/// A collection of <see cref="StringlyKeyedDictionary{TValue}"/> keys.
	/// </summary>
	internal class KeyCollection : ICollection<string>
	{
		private readonly StringlyKeyedDictionary<TValue> dictionary;

		/// <summary>
		/// Initializes a new instance of the <see cref="KeyCollection"/> class.
		/// </summary>
		/// <param name="dictionary">The <see cref="StringlyKeyedDictionary{TValue}"/> to create the collection from.</param>
		/// <exception cref="ArgumentNullException">Throws if <paramref name="dictionary"/> is <see langword="null" />.</exception>
		public KeyCollection(StringlyKeyedDictionary<TValue> dictionary)
		{
			if (dictionary is null)
			{
				throw new ArgumentNullException(nameof(dictionary));
			}

			this.dictionary = dictionary;
		}

		/// <inheritdoc/>
		public int Count => this.dictionary.Count;

		/// <inheritdoc/>
		public bool IsReadOnly => true;

		/// <inheritdoc/>
		public void Add(string item)
		{
			throw new NotSupportedException();
		}

		/// <inheritdoc/>
		public void Clear()
		{
			throw new NotSupportedException();
		}

		/// <inheritdoc/>
		public bool Contains(string item)
		{
			return this.dictionary.ContainsKey(item);
		}

		/// <inheritdoc/>
		public void CopyTo(string[] array, int arrayIndex)
		{
			unchecked
			{
				if (array is null)
				{
					throw new ArgumentNullException(nameof(array));
				}

				if (arrayIndex < 0 || arrayIndex > array.Length)
				{
					throw new ArgumentOutOfRangeException(nameof(arrayIndex));
				}

				if (array.Length - arrayIndex < this.dictionary.Count)
				{
					throw new ArgumentException(nameof(array));
				}

				int count = this.dictionary.count;
				Entry[]? entries = this.dictionary.entries;
				for (int i = 0; i < count; i++)
				{
					if (entries![i].Next >= -1)
					{
						array[arrayIndex++] = entries[i].Key;
					}
				}
			}
		}

		/// <inheritdoc/>
		public bool Remove(string item)
		{
			throw new NotSupportedException();
		}

		/// <inheritdoc/>
		IEnumerator<string> IEnumerable<string>.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		/// <inheritdoc/>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<string>)this).GetEnumerator();
		}

		/// <summary>
		/// Returns an enumerator that iterates through the collection.
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection.</returns>
		public Enumerator GetEnumerator() => new Enumerator(this.dictionary);

		/// <summary>
		/// Struct enumerator for <see cref="KeyCollection"/>.
		/// </summary>
		internal struct Enumerator : IEnumerator<string>
		{
			private readonly StringlyKeyedDictionary<TValue> dictionary;
			private int index;
			private string currentKey;

			/// <summary>
			/// Initializes a new instance of the <see cref="Enumerator"/> struct.
			/// </summary>
			/// <param name="dictionary">The <see cref="StringlyKeyedDictionary{TValue}"/> to enumerate.</param>
			internal Enumerator(StringlyKeyedDictionary<TValue> dictionary)
			{
				this.dictionary = dictionary;
				this.index = 0;
				this.currentKey = default!;
			}

			/// <inheritdoc/>
			public readonly string Current => this.currentKey;

			/// <inheritdoc/>
			readonly object IEnumerator.Current => this.Current;

			/// <inheritdoc/>
			public readonly void Dispose()
			{
			}

			/// <inheritdoc/>
			public bool MoveNext()
			{
				unchecked
				{
					while ((uint)this.index < (uint)this.dictionary.count)
					{
						ref Entry entry = ref this.dictionary.entries![this.index++];

						if (entry.Next >= -1)
						{
							this.currentKey = entry.Key;
							return true;
						}
					}

					this.index = this.dictionary.count + 1;
					this.currentKey = default!;
					return false;
				}
			}

			/// <inheritdoc/>
			public void Reset()
			{
				this.index = 0;
				this.currentKey = default!;
			}
		}
	}

	/// <summary>
	/// A collection of <see cref="StringlyKeyedDictionary{TValue}"/> keys.
	/// </summary>
	internal class ValueCollection : ICollection<TValue>
	{
		private readonly StringlyKeyedDictionary<TValue> dictionary;

		/// <summary>
		/// Initializes a new instance of the <see cref="ValueCollection"/> class.
		/// </summary>
		/// <param name="dictionary">The <see cref="StringlyKeyedDictionary{TValue}"/> to create the collection from.</param>
		/// <exception cref="ArgumentNullException">Throws if <paramref name="dictionary"/> is <see langword="null" />.</exception>
		public ValueCollection(StringlyKeyedDictionary<TValue> dictionary)
		{
			if (dictionary is null)
			{
				throw new ArgumentNullException(nameof(dictionary));
			}

			this.dictionary = dictionary;
		}

		/// <inheritdoc/>
		public int Count => this.dictionary.Count;

		/// <inheritdoc/>
		public bool IsReadOnly => true;

		/// <inheritdoc/>
		public void Add(TValue item)
		{
			throw new NotSupportedException();
		}

		/// <inheritdoc/>
		public void Clear()
		{
			throw new NotSupportedException();
		}

		/// <inheritdoc/>
		public bool Contains(TValue item)
		{
			return this.dictionary.ContainsValue(item);
		}

		/// <inheritdoc/>
		public void CopyTo(TValue[] array, int arrayIndex)
		{
			unchecked
			{
				if (array is null)
				{
					throw new ArgumentNullException(nameof(array));
				}

				if (arrayIndex < 0 || arrayIndex > array.Length)
				{
					throw new ArgumentOutOfRangeException(nameof(arrayIndex));
				}

				if (array.Length - arrayIndex < this.dictionary.Count)
				{
					throw new ArgumentException(nameof(array));
				}

				int count = this.dictionary.count;
				Entry[]? entries = this.dictionary.entries;
				for (int i = 0; i < count; i++)
				{
					if (entries![i].Next >= -1)
					{
						array[arrayIndex++] = entries[i].Value;
					}
				}
			}
		}

		/// <inheritdoc/>
		public bool Remove(TValue item)
		{
			throw new NotSupportedException();
		}

		/// <inheritdoc/>
		IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		/// <inheritdoc/>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<TValue>)this).GetEnumerator();
		}

		/// <summary>
		/// Returns an enumerator that iterates through the collection.
		/// </summary>
		/// <returns>An enumerator that can be used to iterate through the collection.</returns>
		public Enumerator GetEnumerator() => new Enumerator(this.dictionary);

		/// <summary>
		/// Struct enumerator for <see cref="KeyCollection"/>.
		/// </summary>
		internal struct Enumerator : IEnumerator<TValue>
		{
			private readonly StringlyKeyedDictionary<TValue> dictionary;
			private int index;
			private TValue currentValue;

			/// <summary>
			/// Initializes a new instance of the <see cref="Enumerator"/> struct.
			/// </summary>
			/// <param name="dictionary">The <see cref="StringlyKeyedDictionary{TValue}"/> to enumerate.</param>
			internal Enumerator(StringlyKeyedDictionary<TValue> dictionary)
			{
				this.dictionary = dictionary;
				this.index = 0;
				this.currentValue = default!;
			}

			/// <inheritdoc/>
			public readonly TValue Current => this.currentValue;

			/// <inheritdoc/>
			readonly object? IEnumerator.Current => this.currentValue;

			/// <inheritdoc/>
			public readonly void Dispose()
			{
			}

			/// <inheritdoc/>
			public bool MoveNext()
			{
				unchecked
				{
					while ((uint)this.index < (uint)this.dictionary.count)
					{
						ref Entry entry = ref this.dictionary.entries![this.index++];

						if (entry.Next >= -1)
						{
							this.currentValue = entry.Value;
							return true;
						}
					}

					this.index = this.dictionary.count + 1;
					this.currentValue = default!;
					return false;
				}
			}

			/// <inheritdoc/>
			public void Reset()
			{
				this.index = 0;
				this.currentValue = default!;
			}
		}
	}

	/// <summary>
	/// Useful helpers for hash based collections.
	/// </summary>
	private static class StringlyHashHelpers
	{
		// This is the maximum prime smaller than Array.MaxLength.
		private const int MaxPrimeArrayLength = 0x7FFFFFC3;

		private const int HashPrime = 101;

		/// <summary>
		/// Pre-computed list of primes to use for sizing.
		/// </summary>
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

		/// <summary>
		/// Finds the next prime that is at least double <paramref name="oldSize"/>.
		/// </summary>
		/// <param name="oldSize">The provided size.</param>
		/// <returns>The next prime that is at least double <paramref name="oldSize"/>.</returns>
		public static int ExpandPrime(int oldSize)
		{
			unchecked
			{
				// Start by doubling the size.
				int num = 2 * oldSize;

				if ((uint)num > MaxPrimeArrayLength && oldSize < MaxPrimeArrayLength)
				{
					// If we overflowed int32, cap it.
					return MaxPrimeArrayLength;
				}

				return GetPrimeGreaterThan(num);
			}
		}

		/// <summary>
		/// Gets the next prime size greater than <paramref name="min"/>.
		/// </summary>
		/// <param name="min">The provided minimum size.</param>
		/// <returns>The first prime size greater than <paramref name="min"/>.</returns>
		public static int GetPrimeGreaterThan(int min)
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

				// Outside of our predefined table. Compute the hard way.
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

	/// <summary>
	/// Helper class to calculate hash codes.
	/// </summary>
	private static class Marvin
	{
		private static ulong DefaultSeed { get; } = 42; // GenerateSeed();

		/// <summary>
		/// Gets the hash code for the provided <see cref="ReadOnlySpan{T}"/>.
		/// </summary>
		/// <param name="value">The item to calculate the hash code for.</param>
		/// <returns>The hash code.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static int ComputeHash32(ReadOnlySpan<char> value) => ComputeHash32(value, DefaultSeed);

		/// <summary>
		/// Gets the hash code for the provided <see cref="ReadOnlySpan{T}"/>.
		/// </summary>
		/// <param name="value">The item to calculate the hash code for.</param>
		/// <returns>The hash code.</returns>
		internal static int GetHashCodeOrdinalIgnoreCase(ReadOnlySpan<char> value)
		{
			return ComputeHash32OrdinalIgnoreCase(value);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int ComputeHash32(ReadOnlySpan<char> value, ulong seed) => unchecked(ComputeHash32(ref Unsafe.As<char, byte>(ref MemoryMarshal.GetReference(value)), (uint)value.Length * 2 /* in bytes, not chars */, (uint)seed, (uint)(seed >> 32)));

		/// <summary>
		/// Compute a Marvin OrdinalIgnoreCase hash and collapse it into a 32-bit hash.
		/// </summary>
		private static int ComputeHash32OrdinalIgnoreCase(ReadOnlySpan<char> value)
		{
			unchecked
			{
				ulong seed = Marvin.DefaultSeed;
				return Marvin.ComputeHash32OrdinalIgnoreCase(value, ref MemoryMarshal.GetReference(value), value.Length /* in chars, not bytes */, (uint)seed, (uint)(seed >> 32));
			}
		}

		private static int ComputeHash32OrdinalIgnoreCase(ReadOnlySpan<char> value, ref char data, int count, uint p0, uint p1)
		{
			unchecked
			{
				uint ucount = (uint)count; // in chars
				nuint byteOffset = 0; // in bytes
				uint tempValue;

				// We operate on 32-bit integers (two chars) at a time.
				while (ucount >= 2)
				{
					tempValue = Unsafe.ReadUnaligned<uint>(ref Unsafe.As<char, byte>(ref Unsafe.AddByteOffset(ref data, byteOffset)));
					if (!Utf16Utility.AllCharsInUInt32AreAscii(tempValue))
					{
						goto NotAscii;
					}

					p0 += Utf16Utility.ConvertAllAsciiCharsInUInt32ToUppercase(tempValue);
					Block(ref p0, ref p1);

					byteOffset += 4;
					ucount -= 2;
				}

				Debug.Assert(ucount < 2, "We have either one char (16 bits) or zero chars left over");

				if (ucount > 0)
				{
					tempValue = Unsafe.AddByteOffset(ref data, byteOffset);
					if (tempValue > 0x7Fu)
					{
						goto NotAscii;
					}

					if (BitConverter.IsLittleEndian)
					{
						// addition is written with -0x80u to allow fall-through to next statement rather than jmp past it
						p0 += Utf16Utility.ConvertAllAsciiCharsInUInt32ToUppercase(tempValue) + (0x800000u - 0x80u);
					}
					else
					{
						// as above, addition is modified to allow fall-through to next statement rather than jmp past it
						p0 += (Utf16Utility.ConvertAllAsciiCharsInUInt32ToUppercase(tempValue) << 16) + 0x8000u - 0x80000000u;
					}
				}

				if (BitConverter.IsLittleEndian)
				{
					p0 += 0x80u;
				}
				else
				{
					p0 += 0x80000000u;
				}

				Block(ref p0, ref p1);
				Block(ref p0, ref p1);

				return (int)(p1 ^ p0);

			NotAscii:
				Debug.Assert(ucount <= int.MaxValue, "this should fit into a signed int.");

				return StringComparer.OrdinalIgnoreCase.GetHashCode(value.ToString());
			}
		}

		/// <summary>
		/// Compute a Marvin hash and collapse it into a 32-bit hash.
		/// </summary>
		private static int ComputeHash32(ref byte data, uint count, uint p0, uint p1)
		{
			unchecked
			{
				// Control flow of this method generally flows top-to-bottom, trying to
				// minimize the number of branches taken for large (>= 8 bytes, 4 chars) inputs.
				// If small inputs (< 8 bytes, 4 chars) are given, this jumps to a "small inputs"
				// handler at the end of the method.
				if (count < 8)
				{
					// We can't run the main loop, but we might still have 4 or more bytes available to us.
					// If so, jump to the 4 .. 7 bytes logic immediately after the main loop.
					if (count >= 4)
					{
						goto Between4And7BytesRemain;
					}
					else
					{
						goto InputTooSmallToEnterMainLoop;
					}
				}

				// Main loop - read 8 bytes at a time.
				// The block function is unrolled 2x in this loop.
				uint loopCount = count / 8;
				Debug.Assert(loopCount > 0, "Shouldn't reach this code path for small inputs.");

				do
				{
					// Most x86 processors have two dispatch ports for reads, so we can read 2x 32-bit
					// values in parallel. We opt for this instead of a single 64-bit read since the
					// typical use case for Marvin32 is computing String hash codes, and the particular
					// layout of String instances means the starting data is never 8-byte aligned when
					// running in a 64-bit process.
					p0 += Unsafe.ReadUnaligned<uint>(ref data);
					uint nextUInt32 = Unsafe.ReadUnaligned<uint>(ref Unsafe.AddByteOffset(ref data, 4));

					// One block round for each of the 32-bit integers we just read, 2x rounds total.
					Block(ref p0, ref p1);
					p0 += nextUInt32;
					Block(ref p0, ref p1);

					// Bump the data reference pointer and decrement the loop count.

					// Decrementing by 1 every time and comparing against zero allows the JIT to produce
					// better codegen compared to a standard 'for' loop with an incrementing counter.
					// Requires https://github.com/dotnet/runtime/issues/6794 to be addressed first
					// before we can realize the full benefits of this.
					data = ref Unsafe.AddByteOffset(ref data, 8);
				}
				while (--loopCount > 0);

				// n.b. We've not been updating the original 'count' parameter, so its actual value is
				// still the original data length. However, we can still rely on its least significant
				// 3 bits to tell us how much data remains (0 .. 7 bytes) after the loop above is
				// completed.
				if ((count & 0b_0100) == 0)
				{
					goto DoFinalPartialRead;
				}

Between4And7BytesRemain:

// If after finishing the main loop we still have 4 or more leftover bytes, or if we had
// 4 .. 7 bytes to begin with and couldn't enter the loop in the first place, we need to
// consume 4 bytes immediately and send them through one round of the block function.
				Debug.Assert(count >= 4, "Only should've gotten here if the original count was >= 4.");

				p0 += Unsafe.ReadUnaligned<uint>(ref data);
				Block(ref p0, ref p1);

DoFinalPartialRead:

// Finally, we have 0 .. 3 bytes leftover. Since we know the original data length was at
// least 4 bytes (smaller lengths are handled at the end of this routine), we can safely
// read the 4 bytes at the end of the buffer without reading past the beginning of the
// original buffer. This necessarily means the data we're about to read will overlap with
// some data we've already processed, but we can handle that below.
				Debug.Assert(count >= 4, "Only should've gotten here if the original count was >= 4.");

				// Read the last 4 bytes of the buffer.
				uint partialResult = Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref Unsafe.AddByteOffset(ref data, (nuint)count & 7), -4));

				// The 'partialResult' local above contains any data we have yet to read, plus some number
				// of bytes which we've already read from the buffer. An example of this is given below
				// for little-endian architectures. In this table, AA BB CC are the bytes which we still
				// need to consume, and ## are bytes which we want to throw away since we've already
				// consumed them as part of a previous read.
				//
				//                                                    (partialResult contains)   (we want it to contain)
				// count mod 4 = 0 -> [ ## ## ## ## |             ] -> 0x####_####             -> 0x0000_0080
				// count mod 4 = 1 -> [ ## ## ## ## | AA          ] -> 0xAA##_####             -> 0x0000_80AA
				// count mod 4 = 2 -> [ ## ## ## ## | AA BB       ] -> 0xBBAA_####             -> 0x0080_BBAA
				// count mod 4 = 3 -> [ ## ## ## ## | AA BB CC    ] -> 0xCCBB_AA##             -> 0x80CC_BBAA
				count = ~count << 3;

				if (BitConverter.IsLittleEndian)
				{
					partialResult >>= 8; // make some room for the 0x80 byte
					partialResult |= 0x8000_0000u; // put the 0x80 byte at the beginning
					partialResult >>= (int)count & 0x1F; // shift out all previously consumed bytes
				}
				else
				{
					partialResult <<= 8; // make some room for the 0x80 byte
					partialResult |= 0x80u; // put the 0x80 byte at the end
					partialResult <<= (int)count & 0x1F; // shift out all previously consumed bytes
				}

DoFinalRoundsAndReturn:

// Now that we've computed the final partial result, merge it in and run two rounds of
// the block function to finish out the Marvin algorithm.
				p0 += partialResult;
				Block(ref p0, ref p1);
				Block(ref p0, ref p1);

				return (int)(p1 ^ p0);

InputTooSmallToEnterMainLoop:

// We had only 0 .. 3 bytes to begin with, so we can't perform any 32-bit reads.
// This means that we're going to be building up the final result right away and
// will only ever run two rounds total of the block function. Let's initialize
// the partial result to "no data".
				if (BitConverter.IsLittleEndian)
				{
					partialResult = 0x80u;
				}
				else
				{
					partialResult = 0x80000000u;
				}

				if ((count & 0b_0001) != 0)
				{
					// If the buffer is 1 or 3 bytes in length, let's read a single byte now
					// and merge it into our partial result. This will result in partialResult
					// having one of the two values below, where AA BB CC are the buffer bytes.
					//
					//                  (little-endian / big-endian)
					// [ AA          ]  -> 0x0000_80AA / 0xAA80_0000
					// [ AA BB CC    ]  -> 0x0000_80CC / 0xCC80_0000
					partialResult = Unsafe.AddByteOffset(ref data, (nuint)count & 2);

					if (BitConverter.IsLittleEndian)
					{
						partialResult |= 0x8000;
					}
					else
					{
						partialResult <<= 24;
						partialResult |= 0x800000u;
					}
				}

				if ((count & 0b_0010) != 0)
				{
					// If the buffer is 2 or 3 bytes in length, let's read a single ushort now
					// and merge it into the partial result. This will result in partialResult
					// having one of the two values below, where AA BB CC are the buffer bytes.
					//
					//                  (little-endian / big-endian)
					// [ AA BB       ]  -> 0x0080_BBAA / 0xAABB_8000
					// [ AA BB CC    ]  -> 0x80CC_BBAA / 0xAABB_CC80 (carried over from above)
					if (BitConverter.IsLittleEndian)
					{
						partialResult <<= 16;
						partialResult |= (uint)Unsafe.ReadUnaligned<ushort>(ref data);
					}
					else
					{
						partialResult |= (uint)Unsafe.ReadUnaligned<ushort>(ref data);
						partialResult = BitOperations.RotateLeft(partialResult, 16);
					}
				}

				// Everything is consumed! Go perform the final rounds and return.
				goto DoFinalRoundsAndReturn;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void Block(ref uint rp0, ref uint rp1)
		{
			unchecked
			{
				// Intrinsified in mono interpreter
				uint p0 = rp0;
				uint p1 = rp1;

				p1 ^= p0;
				p0 = BitOperations.RotateLeft(p0, 20);

				p0 += p1;
				p1 = BitOperations.RotateLeft(p1, 9);

				p1 ^= p0;
				p0 = BitOperations.RotateLeft(p0, 27);

				p0 += p1;
				p1 = BitOperations.RotateLeft(p1, 19);

				rp0 = p0;
				rp1 = p1;
			}
		}

		private static class Utf16Utility
		{
			/// <summary>
			/// Given a UInt32 that represents two ASCII UTF-16 characters, returns the invariant
			/// uppercase representation of those characters. Requires the input value to contain
			/// two ASCII UTF-16 characters in machine endianness.
			/// </summary>
			/// <remarks>
			/// This is a branchless implementation.
			/// </remarks>
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			internal static uint ConvertAllAsciiCharsInUInt32ToUppercase(uint value)
			{
				unchecked
				{
					// Intrinsified in mono interpreter
					// ASSUMPTION: Caller has validated that input value is ASCII.
					Debug.Assert(AllCharsInUInt32AreAscii(value), "input value is not all ASCII");

					// the 0x80 bit of each word of 'lowerIndicator' will be set iff the word has value >= 'a'
					uint lowerIndicator = value + 0x0080_0080u - 0x0061_0061u;

					// the 0x80 bit of each word of 'upperIndicator' will be set iff the word has value > 'z'
					uint upperIndicator = value + 0x0080_0080u - 0x007B_007Bu;

					// the 0x80 bit of each word of 'combinedIndicator' will be set iff the word has value >= 'a' and <= 'z'
					uint combinedIndicator = lowerIndicator ^ upperIndicator;

					// the 0x20 bit of each word of 'mask' will be set iff the word has value >= 'a' and <= 'z'
					uint mask = (combinedIndicator & 0x0080_0080u) >> 2;

					return value ^ mask; // bit flip lowercase letters [a-z] => [A-Z]
				}
			}

			/// <summary>
			/// Returns true iff the UInt32 represents two ASCII UTF-16 characters in machine endianness.
			/// </summary>
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			internal static bool AllCharsInUInt32AreAscii(uint value) => unchecked((value & ~0x007F_007Fu) == 0);
		}

		private static class BitOperations
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static uint RotateLeft(uint value, int offset)
			=> unchecked((value << offset) | (value >> (32 - offset)));
		}
	}
}
