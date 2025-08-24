# Type shapes

This library leverages PolyType as a source generator that provides fast startup time and a consistent set of attributes that may be used for many purposes within your application.

## Recommended configuration

PolyType is trim-safe and NativeAOT ready, particularly when used in its recommended configuration, where you apply @PolyType.GenerateShapeAttribute on the root type of your data model.

[!code-csharp[](../../samples/cs/TypeShapePatterns.cs#NaturallyAttributed)]

## Witness classes

If you need to directly serialize a type that isn't declared in your project and is not annotated with <xref:PolyType.GenerateShapeAttribute>, you can define another class in your own project to provide that shape.
Doing so leads to default serialization rules being applied to the type (e.g. only public members are serialized).

For this example, suppose you consume a `FamilyTree` type from a library that you don't control and did not annotate their type for serialization.
In your own project, you can define this witness type and use it to serialize an external type.

# [.NET](#tab/net)

[!code-csharp[](../../samples/cs/TypeShapePatterns.cs#WitnessNET)]

# [.NET Standard](#tab/netfx)

[!code-csharp[](../../samples/cs/TypeShapePatterns.cs#WitnessNETFX)]

---

Note the only special bit is providing the `Witness` class as a type argument to the `Serialize` method.
The _name_ of the witness class is completely inconsequential.
A witness class may have any number of <xref:PolyType.GenerateShapeForAttribute`1> attributes on it.
It is typical (but not required) for an assembly to have at most one witness class, with all the external types listed on it that you need to serialize as top-level objects.

You do _not_ need a witness class for an external type to reference that type from a graph that is already rooted in a type that _is_ attributed.

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
