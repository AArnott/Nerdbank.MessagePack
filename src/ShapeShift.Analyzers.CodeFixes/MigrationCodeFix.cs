// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ShapeShift.Analyzers.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp)]
public class MigrationCodeFix : CodeFixProvider
{
	private static readonly NameSyntax Namespace = IdentifierName("ShapeShift");
	private static readonly NameSyntax ConverterNamespace = QualifiedName(IdentifierName("ShapeShift"), IdentifierName("Converters"));
	private static readonly IdentifierNameSyntax ContextParameterName = IdentifierName("context");
	private static readonly AttributeSyntax GenerateShapeAttribute = Attribute(NameInNamespace(IdentifierName("GenerateShape"), IdentifierName("PolyType")));
	private static readonly AttributeSyntax ConstructorShapeAttribute = Attribute(NameInNamespace(IdentifierName("ConstructorShape"), IdentifierName("PolyType")));

	public override ImmutableArray<string> FixableDiagnosticIds => [
		MigrationAnalyzer.FormatterDiagnosticId,
		MigrationAnalyzer.FormatterAttributeDiagnosticId,
		MigrationAnalyzer.MessagePackObjectAttributeUsageDiagnosticId,
		MigrationAnalyzer.KeyAttributeUsageDiagnosticId,
		MigrationAnalyzer.IgnoreMemberAttributeUsageDiagnosticId,
		MigrationAnalyzer.CallbackReceiverDiagnosticId,
		MigrationAnalyzer.SerializationConstructorDiagnosticId,
	];

