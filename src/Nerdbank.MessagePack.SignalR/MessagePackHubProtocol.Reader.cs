// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using System.Text;
using Microsoft.AspNetCore.SignalR;

namespace Nerdbank.MessagePack.SignalR;

/// <content>Contains the deserialize methods of the class.</content>
internal partial class MessagePackHubProtocol
{
	private static T ApplyHeaders<T>(IDictionary<string, string>? source, T destination)
		where T : HubInvocationMessage
	{
		if (source?.Count > 0)
		{
			destination.Headers = source;
		}

		return destination;
	}

	private static void SkipTheRest(ref MessagePackReader reader, int expected, int actual)
	{
		for (int i = expected; i < actual; i++)
		{
			reader.Skip(default);
		}
	}

	private static bool ReadBoolean(ref MessagePackReader reader, string field)
	{
		try
		{
			return reader.ReadBoolean();
		}
		catch (Exception ex)
		{
			throw new MessagePackSerializationException($"Reading '{field}' as Boolean failed.", ex);
		}
	}

	private static string? ReadInvocationId(ref MessagePackReader reader)
		=> ReadString(ref reader, "invocationId");

	private static string? ReadString(ref MessagePackReader reader, IInvocationBinder binder, string field)
	{
		try
		{
#if NET9_0_OR_GREATER
			if (reader.TryReadStringSpan(out ReadOnlySpan<byte> span))
			{
				return binder.GetTarget(span) ?? Encoding.UTF8.GetString(span);
			}
#endif

			return reader.ReadString();
		}
		catch (Exception ex)
		{
			throw new MessagePackSerializationException($"Reading '{field}' as String failed.", ex);
		}
	}

	private static string? ReadString(ref MessagePackReader reader, string fieldName)
	{
		try
		{
			return reader.ReadString();
		}
		catch (Exception ex)
		{
			throw new MessagePackSerializationException($"Reading '{fieldName}' as String failed.", ex);
		}
	}

	private static void ThrowIfNullOrEmpty([NotNull] string? value, string fieldName)
	{
		if (value is null or { Length: 0 })
		{
			throw new MessagePackSerializationException($"Null or empty {fieldName}.");
		}
	}

	private static long ReadInt64(ref MessagePackReader reader, string fieldName)
	{
		try
		{
			return reader.ReadInt64();
		}
		catch (Exception ex)
		{
			throw new MessagePackSerializationException($"Reading '{fieldName}' as Int64 failed.", ex);
		}
	}

	private static int ReadInt32(ref MessagePackReader reader, string fieldName)
	{
		try
		{
			return reader.ReadInt32();
		}
		catch (Exception ex)
		{
			throw new MessagePackSerializationException($"Reading '{fieldName}' as Int32 failed.", ex);
		}
	}

	private static long ReadMapLength(ref MessagePackReader reader, string fieldName)
	{
		try
		{
			return reader.ReadMapHeader();
		}
		catch (Exception ex)
		{
			throw new MessagePackSerializationException($"Reading map length for '{fieldName}' failed.", ex);
		}
	}

	private static long ReadArrayLength(ref MessagePackReader reader, string fieldName)
	{
		try
		{
			return reader.ReadArrayHeader();
		}
		catch (Exception ex)
		{
			throw new MessagePackSerializationException($"Reading array length for '{fieldName}' failed.", ex);
		}
	}

	private static CancelInvocationMessage DeserializeCancelInvocationMessage(ref MessagePackReader reader, int itemCount)
	{
		if (itemCount < 2)
		{
			throw new MessagePackSerializationException($"CancelInvocation message must have at least 2 items: headers and invocationId.");
		}

		IDictionary<string, string>? headers = ReadHeaders(ref reader);
		string? invocationId = ReadInvocationId(ref reader);
		ThrowIfNullOrEmpty(invocationId, "invocation ID for CancelInvocation message");

		SkipTheRest(ref reader, 2, itemCount);
		return ApplyHeaders(headers, new CancelInvocationMessage(invocationId));
	}

	private static CloseMessage DeserializeCloseMessage(ref MessagePackReader reader, int itemCount)
	{
		if (itemCount < 1)
		{
			throw new MessagePackSerializationException("Close message must have at least 1 item: error.");
		}

		string? error = ReadString(ref reader, "error");
		bool allowReconnect = false;

		if (itemCount > 1)
		{
			allowReconnect = ReadBoolean(ref reader, "allowReconnect");
		}

		SkipTheRest(ref reader, 2, itemCount);

		// An empty string is still an error
		if (error == null && !allowReconnect)
		{
			return CloseMessage.Empty;
		}

		return new CloseMessage(error, allowReconnect);
	}

	private static AckMessage DeserializeAckMessage(ref MessagePackReader reader, int itemCount)
	{
		if (itemCount < 1)
		{
			throw new MessagePackSerializationException("Ack message must have at least 1 item: sequenceId.");
		}

		long sequenceId = ReadInt64(ref reader, "sequenceId");

		SkipTheRest(ref reader, 1, itemCount);
		return new AckMessage(sequenceId);
	}

