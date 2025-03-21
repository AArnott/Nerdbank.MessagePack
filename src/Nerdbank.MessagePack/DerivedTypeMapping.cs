// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;

namespace Nerdbank.MessagePack;

/// <summary>
/// A non-generic abstract base class for <see cref="DerivedTypeMapping{TBase}"/>.
/// </summary>
/// <remarks>
/// All users of this class should create an instance of the generic derived <see cref="DerivedTypeMapping{TBase}"/> class instead.
/// </remarks>
public abstract class DerivedTypeMapping : IDerivedTypeMapping
{
	/// <summary>
	/// Gets the base union type.
	/// </summary>
	internal abstract Type BaseType { get; }

	/// <inheritdoc/>
	FrozenDictionary<DerivedTypeIdentifier, ITypeShape> IDerivedTypeMapping.CreateDerivedTypesMapping() => this.CreateDerivedTypesMapping();

	/// <inheritdoc cref="IDerivedTypeMapping.CreateDerivedTypesMapping"/>
	internal abstract FrozenDictionary<DerivedTypeIdentifier, ITypeShape> CreateDerivedTypesMapping();
}
