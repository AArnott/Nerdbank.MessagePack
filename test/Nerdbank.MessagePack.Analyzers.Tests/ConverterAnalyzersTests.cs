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
				public override void Read(ref MessagePackReader reader, ref MyType? value, SerializationContext context) => throw new System.NotImplementedException();
				public override void Write(ref MessagePackWriter writer, in MyType value, SerializationContext context) => throw new System.NotImplementedException();
				public override System.Text.Json.Nodes.JsonObject GetJsonSchema(JsonSchemaContext context, PolyType.Abstractions.ITypeShape typeShape) => throw new System.NotImplementedException();
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
				public override void Read(ref MessagePackReader reader, ref MyType? value, SerializationContext context)
				{
					if (reader.TryReadNil())
					{
						value = null;
					}
					else
					{
						int count = reader.ReadArrayHeader();
						for (int i = 0; i < count; i++)
						{
							reader.Skip(context);
						}

						value = new MyType();
					}
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

				public override System.Text.Json.Nodes.JsonObject GetJsonSchema(JsonSchemaContext context, PolyType.Abstractions.ITypeShape typeShape) => throw new System.NotImplementedException();
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
				public override void Read(ref MessagePackReader reader, ref MyType? value, SerializationContext context)
				{
					if (!reader.TryReadNil())
					{
						int count = reader.ReadMapHeader();
						for (int i = 0; i < count; i++)
						{
							reader.Skip(context);
							reader.Skip(context);
						}

						value = new MyType();
					}
					else
					{
						value = null;
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

				public override System.Text.Json.Nodes.JsonObject GetJsonSchema(JsonSchemaContext context, PolyType.Abstractions.ITypeShape typeShape) => throw new System.NotImplementedException();
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

			#if NET
			[GenerateShape]
			public partial class SomeOtherType
			{
			}
			#else
			public class SomeOtherType
			{
			}
			#endif

			public class MyTypeConverter : MessagePackConverter<MyType>
			{
				public override void Read(ref MessagePackReader reader, ref MyType? value, SerializationContext context)
				{
					if (reader.TryReadNil())
					{
						value = null;
					}
					else
					{
						value = new MyType
						{
			#if NET
							SomeField = context.GetConverter<SomeOtherType>().Read(ref reader, context),
			#else
							SomeField = context.GetConverter<SomeOtherType>(context.TypeShapeProvider).Read(ref reader, context),
			#endif
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

			#if NET
					context.GetConverter<SomeOtherType>().Write(ref writer, value.SomeField, context);
			#else
					context.GetConverter<SomeOtherType>(context.TypeShapeProvider).Write(ref writer, value.SomeField, context);
			#endif
				}

				public override System.Text.Json.Nodes.JsonObject GetJsonSchema(JsonSchemaContext context, PolyType.Abstractions.ITypeShape typeShape) => throw new System.NotImplementedException();
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task NoIssues_DeferToOtherConverter_NonGeneric()
	{
		string source = /* lang=c#-test */ """
			using PolyType;
			using PolyType.Abstractions;
			using Nerdbank.MessagePack;

			public class MyType
			{
				public SomeOtherType SomeField;
			}

			#if NET
			[GenerateShape]
			public partial class SomeOtherType
			{
			}
			#else
			public class SomeOtherType
			{
			}
			#endif

			public class MyTypeConverter : MessagePackConverter<MyType>
			{
				public override void Read(ref MessagePackReader reader, ref MyType? value, SerializationContext context)
				{
					if (reader.TryReadNil())
					{
						value = null;
					}
					else
					{
						value = new MyType
						{
							SomeField = (SomeOtherType)context.GetConverter(typeof(SomeOtherType), null).ReadObject(ref reader, context),
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

					context.GetConverter(typeof(SomeOtherType), null).WriteObject(ref writer, value.SomeField, context);
				}

				public override System.Text.Json.Nodes.JsonObject GetJsonSchema(JsonSchemaContext context, PolyType.Abstractions.ITypeShape typeShape) => throw new System.NotImplementedException();
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
				public override void Read(ref MessagePackReader reader, ref TimeSpan? value, SerializationContext context) => value = new TimeSpan(reader.ReadInt64());

				public override void Write(ref MessagePackWriter writer, in TimeSpan value, SerializationContext context) => writer.Write(value.Ticks);

				public override System.Text.Json.Nodes.JsonObject GetJsonSchema(JsonSchemaContext context, PolyType.Abstractions.ITypeShape typeShape) => throw new System.NotImplementedException();
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
				public override void Read(ref MessagePackReader reader, ref Int16? value, SerializationContext context) => value = reader.ReadInt16();

				public override void Write(ref MessagePackWriter writer, in Int16 value, SerializationContext context) => writer.Write(value);

				public override System.Text.Json.Nodes.JsonObject GetJsonSchema(JsonSchemaContext context, PolyType.Abstractions.ITypeShape typeShape) => throw new System.NotImplementedException();
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
				public override void Read(ref MessagePackReader reader, ref Version? value, SerializationContext context) => value = reader.ReadString() is string str ? new Version(str) : null;

				public override void Write(ref MessagePackWriter writer, in Version value, SerializationContext context) => writer.Write(value?.ToString());

				public override System.Text.Json.Nodes.JsonObject GetJsonSchema(JsonSchemaContext context, PolyType.Abstractions.ITypeShape typeShape) => throw new System.NotImplementedException();
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
				public override void Read(ref MessagePackReader reader, ref Version? value, SerializationContext context) => value = reader.ReadString() is string str ? new Version(str) : null;

				public override void Write(ref MessagePackWriter writer, in Version value, SerializationContext context)
				{
					Span<byte> span = writer.GetSpan(5);
					// Assume something is written to the span.
					writer.Advance(3);
				}

				public override System.Text.Json.Nodes.JsonObject GetJsonSchema(JsonSchemaContext context, PolyType.Abstractions.ITypeShape typeShape) => throw new System.NotImplementedException();
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
				public override void Read(ref MessagePackReader reader, ref TArray? value, SerializationContext context)
				{
					reader.Skip(context);
					value = default;
				}

				public override void Write(ref MessagePackWriter writer, in TArray value, SerializationContext context) => throw new NotImplementedException();
				public override System.Text.Json.Nodes.JsonObject GetJsonSchema(JsonSchemaContext context, PolyType.Abstractions.ITypeShape typeShape) => throw new System.NotImplementedException();
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
				[My]
				public override void Read(ref MessagePackReader reader, ref int? value, SerializationContext context)
				{
					value = reader.ReadInt32();
				}

				public override void Write(ref MessagePackWriter writer, in int value, SerializationContext context) => throw new NotImplementedException();
				public override System.Text.Json.Nodes.JsonObject GetJsonSchema(JsonSchemaContext context, PolyType.Abstractions.ITypeShape typeShape) => throw new System.NotImplementedException();
			}

			[AttributeUsage(AttributeTargets.Method)]
			internal class MyAttribute : Attribute { }
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task NoIssues_TryReadStringSpan()
	{
		string source = /* lang=c#-test */ """
			using System;
			using System.Diagnostics.CodeAnalysis;
			using PolyType;
			using PolyType.Abstractions;
			using Nerdbank.MessagePack;

			internal class ArrayWithFlattenedDimensionsConverter(MessagePackConverter<int> primitiveConverter) : MessagePackConverter<int>
			{
				public override void Read(ref MessagePackReader reader, ref int? value, SerializationContext context)
				{
					if (reader.NextMessagePackType == MessagePackType.String)
					{
						string stringValue;

						// Try to avoid any allocations by reading the string as a span.
						// This only works for case sensitive matches, so be prepared to fallback to string comparisons.
						if (reader.TryReadStringSpan(out ReadOnlySpan<byte> span))
						{
							value = span.Length;
						}
						else
						{
							stringValue = reader.ReadString()!;
							value = stringValue.Length;
						}
					}
					else
					{
						value = primitiveConverter.Read(ref reader, context)!;
					}
				}

				public override void Write(ref MessagePackWriter writer, in int value, SerializationContext context) => throw new NotImplementedException();

				public override System.Text.Json.Nodes.JsonObject GetJsonSchema(JsonSchemaContext context, PolyType.Abstractions.ITypeShape typeShape) => throw new System.NotImplementedException();
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task NoIssues_GetJsonSchema_NotOverriddenInAbstractClass()
	{
		string source = /* lang=c#-test */ """
			using PolyType;
			using Nerdbank.MessagePack;

			public class MyType { }

			public abstract class MyTypeConverter : MessagePackConverter<MyType>
			{
				public override void Read(ref MessagePackReader reader, ref MyType? value, SerializationContext context) => throw new System.NotImplementedException();
				public override void Write(ref MessagePackWriter writer, in MyType value, SerializationContext context) => throw new System.NotImplementedException();
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task NoIssues_GetJsonSchema_OverrideInBaseClass()
	{
		string source = /* lang=c#-test */ """
			using PolyType;
			using Nerdbank.MessagePack;

			public class MyType { }

			public class MyTypeConverter : MessagePackConverter<MyType>
			{
				public override void Read(ref MessagePackReader reader, ref MyType? value, SerializationContext context) => throw new System.NotImplementedException();
				public override void Write(ref MessagePackWriter writer, in MyType value, SerializationContext context) => throw new System.NotImplementedException();
				public override System.Text.Json.Nodes.JsonObject GetJsonSchema(JsonSchemaContext context, PolyType.Abstractions.ITypeShape typeShape) => throw new System.NotImplementedException();
			}

			public class DerivedConverter : MyTypeConverter
			{
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

			#if NET
			[GenerateShape]
			public partial class MyType
			{
			}
			#else
			public partial class MyType
			{
			}
			#endif

			public class MyTypeConverter : MessagePackConverter<MyType>
			{
				public override void Read(ref MessagePackReader reader, ref MyType? value, SerializationContext context) => throw new System.NotImplementedException();
				public override void Write(ref MessagePackWriter writer, in MyType value, SerializationContext context)
				{
					var serializer = {|NBMsgPack030:new MessagePackSerializer()|};
					{|NBMsgPack030:serializer.Serialize(
						value
			#if !NET
						, (ITypeShape<MyType>)null!
			#endif
						)|};
					throw new System.NotImplementedException();
				}
				public override System.Text.Json.Nodes.JsonObject GetJsonSchema(JsonSchemaContext context, PolyType.Abstractions.ITypeShape typeShape) => throw new System.NotImplementedException();
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
				public override void Read(ref MessagePackReader reader, ref MyType? value, SerializationContext context)
				{
					reader.ReadInt32();
					{|NBMsgPack031:reader.ReadInt16()|};
					value = new MyType();
				}

				public override void Write(ref MessagePackWriter writer, in MyType value, SerializationContext context)
				{
					writer.Write(1);
					{|NBMsgPack031:writer.Write(2)|};
				}

				public override System.Text.Json.Nodes.JsonObject GetJsonSchema(JsonSchemaContext context, PolyType.Abstractions.ITypeShape typeShape) => throw new System.NotImplementedException();
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
				public override void {|NBMsgPack031:Read|}(ref MessagePackReader reader, ref MyType? value, SerializationContext context)
				{
					value = new MyType();
				}

				public override void {|NBMsgPack031:Write|}(ref MessagePackWriter writer, in MyType value, SerializationContext context)
				{
				}

				public override System.Text.Json.Nodes.JsonObject GetJsonSchema(JsonSchemaContext context, PolyType.Abstractions.ITypeShape typeShape) => throw new System.NotImplementedException();

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
				public override void Read(ref MessagePackReader reader, ref string? value, SerializationContext context)
					=> value = reader.ReadString() + "R";

				public override void Write(ref MessagePackWriter writer, in string value, SerializationContext context)
					=> writer.Write(value + "W");

				public override System.Text.Json.Nodes.JsonObject GetJsonSchema(JsonSchemaContext context, PolyType.Abstractions.ITypeShape typeShape) => throw new System.NotImplementedException();
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
				public override void Read(ref MessagePackReader reader, ref string? value, SerializationContext context)
					=> value = reader.ReadString() + {|NBMsgPack031:reader.ReadString()|};

				public override void Write(ref MessagePackWriter writer, in string value, SerializationContext context)
					=> writer.Write(value);

				public override System.Text.Json.Nodes.JsonObject GetJsonSchema(JsonSchemaContext context, PolyType.Abstractions.ITypeShape typeShape) => throw new System.NotImplementedException();
			}
			""";
		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task ShouldOverrideGetJsonSchema()
	{
		string source = /* lang=c#-test */ """
			using PolyType;
			using Nerdbank.MessagePack;

			public class MyType { }

			public class {|NBMsgPack032:MyTypeConverter|} : MessagePackConverter<MyType>
			{
				public override void Read(ref MessagePackReader reader, ref MyType? value, SerializationContext context) => throw new System.NotImplementedException();
				public override void Write(ref MessagePackWriter writer, in MyType value, SerializationContext context) => throw new System.NotImplementedException();
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task AsyncConverter_ShouldOverridePreferAsyncSerialization()
	{
		string source = /* lang=c#-test */ """
			#pragma warning disable NBMsgPackAsync

			using System;
			using System.Text.Json.Nodes;
			using System.Threading.Tasks;
			using PolyType.Abstractions;
			using Nerdbank.MessagePack;

			class {|NBMsgPack037:MyConverter|} : MessagePackConverter<int>
			{
				public override void Read(ref MessagePackReader reader, ref int? value, SerializationContext context) => throw new NotImplementedException();

				public override void Write(ref MessagePackWriter writer, in int value, SerializationContext context) => throw new NotImplementedException();

				public override JsonObject GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape) => throw new NotImplementedException();

				public override ValueTask WriteAsync(MessagePackAsyncWriter writer, int value, SerializationContext context) => throw new NotImplementedException();

				public override ValueTask<int> ReadAsync(MessagePackAsyncReader reader, SerializationContext context) => throw new NotImplementedException();
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task AsyncConverter_ReturnsReaderWriter()
	{
		string source = /* lang=c#-test */ """
			#pragma warning disable NBMsgPackAsync

			using System;
			using System.Text.Json.Nodes;
			using System.Threading.Tasks;
			using PolyType.Abstractions;
			using Nerdbank.MessagePack;

			class MyConverter : MessagePackConverter<int>
			{
				public override bool PreferAsyncSerialization => true;

				public override void Read(ref MessagePackReader reader, ref int? value, SerializationContext context) => throw new NotImplementedException();

				public override void Write(ref MessagePackWriter writer, in int value, SerializationContext context) => throw new NotImplementedException();

				public override JsonObject GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape) => throw new NotImplementedException();

				public override async ValueTask WriteAsync(MessagePackAsyncWriter writer, int value, SerializationContext context)
				{
					MessagePackWriter syncWriter = writer.CreateWriter();
					syncWriter.Write(value);
					writer.ReturnWriter(ref syncWriter);

					await Task.Yield();

					syncWriter = writer.CreateWriter();
					writer.ReturnWriter(ref syncWriter);

					await writer.FlushIfAppropriateAsync(context).ConfigureAwait(false);
				}

				public override async ValueTask<int> ReadAsync(MessagePackAsyncReader reader, SerializationContext context)
				{
					MessagePackReader bufferedReader = reader.CreateBufferedReader();
					bufferedReader.ReadInt32();
					reader.ReturnReader(ref bufferedReader);

					MessagePackStreamingReader streamingReader = reader.CreateStreamingReader();
					while (streamingReader.TryRead(out int value).NeedsMoreBytes())
					{
						streamingReader = new(await streamingReader.FetchMoreBytesAsync());
					}

					reader.ReturnReader(ref streamingReader);

					return 5;
				}
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task AsyncConverter_UsesReaderWriterAfterReturn()
	{
		string source = /* lang=c#-test */ """
			#pragma warning disable NBMsgPackAsync

			using System;
			using System.Text.Json.Nodes;
			using System.Threading.Tasks;
			using PolyType.Abstractions;
			using Nerdbank.MessagePack;

			class MyConverter : MessagePackConverter<int>
			{
				public override bool PreferAsyncSerialization => true;

				public override void Read(ref MessagePackReader reader, ref int? value, SerializationContext context) => throw new NotImplementedException();

				public override void Write(ref MessagePackWriter writer, in int value, SerializationContext context) => throw new NotImplementedException();

				public override JsonObject GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape) => throw new NotImplementedException();

				public override async ValueTask WriteAsync(MessagePackAsyncWriter writer, int value, SerializationContext context)
				{
					MessagePackWriter syncWriter = writer.CreateWriter();
					syncWriter.Write(value);
					writer.ReturnWriter(ref syncWriter);
					writer.ReturnWriter(ref {|NBMsgPack034:syncWriter|});
					{|NBMsgPack034:syncWriter|}.Write(value);
					await writer.FlushIfAppropriateAsync(context).ConfigureAwait(false);
				}

				public override async ValueTask<int> ReadAsync(MessagePackAsyncReader reader, SerializationContext context)
				{
					MessagePackReader bufferedReader = reader.CreateBufferedReader();
					bufferedReader.ReadInt32();
					reader.ReturnReader(ref bufferedReader);
					reader.ReturnReader(ref {|NBMsgPack036:bufferedReader|});
					{|NBMsgPack036:bufferedReader|}.ReadInt32();

					MessagePackStreamingReader streamingReader = reader.CreateStreamingReader();
					while (streamingReader.TryRead(out int value).NeedsMoreBytes())
					{
						streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
					}

					reader.ReturnReader(ref streamingReader);
					reader.ReturnReader(ref {|NBMsgPack036:streamingReader|});
					{|NBMsgPack036:streamingReader|}.TryRead(out int value2);

					return 5;
				}
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task AsyncConverter_DoesNotReturnReaderWriter()
	{
		string source = /* lang=c#-test */ """
			#pragma warning disable NBMsgPackAsync

			using System;
			using System.Text.Json.Nodes;
			using System.Threading.Tasks;
			using PolyType.Abstractions;
			using Nerdbank.MessagePack;

			class MyConverter : MessagePackConverter<int>
			{
				public override bool PreferAsyncSerialization => true;

				public override void Read(ref MessagePackReader reader, ref int? value, SerializationContext context) => throw new NotImplementedException();

				public override void Write(ref MessagePackWriter writer, in int value, SerializationContext context) => throw new NotImplementedException();

				public override JsonObject GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape) => throw new NotImplementedException();

				public override async ValueTask WriteAsync(MessagePackAsyncWriter writer, int value, SerializationContext context)
				{
					MessagePackWriter syncWriter = writer.CreateWriter();
					syncWriter.Write(value);
					//writer.ReturnWriter(ref syncWriter);

					{|NBMsgPack033:await|} {|NBMsgPack033:writer.FlushIfAppropriateAsync(context)|}.ConfigureAwait(false);
					{|NBMsgPack033:writer.CreateWriter()|};
				{|NBMsgPack033:}|}

				public override async ValueTask<int> ReadAsync(MessagePackAsyncReader reader, SerializationContext context)
				{
					MessagePackReader bufferedReader = reader.CreateBufferedReader();
					bufferedReader.ReadInt32();
					//reader.ReturnReader(ref bufferedReader);

					MessagePackStreamingReader streamingReader = {|NBMsgPack035:reader.CreateStreamingReader()|};
					while (streamingReader.TryRead(out int value).NeedsMoreBytes())
					{
						streamingReader = new(await streamingReader.FetchMoreBytesAsync());
					}

					//reader.ReturnReader(ref streamingReader);

					{|NBMsgPack035:return|} 5;
				}
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task AsyncConverter_UsesAsyncIOWhileRentalIsCurrent()
	{
		string source = /* lang=c#-test */ """
			#pragma warning disable NBMsgPackAsync

			using System;
			using System.Text.Json.Nodes;
			using System.Threading.Tasks;
			using PolyType.Abstractions;
			using Nerdbank.MessagePack;

			class MyConverter : MessagePackConverter<int>
			{
				public override bool PreferAsyncSerialization => true;

				public override void Read(ref MessagePackReader reader, ref int? value, SerializationContext context) => throw new NotImplementedException();

				public override void Write(ref MessagePackWriter writer, in int value, SerializationContext context) => throw new NotImplementedException();

				public override JsonObject GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape) => throw new NotImplementedException();

				public override async ValueTask WriteAsync(MessagePackAsyncWriter writer, int value, SerializationContext context)
				{
					MessagePackWriter syncWriter = writer.CreateWriter();
					{|NBMsgPack033:writer.WriteNil()|};
					{|NBMsgPack033:await|} {|NBMsgPack033:writer.FlushIfAppropriateAsync(context)|};
					{|NBMsgPack033:writer.Write((ref MessagePackWriter w, int s) => { }, 5)|};
					syncWriter.Write(value);
					writer.ReturnWriter(ref syncWriter);

					await writer.FlushIfAppropriateAsync(context).ConfigureAwait(false);
				}

				public override async ValueTask<int> ReadAsync(MessagePackAsyncReader reader, SerializationContext context)
				{
					MessagePackReader bufferedReader = reader.CreateBufferedReader();
					{|NBMsgPack035:await|} {|NBMsgPack035:reader.ReadAsync()|};
					{|NBMsgPack035:await|} {|NBMsgPack035:reader.BufferNextStructureAsync(context)|};
					bufferedReader.ReadInt32();
					reader.ReturnReader(ref bufferedReader);

					MessagePackStreamingReader streamingReader = reader.CreateStreamingReader();
					while (streamingReader.TryRead(out int value).NeedsMoreBytes())
					{
						streamingReader = new(await streamingReader.FetchMoreBytesAsync());
					}

					reader.ReturnReader(ref streamingReader);

					return 5;
				}
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}
}
