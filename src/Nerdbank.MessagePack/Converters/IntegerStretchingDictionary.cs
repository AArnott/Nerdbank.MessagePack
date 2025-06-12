// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable NBMsgPack031

using System.Diagnostics.CodeAnalysis;

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// A dictionary that wraps a deserialized msgpack map and ensures that all boxed integer lookups are coerced to the appropriate type to allow a match.
/// </summary>
/// <param name="underlying">The underlying dictionary. All integers must be boxed as <see cref="ulong"/> or (if they are negative) as <see cref="long"/>, or else they will not be found when looked up.</param>
internal class IntegerStretchingDictionary(IReadOnlyDictionary<object, object?> underlying) : IReadOnlyDictionary<object, object?>, IDictionary<object, object?>, System.Collections.IEnumerable
{
	private ICollection<object>? keys;
	private ICollection<object?>? values;

	/// <inheritdoc/>
	IEnumerable<object> IReadOnlyDictionary<object, object?>.Keys => underlying.Keys;

	/// <inheritdoc/>
	ICollection<object> IDictionary<object, object?>.Keys => this.keys ??= underlying.Keys as ICollection<object> ?? [.. underlying.Keys];

	/// <inheritdoc/>
	IEnumerable<object?> IReadOnlyDictionary<object, object?>.Values => underlying.Values;

	/// <inheritdoc/>
	ICollection<object?> IDictionary<object, object?>.Values => this.values ??= underlying.Values as ICollection<object?> ?? [.. underlying.Values];

	/// <inheritdoc/>
	public int Count => underlying.Count;

	/// <inheritdoc/>
	bool ICollection<KeyValuePair<object, object?>>.IsReadOnly => true;

	/// <inheritdoc/>
	public object? this[object key] => underlying[StretchInteger(key)];

	/// <inheritdoc/>
	object? IDictionary<object, object?>.this[object key]
	{
		get => underlying[StretchInteger(key)];
		set => throw new NotSupportedException();
	}

	/// <inheritdoc/>
	System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => underlying.Keys.GetEnumerator();

	/// <inheritdoc/>
	public bool ContainsKey(object key) => underlying.ContainsKey(StretchInteger(key));

	/// <inheritdoc/>
	bool ICollection<KeyValuePair<object, object?>>.Contains(KeyValuePair<object, object?> item) => underlying.TryGetValue(StretchInteger(item.Key), out object? value) && EqualityComparer<object?>.Default.Equals(value, item.Value);

	/// <inheritdoc/>
	public bool TryGetValue(object key, [MaybeNullWhen(false)] out object? value) => underlying.TryGetValue(StretchInteger(key), out value);

	/// <inheritdoc/>
	public IEnumerator<KeyValuePair<object, object?>> GetEnumerator() => underlying.GetEnumerator();

	/// <inheritdoc/>
	public void CopyTo(KeyValuePair<object, object?>[] array, int arrayIndex)
	{
		foreach (KeyValuePair<object, object?> item in underlying)
		{
			array[arrayIndex++] = item;
		}
	}

	/// <inheritdoc/>
	void IDictionary<object, object?>.Add(object key, object? value) => throw new NotSupportedException();

	/// <inheritdoc/>
	bool IDictionary<object, object?>.Remove(object key) => throw new NotSupportedException();

	/// <inheritdoc/>
	void ICollection<KeyValuePair<object, object?>>.Add(KeyValuePair<object, object?> item) => throw new NotSupportedException();

	/// <inheritdoc/>
	bool ICollection<KeyValuePair<object, object?>>.Remove(KeyValuePair<object, object?> item) => throw new NotSupportedException();

	/// <inheritdoc/>
	void ICollection<KeyValuePair<object, object?>>.Clear() => throw new NotSupportedException();

	/// <summary>
	/// Stretches an integer value to its 64-bit representation.
	/// </summary>
	/// <param name="key">The boxed integer, or a value of any other type.</param>
	/// <returns>
	/// If <paramref name="key"/> is 0 or a positive integer,
	/// the result will be a boxed <see cref="ulong"/>.
	/// If <paramref name="key"/> is a negative integer,
	/// the result will be a boxed <see cref="long"/>.
	/// For all other <paramref name="key"/> values,
	/// the result will be the original value.
	/// </returns>
	protected static object StretchInteger(object key)
		=> key switch
		{
			sbyte v => v < 0 ? (long)v : (ulong)v,
			short v => v < 0 ? (long)v : (ulong)v,
			int v => v < 0 ? (long)v : (ulong)v,
			byte v => (ulong)v,
			ushort v => (ulong)v,
			uint v => (ulong)v,
			long v when v >= 0 => (ulong)v,
			_ => key,
		};
}
