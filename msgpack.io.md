# Nerdbank.MessagePack

***A modern, fast and NativeAOT-compatible MessagePack serialization library***

[![NuGet package](https://img.shields.io/nuget/v/Nerdbank.MessagePack.svg)](https://www.nuget.org/packages/Nerdbank.MessagePack)
[![Docs](https://img.shields.io/badge/docs-blue)](https://aarnott.github.io/Nerdbank.MessagePack/)
[![codecov](https://codecov.io/gh/AArnott/Nerdbank.MessagePack/graph/badge.svg?token=CLMWEX3M3W)](https://codecov.io/gh/AArnott/Nerdbank.MessagePack)
[![🏭 Build](https://github.com/AArnott/Nerdbank.MessagePack/actions/workflows/build.yml/badge.svg)](https://github.com/AArnott/Nerdbank.MessagePack/actions/workflows/build.yml)

## Features

See [a side-by-side feature comparison across popular libraries](https://aarnott.github.io/Nerdbank.MessagePack/docs/features.html#feature-comparison).

* Serializes in the compact and fast [MessagePack format](https://msgpack.io/).
* **No attributes required** on your data types. Optional attributes allow customization and improve performance.
* **Supports the latest C# syntax** including `required` and `init` properties, `record` classes and structs, and primary constructors.
* This library is [perf-optimized](https://aarnott.github.io/Nerdbank.MessagePack/docs/performance.html) and is **among the fastest** MessagePack serialization libraries available for .NET.
* Works *great* in your **NativeAOT**, trimmed, **SignalR** or **ASP.NET Core MVC** applications or [**Unity**](https://aarnott.github.io/Nerdbank.MessagePack/docs/unity.html) games.
* Many [C# analyzers](https://aarnott.github.io/Nerdbank.MessagePack/analyzers/index.html) to help you avoid common mistakes.
* [Great security](https://aarnott.github.io/Nerdbank.MessagePack/docs/security.html) for deserializing untrusted data.
* [Polymorphic deserialization](https://aarnott.github.io/Nerdbank.MessagePack/docs/unions.html) lets you deserialize derived types.
* True async and [streaming deserialization](https://aarnott.github.io/Nerdbank.MessagePack/docs/streaming-deserialization.html) for large or over-time sequences keeps your apps responsive and memory pressure low.
* [Preserve reference equality](https://aarnott.github.io/Nerdbank.MessagePack/api/Nerdbank.MessagePack.MessagePackSerializer.html#Nerdbank_MessagePack_MessagePackSerializer_PreserveReferences) across serialization/deserialization (optional).
* [Forward compatible data retention](https://aarnott.github.io/Nerdbank.MessagePack/docs/customizing-serialization.html#retaining-unrecognized-data) allows you to deserialize and re-serialize data without dropping properties you didn't know about.
* [Structural equality checking](https://aarnott.github.io/Nerdbank.MessagePack/docs/structural-equality.html) and hashing *for arbitrary types* gives you deep by-value equality semantics without hand-authoring `Equals` and `GetHashCode` overrides.
* **No mutable statics** ensures your code runs properly no matter what other code might run in the same process.
* Only serialize properties with [non-default values](https://aarnott.github.io/Nerdbank.MessagePack/api/Nerdbank.MessagePack.MessagePackSerializer.html#Nerdbank_MessagePack_MessagePackSerializer_SerializeDefaultValues) (optional).
* Primitive msgpack reader and writer APIs for low-level scenarios.
* Author custom converters for advanced scenarios.

## Usage

Given a data type like this:

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

The `[GenerateShape]` attribute is highly encouraged because it boosts startup performance and ensures NativeAOT and trim safety.
Only the top-level type that you serialize needs the attribute.
All types that it references will automatically have their 'shape' source generated as well so the whole object graph can be serialized quickly and safely.

Learn more in our [getting started doc](https://aarnott.github.io/Nerdbank.MessagePack/docs/getting-started.html).
