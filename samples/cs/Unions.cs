// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable CS0618 // Type or member is obsolete -- remove this line after https://github.com/AArnott/Nerdbank.MessagePack/pull/771 merges

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
    }

    class HorsePenWrapper
    {
        public class Horse;

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
        public record Horse : Animal;

        public record QuarterHorse : Horse;
        public record Thoroughbred : Horse;
        #endregion
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
        public record QuarterHorse(string Name) : Horse(Name);
        public record Thoroughbred(string Name) : Horse(Name);
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
}
