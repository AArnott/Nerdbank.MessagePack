// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Sample1
{
    class LossyFarm
    {
        #region LossyFarm
        public class Farm
        {
            public List<Animal> Animals { get; set; } = [];
        }

        public record Animal(string Name);
        public record Cow(string Name, int Weight) : Animal(Name);
        public record Horse(string Name, int Speed) : Animal(Name);
        public record Dog(string Name, string Color) : Animal(Name);
        #endregion
    }

    class RoundtrippingFarmAnimal
    {
        #region RoundtrippingFarmAnimal
        [DerivedTypeShape(typeof(Cow))]
        [DerivedTypeShape(typeof(Horse))]
        [DerivedTypeShape(typeof(Dog))]
        public record Animal(string Name);
        #endregion

        public record Cow(string Name, int Weight) : Animal(Name);
        public record Horse(string Name, int Speed) : Animal(Name);
        public record Dog(string Name, string Color) : Animal(Name);

        public class Farm
        {
            public List<Animal> Animals { get; set; } = [];
        }
    }

    class HorsePenWrapper
    {
        public class Horse
        {
            public string? Name { get; set; }

            public int Speed { get; set; }
        }

        #region HorsePen
        public class HorsePen
        {
            public List<Horse>? Horses { get; set; }
        }
        #endregion
    }

    class HorseBreeds
    {
        public record Animal;

        #region HorseBreeds
        [DerivedTypeShape(typeof(QuarterHorse))]
        [DerivedTypeShape(typeof(Thoroughbred))]
        public record Horse(string Name, int Speed) : Animal;

        public record QuarterHorse(string Name, int Speed) : Horse(Name, Speed);
        public record Thoroughbred(string Name, int Speed) : Horse(Name, Speed);
        #endregion

        public class BreedPen
        {
            public List<Horse> Horses { get; set; } = [];
        }
    }

    class FlattenedAnimal
    {
        #region FlattenedAnimal
        [DerivedTypeShape(typeof(Cow))]
        [DerivedTypeShape(typeof(Dog))]
        [DerivedTypeShape(typeof(Horse))]
        [DerivedTypeShape(typeof(QuarterHorse))]
        [DerivedTypeShape(typeof(Thoroughbred))]
        public record Animal(string Name);
        #endregion

        public record Cow(string Name, int Weight) : Animal(Name);
        public record Dog(string Name, string Color) : Animal(Name);
        public record Horse(string Name) : Animal(Name);
        public record QuarterHorse(string Name, int Speed) : Horse(Name);
        public record Thoroughbred(string Name, int Speed) : Horse(Name);

        public class Farm
        {
            public List<Animal> Animals { get; set; } = [];
        }
    }
}

namespace GenericSubTypes
{
    #region ClosedGenericSubTypes
    [GenerateShape]
    [DerivedTypeShape(typeof(Horse))]
    [DerivedTypeShape(typeof(Cow<SolidHoof>), Name = "SolidHoofedCow")]
    [DerivedTypeShape(typeof(Cow<ClovenHoof>), Name = "ClovenHoofedCow")]
    partial record Animal(string Name);

    record Horse(string Name) : Animal(Name);

    record Cow<THoof>(string Name, THoof Hoof) : Animal(Name);
    record SolidHoof;
    record ClovenHoof;
    #endregion

    #region OpenGenericSubTypes
    [DerivedTypeShape(typeof(Derived<>), Tag = 1)]
    record Base<T>;

    record Derived<T>(T Value) : Base<T>;

    [GenerateShapeFor<Base<int>>]
    partial class Witness;
    #endregion
}

namespace StringAliasTypes
{
    #region StringAliasTypes
    [DerivedTypeShape(typeof(Horse), Name = "H")]
    [DerivedTypeShape(typeof(Cow), Name = "C")]
    record Animal(string Name);

    record Horse(string Name) : Animal(Name);
    record Cow(string Name) : Animal(Name);
    #endregion
}

namespace IntAliasTypes
{
    #region IntAliasTypes
    [DerivedTypeShape(typeof(Horse), Tag = 1)]
    [DerivedTypeShape(typeof(Cow), Tag = 2)]
    record Animal(string Name);

    record Horse(string Name) : Animal(Name);
    record Cow(string Name) : Animal(Name);
    #endregion
}

namespace MixedAliasTypes
{
    #region MixedAliasTypes
    [DerivedTypeShape(typeof(Horse), Tag = 3)]
    [DerivedTypeShape(typeof(Cow), Name = "Cow")]
    record Animal(string Name);

    record Horse(string Name) : Animal(Name);
    record Cow(string Name) : Animal(Name);
    #endregion
}

