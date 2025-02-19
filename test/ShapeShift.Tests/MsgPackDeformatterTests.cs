// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

public partial class MsgPackDeformatterTests
{
	private const sbyte ByteNegativeValue = -3;
	private const byte BytePositiveValue = 3;
	private static readonly ReadOnlySequence<byte> StringEncodedAsFixStr = Encode((ref Writer w) => w.Write("hi"));
	private static readonly MessagePackDeformatter Deformatter = MessagePackDeformatter.Default;
	private static readonly MessagePackFormatter Formatter = MessagePackFormatter.Default;

	private readonly ITestOutputHelper logger;

	public MsgPackDeformatterTests(ITestOutputHelper logger)
	{
		this.logger = logger;
	}

	private delegate void RangeChecker(ref Reader reader);

	private delegate void ReaderOperation(ref Reader reader);

	private delegate T ReadOperation<T>(ref Reader reader);

	private delegate void WriterEncoder(ref Writer writer);

	[Fact]
	public void ReadSingle_ReadIntegersOfVariousLengthsAndMagnitudes()
	{
		foreach ((System.Numerics.BigInteger value, ReadOnlySequence<byte> encoded) in this.integersOfInterest)
		{
			this.logger.WriteLine("Decoding 0x{0:x} from {1}", value, MessagePackCode.ToFormatName(encoded.First.Span[0]));
			Assert.Equal((float)(double)value, new Reader(encoded, MessagePackDeformatter.Default).ReadSingle());
		}
	}

	[Fact]
	public void ReadSingle_CanReadDouble()
	{
		Reader reader = new(Encode((ref Writer w) => w.Write(1.23)), MessagePackDeformatter.Default);
		Assert.Equal(1.23f, reader.ReadSingle());
	}

	[Fact]
	public void ReadArrayHeader_MitigatesLargeAllocations()
	{
		var sequence = new Sequence<byte>();
		var writer = new Writer(sequence, MessagePackFormatter.Default);
		writer.WriteStartVector(9999);
		writer.Flush();

		Assert.Throws<EndOfStreamException>(() =>
		{
			var reader = new Reader(sequence, MessagePackDeformatter.Default);
			reader.ReadStartVector();
		});
	}

	[Fact]
	public void TryReadArrayHeader()
	{
		var sequence = new Sequence<byte>();
		var writer = new Writer(sequence, MessagePackFormatter.Default);
		const int expectedCount = 100;
		writer.WriteStartVector(expectedCount);
		writer.Flush();

		Reader reader = new(sequence.AsReadOnlySequence.Slice(0, sequence.Length - 1), MessagePackDeformatter.Default);
		Assert.False(reader.TryReadStartVector(out _));

		reader = new Reader(sequence, MessagePackDeformatter.Default);
		Assert.True(reader.TryReadStartVector(out int? actualCount));
		Assert.Equal(expectedCount, actualCount);
	}

	[Fact]
	public void ReadMapHeader_MitigatesLargeAllocations()
	{
		var sequence = new Sequence<byte>();
		var writer = new Writer(sequence, MessagePackFormatter.Default);
		writer.WriteStartMap(9999);
		writer.Flush();

		Assert.Throws<EndOfStreamException>(() =>
		{
			var reader = new Reader(sequence, MessagePackDeformatter.Default);
			reader.ReadStartMap();
		});
	}

	[Fact]
	public void TryReadMapHeader()
	{
		var sequence = new Sequence<byte>();
		var writer = new Writer(sequence, MessagePackFormatter.Default);
		const int expectedCount = 100;
		writer.WriteStartMap(expectedCount);
		writer.Flush();

		Reader reader = new(sequence.AsReadOnlySequence.Slice(0, sequence.Length - 1), MessagePackDeformatter.Default);
		Assert.False(reader.TryReadStartMap(out _));

		reader = new Reader(sequence, MessagePackDeformatter.Default);
		Assert.True(reader.TryReadStartMap(out int? actualCount));
		Assert.Equal(expectedCount, actualCount);
	}

	[Fact]
	public void TryReadMapHeader_Ranges()
	{
		this.AssertCodeRange((ref Reader r) => r.TryReadStartMap(out _), c => c is >= MessagePackCode.MinFixMap and <= MessagePackCode.MaxFixMap, c => c is MessagePackCode.Map16 or MessagePackCode.Map32);
	}

