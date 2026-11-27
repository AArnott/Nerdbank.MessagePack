# NBMsgPack133: Union cases must share a discriminator name and distinct values

Cases in one union that declare a discriminator share one serialized name, or one <xref:Nerdbank.MessagePack.KeyAttribute> index, and use distinct values.
Name-keyed cases and keyed cases are separate groups.
Two enums collide when they would encode as the same number and the same name.
A member discriminator uses the serialized enum name, including an `EnumMemberShape` rename.
A type-level discriminator uses the CLR member name.
Different names stay distinct, because the enums may be serialized by name.
A member with <xref:Nerdbank.MessagePack.MessagePackConverterAttribute> is not compared. The analyzer cannot see the encoded value.
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
