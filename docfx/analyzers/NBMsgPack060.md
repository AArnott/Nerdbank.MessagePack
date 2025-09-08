# NBMsgPack060: `UnusedDataPacket` member should have a property shape

A property or field typed as the special @Nerdbank.MessagePack.UnusedDataPacket type must have a property shape generated for it by PolyType.
This is done by default for public members, but other members must have <xref:PolyType.PropertyShapeAttribute> applied (*without* setting its <xref:PolyType.PropertyShapeAttribute.Ignore> property to `true` of course).

## Example violation

The following class has the special property defined as private but without a <xref:PolyType.PropertyShapeAttribute>:

```csharp
public class Person
{
    public required string Name { get; set; }

    private UnusedDataPacket Extension { get; set; } // NBMsgPack060
}
```

## Resolution

Add the <xref:PolyType.PropertyShapeAttribute>:

```csharp
public class Person
{
    public required string Name { get; set; }

    [PropertyShape]
    private UnusedDataPacket Extension { get; set; }
}
```
