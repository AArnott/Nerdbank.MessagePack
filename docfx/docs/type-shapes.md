# Type shapes

This library leverages PolyType as a source generator that provides fast startup time and a consistent set of attributes that may be used for many purposes within your application.

## Recommended configuration

PolyType is trim-safe and NativeAOT ready, particularly when used in its recommended configuration, where you apply <xref:PolyType.GenerateShapeAttribute> on the root type of your data model.

[!code-csharp[](../../samples/cs/TypeShapePatterns.cs#NaturallyAttributed)]

## Witness classes

If you need to directly serialize a type that isn't declared in your project and is not annotated with <xref:PolyType.GenerateShapeAttribute>, you can define another class in your own project to provide that shape.
Doing so leads to default serialization rules being applied to the type (e.g. only public members are serialized).

For this example, suppose you consume a `FamilyTree` type from a library that you don't control and did not annotate their type for serialization.
In your own project, you can define this witness type and use it to serialize an external type.

[!code-csharp[](../../samples/cs/TypeShapePatterns.cs#Witness)]

Note the only special bit is providing the `Witness` class as a type argument to the `Serialize` method.
The _name_ of the witness class is completely inconsequential.
A witness class may have any number of <xref:PolyType.GenerateShapeForAttribute`1> attributes on it.
It is typical (but not required) for an assembly to have at most one witness class, with all the external types listed on it that you need to serialize as top-level objects.

You do _not_ need a witness class for an external type to reference that type from a graph that is already rooted in a type that _is_ attributed.

### Serializing arrays as top-level types

When you need to serialize or deserialize an array as the top-level type (not as a property of another object), you must use a witness type with the <xref:PolyType.GenerateShapeForAttribute`1> attribute, even if the element type itself is already annotated with <xref:PolyType.GenerateShapeAttribute>.

This is because arrays themselves are not types you can annotate directly - they're runtime-constructed generic types.

For example, suppose you have a `ScenarioInput` class that's properly annotated:

```csharp
[GenerateShape]
public partial class ScenarioInput
{
    public string Name { get; set; }
    public int Value { get; set; }
}
```

To serialize or deserialize a `ScenarioInput[]` array as the top-level type, define a witness type:

```csharp
[GenerateShapeFor<ScenarioInput[]>]
partial class Witness;
```

Then use the witness type when calling serialize/deserialize methods:

```csharp
var serializer = new MessagePackSerializer();

// Serialization
ScenarioInput[] data = [ new() { Name = "Test", Value = 42 } ];
byte[] msgpack = serializer.Serialize<ScenarioInput[], Witness>(data);

// Deserialization
ScenarioInput[] result = serializer.Deserialize<ScenarioInput[], Witness>(msgpack);
```

> [!NOTE]
> If you're deserializing data serialized by the older MessagePack-CSharp library, the format is the same. Simply define your data model classes with <xref:PolyType.GenerateShapeAttribute> (instead of the old `MessagePackObjectAttribute`), create a witness type for the array, and deserialize as shown above.

## Fallback configuration

In the unlikely event that you have a need to serialize a type that does _not_ have a shape source-generated for it, you can use the conventional reflection approach of serialization with Nerdbank.MessagePack, if you do not need to run in a trimmed app.

[!code-csharp[](../../samples/cs/TypeShapePatterns.cs#SerializeUnshapedType)]

### Source generated data models

If your data models are themselves declared by a source generator, the PolyType source generator will be unable to emit type shapes for your data models in the same compilation.
Using the <xref:PolyType.ReflectionProvider.ReflectionTypeShapeProvider.Default?displayProperty=nameWithType> is one way you can workaround this, at the cost of somewhat slower serialization (especially the first time due to reflection), but this does not always work in a trimmed application.
And it adds some limitations to what types can be serialized in a NativeAOT application where dynamic code cannot run.

Instead of falling back to reflection, you can still use the PolyType source generator by declaring your data types in another assembly that your serialization code then references.
For example, if assembly "A" declares your data types via a source generator (e.g. Vogen), assembly "B" can reference "A", and then use a Witness type (described above) to source generate the type shapes for all your data types.

Still another option to get your source generated data type to be serializable may be to [define a marshaler to a surrogate type](./surrogate-types.md).

## Working with Vogen

Vogen is a source generator that wraps primitive types in custom structs that can add validation and another level of type safety to your data models.

> [!IMPORTANT]
> Use Vogen 8.0.3 or later, which emits PolyType marshalers so that data types are serialized without extraneous wrappers.

With Vogen, you have the two options described in the above section, to either declare your data models in a separate project or use the reflection type shape provider.
Here is what those two worlds look like:

### Data models in a separate project

Consider the following data models in an auxiliary project:

[!code-csharp[](../../samples/VogenDataTypes/Customer.cs)]

Your serialization code in a referencing assembly then looks like this:

[!code-csharp[](../../samples/cs/ConsumeVogenWithAssemblyIsolation.cs#Sample)]

### Data models in the same project

When your Vogen data models are in the same project, you must use the <xref:PolyType.ReflectionProvider.ReflectionTypeShapeProvider.Default?displayProperty=nameWithType>

First, define the data model like this:

[!code-csharp[](../../samples/cs/ConsumeVogenWithReflectionProvider.cs#DataTypes)]

These data models are almost the same as the auxiliary assembly sample, except that `Customer` does not have to be `partial` nor carry the <xref:PolyType.GenerateShapeAttribute>.

Your serialization code in *the same* assembly then uses <xref:PolyType.ReflectionProvider.ReflectionTypeShapeProvider.Default?displayProperty=nameWithType> and looks like this:

[!code-csharp[](../../samples/cs/ConsumeVogenWithReflectionProvider.cs#SerializeVogen)]
