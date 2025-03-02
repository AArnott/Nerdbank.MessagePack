// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = CodeFixVerifier<Nerdbank.MessagePack.Analyzers.DotNetApiUsageAnalyzer, Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

public class DotNetApiUsageAnalyzerTests
{
	[Fact]
	public async Task SerializeOverload()
	{
#if NET
		string source = /* lang=c#-test */ """
			using PolyType;
			using PolyType.Abstractions;
			using Nerdbank.MessagePack;

			class MyType { }

			[GenerateShape<MyType>]
			partial class Witness;

			class Foo
			{
				private readonly MessagePackSerializer serializer = new();

				internal void Serialize(IBufferWriter<byte> writer, MyType value)
				{
					this.serializer.Serialize(writer, value, Witness.ShapeProvider); // NBMsgPack051: Use an overload that takes a constrained type instead.
				}
			}
			""";
#else
		string source = /* lang=c#-test */ """
			using Nerdbank.MessagePack;

			[GenerateShape]
			partial class MyType { }

			[GenerateShape<MyType>]
			partial class Witness;

			class Foo
			{
				private readonly MessagePackSerializer serializer = new();

				internal void Serialize(IBufferWriter<byte> writer, MyType value)
				{
					this.serializer.Serialize(writer, value, Witness.ShapeProvider);
				}
			}
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
