// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Nerdbank.MessagePack.Analyzers;

/// <summary>
/// Checks <c>UnionDiscriminator</c> declarations and keyed-layout guidance.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UnionDiscriminatorAnalyzer : DiagnosticAnalyzer
{
	/// <summary>The discriminator is not a matching constant.</summary>
	public const string NotConstantDiagnosticId = "NBMsgPack130";

	/// <summary>The discriminator is not a single get-only member.</summary>
	public const string InvalidShapeDiagnosticId = "NBMsgPack131";

	/// <summary>The discriminator value type is not supported.</summary>
	public const string UnsupportedTypeDiagnosticId = "NBMsgPack132";

	/// <summary>Union cases disagree about the discriminator.</summary>
	public const string UnionCasesDiagnosticId = "NBMsgPack133";

	/// <summary>A keyed type should place the discriminator at <c>Key(0)</c>.</summary>
	public const string KeyIndexDiagnosticId = "NBMsgPack134";

	private static readonly DiagnosticDescriptor NotConstant = new(
		NotConstantDiagnosticId,
		"Union discriminator must be a matching constant",
		"Union discriminator '{0}' must be a constant that matches its attribute",
		"Usage",
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		helpLinkUri: AnalyzerUtilities.GetHelpLink(NotConstantDiagnosticId));

	private static readonly DiagnosticDescriptor InvalidShape = new(
		InvalidShapeDiagnosticId,
		"Union discriminator must be one get-only member",
		"{0}",
		"Usage",
		DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		helpLinkUri: AnalyzerUtilities.GetHelpLink(InvalidShapeDiagnosticId));

	private static readonly DiagnosticDescriptor UnsupportedType = new(
		UnsupportedTypeDiagnosticId,
		"Union discriminator value type is not supported",
		"Union discriminator '{0}' has unsupported type '{1}'",
		"Usage",
		DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		helpLinkUri: AnalyzerUtilities.GetHelpLink(UnsupportedTypeDiagnosticId));

	private static readonly DiagnosticDescriptor UnionCases = new(
		UnionCasesDiagnosticId,
		"Union cases must share a discriminator name and distinct values",
		"{0}",
		"Usage",
		DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		helpLinkUri: AnalyzerUtilities.GetHelpLink(UnionCasesDiagnosticId));

	private static readonly DiagnosticDescriptor KeyIndex = new(
		KeyIndexDiagnosticId,
		"Key the union discriminator at 0",
		"Give union discriminator '{0}' Key(0) when this type uses Key",
		"Usage",
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		helpLinkUri: AnalyzerUtilities.GetHelpLink(KeyIndexDiagnosticId));

	/// <inheritdoc/>
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [NotConstant, InvalidShape, UnsupportedType, UnionCases, KeyIndex];

	/// <inheritdoc/>
	public override void Initialize(AnalysisContext context)
	{
		if (context is null)
		{
			throw new ArgumentNullException(nameof(context));
		}

		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.RegisterCompilationStartAction(start =>
		{
			if (start.Compilation.GetTypeByMetadataName("Nerdbank.MessagePack.MessagePackSerializer") is null ||
				start.Compilation.GetTypeByMetadataName("Nerdbank.MessagePack.UnionDiscriminatorAttribute") is not INamedTypeSymbol discriminatorAttribute)
			{
				return;
			}

			Scan scan = new(discriminatorAttribute)
			{
				KeyAttribute = start.Compilation.GetTypeByMetadataName("Nerdbank.MessagePack.KeyAttribute"),
				UnionAttribute = start.Compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.UnionAttribute"),
				DerivedTypeShapeAttribute = start.Compilation.GetTypeByMetadataName("PolyType.DerivedTypeShapeAttribute"),
				ConstructorShapeAttribute = start.Compilation.GetTypeByMetadataName("PolyType.ConstructorShapeAttribute"),
			};
			start.RegisterSemanticModelAction(modelContext => scan.Visit(modelContext));
			start.RegisterCompilationEndAction(end => scan.Finish(end));
		});
	}

	private static DiscriminatorFact? AnalyzeType(INamedTypeSymbol type, INamedTypeSymbol discriminatorAttribute, INamedTypeSymbol? keyAttribute, INamedTypeSymbol? constructorShapeAttribute, List<Diagnostic> diagnostics, SemanticModel model, CancellationToken cancellationToken)
	{
		List<ISymbol> members = [];
		foreach (ISymbol member in type.GetMembers())
		{
			if (member is not (IPropertySymbol or IFieldSymbol) || member.IsImplicitlyDeclared || !SymbolEqualityComparer.Default.Equals(member.ContainingType, type) || IsIgnored(member))
			{
				continue;
			}

			if (HasAttribute(member, discriminatorAttribute))
			{
				members.Add(member);
			}
		}

		bool typeLevel = HasAttribute(type, discriminatorAttribute);
		if (typeLevel && members.Count > 0)
		{
			diagnostics.Add(Diagnostic.Create(InvalidShape, type.Locations.FirstOrDefault(), $"Type '{type.Name}' declares both a type-level union discriminator and one on '{members[0].Name}'."));
			return null;
		}

		if (members.Count > 1)
		{
			diagnostics.Add(Diagnostic.Create(InvalidShape, type.Locations.FirstOrDefault(), $"Type '{type.Name}' declares more than one union discriminator."));
			return null;
		}

		DiscriminatorFact? fact = null;
		if (typeLevel)
		{
			fact = AnalyzeTypeLevel(type, discriminatorAttribute, diagnostics);
		}
		else if (members.Count == 1)
		{
			fact = AnalyzeMember(type, members[0], discriminatorAttribute, keyAttribute, constructorShapeAttribute, diagnostics, model, cancellationToken);
		}

		if (keyAttribute is not null && TypeUsesKey(type, keyAttribute) && (typeLevel || members.Count == 1))
		{
			if (typeLevel)
			{
				diagnostics.Add(Diagnostic.Create(InvalidShape, type.Locations.FirstOrDefault(), $"Type '{type.Name}' uses Key, so its union discriminator must be a member with Key."));
				return null;
			}

			ISymbol target = members[0];
			if (!TryGetKeyIndex(target, keyAttribute, out int index))
			{
				diagnostics.Add(Diagnostic.Create(InvalidShape, target.Locations.FirstOrDefault(), $"Union discriminator '{target.Name}' on '{type.Name}' must have Key because the type uses Key."));
			}
			else if (index != 0)
			{
				diagnostics.Add(Diagnostic.Create(KeyIndex, target.Locations.FirstOrDefault(), target.Name));
			}
		}

		return fact;
	}

	private static DiscriminatorFact? AnalyzeTypeLevel(INamedTypeSymbol type, INamedTypeSymbol discriminatorAttribute, List<Diagnostic> diagnostics)
	{
		AttributeData attribute = type.GetAttributes().First(candidate => SymbolEqualityComparer.Default.Equals(candidate.AttributeClass, discriminatorAttribute));
		if (attribute.ConstructorArguments.Length < 2 || attribute.ConstructorArguments[0].Value is not string name || name.Length == 0)
		{
			diagnostics.Add(Diagnostic.Create(InvalidShape, type.Locations.FirstOrDefault(), $"Type '{type.Name}' declares a union discriminator without a property name."));
			return null;
		}

		if (IsReserved(name))
		{
			diagnostics.Add(Diagnostic.Create(InvalidShape, type.Locations.FirstOrDefault(), $"Type '{type.Name}' cannot use '{name}' as a union discriminator name."));
			return null;
		}

		TypedConstant value = attribute.ConstructorArguments[1];
		if (value.Value is null || value.Type is not ITypeSymbol valueType || !IsSupported(valueType))
		{
			diagnostics.Add(Diagnostic.Create(UnsupportedType, type.Locations.FirstOrDefault(), type.Name, value.Type?.ToDisplayString() ?? "null"));
			return null;
		}

		foreach (ISymbol member in type.GetMembers())
		{
			if (!IsIncludedInShape(member))
			{
				continue;
			}

			if (string.Equals(DeclaredName(member), name, StringComparison.Ordinal))
			{
				diagnostics.Add(Diagnostic.Create(InvalidShape, type.Locations.FirstOrDefault(), $"Type '{type.Name}' already has a property serialized as '{name}'."));
				return null;
			}
		}

		return new DiscriminatorFact(name, Token(value.Type, value.Value), KeyIndex: null);
	}

	private static DiscriminatorFact? AnalyzeMember(INamedTypeSymbol type, ISymbol member, INamedTypeSymbol discriminatorAttribute, INamedTypeSymbol? keyAttribute, INamedTypeSymbol? constructorShapeAttribute, List<Diagnostic> diagnostics, SemanticModel model, CancellationToken cancellationToken)
	{
		ITypeSymbol? memberType = member switch
		{
			IPropertySymbol property => property.Type,
			IFieldSymbol field => field.Type,
			_ => null,
		};
		Location location = member.Locations.FirstOrDefault() ?? Location.None;
		if (memberType is null || !IsSupported(memberType))
		{
			diagnostics.Add(Diagnostic.Create(UnsupportedType, location, member.Name, memberType?.ToDisplayString() ?? "unknown"));
			return null;
		}

		if (IsConstructorParameter(type, member.Name, constructorShapeAttribute))
		{
			diagnostics.Add(Diagnostic.Create(InvalidShape, location, $"Union discriminator '{member.Name}' on '{type.Name}' must not be a constructor parameter."));
			return null;
		}

		if (member is IFieldSymbol)
		{
			diagnostics.Add(Diagnostic.Create(InvalidShape, location, $"Union discriminator '{member.Name}' on '{type.Name}' must be a get-only property. A constant field is not part of the object shape."));
			return null;
		}

		if (member is IPropertySymbol { SetMethod: not null })
		{
			diagnostics.Add(Diagnostic.Create(InvalidShape, location, $"Union discriminator '{member.Name}' on '{type.Name}' must not have a setter."));
			return null;
		}

		string name = DeclaredName(member);
		if (IsReserved(name))
		{
			diagnostics.Add(Diagnostic.Create(InvalidShape, location, $"Union discriminator '{member.Name}' on '{type.Name}' cannot use the name '{name}'."));
			return null;
		}

		AttributeData attribute = member.GetAttributes().First(candidate => SymbolEqualityComparer.Default.Equals(candidate.AttributeClass, discriminatorAttribute));
		bool hasConstant = TryGetConstant(member, model, cancellationToken, out object? actual);
		ITypeSymbol tokenType = memberType;
		object? tokenValue = actual;
		if (attribute.ConstructorArguments.Length > 0)
		{
			TypedConstant argument = attribute.ConstructorArguments[0];
			object? expected = argument.Value;
			if (expected is null)
			{
				diagnostics.Add(Diagnostic.Create(UnsupportedType, location, member.Name, "null"));
				return null;
			}

			tokenValue = expected;
			tokenType = memberType.TypeKind == TypeKind.Enum ? memberType : argument.Type ?? memberType;
			if (!hasConstant || !ValuesEqual(actual, expected))
			{
				diagnostics.Add(Diagnostic.Create(NotConstant, location, member.Name));
			}
		}
		else if (!hasConstant)
		{
			diagnostics.Add(Diagnostic.Create(NotConstant, location, member.Name));
			return null;
		}

		if (tokenValue is null)
		{
			diagnostics.Add(Diagnostic.Create(UnsupportedType, location, member.Name, "null"));
			return null;
		}

		int? keyIndex = keyAttribute is not null && TryGetKeyIndex(member, keyAttribute, out int index) ? index : null;
		return new DiscriminatorFact(name, Token(tokenType, tokenValue), keyIndex);
	}

	private static void AnalyzeUnion(INamedTypeSymbol union, IEnumerable<INamedTypeSymbol> cases, Dictionary<INamedTypeSymbol, DiscriminatorFact> facts, List<Diagnostic> diagnostics)
	{
		string? name = null;
		int? keyIndex = null;
		List<DiscriminatorToken> nameValues = [];
		List<DiscriminatorToken> keyValues = [];
		foreach (INamedTypeSymbol caseType in cases)
		{
			if (!facts.TryGetValue(caseType, out DiscriminatorFact fact))
			{
				continue;
			}

			if (fact.KeyIndex is null)
			{
				if (name is null)
				{
					name = fact.Name;
				}
				else if (!string.Equals(name, fact.Name, StringComparison.Ordinal))
				{
					diagnostics.Add(Diagnostic.Create(UnionCases, union.Locations.FirstOrDefault(), $"Union '{union.Name}' declares discriminator names '{name}' and '{fact.Name}'."));
					return;
				}

				if (ContainsCollision(nameValues, fact.Token))
				{
					diagnostics.Add(Diagnostic.Create(UnionCases, union.Locations.FirstOrDefault(), $"Union '{union.Name}' repeats discriminator value {fact.Token.Display}."));
					return;
				}

				nameValues.Add(fact.Token);
			}
			else
			{
				if (keyIndex is null)
				{
					keyIndex = fact.KeyIndex;
				}
				else if (keyIndex != fact.KeyIndex)
				{
					diagnostics.Add(Diagnostic.Create(UnionCases, union.Locations.FirstOrDefault(), $"Union '{union.Name}' declares discriminator indexes {keyIndex} and {fact.KeyIndex}."));
					return;
				}

				if (ContainsCollision(keyValues, fact.Token))
				{
					diagnostics.Add(Diagnostic.Create(UnionCases, union.Locations.FirstOrDefault(), $"Union '{union.Name}' repeats discriminator value {fact.Token.Display}."));
					return;
				}

				keyValues.Add(fact.Token);
			}
		}
	}

	private static IEnumerable<INamedTypeSymbol> CaseTypes(INamedTypeSymbol union)
	{
		HashSet<INamedTypeSymbol> cases = new(SymbolEqualityComparer.Default);
		foreach (IMethodSymbol constructor in union.InstanceConstructors)
		{
			if (constructor.Parameters.Length == 1 && constructor.Parameters[0].Type is INamedTypeSymbol type &&
				!SymbolEqualityComparer.Default.Equals(type, union) && type.TypeKind is TypeKind.Class or TypeKind.Struct)
			{
				cases.Add(type);
			}
		}

		return cases;
	}

	private static bool TryGetConstant(ISymbol member, SemanticModel model, CancellationToken cancellationToken, out object? value)
	{
		value = null;
		if (member is IFieldSymbol { IsConst: true } field)
		{
			value = field.ConstantValue;
			return true;
		}

		if (member is not IPropertySymbol property)
		{
			return false;
		}

		foreach (SyntaxReference reference in property.DeclaringSyntaxReferences)
		{
			if (reference.GetSyntax(cancellationToken) is not PropertyDeclarationSyntax declaration)
			{
				continue;
			}

			ExpressionSyntax? expression = declaration.ExpressionBody?.Expression;
			AccessorDeclarationSyntax? getter = declaration.AccessorList?.Accessors.FirstOrDefault(accessor => accessor.Keyword.IsKind(SyntaxKind.GetKeyword));
			expression ??= getter?.ExpressionBody?.Expression;
			if (expression is null && getter?.Body?.Statements is [ReturnStatementSyntax { Expression: { } returned }])
			{
				expression = returned;
			}

			if (expression is null || expression.SyntaxTree != model.SyntaxTree)
			{
				continue;
			}

			Optional<object?> constant = model.GetConstantValue(expression, cancellationToken);
			if (constant.HasValue)
			{
				value = constant.Value;
				return true;
			}
		}

		return false;
	}

	private static bool TypeUsesKey(INamedTypeSymbol type, INamedTypeSymbol keyAttribute)
	{
		foreach (ISymbol member in type.GetMembers())
		{
			if (SymbolEqualityComparer.Default.Equals(member.ContainingType, type) && HasAttribute(member, keyAttribute))
			{
				return true;
			}
		}

		return false;
	}

	private static bool TryGetKeyIndex(ISymbol member, INamedTypeSymbol keyAttribute, out int index)
	{
		index = -1;
		foreach (AttributeData attribute in member.GetAttributes())
		{
			if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, keyAttribute))
			{
				continue;
			}

			if (attribute.ConstructorArguments.FirstOrDefault().Value is int value)
			{
				index = value;
			}

			return true;
		}

		return false;
	}

	private static bool IsConstructorParameter(INamedTypeSymbol type, string name, INamedTypeSymbol? constructorShapeAttribute)
	{
		IMethodSymbol? constructor = SelectedConstructor(type, constructorShapeAttribute);
		if (constructor is null)
		{
			return false;
		}

		foreach (IParameterSymbol parameter in constructor.Parameters)
		{
			if (string.Equals(parameter.Name, name, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		return false;
	}

	private static IMethodSymbol? SelectedConstructor(INamedTypeSymbol type, INamedTypeSymbol? constructorShapeAttribute)
	{
		IMethodSymbol[] constructors = [.. type.InstanceConstructors];
		if (constructorShapeAttribute is not null)
		{
			IMethodSymbol[] attributed = [.. constructors.Where(constructor => HasAttribute(constructor, constructorShapeAttribute))];
			if (attributed.Length > 0)
			{
				return attributed.Length == 1 ? attributed[0] : null;
			}
		}

		IMethodSymbol[] accessible = [.. constructors.Where(constructor => constructor.DeclaredAccessibility == Accessibility.Public)];
		return accessible.FirstOrDefault(constructor => constructor.Parameters.Length == 0)
			?? (accessible.Length == 1 ? accessible[0] : null);
	}

	private static bool DiscriminatorIsDeclaredElsewhere(INamedTypeSymbol type, INamedTypeSymbol discriminatorAttribute, SemanticModel model)
	{
		foreach (ISymbol member in type.GetMembers())
		{
			if (member.IsImplicitlyDeclared || !HasAttribute(member, discriminatorAttribute))
			{
				continue;
			}

			return member.DeclaringSyntaxReferences.Length > 0 &&
				member.DeclaringSyntaxReferences.All(reference => reference.SyntaxTree != model.SyntaxTree);
		}

		return false;
	}

	private static bool IsIncludedInShape(ISymbol member)
	{
		if (member.IsStatic || member.IsImplicitlyDeclared || IsIgnored(member))
		{
			return false;
		}

		bool explicitShape = HasPropertyShape(member);
		if (member is IPropertySymbol property)
		{
			if (property.IsIndexer)
			{
				return false;
			}

			return explicitShape || (property.DeclaredAccessibility == Accessibility.Public && property.GetMethod?.DeclaredAccessibility == Accessibility.Public);
		}

		if (member is IFieldSymbol field)
		{
			if (field.IsConst)
			{
				return false;
			}

			return explicitShape || field.DeclaredAccessibility == Accessibility.Public;
		}

		return false;
	}

	private static bool HasPropertyShape(ISymbol member)
	{
		foreach (AttributeData attribute in member.GetAttributes())
		{
			if (attribute.AttributeClass?.Name == "PropertyShapeAttribute")
			{
				return true;
			}
		}

		return false;
	}

	private static string DeclaredName(ISymbol member)
	{
		foreach (AttributeData attribute in member.GetAttributes())
		{
			if (attribute.AttributeClass?.Name != "PropertyShapeAttribute")
			{
				continue;
			}

			foreach (KeyValuePair<string, TypedConstant> named in attribute.NamedArguments)
			{
				if (named.Key == "Name" && named.Value.Value is string name && name.Length > 0)
				{
					return name;
				}
			}
		}

		return member.Name;
	}

	private static bool IsIgnored(ISymbol member)
	{
		foreach (AttributeData attribute in member.GetAttributes())
		{
			if (attribute.AttributeClass?.Name != "PropertyShapeAttribute")
			{
				continue;
			}

			foreach (KeyValuePair<string, TypedConstant> named in attribute.NamedArguments)
			{
				if (named.Key == "Ignore" && named.Value.Value is true)
				{
					return true;
				}
			}
		}

		return false;
	}

	private static bool HasAttribute(ISymbol symbol, INamedTypeSymbol? attribute)
		=> attribute is not null && symbol.GetAttributes().Any(candidate => SymbolEqualityComparer.Default.Equals(candidate.AttributeClass, attribute));

	private static bool IsReserved(string name)
		=> name is "$type" or "$value";

	private static bool IsSupported(ITypeSymbol type)
		=> type.TypeKind == TypeKind.Enum || type.SpecialType is
			SpecialType.System_String or SpecialType.System_Boolean or SpecialType.System_Byte or SpecialType.System_SByte or
			SpecialType.System_Int16 or SpecialType.System_UInt16 or SpecialType.System_Int32 or SpecialType.System_UInt32 or
			SpecialType.System_Int64 or SpecialType.System_UInt64;

	private static bool ValuesEqual(object? left, object? right)
		=> Equals(left, right) || (TryNumber(left, out long leftNumber) && TryNumber(right, out long rightNumber) && leftNumber == rightNumber);

	private static bool TryNumber(object? value, out long number)
	{
		switch (value)
		{
			case byte item:
				number = item;
				return true;
			case sbyte item:
				number = item;
				return true;
			case short item:
				number = item;
				return true;
			case ushort item:
				number = item;
				return true;
			case int item:
				number = item;
				return true;
			case uint item:
				number = item;
				return true;
			case long item:
				number = item;
				return true;
			case ulong item when item <= long.MaxValue:
				number = (long)item;
				return true;
			default:
				number = 0;
				return false;
		}
	}

	private static DiscriminatorToken Token(ITypeSymbol? type, object? value)
	{
		if (type?.TypeKind == TypeKind.Enum)
		{
			return new DiscriminatorToken('e', type.ToDisplayString(), EnumMemberName(type, value), FormatInteger(value));
		}

		if (value is string text)
		{
			return new DiscriminatorToken('s', null, null, text);
		}

		if (value is bool boolean)
		{
			return new DiscriminatorToken('b', null, null, boolean ? "true" : "false");
		}

		if (value is ulong unsigned && unsigned > long.MaxValue)
		{
			return new DiscriminatorToken('u', null, null, unsigned.ToString(CultureInfo.InvariantCulture));
		}

		if (TryNumber(value, out long number))
		{
			return new DiscriminatorToken('n', null, null, number.ToString(CultureInfo.InvariantCulture));
		}

		return new DiscriminatorToken('o', type?.ToDisplayString(), null, value?.ToString() ?? string.Empty);
	}

	private static string? EnumMemberName(ITypeSymbol type, object? value)
	{
		if (type is not INamedTypeSymbol named || value is null)
		{
			return null;
		}

		foreach (ISymbol member in named.GetMembers())
		{
			if (member is IFieldSymbol { IsConst: true, HasConstantValue: true, ConstantValue: { } constant } && ValuesEqual(constant, value))
			{
				return member.Name;
			}
		}

		return null;
	}

	private static string FormatInteger(object? value)
	{
		if (value is ulong unsigned)
		{
			return unsigned.ToString(CultureInfo.InvariantCulture);
		}

		return TryNumber(value, out long number)
			? number.ToString(CultureInfo.InvariantCulture)
			: Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
	}

	private static bool ContainsCollision(List<DiscriminatorToken> values, DiscriminatorToken token)
	{
		foreach (DiscriminatorToken existing in values)
		{
			if (existing.NecessarilyCollides(token))
			{
				return true;
			}
		}

		return false;
	}

	private readonly record struct DiscriminatorFact(string Name, DiscriminatorToken Token, int? KeyIndex);

	private readonly record struct DiscriminatorToken(char Kind, string? Type, string? Member, string Text)
	{
		internal string Display => this.Kind == 'e' ? this.Member ?? this.Text : this.Text;

		internal bool NecessarilyCollides(DiscriminatorToken other)
		{
			if (this.Kind != other.Kind || !string.Equals(this.Text, other.Text, StringComparison.Ordinal))
			{
				return false;
			}

			if (this.Kind != 'e')
			{
				return true;
			}

			if (string.Equals(this.Type, other.Type, StringComparison.Ordinal))
			{
				return true;
			}

			if (this.Member is null && other.Member is null)
			{
				return true;
			}

			return this.Member is not null && string.Equals(this.Member, other.Member, StringComparison.Ordinal);
		}
	}

	private sealed class Scan
	{
		private readonly INamedTypeSymbol discriminatorAttribute;
		private readonly HashSet<INamedTypeSymbol> seen = new(SymbolEqualityComparer.Default);
		private readonly HashSet<INamedTypeSymbol> unions = new(SymbolEqualityComparer.Default);
		private readonly Dictionary<INamedTypeSymbol, HashSet<INamedTypeSymbol>> derivedCases = new(SymbolEqualityComparer.Default);
		private readonly Dictionary<INamedTypeSymbol, DiscriminatorFact> facts = new(SymbolEqualityComparer.Default);

		internal Scan(INamedTypeSymbol discriminatorAttribute)
		{
			this.discriminatorAttribute = discriminatorAttribute;
		}

		internal INamedTypeSymbol? KeyAttribute { get; init; }

		internal INamedTypeSymbol? UnionAttribute { get; init; }

		internal INamedTypeSymbol? DerivedTypeShapeAttribute { get; init; }

		internal INamedTypeSymbol? ConstructorShapeAttribute { get; init; }

		internal void Visit(SemanticModelAnalysisContext context)
		{
			if (context.SemanticModel.SyntaxTree.FilePath.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase))
			{
				return;
			}

			lock (this.seen)
			{
				List<Diagnostic> diagnostics = [];
				foreach (TypeDeclarationSyntax declaration in context.SemanticModel.SyntaxTree.GetRoot(context.CancellationToken).DescendantNodes().OfType<TypeDeclarationSyntax>())
				{
					if (context.SemanticModel.GetDeclaredSymbol(declaration, context.CancellationToken) is not INamedTypeSymbol type ||
						DiscriminatorIsDeclaredElsewhere(type, this.discriminatorAttribute, context.SemanticModel) ||
						!this.seen.Add(type))
					{
						continue;
					}

					if (AnalyzeType(type, this.discriminatorAttribute, this.KeyAttribute, this.ConstructorShapeAttribute, diagnostics, context.SemanticModel, context.CancellationToken) is DiscriminatorFact fact)
					{
						this.facts[type] = fact;
					}

					if (declaration.Keyword.ValueText == "union" || HasAttribute(type, this.UnionAttribute))
					{
						this.unions.Add(type);
					}

					if (this.DerivedTypeShapeAttribute is null)
					{
						continue;
					}

					foreach (AttributeData attribute in type.GetAttributes())
					{
						if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, this.DerivedTypeShapeAttribute) ||
							attribute.ConstructorArguments.FirstOrDefault().Value is not INamedTypeSymbol caseType)
						{
							continue;
						}

						if (!this.derivedCases.TryGetValue(type, out HashSet<INamedTypeSymbol>? cases))
						{
							this.derivedCases.Add(type, cases = new(SymbolEqualityComparer.Default));
						}

						cases.Add(caseType);
					}
				}

				foreach (Diagnostic diagnostic in diagnostics)
				{
					context.ReportDiagnostic(diagnostic);
				}
			}
		}

		internal void Finish(CompilationAnalysisContext context)
		{
			lock (this.seen)
			{
				List<Diagnostic> diagnostics = [];
				foreach (INamedTypeSymbol union in this.unions)
				{
					AnalyzeUnion(union, CaseTypes(union), this.facts, diagnostics);
				}

				foreach (KeyValuePair<INamedTypeSymbol, HashSet<INamedTypeSymbol>> pair in this.derivedCases)
				{
					if (!this.unions.Contains(pair.Key))
					{
						AnalyzeUnion(pair.Key, pair.Value, this.facts, diagnostics);
					}
				}

				foreach (Diagnostic diagnostic in diagnostics)
				{
					context.ReportDiagnostic(diagnostic);
				}
			}
		}
	}
}
