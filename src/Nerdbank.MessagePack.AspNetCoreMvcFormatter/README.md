This library provides MessagePack-based formatters for [ASP.NET MVC](https://github.com/dotnet/aspnetcore), offering significant performance improvements over the default JSON protocol.

## Benefits

- **High Performance**: Uses Nerdbank.MessagePack for fast, compact binary serialization
- **Type Safety**: Leverages MessagePack's type-safe serialization
- **Easy Integration**: Simple extension methods for service registration
- **Full SignalR Support**: Implements all SignalR message types

## Usage

Add [`MessagePackInputFormatter`](https://aarnott.github.io/Nerdbank.MessagePack/api/Nerdbank.MessagePack.AspNetCoreMvcFormatter.MessagePackInputFormatter.html) and/or [`MessagePackOutputFormatter`](https://aarnott.github.io/Nerdbank.MessagePack/api/Nerdbank.MessagePack.AspNetCoreMvcFormatter.MessagePackOutputFormatter.html) to your input and/or output formatter collections respectively, as demonstrated [on our doc page](https://aarnott.github.io/Nerdbank.MessagePack/docs/aspnetmvc.html).
