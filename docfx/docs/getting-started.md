# Getting Started

## Installation

Consume this library via its NuGet Package.
Click on the badge to find its latest version and the instructions for consuming it that best apply to your project.

[![NuGet package](https://img.shields.io/nuget/v/Nerdbank.MessagePack.svg)](https://nuget.org/packages/Nerdbank.MessagePack)

## Usage

Given a type annotated with [`GenerateShapeAttribute`](xref:TypeShape.GenerateShapeAttribute) like this:

```cs
[GenerateShape]
public partial record ARecord(string AString, bool ABoolean);
```

You can serialize and deserialize it like this:

```cs
// Construct a value.
var value = new ARecord("hello", true);

// Create a serializer instance.
MessagePackSerializer serializer = new();

// Serialize the value to the buffer.
byte[] msgpack = serializer.Serialize(value);

// Deserialize it back.
var deserialized = serializer.Deserialize<ARecord>(msgpack);
```

Only the top-level types that you serialize need the attribute.
All types that they reference will automatically have their 'shape' source generated as well so the whole object graph can be serialized.

## Witness classes

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
byte[] msgpack = serializer.Serialize<FamilyTree, Witness>(buffer, familyTree);
```

Note the only special bit is providing the `Witness` class as a type argument to the `Serialize` method.
The *name* of the witness class is completely inconsequential.
A witness class may have any number of `GenerateShapeAttribute<T>` attributes on it.
It is typical (but not required) for an assembly to have at most one witness class, with all the external types listed on it that you need to serialize as top-level objects.

You do *not* need a witness class for an external type to reference that type from a graph that is already rooted in a type that *is* attributed.

### Limitations

Not all types are suitable for serialization.
I/O types (e.g. `Steam`) or types that are more about function that data (e.g. `Task<T>`, `CancellationToken`) are not suitable for serialization.

Typeless serialization is not supported.
For security and trim-friendly reasons, the type of the object being deserialized must be known at compile time.

## Converting to JSON

It can sometimes be useful to understand what msgpack is actually being serialized.
Msgpack being a binary format makes looking at the serialized buffer less than helpful for most folks.

You may use the @"Nerdbank.MessagePack.MessagePackSerializer.ConvertToJson*" method to convert most msgpack buffers to JSON for human inspection.

It is important to note that not all msgpack is expressible as JSON.
In particular the following limitations apply:

* Msgpack maps allow for any type to serve as the key. JSON only supports strings. In such cases, the rendered JSON will emit the msgpack key as-is, and the result will be human-readable but not valid JSON.
* Msgpack supports arbitrary binary extensions. In JSON this will be rendered as a base64-encoded string with an "msgpack extension {typecode} as base64: " prefix.
* Msgpack supports binary blobs. In JSON this will be rendered as a base64-encoded string with an "msgpack binary as base64: " prefix.

The exact JSON emitted, especially for the msgpack-only tokens, is subject to change in future versions of this library.
You should *not* write programs that are expected to parse the JSON produced by this diagnostic method.
Use a JSON serialization library if you want interop-safe, machine-parseable JSON.
