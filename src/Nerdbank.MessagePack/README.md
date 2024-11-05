# Nerdbank.MessagePack

***A modern, fast and NativeAOT-compatible MessagePack serialization library***

[Learn everything on our docs site](https://aarnott.github.io/Nerdbank.MessagePack/).

## Features

* Serializes in the compact and fast [MessagePack format](https://msgpack.org/).
* [Performance](#perf) is on par with the highly tuned and popular MessagePack-CSharp library.
* Automatically serialize any type annotated with the [TypeShape-csharp](https://github.com/eiriktsarpalis/typeshape-csharp) `[GenerateShape]` attribute.
* Automatically serialize non-annotated types by adding [a 'witness' type](#witness) with a similar annotation.
* Fast `ref`-based serialization and deserialization minimizes copying of large structs.
* NativeAOT and trimming compatible.
* Keep memory pressure low by using async serialization directly to/from I/O like a network, IPC pipe or file.
* Primitive msgpack reader and writer APIs for low-level scenarios.
* Author custom converters for advanced scenarios.
* Security mitigations for stack overflows.
* Optionally serialize your custom types as arrays of bvalues instead of maps of names and value for more compact representation and even higher performance.
* Support for serializing instances of certain types derived from the declared type and deserializing them back to their original runtime types using [unions](https://github.com/aarnott/nerdbank.messagepack/tree/main/doc/unions.md).
