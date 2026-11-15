// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = CodeFixVerifier<Nerdbank.MessagePack.Analyzers.UnionDiscriminatorAnalyzer, Nerdbank.MessagePack.Analyzers.CodeFixes.UnionDiscriminatorCodeFix>;

public class UnionDiscriminatorAnalyzerTests
{
	[Test]
	public async Task MatchingConstant_HasNoDiagnostic()
	{
		string source = """
			using Nerdbank.MessagePack;

			public class Create
			{
				[UnionDiscriminator("create")]
				public string Kind => "create";

				public string Uri { get; set; }
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Test]
	public async Task MismatchedConstant_WarnsAndCodeFixes()
	{
		string source = """
			using Nerdbank.MessagePack;

			public class Create
			{
				[UnionDiscriminator("delete")]
				public string {|NBMsgPack130:Kind|} => "create";
			}
			""";

		string fixedSource = """
			using Nerdbank.MessagePack;

			public class Create
			{
				[UnionDiscriminator("create")]
				public string Kind => "create";
			}
			""";

		await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
	}

	[Test]
	public async Task MissingKeyOnKeyedType_IsAnError()
	{
		string source = """
			using Nerdbank.MessagePack;

			public class Create
			{
				[Key(0)]
				public string Uri { get; set; }

				[UnionDiscriminator("create")]
				public string {|NBMsgPack131:Kind|} => "create";
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Test]
	public async Task KeyOtherThanZero_Warns()
	{
		string source = """
			using Nerdbank.MessagePack;

			public class Create
			{
				[Key(0)]
				public string Uri { get; set; }

				[Key(1), UnionDiscriminator("create")]
				public string {|NBMsgPack134:Kind|} => "create";
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Test]
	public async Task DuplicateValues_AreAnError()
	{
		string source = """
			using PolyType;
			using Nerdbank.MessagePack;

			[DerivedTypeShape(typeof(Create))]
			[DerivedTypeShape(typeof(Delete))]
			public partial class {|NBMsgPack133:Change|}
			{
			}

			public partial class Create
			{
				[UnionDiscriminator("create")]
				public string Kind => "create";
			}

			public partial class Delete
			{
				[UnionDiscriminator("create")]
				public string Kind => "create";
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Test]
	public async Task DifferentEnumNames_AreNotDuplicates()
	{
		string source = """
			using PolyType;
			using Nerdbank.MessagePack;

			public enum CreateKind { Create = 0 }
			public enum DeleteKind { Delete = 0 }

			[DerivedTypeShape(typeof(Create))]
			[DerivedTypeShape(typeof(Delete))]
			public partial class Change
			{
			}

			public partial class Create
			{
				[UnionDiscriminator(CreateKind.Create)]
				public CreateKind Kind => CreateKind.Create;
			}

			public partial class Delete
			{
				[UnionDiscriminator(DeleteKind.Delete)]
				public DeleteKind Kind => DeleteKind.Delete;
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Test]
	public async Task SameEnumMemberNameAndNumber_AreDuplicates()
	{
		string source = """
			using PolyType;
			using Nerdbank.MessagePack;

			public enum CreateKind { Create = 0 }
			public enum AlsoCreate { Create = 0 }

			[DerivedTypeShape(typeof(Create))]
			[DerivedTypeShape(typeof(Delete))]
			public partial class {|NBMsgPack133:Change|}
			{
			}

			public partial class Create
			{
				[UnionDiscriminator(CreateKind.Create)]
				public CreateKind Kind => CreateKind.Create;
			}

			public partial class Delete
			{
				[UnionDiscriminator(AlsoCreate.Create)]
				public AlsoCreate Kind => AlsoCreate.Create;
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}
}
