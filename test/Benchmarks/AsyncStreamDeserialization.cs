// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET

using System.IO.Pipelines;

[MemoryDiagnoser]
[GenerateShapeFor<PocoMapInit[]>]
public partial class AsyncStreamDeserialization
{
	private const int MinimumReadSize = 1024;
	private readonly MessagePackSerializer serializer = new();
	private byte[] payload = null!;

	[Params(10_000, 50_000)]
	public int Count { get; set; }

	[GlobalSetup]
	public void Setup()
	{
		PocoMapInit[] value = Enumerable.Range(0, this.Count)
			.Select(n => new PocoMapInit { SomeInt = n, SomeString = "A moderately long string representative of report data." })
			.ToArray();
		this.payload = this.serializer.Serialize<PocoMapInit[], AsyncStreamDeserialization>(value);
		PocoMapInit[]? roundtripped = this.serializer.Deserialize<PocoMapInit[], AsyncStreamDeserialization>(this.payload);
		if (roundtripped?.Length != this.Count)
		{
			throw new InvalidOperationException("The benchmark payload did not roundtrip successfully.");
		}
	}

	[Benchmark]
	public ValueTask<PocoMapInit[]?> DefaultBuffer() => this.DeserializeWithStreamOverloadAsync();

	[Benchmark(Baseline = true)]
	public ValueTask<PocoMapInit[]?> Buffer4KB() => this.DeserializeWithPipeReaderAsync(4 * 1024);

	[Benchmark]
	public ValueTask<PocoMapInit[]?> Buffer64KB() => this.DeserializeWithPipeReaderAsync(64 * 1024);

	[Benchmark]
	public ValueTask<PocoMapInit[]?> Buffer256KB() => this.DeserializeWithPipeReaderAsync(256 * 1024);

	[Benchmark]
	public ValueTask<PocoMapInit[]?> Buffer1MB() => this.DeserializeWithPipeReaderAsync(1024 * 1024);

	[Benchmark]
	public PocoMapInit[]? Synchronous() => this.serializer.Deserialize<PocoMapInit[], AsyncStreamDeserialization>(this.payload);

	private async ValueTask<PocoMapInit[]?> DeserializeWithStreamOverloadAsync()
	{
		using Stream stream = new DelegatingReadStream(this.payload);
		return await this.serializer.DeserializeAsync<PocoMapInit[], AsyncStreamDeserialization>(stream).ConfigureAwait(false);
	}

	private async ValueTask<PocoMapInit[]?> DeserializeWithPipeReaderAsync(int bufferSize)
	{
		using Stream stream = new DelegatingReadStream(this.payload);
		PipeReader reader = PipeReader.Create(
			stream,
			new StreamPipeReaderOptions(MemoryPool<byte>.Shared, bufferSize, MinimumReadSize, leaveOpen: true));
		try
		{
			return await this.serializer.DeserializeAsync<PocoMapInit[], AsyncStreamDeserialization>(reader).ConfigureAwait(false);
		}
		finally
		{
			await reader.CompleteAsync().ConfigureAwait(false);
		}
	}

	private sealed class DelegatingReadStream(byte[] payload) : Stream
	{
		private readonly MemoryStream inner = new(payload, writable: false);

		public override bool CanRead => true;

		public override bool CanSeek => false;

		public override bool CanWrite => false;

		public override long Length => throw new NotSupportedException();

		public override long Position
		{
			get => throw new NotSupportedException();
			set => throw new NotSupportedException();
		}

		public override void Flush() => throw new NotSupportedException();

		public override int Read(byte[] buffer, int offset, int count) => this.inner.Read(buffer, offset, count);

		public override int Read(Span<byte> buffer) => this.inner.Read(buffer);

		public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
			=> this.inner.ReadAsync(buffer, cancellationToken);

		public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

		public override void SetLength(long value) => throw new NotSupportedException();

		public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				this.inner.Dispose();
			}

			base.Dispose(disposing);
		}
	}
}

#endif
