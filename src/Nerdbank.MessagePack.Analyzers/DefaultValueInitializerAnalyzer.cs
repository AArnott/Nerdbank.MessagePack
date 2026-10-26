// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using PolyType.Roslyn;

namespace Nerdbank.MessagePack.Analyzers;

/// <summary>
/// Analyzer that detects optional fields and properties with initializers but no DefaultValueAttribute
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
			INamedTypeSymbol? parameterShapeAttribute = context.Compilation.GetTypeByMetadataName("PolyType.ParameterShapeAttribute");

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

			Dictionary<SyntaxTree, List<(ISymbol Member, SyntaxReference Declaration, IParameterSymbol? Parameter)>> membersByTree = [];
			foreach (ObjectDataModel model in generator.GeneratedModels.Values.OfType<ObjectDataModel>())
			{
				ConstructorDataModel constructor = model.Constructors
					.FirstOrDefault(candidate => candidate.Constructor is not null && candidate.Constructor.FindAttributes(referenceSymbols.ConstructorShapeAttribute).Any());
				if (constructor.Constructor is null)
				{
					constructor = model.Constructors.Length == 1
						? model.Constructors[0]
						: model.Constructors.FirstOrDefault(candidate => candidate.Constructor is { Parameters.Length: 0 });
				}

				Dictionary<string, IParameterSymbol?> parametersByName = GetShapeParameters(model, constructor, parameterShapeAttribute);
				foreach (PropertyDataModel property in model.Properties)
				{
					if (IsRequired(property, referenceSymbols))
					{
						continue;
					}

					ISymbol member = property.PropertySymbol;
					string name = property.LogicalName ?? property.Name;
					if (!parametersByName.TryGetValue(name, out IParameterSymbol? parameter))
					{
						KeyValuePair<string, IParameterSymbol?>[] matches = parametersByName.Where(entry => string.Equals(entry.Key, name, StringComparison.OrdinalIgnoreCase)).ToArray();
						parameter = matches.Length == 1 ? matches[0].Value : null;
					}

					foreach (SyntaxReference declaration in member.OriginalDefinition.DeclaringSyntaxReferences)
					{
						if (!membersByTree.TryGetValue(declaration.SyntaxTree, out List<(ISymbol Member, SyntaxReference Declaration, IParameterSymbol? Parameter)>? members))
						{
							membersByTree.Add(declaration.SyntaxTree, members = []);
						}

						members.Add((member, declaration, parameter));
					}
				}
			}

			context.RegisterSemanticModelAction(modelContext =>
			{
				if (!membersByTree.TryGetValue(modelContext.SemanticModel.SyntaxTree, out List<(ISymbol Member, SyntaxReference Declaration, IParameterSymbol? Parameter)>? members))
				{
					return;
				}

				HashSet<ISymbol> reportedMembers = new(SymbolEqualityComparer.Default);
				foreach ((ISymbol member, SyntaxReference declaration, IParameterSymbol? parameter) in members)
				{
					EqualsValueClauseSyntax? initializer = declaration.GetSyntax(modelContext.CancellationToken) switch
					{
						VariableDeclaratorSyntax variable => variable.Initializer,
						PropertyDeclarationSyntax property => property.Initializer,
						_ => null,
					};
					if (initializer is not null)
					{
						if (AnalyzeInitializer(modelContext, member, initializer.Value, parameter, defaultValueAttribute) is { } diagnostic &&
							reportedMembers.Add(member.OriginalDefinition))
						{
							modelContext.ReportDiagnostic(diagnostic);
						}
					}
				}
			});
		});
	}

	/// <summary>
	/// Resolves shape parameter names before applying the serializer's exact-name precedence.
	/// </summary>
	/// <param name="model">The declaring object shape model.</param>
	/// <param name="constructor">The selected constructor model.</param>
	/// <param name="parameterShapeAttribute">The parameter configuration attribute type.</param>
	/// <returns>Shape parameter names mapped to method parameters, or null for member initializers.</returns>
	private static Dictionary<string, IParameterSymbol?> GetShapeParameters(ObjectDataModel model, ConstructorDataModel constructor, INamedTypeSymbol? parameterShapeAttribute)
	{
		Dictionary<string, IParameterSymbol?> result = new(StringComparer.Ordinal);
		if (constructor.Constructor is null)
		{
			return result;
		}

		foreach (ParameterDataModel parameter in constructor.Parameters)
		{
			PropertyDataModel? matchingProperty = null;
			foreach (PropertyDataModel property in model.Properties)
			{
				if (SymbolEqualityComparer.Default.Equals(property.PropertyType, parameter.Parameter.Type) &&
					MatchesCamelCase(parameter.Parameter.Name, property.Name))
				{
					matchingProperty = property;
					break;
				}
			}

			AttributeData? attribute = parameterShapeAttribute is null ? null : parameter.Parameter.FindAttributes(parameterShapeAttribute).FirstOrDefault();
			string? configuredName = attribute?.NamedArguments.FirstOrDefault(argument => argument.Key == Constants.PropertyShapeAttribute.NameProperty).Value.Value as string;
			string name = configuredName ?? matchingProperty?.LogicalName ?? matchingProperty?.Name ?? parameter.Parameter.Name;
			result[name] = parameter.Parameter;
		}

		if (constructor.Parameters.Length > 0 || constructor.MemberInitializers.Any(property => property.IsRequiredBySyntax || property.IsRequiredByPolicy is true || property.IsInitOnly))
		{
			foreach (PropertyDataModel property in constructor.MemberInitializers)
			{
				string name = property.LogicalName ?? property.Name;
				if (!result.ContainsKey(name))
				{
					// An exact member-initializer match must beat a case-insensitive method-parameter match.
					result.Add(name, null);
				}
			}
		}

		return result;
	}

	/// <summary>
	/// Matches declared names using PolyType's first-character-only Pascal/camel case convention.
	/// </summary>
	/// <param name="left">The parameter name.</param>
	/// <param name="right">The member name.</param>
	/// <returns>Whether the names differ only in the casing of their first character.</returns>
	private static bool MatchesCamelCase(string left, string right) =>
		left.Length == right.Length && (left.Length == 0 ||
			(char.ToLowerInvariant(left[0]) == char.ToLowerInvariant(right[0]) &&
				string.CompareOrdinal(left, 1, right, 1, left.Length - 1) == 0));

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

	private static bool IsRequired(PropertyDataModel property, ReferenceSymbols referenceSymbols)
	{
		AttributeData? attribute = property.PropertySymbol.FindAttributes(referenceSymbols.PropertyShapeAttribute).FirstOrDefault();
		bool? requiredByAttribute = attribute?.NamedArguments
			.FirstOrDefault(argument => argument.Key == Constants.PropertyShapeAttribute.IsRequiredProperty).Value.Value as bool?;
		return requiredByAttribute ?? property.IsRequiredByPolicy ?? property.IsRequiredBySyntax;
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

	private static Diagnostic? AnalyzeInitializer(
		SemanticModelAnalysisContext context,
		ISymbol member,
		ExpressionSyntax value,
		IParameterSymbol? parameter,
		INamedTypeSymbol defaultValueAttribute)
	{
		if (member.IsStatic || member.IsImplicitlyDeclared)
		{
			return null;
		}

		ITypeSymbol? memberType = member switch
		{
			IFieldSymbol { IsConst: false } field => field.Type,
			IPropertySymbol { IsIndexer: false } property => property.Type,
			_ => null,
		};
		if (memberType is null || !IsAttributeCompatibleType(memberType))
		{
			return null;
		}

		Optional<object?> constant = context.SemanticModel.GetConstantValue(value, context.CancellationToken);
		if (constant.HasValue && parameter is { HasExplicitDefaultValue: true })
		{
			// The initializer operation includes the conversion to the member's declared type.
			constant = context.SemanticModel.GetOperation(value.Parent!, context.CancellationToken) switch
			{
				IFieldInitializerOperation initializer => initializer.Value.ConstantValue,
				IPropertyInitializerOperation initializer => initializer.Value.ConstantValue,
				_ => constant,
			};
		}

		if (constant.HasValue && (parameter is { HasExplicitDefaultValue: true }
			? Equals(constant.Value, parameter.ExplicitDefaultValue)
			: constant.Value is (null or false or '\0' or
				(byte)0 or (sbyte)0 or (short)0 or (ushort)0 or
				0 or 0U or 0L or 0UL or 0F or 0D)))
		{
			return null;
		}

		if (IsPrimaryConstructorParameterForwarding(context, value, memberType, parameter))
		{
			return null;
		}

		if (member.GetAttributes().Any(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, defaultValueAttribute)))
		{
			return null;
		}

		Location location = member.Locations.FirstOrDefault() ?? Location.None;
		return Diagnostic.Create(MissingDefaultValueAttributeDescriptor, location, member.Name);
	}

	private static bool IsPrimaryConstructorParameterForwarding(SemanticModelAnalysisContext context, ExpressionSyntax value, ITypeSymbol memberType, IParameterSymbol? parameter)
	{
		while (value is ParenthesizedExpressionSyntax parenthesizedExpression)
		{
			value = parenthesizedExpression.Expression;
		}

		IOperation? operation = context.SemanticModel.GetOperation(value, context.CancellationToken);
		while (true)
		{
			switch (operation)
			{
				case IParenthesizedOperation parenthesized:
					operation = parenthesized.Operand;
					break;
				case IConversionOperation { Conversion.IsIdentity: true } conversion:
					operation = conversion.Operand;
					break;
				default:
					return operation is IParameterReferenceOperation parameterReference &&
						parameter is not null &&
						SymbolEqualityComparer.Default.Equals(parameterReference.Parameter.OriginalDefinition, parameter.OriginalDefinition) &&
						SymbolEqualityComparer.Default.Equals(parameter.Type, memberType) &&
						parameterReference.Parameter.DeclaringSyntaxReferences.Any(reference =>
							reference.GetSyntax(context.CancellationToken) is ParameterSyntax { Parent: ParameterListSyntax { Parent: TypeDeclarationSyntax } });
			}
		}
	}
}
