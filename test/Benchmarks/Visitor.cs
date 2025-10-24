// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

[MemoryDiagnoser]
public partial class Visitor
{
	[Benchmark]
	public void Visit_LargeDataModel()
	{
		MessagePackSerializer serializer = new();
		_ = serializer.GetConverter(PolyType.SourceGenerator.TypeShapeProvider_Benchmarks.Default.LargeDataModel).ValueOrThrow;
	}
}
