// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Nerdbank.MessagePack.Analyzers;

public record MessagePackCSharpReferenceSymbols(
	INamedTypeSymbol MessagePackSerializer,
	INamedTypeSymbol IMessagePackFormatterOfT,
	IMethodSymbol IMessagePackFormatterSerialize,
	IMethodSymbol IMessagePackFormatterDeserialize,
	INamedTypeSymbol MessagePackFormatterAttribute,
	INamedTypeSymbol MessagePackObjectAttribute,
	INamedTypeSymbol KeyAttribute,
	INamedTypeSymbol IgnoreMemberAttribute,
	INamedTypeSymbol SerializationConstructorAttribute,
	INamedTypeSymbol IMessagePackSerializationCallbackReceiver,
	INamedTypeSymbol MessagePackSecurity,
	IMethodSymbol DepthStep,
	INamedTypeSymbol MessagePackReader,
	IPropertySymbol ReaderDepth,
	IMethodSymbol ReaderSkip,
	INamedTypeSymbol IFormatterResolver,
	IMethodSymbol GetFormatterWithVerify,
	IMethodSymbol GetFormatter)
{
	public INamedTypeSymbol IMessagePackFormatterOfTUnbound => this.IMessagePackFormatterOfT.ConstructUnboundGenericType();

	public static bool TryCreate(Compilation compilation, [NotNullWhen(true)] out MessagePackCSharpReferenceSymbols? referenceSymbols)
	{
		if (compilation.ExternalReferences.FirstOrDefault(r => string.Equals(Path.GetFileName(r.Display), "MessagePack.dll", StringComparison.OrdinalIgnoreCase)) is not MetadataReference oldLibraryReference ||
			compilation.GetAssemblyOrModuleSymbol(oldLibraryReference) is not IAssemblySymbol oldLibraryAssembly)
		{
			referenceSymbols = null;
			return false;
		}

		if (compilation.ExternalReferences.FirstOrDefault(r => string.Equals(Path.GetFileName(r.Display), "MessagePack.Annotations.dll", StringComparison.OrdinalIgnoreCase)) is not MetadataReference oldLibraryAnnotationsReference ||
			compilation.GetAssemblyOrModuleSymbol(oldLibraryAnnotationsReference) is not IAssemblySymbol oldLibraryAnnotationsAssembly)
		{
			referenceSymbols = null;
			return false;
		}

		INamedTypeSymbol? messagePackSerializer = oldLibraryAssembly.GetTypeByMetadataName("MessagePack.MessagePackSerializer");
		if (messagePackSerializer is null)
		{
			referenceSymbols = null;
			return false;
		}

		INamedTypeSymbol? messagePackFormatterOfT = oldLibraryAssembly.GetTypeByMetadataName("MessagePack.Formatters.IMessagePackFormatter`1");
		if (messagePackFormatterOfT is null)
		{
			referenceSymbols = null;
			return false;
		}

		IMethodSymbol? formatterSerialize = (IMethodSymbol?)messagePackFormatterOfT.GetMembers("Serialize").SingleOrDefault();
		if (formatterSerialize is null)
		{
			referenceSymbols = null;
			return false;
		}

		IMethodSymbol? formatterDeserialize = (IMethodSymbol?)messagePackFormatterOfT.GetMembers("Deserialize").SingleOrDefault();
		if (formatterDeserialize is null)
		{
			referenceSymbols = null;
			return false;
		}

		INamedTypeSymbol? messagePackFormatterAttribute = oldLibraryAnnotationsAssembly.GetTypeByMetadataName("MessagePack.MessagePackFormatterAttribute");
		if (messagePackFormatterAttribute is null)
		{
			referenceSymbols = null;
			return false;
		}

		INamedTypeSymbol? messagePackObjectAttribute = oldLibraryAnnotationsAssembly.GetTypeByMetadataName("MessagePack.MessagePackObjectAttribute");
		if (messagePackObjectAttribute is null)
		{
			referenceSymbols = null;
			return false;
		}

		INamedTypeSymbol? keyAttribute = oldLibraryAnnotationsAssembly.GetTypeByMetadataName("MessagePack.KeyAttribute");
		if (keyAttribute is null)
		{
			referenceSymbols = null;
			return false;
		}

		INamedTypeSymbol? ignoreMemberAttribute = oldLibraryAnnotationsAssembly.GetTypeByMetadataName("MessagePack.IgnoreMemberAttribute");
		if (ignoreMemberAttribute is null)
		{
			referenceSymbols = null;
			return false;
		}

		INamedTypeSymbol? serializationConstructorAttribute = oldLibraryAnnotationsAssembly.GetTypeByMetadataName("MessagePack.SerializationConstructorAttribute");
		if (serializationConstructorAttribute is null)
		{
			referenceSymbols = null;
			return false;
		}

		INamedTypeSymbol? messagePackSerializationCallbackReceiver = oldLibraryAnnotationsAssembly.GetTypeByMetadataName("MessagePack.IMessagePackSerializationCallbackReceiver");
		if (messagePackSerializationCallbackReceiver is null)
		{
			referenceSymbols = null;
			return false;
		}

		INamedTypeSymbol? messagePackSecurity = oldLibraryAssembly.GetTypeByMetadataName("MessagePack.MessagePackSecurity");
		if (messagePackSecurity is null)
		{
			referenceSymbols = null;
			return false;
		}

		IMethodSymbol? depthStep = messagePackSecurity.GetMembers("DepthStep").OfType<IMethodSymbol>().FirstOrDefault();
		if (depthStep is null)
		{
			referenceSymbols = null;
			return false;
		}

		INamedTypeSymbol? reader = oldLibraryAssembly.GetTypeByMetadataName("MessagePack.MessagePackReader");
		if (reader is null)
		{
			referenceSymbols = null;
			return false;
		}

		IPropertySymbol? readerDepth = reader.GetMembers("Depth").OfType<IPropertySymbol>().FirstOrDefault();
		if (readerDepth is null)
		{
			referenceSymbols = null;
			return false;
		}

		IMethodSymbol? readerSkip = reader.GetMembers("Skip").OfType<IMethodSymbol>().FirstOrDefault();
		if (readerSkip is null)
		{
			referenceSymbols = null;
			return false;
		}

		INamedTypeSymbol? resolver = oldLibraryAssembly.GetTypeByMetadataName("MessagePack.IFormatterResolver");
		if (resolver is null)
		{
			referenceSymbols = null;
			return false;
		}

		IMethodSymbol? getFormatter = (IMethodSymbol?)resolver.GetMembers("GetFormatter").SingleOrDefault();
		if (getFormatter is null)
		{
			referenceSymbols = null;
			return false;
		}

		INamedTypeSymbol? resolverExtensions = oldLibraryAssembly.GetTypeByMetadataName("MessagePack.FormatterResolverExtensions");
		if (resolverExtensions is null)
		{
			referenceSymbols = null;
			return false;
		}

		IMethodSymbol? getFormatterWithVerify = (IMethodSymbol?)resolverExtensions.GetMembers("GetFormatterWithVerify").SingleOrDefault();
		if (getFormatterWithVerify is null)
		{
			referenceSymbols = null;
			return false;
		}

		referenceSymbols = new(
			messagePackSerializer,
			messagePackFormatterOfT,
			formatterSerialize,
			formatterDeserialize,
			messagePackFormatterAttribute,
			messagePackObjectAttribute,
			keyAttribute,
			ignoreMemberAttribute,
			serializationConstructorAttribute,
			messagePackSerializationCallbackReceiver,
			messagePackSecurity,
			depthStep,
			reader,
			readerDepth,
			readerSkip,
			resolver,
			getFormatterWithVerify,
			getFormatter);
		return true;
	}
}
