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

    #region SecureEqualityComparers
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
}
