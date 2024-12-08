// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = CodeFixVerifier<Nerdbank.MessagePack.Analyzers.KnownSubTypeAnalyzers, Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

public class KnownSubTypeAnalyzersTests
{
	[Fact]
	public async Task NoIssues_Interface()
	{
#if NET
		string source = /* lang=c#-test */ """
			using Nerdbank.MessagePack;

			[KnownSubType<DerivedType>(1)]
			public interface IMyType
			{
			}

			public class DerivedType : IMyType, PolyType.IShapeable<DerivedType>
			{
				static PolyType.Abstractions.ITypeShape<DerivedType> PolyType.IShapeable<DerivedType>.GetShape() => throw new System.NotImplementedException();
			}
			""";
#else
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
#endif
		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task NoIssues_Subclass()
	{
#if NET
		string source = /* lang=c#-test */ """
			using Nerdbank.MessagePack;

			[KnownSubType<DerivedType>(1)]
			public class MyType
			{
			}

			public class DerivedType : MyType, PolyType.IShapeable<DerivedType>
			{
				static PolyType.Abstractions.ITypeShape<DerivedType> PolyType.IShapeable<DerivedType>.GetShape() => throw new System.NotImplementedException();
			}
			""";
#else
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
#endif

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task NonDerivedType()
	{
#if NET
		string source = /* lang=c#-test */ """
			using Nerdbank.MessagePack;

			[KnownSubType<{|NBMsgPack010:NonDerivedType|}>(1)]
			public class MyType
			{
			}

			public class NonDerivedType : PolyType.IShapeable<NonDerivedType>
			{
				static PolyType.Abstractions.ITypeShape<NonDerivedType> PolyType.IShapeable<NonDerivedType>.GetShape() => throw new System.NotImplementedException();
			}
			""";
#else
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
#endif

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task NoIssues_NonUniqueAlias_AcrossTypes()
	{
#if NET
		string source = /* lang=c#-test */ """
			using Nerdbank.MessagePack;

			[KnownSubType<DerivedType1>(1)]
			public class MyType
			{
			}

			public class DerivedType1 : MyType, PolyType.IShapeable<DerivedType1>
			{
				static PolyType.Abstractions.ITypeShape<DerivedType1> PolyType.IShapeable<DerivedType1>.GetShape() => throw new System.NotImplementedException();
			}

			[KnownSubType<DerivedType2>(1)]
			public class MyType2
			{
			}

			public class DerivedType2 : MyType2, PolyType.IShapeable<DerivedType2>
			{
				static PolyType.Abstractions.ITypeShape<DerivedType2> PolyType.IShapeable<DerivedType2>.GetShape() => throw new System.NotImplementedException();
			}
			""";
#else
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
#endif

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task NonUniqueAlias()
	{
#if NET
		string source = /* lang=c#-test */ """
			using Nerdbank.MessagePack;

			[KnownSubType<DerivedType1>(1)]
			[KnownSubType<DerivedType2>({|NBMsgPack011:1|})]
			public class MyType
			{
			}

			public class DerivedType1 : MyType, PolyType.IShapeable<DerivedType1>
			{
				static PolyType.Abstractions.ITypeShape<DerivedType1> PolyType.IShapeable<DerivedType1>.GetShape() => throw new System.NotImplementedException();
			}

			public class DerivedType2 : MyType, PolyType.IShapeable<DerivedType2>
			{
				static PolyType.Abstractions.ITypeShape<DerivedType2> PolyType.IShapeable<DerivedType2>.GetShape() => throw new System.NotImplementedException();
			}
			""";
#else
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
#endif

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task NonUniqueSubType()
	{
#if NET
		string source = /* lang=c#-test */ """
			using Nerdbank.MessagePack;

			[KnownSubType<DerivedType1>(1)]
			[KnownSubType<{|NBMsgPack012:DerivedType1|}>(2)]
			public class MyType
			{
			}

			public class DerivedType1 : MyType, PolyType.IShapeable<DerivedType1>
			{
				static PolyType.Abstractions.ITypeShape<DerivedType1> PolyType.IShapeable<DerivedType1>.GetShape() => throw new System.NotImplementedException();
			}
			""";
#else
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
#endif

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task NoIssues_ClosedGenericSubType()
	{
#if NET
		string source = /* lang=c#-test */ """
			using Nerdbank.MessagePack;

			[KnownSubType<DerivedType<int>, Witness>(1)]
			[KnownSubType<DerivedType<bool>, Witness>(2)]
			public class MyType
			{
			}

			public class DerivedType<T> : MyType
			{
			}

			internal class Witness : PolyType.IShapeable<DerivedType<int>>, PolyType.IShapeable<DerivedType<bool>>
			{
				static PolyType.Abstractions.ITypeShape<DerivedType<int>> PolyType.IShapeable<DerivedType<int>>.GetShape() => throw new System.NotImplementedException();
				static PolyType.Abstractions.ITypeShape<DerivedType<bool>> PolyType.IShapeable<DerivedType<bool>>.GetShape() => throw new System.NotImplementedException();
			}
			""";
#else
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

			internal class Witness
			{
			}
			""";
#endif

		await VerifyCS.VerifyAnalyzerAsync(source);
	}
}
