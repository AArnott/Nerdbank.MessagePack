// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// BSD 3-Clause License
// Copyright(c) 2024, serde.msgpack
namespace Serde.MsgPack;

partial class MsgPackReader<TReader>
{
	private struct DeserializeCollection(MsgPackReader<TReader> deserializer, bool isDict, int length) : IDeserializeCollection
	{
		private int index;

		int? IDeserializeCollection.SizeOpt => isDict switch
		{
			true => length / 2,
			false => length,
		};

		bool IDeserializeCollection.TryReadValue<T, D>(ISerdeInfo typeInfo, D d, out T next)
		{
			if (this.index >= length)
			{
				next = default!;
				return false;
			}

			next = d.Deserialize(deserializer);
			this.index++;
			return true;
		}
	}
}
