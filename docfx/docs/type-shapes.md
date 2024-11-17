# Type shapes

This library leverages PolyType as a source generator that provides fast startup time and a consistent set of attributes that may be used for many purposes within your application.

## Recommended configuration

PolyType is trim-safe and NativeAOT ready, particularly when used in its recommended configuration, where you apply @PolyType.GenerateShapeAttribute on the root type of your data model.

[!code-csharp[](../../samples/TypeShapePatterns.cs#NaturallyAttributed)]

## Witness classes

If you need to directly serialize a type that isn't declared in your project and is not annotated with `[GenerateShape]`, you can define another class in your own project to provide that shape.
Doing so leads to default serialization rules being applied to the type (e.g. only public members are serialized).

For this example, suppose you consume a `FamilyTree` type from a library that you don't control and did not annotate their type for serialization.
In your own project, you can define this witness type and use it to serialize an external type.

[!code-csharp[](../../samples/TypeShapePatterns.cs#Witness)]

Note the only special bit is providing the `Witness` class as a type argument to the `Serialize` method.
The _name_ of the witness class is completely inconsequential.
A witness class may have any number of `GenerateShapeAttribute<T>` attributes on it.
It is typical (but not required) for an assembly to have at most one witness class, with all the external types listed on it that you need to serialize as top-level objects.

You do _not_ need a witness class for an external type to reference that type from a graph that is already rooted in a type that _is_ attributed.

## Fallback configuration

In the unlikely event that you have a need to serialize a type that does _not_ have a shape source-generated for it, you can use the conventional reflection approach of serialization with Nerdbank.MessagePack if you do not need to run in a trimmed app.

[!code-csharp[](../../samples/TypeShapePatterns.cs#SerializeUnshapedType)]
