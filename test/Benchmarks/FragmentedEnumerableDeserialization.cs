// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET

[MemoryDiagnoser]
[GenerateShapeFor<int[]>]
public partial class FragmentedEnumerableDeserialization
{
	private readonly MessagePackSerializer serializer = new();
	private byte[] payload = null!;

	[Params(10_000, 20_000, 40_000)]
	public int Count { get; set; }

	[Params(7, 4096, int.MaxValue)]
	public int ReadSize { get; set; }

	[GlobalSetup]
	public async Task Setup()
	{
		int[] value = Enumerable.Range(0, this.Count).ToArray();
		this.payload = this.serializer.Serialize<int[], FragmentedEnumerableDeserialization>(value);
		using Stream stream = new SmallReadStream(this.payload, this.ReadSize);
		int itemsRead = 0;
		await foreach (int[]? item in this.serializer.DeserializeEnumerableAsync<int[], FragmentedEnumerableDeserialization>(stream))
		{
			if (item is null || !item.SequenceEqual(value))
			{
				throw new InvalidOperationException("The benchmark payload did not roundtrip successfully.");
			}

			itemsRead++;
		}

		if (itemsRead != 1)
		{
			throw new InvalidOperationException("Expected exactly one deserialized array.");
		}
	}

	[Benchmark]
	public async Task<int> DeserializeEnumerable()
	{
		using Stream stream = new SmallReadStream(this.payload, this.ReadSize);
		int result = 0;
		await foreach (int[]? item in this.serializer.DeserializeEnumerableAsync<int[], FragmentedEnumerableDeserialization>(stream))
		{
			result += item!.Length;
		}

		return result;
	}

	private sealed class SmallReadStream(byte[] payload, int readSize) : MemoryStream(payload, writable: false)
	{
		public override bool CanSeek => false;

		public override int Read(byte[] buffer, int offset, int count) => base.Read(buffer, offset, Math.Min(count, readSize));

		public override int Read(Span<byte> buffer) => base.Read(buffer[..Math.Min(buffer.Length, readSize)]);

		public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return new(this.Read(buffer.Span));
		}
	}
}

#endif
