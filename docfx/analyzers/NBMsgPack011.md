# NBMsgPack011: `[DerivedTypeShape]` alias must be unique

<xref:PolyType.DerivedTypeShapeAttribute> should specify an alias that is unique within the scope of the type it is applied to.

Learn more about [subtype unions](../docs/unions.md).

## Example violations

```cs
[DerivedTypeShape(typeof(DerivedType1), Tag = 1)]
[DerivedTypeShape(typeof(DerivedType2), Tag = 1)] // Reused an alias
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
[DerivedTypeShape(typeof(DerivedType1), Tag = 1)]
[DerivedTypeShape(typeof(DerivedType2), Tag = 2)]
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
[DerivedTypeShape(typeof(DerivedFromBaseType), Tag = 1)]
class BaseType
{
}

[DerivedTypeShape(typeof(DerivedFromAnotherType), Tag = 1)]
class AnotherType
{
}
```
