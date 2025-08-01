// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using PolyType;

namespace Nerdbank.MessagePack.SignalR;

/// <summary>
/// Base class for all SignalR message data transfer objects.
/// </summary>
[GenerateShape]
public abstract partial record MessageDto
{
    /// <summary>
    /// Gets the message type identifier.
    /// </summary>
    public abstract int Type { get; }
}

/// <summary>
/// DTO for ping messages.
/// </summary>
[GenerateShape]
public partial record PingMessageDto : MessageDto
{
    /// <inheritdoc />
    public override int Type => HubProtocolConstants.PingMessageType;
}

/// <summary>
/// DTO for close messages.
/// </summary>
[GenerateShape]
public partial record CloseMessageDto : MessageDto
{
    /// <inheritdoc />
    public override int Type => HubProtocolConstants.CloseMessageType;

    /// <summary>
    /// Gets or sets the error message, if any.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether reconnection is allowed.
    /// </summary>
    public bool AllowReconnect { get; set; } = true;
}

/// <summary>
/// DTO for invocation messages.
/// </summary>
[GenerateShape]
public partial record InvocationMessageDto : MessageDto
{
    /// <inheritdoc />
    public override int Type => HubProtocolConstants.InvocationMessageType;

    /// <summary>
    /// Gets or sets the headers.
    /// </summary>
    public Dictionary<string, string>? Headers { get; set; }

    /// <summary>
    /// Gets or sets the invocation identifier.
    /// </summary>
    public string? InvocationId { get; set; }

    /// <summary>
    /// Gets or sets the target method name.
    /// </summary>
    public string Target { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the method arguments.
    /// </summary>
    public object?[]? Arguments { get; set; }

    /// <summary>
    /// Gets or sets the stream identifiers.
    /// </summary>
    public string[]? StreamIds { get; set; }
}

/// <summary>
/// DTO for stream invocation messages.
/// </summary>
[GenerateShape]
public partial record StreamInvocationMessageDto : MessageDto
{
    /// <inheritdoc />
    public override int Type => HubProtocolConstants.StreamInvocationMessageType;

    /// <summary>
    /// Gets or sets the headers.
    /// </summary>
    public Dictionary<string, string>? Headers { get; set; }

    /// <summary>
    /// Gets or sets the invocation identifier.
    /// </summary>
    public string InvocationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target method name.
    /// </summary>
    public string Target { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the method arguments.
    /// </summary>
    public object?[]? Arguments { get; set; }

    /// <summary>
    /// Gets or sets the stream identifiers.
    /// </summary>
    public string[]? StreamIds { get; set; }
}

/// <summary>
/// DTO for completion messages.
/// </summary>
[GenerateShape]
public partial record CompletionMessageDto : MessageDto
{
    /// <inheritdoc />
    public override int Type => HubProtocolConstants.CompletionMessageType;

    /// <summary>
    /// Gets or sets the headers.
    /// </summary>
    public Dictionary<string, string>? Headers { get; set; }

    /// <summary>
    /// Gets or sets the invocation identifier.
    /// </summary>
    public string InvocationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error message, if any.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Gets or sets the result value.
    /// </summary>
    public object? Result { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this completion has a result.
    /// </summary>
    public bool HasResult { get; set; }
}

/// <summary>
/// DTO for stream item messages.
/// </summary>
[GenerateShape]
public partial record StreamItemMessageDto : MessageDto
{
    /// <inheritdoc />
    public override int Type => HubProtocolConstants.StreamItemMessageType;

    /// <summary>
    /// Gets or sets the headers.
    /// </summary>
    public Dictionary<string, string>? Headers { get; set; }

    /// <summary>
    /// Gets or sets the invocation identifier.
    /// </summary>
    public string InvocationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the stream item.
    /// </summary>
    public object? Item { get; set; }
}

/// <summary>
/// DTO for cancel invocation messages.
/// </summary>
[GenerateShape]
public partial record CancelInvocationMessageDto : MessageDto
{
    /// <inheritdoc />
    public override int Type => HubProtocolConstants.CancelInvocationMessageType;

    /// <summary>
    /// Gets or sets the headers.
    /// </summary>
    public Dictionary<string, string>? Headers { get; set; }

    /// <summary>
    /// Gets or sets the invocation identifier.
    /// </summary>
    public string InvocationId { get; set; } = string.Empty;
}

/// <summary>
/// Constants for SignalR hub protocol message types.
/// </summary>
internal static class HubProtocolConstants
{
    internal const int InvocationMessageType = 1;
    internal const int StreamInvocationMessageType = 4;
    internal const int CompletionMessageType = 3;
    internal const int StreamItemMessageType = 2;
    internal const int CancelInvocationMessageType = 5;
    internal const int PingMessageType = 6;
    internal const int CloseMessageType = 7;
}