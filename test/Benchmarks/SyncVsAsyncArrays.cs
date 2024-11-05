// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Pipelines;
using static Data;

public class SyncVsAsyncArrays
{
	private readonly MessagePackSerializer serializer = new();
	private readonly Sequence<byte> syncBuffer = new();
	private Pipe pipe = new();

	[Benchmark]
	public void Sync_Serialize()
	{
		this.serializer.Serialize(this.syncBuffer, PocoArray);
		this.syncBuffer.Reset();
	}

	[Benchmark]
	public void Sync_Deserialize()
	{
		PocoClass? result = this.serializer.Deserialize<PocoClass>(PocoArrayMsgpack);
	}

	[Benchmark]
	public async Task Async_Serialize()
	{
		await this.serializer.SerializeAsync(this.pipe.Writer, PocoArray, default);

		this.pipe.Writer.Complete();
		this.pipe.Reader.Complete();
		this.pipe.Reset();
	}

	[Benchmark]
	public async ValueTask Async_Deserialize()
	{
		// Setup.
		this.pipe.Writer.Write(PocoArrayMsgpack);
		await this.pipe.Writer.FlushAsync();

		PocoClass? result = await this.serializer.DeserializeAsync<PocoClass>(this.pipe.Reader, default);
	}
}
