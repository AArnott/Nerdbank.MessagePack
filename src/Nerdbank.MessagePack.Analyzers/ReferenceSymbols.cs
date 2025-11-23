// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Nerdbank.MessagePack.Analyzers;

public record ReferenceSymbols(
	INamedTypeSymbol MessagePackSerializer,
	INamedTypeSymbol MessagePackConverter,
	INamedTypeSymbol MessagePackConverterNonGeneric,
	INamedTypeSymbol MessagePackConverterAttribute,
	INamedTypeSymbol MessagePackReader,
	INamedTypeSymbol MessagePackStreamingReader,
	INamedTypeSymbol MessagePackWriter,
	INamedTypeSymbol KeyAttribute,
	INamedTypeSymbol UnusedDataPacket,
	INamedTypeSymbol DerivedTypeShapeAttribute,
	INamedTypeSymbol GenerateShapeAttribute,
	INamedTypeSymbol GenerateShapeForAttribute,
	INamedTypeSymbol GenerateShapeForGenericAttribute,
	INamedTypeSymbol PropertyShapeAttribute,
	INamedTypeSymbol ConstructorShapeAttribute,
	INamedTypeSymbol UseComparerAttribute)
{
	public INamedTypeSymbol MessagePackConverterUnbound { get; } = MessagePackConverter.ConstructUnboundGenericType();

	public static bool TryCreate(Compilation compilation, [NotNullWhen(true)] out ReferenceSymbols? referenceSymbols)
	{
		IAssemblySymbol libraryAssembly;
		if (compilation.AssemblyName == "Nerdbank.MessagePack")
		{
			libraryAssembly = compilation.Assembly;
		}
		else if (compilation.ExternalReferences.FirstOrDefault(r => string.Equals(Path.GetFileName(r.Display), "Nerdbank.MessagePack.dll", StringComparison.OrdinalIgnoreCase)) is MetadataReference libraryReference &&
			compilation.GetAssemblyOrModuleSymbol(libraryReference) is IAssemblySymbol assembly)
		{
			libraryAssembly = assembly;
		}
		else
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

		INamedTypeSymbol? messagePackConverterNonGeneric = libraryAssembly.GetTypeByMetadataName("Nerdbank.MessagePack.MessagePackConverter");
		if (messagePackConverterNonGeneric is null)
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

		INamedTypeSymbol? messagePackStreamingReader = libraryAssembly.GetTypeByMetadataName("Nerdbank.MessagePack.MessagePackStreamingReader");
		if (messagePackStreamingReader is null)
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

		INamedTypeSymbol? unusedDataPacket = libraryAssembly.GetTypeByMetadataName("Nerdbank.MessagePack.UnusedDataPacket");
		if (unusedDataPacket is null)
		{
			referenceSymbols = null;
			return false;
		}

		INamedTypeSymbol? useComparerAttribute = libraryAssembly.GetTypeByMetadataName("Nerdbank.MessagePack.UseComparerAttribute");
		if (useComparerAttribute is null)
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

		INamedTypeSymbol? generateShapeForAttribute = polytypeAssembly.GetTypeByMetadataName("PolyType.GenerateShapeForAttribute");
		if (generateShapeForAttribute is null)
		{
			referenceSymbols = null;
			return false;
		}

		INamedTypeSymbol? generateShapeForGenericAttribute = polytypeAssembly.GetTypeByMetadataName("PolyType.GenerateShapeForAttribute`1")?.ConstructUnboundGenericType();
		if (generateShapeForGenericAttribute is null)
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

		INamedTypeSymbol? constructorShapeAttribute = polytypeAssembly.GetTypeByMetadataName("PolyType.ConstructorShapeAttribute");
		if (constructorShapeAttribute is null)
		{
			referenceSymbols = null;
			return false;
		}

		INamedTypeSymbol? derivedTypeShapeAttribute = polytypeAssembly.GetTypeByMetadataName("PolyType.DerivedTypeShapeAttribute");
		if (derivedTypeShapeAttribute is null)
		{
			referenceSymbols = null;
			return false;
		}

		referenceSymbols = new ReferenceSymbols(
			messagePackSerializer,
			messagePackConverter,
			messagePackConverterNonGeneric,
			messagePackConverterAttribute,
			messagePackReader,
			messagePackStreamingReader,
			messagePackWriter,
			keyAttribute,
			unusedDataPacket,
			derivedTypeShapeAttribute,
			generateShapeAttribute,
			generateShapeForAttribute,
			generateShapeForGenericAttribute,
			propertyShapeAttribute,
			constructorShapeAttribute,
			useComparerAttribute);
		return true;
	}
}
