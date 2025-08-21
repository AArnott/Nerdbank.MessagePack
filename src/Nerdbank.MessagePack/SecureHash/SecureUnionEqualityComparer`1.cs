// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Nerdbank.MessagePack.SecureHash;

/// <summary>
/// Implements <see cref="SecureEqualityComparer{T}"/> to compare a union type
/// with the appropriate derived type comparer.
/// </summary>
/// <typeparam name="TUnion">The base type.</typeparam>
/// <param name="getComparerAndIndex">A function to acquire the appropriate comparer given the runtime type and its union case index.</param>
internal class SecureUnionEqualityComparer<TUnion>(
	Getter<TUnion, (SecureEqualityComparer<TUnion> Comparer, int? Index)> getComparerAndIndex) : SecureEqualityComparer<TUnion>
{
	/// <inheritdoc/>
	public override bool Equals(TUnion? x, TUnion? y)
	{
		if (x is null || y is null)
		{
			return x is null && y is null;
		}

		(SecureEqualityComparer<TUnion> xComparer, _) = getComparerAndIndex(ref x);
		(SecureEqualityComparer<TUnion> yComparer, _) = getComparerAndIndex(ref y);
		if (xComparer != yComparer)
		{
			// x and y are different types.
			return false;
		}

		return xComparer.Equals(x, y);
	}

	/// <inheritdoc/>
	public override long GetSecureHashCode([DisallowNull] TUnion obj)
	{
		(SecureEqualityComparer<TUnion> comparer, int? index) = getComparerAndIndex(ref obj);
		return HashCode.Combine(index, comparer.GetSecureHashCode(obj!));
	}
}
