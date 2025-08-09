// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Nerdbank.MessagePack.SecureHash;

/// <summary>
/// Implements a hash collision resistant <see cref="IEqualityComparer{T}"/> for a specific derived case of a union type.
/// </summary>
/// <typeparam name="TUnionCase">The derived type.</typeparam>
/// <typeparam name="TUnion">The base type.</typeparam>
/// <param name="inner">The comparer of the derived type.</param>
/// <param name="marshaler">The marshaler to change between <typeparamref name="TUnion"/> and <typeparamref name="TUnionCase"/>.</param>
internal class SecureUnionCaseEqualityComparer<TUnionCase, TUnion>(SecureEqualityComparer<TUnionCase> inner, IMarshaler<TUnionCase, TUnion> marshaler) : SecureEqualityComparer<TUnion>
{
	/// <inheritdoc/>
	public override bool Equals(TUnion? x, TUnion? y) => inner.Equals(marshaler.Unmarshal(x), marshaler.Unmarshal(y));

	/// <inheritdoc/>
	public override long GetSecureHashCode([DisallowNull] TUnion obj) => marshaler.Unmarshal(obj) is TUnionCase c ? inner.GetSecureHashCode(c) : 0;
}
