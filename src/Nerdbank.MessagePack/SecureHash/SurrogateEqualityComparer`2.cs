// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETSTANDARD
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8767 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).
#endif

using System.Diagnostics.CodeAnalysis;

namespace Nerdbank.MessagePack.SecureHash;

/// <summary>
/// An <see cref="IEqualityComparer{T}"/> that operates on values through their surrogates.
/// </summary>
/// <typeparam name="T">The type of value to be compared.</typeparam>
/// <typeparam name="TSurrogate">The type of surrogate to use instead of the value directly.</typeparam>
/// <param name="marshaller">The means to convert between a value and its surrogate.</param>
/// <param name="surrogateComparer">The comparer that operates directly on the surrogate.</param>
internal class SurrogateEqualityComparer<T, TSurrogate>(IMarshaller<T, TSurrogate> marshaller, IEqualityComparer<TSurrogate> surrogateComparer)
	: IEqualityComparer<T>
{
	/// <inheritdoc/>
	public bool Equals(T? x, T? y)
		=> surrogateComparer.Equals(marshaller.ToSurrogate(x), marshaller.ToSurrogate(y));

	/// <inheritdoc/>
	public int GetHashCode([DisallowNull] T obj)
		=> marshaller.ToSurrogate(obj) is TSurrogate surrogate ? surrogateComparer.GetHashCode(surrogate) : 0;
}
