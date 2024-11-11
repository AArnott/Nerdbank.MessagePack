// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Nerdbank.MessagePack.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MigrationAnalyzer : DiagnosticAnalyzer
{
	public const string FormatterDiagnosticId = "NBMsgPack100";
	public const string FormatterAttributeDiagnosticId = "NBMsgPack101";

	public static readonly DiagnosticDescriptor FormatterDiagnostic = new(
		id: FormatterDiagnosticId,
		title: Strings.NBMsgPack100_Title,
		messageFormat: Strings.NBMsgPack100_MessageFormat,
		category: "Migration",
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		helpLinkUri: AnalyzerUtilities.GetHelpLink(FormatterDiagnosticId));

	public static readonly DiagnosticDescriptor FormatterAttributeDiagnostic = new(
		id: FormatterAttributeDiagnosticId,
		title: Strings.NBMsgPack101_Title,
		messageFormat: Strings.NBMsgPack101_MessageFormat,
		category: "Migration",
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		helpLinkUri: AnalyzerUtilities.GetHelpLink(FormatterDiagnosticId));

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
		FormatterDiagnostic,
		FormatterAttributeDiagnostic,
	];

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

		context.RegisterCompilationStartAction(context =>
		{
			if (!ReferenceSymbols.TryCreate(context.Compilation, out ReferenceSymbols? referenceSymbols) ||
				!MessagePackCSharpReferenceSymbols.TryCreate(context.Compilation, out MessagePackCSharpReferenceSymbols? oldLibrarySymbols))
			{
				return;
			}

			context.RegisterSymbolAction(
				context =>
				{
					// Look for implementations of IMessagePackFormatter<T>.
					INamedTypeSymbol target = (INamedTypeSymbol)context.Symbol;
					if (target.IsAssignableTo(oldLibrarySymbols.IMessagePackFormatterOfT))
					{
						Location? location = ((BaseTypeDeclarationSyntax?)target.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(context.CancellationToken))?.Identifier.GetLocation()
							?? target.Locations.FirstOrDefault();
						context.ReportDiagnostic(Diagnostic.Create(FormatterDiagnostic, location));
					}

					// Look for applications of [MessagePackFormatter(typeof(...))]
					// and report any that actually reference a type that derives from the newer MessagePackConverter<T>.
					if (target.GetAttributes().FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, oldLibrarySymbols.MessagePackFormatterAttribute)) is
						{ ConstructorArguments: [{ Value: INamedTypeSymbol formatterType }] } attribute)
					{
						if (formatterType.IsAssignableTo(referenceSymbols.MessagePackConverterUnbound))
						{
							context.ReportDiagnostic(Diagnostic.Create(FormatterAttributeDiagnostic, attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation()));
						}
					}
				},
				SymbolKind.NamedType);
		});
	}
}
