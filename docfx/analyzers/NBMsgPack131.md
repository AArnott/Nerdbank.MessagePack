# NBMsgPack131: Union discriminator must be one get-only member

A type has one union discriminator.
It is a get-only property, or the type-level form `[UnionDiscriminator("kind", "create")]` when the type is a name-keyed map.
A constant field is not part of the object shape.
The member is not a constructor parameter, and its name is not `$type` or `$value`.
A type that uses <xref:Nerdbank.MessagePack.KeyAttribute> on a serialized member must put <xref:Nerdbank.MessagePack.KeyAttribute> on the discriminator member as well.
A member left out of the object shape, including `PropertyShape(Ignore = true)`, does not count.
The type-level form has no index, so it cannot be used on a keyed type.

## Example violation

```cs
[Key(0)]
public string Uri { get; set; }

[UnionDiscriminator("create")]
public string Kind => "create";
```

## Resolution

Add <xref:Nerdbank.MessagePack.KeyAttribute> to `Kind`, or remove `Key` from the type.
