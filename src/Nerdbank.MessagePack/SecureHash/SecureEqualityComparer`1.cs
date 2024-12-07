// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NET
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8767 // null ref annotations
#endif

using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Nerdbank.MessagePack.SecureHash;

/// <summary>
/// A base class for all <see cref="IEqualityComparer{T}"/> implementations that are hash collision resistant.
/// </summary>
/// <typeparam name="T">The type of value to be compared or hashed.</typeparam>
internal abstract class SecureEqualityComparer<T> : IEqualityComparer<T>, IEqualityComparer
{
	/// <summary>
	/// Performs a by-value equality comparison that conforms to the invariant that
	/// if <see cref="Equals(T?, T?)"/> returns <see langword="true"/> for two values,
	/// then <see cref="GetHashCode(T)"/> must return the same value for both.
	/// </summary>
	/// <param name="x">The first value to compare.</param>
	/// <param name="y">The second value to compare.</param>
	/// <returns><see langword="true" /> if the two values are equivalent; <see langword="false"/> otherwise.</returns>
	public abstract bool Equals(T? x, T? y);

	/// <summary>
	/// Gets a collision-resistant hash for the given value, truncated to just 32 bits.
	/// </summary>
	/// <param name="obj">The value to hash.</param>
	/// <returns>The hash function result, truncated to 32 bits.</returns>
	public int GetHashCode([DisallowNull] T obj) => unchecked((int)this.GetSecureHashCode(obj));

	/// <summary>
	/// Gets a collision-resistant hash for the given value, retaining the full 64 bits.
	/// </summary>
	/// <param name="obj">The value to hash.</param>
	/// <returns>The 64 bit hash function result.</returns>
	public abstract long GetSecureHashCode([DisallowNull] T obj);

	/// <inheritdoc/>
	bool IEqualityComparer.Equals(object? x, object? y) => this.Equals((T?)x, (T?)y);

	/// <inheritdoc/>
	int IEqualityComparer.GetHashCode(object obj) => this.GetHashCode((T)obj);
}