	[Fact]
	public void TryReadArrayHeader_Ranges()
	{
		this.AssertCodeRange((ref Reader r) => r.TryReadStartVector(out _), c => c is >= MessagePackCode.MinFixArray and <= MessagePackCode.MaxFixArray, c => c is MessagePackCode.Array16 or MessagePackCode.Array32);
	}

	[Fact]
	public void TryReadString_Ranges()
	{
		this.AssertCodeRange((ref Reader r) => r.TryReadStringSpan(out _), c => false, c => c is (>= MessagePackCode.MinFixStr and <= MessagePackCode.MaxFixStr) or MessagePackCode.Str8 or MessagePackCode.Str16 or MessagePackCode.Str32);
	}

	[Fact]
	public void TryReadInt_Ranges()
	{
		this.AssertCodeRange((ref Reader r) => r.ReadInt32(), c => c is (>= MessagePackCode.MinFixInt and <= MessagePackCode.MaxFixInt) or (>= MessagePackCode.MinNegativeFixInt and <= MessagePackCode.MaxNegativeFixInt), c => c is MessagePackCode.Int64 or MessagePackCode.Int32 or MessagePackCode.Int16 or MessagePackCode.Int8 or MessagePackCode.UInt64 or MessagePackCode.UInt32 or MessagePackCode.UInt16 or MessagePackCode.UInt8);
	}

	[Fact]
	public void ReadExtensionHeader_MitigatesLargeAllocations()
	{
		var sequence = new Sequence<byte>();
		var writer = new Writer(sequence, MessagePackFormatter.Default);
		MessagePackFormatter.Default.Write(ref writer.Buffer, new ExtensionHeader(3, 1));
		writer.Buffer.Write(new byte[1]);
		writer.Flush();

		Assert.Throws<EndOfStreamException>(() =>
		{
			var truncatedReader = new Reader(sequence.AsReadOnlySequence.Slice(0, sequence.Length - 1), MessagePackDeformatter.Default);
			MessagePackDeformatter.Default.ReadExtensionHeader(ref truncatedReader);
		});

		var reader = new Reader(sequence, MessagePackDeformatter.Default);
		MessagePackDeformatter.Default.ReadExtensionHeader(ref reader);
	}

	[Fact]
	public void TryReadStringSpan_Fragmented()
	{
		var contiguousSequence = new Sequence<byte>();
		var writer = new Writer(contiguousSequence, Formatter);
		byte[] expected = [0x1, 0x2, 0x3];
		writer.WriteEncodedString(expected);
		writer.Flush();
		ReadOnlySequence<byte> fragmentedSequence = BuildSequence(
		   contiguousSequence.AsReadOnlySequence.First.Slice(0, 2),
		   contiguousSequence.AsReadOnlySequence.First.Slice(2));

		var reader = new Reader(fragmentedSequence, MessagePackDeformatter.Default);
		Assert.False(reader.TryReadStringSpan(out ReadOnlySpan<byte> span));
		Assert.Equal(0, span.Length);

		// After failing to read the span, a caller should still be able to read it as a sequence.
		ReadOnlySequence<byte>? actualSequence = reader.ReadStringSequence();
		Assert.True(actualSequence.HasValue);
		Assert.False(actualSequence.Value.IsSingleSegment);
		Assert.Equal([1, 2, 3], actualSequence.Value.ToArray());
	}

	[Fact]
	public void TryReadStringSpan_Contiguous()
	{
		var sequence = new Sequence<byte>();
		var writer = new Writer(sequence, MessagePackFormatter.Default);
		byte[] expected = new byte[] { 0x1, 0x2, 0x3 };
		writer.WriteEncodedString(expected);
		writer.Flush();

		var reader = new Reader(sequence, MessagePackDeformatter.Default);
		Assert.True(reader.TryReadStringSpan(out ReadOnlySpan<byte> span));
		Assert.Equal(expected, span.ToArray());
		Assert.True(reader.End);
	}

	[Fact]
	public void TryReadStringSpan_Nil()
	{
		var sequence = new Sequence<byte>();
		var writer = new Writer(sequence, MessagePackFormatter.Default);
		writer.WriteNull();
		writer.Flush();

		ReadOnlySpan<byte> span = default;
		var reader = new Reader(sequence, MessagePackDeformatter.Default);
		try
		{
			reader.TryReadStringSpan(out span);
			Assert.Fail("Expected exception not thrown.");
		}
		catch (SerializationException)
		{
			// This exception is expected.
		}

		Assert.Equal(0, span.Length);
		Assert.Equal(sequence.AsReadOnlySequence.Start, reader.Position);
	}

