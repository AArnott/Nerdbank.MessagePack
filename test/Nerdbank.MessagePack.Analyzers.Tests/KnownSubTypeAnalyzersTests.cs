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

	[Fact]
	public async Task NonUniqueAlias_AcrossTypes()
	{
		string source = /* lang=c#-test */ """
			using Nerdbank.MessagePack;

			[KnownSubType(1, typeof(DerivedType1))]
			public class MyType
			{
			}

			public class DerivedType1 : MyType
			{
			}

			[KnownSubType(1, typeof(DerivedType2))]
			public class MyType2
			{
			}

			public class DerivedType2 : MyType2
			{
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task NonUniqueAlias()
	{
		string source = /* lang=c#-test */ """
			using Nerdbank.MessagePack;

			[KnownSubType(1, typeof(DerivedType1))]
			[KnownSubType({|NBMsgPack011:1|}, typeof(DerivedType2))]
			public class MyType
			{
			}

			public class DerivedType1 : MyType
			{
			}

			public class DerivedType2 : MyType
			{
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task NonUniqueSubType()
	{
		string source = /* lang=c#-test */ """
			using Nerdbank.MessagePack;

			[KnownSubType(1, typeof(DerivedType1))]
			[KnownSubType(2, {|NBMsgPack012:typeof(DerivedType1)|})]
			public class MyType
			{
			}

			public class DerivedType1 : MyType
			{
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task OpenGenericSubType()
	{
		string source = /* lang=c#-test */ """
			using Nerdbank.MessagePack;

			[KnownSubType(1, {|NBMsgPack013:typeof(DerivedType<>)|})]
			public class MyType
			{
			}

			public class DerivedType<T> : MyType
			{
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task ClosedGenericSubType()
	{
		string source = /* lang=c#-test */ """
			using Nerdbank.MessagePack;

			[KnownSubType(1, typeof(DerivedType<int>))]
			[KnownSubType(2, typeof(DerivedType<bool>))]
			public class MyType
			{
			}

			public class DerivedType<T> : MyType
			{
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}
}
