﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

internal static class Data
{
	internal static class PocoMapInit
	{
		internal static readonly global::PocoMapInit Single = new() { SomeInt = 42, SomeString = "Hello, World!" };
		internal static readonly ReadOnlySequence<byte> SingleMsgpack = new(MsgPackCSharp.MessagePackSerializer.Serialize(Single, MsgPackCSharp.MessagePackSerializerOptions.Standard));

		internal static readonly global::PocoMapInit[] Array = Enumerable.Range(0, 1000).Select(n => new global::PocoMapInit { SomeInt = n, SomeString = "Hello" }).ToArray();
		internal static readonly ReadOnlySequence<byte> ArrayMsgpack = new(MsgPackCSharp.MessagePackSerializer.Serialize(Array, MsgPackCSharp.MessagePackSerializerOptions.Standard));
	}

	internal static class PocoMap
	{
		internal static readonly global::PocoMap Single = new() { SomeInt = 42, SomeString = "Hello, World!" };
		internal static readonly ReadOnlySequence<byte> SingleMsgpack = new(MsgPackCSharp.MessagePackSerializer.Serialize(Single, MsgPackCSharp.MessagePackSerializerOptions.Standard));

		internal static readonly global::PocoMap[] Array = Enumerable.Range(0, 1000).Select(n => new global::PocoMap { SomeInt = n, SomeString = "Hello" }).ToArray();
		internal static readonly ReadOnlySequence<byte> ArrayMsgpack = new(MsgPackCSharp.MessagePackSerializer.Serialize(Array, MsgPackCSharp.MessagePackSerializerOptions.Standard));
	}

	internal static class PocoAsArrayInit
	{
		internal static readonly global::PocoAsArrayInit Single = new() { SomeInt = 42, SomeString = "Hello, World!" };
		internal static readonly ReadOnlySequence<byte> SingleMsgpack = new(MsgPackCSharp.MessagePackSerializer.Serialize(Single, MsgPackCSharp.MessagePackSerializerOptions.Standard));

		internal static readonly global::PocoAsArrayInit[] Array = Enumerable.Range(0, 1000).Select(n => new global::PocoAsArrayInit { SomeInt = n, SomeString = "Hello" }).ToArray();
		internal static readonly ReadOnlySequence<byte> ArrayMsgpack = new(MsgPackCSharp.MessagePackSerializer.Serialize(Array, MsgPackCSharp.MessagePackSerializerOptions.Standard));
	}

	internal static class PocoAsArray
	{
		internal static readonly global::PocoAsArray Single = new() { SomeInt = 42, SomeString = "Hello, World!" };
		internal static readonly ReadOnlySequence<byte> SingleMsgpack = new(MsgPackCSharp.MessagePackSerializer.Serialize(Single, MsgPackCSharp.MessagePackSerializerOptions.Standard));

		internal static readonly global::PocoAsArray[] Array = Enumerable.Range(0, 1000).Select(n => new global::PocoAsArray { SomeInt = n, SomeString = "Hello" }).ToArray();
		internal static readonly ReadOnlySequence<byte> ArrayMsgpack = new(MsgPackCSharp.MessagePackSerializer.Serialize(Array, MsgPackCSharp.MessagePackSerializerOptions.Standard));
	}
}
