// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = CodeFixVerifier<Nerdbank.MessagePack.Analyzers.UnusedDataPacketAnalyzer, Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

public class UnusedDataPacketAnalyzerTests
{
	[Fact]
	public async Task NoDiagnostics()
	{
		string source = /* lang=c#-test */ """
			using PolyType;
			using Nerdbank.MessagePack;

			public class MyTypeWithProperty
			{
				[PropertyShape]
				private UnusedDataPacket Extension { get; set; }
			}

			public class MyTypeWithField
			{
				[PropertyShape]
				private UnusedDataPacket extension;
			}
			""";
		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task ShouldHavePropertyShape()
	{
		string source = /* lang=c#-test */ """
			using PolyType;
			using Nerdbank.MessagePack;

			public class MyTypeWithProperty
			{
				private UnusedDataPacket {|NBMsgPack060:Extension|} { get; set; }
			}

			public class MyTypeWithPropertyIgnored
			{
				[PropertyShape(Ignore = true)]
				private UnusedDataPacket {|NBMsgPack060:Extension|} { get; set; }
			}

			public class MyTypeWithField
			{
				private UnusedDataPacket {|NBMsgPack060:Extension|};
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task ShouldBePrivate()
	{
		string source = /* lang=c#-test */ """
			using PolyType;
			using Nerdbank.MessagePack;

			public class MyTypeWithProperty
			{
				public UnusedDataPacket {|NBMsgPack062:Extension|} { get; set; }
			}

			public class MyTypeWithField
			{
				public UnusedDataPacket {|NBMsgPack062:Extension|};
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Fact]
	public async Task ShouldNotHaveKeyAttribute()
	{
		string source = /* lang=c#-test */ """
			using PolyType;
			using Nerdbank.MessagePack;

			public class MyTypeWithProperty
			{
				[{|NBMsgPack061:Key(0)|}, PropertyShape]
				private UnusedDataPacket Extension { get; set; }
			}

			public class MyTypeWithField
			{
				[{|NBMsgPack061:Key(0)|}, PropertyShape]
				private UnusedDataPacket Extension;
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}
}
