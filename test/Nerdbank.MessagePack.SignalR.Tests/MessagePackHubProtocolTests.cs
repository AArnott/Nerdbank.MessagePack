// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Nerdbank.MessagePack.SignalR.Tests;

public class MessagePackHubProtocolTests
{
    private readonly MessagePackHubProtocol protocol;

    public MessagePackHubProtocolTests()
    {
        this.protocol = new MessagePackHubProtocol();
    }

    [Fact]
    public void Constructor_CreatesInstance()
    {
        var protocol = new MessagePackHubProtocol();
        
        Assert.NotNull(protocol);
        Assert.Equal("messagepack", protocol.Name);
        Assert.Equal(1, protocol.Version);
        Assert.Equal(TransferFormat.Binary, protocol.TransferFormat);
    }

    [Fact]
    public void Constructor_WithSerializer_CreatesInstance()
    {
        var serializer = new MessagePackSerializer();
        var protocol = new MessagePackHubProtocol(serializer);
        
        Assert.NotNull(protocol);
        Assert.Equal("messagepack", protocol.Name);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(0, false)]
    [InlineData(2, false)]
    public void IsVersionSupported_ReturnsCorrectResult(int version, bool expected)
    {
        var protocol = new MessagePackHubProtocol();
        
        Assert.Equal(expected, protocol.IsVersionSupported(version));
    }

    [Fact]
    public void GetMessageBytes_PingMessage_ReturnsValidBytes()
    {
        var message = PingMessage.Instance;
        
        var bytes = this.protocol.GetMessageBytes(message);
        
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public void WriteMessage_PingMessage_WritesToOutput()
    {
        var message = PingMessage.Instance;
        var buffer = new ArrayBufferWriter<byte>();
        
        this.protocol.WriteMessage(message, buffer);
        
        Assert.NotEqual(0, buffer.WrittenCount);
    }

    [Fact]
    public void GetMessageBytes_CloseMessage_ReturnsValidBytes()
    {
        var message = new CloseMessage("Test error", false);
        
        var bytes = this.protocol.GetMessageBytes(message);
        
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public void GetMessageBytes_InvocationMessage_ReturnsValidBytes()
    {
        var message = new InvocationMessage("123", "TestMethod", new object[] { "arg1", 42 });
        
        var bytes = this.protocol.GetMessageBytes(message);
        
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public void GetMessageBytes_CompletionMessage_ReturnsValidBytes()
    {
        var message = CompletionMessage.WithResult("123", "test result");
        
        var bytes = this.protocol.GetMessageBytes(message);
        
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public void GetMessageBytes_StreamItemMessage_ReturnsValidBytes()
    {
        var message = new StreamItemMessage("123", "test item");
        
        var bytes = this.protocol.GetMessageBytes(message);
        
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public void GetMessageBytes_CancelInvocationMessage_ReturnsValidBytes()
    {
        var message = new CancelInvocationMessage("123");
        
        var bytes = this.protocol.GetMessageBytes(message);
        
        Assert.True(bytes.Length > 0);
    }
}