# Features

* Serializes in the compact and fast [MessagePack format](https://msgpack.org/).
* [Performance](performance.md) is on par with the highly tuned and popular MessagePack-CSharp library.
* Automatically serialize any type annotated with the [PolyType `[GenerateShape]`](xref:PolyType.GenerateShapeAttribute) attribute
  or non-annotated types by adding [a 'witness' type](type-shapes.md#witness-classes) with a similar annotation.
* Fast `ref`-based serialization and deserialization minimizes copying of large structs.
* NativeAOT and trimming compatible.
* Serialize only properties that have non-default values (optionally).
* Keep memory pressure low by using async serialization directly to/from I/O like a network, IPC pipe or file.
* [Streaming deserialization](streaming-deserialization.md) for large or over-time sequences.
* Primitive msgpack reader and writer APIs for low-level scenarios.
* Author custom converters for advanced scenarios.
* Security mitigations for stack overflows.
* Optionally serialize your custom types as arrays of values instead of maps of names and value for more compact representation and even higher performance.
* Support for serializing instances of certain types derived from the declared type and deserializing them back to their original runtime types using [unions](unions.md).
* Optionally [preserve reference equality](xref:Nerdbank.MessagePack.MessagePackSerializer.PreserveReferences) across serialization/deserialization.
* Structural (i.e. deep, by-value) equality checking for arbitrary types, both with and without collision resistant hash functions.

## Feature comparison

See how this library compares to other .NET MessagePack libraries.

In many cases, the ✅ or ❌ in the table below are hyperlinks to the relevant documentation or an issue you can vote up to request the feature.

Feature                   | Nerdbank.MessagePack | MessagePack-CSharp  |
--------------------------|:--------------------:|:-------------------:|
Optimized for high performance | [✅](performance.md) | [✅](https://github.com/MessagePack-CSharp/MessagePack-CSharp?tab=readme-ov-file#performance) |
Contractless data types   | [✅](getting-started.md)[^1] | [✅](https://github.com/MessagePack-CSharp/MessagePack-CSharp?tab=readme-ov-file#object-serialization) |
Attributed data types     | [✅](customizing-serialization.md) | [✅](https://github.com/MessagePack-CSharp/MessagePack-CSharp?tab=readme-ov-file#object-serialization) |
Polymorphic serialization | [✅](unions.md) | [✅](https://github.com/MessagePack-CSharp/MessagePack-CSharp?tab=readme-ov-file#union)[^4] |
F# union type support     | [✅](fsharp.md) | ❌ |
Typeless serialization    | [✅](xref:Nerdbank.MessagePack.OptionalConverters.WithObjectPrimitiveConverter*) | [✅](https://github.com/MessagePack-CSharp/MessagePack-CSharp?tab=readme-ov-file#typeless) |
`dynamic` serialization    | [✅](getting-started.md#deserialize-to-dynamic) | [✅](https://github.com/MessagePack-CSharp/MessagePack-CSharp/blob/master/doc/ExpandoObject.md)
Skip serializing default values | [✅](xref:Nerdbank.MessagePack.MessagePackSerializer.SerializeDefaultValues) | [❌](https://github.com/MessagePack-CSharp/MessagePack-CSharp/issues/678) |
Required and non-nullable property deserialization guaranteed | [✅](xref:Nerdbank.MessagePack.MessagePackSerializer.DeserializeDefaultValues) | ❌ |
Dynamically use maps or arrays for most compact format | [✅](customizing-serialization.md#array-or-map) | [❌](https://github.com/MessagePack-CSharp/MessagePack-CSharp/issues/1953) |
Surrogate types for automatic serialization of unserializable types | [✅](surrogate-types.md) | ❌ |
Custom converters         | [✅](custom-converters.md) | ✅ |
Stateful converters       | [✅](custom-converters.md#stateful-converters) | ❌ |
Deserialization callback  | [✅](xref:Nerdbank.MessagePack.IMessagePackSerializationCallbacks) | [✅](https://github.com/MessagePack-CSharp/MessagePack-CSharp?tab=readme-ov-file#serialization-callback) |
MsgPack extensions        | ✅ | ✅ |
LZ4 compression           | [❌](https://github.com/AArnott/Nerdbank.MessagePack/issues/34) | [✅](https://github.com/MessagePack-CSharp/MessagePack-CSharp?tab=readme-ov-file#lz4-compression) |
Trim-safe                 | ✅ | ❌ |
NativeAOT ready           | ✅ | ❌[^2] |
Unity                     | [✅](unity.md)[^3] | [✅](https://github.com/MessagePack-CSharp/MessagePack-CSharp?tab=readme-ov-file#unity-support) |
Async                     | [✅](xref:Nerdbank.MessagePack.MessagePackSerializer.SerializeAsync*) | ❌ |
Endless streaming deserialization | [✅](streaming-deserialization.md) | [✅](https://github.com/MessagePack-CSharp/MessagePack-CSharp/?tab=readme-ov-file#multiple-messagepack-structures-on-a-single-stream)
Reference preservation    | [✅](xref:Nerdbank.MessagePack.MessagePackSerializer.PreserveReferences) | ❌ |
Cyclical references       | [✅](xref:Nerdbank.MessagePack.MessagePackSerializer.PreserveReferences) | ❌ |
JSON schema export        | [✅](xref:Nerdbank.MessagePack.MessagePackSerializer.GetJsonSchema*) | ❌ |
Secure defaults           | [✅](security.md) | ❌ |
Automatic hash collection deserialization in secure mode | ✅ | ✅ |
Automatic collision-resistant hash function for arbitrary types | [✅](xref:Nerdbank.MessagePack.StructuralEqualityComparer) | ❌ |
Rejection of data that defines multiple values for the same property | [✅](security.md#multiple-values-for-the-same-property) | ❌ |
Free of mutable statics   | ✅ | ❌ |
Structural `IEqualityComparer<T>` for arbitrary types | ✅ | ❌ |

Security is a complex subject.
[Learn more about how to secure your deserializer](security.md).

[^1]: Nerdbank.MessagePack's approach is more likely to be correct by default and more flexible to fixing when it is not.
[^2]: Although MessagePack-CSharp does not support .NET 8 flavor NativeAOT, it has long-supported Unity's il2cpp runtime, but it requires careful avoidance of dynamic features.
[^3]: Particular steps are currently required, and limitations apply. See our [unity doc](unity.md) for more information.
[^4]: MessagePack-CSharp is limited to derived types that can be attributed on the base type, whereas Nerdbank.MessagePack allows for dynamically identifying derived types at runtime.
