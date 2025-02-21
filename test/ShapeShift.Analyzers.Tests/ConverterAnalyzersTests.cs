﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = CodeFixVerifier<ShapeShift.Analyzers.ConverterAnalyzers, Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

public class ConverterAnalyzersTests
{
	[Fact]
	public async Task NoIssues()
	{
		string source = /* lang=c#-test */ """
			using PolyType;
			using ShapeShift.Converters;

			public class MyType { }

			public class MyTypeConverter : Converter<MyType>
			{
				public override MyType Read(ref Reader reader, SerializationContext context) => throw new System.NotImplementedException();
				public override void Write(ref Writer writer, in MyType value, SerializationContext context) => throw new System.NotImplementedException();
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
			using ShapeShift.Converters;

			public class MyType { }

			public class MyTypeConverter : Converter<MyType>
			{
				public override MyType Read(ref Reader reader, SerializationContext context)
				{
					if (reader.TryReadNull())
					{
						return null;
					}

					int? count = reader.ReadStartVector();
					bool isFirstElement = true;
					for (int i = 0; i < count || (count is null && reader.TryAdvanceToNextElement(ref isFirstElement)); i++)
					{
						reader.Skip(context);
					}

					return new MyType();
				}

				public override void Write(ref Writer writer, in MyType value, SerializationContext context)
				{
					if (value is null)
					{
						writer.WriteNull();
						return;
					}

					writer.WriteStartVector(3);
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
			using ShapeShift.Converters;

			public class MyType { }

			public class MyTypeConverter : Converter<MyType>
			{
				public override MyType Read(ref Reader reader, SerializationContext context)
				{
					if (!reader.TryReadNull())
					{
						int? count = reader.ReadStartMap();
						bool isFirstElement = true;
						for (int i = 0; i < count || (count is null && reader.TryAdvanceToNextElement(ref isFirstElement)); i++)
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

				public override void Write(ref Writer writer, in MyType value, SerializationContext context)
				{
					if (value is null)
					{
						writer.WriteNull();
						return;
					}

					writer.WriteStartMap(3);
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
			using ShapeShift.Converters;

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

			public class MyTypeConverter : Converter<MyType>
			{
				public override MyType Read(ref Reader reader, SerializationContext context)
				{
					if (reader.TryReadNull())
					{
						return null;
					}
					else
					{
						return new MyType
						{
			#if NET
							SomeField = context.GetConverter<SomeOtherType>().Read(ref reader, context),
			#else
							SomeField = context.GetConverter<SomeOtherType>(context.TypeShapeProvider).Read(ref reader, context),
			#endif
						};
					}
				}

				public override void Write(ref Writer writer, in MyType value, SerializationContext context)
				{
					if (value is null)
					{
						writer.WriteNull();
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
			using ShapeShift.Converters;

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

			public class MyTypeConverter : Converter<MyType>
			{
				public override MyType Read(ref Reader reader, SerializationContext context)
				{
					if (reader.TryReadNull())
					{
						return null;
					}
					else
					{
						return new MyType
						{
							SomeField = (SomeOtherType)context.GetConverter(typeof(SomeOtherType), null).ReadObject(ref reader, context),
						};
					}
				}

				public override void Write(ref Writer writer, in MyType value, SerializationContext context)
				{
					if (value is null)
					{
						writer.WriteNull();
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
			using ShapeShift.Converters;

			internal class TimeSpanConverter : Converter<TimeSpan>
			{
				public override TimeSpan Read(ref Reader reader, SerializationContext context) => new TimeSpan(reader.ReadInt64());

				public override void Write(ref Writer writer, in TimeSpan value, SerializationContext context) => writer.Write(value.Ticks);

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
			using ShapeShift.Converters;

			internal class Int16Converter : Converter<Int16>
			{
				public override Int16 Read(ref Reader reader, SerializationContext context) => reader.ReadInt16();

				public override void Write(ref Writer writer, in Int16 value, SerializationContext context) => writer.Write(value);

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
			using ShapeShift.Converters;

			internal class VersionConverter : Converter<Version>
			{
				public override Version Read(ref Reader reader, SerializationContext context) => reader.ReadString() is string value ? new Version(value) : null;

				public override void Write(ref Writer writer, in Version value, SerializationContext context) => writer.Write(value?.ToString());

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
			using ShapeShift.Converters;

			internal class VersionConverter : Converter<Version>
			{
				public override Version Read(ref Reader reader, SerializationContext context) => reader.ReadString() is string value ? new Version(value) : null;

				public override void Write(ref Writer writer, in Version value, SerializationContext context)
				{
					Span<byte> span = writer.Buffer.GetSpan(5);
					// Assume something is written to the span.
					writer.Buffer.Advance(3);
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
			using ShapeShift.Converters;

			internal class ArrayWithFlattenedDimensionsConverter<TArray, TElement> : Converter<TArray>
			{
				public override TArray Read(ref Reader reader, SerializationContext context)
				{
					reader.Skip(context);
					return default;
				}

				public override void Write(ref Writer writer, in TArray value, SerializationContext context) => throw new NotImplementedException();
				public override System.Text.Json.Nodes.JsonObject GetJsonSchema(JsonSchemaContext context, PolyType.Abstractions.ITypeShape typeShape) => throw new System.NotImplementedException();
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task NoIssues_MessagePackFormatterCall()
	{
		string source = /* lang=c#-test */ """
			using System;
			using System.Text.Json.Nodes;
			using PolyType.Abstractions;
			using ShapeShift.Converters;
			using ShapeShift.MessagePack;
			
			internal class DateTimeConverter : Converter<DateTime>
			{
				public override DateTime Read(ref Reader reader, SerializationContext context) => ((MessagePackDeformatter)reader.Deformatter).ReadDateTime(ref reader);

				public override void Write(ref Writer writer, in DateTime value, SerializationContext context) => ((MessagePackFormatter)writer.Formatter).Write(ref writer.Buffer, value);

				public override JsonObject GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape) => throw new NotImplementedException();
			}
			""";
		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task NoIssues_WriteToBuffer()
	{
		string source = /* lang=c#-test */ """
			using System;
			using System.Text.Json.Nodes;
			using PolyType.Abstractions;
			using ShapeShift;
			using ShapeShift.Converters;
			
