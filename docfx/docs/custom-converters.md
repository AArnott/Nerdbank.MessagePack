# Custom converters

While using the [`GenerateShapeAttribute`](xref:PolyType.GenerateShapeAttribute) is by far the simplest way to make an entire type graph serializable, some types may not be compatible with automatic serialization.
In such cases, you can define and register your own custom converter for the incompatible type.

## Define your own converter

Consider class `Foo` that cannot be serialized automatically.

Declare a class that derives from @"Nerdbank.MessagePack.MessagePackConverter`1":

[!code-csharp[](../../samples/CustomConverters.cs#YourOwnConverter)]

> [!CAUTION]
> It is imperative that each `Write` and `Read` method write and read *exactly one* msgpack structure.

A converter that reads or writes more than one msgpack structure may appear to work correctly, but will result in invalid, unparseable msgpack.
Msgpack is a structured, self-describing format similar to JSON.
In JSON, an individual array element or object property value must be described as a single element or the JSON would be invalid.

If you have more than one value to serialize or deserialize (e.g. multiple fields on an object) you MUST use a map or array header with the appropriate number of elements you intend to serialize.
In the @"Nerdbank.MessagePack.MessagePackConverter`1.Write*" method, use @Nerdbank.MessagePack.MessagePackWriter.WriteMapHeader* or @Nerdbank.MessagePack.MessagePackWriter.WriteArrayHeader*.
In the @"Nerdbank.MessagePack.MessagePackConverter`1.Read*" method, use @Nerdbank.MessagePack.MessagePackReader.ReadMapHeader or @Nerdbank.MessagePack.MessagePackReader.ReadArrayHeader.

### Security considerations

Any custom converter should call @Nerdbank.MessagePack.SerializationContext.DepthStep*?displayProperty=nameWithType on the @Nerdbank.MessagePack.SerializationContext argument provided to it to ensure that the depth of the msgpack structure is within acceptable bounds.
This call should be made before reading or writing any msgpack structure (other than nil).

This is important to prevent maliciously crafted msgpack from causing a stack overflow or other denial-of-service attack.
A stack overflow tends to crash the process, whereas a call to @Nerdbank.MessagePack.SerializationContext.DepthStep* merely throws a typical @Nerdbank.MessagePack.MessagePackSerializationException which is catchable and more likely to be caught.

While checking the depth only guards against exploits during *de*serialization, converters should call it during serialization as well to help an application avoid serializing a data structure that they will later be unable to deserialize.
It also taps into the built-in cancellation token checks built into depth tracking.

Applications that have a legitimate need to exceed the default stack depth limit can adjust it by setting @Nerdbank.MessagePack.SerializationContext.MaxDepth?displayProperty=nameWithType to a higher value.

### Delegating to sub-values

The @Nerdbank.MessagePack.SerializationContext.GetConverter* method may be used to obtain a converter to use for members of the type your converter is serializing or deserializing.

[!code-csharp[](../../samples/CustomConverters.cs#DelegateSubValues)]

The above assumes that `SomeOtherType` is a type that you declare and can have @PolyType.GenerateShapeAttribute`1 applied to it.
If this is not the case, you may provide your own type shape and reference that.
For convenience, you may want to apply it directly to your custom converter:

[!code-csharp[](../../samples/CustomConverters.cs#WitnessOnFormatter)]

The @PolyType.GenerateShapeAttribute`1 is what enables `FooConverter` to be a "provider" for the shape of `SomeOtherType`.

Arrays of a type require a shape of their own.
So even if you define your type `MyType` with @PolyType.GenerateShapeAttribute`1, serializing `MyType[]` would require a witness type and attribute. For example:

[!code-csharp[](../../samples/CustomConverters.cs#ArrayWitnessOnFormatter)]

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

[!code-csharp[](../../samples/CustomConverters.cs#ReadWholeArray)]

Note the structure uses a switch statement, which allows for 'holes' in the array to develop over time as properties are removed.
It also implicitly skips values in any unknown array index, such that reading *all* array elements is guaranteed.

### Performance considerations

#### Cancellation handling

A custom converter should honor the @System.Threading.CancellationToken passed to it.
This is mostly automatic because @Nerdbank.MessagePack.SerializationContext.DepthStep*?displayProperty=nameWithType will throw @System.OperationCanceledException if the token is canceled, and most converters should already be calling @Nerdbank.MessagePack.SerializationContext.DepthStep.

For particularly expensive converters, it may be beneficial to check the token periodically through the conversion process.
For typical synchronous converters, this means checking @Nerdbank.MessagePack.SerializationContext.CancellationToken?displayProperty=nameWithType before performing any significant work and periodically thereafter.
Async converters can check the token at this same location or more simply use the token passed explicitly as a parameter.
Async converters should always propagate the token to any async methods they call.

#### Memory pressure

The built-in converters take special considerations to avoid allocating, encoding and deallocating strings for property names.
This reduces GC pressure and removes redundant CPU time spent repeatedly converting UTF-8 encoded property names as strings.
Your custom converters *may* follow similar patterns if tuning performance for your particular type's serialization is important.

### Async converters

@Nerdbank.MessagePack.MessagePackConverter`1 is an abstract class that requires a derived converter to implement synchronous @Nerdbank.MessagePack.MessagePackConverter`1.Write* and @Nerdbank.MessagePack.MessagePackConverter`1.Read* methods.
The base class also declares `virtual` async alternatives to these methods (@Nerdbank.MessagePack.MessagePackConverter`1.WriteAsync* and @Nerdbank.MessagePack.MessagePackConverter`1.ReadAsync*, respectively) which a derived class may *optionally* override.
These default async implementations are correct, and essentially buffer the whole msgpack representation while deferring the actual serialization work to the synchronous methods.

For types that may represent a great deal of data (e.g. arrays and maps), overriding the async methods in order to read or flush msgpack in smaller portions may reduce memory pressure and/or improve performance.
When a derived type overrides the async methods, it should also override @Nerdbank.MessagePack.MessagePackConverter`1.PreferAsyncSerialization to return `true` so that callers know that you have optimized async paths.

The built-in converters, including those that serialize your custom data types by default, already override the async methods with optimal implementations.

## Register your custom converter

There are two ways to get the serializer to use your custom converter.

Note that if your custom type is used as the top-level data type to be serialized, it must still have @PolyType.GenerateShapeAttribute applied as usual.

### Attribute approach

To get your converter to be automatically used wherever the data type that it formats needs to be serialized, apply a @Nerdbank.MessagePack.MessagePackConverterAttribute to your custom data type that points to your custom converter.

[!code-csharp[](../../samples/CustomConverters.cs#CustomConverterByAttribute)]

### Runtime registration

For precise runtime control of where your converter is used and/or how it is instantiated/configured, you may register an instance of your custom converter with an instance of @Nerdbank.MessagePack.MessagePackSerializer using the @Nerdbank.MessagePack.MessagePackSerializer.RegisterConverter*.

[!code-csharp[](../../samples/CustomConverters.cs#CustomConverterByRegister)]
