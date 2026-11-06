// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = CodeFixVerifier<Nerdbank.MessagePack.Analyzers.KeyAttributeUseAnalyzer, Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

public class KeyAttributeUseAnalyzerTests
{
	[Test]
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

	[Test]
	public async Task DataContractIgnoresUnannotatedMembers()
	{
		string source = /* lang=c#-test */ """
			using System.Runtime.Serialization;
			using Nerdbank.MessagePack;

			[DataContract]
			public sealed class State
			{
				[DataMember, Key(0)]
				public int Version { get; set; }

				public string EntityConflict { get; internal set; }

				public int IgnoredField;
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Test]
	[Arguments("[DataContract]")]
	[Arguments("[TypeShape]")]
	public async Task DataContractOnBaseType(string derivedAttribute)
	{
		string source = /* lang=c#-test */ $$"""
			using System.Runtime.Serialization;
			using Nerdbank.MessagePack;
			using PolyType;

			{{derivedAttribute}}
			public class Derived : Base
			{
				[DataMember, Key(0)]
				public int Id { get; set; }
			}

			[DataContract]
			public class Base
			{
				public string EntityConflict { get; internal set; }

				[PropertyShape]
				internal UnusedDataPacket UnusedData { get; set; }
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Test]
	public async Task IgnoreDataMemberWithoutDataContract()
	{
		string source = /* lang=c#-test */ """
			using System.Runtime.Serialization;
			using Nerdbank.MessagePack;

			public class State
			{
				[Key(0)]
				public int Version { get; set; }

				[IgnoreDataMember]
				public string EntityConflict { get; set; }

				[IgnoreDataMember]
				public int IgnoredField;
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Test]
	[Arguments("public int Value { get; set; }")]
	[Arguments("private int Value { get; set; }")]
	[Arguments("public int Value;")]
	[Arguments("private int Value;")]
	public async Task DataContractAttributePrecedence(string member)
	{
		string source = /* lang=c#-test */ $$"""
			using System.Runtime.Serialization;
			using Nerdbank.MessagePack;
			using PolyType;

			[DataContract]
			public class DataMemberIncluded
			{
				[DataMember, IgnoreDataMember, Key(0)]
				{{member}}
			}

			[DataContract]
			public class PropertyShapeIncluded
			{
				[PropertyShape, IgnoreDataMember, Key(0)]
				{{member}}
			}

			[DataContract]
			public class PropertyShapeIgnored
			{
				[DataMember, PropertyShape(Ignore = true), {|NBMsgPack002:Key(0)|}]
				{{member}}
			}

			public class PropertyShapeOverridesIgnoreDataMember
			{
				[PropertyShape, IgnoreDataMember, Key(0)]
				{{member}}
			}

			public class DataMemberWithoutDataContract
			{
				[DataMember, IgnoreDataMember, {|NBMsgPack002:Key(0)|}]
				{{member}}
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Test]
	public async Task MissingKeyOnDataMember()
	{
		string source = /* lang=c#-test */ """
			using System.Runtime.Serialization;
			using Nerdbank.MessagePack;

			[DataContract]
			public class State
			{
				[DataMember, Key(0)]
				public int Version { get; set; }

				[DataMember]
				private int {|NBMsgPack001:Property|} { get; set; }

				[DataMember]
				private int {|NBMsgPack001:Field|};

				[DataMember, {|NBMsgPack002:Key(1)|}]
				private static int Cache { get; set; }

				[{|NBMsgPack002:Key(1)|}]
				public int Ignored { get; set; }
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Test]
	public async Task DataContractDoesNotApplyToBaseMembers()
	{
		string source = /* lang=c#-test */ """
			using System.Runtime.Serialization;
			using Nerdbank.MessagePack;

			[DataContract]
			public class Derived : Base
			{
				[DataMember, Key(0)]
				public int Id { get; set; }
			}

			public class Base
			{
				public string {|NBMsgPack001:EntityConflict|} { get; set; }
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Test]
	public async Task DataContractStatusChangesAcrossHierarchy()
	{
		string source = /* lang=c#-test */ """
			using System.Runtime.Serialization;
			using Nerdbank.MessagePack;

			[DataContract]
			public class Derived : Middle
			{
				[DataMember, Key(0)]
				public int Id { get; set; }

				public int Ignored { get; set; }
			}

			public class Middle : Base
			{
				[Key(1)]
				public int Version { get; set; }

				[IgnoreDataMember]
				public int IgnoredField;
			}

			[DataContract]
			public class Base
			{
				[DataMember, Key(2)]
				public int BaseId { get; set; }

				public int IgnoredBaseField;
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Test]
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

	[Test]
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

	[Test]
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

	[Test]
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

	[Test]
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

	[Test]
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

	[Test]
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

	[Test]
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

	[Test]
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

	[Test]
	public async Task KeyOnPropertyWithOnlyGetterButAlsoHasCtorParam()
	{
		string source = /* lang=c#-test */ """
			using PolyType;
			using Nerdbank.MessagePack;
			
			[GenerateShape]
			internal partial struct AddArgs
			{
				public AddArgs(int a)
				{
					this.a = a;
				}

				[Key(0)]
				public int a { get; }
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Test]
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

	[Test]
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

	[Test]
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
