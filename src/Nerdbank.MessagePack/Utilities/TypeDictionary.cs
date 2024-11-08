// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// This file was originally derived from https://github.com/eiriktsarpalis/PolyType/
// with Eirik Tsarpalis getting credit for the original implementation.
#pragma warning disable SA1402 // File may only contain a single type

using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Nerdbank.MessagePack.Utilities;

/// <summary>
/// Defines a dictionary that can be used to store values keyed on <see cref="Type"/>.
/// </summary>
/// <remarks>
/// Can be used for storing values while walking potentially cyclic type graphs.
/// Includes facility for delayed value computation in case of recursive types.
/// </remarks>
internal class TypeDictionary : IDictionary<Type, object?>, IReadOnlyDictionary<Type, object?>
{
	// Entries with IsCompleted: false denote types whose values are still being computed.
	// In such cases the value is either null or an instance of IResultBox representing a delayed value.
	// These values are only surfaced in lookup calls where a delayedValueFactory parameter is specified.
	private readonly Dictionary<Type, (object? Value, bool IsCompleted)> dict = new();
	private DelayedCollection<Type>? keyCollection;
	private DelayedCollection<object?>? valueCollection;

	/// <summary>
	/// A non-generic interface into the result box.
	/// </summary>
	private interface IResultBox
	{
		/// <summary>
		/// Gets the delayed value.
		/// </summary>
		object? DelayedValue { get; }

		/// <summary>
		/// Replaces the <see cref="DelayedValue"/> with a now completed one.
		/// </summary>
		/// <param name="result">The completed result.</param>
		void CompleteResult(object? result);
	}

	/// <inheritdoc/>
	bool ICollection<KeyValuePair<Type, object?>>.IsReadOnly => false;

	/// <inheritdoc/>
	ICollection<Type> IDictionary<Type, object?>.Keys => this.keyCollection ??= new(this.dict.Where(e => e.Value.IsCompleted).Select(e => e.Key));

	/// <inheritdoc/>
	ICollection<object?> IDictionary<Type, object?>.Values => this.valueCollection ??= new(this.dict.Where(e => e.Value.IsCompleted).Select(e => e.Value.Value));

	/// <summary>
	/// Gets the total number of generated values.
	/// </summary>
	public int Count => this.dict.Count(e => e.Value.IsCompleted);

	/// <inheritdoc/>
	IEnumerable<Type> IReadOnlyDictionary<Type, object?>.Keys => ((IDictionary<Type, object?>)this).Keys;

	/// <inheritdoc/>
	IEnumerable<object?> IReadOnlyDictionary<Type, object?>.Values => ((IDictionary<Type, object?>)this).Values;

	/// <summary>
	/// Gets or sets the value associated with the specified key.
	/// </summary>
	/// <param name="key">The key to look up.</param>
	/// <returns>The value matching the given <paramref name="key"/>.</returns>
	/// <exception cref="KeyNotFoundException">Thrown when the key was not found in the dictionary.</exception>
	public object? this[Type key]
	{
		get => this.dict.TryGetValue(key, out (object? Value, bool IsCompleted) entry) && entry.IsCompleted ? entry.Value : throw new KeyNotFoundException();
		set => this.Add(key, value, overwrite: true);
	}

	/// <summary>
	/// Gets or adds a value keyed on the type represented by <paramref name="shape"/>.
	/// </summary>
	/// <typeparam name="TValue">The type of the value to be resolved.</typeparam>
	/// <param name="shape">The type shape representing the key type.</param>
	/// <param name="visitor">The type shape visitor used to compute the value.</param>
	/// <param name="delayedValueFactory">A factory used to create delayed values in case of recursive types.</param>
	/// <param name="state">The state object to be passed to the visitor.</param>
	/// <returns>The final computed value.</returns>
	public TValue GetOrAdd<TValue>(
		ITypeShape shape,
		ITypeShapeVisitor visitor,
		Func<ResultBox<TValue>, TValue>? delayedValueFactory = null,
		object? state = null)
	{
		if (this.TryGetValue(shape.Type, out TValue? value, delayedValueFactory))
		{
			return value;
		}

		value = (TValue)shape.Accept(visitor, state)!;
		this.Add(shape.Type, value);
		return value;
	}

