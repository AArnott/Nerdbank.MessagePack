// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public partial class StringInterningTests : MessagePackSerializerTestBase
{
	public StringInterningTests(ITestOutputHelper logger)
		: base(logger)
	{
		this.Serializer = this.Serializer with { InternStrings = true };
	}

	[Fact]
	public void InternStringsDefault() => Assert.False(new MessagePackSerializer().InternStrings);

	[Fact]
	public void NoInterning()
	{
		this.Serializer = this.Serializer with { InternStrings = false };
		string[]? deserialized = this.Roundtrip<string[], Witness>(["a", "a"]);
		Assert.NotNull(deserialized);
		Assert.NotSame(deserialized[0], deserialized[1]);
	}

	[Fact]
	public void Interning()
	{
		this.Serializer = this.Serializer with { InternStrings = true };
		string[]? deserialized = this.Roundtrip<string[], Witness>(["a", "a"]);
		Assert.NotNull(deserialized);
		Assert.Same(deserialized[0], deserialized[1]);

		// Do it again, across deserializations.
		string[]? deserialized2 = this.Roundtrip<string[], Witness>(["a", "a"]);
		Assert.NotNull(deserialized2);
		Assert.Same(deserialized[0], deserialized2[0]);
	}

	[Fact]
	public void Null() => this.Roundtrip<string, Witness>(null);

	[Fact]
	public void Empty() => this.Roundtrip<string, Witness>(string.Empty);

	[Fact]
	public void VeryLargeString() => this.Roundtrip<string, Witness>(new string('a', 100_000));

	[Fact]
	public void Fragmented()
	{
		ReadOnlyMemory<byte> buffer = this.Serializer.Serialize<string, Witness>("abc", TestContext.Current.CancellationToken);
		Sequence<byte> seq = new();
		seq.Append(buffer[..^1]);
		seq.Append(buffer[^1..]);
		string? deserialized = this.Serializer.Deserialize<string, Witness>(seq, TestContext.Current.CancellationToken);
		Assert.Equal("abc", deserialized);
	}

	[GenerateShape<string>]
	[GenerateShape<string[]>]
	private partial class Witness;
}
