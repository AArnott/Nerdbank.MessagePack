// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Pipelines;

[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkDotNet.Configs.BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[GenerateShapeFor<PocoMapInit[]>]
public partial class SyncVsAsyncArrays
{
	private readonly MessagePackSerializer serializer = new();
	private readonly Sequence syncBuffer = new();
	private Pipe pipe = new();

	[Benchmark(Baseline = true)]
	[BenchmarkCategory("map-init", "Serialize")]
	public void Sync_Serialize()
	{
		this.serializer.Serialize<PocoMapInit[], SyncVsAsyncArrays>(this.syncBuffer, Data.PocoMapInit.Array);
		this.syncBuffer.Reset();
	}

	[Benchmark(Baseline = true)]
	[BenchmarkCategory("map-init", "Deserialize")]
	public void Sync_Deserialize()
	{
		PocoMapInit[]? result = this.serializer.Deserialize<PocoMapInit[], SyncVsAsyncArrays>(Data.PocoMapInit.ArrayMsgpack);
	}

	[Benchmark]
	[BenchmarkCategory("map-init", "Serialize")]
	public async Task Async_Serialize()
	{
		await this.serializer.SerializeAsync<PocoMapInit[], SyncVsAsyncArrays>(this.pipe.Writer, Data.PocoMapInit.Array, default);

		this.pipe.Writer.Complete();
		this.pipe.Reader.Complete();
		this.pipe.Reset();
	}

	[Benchmark]
	[BenchmarkCategory("map-init", "Deserialize")]
	public async ValueTask Async_Deserialize()
	{
		PocoMapInit[]? result = await this.serializer.DeserializeAsync<PocoMapInit[], SyncVsAsyncArrays>(
			PipeReader.Create(Data.PocoMapInit.ArrayMsgpack),
			default);
	}
}
