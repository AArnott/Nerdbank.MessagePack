// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.IO;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using PolyType;

namespace Nerdbank.MessagePack.SignalR;

/// <summary>
/// Implements the SignalR Hub Protocol using Nerdbank.MessagePack serialization.
/// </summary>
public class MessagePackHubProtocol : IHubProtocol
{
    private readonly MessagePackSerializer serializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessagePackHubProtocol"/> class.
    /// </summary>
    public MessagePackHubProtocol()
        : this(new MessagePackSerializer())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessagePackHubProtocol"/> class.
    /// </summary>
    /// <param name="serializer">The MessagePack serializer to use.</param>
    public MessagePackHubProtocol(MessagePackSerializer serializer)
    {
        this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    }

    /// <inheritdoc />
    public string Name => "messagepack";

    /// <inheritdoc />
    public int Version => 1;

    /// <inheritdoc />
    public TransferFormat TransferFormat => TransferFormat.Binary;

    /// <inheritdoc />
    public bool IsVersionSupported(int version) => version == this.Version;

    /// <inheritdoc />
    public bool TryParseMessage(ref ReadOnlySequence<byte> input, IInvocationBinder binder, out HubMessage? message)
    {
        message = null;
        
        try
        {
            if (input.IsEmpty)
            {
                return false;
            }

            // First, determine the message type by deserializing as base DTO
            var baseDto = this.serializer.Deserialize<MessageDto>(input, Witness.ShapeProvider);
            if (baseDto == null)
            {
                return false;
            }

            // Parse message based on type
            message = baseDto.Type switch
            {
                HubProtocolConstants.InvocationMessageType => this.ParseInvocationMessage(input, binder),
                HubProtocolConstants.StreamInvocationMessageType => this.ParseStreamInvocationMessage(input, binder),
                HubProtocolConstants.CompletionMessageType => this.ParseCompletionMessage(input, binder),
                HubProtocolConstants.StreamItemMessageType => this.ParseStreamItemMessage(input, binder),
                HubProtocolConstants.CancelInvocationMessageType => this.ParseCancelInvocationMessage(input),
                HubProtocolConstants.PingMessageType => PingMessage.Instance,
                HubProtocolConstants.CloseMessageType => this.ParseCloseMessage(input),
                _ => throw new InvalidDataException($"Unknown message type: {baseDto.Type}")
            };

            // Mark the entire input as consumed
            input = default;
            return true;
        }
        catch (Exception ex) when (!(ex is InvalidDataException))
        {
            throw new InvalidDataException("Invalid MessagePack data", ex);
        }
    }

    /// <inheritdoc />
    public void WriteMessage(HubMessage message, IBufferWriter<byte> output)
    {
        var messageDto = this.GetMessageDto(message);
        var writer = new MessagePackWriter(output);
        this.SerializeMessageDto(ref writer, messageDto);
        writer.Flush();
    }

    /// <inheritdoc />
    public ReadOnlyMemory<byte> GetMessageBytes(HubMessage message)
    {
        var messageDto = this.GetMessageDto(message);
        return this.SerializeMessageDtoToBytes(messageDto);
    }

    private MessageDto GetMessageDto(HubMessage message)
    {
        return message switch
        {
            InvocationMessage invocation => new InvocationMessageDto
            {
                Headers = invocation.Headers?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                InvocationId = invocation.InvocationId,
                Target = invocation.Target,
                Arguments = invocation.Arguments,
                StreamIds = invocation.StreamIds,
            },
            
            StreamInvocationMessage streamInvocation => new StreamInvocationMessageDto
            {
                Headers = streamInvocation.Headers?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                InvocationId = streamInvocation.InvocationId,
                Target = streamInvocation.Target,
                Arguments = streamInvocation.Arguments,
                StreamIds = streamInvocation.StreamIds,
            },
            
            CompletionMessage completion => new CompletionMessageDto
            {
                Headers = completion.Headers?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                InvocationId = completion.InvocationId,
                Error = completion.Error,
                Result = completion.Result,
                HasResult = completion.HasResult,
            },
            
            StreamItemMessage streamItem => new StreamItemMessageDto
            {
                Headers = streamItem.Headers?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                InvocationId = streamItem.InvocationId,
                Item = streamItem.Item,
            },
            
            CancelInvocationMessage cancel => new CancelInvocationMessageDto
            {
                Headers = cancel.Headers?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                InvocationId = cancel.InvocationId,
            },
            
            PingMessage => new PingMessageDto(),
            
            CloseMessage close => new CloseMessageDto
            {
                Error = close.Error,
                AllowReconnect = close.AllowReconnect,
            },
            
            _ => throw new InvalidOperationException($"Unsupported message type: {message.GetType().Name}")
        };
    }

