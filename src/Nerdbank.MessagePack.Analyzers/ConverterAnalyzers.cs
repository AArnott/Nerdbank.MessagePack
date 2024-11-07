// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.Operations;

namespace Nerdbank.MessagePack.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ConverterAnalyzers : DiagnosticAnalyzer
{
	public const string CallbackToTopLevelSerializerDiagnosticId = "NBMsgPack030";

	public static readonly DiagnosticDescriptor CallbackToTopLevelSerializerDescriptor = new(
		CallbackToTopLevelSerializerDiagnosticId,
		title: Strings.NBMsgPack030_Title,
		messageFormat: Strings.NBMsgPack030_MessageFormat,
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
		CallbackToTopLevelSerializerDescriptor,
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

			INamedTypeSymbol unboundConverterBase = referenceSymbols.MessagePackConverter.ConstructUnboundGenericType();
			context.RegisterSymbolStartAction(
				context =>
				{
					INamedTypeSymbol target = (INamedTypeSymbol)context.Symbol;
					if (target.IsOrDerivedFrom(unboundConverterBase))
					{
						context.RegisterOperationBlockStartAction(context =>
						{
							context.RegisterOperationAction(
								context =>
								{
									switch (context.Operation)
									{
										case IObjectCreationOperation objectCreation:
											if (objectCreation.Type?.IsOrDerivedFrom(referenceSymbols.MessagePackSerializer) is true)
											{
												context.ReportDiagnostic(Diagnostic.Create(CallbackToTopLevelSerializerDescriptor, objectCreation.Syntax.GetLocation()));
											}

											break;
										case IInvocationOperation invocation:
											if (invocation.TargetMethod.ContainingSymbol is ITypeSymbol typeSymbol &&
												typeSymbol.IsOrDerivedFrom(referenceSymbols.MessagePackSerializer))
											{
												context.ReportDiagnostic(Diagnostic.Create(CallbackToTopLevelSerializerDescriptor, invocation.Syntax.GetLocation()));
											}

											break;
									}
								},
								OperationKind.ObjectCreation,
								OperationKind.Invocation);
						});
					}
				},
				SymbolKind.NamedType);
		});
	}
}
