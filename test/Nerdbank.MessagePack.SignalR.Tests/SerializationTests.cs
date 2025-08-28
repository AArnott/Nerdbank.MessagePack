// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft;
using Microsoft.AspNetCore.SignalR.Protocol;
using Nerdbank.MessagePack;
using Nerdbank.MessagePack.SignalR;
using PolyType;
using Xunit;

public partial class SerializationTests
{
	protected MessagePackSerializer Serializer { get; } = new();

	[Fact]
	public void PingMessage_Serialization()
	{
		IHubProtocol protocol = this.CreateProtocol();
		PingMessage pingMessage = PingMessage.Instance;
		ReadOnlyMemory<byte> bytes = protocol.GetMessageBytes(pingMessage);

		ReadOnlySequence<byte> serializedSequence = new(bytes);
		this.LogMsgPack(serializedSequence);
		Assert.True(protocol.TryParseMessage(ref serializedSequence, new MockInvocationBinder(), out HubMessage? message));
		Assert.True(serializedSequence.IsEmpty);

		Assert.IsType<PingMessage>(message);
	}

	[Fact]
	public void CloseMessage_Serialization()
	{
		IHubProtocol protocol = this.CreateProtocol();
		CloseMessage closeMessage = new("test error", true);
		ReadOnlyMemory<byte> bytes = protocol.GetMessageBytes(closeMessage);

		ReadOnlySequence<byte> serializedSequence = new(bytes);
		this.LogMsgPack(serializedSequence);
		Assert.True(protocol.TryParseMessage(ref serializedSequence, new MockInvocationBinder(), out HubMessage? message));
		Assert.True(serializedSequence.IsEmpty);

		CloseMessage close = Assert.IsType<CloseMessage>(message);
		Assert.Equal("test error", close.Error);
		Assert.True(close.AllowReconnect);
	}

	[Fact]
	public void InvocationMessage_Serialization()
	{
		IHubProtocol protocol = this.CreateProtocol();

		InvocationMessage invocationMessage = new("123", "TestMethod", new object[] { "hello", 42 });
		ReadOnlyMemory<byte> invocationBytes = protocol.GetMessageBytes(invocationMessage);
		Assert.True(invocationBytes.Length > 0);

		MockInvocationBinder binder = new()
		{
			ParameterTypes =
			{
				["TestMethod"] = new[] { typeof(string), typeof(int) },
			},
		};
		ReadOnlySequence<byte> serializedSequence = new(invocationBytes);
		this.LogMsgPack(serializedSequence);
		Assert.True(protocol.TryParseMessage(ref serializedSequence, binder, out HubMessage? message));
		Assert.True(serializedSequence.IsEmpty);

		InvocationMessage invocation = Assert.IsType<InvocationMessage>(message);
		Assert.Equal("123", invocation.InvocationId);
		Assert.Equal("TestMethod", invocation.Target);
		Assert.Equal(2, invocation.Arguments?.Length);
		Assert.Equal("hello", invocation.Arguments![0]);
		Assert.Equal(42, invocation.Arguments[1]);
	}

	[Fact]
	public void StreamInvocationMessage_Serialization()
	{
		IHubProtocol protocol = this.CreateProtocol();

		StreamInvocationMessage streamInvocationMessage = new("456", "StreamMethod", new object[] { "param1", 123 });
		ReadOnlyMemory<byte> streamInvocationBytes = protocol.GetMessageBytes(streamInvocationMessage);
		Assert.True(streamInvocationBytes.Length > 0);

		MockInvocationBinder binder = new()
		{
			ParameterTypes =
			{
				["StreamMethod"] = new[] { typeof(string), typeof(int) },
			},
		};
		ReadOnlySequence<byte> serializedSequence = new(streamInvocationBytes);
		this.LogMsgPack(serializedSequence);
		Assert.True(protocol.TryParseMessage(ref serializedSequence, binder, out HubMessage? message));
		Assert.True(serializedSequence.IsEmpty);

		StreamInvocationMessage streamInvocation = Assert.IsType<StreamInvocationMessage>(message);
		Assert.Equal("456", streamInvocation.InvocationId);
		Assert.Equal("StreamMethod", streamInvocation.Target);
		Assert.Equal(2, streamInvocation.Arguments.Length);
		Assert.Equal("param1", streamInvocation.Arguments[0]);
		Assert.Equal(123, streamInvocation.Arguments[1]);
	}

	[Fact]
	public void StreamItemMessage_Serialization()
	{
		IHubProtocol protocol = this.CreateProtocol();

		StreamItemMessage streamItemMessage = new("789", "stream item data");
		ReadOnlyMemory<byte> streamItemBytes = protocol.GetMessageBytes(streamItemMessage);
		Assert.True(streamItemBytes.Length > 0);

		MockInvocationBinder binder = new()
		{
			StreamItemType =
			{
				["789"] = typeof(string),
			},
		};
		ReadOnlySequence<byte> serializedSequence = new(streamItemBytes);
		this.LogMsgPack(serializedSequence);
		Assert.True(protocol.TryParseMessage(ref serializedSequence, binder, out HubMessage? message));
		Assert.True(serializedSequence.IsEmpty);

		StreamItemMessage streamItem = Assert.IsType<StreamItemMessage>(message);
		Assert.Equal("789", streamItem.InvocationId);
		Assert.Equal("stream item data", streamItem.Item);
	}

