// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers.Binary;

/// <summary>
/// Timestamps outside the range <see cref="DateTime"/> can represent must fail with a
/// <see cref="MessagePackSerializationException"/> that names the seconds, nanoseconds, and range.
/// </summary>
public partial class DateTimeTimestampRangeTests : MessagePackSerializerTestBase
{
	private const long Year0000Seconds = -62167219200;

	/// <summary>The last whole second <see cref="DateTime.MaxValue"/> can represent (9999-12-31T23:59:59Z).</summary>
	private const long MaxRepresentableSeconds = 253402300799;

	/// <summary>10000-01-01T00:00:00Z, one second past <see cref="DateTime.MaxValue"/>.</summary>
	private const long Year10000Seconds = 253402300800;

	private const long OneSecondInNanoseconds = 1_000_000_000;

	/// <summary>
	/// The compliance vector for 0000-01-01T00:00:00Z: timestamp 96, seconds = -62167219200, nanoseconds = 0.
	/// </summary>
	private static readonly byte[] Year0000Timestamp = [0xc7, 0x0c, 0xff, 0x00, 0x00, 0x00, 0x00, 0xff, 0xff, 0xff, 0xf1, 0x86, 0x8b, 0x84, 0x00];

	[Test]
	[Arguments(Year0000Seconds, 0L)]
	[Arguments(Year10000Seconds, 0L)]
	[Arguments(MaxRepresentableSeconds, OneSecondInNanoseconds)]
	public void OutOfRange_ThrowsFromEveryReader(long seconds, long nanoseconds)
	{
		byte[] msgpack = EncodeTimestamp96(seconds, nanoseconds);
		if (seconds == Year0000Seconds && nanoseconds == 0)
		{
			Assert.Equal(Year0000Timestamp, msgpack);
		}

		AssertOutOfRange(msgpack, seconds, nanoseconds);
	}

	[Test]
	public async Task OutOfRange_StreamingReader_ReplenishesThenThrows()
	{
		ReadOnlySequence<byte> sequence = new(Year0000Timestamp);
		MessagePackStreamingReader reader = new(
			sequence.Slice(0, 2),
			(_, pos, examined, ct) => new(new ReadResult(sequence.Slice(pos), false, isCompleted: true)),
			null);

		Assert.Equal(MessagePackPrimitives.DecodeResult.InsufficientBuffer, reader.TryRead(out DateTime _));
		reader = new(await reader.FetchMoreBytesAsync());

		MessagePackSerializationException? ex = null;
		try
		{
			reader.TryRead(out DateTime _);
		}
		catch (MessagePackSerializationException caught)
		{
			ex = caught;
		}

		Assert.NotNull(ex);
		AssertTimestampOutOfRange(ex, Year0000Seconds, 0);
	}

	[Test]
	[Arguments(Year0000Seconds, 0L)]
	[Arguments(Year10000Seconds, 0L)]
	public void OutOfRange_SerializerAndPrimitives(long seconds, long nanoseconds)
	{
		byte[] msgpack = EncodeTimestamp96(seconds, nanoseconds);
		string expected = ExpectedMessage(seconds, nanoseconds);

		MessagePackSerializationException ex = Assert.Throws<MessagePackSerializationException>(() => this.Serializer.Deserialize<DateTime, Witness>(msgpack, this.TimeoutToken));
		Assert.Contains(expected, GetFullMessage(ex));

		ex = Assert.Throws<MessagePackSerializationException>(() =>
		{
			MessagePackReader reader = new(msgpack);
			this.Serializer.DeserializePrimitives(ref reader, this.TimeoutToken);
		});
		Assert.Contains(expected, GetFullMessage(ex));
	}

	[Test]
	[Arguments(Year0000Seconds, 0L)]
	[Arguments(Year10000Seconds, 0L)]
	public async Task OutOfRange_DeserializeAsync(long seconds, long nanoseconds)
	{
		byte[] msgpack = EncodeTimestamp96(seconds, nanoseconds);
		FragmentedPipeReader pipe = new(new ReadOnlySequence<byte>(msgpack));
		MessagePackSerializationException ex = await Assert.ThrowsAsync<MessagePackSerializationException>(
			() => this.Serializer.DeserializeAsync<DateTime, Witness>(pipe, this.TimeoutToken).AsTask());
		Assert.Contains(ExpectedMessage(seconds, nanoseconds), GetFullMessage(ex));
	}

