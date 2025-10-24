# NBMsgPack012: `[DerivedTypeShapeAttribute]` type must be unique

@Nerdbank.MessagePack.DerivedTypeShapeAttribute should specify a type that is unique within the scope of the type it is applied to.

Learn more about [subtype unions](../docs/unions.md).

## Example violations

```cs
[DerivedTypeShapeAttribute<DerivedType>(1)]
[DerivedTypeShapeAttribute<DerivedType>(2)] // assigned second alias to a subtype
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
[DerivedTypeShapeAttribute<DerivedType>(1)]
class BaseType
{
}

class DerivedType : BaseType
{
}
```