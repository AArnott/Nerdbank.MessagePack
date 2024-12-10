// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable NBMsgPackAsync // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using DecodeResult = Nerdbank.MessagePack.MessagePackPrimitives.DecodeResult;

public class MessagePackStreamingReaderTests(ITestOutputHelper logger)
{
	private static readonly ReadOnlySequence<byte> ArrayOf3Bools = CreateMsgPackArrayOf3Bools();

	[Fact]
	public void ReadIncompleteBuffer()
	{
		MessagePackStreamingReader incompleteReader = new(ArrayOf3Bools.Slice(0, 2));
		Assert.Equal(DecodeResult.Success, incompleteReader.TryReadArrayHeader(out int count));
		Assert.Equal(3, count);
		Assert.Equal(DecodeResult.Success, incompleteReader.TryRead(out bool boolean));
		Assert.False(boolean);
		Assert.Equal(DecodeResult.InsufficientBuffer, incompleteReader.TryRead(out boolean));
	}

	[Fact]
	public async Task ReplenishBufferAsync_AddsMoreBytesOnce()
	{
		// Arrange the reader to have an incomplete buffer and that upon request it will get the rest of it.
		MessagePackStreamingReader incompleteReader = new(
			ArrayOf3Bools.Slice(0, 2),
			(_, pos, examined, ct) => new(new ReadResult(ArrayOf3Bools.Slice(pos), false, isCompleted: true)),
			null);

		Assert.Equal(DecodeResult.Success, incompleteReader.TryReadArrayHeader(out int count));
		Assert.Equal(3, count);
		Assert.Equal(DecodeResult.Success, incompleteReader.TryRead(out bool boolean));
		Assert.False(boolean);
		Assert.Equal(DecodeResult.InsufficientBuffer, incompleteReader.TryRead(out boolean));
		incompleteReader = new(await incompleteReader.FetchMoreBytesAsync());
		Assert.Equal(DecodeResult.Success, incompleteReader.TryRead(out boolean));
		Assert.True(boolean);
		Assert.Equal(DecodeResult.Success, incompleteReader.TryRead(out boolean));
		Assert.False(boolean);

		Assert.Equal(DecodeResult.EmptyBuffer, incompleteReader.TryReadNil());
	}

	[Fact]
	public async Task ReplenishBufferAsync_AddsMoreBytes_ThenCompletes()
	{
		// Arrange the reader to have an incomplete buffer and that upon request it will get the rest of it.
		int callCount = 0;
		MessagePackStreamingReader incompleteReader = new(
			ArrayOf3Bools.Slice(0, 2),
			(_, pos, examined, ct) => new(new ReadResult(ArrayOf3Bools.Slice(pos), false, isCompleted: ++callCount > 1)),
			null);

		Assert.Equal(DecodeResult.Success, incompleteReader.TryReadArrayHeader(out int count));
		Assert.Equal(3, count);
		Assert.Equal(DecodeResult.Success, incompleteReader.TryRead(out bool boolean));
		Assert.False(boolean);
		Assert.Equal(DecodeResult.InsufficientBuffer, incompleteReader.TryRead(out boolean));
		incompleteReader = new(await incompleteReader.FetchMoreBytesAsync());
		Assert.Equal(DecodeResult.Success, incompleteReader.TryRead(out boolean));
		Assert.True(boolean);
		Assert.Equal(DecodeResult.Success, incompleteReader.TryRead(out boolean));
		Assert.False(boolean);

		Assert.Equal(DecodeResult.InsufficientBuffer, incompleteReader.TryReadNil());
		incompleteReader = new(await incompleteReader.FetchMoreBytesAsync());
		Assert.Equal(DecodeResult.EmptyBuffer, incompleteReader.TryReadNil());
	}

	[Fact]
	public async Task SkipIncrementally()
	{
		Sequence<byte> seq = new();
		MessagePackWriter writer = new(seq);

		writer.WriteArrayHeader(3);
		writer.Write(5);

		writer.WriteMapHeader(2);
		writer.Write("key1");
		writer.Write(1);
		writer.Write("key2");
		writer.Write(2);

		writer.Write(true);

		writer.Flush();

		ReadOnlySequence<byte> ros = seq.AsReadOnlySequence;
		MessagePackStreamingReader reader = new(ros.Slice(0, 1), FetchMoreBytesAsync, ros);
		SerializationContext context = new();
		int fetchCount = 0;
		while (reader.TrySkip(context).NeedsMoreBytes())
		{
			reader = new(await reader.FetchMoreBytesAsync());
			fetchCount++;
		}

		Assert.Equal(ros.End, reader.Position);
		logger.WriteLine($"Fetched {fetchCount} times (for a sequence that is {ros.Length} bytes long.)");
	}

	private static ReadOnlySequence<byte> CreateMsgPackArrayOf3Bools()
	{
		Sequence<byte> seq = new();
		MessagePackWriter writer = new(seq);
		writer.WriteArrayHeader(3);
		writer.Write(false);
		writer.Write(true);
		writer.Write(false);
		writer.Flush();

		return seq;
	}

	private static ValueTask<ReadResult> FetchMoreBytesAsync(object? state, SequencePosition consumed, SequencePosition examined, CancellationToken cancellationToken)
	{
		ReadOnlySequence<byte> wholeBuffer = (ReadOnlySequence<byte>)state!;

		// Always provide just one more byte.
		ReadOnlySequence<byte> slice = wholeBuffer.Slice(consumed, wholeBuffer.GetPosition(1, examined));
		return new(new ReadResult(slice, isCanceled: false, isCompleted: slice.End.Equals(wholeBuffer.End)));
	}
}
