// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

[GenerateShapeFor<int[]>]
public partial class MessagePackAsyncReaderTests
{
	[Test]
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

	[Test]
	[Arguments(1, false)]
	[Arguments(7, false)]
	[Arguments(1, true)]
	[Arguments(7, true)]
	public async Task BufferNextStructuresAsync_RelocatedBuffers(int readSize, bool asynchronousReads)
	{
		using Sequence<byte> sequence = new();
		MessagePackWriter writer = new(sequence);
		writer.Write(17);
		writer.WriteArrayHeader(4);
		writer.WriteMapHeader(1);
		writer.Write("key");
		writer.WriteArrayHeader(2);
		writer.Write(70_000);
		writer.Write(-33);
		writer.Write(new string('x', 70));
		writer.Write(new byte[40]);
		writer.Write(new Extension(35, new byte[20]));
		writer.Flush();
		byte[] expected = sequence.AsReadOnlySequence.Slice(1).ToArray();
		writer.Write(long.MaxValue);
		writer.WriteArrayHeader(0);
		writer.Write(false);
		writer.Flush();

		RelocatingPipeReader pipeReader = new(sequence.AsReadOnlySequence.ToArray(), readSize, asynchronousReads);
		using MessagePackAsyncReader reader = new(pipeReader) { CancellationToken = default };
		SerializationContext context = new();
		await reader.BufferNextStructureAsync(context);
		MessagePackReader bufferedReader = reader.CreateBufferedReader();
		Assert.Equal(17, bufferedReader.ReadInt32());
		reader.ReturnReader(ref bufferedReader);

		Assert.Equal(2, await reader.BufferNextStructuresAsync(2, 2, context));
		Assert.Equal(1, pipeReader.BytesConsumed);
		bufferedReader = reader.CreateBufferedReader();
		Assert.Equal(expected, bufferedReader.ReadRaw(context).MsgPack.ToArray());
		Assert.Equal(long.MaxValue, bufferedReader.ReadInt64());
		reader.ReturnReader(ref bufferedReader);

		Assert.Equal(2, await reader.BufferNextStructuresAsync(2, 2, context));
		bufferedReader = reader.CreateBufferedReader();
		Assert.Equal(0, bufferedReader.ReadArrayHeader());
		Assert.False(bufferedReader.ReadBoolean());
		Assert.True(bufferedReader.End);
		reader.ReturnReader(ref bufferedReader);
	}

	[Test]
	public async Task BufferNextStructuresAsync_CountUpTo()
	{
		RelocatingPipeReader pipeReader = new([1, 2, 3, 4], readSize: 4);
		using MessagePackAsyncReader reader = new(pipeReader) { CancellationToken = default };
		Assert.Equal(3, await reader.BufferNextStructuresAsync(1, 3, new()));
		Assert.Equal(1, pipeReader.ReadCount);
		MessagePackReader bufferedReader = reader.CreateBufferedReader();
		Assert.Equal(1, bufferedReader.ReadInt32());
		reader.ReturnReader(ref bufferedReader);
		Assert.Equal(3, await reader.BufferNextStructuresAsync(1, 3, new()));
		Assert.Equal(1, pipeReader.ReadCount);
	}

	[Test]
	public async Task BufferNextStructuresAsync_ZeroMinimum()
	{
		RelocatingPipeReader pipeReader = new([1], readSize: 1);
		using MessagePackAsyncReader reader = new(pipeReader) { CancellationToken = default };
		Assert.Equal(0, await reader.BufferNextStructuresAsync(0, 1, new()));
		Assert.Equal(0, pipeReader.ReadCount);
	}

	[Test]
	[Arguments(0)]
	[Arguments(1)]
	[Arguments(2)]
	[Arguments(3)]
	[Arguments(4)]
	public async Task BufferNextStructuresAsync_Truncated(int length)
	{
		byte[] payload = [0x92, 1, MessagePackCode.Int16, 0, 128];
		RelocatingPipeReader pipeReader = new(payload.AsSpan(0, length).ToArray(), readSize: 1);
		using MessagePackAsyncReader reader = new(pipeReader) { CancellationToken = default };
		await Assert.ThrowsAsync<EndOfStreamException>(async () => await reader.BufferNextStructureAsync(new()));
	}

