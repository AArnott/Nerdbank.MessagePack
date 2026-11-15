# NBMsgPack133: Union cases must share a discriminator name and distinct values

Cases in one union that declare a discriminator share one serialized name, or one <xref:Nerdbank.MessagePack.KeyAttribute> index, and use distinct values.
Name-keyed cases and keyed cases are separate groups.
A case without a discriminator is allowed.
Names are compared as written. The property naming policy is not applied.

## Example violation

```cs
[DerivedTypeShape(typeof(Create))]
[DerivedTypeShape(typeof(Delete))]
partial class Change;

partial class Create
{
    [UnionDiscriminator("create")]
    public string Kind => "create";
}

partial class Delete
{
    [UnionDiscriminator("create")]
    public string Kind => "create";
}
```

## Resolution

Give the cases distinct values, and the same name or the same key index.
