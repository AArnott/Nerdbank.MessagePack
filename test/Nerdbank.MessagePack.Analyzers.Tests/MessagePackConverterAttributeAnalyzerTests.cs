// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = CodeFixVerifier<Nerdbank.MessagePack.Analyzers.MessagePackConverterAttributeAnalyzer, Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

public class MessagePackConverterAttributeAnalyzerTests
{
	[Fact]
	public async Task NoIssues()
	{
		string source = /* lang=c#-test */ """
			using PolyType;
			using Nerdbank.MessagePack;

			[MessagePackConverter(typeof(MyTypeConverter))]
			public class MyType
			{
			}

			public class MyTypeConverter : MessagePackConverter<MyType>
			{
				public override MyType Deserialize(ref MessagePackReader reader, SerializationContext context) => throw new System.NotImplementedException();
				public override void Serialize(ref MessagePackWriter writer, ref MyType value, SerializationContext context) => throw new System.NotImplementedException();
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task MissingPublicDefaultCtor()
	{
		string source = /* lang=c#-test */ """
			using PolyType;
			using Nerdbank.MessagePack;

			[MessagePackConverter({|NBMsgPack021:typeof(MyTypeConverter)|})]
			public class MyType
			{
			}

			public class MyTypeConverter : MessagePackConverter<MyType>
			{
				private MyTypeConverter() { }
				public override MyType Deserialize(ref MessagePackReader reader, SerializationContext context) => throw new System.NotImplementedException();
				public override void Serialize(ref MessagePackWriter writer, ref MyType value, SerializationContext context) => throw new System.NotImplementedException();
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task TypeDoesNotDeriveFromConverter()
	{
		string source = /* lang=c#-test */ """
			using PolyType;
			using Nerdbank.MessagePack;

			[MessagePackConverter({|NBMsgPack020:typeof(MyTypeConverter)|})]
			public class MyType
			{
			}

			public class MyTypeConverter
			{
				public MyType Deserialize(ref MessagePackReader reader, SerializationContext context) => throw new System.NotImplementedException();
				public void Serialize(ref MessagePackWriter writer, ref MyType value, SerializationContext context) => throw new System.NotImplementedException();
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task TypeDoesNotDeriveFromConverterOfMatchingType()
	{
		string source = /* lang=c#-test */ """
			using PolyType;
			using Nerdbank.MessagePack;

			[MessagePackConverter({|NBMsgPack020:typeof(IntConverter)|})]
			public class MyType
			{
			}

			public class IntConverter : MessagePackConverter<int>
			{
				public override int Deserialize(ref MessagePackReader reader, SerializationContext context) => throw new System.NotImplementedException();
				public override void Serialize(ref MessagePackWriter writer, ref int value, SerializationContext context) => throw new System.NotImplementedException();
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}
}
