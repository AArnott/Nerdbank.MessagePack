# Nerdbank.MessagePack

***A modern, fast and NativeAOT-compatible MessagePack serialization library***

This is a fast and more user-friendly MessagePack serialization library for .NET and .NET Framework.
This package is brought to you by one of the two major contributors to MessagePack-CSharp.
As its natural successor, this library comes packed with features that its predecessor lacks, and has ongoing support.

## Features

* Serializes in the compact and fast [MessagePack format](https://msgpack.io/).
* [Performance](https://aarnott.github.io/Nerdbank.MessagePack/docs/performance.html) is on par with the highly tuned and popular MessagePack-CSharp library.
* Automatically serialize any type annotated with the [PolyType `[GenerateShape]`](https://eiriktsarpalis.github.io/PolyType/api/PolyType.GenerateShapeAttribute.html) attribute
  or non-annotated types by adding [a 'witness' type](https://aarnott.github.io/Nerdbank.MessagePack/docs/type-shapes.html#witness-classes) with a similar annotation.
* Fast `ref`-based serialization and deserialization minimizes copying of large structs.
* NativeAOT and trimming compatible.
* Serialize only properties that have non-default values (optionally).
* Keep memory pressure low by using async serialization directly to/from I/O like a network, IPC pipe or file.
* [Streaming deserialization](https://aarnott.github.io/Nerdbank.MessagePack/docs/streaming-deserialization.html) for large or over-time sequences.
* Primitive msgpack reader and writer APIs for low-level scenarios.
* Author custom converters for advanced scenarios.
* Security mitigations for stack overflows.
* Optionally serialize your custom types as arrays of values instead of maps of names and value for more compact representation and even higher performance.
* Support for serializing instances of certain types derived from the declared type and deserializing them back to their original runtime types using [unions](https://aarnott.github.io/Nerdbank.MessagePack/docs/unions.html).
* Optionally [preserve reference equality](https://aarnott.github.io/Nerdbank.MessagePack/api/Nerdbank.MessagePack.MessagePackSerializer.html#Nerdbank_MessagePack_MessagePackSerializer_PreserveReferences) across serialization/deserialization.
* Structural (i.e. deep, by-value) equality checking for arbitrary types, both with and without collision resistant hash functions.

[See how these features and more compare with the leading MessagePack library](https://aarnott.github.io/Nerdbank.MessagePack/docs/features.html#feature-comparison).
