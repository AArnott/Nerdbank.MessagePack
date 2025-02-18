// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace ShapeShift.SecureHash;

/// <summary>
/// A secure equality comparer that aggregates the results of multiple other secure equality comparers.
/// </summary>
/// <typeparam name="T">The type that the component equality comparers consider.</typeparam>
/// <param name="components">
/// This is expected to be a reasonably small number (e.g. properties on a type).
/// While hashing, 8 bytes of stack space will be allocated for each of these.
/// If this is an empty array, equality checking will always return <see langword="false" />.
/// </param>
internal class SecureAggregatingEqualityComparer<T>(ImmutableArray<SecureEqualityComparer<T>> components) : SecureEqualityComparer<T>
{
	/// <summary>
	/// Gets a value indicating whether no components were given.
	/// </summary>
	internal bool IsEmpty => components.IsEmpty;

	/// <inheritdoc/>
	public override bool Equals(T? x, T? y) => components.All(c => c.Equals(x, y));

	/// <inheritdoc/>
	public override long GetSecureHashCode([DisallowNull] T obj)
	{
		// Ideally we could switch this to a SIP hash implementation that can process additional data in chunks with a constant amount of memory.
		Span<long> componentHashes = stackalloc long[components.Length];
		for (int i = 0; i < components.Length; i++)
		{
			componentHashes[i] = components[i].GetSecureHashCode(obj);
		}

		return SipHash.Default.Compute(MemoryMarshal.Cast<long, byte>(componentHashes));
	}
}
