// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace Nerdbank.MessagePack.Analyzers.CodeFixes;

/// <summary>
/// Replaces a union discriminator attribute argument with the member's constant expression.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp)]
[Shared]
public sealed class UnionDiscriminatorCodeFix : CodeFixProvider
{
	/// <inheritdoc/>
	public override ImmutableArray<string> FixableDiagnosticIds => [UnionDiscriminatorAnalyzer.NotConstantDiagnosticId];

	/// <inheritdoc/>
	public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	/// <inheritdoc/>
	public override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
		SemanticModel? semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
		if (root is null || semanticModel is null)
		{
			return;
		}

		SyntaxNode node = root.FindNode(context.Span);
		MemberDeclarationSyntax? member = node.FirstAncestorOrSelf<MemberDeclarationSyntax>();
		AttributeSyntax? attribute = member?.AttributeLists.SelectMany(list => list.Attributes).FirstOrDefault(candidate => IsDiscriminator(semanticModel, candidate, context.CancellationToken));
		ExpressionSyntax? constant = member is null ? null : ConstantExpression(member);
		Optional<object?> constantValue = constant is null ? default : semanticModel.GetConstantValue(constant, context.CancellationToken);
		if (attribute is null || constant is null || !constantValue.HasValue)
		{
			return;
		}

		ExpressionSyntax argument = constant;
		if (ContainsTargetTypedDefault(constant))
		{
			ITypeSymbol? constantType = semanticModel.GetTypeInfo(constant, context.CancellationToken).Type;
			if (LiteralFor(constantValue.Value, constantType) is not { } literal)
			{
				return;
			}

			argument = literal;
		}

		context.RegisterCodeFix(
			CodeAction.Create(
				"Use the member's constant",
				cancellationToken => ReplaceAsync(context.Document, attribute, argument, cancellationToken),
				equivalenceKey: nameof(UnionDiscriminatorCodeFix)),
			context.Diagnostics);
	}

	private static bool IsDiscriminator(SemanticModel semanticModel, AttributeSyntax attribute, CancellationToken cancellationToken)
	{
		ISymbol? symbol = semanticModel.GetSymbolInfo(attribute.Name, cancellationToken).Symbol;
		INamedTypeSymbol? type = symbol is IMethodSymbol method ? method.ContainingType : null;
		return type?.ToDisplayString() == "Nerdbank.MessagePack.UnionDiscriminatorAttribute";
	}

	private static ExpressionSyntax? ConstantExpression(MemberDeclarationSyntax member)
	{
		if (member is PropertyDeclarationSyntax property)
		{
			ExpressionSyntax? expression = property.ExpressionBody?.Expression;
			AccessorDeclarationSyntax? getter = property.AccessorList?.Accessors.FirstOrDefault(accessor => accessor.Keyword.IsKind(SyntaxKind.GetKeyword));
			expression ??= getter?.ExpressionBody?.Expression;
			if (expression is null && getter?.Body?.Statements is [ReturnStatementSyntax { Expression: { } returned }])
			{
				return returned;
			}

			return expression;
		}

		if (member is FieldDeclarationSyntax { Declaration.Variables: [VariableDeclaratorSyntax { Initializer: { } initializer }] })
		{
			return initializer.Value;
		}

		return null;
	}

	private static bool ContainsTargetTypedDefault(ExpressionSyntax expression)
		=> expression.DescendantNodesAndSelf().OfType<LiteralExpressionSyntax>().Any(literal => literal.IsKind(SyntaxKind.DefaultLiteralExpression));

	private static ExpressionSyntax? LiteralFor(object? value, ITypeSymbol? type)
	{
		if (value is null || type is null)
		{
			return null;
		}

		return value switch
		{
			bool boolean => SyntaxFactory.LiteralExpression(boolean ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression),
			string text => SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(text)),
			char character => SyntaxFactory.LiteralExpression(SyntaxKind.CharacterLiteralExpression, SyntaxFactory.Literal(character)),
			sbyte or byte or short or ushort or int => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture))),
			uint unsigned => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(unsigned)),
			long signed => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(signed)),
			ulong unsigned => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(unsigned)),
			_ when type.TypeKind == TypeKind.Enum => EnumLiteral(type, value),
			_ => null,
		};
	}

	private static ExpressionSyntax? EnumLiteral(ITypeSymbol type, object value)
	{
		if (type is not INamedTypeSymbol named)
		{
			return null;
		}

		foreach (ISymbol member in named.GetMembers())
		{
			if (member is IFieldSymbol { IsConst: true, HasConstantValue: true, ConstantValue: { } constant } && Equals(constant, value))
			{
				return SyntaxFactory.MemberAccessExpression(
					SyntaxKind.SimpleMemberAccessExpression,
					SyntaxFactory.ParseTypeName(named.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)),
					SyntaxFactory.IdentifierName(member.Name));
			}
		}

		return null;
	}

	private static async Task<Document> ReplaceAsync(Document document, AttributeSyntax attribute, ExpressionSyntax constant, CancellationToken cancellationToken)
	{
		SyntaxNode root = (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false))!;
		AttributeArgumentSyntax argument = SyntaxFactory.AttributeArgument(constant.WithoutTrivia());
		AttributeArgumentListSyntax arguments = SyntaxFactory.AttributeArgumentList(SyntaxFactory.SingletonSeparatedList(argument));
		AttributeSyntax updated = attribute.WithArgumentList(arguments).WithAdditionalAnnotations(Formatter.Annotation);
		return document.WithSyntaxRoot(root.ReplaceNode(attribute, updated));
	}
}
