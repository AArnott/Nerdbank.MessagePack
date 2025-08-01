# Nerdbank.MessagePack.SignalR

This library provides a SignalR Hub Protocol implementation using Nerdbank.MessagePack for efficient binary serialization.

## Features

- **High Performance**: Uses Nerdbank.MessagePack for fast, compact binary serialization
- **Type Safety**: Leverages MessagePack's type-safe serialization
- **Easy Integration**: Simple extension methods for service registration
- **Full SignalR Support**: Implements all SignalR message types

## Usage

### Server-side Registration

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddSignalR()
        .AddMessagePackProtocol();
}
```

### Client-side Registration

```csharp
var connection = new HubConnectionBuilder()
    .WithUrl("/chatHub")
    .AddMessagePackProtocol()
    .Build();
```

### Custom Serializer

You can also provide a custom MessagePack serializer:

```csharp
var customSerializer = MessagePackSerializer.Create(/* custom configuration */);

services.AddSignalR()
    .AddMessagePackProtocol(customSerializer);
```

### Manual Registration

For more control, you can manually register the protocol:

```csharp
services.TryAddEnumerable(ServiceDescriptor.Singleton<IHubProtocol, MessagePackHubProtocol>());
```

## Benefits

- **Smaller Payload**: MessagePack produces smaller payloads compared to JSON
- **Faster Serialization**: Binary serialization is typically faster than text-based formats
- **Type Preservation**: Better type preservation for complex objects
- **NativeAOT Compatible**: Works with .NET Native AOT compilation