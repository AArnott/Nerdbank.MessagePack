// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable NBMsgPackAsync

public abstract class AsyncReaderTests(SerializerBase serializer) : SerializerTestBase(serializer)
{
	[Fact]
	public async Task BufferNextStructureAsync_IncompleteBuffer()
	{
		Sequence<byte> seq = new();
		Writer writer = new(seq, this.Serializer.Formatter);
		writer.WriteStartVector(3);
		writer.Write(1);
		writer.Write(2);
		writer.Write(3);
		writer.Flush();

		ReadOnlySequence<byte> ros = seq.AsReadOnlySequence;
		FragmentedPipeReader pipeReader = new(ros, ros.GetPosition(1));

		SerializationContext context = new();
		using AsyncReader reader = new(pipeReader, this.Serializer.Deformatter) { CancellationToken = default };
		await reader.BufferNextStructureAsync(context);
	}

	public class Json() : AsyncReaderTests(CreateJsonSerializer());

	public class MsgPack() : AsyncReaderTests(CreateMsgPackSerializer());
}