	/// <summary>
	/// Looks up the value for type <paramref name="key"/>, or a delayed value if entering a cyclic occurrence.
	/// </summary>
	/// <typeparam name="TValue">The type of the value to be looked up.</typeparam>
	/// <param name="key">The type for which to look up the value.</param>
	/// <param name="value">The value returned by the lookup operation.</param>
	/// <param name="delayedValueFactory">A factory for creating delayed values in case of cyclic types.</param>
	/// <returns>True if either a completed or delayed value have been returned.</returns>
	public bool TryGetValue<TValue>(Type key, [MaybeNullWhen(false)] out TValue value, Func<ResultBox<TValue>, TValue>? delayedValueFactory = null)
	{
		ref (object? Entry, bool IsCompleted) entryRef = ref CollectionsMarshal.GetValueRefOrNullRef(this.dict, key);
		if (Unsafe.IsNullRef(ref entryRef))
		{
			// First time visiting this type, return no result.
			if (delayedValueFactory != null)
			{
				// If we're specifying a delayed factory, add an empty entry
				// to denote that the next lookup should create a delayed value.
				this.dict[key] = (default(TValue), IsCompleted: false);
			}

			value = default;
			return false;
		}
		else if (!entryRef.IsCompleted)
		{
			// Second time visiting this type without a value being computed, encountering a potential cyclic type.
			if (delayedValueFactory is null)
			{
				// If no delayed factory is specified, return no result.
				value = default;
				return false;
			}

			Debug.Assert(entryRef.Entry is null or IResultBox, $"{entryRef.Entry} is null or IResultBox");

			if (entryRef.Entry is IResultBox existingResultBox)
			{
				// A delayed value has already bee created, return that.
				value = (TValue)existingResultBox.DelayedValue!;
			}
			else
			{
				// Create a new delayed value and update the entry.
				var newResultBox = new ResultBoxImpl<TValue>();
				newResultBox.DelayedValue = value = delayedValueFactory(newResultBox);
				entryRef = (newResultBox, IsCompleted: false);
			}

			return true;
		}
		else
		{
			// We found a completed entry, return it.
			value = (TValue)entryRef.Entry!;
			return true;
		}
	}

	/// <summary>
	/// Adds a new entry to the dictionary, completing any delayed values for the key type.
	/// </summary>
	/// <typeparam name="TValue">The type of the value to be added.</typeparam>
	/// <param name="key">The key type of the new entry.</param>
	/// <param name="value">The value of the new entry.</param>
	/// <param name="overwrite">Whether to overwrite existing entries.</param>
	public void Add<TValue>(Type key, TValue value, bool overwrite = false)
	{
		ref (object? Entry, bool IsCompleted) entryRef = ref CollectionsMarshal.GetValueRefOrNullRef(this.dict, key);

		if (Unsafe.IsNullRef(ref entryRef))
		{
			this.dict[key] = (value, IsCompleted: true);
		}
		else
		{
			if (entryRef.IsCompleted && !overwrite)
			{
				throw new InvalidOperationException($"A key of type '{key}' has already been added to the cache.");
			}

			if (entryRef.Entry is IResultBox resultBox)
			{
				// Complete the delayed value with the new value.
				Debug.Assert(!entryRef.IsCompleted, "!entryRef.IsCompleted");
				resultBox.CompleteResult(value);
			}

			entryRef = (value, IsCompleted: true);
		}
	}

	/// <summary>
	/// Checks if the specified key is present in the dictionary.
	/// </summary>
	/// <param name="key">The key to look up.</param>
	/// <returns>A value indicating whether the type exists as a key in the dictionary.</returns>
	public bool ContainsKey(Type key) => this.dict.TryGetValue(key, out (object? Value, bool IsCompleted) entry) && entry.IsCompleted;

	/// <summary>
	/// Clears the contents of the dictionary.
	/// </summary>
	public void Clear() => this.dict.Clear();

	/// <summary>
	/// Removes the entry associated with the specified key.
	/// </summary>
	/// <param name="key">The key to remove.</param>
	/// <returns>A boolean indicating whether an entry was succesfully removed.</returns>
	public bool Remove(Type key) => this.dict.Remove(key);

	/// <inheritdoc/>
	bool IDictionary<Type, object?>.TryGetValue(Type key, out object? value) => this.TryGetValue(key, out value);

