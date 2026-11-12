// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = CodeFixVerifier<Nerdbank.MessagePack.Analyzers.SerializationCallbacksAnalyzer, Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

public class SerializationCallbacksAnalyzerTests
{
	[Test]
	public async Task ReferenceTypes_NoDiagnostic()
	{
		string source = /* lang=c#-test */ """
			using Nerdbank.MessagePack;

			class CallbackClass : IMessagePackSerializationCallbacks
			{
				public void OnBeforeSerialize() { }
				public void OnAfterSerialize() { }
				public void OnBeforeDeserialize() { }
				public void OnAfterDeserialize() { }
			}

			record CallbackRecord : IMessagePackSerializationCallbacks
			{
				public void OnBeforeSerialize() { }
				public void OnAfterSerialize() { }
				public void OnBeforeDeserialize() { }
				public void OnAfterDeserialize() { }
			}

			struct PlainStruct
			{
				public int Value => 0;
			}

			record struct PlainRecordStruct
			{
				public int Value { get; set; }
			}

			struct EquatableStruct : System.IEquatable<EquatableStruct>
			{
				public bool Equals(EquatableStruct other) => true;
			}

			interface ICallbacks : IMessagePackSerializationCallbacks
			{
			}

			class IndirectClass : ICallbacks
			{
				public void OnBeforeSerialize() { }
				public void OnAfterSerialize() { }
				public void OnBeforeDeserialize() { }
				public void OnAfterDeserialize() { }
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Test]
	public async Task Struct_Reports()
	{
		string source = /* lang=c#-test */ """
			using Nerdbank.MessagePack;
			using Callbacks = Nerdbank.MessagePack.IMessagePackSerializationCallbacks;

			struct Counter : {|NBMsgPack120:IMessagePackSerializationCallbacks|}
			{
				public void OnBeforeSerialize() { }
				public void OnAfterSerialize() { }
				public void OnBeforeDeserialize() { }
				public void OnAfterDeserialize() { }
			}

			readonly struct ReadOnlyCounter : {|NBMsgPack120:IMessagePackSerializationCallbacks|}
			{
				public void OnBeforeSerialize() { }
				public void OnAfterSerialize() { }
				public void OnBeforeDeserialize() { }
				public void OnAfterDeserialize() { }
			}

			ref struct RefCounter : {|NBMsgPack120:IMessagePackSerializationCallbacks|}
			{
				public void OnBeforeSerialize() { }
				public void OnAfterSerialize() { }
				public void OnBeforeDeserialize() { }
				public void OnAfterDeserialize() { }
			}

			struct GenericCounter<T> : {|NBMsgPack120:IMessagePackSerializationCallbacks|}
			{
				public void OnBeforeSerialize() { }
				public void OnAfterSerialize() { }
				public void OnBeforeDeserialize() { }
				public void OnAfterDeserialize() { }
			}

			struct ExplicitCounter : {|NBMsgPack120:IMessagePackSerializationCallbacks|}
			{
				void IMessagePackSerializationCallbacks.OnBeforeSerialize() { }
				void IMessagePackSerializationCallbacks.OnAfterSerialize() { }
				void IMessagePackSerializationCallbacks.OnBeforeDeserialize() { }
				void IMessagePackSerializationCallbacks.OnAfterDeserialize() { }
			}

			struct AliasCounter : {|NBMsgPack120:Callbacks|}
			{
				public void OnBeforeSerialize() { }
				public void OnAfterSerialize() { }
				public void OnBeforeDeserialize() { }
				public void OnAfterDeserialize() { }
			}

			struct QualifiedCounter : {|NBMsgPack120:global::Nerdbank.MessagePack.IMessagePackSerializationCallbacks|}
			{
				public void OnBeforeSerialize() { }
				public void OnAfterSerialize() { }
				public void OnBeforeDeserialize() { }
				public void OnAfterDeserialize() { }
			}

			class Outer
			{
				struct NestedCounter : {|NBMsgPack120:IMessagePackSerializationCallbacks|}
				{
					public void OnBeforeSerialize() { }
					public void OnAfterSerialize() { }
					public void OnBeforeDeserialize() { }
					public void OnAfterDeserialize() { }
				}
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Test]
	public async Task RecordStruct_Reports()
	{
		string source = /* lang=c#-test */ """
			using Nerdbank.MessagePack;

			record struct Counter : {|NBMsgPack120:IMessagePackSerializationCallbacks|}
			{
				public void OnBeforeSerialize() { }
				public void OnAfterSerialize() { }
				public void OnBeforeDeserialize() { }
				public void OnAfterDeserialize() { }
			}

			readonly record struct ReadOnlyCounter : {|NBMsgPack120:IMessagePackSerializationCallbacks|}
			{
				public void OnBeforeSerialize() { }
				public void OnAfterSerialize() { }
				public void OnBeforeDeserialize() { }
				public void OnAfterDeserialize() { }
			}

			record struct PositionalCounter(int Value) : {|NBMsgPack120:IMessagePackSerializationCallbacks|}
			{
				public void OnBeforeSerialize() => _ = this.Value;
				public void OnAfterSerialize() { }
				public void OnBeforeDeserialize() { }
				public void OnAfterDeserialize() { }
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Test]
	public async Task IndirectInterface_Reports()
	{
		string source = /* lang=c#-test */ """
			using Nerdbank.MessagePack;

			interface IMid : IMessagePackSerializationCallbacks
			{
			}

			interface IOuter : IMid
			{
			}

			struct Counter : {|NBMsgPack120:IOuter|}
			{
				public void OnBeforeSerialize() { }
				public void OnAfterSerialize() { }
				public void OnBeforeDeserialize() { }
				public void OnAfterDeserialize() { }
			}

			struct Both : {|NBMsgPack120:IMid|}, {|NBMsgPack120:IMessagePackSerializationCallbacks|}
			{
				public void OnBeforeSerialize() { }
				public void OnAfterSerialize() { }
				public void OnBeforeDeserialize() { }
				public void OnAfterDeserialize() { }
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}

	[Test]
	public async Task PartialStruct_ReportsOnImplementingDeclaration()
	{
		string source = /* lang=c#-test */ """
			using Nerdbank.MessagePack;

			partial struct Counter : {|NBMsgPack120:IMessagePackSerializationCallbacks|}
			{
				public void OnBeforeSerialize() { }
				public void OnAfterSerialize() { }
			}

			partial struct Counter
			{
				public void OnBeforeDeserialize() { }
				public void OnAfterDeserialize() { }
			}
			""";

		await VerifyCS.VerifyAnalyzerAsync(source);
	}
}
