// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using K4os.Compression.LZ4;
using K4os.Compression.LZ4.Encoders;
using K4os.Compression.LZ4.Streams;

public partial class LZ4Tests(ITestOutputHelper logger) : MessagePackSerializerTestBase(logger)
{
	[Fact]
	public void CompressBlock()
	{
		Person[] array = Enumerable.Range(0, 100).Select(i => new Person($"First{i}", $"Last{i}")).ToArray();

		Sequence<byte> sequence = new();
		this.Serializer.Serialize<Person[], Witness>(sequence, array);

		Sequence<byte> compressed = new();
		LZ4Frame.Encode(sequence, compressed);

		this.Logger.WriteLine($"MsgPack size: {sequence.Length,8:N0}");
		this.Logger.WriteLine($"LZ4 size:     {compressed.Length,8:N0} ({(double)compressed.Length / sequence.Length:P0})");

		Sequence<byte> decompressed = new();
		LZ4Frame.Decode(compressed, decompressed);
		Person[]? deserialized = this.Serializer.Deserialize<Person[], Witness>(decompressed);
		Assert.NotNull(deserialized);
		Assert.Equal(array.AsSpan(), deserialized.AsSpan());
	}

	[GenerateShape]
	public partial record Person(string FirstName, string LastName);

	[GenerateShape<Person[]>]
	private partial class Witness;
}
