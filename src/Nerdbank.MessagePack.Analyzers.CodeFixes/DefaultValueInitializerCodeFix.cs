// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Nerdbank.MessagePack.Analyzers.CodeFixes;

/// <summary>
/// Code fix provider to add DefaultValueAttribute to fields and properties with initializers.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp)]
[Shared]
public class DefaultValueInitializerCodeFix : CodeFixProvider
{
	public override ImmutableArray<string> FixableDiagnosticIds => [DefaultValueInitializerAnalyzer.MissingDefaultValueAttributeDiagnosticId];

	public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
		if (root is null)
		{
			return;
		}

		SemanticModel? semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
		if (semanticModel is null)
		{
			return;
		}

		Diagnostic diagnostic = context.Diagnostics[0];
		TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;

		// Find the member declaration
		SyntaxNode? node = root.FindNode(diagnosticSpan);
		if (node is null)
		{
			return;
		}

		// Find the containing member (field or property)
		VariableDeclaratorSyntax? variableDeclarator = node.AncestorsAndSelf().OfType<VariableDeclaratorSyntax>().FirstOrDefault();
		PropertyDeclarationSyntax? propertyDeclaration = node.AncestorsAndSelf().OfType<PropertyDeclarationSyntax>().FirstOrDefault();

		if (variableDeclarator?.Initializer is EqualsValueClauseSyntax fieldInitializer)
		{
			// It's a field. Only offer the fix when the declaration declares a single variable,
			// since a shared attribute list would incorrectly apply to every variable in the declaration
			// (e.g. `int First = 1, Second = 2;`).
			if (variableDeclarator.Parent is VariableDeclarationSyntax { Variables.Count: 1 } fieldDeclaration &&
				semanticModel.GetDeclaredSymbol(variableDeclarator, context.CancellationToken) is IFieldSymbol fieldSymbol &&
				TryCreateDefaultValueArgument(fieldInitializer.Value, fieldDeclaration.Type, fieldSymbol.Type, semanticModel, context.CancellationToken, out AttributeArgumentSyntax argument))
			{
				context.RegisterCodeFix(
					CodeAction.Create(
						"Add [DefaultValue(...)]",
						cancellationToken => Task.FromResult(this.AddDefaultValueAttribute(context.Document, root, variableDeclarator.Parent?.Parent, argument)),
						equivalenceKey: nameof(DefaultValueInitializerCodeFix)),
					diagnostic);
			}
		}
		else if (propertyDeclaration?.Initializer is EqualsValueClauseSyntax propertyInitializer)
		{
			// It's a property
			if (semanticModel.GetDeclaredSymbol(propertyDeclaration, context.CancellationToken) is IPropertySymbol propertySymbol &&
				TryCreateDefaultValueArgument(propertyInitializer.Value, propertyDeclaration.Type, propertySymbol.Type, semanticModel, context.CancellationToken, out AttributeArgumentSyntax argument))
			{
				context.RegisterCodeFix(
					CodeAction.Create(
						"Add [DefaultValue(...)]",
						cancellationToken => Task.FromResult(this.AddDefaultValueAttribute(context.Document, root, propertyDeclaration, argument)),
						equivalenceKey: nameof(DefaultValueInitializerCodeFix)),
					diagnostic);
			}
		}
	}

	private static bool TryCreateDefaultValueArgument(
		ExpressionSyntax initializerValue,
		TypeSyntax memberTypeSyntax,
		ITypeSymbol memberType,
		SemanticModel semanticModel,
		CancellationToken cancellationToken,
		out AttributeArgumentSyntax argument)
	{
		if (!semanticModel.GetConstantValue(initializerValue, cancellationToken).HasValue)
		{
			argument = null!;
			return false;
		}

		ExpressionSyntax attributeValue = initializerValue.WithoutTrivia();
		TypeInfo typeInfo = semanticModel.GetTypeInfo(initializerValue, cancellationToken);
		if (initializerValue.IsKind(SyntaxKind.DefaultLiteralExpression) ||
			(typeInfo.Type is not null && !SymbolEqualityComparer.Default.Equals(typeInfo.Type, memberType)))
		{
			attributeValue = SyntaxFactory.CastExpression(memberTypeSyntax.WithoutTrivia(), ParenthesizeForCast(attributeValue));
		}

		argument = SyntaxFactory.AttributeArgument(attributeValue);
		return true;
	}

	private static ExpressionSyntax ParenthesizeForCast(ExpressionSyntax expression)
		=> expression is BinaryExpressionSyntax or ConditionalExpressionSyntax
			? SyntaxFactory.ParenthesizedExpression(expression)
			: expression;

	private Document AddDefaultValueAttribute(Document document, SyntaxNode root, SyntaxNode? memberDeclaration, AttributeArgumentSyntax argument)
	{
		if (memberDeclaration is null)
		{
			return document;
		}

		AttributeArgumentSyntax[] args = [argument];

		AttributeSyntax attribute = SyntaxFactory.Attribute(
			SyntaxFactory.ParseName("global::System.ComponentModel.DefaultValue"),
			SyntaxFactory.AttributeArgumentList(SyntaxFactory.SeparatedList(args)));

		AttributeListSyntax attributeList = SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(attribute));

		SyntaxNode newMemberDeclaration = memberDeclaration switch
		{
			FieldDeclarationSyntax field => field.AddAttributeLists(attributeList),
			PropertyDeclarationSyntax property => property.AddAttributeLists(attributeList),
			_ => memberDeclaration,
		};

		SyntaxNode newRoot = root.ReplaceNode(memberDeclaration, newMemberDeclaration);
		return document.WithSyntaxRoot(newRoot);
	}
}
