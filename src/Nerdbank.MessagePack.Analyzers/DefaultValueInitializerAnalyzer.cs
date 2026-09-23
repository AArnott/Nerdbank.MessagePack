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
		helpLinkUri: AnalyzerUtilities.GetHelpLink(MissingDefaultValueAttributeDiagnosticId));

	private static readonly SymbolDisplayFormat QualifiedNameOnlyFormat = new(
		globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
		typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
		memberOptions: SymbolDisplayMemberOptions.IncludeContainingType);

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

			ImmutableArray<INamedTypeSymbol> declaredTypes = GetDeclaredTypes(context.Compilation.Assembly.GlobalNamespace).ToImmutableArray();
			List<ITypeSymbol> shapeRoots = [];
			List<string> shapePatterns = [];
			foreach (INamedTypeSymbol declaredType in declaredTypes)
			{
				CollectShapeRoots(declaredType, shapeRoots, shapePatterns, referenceSymbols);
			}

			PolyTypeShapeSynthesis generator = new(context.Compilation.Assembly, knownSymbols, referenceSymbols, context.CancellationToken);
			foreach (ITypeSymbol shapeRoot in shapeRoots)
			{
				generator.IncludeType(shapeRoot);
			}

			foreach (string pattern in shapePatterns)
			{
				if (!IsValidPattern(pattern))
				{
					continue;
				}

				foreach (INamedTypeSymbol declaredType in declaredTypes)
				{
					if (declaredType is not { IsGenericType: true, IsDefinition: true } &&
						IsMatch(declaredType.ToDisplayString(QualifiedNameOnlyFormat), pattern))
					{
						generator.IncludeType(declaredType);
					}
				}
			}

			Dictionary<INamedTypeSymbol, TypeDataModel> generatedModelsBySourceDeclaration = new(SymbolEqualityComparer.Default);
			foreach (KeyValuePair<ITypeSymbol, TypeDataModel> generatedModel in generator.GeneratedModels)
			{
				if (GetSourceDeclaration(generatedModel.Key) is { } sourceDeclaration && !generatedModelsBySourceDeclaration.ContainsKey(sourceDeclaration))
				{
					generatedModelsBySourceDeclaration.Add(sourceDeclaration, generatedModel.Value);
				}
			}

			context.RegisterSymbolAction(
				symbolContext =>
				{
					if (generatedModelsBySourceDeclaration.TryGetValue((INamedTypeSymbol)symbolContext.Symbol, out TypeDataModel? model))
					{
						this.AnalyzeTypeModel(symbolContext, model, defaultValueAttribute, referenceSymbols);
					}
				},
				SymbolKind.NamedType);
		});
	}

	private static void CollectShapeRoots(
		INamedTypeSymbol typeSymbol,
		ICollection<ITypeSymbol> shapeRoots,
		ICollection<string> shapePatterns,
		ReferenceSymbols referenceSymbols)
	{
		// Check if this type has a GenerateShapeAttribute or GenerateShapeForAttribute - this is our entry point
		foreach (AttributeData attribute in typeSymbol.GetAttributes())
		{
			if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, referenceSymbols.GenerateShapeForAttribute))
			{
				if (HasCustomShapeConfiguration(attribute))
				{
					continue;
				}

				if (attribute.ConstructorArguments is [{ Kind: TypedConstantKind.Type, Value: ITypeSymbol shapedType }])
				{
					shapeRoots.Add(shapedType);
				}
				else if (attribute.ConstructorArguments is [{ Kind: TypedConstantKind.Primitive, Value: string pattern }])
				{
					shapePatterns.Add(pattern);
				}

				continue;
			}

			// Look for generic variant.
			if (attribute.AttributeClass?.TypeArguments is [{ } typeArg] && SymbolEqualityComparer.Default.Equals(attribute.AttributeClass.ConstructUnboundGenericType(), referenceSymbols.GenerateShapeForGenericAttribute))
			{
				if (!HasCustomShapeConfiguration(attribute))
				{
					shapeRoots.Add(typeArg);
				}

				continue;
			}

			// Look for ordinary GenerateShapeAttribute.
			if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, referenceSymbols.GenerateShapeAttribute))
			{
				if (!HasCustomShapeConfiguration(attribute))
				{
					shapeRoots.Add(typeSymbol);
				}
			}
		}
	}

	private static bool HasCustomShapeConfiguration(AttributeData attribute) => !attribute.NamedArguments.IsEmpty;

	private static INamedTypeSymbol? GetSourceDeclaration(ITypeSymbol typeSymbol)
	{
		return typeSymbol is INamedTypeSymbol namedType ? namedType.OriginalDefinition : null;
	}

	private static IEnumerable<INamedTypeSymbol> GetDeclaredTypes(INamespaceOrTypeSymbol container)
	{
		foreach (INamedTypeSymbol type in container.GetTypeMembers())
		{
			yield return type;

			foreach (INamedTypeSymbol nestedType in GetDeclaredTypes(type))
			{
				yield return nestedType;
			}
		}

		if (container is INamespaceSymbol namespaceSymbol)
		{
			foreach (INamespaceSymbol childNamespace in namespaceSymbol.GetNamespaceMembers())
			{
				foreach (INamedTypeSymbol type in GetDeclaredTypes(childNamespace))
				{
					yield return type;
				}
			}
		}
	}

	private static bool IsMatch(string value, string pattern)
	{
		int valueIndex = 0;
		int patternIndex = 0;
		int wildcardIndex = -1;
		int wildcardValueIndex = -1;

		while (valueIndex < value.Length)
		{
			if (patternIndex < pattern.Length && (pattern[patternIndex] == '?' || pattern[patternIndex] == value[valueIndex]))
			{
				valueIndex++;
				patternIndex++;
			}
			else if (patternIndex < pattern.Length && pattern[patternIndex] == '*')
			{
				wildcardIndex = patternIndex++;
				wildcardValueIndex = valueIndex;
			}
			else if (wildcardIndex >= 0)
			{
				patternIndex = wildcardIndex + 1;
				valueIndex = ++wildcardValueIndex;
			}
			else
			{
				return false;
			}
		}

		while (patternIndex < pattern.Length && pattern[patternIndex] == '*')
		{
			patternIndex++;
		}

		return patternIndex == pattern.Length;
	}

	private static bool IsValidPattern(string pattern) => pattern.Length > 0;

	private static bool IsAttributeCompatibleType(ITypeSymbol type)
		=> type.TypeKind is TypeKind.Enum ||
			type.SpecialType is
				SpecialType.System_Boolean or
				SpecialType.System_Byte or
				SpecialType.System_SByte or
				SpecialType.System_Char or
				SpecialType.System_Double or
				SpecialType.System_Int16 or
				SpecialType.System_UInt16 or
				SpecialType.System_Int32 or
				SpecialType.System_UInt32 or
				SpecialType.System_Int64 or
				SpecialType.System_UInt64 or
				SpecialType.System_Single or
				SpecialType.System_String;

	private void AnalyzeTypeModel(SymbolAnalysisContext context, TypeDataModel model, INamedTypeSymbol defaultValueAttribute, ReferenceSymbols referenceSymbols)
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

	private void AnalyzeField(SymbolAnalysisContext context, IFieldSymbol field, INamedTypeSymbol defaultValueAttribute, ReferenceSymbols referenceSymbols)
	{
		// Skip static, const, or compiler-generated fields
		if (field.IsConst)
		{
			return;
		}

		// Check if the field type supports a value that can be represented by DefaultValueAttribute.
		if (!IsAttributeCompatibleType(field.Type) || !this.HasInitializer(field))
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
		if (property.IsIndexer)
		{
			return;
		}

		// Check if the property type supports a value that can be represented by DefaultValueAttribute.
		if (!IsAttributeCompatibleType(property.Type) || !this.HasInitializer(property))
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
