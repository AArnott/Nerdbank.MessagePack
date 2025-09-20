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
				public const int SomeConstant = 42;

				[Key(0)]
				public int MyProperty1 { get; set; }

				[Key(1)]
				public int MyProperty2 { get; set; }

				[PropertyShape(Ignore = true)]
				public int IgnoredProperty { get; set; }

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

	[Fact]
	public async Task KeyOnReadOnlyCollectionProperty()
	{
		string source = /* lang=c#-test */ """
			using PolyType;
			using Nerdbank.MessagePack;
			using System.Collections.Generic;

			[GenerateShape]
			public partial class MyType
			{
				[Key(0)]
				public int MyProperty1 { get; set; }

				[Key(1)]
				public Dictionary<string, string> Parameters { get; } = new Dictionary<string, string>();

				[Key(2)]
				public List<int> Numbers { get; } = new List<int>();
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task KeyOnReadOnlyCollectionPropertyVariousTypes()
	{
		string source = /* lang=c#-test */ """
			using PolyType;
			using Nerdbank.MessagePack;
			using System.Collections.Generic;
			using System.Collections.Concurrent;

			[GenerateShape]
			public partial class MyType
			{
				[Key(0)]
				public int MyProperty1 { get; set; }

				// Common mutable collections should be allowed
				[Key(1)]
				public Dictionary<string, string> Parameters { get; } = new();

				[Key(2)]
				public List<int> Numbers { get; } = new();

				[Key(3)]
				public HashSet<string> Tags { get; } = new();

				[Key(4)]
				public Queue<int> Items { get; } = new();

				[Key(5)]
				public ConcurrentDictionary<string, int> Counters { get; } = new();

				// Read-only collection field should also be allowed 
				[Key(6)]
				public readonly HashSet<string> ReadOnlyCollectionField = new();

				// Read-only property with non-collection type should still trigger the warning
				[{|NBMsgPack002:Key(7)|}]
				public string ReadOnlyString { get; } = "test";

				// Read-only field with non-collection type should still trigger the warning  
				[{|NBMsgPack002:Key(8)|}]
				public readonly string ReadOnlyStringField = "test";

				// Constructor parameter should be fine
				[Key(9)]
				public string CtorParam { get; }

				public MyType(string ctorParam) => CtorParam = ctorParam;
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task KeyOnReadOnlyCollectionFields()
	{
		string source = /* lang=c#-test */ """
			using PolyType;
			using Nerdbank.MessagePack;
			using System.Collections.Generic;
			using System.Collections.Concurrent;

			[GenerateShape]
			public partial class MyType
			{
				[Key(0)]
				public int MyProperty1 { get; set; }

				// Read-only collection fields should be allowed (no NBMsgPack002 warnings)
				[Key(1)]
				public readonly Dictionary<string, string> Parameters = new();

				[Key(2)]
				public readonly List<int> Numbers = new();

				[Key(3)]
				public readonly HashSet<string> Tags = new();

				[Key(4)]
				public readonly ConcurrentDictionary<string, int> Counters = new();

				// Read-only non-collection field should still trigger the warning
				[{|NBMsgPack002:Key(5)|}]
				public readonly string ReadOnlyString = "test";

				// Constructor parameter field should be fine
				[Key(6)]
				public readonly string CtorParam;

				public MyType(string ctorParam) => CtorParam = ctorParam;
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}
}
