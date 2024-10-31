// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NBMsgPack001ConsistentUseOfKeyAnalyzer : DiagnosticAnalyzer
{
	public const string DiagnosticId = "NBMsgPack001";

	public static readonly DiagnosticDescriptor Descriptor = new(
		id: DiagnosticId,
		title: Strings.NBMsgPack001_Title,
		messageFormat: Strings.NBMsgPack001_MessageFormat,
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		helpLinkUri: AnalyzerUtilities.GetHelpLink(DiagnosticId));

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Descriptor];

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

		context.RegisterCompilationStartAction(context =>
		{
			if (!context.Compilation.ReferencedAssemblyNames.Any(n => n.Name == "Nerdbank.MessagePack"))
			{
				return;
			}

			context.RegisterSymbolAction(
				context =>
				{
					this.SearchForInconsistentKeyUsage(context);
				},
				SymbolKind.NamedType);
		});
	}

	private void SearchForInconsistentKeyUsage(SymbolAnalysisContext context)
	{
		ITypeSymbol typeSymbol = (ITypeSymbol)context.Symbol;
		bool? keyAttributeApplied = null;
		foreach (ISymbol memberSymbol in typeSymbol.GetMembers())
		{
			if (!this.IsMemberSerialized(memberSymbol))
			{
				continue;
			}

			switch (memberSymbol)
			{
				case IPropertySymbol propertySymbol:
				case IFieldSymbol fieldSymbol:
					bool keyAppliedHere = memberSymbol.FindAttributes(Constants.KeyAttribute.TypeName, Constants.KeyAttribute.Namespace).Any();
					if (keyAttributeApplied is null)
					{
						keyAttributeApplied = keyAppliedHere;
					}
					else if (keyAttributeApplied != keyAppliedHere)
					{
						context.ReportDiagnostic(Diagnostic.Create(Descriptor, memberSymbol.Locations.First()));
					}

					break;
			}
		}
	}

	private bool IsMemberSerialized(ISymbol member)
	{
		AttributeData? propertyShapeAttribute = member.FindAttributes(Constants.GenerateShapeAttribute.TypeName, Constants.GenerateShapeAttribute.Namespace).FirstOrDefault();
		bool? ignored = propertyShapeAttribute?.NamedArguments.FirstOrDefault(a => a.Key == Constants.GenerateShapeAttribute.IgnoreProperty).Value.Value as bool?;
		return (member.DeclaredAccessibility is Accessibility.Public && ignored is not true)
			|| ignored is false;
	}
}
