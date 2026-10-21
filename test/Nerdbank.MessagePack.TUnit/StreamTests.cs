// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public partial class StreamTests : MessagePackSerializerTestBase
{
	[Test, MatrixDataSource]
	public async Task SerializeWithStreamAsync(bool memoryStream)
	{
		Person person = new("Andrew", "Arnott");

		Stream stream = new MemoryStream();
		if (!memoryStream)
		{
			stream = new MonitoringStream(stream);
		}

		await this.Serializer.SerializeAsync(stream, person, this.TimeoutToken);

		stream.Position = 0;
		Person? deserialized = await this.Serializer.DeserializeAsync<Person>(stream, this.TimeoutToken);

		Assert.Equal(person, deserialized);
	}

	[Test, MatrixDataSource]
	public void SerializeWithStream(bool memoryStream)
	{
		Person person = new("Andrew", "Arnott");

		Stream stream = new MemoryStream();
		if (!memoryStream)
		{
			stream = new MonitoringStream(stream);
		}

		this.Serializer.Serialize(stream, person, this.TimeoutToken);

		stream.Position = 0;
		Person? deserialized = this.Serializer.Deserialize<Person>(stream, this.TimeoutToken);
	}

	[Test]
	public void Deserialize_FromMemoryStreamAtNonZeroPosition()
	{
		// Create test data
		Person person1 = new("Andrew", "Arnott");
		Person person2 = new("Jane", "Doe");

		// Serialize both persons to a MemoryStream
		MemoryStream stream = new();
		this.Serializer.Serialize(stream, person1, this.TimeoutToken);
		long person2Position = stream.Position;
		this.Serializer.Serialize(stream, person2, this.TimeoutToken);

		// Reset to position of second person and deserialize
		stream.Position = person2Position;
		Person? deserialized = this.Serializer.Deserialize<Person>(stream, this.TimeoutToken);

		// Should get the second person, not the first
		Assert.Equal(person2, deserialized);
		Assert.NotEqual(person1, deserialized);
	}

	[Test]
	public async Task DeserializeAsync_FromMemoryStreamAtNonZeroPosition()
	{
		// Create test data
		Person person1 = new("Andrew", "Arnott");
		Person person2 = new("Jane", "Doe");

		// Serialize both persons to a MemoryStream
		MemoryStream stream = new();
		await this.Serializer.SerializeAsync(stream, person1, this.TimeoutToken);
		long person2Position = stream.Position;
		await this.Serializer.SerializeAsync(stream, person2, this.TimeoutToken);

		// Reset to position of second person and deserialize
		stream.Position = person2Position;
		Person? deserialized = await this.Serializer.DeserializeAsync<Person>(stream, this.TimeoutToken);

		// Should get the second person, not the first
		Assert.Equal(person2, deserialized);
		Assert.NotEqual(person1, deserialized);
	}

	[Test]
	public async Task SerializeAsync_Stream_FailureDoesNotFlushPendingBytes()
	{
		WriteTrackingStream stream = new();
		MessagePackSerializationException ex = await Assert.ThrowsAsync<MessagePackSerializationException>(async () => await this.Serializer.SerializeAsync(stream, new FailsMidSerialization { First = new string('a', 100 * 1024) }, this.TimeoutToken));
		Assert.IsType<InvalidOperationException>(ex.GetBaseException());

		Assert.Equal(0, stream.BytesWritten);
		Assert.False(stream.IsDisposed);
	}

	[Test]
	public async Task SerializeObjectAsync_Stream_FailureDoesNotFlushPendingBytes()
	{
		WriteTrackingStream stream = new();
		MessagePackSerializationException ex = await Assert.ThrowsAsync<MessagePackSerializationException>(async () => await this.Serializer.SerializeObjectAsync(stream, new FailsMidSerialization { First = new string('a', 100 * 1024) }, GetShape<FailsMidSerialization>(), this.TimeoutToken));
		Assert.IsType<InvalidOperationException>(ex.GetBaseException());

		Assert.Equal(0, stream.BytesWritten);
		Assert.False(stream.IsDisposed);
	}

	private static ITypeShape GetShape<T>()
		where T : IShapeable<T> => T.GetTypeShape();

	[GenerateShape]
	public partial record Person(string FirstName, string LastName);

	[GenerateShape]
	public partial class FailsMidSerialization
	{
		public string? First { get; set; }

		public string Second
		{
			get => throw new InvalidOperationException("Serialization failed.");
			set { }
		}
	}

	// Deliberately not a MemoryStream so the PipeWriter-based code path is used.
	private class WriteTrackingStream : Stream
	{
		public long BytesWritten { get; private set; }

		public bool IsDisposed { get; private set; }

		public override bool CanRead => false;

		public override bool CanSeek => false;

		public override bool CanWrite => true;

		public override long Length => throw new NotSupportedException();

		public override long Position
		{
			get => throw new NotSupportedException();
			set => throw new NotSupportedException();
		}

		public override void Flush()
		{
		}

		public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

		public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

		public override void SetLength(long value) => throw new NotSupportedException();

		public override void Write(byte[] buffer, int offset, int count) => this.BytesWritten += count;

		public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
		{
			this.BytesWritten += buffer.Length;
			return default;
		}

		protected override void Dispose(bool disposing)
		{
			this.IsDisposed = true;
			base.Dispose(disposing);
		}
	}
}
