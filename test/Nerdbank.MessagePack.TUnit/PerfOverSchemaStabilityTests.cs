// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

[Property("PerfOverSchemaStability", "true")]
public partial class PerfOverSchemaStabilityTests : MessagePackSerializerTestBase
{
	public PerfOverSchemaStabilityTests()
	{
		this.Serializer = this.Serializer with { PerfOverSchemaStability = true };
	}

	[Test]
	public void ObjectMapBecomesArray()
	{
		ReadOnlySequence<byte> msgpack = this.AssertRoundtrip(new RecordWithoutKeyAttributes("Andrew", 99));
		MessagePackReader reader = new(msgpack);
		Assert.Equal(2, reader.ReadArrayHeader());
	}

	[Test]
	public void DerivedTypeIdentifierIsInt()
	{
		ReadOnlySequence<byte> msgpack = this.AssertRoundtrip<Animal>(new Horse("Andrew", 99));
		MessagePackReader reader = new(msgpack);

		// Read the header for the array that includes the type identifier, and the value.
		Assert.Equal(2, reader.ReadArrayHeader());

		// Ensure the type identifier is an integer.
		Assert.Equal(0, reader.ReadInt32());

		// May as well verify that the value itself is also an array.
		Assert.Equal(2, reader.ReadArrayHeader());

		// Verify that other derived types can similarly be distinguished.
		// We assume that the important assertions made earlier hold for all derived types.
		this.AssertRoundtrip<Animal>(new Dog("Rover", "Red"));
	}

	[GenerateShape]
	internal partial record RecordWithoutKeyAttributes(string Name, int Age);

	[GenerateShape]
	[DerivedTypeShape(typeof(Horse))]
	[DerivedTypeShape(typeof(Dog))]
	internal partial record Animal(string Name);

	internal record Horse(string Name, int Speed) : Animal(Name);

	internal record Dog(string Name, string Color) : Animal(Name);
}
