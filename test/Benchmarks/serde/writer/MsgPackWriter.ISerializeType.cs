// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// BSD 3-Clause License
// Copyright(c) 2024, serde.msgpack
using Serde;

namespace Serde.MsgPack;

partial class MsgPackWriter : ISerializeType
{
	void ISerializeType.End()
	{
		// No action needed, all collections are length-prefixed
	}

	void ISerializeType.SerializeField<T, U>(ISerdeInfo typeInfo, int index, T value, U serialize)
	{
		serialize.Serialize(value, this);
	}
}
