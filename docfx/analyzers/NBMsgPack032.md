# NBMsgPack032: Converters should override GetJsonSchema

Custom converters (classes that derive from @"ShapeShift.MessagePackConverter`1") should override the @ShapeShift.MessagePackConverter`1.GetJsonSchema\* method to document the schema of the msgpack they emit.

[Learn more about custom converters](../docs/custom-converters.md).

## Example violations

The following class converts `MyType` into a single integer.
It fails to document this, however.

```cs
public class MyTypeConverter : MessagePackConverter<MyType>
{
    public override MyType Read(ref MessagePackReader reader, SerializationContext context)
    {
        int a = reader.ReadInt32();
        return new MyType(a);
    }

    public override void Write(ref MessagePackWriter writer, in MyType value, SerializationContext context)
    {
        writer.Write(value.A);
    }
}
```

## Resolution

Override the @ShapeShift.MessagePackConverter`1.GetJsonSchema\* method to document that the object is serialized as an integer.

```cs
public class MyTypeConverter : MessagePackConverter<MyType>
{
    public override MyType Read(ref MessagePackReader reader, SerializationContext context)
    {
        int a = reader.ReadInt32();
        return new MyType(a);
    }

    public override void Write(ref MessagePackWriter writer, in MyType value, SerializationContext context)
    {
        writer.Write(value.A);
    }

    public override JsonObject GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
    {
        return new JsonObject
        {
             ["type"] = "integer",
        };
    }
}
```

Learn more about [JSON Schema](https://json-schema.org/) and review the docs for the @ShapeShift.MessagePackConverter`1.GetJsonSchema\* method for details.
