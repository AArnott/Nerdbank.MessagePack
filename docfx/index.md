# Overview

Nerdbank.MessagePack serializes .NET object graphs as [MessagePack](https://msgpack.io/): simple to use, and fast in both throughput and startup.

## Installation

```bash
dotnet add package Nerdbank.MessagePack
```

[![Nerdbank.MessagePack NuGet package](https://img.shields.io/nuget/v/Nerdbank.MessagePack.svg?label=Nerdbank.MessagePack)](https://www.nuget.org/packages/Nerdbank.MessagePack)

SignalR and ASP.NET Core MVC packages are covered in [Getting Started](docs/getting-started.md#installation).

## Highlights

* Serializes with the compact [MessagePack](https://msgpack.io/) format and is [among the fastest](docs/performance.md) .NET implementations.
* **No attributes required** on your data types. Optional attributes customize serialization and improve performance.
* **Modern C#**, including `required` and `init` properties, `record` types, and primary constructors.
* Ready for **NativeAOT**, trimming, **SignalR**, **ASP.NET Core MVC**, and [Unity](docs/unity.md).
* [Secure by default](docs/security.md) when deserializing untrusted data.
* [Polymorphic unions](docs/unions.md), [streaming deserialization](docs/streaming-deserialization.md), and [partial reads](docs/targeted-deserialization.md).

See the full list, and how this library compares with others, on the [Features](docs/features.md) page.

## Getting started

[Getting Started](docs/getting-started.md) walks through installation, shaping a type, and your first serialize/deserialize round-trip.
