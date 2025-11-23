// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Nerdbank.MessagePack.Analyzers;

/// <summary>
/// Analyzer that detects fields and properties with initializers but no DefaultValueAttribute
/// on types that have source-generated shapes.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DefaultValueInitializerAnalyzer : DiagnosticAnalyzer
{
	public const string MissingDefaultValueAttributeDiagnosticId = "NBMsgPack110";

	public static readonly DiagnosticDescriptor MissingDefaultValueAttributeDescriptor = new(
		id: MissingDefaultValueAttributeDiagnosticId,
		title: Strings.NBMsgPack110_Title,
		messageFormat: Strings.NBMsgPack110_MessageFormat,
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		helpLinkUri: AnalyzerUtilities.GetHelpLink(MissingDefaultValueAttributeDiagnosticId));

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
		MissingDefaultValueAttributeDescriptor,
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

			// Get the DefaultValue attribute symbol
			INamedTypeSymbol? defaultValueAttribute = context.Compilation.GetTypeByMetadataName("System.ComponentModel.DefaultValueAttribute");
			if (defaultValueAttribute is null)
			{
				return;
			}

			context.RegisterSymbolAction(
				symbolContext =>
				{
					this.AnalyzeType(symbolContext, referenceSymbols, defaultValueAttribute);
				},
				SymbolKind.NamedType);
		});
	}

	private void AnalyzeType(SymbolAnalysisContext context, ReferenceSymbols referenceSymbols, INamedTypeSymbol defaultValueAttribute)
	{
		INamedTypeSymbol typeSymbol = (INamedTypeSymbol)context.Symbol;

		// Check if this type has a GenerateShapeAttribute
		if (!typeSymbol.GetAttributes().Any(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, referenceSymbols.GenerateShapeAttribute)))
		{
			return;
		}

		// Check all fields and properties
		foreach (ISymbol member in typeSymbol.GetMembers())
		{
			if (member is IFieldSymbol field)
			{
				this.AnalyzeField(context, field, defaultValueAttribute, referenceSymbols);
			}
			else if (member is IPropertySymbol property)
			{
				this.AnalyzeProperty(context, property, defaultValueAttribute, referenceSymbols);
			}
		}
	}

	private void AnalyzeField(SymbolAnalysisContext context, IFieldSymbol field, INamedTypeSymbol defaultValueAttribute, ReferenceSymbols referenceSymbols)
	{
		// Skip static, const, or compiler-generated fields
		if (field.IsStatic || field.IsConst || field.IsImplicitlyDeclared)
		{
			return;
		}

		// Check if the field is marked to be ignored
		if (field.GetAttributes().Any(attr =>
			SymbolEqualityComparer.Default.Equals(attr.AttributeClass, referenceSymbols.PropertyShapeAttribute) &&
			attr.NamedArguments.Any(na => na.Key == Constants.PropertyShapeAttribute.IgnoreProperty && na.Value.Value is true)))
		{
			return;
		}

		// Check if field has an initializer
		if (!this.HasInitializer(field))
		{
			return;
		}

		// Check if field already has DefaultValueAttribute
		if (field.GetAttributes().Any(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, defaultValueAttribute)))
		{
			return;
		}

		// Report diagnostic
		Location location = field.Locations.FirstOrDefault() ?? Location.None;
		context.ReportDiagnostic(Diagnostic.Create(MissingDefaultValueAttributeDescriptor, location, field.Name));
	}

	private void AnalyzeProperty(SymbolAnalysisContext context, IPropertySymbol property, INamedTypeSymbol defaultValueAttribute, ReferenceSymbols referenceSymbols)
	{
		// Skip static, indexers, or compiler-generated properties
		if (property.IsStatic || property.IsIndexer || property.IsImplicitlyDeclared)
		{
			return;
		}

		// Check if the property is marked to be ignored
		if (property.GetAttributes().Any(attr =>
			SymbolEqualityComparer.Default.Equals(attr.AttributeClass, referenceSymbols.PropertyShapeAttribute) &&
			attr.NamedArguments.Any(na => na.Key == Constants.PropertyShapeAttribute.IgnoreProperty && na.Value.Value is true)))
		{
			return;
		}

		// Check if property has an initializer
		if (!this.HasInitializer(property))
		{
			return;
		}

		// Check if property already has DefaultValueAttribute
		if (property.GetAttributes().Any(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, defaultValueAttribute)))
		{
			return;
		}

		// Report diagnostic
		Location location = property.Locations.FirstOrDefault() ?? Location.None;
		context.ReportDiagnostic(Diagnostic.Create(MissingDefaultValueAttributeDescriptor, location, property.Name));
	}

	private bool HasInitializer(ISymbol symbol)
	{
		// Check if the symbol has a syntax reference (declaration)
		foreach (SyntaxReference syntaxRef in symbol.DeclaringSyntaxReferences)
		{
			SyntaxNode node = syntaxRef.GetSyntax();

			if (node is VariableDeclaratorSyntax variableDeclarator && variableDeclarator.Initializer is not null)
			{
				return true;
			}

			if (node is PropertyDeclarationSyntax propertyDeclaration && propertyDeclaration.Initializer is not null)
			{
				return true;
			}
		}

		return false;
	}
}
