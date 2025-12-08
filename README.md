﻿# Nerdbank.MessagePack

***A modern, fast and NativeAOT-compatible MessagePack serialization library***

[![Docs](https://img.shields.io/badge/docs-blue)](https://aarnott.github.io/Nerdbank.MessagePack/)
[![codecov](https://codecov.io/gh/AArnott/Nerdbank.MessagePack/graph/badge.svg?token=CLMWEX3M3W)](https://codecov.io/gh/AArnott/Nerdbank.MessagePack)
[![🏭 Build](https://github.com/AArnott/Nerdbank.MessagePack/actions/workflows/build.yml/badge.svg)](https://github.com/AArnott/Nerdbank.MessagePack/actions/workflows/build.yml)

[![Nerdbank.MessagePack NuGet package](https://img.shields.io/nuget/v/Nerdbank.MessagePack.svg?label=Nerdbank.MessagePack)](https://www.nuget.org/packages/Nerdbank.MessagePack)<br />
[![Nerdbank.MessagePack.SignalR NuGet package](https://img.shields.io/nuget/v/Nerdbank.MessagePack.SignalR.svg?label=Nerdbank.MessagePack.SignalR)](https://www.nuget.org/packages/Nerdbank.MessagePack.SignalR)<br />
[![Nerdbank.MessagePack.AspNetCoreMvcFormatter NuGet package](https://img.shields.io/nuget/v/Nerdbank.MessagePack.AspNetCoreMvcFormatter.svg?label=Nerdbank.MessagePack.AspNetCoreMvcFormatter)](https://www.nuget.org/packages/Nerdbank.MessagePack.AspNetCoreMvcFormatter)

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
* Deserialize [just the fragment you require](https://aarnott.github.io/Nerdbank.MessagePack/docs/targeted-deserialization.html) with intuitive LINQ expressions.
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

See [a feature comparison table](https://aarnott.github.io/Nerdbank.MessagePack/docs/features.html#feature-comparison) that compares the two libraries.

## Consuming CI builds

You can acquire CI build packages (with no assurance of quality) to get early access to the latest changes without waiting for the next release to nuget.org.

There are two feeds you can use to acquire these packages:

- [GitHub Packages](https://github.com/AArnott?tab=packages&repo_name=Nerdbank.MessagePack) (requires GitHub authentication)
- [Azure Artifacts](https://dev.azure.com/andrewarnott/OSS/_artifacts/feed/PublicCI) (no authentication required)

## Sponsorships

[GitHub Sponsors](https://github.com/sponsors/AArnott)

<details>
<summary>Zcash</summary>

Address: u1vv2ws6xhs72faugmlrasyeq298l05rrj6wfw8hr3r29y3czev5qt4ugp7kylz6suu04363ze92dfg8ftxf3237js0x9p5r82fgy47xkjnw75tqaevhfh0rnua72hurt22v3w3f7h8yt6mxaa0wpeeh9jcm359ww3rl6fj5ylqqv54uuwrs8q4gys9r3cxdm3yslsh3rt6p7wznzhky7

</details>
