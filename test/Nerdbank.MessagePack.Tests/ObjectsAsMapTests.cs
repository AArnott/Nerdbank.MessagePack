// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public partial class ObjectsAsMapTests(ITestOutputHelper logger) : MessagePackSerializerTestBase(logger)
{
	[Fact]
	public void PropertyWithAlteredName()
	{
		Person person = new Person { FirstName = "Andrew", LastName = "Arnott" };
		Sequence<byte> buffer = new();
		this.Serializer.Serialize(buffer, person);
		this.LogMsgPack(buffer);

		MessagePackReader reader = new(buffer);
		Assert.Equal(2, reader.ReadMapHeader());
		Assert.Equal("first_name", reader.ReadString());
		Assert.Equal("Andrew", reader.ReadString());
		Assert.Equal("last_name", reader.ReadString());
		Assert.Equal("Arnott", reader.ReadString());

		Assert.Equal(person, this.Serializer.Deserialize<Person>(buffer));
	}

	[GenerateShape]
	public partial record Person
	{
		[PropertyShape(Name = "first_name")]
		public required string FirstName { get; init; }

		[PropertyShape(Name = "last_name")]
		public required string LastName { get; init; }
	}
}