	public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		foreach (Diagnostic diagnostic in context.Diagnostics)
		{
			switch (diagnostic.Id)
			{
				case MigrationAnalyzer.FormatterDiagnosticId:
					context.RegisterCodeFix(
						CodeAction.Create(
							title: "Migrate to Converter<T>",
							createChangedDocument: cancellationToken => this.MigrateToConverterAsync(context.Document, diagnostic.Location.SourceSpan, cancellationToken),
							equivalenceKey: "Migrate to Converter<T>"),
						diagnostic);
					break;
				case MigrationAnalyzer.FormatterAttributeDiagnosticId:
					context.RegisterCodeFix(
						CodeAction.Create(
							title: "Migrate to ConverterAttribute",
							createChangedDocument: cancellationToken => this.MigrateToConverterAttributeAsync(context.Document, diagnostic.Location.SourceSpan, cancellationToken),
							equivalenceKey: "Migrate to ConverterAttribute"),
						diagnostic);
					break;
				case MigrationAnalyzer.MessagePackObjectAttributeUsageDiagnosticId:
					context.RegisterCodeFix(
						CodeAction.Create(
							title: "Remove MessagePackObjectAttribute and add GenerateShapeAttribute if necessary",
							createChangedDocument: cancellationToken => this.RemoveMessagePackObjectAttributeAsync(context.Document, diagnostic.Location.SourceSpan, cancellationToken),
							equivalenceKey: "Remove MessagePackObjectAttribute"),
						diagnostic);
					break;
				case MigrationAnalyzer.KeyAttributeUsageDiagnosticId:
					// Determine whether to replace with KeyAttribute or PropertyShapeAttribute.
					SyntaxNode? tree = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
					if (tree?.FindNode(context.Span) is AttributeSyntax keyAttribute && IsStringKeyAttribute(keyAttribute))
					{
						context.RegisterCodeFix(
							CodeAction.Create(
								title: "Use PropertyShape(Name) instead",
								createChangedDocument: cancellationToken => this.ReplaceKeyAttributeAsync(context.Document, diagnostic.Location.SourceSpan, cancellationToken),
								equivalenceKey: "Use PropertyShapeAttribute.Name"),
							diagnostic);
					}
					else
					{
						context.RegisterCodeFix(
							CodeAction.Create(
								title: "Use ShapeShift.KeyAttribute",
								createChangedDocument: cancellationToken => this.ReplaceKeyAttributeAsync(context.Document, diagnostic.Location.SourceSpan, cancellationToken),
								equivalenceKey: "Use ShapeShift.KeyAttribute"),
							diagnostic);
					}

					break;
				case MigrationAnalyzer.IgnoreMemberAttributeUsageDiagnosticId:
					// Determine whether to remove or replace the attribute and offer the appropriate code fix for it.
					tree = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
					if (tree?.FindNode(context.Span) is AttributeSyntax { Parent.Parent: MemberDeclarationSyntax member } att)
					{
						if (member.Modifiers.Any(SyntaxKind.PublicKeyword))
						{
							context.RegisterCodeFix(
								CodeAction.Create(
									title: "Replace IgnoreMemberAttribute with PropertyShapeAttribute",
									createChangedDocument: cancellationToken => this.ReplaceIgnoreMemberAttribute(context.Document, att, false, cancellationToken),
									equivalenceKey: "Replace IgnoreMemberAttribute with PropertyShapeAttribute"),
								diagnostic);
						}
						else
						{
							context.RegisterCodeFix(
								CodeAction.Create(
									title: "Remove IgnoreMemberAttribute",
									createChangedDocument: cancellationToken => this.ReplaceIgnoreMemberAttribute(context.Document, att, true, cancellationToken),
									equivalenceKey: "Remove IgnoreMemberAttribute"),
								diagnostic);
						}
					}

					break;
				case MigrationAnalyzer.CallbackReceiverDiagnosticId:
					context.RegisterCodeFix(
						CodeAction.Create(
							title: "Implement ISerializationCallbacks instead",
							createChangedDocument: cancellationToken => this.ImplementSerializationCallbacksAsync(context.Document, diagnostic, cancellationToken),
							equivalenceKey: "Implement ISerializationCallbacks"),
						diagnostic);
					break;
				case MigrationAnalyzer.SerializationConstructorDiagnosticId:
					context.RegisterCodeFix(
					CodeAction.Create(
						title: "Use ConstructorShape instead",
						createChangedDocument: cancellationToken => this.ReplaceSerializationConstructorAttribute(context.Document, diagnostic.Location.SourceSpan, cancellationToken),
						equivalenceKey: "Use ConstructorShape instead"),
					diagnostic);
					break;
			}
		}
	}

	private static bool IsStringKeyAttribute(AttributeSyntax attribute)
	{
		return attribute is { ArgumentList.Arguments: [AttributeArgumentSyntax { Expression: LiteralExpressionSyntax { RawKind: (int)SyntaxKind.StringLiteralExpression } }] };
	}

	private static NameSyntax NameInNamespace(SimpleNameSyntax name) => NameInNamespace(name, Namespace);

	private static NameSyntax NameInNamespace(SimpleNameSyntax name, NameSyntax @namespace) => QualifiedName(@namespace, name).WithAdditionalAnnotations(Simplifier.AddImportsAnnotation, Simplifier.Annotation);

	private async Task<Document> MigrateToConverterAsync(Document document, TextSpan sourceSpan, CancellationToken cancellationToken)
	{
		Compilation? compilation = await document.Project.GetCompilationAsync(cancellationToken);
		if (compilation is null)
		{
			return document;
		}

		if (!MessagePackCSharpReferenceSymbols.TryCreate(compilation, out MessagePackCSharpReferenceSymbols? oldLibrarySymbols) ||
			!ReferenceSymbols.TryCreate(compilation, out ReferenceSymbols? referenceSymbols))
		{
			return document;
		}

		CompilationUnitSyntax? root = (CompilationUnitSyntax?)await document.GetSyntaxRootAsync(cancellationToken);
		if (root?.FindNode(sourceSpan) is not ClassDeclarationSyntax formatterType)
		{
			return document;
		}

		SemanticModel? semanticModel = await document.GetSemanticModelAsync(cancellationToken);
		INamedTypeSymbol? formatterSymbol = semanticModel.GetDeclaredSymbol(formatterType, cancellationToken);
		INamedTypeSymbol? formatterInterfaceSymbol = formatterSymbol?.AllInterfaces.FirstOrDefault(i => SymbolEqualityComparer.Default.Equals(i.ConstructUnboundGenericType(), oldLibrarySymbols.IMessagePackFormatterOfTUnbound));

		root = (CompilationUnitSyntax)root.Accept(new FormatterMigrationVisitor(semanticModel, oldLibrarySymbols, cancellationToken)
		{
			FormatterDeclaration = formatterType,
			FormatterInterfaceSymbol = formatterInterfaceSymbol,
			SerializeMethod = formatterInterfaceSymbol?.GetMembers("Serialize").FirstOrDefault() is IMethodSymbol serializeMethodSymbol
					? formatterSymbol?.FindImplementationForInterfaceMember(serializeMethodSymbol)?.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(cancellationToken) as MethodDeclarationSyntax
					: null,
			DeserializeMethod = formatterInterfaceSymbol?.GetMembers("Deserialize").FirstOrDefault() is IMethodSymbol deserializeMethodSymbol
					? formatterSymbol?.FindImplementationForInterfaceMember(deserializeMethodSymbol)?.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(cancellationToken) as MethodDeclarationSyntax
					: null,
		})!;

		return await this.AddImportAndSimplifyAsync(document.WithSyntaxRoot(root), cancellationToken);
	}

	private async Task<Document> MigrateToConverterAttributeAsync(Document document, TextSpan sourceSpan, CancellationToken cancellationToken)
	{
		SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken);
		if (root?.FindNode(sourceSpan) is not AttributeSyntax attribute)
		{
			return document;
		}

		// - [MessagePackFormatter(typeof(MyTypeFormatter))]
		// + [Converter(typeof(MyTypeFormatter))]
		root = root.ReplaceNode(
			attribute,
			attribute.WithName(NameInNamespace(IdentifierName("Converter"), Namespace)));

		return await this.AddImportAndSimplifyAsync(document.WithSyntaxRoot(root), cancellationToken);
	}

	private async Task<Document> RemoveMessagePackObjectAttributeAsync(Document document, TextSpan sourceSpan, CancellationToken cancellationToken)
	{
		CompilationUnitSyntax? root = (CompilationUnitSyntax?)await document.GetSyntaxRootAsync(cancellationToken);
		if (root is null)
		{
			return document;
		}

		if (root.FindNode(sourceSpan) is not AttributeSyntax originalAttribute)
		{
			return document;
		}

		BaseTypeDeclarationSyntax? originalAttributedTypeSyntax = originalAttribute.Parent?.Parent as BaseTypeDeclarationSyntax;

		root = originalAttributedTypeSyntax is not null ? root.TrackNodes(originalAttribute, originalAttributedTypeSyntax) : root.TrackNodes(originalAttribute);
		AttributeSyntax attribute = root.GetCurrentNode(originalAttribute) ?? throw new InvalidOperationException();

		// If this type appears in a call to MessagePackSerializer, make it partial and tack on [GenerateShape].
		bool attributeRemoved = false;
		if (originalAttributedTypeSyntax is not null &&
			await document.GetSemanticModelAsync(cancellationToken) is SemanticModel semanticModel &&
			semanticModel?.GetDeclaredSymbol(originalAttributedTypeSyntax, cancellationToken) is INamedTypeSymbol attributedTypeSymbol)
		{
			Compilation? compilation = await document.Project.GetCompilationAsync(cancellationToken);
			if (compilation is not null &&
				MessagePackCSharpReferenceSymbols.TryCreate(compilation, out MessagePackCSharpReferenceSymbols? oldLibSymbols) &&
				ReferenceSymbols.TryCreate(compilation, out ReferenceSymbols? referenceSymbols) &&
				await this.IsTypeUsedInSerializerCallAsync(oldLibSymbols, attributedTypeSymbol, document.Project.Solution, cancellationToken) &&
				root.GetCurrentNode(originalAttributedTypeSyntax) is BaseTypeDeclarationSyntax attributedTypeSyntax)
			{
				BaseTypeDeclarationSyntax modified = attributedTypeSyntax;
				if (!modified.Modifiers.Any(SyntaxKind.PartialKeyword))
				{
					modified = modified.AddModifiers(Token(SyntaxKind.PartialKeyword));
				}

				root = root.ReplaceNode(attributedTypeSyntax, modified);

				if (!attributedTypeSymbol.FindAttributes(referenceSymbols.GenerateShapeAttribute).Any() && root.GetCurrentNode(originalAttribute) is { } attToReplace)
				{
					attributeRemoved = true;
					root = root.ReplaceNode(attToReplace, GenerateShapeAttribute);
				}
			}
		}

		if (!attributeRemoved)
		{
			if (attribute.Parent is AttributeListSyntax { Attributes.Count: 1 })
			{
				// Remove the whole list.
				root = root.RemoveNode(attribute.Parent, SyntaxRemoveOptions.KeepLeadingTrivia)!;
			}
			else
			{
				// Remove just the attribute.
				root = root.RemoveNode(attribute, SyntaxRemoveOptions.KeepNoTrivia)!;
			}
		}

		return await this.AddImportAndSimplifyAsync(document.WithSyntaxRoot(root), cancellationToken);
	}

	private async Task<bool> IsTypeUsedInSerializerCallAsync(MessagePackCSharpReferenceSymbols oldLibSymbols, INamedTypeSymbol dataTypeSymbol, Solution solution, CancellationToken cancellationToken)
	{
		IEnumerable<ReferencedSymbol> references = await SymbolFinder.FindReferencesAsync(dataTypeSymbol, solution, cancellationToken);
		foreach (ReferenceLocation referenceLocation in references.SelectMany(r => r.Locations))
		{
			foreach (DocumentId docId in solution.GetDocumentIdsWithFilePath(referenceLocation.Location.SourceTree?.FilePath))
			{
				if (solution.GetDocument(docId) is Document doc)
				{
					SemanticModel? docSemanticModel = await doc.GetSemanticModelAsync(cancellationToken);
					SyntaxNode? docRoot = await doc.GetSyntaxRootAsync(cancellationToken);
					if (docRoot is not null && docSemanticModel is not null &&
						docRoot.FindNode(referenceLocation.Location.SourceSpan).FirstAncestorOrSelf<InvocationExpressionSyntax>() is { Expression: MemberAccessExpressionSyntax { Expression: { } receiver } } &&
						docSemanticModel.GetSymbolInfo(receiver, cancellationToken).Symbol is INamedTypeSymbol receiverTypeSymbol &&
						SymbolEqualityComparer.Default.Equals(receiverTypeSymbol, oldLibSymbols.MessagePackSerializer))
					{
						return true;
					}
				}
			}
		}

		return false;
	}

	private async Task<Document> ReplaceKeyAttributeAsync(Document document, TextSpan sourceSpan, CancellationToken cancellationToken)
	{
		CompilationUnitSyntax? root = (CompilationUnitSyntax?)await document.GetSyntaxRootAsync(cancellationToken);
		if (root is null)
		{
			return document;
		}

		if (root.FindNode(sourceSpan) is not AttributeSyntax attribute)
		{
			return document;
		}

		// One of:
		// => PropertyShape(Name = "")
		// => [Key(n)]
		AttributeSyntax newAttribute = IsStringKeyAttribute(attribute)
			? Attribute(NameInNamespace(IdentifierName("PropertyShape"), IdentifierName("PolyType")))
				.AddArgumentListArguments(attribute.ArgumentList!.Arguments[0].WithNameEquals(NameEquals(IdentifierName("Name"))).WithAdditionalAnnotations(Formatter.Annotation))
			: attribute.WithName(NameInNamespace(IdentifierName("Key")));

		root = root.ReplaceNode(attribute, newAttribute);

		return await this.AddImportAndSimplifyAsync(document.WithSyntaxRoot(root), cancellationToken);
	}

	private async Task<Document> ReplaceIgnoreMemberAttribute(Document document, AttributeSyntax attribute, bool remove, CancellationToken cancellationToken)
	{
		CompilationUnitSyntax? root = (CompilationUnitSyntax)await attribute.SyntaxTree.GetRootAsync(cancellationToken);

		if (remove)
		{
			root = attribute.Parent is AttributeListSyntax { Attributes.Count: 1 }
				? root.RemoveNode(attribute.Parent, SyntaxRemoveOptions.KeepEndOfLine)
				: root.RemoveNode(attribute, SyntaxRemoveOptions.KeepNoTrivia);
		}
		else
		{
			AttributeSyntax propertyShapeAttribute = Attribute(NameInNamespace(IdentifierName("PropertyShape"), IdentifierName("PolyType")))
				.AddArgumentListArguments(
					AttributeArgument(LiteralExpression(SyntaxKind.TrueLiteralExpression)).WithNameEquals(NameEquals(IdentifierName("Ignore"))).WithAdditionalAnnotations(Formatter.Annotation));
			root = root.ReplaceNode(attribute, propertyShapeAttribute);
		}

		if (root is null)
		{
			return document;
		}

		return await this.AddImportAndSimplifyAsync(document.WithSyntaxRoot(root), cancellationToken);
	}

	private async Task<Document> ImplementSerializationCallbacksAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
	{
		CompilationUnitSyntax? root = (CompilationUnitSyntax?)await document.GetSyntaxRootAsync(cancellationToken);
		if (root is null)
		{
			return document;
		}

		if (root.FindNode(diagnostic.Location.SourceSpan) is BaseTypeSyntax baseType)
		{
			List<MethodDeclarationSyntax> methods = new();
			foreach (Location addl in diagnostic.AdditionalLocations)
			{
				if (root.FindNode(addl.SourceSpan) is MethodDeclarationSyntax method)
				{
					methods.Add(method);
				}
			}

			root = root.TrackNodes([baseType, .. methods]);

			if (root.GetCurrentNode(baseType) is { } currentBaseType)
			{
				root = root.ReplaceNode(
					currentBaseType,
					SimpleBaseType(NameInNamespace(IdentifierName("ISerializationCallbacks"), Namespace)));
			}

			foreach (MethodDeclarationSyntax oldMethod in methods)
			{
				if (root.GetCurrentNode(oldMethod) is MethodDeclarationSyntax currentMethod)
				{
					root = root.ReplaceNode(
						currentMethod,
						currentMethod.WithExplicitInterfaceSpecifier(ExplicitInterfaceSpecifier(IdentifierName("ISerializationCallbacks"))));
				}
			}
		}

		return await this.AddImportAndSimplifyAsync(document.WithSyntaxRoot(root), cancellationToken);
	}

	private async Task<Document> ReplaceSerializationConstructorAttribute(Document document, TextSpan sourceSpan, CancellationToken cancellationToken)
	{
		CompilationUnitSyntax? root = (CompilationUnitSyntax?)await document.GetSyntaxRootAsync(cancellationToken);
		if (root is null)
		{
			return document;
		}

		if (root.FindNode(sourceSpan) is not AttributeSyntax originalAttribute)
		{
			return document;
		}

		root = root.ReplaceNode(
			originalAttribute,
			ConstructorShapeAttribute);

		return await this.AddImportAndSimplifyAsync(document.WithSyntaxRoot(root), cancellationToken);
	}

	private async Task<Document> AddImportAndSimplifyAsync(Document document, CancellationToken cancellationToken)
	{
		Document modifiedDocument = document;
		modifiedDocument = await ImportAdder.AddImportsAsync(document, cancellationToken: cancellationToken);
		modifiedDocument = await Simplifier.ReduceAsync(modifiedDocument, cancellationToken: cancellationToken);
		modifiedDocument = await Formatter.FormatAsync(modifiedDocument, cancellationToken: cancellationToken);
		return modifiedDocument;
	}

	private class FormatterMigrationVisitor(SemanticModel? semanticModel, MessagePackCSharpReferenceSymbols oldLibrarySymbols, CancellationToken cancellationToken) : CSharpSyntaxRewriter
	{
		private static readonly SyntaxAnnotation MadePartial = new();

		private readonly FrozenDictionary<IMethodSymbol, string> methodNameReplacements = new Dictionary<IMethodSymbol, string>(SymbolEqualityComparer.Default)
		{
			[oldLibrarySymbols.ReaderTryReadNil] = "TryReadNull",
			[oldLibrarySymbols.WriterWriteNil] = "WriteNull",
			[oldLibrarySymbols.ReaderReadArrayHeader] = "ReadStartVector",
			[oldLibrarySymbols.WriterWriteArrayHeader] = "WriteStartVector",
			[oldLibrarySymbols.ReaderReadMapHeader] = "ReadStartMap",
			[oldLibrarySymbols.WriterWriteMapHeader] = "WriteStartMap",
		}.ToFrozenDictionary(SymbolEqualityComparer.Default);

		private readonly Dictionary<string, TypeSyntax> requiredWitnesses = new(StringComparer.Ordinal);

		public required INamedTypeSymbol? FormatterInterfaceSymbol { get; init; }

		public required ClassDeclarationSyntax FormatterDeclaration { get; init; }

		public required MethodDeclarationSyntax? SerializeMethod { get; init; }

		public required MethodDeclarationSyntax? DeserializeMethod { get; init; }

		public override SyntaxNode? VisitStructDeclaration(StructDeclarationSyntax node) => node;

		public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
		{
			// Only visit inside if it's the formatter class we care about.
			if (node == this.FormatterDeclaration)
			{
				SyntaxNode? result = base.VisitClassDeclaration(node);

				if (this.requiredWitnesses.Count > 0 && result is ClassDeclarationSyntax classDecl)
				{
					AttributeListSyntax[] attributeLists = this.requiredWitnesses.Values.Select(
						type => AttributeList().AddAttributes(Attribute(QualifiedName(IdentifierName("PolyType"), GenericName("GenerateShape").AddTypeArgumentListArguments(type))))).ToArray();
					classDecl = classDecl.AddAttributeLists(attributeLists);

					if (!classDecl.Modifiers.Any(SyntaxKind.PartialKeyword))
					{
						classDecl = classDecl
							.AddModifiers(Token(SyntaxKind.PartialKeyword))
							.WithAdditionalAnnotations(MadePartial);
					}

					result = classDecl;
				}

				return result;
			}
			else
			{
				// look for nested types that need to be marked with [GenerateShape]
				SyntaxNode? result = base.VisitClassDeclaration(node);

				// If any child nodes were made partial, we need the containing type to be partial as well.
				if (result is BaseTypeDeclarationSyntax baseTypeDecl &&
					baseTypeDecl.ContainsAnnotations &&
					baseTypeDecl.DescendantNodesAndSelf(n => n is BaseTypeDeclarationSyntax).Any(t => t.HasAnnotation(MadePartial)) &&
					!baseTypeDecl.Modifiers.Any(SyntaxKind.PartialKeyword))
				{
					result = baseTypeDecl.AddModifiers(Token(SyntaxKind.PartialKeyword));
				}

				return result;
			}
		}

		public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
		{
			if (semanticModel.GetSymbolInfo(node, cancellationToken).Symbol is IMethodSymbol methodSymbol)
			{
				{
					// - reader.Skip();
					// + reader.Skip(context);
					if (SymbolEqualityComparer.Default.Equals(methodSymbol, oldLibrarySymbols.ReaderSkip))
					{
						return node.AddArgumentListArguments(Argument(ContextParameterName));
					}
				}

				{
					// - options.Security.DepthStep(ref reader);
					// + context.DepthStep();
					if (SymbolEqualityComparer.Default.Equals(methodSymbol, oldLibrarySymbols.DepthStep))
					{
						return node
							.WithExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, ContextParameterName, IdentifierName("DepthStep")))
							.WithArgumentList(ArgumentList());
					}
				}

				{
					if (methodSymbol is { IsGenericMethod: false, ContainingType: INamedTypeSymbol { IsGenericType: true } containingSymbol })
					{
						// Unbind the container and reconstruct the method symbol.
						IMethodSymbol unboundMethod = containingSymbol.ConstructedFrom.GetMembers(methodSymbol.Name).OfType<IMethodSymbol>().FirstOrDefault();

						// - (formatter).Deserialize(ref reader, options)
						// + (converter).Read(ref reader, context)
						if (node.ArgumentList.Arguments.Count == 2 && SymbolEqualityComparer.Default.Equals(unboundMethod, oldLibrarySymbols.IMessagePackFormatterDeserialize))
						{
							node = (InvocationExpressionSyntax)base.VisitInvocationExpression(node)!;
							if (node.Expression is MemberAccessExpressionSyntax memberAccess)
							{
								node = node.WithExpression(memberAccess.WithName(IdentifierName("Read")));
							}

							return node.ReplaceNode(node.ArgumentList.Arguments[1], Argument(ContextParameterName));
						}

						// - (formatter).Serialize(ref writer, value, options)
						// + (converter).Write(ref writer, value, context)
						if (node.ArgumentList.Arguments.Count == 3 && SymbolEqualityComparer.Default.Equals(unboundMethod, oldLibrarySymbols.IMessagePackFormatterSerialize))
						{
							node = (InvocationExpressionSyntax)base.VisitInvocationExpression(node)!;
							if (node.Expression is MemberAccessExpressionSyntax memberAccess)
							{
								node = node.WithExpression(memberAccess.WithName(IdentifierName("Write")));
							}

							return node.ReplaceNode(node.ArgumentList.Arguments[2], Argument(ContextParameterName));
						}
					}
				}

				{
					// - options.Resolver.GetFormatterWithVerify<string>()
					// + context.GetConverter<string>(ThisConverter.ShapeProvider)
					if (methodSymbol is { IsGenericMethod: true } &&
						(SymbolEqualityComparer.Default.Equals(methodSymbol.ConstructedFrom, oldLibrarySymbols.GetFormatterWithVerify) ||
						 SymbolEqualityComparer.Default.Equals(methodSymbol.ConstructedFrom, oldLibrarySymbols.GetFormatterWithVerify.ReduceExtensionMethod(oldLibrarySymbols.IFormatterResolver))))
					{
						if (node.Expression is MemberAccessExpressionSyntax { Name: GenericNameSyntax { TypeArgumentList: { Arguments: [TypeSyntax typeArg] } } })
						{
							if (typeArg is NullableTypeSyntax { ElementType: { } nullableElement })
							{
								typeArg = nullableElement;
							}

							string typeArgString = typeArg.ToString();
							if (!this.requiredWitnesses.ContainsKey(typeArgString))
							{
								this.requiredWitnesses.Add(typeArgString, typeArg);
							}

							GenericNameSyntax getConverter = GenericName("GetConverter").AddTypeArgumentListArguments(typeArg);
							return node
								.WithExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, ContextParameterName, getConverter))
								.WithArgumentList(ArgumentList().AddArguments(
									Argument(MemberAccessExpression(
										SyntaxKind.SimpleMemberAccessExpression,
										IdentifierName(this.FormatterDeclaration.Identifier),
										IdentifierName("ShapeProvider")))));
						}
					}
				}

				{
					// Trivial method name replacements.
					if (this.methodNameReplacements.TryGetValue(methodSymbol, out string? newName) && node is { Expression: MemberAccessExpressionSyntax { Name: { } methodName } })
					{
						return node.ReplaceNode(methodName, IdentifierName(newName));
					}
				}
			}

			return base.VisitInvocationExpression(node);
		}

		public override SyntaxNode? VisitExpressionStatement(ExpressionStatementSyntax node)
		{
			// - reader.Depth--;
			switch (node.Expression)
			{
				case PrefixUnaryExpressionSyntax prefix:
					if (SymbolEqualityComparer.Default.Equals(semanticModel.GetSymbolInfo(prefix.Operand, cancellationToken).Symbol, oldLibrarySymbols.ReaderDepth))
					{
						return null;
					}

					break;
				case PostfixUnaryExpressionSyntax postfix:
					if (SymbolEqualityComparer.Default.Equals(semanticModel.GetSymbolInfo(postfix.Operand, cancellationToken).Symbol, oldLibrarySymbols.ReaderDepth))
					{
						return null;
					}

					break;
			}

			return base.VisitExpressionStatement(node);
		}

		public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
		{
			if (node == this.SerializeMethod)
			{
				MethodDeclarationSyntax result = (MethodDeclarationSyntax)base.VisitMethodDeclaration(node)!;

				// - public void Serialize(ref MessagePackWriter writer, MyType value, MessagePackSerializerOptions options)
				// + public override void Write(ref ShapeShift.MessagePackWriter writer, in MyType value, SerializationContext context)

				// Add override modifier.
				result = result.AddModifiers(Token(SyntaxKind.OverrideKeyword));

				// Add `in` modifier to second parameter.
				result = result.ReplaceNode(
					result.ParameterList.Parameters[1],
					result.ParameterList.Parameters[1].AddModifiers(Token(SyntaxKind.InKeyword)));

				// Replace last parameter.
				result = ReplaceOptionsParameterWithContext(result, 2);

				// Fix the first parameter type name.
				result = result.ReplaceNode(
					result.ParameterList.Parameters[0],
					result.ParameterList.Parameters[0].WithType(NameInNamespace(IdentifierName("Writer"), ConverterNamespace)));

				// Rename the method
				result = result.WithIdentifier(Identifier("Write"));

				return result;
			}
			else if (node == this.DeserializeMethod)
			{
				MethodDeclarationSyntax result = (MethodDeclarationSyntax)base.VisitMethodDeclaration(node)!;

				// - public MyType Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
				// + public override MyType Read(ref ShapeShift.Converters.Reader reader, SerializationContext context)

				// Add override modifier.
				result = result.AddModifiers(Token(SyntaxKind.OverrideKeyword));

				// Replace last parameter.
				result = ReplaceOptionsParameterWithContext(result, 1);

				// Fix the first parameter type name.
				result = result.ReplaceNode(
					result.ParameterList.Parameters[0],
					result.ParameterList.Parameters[0].WithType(NameInNamespace(IdentifierName("Reader"), ConverterNamespace)));

				// Rename the method
				result = result.WithIdentifier(Identifier("Read"));

				return result;
			}
			else
			{
				// Do not process anything within a method that we're not interested in.
				return node;
			}

			MethodDeclarationSyntax ReplaceOptionsParameterWithContext(MethodDeclarationSyntax method, int paramIndex)
			{
				return method.ParameterList.Parameters.Count <= paramIndex ? method : method.ReplaceNode(
					method.ParameterList.Parameters[paramIndex],
					Parameter(ContextParameterName.Identifier).WithType(NameInNamespace(IdentifierName("SerializationContext"), ConverterNamespace)));
			}
		}

		public override SyntaxNode? VisitBlock(BlockSyntax node)
		{
			SyntaxNode? result = base.VisitBlock(node);

			// Replace try blocks with no catch blocks and no or empty finally blocks with just the statements within the try blocks.
			if (result is BlockSyntax blockResult)
			{
				TryStatementSyntax[] pointlessTryBlocks = blockResult.Statements.OfType<TryStatementSyntax>().Where(s => s is { Catches.Count: 0, Finally: null or { Block.Statements.Count: 0 } }).ToArray();
				blockResult = blockResult.TrackNodes(pointlessTryBlocks);
				foreach (TryStatementSyntax oldPointlessTryBlock in pointlessTryBlocks)
				{
					blockResult = blockResult.InsertNodesAfter(blockResult.GetCurrentNode(oldPointlessTryBlock)!, oldPointlessTryBlock.Block.Statements.Select(s => s.WithAdditionalAnnotations(Formatter.Annotation)));
					blockResult = blockResult.RemoveNode(blockResult.GetCurrentNode(oldPointlessTryBlock)!, SyntaxRemoveOptions.KeepNoTrivia)!;
				}

				result = blockResult;
			}

			return result;
		}

		public override SyntaxNode? VisitBaseList(BaseListSyntax node)
		{
			foreach (BaseTypeSyntax baseType in node.Types)
			{
				if (baseType.Type is not GenericNameSyntax { TypeArgumentList.Arguments: [TypeSyntax typeArg] } genericBaseType)
				{
					continue;
				}

				TypeInfo baseTypeInfo = semanticModel.GetTypeInfo(genericBaseType, cancellationToken);
				if (SymbolEqualityComparer.Default.Equals(baseTypeInfo.Type, this.FormatterInterfaceSymbol))
				{
					// Remove IMessagePackFormatter<T>
					node = node.RemoveNode(baseType, SyntaxRemoveOptions.KeepNoTrivia)!;

					// We do not want the nullable annotation on the type argument for reference or value types.
					if (typeArg is NullableTypeSyntax nullableType)
					{
						typeArg = nullableType.ElementType;
					}

					// Insert Converter<T> as the first base type.
					// It must be first because it's a derived class, whereas the interface we just removed could have appeared anywhere.
					NameSyntax converterBaseType = NameInNamespace(GenericName("Converter").AddTypeArgumentListArguments(typeArg), ConverterNamespace);
					node = node.WithTypes(node.Types.Insert(0, SimpleBaseType(converterBaseType)));

					break;
				}
			}

			return base.VisitBaseList(node);
		}
	}
}
