// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace ShapeShift.SecureHash;

/// <summary>
/// A secure equality comparer that focuses on just one property for a given type.
/// </summary>
/// <typeparam name="TDeclaringType">The type that declares the property.</typeparam>
/// <typeparam name="TPropertyType">The type of the property itself.</typeparam>
/// <param name="getter">The function that can retrieve the value of the property from an instance of <typeparamref name="TDeclaringType"/>.</param>
/// <param name="equalityComparer">The equality comparer to use for the value of the property.</param>
internal class SecurePropertyEqualityComparer<TDeclaringType, TPropertyType>(
	Getter<TDeclaringType, TPropertyType> getter,
	SecureEqualityComparer<TPropertyType> equalityComparer) : SecureEqualityComparer<TDeclaringType>
{
	/// <inheritdoc/>
	public override bool Equals(TDeclaringType? x, TDeclaringType? y)
		=> x is null || y is null ? ReferenceEquals(x, y) : x.GetType() == y.GetType() && equalityComparer.Equals(getter(ref x), getter(ref y));

	/// <inheritdoc/>
	public override long GetSecureHashCode([DisallowNull] TDeclaringType obj)
		=> getter(ref obj) is TPropertyType value ? equalityComparer.GetSecureHashCode(value) : 0;
}
