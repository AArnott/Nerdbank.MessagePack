// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Nerdbank.MessagePack.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class KnownSubTypeAnalyzers : DiagnosticAnalyzer
{
	public const string NonDerivedTypeDiagnosticId = "NBMsgPack010";

	public static readonly DiagnosticDescriptor NonDerivedTypeDescriptor = new(
		id: NonDerivedTypeDiagnosticId,
		title: Strings.NBMsgPack010_Title,
		messageFormat: Strings.NBMsgPack010_MessageFormat,
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		helpLinkUri: AnalyzerUtilities.GetHelpLink(NonDerivedTypeDiagnosticId));

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
		NonDerivedTypeDescriptor,
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

			context.RegisterSymbolAction(
				context =>
				{
					INamedTypeSymbol appliedSymbol = (INamedTypeSymbol)context.Symbol;
					AttributeData[] attributeDatas = context.Symbol.FindAttributes(referenceSymbols.KnownSubTypeAttribute).ToArray();
					foreach (AttributeData att in attributeDatas)
					{
						int? alias = att.ConstructorArguments[0].Value is int a ? a : null;
						ITypeSymbol? subType = att.ConstructorArguments[1].Value as ITypeSymbol;
						if (subType is null)
						{
							return;
						}

						if (!subType.IsAssignableTo(appliedSymbol))
						{
							Location? location = appliedSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(context.CancellationToken).GetLocation();
							if (att.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken) is AttributeSyntax { ArgumentList.Arguments: [_, AttributeArgumentSyntax typeArgSyntax] })
							{
								location = typeArgSyntax.GetLocation();
							}

							context.ReportDiagnostic(Diagnostic.Create(
								NonDerivedTypeDescriptor,
								location,
								subType.Name));
						}
					}
				},
				SymbolKind.NamedType);
		});
	}
}
