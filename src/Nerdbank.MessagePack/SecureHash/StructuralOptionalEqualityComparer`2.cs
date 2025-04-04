﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NET
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8767 // null ref annotations
#endif

using System.Diagnostics.CodeAnalysis;

namespace Nerdbank.MessagePack.SecureHash;

/// <summary>
/// Implements a hash collision resistant <see cref="IEqualityComparer{T}"/> for nullable value types.
/// </summary>
/// <typeparam name="TOptional">The optional wrapper around <typeparamref name="TElement"/>.</typeparam>
/// <typeparam name="TElement">The value type.</typeparam>
/// <param name="inner">The inner equality comparer for the non-nullable value type.</param>
/// <param name="deconstructor">A function to unwrap an optional value.</param>
internal class StructuralOptionalEqualityComparer<TOptional, TElement>(
	IEqualityComparer<TElement> inner,
	OptionDeconstructor<TOptional, TElement> deconstructor) : IEqualityComparer<TOptional>
{
	/// <inheritdoc/>
	public bool Equals(TOptional? x, TOptional? y)
	{
		bool xUnwrapped = deconstructor(x, out TElement? xValue);
		bool yUnwrapped = deconstructor(y, out TElement? yValue);
		if (!xUnwrapped || !yUnwrapped)
		{
			return !xUnwrapped && !yUnwrapped;
		}

		return inner.Equals(xValue, yValue);
	}

	/// <inheritdoc/>
	public int GetHashCode([DisallowNull] TOptional obj)
	{
		if (!deconstructor(obj, out TElement? value))
		{
			return 0;
		}

		return inner.GetHashCode(value!);
	}
}
