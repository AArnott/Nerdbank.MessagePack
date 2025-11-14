// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable CS0618 // Type or member is obsolete -- remove this line after https://github.com/AArnott/Nerdbank.MessagePack/pull/771 merges

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
        public Dictionary<CustomType, string> Dictionary { get; } = new(StructuralEqualityComparer.GetHashCollisionResistant<CustomType>());

        public HashSet<CustomType> HashSet { get; } = new(StructuralEqualityComparer.GetHashCollisionResistant<CustomType>());
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
        public Dictionary<CustomType, string> Dictionary { get; } = new(StructuralEqualityComparer.GetHashCollisionResistantSourceGenerated<CustomType>());

        public HashSet<CustomType> HashSet { get; } = new(StructuralEqualityComparer.GetHashCollisionResistantSourceGenerated<CustomType>());
    }

    [GenerateShape]
    public partial class CustomType
    {
        // Whatever members you want. Make them public or attribute with [PropertyShape]
        // to include them in the hash and equality checks as part of the dictionary keys.
    }
    #endregion
#endif
}
