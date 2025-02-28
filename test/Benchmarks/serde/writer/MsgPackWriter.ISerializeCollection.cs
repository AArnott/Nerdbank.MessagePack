// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// BSD 3-Clause License
// Copyright(c) 2024, serde.msgpack
namespace Serde.MsgPack;

partial class MsgPackWriter : ISerializeCollection
{
	void ISerializeCollection.End(ISerdeInfo typeInfo)
	{
		// No action needed, all collections are length-prefixed
	}

	void ISerializeCollection.SerializeElement<T, U>(T value, U serialize)
	{
		serialize.Serialize(value, this);
	}
}