	private static SequenceMessage DeserializeSequenceMessage(ref MessagePackReader reader, int itemCount)
	{
		if (itemCount < 1)
		{
			throw new MessagePackSerializationException("Sequence message must have at least 1 item: sequenceId.");
		}

		long sequenceId = ReadInt64(ref reader, "sequenceId");
		SkipTheRest(ref reader, 1, itemCount);
		return new SequenceMessage(sequenceId);
	}

	private static IDictionary<string, string>? ReadHeaders(ref MessagePackReader reader)
	{
		MessagePackReader peekReader = reader.CreatePeekReader();
		if (ReadMapLength(ref peekReader, "headers") == 0)
		{
			reader.Skip(default);
			return null;
		}

		return EnvelopeSerializer.Deserialize<IDictionary<string, string>>(ref reader, Witness.GeneratedTypeShapeProvider);
	}

	private static string[]? ReadStreamIds(ref MessagePackReader reader)
	{
		MessagePackReader peekReader = reader.CreatePeekReader();
		if (ReadArrayLength(ref peekReader, "streamIds") == 0)
		{
			reader.Skip(default);
			return null;
		}

		return EnvelopeSerializer.Deserialize<string[]>(ref reader, Witness.GeneratedTypeShapeProvider);
	}

	private static Type? TryGetReturnType(IInvocationBinder binder, string invocationId)
	{
		try
		{
			return binder.GetReturnType(invocationId);
		}

		// GetReturnType throws if invocationId not found, this can be caused by the server canceling a client-result but the client still sending a result
		// For now let's ignore the failure and skip parsing the result, server will log that the result wasn't expected anymore and ignore the message
		// In the future we may want a CompletionBindingFailureMessage that we can flow to the dispatcher for handling
		catch (Exception)
		{
			return null;
		}
	}

	private object?[] BindArguments(ref MessagePackReader reader, IInvocationBinder binder, string target)
	{
		IReadOnlyList<Type> parameterTypes = binder.GetParameterTypes(target);

		long argumentCount = ReadArrayLength(ref reader, "arguments");

		object?[] result = new object?[parameterTypes.Count];
		for (int i = 0; i < argumentCount; i++)
		{
			if (i < result.Length)
			{
				result[i] = this.userSerializer.DeserializeObject(ref reader, this.userTypeShapeProvider.Resolve(parameterTypes[i]));
			}
			else
			{
				reader.Skip(default);
			}
		}

		return result;
	}

	private HubMessage? ParseMessage(ref MessagePackReader reader, IInvocationBinder binder)
	{
		int remainingElements = reader.ReadArrayHeader();
		if (remainingElements < 1)
		{
			throw new MessagePackSerializationException("MessagePack array header must have at least one element.");
		}

		int messageType = reader.ReadInt32();
		remainingElements--;

		HubMessage? result = messageType switch
		{
			HubProtocolConstants.InvocationMessageType => this.DeserializeInvocationMessage(ref reader, binder, remainingElements),
			HubProtocolConstants.StreamInvocationMessageType => this.DeserializeStreamInvocationMessage(ref reader, binder, remainingElements),
			HubProtocolConstants.StreamItemMessageType => this.DeserializeStreamItemMessage(ref reader, binder, remainingElements),
			HubProtocolConstants.CompletionMessageType => this.DeserializeCompletionMessage(ref reader, binder, remainingElements),
			HubProtocolConstants.CancelInvocationMessageType => DeserializeCancelInvocationMessage(ref reader, remainingElements),
			HubProtocolConstants.PingMessageType => PingMessage.Instance,
			HubProtocolConstants.CloseMessageType => DeserializeCloseMessage(ref reader, remainingElements),
			HubProtocolConstants.AckMessageType => DeserializeAckMessage(ref reader, remainingElements),
			HubProtocolConstants.SequenceMessageType => DeserializeSequenceMessage(ref reader, remainingElements),
			_ => null, // Future protocol changes can add message types, old clients can ignore them.
		};

		return result;
	}

	private HubMessage DeserializeInvocationMessage(ref MessagePackReader reader, IInvocationBinder binder, int itemCount)
	{
		if (itemCount < 4)
		{
			throw new MessagePackSerializationException("Invocation message must have at least 4 items: headers, invocationId, target, and arguments.");
		}

		IDictionary<string, string>? headers = ReadHeaders(ref reader);
		string? invocationId = ReadInvocationId(ref reader);

		// For MsgPack, SignalR represents an empty invocation ID as an empty string,
		// so we need to normalize that to "null", which is what indicates a non-blocking invocation.
		if (string.IsNullOrEmpty(invocationId))
		{
			invocationId = null;
		}

		string? target = ReadString(ref reader, binder, "target");
		ThrowIfNullOrEmpty(target, "target for Invocation message");

		object?[]? arguments;
		try
		{
			arguments = this.BindArguments(ref reader, binder, target);
		}
		catch (Exception ex)
		{
			return new InvocationBindingFailureMessage(invocationId, target, ExceptionDispatchInfo.Capture(ex));
		}

		string[]? streams = null;

		// Previous clients will send 5 items, so we check if they sent a stream array or not
		if (itemCount > 4)
		{
			streams = ReadStreamIds(ref reader);
		}

		SkipTheRest(ref reader, 5, itemCount);
		return ApplyHeaders(headers, new InvocationMessage(invocationId, target, arguments, streams));
	}

