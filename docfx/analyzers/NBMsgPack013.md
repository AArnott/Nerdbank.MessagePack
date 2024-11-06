# NBMsgPack013: `[KnownSubType]` type must not be an open generic

@Nerdbank.MessagePack.KnownSubTypeAttribute should specify a type that is either non-generic or a closed generic.

Learn more about [subtype unions](../docs/unions.md).

## Example violations

```cs
[KnownSubType(1, typeof(DerivedType<>))]
class BaseType
{
}

class DerivedType<T> : BaseType
{
}
```

## Resolution

Provide a type argument to close the generic type.
You may specify multiple closed generics of the same generic type, each with a unique alias.

```cs
[KnownSubType(1, typeof(DerivedType<int>))]
[KnownSubType(2, typeof(DerivedType<bool>))]
class BaseType
{
}

class DerivedType<T> : BaseType
{
}
```
