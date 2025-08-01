// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.AspNetCore.Connections;
using Xunit;

namespace Nerdbank.MessagePack.SignalR.Tests;

public class MessagePackHubProtocolBasicTests
{
    [Fact]
    public void Constructor_Success()
    {
        var protocol = new MessagePackHubProtocol();
        Assert.NotNull(protocol);
        Assert.Equal("messagepack", protocol.Name);
        Assert.Equal(1, protocol.Version);
        Assert.Equal(TransferFormat.Binary, protocol.TransferFormat);
    }

    [Fact]
    public void GetMessageBytes_PingMessage_Success()
    {
        var protocol = new MessagePackHubProtocol();
        var message = PingMessage.Instance;
        
        var bytes = protocol.GetMessageBytes(message);
        
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
    }
}