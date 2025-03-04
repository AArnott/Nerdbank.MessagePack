// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Nerdbank.MessagePack.SecureHash;

/// <summary>
/// Implements <see cref="SecureEqualityComparer{T}"/> to compare a union type
/// with the appropriate derived type comparer.
/// </summary>
/// <typeparam name="TUnion">The base type.</typeparam>
/// <param name="getComparer">A function to acquire the appropriate comparer given the runtime type.</param>
internal class SecureUnionEqualityComparer<TUnion>(
	Getter<TUnion, SecureEqualityComparer<TUnion>> getComparer) : SecureEqualityComparer<TUnion>
{
	/// <inheritdoc/>
	public override bool Equals(TUnion? x, TUnion? y)
	{
		if (x is null || y is null)
		{
			return x is null && y is null;
		}

		SecureEqualityComparer<TUnion> xComparer = getComparer(ref x);
		SecureEqualityComparer<TUnion> yComparer = getComparer(ref y);
		if (xComparer != yComparer)
		{
			// x and y are different types.
			return false;
		}

		return xComparer.Equals(x, y);
	}

	/// <inheritdoc/>
	public override long GetSecureHashCode([DisallowNull] TUnion obj) => getComparer(ref obj).GetSecureHashCode(obj!);
}
