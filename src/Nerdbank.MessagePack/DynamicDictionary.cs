// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;

namespace Nerdbank.MessagePack;

/// <summary>
/// A schema-less dictionary deserialized from msgpack, optimized to work great with the C# <c>dynamic</c> keyword.
/// </summary>
/// <remarks>
/// This class <em>could</em> be rewritten to resemble a read-only <see cref="ExpandoObject"/>, which
/// does not require dynamic code support in the runtime.
/// That would be a lot of code to copy from the runtime though, so until someone asks for it,
/// this is simply restricted in its use.
/// </remarks>
internal class DynamicDictionary : DynamicObject, IReadOnlyDictionary<object, object?>, IDictionary<object, object?>, IEnumerable
{
	private readonly IReadOnlyDictionary<object, object?> underlying;
	private ICollection<object>? keys;
	private ICollection<object?>? values;

	/// <summary>
	/// Initializes a new instance of the <see cref="DynamicDictionary"/> class.
	/// </summary>
	/// <param name="underlying">The underlying dictionary.</param>
	[RequiresDynamicCode(Reasons.DynamicObject)]
	internal DynamicDictionary(IReadOnlyDictionary<object, object?> underlying)
	{
		this.underlying = underlying;
	}

	/// <inheritdoc/>
	IEnumerable<object> IReadOnlyDictionary<object, object?>.Keys => this.underlying.Keys;

	/// <inheritdoc/>
	ICollection<object> IDictionary<object, object?>.Keys => this.keys ??= this.underlying.Keys as ICollection<object> ?? [.. this.underlying.Keys];

	/// <inheritdoc/>
	IEnumerable<object?> IReadOnlyDictionary<object, object?>.Values => this.underlying.Values;

	/// <inheritdoc/>
	ICollection<object?> IDictionary<object, object?>.Values => this.values ??= this.underlying.Values as ICollection<object?> ?? [.. this.underlying.Values];

	/// <inheritdoc/>
	int IReadOnlyCollection<KeyValuePair<object, object?>>.Count => this.underlying.Count;

	/// <inheritdoc/>
	int ICollection<KeyValuePair<object, object?>>.Count => this.underlying.Count;

	/// <inheritdoc/>
	bool ICollection<KeyValuePair<object, object?>>.IsReadOnly => true;

	/// <inheritdoc/>
	object? IReadOnlyDictionary<object, object?>.this[object key] => this.underlying[StretchInteger(key)];

	/// <inheritdoc/>
	object? IDictionary<object, object?>.this[object key]
	{
		get => this.underlying[StretchInteger(key)];
		set => throw new NotSupportedException();
	}

	/// <inheritdoc/>
	public override bool TryGetMember(GetMemberBinder binder, out object? result)
	{
		result = this.underlying[binder.Name];
		return true;
	}

	/// <inheritdoc/>
	public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object? result)
	{
		if (indexes.Length != 1)
		{
			result = null;
			return false;
		}

		result = this.underlying[StretchInteger(indexes[0])];
		return true;
	}

	/// <inheritdoc/>
	public override IEnumerable<string> GetDynamicMemberNames() => this.underlying.Keys.OfType<string>();

	/// <inheritdoc/>
	public IEnumerator GetEnumerator() => this.underlying.Keys.GetEnumerator();

	/// <inheritdoc/>
	bool IReadOnlyDictionary<object, object?>.ContainsKey(object key) => this.underlying.ContainsKey(StretchInteger(key));

	/// <inheritdoc/>
	bool IDictionary<object, object?>.ContainsKey(object key) => this.underlying.ContainsKey(StretchInteger(key));

	/// <inheritdoc/>
	bool ICollection<KeyValuePair<object, object?>>.Contains(KeyValuePair<object, object?> item) => this.underlying.TryGetValue(StretchInteger(item.Key), out object? value) && EqualityComparer<object?>.Default.Equals(value, item.Value);

	/// <inheritdoc/>
	bool IReadOnlyDictionary<object, object?>.TryGetValue(object key, [MaybeNullWhen(false)] out object? value) => this.underlying.TryGetValue(StretchInteger(key), out value);

	/// <inheritdoc/>
	bool IDictionary<object, object?>.TryGetValue(object key, out object? value) => this.underlying.TryGetValue(StretchInteger(key), out value);

	/// <inheritdoc/>
	IEnumerator<KeyValuePair<object, object?>> IEnumerable<KeyValuePair<object, object?>>.GetEnumerator() => this.underlying.GetEnumerator();

	/// <inheritdoc/>
	void ICollection<KeyValuePair<object, object?>>.CopyTo(KeyValuePair<object, object?>[] array, int arrayIndex)
	{
		foreach (KeyValuePair<object, object?> item in this.underlying)
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
	/// If <paramref name="key"/> is a positive integer,
	/// the result will be a boxed <see cref="ulong"/>.
	/// If <paramref name="key"/> is a negative integer,
	/// the result will be a boxed <see cref="long"/>.
	/// For all other <paramref name="key"/> values,
	/// the result will be the original value.
	/// </returns>
	internal static object StretchInteger(object key)
		=> key switch
		{
			sbyte v => v < 0 ? (long)v : (ulong)v,
			short v => v < 0 ? (long)v : (ulong)v,
			int v => v < 0 ? (long)v : (ulong)v,
			byte v => (ulong)v,
			ushort v => (ulong)v,
			uint v => (ulong)v,
			_ => key,
		};
}
