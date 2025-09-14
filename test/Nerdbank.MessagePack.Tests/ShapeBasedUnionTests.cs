// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public partial class ShapeBasedUnionTests : MessagePackSerializerTestBase
{
	[Fact]
	public void CreateShapeBasedUnionConverter_ReturnsNull_WhenNoDistinguishingCharacteristics()
	{
		// Create types that are identical in structure
		ITypeShape<IdenticalTypeBase> baseShape = Witness.GeneratedTypeShapeProvider.GetTypeShapeOrThrow<IdenticalTypeBase>();
		ITypeShape<IdenticalType1> shape1 = Witness.GeneratedTypeShapeProvider.GetTypeShapeOrThrow<IdenticalType1>();
		ITypeShape<IdenticalType2> shape2 = Witness.GeneratedTypeShapeProvider.GetTypeShapeOrThrow<IdenticalType2>();

		MessagePackConverter<IdenticalTypeBase>? converter = this.Serializer.CreateShapeBasedUnionConverter(baseShape, shape1, shape2);

		Assert.Null(converter);
	}

	[Fact]
	public void RequiredPropertyDistinction_Roundtrip()
	{
		// Create a converter for types with distinguishing properties
		ITypeShape<Animal> animalShape = Witness.GeneratedTypeShapeProvider.GetTypeShapeOrThrow<Animal>();
		ITypeShape dogShape = Witness.GeneratedTypeShapeProvider.GetTypeShapeOrThrow<Dog>();
		ITypeShape catShape = Witness.GeneratedTypeShapeProvider.GetTypeShapeOrThrow<Cat>();

		var typeShapes = new List<ITypeShape> { dogShape, catShape };

		MessagePackConverter<Animal>? converter = this.Serializer.CreateShapeBasedUnionConverter(animalShape, dogShape, catShape);

		Assert.NotNull(converter);

		// Test serialization and deserialization with shape-based detection
		this.Serializer = this.Serializer with { Converters = [converter] };

		Dog originalDog = new() { Name = "Buddy", BarkVolume = 5 };
		Animal? deserializedAnimal = this.Roundtrip<Animal>(originalDog);

		Assert.IsType<Dog>(deserializedAnimal);
		Dog deserializedDog = (Dog)deserializedAnimal;
		Assert.Equal("Buddy", deserializedDog.Name);
		Assert.Equal(5, deserializedDog.BarkVolume);

		Cat originalCat = new() { Name = "Whiskers", MeowPitch = 3 };
		deserializedAnimal = this.Roundtrip<Animal>(originalCat);

		Assert.IsType<Cat>(deserializedAnimal);
		Cat deserializedCat = (Cat)deserializedAnimal;
		Assert.Equal("Whiskers", deserializedCat.Name);
		Assert.Equal(3, deserializedCat.MeowPitch);
	}

	[GenerateShape]
	public partial record Animal
	{
		public string Name { get; init; } = string.Empty;
	}

	[GenerateShape]
	public partial record Dog : Animal
	{
		public int BarkVolume { get; init; }
	}

	[GenerateShape]
	public partial record Cat : Animal
	{
		public int MeowPitch { get; init; }
	}

	[GenerateShape]
	public partial record IdenticalTypeBase;

	[GenerateShape]
	public partial record IdenticalType1 : IdenticalTypeBase
	{
		public string CommonProperty { get; init; } = string.Empty;
	}

	[GenerateShape]
	public partial record IdenticalType2 : IdenticalTypeBase
	{
		public string CommonProperty { get; init; } = string.Empty;
	}

	[GenerateShapeFor<Animal>]
	[GenerateShapeFor<Dog>]
	[GenerateShapeFor<Cat>]
	[GenerateShapeFor<IdenticalType1>]
	[GenerateShapeFor<IdenticalType2>]
	private partial class Witness;
}
