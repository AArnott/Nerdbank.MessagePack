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
	public async Task NoIssues_StructureIsReadIntoReturnValueViaConstructor()
	{
		string source = /* lang=c#-test */ """
			using System;
			using PolyType;
			using PolyType.Abstractions;
			using Nerdbank.MessagePack;

			internal class TimeSpanConverter : MessagePackConverter<TimeSpan>
			{
				/// <inheritdoc/>
				public override TimeSpan Read(ref MessagePackReader reader, SerializationContext context) => new TimeSpan(reader.ReadInt64());

				/// <inheritdoc/>
				public override void Write(ref MessagePackWriter writer, in TimeSpan value, SerializationContext context) => writer.Write(value.Ticks);
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task NoIssues_StructureIsReadDirectlyIntoReturnValue()
	{
		string source = /* lang=c#-test */ """
			using System;
			using PolyType;
			using PolyType.Abstractions;
			using Nerdbank.MessagePack;

			internal class Int16Converter : MessagePackConverter<Int16>
			{
				/// <inheritdoc/>
				public override Int16 Read(ref MessagePackReader reader, SerializationContext context) => reader.ReadInt16();

				/// <inheritdoc/>
				public override void Write(ref MessagePackWriter writer, in Int16 value, SerializationContext context) => writer.Write(value);
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task NoIssues_StructureIsReadWithinConditionalExpression()
	{
		string source = /* lang=c#-test */ """
			using System;
			using PolyType;
			using PolyType.Abstractions;
			using Nerdbank.MessagePack;

			internal class VersionConverter : MessagePackConverter<Version>
			{
				/// <inheritdoc/>
				public override Version Read(ref MessagePackReader reader, SerializationContext context) => reader.ReadString() is string value ? new Version(value) : null;

				/// <inheritdoc/>
				public override void Write(ref MessagePackWriter writer, in Version value, SerializationContext context) => writer.Write(value?.ToString());
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task NoIssues_WriterUsesGetSpanAdvance()
	{
		string source = /* lang=c#-test */ """
			using System;
			using PolyType;
			using PolyType.Abstractions;
			using Nerdbank.MessagePack;

			internal class VersionConverter : MessagePackConverter<Version>
			{
				/// <inheritdoc/>
				public override Version Read(ref MessagePackReader reader, SerializationContext context) => reader.ReadString() is string value ? new Version(value) : null;

				/// <inheritdoc/>
				public override void Write(ref MessagePackWriter writer, in Version value, SerializationContext context)
				{
					Span<byte> span = writer.GetSpan(5);
					// Assume something is written to the span.
					writer.Advance(3);
				}
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task NoIssues_SkipRead()
	{
		string source = /* lang=c#-test */ """
			using System;
			using System.Diagnostics.CodeAnalysis;
			using PolyType;
			using PolyType.Abstractions;
			using Nerdbank.MessagePack;
			
			internal class ArrayWithFlattenedDimensionsConverter<TArray, TElement> : MessagePackConverter<TArray>
			{
				public override TArray Read(ref MessagePackReader reader, SerializationContext context)
				{
					reader.Skip(context);
					return default;
				}

				public override void Write(ref MessagePackWriter writer, in TArray value, SerializationContext context) => throw new NotImplementedException();
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task NoIssues_ReadHasAttribute()
	{
		string source = /* lang=c#-test */ """
			using System;
			using System.Diagnostics.CodeAnalysis;
			using PolyType;
			using PolyType.Abstractions;
			using Nerdbank.MessagePack;
			
			internal class ArrayWithFlattenedDimensionsConverter : MessagePackConverter<int>
			{
				[UnconditionalSuppressMessage("AOT", "IL3050")]
				public override int Read(ref MessagePackReader reader, SerializationContext context)
				{
					return reader.ReadInt32();
				}

				public override void Write(ref MessagePackWriter writer, in int value, SerializationContext context) => throw new NotImplementedException();
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

	[Fact]
	public async Task ConvertReadsStringInBinaryExpression()
	{
		string source = /* lang=c#-test */ """
			using PolyType;
			using Nerdbank.MessagePack;
			
			class CustomStringConverter : MessagePackConverter<string>
			{
				public override string Read(ref MessagePackReader reader, SerializationContext context)
					=> reader.ReadString() + "R";

				public override void Write(ref MessagePackWriter writer, in string value, SerializationContext context)
					=> writer.Write(value + "W");
			}
			""";
		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task ConvertReadsStringOnBothSidesOfBinaryExpression()
	{
		string source = /* lang=c#-test */ """
			using PolyType;
			using Nerdbank.MessagePack;
			
			class CustomStringConverter : MessagePackConverter<string>
			{
				public override string Read(ref MessagePackReader reader, SerializationContext context)
					=> reader.ReadString() + {|NBMsgPack031:reader.ReadString()|};

				public override void Write(ref MessagePackWriter writer, in string value, SerializationContext context)
					=> writer.Write(value);
			}
			""";
		await VerifyCS.VerifyAnalyzerAsync(source);
	}
}
