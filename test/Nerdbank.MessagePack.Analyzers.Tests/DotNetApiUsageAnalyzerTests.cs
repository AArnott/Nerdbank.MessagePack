// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = CodeFixVerifier<Nerdbank.MessagePack.Analyzers.DotNetApiUsageAnalyzer, Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

public class DotNetApiUsageAnalyzerTests
{
	[Fact]
	public async Task KnownSubTypeAttribute()
	{
#if NET
		string source = /* lang=c#-test */ """
			using PolyType;
			using PolyType.Abstractions;
			using Nerdbank.MessagePack;

			[{|NBMsgPack051:KnownSubTypeAttribute(typeof(MyDerived))|}]
			class MyType { }

			partial class MyDerived : MyType, IShapeable<MyDerived>
			{
				public static ITypeShape<MyDerived> GetShape() => throw new System.NotImplementedException();
			}
			""";
#else
		string source = /* lang=c#-test */ """
			using Nerdbank.MessagePack;

			[KnownSubTypeAttribute(typeof(MyDerived))]
			class MyType { }

			class MyDerived : MyType { }
			""";
#endif
		await VerifyCS.VerifyAnalyzerAsync(source);
	}

#if NET
	[Fact]
	public async Task KnownSubTypeGenericAttribute()
	{
		string source = /* lang=c#-test */ """
			using PolyType;
			using PolyType.Abstractions;
			using Nerdbank.MessagePack;

			[KnownSubTypeAttribute<MyDerived>]
			class MyType { }

			partial class MyDerived : MyType, IShapeable<MyDerived>
			{
				public static ITypeShape<MyDerived> GetShape() => throw new System.NotImplementedException();
			}
			""";
		await VerifyCS.VerifyAnalyzerAsync(source);
	}
#endif
}
