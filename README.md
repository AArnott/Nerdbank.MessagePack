# Nerdbank.MessagePack

***A modern, fast and NativeAOT-compatible MessagePack serialization library***

[![NuGet package](https://img.shields.io/nuget/v/Nerdbank.MessagePack.svg)](https://nuget.org/packages/Nerdbank.MessagePack)
[![codecov](https://codecov.io/gh/AArnott/Nerdbank.MessagePack/graph/badge.svg?token=CLMWEX3M3W)](https://codecov.io/gh/AArnott/Nerdbank.MessagePack)

## Features

* Serializes in the compact and fast [MessagePack format](https://msgpack.org/).
* [Performance](#perf) is on par with the highly tuned and popular MessagePack-CSharp library.
* Automatically serialize any type annotated with the [PolyType](https://github.com/eiriktsarpalis/PolyType) `[GenerateShape]` attribute.
* Automatically serialize non-annotated types by adding [a 'witness' type](https://aarnott.github.io/Nerdbank.MessagePack/docs/getting-started.html#witness) with a similar annotation.
* Fast `ref`-based serialization and deserialization minimizes copying of large structs.
* NativeAOT and trimming compatible.
* Keep memory pressure low by using async serialization directly to/from I/O like a network, IPC pipe or file.
* Primitive msgpack reader and writer APIs for low-level scenarios.
* Author custom converters for advanced scenarios.
* Security mitigations for stack overflows.
* Optionally serialize your custom types as arrays of values instead of maps of names and value for more compact representation and even higher performance.
* Support for serializing instances of certain types derived from the declared type and deserializing them back to their original runtime types using [unions](https://aarnott.github.io/Nerdbank.MessagePack/docs/unions.html).

## Usage

Given a type annotated with `[GenerateShape]` like this:

```cs
[GenerateShape]
public partial record ARecord(string AString, bool ABoolean, float AFloat, double ADouble);
```

You can serialize and deserialize it like this:

```cs
// Construct a value.
var value = new ARecord("hello", true, 1.0f, 2.0);

// Create a serializer instance.
MessagePackSerializer serializer = new();

// Serialize the value to the buffer.
byte[] msgpack = serializer.Serialize(value);

// Deserialize it back.
var deserialized = serializer.Deserialize<ARecord>(msgpack);
```

Only the top-level types that you serialize need the attribute.
All types that they reference will automatically have their 'shape' source generated as well so the whole object graph can be serialized.

## <a name="perf"></a>Performance

This library has superior startup performance compared to MessagePack-CSharp due to not relying on reflection and Ref.Emit.
Throughput performance is on par with MessagePack-CSharp.

When using AOT source generation from MessagePack-CSharp and objects serialized with maps (as opposed to arrays), MessagePack-CSharp is slightly faster at *de*serialization.
We may close this gap in the future by adding AOT source generation to *this* library as well.

## Why another MessagePack library?

[MessagePack-CSharp](https://github.com/MessagePack-CSharp/MessagePack-CSharp) is a great library, and in fact is chiefly maintained by the same author as *this* library.
Here are some reasons a new library was created:

* MessagePack-CSharp has a long history and breaking changes are difficult to introduce.
* MessagePack-CSharp was not "Native AOT" compatible nor trim-friendly (although it has a long history of getting *mostly* there through various tricks).
* Nerdbank.MessagePack is based on `[GenerateShape]`, so it is *far* simpler than MessagePack-CSharp to author and maintain.
* Nerdbank.MessagePack has no mutable statics, with the functional unpredictability that can bring.
* Nerdbank.MessagePack can dynamically create converters with various options that may vary from other uses within the same process, providing more flexibility than MessagePack-CSharp's strict generic type static storage mechanism.
* Nerdbank.MessagePack is far simpler to use. One attribute at the base of an object graph is typically all you need. MessagePack-CSharp demands attributes on every single type and every single field or property (even members that will not be serialized).
* Nerdbank.MessagePack makes adding some long-sought for features from MessagePack-CSharp far easier to implement.

See [a feature comparison table](https://aarnott.github.io/Nerdbank.MessagePack/docs/migrating.html#feature-comparison) that compares the two libraries.
