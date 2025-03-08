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
		FragmentedPipeReader pipeReader = new(ros, ros.GetPosition(1));

		SerializationContext context = new();
		using MessagePackAsyncReader reader = new(pipeReader) { CancellationToken = default };
		await reader.BufferNextStructureAsync(context);
	}
}
