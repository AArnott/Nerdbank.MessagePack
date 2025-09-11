// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    #region Witness
    // This class declared in another assembly, unattributed and outside of your control.
    public class FamilyTree
    {
    }

    // Within your own assembly, define a 'witness' class with one or more shapes generated for external types.
    [GenerateShapeFor<FamilyTree>]
    partial class Witness;

    void Serialize()
    {
        var familyTree = new FamilyTree();
        var serializer = new MessagePackSerializer();

        // Serialize the FamilyTree instance using the shape generated from your witness class.
        byte[] msgpack = serializer.Serialize<FamilyTree, Witness>(familyTree);
    }
    #endregion
}

#pragma warning disable NBMsgPack051 // We're deliberately using the less preferred pattern.

class ReflectionShapeProvider
{
    #region SerializeUnshapedType
    void SerializeUnshapedType()
    {
        Person person = new("Andrew", "Arnott");

        MessagePackSerializer serializer = new();
        byte[] msgpack = serializer.Serialize(person, ReflectionTypeShapeProvider.Default);
        Person? deserialized = serializer.Deserialize<Person>(msgpack, ReflectionTypeShapeProvider.Default);
    }

    record Person(string FirstName, string LastName);
    #endregion
}

#pragma warning restore NBMsgPack051 // We're deliberately using the less preferred pattern.
