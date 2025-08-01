# SignalR Hub Protocol

The `Nerdbank.MessagePack.SignalR` library provides a MessagePack-based Hub Protocol implementation for ASP.NET Core SignalR, offering significant performance improvements over the default JSON protocol.

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

## Server Configuration

Add the MessagePack protocol to your SignalR hub:

```csharp
using Nerdbank.MessagePack.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Add SignalR with MessagePack protocol
builder.Services.AddSignalR()
    .AddMessagePackProtocol();

var app = builder.Build();

// Configure hub endpoint
app.MapHub<ChatHub>("/chatHub");
```

### Custom Serializer Configuration

You can provide a custom MessagePack serializer with specific configuration:

```csharp
var customSerializer = new MessagePackSerializer(new MessagePackSerializerOptions
{
    // Your custom configuration
});

builder.Services.AddSignalR()
    .AddMessagePackProtocol(customSerializer);
```

### Manual Registration

For more control over service registration:

```csharp
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection.Extensions;

builder.Services.TryAddEnumerable(
    ServiceDescriptor.Singleton<IHubProtocol, MessagePackHubProtocol>());
```

## Client Configuration

For .NET SignalR clients, add the MessagePack protocol to your connection:

```csharp
using Microsoft.AspNetCore.SignalR.Client;
using Nerdbank.MessagePack.SignalR;

var connection = new HubConnectionBuilder()
    .WithUrl("https://example.com/chatHub")
    .AddMessagePackProtocol()
    .Build();

await connection.StartAsync();
```

### Client with Custom Serializer

```csharp
var customSerializer = new MessagePackSerializer(/* custom options */);

var connection = new HubConnectionBuilder()
    .WithUrl("https://example.com/chatHub")
    .AddMessagePackProtocol(customSerializer)
    .Build();
```

## Supported Message Types

The MessagePack Hub Protocol supports all SignalR message types:

- **InvocationMessage**: Method calls from client to server
- **StreamInvocationMessage**: Streaming method calls  
- **CompletionMessage**: Method call completions
- **StreamItemMessage**: Individual stream items
- **CancelInvocationMessage**: Stream cancellations
- **PingMessage**: Keep-alive pings
- **CloseMessage**: Connection close notifications

## Hub Implementation

Your SignalR hubs work exactly the same way with the MessagePack protocol:

```csharp
public class ChatHub : Hub
{
    public async Task SendMessage(string user, string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", user, message);
    }

    public async IAsyncEnumerable<string> StreamData(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (int i = 0; i < 10; i++)
        {
            yield return $"Data {i}";
            await Task.Delay(1000, cancellationToken);
        }
    }
}
```

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
- ASP.NET Core 6.0 and later
- .NET SignalR clients
- NativeAOT compilation
- All standard SignalR features (groups, user connections, etc.)

## Migration from JSON Protocol

Migration is straightforward and requires minimal code changes:

1. Install the `Nerdbank.MessagePack.SignalR` package
2. Add `.AddMessagePackProtocol()` to your SignalR registration
3. Update clients to use the MessagePack protocol
4. No changes needed to your Hub methods or client method calls

Both server and clients must use the same protocol for communication.