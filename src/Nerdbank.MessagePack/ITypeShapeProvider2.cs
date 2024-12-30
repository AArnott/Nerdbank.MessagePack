// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack;

/// <summary>
/// An extension of <see cref="ITypeShapeProvider"/> that provides shapes for
/// generic types.
/// The idea is that this interface be merged into <see cref="ITypeShapeProvider"/> itself.
/// </summary>
public interface ITypeShapeProvider2
{
	/// <summary>
	/// Provides the shape for a generic type.
	/// </summary>
	/// <param name="unboundGenericType">The unbound generic type.</param>
	/// <param name="genericTypeArguments">The type arguments.</param>
	/// <returns>The shape.</returns>
	public ITypeShape? GetShape(Type unboundGenericType, ReadOnlySpan<Type> genericTypeArguments);
}
