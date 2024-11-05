# Customizing serialization

The [`GenerateShapeAttribute`](xref:TypeShape.GenerateShapeAttribute) applied to the root type of your serialized object graph is the quickest and easiest way to get serialization going.
It usually does a great job by default.
When you need to tweak some aspects of serialization, several techniques are available.

## Including/excluding members

By default, only public properties and fields are included in serialization.

Non-public fields and properties may be included by applying @TypeShape.PropertyShapeAttribute to the member.

Public fields and properties may similarly be *excluded* from serialiation by applying @TypeShape.PropertyShapeAttribute to the member and settings its @TypeShape.PropertyShapeAttribute.Ignore property to `true`.

```cs
class MyType
{
    [PropertyShape(Ignore = true)] // exclude this property from serialization
    public string MyName { get; set; }

    [PropertyShape] // include this non-public property in serialization
    internal string InternalMember { get; set; }
}
```

## Changing property name

The serialized name for a property may be changed from its declared C# name by applying @TypeShape.PropertyShapeAttribute and settings its @TypeShape.PropertyShapeAttribute.Name property.

```cs
class MyType
{
    [PropertyShape(Name = "myName")]
    public string MyName { get; set; }
}
```

## Serialize as an array of values

By default, a type is serialized as a map of property name=value pairs.
This is typically very safe for forward/backward version compatibility.
This also leads to a great experience when other programs or platforms consume the msgpack that you serialize.
For example when javascript deserializes msgpack, the javascript object they get will have properties and values similar to C#.

For even better serialization performance and more compact msgpack, you can opt your type into serializing as an array of values instead.
To do this, apply @Nerdbank.MessagePack.KeyAttribute to each property and field that needs to be serialized on your type.

```cs
[GenerateShape]
class MyType
{
    [Key(0)]
    public string OneProperty { get; set; }

    [Key(1)]
    public string AnotherProperty { get; set; }
}
```

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

> [!IMPORTANT]
> When using @Nerdbank.MessagePack.KeyAttribute, keep version compatibility in mind across versions of your type.
> Do not use the same key index for two different properties over time if older deserializers may read your newer data or they will assign the value to the wrong property.
> It is OK to leave 'holes' in the array.
> When you delete a property, reserve the `Key` index that it had been assigned so it is never reused, perhaps using a code comment.