			internal class PreformattedRawBytesConverter : Converter<PreformattedRawBytes>
			{
				public override PreformattedRawBytes Read(ref Reader reader, SerializationContext context) => new PreformattedRawBytes(reader.ReadRaw(context)).ToOwned();

				public override void Write(ref Writer writer, in PreformattedRawBytes value, SerializationContext context) => writer.Buffer.Write(value.RawBytes);

				public override JsonObject GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape) => throw new NotImplementedException();
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
			using ShapeShift.Converters;

			internal class ArrayWithFlattenedDimensionsConverter : Converter<int>
			{
				[My]
				public override int Read(ref Reader reader, SerializationContext context)
				{
					return reader.ReadInt32();
				}

				public override void Write(ref Writer writer, in int value, SerializationContext context) => throw new NotImplementedException();
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
			using ShapeShift.Converters;

			internal class ArrayWithFlattenedDimensionsConverter(Converter<int> primitiveConverter) : Converter<int>
			{
				public override int Read(ref Reader reader, SerializationContext context)
				{
					if (reader.NextTypeCode == TokenType.String)
					{
						string stringValue;

						// Try to avoid any allocations by reading the string as a span.
						// This only works for case sensitive matches, so be prepared to fallback to string comparisons.
						if (reader.TryReadStringSpan(out ReadOnlySpan<byte> span))
						{
							return span.Length;
						}
						else
						{
							stringValue = reader.ReadString()!;
						}

						return stringValue.Length;
					}
					else
					{
						return primitiveConverter.Read(ref reader, context)!;
					}
				}

				public override void Write(ref Writer writer, in int value, SerializationContext context) => throw new NotImplementedException();

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
			using ShapeShift.Converters;

			public class MyType { }

