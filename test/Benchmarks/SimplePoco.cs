// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Benchmarks;

public partial class SimplePoco
{
	private static readonly PocoClass Poco = new() { SomeInt = 42, SomeString = "Hello, World!" };
	private static readonly ReadOnlySequence<byte> PocoMsgpack = new(MsgPackCSharp.MessagePackSerializer.Serialize(Poco, MsgPackCSharp.MessagePackSerializerOptions.Standard));
	private readonly MessagePackSerializer serializer = new();
	private readonly Sequence<byte> buffer = new();

	[Benchmark]
	public void Serialize()
	{
		this.serializer.Serialize(this.buffer, Poco);
	}

	[Benchmark]
	public void Serialize_MsgPackCSharp()
	{
		MsgPackCSharp.MessagePackSerializer.Serialize(this.buffer, Poco, MsgPackCSharp.MessagePackSerializerOptions.Standard);
	}

	[Benchmark]
	public void Deserialize()
	{
		this.serializer.Deserialize<PocoClass>(PocoMsgpack);
	}

	[Benchmark]
	public void Deserialize_MsgPackCSharp()
	{
		MsgPackCSharp.MessagePackSerializer.Deserialize<PocoClass>(PocoMsgpack, MsgPackCSharp.MessagePackSerializerOptions.Standard);
	}

	[IterationCleanup]
	public void Cleanup() => this.buffer.Reset();

	[GenerateShape, MsgPackCSharp.MessagePackObject(keyAsPropertyName: true)]
	public partial class PocoClass
	{
		public int SomeInt { get; set; }

		public string? SomeString { get; set; }
	}
}
