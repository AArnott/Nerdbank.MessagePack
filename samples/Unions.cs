// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Sample1
{
    class FarmWrapper
    {
        public class Animal;

        #region Farm
        public class Farm
        {
            public List<Animal>? Animals { get; set; }
        }
        #endregion
    }

    class FarmAnimals
    {
        #region FarmAnimals
        [DerivedTypeShape(typeof(Cow), Tag = 1)]
        [DerivedTypeShape(typeof(Horse), Tag = 2)]
        [DerivedTypeShape(typeof(Dog), Tag = 3)]
        public class Animal
        {
            public string? Name { get; set; }
        }

        public class Cow : Animal { }
        public class Horse : Animal { }
        public class Dog : Animal { }
        #endregion
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
        public class Animal;

        #region HorseBreeds
        [DerivedTypeShape(typeof(QuarterHorse), Tag = 1)]
        [DerivedTypeShape(typeof(Thoroughbred), Tag = 2)]
        public partial class Horse : Animal { }

        public class QuarterHorse : Horse { }
        public class Thoroughbred : Horse { }
        #endregion
    }
}

namespace GenericSubTypes
{
    #region ClosedGenericSubTypes
    [DerivedTypeShape(typeof(Horse), Tag = 1)]
    [DerivedTypeShape(typeof(Cow<SolidHoof>), Tag = 2)]
    [DerivedTypeShape(typeof(Cow<ClovenHoof>), Tag = 3)]
    class Animal
    {
        public string? Name { get; set; }
    }

    class Horse : Animal { }

    class Cow<THoof> : Animal { }

    class SolidHoof { }

    class ClovenHoof { }
    #endregion
}

namespace StringAliasTypes
{
    #region StringAliasTypes
    [GenerateShape]
    [DerivedTypeShape(typeof(Horse), Name = "Horse")]
    [DerivedTypeShape(typeof(Cow), Name = "Cow")]
    partial class Animal
    {
        public string? Name { get; set; }
    }

    class Horse : Animal { }

    class Cow : Animal { }
    #endregion
}

namespace MixedAliasTypes
{
    #region MixedAliasTypes
    [GenerateShape]
    [DerivedTypeShape(typeof(Horse), Tag = 1)]
    [DerivedTypeShape(typeof(Cow), Name = "Cow")]
    partial class Animal
    {
        public string? Name { get; set; }
    }

    class Horse : Animal { }

    class Cow : Animal { }
    #endregion
}

namespace InferredAliasTypes
{
    #region InferredAliasTypes
    [GenerateShape]
    [DerivedTypeShape(typeof(Horse))]
    [DerivedTypeShape(typeof(Cow))]
    partial class Animal
    {
        public string? Name { get; set; }
    }

    class Horse : Animal { }

    class Cow : Animal { }
    #endregion
}

namespace RuntimeSubTypes
{
#if NET
    #region RuntimeSubTypesNET
    class Animal
    {
        public string? Name { get; set; }
    }

    [GenerateShape]
    partial class Horse : Animal { }

    [GenerateShape]
    partial class Cow : Animal { }

    class SerializationConfigurator
    {
        internal void ConfigureAnimalsMapping(MessagePackSerializer serializer)
        {
            KnownSubTypeMapping<Animal> mapping = new();
            mapping.Add<Horse>(1);
            mapping.Add<Cow>(2);

            serializer.RegisterKnownSubTypes(mapping);
        }
    }
    #endregion
#else
    #region RuntimeSubTypesNETFX
    class Animal
    {
        public string? Name { get; set; }
    }

    [GenerateShape]
    partial class Horse : Animal { }

    [GenerateShape]
    partial class Cow : Animal { }

    [GenerateShape<Horse>]
    [GenerateShape<Cow>]
    partial class Witness;

    class SerializationConfigurator
    {
        internal void ConfigureAnimalsMapping(MessagePackSerializer serializer)
        {
            KnownSubTypeMapping<Animal> mapping = new();
            mapping.Add<Horse>(1, Witness.ShapeProvider);
            mapping.Add<Cow>(2, Witness.ShapeProvider);

            serializer.RegisterKnownSubTypes(mapping);
        }
    }
    #endregion
#endif
}
