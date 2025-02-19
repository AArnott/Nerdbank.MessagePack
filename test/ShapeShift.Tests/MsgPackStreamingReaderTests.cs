// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable NBMsgPackAsync // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using System.Text;

public class MsgPackStreamingReaderTests(ITestOutputHelper logger)
{
	private static readonly MessagePackFormatter Formatter = MessagePackFormatter.Default;
	private static readonly MessagePackDeformatter Deformatter = MessagePackDeformatter.Default;

	private static readonly ReadOnlySequence<byte> ArrayOf3Bools = CreateMsgPackArrayOf3Bools();

	[Fact]
	public void ReadIncompleteBuffer()
	{
		StreamingReader incompleteReader = new(ArrayOf3Bools.Slice(0, 2), Deformatter);
		Assert.Equal(DecodeResult.Success, incompleteReader.TryReadArrayHeader(out int? count));
		Assert.Equal(3, count);
		Assert.Equal(DecodeResult.Success, incompleteReader.TryRead(out bool boolean));
		Assert.False(boolean);
		Assert.Equal(DecodeResult.InsufficientBuffer, incompleteReader.TryRead(out boolean));
	}

	[Fact]
	public async Task ReplenishBufferAsync_AddsMoreBytesOnce()
	{
		// Arrange the reader to have an incomplete buffer and that upon request it will get the rest of it.
		StreamingReader incompleteReader = new(
			ArrayOf3Bools.Slice(0, 2),
			(_, pos, examined, ct) => new(new ReadResult(ArrayOf3Bools.Slice(pos), false, isCompleted: true)),
			null,
			Deformatter);

		Assert.Equal(DecodeResult.Success, incompleteReader.TryReadArrayHeader(out int? count));
		Assert.Equal(3, count);
		Assert.Equal(DecodeResult.Success, incompleteReader.TryRead(out bool boolean));
		Assert.False(boolean);
		Assert.Equal(DecodeResult.InsufficientBuffer, incompleteReader.TryRead(out boolean));
		incompleteReader = new(await incompleteReader.FetchMoreBytesAsync());
		Assert.Equal(DecodeResult.Success, incompleteReader.TryRead(out boolean));
		Assert.True(boolean);
		Assert.Equal(DecodeResult.Success, incompleteReader.TryRead(out boolean));
		Assert.False(boolean);

		Assert.Equal(DecodeResult.InsufficientBuffer, incompleteReader.TryReadNull());
	}

	[Fact]
	public async Task ReplenishBufferAsync_AddsMoreBytes_ThenCompletes()
	{
		// Arrange the reader to have an incomplete buffer and that upon request it will get the rest of it.
		int callCount = 0;
		StreamingReader incompleteReader = new(
			ArrayOf3Bools.Slice(0, 2),
			(_, pos, examined, ct) => new(new ReadResult(ArrayOf3Bools.Slice(pos), false, isCompleted: ++callCount > 1)),
			null,
			Deformatter);

		Assert.Equal(DecodeResult.Success, incompleteReader.TryReadArrayHeader(out int? count));
		Assert.Equal(3, count);
		Assert.Equal(DecodeResult.Success, incompleteReader.TryRead(out bool boolean));
		Assert.False(boolean);
		Assert.Equal(DecodeResult.InsufficientBuffer, incompleteReader.TryRead(out boolean));
		incompleteReader = new(await incompleteReader.FetchMoreBytesAsync());
		Assert.Equal(DecodeResult.Success, incompleteReader.TryRead(out boolean));
		Assert.True(boolean);
		Assert.Equal(DecodeResult.Success, incompleteReader.TryRead(out boolean));
		Assert.False(boolean);

		Assert.Equal(DecodeResult.InsufficientBuffer, incompleteReader.TryReadNull());
		incompleteReader = new(await incompleteReader.FetchMoreBytesAsync());
		Assert.Equal(DecodeResult.InsufficientBuffer, incompleteReader.TryReadNull());
	}

	[Fact]
	public async Task SkipIncrementally()
	{
		Sequence<byte> seq = new();
		Writer writer = new(seq, MessagePackFormatter.Default);

		// For an exhaustive test, we must use at least one of every msgpack token type (at least, one for interesting branch of the internal switch statement).
		// 0. array
		writer.WriteStartVector(3);

		// 1. map
		writer.WriteStartMap(2);
		writer.Write("key1");   // String!
		writer.Write(1);        // Integer!
		writer.Write("key2");
		writer.Write(true);           // Boolean!

		// 2. extension
		Formatter.Write(ref writer.Buffer, new Extension(35, new byte[] { 1, 2, 3 }));

		// 3. binary
		writer.Write([6, 8]);

		// One extra msgpack element that should *not* be skipped.
		writer.Write(false);

		writer.Flush();

		ReadOnlySequence<byte> ros = seq.AsReadOnlySequence;
		StreamingReader reader = new(ros.Slice(0, 1), MessagePackSerializerTestBase.FetchOneByteAtATimeAsync, ros, MessagePackDeformatter.Default);
		SerializationContext context = new();
		int fetchCount = 0;
		while (reader.TrySkip(ref context).NeedsMoreBytes())
		{
			reader = new(await reader.FetchMoreBytesAsync());
			fetchCount++;
		}

		bool boolValue;
		while (reader.TryRead(out boolValue).NeedsMoreBytes())
		{
			reader = new(await reader.FetchMoreBytesAsync());
			fetchCount++;
		}

		Assert.False(boolValue);
		Assert.Equal(ros.End, reader.Position);
		logger.WriteLine($"Fetched {fetchCount} times (for a sequence that is {ros.Length} bytes long.)");
	}

