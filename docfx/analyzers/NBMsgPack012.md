# NBMsgPack012: `[DerivedTypeShape]` type must be unique

<xref:PolyType.DerivedTypeShapeAttribute> should specify a type that is unique within the scope of the type it is applied to.

Learn more about [subtype unions](../docs/unions.md).

## Example violations

```cs
[DerivedTypeShape(typeof(DerivedType), Tag = 1)]
[DerivedTypeShape(typeof(DerivedType), Tag = 2)] // assigned second alias to a subtype
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
[DerivedTypeShape(typeof(DerivedType), Tag = 1)]
class BaseType
{
}

class DerivedType : BaseType
{
}
```
