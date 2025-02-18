# Features

- Serializes in the compact and fast [MessagePack format](https://msgpack.org/).
- [Performance](performance.md) is on par with the highly tuned and popular MessagePack-CSharp library.
- Automatically serialize any type annotated with the [PolyType `[GenerateShape]`](xref:PolyType.GenerateShapeAttribute) attribute
  or non-annotated types by adding [a 'witness' type](type-shapes.md#witness-classes) with a similar annotation.
- Fast `ref`-based serialization and deserialization minimizes copying of large structs.
- NativeAOT and trimming compatible.
- Serialize only properties that have non-default values (optionally).
- Keep memory pressure low by using async serialization directly to/from I/O like a network, IPC pipe or file.
- [Streaming deserialization](streaming-deserialization.md) for large or over-time sequences.
- Primitive msgpack reader and writer APIs for low-level scenarios.
- Author custom converters for advanced scenarios.
- Security mitigations for stack overflows.
- Optionally serialize your custom types as arrays of values instead of maps of names and value for more compact representation and even higher performance.
- Support for serializing instances of certain types derived from the declared type and deserializing them back to their original runtime types using [unions](unions.md).
- Optionally [preserve reference equality](xref:ShapeShift.MessagePackSerializer.PreserveReferences) across serialization/deserialization.
- Structural (i.e. deep, by-value) equality checking for arbitrary types, both with and without collision resistant hash functions.
