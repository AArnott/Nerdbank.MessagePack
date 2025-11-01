// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Samples;

internal partial class TargetedDeserialization
{
    private readonly MessagePackSerializer serializer = new();

    internal void Demo(byte[] msgpack)
    {
        #region Simple
        // Reach into the msgpack to retrieve the first family member's name,
        // without deserializing the entire structure.
        string? householdHeadName = this.serializer.DeserializePath<Family, string>(msgpack, new(f => f.Members[0].Name));
        #endregion

        #region Nullable
        string? nameOfHeadsFather = this.serializer.DeserializePath<Family, string>(msgpack, new(f => f.Members[0].Father!.Name));
        #endregion
    }

    [GenerateShape]
    public partial class Family
    {
        public List<Person> Members { get; init; } = [];
    }

    public class Person
    {
        public required string Name { get; init; }

        public Person? Father { get; set; }
    }
}
