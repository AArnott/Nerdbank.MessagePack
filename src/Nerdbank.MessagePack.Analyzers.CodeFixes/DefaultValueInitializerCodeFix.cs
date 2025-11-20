// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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

		Diagnostic diagnostic = context.Diagnostics[0];
		Microsoft.CodeAnalysis.Text.TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;

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
			// It's a field
			if (this.IsConstExpression(fieldInitializer.Value))
			{
				context.RegisterCodeFix(
					CodeAction.Create(
						"Add [DefaultValue(...)]",
						cancellationToken => this.AddDefaultValueAttributeAsync(context.Document, root, variableDeclarator.Parent?.Parent, fieldInitializer.Value, cancellationToken),
						equivalenceKey: nameof(DefaultValueInitializerCodeFix)),
					diagnostic);
			}
		}
		else if (propertyDeclaration?.Initializer is EqualsValueClauseSyntax propertyInitializer)
		{
			// It's a property
			if (this.IsConstExpression(propertyInitializer.Value))
			{
				context.RegisterCodeFix(
					CodeAction.Create(
						"Add [DefaultValue(...)]",
						cancellationToken => this.AddDefaultValueAttributeAsync(context.Document, root, propertyDeclaration, propertyInitializer.Value, cancellationToken),
						equivalenceKey: nameof(DefaultValueInitializerCodeFix)),
					diagnostic);
			}
		}
	}

	private bool IsConstExpression(ExpressionSyntax expression)
	{
		return expression is LiteralExpressionSyntax
			|| expression is MemberAccessExpressionSyntax // For enum values like TestEnum.Second
			|| expression is DefaultExpressionSyntax
			|| (expression is PrefixUnaryExpressionSyntax unary && this.IsConstExpression(unary.Operand)); // For negative numbers
	}

	private async Task<Document> AddDefaultValueAttributeAsync(Document document, SyntaxNode root, SyntaxNode? memberDeclaration, ExpressionSyntax initializerValue, CancellationToken cancellationToken)
	{
		if (memberDeclaration is null)
		{
			return document;
		}

		// Create the DefaultValue attribute
		string attributeArgument = initializerValue.ToString();

		// For member access expressions (enums), we need typeof(EnumType).GetField("EnumValue").GetRawConstantValue()
		// But for simplicity, we'll use the member access syntax directly as it's more readable
		AttributeArgumentSyntax[] args = [SyntaxFactory.AttributeArgument(SyntaxFactory.ParseExpression(attributeArgument))];

		AttributeSyntax attribute = SyntaxFactory.Attribute(
			SyntaxFactory.ParseName("System.ComponentModel.DefaultValue"),
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
