# F# techniques

F# users can enjoy a superb serialization experience with Nerdbank.MessagePack.
Nerdbank.MessagePack's [union support](unions.md) includes support for native F# union types, thanks for PolyType's built-in support for them.

The following snippet shows serializing a farm with various animals, converting to JSON for inspection, and deserializing the msgpack back again.

[!code-fsharp[](../../samples/fs/Program.fs#L7-L32)]

## AOT readiness

The above snippet uses the @PolyType.ReflectionProvider.ReflectionTypeShapeProvider which allows a single F# project to work out of the box.

Reflection can be avoided, and an F# program can be AOT-safe by:

1. Define your data layer in an F# library project.
1. Define a [witness type](type-shapes.md#witness-classes) for your F# data types from within a C# project that references your F# data types library.
1. Define your F# application that references both your data layer F# project and your C# witness type project.
   Pass the witness type to any serialize/deserialize method such as @Nerdbank.MessagePack.MessagePackSerializer.Serialize``2(``0@,System.Threading.CancellationToken) and @Nerdbank.MessagePack.MessagePackSerializer.Deserialize``2(System.ReadOnlyMemory{System.Byte},System.Threading.CancellationToken)
