# ASP.NET MVC formatters

This library provides MessagePack-based formatters for [ASP.NET MVC](https://github.com/dotnet/aspnetcore), offering significant performance improvements over the default JSON protocol.

[![NuGet package](https://img.shields.io/nuget/v/Nerdbank.MessagePack.AspNetCoreMvcFormatter.svg)](https://nuget.org/packages/Nerdbank.MessagePack.AspNetCoreMvcFormatter)

## Benefits

- **Smaller Payloads**: MessagePack produces significantly smaller payloads compared to JSON
- **Faster Serialization**: Binary serialization is typically faster than text-based formats
- **Type Safety**: Leverages MessagePack's type-safe serialization system
- **NativeAOT Compatible**: Works seamlessly with .NET Native AOT compilation

## Installation

Install the NuGet package:

```xml
<PackageReference Include="Nerdbank.MessagePack.AspNetCoreMvcFormatter" Version="x.x.x" />
```

Add <xref:Nerdbank.MessagePack.AspNetCoreMvcFormatter.MessagePackInputFormatter> and/or <xref:Nerdbank.MessagePack.AspNetCoreMvcFormatter.MessagePackOutputFormatter> to your input and/or output formatter collecionts respectively, as demonstrated in the configuration sample below:

[!code-csharp[](../../samples/AspNetMvc/Program.cs#Configuration)]

## Usage

The JavaScript client should send data with `application/x-msgpack` as the HTTP `Content-Type` header.
The `Accept` header should include this same content-type so that the server will utilize the MessagePack formatter to send the optimized data format back to the client.