	private HubMessage DeserializeStreamInvocationMessage(ref MessagePackReader reader, IInvocationBinder binder, int itemCount)
	{
		if (itemCount < 4)
		{
			throw new MessagePackSerializationException("Invocation message must have at least 4 items: headers, invocationId, target, and arguments.");
		}

		IDictionary<string, string>? headers = ReadHeaders(ref reader);
		string? invocationId = ReadInvocationId(ref reader);
		ThrowIfNullOrEmpty(invocationId, "invocation ID for StreamInvocation message");

		string? target = ReadString(ref reader, "target");
		ThrowIfNullOrEmpty(target, "target for StreamInvocation message");

		object?[] arguments;
		try
		{
			arguments = this.BindArguments(ref reader, binder, target);
		}
		catch (Exception ex)
		{
			return new InvocationBindingFailureMessage(invocationId, target, ExceptionDispatchInfo.Capture(ex));
		}

		string[]? streams = null;

		// Previous clients will send 5 items, so we check if they sent a stream array or not
		if (itemCount > 4)
		{
			streams = ReadStreamIds(ref reader);
		}

		SkipTheRest(ref reader, 5, itemCount);
		return ApplyHeaders(headers, new StreamInvocationMessage(invocationId, target, arguments, streams));
	}

	private HubMessage DeserializeStreamItemMessage(ref MessagePackReader reader, IInvocationBinder binder, int itemCount)
	{
		if (itemCount < 2)
		{
			throw new MessagePackSerializationException("StreamItem message must have at least 2 items: headers and invocationId.");
		}

		IDictionary<string, string>? headers = ReadHeaders(ref reader);
		string? invocationId = ReadInvocationId(ref reader);
		ThrowIfNullOrEmpty(invocationId, "invocation ID for StreamItem message");

		object? value;
		try
		{
			Type itemType = binder.GetStreamItemType(invocationId);
			value = this.userSerializer.DeserializeObject(ref reader, this.userTypeShapeProvider.Resolve(itemType));
		}
		catch (Exception ex)
		{
			return new StreamBindingFailureMessage(invocationId, ExceptionDispatchInfo.Capture(ex));
		}

		SkipTheRest(ref reader, 5, itemCount);
		return ApplyHeaders(headers, new StreamItemMessage(invocationId, value));
	}

	private CompletionMessage DeserializeCompletionMessage(ref MessagePackReader reader, IInvocationBinder binder, int itemCount)
	{
		if (itemCount < 3)
		{
			throw new MessagePackSerializationException("Completion message must have at least 3 items: headers, invocationId, and resultKind.");
		}

		IDictionary<string, string>? headers = ReadHeaders(ref reader);
		string? invocationId = ReadInvocationId(ref reader);
		ThrowIfNullOrEmpty(invocationId, "invocation ID for Completion message");

		int resultKind = ReadInt32(ref reader, "resultKind");

		string? error = null;
		object? result = null;
		bool hasResult = false;

		switch (resultKind)
		{
			case ErrorResult:
				if (itemCount < 4)
				{
					throw new MessagePackSerializationException("An error result requires at least 4 elements.");
				}

				error = ReadString(ref reader, "error");
				break;
			case NonVoidResult:
				hasResult = true;
				if (itemCount < 4)
				{
					throw new MessagePackSerializationException("An completion result requires at least 4 elements.");
				}

				Type? itemType = TryGetReturnType(binder, invocationId);
				if (itemType is null)
				{
					reader.Skip(default);
				}
				else
				{
					if (itemType == typeof(RawResult))
					{
						result = new RawResult(reader.ReadRaw(default(SerializationContext)));
					}
					else
					{
						try
						{
							result = this.userSerializer.DeserializeObject(ref reader, this.userTypeShapeProvider.Resolve(itemType));
						}
						catch (Exception ex)
						{
							error = $"Error trying to deserialize result to {itemType.Name}. {ex.Message}";
							hasResult = false;
						}
					}
				}

				break;
			case VoidResult:
				hasResult = false;
				break;
			default:
				throw new MessagePackSerializationException("Invalid invocation result kind.");
		}

		SkipTheRest(ref reader, (hasResult || error is not null) ? 4 : 3, itemCount);
		return ApplyHeaders(headers, new CompletionMessage(invocationId, error, result, hasResult));
	}
}