	[Fact]
	public void TryReadStringSpan_WrongType()
	{
		var sequence = new Sequence<byte>();
		var writer = new Writer(sequence, MessagePackFormatter.Default);
		writer.Write(3);
		writer.Flush();

		Assert.Throws<SerializationException>(() =>
		{
			var reader = new Reader(sequence, MessagePackDeformatter.Default);
			reader.TryReadStringSpan(out ReadOnlySpan<byte> span);
		});
	}

	[Fact]
	public void ReadStringSpan_Fragmented()
	{
		var contiguousSequence = new Sequence<byte>();
		var writer = new Writer(contiguousSequence, Formatter);
		byte[] expected = [0x1, 0x2, 0x3];
		writer.WriteEncodedString(expected);
		writer.Flush();
		ReadOnlySequence<byte> fragmentedSequence = BuildSequence(
		   contiguousSequence.AsReadOnlySequence.First.Slice(0, 2),
		   contiguousSequence.AsReadOnlySequence.First.Slice(2));

		var reader = new Reader(fragmentedSequence, MessagePackDeformatter.Default);
		ReadOnlySpan<byte> span = reader.ReadStringSpan();
		Assert.Equal([1, 2, 3], span.ToArray());
	}

	[Fact]
	public void ReadStringSpan_Contiguous()
	{
		var sequence = new Sequence<byte>();
		var writer = new Writer(sequence, MessagePackFormatter.Default);
		byte[] expected = [0x1, 0x2, 0x3];
		writer.WriteEncodedString(expected);
		writer.Flush();

		var reader = new Reader(sequence, MessagePackDeformatter.Default);
		ReadOnlySpan<byte> span = reader.ReadStringSpan();
		Assert.Equal(expected, span.ToArray());
		Assert.True(reader.End);
	}

	[Fact]
	public void ReadStringSpan_Nil()
	{
		var sequence = new Sequence<byte>();
		var writer = new Writer(sequence, MessagePackFormatter.Default);
		writer.WriteNull();
		writer.Flush();

		Assert.Throws<SerializationException>(() =>
		{
			var reader = new Reader(sequence, MessagePackDeformatter.Default);
			reader.ReadStringSpan();
		});
	}

	[Fact]
	public void ReadStringSpan_WrongType()
	{
		var sequence = new Sequence<byte>();
		var writer = new Writer(sequence, MessagePackFormatter.Default);
		writer.Write(3);
		writer.Flush();

		Assert.Throws<SerializationException>(() =>
		{
			var reader = new Reader(sequence, MessagePackDeformatter.Default);
			reader.ReadStringSpan();
		});
	}

	[Fact]
	public void ReadString_MultibyteChars()
	{
		var reader = new Reader(TestConstants.MsgPackEncodedMultibyteCharString, MessagePackDeformatter.Default);
		string? actual = reader.ReadString();
		Assert.Equal(TestConstants.MultibyteCharString, actual);
	}

	[Fact]
	public void ReadRaw()
	{
		var sequence = new Sequence<byte>();
		var writer = new Writer(sequence, MessagePackFormatter.Default);
		writer.Write(3);
		writer.WriteStartVector(2);
		writer.Write(1);
		writer.Write("Hi");
		writer.Write(5);
		writer.Flush();

		var reader = new Reader(sequence.AsReadOnlySequence, MessagePackDeformatter.Default);

		ReadOnlySequence<byte> first = reader.ReadRaw(new SerializationContext());
		Assert.Equal(1, first.Length);
		Assert.Equal(3, new Reader(first, MessagePackDeformatter.Default).ReadInt32());

		ReadOnlySequence<byte> second = reader.ReadRaw(new SerializationContext());
		Assert.Equal(5, second.Length);

		ReadOnlySequence<byte> third = reader.ReadRaw(new SerializationContext());
		Assert.Equal(1, third.Length);

		Assert.True(reader.End);
	}

	[Fact]
	public void Read_CheckOperations_WithNoBytesLeft()
	{
		ReadOnlySequence<byte> partialMessage = default;

		AssertThrowsEndOfStreamException(partialMessage, (ref Reader reader) => reader.NextTypeCode);

		// These Try methods are meant to return false when it's not a matching code. End of stream when calling these methods is still unexpected.
		AssertThrowsEndOfStreamException(partialMessage, (ref Reader reader) => reader.TryReadNull());
		AssertThrowsEndOfStreamException(partialMessage, (ref Reader reader) => reader.TryReadStringSpan(out _));
		AssertThrowsEndOfStreamException(partialMessage, (ref Reader reader) => reader.IsNull);
	}

