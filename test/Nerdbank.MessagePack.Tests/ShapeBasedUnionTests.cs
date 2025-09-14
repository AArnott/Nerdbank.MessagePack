// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public partial class ShapeBasedUnionTests : MessagePackSerializerTestBase
{
    [Fact]
    public void RequiredPropertyDistinction_BasicTest()
    {
        // Create some test types with distinguishing required properties
        ITypeShape animalShape = Witness.GeneratedTypeShapeProvider.GetTypeShape<Animal>();
        ITypeShape dogShape = Witness.GeneratedTypeShapeProvider.GetTypeShape<Dog>();
        ITypeShape catShape = Witness.GeneratedTypeShapeProvider.GetTypeShape<Cat>();

        var typeShapes = new List<ITypeShape> { animalShape, dogShape, catShape };

        // Test the analyzer
        ShapeBasedUnionAnalyzer.ShapeBasedUnionMapping? mapping = ShapeBasedUnionAnalyzer.AnalyzeShapes(typeShapes);
        
        Assert.NotNull(mapping);
        Assert.NotEmpty(mapping.Steps);
    }

    [Fact]
    public void CreateShapeBasedUnionConverter_ReturnsNull_WhenNoDistinguishingCharacteristics()
    {
        // Create types that are identical in structure
        ITypeShape shape1 = Witness.GeneratedTypeShapeProvider.GetTypeShape<IdenticalType1>();
        ITypeShape shape2 = Witness.GeneratedTypeShapeProvider.GetTypeShape<IdenticalType2>();

        var typeShapes = new List<ITypeShape> { shape1, shape2 };

        MessagePackConverter<object>? converter = this.Serializer.CreateShapeBasedUnionConverter<object>(typeShapes, Witness.GeneratedTypeShapeProvider);
        
        Assert.Null(converter);
    }

    [Fact]
    public void RequiredPropertyDistinction_Roundtrip()
    {
        // Create a converter for types with distinguishing properties
        ITypeShape dogShape = Witness.GeneratedTypeShapeProvider.GetTypeShape<Dog>();
        ITypeShape catShape = Witness.GeneratedTypeShapeProvider.GetTypeShape<Cat>();

        var typeShapes = new List<ITypeShape> { dogShape, catShape };

        MessagePackConverter<Animal>? converter = this.Serializer.CreateShapeBasedUnionConverter<Animal>(typeShapes, Witness.GeneratedTypeShapeProvider);
        
        Assert.NotNull(converter);

        // Test serialization and deserialization with shape-based detection
        this.Serializer = this.Serializer with { Converters = [converter] };

        Dog originalDog = new() { Name = "Buddy", BarkVolume = 5 };
        Animal deserializedAnimal = this.Roundtrip<Animal>(originalDog);
        
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
    public partial record IdenticalType1
    {
        public string CommonProperty { get; init; } = string.Empty;
    }

    [GenerateShape]
    public partial record IdenticalType2
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