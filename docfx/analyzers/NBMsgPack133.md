# NBMsgPack133: Union cases must share a discriminator name and distinct values

Cases in one union that declare a discriminator share one serialized name, or one <xref:Nerdbank.MessagePack.KeyAttribute> index, and use distinct values.
Name-keyed cases and keyed cases are separate groups.
Two enums collide when they are the same value of one enum type.
Enums of different types collide when they share a member name and a numeric value, or when they share a numeric value and neither has a name.
Different member names stay distinct, because the enums may be serialized by name.
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
