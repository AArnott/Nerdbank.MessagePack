// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ShapeShift.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class RefParametersForRefStructsAnalyzer : DiagnosticAnalyzer
{
	public const string UseRefParametersForRefStructsDiagnosticId = "NBMsgPack050";

	public static readonly DiagnosticDescriptor UseRefParametersForRefStructsDiagnostic = new(
		id: UseRefParametersForRefStructsDiagnosticId,
		title: Strings.NBMsgPack050_Title,
		messageFormat: Strings.NBMsgPack050_MessageFormat,
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		helpLinkUri: AnalyzerUtilities.GetHelpLink(UseRefParametersForRefStructsDiagnosticId));

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
		UseRefParametersForRefStructsDiagnostic,
	];

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

		context.RegisterCompilationStartAction(
			context =>
			{
				if (!ReferenceSymbols.TryCreate(context.Compilation, out ReferenceSymbols? referenceSymbols))
				{
					return;
				}

				FrozenSet<ISymbol> guardedRefStructs = new[]
				{
					referenceSymbols.MessagePackReader,
					referenceSymbols.MessagePackStreamingReader,
					referenceSymbols.MessagePackWriter,
				}.ToFrozenSet<ISymbol>(SymbolEqualityComparer.Default);

				context.RegisterSymbolAction(
					context =>
					{
						IMethodSymbol method = (IMethodSymbol)context.Symbol;
						foreach (IParameterSymbol parameter in method.Parameters)
						{
							if (parameter.RefKind == RefKind.None && guardedRefStructs.Contains(parameter.Type))
							{
								Location? location = ((ParameterSyntax?)parameter.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(context.CancellationToken))?.Type?.GetLocation()
									?? parameter.Locations.FirstOrDefault();
								if (location is not null)
								{
									context.ReportDiagnostic(Diagnostic.Create(UseRefParametersForRefStructsDiagnostic, location, parameter.Type.Name));
								}
							}
						}
					},
					SymbolKind.Method);
			});
	}
}
