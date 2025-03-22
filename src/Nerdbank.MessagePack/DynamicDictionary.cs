// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;

namespace Nerdbank.MessagePack;

/// <summary>
/// A schema-less dictionary deserialized from msgpack, optimized to work great with the C# <c>dynamic</c> keyword.
/// </summary>
/// <param name="underlying">The underlying dictionary.</param>
internal class DynamicDictionary(IReadOnlyDictionary<object, object?> underlying) : DynamicObject, IReadOnlyDictionary<object, object?>, IEnumerable
{
	/// <inheritdoc/>
	IEnumerable<object> IReadOnlyDictionary<object, object?>.Keys => underlying.Keys;

	/// <inheritdoc/>
	IEnumerable<object?> IReadOnlyDictionary<object, object?>.Values => underlying.Values;

	/// <inheritdoc/>
	int IReadOnlyCollection<KeyValuePair<object, object?>>.Count => underlying.Count;

	/// <inheritdoc/>
	object? IReadOnlyDictionary<object, object?>.this[object key] => underlying[StretchInteger(key)];

	/// <inheritdoc/>
	public override bool TryGetMember(GetMemberBinder binder, out object? result)
	{
		result = underlying[binder.Name];
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

		result = underlying[StretchInteger(indexes[0])];
		return true;
	}

	/// <inheritdoc/>
	public override IEnumerable<string> GetDynamicMemberNames() => underlying.Keys.OfType<string>();

	/// <inheritdoc/>
	public IEnumerator GetEnumerator() => underlying.Keys.GetEnumerator();

	/// <inheritdoc/>
	bool IReadOnlyDictionary<object, object?>.ContainsKey(object key) => underlying.ContainsKey(StretchInteger(key));

	/// <inheritdoc/>
	bool IReadOnlyDictionary<object, object?>.TryGetValue(object key, [MaybeNullWhen(false)] out object? value) => underlying.TryGetValue(StretchInteger(key), out value);

	/// <inheritdoc/>
	IEnumerator<KeyValuePair<object, object?>> IEnumerable<KeyValuePair<object, object?>>.GetEnumerator() => underlying.GetEnumerator();

	private static object StretchInteger(object key)
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
