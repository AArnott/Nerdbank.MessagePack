// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Nerdbank.MessagePack.Analyzers;

public record ReferenceSymbols(
	INamedTypeSymbol MessagePackSerializer,
	INamedTypeSymbol MessagePackConverter,
	INamedTypeSymbol MessagePackConverterAttribute,
	INamedTypeSymbol MessagePackReader,
	INamedTypeSymbol MessagePackWriter,
	INamedTypeSymbol KeyAttribute,
	INamedTypeSymbol KnownSubTypeAttribute,
	INamedTypeSymbol GenerateShapeAttribute,
	INamedTypeSymbol PropertyShapeAttribute)
{
	public INamedTypeSymbol MessagePackConverterUnbound { get; } = MessagePackConverter.ConstructUnboundGenericType();

	public static bool TryCreate(Compilation compilation, [NotNullWhen(true)] out ReferenceSymbols? referenceSymbols)
	{
		if (compilation.ExternalReferences.FirstOrDefault(r => string.Equals(Path.GetFileName(r.Display), "Nerdbank.MessagePack.dll", StringComparison.OrdinalIgnoreCase)) is not MetadataReference libraryReference ||
			compilation.GetAssemblyOrModuleSymbol(libraryReference) is not IAssemblySymbol libraryAssembly)
		{
			referenceSymbols = null;
			return false;
		}

		INamedTypeSymbol? messagePackSerializer = libraryAssembly.GetTypeByMetadataName("Nerdbank.MessagePack.MessagePackSerializer");
		if (messagePackSerializer is null)
		{
			referenceSymbols = null;
			return false;
		}

		INamedTypeSymbol? messagePackConverter = libraryAssembly.GetTypeByMetadataName("Nerdbank.MessagePack.MessagePackConverter`1");
		if (messagePackConverter is null)
		{
			referenceSymbols = null;
			return false;
		}

		INamedTypeSymbol? messagePackConverterAttribute = libraryAssembly.GetTypeByMetadataName("Nerdbank.MessagePack.MessagePackConverterAttribute");
		if (messagePackConverterAttribute is null)
		{
			referenceSymbols = null;
			return false;
		}

		INamedTypeSymbol? messagePackReader = libraryAssembly.GetTypeByMetadataName("Nerdbank.MessagePack.MessagePackReader");
		if (messagePackReader is null)
		{
			referenceSymbols = null;
			return false;
		}

		INamedTypeSymbol? messagePackWriter = libraryAssembly.GetTypeByMetadataName("Nerdbank.MessagePack.MessagePackWriter");
		if (messagePackWriter is null)
		{
			referenceSymbols = null;
			return false;
		}

		INamedTypeSymbol? keyAttribute = libraryAssembly.GetTypeByMetadataName("Nerdbank.MessagePack.KeyAttribute");
		if (keyAttribute is null)
		{
			referenceSymbols = null;
			return false;
		}

		INamedTypeSymbol? knownSubTypeAttribute = libraryAssembly.GetTypeByMetadataName("Nerdbank.MessagePack.KnownSubTypeAttribute");
		if (knownSubTypeAttribute is null)
		{
			referenceSymbols = null;
			return false;
		}

		if (compilation.ExternalReferences.FirstOrDefault(r => string.Equals(Path.GetFileName(r.Display), "PolyType.dll", StringComparison.OrdinalIgnoreCase)) is not MetadataReference polytypeReference ||
			compilation.GetAssemblyOrModuleSymbol(polytypeReference) is not IAssemblySymbol polytypeAssembly)
		{
			referenceSymbols = null;
			return false;
		}

		INamedTypeSymbol? generateShapeAttribute = polytypeAssembly.GetTypeByMetadataName("PolyType.GenerateShapeAttribute");
		if (generateShapeAttribute is null)
		{
			referenceSymbols = null;
			return false;
		}

		INamedTypeSymbol? propertyShapeAttribute = polytypeAssembly.GetTypeByMetadataName("PolyType.PropertyShapeAttribute");
		if (propertyShapeAttribute is null)
		{
			referenceSymbols = null;
			return false;
		}

		referenceSymbols = new ReferenceSymbols(
			messagePackSerializer,
			messagePackConverter,
			messagePackConverterAttribute,
			messagePackReader,
			messagePackWriter,
			keyAttribute,
			knownSubTypeAttribute,
			generateShapeAttribute,
			propertyShapeAttribute);
		return true;
	}
}