	[Fact]
	public void Read_WithInsufficientBytesLeft()
	{
		void AssertIncomplete<T>(WriterEncoder encoder, ReadOperation<T> decoder, bool validMsgPack = true)
		{
			ReadOnlySequence<byte> sequence = Encode(encoder);

			// Test with every possible truncated length.
			for (long len = sequence.Length - 1; len >= 0; len--)
			{
				ReadOnlySequence<byte> truncated = sequence.Slice(0, len);
				AssertThrowsEndOfStreamException<T>(truncated, decoder);

				if (validMsgPack)
				{
					AssertThrowsEndOfStreamException(truncated, (ref Reader reader) => reader.Skip(new SerializationContext()));
				}
			}
		}

		AssertIncomplete((ref Writer writer) => writer.WriteStartVector(0xfffffff), (ref Reader reader) => reader.ReadStartVector());
		AssertIncomplete((ref Writer writer) => writer.Write(true), (ref Reader reader) => reader.ReadBoolean());
		AssertIncomplete((ref Writer writer) => writer.Write(0xff), (ref Reader reader) => reader.ReadByte());
		AssertIncomplete((ref Writer writer) => writer.WriteEncodedString(Encoding.UTF8.GetBytes("hi")), (ref Reader reader) => reader.ReadBytes());
		AssertIncomplete((ref Writer writer) => writer.Write('c'), (ref Reader reader) => reader.ReadChar());
		AssertIncomplete((ref Writer writer) => writer.Write(double.MaxValue), (ref Reader reader) => reader.ReadDouble());
		AssertIncomplete((ref Writer writer) => Formatter.Write(ref writer.Buffer, new Extension(5, new byte[3])), (ref Reader reader) => Deformatter.ReadExtension(ref reader));
		AssertIncomplete((ref Writer writer) => Formatter.Write(ref writer.Buffer, new ExtensionHeader(5, 3)), (ref Reader reader) => Deformatter.ReadExtensionHeader(ref reader));
		AssertIncomplete((ref Writer writer) => writer.Write(0xff), (ref Reader reader) => reader.ReadInt16());
		AssertIncomplete((ref Writer writer) => writer.Write(0xff), (ref Reader reader) => reader.ReadInt32());
		AssertIncomplete((ref Writer writer) => writer.Write(0xff), (ref Reader reader) => reader.ReadInt64());
		AssertIncomplete((ref Writer writer) => writer.WriteStartMap(0xfffffff), (ref Reader reader) => reader.ReadStartMap());
#pragma warning disable SA1107 // Code should not contain multiple statements on one line
		AssertIncomplete((ref Writer writer) => writer.WriteNull(), (ref Reader reader) => { reader.ReadNull(); return MessagePackCode.Nil; });
#pragma warning restore SA1107 // Code should not contain multiple statements on one line
		AssertIncomplete((ref Writer writer) => writer.Write("hi"), (ref Reader reader) => reader.ReadRaw(new SerializationContext()));
		AssertIncomplete((ref Writer writer) => writer.Buffer.Write(new byte[10]), (ref Reader reader) => reader.ReadRaw(10), validMsgPack: false);
		AssertIncomplete((ref Writer writer) => writer.Write(0xff), (ref Reader reader) => reader.ReadSByte());
		AssertIncomplete((ref Writer writer) => writer.Write(0xff), (ref Reader reader) => reader.ReadSingle());
		AssertIncomplete((ref Writer writer) => writer.Write("hi"), (ref Reader reader) => reader.ReadString());
		AssertIncomplete((ref Writer writer) => writer.WriteEncodedString(Encoding.UTF8.GetBytes("hi")), (ref Reader reader) => reader.ReadStringSequence());
		AssertIncomplete((ref Writer writer) => writer.Write(0xff), (ref Reader reader) => reader.ReadUInt16());
		AssertIncomplete((ref Writer writer) => writer.Write(0xff), (ref Reader reader) => reader.ReadUInt32());
		AssertIncomplete((ref Writer writer) => writer.Write(0xff), (ref Reader reader) => reader.ReadUInt64());
	}

