# SignalR

This library provides a MessagePack-based Hub Protocol implementation for ASP.NET Core SignalR, offering significant performance improvements over the default JSON protocol.

[![Nerdbank.MessagePack.SignalR NuGet package](https://img.shields.io/nuget/v/Nerdbank.MessagePack.SignalR.svg?label=Nerdbank.MessagePack.SignalR)](https://www.nuget.org/packages/Nerdbank.MessagePack.SignalR)

## Benefits

- **Smaller Payloads**: MessagePack produces significantly smaller payloads compared to JSON
- **Faster Serialization**: Binary serialization is typically faster than text-based formats
- **Type Safety**: Leverages MessagePack's type-safe serialization system
- **NativeAOT Compatible**: Works seamlessly with .NET Native AOT compilation

## Installation

Install the NuGet package:

```xml
<PackageReference Include="Nerdbank.MessagePack.SignalR" Version="x.x.x" />
```

Add a call to <xref:Nerdbank.MessagePack.SignalR.ServiceCollectionExtensions.AddMessagePackProtocol*> to your builder class, as demonstrated in the configuration samples below.

## Configuration

SignalR being an RPC system requires a provider for type shapes for all parameter and return types used in your RPC methods.
This can be <xref:PolyType.ReflectionProvider.ReflectionTypeShapeProvider.Default?displayProperty=nameWithType> or (preferably) via the PolyType source generator using a [witness class](type-shapes.md)

The sample configurations below will be using the witness class approach.

[!code-csharp[](../../samples/SignalR/Program.cs#Witness)]

### Server Configuration

Add the MessagePack protocol to your SignalR hub:

[!code-csharp[](../../samples/SignalR/Program.cs#BasicSample)]

#### Custom Serializer Configuration

You can provide a custom MessagePack serializer with specific configuration:

[!code-csharp[](../../samples/SignalR/Program.cs#CustomizedSerializer)]

### Client Configuration

For .NET SignalR clients, add the MessagePack protocol to your connection:

[!code-csharp[](../../samples/SignalR/Client.cs#Basic)]

#### Client with Custom Serializer

[!code-csharp[](../../samples/SignalR/Client.cs#CustomSerializer)]

## Supported Message Types

The MessagePack Hub Protocol supports all SignalR message types:

- `InvocationMessage`: Method calls from client to server
- `StreamInvocationMessage`: Streaming method calls
- `CompletionMessage`: Method call completions
- `StreamItemMessage`: Individual stream items
- `CancelInvocationMessage`: Stream cancellations
- `PingMessage`: Keep-alive pings
- `CloseMessage`: Connection close notifications
- `AckMessage`: Acknowledgements
- `SequenceMessage`: Sequence messages

## Hub Implementation

Your SignalR hubs work exactly the same way with the MessagePack protocol:

[!code-csharp[](../../samples/SignalR/ChatHub.cs#Sample)]

## Performance Considerations

The MessagePack protocol generally provides better performance characteristics:

- **Bandwidth**: 20-50% smaller payload sizes compared to JSON
- **CPU**: Faster serialization/deserialization, especially for complex objects
- **Memory**: Lower memory allocation during serialization

Consider using MessagePack when:
- You have high-frequency message exchanges
- Your messages contain complex data structures
- Bandwidth is a concern (mobile applications, metered connections)
- You need maximum performance

## Compatibility

The MessagePack Hub Protocol is compatible with:
- ASP.NET Core 8.0 and later
- .NET SignalR clients
- NativeAOT compilation
- All standard SignalR features (groups, user connections, etc.)

While SignalR comes with its own MessagePack implementation, it does so by relying on the MessagePack-CSharp library, which is harder to use and not NativeAOT safe.
Nerdbank.MessagePack.SignalR provides a comparably high performance, easier to use and NativeAOT-safe alternative.
It is compatible with other MessagePack implementations that follow the same protocol as prescribed by SignalR [in their spec](https://github.com/dotnet/aspnetcore/blob/main/src/SignalR/docs/specs/HubProtocol.md#messagepack-msgpack-encoding).

## Migration from JSON Protocol

Migration is straightforward and requires minimal code changes:

1. Install the `Nerdbank.MessagePack.SignalR` package
2. Add .<xref:Nerdbank.MessagePack.SignalR.ServiceCollectionExtensions.AddMessagePackProtocol*> to your SignalR registration
3. Update clients to use the MessagePack protocol
4. No changes needed to your Hub methods or client method calls

Both server and clients must use the same protocol for communication.