    private InvocationMessage ParseInvocationMessage(ReadOnlySequence<byte> input, IInvocationBinder binder)
    {
        var dto = this.serializer.Deserialize<InvocationMessageDto>(input, Witness.ShapeProvider);
        
        var arguments = this.ConvertArguments(dto.Arguments, binder, dto.Target);
        
        return new InvocationMessage(dto.InvocationId, dto.Target, arguments, dto.StreamIds)
        {
            Headers = dto.Headers != null ? new Dictionary<string, string>(dto.Headers) : null
        };
    }

    private StreamInvocationMessage ParseStreamInvocationMessage(ReadOnlySequence<byte> input, IInvocationBinder binder)
    {
        var dto = this.serializer.Deserialize<StreamInvocationMessageDto>(input, Witness.ShapeProvider);
        
        var arguments = this.ConvertArguments(dto.Arguments, binder, dto.Target);
        
        return new StreamInvocationMessage(dto.InvocationId, dto.Target, arguments, dto.StreamIds)
        {
            Headers = dto.Headers != null ? new Dictionary<string, string>(dto.Headers) : null
        };
    }

    private CompletionMessage ParseCompletionMessage(ReadOnlySequence<byte> input, IInvocationBinder binder)
    {
        var dto = this.serializer.Deserialize<CompletionMessageDto>(input, Witness.ShapeProvider);
        
        CompletionMessage message;
        if (dto.Error != null)
        {
            message = CompletionMessage.WithError(dto.InvocationId, dto.Error);
        }
        else if (dto.HasResult)
        {
            message = CompletionMessage.WithResult(dto.InvocationId, dto.Result);
        }
        else
        {
            message = CompletionMessage.Empty(dto.InvocationId);
        }

        if (dto.Headers != null)
        {
            foreach (var header in dto.Headers)
            {
                message.Headers[header.Key] = header.Value;
            }
        }

        return message;
    }

    private StreamItemMessage ParseStreamItemMessage(ReadOnlySequence<byte> input, IInvocationBinder binder)
    {
        var dto = this.serializer.Deserialize<StreamItemMessageDto>(input, Witness.ShapeProvider);
        
        return new StreamItemMessage(dto.InvocationId, dto.Item)
        {
            Headers = dto.Headers != null ? new Dictionary<string, string>(dto.Headers) : null
        };
    }

    private CancelInvocationMessage ParseCancelInvocationMessage(ReadOnlySequence<byte> input)
    {
        var dto = this.serializer.Deserialize<CancelInvocationMessageDto>(input, Witness.ShapeProvider);
        
        return new CancelInvocationMessage(dto.InvocationId)
        {
            Headers = dto.Headers != null ? new Dictionary<string, string>(dto.Headers) : null
        };
    }

    private CloseMessage ParseCloseMessage(ReadOnlySequence<byte> input)
    {
        var dto = this.serializer.Deserialize<CloseMessageDto>(input, Witness.ShapeProvider);
        
        return new CloseMessage(dto.Error, dto.AllowReconnect);
    }

    private object?[] ConvertArguments(object?[]? arguments, IInvocationBinder binder, string target)
    {
        if (arguments == null)
        {
            return Array.Empty<object>();
        }

        var parameterTypes = binder.GetParameterTypes(target);
        
        if (arguments.Length != parameterTypes.Count)
        {
            throw new InvalidDataException($"Argument count mismatch. Expected {parameterTypes.Count}, got {arguments.Length}");
        }

        var result = new object?[arguments.Length];
        for (int i = 0; i < arguments.Length; i++)
        {
            if (arguments[i] != null && i < parameterTypes.Count)
            {
                var targetType = parameterTypes[i];
                if (targetType != typeof(object) && !targetType.IsAssignableFrom(arguments[i]!.GetType()))
                {
                    // Need to convert the type - this is complex with the current approach
                    // For now, just use the original value if it's assignable
                    if (targetType.IsAssignableFrom(arguments[i]!.GetType()))
                    {
                        result[i] = arguments[i];
                    }
                    else
                    {
                        // For type conversion, we'd need more sophisticated handling
                        // This is a limitation we can improve later
                        result[i] = arguments[i];
                    }
                }
                else
                {
                    result[i] = arguments[i];
                }
            }
            else
            {
                result[i] = arguments[i];
            }
        }
        return result;
    }

