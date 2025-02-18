// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = CodeFixVerifier<ShapeShift.Analyzers.MessagePackConverterAttributeAnalyzer, Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

public class MessagePackConverterAttributeAnalyzerTests
{
	[Fact]
	public async Task NoIssues()
	{
		string source = /* lang=c#-test */ """
			using PolyType;
			using ShapeShift;
			using ShapeShift.Converters;

			[Converter(typeof(MyTypeConverter))]
			public class MyType
			{
			}

			public class MyTypeConverter : Converter<MyType>
			{
				public override MyType Read(ref Reader reader, SerializationContext context) => throw new System.NotImplementedException();
				public override void Write(ref Writer writer, in MyType value, SerializationContext context) => throw new System.NotImplementedException();
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task MissingPublicDefaultCtor()
	{
		string source = /* lang=c#-test */ """
			using PolyType;
			using ShapeShift;
			using ShapeShift.Converters;
			
			[Converter({|NBMsgPack021:typeof(MyTypeConverter)|})]
			public class MyType
			{
			}

			public class MyTypeConverter : Converter<MyType>
			{
				private MyTypeConverter() { }
				public override MyType Read(ref Reader reader, SerializationContext context) => throw new System.NotImplementedException();
				public override void Write(ref Writer writer, in MyType value, SerializationContext context) => throw new System.NotImplementedException();
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task TypeDoesNotDeriveFromConverter()
	{
		string source = /* lang=c#-test */ """
			using PolyType;
			using ShapeShift;
			using ShapeShift.Converters;
			
			[Converter({|NBMsgPack020:typeof(MyTypeConverter)|})]
			public class MyType
			{
			}

			public class MyTypeConverter
			{
				public MyType Read(ref Reader reader, SerializationContext context) => throw new System.NotImplementedException();
				public void Write(ref Writer writer, ref MyType value, SerializationContext context) => throw new System.NotImplementedException();
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task TypeDoesNotDeriveFromConverterOfMatchingType()
	{
		string source = /* lang=c#-test */ """
			using PolyType;
			using ShapeShift;
			using ShapeShift.Converters;
			
			[Converter({|NBMsgPack020:typeof(IntConverter)|})]
			public class MyType
			{
			}

			public class IntConverter : Converter<int>
			{
				public override int Read(ref Reader reader, SerializationContext context) => throw new System.NotImplementedException();
				public override void Write(ref Writer writer, in int value, SerializationContext context) => throw new System.NotImplementedException();
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}
}
