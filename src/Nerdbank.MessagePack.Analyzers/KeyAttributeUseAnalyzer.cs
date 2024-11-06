// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using TypeShape.Roslyn;

namespace Nerdbank.MessagePack.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class KeyAttributeUseAnalyzer : DiagnosticAnalyzer
{
	public const string InconsistentUseDiagnosticId = "NBMsgPack001";
	public const string KeyOnNonSerializedMemberDiagnosticId = "NBMsgPack002";
	public const string NonUniqueKeysDiagnosticId = "NBMsgPack003";

	public static readonly DiagnosticDescriptor InconsistentUseDescriptor = new(
		id: InconsistentUseDiagnosticId,
		title: Strings.NBMsgPack001_Title,
		messageFormat: Strings.NBMsgPack001_MessageFormat,
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		helpLinkUri: AnalyzerUtilities.GetHelpLink(InconsistentUseDiagnosticId));

	public static readonly DiagnosticDescriptor KeyOnNonSerializedMemberDescriptor = new(
		id: KeyOnNonSerializedMemberDiagnosticId,
		title: Strings.NBMsgPack002_Title,
		messageFormat: Strings.NBMsgPack002_MessageFormat,
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		helpLinkUri: AnalyzerUtilities.GetHelpLink(KeyOnNonSerializedMemberDiagnosticId));

	public static readonly DiagnosticDescriptor NonUniqueKeysDescriptor = new(
		id: NonUniqueKeysDiagnosticId,
		title: Strings.NBMsgPack003_Title,
		messageFormat: Strings.NBMsgPack003_MessageFormat,
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		helpLinkUri: AnalyzerUtilities.GetHelpLink(NonUniqueKeysDiagnosticId));

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
		InconsistentUseDescriptor,
		KeyOnNonSerializedMemberDescriptor,
		NonUniqueKeysDescriptor,
	];

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

		context.RegisterCompilationStartAction(context =>
		{
			TypeShape.Roslyn.KnownSymbols typeShapeSymbols = new(context.Compilation);
			TypeDataModelGenerator typeDataModelGenerator = new(context.Compilation.Assembly, typeShapeSymbols, context.CancellationToken);
			foreach (var model in typeDataModelGenerator.GeneratedModels)
			{

			}

			if (!ReferenceSymbols.TryCreate(context.Compilation, out ReferenceSymbols? referenceSymbols))
			{
				return;
			}

			context.RegisterSymbolAction(
				context =>
				{
					this.SearchForInconsistentKeyUsage(context, referenceSymbols);
				},
				SymbolKind.NamedType);
		});
	}

	private void SearchForInconsistentKeyUsage(SymbolAnalysisContext context, ReferenceSymbols referenceSymbols)
	{
		ITypeSymbol typeSymbol = (ITypeSymbol)context.Symbol;
		bool? keyAttributeApplied = null;
		Dictionary<int, ISymbol>? keysAssigned = null;
		foreach (ISymbol memberSymbol in typeSymbol.GetMembers())
		{
			switch (memberSymbol)
			{
				case IPropertySymbol:
				case IFieldSymbol:
					AttributeData? keyAttribute = memberSymbol.FindAttributes(referenceSymbols.KeyAttribute).FirstOrDefault();
					if (!this.IsMemberSerialized(memberSymbol, referenceSymbols))
					{
						if (keyAttribute is not null)
						{
							Location? location = keyAttribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation()
								?? memberSymbol.Locations.FirstOrDefault();
							context.ReportDiagnostic(Diagnostic.Create(KeyOnNonSerializedMemberDescriptor, location));
						}

						continue;
					}

					if (keyAttributeApplied is null)
					{
						keyAttributeApplied = keyAttribute is not null;
					}
					else if (keyAttributeApplied != keyAttribute is not null)
					{
						context.ReportDiagnostic(Diagnostic.Create(InconsistentUseDescriptor, memberSymbol.Locations.First()));
					}

					if (keyAttribute is not null)
					{
						keysAssigned ??= new();
						if (keyAttribute.ConstructorArguments is [{ Value: int index }])
						{
							if (keysAssigned.TryGetValue(index, out ISymbol? priorUser))
							{
								Location? location = keyAttribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation() ?? memberSymbol.Locations.First();
								Location[]? addlLocations = null;
								if (priorUser.DeclaringSyntaxReferences is [SyntaxReference priorLocation, ..])
								{
									addlLocations = [priorLocation.GetSyntax(context.CancellationToken).GetLocation()];
								}

								context.ReportDiagnostic(Diagnostic.Create(
									NonUniqueKeysDescriptor,
									location,
									addlLocations,
									priorUser.Name));
							}
							else
							{
								keysAssigned.Add(index, memberSymbol);
							}
						}
					}

					break;
			}
		}
	}

	private bool IsMemberSerialized(ISymbol member, ReferenceSymbols referenceSymbols)
	{
		AttributeData? propertyShapeAttribute = member.FindAttributes(referenceSymbols.PropertyShapeAttribute).FirstOrDefault();
		bool? ignored = propertyShapeAttribute?.NamedArguments.FirstOrDefault(a => a.Key == Constants.PropertyShapeAttribute.IgnoreProperty).Value.Value as bool?;
		return ignored is not true &&
			(member.DeclaredAccessibility is Accessibility.Public || propertyShapeAttribute is not null);
	}
}
