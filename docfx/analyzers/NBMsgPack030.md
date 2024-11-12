# NBMsgPack030: Converters should not call top-level `MessagePackSerializer` methods

Custom converters (classes that derive from @"Nerdbank.MessagePack.MessagePackConverter`1") should not make calls to @Nerdbank.MessagePack.MessagePackSerializer.
Such calls risk resetting the stack guard, unnecessary cache misses, etc.

A custom converter will often find itself in a situation where the type it is serializing is made up of sub-values that themselves can be serialized automatically, without a custom converter.
In such cases, @Nerdbank.MessagePack.SerializationContext.GetConverter*?displayProperty=nameWithType is the appropriate tool to delegate conversion of these sub-values.

[Learn more about custom converters](../docs/custom-converters.md).

## Example violations

```cs
public class MyTypeConverter : MessagePackConverter<MyType>
{
    public override MyType? Read(ref MessagePackReader reader, SerializationContext context) => throw new System.NotImplementedException();

    public override void Write(ref MessagePackWriter writer, in MyType? value, SerializationContext context)
    {
        var serializer = new MessagePackSerializer(); // NBMsgPack030
        serializer.Serialize(value.SomeProperty);     // NBMsgPack030
    }
}

public class MyType
{
    public SomeOtherType? SomeProperty { get; set; }
}

[GenerateShape]
public partial class SomeOtherType {}
```

## Resolution

Replace top-level calls to the serializer to direct calls to a converter, which may come from @Nerdbank.MessagePack.SerializationContext.GetConverter*?displayProperty=nameWithType.

```cs
public override void Write(ref MessagePackWriter writer, in MyType? value, SerializationContext context)
{
    SomeOtherType? someProperty = value.SomeProperty;
    context.GetConverter<SomeOtherType>().Write(ref writer, ref someProperty, context);
}
```
