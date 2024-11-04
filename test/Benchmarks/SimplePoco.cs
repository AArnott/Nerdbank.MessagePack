// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using static Data;

public partial class SimplePoco
{
	private readonly MessagePackSerializer serializer = new();
	private readonly Sequence<byte> buffer = new();

	[Benchmark]
	public void Serialize()
	{
		this.serializer.Serialize(this.buffer, Poco);
		this.buffer.Reset();
	}

	[Benchmark]
	public void Serialize_MsgPackCSharp()
	{
		MsgPackCSharp.MessagePackSerializer.Serialize(this.buffer, Poco, MsgPackCSharp.MessagePackSerializerOptions.Standard);
		this.buffer.Reset();
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
}
