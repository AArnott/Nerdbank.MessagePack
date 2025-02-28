// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// BSD 3-Clause License
// Copyright(c) 2024, serde.msgpack
using Serde;
using Serde.IO;

namespace Serde.MsgPack;

public static class MsgPackSerializer
{
	public static byte[] Serialize<T>(T value)
		where T : ISerializeProvider<T>
	{
		using var buffer = new ScratchBuffer();
		var writer = new MsgPackWriter(buffer);
		ISerialize<T> serializeObject = T.SerializeInstance;
		serializeObject.Serialize(value, writer);
		return buffer.Span.ToArray();
	}

	public static byte[] Serialize<T, U>(T value, U proxy)
		where U : ISerialize<T>
	{
		using var buffer = new ScratchBuffer();
		var writer = new MsgPackWriter(buffer);
		proxy.Serialize(value, writer);
		return buffer.Span.ToArray();
	}

	public static T Deserialize<T>(byte[] bytes)
		where T : IDeserializeProvider<T>
	{
		var byteBuffer = new ArrayBufReader(bytes);
		using var reader = new MsgPackReader<ArrayBufReader>(byteBuffer);
		return T.DeserializeInstance.Deserialize(reader);
	}

	public static T Deserialize<T, U>(byte[] bytes, U proxy)
		where U : IDeserialize<T>
	{
		var byteBuffer = new ArrayBufReader(bytes);
		using var reader = new MsgPackReader<ArrayBufReader>(byteBuffer);
		return proxy.Deserialize(reader);
	}
}
