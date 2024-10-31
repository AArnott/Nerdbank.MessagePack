# Nerdbank.MessagePack

***A modern, fast and NativeAOT-compatible MessagePack serialization library***

[Read more at our project repo](https://github.com/aarnott/nerdbank.messagepack?tab=readme-ov-file#readme)

## Features

* Serializes in the compact and fast [MessagePack format](https://msgpack.org/).
* [Performance](#perf) is on par with the highly tuned and popular MessagePack-CSharp library.
* Automatically serialize any type annotated with the [TypeShape-csharp](https://github.com/eiriktsarpalis/typeshape-csharp) `[GenerateShape]` attribute.
* Automatically serialize non-annotated types by adding [a 'witness' type](#witness) with a similar annotation.
* Fast `ref`-based serialization and deserialization minimizes copying of large structs.
* NativeAOT and trimming compatible.
* Primitive msgpack reader and writer APIs for low-level scenarios.
* Author custom converters for advanced scenarios.
* Security mitigations for stack overflows.
* Optionally serialize your custom types as arrays of bvalues instead of maps of names and value for more compact representation and even higher performance.
* Support for serializing instances of certain types derived from the declared type and deserializing them back to their original runtime types using [unions](https://github.com/aarnott/nerdbank.messagepack/tree/main/doc/unions.md).

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

Only the top-level types that you serialize need the attribute.
All types that they reference will automatically have their 'shape' source generated as well so the whole object graph can be serialized.

### <a name="witness"></a>Witness classes

If you need to directly serialize a type that isn't declared in your project and is not annotated with `[GenerateShape]`, you can define another class in your own project to provide that shape.
Doing so leads to default serialization rules being applied to the type (e.g. only public members are serialized).

For this example, suppose you consume a `FamilyTree` type from a library that you don't control and did not annotate their type for serialization.
In your own project, you can define this witness type:

```cs
[GenerateShape<FamilyTree>]
partial class Witness;
```

You may then serialize a family tree like this:

```cs
var familyTree = new FamilyTree();
var serializer = new MessagePackSerializer();
var buffer = new Sequence<byte>();
serializer.Serialize<FamilyTree, Witness>(buffer, familyTree);
```

Note the only special bit is providing the `Witness` class as a type argument to the `Serialize` method.
The *name* of the witness class is completely inconsequential.
A witness class may have any number of `GenerateShapeAttribute<T>` attributes on it.
It is typical (but not required) for an assembly to have at most one witness class, with all the external types listed on it that you need to serialize as top-level objects.

You do *not* need a witness class for an external type to reference that type from a graph that is already rooted in a type that *is* attributed.

## <a name="perf"></a>Performance

This library has superior startup performance compared to MessagePack-CSharp due to not relying on reflection and Ref.Emit.
Throughput performance is on par with MessagePack-CSharp.

When using AOT source generation from MessagePack-CSharp and objects serialized with maps (as opposed to arrays), MessagePack-CSharp is slightly faster at *de*serialization.
We may close this gap in the future by adding AOT source generation to *this* library as well.
