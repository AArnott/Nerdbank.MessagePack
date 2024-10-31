// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = CodeFixVerifier<Nerdbank.MessagePack.Analyzers.NBMsgPack001ConsistentUseOfKeyAnalyzer, Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

public class NBMsgPack001ConsistentUseOfKeyAnalyzerTests
{
	[Fact]
	public async Task NoIssues()
	{
		string source = /* lang=c#-test */ """
			using TypeShape;
			using Nerdbank.MessagePack;

			[GenerateShape]
			public class MyType
			{
				[Key(0)]
				public int MyProperty1 { get; set; }

				[Key(1)]
				public int MyProperty2 { get; set; }
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task MissingKey()
	{
		string source = /* lang=c#-test */ """
			using TypeShape;
			using Nerdbank.MessagePack;

			[GenerateShape]
			public class MyType
			{
				[Key(0)]
				public int MyProperty1 { get; set; }

				public int [|MyProperty2|] { get; set; }
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}
}
