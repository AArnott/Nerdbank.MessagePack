// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = CodeFixVerifier<Nerdbank.MessagePack.Analyzers.DefaultValueInitializerAnalyzer, Nerdbank.MessagePack.Analyzers.CodeFixes.DefaultValueInitializerCodeFix>;

public class DefaultValueInitializerAnalyzerTests
{
	[Fact]
	public async Task NoIssues_NoGenerateShapeAttribute()
	{
		string source = /* lang=c#-test */ """
			using PolyType;

			public partial class MyType
			{
				public int MyField = 42;
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task NoIssues_NoInitializer()
	{
		string source = /* lang=c#-test */ """
			using PolyType;

			[GenerateShape]
			public partial class MyType
			{
				public int MyField;
				public int MyProperty { get; set; }
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task NoIssues_HasDefaultValueAttribute()
	{
		string source = /* lang=c#-test */ """
			using PolyType;
			using System.ComponentModel;

			[GenerateShape]
			public partial class MyType
			{
				[DefaultValue(42)]
				public int MyField = 42;

				[DefaultValue("test")]
				public string MyProperty { get; set; } = "test";
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task NoIssues_IgnoredProperty()
	{
		string source = /* lang=c#-test */ """
			using PolyType;

			[GenerateShape]
			public partial class MyType
			{
				[PropertyShape(Ignore = true)]
				public int MyField = 42;
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task FieldWithIntInitializer()
	{
		string source = /* lang=c#-test */ """
			using PolyType;

			[GenerateShape]
			public partial class MyType
			{
				public int {|NBMsgPack110:MyField|} = 42;
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task FieldWithEnumInitializer()
	{
		string source = /* lang=c#-test */ """
			using PolyType;

			[GenerateShape]
			public partial class TestClass
			{
				public TestEnum {|NBMsgPack110:MyEnum|} = TestEnum.Second;
			}

			public enum TestEnum
			{
				First = 0,
				Second = 1
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task PropertyWithStringInitializer()
	{
		string source = /* lang=c#-test */ """
			using PolyType;

			[GenerateShape]
			public partial class MyType
			{
				public string {|NBMsgPack110:MyProperty|} { get; set; } = "test";
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task CodeFix_FieldWithIntInitializer()
	{
		string source = /* lang=c#-test */ """
			using PolyType;

			[GenerateShape]
			public partial class MyType
			{
				public int {|NBMsgPack110:MyField|} = 42;
			}
			""";

		string fixedSource = /* lang=c#-test */ """
			using PolyType;

			[GenerateShape]
			public partial class MyType
			{
				[System.ComponentModel.DefaultValue(42)]
				public int MyField = 42;
			}
			""";

		await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
	}

	[Fact]
	public async Task CodeFix_FieldWithEnumInitializer()
	{
		string source = /* lang=c#-test */ """
			using PolyType;

			[GenerateShape]
			public partial class TestClass
			{
				public TestEnum {|NBMsgPack110:MyEnum|} = TestEnum.Second;
			}

			public enum TestEnum
			{
				First = 0,
				Second = 1
			}
			""";

		string fixedSource = /* lang=c#-test */ """
			using PolyType;

			[GenerateShape]
			public partial class TestClass
			{
				[System.ComponentModel.DefaultValue(TestEnum.Second)]
				public TestEnum MyEnum = TestEnum.Second;
			}

			public enum TestEnum
			{
				First = 0,
				Second = 1
			}
			""";

		await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
	}

	[Fact]
	public async Task CodeFix_PropertyWithStringInitializer()
	{
		string source = /* lang=c#-test */ """
			using PolyType;

			[GenerateShape]
			public partial class MyType
			{
				public string {|NBMsgPack110:MyProperty|} { get; set; } = "test";
			}
			""";

		string fixedSource = /* lang=c#-test */ """
			using PolyType;

			[GenerateShape]
			public partial class MyType
			{
				[System.ComponentModel.DefaultValue("test")]
				public string MyProperty { get; set; } = "test";
			}
			""";

		await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
	}

	[Fact]
	public async Task CodeFix_PropertyWithNegativeNumberInitializer()
	{
		string source = /* lang=c#-test */ """
			using PolyType;

			[GenerateShape]
			public partial class MyType
			{
				public int {|NBMsgPack110:MyProperty|} { get; set; } = -42;
			}
			""";

		string fixedSource = /* lang=c#-test */ """
			using PolyType;

			[GenerateShape]
			public partial class MyType
			{
				[System.ComponentModel.DefaultValue(-42)]
				public int MyProperty { get; set; } = -42;
			}
			""";

		await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
	}

	[Fact]
	public async Task MultipleFieldsWithInitializers()
	{
		string source = /* lang=c#-test */ """
			using PolyType;

			[GenerateShape]
			public partial class MyType
			{
				public int {|NBMsgPack110:Field1|} = 1;
				public string {|NBMsgPack110:Field2|} = "test";
				public bool {|NBMsgPack110:Field3|} = true;
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task TransitiveTypeWithInitializer()
	{
		string source = /* lang=c#-test */ """
			using PolyType;

			[GenerateShape]
			public partial class RootType
			{
				public NestedType Nested { get; set; }
			}

			public partial class NestedType
			{
				public int {|NBMsgPack110:InitializedField|} = 42;
			}

			public partial class UnrelatedType
			{
				public int UnrelatedField = 42;
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task TransitiveTypeWithInitializer_UnreachableDueToIgnoredProperty()
	{
		string source = /* lang=c#-test */ """
			using PolyType;

			[GenerateShape]
			public partial class RootType
			{
				[PropertyShape(Ignore = true)]
				public NestedType Nested1 { get; set; }

				private NestedType Nested2 { get; set; }
			}

			public partial class NestedType
			{
				public int InitializedField = 42;
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task TransitiveTypeMultipleLevels()
	{
		string source = /* lang=c#-test */ """
			using PolyType;

			[GenerateShape]
			public partial class RootType
			{
				public Level1Type Level1 { get; set; }
			}

			public partial class Level1Type
			{
				public Level2Type Level2 { get; set; }
			}

			public partial class Level2Type
			{
				public string {|NBMsgPack110:Data|} = "default";
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}
}
