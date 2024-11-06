# NBMsgPack010: `[KnownSubType]` should specify an assignable type

@Nerdbank.MessagePack.KnownSubTypeAttribute should specify a type that is assignable to the type the attribute is applied to.

## Example violations

```cs
[KnownSubType(typeof(DerivedType))] // DerivedType is not in fact derived from BaseType
class BaseType
{
}

class DerivedType
{
}
```

## Resolution

Remove the attribute or specify a type that is actually derived from the applied type.

In the fixed code below, the `DerivedType` is actually fixed to derive from `BaseType`.

```cs
[KnownSubType(typeof(DerivedType))]
class BaseType
{
}

class DerivedType : BaseType
{
}
```
