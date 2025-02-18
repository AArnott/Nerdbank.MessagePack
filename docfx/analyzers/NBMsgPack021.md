# NBMsgPack021: `[MessagePackConverter]` type missing default constructor

@ShapeShift.MessagePackConverterAttribute should specify a type that declares a public default constructor.

Learn more about [custom converters](../docs/custom-converters.md).

## Example violations

In the below example, `MyTypeConverter` declares an explicit, _non-public_ constructor.

```cs
[MessagePackConverter(typeof(MyTypeConverter))]
public class MyType
{
}

public class MyTypeConverter : MessagePackConverter<MyType>
{
	private MyTypeConverter() { }
	public override MyType Read(ref MessagePackReader reader, SerializationContext context) => throw new System.NotImplementedException();
	public override void Write(ref MessagePackWriter writer, in MyType value, SerializationContext context) => throw new System.NotImplementedException();
}
```

## Resolution

Fix the converter type to declare a public default constructor, or remove the explicit constructor so the C# language provides a default constructor.

```cs
[MessagePackConverter(typeof(MyTypeConverter))]
public class MyType
{
}

public class MyTypeConverter : MessagePackConverter<MyType>
{
	public override MyType Read(ref MessagePackReader reader, SerializationContext context) => throw new System.NotImplementedException();
	public override void Write(ref MessagePackWriter writer, in MyType value, SerializationContext context) => throw new System.NotImplementedException();
}
```
