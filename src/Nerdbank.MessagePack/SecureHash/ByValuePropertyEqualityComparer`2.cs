// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NET
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8767 // null ref annotations
#endif

using System.Diagnostics.CodeAnalysis;

namespace Nerdbank.MessagePack.SecureHash;

/// <summary>
/// An equality comparer that focuses on just one property for a given type.
/// </summary>
/// <typeparam name="TDeclaringType">The type that declares the property.</typeparam>
/// <typeparam name="TPropertyType">The type of the property itself.</typeparam>
/// <param name="getter">The function that can retrieve the value of the property from an instance of <typeparamref name="TDeclaringType"/>.</param>
/// <param name="equalityComparer">The equality comparer to use for the value of the property.</param>
internal class ByValuePropertyEqualityComparer<TDeclaringType, TPropertyType>(
	Getter<TDeclaringType, TPropertyType> getter,
	IEqualityComparer<TPropertyType> equalityComparer) : IEqualityComparer<TDeclaringType>
{
	/// <inheritdoc/>
	public bool Equals(TDeclaringType? x, TDeclaringType? y)
		=> x is null || y is null ? ReferenceEquals(x, y) : x.GetType() == y.GetType() && equalityComparer.Equals(getter(ref x), getter(ref y));

	/// <inheritdoc/>
	public int GetHashCode([DisallowNull] TDeclaringType obj)
		=> getter(ref obj) is TPropertyType value ? equalityComparer.GetHashCode(value) : 0;
}
