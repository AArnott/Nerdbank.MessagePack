// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = CodeFixVerifier<Nerdbank.MessagePack.Analyzers.ConverterAnalyzers, Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

public class ConverterAnalyzersTests
{
	[Fact]
	public async Task NoIssues()
	{
		string source = /* lang=c#-test */ """
			using PolyType;
			using Nerdbank.MessagePack;

			public class MyType { }
			
			public class MyTypeConverter : MessagePackConverter<MyType>
			{
				public override MyType Deserialize(ref MessagePackReader reader, SerializationContext context) => throw new System.NotImplementedException();
				public override void Serialize(ref MessagePackWriter writer, ref MyType value, SerializationContext context) => throw new System.NotImplementedException();
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task CreatesNewSerializer()
	{
		string source = /* lang=c#-test */ """
			using PolyType;
			using PolyType.Abstractions;
			using Nerdbank.MessagePack;

			public partial class MyType : IShapeable<MyType>
			{
				public static ITypeShape<MyType> GetShape() => throw new System.NotImplementedException();
			}
			
			public class MyTypeConverter : MessagePackConverter<MyType>
			{
				public override MyType Deserialize(ref MessagePackReader reader, SerializationContext context) => throw new System.NotImplementedException();
				public override void Serialize(ref MessagePackWriter writer, ref MyType value, SerializationContext context)
				{
					var serializer = {|NBMsgPack030:new MessagePackSerializer()|};
					{|NBMsgPack030:serializer.Serialize(value)|};
				}
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}
}
