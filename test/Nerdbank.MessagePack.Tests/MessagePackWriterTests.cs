// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using MessagePackWriterCref = MessagePack.MessagePackWriter;

public class MessagePackWriterTests
{
	private readonly ITestOutputHelper logger;

	public MessagePackWriterTests(ITestOutputHelper logger)
	{
		this.logger = logger;
	}

	/// <summary>
	/// Verifies that <see cref="MessagePackWriterCref.WriteRaw(ReadOnlySpan{byte})"/>
	/// accepts a span that came from stackalloc.
	/// </summary>
	[Fact]
	public unsafe void WriteRaw_StackAllocatedSpan()
	{
		var sequence = new Sequence<byte>();
		var writer = new MessagePackWriter(sequence);

		Span<byte> bytes = stackalloc byte[8];
		bytes[0] = 1;
		bytes[7] = 2;
		fixed (byte* pBytes = bytes)
		{
			var flexSpan = new Span<byte>(pBytes, bytes.Length);
			writer.WriteRaw(flexSpan);
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
		var writer = new MessagePackWriter(sequence);
		writer.Write((byte[]?)null);
		writer.Flush();
		var reader = new MessagePackReader(sequence.AsReadOnlySequence);
		Assert.True(reader.TryReadNil());
	}

	[Fact]
	public void Write_ByteArray()
	{
		var sequence = new Sequence<byte>();
		var writer = new MessagePackWriter(sequence);
		byte[] buffer = [1, 2, 3];
		writer.Write(buffer);
		writer.Flush();
		var reader = new MessagePackReader(sequence.AsReadOnlySequence);
		Assert.Equal(buffer, reader.ReadBytes()?.ToArray());
	}

	[Fact]
	public void Write_String_null()
	{
		var sequence = new Sequence<byte>();
		var writer = new MessagePackWriter(sequence);
		writer.Write((string?)null);
		writer.Flush();
		var reader = new MessagePackReader(sequence.AsReadOnlySequence);
		Assert.True(reader.TryReadNil());
	}

	[Fact]
	public void Write_String()
	{
		var sequence = new Sequence<byte>();
		var writer = new MessagePackWriter(sequence);
		string expected = "hello";
		writer.Write(expected);
		writer.Flush();
		var reader = new MessagePackReader(sequence.AsReadOnlySequence);
		Assert.Equal(expected, reader.ReadString());
	}

	[Fact]
	public void Write_String_MultibyteChars()
	{
		var sequence = new Sequence<byte>();
		var writer = new MessagePackWriter(sequence);
		writer.Write(TestConstants.MultibyteCharString);
		writer.Flush();

		this.logger.WriteLine("Written bytes: [{0}]", string.Join(", ", sequence.AsReadOnlySequence.ToArray().Select(b => string.Format(CultureInfo.InvariantCulture, "0x{0:x2}", b))));
		Assert.Equal(TestConstants.MsgPackEncodedMultibyteCharString.ToArray(), sequence.AsReadOnlySequence.ToArray());
	}

	[Fact]
	public void WriteStringHeader()
	{
		var sequence = new Sequence<byte>();
		var writer = new MessagePackWriter(sequence);
		byte[] strBytes = Encoding.UTF8.GetBytes("hello");
		writer.WriteStringHeader(strBytes.Length);
		writer.WriteRaw(strBytes);
		writer.Flush();

		var reader = new MessagePackReader(sequence);
		Assert.Equal("hello", reader.ReadString());
	}

	[Fact]
	public void WriteBinHeader()
	{
		var sequence = new Sequence<byte>();
		var writer = new MessagePackWriter(sequence);
		writer.WriteBinHeader(5);
		writer.WriteRaw([1, 2, 3, 4, 5]);
		writer.Flush();

		var reader = new MessagePackReader(sequence);
		Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, reader.ReadBytes()?.ToArray());
	}

	[Fact]
	public void WriteExtensionFormatHeader_NegativeExtension()
	{
		var sequence = new Sequence<byte>();
		var writer = new MessagePackWriter(sequence);

		var header = new ExtensionHeader(-1, 10);
		writer.WriteExtensionFormatHeader(header);
		writer.WriteRaw(new byte[10]);
		writer.Flush();

		ReadOnlySequence<byte> written = sequence.AsReadOnlySequence;
		var reader = new MessagePackReader(written);
		ExtensionHeader readHeader = reader.ReadExtensionHeader();

		Assert.Equal(header.TypeCode, readHeader.TypeCode);
		Assert.Equal(header.Length, readHeader.Length);
	}

	[Fact]
	public void CancellationToken()
	{
		var sequence = new Sequence<byte>();
		var writer = new MessagePackWriter(sequence);
		Assert.False(writer.CancellationToken.CanBeCanceled);

		var cts = new CancellationTokenSource();
		writer.CancellationToken = cts.Token;
		Assert.Equal(cts.Token, writer.CancellationToken);
	}

	[Fact]
	public void TryWriteWithBuggyWriter()
	{
		Assert.Throws<InvalidOperationException>(() =>
		{
			var writer = new MessagePackWriter(new BuggyBufferWriter());
			writer.WriteRaw(new byte[10]);
		});
	}

	[Fact]
	public void WriteVeryLargeData()
	{
		Sequence<byte> sequence = new();
		MessagePackWriter writer = new(sequence);
		writer.WriteRaw(new byte[1024 * 1024]);
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
