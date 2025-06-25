# Getting Started

## Installation

Consume this library via its NuGet Package.
Click on the badge to find its latest version and the instructions for consuming it that best apply to your project.

[![NuGet package](https://img.shields.io/nuget/v/Nerdbank.MessagePack.svg)](https://nuget.org/packages/Nerdbank.MessagePack)

### C# language version

When your project targets .NET Framework or .NET Standard 2.x, the C# language version may default to 7.3.
C# 12 introduced support for generic attributes, which is a fundamental requirement PolyType, upon which Nerdbank.MessagePack is based.
If you find generic attributes are not allowed in your C# code, change your .csproj file to include this snippet:

```xml
<PropertyGroup>
  <LangVersion>12</LangVersion>
</PropertyGroup>
```

Using the latest C# language version (even when targeting older runtimes like .NET Framework) is generally a Good Thing.
C# automatically produces errors if you try to use certain newer language features that requires a newer runtime.

## Usage

Given a type annotated with <xref:PolyType.GenerateShapeAttribute> like this:

[!code-csharp[](../../samples/cs/GettingStarted.cs#SimpleRecord)]

> [!IMPORTANT]
> All types attributed with <xref:PolyType.GenerateShapeAttribute> must be declared with the `partial` modifier.
> If these are nested types, all containing types must also have the `partial` modifier.

Only the top-level types that you serialize need <xref:PolyType.GenerateShapeAttribute>.
All types that they reference will automatically have their 'shape' source generated as well so the whole object graph can be serialized.
This means that only the top-level types need to be declared with the `partial` modifier.

You can serialize and deserialize it like this:

# [.NET](#tab/net)

[!code-csharp[](../../samples/cs/GettingStarted.cs#SimpleRecordRoundtripNET)]

# [.NET Standard](#tab/netfx)

[!code-csharp[](../../samples/cs/GettingStarted.cs#SimpleRecordRoundtripNETFX)]

---

If you need to directly serialize a type that isn't declared in your project and is not annotated with `[GenerateShape]`, you can define another class in your own project to provide that shape.
Learn more about [witness classes](type-shapes.md#witness-classes).

Learn more about serialization policies and how to customize them over at [Customizing Serialization](customizing-serialization.md).

### Limitations

Not all types are suitable for serialization.
I/O types (e.g. `Steam`) or types that are more about function that data (e.g. `Task<T>`, `CancellationToken`) are not suitable for serialization.

For security and trim-friendly reasons, the type of the object being deserialized must be known at compile time, by default.
An [optional `object` converter](xref:Nerdbank.MessagePack.OptionalConverters.WithObjectConverter*) can be used to serialize any runtime type for which a shape is available. It will deserialize into maps, arrays, and primitives rather than the original type.
[Custom converters](custom-converters.md) can be written to overcome these limitations where required.

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

## Untyped deserialization

When you do not have types declared that resemble the msgpack schema you need to deserialize, you can use the <xref:Nerdbank.MessagePack.MessagePackSerializer.DeserializeDynamicPrimitives*?displayProperty=nameWithType> method.

[!code-csharp[](../../samples/cs/PrimitiveDeserialization.cs#DeserializeDynamicPrimitives)]

Or if you don't like the `dynamic` keyword, you can use the dictionary approach using <xref:Nerdbank.MessagePack.MessagePackSerializer.DeserializePrimitives*?displayProperty=nameWithType>:

[!code-csharp[](../../samples/cs/PrimitiveDeserialization.cs#DeserializePrimitives)]

Note that the `dynamic` approach allows the dictionary indexer syntax as well, but does not require any casting.

A built-in @System.Dynamic.ExpandoObject converter is included in the library as well.
It will deserialize any msgpack structure into primitives and simple structures.
Serializing an `ExpandoObject` requires that serialization start with a shape provider that can describe every runtime type in the object graph.

# [.NET](#tab/net)

[!code-csharp[](../../samples/cs/PrimitiveDeserialization.cs#DeserializeExpandoObjectNET)]

# [.NET Standard](#tab/netfx)

[!code-csharp[](../../samples/cs/PrimitiveDeserialization.cs#DeserializeExpandoObjectNETFX)]

---
