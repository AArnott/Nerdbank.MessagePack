// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

[GenerateShape, MsgPackCSharp.MessagePackObject(keyAsPropertyName: true)]
public partial class PocoMap
{
	public int SomeInt { get; set; }

	public string? SomeString { get; set; }
}

[GenerateShape, MsgPackCSharp.MessagePackObject(keyAsPropertyName: true)]
public partial class PocoMapInit
{
	public int SomeInt { get; init; }

	public string? SomeString { get; init; }
}

[GenerateShape, MsgPackCSharp.MessagePackObject]
public partial class PocoAsArray
{
	[Key(0), MsgPackCSharp.Key(0)]
	public int SomeInt { get; set; }

	[Key(1), MsgPackCSharp.Key(1)]
	public string? SomeString { get; set; }
}

[GenerateShape, MsgPackCSharp.MessagePackObject]
public partial class PocoAsArrayInit
{
	[Key(0), MsgPackCSharp.Key(0)]
	public int SomeInt { get; init; }

	[Key(1), MsgPackCSharp.Key(1)]
	public string? SomeString { get; init; }
}
