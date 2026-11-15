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
	public async Task TargetTypedDefault_CodeFixUsesTheConstantValue()
	{
		string source = """
			using Nerdbank.MessagePack;

			public class Create
			{
				[UnionDiscriminator(true)]
				public bool {|NBMsgPack130:Kind|} => default;
			}
			""";

		string fixedSource = """
			using Nerdbank.MessagePack;

			public class Create
			{
				[UnionDiscriminator(false)]
				public bool Kind => default;
			}
			""";

		await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
	}

	[Test]
	public async Task NullConstant_HasNoCodeFix()
	{
		string source = """
			#nullable enable
			using Nerdbank.MessagePack;

			public class Create
			{
				[UnionDiscriminator("create")]
				public string? {|NBMsgPack130:Kind|} => null;
			}
			""";

		await VerifyCS.VerifyNoCodeFixAsync(source, VerifyCS.Diagnostic("NBMsgPack130").WithSpan(7, 17, 7, 21).WithArguments("Kind"));
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

	[Test]
	public async Task RenamedEnumMembers_AreNotDuplicates()
	{
		string source = """
			using PolyType;
			using Nerdbank.MessagePack;

			public enum CreateKind
			{
				[EnumMemberShape(Name = "create")]
				Create = 0,
			}

			public enum AlsoCreate
			{
				[EnumMemberShape(Name = "delete")]
				Create = 0,
			}

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
				[UnionDiscriminator(AlsoCreate.Create)]
				public AlsoCreate Kind => AlsoCreate.Create;
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Test]
	public async Task CustomConverter_IsNotADuplicate()
	{
		string source = """
			using PolyType;
			using Nerdbank.MessagePack;

			[DerivedTypeShape(typeof(Plain))]
			[DerivedTypeShape(typeof(Encoded))]
			public partial class Change
			{
			}

			public partial class Plain
			{
				[UnionDiscriminator(1)]
				public int Code => 1;
			}

			public partial class Encoded
			{
				[MessagePackConverter(typeof(XorIntConverter))]
				[UnionDiscriminator(1)]
				public int Code => 1;
			}

			public class XorIntConverter : MessagePackConverter<int>
			{
				public override int Read(ref MessagePackReader reader, SerializationContext context) => reader.ReadInt32();

				public override void Write(ref MessagePackWriter writer, in int value, SerializationContext context) => writer.Write(value);
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Test]
	public async Task IgnoredKey_DoesNotMakeTheTypeKeyed()
	{
		string source = """
			using PolyType;
			using Nerdbank.MessagePack;

			public class Create
			{
				[Key(0), PropertyShape(Ignore = true)]
				public int Hidden { get; set; }

				[UnionDiscriminator("create")]
				public string Kind => "create";
			}

			[UnionDiscriminator("Kind", "create")]
			public class Named
			{
				[Key(0), PropertyShape(Ignore = true)]
				public int Hidden { get; set; }
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}
}
