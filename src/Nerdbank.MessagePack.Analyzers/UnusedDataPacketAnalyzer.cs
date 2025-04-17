// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UnusedDataPacketAnalyzer : DiagnosticAnalyzer
{
	public const string MissingPropertyShapeDiagnosticId = "NBMsgPack060";
	public const string KeyAttributeDiagnosticId = "NBMsgPack061";
	public const string ShouldBePrivateDiagnosticId = "NBMsgPack062";

	public static readonly DiagnosticDescriptor MissingPropertyShapeDescriptor = new(
		id: MissingPropertyShapeDiagnosticId,
		title: Strings.NBMsgPack060_Title,
		messageFormat: Strings.NBMsgPack060_MessageFormat,
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		helpLinkUri: AnalyzerUtilities.GetHelpLink(MissingPropertyShapeDiagnosticId));

	public static readonly DiagnosticDescriptor KeyAttributeDescriptor = new(
		id: KeyAttributeDiagnosticId,
		title: Strings.NBMsgPack061_Title,
		messageFormat: Strings.NBMsgPack061_MessageFormat,
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		helpLinkUri: AnalyzerUtilities.GetHelpLink(KeyAttributeDiagnosticId));

	public static readonly DiagnosticDescriptor ShouldBePrivateDescriptor = new(
		id: ShouldBePrivateDiagnosticId,
		title: Strings.NBMsgPack062_Title,
		messageFormat: Strings.NBMsgPack062_MessageFormat,
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		helpLinkUri: AnalyzerUtilities.GetHelpLink(ShouldBePrivateDiagnosticId));

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
		MissingPropertyShapeDescriptor,
		KeyAttributeDescriptor,
		ShouldBePrivateDescriptor,
	];

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

				context.RegisterSymbolAction(
					context =>
					{
						if (AnalyzerUtilities.IsUnusedDataPacketMember(context.Symbol, referenceSymbols))
						{
							if (context.Symbol.DeclaredAccessibility != Accessibility.Private)
							{
								context.ReportDiagnostic(Diagnostic.Create(ShouldBePrivateDescriptor, context.Symbol.Locations[0], context.Symbol.Name, context.Symbol.ContainingSymbol.Name));
							}

							if (!AnalyzerUtilities.HasPropertyShape(context.Symbol, referenceSymbols))
							{
								context.ReportDiagnostic(Diagnostic.Create(MissingPropertyShapeDescriptor, context.Symbol.Locations[0], context.Symbol.Name, context.Symbol.ContainingSymbol.Name));
							}

							if (context.Symbol.FindAttributes(referenceSymbols.KeyAttribute).FirstOrDefault() is { } keyAttribute)
							{
								Location location = keyAttribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken)?.GetLocation() ?? context.Symbol.Locations[0];
								context.ReportDiagnostic(Diagnostic.Create(KeyAttributeDescriptor, location, context.Symbol.Name, context.Symbol.ContainingSymbol.Name));
							}
						}
					},
					SymbolKind.Property,
					SymbolKind.Field);
			});
	}
}
