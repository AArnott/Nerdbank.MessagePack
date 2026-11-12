// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Nerdbank.MessagePack.Analyzers;

/// <summary>
/// Reports an error when a struct implements <c>IMessagePackSerializationCallbacks</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SerializationCallbacksAnalyzer : DiagnosticAnalyzer
{
	/// <summary>
	/// The diagnostic ID reported when a struct implements <c>IMessagePackSerializationCallbacks</c>.
	/// </summary>
	public const string ReferenceTypeOnlyDiagnosticId = "NBMsgPack120";

	/// <summary>
	/// The diagnostic reported when a struct implements <c>IMessagePackSerializationCallbacks</c>.
	/// </summary>
	public static readonly DiagnosticDescriptor ReferenceTypeOnlyDescriptor = new(
		id: ReferenceTypeOnlyDiagnosticId,
		title: Strings.NBMsgPack120_Title,
		messageFormat: Strings.NBMsgPack120_MessageFormat,
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		helpLinkUri: AnalyzerUtilities.GetHelpLink(ReferenceTypeOnlyDiagnosticId));

	/// <inheritdoc/>
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
		ReferenceTypeOnlyDescriptor,
	];

	/// <inheritdoc/>
	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

		context.RegisterCompilationStartAction(
			context =>
			{
				if (!ReferenceSymbols.TryCreate(context.Compilation, out ReferenceSymbols? referenceSymbols))
				{
					return;
				}

				context.RegisterSyntaxNodeAction(
					context => AnalyzeTypeDeclaration(context, referenceSymbols.MessagePackSerializationCallbacks),
					SyntaxKind.StructDeclaration,
					SyntaxKind.RecordDeclaration,
					SyntaxKind.RecordStructDeclaration);
			});
	}

	private static void AnalyzeTypeDeclaration(SyntaxNodeAnalysisContext context, INamedTypeSymbol callbacksInterface)
	{
		if (context.Node is not TypeDeclarationSyntax declaration ||
			context.SemanticModel.GetDeclaredSymbol(declaration, context.CancellationToken) is not INamedTypeSymbol { TypeKind: TypeKind.Struct } type ||
			!ImplementsCallbacks(type, callbacksInterface) ||
			declaration.BaseList is null)
		{
			return;
		}

		foreach (BaseTypeSyntax baseTypeSyntax in declaration.BaseList.Types)
		{
			ITypeSymbol? implemented = context.SemanticModel.GetTypeInfo(baseTypeSyntax.Type, context.CancellationToken).Type;
			if (implemented is not null && IntroducesCallbacks(implemented, callbacksInterface))
			{
				context.ReportDiagnostic(Diagnostic.Create(
					ReferenceTypeOnlyDescriptor,
					baseTypeSyntax.Type.GetLocation(),
					type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
			}
		}
	}

	private static bool ImplementsCallbacks(ITypeSymbol type, INamedTypeSymbol callbacksInterface)
	{
		foreach (INamedTypeSymbol implemented in type.AllInterfaces)
		{
			if (SymbolEqualityComparer.Default.Equals(implemented, callbacksInterface))
			{
				return true;
			}
		}

		return false;
	}

	private static bool IntroducesCallbacks(ITypeSymbol type, INamedTypeSymbol callbacksInterface)
	{
		if (SymbolEqualityComparer.Default.Equals(type, callbacksInterface))
		{
			return true;
		}

		return ImplementsCallbacks(type, callbacksInterface);
	}
}
