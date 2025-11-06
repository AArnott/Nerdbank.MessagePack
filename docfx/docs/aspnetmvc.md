# ASP.NET MVC formatters

This library provides MessagePack-based formatters for [ASP.NET MVC](https://github.com/dotnet/aspnetcore), offering significant performance improvements over the default JSON protocol.

## Benefits

- **Smaller Payloads**: MessagePack produces significantly smaller payloads compared to JSON
- **Faster Serialization**: Binary serialization is typically faster than text-based formats
- **Type Safety**: Leverages MessagePack's type-safe serialization system
- **NativeAOT Compatible**: Works seamlessly with .NET Native AOT compilation

## Installation

Install the NuGet package:

[![Nerdbank.MessagePack.AspNetCoreMvcFormatter NuGet package](https://img.shields.io/nuget/v/Nerdbank.MessagePack.AspNetCoreMvcFormatter.svg?label=Nerdbank.MessagePack.AspNetCoreMvcFormatter)](https://www.nuget.org/packages/Nerdbank.MessagePack.AspNetCoreMvcFormatter)

Add <xref:Nerdbank.MessagePack.AspNetCoreMvcFormatter.MessagePackInputFormatter> and/or <xref:Nerdbank.MessagePack.AspNetCoreMvcFormatter.MessagePackOutputFormatter> to your input and/or output formatter collections respectively, as demonstrated in the configuration sample below:

[!code-csharp[](../../samples/AspNetMvc/Program.cs#Configuration)]

## Usage

### Server

Add `[Produces("application/x-msgpack")]` to the action or controller that returns msgpack-encoded data.

[!code-csharp[](../../samples/AspNetMvc/Controllers/PersonController.cs#Controller)]

### Client

The JavaScript client should send data with `application/x-msgpack` as the HTTP `Content-Type` header.
The `Accept` header should include this same content-type so that the server will utilize the MessagePack formatter to send the optimized data format back to the client.

[!code-html[](../../samples/AspNetMvc/Views/Home/Index.cshtml)]
