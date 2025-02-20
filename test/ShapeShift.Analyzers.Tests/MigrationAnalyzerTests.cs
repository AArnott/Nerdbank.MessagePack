// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using VerifyCS = CodeFixVerifier<ShapeShift.Analyzers.MigrationAnalyzer, ShapeShift.Analyzers.CodeFixes.MigrationCodeFix>;

public class MigrationAnalyzerTests
{
	[Fact]
	public async Task Formatter()
	{
		string source = /* lang=c#-test */ """
			#nullable enable

			using MessagePack;
			using MessagePack.Formatters;

			[MessagePackFormatter(typeof(MyTypeFormatter))]
			public class MyType
			{
				public string? Name { get; set; }

				private class {|NBMsgPack100:MyTypeFormatter|} : IMessagePackFormatter<MyType?>
				{
					public MyType? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
					{
						if (reader.TryReadNil())
						{
							return null;
						}

						string? name = null;
						options.Security.DepthStep(ref reader);
						try
						{
							int? count = reader.ReadArrayHeader();
							for (int i = 0; i < count; i++)
							{
								switch (i)
								{
									case 0:
										name = options.Resolver.GetFormatterWithVerify<string>().Deserialize(ref reader, options);
										break;
									default:
										reader.Skip();
										break;
									}
							}

							return new MyType { Name = name };
						}
						finally
						{
							reader.Depth--;
						}
					}

					public void Serialize(ref MessagePackWriter writer, MyType? value, MessagePackSerializerOptions options)
					{
						if (value is null)
						{
							writer.WriteNil();
							return;
						}

						writer.WriteArrayHeader(1);
						options.Resolver.GetFormatterWithVerify<string?>().Serialize(ref writer, value.Name, options);
					}
				}
			}
			""";

		string fixedSource = /* lang=c#-test */ """
			#nullable enable

			using MessagePack;
			using MessagePack.Formatters;
			using PolyType;
			using ShapeShift;
			using ShapeShift.Converters;

			[Converter(typeof(MyTypeFormatter))]
			public partial class MyType
			{
				public string? Name { get; set; }

				[GenerateShape<string>]
				private partial class MyTypeFormatter : Converter<MyType>
				{
					public override MyType? Read(ref Reader reader, SerializationContext context)
					{
						if (reader.TryReadNull())
						{
							return null;
						}

						string? name = null;
						context.DepthStep();
						int? count = reader.ReadStartVector();
						for (int i = 0; i < count || (count is null && reader.TryAdvanceToNextElement()); i++)
						{
							switch (i)
							{
								case 0:
									name = context.GetConverter<string>(MyTypeFormatter.ShapeProvider).Read(ref reader, context);
									break;
								default:
									reader.Skip(context);
									break;
							}
						}

						return new MyType { Name = name };
					}

					public override void Write(ref Writer writer, in MyType? value, SerializationContext context)
					{
						if (value is null)
						{
							writer.WriteNull();
							return;
						}

						writer.WriteStartVector(1);
						context.GetConverter<string>(MyTypeFormatter.ShapeProvider).Write(ref writer, value.Name, context);
					}
				}
			}
			""";

		await this.VerifyCodeFixAsync(source, fixedSource, 2);
	}

	[Fact]
	public async Task MessagePackObject_Keys()
	{
		string source = /* lang=c#-test */ """
			using MessagePack;
			using MessagePack.Formatters;

			/// <summary>
			/// Doc comment
			/// </summary>
			[{|NBMsgPack102:MessagePackObject|}]
			public class MyType
			{
				/// <summary>doc comment</summary>
				[{|NBMsgPack103:Key(0)|}]
				public string Name { get; set; }
			}
			""";

		string fixedSource = /* lang=c#-test */ """
			using MessagePack;
			using MessagePack.Formatters;

			/// <summary>
			/// Doc comment
			/// </summary>
			public class MyType
			{
				/// <summary>doc comment</summary>
				[ShapeShift.Key(0)]
				public string Name { get; set; }
			}
			""";

		await this.VerifyCodeFixAsync(source, fixedSource, 2);
	}

	[Fact]
	public async Task MessagePackObject_IgnoreMember()
	{
		string source = /* lang=c#-test */ """
			using MessagePack;
			using MessagePack.Formatters;

			public class MyType
			{
				public string Name { get; set; }

				[{|NBMsgPack104:IgnoreMember|}]
				public string PublicIgnored { get; set; }
				
				[{|NBMsgPack104:IgnoreMember|}]
				internal string NonPublicIgnored { get; set; }
			}
			""";

		string fixedSource = /* lang=c#-test */ """
			using MessagePack;
			using MessagePack.Formatters;
			using PolyType;

			public class MyType
			{
				public string Name { get; set; }

				[PropertyShape(Ignore = true)]
				public string PublicIgnored { get; set; }

				internal string NonPublicIgnored { get; set; }
			}
			""";

		await this.VerifyCodeFixAsync(source, fixedSource, 2);
	}