	[Fact]
	public void CreatePeekReader()
	{
		Reader reader = new(StringEncodedAsFixStr, MessagePackDeformatter.Default);
		reader.ReadRaw(1); // advance to test that the peek reader starts from a non-initial position.
		Reader peek = reader;

		// Verify equivalence
		Assert.Equal(reader.Position, peek.Position);
		Assert.Equal(reader.Sequence, peek.Sequence);

		// Verify that advancing the peek reader does not advance the original.
		SequencePosition originalPosition = reader.Position;
		peek.ReadRaw(1);
		Assert.NotEqual(originalPosition, peek.Position);
		Assert.Equal(originalPosition, reader.Position);
	}

	private static void AssertThrowsEndOfStreamException(ReadOnlySequence<byte> sequence, ReaderOperation readOperation)
	{
		Assert.Throws<EndOfStreamException>(() =>
		{
			var reader = new Reader(sequence, MessagePackDeformatter.Default);
			readOperation(ref reader);
		});
	}

	private static void AssertThrowsEndOfStreamException<T>(ReadOnlySequence<byte> sequence, ReadOperation<T> readOperation)
	{
		Assert.Throws<EndOfStreamException>(() =>
		{
			Decode(sequence, readOperation);
		});
	}

	private static T Decode<T>(ReadOnlySequence<byte> sequence, ReadOperation<T> readOperation)
	{
		var reader = new Reader(sequence, MessagePackDeformatter.Default);
		return readOperation(ref reader);
	}

	private static ReadOnlySequence<byte> Encode(WriterEncoder cb)
	{
		var sequence = new Sequence<byte>();
		var writer = new Writer(sequence, MessagePackFormatter.Default);
		cb(ref writer);
		writer.Flush();
		return sequence.AsReadOnlySequence;
	}

	private static ReadOnlySequence<T> BuildSequence<T>(params ReadOnlyMemory<T>[] memoryChunks)
	{
		var sequence = new Sequence<T>(new ExactArrayPool<T>())
		{
			MinimumSpanLength = -1,
		};
		foreach (ReadOnlyMemory<T> chunk in memoryChunks)
		{
			Span<T> span = sequence.GetSpan(chunk.Length);
			chunk.Span.CopyTo(span);
			sequence.Advance(chunk.Length);
		}

		return sequence;
	}

	private void AssertCodeRange(RangeChecker predicate, Func<byte, bool> isOneByteRepresentation, Func<byte, bool> isIntroductoryByte)
	{
		bool mismatch = false;
		byte[] buffer = new byte[1];
		foreach (byte code in Enumerable.Range(byte.MinValue, byte.MaxValue))
		{
			try
			{
				bool expectedOneByte = isOneByteRepresentation(code);
				bool expectedLeadingByte = isIntroductoryByte(code);
				if (expectedLeadingByte && expectedOneByte)
				{
					throw new Exception("Byte cannot be both a leading byte and a one-byte representation.");
				}

				bool expectedMatch = expectedOneByte || expectedLeadingByte;
				buffer[0] = code;
				Reader reader = new(buffer, MessagePackDeformatter.Default);
				bool actual;
				try
				{
					predicate(ref reader);
					actual = true;
				}
				catch (SerializationException)
				{
					actual = false;
				}
				catch (EndOfStreamException) when (expectedLeadingByte)
				{
					actual = true;
				}

				if (expectedMatch != actual)
				{
					this.logger.WriteLine($"Byte 0x{code:x2} was {actual} but was expected to be {expectedMatch}.");
					mismatch = true;
				}
			}
			catch (Exception ex)
			{
				mismatch = true;
				this.logger.WriteLine($"Byte 0x{code:x2} threw an exception: {ex}");
			}
		}

		if (mismatch)
		{
			throw new Exception("One or more byte values did not match the expected range.");
		}
	}

	/// <summary>
	/// Verifies that a ref struct can create and store a MessagePackReader given a short-lived copy of a
	/// <see cref="ReadOnlySequence{T}"/>.
	/// </summary>
	/// <remarks>
	/// This is a 'test' simply by being declared, since C# won't compile it if it's not valid.
	/// </remarks>
	private ref struct MySequenceReader
	{
		private Reader reader;

		public MySequenceReader(ReadOnlySequence<byte> seq)
		{
			this.reader = new Reader(seq, MessagePackDeformatter.Default);
		}
	}

	private class ExactArrayPool<T> : ArrayPool<T>
	{
		public override T[] Rent(int minimumLength) => new T[minimumLength];

		public override void Return(T[] array, bool clearArray = false)
		{
		}
	}
}
