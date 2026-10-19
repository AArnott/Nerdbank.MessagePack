// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft;

namespace Nerdbank.MessagePack;

/// <summary>
/// Supplies C# union case identifiers using a delegate that receives the union and payload types.
/// </summary>
/// <param name="resolver">The thread-safe resolver, whose results must remain consistent while the provider is in use.</param>
public sealed class DelegateUnionTypeAliasProvider(UnionCaseAliasResolver resolver) : IUnionTypeAliasProvider
{
	private readonly UnionCaseAliasResolver resolver = Requires.NotNull(resolver);

	/// <inheritdoc/>
	public DerivedTypeIdentifier? GetAlias(IUnionTypeShape unionShape, IUnionCaseShape caseShape)
	{
		Requires.NotNull(unionShape);
		Requires.NotNull(caseShape);
		return this.resolver(unionShape.Type, caseShape.UnionCaseType.Type);
	}
}
