// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack;

/// <summary>
/// A strong ref string interning collection.
/// </summary>
internal class StringInterning : IPoolableObject
{
	private readonly StringlyKeyedDictionary<string> cache = new(32, isCaseSensitive: true);

	MessagePackSerializer? IPoolableObject.Owner { get; set; }

	void IPoolableObject.Recycle() => this.Clear();

	/// <summary>
	/// Clears the cache of any strong references to interned strings.
	/// </summary>
	internal void Clear() => this.cache.Clear();

	/// <summary>
	/// Returns an interned string for the given string.
	/// </summary>
	/// <param name="value">The string to be interned.</param>
	/// <returns>A reference to an equivalent, interned string. This will be <paramref name="value"/> itself if the string was not previously interned.</returns>
	internal string Intern(string value)
	{
		if (this.cache.TryGetValue(value, out string? interned))
		{
			return interned;
		}

		this.cache[value] = value;
		return value;
	}

	/// <summary>
	/// Returns an interned string for a given character span.
	/// </summary>
	/// <param name="value">The characters for which an interned string is required.</param>
	/// <returns>The interned string.</returns>
	internal string Intern(ReadOnlySpan<char> value)
	{
		if (this.cache.TryGetValue(value, out string? interned))
		{
			return interned;
		}

		string newValue = value.ToString();
		this.cache[newValue] = newValue;
		return newValue;
	}
}
