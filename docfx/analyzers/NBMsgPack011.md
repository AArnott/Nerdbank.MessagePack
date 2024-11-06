# NBMsgPack010: `[KnownSubType]` alias must be unique

@Nerdbank.MessagePack.KnownSubTypeAttribute should specify an alias that is unique within the scope of the type it is applied to.

Learn more about [subtype unions](../docs/unions.md).

## Example violations

```cs
[KnownSubType(1, typeof(DerivedType1))]
[KnownSubType(1, typeof(DerivedType2))] // Reused an alias
class BaseType
{
}

class DerivedType1 : BaseType
{
}

class DerivedType2 : BaseType
{
}
```

## Resolution

Change the alias to one that has not yet been used.

```cs
[KnownSubType(1, typeof(DerivedType1))]
[KnownSubType(2, typeof(DerivedType2))]
class BaseType
{
}

class DerivedType1 : BaseType
{
}

class DerivedType2 : BaseType
{
}
```

## Additional examples

Note that across types, the alias does not need to be unique.
The following is perfectly valid:

```cs
[KnownSubType(1, typeof(DerivedFromBaseType))]
class BaseType
{
}

[KnownSubType(1, typeof(DerivedFromAnotherType))]
class AnotherType
{
}
```
