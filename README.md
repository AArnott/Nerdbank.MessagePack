# Nerdbank.MessagePack

***A modern, fast and NativeAOT-compatible MessagePack serialization library***

[![NuGet package](https://img.shields.io/nuget/v/Nerdbank.MessagePack.svg)](https://nuget.org/packages/Nerdbank.MessagePack)

## Features

* Serializes is the compact and fast [MessagePack format](https://msgpack.org/).
* Automatically serialize any type annotated with the [TypeShape-csharp](https://github.com/eiriktsarpalis/typeshape-csharp) `[GenerateShape]` attribute.
* Fast `ref`-based serialization and deserialization minimizes copying of large structs.
* NativeAOT compatible.
* Primitive msgpack reader and writer APIs for low-level scenarios.
* Author custom converters for advanced scenarios.
* Security mitigations for stack overflows.

### Potential future features

* Automatically serialize non-annotated types by adding a 'witness' type with a similar annotation.
* Security mitigations for hash collision attacks.
* Serialize only "changes" to an object graph and deserialize onto existing objects to apply those changes.
* Async serialization and deserialization.
* Streaming deserialization.
* Optional LZ4 compression.

### Analyzer ideas

* Converters should *not* call out to the top-level serialization functions (as this would bypass the depth check and user options).
* Write only one value -- or an (optional) runtime check.
* Converters of reference types should always use a nullable ref annotation on that type.

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
Sequence<byte> buffer = new();

// Serialize the value to the buffer.
serializer.Serialize(buffer, value);

// Deserialize it back.
var deserialized = this.serializer.Deserialize<ARecord>(buffer);
```
