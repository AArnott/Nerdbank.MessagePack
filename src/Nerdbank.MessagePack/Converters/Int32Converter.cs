// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack.Converters;

internal class Int32Converter : MessagePackConverter<int>
{
	public override int Deserialize(ref MessagePackReader reader) => reader.ReadInt32();

	public override void Serialize(ref MessagePackWriter writer, ref int value) => writer.Write(value);
}
