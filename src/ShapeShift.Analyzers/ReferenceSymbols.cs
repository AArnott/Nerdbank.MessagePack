// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace ShapeShift.Analyzers;

public record ReferenceSymbols(
	INamedTypeSymbol MessagePackSerializer,
	INamedTypeSymbol Converter,
	INamedTypeSymbol ConverterNonGeneric,
	INamedTypeSymbol ConverterAttribute,
	INamedTypeSymbol Reader,
	INamedTypeSymbol StreamingReader,
	INamedTypeSymbol Writer,
	INamedTypeSymbol KeyAttribute,
	INamedTypeSymbol KnownSubTypeAttribute,
	INamedTypeSymbol GenerateShapeAttribute,
	INamedTypeSymbol PropertyShapeAttribute,
	INamedTypeSymbol ConstructorShapeAttribute)
{
	public INamedTypeSymbol MessagePackConverterUnbound { get; } = Converter.ConstructUnboundGenericType();

	public static bool TryCreate(Compilation compilation, [NotNullWhen(true)] out ReferenceSymbols? referenceSymbols)
	{
		IAssemblySymbol libraryAssembly;
		if (compilation.AssemblyName == "ShapeShift")
		{
			libraryAssembly = compilation.Assembly;
		}
		else if (compilation.ExternalReferences.FirstOrDefault(r => string.Equals(Path.GetFileName(r.Display), "ShapeShift.dll", StringComparison.OrdinalIgnoreCase)) is MetadataReference libraryReference &&
			compilation.GetAssemblyOrModuleSymbol(libraryReference) is IAssemblySymbol assembly)
		{
			libraryAssembly = assembly;
		}
		else
		{
			referenceSymbols = null;
			return false;
		}

		INamedTypeSymbol? messagePackSerializer = libraryAssembly.GetTypeByMetadataName("ShapeShift.MessagePackSerializer");
		if (messagePackSerializer is null)
		{
			referenceSymbols = null;
			return false;
		}

		INamedTypeSymbol? converter = libraryAssembly.GetTypeByMetadataName("ShapeShift.Converters.Converter`1");
		if (converter is null)
		{
			referenceSymbols = null;
			return false;
		}

		INamedTypeSymbol? converterNonGeneric = libraryAssembly.GetTypeByMetadataName("ShapeShift.Converters.Converter");
		if (converterNonGeneric is null)
		{
			referenceSymbols = null;
			return false;
		}

		INamedTypeSymbol? converterAttribute = libraryAssembly.GetTypeByMetadataName("ShapeShift.ConverterAttribute");
		if (converterAttribute is null)
		{
			referenceSymbols = null;
			return false;
		}

		INamedTypeSymbol? reader = libraryAssembly.GetTypeByMetadataName("ShapeShift.Converters.Reader");
		if (reader is null)
		{
			referenceSymbols = null;
			return false;
		}

		INamedTypeSymbol? streamingReader = libraryAssembly.GetTypeByMetadataName("ShapeShift.Converters.StreamingReader");
		if (streamingReader is null)
		{
			referenceSymbols = null;
			return false;
		}

		INamedTypeSymbol? writer = libraryAssembly.GetTypeByMetadataName("ShapeShift.Converters.Writer");
		if (writer is null)
		{
			referenceSymbols = null;
			return false;
		}

		INamedTypeSymbol? keyAttribute = libraryAssembly.GetTypeByMetadataName("ShapeShift.KeyAttribute");
		if (keyAttribute is null)
		{
			referenceSymbols = null;
			return false;
		}

		INamedTypeSymbol? knownSubTypeAttribute = libraryAssembly.GetTypeByMetadataName("ShapeShift.KnownSubTypeAttribute");
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

		INamedTypeSymbol? constructorShapeAttribute = polytypeAssembly.GetTypeByMetadataName("PolyType.ConstructorShapeAttribute");
		if (constructorShapeAttribute is null)
		{
			referenceSymbols = null;
			return false;
		}

		referenceSymbols = new ReferenceSymbols(
			messagePackSerializer,
			converter,
			converterNonGeneric,
			converterAttribute,
			reader,
			streamingReader,
			writer,
			keyAttribute,
			knownSubTypeAttribute,
			generateShapeAttribute,
			propertyShapeAttribute,
			constructorShapeAttribute);
		return true;
	}
}
