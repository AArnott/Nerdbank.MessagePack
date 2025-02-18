// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;

namespace ShapeShift;

/// <summary>
/// A non-generic accessor for <see cref="KnownSubTypeMapping{TBase}"/>
/// so that multiple such mappings can be stored in a collection and retrieved later.
/// </summary>
internal interface IKnownSubTypeMapping
{
	/// <summary>
	/// Constructs a read-only dictionary of sub-types, keyed by their aliases.
	/// </summary>
	/// <returns>A collection of sub-types and aliases.</returns>
	/// <remarks>
	/// It is not strictly required that the implementation guarantee that each type is unique,
	/// because the requirement for uniqueness is enforced later when the known sub-type converter is initialized.
	/// </remarks>
	FrozenDictionary<SubTypeAlias, ITypeShape> CreateSubTypesMapping();
}