	[Test]
	public void InRange_Boundaries_ReadFromEveryReader()
	{
		DateTime min = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
		DateTime max = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);
		AssertReads(Encode(min), min);
		AssertReads(Encode(max), max);
		Assert.Equal(MessagePackCode.Ext8, Encode(min)[0]);
		Assert.Equal(MessagePackCode.Ext8, Encode(max)[0]);
	}

	[Test]
	public void InRange_Timestamp32And64()
	{
		DateTime wholeSeconds = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		DateTime withFraction = new(2000, 1, 1, 0, 0, 0, 1, DateTimeKind.Utc);
		byte[] timestamp32 = Encode(wholeSeconds);
		byte[] timestamp64 = Encode(withFraction);
		Assert.Equal(MessagePackCode.FixExt4, timestamp32[0]);
		Assert.Equal(MessagePackCode.FixExt8, timestamp64[0]);
		AssertReads(timestamp32, wholeSeconds);
		AssertReads(timestamp64, withFraction);
	}

	[Test]
	public async Task InRange_Boundaries_DeserializeAsync()
	{
		await this.AssertRoundtripAsync<DateTime, Witness>(DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc));
		await this.AssertRoundtripAsync<DateTime, Witness>(DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc));
	}

	[Test]
	public void InRange_DeserializePrimitives_ReturnsDateTime()
	{
		DateTime expected = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
		byte[] msgpack = Encode(expected);
		MessagePackReader reader = new(msgpack);
		object? value = this.Serializer.DeserializePrimitives(ref reader, this.TimeoutToken);
		DateTime actual = Assert.IsType<DateTime>(value);
		AssertSame(expected, actual);
	}

	private static void AssertReads(ReadOnlyMemory<byte> msgpack, DateTime expected)
	{
		Assert.Equal(MessagePackPrimitives.DecodeResult.Success, MessagePackPrimitives.TryRead(msgpack.Span, out DateTime primitive, out int size));
		Assert.Equal(msgpack.Length, size);
		AssertSame(expected, primitive);

		Assert.Equal(MessagePackPrimitives.DecodeResult.Success, MessagePackPrimitives.TryReadExtensionHeader(msgpack.Span, out ExtensionHeader header, out int headerSize));
		Assert.Equal(MessagePackPrimitives.DecodeResult.Success, MessagePackPrimitives.TryRead(msgpack.Span.Slice(headerSize), header, out DateTime fromHeader, out _));
		AssertSame(expected, fromHeader);

		MessagePackReader reader = new(msgpack);
		AssertSame(expected, reader.ReadDateTime());
		Assert.True(reader.End);

		reader = new(msgpack);
		header = reader.ReadExtensionHeader();
		AssertSame(expected, reader.ReadDateTime(header));
		Assert.True(reader.End);

		ReadOnlySequence<byte> fragmented = SequenceBuilder.InsertFragmentBreak(new ReadOnlySequence<byte>(msgpack), 3);
		MessagePackStreamingReader streaming = new(fragmented);
		Assert.Equal(MessagePackPrimitives.DecodeResult.Success, streaming.TryRead(out DateTime streamed));
		AssertSame(expected, streamed);

		streaming = new(fragmented);
		Assert.Equal(MessagePackPrimitives.DecodeResult.Success, streaming.TryRead(out header));
		Assert.Equal(MessagePackPrimitives.DecodeResult.Success, streaming.TryRead(header, out DateTime streamedFromHeader));
		AssertSame(expected, streamedFromHeader);
	}

	private static void AssertOutOfRange(ReadOnlyMemory<byte> msgpack, long seconds, long nanoseconds)
	{
		MessagePackSerializationException ex = Assert.Throws<MessagePackSerializationException>(() => MessagePackPrimitives.TryRead(msgpack.Span, out DateTime _, out _));
		AssertTimestampOutOfRange(ex, seconds, nanoseconds);

		Assert.Equal(MessagePackPrimitives.DecodeResult.Success, MessagePackPrimitives.TryReadExtensionHeader(msgpack.Span, out ExtensionHeader header, out int headerSize));
		ex = Assert.Throws<MessagePackSerializationException>(() => MessagePackPrimitives.TryRead(msgpack.Span.Slice(headerSize), header, out DateTime _, out _));
		AssertTimestampOutOfRange(ex, seconds, nanoseconds);

		ex = Assert.Throws<MessagePackSerializationException>(() =>
		{
			MessagePackReader reader = new(msgpack);
			reader.ReadDateTime();
		});
		AssertTimestampOutOfRange(ex, seconds, nanoseconds);

		ex = Assert.Throws<MessagePackSerializationException>(() =>
		{
			MessagePackReader reader = new(msgpack);
			ExtensionHeader readHeader = reader.ReadExtensionHeader();
			reader.ReadDateTime(readHeader);
		});
		AssertTimestampOutOfRange(ex, seconds, nanoseconds);

		ReadOnlySequence<byte> fragmented = SequenceBuilder.InsertFragmentBreak(new ReadOnlySequence<byte>(msgpack), 3);
		ex = Assert.Throws<MessagePackSerializationException>(() =>
		{
			MessagePackStreamingReader streaming = new(fragmented);
			streaming.TryRead(out DateTime _);
		});
		AssertTimestampOutOfRange(ex, seconds, nanoseconds);

		ex = Assert.Throws<MessagePackSerializationException>(() =>
		{
			MessagePackStreamingReader streaming = new(fragmented);
			MessagePackPrimitives.DecodeResult headerResult = streaming.TryRead(out ExtensionHeader readHeader);
			Assert.Equal(MessagePackPrimitives.DecodeResult.Success, headerResult);
			streaming.TryRead(readHeader, out DateTime _);
		});
		AssertTimestampOutOfRange(ex, seconds, nanoseconds);

		ex = Assert.Throws<MessagePackSerializationException>(() =>
		{
			MessagePackReader reader = new(fragmented);
			reader.ReadDateTime();
		});
		AssertTimestampOutOfRange(ex, seconds, nanoseconds);
	}

	private static void AssertTimestampOutOfRange(MessagePackSerializationException exception, long seconds, long nanoseconds)
	{
		Assert.Equal(ExpectedMessage(seconds, nanoseconds), exception.Message);
		Assert.IsType<ArgumentOutOfRangeException>(exception.InnerException);
	}

	private static void AssertSame(DateTime expected, DateTime actual)
	{
		Assert.Equal(expected.Ticks, actual.Ticks);
		Assert.Equal(DateTimeKind.Utc, actual.Kind);
	}

	private static string ExpectedMessage(long seconds, long nanoseconds)
		=> $"The msgpack timestamp ({seconds} seconds and {nanoseconds} nanoseconds since the Unix epoch) is outside the range System.DateTime can represent (0001-01-01T00:00:00.0000000Z through 9999-12-31T23:59:59.9999999Z).";

	private static byte[] Encode(DateTime value)
	{
		using Sequence<byte> sequence = new();
		MessagePackWriter writer = new(sequence);
		writer.Write(value);
		writer.Flush();
		return sequence.AsReadOnlySequence.ToArray();
	}

	private static byte[] EncodeTimestamp96(long seconds, long nanoseconds)
	{
		byte[] msgpack = new byte[15];
		msgpack[0] = MessagePackCode.Ext8;
		msgpack[1] = 12;
		msgpack[2] = unchecked((byte)ReservedMessagePackExtensionTypeCode.DateTime);
		BinaryPrimitives.WriteUInt32BigEndian(msgpack.AsSpan(3), checked((uint)nanoseconds));
		BinaryPrimitives.WriteInt64BigEndian(msgpack.AsSpan(7), seconds);
		return msgpack;
	}

	[GenerateShapeFor<DateTime>]
	private partial class Witness;
}
