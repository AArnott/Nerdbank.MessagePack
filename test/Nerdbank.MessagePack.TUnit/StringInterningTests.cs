// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public partial class StringInterningTests : MessagePackSerializerTestBase
{
	public StringInterningTests()
	{
		this.Serializer = this.Serializer with { InternStrings = true };
	}

	[Test]
	public void InternStringsDefault() => Assert.False(new MessagePackSerializer().InternStrings);

	[Test]
	public void NoInterning()
	{
		this.Serializer = this.Serializer with { InternStrings = false };
		string[]? deserialized = this.Roundtrip<string[], Witness>(["a", "a"]);
		Assert.NotNull(deserialized);
		Assert.NotSame(deserialized[0], deserialized[1]);
	}

	[Test]
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

	[Test]
	public void Null() => this.Roundtrip<string, Witness>(null);

	[Test]
	public void Empty() => this.Roundtrip<string, Witness>(string.Empty);

	[Test]
	public void VeryLargeString() => this.Roundtrip<string, Witness>(new string('a', 100_000));

	[Test]
	public void Fragmented()
	{
		ReadOnlyMemory<byte> buffer = this.Serializer.Serialize<string, Witness>("abc", this.TimeoutToken);
		Sequence<byte> seq = new();
		seq.Append(buffer[..^1]);
		seq.Append(buffer[^1..]);
		string? deserialized = this.Serializer.Deserialize<string, Witness>(seq, this.TimeoutToken);
		Assert.Equal("abc", deserialized);
	}

	[GenerateShapeFor<string>]
	[GenerateShapeFor<string[]>]
	private partial class Witness;
}
