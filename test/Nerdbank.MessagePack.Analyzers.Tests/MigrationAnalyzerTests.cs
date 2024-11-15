// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using VerifyCS = CodeFixVerifier<Nerdbank.MessagePack.Analyzers.MigrationAnalyzer, Nerdbank.MessagePack.Analyzers.CodeFixes.MigrationCodeFix>;

public class MigrationAnalyzerTests
{
	[Fact(Skip = "Not yet passing due to missing PolyType source generator")]
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
			}

			public class {|NBMsgPack100:MyTypeFormatter|} : IMessagePackFormatter<MyType?>
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
						int count = reader.ReadArrayHeader();
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
			""";

		string fixedSource = /* lang=c#-test */ """
			#nullable enable

			using MessagePack;
			using MessagePack.Formatters;
			using Nerdbank.MessagePack;
			using PolyType;

			[MessagePackConverter(typeof(MyTypeFormatter))]
			public class MyType
			{
				public string? Name { get; set; }
			}

			[GenerateShape<string>]
			public partial class MyTypeFormatter : MessagePackConverter<MyType>
			{
				public override MyType? Read(ref Nerdbank.MessagePack.MessagePackReader reader, SerializationContext context)
				{
					if (reader.TryReadNil())
					{
						return null;
					}

					string? name = null;
					context.DepthStep();
					int count = reader.ReadArrayHeader();
					for (int i = 0; i < count; i++)
					{
						switch (i)
						{
							case 0:
								name = context.GetConverter<string, MyTypeFormatter>().Read(ref reader, context);
								break;
							default:
								reader.Skip(context);
								break;
						}
					}

					return new MyType { Name = name };
				}

				public override void Write(ref Nerdbank.MessagePack.MessagePackWriter writer, in MyType? value, SerializationContext context)
				{
					if (value is null)
					{
						writer.WriteNil();
						return;
					}

					writer.WriteArrayHeader(1);
					context.GetConverter<string, MyTypeFormatter>().Write(ref writer, value.Name, context);
				}
			}
			""";

		await this.VerifyCodeFixAsync(source, fixedSource);
	}

	[Fact(Skip = "Not yet passing due to unexpected iteration count")]
	public async Task MessagePackObject_Keys()
	{
		string source = /* lang=c#-test */ """
			using MessagePack;
			using MessagePack.Formatters;

			[{|NBMsgPack102:MessagePackObject|}]
			public class MyType
			{
				[{|NBMsgPack103:Key(0)|}]
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
				[Nerdbank.MessagePack.Key(0)]
				public string Name { get; set; }

				[PropertyShape(Ignore = true)]
				public string PublicIgnored { get; set; }

				internal string NonPublicIgnored { get; set; }
			}
			""";

		await this.VerifyCodeFixAsync(source, fixedSource);
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
			}
			""";

		string fixedSource = /* lang=c#-test */ """
			using MessagePack;
			using MessagePack.Formatters;

			public class MyType
			{
				public string Name { get; set; }
			}
			""";

		await this.VerifyCodeFixAsync(source, fixedSource);
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

	private Task VerifyCodeFixAsync([StringSyntax("c#-test")] string source, [StringSyntax("c#-test")] string fixedSource)
	{
		return new VerifyCS.Test
		{
			TestCode = source.Replace("\t", "    "),
			FixedCode = fixedSource.Replace("\t", "    "),
			ReferenceAssemblies = ReferencesHelper.DefaultTargetFrameworkReferences.WithPackages([
				new PackageIdentity("MessagePack", "2.5.187"),
			]),
		}.RunAsync();
	}
}
