# NBMsgPack130: Union discriminator must be a matching constant

<xref:Nerdbank.MessagePack.UnionDiscriminatorAttribute> belongs on a get-only property whose getter returns a constant.
When the attribute supplies a value, the getter must return that same value.

## Example violation

```cs
[UnionDiscriminator("delete")]
public string Kind => "create";
```

## Resolution

Use the getter's constant. The code fix "Use the member's constant" rewrites the attribute argument.