	[Fact]
	public void CompletionMessage_WithResult_Serialization()
	{
		IHubProtocol protocol = this.CreateProtocol();

		CompletionMessage completionMessage = CompletionMessage.WithResult("101", "completion result");
		ReadOnlyMemory<byte> completionBytes = protocol.GetMessageBytes(completionMessage);
		Assert.True(completionBytes.Length > 0);

		MockInvocationBinder binder = new()
		{
			ReturnType =
			{
				["101"] = typeof(string),
			},
		};
		ReadOnlySequence<byte> serializedSequence = new(completionBytes);
		this.LogMsgPack(serializedSequence);
		Assert.True(protocol.TryParseMessage(ref serializedSequence, binder, out HubMessage? message));
		Assert.True(serializedSequence.IsEmpty);

		CompletionMessage completion = Assert.IsType<CompletionMessage>(message);
		Assert.Equal("101", completion.InvocationId);
		Assert.True(completion.HasResult);
		Assert.Equal("completion result", completion.Result);
		Assert.Null(completion.Error);
	}

	[Fact]
	public void CompletionMessage_WithError_Serialization()
	{
		IHubProtocol protocol = this.CreateProtocol();

		CompletionMessage completionMessage = CompletionMessage.WithError("102", "Something went wrong");
		ReadOnlyMemory<byte> completionBytes = protocol.GetMessageBytes(completionMessage);
		Assert.True(completionBytes.Length > 0);

		MockInvocationBinder binder = new();
		ReadOnlySequence<byte> serializedSequence = new(completionBytes);
		this.LogMsgPack(serializedSequence);
		Assert.True(protocol.TryParseMessage(ref serializedSequence, binder, out HubMessage? message));
		Assert.True(serializedSequence.IsEmpty);

		CompletionMessage completion = Assert.IsType<CompletionMessage>(message);
		Assert.Equal("102", completion.InvocationId);
		Assert.False(completion.HasResult);
		Assert.Null(completion.Result);
		Assert.Equal("Something went wrong", completion.Error);
	}

	[Fact]
	public void CompletionMessage_Empty_Serialization()
	{
		IHubProtocol protocol = this.CreateProtocol();

		CompletionMessage completionMessage = CompletionMessage.Empty("103");
		ReadOnlyMemory<byte> completionBytes = protocol.GetMessageBytes(completionMessage);
		Assert.True(completionBytes.Length > 0);

		MockInvocationBinder binder = new();
		ReadOnlySequence<byte> serializedSequence = new(completionBytes);
		this.LogMsgPack(serializedSequence);
		Assert.True(protocol.TryParseMessage(ref serializedSequence, binder, out HubMessage? message));
		Assert.True(serializedSequence.IsEmpty);

		CompletionMessage completion = Assert.IsType<CompletionMessage>(message);
		Assert.Equal("103", completion.InvocationId);
		Assert.False(completion.HasResult);
		Assert.Null(completion.Result);
		Assert.Null(completion.Error);
	}

	[Fact]
	public void CancelInvocationMessage_Serialization()
	{
		IHubProtocol protocol = this.CreateProtocol();

		CancelInvocationMessage cancelInvocationMessage = new("201");
		ReadOnlyMemory<byte> cancelInvocationBytes = protocol.GetMessageBytes(cancelInvocationMessage);
		Assert.True(cancelInvocationBytes.Length > 0);

		MockInvocationBinder binder = new();
		ReadOnlySequence<byte> serializedSequence = new(cancelInvocationBytes);
		this.LogMsgPack(serializedSequence);
		Assert.True(protocol.TryParseMessage(ref serializedSequence, binder, out HubMessage? message));
		Assert.True(serializedSequence.IsEmpty);

		CancelInvocationMessage cancelInvocation = Assert.IsType<CancelInvocationMessage>(message);
		Assert.Equal("201", cancelInvocation.InvocationId);
	}

	[Fact]
	public void AckMessage_Serialization()
	{
		IHubProtocol protocol = this.CreateProtocol();

		AckMessage ackMessage = new(42);
		ReadOnlyMemory<byte> ackBytes = protocol.GetMessageBytes(ackMessage);
		Assert.True(ackBytes.Length > 0);

		MockInvocationBinder binder = new();
		ReadOnlySequence<byte> serializedSequence = new(ackBytes);
		this.LogMsgPack(serializedSequence);
		Assert.True(protocol.TryParseMessage(ref serializedSequence, binder, out HubMessage? message));
		Assert.True(serializedSequence.IsEmpty);

		AckMessage ack = Assert.IsType<AckMessage>(message);
		Assert.Equal(42, ack.SequenceId);
	}

	[Fact]
	public void SequenceMessage_Serialization()
	{
		IHubProtocol protocol = this.CreateProtocol();

		SequenceMessage sequenceMessage = new(99);
		ReadOnlyMemory<byte> sequenceBytes = protocol.GetMessageBytes(sequenceMessage);
		Assert.True(sequenceBytes.Length > 0);

		MockInvocationBinder binder = new();
		ReadOnlySequence<byte> serializedSequence = new(sequenceBytes);
		this.LogMsgPack(serializedSequence);
		Assert.True(protocol.TryParseMessage(ref serializedSequence, binder, out HubMessage? message));
		Assert.True(serializedSequence.IsEmpty);

		SequenceMessage sequence = Assert.IsType<SequenceMessage>(message);
		Assert.Equal(99, sequence.SequenceId);
	}

	private void LogMsgPack(ReadOnlySequence<byte> payload)
	{
		Assumes.True(BinaryMessageFormatter.TryParseMessage(ref payload, out ReadOnlySequence<byte> msgpack));
		TestContext.Current.TestOutputHelper?.WriteLine(this.Serializer.ConvertToJson(msgpack));
	}

	private IHubProtocol CreateProtocol()
		=> TestUtilities.CreateHubProtocol(Witness.ShapeProvider, this.Serializer);

	[GenerateShapeFor<string>]
	[GenerateShapeFor<int>]
	[GenerateShapeFor<bool>]
	[GenerateShapeFor<long>]
	private partial class Witness;
}
