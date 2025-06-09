// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NET
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8767 // null ref annotations
#endif

using System.Diagnostics.CodeAnalysis;

namespace Nerdbank.MessagePack.SecureHash;

/// <summary>
/// An implementation of <see cref="IEqualityComparer{T}"/> that delegates to <see cref="IStructuralSecureEqualityComparer{T}"/> on an object.
/// </summary>
/// <typeparam name="T">The self-implementing type to be compared.</typeparam>
internal class StructuralCustomEqualityComparer<T> : IEqualityComparer<T>
{
	/// <summary>
	/// The singleton that may be used for any type that implements <see cref="IStructuralSecureEqualityComparer{T}"/>.
	/// </summary>
	internal static readonly StructuralCustomEqualityComparer<T> Default = new();

	/// <inheritdoc/>
	public bool Equals(T? x, T? y)
	{
		if (x is null || y is null)
		{
			return ReferenceEquals(x, y);
		}

		return ((IStructuralSecureEqualityComparer<T>)x).StructuralEquals(y);
	}

	/// <inheritdoc/>
	public int GetHashCode([DisallowNull] T obj) => ((IStructuralSecureEqualityComparer<T>)obj).GetHashCode();
}
