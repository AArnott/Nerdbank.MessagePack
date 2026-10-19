// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp;
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

			HashSet<ISymbol> serializedMembers = new(SymbolEqualityComparer.Default);
			foreach (ObjectDataModel model in generator.GeneratedModels.Values.OfType<ObjectDataModel>())
			{
				foreach (PropertyDataModel property in model.Properties)
				{
					serializedMembers.Add(property.PropertySymbol.OriginalDefinition);
				}
			}

			context.RegisterSyntaxNodeAction(
				nodeContext =>
				{
					EqualsValueClauseSyntax? initializer = nodeContext.Node switch
					{
						VariableDeclaratorSyntax variable => variable.Initializer,
						PropertyDeclarationSyntax property => property.Initializer,
						_ => null,
					};
					if (initializer is not null &&
						nodeContext.SemanticModel.GetDeclaredSymbol(nodeContext.Node, nodeContext.CancellationToken) is { } member)
					{
						AnalyzeInitializer(nodeContext, member, initializer.Value, serializedMembers, defaultValueAttribute);
					}
				},
				SyntaxKind.VariableDeclarator,
				SyntaxKind.PropertyDeclaration);
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

	private static void AnalyzeInitializer(
		SyntaxNodeAnalysisContext context,
		ISymbol member,
		ExpressionSyntax value,
		HashSet<ISymbol> serializedMembers,
		INamedTypeSymbol defaultValueAttribute)
	{
		if (member.IsStatic || member.IsImplicitlyDeclared || !serializedMembers.Contains(member.OriginalDefinition))
		{
			return;
		}

		ITypeSymbol? memberType = member switch
		{
			IFieldSymbol { IsConst: false } field => field.Type,
			IPropertySymbol { IsIndexer: false } property => property.Type,
			_ => null,
		};
		if (memberType is null || !IsAttributeCompatibleType(memberType))
		{
			return;
		}

		Optional<object?> constant = context.SemanticModel.GetConstantValue(value, context.CancellationToken);
		if (constant.HasValue && constant.Value is (null or false or '\0' or
			(byte)0 or (sbyte)0 or (short)0 or (ushort)0 or
			0 or 0U or 0L or 0UL or 0F or 0D))
		{
			return;
		}

		if (member.GetAttributes().Any(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, defaultValueAttribute)))
		{
			return;
		}

		Location location = member.Locations.FirstOrDefault() ?? Location.None;
		context.ReportDiagnostic(Diagnostic.Create(MissingDefaultValueAttributeDescriptor, location, member.Name));
	}
}
