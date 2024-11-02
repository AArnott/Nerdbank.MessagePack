// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public partial class ObjectsAsArraysTests(ITestOutputHelper logger) : MessagePackSerializerTestBase(logger)
{
	[Fact]
	public void Person_Roundtrip() => this.AssertRoundtrip(new Person { FirstName = "Andrew", LastName = "Arnott" });

	[Fact]
	public void PersonWithDefaultConstructor_Roundtrip() => this.AssertRoundtrip(new PersonWithDefaultConstructor { FirstName = "Andrew", LastName = "Arnott" });

	[Fact]
	public void Null() => this.AssertRoundtrip<Person>(null);

	[Fact]
	public void Null_DefaultCtro() => this.AssertRoundtrip<PersonWithDefaultConstructor>(null);

	[Fact]
	public void Person_SerializesAsArray()
	{
		Person person = new Person { FirstName = "Andrew", LastName = "Arnott" };
		Sequence<byte> buffer = new();
		this.Serializer.Serialize(buffer, person);
		this.LogMsgPack(buffer);

		MessagePackReader reader = new(buffer);
		Assert.Equal(3, reader.ReadArrayHeader());
		Assert.Equal("Andrew", reader.ReadString());
		Assert.True(reader.TryReadNil());
		Assert.Equal("Arnott", reader.ReadString());
		Assert.True(reader.End);

		Person? deserialized = this.Serializer.Deserialize<Person>(buffer);
		Assert.Equal(person, deserialized);
	}

	[GenerateShape]
	public partial record Person
	{
		[Key(0)]
		public required string FirstName { get; init; }

		[Key(2)] // Deliberately leave an empty spot.
		public required string LastName { get; init; }
	}

	[GenerateShape]
	public partial record PersonWithDefaultConstructor
	{
		[Key(0)]
		public string? FirstName { get; set; }

		[Key(2)] // Deliberately leave an empty spot.
		public string? LastName { get; set; }
	}
}
