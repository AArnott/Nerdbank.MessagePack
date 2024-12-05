// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DecodeResult = Nerdbank.MessagePack.MessagePackPrimitives.DecodeResult;

public class MessagePackStreamingReaderTests
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
		incompleteReader = new(await incompleteReader.ReplenishBufferAsync());
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
		incompleteReader = new(await incompleteReader.ReplenishBufferAsync());
		Assert.Equal(DecodeResult.Success, incompleteReader.TryRead(out boolean));
		Assert.True(boolean);
		Assert.Equal(DecodeResult.Success, incompleteReader.TryRead(out boolean));
		Assert.False(boolean);

		Assert.Equal(DecodeResult.InsufficientBuffer, incompleteReader.TryReadNil());
		incompleteReader = new(await incompleteReader.ReplenishBufferAsync());
		Assert.Equal(DecodeResult.EmptyBuffer, incompleteReader.TryReadNil());
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

	// TODO: Remove/move this sample
	[MessagePackConverter(typeof(SomeTypeConverter))]
	record SomeType(string Message, int Number);

	class SomeTypeConverter
	{
		internal async ValueTask<(SomeType, MessagePackStreamingReader.BufferRefresh)> Read(MessagePackStreamingReader.BufferRefresh readerInputs)
		{
			MessagePackStreamingReader reader = new(readerInputs);

			while (reader.TryReadArrayHeader(out int count).NeedsMoreBytes())
			{
				reader = new(await reader.ReplenishBufferAsync());
			}

			string? str;
			while (reader.TryRead(out str).NeedsMoreBytes())
			{
				reader = new(await reader.ReplenishBufferAsync());
			}

			int num;
			while (reader.TryRead(out num).NeedsMoreBytes())
			{
				reader = new(await reader.ReplenishBufferAsync());
			}

			return (new SomeType(str, num), reader.GetExchangeInfo());
		}
	}
}
