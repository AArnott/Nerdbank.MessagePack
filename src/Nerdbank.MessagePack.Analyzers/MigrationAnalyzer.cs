// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Nerdbank.MessagePack.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MigrationAnalyzer : DiagnosticAnalyzer
{
	public const string FormatterDiagnosticId = "NBMsgPack100";
	public const string FormatterAttributeDiagnosticId = "NBMsgPack101";
	public const string MessagePackObjectAttributeUsageDiagnosticId = "NBMsgPack102";
	public const string KeyAttributeUsageDiagnosticId = "NBMsgPack103";
	public const string IgnoreMemberAttributeUsageDiagnosticId = "NBMsgPack104";

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

	public static readonly DiagnosticDescriptor MessagePackObjectAttributeUsageDiagnostic = new(
		id: MessagePackObjectAttributeUsageDiagnosticId,
		title: Strings.NBMsgPack102_Title,
		messageFormat: Strings.NBMsgPack102_MessageFormat,
		category: "Migration",
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		helpLinkUri: AnalyzerUtilities.GetHelpLink(MessagePackObjectAttributeUsageDiagnosticId));

	public static readonly DiagnosticDescriptor KeyAttributeUsageDiagnostic = new(
		id: KeyAttributeUsageDiagnosticId,
		title: Strings.NBMsgPack103_Title,
		messageFormat: Strings.NBMsgPack103_MessageFormat,
		category: "Migration",
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		helpLinkUri: AnalyzerUtilities.GetHelpLink(KeyAttributeUsageDiagnosticId));

	public static readonly DiagnosticDescriptor IgnoreMemberAttributeUsageDiagnostic = new(
		id: IgnoreMemberAttributeUsageDiagnosticId,
		title: Strings.NBMsgPack104_Title,
		messageFormat: Strings.NBMsgPack104_MessageFormat,
		category: "Migration",
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		helpLinkUri: AnalyzerUtilities.GetHelpLink(IgnoreMemberAttributeUsageDiagnosticId));

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
		FormatterDiagnostic,
		FormatterAttributeDiagnostic,
		MessagePackObjectAttributeUsageDiagnostic,
		KeyAttributeUsageDiagnostic,
		IgnoreMemberAttributeUsageDiagnostic,
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
					// Look for applications of [Key]
					if (context.Symbol.FindAttributes(oldLibrarySymbols.KeyAttribute).FirstOrDefault() is AttributeData keyAttribute)
					{
						context.ReportDiagnostic(Diagnostic.Create(KeyAttributeUsageDiagnostic, keyAttribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation()));
					}

					// Look for applications of [IgnoreMember]
					if (context.Symbol.FindAttributes(oldLibrarySymbols.IgnoreMemberAttribute).FirstOrDefault() is AttributeData ignoreMemberAttribute)
					{
						context.ReportDiagnostic(Diagnostic.Create(IgnoreMemberAttributeUsageDiagnostic, ignoreMemberAttribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation()));
					}
				},
				SymbolKind.Property,
				SymbolKind.Field);

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
					if (target.FindAttributes(oldLibrarySymbols.MessagePackFormatterAttribute).FirstOrDefault() is
						{ ConstructorArguments: [{ Value: INamedTypeSymbol formatterType }] } attribute)
					{
						if (formatterType.IsAssignableTo(referenceSymbols.MessagePackConverterUnbound))
						{
							context.ReportDiagnostic(Diagnostic.Create(FormatterAttributeDiagnostic, attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation()));
						}
					}

					// Look for applications of [MessagePackObject]
					if (target.FindAttributes(oldLibrarySymbols.MessagePackObjectAttribute).FirstOrDefault() is AttributeData msgpackObjectAttribute)
					{
						context.ReportDiagnostic(Diagnostic.Create(MessagePackObjectAttributeUsageDiagnostic, msgpackObjectAttribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation()));
					}
				},
				SymbolKind.NamedType);
		});
	}
}
