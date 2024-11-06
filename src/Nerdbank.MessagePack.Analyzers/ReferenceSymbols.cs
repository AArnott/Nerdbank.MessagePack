// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Nerdbank.MessagePack.Analyzers;

internal record ReferenceSymbols(
	INamedTypeSymbol MessagePackConverter,
	INamedTypeSymbol MessagePackConverterAttribute,
	INamedTypeSymbol KeyAttribute,
	INamedTypeSymbol KnownSubTypeAttribute,
	INamedTypeSymbol PropertyShapeAttribute)
{
	internal static bool TryCreate(Compilation compilation, [NotNullWhen(true)] out ReferenceSymbols? referenceSymbols)
	{
		if (!compilation.ReferencedAssemblyNames.Any(id => id.Name == "Nerdbank.MessagePack"))
		{
			referenceSymbols = null;
			return false;
		}

		INamedTypeSymbol? messagePackConverter = compilation.GetTypeByMetadataName("Nerdbank.MessagePack.MessagePackConverter`1");
		if (messagePackConverter is null)
		{
			referenceSymbols = null;
			return false;
		}

		INamedTypeSymbol? messagePackConverterAttribute = compilation.GetTypeByMetadataName("Nerdbank.MessagePack.MessagePackConverterAttribute");
		if (messagePackConverterAttribute is null)
		{
			referenceSymbols = null;
			return false;
		}

		INamedTypeSymbol? keyAttribute = compilation.GetTypeByMetadataName("Nerdbank.MessagePack.KeyAttribute");
		if (keyAttribute is null)
		{
			referenceSymbols = null;
			return false;
		}

		INamedTypeSymbol? knownSubTypeAttribute = compilation.GetTypeByMetadataName("Nerdbank.MessagePack.KnownSubTypeAttribute");
		if (knownSubTypeAttribute is null)
		{
			referenceSymbols = null;
			return false;
		}

		INamedTypeSymbol? propertyShapeAttribute = compilation.GetTypeByMetadataName("TypeShape.PropertyShapeAttribute");
		if (propertyShapeAttribute is null)
		{
			referenceSymbols = null;
			return false;
		}

		referenceSymbols = new ReferenceSymbols(messagePackConverter, messagePackConverterAttribute, keyAttribute, knownSubTypeAttribute, propertyShapeAttribute);
		return true;
	}
}
