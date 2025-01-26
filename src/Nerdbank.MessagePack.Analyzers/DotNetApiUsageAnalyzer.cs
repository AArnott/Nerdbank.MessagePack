// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Nerdbank.MessagePack.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DotNetApiUsageAnalyzer : DiagnosticAnalyzer
{
	public const string UseDotNetApiDiagnosticId = "NBMsgPack051";

	public static readonly DiagnosticDescriptor UseDotNetApiDescriptor = new(
		id: UseDotNetApiDiagnosticId,
		title: Strings.NBMsgPack051_Title,
		messageFormat: Strings.NBMsgPack051_MessageFormat,
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		helpLinkUri: AnalyzerUtilities.GetHelpLink(UseDotNetApiDiagnosticId));

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [UseDotNetApiDescriptor];

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

		context.RegisterCompilationStartAction(context =>
		{
			if (!ReferenceSymbols.TryCreate(context.Compilation, out ReferenceSymbols? referenceSymbols))
			{
				return;
			}

			ConcurrentDictionary<ISymbol, string?> obsoleteMessage = new(SymbolEqualityComparer.Default);
			context.RegisterOperationAction(
				context =>
				{
					Location? location = null;
					ISymbol? symbol = context.Operation switch
					{
						IInvocationOperation invocation => invocation.TargetMethod,
						IAttributeOperation attribute => attribute.Type,
						_ => null,
					};

					if (symbol is null)
					{
						return;
					}

					if (!obsoleteMessage.TryGetValue(symbol, out string? message))
					{
						message = symbol.FindAttributes(Constants.PreferDotNetAlternativeApiAttribute.TypeName, Constants.PreferDotNetAlternativeApiAttribute.Namespace)
							.FirstOrDefault()
							?.ConstructorArguments.FirstOrDefault().Value as string;
						obsoleteMessage.TryAdd(symbol, message);
					}

					if (message is not null)
					{
						location ??= context.Operation.Syntax.GetLocation();
						context.ReportDiagnostic(Diagnostic.Create(UseDotNetApiDescriptor, location, message));
					}
				},
				OperationKind.Invocation | OperationKind.Attribute);

			context.RegisterSymbolAction(
				context =>
				{
					if (context.Symbol is not INamedTypeSymbol declaredType)
					{
						return;
					}

					foreach (AttributeData att in declaredType.GetAttributes())
					{
						if (att.AttributeClass is null)
						{
							continue;
						}

						if (!obsoleteMessage.TryGetValue(att.AttributeClass, out string? message))
						{
							message = att.AttributeClass.FindAttributes(Constants.PreferDotNetAlternativeApiAttribute.TypeName, Constants.PreferDotNetAlternativeApiAttribute.Namespace)
								.FirstOrDefault()
								?.ConstructorArguments.FirstOrDefault().Value as string;
							obsoleteMessage.TryAdd(att.AttributeClass, message);
						}

						if (message is not null)
						{
							Location? location = att.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation();
							if (location is not null)
							{
								context.ReportDiagnostic(Diagnostic.Create(UseDotNetApiDescriptor, location, message));
							}
						}
					}
				},
				SymbolKind.NamedType);
		});
	}
}
