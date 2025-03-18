// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Nerdbank.MessagePack.Analyzers;

public static class AnalyzerUtilities
{
	public static IEnumerable<AttributeData> FindAttributes(this ISymbol symbol, string name, ImmutableArray<string> containingNamespaces)
	{
		foreach (AttributeData att in symbol.GetAttributes())
		{
			if (att.AttributeClass?.Name == name && IsInNamespace(att.AttributeClass, containingNamespaces.AsSpan()))
			{
				yield return att;
			}
		}
	}

	public static IEnumerable<AttributeData> FindAttributes(this ISymbol symbol, INamedTypeSymbol attributeSymbol)
	{
		foreach (AttributeData att in symbol.GetAttributes())
		{
			INamedTypeSymbol? attClass = att.AttributeClass;
			if (attClass?.IsGenericType is true && attributeSymbol.IsUnboundGenericType)
			{
				attClass = attClass.ConstructUnboundGenericType();
			}

			if (SymbolEqualityComparer.Default.Equals(attClass, attributeSymbol) || attClass?.IsAssignableTo(attributeSymbol) is true)
			{
				yield return att;
			}
		}
	}

	public static IEnumerable<INamedTypeSymbol> EnumerateBaseTypes(this ITypeSymbol symbol)
	{
		while (symbol.BaseType is not null)
		{
			yield return symbol.BaseType;
			symbol = symbol.BaseType;
		}
	}

	public static bool IsAssignableTo(this ITypeSymbol subType, INamedTypeSymbol baseTypeOrInterface)
	{
		if (IsOrDerivedFrom(subType, baseTypeOrInterface))
		{
			return true;
		}

		INamedTypeSymbol? unboundGenericBaseTypeOrInterface = baseTypeOrInterface.IsGenericType && !baseTypeOrInterface.IsUnboundGenericType ? baseTypeOrInterface.ConstructUnboundGenericType() : null;

		return baseTypeOrInterface.TypeKind == TypeKind.Interface
			&& subType.AllInterfaces.Any(i =>
				SymbolEqualityComparer.Default.Equals(i, baseTypeOrInterface) ||
				(unboundGenericBaseTypeOrInterface is not null && SymbolEqualityComparer.Default.Equals(TryUnbindGeneric(i), unboundGenericBaseTypeOrInterface)));

		static INamedTypeSymbol TryUnbindGeneric(INamedTypeSymbol type) => type.IsGenericType && !type.IsUnboundGenericType ? type.ConstructUnboundGenericType() : type;
	}

	public static bool IsEquivalent(this ITypeSymbol left, ITypeSymbol right)
	{
		if (SymbolEqualityComparer.Default.Equals(left, right))
		{
			return true;
		}

		if (left is ITypeParameterSymbol leftParam && right is ITypeParameterSymbol rightParam &&
			leftParam.MetadataName == rightParam.MetadataName)
		{
			return true;
		}

		if (left is INamedTypeSymbol { IsGenericType: true, IsUnboundGenericType: false } boundCurrent &&
			right is INamedTypeSymbol { IsGenericType: true, IsUnboundGenericType: false } boundBaseType)
		{
			// Both types are bound, yet they are not equal.
			// This may be because they contain "unique" generic type *parameters*, which we don't consider to be unique.
			if (SymbolEqualityComparer.Default.Equals(boundCurrent.ConstructedFrom, boundBaseType.ConstructedFrom))
			{
				// Compare generic type arguments
				if (boundCurrent.TypeArguments.Zip(boundBaseType.TypeArguments, static (left, right) => (left, right)).All(static pair => IsEquivalent(pair.left, pair.right)))
				{
					return true;
				}
			}
		}

		return false;
	}

	public static bool IsOrDerivedFrom(this ITypeSymbol subType, ITypeSymbol baseType)
	{
		ITypeSymbol? current = subType;
		while (current != null)
		{
			if (IsEquivalent(current, baseType))
			{
				return true;
			}

			if (current is INamedTypeSymbol { IsGenericType: true, IsUnboundGenericType: false } generic &&
				baseType is INamedTypeSymbol { IsUnboundGenericType: true } &&
				SymbolEqualityComparer.Default.Equals(generic.ConstructUnboundGenericType(), baseType))
			{
				return true;
			}

			current = current.BaseType ?? current.OriginalDefinition.BaseType;
		}

		return false;
	}

	public static bool IsInNamespace(ISymbol? symbol, ReadOnlySpan<string> namespaces)
	{
		if (symbol is null)
		{
			return false;
		}

		ISymbol? targetSymbol = symbol;
		for (int i = namespaces.Length - 1; i >= 0; i--)
		{
			if (targetSymbol.ContainingNamespace.Name != namespaces[i])
			{
				return false;
			}

			targetSymbol = targetSymbol.ContainingNamespace;
		}

		return targetSymbol.ContainingNamespace.IsGlobalNamespace;
	}

	public static IEnumerable<ISymbol> GetAllMembers(this ITypeSymbol symbol)
	{
		foreach (ISymbol member in symbol.GetMembers())
		{
			yield return member;
		}

		foreach (INamedTypeSymbol baseType in symbol.EnumerateBaseTypes())
		{
			foreach (ISymbol member in baseType.GetMembers())
			{
				yield return member;
			}
		}
	}

	public static Location? GetArgumentLocation(AttributeData att, int argumentIndex, CancellationToken cancellationToken)
	{
		if (att.ApplicationSyntaxReference?.GetSyntax(cancellationToken) is AttributeSyntax a && a.ArgumentList?.Arguments.Count >= argumentIndex)
		{
			return a.ArgumentList.Arguments[argumentIndex].GetLocation();
		}

		return null;
	}

	public static Location? GetTypeArgumentLocation(AttributeData att, int typeArgumentIndex, CancellationToken cancellationToken)
		=> att.ApplicationSyntaxReference?.GetSyntax(cancellationToken) is AttributeSyntax { Name: GenericNameSyntax { TypeArgumentList.Arguments: { } a } } && a.Count > typeArgumentIndex
			? a[typeArgumentIndex].GetLocation()
			: null;

	public static string GetFullName(this INamedTypeSymbol symbol)
	{
		var sb = new StringBuilder();

		if (symbol.ContainingType is not null)
		{
			sb.Append(GetFullName(symbol.ContainingType));
			sb.Append('.');
		}
		else if (!symbol.ContainingNamespace.IsGlobalNamespace)
		{
			sb.Append(symbol.ContainingNamespace.MetadataName);
			sb.Append('.');
		}

		sb.Append(symbol.MetadataName);
		return sb.ToString();
	}

	internal static string GetHelpLink(string diagnosticId) => $"https://aarnott.github.io/Nerdbank.MessagePack/analyzers/{diagnosticId}.html";
}
