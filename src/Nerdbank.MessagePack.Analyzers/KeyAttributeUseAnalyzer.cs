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

	/// <summary>
	/// Determines whether the given type is likely to be a mutable collection that would support IDeserializeInto{T}.
	/// This is a heuristic to match the behavior of StandardVisitor without having to replicate all its complex logic.
	/// </summary>
	/// <param name="type">The type to check.</param>
	/// <returns>True if the type is likely to support IDeserializeInto{T}; otherwise, false.</returns>
	private static bool IsLikelyMutableCollectionType(ITypeSymbol type)
	{
		if (type is not INamedTypeSymbol namedType)
		{
			return false;
		}

		// Check for well-known mutable collection types in System.Collections.Generic
		if (AnalyzerUtilities.IsInNamespace(type, ["System", "Collections", "Generic"]))
		{
			return namedType.Name switch
			{
				"List" when namedType.TypeArguments.Length == 1 => true,
				"HashSet" when namedType.TypeArguments.Length == 1 => true,
				"SortedSet" when namedType.TypeArguments.Length == 1 => true,
				"LinkedList" when namedType.TypeArguments.Length == 1 => true,
				"Queue" when namedType.TypeArguments.Length == 1 => true,
				"Stack" when namedType.TypeArguments.Length == 1 => true,
				"Dictionary" when namedType.TypeArguments.Length == 2 => true,
				"SortedDictionary" when namedType.TypeArguments.Length == 2 => true,
				"SortedList" when namedType.TypeArguments.Length == 2 => true,
				_ => false,
			};
		}

		// Check for well-known mutable collection types in System.Collections.Concurrent
		if (AnalyzerUtilities.IsInNamespace(type, ["System", "Collections", "Concurrent"]))
		{
			return namedType.Name switch
			{
				"ConcurrentQueue" when namedType.TypeArguments.Length == 1 => true,
				"ConcurrentStack" when namedType.TypeArguments.Length == 1 => true,
				"ConcurrentBag" when namedType.TypeArguments.Length == 1 => true,
				"ConcurrentDictionary" when namedType.TypeArguments.Length == 2 => true,
				_ => false,
			};
		}

		// Check for well-known mutable collection types in System.Collections
		if (AnalyzerUtilities.IsInNamespace(type, ["System", "Collections"]))
		{
			return namedType.Name switch
			{
				"ArrayList" => true,
				"Hashtable" => true,
				_ => false,
			};
		}

		return false;
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

		// Read-only properties and fields can still be serialized if they are collection types that support IDeserializeInto<T>
		if (isReadOnly)
		{
			ITypeSymbol? memberType = member switch
			{
				IPropertySymbol property => property.Type,
				IFieldSymbol field => field.Type,
				_ => null,
			};

			if (memberType is not null)
			{
				isReadOnly = !IsLikelyMutableCollectionType(memberType);
			}
		}

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
		// For structs, filter out the implicit parameterless constructor.
		static bool IsImplicitStructConstructor(IMethodSymbol ctor) =>
			ctor.ContainingType.TypeKind == TypeKind.Struct && ctor.Parameters.IsEmpty && ctor.IsImplicitlyDeclared;
		IMethodSymbol[] publicConstructors = [.. typeSymbol.InstanceConstructors
			.Where(ctor => ctor.DeclaredAccessibility == Accessibility.Public && !IsImplicitStructConstructor(ctor))];

		if (publicConstructors is [{ } candidate])
		{
			return candidate;
		}

		// There is not exactly one chosen constructor, so bail out.
		return null;
	}
}
