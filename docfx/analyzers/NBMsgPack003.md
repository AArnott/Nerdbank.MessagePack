# NBMsgPack003: `[Key]` index must be unique

@Nerdbank.MessagePack.KeyAttribute must have unique indexes provided across all members of a type, including base types.

This is because for a given object, the indexes determine the array index that will contain the value for a property.

## Example violations

Non-unique within a class:

```cs
class MyType
{
    [Key(0)]
    public int Property1 { get; set; }

    [Key(0)]
    public int Property2 { get; set; }
}
```

Non-unique across a type hierarchy:

```cs
class BaseType
{
    [Key(0)]
    public int BaseProperty { get; set; }
}

class DerivedType : BaseType
{
    [Key(0)] // This must be a different index than BaseType.BaseProperty
    public int DerivedProperty { get; set; }
}
```

## Resolution

Reassign indexes so that each member has a unique value assigned.
When `[Key]` is used across a type hierarchy, lower indexes are typically assigned to base types and higher indexes to derived types.
