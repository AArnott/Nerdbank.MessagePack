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

			public class Members
			{
				[MessagePackConverter(typeof(MyTypeConverter))]
				public MyType Value { get; set; }
			}

			public class MyTypeConverter : MessagePackConverter<MyType>
			{
				public override MyType Read(ref MessagePackReader reader, SerializationContext context) => throw new System.NotImplementedException();
				public override void Write(ref MessagePackWriter writer, in MyType value, SerializationContext context) => throw new System.NotImplementedException();
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task NoIssues_OpenGeneric()
	{
		string source = /* lang=c#-test */ """
			using PolyType;
			using Nerdbank.MessagePack;

			[MessagePackConverter(typeof(MyTypeConverter<>))]
			public class MyType<T>
			{
			}

			public class MemberOf<T>
			{
				[MessagePackConverter(typeof(MyTypeConverter<>))]
				public MyType<T> Value { get; set; }
			}

			public class MemberOf
			{
				[MessagePackConverter(typeof(MyTypeConverter<>))]
				public MyType<int> Value { get; set; }
			}

			public class MyTypeConverter<T> : MessagePackConverter<MyType<T>>
			{
				public override MyType<T> Read(ref MessagePackReader reader, SerializationContext context) => throw new System.NotImplementedException();
				public override void Write(ref MessagePackWriter writer, in MyType<T> value, SerializationContext context) => throw new System.NotImplementedException();
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

			public class MemberApplied
			{
				[MessagePackConverter({|NBMsgPack021:typeof(MyTypeConverter)|})]
				public MyType Value { get; set; }
			}

			public class MyTypeConverter : MessagePackConverter<MyType>
			{
				private MyTypeConverter() { }
				public override MyType Read(ref MessagePackReader reader, SerializationContext context) => throw new System.NotImplementedException();
				public override void Write(ref MessagePackWriter writer, in MyType value, SerializationContext context) => throw new System.NotImplementedException();
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

			public class MemberApplied
			{
				[MessagePackConverter({|NBMsgPack020:typeof(MyTypeConverter)|})]
				public MyType Value { get; set; }
			}

			public class MyTypeConverter
			{
				public MyType Read(ref MessagePackReader reader, SerializationContext context) => throw new System.NotImplementedException();
				public void Write(ref MessagePackWriter writer, ref MyType value, SerializationContext context) => throw new System.NotImplementedException();
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

			public class MemberApplied
			{
				[MessagePackConverter({|NBMsgPack020:typeof(IntConverter)|})]
				public string Value { get; set; }
			}

			public class IntConverter : MessagePackConverter<int>
			{
				public override int Read(ref MessagePackReader reader, SerializationContext context) => throw new System.NotImplementedException();
				public override void Write(ref MessagePackWriter writer, in int value, SerializationContext context) => throw new System.NotImplementedException();
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}
}
