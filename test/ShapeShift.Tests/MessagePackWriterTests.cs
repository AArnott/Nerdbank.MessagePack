// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;

public class MessagePackWriterTests
{
	private static readonly MsgPackFormatter Formatter = MsgPackFormatter.Default;
	private static readonly MsgPackDeformatter Deformatter = MsgPackDeformatter.Default;
	private readonly ITestOutputHelper logger;

	public MessagePackWriterTests(ITestOutputHelper logger)
	{
		this.logger = logger;
	}

	/// <summary>
	/// Verifies that <see cref="BufferWriter.Write(ReadOnlySpan{byte})"/>
	/// accepts a span that came from stackalloc.
	/// </summary>
	[Fact]
	public unsafe void WriteRaw_StackAllocatedSpan()
	{
		var sequence = new Sequence<byte>();
		var writer = new Writer(sequence, Formatter);

		Span<byte> bytes = stackalloc byte[8];
		bytes[0] = 1;
		bytes[7] = 2;
		fixed (byte* pBytes = bytes)
		{
			var flexSpan = new Span<byte>(pBytes, bytes.Length);
			writer.Buffer.Write(flexSpan);
		}

		writer.Flush();
		byte[] written = sequence.AsReadOnlySequence.ToArray();
		Assert.Equal(1, written[0]);
		Assert.Equal(2, written[7]);
	}

	[Fact]
	public void Write_ByteArray_null()
	{
		var sequence = new Sequence<byte>();
		var writer = new Writer(sequence, Formatter);
		writer.Write((byte[]?)null);
		writer.Flush();
		var reader = new Reader(sequence.AsReadOnlySequence, Deformatter);
		Assert.True(reader.TryReadNull());
	}

	[Fact]
	public void Write_ByteArray()
	{
		var sequence = new Sequence<byte>();
		var writer = new Writer(sequence, Formatter);
		byte[] buffer = [1, 2, 3];
		writer.Write(buffer);
		writer.Flush();
		var reader = new Reader(sequence.AsReadOnlySequence, Deformatter);
		Assert.Equal(buffer, reader.ReadBytes()?.ToArray());
	}

	[Fact]
	public void Write_String_null()
	{
		var sequence = new Sequence<byte>();
		var writer = new Writer(sequence, Formatter);
		writer.Write((string?)null);
		writer.Flush();
		var reader = new Reader(sequence.AsReadOnlySequence, Deformatter);
		Assert.True(reader.TryReadNull());
	}

	[Fact]
	public void Write_String()
	{
		var sequence = new Sequence<byte>();
		var writer = new Writer(sequence, Formatter);
		string expected = "hello";
		writer.Write(expected);
		writer.Flush();
		var reader = new Reader(sequence.AsReadOnlySequence, Deformatter);
		Assert.Equal(expected, reader.ReadString());
	}

	[Fact]
	public void Write_String_MultibyteChars()
	{
		var sequence = new Sequence<byte>();
		var writer = new Writer(sequence, Formatter);
		writer.Write(TestConstants.MultibyteCharString);
		writer.Flush();

		this.logger.WriteLine("Written bytes: [{0}]", string.Join(", ", sequence.AsReadOnlySequence.ToArray().Select(b => string.Format(CultureInfo.InvariantCulture, "0x{0:x2}", b))));
		Assert.Equal(TestConstants.MsgPackEncodedMultibyteCharString.ToArray(), sequence.AsReadOnlySequence.ToArray());
	}

	[Fact]
	public void WriteStringHeader()
	{
		var sequence = new Sequence<byte>();
		var writer = new Writer(sequence, Formatter);
		byte[] strBytes = Encoding.UTF8.GetBytes("hello");
		Formatter.WriteStringHeader(ref writer, strBytes.Length);
		writer.Buffer.Write(strBytes);
		writer.Flush();

		var reader = new Reader(sequence, Deformatter);
		Assert.Equal("hello", reader.ReadString());
	}

	[Fact]
	public void Write_MessagePackString()
	{
		PreformattedString msgpackString = new("abc", Formatter);
		Sequence<byte> seq = new();
		Writer writer = new(seq, Formatter);
		writer.Write(msgpackString);
		writer.Flush();

		Reader reader = new(seq, Deformatter);
		Assert.Equal("abc", reader.ReadString());
	}

	[Fact]
	public void WriteBinHeader()
	{
		var sequence = new Sequence<byte>();
		var writer = new Writer(sequence, Formatter);
		Formatter.WriteBinHeader(ref writer, 5);
		writer.Buffer.Write([1, 2, 3, 4, 5]);
		writer.Flush();

		var reader = new Reader(sequence, Deformatter);
		Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, reader.ReadBytes()?.ToArray());
	}

	[Fact]
	public void WriteExtensionHeader_NegativeExtension()
	{
		var sequence = new Sequence<byte>();
		var writer = new Writer(sequence, Formatter);

		var header = new ExtensionHeader(-1, 10);
		Formatter.Write(ref writer, header);
		writer.Buffer.Write(new byte[10]);
		writer.Flush();

		ReadOnlySequence<byte> written = sequence.AsReadOnlySequence;
		var reader = new Reader(written, Deformatter);
		ExtensionHeader readHeader = Deformatter.ReadExtensionHeader(ref reader);

		Assert.Equal(header.TypeCode, readHeader.TypeCode);
		Assert.Equal(header.Length, readHeader.Length);
	}

	[Fact]
	public void TryWriteWithBuggyWriter()
	{
		Assert.Throws<InvalidOperationException>(() =>
		{
			var writer = new Writer(new BuggyBufferWriter(), Formatter);
			writer.Buffer.Write(new byte[10]);
		});
	}

	[Fact]
	public void WriteVeryLargeData()
	{
		Sequence<byte> sequence = new();
		Writer writer = new(sequence, Formatter);
		writer.Buffer.Write(new byte[1024 * 1024]);
	}

	/// <summary>
	/// Besides being effectively a no-op, this <see cref="IBufferWriter{T}"/>
	/// is buggy because it can return empty arrays, which should never happen.
	/// A sizeHint=0 means give me whatever memory is available, but should never be empty.
	/// </summary>
	private class BuggyBufferWriter : IBufferWriter<byte>
	{
		public void Advance(int count)
		{
		}

		public Memory<byte> GetMemory(int sizeHint = 0) => new byte[sizeHint];

		public Span<byte> GetSpan(int sizeHint = 0) => new byte[sizeHint];
	}
}
