// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Protocol;
using Nerdbank.MessagePack;
using Nerdbank.MessagePack.SignalR;
using Nerdbank.Streams;
using PolyType;
using Xunit;

public partial class MessagePackHubProtocolTests
{
	private readonly IHubProtocol protocol;

	public MessagePackHubProtocolTests()
	{
		this.protocol = CreateProtocol();
	}

	[Fact]
	public void Constructor_CreatesInstance()
	{
		Assert.NotNull(this.protocol);
		Assert.Equal("messagepack", this.protocol.Name);
		Assert.Equal(2, this.protocol.Version);
		Assert.Equal(TransferFormat.Binary, this.protocol.TransferFormat);
	}

	[Fact]
	public void Constructor_WithSerializer_CreatesInstance()
	{
		var serializer = new MessagePackSerializer();
		IHubProtocol protocol = CreateProtocol(serializer);

		Assert.NotNull(protocol);
		Assert.Equal("messagepack", protocol.Name);
	}

	[Theory]
	[InlineData(1, true)]
	[InlineData(2, true)]
	[InlineData(3, false)]
	public void IsVersionSupported_ReturnsCorrectResult(int version, bool expected)
	{
		Assert.Equal(expected, this.protocol.IsVersionSupported(version));
	}

	[Fact]
	public void GetMessageBytes_PingMessage_ReturnsValidBytes()
	{
		PingMessage message = PingMessage.Instance;

		ReadOnlyMemory<byte> bytes = this.protocol.GetMessageBytes(message);

		Assert.True(bytes.Length > 0);
	}

	[Fact]
	public void WriteMessage_PingMessage_WritesToOutput()
	{
		PingMessage message = PingMessage.Instance;
		var buffer = new Sequence<byte>();

		this.protocol.WriteMessage(message, buffer);

		Assert.NotEqual(0, buffer.Length);
	}

	[Fact]
	public void GetMessageBytes_CloseMessage_ReturnsValidBytes()
	{
		var message = new CloseMessage("Test error", false);

		ReadOnlyMemory<byte> bytes = this.protocol.GetMessageBytes(message);

		Assert.True(bytes.Length > 0);
	}

	[Fact]
	public void GetMessageBytes_InvocationMessage_ReturnsValidBytes()
	{
		var message = new InvocationMessage("123", "TestMethod", new object[] { "arg1", 42 });

		ReadOnlyMemory<byte> bytes = this.protocol.GetMessageBytes(message);

		Assert.True(bytes.Length > 0);
	}

	[Fact]
	public void GetMessageBytes_CompletionMessage_ReturnsValidBytes()
	{
		var message = CompletionMessage.WithResult("123", "test result");

		ReadOnlyMemory<byte> bytes = this.protocol.GetMessageBytes(message);

		Assert.True(bytes.Length > 0);
	}

	[Fact]
	public void GetMessageBytes_StreamItemMessage_ReturnsValidBytes()
	{
		var message = new StreamItemMessage("123", "test item");

		ReadOnlyMemory<byte> bytes = this.protocol.GetMessageBytes(message);

		Assert.True(bytes.Length > 0);
	}

	[Fact]
	public void GetMessageBytes_CancelInvocationMessage_ReturnsValidBytes()
	{
		var message = new CancelInvocationMessage("123");

		ReadOnlyMemory<byte> bytes = this.protocol.GetMessageBytes(message);

		Assert.True(bytes.Length > 0);
	}

	private static IHubProtocol CreateProtocol(MessagePackSerializer? serializer = null)
		=> TestUtilities.CreateHubProtocol(Witness.ShapeProvider, serializer);

	[GenerateShapeFor<bool>]
	private partial class Witness;
}
