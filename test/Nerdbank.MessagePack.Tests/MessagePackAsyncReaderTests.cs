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

	[Fact]
	public async Task CreatePeekReader_SharesBuffer()
	{
		// Arrange: Create a simple msgpack structure
		Sequence<byte> seq = new();
		MessagePackWriter writer = new(seq);
		writer.WriteArrayHeader(2);
		writer.Write("hello");
		writer.Write(42);
		writer.Flush();

		ReadOnlySequence<byte> ros = seq.AsReadOnlySequence;
		PipeReader pipeReader = new TestPipeReader(ros);

		using MessagePackAsyncReader originalReader = new(pipeReader) { CancellationToken = default };
		await originalReader.ReadAsync();

		// Act: Create a peek reader
		using MessagePackAsyncReader peekReader = originalReader.CreatePeekReader();

		// Assert: Both readers should share the same underlying data
		Assert.NotSame(originalReader, peekReader);

		// Both readers should have access to the same data
		await peekReader.ReadAsync();

		// Test original reader first
		MessagePackReader originalBuffered = originalReader.CreateBufferedReader();
		Assert.Equal(MessagePackType.Array, originalBuffered.NextMessagePackType);
		originalReader.ReturnReader(ref originalBuffered);

		// Test peek reader second
		MessagePackReader peekBuffered = peekReader.CreateBufferedReader();
		Assert.Equal(MessagePackType.Array, peekBuffered.NextMessagePackType);
		peekReader.ReturnReader(ref peekBuffered);
	}

	[Fact]
	public async Task CreatePeekReader_IndependentPositions()
	{
		// Arrange: Create msgpack with multiple values
		Sequence<byte> seq = new();
		MessagePackWriter writer = new(seq);
		writer.Write("first");
		writer.Write("second");
		writer.Write("third");
		writer.Flush();

		ReadOnlySequence<byte> ros = seq.AsReadOnlySequence;
		PipeReader pipeReader = new TestPipeReader(ros);

		using MessagePackAsyncReader originalReader = new(pipeReader) { CancellationToken = default };
		await originalReader.ReadAsync();

		// Act: Create peek reader and advance it
		using MessagePackAsyncReader peekReader = originalReader.CreatePeekReader();
		await peekReader.ReadAsync();

		MessagePackReader peekBuffered = peekReader.CreateBufferedReader();
		string firstValue = peekBuffered.ReadString(); // Read "first"
		string secondValue = peekBuffered.ReadString(); // Read "second"
		peekReader.ReturnReader(ref peekBuffered);

		// Verify original reader is still at the beginning
		MessagePackReader originalBuffered = originalReader.CreateBufferedReader();
		string originalFirst = originalBuffered.ReadString(); // Should still read "first"
		originalReader.ReturnReader(ref originalBuffered);

		// Assert: Peek reader advanced, original did not
		Assert.Equal("first", firstValue);
		Assert.Equal("second", secondValue);
		Assert.Equal("first", originalFirst);
	}

	[Fact]
	public async Task CreatePeekReader_SharedBufferGrowth()
	{
		// Arrange: Create a fragmented pipe reader that will require buffer growth
		Sequence<byte> seq = new();
		MessagePackWriter writer = new(seq);
		writer.Write("small");
		writer.WriteArrayHeader(100); // Large structure to trigger buffer expansion
		for (int i = 0; i < 100; i++)
		{
			writer.Write(i);
		}

		writer.Flush();

		ReadOnlySequence<byte> ros = seq.AsReadOnlySequence;

		// Fragment the buffer to force growth during reading
		FragmentedPipeReader pipeReader = new(ros, ros.GetPosition(1));

		using MessagePackAsyncReader originalReader = new(pipeReader) { CancellationToken = default };
		await originalReader.ReadAsync();

		// Act: Create peek reader
		using MessagePackAsyncReader peekReader = originalReader.CreatePeekReader();

		// Force buffer growth by reading large structure
		SerializationContext context = new();
		await peekReader.BufferNextStructuresAsync(2, 2, context);

		// Assert: Both readers should benefit from the expanded buffer
		// Test original reader first
		MessagePackReader originalBuffered = originalReader.CreateBufferedReader();
		string originalValue = originalBuffered.ReadString();
		originalReader.ReturnReader(ref originalBuffered);

		// Test peek reader second
		MessagePackReader peekBuffered = peekReader.CreateBufferedReader();
		string peekValue = peekBuffered.ReadString();
		peekReader.ReturnReader(ref peekBuffered);

		Assert.Equal("small", originalValue);
		Assert.Equal("small", peekValue);
	}

	[Fact]
	public async Task CreatePeekReader_CanBeDisposed()
	{
		// Arrange
		Sequence<byte> seq = new();
		MessagePackWriter writer = new(seq);
		writer.Write("test");
		writer.Flush();

		ReadOnlySequence<byte> ros = seq.AsReadOnlySequence;
		PipeReader pipeReader = new TestPipeReader(ros);

		using MessagePackAsyncReader originalReader = new(pipeReader) { CancellationToken = default };
		await originalReader.ReadAsync();

		// Act & Assert: Should be able to create, use, and dispose peek reader
		using (MessagePackAsyncReader peekReader = originalReader.CreatePeekReader())
		{
			await peekReader.ReadAsync();
			MessagePackReader buffered = peekReader.CreateBufferedReader();
			string value = buffered.ReadString();
			Assert.Equal("test", value);
			peekReader.ReturnReader(ref buffered);
		}

		// Original reader should still work after peek reader is disposed
		MessagePackReader originalBuffered = originalReader.CreateBufferedReader();
		string originalValue = originalBuffered.ReadString();
		Assert.Equal("test", originalValue);
		originalReader.ReturnReader(ref originalBuffered);
	}

	[Fact]
	public void CreatePeekReader_RequiresReaderReturned()
	{
		// Arrange
		Sequence<byte> seq = new();
		MessagePackWriter writer = new(seq);
		writer.Write("test");
		writer.Flush();

		ReadOnlySequence<byte> ros = seq.AsReadOnlySequence;
		PipeReader pipeReader = new TestPipeReader(ros);

		using MessagePackAsyncReader reader = new(pipeReader) { CancellationToken = default };

		// Act: Try to create peek reader without returning previous reader
		MessagePackStreamingReader streamingReader = reader.CreateStreamingReader();

		// Assert: Should throw since reader not returned
		Assert.Throws<InvalidOperationException>(() => reader.CreatePeekReader());

		// Clean up - must return reader before disposal to avoid exception
		reader.ReturnReader(ref streamingReader);
	}

	/// <summary>
	/// Simple test implementation of PipeReader for testing.
	/// </summary>
	private class TestPipeReader : PipeReader
	{
		private readonly ReadOnlySequence<byte> buffer;
		private bool completed;

		public TestPipeReader(ReadOnlySequence<byte> buffer)
		{
			this.buffer = buffer;
		}

		public override void AdvanceTo(SequencePosition consumed) => this.AdvanceTo(consumed, consumed);

		public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
		{
			// Simple implementation that doesn't actually track position
		}

		public override void CancelPendingRead()
		{
		}

		public override void Complete(Exception? exception = null)
		{
			this.completed = true;
		}

		public override ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
		{
			if (this.completed)
			{
				return new ValueTask<ReadResult>(new ReadResult(ReadOnlySequence<byte>.Empty, false, true));
			}

			return new ValueTask<ReadResult>(new ReadResult(this.buffer, false, false));
		}

		public override bool TryRead(out ReadResult result)
		{
			if (this.completed)
			{
				result = new ReadResult(ReadOnlySequence<byte>.Empty, false, true);
				return true;
			}

			result = new ReadResult(this.buffer, false, false);
			return true;
		}
	}
}
