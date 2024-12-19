// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Nerdbank.MessagePack.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ConverterAnalyzers : DiagnosticAnalyzer
{
	public const string CallbackToTopLevelSerializerDiagnosticId = "NBMsgPack030";
	public const string NotExactlyOneStructureDiagnosticId = "NBMsgPack031";
	public const string OverrideGetJsonSchemaDiagnosticId = "NBMsgPack032";
	public const string AsyncConverterShouldReturnWriterDiagnosticId = "NBMsgPack033";
	public const string AsyncConverterShouldNotReuseWriterDiagnosticId = "NBMsgPack034";
	public const string AsyncConverterShouldReturnReaderDiagnosticId = "NBMsgPack035";
	public const string AsyncConverterShouldNotReuseReaderDiagnosticId = "NBMsgPack036";
	public const string AsyncConverterShouldOverridePreferAsyncSerializationDiagnosticId = "NBMsgPack037";

	public static readonly DiagnosticDescriptor CallbackToTopLevelSerializerDescriptor = new(
		CallbackToTopLevelSerializerDiagnosticId,
		title: Strings.NBMsgPack030_Title,
		messageFormat: Strings.NBMsgPack030_MessageFormat,
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		helpLinkUri: AnalyzerUtilities.GetHelpLink(CallbackToTopLevelSerializerDiagnosticId));

	public static readonly DiagnosticDescriptor NotExactlyOneStructureDescriptor = new(
		NotExactlyOneStructureDiagnosticId,
		title: Strings.NBMsgPack031_Title,
		messageFormat: Strings.NBMsgPack031_MessageFormat,
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		helpLinkUri: AnalyzerUtilities.GetHelpLink(NotExactlyOneStructureDiagnosticId));

	public static readonly DiagnosticDescriptor OverrideGetJsonSchemaDescriptor = new(
		OverrideGetJsonSchemaDiagnosticId,
		title: Strings.NBMsgPack032_Title,
		messageFormat: Strings.NBMsgPack032_MessageFormat,
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		helpLinkUri: AnalyzerUtilities.GetHelpLink(CallbackToTopLevelSerializerDiagnosticId));

	public static readonly DiagnosticDescriptor AsyncConverterShouldReturnWriterDescriptor = new(
		AsyncConverterShouldReturnWriterDiagnosticId,
		title: Strings.NBMsgPack033_Title,
		messageFormat: Strings.NBMsgPack033_MessageFormat,
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		helpLinkUri: AnalyzerUtilities.GetHelpLink(AsyncConverterShouldReturnWriterDiagnosticId));

	public static readonly DiagnosticDescriptor AsyncConverterShouldNotReuseWriterDescriptor = new(
		AsyncConverterShouldNotReuseWriterDiagnosticId,
		title: Strings.NBMsgPack034_Title,
		messageFormat: Strings.NBMsgPack034_MessageFormat,
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		helpLinkUri: AnalyzerUtilities.GetHelpLink(AsyncConverterShouldNotReuseWriterDiagnosticId));

	public static readonly DiagnosticDescriptor AsyncConverterShouldReturnReaderDescriptor = new(
		AsyncConverterShouldReturnReaderDiagnosticId,
		title: Strings.NBMsgPack035_Title,
		messageFormat: Strings.NBMsgPack035_MessageFormat,
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		helpLinkUri: AnalyzerUtilities.GetHelpLink(AsyncConverterShouldReturnReaderDiagnosticId));

	public static readonly DiagnosticDescriptor AsyncConverterShouldNotReuseReaderDescriptor = new(
		AsyncConverterShouldNotReuseReaderDiagnosticId,
		title: Strings.NBMsgPack036_Title,
		messageFormat: Strings.NBMsgPack036_MessageFormat,
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		helpLinkUri: AnalyzerUtilities.GetHelpLink(AsyncConverterShouldNotReuseReaderDiagnosticId));

	public static readonly DiagnosticDescriptor AsyncConverterShouldOverridePreferAsyncSerializationDescriptor = new(
		AsyncConverterShouldOverridePreferAsyncSerializationDiagnosticId,
		title: Strings.NBMsgPack037_Title,
		messageFormat: Strings.NBMsgPack037_MessageFormat,
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true,
		helpLinkUri: AnalyzerUtilities.GetHelpLink(AsyncConverterShouldOverridePreferAsyncSerializationDiagnosticId));

	private static readonly AsyncRentalReturnAnalysisInputs AsyncReadRentalAnalysis = new()
	{
		RentalMethodNames = new[] { "CreateStreamingReader", "CreateBufferedReader" }.ToFrozenSet(StringComparer.Ordinal),
		ReturnMethodNames = new[] { "ReturnReader" }.ToFrozenSet(StringComparer.Ordinal),
		ReturnBeforeMethodTester = name => name.StartsWith("ReadAsync"),
		ReturnRentalFirst = AsyncConverterShouldReturnReaderDescriptor,
		DoNotReuseRental = AsyncConverterShouldNotReuseReaderDescriptor,
	};

	private static readonly AsyncRentalReturnAnalysisInputs AsyncWriteRentalAnalysis = new()
	{
		RentalMethodNames = new[] { "CreateWriter" }.ToFrozenSet(StringComparer.Ordinal),
		ReturnMethodNames = new[] { "ReturnWriter" }.ToFrozenSet(StringComparer.Ordinal),
		ReturnBeforeMethodTester = name => name.StartsWith("Flush") || name.StartsWith("Write"),
		ReturnRentalFirst = AsyncConverterShouldReturnWriterDescriptor,
		DoNotReuseRental = AsyncConverterShouldNotReuseWriterDescriptor,
	};

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
		CallbackToTopLevelSerializerDescriptor,
		NotExactlyOneStructureDescriptor,
		OverrideGetJsonSchemaDescriptor,
		AsyncConverterShouldReturnWriterDescriptor,
		AsyncConverterShouldNotReuseWriterDescriptor,
		AsyncConverterShouldReturnReaderDescriptor,
		AsyncConverterShouldNotReuseReaderDescriptor,
		AsyncConverterShouldOverridePreferAsyncSerializationDescriptor,
	];

	public override void Initialize(AnalysisContext context)
	{
		if (!Debugger.IsAttached)
		{
			context.EnableConcurrentExecution();
		}

		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

		context.RegisterCompilationStartAction(context =>
		{
			if (!ReferenceSymbols.TryCreate(context.Compilation, out ReferenceSymbols? referenceSymbols))
			{
				return;
			}

			INamedTypeSymbol unboundConverterBase = referenceSymbols.MessagePackConverter.ConstructUnboundGenericType();
			context.RegisterSymbolStartAction(
				context =>
				{
					INamedTypeSymbol target = (INamedTypeSymbol)context.Symbol;
					if (target.IsOrDerivedFrom(unboundConverterBase))
					{
						bool isAsyncConverter = target.GetAllMembers().Any(m => m is IMethodSymbol { Name: "ReadAsync" or "WriteAsync", OverriddenMethod: not null });

						context.RegisterOperationBlockStartAction(context =>
						{
							context.RegisterOperationAction(
								context =>
								{
									switch (context.Operation)
									{
										case IObjectCreationOperation objectCreation:
											if (objectCreation.Type?.IsOrDerivedFrom(referenceSymbols.MessagePackSerializer) is true)
											{
												context.ReportDiagnostic(Diagnostic.Create(CallbackToTopLevelSerializerDescriptor, objectCreation.Syntax.GetLocation()));
											}

											break;
										case IInvocationOperation invocation:
											if (invocation.TargetMethod.ContainingSymbol is ITypeSymbol typeSymbol &&
												typeSymbol.IsOrDerivedFrom(referenceSymbols.MessagePackSerializer))
											{
												context.ReportDiagnostic(Diagnostic.Create(CallbackToTopLevelSerializerDescriptor, invocation.Syntax.GetLocation()));
											}

											break;
									}
								},
								OperationKind.ObjectCreation,
								OperationKind.Invocation);

							context.RegisterOperationBlockEndAction(context => this.AnalyzeStructureCounts(context, referenceSymbols));

							if (context.OwningSymbol is IMethodSymbol { Name: "ReadAsync" or "WriteAsync", OverriddenMethod: not null })
							{
								bool isReader = context.OwningSymbol.Name == "ReadAsync";
								AsyncRentalReturnAnalysisInputs inputs = isReader ? AsyncReadRentalAnalysis : AsyncWriteRentalAnalysis;
								context.RegisterOperationBlockEndAction(
									context => this.AnalyzeSyncRentalUsage(context, referenceSymbols, inputs));
							}
						});

						if (!SymbolEqualityComparer.Default.Equals(target, referenceSymbols.MessagePackConverter) && !target.IsAbstract)
						{
							context.RegisterSymbolEndAction(context =>
							{
								INamedTypeSymbol symbol = (INamedTypeSymbol)context.Symbol;
								if (!symbol.GetAllMembers().Any(m => m is IMethodSymbol { Name: "GetJsonSchema", OverriddenMethod: not null }))
								{
									if (symbol.Locations.FirstOrDefault(l => l.IsInSource) is { } location)
									{
										context.ReportDiagnostic(Diagnostic.Create(OverrideGetJsonSchemaDescriptor, location));
									}
								}

								if (isAsyncConverter)
								{
									// This converter specifically implements async functionality.
									IPropertySymbol? prefersAsyncSerialization = symbol.GetAllMembers().OfType<IPropertySymbol>().FirstOrDefault(p => p is { Name: "PreferAsyncSerialization", OverriddenProperty: not null });
									if (prefersAsyncSerialization is null)
									{
										context.ReportDiagnostic(Diagnostic.Create(AsyncConverterShouldOverridePreferAsyncSerializationDescriptor, symbol.Locations.FirstOrDefault(l => l.IsInSource)));
									}
								}
							});
						}
					}
				},
				SymbolKind.NamedType);
		});
	}

	private void AnalyzeStructureCounts(OperationBlockAnalysisContext context, ReferenceSymbols referenceSymbols)
	{
		if (context.OwningSymbol is not IMethodSymbol { IsOverride: true } method)
		{
			return;
		}

		int? GetVariableImpact(IInvocationOperation invocation) => invocation.Arguments.Length > 0 && invocation.Arguments[0].Value is ILiteralOperation { ConstantValue.Value: int count } ? -count : null;

		Func<IInvocationOperation, (int? Impact, bool Unconditional)> relevantMethodTest;
		switch (method.Name)
		{
			case "Write":
				relevantMethodTest = i =>
				{
					if (SymbolEqualityComparer.Default.Equals(referenceSymbols.MessagePackWriter, i.TargetMethod.ContainingSymbol))
					{
						return i.TargetMethod.Name switch
						{
							"WriteArrayHeader" => (1 + GetVariableImpact(i), true),
							"WriteMapHeader" => (1 + (GetVariableImpact(i) * 2), true),
							string t when t.StartsWith("Write", StringComparison.Ordinal) => (1, true),
							"GetSpan" or "Advance" => (null, true), // Advance case, which we'll just assume they're doing correctly.
							_ => (0, true),
						};
					}
					else if (i.TargetMethod.ContainingSymbol is ITypeSymbol s && s.IsOrDerivedFrom(referenceSymbols.MessagePackConverterUnbound))
					{
						return i.TargetMethod.Name switch
						{
							"Write" or "WriteAsync" => (1, true),
							_ => (0, true),
						};
					}
					else
					{
						return (0, true);
					}
				};

				break;
			case "Read":
				relevantMethodTest = i =>
				{
					if (SymbolEqualityComparer.Default.Equals(referenceSymbols.MessagePackReader, i.TargetMethod.ContainingSymbol))
					{
						return i.TargetMethod.Name switch
						{
							"ReadArrayHeader" or "ReadMapHeader" => (null, true), // Advanced case, which we'll just assume they're doing correctly.
							"TryReadArrayHeader" or "TryReadMapHeader" => (null, false), // Advanced case, which we'll just assume they're doing correctly.
							"TryReadNil" => (1, false),
							"TryReadStringSpan" => (1, false),
							"Skip" => (1, true),
							string t when t.StartsWith("Read", StringComparison.Ordinal) => (1, true),
							_ => (0, true),
						};
					}
					else if (i.TargetMethod.ContainingSymbol is ITypeSymbol s && s.IsOrDerivedFrom(referenceSymbols.MessagePackConverterUnbound))
					{
						return i.TargetMethod.Name switch
						{
							"Read" or "ReadAsync" => (1, true),
							_ => (0, true),
						};
					}
					else
					{
						return (0, true);
					}
				};

				break;
			default:
				return;
		}

		foreach (IOperation block in context.OperationBlocks)
		{
			if (block.Kind != OperationKind.Block)
			{
				continue;
			}

			ControlFlowGraph flow = context.GetControlFlowGraph(block);
			if (flow.Blocks[0].FallThroughSuccessor?.Destination is { } initialBlock)
			{
				VisitBlock(initialBlock, 0, ImmutableHashSet<BasicBlock>.Empty);
			}

			void VisitBlock(BasicBlock basicBlock, int ops, ImmutableHashSet<BasicBlock> recursionGuard)
			{
				if (recursionGuard.Contains(basicBlock))
				{
					return;
				}

				recursionGuard = recursionGuard.Add(basicBlock);

				bool TestOperation(IOperation op)
				{
					if (op is IInvocationOperation invocation)
					{
						(int? impact, bool unconditional) = relevantMethodTest(invocation);
						if (impact.HasValue)
						{
							if (!unconditional)
							{
								// The code doesn't care whether it is actually reading a token or not, which is a bug.
								context.ReportDiagnostic(Diagnostic.Create(NotExactlyOneStructureDescriptor, op.Syntax.GetLocation()));
								return true;
							}

							ops += impact.Value;
							if (ops > 1)
							{
								// Too many structures.
								context.ReportDiagnostic(Diagnostic.Create(NotExactlyOneStructureDescriptor, op.Syntax.GetLocation()));
								return true;
							}
						}
						else
						{
							// Non-deterministic situation. Skip testing.
							return true;
						}
					}

					return false;
				}

				foreach (IOperation op in basicBlock.Operations.SelectMany(op => op.DescendantsAndSelf()))
				{
					if (TestOperation(op))
					{
						return;
					}
				}

				if (basicBlock.ConditionKind == ControlFlowConditionKind.None && basicBlock.BranchValue is not null)
				{
					if (TestOperation(basicBlock.BranchValue))
					{
						return;
					}

					foreach (IOperation op in basicBlock.BranchValue.ChildOperations.SelectMany(op => op.DescendantsAndSelf()))
					{
						if (TestOperation(op))
						{
							return;
						}
					}
				}

				if (ops < 1 && basicBlock.Kind == BasicBlockKind.Exit)
				{
					// Insufficient structures.
					// Ideally the location should be the path that led to this point.
					Location location = ((MethodDeclarationSyntax)method.DeclaringSyntaxReferences[0].GetSyntax(context.CancellationToken)).Identifier.GetLocation();
					context.ReportDiagnostic(Diagnostic.Create(NotExactlyOneStructureDescriptor, location));
					return;
				}

				int? branchValueImpact = 0;
				bool branchValueUnconditional = true;
				if (basicBlock.ConditionKind != ControlFlowConditionKind.None)
				{
					switch (basicBlock.BranchValue)
					{
						case IInvocationOperation conditionInvocation:
							(branchValueImpact, branchValueUnconditional) = relevantMethodTest(conditionInvocation);
							break;
						case IBinaryOperation binaryOperation:
							if (TestOperation(binaryOperation.LeftOperand) || TestOperation(binaryOperation.RightOperand))
							{
								return;
							}

							break;
						case IIsPatternOperation patternOperation:
							foreach (IOperation op in basicBlock.BranchValue.ChildOperations.SelectMany(op => op.DescendantsAndSelf()))
							{
								if (TestOperation(op))
								{
									return;
								}
							}

							break;
					}
				}

				if (branchValueImpact is null)
				{
					return;
				}

				if (basicBlock.FallThroughSuccessor?.Destination is BasicBlock nextBlock)
				{
					VisitBlock(nextBlock, basicBlock.ConditionKind == ControlFlowConditionKind.WhenFalse || branchValueUnconditional ? ops + branchValueImpact.Value : ops, recursionGuard);
				}

				if (basicBlock.ConditionalSuccessor?.Destination is BasicBlock conditionalBlock)
				{
					VisitBlock(conditionalBlock, basicBlock.ConditionKind == ControlFlowConditionKind.WhenTrue || branchValueUnconditional ? ops + branchValueImpact.Value : ops, recursionGuard);
				}
			}
		}
	}

	private void AnalyzeSyncRentalUsage(OperationBlockAnalysisContext context, ReferenceSymbols referenceSymbols, AsyncRentalReturnAnalysisInputs inputs)
	{
		IMethodSymbol containingMethod = (IMethodSymbol)context.OwningSymbol;
		IParameterSymbol asyncIO = containingMethod.Parameters[0];

		foreach (IOperation block in context.OperationBlocks)
		{
			if (block.Kind != OperationKind.Block)
			{
				continue;
			}

			ControlFlowGraph flow = context.GetControlFlowGraph(block);
			if (flow.Blocks[0].FallThroughSuccessor?.Destination is { } initialBlock)
			{
				VisitBlock(initialBlock, null, ImmutableHashSet.Create<ILocalSymbol>(SymbolEqualityComparer.Default), ImmutableHashSet<BasicBlock>.Empty, null);
			}

			void VisitBlock(BasicBlock basicBlock, ILocalSymbol? rentalHeld, ImmutableHashSet<ILocalSymbol> returnedRentals, ImmutableHashSet<BasicBlock> recursionGuard, ControlFlowBranch? pathToHere)
			{
				if (recursionGuard.Contains(basicBlock))
				{
					return;
				}

				recursionGuard = recursionGuard.Add(basicBlock);

				RentalOperationVisitor visitor = new(context, asyncIO, returnedRentals, rentalHeld, inputs);
				foreach (IOperation op in basicBlock.Operations)
				{
					op.Accept(visitor);
				}

				rentalHeld = visitor.CurrentRental;
				returnedRentals = visitor.ReturnedRentals;

				if (basicBlock.FallThroughSuccessor?.Destination is BasicBlock nextBlock)
				{
					VisitBlock(nextBlock, rentalHeld, returnedRentals, recursionGuard, basicBlock.FallThroughSuccessor);
				}

				if (basicBlock.ConditionalSuccessor?.Destination is BasicBlock conditionalBlock)
				{
					VisitBlock(conditionalBlock, rentalHeld, returnedRentals, recursionGuard, basicBlock.ConditionalSuccessor);
				}

				if (basicBlock.Kind == BasicBlockKind.Exit
					&& pathToHere?.Semantics is ControlFlowBranchSemantics.Return or ControlFlowBranchSemantics.Regular
					&& rentalHeld is not null)
				{
					Location? location =

						// Set the diagnostic on the return statement if there is one.
						pathToHere.Source.BranchValue?.Syntax.FirstAncestorOrSelf<ReturnStatementSyntax>()?.ReturnKeyword.GetLocation() ??

						// Set the diagnostic on the closing curly brace if we couldn't find a return statement.
						((MethodDeclarationSyntax)containingMethod.DeclaringSyntaxReferences[0].GetSyntax(context.CancellationToken)).Body?.CloseBraceToken.GetLocation() ??

						// Fallback to the method name.
						((MethodDeclarationSyntax?)containingMethod.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(context.CancellationToken))?.Identifier.GetLocation();

					context.ReportDiagnostic(Diagnostic.Create(inputs.ReturnRentalFirst, location));
				}
			}
		}
	}

	private struct AsyncRentalReturnAnalysisInputs
	{
		public required FrozenSet<string> RentalMethodNames { get; init; }

		public required FrozenSet<string> ReturnMethodNames { get; init; }

		public required Func<string, bool> ReturnBeforeMethodTester { get; init; }

		public required DiagnosticDescriptor ReturnRentalFirst { get; init; }

		public required DiagnosticDescriptor DoNotReuseRental { get; init; }
	}

	private class RentalOperationVisitor(OperationBlockAnalysisContext context, IParameterSymbol asyncIO, ImmutableHashSet<ILocalSymbol> returnedRentals, ILocalSymbol? currentRental, AsyncRentalReturnAnalysisInputs inputs) : OperationVisitor
	{
		internal ILocalSymbol? CurrentRental => currentRental;

		internal ImmutableHashSet<ILocalSymbol> ReturnedRentals => returnedRentals;

		public override void DefaultVisit(IOperation operation)
		{
			foreach (IOperation op in operation.ChildOperations)
			{
				op.Accept(this);
			}
		}

		public override void VisitInvocation(IInvocationOperation operation)
		{
			base.VisitInvocation(operation);

			// Is this an invocation of the async reader/writer?
			if (operation.Instance is IParameterReferenceOperation { Parameter: IParameterSymbol p } && SymbolEqualityComparer.Default.Equals(p, asyncIO))
			{
				if (inputs.RentalMethodNames.Contains(operation.TargetMethod.Name))
				{
					if (currentRental is not null)
					{
						// report diagnostic: return rental first.
						context.ReportDiagnostic(Diagnostic.Create(
							inputs.ReturnRentalFirst,
							operation.Syntax.GetLocation()));
					}

					// Find the assignment.
					if (operation.Parent is IAssignmentOperation { Target: ILocalReferenceOperation { Local: ILocalSymbol local } })
					{
						currentRental = local;
						returnedRentals = returnedRentals.Remove(local);
					}
				}
				else if (inputs.ReturnMethodNames.Contains(operation.TargetMethod.Name))
				{
					if (currentRental is not null)
					{
						returnedRentals = returnedRentals.Add(currentRental);
						currentRental = null;
					}
				}
				else if (currentRental is not null)
				{
					// Other invocations of the async reader/writer are not allowed when a rental is held.
					context.ReportDiagnostic(Diagnostic.Create(
						inputs.ReturnRentalFirst,
						operation.Syntax.GetLocation()));
				}
			}
		}

		public override void VisitLocalReference(ILocalReferenceOperation operation)
		{
			bool leftOfAssignment = operation.Parent is IAssignmentOperation assignment && assignment.Target == operation;
			if (!leftOfAssignment && returnedRentals.Contains(operation.Local))
			{
				context.ReportDiagnostic(Diagnostic.Create(
					inputs.DoNotReuseRental,
					operation.Syntax.GetLocation()));
			}

			base.VisitLocalReference(operation);
		}

		public override void VisitAwait(IAwaitOperation operation)
		{
			base.VisitAwait(operation);

			if (currentRental is not null)
			{
				// This is allowed, only if the awaited expression is an invocation on the rental,
				// and the result of the awaited expression is reassigned back to the rental.
				// We allow both with and without the .ConfigureAwait(bool) suffix.
				bool allowed = (IsMethodOnRental(currentRental, operation.Operation) || (operation.Operation is IInvocationOperation { Instance: IInvocationOperation configuredAwaiter } && IsMethodOnRental(currentRental, configuredAwaiter)))
					&& operation.Parent is IArgumentOperation { Parent: IObjectCreationOperation { Parent: IConversionOperation { Parent: IAssignmentOperation { Target: ILocalReferenceOperation { Local: ILocalSymbol assignedLocal } } } } }
					&& SymbolEqualityComparer.Default.Equals(assignedLocal, currentRental);
				if (!allowed)
				{
					context.ReportDiagnostic(Diagnostic.Create(
						inputs.ReturnRentalFirst,
						((AwaitExpressionSyntax)operation.Syntax).AwaitKeyword.GetLocation()));
				}
			}

			static bool IsMethodOnRental(ILocalSymbol? rental, IOperation operation)
			{
				return operation is IInvocationOperation { Instance: ILocalReferenceOperation { Local: ILocalSymbol receiver } }
					&& SymbolEqualityComparer.Default.Equals(receiver, rental);
			}
		}
	}
}
