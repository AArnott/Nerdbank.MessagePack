// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

[GenerateShape, MsgPackCSharp.MessagePackObject(keyAsPropertyName: true)]
public partial class PocoClass
{
	public int SomeInt { get; init; }

	public string? SomeString { get; init; }
}

[GenerateShape, MsgPackCSharp.MessagePackObject(keyAsPropertyName: true)]
public partial class ArrayOfPocos
{
	public PocoClass[]? Items { get; init; }
}
