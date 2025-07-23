// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack.Analyzers;

/// <summary>
/// Analyzes uses of the <c>UseComparerAttribute</c> to ensure that it is applied correctly.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UseComparerAttributeAnalyzer : DiagnosticAnalyzer
{
	public const string OpenGenericTypeDiagnosticId = "NBMsgPack070";
	public const string InvalidMemberDiagnosticId = "NBMsgPack071";
	public const string IncompatibleComparerDiagnosticId = "NBMsgPack072";
	public const string AbstractTypeDiagnosticId = "NBMsgPack073";

	public static readonly DiagnosticDescriptor OpenGenericTypeDescriptor = new(
		id: OpenGenericTypeDiagnosticId,
		title: Strings.NBMsgPack070_Title,
		messageFormat: Strings.NBMsgPack070_MessageFormat,
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		helpLinkUri: AnalyzerUtilities.GetHelpLink(OpenGenericTypeDiagnosticId));

	public static readonly DiagnosticDescriptor InvalidMemberDescriptor = new(
		id: InvalidMemberDiagnosticId,
		title: Strings.NBMsgPack071_Title,
		messageFormat: Strings.NBMsgPack071_MessageFormat,
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		helpLinkUri: AnalyzerUtilities.GetHelpLink(InvalidMemberDiagnosticId));

	public static readonly DiagnosticDescriptor IncompatibleComparerDescriptor = new(
		id: IncompatibleComparerDiagnosticId,
		title: Strings.NBMsgPack072_Title,
		messageFormat: Strings.NBMsgPack072_MessageFormat,
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		helpLinkUri: AnalyzerUtilities.GetHelpLink(IncompatibleComparerDiagnosticId));

	public static readonly DiagnosticDescriptor AbstractTypeDescriptor = new(
		id: AbstractTypeDiagnosticId,
		title: Strings.NBMsgPack073_Title,
		messageFormat: Strings.NBMsgPack073_MessageFormat,
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		helpLinkUri: AnalyzerUtilities.GetHelpLink(AbstractTypeDiagnosticId));

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
		OpenGenericTypeDescriptor,
		InvalidMemberDescriptor,
		IncompatibleComparerDescriptor,
		AbstractTypeDescriptor,
	];

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

		context.RegisterCompilationStartAction(context =>
		{
			if (!ReferenceSymbols.TryCreate(context.Compilation, out ReferenceSymbols? referenceSymbols))
			{
				return;
			}

			context.RegisterSymbolAction(
				context => AnalyzeUseComparerAttributeUsage(context, referenceSymbols),
				SymbolKind.Property,
				SymbolKind.Field,
				SymbolKind.Parameter);
		});
	}

	private static void AnalyzeUseComparerAttributeUsage(SymbolAnalysisContext context, ReferenceSymbols referenceSymbols)
	{
		ISymbol symbol = context.Symbol;

		foreach (AttributeData attribute in symbol.FindAttributes(referenceSymbols.UseComparerAttribute))
		{
			if (attribute.ConstructorArguments.Length == 0)
			{
				continue;
			}

			// Get the comparer type from the first argument
			if (attribute.ConstructorArguments[0].Value is not INamedTypeSymbol comparerType)
			{
				continue;
			}

			// Check if the type is an open generic
			if (comparerType.IsUnboundGenericType)
			{
				Location? location = AnalyzerUtilities.GetArgumentLocation(attribute, 0, context.CancellationToken) ??
					attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation();
				if (location is not null)
				{
					context.ReportDiagnostic(Diagnostic.Create(OpenGenericTypeDescriptor, location, comparerType.Name));
				}

				continue;
			}

			// Get the member name from the second argument if it exists
			string? memberName = null;
			if (attribute.ConstructorArguments.Length > 1 && attribute.ConstructorArguments[1].Value is string memberNameValue)
			{
				memberName = memberNameValue;
			}

			// Check if the type is abstract and no member is specified
			if (comparerType.IsAbstract && memberName is null)
			{
				Location? location = AnalyzerUtilities.GetArgumentLocation(attribute, 0, context.CancellationToken) ??
					attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation();
				if (location is not null)
				{
					context.ReportDiagnostic(Diagnostic.Create(AbstractTypeDescriptor, location, comparerType.Name));
				}

				continue;
			}

			// Validate the member if specified
			IPropertySymbol? memberProperty = null;
			if (memberName is not null)
			{
				memberProperty = comparerType.GetMembers(memberName).OfType<IPropertySymbol>().FirstOrDefault();
				if (memberProperty is not { DeclaredAccessibility: Accessibility.Public })
				{
					Location? location = AnalyzerUtilities.GetArgumentLocation(attribute, 1, context.CancellationToken) ??
						attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation();
					if (location is not null)
					{
						context.ReportDiagnostic(Diagnostic.Create(InvalidMemberDescriptor, location, memberName, comparerType.Name));
					}

					continue;
				}

				// If the type is abstract, ensure the member is static
				if (comparerType.IsAbstract && !memberProperty.IsStatic)
				{
					Location? location = AnalyzerUtilities.GetArgumentLocation(attribute, 1, context.CancellationToken) ??
						attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation();
					if (location is not null)
					{
						context.ReportDiagnostic(Diagnostic.Create(InvalidMemberDescriptor, location, memberName, comparerType.Name));
					}

					continue;
				}
			}

			// Validate that the comparer type implements the correct interface
			ITypeSymbol? comparerImplementationType = memberName is not null ? memberProperty?.Type : comparerType;
			ITypeSymbol? collectionElementType = GetCollectionElementType(symbol);
			if (comparerImplementationType is not null && collectionElementType is not null && !IsValidComparerType(comparerImplementationType, collectionElementType))
			{
				Location? location = attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation();
				if (location is not null)
				{
					context.ReportDiagnostic(Diagnostic.Create(IncompatibleComparerDescriptor, location));
				}
			}
		}
	}

	private static bool IsValidComparerType(ITypeSymbol comparerType, ITypeSymbol elementType)
	{
		if (IsValidType(comparerType))
		{
			return true;
		}

		// Check if the comparer implements IComparer<T> or IEqualityComparer<T>
		foreach (INamedTypeSymbol interfaceType in comparerType.AllInterfaces)
		{
			if (IsValidType(interfaceType))
			{
				return true;
			}
		}

		return false;

		bool IsValidType(ITypeSymbol type)
		{
			if (type is INamedTypeSymbol { TypeKind: TypeKind.Interface, TypeArguments: [{ } typeArg], Name: "IComparer" or "IEqualityComparer" } &&
				SymbolEqualityComparer.Default.Equals(typeArg, elementType))
			{
				return true;
			}

			return false;
		}
	}

	private static ITypeSymbol? GetCollectionElementType(ISymbol symbol)
	{
		ITypeSymbol? symbolType = symbol switch
		{
			IPropertySymbol property => property.Type,
			IFieldSymbol field => field.Type,
			IParameterSymbol parameter => parameter.Type,
			_ => null,
		};

		// This is a cheap check, as it assumes generic collections are used.
		// To be more thorough, we'd want to look at interfaces that the symbol implements like IReadOnlyDictionary<,> and IEnumerable<,>,
		// allowing for IEnumerable<KeyValuePair<K, V>> as well.
		return symbolType switch
		{
			INamedTypeSymbol { TypeArguments: [{ } key] } => key,
			INamedTypeSymbol { TypeArguments: [{ } key, _] } => key, // For dictionaries, we only care about the key type
			_ => null,
		};
	}
}
