// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = CodeFixVerifier<Nerdbank.MessagePack.Analyzers.KnownSubTypeAnalyzers, Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

public class KnownSubTypeAnalyzersTests
{
	[Fact]
	public async Task NoIssues_Interface()
	{
		string source = /* lang=c#-test */ """
			using Nerdbank.MessagePack;

			[KnownSubType(1, typeof(DerivedType))]
			public interface IMyType
			{
			}

			public class DerivedType : IMyType
			{
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task NoIssues_Subclass()
	{
		string source = /* lang=c#-test */ """
			using Nerdbank.MessagePack;

			[KnownSubType(1, typeof(DerivedType))]
			public class MyType
			{
			}

			public class DerivedType : MyType
			{
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task NonDerivedType()
	{
		string source = /* lang=c#-test */ """
			using Nerdbank.MessagePack;

			[KnownSubType(1, {|NBMsgPack010:typeof(NonDerivedType)|})]
			public class MyType
			{
			}

			public class NonDerivedType
			{
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}
}