namespace RuntimeSubTypes
{
#if NET
    #region RuntimeSubTypesNET
    record Animal(string Name);

    [GenerateShape]
    partial record Horse(string Name) : Animal(Name);

    [GenerateShape]
    partial record Cow(string Name) : Animal(Name);

    class SerializationConfigurator
    {
        internal MessagePackSerializer ConfigureAnimalsMapping(MessagePackSerializer serializer)
        {
            DerivedShapeMapping<Animal> mapping = new();
            mapping.Add<Horse>(1);
            mapping.Add<Cow>(2);

            return serializer with { DerivedTypeUnions = [.. serializer.DerivedTypeUnions, mapping] };
        }
    }
    #endregion
#else
    #region RuntimeSubTypesNETFX
    record Animal(string Name);

    [GenerateShape]
    partial record Horse(string Name) : Animal(Name);

    [GenerateShape]
    partial record Cow(string Name) : Animal(Name);

    class SerializationConfigurator
    {
        internal MessagePackSerializer ConfigureAnimalsMapping(MessagePackSerializer serializer)
        {
            DerivedShapeMapping<Animal> mapping = new();
            mapping.AddSourceGenerated<Horse>(1);
            mapping.AddSourceGenerated<Cow>(2);

            return serializer with { DerivedTypeUnions = [.. serializer.DerivedTypeUnions, mapping] };
        }
    }
    #endregion
#endif

    partial class UseDiscriminatorObjectsSample
    {
        #region UseDiscriminatorObjects
        [GenerateShape]
        [DerivedTypeShape(typeof(A), Name = "A")]
        [DerivedTypeShape(typeof(B), Name = "B")]
        public partial record Base;

        [GenerateShape]
        public partial record A(int ValueA) : Base;

        [GenerateShape]
        public partial record B(int ValueB) : Base;

        void SerializeWithObjectFormat()
        {
            // Create a serializer with UseDiscriminatorObjects enabled
            var serializer = new MessagePackSerializer
            {
                UseDiscriminatorObjects = true,
            };

            Base value = new A(1);

            // Serializes as: {"A":{"ValueA":1}}
            // Instead of default: ["A",{"ValueA":1}]
            byte[] msgpack = serializer.Serialize(value, CancellationToken.None);
        }
        #endregion
    }

    class RuntimeSubTypesDisabler
    {
        #region RuntimeSubTypesDisabled
        MessagePackSerializer CreateSerializer()
        {
            return new MessagePackSerializer
            {
                DerivedTypeUnions = [DerivedTypeUnion.CreateDisabled(typeof(Animal))],
            };
        }
        #endregion
    }
}

namespace MultiLevelAnimals
{
    #region MultiLevel
    [DerivedTypeShape(typeof(Cow))]
    [DerivedTypeShape(typeof(Horse))]
    [DerivedTypeShape(typeof(Dog))]
    public record Animal(string Name);

    public record Cow(string Name, int Weight) : Animal(Name);
    public record Dog(string Name, string Color) : Animal(Name);

    [DerivedTypeShape(typeof(QuarterHorse))]
    [DerivedTypeShape(typeof(Thoroughbred))]
    public record Horse(string Name) : Animal(Name);

    public record QuarterHorse(string Name, int Speed) : Horse(Name);
    public record Thoroughbred(string Name, int Speed) : Horse(Name);

    public class Farm
    {
        public List<Animal> Animals { get; set; } = [];
    }
    #endregion
}

namespace DuckTyping
{
    #region DuckTyping
#pragma warning disable DuckTyping // Experimental API

    [GenerateShape]
    public abstract partial record Animal(string Name);

    [GenerateShape]
    public partial record Dog(string Name, int BarkVolume) : Animal(Name);

    [GenerateShape]
    public partial record Cat(string Name, int MeowPitch) : Animal(Name);

    public class AnimalShelter
    {
        public MessagePackSerializer Serializer { get; } = new()
        {
            DerivedTypeUnions = [
                new DerivedTypeDuckTyping(
                    TypeShapeResolver.ResolveDynamicOrThrow<Animal>(),
                    TypeShapeResolver.ResolveDynamicOrThrow<Dog>(),
                    TypeShapeResolver.ResolveDynamicOrThrow<Cat>()),
            ],
        };
    }
    #endregion

    #region OutOfOrderDiscriminator
    public class TrustedAnimalShelter
    {
        public MessagePackSerializer Serializer { get; } = new()
        {
            // A discriminator that is not the first property buffers the whole union value.
            AllowOutOfOrderDiscriminator = true,
        };
    }
    #endregion
}

#if NET11_0_OR_GREATER
namespace InferredFarmAnimals
{
    #region InferredFarmAnimal
    [GenerateShape]
    public partial class Farm
    {
        public List<Animal> Animals { get; set; } = [];
    }

