// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NET
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8767 // null ref annotations
#endif

using System.Diagnostics.CodeAnalysis;

namespace Nerdbank.MessagePack.SecureHash;

/// <summary>
/// Implements an <see cref="IEqualityComparer{T}"/> for a specific derived case of a union type.
/// </summary>
/// <typeparam name="TUnionCase">The derived type.</typeparam>
/// <typeparam name="TUnion">The base type.</typeparam>
/// <param name="inner">The comparer of the derived type.</param>
internal class StructuralUnionCaseEqualityComparer<TUnionCase, TUnion>(IEqualityComparer<TUnionCase> inner) : IEqualityComparer<TUnion>
	where TUnionCase : TUnion
{
	/// <inheritdoc/>
	public bool Equals(TUnion? x, TUnion? y) => inner.Equals((TUnionCase?)x, (TUnionCase?)y);

	/// <inheritdoc/>
	public int GetHashCode([DisallowNull] TUnion obj) => inner.GetHashCode((TUnionCase)obj);
}