	[Fact]
	public async Task MessagePackObject_Map()
	{
		string source = /* lang=c#-test */ """
			using MessagePack;
			using MessagePack.Formatters;

			[{|NBMsgPack102:MessagePackObject(true)|}]
			public class MyType
			{
				public string Name { get; set; }

				[{|NBMsgPack103:Key("AnotherName")|}]
				public string AnotherProperty { get; set; }
			}
			""";

		string fixedSource = /* lang=c#-test */ """
			using MessagePack;
			using MessagePack.Formatters;
			using PolyType;

			public class MyType
			{
				public string Name { get; set; }
			
				[PropertyShape(Name = "AnotherName")]
				public string AnotherProperty { get; set; }
			}
			""";

		await this.VerifyCodeFixAsync(source, fixedSource, 2);
	}

	/// <summary>
	/// Verifies that [GenerateShape] is added when removing MessagePackObjectAttribute
	/// when the class is used in a top level call to MessagePackSerializer.
	/// </summary>
	[Fact]
	public async Task MessagePackObject_WithTopLevelUsage()
	{
		string source = /* lang=c#-test */ """
			using MessagePack;
			using MessagePack.Formatters;

			/// <summary>
			/// Doc comment
			/// </summary>
			[{|NBMsgPack102:MessagePackObject(true)|}]
			public class MyType
			{
				public string Name { get; set; }
			}

			class Other
			{
				void Foo()
				{
					MessagePackSerializer.Serialize(new MyType());
				}
			}
			""";

		string fixedSource = /* lang=c#-test */ """
			using MessagePack;
			using MessagePack.Formatters;
			using PolyType;

			/// <summary>
			/// Doc comment
			/// </summary>
			[GenerateShape]
			public partial class MyType
			{
				public string Name { get; set; }
			}

			class Other
			{
				void Foo()
				{
					MessagePackSerializer.Serialize(new MyType());
				}
			}
			""";

		await this.VerifyCodeFixAsync(source, fixedSource);
	}

	[Fact]
	public async Task ClassImplementsOldCallbackInterface_ExplicitMethods()
	{
		string source = /* lang=c#-test */ """
			using MessagePack;

			class A : {|NBMsgPack105:IMessagePackSerializationCallbackReceiver|}
			{
				void IMessagePackSerializationCallbackReceiver.OnAfterDeserialize()
				{
					// deserialize
				}

				void IMessagePackSerializationCallbackReceiver.OnBeforeSerialize()
				{
					// serialize
				}
			}
			""";

		string fixedSource = /* lang=c#-test */ """
			using MessagePack;
			using ShapeShift;

			class A : ISerializationCallbacks
			{
				void ISerializationCallbacks.OnAfterDeserialize()
				{
					// deserialize
				}

				void ISerializationCallbacks.OnBeforeSerialize()
				{
					// serialize
				}
			}
			""";

		await this.VerifyCodeFixAsync(source, fixedSource);
	}

	[Fact]
	public async Task ClassImplementsOldCallbackInterface_PublicMethods()
	{
		string source = /* lang=c#-test */ """
			using MessagePack;

			class A : {|NBMsgPack105:IMessagePackSerializationCallbackReceiver|}
			{
				public void OnAfterDeserialize()
				{
					// deserialize
				}

				public void OnBeforeSerialize()
				{
					// serialize
				}
			}
			""";

		string fixedSource = /* lang=c#-test */ """
			using MessagePack;
			using ShapeShift;

			class A : ISerializationCallbacks
			{
				public void OnAfterDeserialize()
				{
					// deserialize
				}

				public void OnBeforeSerialize()
				{
					// serialize
				}
			}
			""";

		await this.VerifyCodeFixAsync(source, fixedSource);
	}

	[Fact]
	public async Task SerializationConstructor()
	{
		string source = /* lang=c#-test */ """
			using MessagePack;

			class A
			{
				[{|NBMsgPack106:SerializationConstructor|}]
				public A(int x)
				{
				}
			}
			""";

		string fixedSource = /* lang=c#-test */ """
			using MessagePack;
			using PolyType;

			class A
			{
				[ConstructorShape]
				public A(int x)
				{
				}
			}
			""";

		await this.VerifyCodeFixAsync(source, fixedSource);
	}

	private Task VerifyCodeFixAsync([StringSyntax("c#-test")] string source, [StringSyntax("c#-test")] string fixedSource, int iterations = 1)
	{
		return new VerifyCS.Test
		{
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
			TestCode = source.Replace("\t", "    "),
			FixedCode = fixedSource.Replace("\t", "    "),
			ReferenceAssemblies = ReferencesHelper.References.WithPackages([
				new PackageIdentity("MessagePack", "2.5.187"),
			]),
			NumberOfFixAllInDocumentIterations = iterations,
			NumberOfFixAllInProjectIterations = iterations,
			NumberOfFixAllIterations = iterations,
			NumberOfIncrementalIterations = iterations,
		}.RunAsync();
	}
}
