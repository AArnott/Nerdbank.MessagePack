// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = CodeFixVerifier<Nerdbank.MessagePack.Analyzers.DotNetApiUsageAnalyzer, Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

public class DotNetApiUsageAnalyzerTests
{
	[Fact]
	public async Task SerializeOverload_Unconstrained()
	{
#if NET
		string source = /* lang=c#-test */ """
			using System.Buffers;
			using PolyType;
			using PolyType.Abstractions;
			using Nerdbank.MessagePack;

			class MyType { }

			[GenerateShapeFor<MyType>]
			partial class Witness;

			class Foo
			{
				private readonly MessagePackSerializer serializer = new();

				internal void Serialize(IBufferWriter<byte> writer, MyType value)
				{
					{|NBMsgPack051:this.serializer.Serialize(writer, value, Witness.ShapeProvider)|};
				}
			}
			""";
#else
		string source = /* lang=c#-test */ """
			using System.Buffers;
			using PolyType;
			using PolyType.Abstractions;
			using Nerdbank.MessagePack;

			[GenerateShape]
			partial class MyType { }

			[GenerateShapeFor<MyType>]
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
	public async Task SerializeOverload_Constrained()
	{
		string source = /* lang=c#-test */ """
			using System.Buffers;
			using PolyType;
			using PolyType.Abstractions;
			using Nerdbank.MessagePack;

			[GenerateShape]
			partial class MyType { }
			
			class Foo
			{
				private readonly MessagePackSerializer serializer = new();
			
				internal void Serialize(IBufferWriter<byte> writer, MyType value)
				{
					this.serializer.Serialize(writer, value);
				}
			}
			""";
		await VerifyCS.VerifyAnalyzerAsync(source);
	}
#endif
}
