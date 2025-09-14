// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;

namespace Nerdbank.MessagePack;

/// <summary>
/// A non-generic abstract base class for <see cref="DerivedShapeMapping{TBase}"/>.
/// </summary>
/// <remarks>
/// All users of this class should create an instance of the generic derived <see cref="DerivedShapeMapping{TBase}"/> class instead.
/// </remarks>
public abstract class DerivedTypeMapping : IDerivedTypeMapping
{
	/// <summary>
	/// Gets the base union type.
	/// </summary>
	public abstract Type BaseType { get; }

	/// <summary>
	/// Gets a value indicating whether the union behavior is disabled on the <see cref="BaseType"/>.
	/// </summary>
	/// <value>The default value is <see langword="false"/>.</value>
	/// <remarks>
	/// As the default behavior is non-union behavior anyway,
	/// this property is primarily useful for forcing the serializer to ignore any and all
	/// <see cref="DerivedTypeShapeAttribute"/> that may be present on the <see cref="BaseType"/>.
	/// </remarks>
	public bool Disabled { get; init; }

	/// <inheritdoc/>
	FrozenDictionary<DerivedTypeIdentifier, ITypeShape> IDerivedTypeMapping.CreateDerivedTypesMapping() => this.CreateDerivedTypesMapping();

	/// <inheritdoc cref="IDerivedTypeMapping.CreateDerivedTypesMapping"/>
	internal abstract FrozenDictionary<DerivedTypeIdentifier, ITypeShape> CreateDerivedTypesMapping();
}
