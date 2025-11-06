// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

[Trait("IgnoreKeyAttributes", "true")]
public partial class IgnoreKeyAttributesTests : MessagePackSerializerTestBase
{
	[Fact]
	public void ObjectWithKeyAttributesBecomesMap()
	{
		this.Serializer = this.Serializer with { IgnoreKeyAttributes = true };
		ReadOnlySequence<byte> msgpack = this.AssertRoundtrip(new RecordWithKeyAttributes("Andrew", 99));
		MessagePackReader reader = new(msgpack);

		// Should be a map (not an array) because we're ignoring KeyAttributes
		Assert.Equal(2, reader.ReadMapHeader());
	}

	[Fact]
	public void ObjectWithKeyAttributesBecomesArray_WhenIgnoreKeyAttributesIsFalse()
	{
		this.Serializer = this.Serializer with { IgnoreKeyAttributes = false };
		ReadOnlySequence<byte> msgpack = this.AssertRoundtrip(new RecordWithKeyAttributes("Andrew", 99));
		MessagePackReader reader = new(msgpack);

		// Should be an array because KeyAttributes are respected
		Assert.Equal(2, reader.ReadArrayHeader());
	}

	[Fact]
	public void IgnoreKeyAttributes_TakesPrecedenceOverPerfOverSchemaStability()
	{
		// When both are true, IgnoreKeyAttributes should win and produce maps
		this.Serializer = this.Serializer with
		{
			IgnoreKeyAttributes = true,
			PerfOverSchemaStability = true,
		};
		ReadOnlySequence<byte> msgpack = this.AssertRoundtrip(new RecordWithKeyAttributes("Andrew", 99));
		MessagePackReader reader = new(msgpack);

		// Should be a map because IgnoreKeyAttributes takes precedence
		Assert.Equal(2, reader.ReadMapHeader());
	}

	[Fact]
	public void ObjectWithoutKeyAttributes_NotAffectedByIgnoreKeyAttributes()
	{
		this.Serializer = this.Serializer with { IgnoreKeyAttributes = true };
		ReadOnlySequence<byte> msgpack = this.AssertRoundtrip(new RecordWithoutKeyAttributes("Andrew", 99));
		MessagePackReader reader = new(msgpack);

		// Should be a map because the type doesn't have KeyAttributes anyway
		Assert.Equal(2, reader.ReadMapHeader());
	}

	[Fact]
	public void ComplexObject_WithNestedKeyAttributes()
	{
		this.Serializer = this.Serializer with { IgnoreKeyAttributes = true };
		ComplexRecord original = new(
			new RecordWithKeyAttributes("Alice", 30),
			new RecordWithKeyAttributes("Bob", 40));

		ReadOnlySequence<byte> msgpack = this.AssertRoundtrip(original);
		MessagePackReader reader = new(msgpack);

		// Outer object should be a map
		Assert.Equal(2, reader.ReadMapHeader());
	}

	[GenerateShape]
	internal partial record RecordWithKeyAttributes(
		[property: Key(0)] string Name,
		[property: Key(1)] int Age);

	[GenerateShape]
	internal partial record RecordWithoutKeyAttributes(string Name, int Age);

	[GenerateShape]
	internal partial record ComplexRecord(
		RecordWithKeyAttributes Person1,
		RecordWithKeyAttributes Person2);
}
