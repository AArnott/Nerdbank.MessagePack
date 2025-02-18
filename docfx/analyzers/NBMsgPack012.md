# NBMsgPack012: `[KnownSubType]` type must be unique

@ShapeShift.KnownSubTypeAttribute should specify a type that is unique within the scope of the type it is applied to.

Learn more about [subtype unions](../docs/unions.md).

## Example violations

```cs
[KnownSubType<DerivedType>(1)]
[KnownSubType<DerivedType>(2)] // assigned second alias to a subtype
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
[KnownSubType<DerivedType>(1)]
class BaseType
{
}

class DerivedType : BaseType
{
}
```