			public abstract class MyTypeConverter : Converter<MyType>
			{
				public override MyType Read(ref Reader reader, SerializationContext context) => throw new System.NotImplementedException();
				public override void Write(ref Writer writer, in MyType value, SerializationContext context) => throw new System.NotImplementedException();
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task NoIssues_GetJsonSchema_OverrideInBaseClass()
	{
		string source = /* lang=c#-test */ """
			using PolyType;
			using ShapeShift.Converters;

			public class MyType { }

			public class MyTypeConverter : Converter<MyType>
			{
				public override MyType Read(ref Reader reader, SerializationContext context) => throw new System.NotImplementedException();
				public override void Write(ref Writer writer, in MyType value, SerializationContext context) => throw new System.NotImplementedException();
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
			using ShapeShift;
			using ShapeShift.Converters;

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

			public class MyTypeConverter : Converter<MyType>
			{
				public override MyType Read(ref Reader reader, SerializationContext context) => throw new System.NotImplementedException();
				public override void Write(ref Writer writer, in MyType value, SerializationContext context)
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
			using ShapeShift.Converters;

			public class MyType { }

			public class MyTypeConverter : Converter<MyType>
			{
				public override MyType Read(ref Reader reader, SerializationContext context)
				{
					reader.ReadInt32();
					{|NBMsgPack031:reader.ReadInt16()|};
					return new MyType();
				}

				public override void Write(ref Writer writer, in MyType value, SerializationContext context)
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
			using ShapeShift.Converters;

			public class MyType { }

			public class MyTypeConverter : Converter<MyType>
			{
				public override MyType {|NBMsgPack031:Read|}(ref Reader reader, SerializationContext context)
				{
					return new MyType();
				}

				public override void {|NBMsgPack031:Write|}(ref Writer writer, in MyType value, SerializationContext context)
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
			using ShapeShift.Converters;

			class CustomStringConverter : Converter<string>
			{
				public override string Read(ref Reader reader, SerializationContext context)
					=> reader.ReadString() + "R";

				public override void Write(ref Writer writer, in string value, SerializationContext context)
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
			using ShapeShift.Converters;

			class CustomStringConverter : Converter<string>
			{
				public override string Read(ref Reader reader, SerializationContext context)
					=> reader.ReadString() + {|NBMsgPack031:reader.ReadString()|};

				public override void Write(ref Writer writer, in string value, SerializationContext context)
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
			using ShapeShift.Converters;

			public class MyType { }

			public class {|NBMsgPack032:MyTypeConverter|} : Converter<MyType>
			{
				public override MyType Read(ref Reader reader, SerializationContext context) => throw new System.NotImplementedException();
				public override void Write(ref Writer writer, in MyType value, SerializationContext context) => throw new System.NotImplementedException();
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
			using ShapeShift.Converters;

			class {|NBMsgPack037:MyConverter|} : Converter<int>
			{
				public override int Read(ref Reader reader, SerializationContext context) => throw new NotImplementedException();

				public override void Write(ref Writer writer, in int value, SerializationContext context) => throw new NotImplementedException();

				public override JsonObject GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape) => throw new NotImplementedException();

				public override ValueTask WriteAsync(AsyncWriter writer, int value, SerializationContext context) => throw new NotImplementedException();

				public override ValueTask<int> ReadAsync(AsyncReader reader, SerializationContext context) => throw new NotImplementedException();
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
			using ShapeShift.Converters;

			class MyConverter : Converter<int>
			{
				public override bool PreferAsyncSerialization => true;

				public override int Read(ref Reader reader, SerializationContext context) => throw new NotImplementedException();

				public override void Write(ref Writer writer, in int value, SerializationContext context) => throw new NotImplementedException();

				public override JsonObject GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape) => throw new NotImplementedException();

				public override async ValueTask WriteAsync(AsyncWriter writer, int value, SerializationContext context)
				{
					Writer syncWriter = writer.CreateWriter();
					syncWriter.Write(value);
					writer.ReturnWriter(ref syncWriter);

					await Task.Yield();

					syncWriter = writer.CreateWriter();
					writer.ReturnWriter(ref syncWriter);

					await writer.FlushIfAppropriateAsync(context).ConfigureAwait(false);
				}

				public override async ValueTask<int> ReadAsync(AsyncReader reader, SerializationContext context)
				{
					Reader bufferedReader = reader.CreateBufferedReader();
					bufferedReader.ReadInt32();
					reader.ReturnReader(ref bufferedReader);

					StreamingReader streamingReader = reader.CreateStreamingReader();
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
			using ShapeShift.Converters;

			class MyConverter : Converter<int>
			{
				public override bool PreferAsyncSerialization => true;

				public override int Read(ref Reader reader, SerializationContext context) => throw new NotImplementedException();

				public override void Write(ref Writer writer, in int value, SerializationContext context) => throw new NotImplementedException();

				public override JsonObject GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape) => throw new NotImplementedException();

				public override async ValueTask WriteAsync(AsyncWriter writer, int value, SerializationContext context)
				{
					Writer syncWriter = writer.CreateWriter();
					syncWriter.Write(value);
					writer.ReturnWriter(ref syncWriter);
					writer.ReturnWriter(ref {|NBMsgPack034:syncWriter|});
					{|NBMsgPack034:syncWriter|}.Write(value);
					await writer.FlushIfAppropriateAsync(context).ConfigureAwait(false);
				}

				public override async ValueTask<int> ReadAsync(AsyncReader reader, SerializationContext context)
				{
					Reader bufferedReader = reader.CreateBufferedReader();
					bufferedReader.ReadInt32();
					reader.ReturnReader(ref bufferedReader);
					reader.ReturnReader(ref {|NBMsgPack036:bufferedReader|});
					{|NBMsgPack036:bufferedReader|}.ReadInt32();

					StreamingReader streamingReader = reader.CreateStreamingReader();
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
			using ShapeShift.Converters;

			class MyConverter : Converter<int>
			{
				public override bool PreferAsyncSerialization => true;

				public override int Read(ref Reader reader, SerializationContext context) => throw new NotImplementedException();

				public override void Write(ref Writer writer, in int value, SerializationContext context) => throw new NotImplementedException();

				public override JsonObject GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape) => throw new NotImplementedException();

				public override async ValueTask WriteAsync(AsyncWriter writer, int value, SerializationContext context)
				{
					Writer syncWriter = writer.CreateWriter();
					syncWriter.Write(value);
					//writer.ReturnWriter(ref syncWriter);

					{|NBMsgPack033:await|} {|NBMsgPack033:writer.FlushIfAppropriateAsync(context)|}.ConfigureAwait(false);
					{|NBMsgPack033:writer.CreateWriter()|};
				{|NBMsgPack033:}|}

				public override async ValueTask<int> ReadAsync(AsyncReader reader, SerializationContext context)
				{
					Reader bufferedReader = reader.CreateBufferedReader();
					bufferedReader.ReadInt32();
					//reader.ReturnReader(ref bufferedReader);

					StreamingReader streamingReader = {|NBMsgPack035:reader.CreateStreamingReader()|};
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
			using ShapeShift.Converters;

			class MyConverter : Converter<int>
			{
				public override bool PreferAsyncSerialization => true;

				public override int Read(ref Reader reader, SerializationContext context) => throw new NotImplementedException();

				public override void Write(ref Writer writer, in int value, SerializationContext context) => throw new NotImplementedException();

				public override JsonObject GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape) => throw new NotImplementedException();

				public override async ValueTask WriteAsync(AsyncWriter writer, int value, SerializationContext context)
				{
					Writer syncWriter = writer.CreateWriter();
					{|NBMsgPack033:writer.WriteNull()|};
					{|NBMsgPack033:await|} {|NBMsgPack033:writer.FlushIfAppropriateAsync(context)|};
					syncWriter.Write(value);
					writer.ReturnWriter(ref syncWriter);

					await writer.FlushIfAppropriateAsync(context).ConfigureAwait(false);
				}

				public override async ValueTask<int> ReadAsync(AsyncReader reader, SerializationContext context)
				{
					Reader bufferedReader = reader.CreateBufferedReader();
					{|NBMsgPack035:await|} {|NBMsgPack035:reader.ReadAsync()|};
					{|NBMsgPack035:await|} {|NBMsgPack035:reader.BufferNextStructureAsync(context)|};
					bufferedReader.ReadInt32();
					reader.ReturnReader(ref bufferedReader);

					StreamingReader streamingReader = reader.CreateStreamingReader();
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
