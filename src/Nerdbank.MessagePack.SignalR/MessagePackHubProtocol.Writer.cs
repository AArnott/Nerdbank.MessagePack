// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Nerdbank.MessagePack.SignalR;

/// <content>Contains the serialize methods of the class.</content>
internal partial class MessagePackHubProtocol
{
	private static void Serialize(ref MessagePackWriter writer, CancelInvocationMessage message)
	{
		writer.WriteArrayHeader(3);
		writer.Write(HubProtocolConstants.CancelInvocationMessageType);
		WriteHeaders(ref writer, message.Headers);
		writer.Write(message.InvocationId);
	}

	private static void Serialize(ref MessagePackWriter writer, CloseMessage message)
	{
		writer.WriteArrayHeader(3);
		writer.Write(HubProtocolConstants.CloseMessageType);
		writer.Write(string.IsNullOrEmpty(message.Error) ? null : message.Error);
		writer.Write(message.AllowReconnect);
	}

	private static void Serialize(ref MessagePackWriter writer, PingMessage message)
	{
		writer.WriteArrayHeader(1);
		writer.Write(HubProtocolConstants.PingMessageType);
	}

	private static void Serialize(ref MessagePackWriter writer, AckMessage message)
	{
		writer.WriteArrayHeader(2);
		writer.Write(HubProtocolConstants.AckMessageType);
		writer.Write(message.SequenceId);
	}

	private static void Serialize(ref MessagePackWriter writer, SequenceMessage message)
	{
		writer.WriteArrayHeader(2);
		writer.Write(HubProtocolConstants.SequenceMessageType);
		writer.Write(message.SequenceId);
	}

	private static void WriteHeaders(ref MessagePackWriter writer, IDictionary<string, string>? headers)
	   => EnvelopeSerializer.Serialize(ref writer, headers ?? ImmutableDictionary<string, string>.Empty, PolyType.SourceGenerator.TypeShapeProvider_Nerdbank_MessagePack_SignalR.Default.IDictionary_String_String);

	private static void WriteStreamIds(ref MessagePackWriter writer, string[]? streamIds)
		=> EnvelopeSerializer.Serialize(ref writer, streamIds ?? [], PolyType.SourceGenerator.TypeShapeProvider_Nerdbank_MessagePack_SignalR.Default.String_Array);

	private void WriteMessageCore(IBufferWriter<byte> output, HubMessage message)
	{
		MessagePackWriter writer = new(output);

		switch (message)
		{
			case InvocationMessage invocationMessage:
				this.Serialize(ref writer, invocationMessage);
				break;
			case StreamInvocationMessage streamInvocationMessage:
				this.Serialize(ref writer, streamInvocationMessage);
				break;
			case StreamItemMessage streamItemMessage:
				this.Serialize(ref writer, streamItemMessage);
				break;
			case CompletionMessage completionMessage:
				this.Serialize(ref writer, completionMessage);
				break;
			case CancelInvocationMessage cancelInvocationMessage:
				Serialize(ref writer, cancelInvocationMessage);
				break;
			case PingMessage ping:
				Serialize(ref writer, ping);
				break;
			case CloseMessage closeMessage:
				Serialize(ref writer, closeMessage);
				break;
			case AckMessage ackMessage:
				Serialize(ref writer, ackMessage);
				break;
			case SequenceMessage sequenceMessage:
				Serialize(ref writer, sequenceMessage);
				break;
			default:
				throw new MessagePackSerializationException($"Unexpected message type: {message.GetType().Name}");
		}

		writer.Flush();
	}

	private void Serialize(ref MessagePackWriter writer, InvocationMessage message)
	{
		writer.WriteArrayHeader(6);

		writer.Write(HubProtocolConstants.InvocationMessageType);
		WriteHeaders(ref writer, message.Headers);
		writer.Write(string.IsNullOrEmpty(message.InvocationId) ? null : message.InvocationId);
		writer.Write(message.Target);

		if (message.Arguments is null)
		{
			writer.WriteArrayHeader(0);
		}
		else
		{
			writer.WriteArrayHeader(message.Arguments.Length);
			foreach (object? arg in message.Arguments)
			{
				this.SerializeUserData(ref writer, arg);
			}
		}

		WriteStreamIds(ref writer, message.StreamIds);
	}

	private void Serialize(ref MessagePackWriter writer, StreamInvocationMessage message)
	{
		writer.WriteArrayHeader(6);

		writer.Write(HubProtocolConstants.StreamInvocationMessageType);
		WriteHeaders(ref writer, message.Headers);
		writer.Write(message.InvocationId);
		writer.Write(message.Target);

		writer.WriteArrayHeader(message.Arguments.Length);
		foreach (object? arg in message.Arguments)
		{
			this.SerializeUserData(ref writer, arg);
		}

		WriteStreamIds(ref writer, message.StreamIds);
	}

	private void Serialize(ref MessagePackWriter writer, StreamItemMessage message)
	{
		writer.WriteArrayHeader(4);
		writer.Write(HubProtocolConstants.StreamItemMessageType);
		WriteHeaders(ref writer, message.Headers);
		writer.Write(message.InvocationId);
		this.SerializeUserData(ref writer, message.Item);
	}

	private void Serialize(ref MessagePackWriter writer, CompletionMessage message)
	{
		int resultKind =
			message.Error != null ? ErrorResult :
			message.HasResult ? NonVoidResult :
			VoidResult;

		writer.WriteArrayHeader(4 + (resultKind != VoidResult ? 1 : 0));
		writer.Write(HubProtocolConstants.CompletionMessageType);
		WriteHeaders(ref writer, message.Headers);
		writer.Write(message.InvocationId);
		writer.Write(resultKind);
		switch (resultKind)
		{
			case ErrorResult:
				writer.Write(message.Error);
				break;
			case NonVoidResult:
				this.SerializeUserData(ref writer, message.Result);
				break;
		}
	}

	private void SerializeUserData(ref MessagePackWriter writer, object? argument)
	{
		switch (argument)
		{
			case RawResult result:
				writer.WriteRaw(result.RawSerializedData);
				break;
			case null:
				writer.WriteNil();
				break;
			default:
				Type declaredArgumentType = argument.GetType(); // TODO: get SignalR to tell us the actual parameter type.
				this.userSerializer.SerializeObject(ref writer, argument, this.userTypeShapeProvider.GetTypeShapeOrThrow(declaredArgumentType));
				break;
		}
	}
}
