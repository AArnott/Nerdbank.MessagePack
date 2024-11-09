# NBMsgPack031: Converters should read or write exactly one msgpack structure

Custom converters (classes that derive from @"Nerdbank.MessagePack.MessagePackConverter`1") should serialize a given value with exactly one msgpack structure, whether that is a scalar value like an integer, or a vector like an array or a map.
If there is nothing to write, you may write an empty array header.
If there is more than one value to write, lead with an array header that exactly predicts the number of values that will follow.

[Learn more about custom converters](../docs/custom-converters.md).

## Example violations

The following converter reads and writes two distinct values, which is not allowed.

```cs
public class MyTypeConverter : MessagePackConverter<MyType>
{
    public override MyType Deserialize(ref MessagePackReader reader, SerializationContext context)
    {
        int a = reader.ReadInt32();
        short b = reader.ReadInt16();  // NBMsgPack031
        return new MyType(a, b);
    }

    public override void Serialize(ref MessagePackWriter writer, in MyType value, SerializationContext context)
    {
        writer.Write(value.A);
        writer.Write(value.B);  // NBMsgPack031
    }
}
```

Each deferral of serialization to other converters counts as exactly one value each.

```cs
public class MyTypeConverter : MessagePackConverter<MyType>
{
    public override MyType Deserialize(ref MessagePackReader reader, SerializationContext context)
    {
        AnotherType a = context.GetConverter<AnotherType>().Deserialize(ref reader, context);
        YetAnotherType b = context.GetConverter<YetAnotherType>().Deserialize(ref reader, context);  // NBMsgPack031
        return new MyType(a, b);
    }

    public override void Serialize(ref MessagePackWriter writer, in MyType value, SerializationContext context)
    {
        AnotherType a = context.GetConverter<AnotherType>().Serialize(ref writer, ref value.A, context);
        YetAnotherType b = context.GetConverter<YetAnotherType>().Serialize(ref writer, ref value.B, context);  // NBMsgPack031
    }
}
```

## Resolution

When multiple values need to be serialized, always start with an array or map header.

When deserializing, always deserialize exactly the prescribed number of elements even if they do not match the expected count.
Or throw if the converter cannot successfully deserialize the value with the actual number of elements.

In the following example, the deserializer throws if it does not encounter exactly two elements.

```cs
public class MyTypeConverter : MessagePackConverter<MyType>
{
    public override MyType Deserialize(ref MessagePackReader reader, SerializationContext context)
    {
        int count = reader.ReadArrayHeader();
        if (count != 2)
        {
            throw new MessagePackSerializationException("Expected 2 elements.");
        }

        int property1 = reader.ReadInt32();
        short property2 = reader.ReadInt16();
        return new MyType(property1, property2);
    }

    public override void Serialize(ref MessagePackWriter writer, in MyType value, SerializationContext context)
    {
        writer.WriteArrayHeader(2);
        writer.Write(value.Property1);
        writer.Write(value.Property2);
    }
}
```

In this next example, the deserializer is flexible to forward/backward compatibility by reading any number of elements, and uses default values if fewer than 2 are encountered:

```cs
public override MyType Deserialize(ref MessagePackReader reader, SerializationContext context)
{
    int count = reader.ReadArrayHeader();

    // Initialize default values in case the array has fewer than 2 elements.
    int property1 = 0;
    short property2 = 0;

    for (int i = 0; i < count; i++)
    {
        switch (i)
        {
            case 0:
                property1 = reader.ReadInt32();
                break;
            case 1:
                property2 = reader.ReadInt16();
                break;
            default:
                // Very important that we read elements in the array belonging to this object even if we don't know what to do with them.
                // Calling Skip() is a way to read and drop the element without knowing what kind it is.
                reader.Skip();
                break;
        }
    }

    return new MyType(property1, property2);
}
```
