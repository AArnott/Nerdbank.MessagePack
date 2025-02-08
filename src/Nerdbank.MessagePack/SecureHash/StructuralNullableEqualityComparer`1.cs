// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NET
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8767 // null ref annotations
#endif

using System.Diagnostics.CodeAnalysis;

namespace Nerdbank.PolySerializer.SecureHash;

/// <summary>
/// Implements a hash collision resistant <see cref="IEqualityComparer{T}"/> for nullable value types.
/// </summary>
/// <typeparam name="T">The value type.</typeparam>
/// <param name="inner">The inner equality comparer for the non-nullable value type.</param>
internal class StructuralNullableEqualityComparer<T>(IEqualityComparer<T> inner) : IEqualityComparer<T?>
	where T : struct
{
	/// <inheritdoc/>
	public bool Equals(T? x, T? y)
	{
		if (x is null || y is null)
		{
			return x is null && y is null;
		}

		return inner.Equals(x.Value, y.Value);
	}

	/// <inheritdoc/>
	public int GetHashCode([DisallowNull] T? obj)
	{
		if (obj is null)
		{
			return 0;
		}

		return inner.GetHashCode(obj.Value);
	}
}
