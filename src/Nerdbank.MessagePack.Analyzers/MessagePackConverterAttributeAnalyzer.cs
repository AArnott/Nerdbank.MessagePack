﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MessagePackConverterAttributeAnalyzer : DiagnosticAnalyzer
{
	public const string InvalidConverterTypeDiagnosticId = "NBMsgPack020";
	public const string ConverterMissingDefaultCtorDiagnosticId = "NBMsgPack021";

	public static readonly DiagnosticDescriptor InvalidConverterTypeDescriptor = new(
		id: InvalidConverterTypeDiagnosticId,
		title: Strings.NBMsgPack020_Title,
		messageFormat: Strings.NBMsgPack020_MessageFormat,
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		helpLinkUri: AnalyzerUtilities.GetHelpLink(InvalidConverterTypeDiagnosticId));

	public static readonly DiagnosticDescriptor ConverterMissingDefaultCtorDescriptor = new(
		id: ConverterMissingDefaultCtorDiagnosticId,
		title: Strings.NBMsgPack021_Title,
		messageFormat: Strings.NBMsgPack021_MessageFormat,
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		helpLinkUri: AnalyzerUtilities.GetHelpLink(ConverterMissingDefaultCtorDiagnosticId));

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
		InvalidConverterTypeDescriptor,
		ConverterMissingDefaultCtorDescriptor,
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
					var appliedType = (INamedTypeSymbol)context.Symbol;
					AnalyzeAttributeOnSymbol(appliedType, appliedType);

					foreach (ISymbol member in appliedType.GetMembers())
					{
						switch (member)
						{
							case IPropertySymbol property:
								AnalyzeAttributeOnSymbol(property, property.Type);
								break;
							case IFieldSymbol field:
								AnalyzeAttributeOnSymbol(field, field.Type);
								break;
						}
					}

					void AnalyzeAttributeOnSymbol(ISymbol targetSymbol, ITypeSymbol appliedType)
					{
						if (targetSymbol.FindAttributes(referenceSymbols.MessagePackConverterAttribute).FirstOrDefault() is not { } att)
						{
							return;
						}

						if (att.ConstructorArguments is not [{ Value: INamedTypeSymbol converterType }])
						{
							return;
						}

						if (converterType is { IsUnboundGenericType: true, TypeArguments.Length: int requiredTypeArgsCount } && appliedType is INamedTypeSymbol { IsUnboundGenericType: false, TypeArguments: { } supplied } &&
							requiredTypeArgsCount == supplied.Length)
						{
							converterType = converterType.OriginalDefinition.Construct([.. supplied]);
						}

						if (!converterType.IsOrDerivedFrom(referenceSymbols.MessagePackConverter.Construct(appliedType)))
						{
							context.ReportDiagnostic(Diagnostic.Create(InvalidConverterTypeDescriptor, GetArgumentLocation(0), appliedType.Name));
						}
						else if (!converterType.OriginalDefinition.InstanceConstructors.Any(c => c.Parameters.IsEmpty && c.DeclaredAccessibility == Accessibility.Public))
						{
							context.ReportDiagnostic(Diagnostic.Create(ConverterMissingDefaultCtorDescriptor, GetArgumentLocation(0), appliedType.Name));
						}

						Location? GetArgumentLocation(int argumentIndex)
						{
							return AnalyzerUtilities.GetArgumentLocation(att, argumentIndex, context.CancellationToken)
								?? appliedType.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(context.CancellationToken).GetLocation();
						}
					}
				},
				SymbolKind.NamedType);
		});
	}
}
