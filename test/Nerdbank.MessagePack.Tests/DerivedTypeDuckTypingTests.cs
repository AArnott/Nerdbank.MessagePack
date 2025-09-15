// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public partial class DerivedTypeDuckTypingTests : MessagePackSerializerTestBase
{
	[Fact]
	public void CreateShapeBasedUnionConverter_ReturnsNull_WhenNoDistinguishingCharacteristics()
	{
		// Create types that are identical in structure
		ITypeShape<IdenticalTypeBase> baseShape = Witness.GeneratedTypeShapeProvider.GetTypeShapeOrThrow<IdenticalTypeBase>();
		ITypeShape<IdenticalType1> shape1 = Witness.GeneratedTypeShapeProvider.GetTypeShapeOrThrow<IdenticalType1>();
		ITypeShape<IdenticalType2> shape2 = Witness.GeneratedTypeShapeProvider.GetTypeShapeOrThrow<IdenticalType2>();

		Assert.Throws<ArgumentException>(() => new DerivedTypeDuckTyping(baseShape, shape1, shape2));
	}

	[Fact]
	public void RequiredPropertyDistinction_Roundtrip()
	{
		// Create a converter for types with distinguishing properties
		ITypeShape<Animal> animalShape = Witness.GeneratedTypeShapeProvider.GetTypeShapeOrThrow<Animal>();
		ITypeShape dogShape = Witness.GeneratedTypeShapeProvider.GetTypeShapeOrThrow<Dog>();
		ITypeShape catShape = Witness.GeneratedTypeShapeProvider.GetTypeShapeOrThrow<Cat>();

		DerivedTypeDuckTyping duckTyping = new(animalShape, dogShape, catShape);

		// Test serialization and deserialization with shape-based detection
		this.Serializer = this.Serializer with { DerivedTypeUnions = [duckTyping] };

		this.AssertRoundtrip<Animal>(new Dog() { Name = "Buddy", BarkVolume = 5 });
		this.AssertRoundtrip<Animal>(new Cat() { Name = "Whiskers", MeowPitch = 3 });
	}

	[Fact]
	public void OnlyRequiredPropertyPresent_Dog()
	{
		Sequence<byte> seq = new();
		MessagePackWriter writer = new MessagePackWriter(seq);
		writer.WriteMapHeader(1);
		writer.Write(nameof(Dog.BarkVolume));
		writer.Write(10);
		writer.Flush();

		Animal? animal = this.Serializer.Deserialize<Animal>(seq, TestContext.Current.CancellationToken);
		Assert.Equal(new Dog { BarkVolume = 10 }, animal);
	}

	[Fact]
	public void OnlyRequiredPropertyPresent_Cat()
	{
		Sequence<byte> seq = new();
		MessagePackWriter writer = new MessagePackWriter(seq);
		writer.WriteMapHeader(1);
		writer.Write(nameof(Cat.MeowPitch));
		writer.Write(10);
		writer.Flush();

		Animal? animal = this.Serializer.Deserialize<Animal>(seq, TestContext.Current.CancellationToken);
		Assert.Equal(new Cat { MeowPitch = 10 }, animal);
	}

	[GenerateShape]
	public partial record Animal
	{
		public string Name { get; init; } = string.Empty;
	}

	[GenerateShape]
	public partial record Dog : Animal
	{
		public required int BarkVolume { get; init; }
	}

	[GenerateShape]
	public partial record Cat : Animal
	{
		public required int MeowPitch { get; init; }
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
