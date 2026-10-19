// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Nerdbank.MessagePack.Analyzers;

[MemoryDiagnoser]
public class DefaultValueInitializerAnalysis
{
	private readonly ImmutableArray<DiagnosticAnalyzer> analyzers = [new DefaultValueInitializerAnalyzer()];
	private readonly CSharpCompilationOptions options = new(OutputKind.DynamicallyLinkedLibrary);
	private ImmutableArray<SyntaxTree> trees;
	private ImmutableArray<MetadataReference> references;

	[Params("Shaped", "Sparse", "Unshaped")]
	public string Scenario { get; set; } = "Shaped";

	[Params(100)]
	public int TypeCount { get; set; }

	[GlobalSetup]
	public async Task Setup()
	{
		this.references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
			.Append(typeof(GenerateShapeAttribute).Assembly.Location)
			.Append(typeof(MessagePackSerializer).Assembly.Location)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.Select(path => MetadataReference.CreateFromFile(path))
			.ToImmutableArray<MetadataReference>();
		this.trees = Enumerable.Range(0, this.TypeCount).Select(index =>
		{
			bool shaped = this.Scenario switch
			{
				"Shaped" => true,
				"Sparse" => index % 10 == 0,
				"Unshaped" => false,
				_ => throw new InvalidOperationException("Unknown benchmark scenario."),
			};
			string source = $$"""
				{{(shaped ? "[PolyType.GenerateShape]" : string.Empty)}}
				public partial class Model{{index}}(int number = 42)
				{
				    public int Number { get; init; } = number;
				    public int Offset { get; init; } = number + 1;
				    public int Value { get; set; } = 42;
				    public bool Flag { get; set; } = false;
				    public string Name { get; set; } = "test";
				    public int Zero = 0;
				    public long Widened = number;
				}
				""";
			return CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp12));
		}).ToImmutableArray<SyntaxTree>();

		Diagnostic[] errors = this.CreateCompilation().GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
		if (errors.Length != 0)
		{
			throw new InvalidOperationException(string.Join(Environment.NewLine, errors.Select(d => d.ToString())));
		}

		ImmutableArray<Diagnostic> diagnostics = await this.Analyze();
		int shapedTypes = this.Scenario switch
		{
			"Shaped" => this.TypeCount,
			"Sparse" => (this.TypeCount + 9) / 10,
			_ => 0,
		};
		if (diagnostics.Length != shapedTypes * 4 || diagnostics.Any(d => d.Id != DefaultValueInitializerAnalyzer.MissingDefaultValueAttributeDiagnosticId))
		{
			throw new InvalidOperationException("Unexpected analyzer diagnostics in benchmark workload.");
		}
	}

	[Benchmark]
	public Task<ImmutableArray<Diagnostic>> Analyze()
		=> this.CreateCompilation().WithAnalyzers(this.analyzers).GetAnalyzerDiagnosticsAsync();

	private CSharpCompilation CreateCompilation()
		=> CSharpCompilation.Create("AnalyzerWorkload", this.trees, this.references, this.options);
}

#endif
