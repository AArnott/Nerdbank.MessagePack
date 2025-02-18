// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace ShapeShift.SecureHash;

/// <summary>
/// A <see cref="SecureEqualityComparer{T}"/> that operates on values through their surrogates.
/// </summary>
/// <typeparam name="T">The type of value to be compared.</typeparam>
/// <typeparam name="TSurrogate">The type of surrogate to use instead of the value directly.</typeparam>
/// <param name="marshaller">The means to convert between a value and its surrogate.</param>
/// <param name="surrogateComparer">The comparer that operates directly on the surrogate.</param>
internal class SurrogateSecureEqualityComparer<T, TSurrogate>(IMarshaller<T, TSurrogate> marshaller, SecureEqualityComparer<TSurrogate> surrogateComparer)
	: SecureEqualityComparer<T>
{
	/// <inheritdoc/>
	public override bool Equals(T? x, T? y)
		=> surrogateComparer.Equals(marshaller.ToSurrogate(x), marshaller.ToSurrogate(y));

	/// <inheritdoc/>
	public override long GetSecureHashCode([DisallowNull] T obj)
		=> marshaller.ToSurrogate(obj) is TSurrogate surrogate ? surrogateComparer.GetSecureHashCode(surrogate) : 0;
}
