# NBMsgPack020: `[MessagePackConverter]` type must be compatible converter

@Nerdbank.MessagePack.MessagePackConverterAttribute should specify a type that derives from @Nerdbank.MessagePack.MessagePackConverter`1 where the type argument is the type the attribute is applied to.

Learn more about [custom converters](../docs/custom-converters.md).

## Example violations

```cs
[MessagePackConverter(typeof(MyTypeConverter))] // MyTypeConverter does not derive from the correct base type
public class MyType
{
}

public class MyTypeConverter
{
	public MyType Read(ref MessagePackReader reader, SerializationContext context) => throw new System.NotImplementedException();
	public void Write(ref MessagePackWriter writer, ref MyType value, SerializationContext context) => throw new System.NotImplementedException();
}
```

## Resolution

Fix the converter type to derive from @Nerdbank.MessagePack.MessagePackConverter`1.

```cs
[MessagePackConverter(typeof(MyTypeConverter)]
class MyType
{
}

public class MyTypeConverter : MessagePackConverter<MyType>
{
	public override MyType Read(ref MessagePackReader reader, SerializationContext context) => throw new System.NotImplementedException();
	public override void Write(ref MessagePackWriter writer, in MyType value, SerializationContext context) => throw new System.NotImplementedException();
}
```
