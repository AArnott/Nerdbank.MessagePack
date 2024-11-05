# Custom converters

While using the [`GenerateShapeAttribute`](xref:TypeShape.GenerateShapeAttribute) is by far the simplest way to make an entire type graph serializable, some types may not be compatible with automatic serialization.
In such cases, you can define and register your own custom converter for the incompatible type.

## Define your own converter

Consider class `Foo` that cannot be serialized automatically.

Declare a class that derives from @"Nerdbank.MessagePack.MessagePackConverter``1":

```cs
using Nerdbank.MessagePack;

class FooConverter : MessagePackConverter<Foo?>
{
    public override Foo? Deserialize(ref MessagePackReader reader, SerializationContext context)
    {
        if (reader.TryReadNil())
        {
            return null;
        }

        int property1 = 0;
        string? property2 = null;

        int count = reader.ReadMapHeader();
        for (int i = 0; i < count; i++)
        {
            string? key = reader.ReadString();
            switch (key)
            {
                case "MyProperty":
                    property1 = reader.ReadInt32();
                    break;
                case "MyProperty2":
                    property2 = reader.ReadString();
                    break;
                default:
                    // Skip the value, as we don't know where to put it.
                    reader.Skip();
                    break;
            }
        }

        return new Foo(property1, property2);
    }

    public override void Serialize(ref MessagePackWriter writer, ref Foo? value, SerializationContext context)
    {
        if (value is null)
        {
            writer.WriteNil();
            return;
        }

        writer.WriteMapHeader(2);

        writer.WriteString("MyProperty");
        writer.Write(value.MyProperty);

        writer.WriteString("MyProperty2");
        writer.Write(value.MyProperty2);
    }
}
```

> [!CAUTION]
> It is imperative that each `Serialize` and `Deserialize` method write and read *exactly one* msgpack structure.

A converter that reads or writes more than one msgpack structure may appear to work correctly, but will result in invalid, unparseable msgpack.
Msgpack is a structured, self-describing format similar to JSON.
In JSON, an individual array element or object property value must be described as a single element or the JSON would be invalid.

If you have more than one value to serialize or deserialize (e.g. multiple fields on an object) you MUST use a map or array header with the appropriate number of elements you intend to serialize.
In the @"Nerdbank.MessagePack.MessagePackConverter`1.Serialize*" method, use @Nerdbank.MessagePack.MessagePackWriter.WriteMapHeader* or @Nerdbank.MessagePack.MessagePackWriter.WriteArrayHeader*.
In the @"Nerdbank.MessagePack.MessagePackConverter`1.Deserialize*" method, use @Nerdbank.MessagePack.MessagePackReader.ReadMapHeader or @Nerdbank.MessagePack.MessagePackReader.ReadArrayHeader.

### Version compatibility

> [!IMPORTANT]
> Consider forward and backward version compatibility in your serializer.
> Assume that your converter will deserialize values that a newer or older version of your converter serialized.

Version compatibility may take several forms.
Most typically it means to be prepared to skip values that you don't recognize.
For example when reading maps, skip values when you don't recognize the property name.
When reading arrays, you must read *all* the values in the array, even if you don't expect more than some given number of elements.

The sample above demonstrates reading all map entries and values, including explicitly skipping entries and values that the converter does not recognize.
If you're serializing only property values as an array, it is equally important to deserialize every array element, even if fewer elements are expected than are actually there. For example:

```cs
int count = reader.ReadArrayHeader();
for (int i = 0; i < count; i++)
{
    switch (i)
    {
        case 0:
            property1 = reader.ReadInt32();
            break;
        case 1:
            property2 = reader.ReadString();
            break;
        default:
            // Skip the value, as we don't know where to put it.
            reader.Skip();
            break;
    }
}
```

Note the structure uses a switch statement, which allows for 'holes' in the array to develop over time as properties are removed.
It also implicitly skips values in any unknown array index, such that reading *all* array elements is guaranteed.

### Performance considerations

The built-in converters take special considerations to avoid allocating, encoding and deallocating strings for property names.
This reduces GC pressure and removes redundant CPU time spent repeatedly converting UTF-8 encoded property names as strings.
Your custom converters *may* follow similar patterns if tuning performance for your particular type's serialization is important.

## Register your custom converter

Register an instance of your custom converter with an instance of @Nerdbank.MessagePack.MessagePackSerializer using the @Nerdbank.MessagePack.MessagePackSerializer.RegisterConverter*.
