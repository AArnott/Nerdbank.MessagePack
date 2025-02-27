// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;

public abstract partial class WriterTests(SerializerBase serializer) : SerializerTestBase(serializer)
{
	protected Formatter Formatter => this.Serializer.Formatter;

	protected Deformatter Deformatter => this.Serializer.Deformatter;

	/// <summary>
	/// Verifies that <see cref="BufferWriter.Write(ReadOnlySpan{byte})"/>
	/// accepts a span that came from stackalloc.
	/// </summary>
	[Fact]
	public unsafe void WriteRaw_StackAllocatedSpan()
	{
		var sequence = new Sequence<byte>();
		var writer = new Writer(sequence, this.Formatter);

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
		var writer = new Writer(sequence, this.Formatter);
		writer.Write((byte[]?)null);
		writer.Flush();
		var reader = new Reader(sequence.AsReadOnlySequence, this.Deformatter);
		Assert.True(reader.TryReadNull());
	}

	[Fact]
	public void Write_ByteArray()
	{
		var sequence = new Sequence<byte>();
		var writer = new Writer(sequence, this.Formatter);
		byte[] buffer = [1, 2, 3];
		writer.Write(buffer);
		writer.Flush();
		var reader = new Reader(sequence.AsReadOnlySequence, this.Deformatter);
		Assert.Equal(buffer, reader.ReadBytes()?.ToArray());
	}

	[Fact]
	public void Write_String_null()
	{
		var sequence = new Sequence<byte>();
		var writer = new Writer(sequence, this.Formatter);
		writer.Write((string?)null);
		writer.Flush();
		var reader = new Reader(sequence.AsReadOnlySequence, this.Deformatter);
		Assert.True(reader.TryReadNull());
	}

	[Fact]
	public void Write_String()
	{
		var sequence = new Sequence<byte>();
		var writer = new Writer(sequence, this.Formatter);
		string expected = "hello";
		writer.Write(expected);
		writer.Flush();
		var reader = new Reader(sequence.AsReadOnlySequence, this.Deformatter);
		Assert.Equal(expected, reader.ReadString());
	}

	[Fact]
	public void Write_PreformattedString()
	{
		PreformattedString msgpackString = new("abc", this.Formatter);
		Sequence<byte> seq = new();
		Writer writer = new(seq, this.Formatter);
		writer.Write(msgpackString);
		writer.Flush();

		Reader reader = new(seq, this.Deformatter);
		Assert.Equal("abc", reader.ReadString());
	}

	[Fact]
	public void TryWriteWithBuggyWriter()
	{
		Assert.Throws<InvalidOperationException>(() =>
		{
			var writer = new Writer(new BuggyBufferWriter(), this.Formatter);
			writer.Buffer.Write(new byte[10]);
		});
	}

	[Fact]
	public void WriteVeryLargeData()
	{
		Sequence<byte> sequence = new();
		Writer writer = new(sequence, this.Formatter);
		writer.Buffer.Write(new byte[1024 * 1024]);
	}

	public class Json() : WriterTests(CreateJsonSerializer());

	public class MsgPack() : WriterTests(CreateMsgPackSerializer())
	{
		protected new ShapeShift.MessagePack.MessagePackFormatter Formatter => (ShapeShift.MessagePack.MessagePackFormatter)base.Formatter;

		protected new ShapeShift.MessagePack.MessagePackDeformatter Deformatter => (ShapeShift.MessagePack.MessagePackDeformatter)base.Deformatter;

		[Fact]
		public void Write_String_MultibyteChars()
		{
			var sequence = new Sequence<byte>();
			var writer = new Writer(sequence, this.Formatter);
			writer.Write(TestConstants.MultibyteCharString);
			writer.Flush();

			this.Logger.WriteLine("Written bytes: [{0}]", string.Join(", ", sequence.AsReadOnlySequence.ToArray().Select(b => string.Format(CultureInfo.InvariantCulture, "0x{0:x2}", b))));
			Assert.Equal(TestConstants.MsgPackEncodedMultibyteCharString.ToArray(), sequence.AsReadOnlySequence.ToArray());
		}

		[Fact]
		public void WriteStringHeader()
		{
			var sequence = new Sequence<byte>();
			var writer = new Writer(sequence, this.Formatter);
			byte[] strBytes = Encoding.UTF8.GetBytes("hello");
			this.Formatter.WriteStringHeader(ref writer.Buffer, strBytes.Length);
			writer.Buffer.Write(strBytes);
			writer.Flush();

			var reader = new Reader(sequence, this.Deformatter);
			Assert.Equal("hello", reader.ReadString());
		}

		[Fact]
		public void WriteBinHeader()
		{
			var sequence = new Sequence<byte>();
			var writer = new Writer(sequence, this.Formatter);
			this.Formatter.WriteBinHeader(ref writer.Buffer, 5);
			writer.Buffer.Write([1, 2, 3, 4, 5]);
			writer.Flush();

			var reader = new Reader(sequence, this.Deformatter);
			Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, reader.ReadBytes()?.ToArray());
		}

		[Fact]
		public void WriteExtensionHeader_NegativeExtension()
		{
			var sequence = new Sequence<byte>();
			var writer = new Writer(sequence, this.Formatter);

			var header = new ShapeShift.MessagePack.ExtensionHeader(-1, 10);
			this.Formatter.Write(ref writer.Buffer, header);
			writer.Buffer.Write(new byte[10]);
			writer.Flush();

			ReadOnlySequence<byte> written = sequence.AsReadOnlySequence;
			var reader = new Reader(written, this.Deformatter);
			ShapeShift.MessagePack.ExtensionHeader readHeader = this.Deformatter.ReadExtensionHeader(ref reader);

			Assert.Equal(header.TypeCode, readHeader.TypeCode);
			Assert.Equal(header.Length, readHeader.Length);
		}
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