    [TypeShape(InferClosedTypePolymorphism = true)]
    public closed record Animal(string Name);

    public sealed record Cow(string Name, int Weight) : Animal(Name);
    public sealed record Horse(string Name, int Speed) : Animal(Name);
    public sealed record Dog(string Name, string Color) : Animal(Name);
    #endregion
}

namespace CSharpUnions
{
    #region CSharpUnion
    [GenerateShape]
    public partial union Pet(Cat, Dog);

    public record Cat(string Name, int MeowPitch);

    public record Dog(string Name, int BarkVolume);
    #endregion

    class Usage
    {
        void Roundtrip()
        {
            #region CSharpUnionUsage
            MessagePackSerializer serializer = new();
            Pet pet = new Dog("Rover", 10);
            byte[] msgpack = serializer.Serialize(pet);

            // Prints: ["Dog",{"Name":"Rover","BarkVolume":10}]
            Console.WriteLine(serializer.ConvertToJson(msgpack));

            Pet deserialized = serializer.Deserialize<Pet>(msgpack);
            if (deserialized is Dog dog)
            {
                Console.WriteLine($"{dog.Name} barks at volume {dog.BarkVolume}.");
            }
            #endregion
        }
    }

    #region DocumentChange
    [GenerateShape]
    public partial union DocumentChange(CreateFile, TextDocumentEdit);

    public record CreateFile(string Uri)
    {
        [UnionDiscriminator("create")]
        public string Kind => "create";
    }

    public record TextDocumentEdit(TextDocument TextDocument, TextEdit[] Edits);

    public record TextDocument(string Uri);

    public record TextEdit(string NewText);
    #endregion
}
#endif

namespace UnionSampleOutput
{
    internal static class Graphs
    {
        public static Sample1.LossyFarm.Farm LossyFarm() => new()
        {
            Animals =
            [
                new Sample1.LossyFarm.Cow("Bessie", 1400),
                new Sample1.LossyFarm.Horse("Lighting", 45),
                new Sample1.LossyFarm.Dog("Rover", "Brown"),
            ],
        };

        public static Sample1.RoundtrippingFarmAnimal.Farm RoundtrippingFarm() => new()
        {
            Animals =
            [
                new Sample1.RoundtrippingFarmAnimal.Cow("Bessie", 1400),
                new Sample1.RoundtrippingFarmAnimal.Horse("Lighting", 45),
                new Sample1.RoundtrippingFarmAnimal.Dog("Rover", "Brown"),
            ],
        };

        public static Sample1.HorsePenWrapper.HorsePen HorsePen() => new()
        {
            Horses =
            [
                new Sample1.HorsePenWrapper.Horse { Name = "Lighting", Speed = 45 },
                new Sample1.HorsePenWrapper.Horse { Name = "Flash", Speed = 48 },
            ],
        };

        public static Sample1.HorseBreeds.BreedPen HorseBreeds() => new()
        {
            Horses =
            [
                new Sample1.HorseBreeds.QuarterHorse("Lighting", 45),
                new Sample1.HorseBreeds.Thoroughbred("Flash", 48),
            ],
        };

        public static MultiLevelAnimals.Farm MultiLevel() => new()
        {
            Animals =
            [
                new MultiLevelAnimals.Cow("Bessie", 1400),
                new MultiLevelAnimals.QuarterHorse("Lighting", 45),
                new MultiLevelAnimals.Thoroughbred("Flash", 48),
                new MultiLevelAnimals.Dog("Rover", "Brown"),
            ],
        };

        public static Sample1.FlattenedAnimal.Farm FlattenedAnimal() => new()
        {
            Animals =
            [
                new Sample1.FlattenedAnimal.Cow("Bessie", 1400),
                new Sample1.FlattenedAnimal.QuarterHorse("Lighting", 45),
                new Sample1.FlattenedAnimal.Thoroughbred("Flash", 48),
                new Sample1.FlattenedAnimal.Dog("Rover", "Brown"),
            ],
        };

        public static DuckTyping.Dog Dog() => new("Rover", 10);

        public static DuckTyping.Cat Cat() => new("Whiskers", 13);

        public static RuntimeSubTypes.UseDiscriminatorObjectsSample.A DiscriminatorObject() => new(1);

#if NET11_0_OR_GREATER
        public static CSharpUnions.DocumentChange DocumentChangeCreate() => new CSharpUnions.CreateFile("file:///a.cs");

        public static CSharpUnions.DocumentChange DocumentChangeEdit() => new CSharpUnions.TextDocumentEdit(new("file:///a.cs"), []);
#endif
    }
}
