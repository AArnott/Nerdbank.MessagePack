# NBMsgPack134: Key the union discriminator at 0

When a type uses <xref:Nerdbank.MessagePack.KeyAttribute>, `Key(0)` puts the discriminator at the start of the array or int-keyed map.
Another index is valid. The discriminator is still checked at that index.

## Example violation

```cs
[Key(1), UnionDiscriminator("create")]
public string Kind => "create";

[Key(0)]
public string Uri { get; set; }
```

## Resolution

Move the discriminator to `Key(0)` when the protocol can change.
Keep the current index when existing payloads already use it.
