// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
		IMethodSymbol? deserializingConstructor = this.GetDeserializingConstructor(typeSymbol as INamedTypeSymbol, referenceSymbols);
		ImmutableHashSet<string> ctorParameterNames = deserializingConstructor?.Parameters.Select(p => p.Name).ToImmutableHashSet(StringComparer.OrdinalIgnoreCase) ?? ImmutableHashSet<string>.Empty;

		bool? keyAttributeApplied = null;
		Dictionary<int, ISymbol>? keysAssigned = null;
		foreach (ISymbol memberSymbol in typeSymbol.GetAllMembers())
		{
			switch (memberSymbol)
			{
				case IPropertySymbol:
				case IFieldSymbol:
					AttributeData? keyAttribute = memberSymbol.FindAttributes(referenceSymbols.KeyAttribute).FirstOrDefault();
					if (!this.IsMemberSerialized(memberSymbol, ctorParameterNames, referenceSymbols))
					{
						if (keyAttribute is not null)
						{
							Location? location = keyAttribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation()
								?? memberSymbol.Locations.FirstOrDefault();
							context.ReportDiagnostic(Diagnostic.Create(KeyOnNonSerializedMemberDescriptor, location));
						}

						continue;
					}

					if (AnalyzerUtilities.IsUnusedDataPacketMember(memberSymbol, referenceSymbols))
					{
						// Another analyzer scans this special member.
						continue;
					}

					if (keyAttributeApplied is null)
					{
						keyAttributeApplied = keyAttribute is not null;
					}
					else if (keyAttributeApplied != keyAttribute is not null)
					{
						Location? location = memberSymbol.Locations.FirstOrDefault(l => l.SourceTree is not null) ??
							typeSymbol.Locations.FirstOrDefault(l => l.SourceTree is not null);
						context.ReportDiagnostic(
							Diagnostic.Create(
								InconsistentUseDescriptor,
								location,
								$"{typeSymbol.Name}.{memberSymbol.Name}"));
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
									$"{priorUser.ContainingSymbol.Name}.{priorUser.Name}"));
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

	private bool IsMemberSerialized(ISymbol member, ImmutableHashSet<string> ctorParameterNames, ReferenceSymbols referenceSymbols)
	{
		bool isReadOnly = !ctorParameterNames.Contains(member.Name) && member switch
		{
			IPropertySymbol p => p.IsReadOnly,
			IFieldSymbol f => f.IsReadOnly,
			_ => false,
		};
		return AnalyzerUtilities.HasPropertyShape(member, referenceSymbols) && !isReadOnly;
	}

	private IMethodSymbol? GetDeserializingConstructor(INamedTypeSymbol? typeSymbol, ReferenceSymbols referenceSymbols)
	{
		if (typeSymbol is null)
		{
			return null;
		}

		// Find the constructor with the appropriate attribute.
		if (typeSymbol.InstanceConstructors.FirstOrDefault(ctor => ctor.FindAttributes(referenceSymbols.ConstructorShapeAttribute).Any()) is IMethodSymbol ctor)
		{
			return ctor;
		}

		// Fallback to the one acceptable constructor, if there is exactly one.
		if (typeSymbol.InstanceConstructors.Where(ctor => ctor.DeclaredAccessibility == Accessibility.Public).ToArray() is [{ } candidate])
		{
			return candidate;
		}

		// There is not exactly one chosen constructor, so bail out.
		return null;
	}
}
