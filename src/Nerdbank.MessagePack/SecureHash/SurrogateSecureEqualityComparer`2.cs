// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Nerdbank.MessagePack.SecureHash;

/// <summary>
/// A <see cref="SecureEqualityComparer{T}"/> that operates on values through their surrogates.
/// </summary>
/// <typeparam name="T">The type of value to be compared.</typeparam>
/// <typeparam name="TSurrogate">The type of surrogate to use instead of the value directly.</typeparam>
/// <param name="marshaler">The means to convert between a value and its surrogate.</param>
/// <param name="surrogateComparer">The comparer that operates directly on the surrogate.</param>
internal class SurrogateSecureEqualityComparer<T, TSurrogate>(IMarshaler<T, TSurrogate> marshaler, SecureEqualityComparer<TSurrogate> surrogateComparer)
	: SecureEqualityComparer<T>
{
	/// <inheritdoc/>
	public override bool Equals(T? x, T? y)
		=> surrogateComparer.Equals(marshaler.Marshal(x), marshaler.Marshal(y));

	/// <inheritdoc/>
	public override long GetSecureHashCode([DisallowNull] T obj)
		=> marshaler.Marshal(obj) is TSurrogate surrogate ? surrogateComparer.GetSecureHashCode(surrogate) : 0;
}
