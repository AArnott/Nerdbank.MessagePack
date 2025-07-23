// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = CodeFixVerifier<Nerdbank.MessagePack.Analyzers.UseComparerAttributeAnalyzer, Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

public class UseComparerAttributeAnalyzerTests
{
	[Fact]
	public async Task NoIssues_ValidComparer()
	{
		string source = /* lang=c#-test */ """
			using System;
			using System.Collections.Generic;
			using PolyType;
			using Nerdbank.MessagePack;

			[GenerateShape]
			public partial class MyType
			{
				[UseComparer(typeof(MyComparer))]
				public Dictionary<string, int> MyDictionary { get; set; } = new(StringComparer.OrdinalIgnoreCase);
			}

			public class MyComparer : IEqualityComparer<string>
			{
				public bool Equals(string x, string y) => EqualityComparer<string>.Default.Equals(x, y);
				public int GetHashCode(string obj) => EqualityComparer<string>.Default.GetHashCode(obj);
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task NoIssues_ValidComparerWithMember()
	{
		string source = /* lang=c#-test */ """
			using System;
			using System.Collections.Generic;
			using PolyType;
			using Nerdbank.MessagePack;

			[GenerateShape]
			public partial class MyType
			{
				[UseComparer(typeof(StringComparer), nameof(StringComparer.OrdinalIgnoreCase))]
				public Dictionary<string, int> MyDictionary { get; set; } = new(StringComparer.OrdinalIgnoreCase);
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task NoIssues_ValidHashSetComparer()
	{
		string source = /* lang=c#-test */ """
			using System;
			using System.Collections.Generic;
			using PolyType;
			using Nerdbank.MessagePack;

			[GenerateShape]
			public partial class MyType
			{
				[UseComparer(typeof(StringComparer), nameof(StringComparer.OrdinalIgnoreCase))]
				public HashSet<string> MyHashSet { get; set; } = new(StringComparer.OrdinalIgnoreCase);
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task OpenGenericType_ReportsError()
	{
		string source = /* lang=c#-test */ """
			using System.Collections.Generic;
			using PolyType;
			using Nerdbank.MessagePack;

			public class MyComparer<T> : IEqualityComparer<T>
			{
				public bool Equals(T x, T y) => EqualityComparer<T>.Default.Equals(x, y);
				public int GetHashCode(T obj) => EqualityComparer<T>.Default.GetHashCode(obj);
			}

			[GenerateShape]
			public partial class MyType
			{
				[UseComparer({|NBMsgPack070:typeof(MyComparer<>)|})]
				public HashSet<string> MyHashSet { get; set; } = new();
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task InvalidMemberName_ReportsError()
	{
		string source = /* lang=c#-test */ """
			using System;
			using System.Collections.Generic;
			using PolyType;
			using Nerdbank.MessagePack;

			[GenerateShape]
			public partial class MyType
			{
				[UseComparer(typeof(StringComparer), {|NBMsgPack071:"NonExistentMember"|})]
				public Dictionary<string, int> MyDictionary { get; set; } = new();
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task PrivateMemberName_ReportsError()
	{
		string source = /* lang=c#-test */ """
			using System;
			using System.Collections.Generic;
			using PolyType;
			using Nerdbank.MessagePack;

			public class MyComparerProvider
			{
				private static IEqualityComparer<string> PrivateComparer => StringComparer.OrdinalIgnoreCase;
			}

			[GenerateShape]
			public partial class MyType
			{
				[UseComparer(typeof(MyComparerProvider), {|NBMsgPack071:"PrivateComparer"|})]
				public HashSet<string> MyHashSet { get; set; } = new();
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task IncompatibleComparerType_ReportsError()
	{
		string source = /* lang=c#-test */ """
			using System.Collections.Generic;
			using PolyType;
			using Nerdbank.MessagePack;

			public class NotAComparer
			{
			}

			[GenerateShape]
			public partial class MyType
			{
				[{|NBMsgPack072:UseComparer(typeof(NotAComparer))|}]
				public HashSet<string> MyHashSet { get; set; } = new();
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task WrongComparerElementType_ReportsError()
	{
		string source = /* lang=c#-test */ """
			using System.Collections.Generic;
			using PolyType;
			using Nerdbank.MessagePack;

			public class IntComparer : IEqualityComparer<int>
			{
				public bool Equals(int x, int y) => x == y;
				public int GetHashCode(int obj) => obj.GetHashCode();
			}

			[GenerateShape]
			public partial class MyType
			{
				[{|NBMsgPack072:UseComparer(typeof(IntComparer))|}]
				public HashSet<string> MyHashSet { get; set; } = new();
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task ValidCustomComparer()
	{
		string source = /* lang=c#-test */ """
			using System.Collections.Generic;
			using PolyType;
			using Nerdbank.MessagePack;

			public class MyStringComparer : IEqualityComparer<string>
			{
				public bool Equals(string x, string y) => string.Equals(x, y, System.StringComparison.OrdinalIgnoreCase);
				public int GetHashCode(string obj) => obj?.ToUpperInvariant().GetHashCode() ?? 0;
			}

			[GenerateShape]
			public partial class MyType
			{
				[UseComparer(typeof(MyStringComparer))]
				public HashSet<string> MyHashSet { get; set; } = new(new MyStringComparer());
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task ValidOnParameter()
	{
		string source = /* lang=c#-test */ """
			using System;
			using System.Collections.Generic;
			using PolyType;
			using Nerdbank.MessagePack;

			[GenerateShape]
			public partial class MyType
			{
				public MyType([UseComparer(typeof(StringComparer), nameof(StringComparer.OrdinalIgnoreCase))] Dictionary<string, int> dict)
				{
					Dict = dict;
				}

				public Dictionary<string, int> Dict { get; }
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task ValidOnField()
	{
		string source = /* lang=c#-test */ """
			using System;
			using System.Collections.Generic;
			using PolyType;
			using Nerdbank.MessagePack;

			[GenerateShape]
			public partial class MyType
			{
				[UseComparer(typeof(StringComparer), nameof(StringComparer.OrdinalIgnoreCase))]
				public Dictionary<string, int> MyField = new(StringComparer.OrdinalIgnoreCase);
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task AbstractType_ReportsError()
	{
		string source = /* lang=c#-test */ """
			using System.Collections.Generic;
			using PolyType;
			using Nerdbank.MessagePack;

			public abstract class AbstractComparer : IEqualityComparer<string>
			{
				public abstract bool Equals(string x, string y);
				public abstract int GetHashCode(string obj);
			}

			[GenerateShape]
			public partial class MyType
			{
				[UseComparer({|NBMsgPack073:typeof(AbstractComparer)|})]
				public HashSet<string> MyHashSet { get; set; } = new();
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task AbstractTypeWithStaticMember_NoError()
	{
		string source = /* lang=c#-test */ """
			using System;
			using System.Collections.Generic;
			using PolyType;
			using Nerdbank.MessagePack;

			public abstract class AbstractComparerProvider
			{
				public static IEqualityComparer<string> Default => StringComparer.OrdinalIgnoreCase;
			}

			[GenerateShape]
			public partial class MyType
			{
				[UseComparer(typeof(AbstractComparerProvider), nameof(AbstractComparerProvider.Default))]
				public HashSet<string> MyHashSet { get; set; } = new(AbstractComparerProvider.Default);
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task AbstractTypeWithInstanceMember_ReportsError()
	{
		string source = /* lang=c#-test */ """
			using System;
			using System.Collections.Generic;
			using PolyType;
			using Nerdbank.MessagePack;

			public abstract class AbstractComparerProvider
			{
				public IEqualityComparer<string> InstanceComparer => StringComparer.OrdinalIgnoreCase;
			}

			[GenerateShape]
			public partial class MyType
			{
				[UseComparer(typeof(AbstractComparerProvider), {|NBMsgPack071:"InstanceComparer"|})]
				public HashSet<string> MyHashSet { get; set; } = new();
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}
}
