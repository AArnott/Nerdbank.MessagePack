// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

internal static class Data
{
	internal static readonly PocoClass Poco = new() { SomeInt = 42, SomeString = "Hello, World!" };
	internal static readonly ReadOnlySequence<byte> PocoMsgpack = new(MsgPackCSharp.MessagePackSerializer.Serialize(Poco, MsgPackCSharp.MessagePackSerializerOptions.Standard));

	internal static readonly ArrayOfPocos PocoArray = new() { Items = Enumerable.Range(0, 1000).Select(n => new PocoClass { SomeInt = n, SomeString = "Hello" }).ToArray() };
	internal static readonly ReadOnlySequence<byte> PocoArrayMsgpack = new(MsgPackCSharp.MessagePackSerializer.Serialize(PocoArray, MsgPackCSharp.MessagePackSerializerOptions.Standard));
}
