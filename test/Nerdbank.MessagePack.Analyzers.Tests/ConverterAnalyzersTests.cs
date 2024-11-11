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
				public override MyType Read(ref MessagePackReader reader, SerializationContext context) => throw new System.NotImplementedException();
				public override void Write(ref MessagePackWriter writer, in MyType value, SerializationContext context) => throw new System.NotImplementedException();
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task NoIssues_MultipleStructuresWithArrayHeader()
	{
		string source = /* lang=c#-test */ """
			using PolyType;
			using Nerdbank.MessagePack;

			public class MyType { }
			
			public class MyTypeConverter : MessagePackConverter<MyType>
			{
				public override MyType Read(ref MessagePackReader reader, SerializationContext context)
				{
					if (reader.TryReadNil())
					{
						return null;
					}

					int count = reader.ReadArrayHeader();
					for (int i = 0; i < count; i++)
					{
						reader.Skip(context);
					}

					return new MyType();
				}

				public override void Write(ref MessagePackWriter writer, in MyType value, SerializationContext context)
				{
					if (value is null)
					{
						writer.WriteNil();
						return;
					}

					writer.WriteArrayHeader(3);
					writer.Write(1);
					writer.Write(2);
					writer.Write(3);
				}
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task NoIssues_MultipleStructuresWithMapHeader()
	{
		string source = /* lang=c#-test */ """
			using PolyType;
			using Nerdbank.MessagePack;

			public class MyType { }
			
			public class MyTypeConverter : MessagePackConverter<MyType>
			{
				public override MyType Read(ref MessagePackReader reader, SerializationContext context)
				{
					if (!reader.TryReadNil())
					{
						int count = reader.ReadMapHeader();
						for (int i = 0; i < count; i++)
						{
							reader.Skip(context);
							reader.Skip(context);
						}

						return new MyType();
					}
					else
					{
						return null;
					}
				}

				public override void Write(ref MessagePackWriter writer, in MyType value, SerializationContext context)
				{
					if (value is null)
					{
						writer.WriteNil();
						return;
					}

					writer.WriteMapHeader(3);
					writer.Write("p1");
					writer.Write(1);
					writer.Write("p2");
					writer.Write(2);
					writer.Write("p3");
					writer.Write(3);
				}
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task NoIssues_DeferToOtherConverter()
	{
		string source = /* lang=c#-test */ """
			using PolyType;
			using PolyType.Abstractions;
			using Nerdbank.MessagePack;

			public class MyType
			{
				public SomeOtherType SomeField;
			}

			public class SomeOtherType : IShapeable<SomeOtherType>
			{
				public static ITypeShape<SomeOtherType> GetShape() => throw new System.NotImplementedException();
			}

			public class MyTypeConverter : MessagePackConverter<MyType>
			{
				public override MyType Read(ref MessagePackReader reader, SerializationContext context)
				{
					if (reader.TryReadNil())
					{
						return null;
					}
					else
					{
						return new MyType
						{
							SomeField = context.GetConverter<SomeOtherType>().Read(ref reader, context),
						};
					}
				}

				public override void Write(ref MessagePackWriter writer, in MyType value, SerializationContext context)
				{
					if (value is null)
					{
						writer.WriteNil();
						return;
					}

					context.GetConverter<SomeOtherType>().Write(ref writer, value.SomeField, context);
				}
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
				public override MyType Read(ref MessagePackReader reader, SerializationContext context) => throw new System.NotImplementedException();
				public override void Write(ref MessagePackWriter writer, in MyType value, SerializationContext context)
				{
					var serializer = {|NBMsgPack030:new MessagePackSerializer()|};
					{|NBMsgPack030:serializer.Serialize(value)|};
					throw new System.NotImplementedException();
				}
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task MultipleStructures()
	{
		string source = /* lang=c#-test */ """
			using PolyType;
			using Nerdbank.MessagePack;

			public class MyType { }
			
			public class MyTypeConverter : MessagePackConverter<MyType>
			{
				public override MyType Read(ref MessagePackReader reader, SerializationContext context)
				{
					reader.ReadInt32();
					{|NBMsgPack031:reader.ReadInt16()|};
					return new MyType();
				}

				public override void Write(ref MessagePackWriter writer, in MyType value, SerializationContext context)
				{
					writer.Write(1);
					{|NBMsgPack031:writer.Write(2)|};
				}
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task ZeroStructures()
	{
		string source = /* lang=c#-test */ """
			using PolyType;
			using Nerdbank.MessagePack;

			public class MyType { }
			
			public class MyTypeConverter : MessagePackConverter<MyType>
			{
				public override MyType {|NBMsgPack031:Read|}(ref MessagePackReader reader, SerializationContext context)
				{
					return new MyType();
				}

				public override void {|NBMsgPack031:Write|}(ref MessagePackWriter writer, in MyType value, SerializationContext context)
				{
				}

				// Not an error to not write things here.
				void Helper()
				{
					if (true)
					{
					}
				}
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}
}
