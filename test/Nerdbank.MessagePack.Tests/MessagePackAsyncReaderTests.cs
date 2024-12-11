// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable NBMsgPackAsync

public class MessagePackAsyncReaderTests
{
	[Fact]
	public async Task BufferNextStructureAsync_IncompleteBuffer()
	{
		Sequence<byte> seq = new();
		MessagePackWriter writer = new(seq);
		writer.WriteArrayHeader(3);
		writer.Write(1);
		writer.Write(2);
		writer.Write(3);
		writer.Flush();

		ReadOnlySequence<byte> ros = seq.AsReadOnlySequence;

		Pipe pipe = new();
		pipe.Writer.Write(ros.Slice(0, 1));
		await pipe.Writer.FlushAsync();

		SerializationContext context = new();
		MessagePackAsyncReader reader = new(pipe.Reader) { CancellationToken = default };
		Task bufferTask = reader.BufferNextStructureAsync(context).AsTask();

		await Task.Delay(MessagePackSerializerTestBase.AsyncDelay);
		pipe.Writer.Write(ros.Slice(1));
		await pipe.Writer.CompleteAsync();

		await bufferTask;
	}
}
