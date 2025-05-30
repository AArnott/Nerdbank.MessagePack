// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack;

/// <summary>
/// Provides <see cref="IEqualityComparer{T}"/> and/or <see cref="IComparer{T}"/> objects
/// for given <see cref="ITypeShape{T}"/> objects.
/// </summary>
public interface IComparerProvider
{
	/// <summary>
	/// Gets an <see cref="IEqualityComparer{T}"/> for a type described by a given <see cref="ITypeShape{T}"/>.
	/// </summary>
	/// <typeparam name="T">The type of object to be compared.</typeparam>
	/// <param name="shape">The shape of the type to be compared.</param>
	/// <returns>An instance of <see cref="IEqualityComparer{T}"/> if the provider has or can construct one for the given type shape; otherwise <see langword="null" />.</returns>
	IEqualityComparer<T>? GetEqualityComparer<T>(ITypeShape<T> shape);

	/// <summary>
	/// Gets an <see cref="IComparer{T}"/> for a type described by a given <see cref="ITypeShape{T}"/>.
	/// </summary>
	/// <typeparam name="T">The type of object to be compared.</typeparam>
	/// <param name="shape">The shape of the type to be compared.</param>
	/// <returns>An instance of <see cref="IComparer{T}"/> if the provider has or can construct one for the given type shape; otherwise <see langword="null" />.</returns>
	IComparer<T>? GetComparer<T>(ITypeShape<T> shape);
}
