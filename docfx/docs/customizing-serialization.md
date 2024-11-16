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

The serialized name for a property may be changed from its declared C# name by applying @PolyType.PropertyShapeAttribute and settings its @PolyType.PropertyShapeAttribute.Name property.

```cs
class MyType
{
    [PropertyShape(Name = "myName")]
    public string MyName { get; set; }
}
```

Alternatively you can apply a consistent transformation policy for *all* property names by setting the @Nerdbank.MessagePack.MessagePackSerializer.PropertyNamingPolicy?displayProperty=nameWithType property.

For example, you can apply a camelCase transformation with @Nerdbank.MessagePack.MessagePackNamingPolicy.CamelCase like this:

```cs
var serializer = new MessagePackSerializer
{
    PropertyNamingPolicy = MessagePackNamingPolicy.CamelCase,
};
```

At which point all serialization/deserialization done with that instance will use camelCase for property names.

A property name set explicitly with @PolyType.PropertyShapeAttribute.Name?displayProperty=nameWithType will override the naming policy.

You can use any of the naming policies provided with the @Nerdbank.MessagePack.MessagePackNamingPolicy class, or you can provide your own implementation by deriving from the class yourself.

When using a deserializing constructor, the parameter names on the constructor should match the C# property name -- *not* the serialized name specified by @PolyType.PropertyShapeAttribute.Name or some @Nerdbank.MessagePack.MessagePackNamingPolicy.

## Deserializing constructors

The simplest deserialization is into a type with a default constructor and mutable fields and properties.
When the type contains serializable, readonly fields or properties with only a getter, a non-default constructor may be required to set the values for those members.

Consider this immutable type:

```cs
[GenerateShape]
public partial class ImmutablePerson
{
    public ImmutablePerson(string? name)
    {
        this.Name = name;
    }

    public string? Name { get; }
}
```

The intent is of course for `Name` to be serialized.
Deserialization cannot be done into the `Name` property because there is no setter defined.
And in fact there is no default constructor defined, so the deserializer must invoke the non-default constructor, having matched the parameters in it to values available to be deserialized.

Important point to note is that the constructor parameter name matches the property name, modulo the PascalCase name to camelCase name.
This is how the deserializer matches up.

Let's consider a variant where the serialized name does not match the property name:

```cs
[GenerateShape]
public partial class ImmutablePerson
{
    public ImmutablePerson(string? name)
    {
        this.Name = name;
    }

    [PropertyShape(Name = "person_name")]
    public string? Name { get; }
}
```

This will serialize the property with the name `person_name`.
Note that the constructor parameter name is _still_ a case-variant of the `Name` property rather than being based on the renamed `person_name` string.

### Constructor overload resolution

When a type declares multiple constructors, the deserializer may need help to know which overload you intend for deserialization to use.
To identify the intended constructor, apply the @PolyType.ConstructorShapeAttribute to it.

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
