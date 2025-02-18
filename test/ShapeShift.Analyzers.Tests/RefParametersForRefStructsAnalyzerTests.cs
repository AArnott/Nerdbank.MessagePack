// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = CodeFixVerifier<ShapeShift.Analyzers.RefParametersForRefStructsAnalyzer, Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

public class RefParametersForRefStructsAnalyzerTests
{
	[Fact]
	public async Task MethodWithRefParameters()
	{
		string testSource = /* lang=c#-test */ """
			#pragma warning disable NBMsgPackAsync

			using ShapeShift.Converters;

			class Test
			{
				public void Method(ref Reader reader)
				{
				}

				public void Method(ref StreamingReader reader)
				{
				}

				public void Method(ref Writer writer)
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
			#pragma warning disable NBMsgPackAsync

			using ShapeShift.Converters;

			class Test
			{
				public void Method({|NBMsgPack050:Reader|} reader)
				{
				}

				public void Method({|NBMsgPack050:StreamingReader|} reader)
				{
				}

				public void Method({|NBMsgPack050:Writer|} writer)
				{
				}
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(testSource);
	}
}
