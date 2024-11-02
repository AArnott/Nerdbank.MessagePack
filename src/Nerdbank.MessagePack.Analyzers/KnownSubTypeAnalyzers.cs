// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Nerdbank.MessagePack.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class KnownSubTypeAnalyzers : DiagnosticAnalyzer
{
	public const string NonDerivedTypeDiagnosticId = "NBMsgPack010";
	public const string NonUniqueAliasDiagnosticId = "NBMsgPack011";
	public const string NonUniqueTypeDiagnosticId = "NBMsgPack012";
	public const string OpenGenericTypeDiagnosticId = "NBMsgPack013";
	public const string TypeIsMissingShapeDiagnosticId = "NBMsgPack014";

	public static readonly DiagnosticDescriptor NonDerivedTypeDescriptor = new(
		id: NonDerivedTypeDiagnosticId,
		title: Strings.NBMsgPack010_Title,
		messageFormat: Strings.NBMsgPack010_MessageFormat,
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		helpLinkUri: AnalyzerUtilities.GetHelpLink(NonDerivedTypeDiagnosticId));

	public static readonly DiagnosticDescriptor NonUniqueAliasDescriptor = new(
		id: NonUniqueAliasDiagnosticId,
		title: Strings.NBMsgPack011_Title,
		messageFormat: Strings.NBMsgPack011_MessageFormat,
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		helpLinkUri: AnalyzerUtilities.GetHelpLink(NonUniqueAliasDiagnosticId));

	public static readonly DiagnosticDescriptor NonUniqueTypeDescriptor = new(
		id: NonUniqueTypeDiagnosticId,
		title: Strings.NBMsgPack012_Title,
		messageFormat: Strings.NBMsgPack012_MessageFormat,
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		helpLinkUri: AnalyzerUtilities.GetHelpLink(NonUniqueTypeDiagnosticId));

	public static readonly DiagnosticDescriptor OpenGenericTypeDescriptor = new(
		id: OpenGenericTypeDiagnosticId,
		title: Strings.NBMsgPack013_Title,
		messageFormat: Strings.NBMsgPack013_MessageFormat,
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		helpLinkUri: AnalyzerUtilities.GetHelpLink(OpenGenericTypeDiagnosticId));

	////public static readonly DiagnosticDescriptor TypeIsMissingShapeDescriptor = new(
	////	id: TypeIsMissingShapeDiagnosticId,
	////	title: Strings.NBMsgPack014_Title,
	////	messageFormat: Strings.NBMsgPack014_MessageFormat,
	////	category: "Usage",
	////	defaultSeverity: DiagnosticSeverity.Error,
	////	isEnabledByDefault: true,
	////	helpLinkUri: AnalyzerUtilities.GetHelpLink(TypeIsMissingShapeDiagnosticId));

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
		NonDerivedTypeDescriptor,
		NonUniqueAliasDescriptor,
		NonUniqueTypeDescriptor,
		OpenGenericTypeDescriptor,
		////TypeIsMissingShapeDescriptor,
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
					AttributeData[] attributeDatas = context.Symbol.FindAttributes(referenceSymbols.IKnownSubTypeAttribute).ToArray();
					Dictionary<int, ITypeSymbol?>? typesByAlias = null;
					Dictionary<ITypeSymbol, int?>? aliasesByType = null;
					foreach (AttributeData att in attributeDatas)
					{
						int? alias = att.ConstructorArguments is [{ Value: int a }] ? a : null;
						ITypeSymbol? subType = att.AttributeClass?.TypeArguments[0];

						if (alias is not null)
						{
							typesByAlias ??= new();
							if (typesByAlias.TryGetValue(alias.Value, out ITypeSymbol? existingAssignment))
							{
								context.ReportDiagnostic(Diagnostic.Create(
									NonUniqueAliasDescriptor,
									GetArgumentLocation(0)));
							}
							else
							{
								typesByAlias.Add(alias.Value, subType);
							}
						}

						if (subType is not null)
						{
							aliasesByType ??= new(SymbolEqualityComparer.Default);
							if (aliasesByType.TryGetValue(subType, out int? existingAlias))
							{
								context.ReportDiagnostic(Diagnostic.Create(
									NonUniqueTypeDescriptor,
									GetTypeArgumentLocation(0),
									existingAlias));
							}
							else
							{
								aliasesByType.Add(subType, alias);
							}
						}

						if (subType?.IsAssignableTo(appliedSymbol) is false)
						{
							context.ReportDiagnostic(Diagnostic.Create(
								NonDerivedTypeDescriptor,
								GetTypeArgumentLocation(0),
								subType.Name));
						}

						if (subType is INamedTypeSymbol { IsUnboundGenericType: true })
						{
							context.ReportDiagnostic(Diagnostic.Create(
								OpenGenericTypeDescriptor,
								GetTypeArgumentLocation(0)));
						}

						Location? GetArgumentLocation(int argumentIndex)
							=> AnalyzerUtilities.GetArgumentLocation(att, argumentIndex, context.CancellationToken)
								?? att.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation()
								?? appliedSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(context.CancellationToken).GetLocation();

						Location? GetTypeArgumentLocation(int typeArgumentIndex)
							=> AnalyzerUtilities.GetTypeArgumentLocation(att, typeArgumentIndex, context.CancellationToken)
								?? att.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation()
								?? appliedSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(context.CancellationToken).GetLocation();
					}
				},
				SymbolKind.NamedType);
		});
	}
}
