// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Nerdbank.MessagePack.SecureHash;

/// <summary>
/// An implementation of <see cref="SecureEqualityComparer{T}"/> that delegates to <see cref="IDeepSecureEqualityComparer{T}"/> on an object.
/// </summary>
/// <typeparam name="T">The self-implementing type to be compared.</typeparam>
internal class SecureCustomEqualityComparer<T> : SecureEqualityComparer<T>
{
	/// <summary>
	/// The singleton that may be used for any type that implements <see cref="IDeepSecureEqualityComparer{T}"/>.
	/// </summary>
	internal static readonly SecureCustomEqualityComparer<T> Default = new();

	/// <inheritdoc/>
	public override bool Equals(T? x, T? y)
	{
		if (x is null || y is null)
		{
			return ReferenceEquals(x, y);
		}

		return ((IDeepSecureEqualityComparer<T>)x).DeepEquals(y);
	}

	/// <inheritdoc/>
	public override long GetSecureHashCode([DisallowNull] T obj) => ((IDeepSecureEqualityComparer<T>)obj).GetSecureHashCode();
}
