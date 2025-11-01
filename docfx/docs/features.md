# Features

* Serializes in the compact and fast [MessagePack format](https://msgpack.io/).
* **No attributes required** on your data types. Optional attributes allow customization and improve performance.
* **Supports the latest C# syntax** including `required` and `init` properties, `record` classes and structs, and primary constructors.
* This library is [perf-optimized](performance.md) and is **among the fastest** MessagePack serialization libraries available for .NET.
* Works *great* in your **NativeAOT**, trimmed, **SignalR** or **ASP.NET Core MVC** applications or [**Unity**](unity.md) games.
* Many [C# analyzers](../analyzers/index.md) to help you avoid common mistakes.
* [Great security](security.md) for deserializing untrusted data.
* [Polymorphic deserialization](unions.md) lets you deserialize derived types.
* True async and [streaming deserialization](streaming-deserialization.md) for large or over-time sequences keeps your apps responsive and memory pressure low.
* Deserialize [just the fragment you require](targeted-deserialization.md) with intuitive LINQ expressions.
* [Preserve reference equality](xref:Nerdbank.MessagePack.MessagePackSerializer.PreserveReferences) across serialization/deserialization (optional).
* [Forward compatible data retention](customizing-serialization.md#retaining-unrecognized-data) allows you to deserialize and re-serialize data without dropping properties you didn't know about.
* [Structural equality checking](structural-equality.md) and hashing *for arbitrary types* gives you deep by-value equality semantics without hand-authoring `Equals` and `GetHashCode` overrides.
* **No mutable statics** ensures your code runs properly no matter what other code might run in the same process.
* Only serialize properties with [non-default values](xref:Nerdbank.MessagePack.MessagePackSerializer.SerializeDefaultValues) (optional).
* Primitive msgpack reader and writer APIs for low-level scenarios.
* Author custom converters for advanced scenarios.

## Feature comparison

See how this library compares to other .NET MessagePack libraries.

In many cases, the ✅ or ❌ in the table below are hyperlinks to the relevant documentation or an issue you can vote up to request the feature.

Feature                   | Nerdbank.MessagePack | MessagePack-CSharp  | Serde.NET
--------------------------|:--------------------:|:-------------------:|:-----------:|
Optimized for high performance | [✅](performance.md) | [✅](https://github.com/MessagePack-CSharp/MessagePack-CSharp?tab=readme-ov-file#performance) | ✅
Contractless data types   | [✅](getting-started.md)[^1] | [✅](https://github.com/MessagePack-CSharp/MessagePack-CSharp?tab=readme-ov-file#object-serialization) | ❌ |
Attributed data types     | [✅](customizing-serialization.md) | [✅](https://github.com/MessagePack-CSharp/MessagePack-CSharp?tab=readme-ov-file#object-serialization) | [✅](https://serdedotnet.github.io/generator/options.html)
Polymorphic serialization | [✅](unions.md) | [✅](https://github.com/MessagePack-CSharp/MessagePack-CSharp?tab=readme-ov-file#union)[^4] | [✅](https://serdedotnet.github.io/data-model.html)
Duck-typed polymorphic serialization | [✅](unions.md#duck-typing) | ❌ | ❌ |
F# union type support     | [✅](fsharp.md) | ❌ | ❌ |
Typeless serialization    | [✅](xref:Nerdbank.MessagePack.OptionalConverters.WithObjectConverter*) | [✅](https://github.com/MessagePack-CSharp/MessagePack-CSharp?tab=readme-ov-file#typeless) | ❌ |
`dynamic` serialization    | [✅](getting-started.md#untyped-deserialization) | [✅](https://github.com/MessagePack-CSharp/MessagePack-CSharp/blob/master/doc/ExpandoObject.md) | ❌ |
Forward compatible data retention | [✅](customizing-serialization.md#retaining-unrecognized-data) | ❌ | ❌ |
Property name transformations | [✅](xref:Nerdbank.MessagePack.MessagePackSerializer.PropertyNamingPolicy) | ❌ | ❌ |
Skip serializing default values | [✅](xref:Nerdbank.MessagePack.MessagePackSerializer.SerializeDefaultValues) | [❌](https://github.com/MessagePack-CSharp/MessagePack-CSharp/issues/678) | 🌗 |
Required and non-nullable property deserialization guaranteed | [✅](xref:Nerdbank.MessagePack.MessagePackSerializer.DeserializeDefaultValues) | ❌ | ✅ |
Dynamically use maps or arrays for most compact format | [✅](customizing-serialization.md#array-or-map) | [❌](https://github.com/MessagePack-CSharp/MessagePack-CSharp/issues/1953) | ❌ |
Surrogate types for automatic serialization of unserializable types | [✅](surrogate-types.md) | ❌ | [✅](https://serdedotnet.github.io/foreign-types.html) |
Custom converters         | [✅](custom-converters.md) | ✅ | [✅](https://serdedotnet.github.io/customization.html)
Stateful converters       | [✅](custom-converters.md#stateful-converters) | ❌ | ❌ |
Deserialization callback  | [✅](xref:Nerdbank.MessagePack.IMessagePackSerializationCallbacks) | [✅](https://github.com/MessagePack-CSharp/MessagePack-CSharp?tab=readme-ov-file#serialization-callback) | ❌ |
MsgPack extensions        | ✅ | ✅ | ❌ |
LZ4 compression           | [❌](https://github.com/AArnott/Nerdbank.MessagePack/issues/34) | [✅](https://github.com/MessagePack-CSharp/MessagePack-CSharp?tab=readme-ov-file#lz4-compression) | ❌ |
Trim-safe                 | ✅ | ❌ | ✅ |
NativeAOT ready           | ✅ | ❌[^2] | ✅ |
Unity                     | [✅](unity.md)[^3] | [✅](https://github.com/MessagePack-CSharp/MessagePack-CSharp?tab=readme-ov-file#unity-support) | ❔ |
Async                     | [✅](xref:Nerdbank.MessagePack.MessagePackSerializer.SerializeAsync*) | ❌ | ❌ |
Endless streaming deserialization | [✅](streaming-deserialization.md) | [✅](https://github.com/MessagePack-CSharp/MessagePack-CSharp/?tab=readme-ov-file#multiple-messagepack-structures-on-a-single-stream) | ❌ |
Targeted partial deserialization | [✅](targeted-deserialization.md) | ❌ | ❌ |
Reference preservation    | [✅](xref:Nerdbank.MessagePack.MessagePackSerializer.PreserveReferences) | ❌ | ❌ |
Cyclical references       | [✅](xref:Nerdbank.MessagePack.MessagePackSerializer.PreserveReferences) | ❌ | ❌ |
JSON schema export        | [✅](xref:Nerdbank.MessagePack.MessagePackSerializer.GetJsonSchema*) | ❌ | ❌ |
Secure defaults           | [✅](security.md) | ❌ | ❌ |
Automatic hash collection deserialization in secure mode | ✅ | ✅ | ❌ |
Automatic collision-resistant hash function for arbitrary types | [✅](xref:Nerdbank.MessagePack.StructuralEqualityComparer) | ❌ | ❌ |
Rejection of data that defines multiple values for the same property | [✅](security.md#multiple-values-for-the-same-property) | ❌ | ❌ |
Free of mutable statics   | ✅ | ❌ | ✅ |
Structural `IEqualityComparer<T>` for arbitrary types | ✅ | ❌ | ❌ |

Security is a complex subject.
[Learn more about how to secure your deserializer](security.md).

[^1]: Nerdbank.MessagePack's approach is more likely to be correct by default and more flexible to fixing when it is not.
[^2]: Although MessagePack-CSharp does not support .NET 8 flavor NativeAOT, it has long-supported Unity's il2cpp runtime, but it requires careful avoidance of dynamic features.
[^3]: See our [unity doc](unity.md) for instructions.
[^4]: MessagePack-CSharp is limited to derived types that can be attributed on the base type, whereas Nerdbank.MessagePack allows for dynamically identifying derived types at runtime.