	[Fact]
	public async Task TryRead_Extension()
	{
		Extension originalExtension = new(1, new byte[] { 1, 2, 3 });

		Sequence<byte> seq = new();
		Writer writer = new(seq, MessagePackFormatter.Default);
		Formatter.Write(ref writer.Buffer, originalExtension);
		writer.Flush();

		FragmentedPipeReader pipeReader = new(seq);
		StreamingReader reader = new(
			seq.AsReadOnlySequence.Slice(0, 3),
			(_, pos, examined, ct) => new(new ReadResult(seq.AsReadOnlySequence.Slice(pos), false, true)),
			null,
			MessagePackDeformatter.Default);
		Extension deserializedExtension;
		while (Deformatter.StreamingDeformatter.TryRead(ref reader.Reader, out deserializedExtension).NeedsMoreBytes())
		{
			reader = new(await reader.FetchMoreBytesAsync());
		}

		Assert.Equal(originalExtension, deserializedExtension);
	}

	[Fact]
	public async Task TryReadBinary()
	{
		byte[] originalData = [1, 2, 3];

		Sequence<byte> seq = new();
		Writer writer = new(seq, MessagePackFormatter.Default);
		writer.Write(originalData);
		writer.Flush();

		FragmentedPipeReader pipeReader = new(seq);
		StreamingReader reader = new(
			seq.AsReadOnlySequence.Slice(0, 2),
			(_, pos, examined, ct) => new(new ReadResult(seq.AsReadOnlySequence.Slice(pos), false, true)),
			null,
			MessagePackDeformatter.Default);
		ReadOnlySequence<byte> deserializedData;
		while (reader.TryReadBinary(out deserializedData).NeedsMoreBytes())
		{
			reader = new(await reader.FetchMoreBytesAsync());
		}

		Assert.Equal(originalData, deserializedData.ToArray());
	}

	[Fact]
	public async Task TryRead_String()
	{
		string originalData = "hello";

		Sequence<byte> seq = new();
		Writer writer = new(seq, MessagePackFormatter.Default);
		writer.Write(originalData);
		writer.Flush();

		FragmentedPipeReader pipeReader = new(seq);
		StreamingReader reader = new(
			seq.AsReadOnlySequence.Slice(0, 2),
			(_, pos, examined, ct) => new(new ReadResult(seq.AsReadOnlySequence.Slice(pos), false, true)),
			null,
			Deformatter);
		string? deserializedString;
		while (reader.TryRead(out deserializedString).NeedsMoreBytes())
		{
			reader = new(await reader.FetchMoreBytesAsync());
		}

		Assert.Equal(originalData, deserializedString);
	}

	[Fact]
	public async Task TryReadStringSequence()
	{
		string originalData = "hello";

		Sequence<byte> seq = new();
		Writer writer = new(seq, MessagePackFormatter.Default);
		writer.Write(originalData);
		writer.Flush();

		FragmentedPipeReader pipeReader = new(seq);
		StreamingReader reader = new(
			seq.AsReadOnlySequence.Slice(0, 2),
			(_, pos, examined, ct) => new(new ReadResult(seq.AsReadOnlySequence.Slice(pos), false, true)),
			null,
			Deformatter);
		ReadOnlySequence<byte> deserializedString;
		while (reader.TryReadStringSequence(out deserializedString).NeedsMoreBytes())
		{
			reader = new(await reader.FetchMoreBytesAsync());
		}

		Assert.Equal(originalData, Encoding.UTF8.GetString(deserializedString.ToArray()));
	}

	[Fact]
	public async Task TryReadStringSpan()
	{
		string originalData = "hello";

		Sequence<byte> seq = new();
		Writer writer = new(seq, MessagePackFormatter.Default);
		writer.Write(originalData);
		writer.Flush();

		FragmentedPipeReader pipeReader = new(seq);
		StreamingReader reader = new(
			seq.AsReadOnlySequence.Slice(0, 2),
			(_, pos, examined, ct) => new(new ReadResult(seq.AsReadOnlySequence.Slice(pos), false, true)),
			null,
			Deformatter);
		ReadOnlySpan<byte> deserializedString;
		while (reader.TryReadStringSpan(out bool contiguous, out deserializedString).NeedsMoreBytes())
		{
			reader = new(await reader.FetchMoreBytesAsync());
		}

		Assert.Equal(originalData, Encoding.UTF8.GetString(deserializedString.ToArray()));
	}

	private static ReadOnlySequence<byte> CreateMsgPackArrayOf3Bools()
	{
		Sequence<byte> seq = new();
		Writer writer = new(seq, MessagePackFormatter.Default);
		writer.WriteStartVector(3);
		writer.Write(false);
		writer.Write(true);
		writer.Write(false);
		writer.Flush();

		return seq;
	}
}
