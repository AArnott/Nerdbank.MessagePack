// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Nerdbank.MessagePack.SignalR.Tests;

/// <summary>
/// Simple validation that our SignalR integration compiles and basic functionality works.
/// </summary>
public class SimpleValidationTest
{
    [Fact]
    public void BasicInstantiation_Works()
    {
        // Test 1: Basic instantiation
        var protocol = new MessagePackHubProtocol();
        Assert.NotNull(protocol);
        Assert.Equal("messagepack", protocol.Name);
        Assert.Equal(1, protocol.Version);
        Assert.Equal(TransferFormat.Binary, protocol.TransferFormat);
    }

    [Fact] 
    public void ServiceRegistration_Works()
    {
        // Test 2: Service registration
        var services = new ServiceCollection();
        services.AddMessagePackProtocol();
        var serviceProvider = services.BuildServiceProvider();
        var registeredProtocols = serviceProvider.GetServices<IHubProtocol>();
        
        Assert.Contains(registeredProtocols, p => p is MessagePackHubProtocol);
    }

    [Fact]
    public void MessageSerialization_Works()
    {
        var protocol = new MessagePackHubProtocol();
        
        // Test 3: Message serialization
        var pingMessage = PingMessage.Instance;
        var bytes = protocol.GetMessageBytes(pingMessage);
        Assert.True(bytes.Length > 0);
        
        var closeMessage = new CloseMessage("test error", true);
        var closeBytes = protocol.GetMessageBytes(closeMessage);
        Assert.True(closeBytes.Length > 0);
        
        var invocationMessage = new InvocationMessage("123", "TestMethod", new object[] { "hello", 42 });
        var invocationBytes = protocol.GetMessageBytes(invocationMessage);
        Assert.True(invocationBytes.Length > 0);
    }
}