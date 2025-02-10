// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.PolySerializer.MessagePack;

internal class MsgPackFormatter : Formatter
{
	internal static readonly MsgPackFormatter Instance = new();

	private MsgPackFormatter() { }

	protected internal override void GetEncodedStringBytes(string value, out ReadOnlyMemory<byte> utf8Bytes, out ReadOnlyMemory<byte> msgpackEncoded)
		=> StringEncoding.GetEncodedStringBytes(value, out utf8Bytes, out msgpackEncoded);
}
