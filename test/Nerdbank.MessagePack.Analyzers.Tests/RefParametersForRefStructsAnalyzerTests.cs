// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = CodeFixVerifier<Nerdbank.MessagePack.Analyzers.RefParametersForRefStructsAnalyzer, Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

public class RefParametersForRefStructsAnalyzerTests
{
	[Fact]
	public async Task MethodWithRefParameters()
	{
		string testSource = /* lang=c#-test */ """
			using Nerdbank.MessagePack;

			class Test
			{
				public void Method(ref MessagePackReader reader)
				{
				}

				public void Method(ref MessagePackStreamingReader reader)
				{
				}

				public void Method(ref MessagePackWriter writer)
				{
				}
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(testSource);
	}

	[Fact]
	public async Task MethodWithParameters_MissingRef()
	{
		string testSource = /* lang=c#-test */ """
			using Nerdbank.MessagePack;

			class Test
			{
				public void Method({|NBMsgPack050:MessagePackReader|} reader)
				{
				}

				public void Method({|NBMsgPack050:MessagePackStreamingReader|} reader)
				{
				}

				public void Method({|NBMsgPack050:MessagePackWriter|} writer)
				{
				}
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(testSource);
	}
}
