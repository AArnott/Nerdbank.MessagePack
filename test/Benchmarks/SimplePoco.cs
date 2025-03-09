﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Benchmarks;
using STJ = System.Text.Json;

[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkDotNet.Configs.BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public partial class SimplePoco
{
	private readonly MessagePackSerializer serializer = new() { SerializeDefaultValues = SerializeDefaultValuesPolicy.Always };
	private readonly Sequence buffer = new();

	[Benchmark]
	[BenchmarkCategory("map-init", "Serialize")]
	public void SerializeMapInit()
	{
		this.serializer.Serialize(this.buffer, Data.PocoMapInit.Single);
		this.buffer.Reset();
	}

	[Benchmark(Baseline = true)]
	[BenchmarkCategory("map-init", "Serialize")]
	public void SerializeMapInit_MsgPackCSharp()
	{
		MsgPackCSharp.MessagePackSerializer.Serialize(this.buffer, Data.PocoMapInit.Single, MsgPackCSharp.MessagePackSerializerOptions.Standard);
		this.buffer.Reset();
	}

	[Benchmark]
	[BenchmarkCategory("map-init", "Serialize")]
	public void SerializeMapInit_STJ() => this.SerializeJson(Data.PocoMapInit.Single, STJSourceGenerationContext.Default.PocoMapInit);

	[Benchmark]
	[BenchmarkCategory("map-init", "Deserialize")]
	public void DeserializeMapInit()
	{
		this.serializer.Deserialize<PocoMapInit>(Data.PocoMapInit.SingleMsgpack);
	}

	[Benchmark(Baseline = true)]
	[BenchmarkCategory("map-init", "Deserialize")]
	public void DeserializeMapInit_MsgPackCSharp()
	{
		MsgPackCSharp.MessagePackSerializer.Deserialize<PocoMapInit>(Data.PocoMapInit.SingleMsgpack, MsgPackCSharp.MessagePackSerializerOptions.Standard);
	}

	[Benchmark]
	[BenchmarkCategory("map-init", "Deserialize")]
	public void DeserializeMapInit_STJ() => this.DeserializeJson(Data.PocoMapInit.SingleJson, STJSourceGenerationContext.Default.PocoMapInit);

	[Benchmark]
	[BenchmarkCategory("map", "Serialize")]
	public void SerializeMap()
	{
		this.serializer.Serialize(this.buffer, Data.PocoMap.Single);
		this.buffer.Reset();
	}

	[Benchmark(Baseline = true)]
	[BenchmarkCategory("map", "Serialize")]
	public void SerializeMap_MsgPackCSharp()
	{
		MsgPackCSharp.MessagePackSerializer.Serialize(this.buffer, Data.PocoMap.Single, MsgPackCSharp.MessagePackSerializerOptions.Standard);
		this.buffer.Reset();
	}

	[Benchmark]
	[BenchmarkCategory("map", "Serialize")]
	public void SerializeMap_STJ() => this.SerializeJson(Data.PocoMap.Single, STJSourceGenerationContext.Default.PocoMap);

	[Benchmark]
	[BenchmarkCategory("map", "Deserialize")]
	public void DeserializeMap()
	{
		this.serializer.Deserialize<PocoMap>(Data.PocoMap.SingleMsgpack);
	}

	[Benchmark(Baseline = true)]
	[BenchmarkCategory("map", "Deserialize")]
	public void DeserializeMap_MsgPackCSharp()
	{
		MsgPackCSharp.MessagePackSerializer.Deserialize<PocoMap>(Data.PocoMap.SingleMsgpack, MsgPackCSharp.MessagePackSerializerOptions.Standard);
	}

	[Benchmark]
	[BenchmarkCategory("map", "Deserialize")]
	public void DeserializeMap_STJ() => this.DeserializeJson(Data.PocoMap.SingleJson, STJSourceGenerationContext.Default.PocoMap);

	[Benchmark]
	[BenchmarkCategory("array", "Serialize")]
	public void SerializeAsArray()
	{
		this.serializer.Serialize(this.buffer, Data.PocoAsArray.Single);
		this.buffer.Reset();
	}

	[Benchmark(Baseline = true)]
	[BenchmarkCategory("array", "Serialize")]
	public void SerializeAsArray_MsgPackCSharp()
	{
		MsgPackCSharp.MessagePackSerializer.Serialize(this.buffer, Data.PocoAsArray.Single, MsgPackCSharp.MessagePackSerializerOptions.Standard);
		this.buffer.Reset();
	}

	[Benchmark]
	[BenchmarkCategory("array", "Serialize")]
	public void SerializeAsArray_STJ() => this.SerializeJson(Data.PocoAsArray.Single, STJSourceGenerationContext.Default.PocoAsArray);

	[Benchmark]
	[BenchmarkCategory("array", "Deserialize")]
	public void DeserializeAsArray()
	{
		this.serializer.Deserialize<PocoAsArray>(Data.PocoAsArray.SingleMsgpack);
	}

	[Benchmark(Baseline = true)]
	[BenchmarkCategory("array", "Deserialize")]
	public void DeserializeAsArray_MsgPackCSharp()
	{
		MsgPackCSharp.MessagePackSerializer.Deserialize<PocoAsArray>(Data.PocoAsArray.SingleMsgpack, MsgPackCSharp.MessagePackSerializerOptions.Standard);
	}

	[Benchmark]
	[BenchmarkCategory("array", "Deserialize")]
	public void DeserializeAsArray_STJ() => this.DeserializeJson(Data.PocoAsArray.SingleJson, STJSourceGenerationContext.Default.PocoAsArray);

	[Benchmark]
	[BenchmarkCategory("array-init", "Serialize")]
	public void SerializeAsArrayInit()
	{
		this.serializer.Serialize(this.buffer, Data.PocoAsArrayInit.Single);
		this.buffer.Reset();
	}

	[Benchmark(Baseline = true)]
	[BenchmarkCategory("array-init", "Serialize")]
	public void SerializeAsArrayInit_MsgPackCSharp()
	{
		MsgPackCSharp.MessagePackSerializer.Serialize(this.buffer, Data.PocoAsArrayInit.Single, MsgPackCSharp.MessagePackSerializerOptions.Standard);
		this.buffer.Reset();
	}

	[Benchmark]
	[BenchmarkCategory("array-init", "Serialize")]
	public void SerializeAsArrayInit_STJ() => this.SerializeJson(Data.PocoAsArrayInit.Single, STJSourceGenerationContext.Default.PocoAsArrayInit);

	[Benchmark]
	[BenchmarkCategory("array-init", "Deserialize")]
	public void DeserializeAsArrayInit()
	{
		this.serializer.Deserialize<PocoAsArrayInit>(Data.PocoAsArrayInit.SingleMsgpack);
	}

	[Benchmark(Baseline = true)]
	[BenchmarkCategory("array-init", "Deserialize")]
	public void DeserializeAsArrayInit_MsgPackCSharp()
	{
		MsgPackCSharp.MessagePackSerializer.Deserialize<PocoAsArrayInit>(Data.PocoAsArrayInit.SingleMsgpack, MsgPackCSharp.MessagePackSerializerOptions.Standard);
	}

	[Benchmark]
	[BenchmarkCategory("array-init", "Deserialize")]
	public void DeserializeAsArrayInit_STJ() => this.DeserializeJson(Data.PocoAsArrayInit.SingleJson, STJSourceGenerationContext.Default.PocoAsArrayInit);

	private void SerializeJson<T>(T value, STJ.Serialization.Metadata.JsonTypeInfo<T> typeInfo)
	{
		STJ.Utf8JsonWriter writer = new(this.buffer);
		STJ.JsonSerializer.Serialize(writer, value, typeInfo);
		writer.Flush();
		this.buffer.Reset();
	}

	private T? DeserializeJson<T>(ReadOnlySequence<byte> json, STJ.Serialization.Metadata.JsonTypeInfo<T> typeInfo)
	{
		STJ.Utf8JsonReader r = new(json);
		return STJ.JsonSerializer.Deserialize(ref r, typeInfo);
	}
}
