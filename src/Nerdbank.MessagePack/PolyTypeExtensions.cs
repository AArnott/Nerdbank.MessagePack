// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack;

/// <summary>
/// Extensions for types from the PolyType library that are used in this library.
/// </summary>
internal static class PolyTypeExtensions
{
	/// <inheritdoc cref="TypeShapeProviderExtensions.GetTypeShape{T}(ITypeShapeProvider, bool)"/>
	internal static ITypeShape<T> Resolve<T>(this ITypeShapeProvider typeShapeProvider) => typeShapeProvider.GetTypeShape<T>(throwIfMissing: true)!;

	/// <inheritdoc cref="TypeShapeProviderExtensions.GetTypeShape(ITypeShapeProvider, Type, bool)"/>
	internal static ITypeShape Resolve(this ITypeShapeProvider typeShapeProvider, Type type) => typeShapeProvider.GetTypeShape(type, throwIfMissing: true)!;
}
