# Customizing serialization

The [`GenerateShapeAttribute`](xref:PolyType.GenerateShapeAttribute) applied to the root type of your serialized object graph is the quickest and easiest way to get serialization going.
It usually does a great job by default.
When you need to tweak some aspects of serialization, several techniques are available.

## Properties on MessagePackSerializer

The @Nerdbank.MessagePack.MessagePackSerializer#properties class itself contains several properties that can easily customize serialization.
Review the API documentation for each property to learn about them.

## Including/excluding members

By default, only public properties and fields are included in serialization.

Non-public fields and properties may be included by applying @PolyType.PropertyShapeAttribute to the member.

Public fields and properties may similarly be *excluded* from serialiation by applying @PolyType.PropertyShapeAttribute to the member and settings its @PolyType.PropertyShapeAttribute.Ignore property to `true`.

[!code-csharp[](../../samples/CustomizingSerialization.cs#IncludingExcludingMembers)]

## Changing property name

The serialized name for a property may be changed from its declared C# name by applying @PolyType.PropertyShapeAttribute and settings its @PolyType.PropertyShapeAttribute.Name property.

[!code-csharp[](../../samples/CustomizingSerialization.cs#ChangingPropertyNames)]

Alternatively you can apply a consistent transformation policy for *all* property names by setting the @Nerdbank.MessagePack.MessagePackSerializer.PropertyNamingPolicy?displayProperty=nameWithType property.

For example, you can apply a camelCase transformation with @Nerdbank.MessagePack.MessagePackNamingPolicy.CamelCase like this:

[!code-csharp[](../../samples/CustomizingSerialization.cs#ApplyNamePolicy)]

At which point all serialization/deserialization done with that instance will use camelCase for property names.

A property name set explicitly with @PolyType.PropertyShapeAttribute.Name?displayProperty=nameWithType will override the naming policy.

You can use any of the naming policies provided with the @Nerdbank.MessagePack.MessagePackNamingPolicy class, or you can provide your own implementation by deriving from the class yourself.

When using a deserializing constructor, the parameter names on the constructor should match the C# property name -- *not* the serialized name specified by @PolyType.PropertyShapeAttribute.Name or some @Nerdbank.MessagePack.MessagePackNamingPolicy.

## Deserializing constructors

The simplest deserialization is into a type with a default constructor and mutable fields and properties.
When the type contains serializable, readonly fields or properties with only a getter, a non-default constructor may be required to set the values for those members.

Consider this immutable type:

[!code-csharp[](../../samples/CustomizingSerialization.cs#DeserializingConstructors)]

The intent is of course for `Name` to be serialized.
Deserialization cannot be done into the `Name` property because there is no setter defined.
And in fact there is no default constructor defined, so the deserializer must invoke the non-default constructor, having matched the parameters in it to values available to be deserialized.

Important point to note is that the constructor parameter name matches the property name, modulo the PascalCase name to camelCase name.
This is how the deserializer matches up.

Let's consider a variant where the serialized name does not match the property name:

[!code-csharp[](../../samples/CustomizingSerialization.cs#DeserializingConstructorsPropertyRenamed)]

This will serialize the property with the name `person_name`.
Note that the constructor parameter name is _still_ a case-variant of the `Name` property rather than being based on the renamed `person_name` string.

### Constructor overload resolution

When a type declares multiple constructors, the deserializer may need help to know which overload you intend for deserialization to use.
To identify the intended constructor, apply the @PolyType.ConstructorShapeAttribute to it.

## Callbacks

A type may implement @Nerdbank.MessagePack.IMessagePackSerializationCallbacks in order to be notified of when it is about to be serialized or has just been deserialized.

## Serialize objects with indexes for keys

By default, a type is serialized as a map of property name=value pairs.
This is typically very safe for forward/backward version compatibility.
This also leads to a great experience when other programs or platforms consume the msgpack that you serialize.
For example when javascript deserializes msgpack, the javascript object they get will have properties and values similar to C#.

For even better serialization performance and more compact msgpack, you can opt your type into serializing with indexes instead of property names.
To do this, apply @Nerdbank.MessagePack.KeyAttribute to each property and field that needs to be serialized on your type.

[!code-csharp[](../../samples/CustomizingSerialization.cs#SerializeWithKey)]

These attributes give the serializer a durable and more compact identifier for properties than their names.
Using JSON for a readable representation, we can see that the msgpack changes when using these `Key` attributes from this:

```json
{
    "OneProperty": "value1",
    "AnotherProperty": "value2"
}
```

to this:

```json
["value1", "value2"]
```

or this:

```json
{
    0: "value1",
    1: "value2"
}
```

When the array format is used, the indexes provided to the @Nerdbank.MessagePack.KeyAttribute become indexes into the array.
When the map format is used, the indexes become keys in the map.

> [!IMPORTANT]
> When using @Nerdbank.MessagePack.KeyAttribute, keep version compatibility in mind across versions of your type.
> Do not use the same key index for two different properties over time if older deserializers may read your newer data or they will assign the value to the wrong property.
> It is OK to leave 'holes' in the array.
> When you delete a property, reserve the `Key` index that it had been assigned so it is never reused, perhaps using a code comment.

Note in the above "JSON" snippet that the keys are integers instead of strings.
This is something that isn't actually allowed in JSON, but is perfectly valid in msgpack and helps with writing very compact maps.

### Array or map?

Whether the array or map is used depends on which format the serializer believes will produce a more compact result.
When all values on an object are set to non-default values and there are no gaps due to undefined indexes, an array provides the most compact representation because the indexes are implicit.
When some properties of the object would ideally be skipped because the values are their defaults and/or there are gaps in assigned indexes, a map may be more compact.
A map has its own overhead because indexes are not implicit.

When @Nerdbank.MessagePack.MessagePackSerializer.SerializeDefaultValues?displayProperty=nameWithType is set to @Nerdbank.MessagePack.SerializeDefaultValuesPolicy.Always?displayProperty=nameWithType, the array format is always chosen, even if gaps from unassigned indexes may exist in the array.
But when this property is set to @Nerdbank.MessagePack.SerializeDefaultValuesPolicy.Never, even if the array format is chosen, it may be a shorter array because of properties at the end of the array that are set to their default values.

In the case above, the array format would have been chosen because there are two non-default values and no gaps.
Let's now consider another case:

[!code-csharp[](../../samples/CustomizingSerialization.cs#SerializeWithKeyAndGaps)]

Note the large gap between assigned indexes 0 and 5 in this class.
This could be justified by the removal of properties with indexes 1-4 and a desire to retain compatibility with previous versions of the serialized schema.

Serializing this object with @Nerdbank.MessagePack.MessagePackSerializer.SerializeDefaultValues set to @Nerdbank.MessagePack.SerializeDefaultValuesPolicy.Always, we'd get a 6-element array.
If both properties were set, the serialized form might look like this:

```json
["value1",null,null,null,null,"value2"]
```

For the rest of this section, let's assume @Nerdbank.MessagePack.MessagePackSerializer.SerializeDefaultValues is set to @Nerdbank.MessagePack.SerializeDefaultValuesPolicy.Never.
This immediately changes the binary representation of the serialized object above to just this:

```json
{
    0: "value1",
    5: "value2"
}
```

When `OneProperty` is non-null and `AnotherProperty` is null, the most compact format is an array with only one element, since it saves the `0` key:

```json
["value1"]
```

But suppose now that `OneProperty` is null and `AnotherProperty` is non-null.
This changes the dynamics because an array representation like this would waste 5 nil bytes:

```json
[null,null,null,null,null,"value2"]
```

So instead, we serialize as a map automatically, which pays only 1 byte to add the `5` key in the map:

```json
{
    5: "value2"
}
```

> [!NOTE]
> The serializer chooses between an array and map based on a quick calculation that estimates which will be more compact.
> This estimate will not always lead to the most compact outcome, but should generally be optimal when the difference is significant.
> Calculating the precise length of both options would significantly slow serialization and is why we use a fairly accurate estimate instead.

## Multi-dimensional arrays

The msgpack binary format does not specify how multi-dimensional arrays are to be encoded.
As a result, this library has chosen a default format for them.
For interoperability with other libraries you may want to change this format to another option.

Use the @Nerdbank.MessagePack.MessagePackSerializer.MultiDimensionalArrayFormat?displayProperty=nameWithType property to change the format.

## Resolving extension type code conflicts

The msgpack spec supports extensions.
Each extension must define a type code in the range of [-128, 127].
The negative values are all reserved for official extensions, leaving 0-127 for applications to use.

This library defines its own extensions for certain features.
These use type codes in the 0-127 range.
If these conflict with extensions that your application defines or that other libraries your application uses defines, you can reassign type codes for this library's extensions by setting @Nerdbank.MessagePack.MessagePackSerializer.LibraryExtensionTypeCodes?displayProperty=nameWithType.

## Understanding the schema

It can be useful to periodically audit your data type graph to ensure that it is serializing what you expect.
One way to do this is serialize an actual object graph and convert the msgpack to JSON using the @Nerdbank.MessagePack.MessagePackSerializer.ConvertToJson*?displayProperty=nameWithType method.
This will show you the serialized form of your object graph and help you understand how it is being serialized.
But this approach will only show you data that actually got serialized.
Optional values left to their default values may *not* be serialized, giving you an incomplete idea of what *might* be serialized.

To see a full description of what will or might be serialized, use the @Nerdbank.MessagePack.MessagePackSerializer.GetJsonSchema*?displayProperty=nameWithType method.
Obtaining a JSON schema can be useful for aiding in publishing a formal spec for your data type for interoperability with other systems that may need to redefine the types in their native syntax.

Consider that custom converters registered with an @Nerdbank.MessagePack.MessagePackSerializer instance and properties set on it can affect the schema.
Be sure to set these properties before calling @Nerdbank.MessagePack.MessagePackSerializer.GetJsonSchema*.

The schema generator has no insight into custom converters, so a warning will be included in the schema at locations where custom converters would be used, but the schema will attempt to represent what would typically be serialized without a custom converter.
