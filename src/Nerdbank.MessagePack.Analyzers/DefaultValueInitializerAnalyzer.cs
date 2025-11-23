// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using PolyType.Roslyn;

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
		helpLinkUri: AnalyzerUtilities.GetHelpLink(MissingDefaultValueAttributeDiagnosticId),
		customTags: WellKnownDiagnosticTags.CompilationEnd);

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

			KnownSymbols knownSymbols = new(context.Compilation);

			// Get the DefaultValue attribute symbol
			INamedTypeSymbol? defaultValueAttribute = context.Compilation.GetTypeByMetadataName("System.ComponentModel.DefaultValueAttribute");
			if (defaultValueAttribute is null)
			{
				return;
			}

			// Use PolyType.Roslyn's TypeDataModelGenerator to find all types transitively included in the shape
			// TODO: What about finding TypeShapeAttribute, PropertyShapeAttribute, assembly-level attributes, etc.?
			//       Is the Include method thread-safe?
			PolyTypeShapeSynthesis generator = new(context.Compilation.Assembly, knownSymbols, context.CancellationToken);

			context.RegisterSymbolAction(
				symbolContext => this.CollectShapes(symbolContext, generator, referenceSymbols),
				SymbolKind.NamedType);

			context.RegisterCompilationEndAction(
				context =>
				{
					// Look over all shaped types.
					// Get all generated models - these are all the types for which shapes are generated
					IEnumerable<TypeDataModel> allModels = generator.GeneratedModels.Values;

					// Analyze each type that has a shape generated
					foreach (TypeDataModel model in allModels)
					{
						this.AnalyzeTypeModel(context, model, defaultValueAttribute, referenceSymbols);
					}
				});
		});
	}

	private void CollectShapes(SymbolAnalysisContext context, TypeDataModelGenerator generator, ReferenceSymbols referenceSymbols)
	{
		INamedTypeSymbol typeSymbol = (INamedTypeSymbol)context.Symbol;

		// Check if this type has a GenerateShapeAttribute or GenerateShapeForAttribute - this is our entry point
		foreach (AttributeData attribute in typeSymbol.GetAttributes())
		{
			// TODO: What about all the other arguments to these attributes?
			if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, referenceSymbols.GenerateShapeForAttribute))
			{
				if (attribute.ConstructorArguments is [{ Kind: TypedConstantKind.Type, Value: ITypeSymbol shapedType }])
				{
					generator.IncludeType(shapedType);
				}

				continue;
			}

			// Look for generic variant.
			if (attribute.AttributeClass?.TypeArguments is [{ } typeArg] && SymbolEqualityComparer.Default.Equals(attribute.AttributeClass.ConstructUnboundGenericType(), referenceSymbols.GenerateShapeForGenericAttribute))
			{
				generator.IncludeType(typeArg);
				continue;
			}

			// Look for ordinary GenerateShapeAttribute.
			if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, referenceSymbols.GenerateShapeAttribute))
			{
				generator.IncludeType(typeSymbol);
			}
		}
	}

	private void AnalyzeTypeModel(CompilationAnalysisContext context, TypeDataModel model, INamedTypeSymbol defaultValueAttribute, ReferenceSymbols referenceSymbols)
	{
		// Only analyze object types that have properties
		if (model is not ObjectDataModel objectModel)
		{
			return;
		}

		// Check all properties from the model
		foreach (PropertyDataModel property in objectModel.Properties)
		{
			ISymbol member = property.PropertySymbol;

			if (member.IsStatic || member.IsImplicitlyDeclared)
			{
				continue;
			}

			if (member is IFieldSymbol field)
			{
				this.AnalyzeField(context, field, defaultValueAttribute, referenceSymbols);
			}
			else if (member is IPropertySymbol propertySymbol)
			{
				this.AnalyzeProperty(context, propertySymbol, defaultValueAttribute, referenceSymbols);
			}
		}
	}

	private void AnalyzeField(CompilationAnalysisContext context, IFieldSymbol field, INamedTypeSymbol defaultValueAttribute, ReferenceSymbols referenceSymbols)
	{
		// Skip static, const, or compiler-generated fields
		if (field.IsConst)
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

	private void AnalyzeProperty(CompilationAnalysisContext context, IPropertySymbol property, INamedTypeSymbol defaultValueAttribute, ReferenceSymbols referenceSymbols)
	{
		// Skip static, indexers, or compiler-generated properties
		if (property.IsIndexer)
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
