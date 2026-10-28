// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack;

/// <summary>
/// Supplies custom identifiers for cases of C# unions.
/// </summary>
/// <remarks>
/// Implementations must be thread-safe and return consistent results for the lifetime of a serializer that uses them.
/// Aliases are resolved while constructing converters, not for each serialized value.
/// </remarks>
public interface IUnionTypeAliasProvider
{
	/// <summary>
	/// Gets a custom identifier for a union case.
	/// </summary>
	/// <param name="unionShape">The shape of the containing union.</param>
	/// <param name="caseShape">A case belonging to <paramref name="unionShape"/>.</param>
	/// <returns>The custom identifier, or <see langword="null"/> to use the normal identifier.</returns>
	DerivedTypeIdentifier? GetAlias(IUnionTypeShape unionShape, IUnionCaseShape caseShape);
}
