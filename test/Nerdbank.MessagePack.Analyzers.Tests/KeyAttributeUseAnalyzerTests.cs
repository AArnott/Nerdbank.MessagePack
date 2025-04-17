// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = CodeFixVerifier<Nerdbank.MessagePack.Analyzers.KeyAttributeUseAnalyzer, Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

public class KeyAttributeUseAnalyzerTests
{
	[Fact]
	public async Task NoIssues()
	{
		string source = /* lang=c#-test */ """
			using PolyType;
			using Nerdbank.MessagePack;

			[GenerateShape]
			public partial class MyType
			{
				[Key(0)]
				public int MyProperty1 { get; set; }

				[Key(1)]
				public int MyProperty2 { get; set; }

				[Key(2), PropertyShape]
				internal int MyProperty3 { get; set; }

				[PropertyShape]
				private UnusedDataPacket Extension;
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task KeyReuseInOneClass()
	{
		string source = /* lang=c#-test */ """
			using PolyType;
			using Nerdbank.MessagePack;

			[GenerateShape]
			public partial class MyType
			{
				[Key(0)]
				public int MyProperty1 { get; set; }

				[{|NBMsgPack003:Key(0)|}]
				public int MyProperty2 { get; set; }
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task KeyReuseAcrossClassHierarchy()
	{
		string source = /* lang=c#-test */ """
			using PolyType;
			using Nerdbank.MessagePack;

			[GenerateShape]
			public partial class MyBaseType
			{
				[{|NBMsgPack003:Key(0)|}]
				public int MyProperty1 { get; set; }
			}

			[GenerateShape]
			public partial class MyType : MyBaseType
			{
				[Key(0)]
				public int MyProperty2 { get; set; }
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task MissingKey()
	{
		string source = /* lang=c#-test */ """
			using PolyType;
			using Nerdbank.MessagePack;

			[GenerateShape]
			public partial class MyType
			{
				[Key(0)]
				public int MyProperty1 { get; set; }

				public int {|NBMsgPack001:MyProperty2|} { get; set; }
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task MissingKeyOnBaseType()
	{
		string source = /* lang=c#-test */ """
			using PolyType;
			using Nerdbank.MessagePack;

			[GenerateShape]
			public partial class MyBaseType
			{
				public int {|NBMsgPack001:MyProperty1|} { get; set; }
			}

			[GenerateShape]
			public partial class MyType : MyBaseType
			{
				[Key(1)]
				public int MyProperty2 { get; set; }
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task KeyOnNonSerializedInternalProperty()
	{
		string source = /* lang=c#-test */ """
			using PolyType;
			using Nerdbank.MessagePack;

			[GenerateShape]
			public partial class MyType
			{
				[Key(0)]
				public int MyProperty1 { get; set; }

				[{|NBMsgPack002:Key(1)|}]
				internal int MyProperty2 { get; set; }
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task KeyOnNonSerializedPublicProperty()
	{
		string source = /* lang=c#-test */ """
			using PolyType;
			using Nerdbank.MessagePack;

			[GenerateShape]
			public partial class MyType
			{
				[Key(0)]
				public int MyProperty1 { get; set; }

				[{|NBMsgPack002:Key(1)|}, PropertyShape(Ignore = true)]
				internal int MyProperty2 { get; set; }
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task KeyNotOnPropertyWithOnlyGetter()
	{
		string source = /* lang=c#-test */ """
			using PolyType;
			using Nerdbank.MessagePack;
			
			[GenerateShape]
			public partial record ClassWithUnserializedPropertyGetters
			{
				public string PropertyChanged => throw new System.NotImplementedException();

				[Key(0)]
				public bool Value { get; set; }
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task KeyNotOnPropertyWithOnlyGetterWithPropertyShapeAttribute()
	{
		string source = /* lang=c#-test */ """
			using PolyType;
			using Nerdbank.MessagePack;
			
			[GenerateShape]
			public partial record ClassWithUnserializedPropertyGetters
			{
				[PropertyShape]
				public string PropertyChanged => throw new System.NotImplementedException();

				[Key(0)]
				public bool Value { get; set; }
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task KeyNotOnPropertyWithOnlyGetterButAlsoHasCtorParam()
	{
		string source = /* lang=c#-test */ """
			using PolyType;
			using Nerdbank.MessagePack;
			
			[GenerateShape]
			public partial record ClassWithUnserializedPropertyGetters
			{
				public ClassWithUnserializedPropertyGetters(string propertyChanged) => this.PropertyChanged = propertyChanged;

				public string PropertyChanged { get; }

				[Key(0)]
				public bool {|NBMsgPack001:Value|} { get; set; }
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}
}
