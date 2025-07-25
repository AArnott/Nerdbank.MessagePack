// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

partial class IncludingExcludingMembers
{
    #region IncludingExcludingMembers
    class MyType
    {
        [PropertyShape(Ignore = true)] // exclude this property from serialization
        public string? MyName { get; set; }

        [PropertyShape] // include this non-public property in serialization
        internal string? InternalMember { get; set; }
    }
    #endregion
}

partial class ChangingPropertyNames
{
    #region ChangingPropertyNames
    class MyType
    {
        [PropertyShape(Name = "name")] // serialize this property as "name"
        public string? MyName { get; set; }
    }
    #endregion
}

partial class ChangingEnumValueNames
{
    #region ChangingEnumNames
    enum MyEnum
    {
        [EnumMemberShape(Name = "1st")] // serialize this enum value as "1st"
        One,
        [EnumMemberShape(Name = "2nd")] // serialize this enum value as "2nd"
        Two,
    }
    #endregion
}

partial class ApplyNamePolicy
{
    class MyType
    {
        void Example()
        {
            #region ApplyNamePolicy
            var serializer = new MessagePackSerializer
            {
                PropertyNamingPolicy = MessagePackNamingPolicy.CamelCase,
            };
            #endregion
        }
    }
}

namespace DeserializingConstructors
{
    #region DeserializingConstructors
    [GenerateShape]
    partial class ImmutablePerson
    {
        public ImmutablePerson(string? name)
        {
            this.Name = name;
        }

        public string? Name { get; }
    }
    #endregion
}

namespace DeserializingConstructorsPropertyRenamed
{
    #region DeserializingConstructorsPropertyRenamed
    [GenerateShape]
    partial class ImmutablePerson
    {
        public ImmutablePerson(string? name)
        {
            this.Name = name;
        }

        [PropertyShape(Name = "person_name")]
        public string? Name { get; }
    }
    #endregion
}

namespace SerializeWithKey
{
    #region SerializeWithKey
    [GenerateShape]
    partial class MyType
    {
        [Key(0)]
        public string? OneProperty { get; set; }

        [Key(1)]
        public string? AnotherProperty { get; set; }
    }
    #endregion

    #region SerializeWithKeyAndGaps
    [GenerateShape]
    partial class MyTypeWithGaps
    {
        [Key(0)]
        public string? OneProperty { get; set; }

        [Key(5)]
        public string? AnotherProperty { get; set; }
    }
    #endregion
}

namespace VersionSafeObject
{
    #region VersionSafeObject
    public class Person
    {
        public required string Name { get; set; }

        [PropertyShape]
        private UnusedDataPacket? UnusedData { get; set; }
    }
    #endregion
}

namespace CustomComparerOnMemberViaAttribute
{
    #region CustomComparerOnMemberViaAttribute
    public class Person
    {
        [UseComparer(typeof(StringComparer), nameof(StringComparer.OrdinalIgnoreCase))]
        public Dictionary<string, Person> ChildrenByName { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }
    #endregion
}

namespace CustomComparerOnMemberViaInitializer
{
    #region CustomComparerOnMemberViaInitializer
    public class Person
    {
        public Dictionary<string, Person> ChildrenByName { get; } = new(StringComparer.OrdinalIgnoreCase);
    }
    #endregion
}

namespace CustomComparerOnParameter
{
    #region CustomComparerOnParameter
    public record Person(
        [UseComparer(typeof(StringComparer), nameof(StringComparer.OrdinalIgnoreCase))] Dictionary<string, Person> ChildrenByName);
    #endregion
}
