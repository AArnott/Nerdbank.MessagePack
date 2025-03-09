// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Pipelines;
using Benchmarks;
using STJ = System.Text.Json;

[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkDotNet.Configs.BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class SyncVsAsyncPoco
{
	private readonly MessagePackSerializer serializer = new();
	private readonly ArrayBufferWriter<byte> syncBuffer = new();
	private readonly PipeWriter nullPipeWriter = PipeWriter.Create(Stream.Null);

	[Benchmark(Baseline = true)]
	[BenchmarkCategory("map-init", "Deserialize")]
	public void DeserializeMapInit()
	{
		PocoMapInit? result = this.serializer.Deserialize<PocoMapInit>(Data.PocoMapInit.SingleMsgpack);
	}

	[Benchmark]
	[BenchmarkCategory("map-init", "Deserialize")]
	public async ValueTask DeserializeAsyncMapInit()
	{
		PocoMapInit? result = await this.serializer.DeserializeAsync<PocoMapInit>(PipeReader.Create(Data.PocoMapInit.SingleMsgpack), default);
	}

	[Benchmark]
	[BenchmarkCategory("map-init", "Deserialize")]
	public async ValueTask JsonDeserializeAsyncMapInit() => await this.DeserializeJsonAsync(Data.PocoMapInit.SingleJsonStream, STJSourceGenerationContext.Default.PocoMapInit);

	[Benchmark(Baseline = true)]
	[BenchmarkCategory("map", "Serialize")]
	public void SerializeMap()
	{
		this.serializer.Serialize(this.syncBuffer, Data.PocoMap.Single);
		this.syncBuffer.Clear();
	}

	[Benchmark]
	[BenchmarkCategory("map", "Serialize")]
	public async Task SerializeAsyncMap()
	{
		await this.serializer.SerializeAsync(this.nullPipeWriter, Data.PocoMap.Single, default);
	}

	[Benchmark]
	[BenchmarkCategory("map", "Serialize")]
	public Task JsonSerializeAsyncMap() => this.SerializeJsonAsync(Data.PocoMap.Single, STJSourceGenerationContext.Default.PocoMap);

	[Benchmark(Baseline = true)]
	[BenchmarkCategory("map", "Deserialize")]
	public void DeserializeMap()
	{
		PocoMap? result = this.serializer.Deserialize<PocoMap>(Data.PocoMap.SingleMsgpack);
	}

	[Benchmark]
	[BenchmarkCategory("map", "Deserialize")]
	public async ValueTask DeserializeAsyncMap()
	{
		PocoMap? result = await this.serializer.DeserializeAsync<PocoMap>(PipeReader.Create(Data.PocoMap.SingleMsgpack), default);
	}

	[Benchmark]
	[BenchmarkCategory("map", "Deserialize")]
	public async ValueTask JsonDeserializeAsyncMap() => await this.DeserializeJsonAsync(Data.PocoMap.SingleJsonStream, STJSourceGenerationContext.Default.PocoMap);

	[Benchmark(Baseline = true)]
	[BenchmarkCategory("array-init", "Deserialize")]
	public void DeserializeAsArrayInit()
	{
		PocoAsArrayInit? result = this.serializer.Deserialize<PocoAsArrayInit>(Data.PocoAsArrayInit.SingleMsgpack);
	}

	[Benchmark]
	[BenchmarkCategory("array-init", "Deserialize")]
	public async ValueTask DeserializeAsyncAsArrayInit()
	{
		PocoAsArrayInit? result = await this.serializer.DeserializeAsync<PocoAsArrayInit>(PipeReader.Create(Data.PocoAsArrayInit.SingleMsgpack), default);
	}

	[Benchmark(Baseline = true)]
	[BenchmarkCategory("array", "Serialize")]
	public void SerializeAsArray()
	{
		this.serializer.Serialize(this.syncBuffer, Data.PocoAsArray.Single);
		this.syncBuffer.Clear();
	}

	[Benchmark]
	[BenchmarkCategory("array", "Serialize")]
	public async Task SerializeAsyncAsArray()
	{
		await this.serializer.SerializeAsync(this.nullPipeWriter, Data.PocoAsArray.Single, default);
	}

	[Benchmark(Baseline = true)]
	[BenchmarkCategory("array", "Deserialize")]
	public void DeserializeAsArray()
	{
		PocoAsArray? result = this.serializer.Deserialize<PocoAsArray>(Data.PocoAsArray.SingleMsgpack);
	}

	[Benchmark]
	[BenchmarkCategory("array", "Deserialize")]
	public async ValueTask DeserializeAsyncAsArray()
	{
		PocoAsArray? result = await this.serializer.DeserializeAsync<PocoAsArray>(PipeReader.Create(Data.PocoAsArray.SingleMsgpack), default);
	}

	private Task SerializeJsonAsync<T>(T value, STJ.Serialization.Metadata.JsonTypeInfo<T> typeInfo) => STJ.JsonSerializer.SerializeAsync(Stream.Null, value, typeInfo);

	private ValueTask<T?> DeserializeJsonAsync<T>(Stream json, STJ.Serialization.Metadata.JsonTypeInfo<T> typeInfo)
	{
		json.Position = 0;
		return STJ.JsonSerializer.DeserializeAsync(json, typeInfo);
	}
}
