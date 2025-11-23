// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using PolyType.Roslyn;

namespace Nerdbank.MessagePack.Analyzers;

internal class PolyTypeShapeSynthesis : TypeDataModelGenerator
{
	public PolyTypeShapeSynthesis(ISymbol generationScope, KnownSymbols knownSymbols, CancellationToken cancellationToken)
		: base(generationScope, knownSymbols, cancellationToken)
	{
	}

	// TODO: Override a bunch of methods to match PolyType's default behavior.
}
