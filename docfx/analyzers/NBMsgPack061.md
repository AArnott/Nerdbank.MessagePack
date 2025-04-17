# NBMsgPack061: `UnusedDataPacket` member should not have a KeyAttribute

When used within a class, @Nerdbank.MessagePack.KeyAttribute must be applied on every serializable property and field.
Learn more about this in the [NBMsgPack001 diagnostic documentation](NBMsgPack001.md).

Properties and fields typed as @Nerdbank.MessagePack.UnusedDataPacket are specially serialized properties and have no designated position in an array.
Learn more about this in [the retaining unrecognized data documentation](../docs/customizing-serialization.md#retaining-unrecognized-data).
As such, @Nerdbank.MessagePack.KeyAttribute should _not_ be applied to these properties.

## Example violation

```cs
public class Person
{
    [Key(0)]
    public required string Name { get; set; }

    [Key(1)] // NBMsgPack061
    [PropertyShape]
    private UnusedDataPacket Extension { get; set; }
}
```

## Resolution

Drop the @Nerdbank.MessagePack.KeyAttribute from the @Nerdbank.MessagePack.UnusedDataPacket property.

```cs
public class Person
{
    [Key(0)]
    public required string Name { get; set; }

    [PropertyShape]
    private UnusedDataPacket Extension { get; set; }
}
```
