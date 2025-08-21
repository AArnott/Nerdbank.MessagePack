// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NET
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8767 // null ref annotations
#endif

using System.Diagnostics.CodeAnalysis;

namespace Nerdbank.MessagePack.SecureHash;

/// <summary>
/// Implements <see cref="IEqualityComparer{T}"/> to compare a union type
/// with the appropriate derived type comparer.
/// </summary>
/// <typeparam name="TUnion">The base type.</typeparam>
/// <param name="getComparerAndIndex">A function to acquire the appropriate comparer given the runtime type and its union case index.</param>
internal class StructuralUnionEqualityComparer<TUnion>(
	Getter<TUnion, (IEqualityComparer<TUnion> Comparer, int? Index)> getComparerAndIndex) : IEqualityComparer<TUnion>
{
	/// <inheritdoc/>
	public bool Equals(TUnion? x, TUnion? y)
	{
		if (x is null || y is null)
		{
			return x is null && y is null;
		}

		(IEqualityComparer<TUnion> xComparer, _) = getComparerAndIndex(ref x);
		(IEqualityComparer<TUnion> yComparer, _) = getComparerAndIndex(ref y);
		if (xComparer != yComparer)
		{
			// x and y are different types.
			return false;
		}

		return xComparer.Equals(x, y);
	}

	/// <inheritdoc/>
	public int GetHashCode([DisallowNull] TUnion obj)
	{
		(IEqualityComparer<TUnion> comparer, int? index) = getComparerAndIndex(ref obj);
		return HashCode.Combine(index, comparer.GetHashCode(obj!));
	}
}
