This library provides a MessagePack-based Hub Protocol implementation for ASP.NET Core SignalR, offering significant performance improvements over the default JSON protocol.

## Benefits

- **High Performance**: Uses Nerdbank.MessagePack for fast, compact binary serialization
- **Type Safety**: Leverages MessagePack's type-safe serialization
- **Easy Integration**: Simple extension methods for service registration
- **Full SignalR Support**: Implements all SignalR message types

## Usage

Add a call to [`AddMessagePackProtocol`](https://aarnott.github.io/Nerdbank.MessagePack/api/Nerdbank.MessagePack.SignalR.ServiceCollectionExtensions.html#Nerdbank_MessagePack_SignalR_ServiceCollectionExtensions_AddMessagePackProtocol_) to your builder class.

See more samples and help [on our doc page](https://aarnott.github.io/Nerdbank.MessagePack/docs/signalr.html).
