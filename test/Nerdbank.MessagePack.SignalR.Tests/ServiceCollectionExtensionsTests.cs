// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Nerdbank.MessagePack.SignalR.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddMessagePackProtocol_RegistersProtocol()
    {
        var services = new ServiceCollection();
        
        services.AddMessagePackProtocol();
        
        var serviceProvider = services.BuildServiceProvider();
        var protocols = serviceProvider.GetServices<IHubProtocol>();
        
        Assert.Contains(protocols, p => p is MessagePackHubProtocol);
    }

    [Fact]
    public void AddMessagePackProtocol_WithSerializer_RegistersProtocol()
    {
        var services = new ServiceCollection();
        var serializer = new MessagePackSerializer();
        
        services.AddMessagePackProtocol(serializer);
        
        var serviceProvider = services.BuildServiceProvider();
        var protocols = serviceProvider.GetServices<IHubProtocol>();
        
        Assert.Contains(protocols, p => p is MessagePackHubProtocol);
    }
}