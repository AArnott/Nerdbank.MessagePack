# NBMsgPack062: `UnusedDataPacket` properties should be private

A data type containing the special @Nerdbank.MessagePack.UnusedDataPacket member should be declared as `private`.

## Example violation

```cs
public class Person
{
    public required string Name { get; set; }

    public UnusedDataPacket Extension { get; set; } // NBMsgPack062
}
```

## Resolution

Change the `public` accessibility modifier to `private`.
In doing so, remember to also add a <xref:PolyType.PropertyShapeAttribute> to avoid introducing [NBMsgPack060](NBMsgPack060.md).

```cs
public class Person
{
    public required string Name { get; set; }

    [PropertyShape]
    private UnusedDataPacket Extension { get; set; }
}
```