	/// <inheritdoc/>
	void IDictionary<Type, object?>.Add(Type key, object? value) => this.Add(key, value);

	/// <inheritdoc/>
	void ICollection<KeyValuePair<Type, object?>>.Add(KeyValuePair<Type, object?> item) => this.Add(item.Key, item.Value);

	/// <inheritdoc/>
	bool ICollection<KeyValuePair<Type, object?>>.Contains(KeyValuePair<Type, object?> item) => this.dict.Contains(new(item.Key, (item.Value, IsCompleted: true)));

	/// <inheritdoc/>
	bool ICollection<KeyValuePair<Type, object?>>.Remove(KeyValuePair<Type, object?> item) => ((ICollection<KeyValuePair<Type, (object?, bool)>>)this.dict).Remove(new(item.Key, (item.Value, true)));

	/// <inheritdoc/>
	void ICollection<KeyValuePair<Type, object?>>.CopyTo(KeyValuePair<Type, object?>[] array, int arrayIndex)
	{
		if (array.Length - arrayIndex < this.Count)
		{
			throw new ArgumentException("Insufficient space in the target array.", nameof(array));
		}

		foreach (KeyValuePair<Type, (object? Value, bool IsCompleted)> entry in this.dict)
		{
			if (entry.Value.IsCompleted)
			{
				array[arrayIndex++] = new(entry.Key, entry.Value.Value);
			}
		}
	}

	/// <inheritdoc/>
	IEnumerator<KeyValuePair<Type, object?>> IEnumerable<KeyValuePair<Type, object?>>.GetEnumerator()
	{
		foreach (KeyValuePair<Type, (object? Value, bool IsCompleted)> entry in this.dict)
		{
			if (entry.Value.IsCompleted)
			{
				yield return new KeyValuePair<Type, object?>(entry.Key, entry.Value.Value);
			}
		}
	}

	/// <inheritdoc/>
	IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<KeyValuePair<Type, object?>>)this).GetEnumerator();

	/// <inheritdoc/>
	bool IReadOnlyDictionary<Type, object?>.TryGetValue(Type key, out object? value) => this.TryGetValue(key, out value);

	private sealed class ResultBoxImpl<T> : ResultBox<T>, IResultBox
	{
		object? IResultBox.DelayedValue => this.DelayedValue;

		internal T? DelayedValue { get; set; }

		public override void CompleteResult(T? result)
		{
			base.CompleteResult(result);
			this.DelayedValue = default;
		}

		void IResultBox.CompleteResult(object? result) => this.CompleteResult((T)result!);
	}

	private sealed class DelayedCollection<T>(IEnumerable<T> source) : ICollection<T>
	{
		public int Count => source.Count();

		public bool IsReadOnly => true;

		public bool Contains(T item) => source.Contains(item);

		public void CopyTo(T[] array, int arrayIndex) => source.ToArray().CopyTo(array, arrayIndex);

		IEnumerator IEnumerable.GetEnumerator() => source.GetEnumerator();

		public IEnumerator<T> GetEnumerator() => source.GetEnumerator();

		public bool Remove(T item) => throw new NotSupportedException();

		public void Add(T item) => throw new NotSupportedException();

		public void Clear() => throw new NotSupportedException();
	}
}

/// <summary>
/// A container that holds the delayed result of a computation.
/// </summary>
/// <typeparam name="T">The type of the contained value.</typeparam>
internal abstract class ResultBox<T>
{
	/// <summary>
	/// Stores the result, when it is ready.
	/// </summary>
	private T? result;

	/// <summary>
	/// Gets a value indicating whether the result has been computed.
	/// </summary>
	public bool IsCompleted { get; private protected set; }

	/// <summary>
	/// Gets the contained result if populated.
	/// </summary>
	/// <exception cref="InvalidOperationException">Thrown if the result is not ready yet.</exception>
	public T Result
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			if (!this.IsCompleted)
			{
				Throw();
				static void Throw() => throw new InvalidOperationException($"Value of type '{typeof(T)}' has not been completed yet.");
			}

			return this.result!;
		}
	}

	/// <summary>
	/// Sets this box as containing a finalized result.
	/// </summary>
	/// <param name="result">The final result.</param>
	public virtual void CompleteResult(T? result)
	{
		this.result = result;
		this.IsCompleted = true;
	}
}
