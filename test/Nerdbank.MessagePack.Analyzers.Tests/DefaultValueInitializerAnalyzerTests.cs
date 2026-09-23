// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = CodeFixVerifier<Nerdbank.MessagePack.Analyzers.DefaultValueInitializerAnalyzer, Nerdbank.MessagePack.Analyzers.CodeFixes.DefaultValueInitializerCodeFix>;

public class DefaultValueInitializerAnalyzerTests
{
	[Test]
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

	[Test]
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

	[Test]
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

	[Test]
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

	[Test]
	public async Task NonPublicPropertyIncludedByAttribute()
	{
		string source = /* lang=c#-test */ """
			using PolyType;

			[GenerateShape]
			public partial class MyType
			{
				[PropertyShape]
				private int {|NBMsgPack110:MyProperty|} { get; set; } = 42;
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Test]
	public async Task GenerateShapeForPattern()
	{
		string source = /* lang=c#-test */ """
			using PolyType;

			[GenerateShapeFor("Models.*")]
			public partial class Witness;

			namespace Models
			{
				public class Matched
				{
					public int {|NBMsgPack110:Value|} = 42;
				}
			}

			namespace Other
			{
				public class Unmatched
				{
					public int Value = 42;
				}
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Test]
	public async Task GenerateShapeForWildcardOnlyPattern()
	{
		string source = /* lang=c#-test */ """
			#pragma warning disable PT0015 // Verify this analyzer honors wildcard-only patterns independently of PolyType's broad-pattern warning.
			using PolyType;

			[GenerateShapeFor("*")]
			public partial class Witness;

			namespace Models
			{
				public class Matched
				{
					public int {|NBMsgPack110:Value|} = 42;
				}
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Test]
	public async Task GenerateShapeForPatternSkipsGenericTypeDefinitions()
	{
		string source = /* lang=c#-test */ """
			using PolyType;

			[GenerateShapeFor("Models.*")]
			public partial class Witness;

			namespace Models
			{
				public class NonGeneric;

				public class Generic<T>
				{
					public int Value = 42;
				}
			}

			public class Unmatched
			{
				public int Value = 42;
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Test]
	public async Task GenerateShapeForTypes()
	{
		string source = /* lang=c#-test */ """
			using PolyType;

			[GenerateShapeFor(typeof(NonGenericTarget))]
			[GenerateShapeFor<GenericTarget>]
			public partial class Witness;

			public class NonGenericTarget
			{
				public int {|NBMsgPack110:Value|} = 42;
			}

			public class GenericTarget
			{
				public int {|NBMsgPack110:Value|} = 42;
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Test]
	public async Task GenerateShapeForClosedGenericType()
	{
		string source = /* lang=c#-test */ """
			using PolyType;

			[GenerateShapeFor<Box<int>>]
			public partial class Witness;

			public class Box<T>
			{
				public int {|NBMsgPack110:Value|} = 42;
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Test]
	public async Task GenerateShapeForTypeWithCustomConfigurationIsIgnored()
	{
		string source = /* lang=c#-test */ """
			using PolyType;

			[GenerateShapeFor<ConfiguredTarget>(Kind = TypeShapeKind.None)]
			public partial class Witness;

			public class ConfiguredTarget
			{
				public int Value = 42;
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Test]
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

	[Test]
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

	[Test]
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

	[Test]
	public async Task NonConstantMemberTypesIgnored()
	{
		string source = /* lang=c#-test */ """
			using System.Collections.Generic;
			using PolyType;

			[GenerateShape]
			public partial class MyType
			{
				public object SomeProperty { get; set; } = new();
				public List<int> Values { get; set; } = new();
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Test]
	public async Task NonConstantInitializerHasNoCodeFix()
	{
		string source = /* lang=c#-test */ """
			using PolyType;

			[GenerateShape]
			public partial class MyType
			{
				public int {|NBMsgPack110:Computed|} { get; set; } = GetValue();

				private static int GetValue() => 42;
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
		var codeFixTest = new VerifyCS.Test
		{
			TestCode = source,
			FixedCode = source,
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
		};
		codeFixTest.FixedState.ExpectedDiagnostics.Add(VerifyCS.Diagnostic().WithSpan(6, 13, 6, 21).WithArguments("Computed"));
		await codeFixTest.RunAsync(CancellationToken.None);
	}

	[Test]
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
			|[global::System.ComponentModel.DefaultValue(42)]
			|public int MyField = 42;
			}
			""".Replace("|", "    ");

		await VerifyCodeFixWithGeneratedSourcesSkippedAsync(source, fixedSource);
	}

	[Test]
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
			|[global::System.ComponentModel.DefaultValue(TestEnum.Second)]
			|public TestEnum MyEnum = TestEnum.Second;
			}

			public enum TestEnum
			{
				First = 0,
				Second = 1
			}
			""".Replace("|", "    ");

		await VerifyCodeFixWithGeneratedSourcesSkippedAsync(source, fixedSource);
	}

	[Test]
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
			|[global::System.ComponentModel.DefaultValue("test")]
			|public string MyProperty { get; set; } = "test";
			}
			""".Replace("|", "    ");

		await VerifyCodeFixWithGeneratedSourcesSkippedAsync(source, fixedSource);
	}

	[Test]
	public async Task CodeFix_GloballyQualifiesAttributeName()
	{
		string source = /* lang=c#-test */ """
			using PolyType;
			using System = ShadowedSystem;

			public static class ShadowedSystem
			{
			}

			[GenerateShape]
			public partial class MyType
			{
				public int {|NBMsgPack110:MyProperty|} { get; set; } = 42;
			}
			""";

		string fixedSource = /* lang=c#-test */ """
			using PolyType;
			using System = ShadowedSystem;

			public static class ShadowedSystem
			{
			}

			[GenerateShape]
			public partial class MyType
			{
			|[global::System.ComponentModel.DefaultValue(42)]
			|public int MyProperty { get; set; } = 42;
			}
			""".Replace("|", "    ");

		await VerifyCodeFixWithGeneratedSourcesSkippedAsync(source, fixedSource);
	}

	[Test]
	public async Task CodeFix_CastsDefaultLiteralInitializer()
	{
		string source = /* lang=c#-test */ """
			using PolyType;

			[GenerateShape]
			public partial class MyType
			{
				public int {|NBMsgPack110:MyProperty|} { get; set; } = default;
			}
			""";

		string fixedSource = /* lang=c#-test */ """
			using PolyType;

			[GenerateShape]
			public partial class MyType
			{
			|[global::System.ComponentModel.DefaultValue((int)default)]
			|public int MyProperty { get; set; } = default;
			}
			""".Replace("|", "    ");

		await VerifyCodeFixWithGeneratedSourcesSkippedAsync(source, fixedSource);
	}

	[Test]
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
			|[global::System.ComponentModel.DefaultValue(-42)]
			|public int MyProperty { get; set; } = -42;
			}
			""".Replace("|", "    ");

		await VerifyCodeFixWithGeneratedSourcesSkippedAsync(source, fixedSource);
	}

	[Test]
	public async Task CodeFix_PropertyWithConstantExpressionInitializer()
	{
		string source = /* lang=c#-test */ """
			using PolyType;

			[GenerateShape]
			public partial class MyType
			{
				private const int BaseValue = 40;

				public int {|NBMsgPack110:MyProperty|} { get; set; } = BaseValue + 2;
			}
			""";

		string fixedSource = /* lang=c#-test */ """
			using PolyType;

			[GenerateShape]
			public partial class MyType
			{
				private const int BaseValue = 40;

			|[global::System.ComponentModel.DefaultValue(BaseValue + 2)]
			|public int MyProperty { get; set; } = BaseValue + 2;
			}
			""".Replace("|", "    ");

		await VerifyCodeFixWithGeneratedSourcesSkippedAsync(source, fixedSource);
	}

	[Test]
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

	[Test]
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

	[Test]
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

	[Test]
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

	[Test]
	public async Task FieldWithAdditionalIntegerTypes()
	{
		string source = /* lang=c#-test */ """
			using PolyType;

			[GenerateShape]
			public partial class MyType
			{
				public sbyte {|NBMsgPack110:SByteField|} = 1;
				public ushort {|NBMsgPack110:UShortField|} = 2;
				public uint {|NBMsgPack110:UIntField|} = 3;
				public ulong {|NBMsgPack110:ULongField|} = 4;
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Test]
	public async Task CodeFix_CastsInitializerWhenNeededForExactAttributeValueType()
	{
		string source = /* lang=c#-test */ """
			using PolyType;

			[GenerateShape]
			public partial class MyType
			{
				public short {|NBMsgPack110:ShortField|} = 1;
			}
			""";

		string fixedSource = /* lang=c#-test */ """
			using PolyType;

			[GenerateShape]
			public partial class MyType
			{
			|[global::System.ComponentModel.DefaultValue((short)1)]
			|public short ShortField = 1;
			}
			""".Replace("|", "    ");

		await VerifyCodeFixWithGeneratedSourcesSkippedAsync(source, fixedSource);
	}

	[Test]
	public async Task NoCodeFix_MultiVariableFieldDeclaration()
	{
		// The code fix must not be offered for a multi-variable field declaration, since the
		// resulting attribute would incorrectly apply to every variable in the declaration.
		string source = /* lang=c#-test */ """
			using PolyType;

			[GenerateShape]
			public partial class MyType
			{
				public int {|NBMsgPack110:First|} = 1, {|NBMsgPack110:Second|} = 2;
			}
			""";

		var codeFixTest = new VerifyCS.Test
		{
			TestCode = source,
			FixedCode = source,
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
		};
		codeFixTest.FixedState.ExpectedDiagnostics.Add(VerifyCS.Diagnostic().WithSpan(6, 13, 6, 18).WithArguments("First"));
		codeFixTest.FixedState.ExpectedDiagnostics.Add(VerifyCS.Diagnostic().WithSpan(6, 24, 6, 30).WithArguments("Second"));
		await codeFixTest.RunAsync(CancellationToken.None);
	}

	private static Task VerifyCodeFixWithGeneratedSourcesSkippedAsync(string source, string fixedSource)
	{
		// PolyType's source generator produces additional documents for these tests (since the
		// analyzer requires a real shape graph), so generated source checks are skipped here
		// rather than in the shared CodeFixVerifier helper, to preserve that regression coverage
		// for analyzers that don't rely on source generation.
		var test = new VerifyCS.Test
		{
			TestCode = source,
			FixedCode = fixedSource,
			TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
		};
		return test.RunAsync();
	}
}