    private void SerializeMessageDto(ref MessagePackWriter writer, MessageDto dto)
    {
        switch (dto)
        {
            case PingMessageDto ping:
                this.serializer.Serialize(ref writer, ping, Witness.ShapeProvider, default);
                break;
            case CloseMessageDto close:
                this.serializer.Serialize(ref writer, close, Witness.ShapeProvider, default);
                break;
            case InvocationMessageDto invocation:
                this.serializer.Serialize(ref writer, invocation, Witness.ShapeProvider, default);
                break;
            case StreamInvocationMessageDto streamInvocation:
                this.serializer.Serialize(ref writer, streamInvocation, Witness.ShapeProvider, default);
                break;
            case CompletionMessageDto completion:
                this.serializer.Serialize(ref writer, completion, Witness.ShapeProvider, default);
                break;
            case StreamItemMessageDto streamItem:
                this.serializer.Serialize(ref writer, streamItem, Witness.ShapeProvider, default);
                break;
            case CancelInvocationMessageDto cancel:
                this.serializer.Serialize(ref writer, cancel, Witness.ShapeProvider, default);
                break;
            default:
                throw new InvalidOperationException($"Unknown DTO type: {dto.GetType().Name}");
        }
    }

    private ReadOnlyMemory<byte> SerializeMessageDtoToBytes(MessageDto dto)
    {
        return dto switch
        {
            PingMessageDto ping => this.serializer.Serialize(ping, Witness.ShapeProvider, default),
            CloseMessageDto close => this.serializer.Serialize(close, Witness.ShapeProvider, default),
            InvocationMessageDto invocation => this.serializer.Serialize(invocation, Witness.ShapeProvider, default),
            StreamInvocationMessageDto streamInvocation => this.serializer.Serialize(streamInvocation, Witness.ShapeProvider, default),
            CompletionMessageDto completion => this.serializer.Serialize(completion, Witness.ShapeProvider, default),
            StreamItemMessageDto streamItem => this.serializer.Serialize(streamItem, Witness.ShapeProvider, default),
            CancelInvocationMessageDto cancel => this.serializer.Serialize(cancel, Witness.ShapeProvider, default),
            _ => throw new InvalidOperationException($"Unknown DTO type: {dto.GetType().Name}"),
        };
    }

    private ReadOnlyMemory<byte> SerializeObjectToBytes(object value)
    {
        // For simple types, we can handle them directly
        return value switch
        {
            string str => this.serializer.Serialize(str, Witness.ShapeProvider, default),
            int i => this.serializer.Serialize(i, Witness.ShapeProvider, default),
            bool b => this.serializer.Serialize(b, Witness.ShapeProvider, default),
            double d => this.serializer.Serialize(d, Witness.ShapeProvider, default),
            float f => this.serializer.Serialize(f, Witness.ShapeProvider, default),
            long l => this.serializer.Serialize(l, Witness.ShapeProvider, default),
            // For complex objects, we might need to use object arrays or dictionaries
            // This is a limitation of the current approach
            _ => throw new NotSupportedException($"Type {value.GetType().Name} is not supported for argument conversion"),
        };
    }
}

/// <summary>
/// Witness class for shape providers.
/// </summary>
[GenerateShapeFor<MessageDto>]
[GenerateShapeFor<PingMessageDto>]
[GenerateShapeFor<CloseMessageDto>]
[GenerateShapeFor<InvocationMessageDto>]
[GenerateShapeFor<StreamInvocationMessageDto>]
[GenerateShapeFor<CompletionMessageDto>]
[GenerateShapeFor<StreamItemMessageDto>]
[GenerateShapeFor<CancelInvocationMessageDto>]
[GenerateShapeFor<string>]
[GenerateShapeFor<int>]
[GenerateShapeFor<bool>]
[GenerateShapeFor<double>]
[GenerateShapeFor<float>]
[GenerateShapeFor<long>]
[GenerateShapeFor<object>]
[GenerateShapeFor<object[]>]
[GenerateShapeFor<Dictionary<string, string>>]
[GenerateShapeFor<string[]>]
internal partial class Witness;