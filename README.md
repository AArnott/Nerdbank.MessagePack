# Nerdbank.MessagePack

***A modern, fast and NativeAOT-compatible MessagePack serialization library***

[![NuGet package](https://img.shields.io/nuget/v/Nerdbank.MessagePack.svg)](https://nuget.org/packages/Nerdbank.MessagePack)

## Features

* Serializes in the compact and fast [MessagePack format](https://msgpack.org/).
* [Performance](#perf) is on par with the highly tuned and popular MessagePack-CSharp library.
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
* AOT source generation.

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

Only the top-level types that you serialize need the attribute.
All types that they reference will automatically have their 'shape' source generated as well so the whole object graph can be serialized.

### Witness classes

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

### Limitations

Not all types are suitable for serialization.
I/O types (e.g. `Steam`) or types that are more about function that data (e.g. `Task<T>`, `CancellationToken`) are not suitable for serialization.

## <a name="perf"></a>Performance

This library has superior startup performance compared to MessagePack-CSharp due to not relying on reflection and Ref.Emit.
Throughput performance is on par with MessagePack-CSharp.

When using AOT source generation from MessagePack-CSharp and objects serialized with maps (as opposed to arrays), MessagePack-CSharp is slightly faster at *de*serialization.
We may close this gap in the future by adding AOT source generation to *this* library as well.

## Why another MessagePack library?

MessagePack-CSharp is a great library, and in fact is chiefly maintained by the same author as *this* library.
Here are some reasons a new library was created:

* MessagePack-CSharp has a long history and breaking changes are difficult to introduce.
* MessagePack-CSharp was not "Native AOT" compatible nor trim-friendly (although it has a long history of getting *mostly* there through various tricks).
* Nerdbank.MessagePack is based on `[GenerateShape]`, so it is *far* simpler than MessagePack-CSharp to author and maintain.
* Nerdbank.MessagePack has no mutable statics, with the functional unpredictability that can bring.
* Nerdbank.MessagePack can dynamically create converters with various options that may vary from other uses within the same process, providing more flexibility than MessagePack-CSharp's strict generic type static storage mechanism.
* Nerdbank.MessagePack is far simpler to use. One attribute at the base of an object graph is typically all you need. MessagePack-CSharp demands attributes on every single type and every single field or property (even members that will not be serialized).
* Nerdbank.MessagePack makes adding some long-sought for features from MessagePack-CSharp far easier to implement.
