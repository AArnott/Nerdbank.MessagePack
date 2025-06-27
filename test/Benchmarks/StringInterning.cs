// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

[MemoryDiagnoser]
public partial class StringInterning
{
	private static readonly MessagePackSerializer NonInterning = new() { InternStrings = false };
	private static readonly MessagePackSerializer Interning = new() { InternStrings = true };
	private static readonly byte[] StringArrayMsgPack = NonInterning.Serialize<string[], Witness>(["Hello, World!", "Hello, World!"]);

	[Benchmark(Baseline = true)]
	public void NonInterning_StringArray() => NonInterning.Deserialize<string[], Witness>(StringArrayMsgPack);

	[Benchmark]
	public void Interning_StringArray() => Interning.Deserialize<string[], Witness>(StringArrayMsgPack);

	[GenerateShapeFor<string[]>]
	private partial class Witness;
}
