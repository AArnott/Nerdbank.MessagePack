# NBMsgPack012: `[KnownSubType]` type must be unique

@Nerdbank.MessagePack.KnownSubTypeAttribute should specify a type that is unique within the scope of the type it is applied to.

Learn more about [subtype unions](../docs/unions.md).

## Example violations

```cs
[KnownSubType(1, typeof(DerivedType))]
[KnownSubType(2, typeof(DerivedType))] // assigned second alias to a subtype
class BaseType
{
}

class DerivedType : BaseType
{
}
```

## Resolution

Remove the extra alias or assign it to a different type.

```cs
[KnownSubType(1, typeof(DerivedType))]
class BaseType
{
}

class DerivedType : BaseType
{
}
```
