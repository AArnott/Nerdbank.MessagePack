// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Nerdbank.MessagePack;
using PolyType.ReflectionProvider;

partial class SourceGenerated
{
    #region NaturallyAttributed
    [GenerateShape]
    public partial record Tree(string Name, Fruit[] Fruits);

    public record Fruit(double Weight, string Color);
    #endregion
}

partial class WitnessGenerated
{
#if NET
    #region WitnessNET
    // This class declared in another assembly, unattributed and outside of your control.
    public class FamilyTree
    {
    }

    // Within your own assembly, define a 'witness' class with one or more shapes generated for external types.
    [GenerateShape<FamilyTree>]
    partial class Witness;

    void Serialize()
    {
        var familyTree = new FamilyTree();
        var serializer = new MessagePackSerializer();

        // Serialize the FamilyTree instance using the shape generated from your witness class.
        byte[] msgpack = serializer.Serialize<FamilyTree, Witness>(familyTree);
    }
    #endregion
#else
    #region WitnessNETFX
    // This class declared in another assembly, unattributed and outside of your control.
    public class FamilyTree
    {
    }

    // Within your own assembly, define a 'witness' class with one or more shapes generated for external types.
    [GenerateShape<FamilyTree>]
    partial class Witness;

    void Serialize()
    {
        var familyTree = new FamilyTree();
        var serializer = new MessagePackSerializer();

        // Serialize the FamilyTree instance using the shape generated from your witness class.
        byte[] msgpack = serializer.Serialize<FamilyTree>(familyTree, Witness.ShapeProvider);
    }
    #endregion
#endif
}

class ReflectionShapeProvider
{
    #region SerializeUnshapedType
    void SerializeUnshapedType()
    {
        Person person = new("Andrew", "Arnott");

        ITypeShape<Person> shape = ReflectionTypeShapeProvider.Default.GetShape<Person>();
        MessagePackSerializer serializer = new();

        byte[] msgpack = serializer.Serialize(person, shape);
        Person? deserialized = serializer.Deserialize(msgpack, shape);
    }

    record Person(string FirstName, string LastName);
    #endregion
}
