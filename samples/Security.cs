// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

partial class Security
{
    void SetMaxDepth()
    {
        #region SetMaxDepth
        var serializer = new MessagePackSerializer
        {
            StartingContext = new SerializationContext
            {
                MaxDepth = 100,
            },
        };
        #endregion
    }

#if NET
    #region SecureEqualityComparersNET
    [GenerateShape]
    public partial class HashCollisionResistance
    {
        public HashCollisionResistance()
        {
            this.Dictionary = new(ByValueEqualityComparer.GetHashCollisionResistant<CustomType>());
            this.HashSet = new(ByValueEqualityComparer.GetHashCollisionResistant<CustomType>());
        }

        public Dictionary<CustomType, string> Dictionary { get; }

        public HashSet<CustomType> HashSet { get; }
    }

    [GenerateShape]
    public partial class CustomType
    {
        // Whatever members you want. Make them public or attribute with [PropertyShape]
        // to include them in the hash and equality checks as part of the dictionary keys.
    }

    #endregion
#else
    #region SecureEqualityComparersNETFX
    [GenerateShape]
    public partial class HashCollisionResistance
    {
        public HashCollisionResistance()
        {
            this.Dictionary = new(ByValueEqualityComparer.GetHashCollisionResistant<CustomType>(Witness.ShapeProvider));
            this.HashSet = new(ByValueEqualityComparer.GetHashCollisionResistant<CustomType>(Witness.ShapeProvider));
        }

        public Dictionary<CustomType, string> Dictionary { get; }

        public HashSet<CustomType> HashSet { get; }
    }

    [GenerateShape]
    public partial class CustomType
    {
        // Whatever members you want. Make them public or attribute with [PropertyShape]
        // to include them in the hash and equality checks as part of the dictionary keys.
    }

    [GenerateShape<CustomType>]
    internal partial class Witness;
    #endregion
#endif
}