	[Test]
	public async Task BufferNextStructuresAsync_InvalidCode()
	{
		RelocatingPipeReader pipeReader = new([0x92, 1, MessagePackCode.NeverUsed], readSize: 1);
		using MessagePackAsyncReader reader = new(pipeReader) { CancellationToken = default };
		await Assert.ThrowsAsync<MessagePackSerializationException>(async () => await reader.BufferNextStructureAsync(new()));
	}

	[Test]
	public async Task BufferNextStructuresAsync_Canceled()
	{
		using CancellationTokenSource cts = new();
		cts.Cancel();
		RelocatingPipeReader pipeReader = new([1], readSize: 1);
		using MessagePackAsyncReader reader = new(pipeReader) { CancellationToken = cts.Token };
		await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await reader.BufferNextStructureAsync(new()));
	}

	[Test]
	public async Task DeserializeEnumerableAsync_RelocatedBuffers()
	{
		MessagePackSerializer serializer = new();
		int[][] expected = [Enumerable.Range(0, 500).ToArray(), [], [int.MinValue, int.MaxValue]];
		using Sequence<byte> sequence = new();
		foreach (int[] item in expected)
		{
			serializer.Serialize<int[], MessagePackAsyncReaderTests>(sequence, item);
		}

		RelocatingPipeReader pipeReader = new(sequence.AsReadOnlySequence.ToArray(), readSize: 7, asynchronousReads: true);
		List<int[]?> actual = [];
		await foreach (int[]? item in serializer.DeserializeEnumerableAsync<int[], MessagePackAsyncReaderTests>(pipeReader))
		{
			actual.Add(item);
		}

		Assert.Equal(expected.Length, actual.Count);
		for (int i = 0; i < expected.Length; i++)
		{
			Assert.Equal(expected[i], actual[i]);
		}
	}

	private sealed class RelocatingPipeReader(byte[] payload, int readSize, bool asynchronousReads = false) : PipeReader
	{
		private ReadOnlySequence<byte> buffer;
		private byte[] memory = [];
		private int available;
		private bool expectAdvance;

		internal int BytesConsumed { get; private set; }

		internal int ReadCount { get; private set; }

		public override void AdvanceTo(SequencePosition consumed) => this.AdvanceTo(consumed, consumed);

		public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
		{
			Assert.True(this.expectAdvance);
			long consumedLength = this.buffer.Slice(0, consumed).Length;
			Assert.InRange(this.buffer.Slice(0, examined).Length, consumedLength, this.buffer.Length);
			this.BytesConsumed += (int)consumedLength;
			this.expectAdvance = false;
			this.memory.AsSpan().Fill(MessagePackCode.NeverUsed);
		}

		public override async ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
		{
			Assert.False(this.expectAdvance);
			cancellationToken.ThrowIfCancellationRequested();
			if (asynchronousReads)
			{
				await Task.Yield();
			}

			this.available = Math.Min(payload.Length, this.available + readSize);
			this.memory = payload.AsSpan(this.BytesConsumed, this.available - this.BytesConsumed).ToArray();
			int split = this.memory.Length / 2;
			this.buffer = SequenceBuilder.Create<byte>(this.memory.AsMemory(0, split), ReadOnlyMemory<byte>.Empty, this.memory.AsMemory(split));
			this.expectAdvance = true;
			this.ReadCount++;
			return new(this.buffer, isCanceled: false, isCompleted: this.available == payload.Length);
		}

		public override void CancelPendingRead() => throw new NotSupportedException();

		public override void Complete(Exception? exception = null)
		{
		}

		public override bool TryRead(out ReadResult result) => throw new NotSupportedException();
	}
}
